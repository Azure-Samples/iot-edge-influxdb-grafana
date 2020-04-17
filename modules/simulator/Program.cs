namespace Simulator
{
    using System.Threading.Tasks;
    using ModuleWrapper;
    using Microsoft.Extensions.DependencyInjection;
    
    class Program
    {
        static async Task Main(string[] args)
        {
            await ModuleHost.Run<SimulatorModule>(args,
                (services, configuration) =>
                {
                    if (configuration["Environment"] == "Debug")
                        services.AddSingleton<IModuleClient, MockModuleClientProxy>();
                    else
                        services.AddSingleton<IModuleClient, ModuleClientProxy>();
                });
        }
    }
}
