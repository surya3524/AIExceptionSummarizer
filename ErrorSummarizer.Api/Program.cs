using ErrorSummarizer.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register summarizer services
builder.Services.AddSingleton<ErrorSummarizer.Api.Services.IErrorSummarizer, ErrorSummarizer.Api.Services.BasicHeuristicErrorSummarizer>();

builder.Services.AddControllers();

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
