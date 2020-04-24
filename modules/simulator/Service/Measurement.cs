
namespace Simulator.Service
{
    using Newtonsoft.Json;
    public class Measurement
    {

        [JsonProperty(PropertyName = "Timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty(PropertyName = "MachineId")]
        public string MachineId { get; set; }

        [JsonProperty(PropertyName = "MachineTemperature")]
        public string MachineTemperature { get; set; }

        [JsonProperty(PropertyName = "AmbientTemperature")]
        public string AmbientTemperature { get; set; }

        [JsonProperty(PropertyName = "ConveyorBeltSpeed")]
        public string ConveyorBeltSpeed { get; set; }

        
        [JsonProperty(PropertyName = "GearTension")]
        public string GearTension { get; set; }

        [JsonProperty(PropertyName = "WorkerDetected")]
        public string WorkerDetected { get; set; }
    }
}

