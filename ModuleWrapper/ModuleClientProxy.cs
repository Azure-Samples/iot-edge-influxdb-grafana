
namespace ModuleWrapper
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared;
    using Serilog;

    public class ModuleClientProxy : IModuleClient
    {
        private ModuleClient ModuleClient { get; }
        public CancellationTokenSource CancellationTokenSource { get; private set; }

        public ModuleClientProxy(
            CancellationTokenSource cancellationTokenSource)
        {
            CancellationTokenSource = cancellationTokenSource;

            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            ModuleClient = ModuleClient.CreateFromEnvironmentAsync(settings).Result; ;
            Log.Information("Created ModuleClient From Environment");
        }

        public async Task SendEventAsync(string outputName, Message message)
        {
            await ModuleClient.SendEventAsync(outputName, message);
            Log.Information($"Message Sent to {outputName}");
        }

        public async Task SetInputMessageHandlerAsync(string inputName, MessageHandler messageHandler, object userContext)
        {
            await ModuleClient.SetInputMessageHandlerAsync(inputName, messageHandler, userContext);
            Log.Information($"Message Handler Set for {inputName}");
        }

        public async Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext)
        {
            await ModuleClient.SetMethodHandlerAsync(methodName, methodHandler, userContext);
            Log.Information($"Method Handler Set for {methodName}");
        }

    
        public async Task OpenAsync()
        {            
            await ModuleClient.OpenAsync();
            Log.Information("Opened ModuleClient");
        }

        public async Task CloseAsync()
        {
            await ModuleClient.CloseAsync();
            Log.Information("Closed ModuleClient");
        }

        public async Task<Twin> GetTwinAsync(CancellationToken cancellationToken)
        {
            return await ModuleClient.GetTwinAsync(cancellationToken);            
        }

        public async Task<Twin> GetTwinAsync()
        {
            return await ModuleClient.GetTwinAsync();
        }
    }
}
