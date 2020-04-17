
namespace ModuleWrapper
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;

    public interface IModuleClient
    {
        Task OpenAsync();
        Task CloseAsync();
        Task SendEventAsync(string outputName, Message message);
        Task SetInputMessageHandlerAsync(string inputName, MessageHandler messageHandler, object userContext);
        Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext);
        Task<Twin> GetTwinAsync(CancellationToken cancellationToken);
        Task<Twin> GetTwinAsync();
    }
}
