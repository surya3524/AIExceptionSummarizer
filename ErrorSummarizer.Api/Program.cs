using ErrorSummarizer.Api.Middleware;
using ErrorSummarizer.Api.Services;
using ErrorSummarizer.Api.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Options from configuration
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection("Llm"));

// Core services
builder.Services.AddSingleton<IRedactionService, BasicRedactionService>();
// Register OpenAI client
builder.Services.AddHttpClient<ILlmClient, OpenAiLlmClient>();
// LLM summarizer only (no fallback now) - provide a minimal wrapper implementation
builder.Services.AddSingleton<IErrorSummarizer, LlmOnlyErrorSummarizer>();

builder.Services.AddControllers();
builder.Services.AddHttpClient("LlmClient", (sp, client) =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LlmOptions>>().Value;
    client.BaseAddress = new Uri(opts.Endpoint);
    client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", opts.ApiKey);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Add custom error summarizer middleware early (after HTTPS + before routing)
app.UseErrorSummarizer();

app.MapControllers();

app.Run();

// Remove inline WeatherForecast sample record/controller to simplify

public partial class Program { }public partial class Program { }