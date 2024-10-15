---
languages:
  - csharp
products:
  - dotnet
  - dotnet-aspire
page_type: sample
name: ".NET Aspire dapr sample app"
urlFragment: "aspire-dapr"
description: "A sample .NET Aspire app that shows how to use dapr"
---

# .NET Aspire dapr sample app

This is a simple .NET app that shows how to use Dapr with .NET Aspire orchestration.

## Demonstrates

- How to configure a .NET Aspire app to work with Dapr

## Sample prerequisites

Dapr installation instructions can be found [here](https://docs.dapr.io/getting-started/install-dapr-cli/). After installing the Dapr CLI, remember to run `dapr init` as described [here](https://docs.dapr.io/getting-started/install-dapr-selfhost/).

This sample is written in C# and targets .NET 8.0. It requires the [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later.

## Building the sample

To download and run the sample, follow these steps:

1. Clone the `dotnet/aspire-samples` repository.
2. In Visual Studio (2022 or later):
   1. On the menu bar, choose **File** > **Open** > **Project/Solution**.
   2. Navigate to the folder that holds the sample code, and open the solution (.sln) file.
   3. Right click the _AspireWithDapr.AppHost_ project in the solution explore and choose it as the startup project.
   4. Choose the <kbd>F5</kbd> key to run with debugging, or <kbd>Ctrl</kbd>+<kbd>F5</kbd> keys to run the project without debugging.
3. From the command line:
   1. Navigate to the folder that holds the sample code.
   2. At the command line, type [`dotnet run`](https://docs.microsoft.com/dotnet/core/tools/dotnet-run).

## Run the sample app with Dapr CLI

To run the app with New Relic as your observability backend, configure your environment variables and run the Dapr CLI by executing the following at the command prompt (opened to the base directory of the sample):

```bash
export NEW_RELIC_LICENSE_KEY=MY_NEW_RELIC_LICENSE_KEY
export OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp.nr-data.net:4317
export OTEL_EXPORTER_OTLP_HEADERS="api-key={$NEW_RELIC_LICENSE_KEY}"

export NEW_RELIC_ACCOUNT_ID=MY_NEW_RELIC_ACCOUNT_ID
export NEW_RELIC_INSIGHTS_INSERT_KEY=MY_NEW_RELIC_INSIGHTS_INSERT_KEY

dapr run -f .
```

## Run the sample app with leveraging .NET Aspire

To run the app, run the .NET Aspire app by executing the following at the command prompt (opened to the base directory of the sample):

```bash
export NEW_RELIC_LICENSE_KEY=MY_NEW_RELIC_LICENSE_KEY
export OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp.nr-data.net:4317
export OTEL_EXPORTER_OTLP_HEADERS="api-key={$NEW_RELIC_LICENSE_KEY}"

export NEW_RELIC_ACCOUNT_ID=MY_NEW_RELIC_ACCOUNT_ID
export NEW_RELIC_INSIGHTS_INSERT_KEY=MY_NEW_RELIC_INSIGHTS_INSERT_KEY

dotnet run --project AspireWithDapr.AppHost
```

1. On the **Resources** page, click on one of the endpoints for the listed project. This launches the simple .NET app.
1. In the .NET web app:
   1. Visit the **Weather**.
   1. Visit the **Orders**.

For more information about using dapr, see the [Dapr documentation](https://docs.dapr.io/developing-applications/sdks/dotnet/).
