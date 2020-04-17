namespace Orchestrator.Service
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;
    
    [JsonObject(ItemRequired = Required.Always)]
    internal class ORM
    {
        public List<Database> Databases { get; set; }
    }

    [JsonObject(ItemRequired = Required.Always)]
    internal class Database
    {
        public string Name { get; set; }
        public List<Measurement> Measurements { get; set; }
        public string TimestampPayloadMapping { get; set; }
    }

    [JsonObject(ItemRequired = Required.Always)]
    internal class Measurement
    {
        public string Name { get; set; }
        public List<Tag> Tags { get; set; }
        public List<Field> Fields { get; set; }
    }

    [JsonObject(ItemRequired = Required.Always)]
    internal class Tag
    {
        public string Name { get; set; }
        public string PayloadMapping { get; set; }
        public bool AbandonIfEmpty { get; set; }
    }

    internal class Field
    {
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public string PayloadMapping { get; set; }

        [JsonProperty(Required = Required.Always)]
        public FieldType Type { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool AbandonIfEmpty { get; set; }
        public bool IgnoreIfEmpty { get; set; }
        public object ValueIfMissing { get; set; }
    }

    internal enum FieldType
    {
        //prefix with @ since string and float are reserve words
        [Description("string")]
        @string,
        [Description("integer")]
        integer,
        [Description("float")]
        @float,
        [Description("Boolean")]
        Boolean
    }
}
