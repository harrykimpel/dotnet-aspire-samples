using Aspire.Hosting.Dapr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

// OpenTelemetry
var NEW_RELIC_REGION = Environment.GetEnvironmentVariable("NEW_RELIC_REGION");
string OTEL_EXPORTER_OTLP_ENDPOINT = "https://otlp.nr-data.net";
if (NEW_RELIC_REGION != null &&
    NEW_RELIC_REGION != "" &&
    NEW_RELIC_REGION == "EU")
{
    OTEL_EXPORTER_OTLP_ENDPOINT = "https://otlp.eu01.nr-data.net";
}
var NEW_RELIC_LICENSE_KEY = Environment.GetEnvironmentVariable("NEW_RELIC_LICENSE_KEY");
string OTEL_EXPORTER_OTLP_HEADERS = "api-key=" + NEW_RELIC_LICENSE_KEY;
//string OTEL_EXPORTER_OTLP_PROTOCOL = "http/protobuf";

builder.AddProject<Projects.AspireWithDapr_ApiService>("api")
    .WithDaprSidecar(options =>
    {
        options.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", OTEL_EXPORTER_OTLP_ENDPOINT);
        options.WithEnvironment("OTEL_EXPORTER_OTLP_HEADERS", OTEL_EXPORTER_OTLP_HEADERS);
        //options.WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", OTEL_EXPORTER_OTLP_PROTOCOL);
    })
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", OTEL_EXPORTER_OTLP_ENDPOINT)
    .WithEnvironment("OTEL_EXPORTER_OTLP_HEADERS", OTEL_EXPORTER_OTLP_HEADERS);
//.WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", OTEL_EXPORTER_OTLP_PROTOCOL)
//.WithEnvironment("OTEL_SERVICE_NAME", "apiservice");

builder.AddProject<Projects.AspireWithDapr_Web>("web")
    .WithDaprSidecar(options =>
    {
        options.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", OTEL_EXPORTER_OTLP_ENDPOINT);
        options.WithEnvironment("OTEL_EXPORTER_OTLP_HEADERS", OTEL_EXPORTER_OTLP_HEADERS);
        //options.WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", OTEL_EXPORTER_OTLP_PROTOCOL);
    })
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", OTEL_EXPORTER_OTLP_ENDPOINT)
    .WithEnvironment("OTEL_EXPORTER_OTLP_HEADERS", OTEL_EXPORTER_OTLP_HEADERS);
//.WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", OTEL_EXPORTER_OTLP_PROTOCOL)
//.WithEnvironment("OTEL_SERVICE_NAME", "webfrontend");

// Workaround for https://github.com/dotnet/aspire/issues/2219
if (builder.Configuration.GetValue<string>("DAPR_CLI_PATH") is { } daprCliPath)
{
    builder.Services.Configure<DaprOptions>(options =>
    {
        options.DaprPath = daprCliPath;
    });
}

builder.Build().Run();

// Workaround for https://github.com/dotnet/aspire/issues/5089#issuecomment-2258058030
public static class DaprSidecarResourceBuilderExtensions
{
    private const string ConnectionStringEnvironmentName = "ConnectionStrings__";

    public static IResourceBuilder<IDaprSidecarResource> WithReference(this IResourceBuilder<IDaprSidecarResource> builder, IResourceBuilder<IResourceWithConnectionString> component, string? connectionName = null, bool optional = false)
    {
        connectionName ??= component.Resource.Name;

        builder.WithAnnotation(new EnvironmentCallbackAnnotation(context =>
        {
            var connectionStringName = component.Resource.ConnectionStringEnvironmentVariable ?? $"{ConnectionStringEnvironmentName}{connectionName}";
            context.EnvironmentVariables[connectionStringName] = new ConnectionStringReference(component.Resource, optional);
            return Task.CompletedTask;
        }));

        return builder;
    }

    public static IResourceBuilder<IDaprSidecarResource> WithEnvironment(this IResourceBuilder<IDaprSidecarResource> builder, string name, string? value)
    {
        return builder.WithAnnotation(new EnvironmentCallbackAnnotation(name, () => value ?? string.Empty));
    }

    public static IResourceBuilder<IDaprSidecarResource> WithEnvironment(this IResourceBuilder<IDaprSidecarResource> builder, string name, ReferenceExpression value)
    {
        return builder.WithAnnotation(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables[name] = value;
            return Task.CompletedTask;
        }));
    }

    public static IResourceBuilder<IDaprSidecarResource> WithEnvironment(this IResourceBuilder<IDaprSidecarResource> builder, string name, Func<string> callback)
    {
        return builder.WithAnnotation(new EnvironmentCallbackAnnotation(name, callback));
    }

    public static IResourceBuilder<IDaprSidecarResource> WithEnvironment(this IResourceBuilder<IDaprSidecarResource> builder, Action<EnvironmentCallbackContext> callback)
    {
        return builder.WithAnnotation(new EnvironmentCallbackAnnotation(callback));
    }

    public static IResourceBuilder<IDaprSidecarResource> WithEnvironment(this IResourceBuilder<IDaprSidecarResource> builder, Func<EnvironmentCallbackContext, Task> callback)
    {
        return builder.WithAnnotation(new EnvironmentCallbackAnnotation(callback));
    }

    public static IResourceBuilder<IDaprSidecarResource> WithEnvironment(this IResourceBuilder<IDaprSidecarResource> builder, string name, EndpointReference endpointReference)
    {
        return builder.WithAnnotation(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables[name] = endpointReference;
            return Task.CompletedTask;
        }));
    }

    public static IResourceBuilder<IDaprSidecarResource> WithEnvironment(this IResourceBuilder<IDaprSidecarResource> builder, string name, IResourceBuilder<ParameterResource> parameter)
    {
        return builder.WithAnnotation(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables[name] = parameter.Resource;
            return Task.CompletedTask;
        }));
    }

    public static IResourceBuilder<IDaprSidecarResource> WithEnvironment(this IResourceBuilder<IDaprSidecarResource> builder, string envVarName, IResourceBuilder<IResourceWithConnectionString> resource)
    {
        return builder.WithAnnotation(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables[envVarName] = new ConnectionStringReference(resource.Resource, optional: false);
            return Task.CompletedTask;
        }));
    }
}