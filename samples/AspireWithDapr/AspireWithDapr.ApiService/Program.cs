using System.Diagnostics;
using System.Text.Json.Serialization;

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
    try
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

        return Results.Ok(forecast);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception: {ex}");
        app.Logger.LogInformation($"Exception: {ex}");
    }

    return Results.Ok("");
});

//string baseURL = (Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost") + ":" + (Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500"); //reconfigure cpde to make requests to Dapr sidecar
const string PUBSUBNAME = "orderpubsub";
const string TOPIC = "orders";

// Register Dapr pub/sub subscriptions
app.MapGet("/dapr/subscribe", () =>
{
    var sub = new DaprSubscription(PubsubName: PUBSUBNAME, Topic: TOPIC, Route: TOPIC);
    Console.WriteLine($"Dapr pub/sub is subscribed to: {sub}");
    return Results.Json(new DaprSubscription[] { sub });
});

// Dapr subscription in /dapr/subscribe sets up this route
app.MapPost("/orders", (DaprData<Order> requestData) =>
{
    int orderId = requestData.Data.OrderId;
    Console.WriteLine($"Subscriber received Order Id: {orderId}");
    app.Logger.LogInformation($"Subscriber received Order Id: {orderId}");
    var activity = Activity.Current;
    activity?.SetTag("orderId", orderId);

    if (orderId % 10 == 0)
    {
        // Simulate an error for roughly every 10th order
        activity?.SetStatus(ActivityStatusCode.Error, "Something bad happened!");
        throw new Exception($"Simulated error for order id: {orderId}");
    }

    if (20 <= orderId && orderId <= 40)
    {
        // Simulate a delay for order IDs 20-40
        Random random = new Random();
        int randomDelay = random.Next(500, 1900); // Generates a random number between 1 and 100
        Thread.Sleep(randomDelay);
    }

    return Results.Ok(requestData.Data);
});

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public record DaprData<T>([property: JsonPropertyName("data")] T Data);
public record Order([property: JsonPropertyName("orderId")] int OrderId);
public record DaprSubscription(
  [property: JsonPropertyName("pubsubname")] string PubsubName,
  [property: JsonPropertyName("topic")] string Topic,
  [property: JsonPropertyName("route")] string Route);
