
namespace Simulator.Service
{
    using Newtonsoft.Json;
    public class Measurement
    {

        [JsonProperty(PropertyName = "Timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty(PropertyName = "MachineId")]
        public string MachineId { get; set; }

        [JsonProperty(PropertyName = "Sin1")]
        public string Sin1 { get; set; }

        [JsonProperty(PropertyName = "Sin2")]
        public string Sin2 { get; set; }

        [JsonProperty(PropertyName = "Saw1")]
        public string Saw1 { get; set; }

        
        [JsonProperty(PropertyName = "Saw2")]
        public string Saw2 { get; set; }

        [JsonProperty(PropertyName = "Square")]
        public string Square { get; set; }
    }
}

