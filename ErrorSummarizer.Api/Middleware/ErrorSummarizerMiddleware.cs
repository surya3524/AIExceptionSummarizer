using System.Net;
using System.Text.Json;
using ErrorSummarizer.Api.Services;

namespace ErrorSummarizer.Api.Middleware;

public class ErrorSummarizerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IErrorSummarizer _summarizer;
    private readonly ILogger<ErrorSummarizerMiddleware> _logger;

    public ErrorSummarizerMiddleware(RequestDelegate next, IErrorSummarizer summarizer, ILogger<ErrorSummarizerMiddleware> logger)
    {
        _next = next;
        _summarizer = summarizer;
        _logger = logger;
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
            var summary = await _summarizer.SummarizeAsync(ex, correlationId);
            _logger.LogError(ex, "{Summary}", summary);
            await WriteProblemDetailsAsync(context, ex, summary, correlationId);
        }
    }

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

    private static async Task WriteProblemDetailsAsync(HttpContext context, Exception ex, string summary, string correlationId)
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
            detail = ex.Message,
            summary,
            exceptionType = ex.GetType().FullName,
            stackTraceTop = ex.StackTrace?.Split('\n').FirstOrDefault()?.Trim()
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}

public static class ErrorSummarizerMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorSummarizer(this IApplicationBuilder app)
        => app.UseMiddleware<ErrorSummarizerMiddleware>();
}