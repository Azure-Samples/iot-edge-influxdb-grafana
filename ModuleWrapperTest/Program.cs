namespace ModuleWrapperTest
{
    using ModuleWrapper;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Azure.Devices.Client;
    using System.Text;

    class Program
    {
        static async Task Main(string[] args)
        {
            await ModuleHost.Run<TestModule>(args,
                (services, configuration) =>
                {
                    services.AddSingleton<IModuleClient, MockModuleClientProxy>();
                    
                    services.AddHostedService(x =>
                         new MockMessageInjector(x.GetRequiredService<IConfiguration>(),
                             x.GetRequiredService<IModuleClient>(),
                             async (config, client) =>
                                 await client.SendEventAsync("input", new Message(Encoding.UTF8.GetBytes("Sample Message")))));
                });
        }
    }
}
