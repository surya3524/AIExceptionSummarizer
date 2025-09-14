using System.Net;
using System.Text;
using System.Text.Json;
using ErrorSummarizer.Api.Services;

namespace ErrorSummarizer.Api.Middleware;

public class ErrorSummarizerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IErrorSummarizer _summarizer;
    private readonly ILogger<ErrorSummarizerMiddleware> _logger;
    private readonly IRedactionService _redactor;

    private static readonly string[] HeaderAllowList = ["User-Agent", "Accept", "Content-Type", "X-Correlation-Id"];
    private static readonly string[] ClaimAllowList = ["sub", "name", "oid"];

    public ErrorSummarizerMiddleware(RequestDelegate next, IErrorSummarizer summarizer, ILogger<ErrorSummarizerMiddleware> logger, IRedactionService redactor)
    {
        _next = next;
        _summarizer = summarizer;
        _logger = logger;
        _redactor = redactor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = EnsureCorrelationId(context);
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var errCtx = await BuildContextAsync(context, ex, correlationId);
            string summary;
            try
            {
                summary = await _summarizer.SummarizeAsync(ex, errCtx, context.RequestAborted);
            }
            catch (Exception summarizerEx)
            {
                summary = $"Heuristic fallback. Summarizer failure: {summarizerEx.Message}";
            }
            _logger.LogError(ex, "Error CorrelationId={CorrelationId} Type={Type} Route={Method} {Route} Summary={Summary}", correlationId, ex.GetType().Name, context.Request.Method, context.Request.Path, summary);
            await WriteProblemDetailsAsync(context, ex, correlationId);
        }
    }

    private async Task<ErrorContext> BuildContextAsync(HttpContext ctx, Exception ex, string correlationId)
    {
        string? body = null;
        if (CanReadBody(ctx))
        {
            try
            {
                ctx.Request.EnableBuffering();
                using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, leaveOpen: true);
                var raw = await reader.ReadToEndAsync();
                ctx.Request.Body.Position = 0;
                body = _redactor.RedactBody(raw);
            }
            catch { /* swallow body read problems */ }
        }

        var headers = _redactor.RedactHeaders(ctx.Request.Headers, HeaderAllowList);
        var claims = ctx.User?.Identity?.IsAuthenticated == true ? _redactor.RedactClaims(ctx.User.Claims, ClaimAllowList) : new Dictionary<string,string>();

        var frames = ex.StackTrace?.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.StartsWith("at "))
            .Take(8)
            .ToList();
        var top = frames?.FirstOrDefault();
        var appFrames = frames?.Where(f => !f.Contains("Microsoft.") && !f.Contains("System."))
            .Take(5)
            .ToList();
        return new ErrorContext
        {
            CorrelationId = correlationId,
            Method = ctx.Request.Method,
            Route = ctx.Request.Path.Value ?? string.Empty,
            Headers = new Dictionary<string,string>(headers),
            Claims = new Dictionary<string,string>(claims!),
            SanitizedBody = body,
            EnvironmentName = ctx.RequestServices.GetService<IHostEnvironment>()?.EnvironmentName,
            UserAgent = headers.TryGetValue("User-Agent", out var ua) ? ua : null,
            TopStackFrame = top,
            TopAppFrames = appFrames
        };
    }

    private static bool CanReadBody(HttpContext ctx)
        => ctx.Request.ContentLength is > 0 && (ctx.Request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true || ctx.Request.ContentType?.Contains("text", StringComparison.OrdinalIgnoreCase) == true) && string.Equals(ctx.Request.Method, "GET", StringComparison.OrdinalIgnoreCase) == false;

    private static string EnsureCorrelationId(HttpContext ctx)
    {
        if (!ctx.Request.Headers.TryGetValue("X-Correlation-Id", out var existing) || string.IsNullOrWhiteSpace(existing))
        {
            var generated = Guid.NewGuid().ToString("n");
            ctx.Request.Headers["X-Correlation-Id"] = generated;
            ctx.Response.Headers["X-Correlation-Id"] = generated;
            return generated;
        }
        else
        {
            var id = existing.ToString();
            ctx.Response.Headers["X-Correlation-Id"] = id;
            return id;
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, Exception ex, string correlationId)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = "https://httpstatuses.com/500",
            title = "An unexpected error occurred",
            status = 500,
            correlationId,
            detail = "Unhandled server error.",
            exceptionType = ex.GetType().FullName
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}

public static class ErrorSummarizerMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorSummarizer(this IApplicationBuilder app)
        => app.UseMiddleware<ErrorSummarizerMiddleware>();
}