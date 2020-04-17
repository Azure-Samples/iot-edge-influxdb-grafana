
namespace ModuleWrapper
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Amqp.Framing;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;
    using Serilog;

    public class MockModuleClientProxy : IModuleClient
    {
        public Dictionary<string, List<Message>> MessageQueues { get; private set; }
        public Dictionary<string, ValueTuple<MessageHandler, object>> InputMessageHandlers { get; private set; }
        public Dictionary<string, MethodCallback> MethodMessageHandlers { get; private set; }
        public TaskTimer TaskTimer { get; private set; }
        public CancellationTokenSource CancellationTokenSource { get; private set; }

        public MockModuleClientProxy(CancellationTokenSource cancellationTokenSource)
        {
            CancellationTokenSource = cancellationTokenSource;

            MessageQueues = new Dictionary<string, List<Message>>();
            InputMessageHandlers = new Dictionary<string, (MessageHandler, object)>();
            MethodMessageHandlers = new Dictionary<string, MethodCallback>();
            TaskTimer = new TaskTimer(OnTimer,
                TimeSpan.FromSeconds(1),
                CancellationTokenSource);
        }

        private void OnTimer()
        {
            lock (MessageQueues)
                foreach (var queue in MessageQueues)
                {
                    if (InputMessageHandlers.ContainsKey(queue.Key))
                        foreach (var message in queue.Value)
                            InputMessageHandlers[queue.Key].Item1(message, InputMessageHandlers[queue.Key].Item2);
                    MessageQueues[queue.Key].Clear();
                }
            // TODO: Process method messsages too
        }

        public async Task SendEventAsync(string outputName, Message message)
        {
            lock (MessageQueues)
            {
                if (!MessageQueues.ContainsKey(outputName))
                    MessageQueues[outputName] = new List<Message>();
                MessageQueues[outputName].Add(message);
            }
            Log.Information($"Message Sent to {outputName}");
            await Task.FromResult(0);
        }

        public async Task SetInputMessageHandlerAsync(string inputName, MessageHandler messageHandler, object userContext)
        {
            InputMessageHandlers[inputName] = (messageHandler, userContext);

            Log.Information($"Message Handler Set for {inputName}");
            await Task.FromResult(0);
        }

        public async Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext)
        {
            MethodMessageHandlers[methodName] = methodHandler;

            Log.Information($"Method Handler Set for {methodName}");
            await Task.FromResult(0);
        }

        public async Task OpenAsync()
        {
            Log.Information("Opened ModuleClient");
            TaskTimer.Start();

            await Task.FromResult(0);
        }

        public async Task CloseAsync()
        {
            Log.Information("Closed ModuleClient");
            CancellationTokenSource.Cancel();

            await Task.FromResult(0);
        }

        public async Task<Twin> GetTwinAsync(CancellationToken cancellationToken)
        {
            Log.Information("GetTwinAsync");
            return await Task.FromResult<Twin>(null);
        }

        public async Task<Twin> GetTwinAsync()
        {
            Log.Information("GetTwinAsync");
            return await Task.FromResult<Twin>(null);
        }
    }
}
