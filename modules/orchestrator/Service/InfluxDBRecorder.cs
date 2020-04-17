namespace Orchestrator.Service
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Serilog;
    using AdysTech.InfluxDB.Client.Net;
    using Microsoft.Extensions.Configuration;
    using ModuleWrapper;
    using System.Reflection;
    using Orchestrator.Abstraction;

    public class InfluxDBRecorder : ITimeSeriesRecorder
    {
        private enum ValidationStatus
        {
            success,
            fail,
            useDefaultValue
        }

        private enum InsertStatus
        {
            ok,
            abandon,
            noneAdded
        }

        public IConfiguration Configuration { get; }
        public IModuleClient ModuleClient { get; }
        public CancellationTokenSource CancellationTokenSource { get; }
        private IInfluxDBClient InfluxDBClient { get; }
        
        private List<Database> databases = null;

        public InfluxDBRecorder(IConfiguration configuration, 
            IModuleClient moduleClient,
            CancellationTokenSource cancellationTokenSource,
            IInfluxDBClient influxDBClient)
        {
            Configuration = configuration;
            ModuleClient = moduleClient;
            CancellationTokenSource = cancellationTokenSource;
            InfluxDBClient = influxDBClient;            
        }

        public async Task InitializeAsync()
        {
            ORM orm = null;

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Orchestrator.Service.TelemetrySelection.json"))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                orm = JsonConvert.DeserializeObject<ORM>(result, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Error
                });
            }
            while (true)
            {
                try
                {
                    await InfluxDBClient.GetInfluxDBNamesAsync();
                    Log.Information("Connected to Influxdb");
                    break;
                }
                catch (Exception ex)
                {
                    Log.Information("Could not connect to Influx: " + ex.Message);
                    Log.Information("Retying in 5 seconds...");
                }
                await Task.Delay(5000);
            }

            foreach (var db in orm.Databases)
            {
                if (string.IsNullOrWhiteSpace(db.Name))
                {
                    throw new Exception("Error: Empty or null database name in TelemetrySelection: " + orm);
                }
                //can check if DB exists first so success true/false has meaning
                //can add check for success = false if DB doesn't already exist
                bool success = await InfluxDBClient.CreateDatabaseAsync(db.Name);
                var retentionPolicy = new InfluxRetentionPolicy()
                {
                    Name = "ConfiguredRetentionPolicyFromEnv",
                    DBName = db.Name,
                    Duration = TimeSpan.FromDays(Configuration.GetValue("INFLUX_RETENTION_IN_DAYS", 30)),
                    IsDefault = false
                };
                if (!await InfluxDBClient.CreateRetentionPolicyAsync(retentionPolicy))
                {
                    throw new Exception("Error: Could not create retention policy for influxdb: " + db.Name);
                }
            }

            databases = orm.Databases;
        }

        public async Task RecordMessageAsync(string telemetryJson)
        {
            JToken events = JToken.Parse(telemetryJson);

            JArray eventsArray = events as JArray;
            if (eventsArray == null)
            {
                if (events is JObject)
                {
                    eventsArray = new JArray();
                    eventsArray.Add(events);
                }
                else
                {
                    throw new Exception("Error: entries is not JArray or JObject");
                }
            }
            await InsertJsonIntoInfluxAsync(eventsArray);
        }

        private async Task InsertJsonIntoInfluxAsync(JArray entries)
        {
            foreach (JObject entry in entries)
            {
                foreach (var db in databases)
                {
                    DateTime utcTimestamp;
                    if (!TryGetTimestampFromJson(out utcTimestamp, entry, db))
                    {
                        continue;
                    }

                    foreach (var measurement in db.Measurements)
                    {
                        var dataPoint = new InfluxDatapoint<InfluxValueField>();
                        if (string.IsNullOrWhiteSpace(measurement.Name))
                        {
                            Log.Warning("Error: Null or empty measurement name");
                            continue;
                        }
                        dataPoint.MeasurementName = measurement.Name;
                        dataPoint.UtcTimestamp = utcTimestamp;
                        dataPoint.Precision = TimePrecision.Milliseconds;

                        //validate
                        var tagValidation = TryValidateTags(measurement.Tags, entry);
                        var fieldValidation = TryValidateFields(measurement.Fields, entry);

                        if (tagValidation.insertStatus == InsertStatus.abandon
                           || fieldValidation.insertStatus == InsertStatus.abandon)
                        {
                            Log.Warning("Could not validate tags/fields or insert was abandoned for measurement: {measurementName}", measurement.Name);
                            continue;
                        }

                        //make sure at least 1 field was added successfully
                        if (fieldValidation.insertStatus == InsertStatus.noneAdded)
                        {
                            Log.Warning("Error: No fields successfully added for {measurementName}", measurement.Name);
                            continue;
                        }

                        //insert valid data
                        var tagPoints = GetTagInserts(measurement.Tags, entry, tagValidation.validationStatuses);
                        foreach (var point in tagPoints)
                        {
                            dataPoint.Tags.Add(point.name, point.data);
                        }

                        var fieldPoints = GetFieldInserts(measurement.Fields, entry, fieldValidation.validationStatuses);
                        foreach (var point in fieldPoints)
                        {
                            var fieldValue = new InfluxValueField(point.data);
                            dataPoint.Fields.Add(point.name, fieldValue);
                        }

                        var success = await InfluxDBClient.PostPointAsync(db.Name, dataPoint);
                        if (!success)
                        {
                            Log.Warning("Could not add data point {measurementName} to influx", measurement.Name);
                            continue;
                        }
                        Log.Information("Adding {measurementName} was successful", measurement.Name);
                    }
                }
            }
        }

        private static bool TryGetTimestampFromJson(out DateTime utcTimestamp, JObject entry, Database db)
        {
            try
            {
                long unixTimeStamp = (long)entry.SelectToken(db.TimestampPayloadMapping);
                utcTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeStamp).UtcDateTime;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Error: Could not get/convert event timestamp in Json: {entry} \n {exceptionMessage}", entry, ex.Message);
                utcTimestamp = new DateTime();
                return false;
            }
        }

        private static (InsertStatus insertStatus, List<ValidationStatus> validationStatuses) TryValidateTags(List<Tag> tags, JObject entry)
        {
            var statuses = new List<ValidationStatus>();
            foreach (var tag in tags)
            {
                var data = (string)entry.SelectToken(tag.PayloadMapping);

                if (string.IsNullOrWhiteSpace(data) || string.IsNullOrWhiteSpace(tag.Name))
                {
                    if (tag.AbandonIfEmpty)
                    {
                        Log.Information("Abandoning Insert: Tag data for {tagName} is missing and AbandonIfEmpty is true", tag.Name);
                        return (InsertStatus.abandon, null);
                    }
                    else
                    {
                        Log.Warning("Error: Null or empty tag data/name, skipping...");
                        statuses.Add(ValidationStatus.fail);
                        continue;
                    }
                }

                statuses.Add(ValidationStatus.success);
            }
            return (InsertStatus.ok, statuses);
        }

        private static (InsertStatus insertStatus, List<ValidationStatus> validationStatuses) TryValidateFields(List<Field> fields, JObject entry)
        {
            var fieldsValidated = 0;
            var statuses = new List<ValidationStatus>();
            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field.Name))
                {
                    Log.Warning("Error: Null or empty field name, skipping...");
                    statuses.Add(ValidationStatus.fail);
                    continue;
                }
                
                var fieldAndFieldType = GetFieldAsType(field, entry);

                //If field data is null, use default values.
                bool stringIsEmpty = (fieldAndFieldType.FieldType == typeof(string)) &&
                                     (string.IsNullOrWhiteSpace(fieldAndFieldType.fieldAsType));
                if (fieldAndFieldType.fieldAsType is null || stringIsEmpty)
                {
                    if (field.AbandonIfEmpty)
                    {
                        Log.Information("Abandoning Insert: Field data for {fieldName} is missing and AbandonIfEmpty is true", field.Name);
                        return (InsertStatus.abandon, null);
                    }

                    if (field.IgnoreIfEmpty)
                    {
                        //Ignore this field, process the next
                        statuses.Add(ValidationStatus.fail);
                        continue;
                    }

                    statuses.Add(ValidationStatus.useDefaultValue);
                    fieldsValidated++;
                    continue;
                }
                statuses.Add(ValidationStatus.success);
                fieldsValidated++;
            }
            if (fieldsValidated < 1)
            {
                return (InsertStatus.noneAdded, null);
            }

            return (InsertStatus.ok, statuses);
        }

        private static (dynamic fieldAsType, Type FieldType) GetFieldAsType(Field field, JObject entry)
        {
            //Determine the field type based on what was suplied in the json
            Type ResFieldType = null;
            switch (field.Type)
            {
                case FieldType.@float:
                    ResFieldType = typeof(double);
                    break;
                case FieldType.@string:
                    ResFieldType = typeof(string);
                    break;
                case FieldType.integer:
                    ResFieldType = typeof(int);
                    break;
                case FieldType.Boolean:
                    ResFieldType = typeof(bool);
                    break;
            }
            var data = entry.SelectToken(field.PayloadMapping);
            var dataString = data?.ToString();
            
            if(string.IsNullOrEmpty(dataString))
                return(null, ResFieldType);

            try 
            {
                return (Convert.ChangeType(data, ResFieldType), ResFieldType);
            } 
            catch (Exception ex)
            {
                Log.Error("Error: Could not convert data to type, returning null", entry, ex.Message);
            }
            
            return(null, ResFieldType);
        }

        private static List<(string name, string data)> GetTagInserts(List<Tag> tags, JObject entry, List<ValidationStatus> validationStatuses)
        {
            var points = new List<(string name, string data)>();
            for (var i = 0; i < tags.Count; i++)
            {
                var tag = tags[i];
                var data = (string)entry.SelectToken(tag.PayloadMapping);

                if (validationStatuses[i] == ValidationStatus.success)
                {
                    points.Add((tag.Name, data));
                }
            }
            return points;
        }

        private static List<(string name, dynamic data)> GetFieldInserts(List<Field> fields, JObject entry, List<ValidationStatus> validationStatuses)
        {
            var points = new List<(string name, dynamic data)>();

            for (var i = 0; i < fields.Count; i++)
            {
                if(validationStatuses[i] != ValidationStatus.fail)
                {
                    var field = fields[i];
                    var data = (string)entry.SelectToken(field.PayloadMapping);
                    dynamic fieldAsType = null;

                    if (validationStatuses[i] == ValidationStatus.success)
                    {
                        var fType = GetFieldAsType(field, entry);
                        fieldAsType = fType.fieldAsType;
                    }
                    else if (validationStatuses[i] == ValidationStatus.useDefaultValue)
                    {
                        fieldAsType = field.ValueIfMissing;
                    }
                    points.Add((field.Name, fieldAsType));
                }
            }
            return points;
        }

        public async Task DisconnectAsync()
        {
            Log.Information("Disconnecting Edge Hub Client Connection");

            await ModuleClient.CloseAsync();
        }

      
    }
}
