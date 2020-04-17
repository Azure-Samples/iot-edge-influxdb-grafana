using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ModuleWrapper;
using Serilog;
using Serilog.Context;
using System.Threading;
using System.Threading.Tasks;

namespace ModuleWrapperTest
{
    public class TestModule : IHostedService
    {
        public IConfiguration Configuration { get; }
        public IModuleClient ModuleClient { get; }
        public CancellationTokenSource CancellationTokenSource { get; }

        public TestModule(IConfiguration configuration,
            IModuleClient moduleClient,
            CancellationTokenSource cancellationTokenSource)
        {
            Configuration = configuration;
            ModuleClient = moduleClient;
            CancellationTokenSource = cancellationTokenSource;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (LogContext.PushProperty("FunctionId", nameof(StartAsync)))
            {
                Log.Information("Opening Edge Module Connection");
                await ModuleClient.OpenAsync();

                Log.Information("Beginning to Process Messages");

                await ModuleClient.SetInputMessageHandlerAsync("input",
                    new MessageHandler(async (message, context) =>
                    {
                        Log.Information("Received message in input route.");
                        return await Task.FromResult(MessageResponse.Completed);
                    }), ModuleClient);
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
