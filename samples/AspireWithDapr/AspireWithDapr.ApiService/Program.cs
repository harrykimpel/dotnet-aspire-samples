using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    app.Logger.LogInformation("Receiving request for weather forecast.");
    int maxRange = 5;
    var forecast = Enumerable.Range(1, maxRange).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();

    var activity = Activity.Current;
    activity?.SetTag("amountWeatherForecasts", forecast.Length);

    int totalTempC = 0;
    int totalTempF = 0;
    foreach (WeatherForecast fc in forecast)
    {
        totalTempC += fc.TemperatureC;
        totalTempF += fc.TemperatureF;
    }
    float avgTempC = (float)totalTempC / (float)maxRange;
    float avgTempF = (float)totalTempF / (float)maxRange;
    activity?.SetTag("avgTemperatureC", avgTempC);
    activity?.SetTag("avgTemperatureF", avgTempF);
    app.Logger.LogInformation($"The average temperature in the next {maxRange} days is {avgTempC} C or {avgTempF} F.");

    return forecast;
});

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
