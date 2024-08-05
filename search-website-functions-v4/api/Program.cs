using Microsoft.Extensions.Hosting;
// Application Insights SDK: https://www.nuget.org/packages/Microsoft.ApplicationInsights.Web 

var telemetryClient = new TelemetryClient();
telemetryClient.InstrumentationKey = "2ec35943-37ff-45d9-8914-9efc8e3fe41b";

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
