using Dapr.Client;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AspireWithDapr.Web;

public class WeatherApiClient(DaprClient daprClient)
{
    string baseURL = (Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost") + ":" + (Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500"); //reconfigure cpde to make requests to Dapr sidecar
    const string PUBSUBNAME = "orderpubsub";
    const string TOPIC = "orders";

    public async Task<WeatherForecast[]> GetWeatherAsync()
    {
        Console.WriteLine("Making request to weather API ...");

        return await daprClient.InvokeMethodAsync<WeatherForecast[]>(HttpMethod.Get, "api", "weatherforecast");
    }

    public async Task<bool> SubmitOrderAsync(int randomCustomerId, int randomOrderId)
    {
        Console.WriteLine("Submitting order ...");

        Console.WriteLine($"Publishing to baseURL: {baseURL}, Pubsub Name: {PUBSUBNAME}, Topic: {TOPIC} ");

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var order = new Order(randomCustomerId, randomOrderId);
        var orderJson = JsonSerializer.Serialize<Order>(order);
        var content = new StringContent(orderJson, Encoding.UTF8, "application/json");

        // Publish an event/message using Dapr PubSub via HTTP Post
        var response = await httpClient.PostAsync($"{baseURL}/v1.0/publish/{PUBSUBNAME}/{TOPIC}", content);

        return true;
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public record Order([property: JsonPropertyName("customerId")] int CustomerId, [property: JsonPropertyName("orderId")] int OrderId);