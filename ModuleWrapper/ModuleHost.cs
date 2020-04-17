namespace ModuleWrapper
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Serilog;
    using Microsoft.Extensions.Configuration;
    using Serilog.Context;
    using System.Reflection;
    using System.Threading;
    using System.Runtime.Loader;

    public static class ModuleHost
    {
        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), taskCompletionSource);
            return taskCompletionSource.Task;
        }

        public delegate void ConfigureServices(IServiceCollection collection, IConfiguration configuration);

        public static async Task Run<THostedService>(string[] args, ConfigureServices configureServices = null)
            where THostedService : class, IHostedService
        {
            Serilog.Debugging.SelfLog.Enable(Console.Error);

            var cancellationTokenSource = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (cts) => cancellationTokenSource.Cancel();
            Console.CancelKeyPress += (sender, cts) => cancellationTokenSource.Cancel();

            var assembly = Assembly.GetExecutingAssembly();
            IConfiguration configuration;
            using (var resourceStream = assembly.GetManifestResourceStream("ModuleWrapper.SerilogSettings.json"))
            {
                configuration = new ConfigurationBuilder()
                    .AddJsonStream(resourceStream)
                    .AddJsonFile("SerilogSettings.json", true)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args)
                    .Build();
            }
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            LogContext.PushProperty("ModuleId", typeof(THostedService).Name);
            LogContext.PushProperty("FunctionId", nameof(Run));

            var host = new HostBuilder()
                .UseDefaultServiceProvider((context, options) => {
                    options.ValidateOnBuild = true;
                })
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddJsonFile("hostsettings.json", optional: true);
                    configHost.AddEnvironmentVariables();
                    configHost.AddCommandLine(args);
                })
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.AddJsonFile("appsettings.json", optional: true);
                    configApp.AddJsonFile(
                        $"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
                        optional: true);
                    configApp.AddEnvironmentVariables();
                    configApp.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<THostedService>();
                    services.AddSingleton(cancellationTokenSource);

                    configureServices?.Invoke(services, hostContext.Configuration);
                })
                .UseSerilog()
                .UseConsoleLifetime()                                
                .Build();
            
            await Task.WhenAny(host.RunAsync(), WhenCancelled(cancellationTokenSource.Token));

            Log.Information("Exiting..");
        }
    }
}