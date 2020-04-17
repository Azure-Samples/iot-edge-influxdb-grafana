# Introduction to InfluxDB

[InfluxDB](https://docs.influxdata.com/influxdb/v1.7/) is an open source schemaless time series database.  

It is important to understand [the key concepts of InfluxDB](https://docs.influxdata.com/influxdb/v1.7/concepts/key_concepts/) and 
[the difference between InfluxDB and SQL](https://docs.influxdata.com/influxdb/v1.7/concepts/crosswalk/).
In this solution we use InfluxDB 1.7 which is the current version recommended for production systems with InfluxDB 2.0 in beta.

## Terminology

From the above documentation:

- **Point** : A single data record which has four parts: a measurement, tag set, field set and a timestamp.
- **Fields**: Non-indexed data which is recorded, ex. temperature, pressure, weight.  Required.  Must be either string, bool, float or int.
- **Tag** : A tag is made up of an indexed key-vaue pair which can be queried to group points.  Often multiple tags, referred to as a tag set, are used ex. location and machine_type and these are called tag sets.  Optional but recommended.  Always stored as strings.
- **Series** : A collection of points that share a measurement, tag set, and field key.
- **Measurement** : A container for tags, fields, and time.  Conceptually similar to a table.

Queries on data should be made against the time and the tag since fields are not indexed.

# IoT Edge InfluxDB Solution

## Mapping File

To use InfluxDB 1.7 on Azure IoT Edge in this solution, you need to understand and know how to change the mapping file (`TelemetrySelection.json` in this solution) and possibly a few options for configuring InfluxDB.  The mapping file defines the JSON schema of incoming data and adds two options for data handling - `IgnoreIfMissing` and `AbandonIfMissing`.

The mapping file structure is:

```
{
  "Databases": [
    {
      "Name": "<InfluxDBDatabaseName>",
      "Measurements": [
        {
          "Name": "<MeasurementName>",
          "Tags": [
            {
              "Name": "<TagName>",
              "PayloadMapping": "<PathToTagInJsonSchema>",
              "IgnoreIfMissing": "<true | false:optional>",
              "ValueIfMissing": <If IgnoreIfMissing is true, value to insert into DB>,
              "AbandonIfEmpty": <true | false:required>
            }
          ],
          "Fields": [
            {
              "Name": "<FieldName>",
              "PayloadMapping": "<PathToFieldInJsonSchema>",
              "Type": "<InfluxDB Type string | float | integer | Boolean>",
              "IgnoreIfMissing": "<true | false:optional>",
              "ValueIfMissing": <If IgnoreIfMissing is true, value to insert into DB>,
              "AbandonIfEmpty": <true | false required>,
            }
          ]
        }
      ],
      "TimestampPayloadMapping": "<PathToTimestampInJsonSchema> - Note that this currently must be milliseconds"
    }
  ]
}
```

The mapping file allows for an array of databases, an array of tags, and an array of fields to allow for multiple definitions depending on the incoming JSON.  

* **Name** - This is the name of the key that will be written to InfluxDB for this value.
* **PayloadMapping** - This is a dot notation path to the data value in the JSON Schema.  
* **IgnoreIfMissing** - If this is `true`, then when the data is written to InfluxDB this value will not be included (unlike SQL, InfluxDB does not require that all data be included and allows for more tags and fields to be added over time).  If it is false, then it will use the `ValueIfMissing` field to write data.
* **ValueIfMissing** - If data is missing and you still wish to write something to InfluxDB, enter it into this field.  For example, instead of having no value when a sensor is not working, you wish to write 0 or -1 to later indicate a failure on a grafana dashboard it would go here.
* **AbandonIfMissing** - If this is `true`, none of the data will be written to InfluxDB.  One use case would be if there is some critical data that must be included for any of it to make sense in InfluxDB.  Ex, if there are no tags and querying by time alone would not make sense.

### Mapping File Example

If you have a sample JSON like what is in `TelemetryMessageSample.json`:

```
{
  "Timestamp": "1583451789832",
  "System": {
    "Core": {
      "MachineSampleWeight": {
        "TestWeight": 40.0
      }
    }
  }
}
```

then the mapping file could look like:

```
{
    "Databases": [
      {
        "Name": "telemetry",
        "Measurements": [
          {
            "Name": "influx_measurements",
            "Tags": [
              {
                "Name": "MachineId",
                "PayloadMapping": "MachineId",
                "AbandonIfEmpty": false
              }
            ],
            "Fields": [
              {
                "Name": "TestWeight",
                "PayloadMapping": "System.Core.MachineSampleWeight.TestWeight",
                "Type": "float",
                "AbandonIfEmpty": false,
                "IgnoreIfEmpty": true
              }
            ]
          }
        ],
        "TimestampPayloadMapping": "Timestamp"
      }
    ]
}
```

`PayloadMapping` above demonstrates the dot notation of how to map between the JSON schema and an InfluxDB key.

You can see that `MachineId` has `AbandonIfEmpty` set to `false`.  This is because currently we do not include `MachineId` in the incoming JSON but we know that we will likely include it later.  Further, as an example you can see that if the weight value is empty we `Ignore` that value.  Since this is the only data value we write, we could also have used `AbandonIfEmpty` set to true.

### InfluxDB Options

In the `InfluxDBRecorder.cs` file, we currently configure a retention policy for data of 30 days for all databases.  You may want to configure this in the `InitializeAsync` method depending on your use case.  See [InfluxDB Retention Policy](https://docs.influxdata.com/influxdb/v1.7/query_language/database_management/#create-retention-policies-with-create-retention-policy) for more information on available options.

Also note that this currently only expects the timestamp in milliseconds while [InfluxDB supports precision](https://www.docs.influxdata.com/influxdb/v1.7/tools/shell/) from nanoseconds up to hour and above.  

### Useful links for understanding InfluxDB

[InfluxDB Key Concepts](https://docs.influxdata.com/influxdb/v1.7/concepts/key_concepts/)
[InfluxDB CLI](https://docs.influxdata.com/influxdb/v1.7/tools/shell/)  -- You can run the command `docker exec -it <containerId> influx` to use the CLI and see/verify what is being written to InfluxDB from the Orchestrator Module and check settings (ex. is retention policy what I expect?)
[InfluxDB Query Language](https://docs.influxdata.com/influxdb/v1.7/query_language/)
[InfluxDB Compared to SQL](https://docs.influxdata.com/influxdb/v1.7/concepts/crosswalk/)
[AdysTech.InfluxDB.Client.Net](https://github.com/AdysTech/InfluxDB.Client.Net) - This is covered more below but the README explains how this dotnet package works with InfluxDB.

## Orchestrator Module and How It Works with the Mapping File and InfluxDB

In the Orchestrator module we've added `InfluxDBRecorder.cs` and `ORM.cs`.  `InfluxDBRecorder` is an abstraction of `ITimeSeriesRecorder` and is called within the `OrchestratorModule`.  It acts as the interface to InfluxDB using the dotnet package `AdysTech.InfluxDB.Client.Net`, the InfluxDB structure in `ORM`, and the mapping file.  

`InfluxDBRecorder` translates between the mapping file and InfluxDB using Newtonsoft to parse the incoming JSON and converts it into the ORM class and [AdysTech.InfluxDB.Client.Net](https://github.com/AdysTech/InfluxDB.Client.Net) to write to InfluxDB.

[AdysTech.InfluxDB.Client.Net](https://github.com/AdysTech/InfluxDB.Client.Net) is an open source Dotnet client to connect to InfluxDB 1.7 since there is no official dotnet client.  This is referenced as (one of the dotnet packages to use on the InfluxDB website)[https://docs.influxdata.com/influxdb/v1.7/tools/api_client_libraries/].  It has very good documentation that shows how it interfaces with InfluxDB.  In this solution, we use it to create databases with data retention polices and to write data in `InfluxDBRecorder`.

`ORM.cs` takes the InfluxDB database and data structure and translates that into a class for use with `InfluxDBRecorder` and the mapping file.  Within the class, you will find definitions that align with InfluxDB's data structure including database, measurement, timestamp, tags, fields and field types (the four datatypes which InfluxDB allows for fields).  Some items within that file, `PayloadMapping`, `AbandonIfMissing`, `ValueIfMissing` will be explained in the section on the mapping file below.

## Dockerfile

The InfluxDB docker image referenced in the folder `modules\influxdb`. This folder is not strictly necessary, you could add the image to the `deployment.template.json` file.  However, by adding a `module.json` file and Dockerfiles for this public container, you can upload the container to an Azure Container Registry, as is done in this repo.  While this is currently redundant since 1.7 is the latest image on Dockerhub and in each build we are using the same environment variables, it highlights how to put different builds of a public docker image in a private container.  Additionally, by storing an image in a separate private repository, you can guarantee reusing the same image in each build regardless of changes on Dockerhub.  If you later decide to use a specific build, you can modify the `deployment.json` file or modify the Dockerfiles and `module.json`  in the `influxdb` folder.  The way to undo adding new images to the container registry would be to change the `${BUILD_BUILDID}` field in `module.json`.

## INFLUX_BIND Environment Variable

This module requires an INFLUX_BIND environment variable to map a folder inside of the container to the device.  Without this environment variable set, data is **not** persistent.  Therefore, this environment variable is included for when the module restarts or if for some reason you need to export the data from the device filesystem directly.