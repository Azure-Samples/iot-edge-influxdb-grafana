namespace ModuleWrapper
{
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    public delegate Task InjectMessageAsyncDelegate(IConfiguration configuration, IModuleClient moduleClient);
    public class MockMessageInjector : IHostedService
    {
        public IConfiguration Configuration { get; }
        public IModuleClient ModuleClient { get; }
        public InjectMessageAsyncDelegate InjectMessage { get; }
        public MockMessageInjector(IConfiguration configuration,
            IModuleClient moduleClient,
            InjectMessageAsyncDelegate injectMessage)
        {
            Configuration = configuration;
            ModuleClient = moduleClient;
            InjectMessage = injectMessage;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // TODO: Make this repeating
            await InjectMessage(Configuration, ModuleClient);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.FromResult(0);
        }
    }
}