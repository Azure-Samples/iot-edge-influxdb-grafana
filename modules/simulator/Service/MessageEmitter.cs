namespace Simulator.Service
{
    using Microsoft.Azure.Devices.Client;
    using ModuleWrapper;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class MessageEmitter
    {
        int MessageIndex;
        int IntervalInMilliseconds;

        IModuleClient ModuleClient { get; }
        List<Measurement> Messages { get; set; }
        CancellationTokenSource CancellationTokenSource { get; }

        TaskTimer TaskTimer { get; set; }

        public MessageEmitter(IModuleClient moduleClient, CancellationTokenSource cancellationTokenSource)
        {
            MessageIndex = 0;
            IntervalInMilliseconds = 1000;
            ModuleClient = moduleClient;
            CancellationTokenSource = cancellationTokenSource;
        }

        public async Task Init()
        {
            var twin = await ModuleClient.GetTwinAsync();
            if (twin!= null && twin.Properties.Desired.Contains("IntervalInMilliseconds"))
                int.TryParse(twin.Properties.Desired["IntervalInMilliseconds"], out IntervalInMilliseconds);

            Messages = File.ReadAllLines("sampleData.csv")
                                          .Skip(1)
                                          .Select(v => CreateMeasurementFromCsvLine(v))
                                          .Where(v => v != null)
                                          .ToList();

            Debug.Assert(Messages.Count > 0);

            Log.Information($"Loaded {Messages.Count} messages.");

            TaskTimer = new TaskTimer(async () => await EmitMessage(),
                                        TimeSpan.FromMilliseconds(IntervalInMilliseconds),
                                        CancellationTokenSource);
        }

        /// <summary>
        /// This method is the main message pump
        /// It starts a Task based timer that periodically emits a message
        /// </summary>
        public void Start()
        {
            // Start the timer
            TaskTimer.Start();
        }

        public async Task EmitMessage()
        {
            if (MessageIndex >= Messages.Count)
                MessageIndex = 0;

            Log.Information($"Emitting telemetry message {MessageIndex}");

            var message = Messages[MessageIndex++];
            message.Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();

            var json = JsonConvert.SerializeObject(message, Formatting.Indented);
            using (var iotMessage = new Message(Encoding.UTF8.GetBytes(json.Replace("NaN", ""))))
            {
                await ModuleClient.SendEventAsync("telemetry", iotMessage);
            }
        }

        private Measurement CreateMeasurementFromCsvLine(string line)
        {
            try
            {
                var data = line.Split(',');
                var sampleMeasurement = new Measurement()
                {
                    MachineId = data[1],
                    Sin1 = data[2],
                    Sin2 = data[3],
                    Saw1 = data[4],
                    Saw2 = data[5],
                    Square = data[6]
                };
                return sampleMeasurement;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error converting csv line {line} to object");
            }
            return null;
        }
    }
}
