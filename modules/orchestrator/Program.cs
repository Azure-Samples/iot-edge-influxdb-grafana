namespace Orchestrator
{
    using System;
    using System.Threading.Tasks;
    using System.Net.Http;

    using ModuleWrapper;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using AdysTech.InfluxDB.Client.Net;
    using Microsoft.Azure.Devices.Client;
    using System.IO;
    using Orchestrator.Mock;
    using Orchestrator.Abstraction;
    using Orchestrator.Service;

    class Program
    {
        static async Task Main(string[] args)
        {
            await ModuleHost.Run<OrchestratorModule>(args,
                (services, configuration) =>
                {
                    if (configuration["Environment"] == "Debug")
                    {
                        services.AddSingleton<IModuleClient, MockModuleClientProxy>();
                        services.AddHostedService(x =>
                         new MockMessageInjector(x.GetRequiredService<IConfiguration>(),
                             x.GetRequiredService<IModuleClient>(),
                             async (config, client) =>
                                 await client.SendEventAsync("telemetry", new Message(File.ReadAllBytes("Mock/TelemetryMessageSample.json")))));

                        services.AddSingleton<IHttpHandler, MockHttpClientHandler>();
                        services.AddSingleton<ITimeSeriesRecorder, InfluxDBRecorder>();
                        services.AddSingleton<IInfluxDBClient, InfluxDBClient>((e) =>
                           new InfluxDBClient(
                               configuration.GetValue("INFLUX_URL", "http://localhost:8897"),
                               configuration.GetValue("INFLUX_USERNAME", ""),
                               configuration.GetValue("INFLUX_PASSWORD", "")));

                        services.AddSingleton<IHttpHandler, Service.HttpClientHandler>((e) => new Service.HttpClientHandler( "http://localhost:8000/"));
                    }
                    else
                    {
                        services.AddSingleton<IModuleClient, ModuleClientProxy>();
                        // services.AddSingleton<IHttpHandler, Service.HttpClientHandler>((e) => new Service.HttpClientHandler(configuration.GetValue("ML_BASE_URI", "http://ml:5001/")));
                        services.AddSingleton<IHttpHandler, MockHttpClientHandler>();
                        services.AddSingleton<IInfluxDBClient, InfluxDBClient>((e) =>
                           new InfluxDBClient(
                               configuration.GetValue("INFLUX_URL", "http://influxdb:8086"),
                               configuration.GetValue("INFLUX_USERNAME", ""),
                               configuration.GetValue("INFLUX_PASSWORD", "")));

                        services.AddSingleton<ITimeSeriesRecorder, InfluxDBRecorder>();
                    }

                });
        }
    }
}
