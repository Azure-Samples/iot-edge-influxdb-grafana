namespace Simulator
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using ModuleWrapper;
    using Serilog;
    using Serilog.Context;
    using Simulator.Service;
    using System.Threading;
    using System.Threading.Tasks;
    public class SimulatorModule : IHostedService
    {
        public IConfiguration Configuration { get; }
        public IModuleClient ModuleClient{ get; }
        public CancellationTokenSource CancellationTokenSource { get; }

        public SimulatorModule(IConfiguration configuration,
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
                MessageEmitter messageEmitter = new MessageEmitter(ModuleClient, CancellationTokenSource);
                await messageEmitter.Init();
                messageEmitter.Start();
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
