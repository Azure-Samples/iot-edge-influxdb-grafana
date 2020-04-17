namespace Orchestrator
{
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using ModuleWrapper;
    using Newtonsoft.Json.Linq;
    using Orchestrator.Abstraction;
    using Serilog;
    using Serilog.Context;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class OrchestratorModule : IHostedService
    {
        public IConfiguration Configuration { get; }
        public IModuleClient ModuleClient { get; }
        public IHttpHandler HttpClient { get; }
        public ITimeSeriesRecorder TimeSeriesRecorder { get; }
        public CancellationTokenSource CancellationTokenSource { get; }


        public OrchestratorModule(IConfiguration configuration,
            IModuleClient moduleClient,
            IHttpHandler httpClient,
            ITimeSeriesRecorder timeSeriesRecorder,
            CancellationTokenSource cancellationTokenSource)
        {
            Configuration = configuration;
            ModuleClient = moduleClient;
            CancellationTokenSource = cancellationTokenSource;
            HttpClient = httpClient;
            TimeSeriesRecorder = timeSeriesRecorder;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (LogContext.PushProperty("FunctionId", nameof(StartAsync)))
            {
                Log.Information("Opening Edge Module Connection");
                await ModuleClient.OpenAsync();

                Log.Information("Initializing InfluxDBRecorder");
                await TimeSeriesRecorder.InitializeAsync();

                Log.Information("Beginning to Process Messages");
                await ModuleClient.SetInputMessageHandlerAsync("telemetry", new MessageHandler(
                    async (message, e) =>
                    {
                        Log.Information("Processing message..");
                        var telemetryJson = Encoding.UTF8.GetString(message.GetBytes());
                        try
                        {
                            await TimeSeriesRecorder.RecordMessageAsync(telemetryJson);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"Error for message {telemetryJson}");
                        }
                        return await Task.FromResult(MessageResponse.Completed);
                    }
                    ), ModuleClient);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            using (LogContext.PushProperty("FunctionId", nameof(StopAsync)))
            {
                Log.Information("Shutting Down");
                return Task.FromResult(0);
            }
        }
    }
}
