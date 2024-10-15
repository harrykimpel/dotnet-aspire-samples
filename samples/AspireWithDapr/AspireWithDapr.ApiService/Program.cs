using System.Diagnostics;
using System.Text;
using System.Text.Json;
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

// Register Dapr pub/sub subscriptions programmatically
//string baseURL = (Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost") + ":" + (Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500"); //reconfigure cpde to make requests to Dapr sidecar
const string PUBSUBNAME = "orderpubsub";
const string TOPIC = "orders";
const string TOPIC_FAILED = "poisonOrders";
const string ROUTE_FAILED = "failedOrders";

app.MapGet("/dapr/subscribe", () =>
{
    var sub1 = new DaprSubscription(PubsubName: PUBSUBNAME, Topic: TOPIC, Route: TOPIC);
    Console.WriteLine($"Dapr pub/sub is subscribed to: {sub1}");

    var sub2 = new DaprSubscription(PubsubName: PUBSUBNAME, Topic: TOPIC_FAILED, Route: ROUTE_FAILED);
    Console.WriteLine($"Dapr pub/sub is subscribed to: {sub2}");

    return Results.Json(new DaprSubscription[] { sub1, sub2 });
});

// Dapr subscription in /dapr/subscribe sets up this route when configuring it programmatically
app.MapPost("/orders", async (DaprData<Order> requestData) =>
{
    int orderId = requestData.Data.OrderId;
    Console.WriteLine($"Subscriber received Order Id: {orderId}");
    app.Logger.LogInformation($"Subscriber received Order Id: {orderId}");
    var activity = Activity.Current;
    activity?.SetTag("orderId", orderId);

    if (orderId % 10 == 0)
    {
        // Simulate an error for roughly every 10th order
        app.Logger.LogInformation($"Simulated error for order id: {orderId}");
        activity?.SetStatus(ActivityStatusCode.Error, "Something bad happened!");
        // If all retries fail, publish the message to the dead letter topic
        await SendToDeadLetterTopic(requestData.Data, $"Simulated error for order id: {orderId}");
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

async Task SendToDeadLetterTopic(Order order, string errorReason)
{
    string baseURL = (Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost") + ":" + (Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500"); //reconfigure cpde to make requests to Dapr sidecar
    string PUBSUBNAME = "orderpubsub";
    var deadLetterTopic = "poisonOrders"; // Define your Kafka dead letter topic
    Console.WriteLine($"Message sent to dead letter topic: {deadLetterTopic}");

    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

    var orderJson = JsonSerializer.Serialize<Order>(order);
    var content = new StringContent(orderJson, Encoding.UTF8, "application/json");

    // Publish an event/message using Dapr PubSub via HTTP Post
    var response = httpClient.PostAsync($"{baseURL}/v1.0/publish/{PUBSUBNAME}/{deadLetterTopic}", content);
}

app.MapPost("/failedOrders", async (DaprData<Order> requestData) =>
{
    try
    {
        int orderId = requestData.Data.OrderId;
        Console.WriteLine($"Subscriber received failed Order Id: {orderId}");
        app.Logger.LogInformation($"Subscriber received failed Order Id: {orderId}");
        var activity = Activity.Current;
        activity?.SetTag("orderId", orderId);

        // send to New Relic Insights event
        FailedOrder failedOrder = new FailedOrder(eventType: "FailedOrder", OrderId: orderId);
        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        var NEW_RELIC_ACCOUNT_ID = Environment.GetEnvironmentVariable("NEW_RELIC_ACCOUNT_ID");
        var NEW_RELIC_INSIGHTS_INSERT_KEY = Environment.GetEnvironmentVariable("NEW_RELIC_INSIGHTS_INSERT_KEY");
        httpClient.DefaultRequestHeaders.Add("X-Insert-Key", NEW_RELIC_INSIGHTS_INSERT_KEY);
        string url2 = $"https://insights.newrelic.com/v1/accounts/{NEW_RELIC_ACCOUNT_ID}/events";
        var url = $"https://insights-collector.newrelic.com/v1/accounts/{NEW_RELIC_ACCOUNT_ID}/events";

        var response = await httpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize<FailedOrder>(failedOrder), Encoding.UTF8, "application/json"));

        string result = response.Content.ReadAsStringAsync().Result;

        //app.Logger.LogInformation("NR result: " + result);

        if (response.IsSuccessStatusCode)
        {
            app.Logger.LogInformation($"Failed order received (order id: {orderId}) and sent to custom New Relic event for investigation.");
        }
        else
        {
            app.Logger.LogInformation($"Failed to send failed order id: {orderId} to New Relic event.");
            app.Logger.LogInformation($"Response: {response}");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogInformation($"Exception: {ex}");
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
public record FailedOrder([property: JsonPropertyName("eventType")] string eventType, [property: JsonPropertyName("orderId")] int OrderId);
public record DaprSubscription(
  [property: JsonPropertyName("pubsubname")] string PubsubName,
  [property: JsonPropertyName("topic")] string Topic,
  [property: JsonPropertyName("route")] string Route);
