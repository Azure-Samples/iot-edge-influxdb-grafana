# Introduction to Grafana

_"[Grafana](https://grafana.com/grafana/) allows you to query, visualize, alert on and understand your metrics no matter where they are stored. Create, explore, and share dashboards with your team and foster a data driven culture."_ - Grafana landing page

Grafana comes with a built-in plugin to InfluxDB which allows for easy configuration and querying.  Further, the included panels (Grafana's term for a chart / graph / other visualization) work very well to have immediate results in displaying data and iterating on different possibilities.  There are many additional panels and data sources which can be added to extend out-of-the box functionality.

## Grafana on Azure IoT Edge

A requirement for using Grafana on Azure IoT Edge is to be able to load Grafana with a saved connection to the on-device InfluxDB and pre-configured dashboards.  Further, if IoT Edge restarts, Grafana should reload without any changes.

# Dockerfiles

For Grafana to load configurations when it starts, the Grafana image available at DockerHub https://hub.docker.com/u/grafana/ is not sufficient (unlike the InfluxDB image).  We have extended the Dockerfile for the module to work on IoT Edge which make the necessary updates.  Lines 21-23 in `Dockefile.amd64` show where the provisioning files, dashboards and datasources are copied into the image in case you want to change or update that at a later time.  `ENV GF_PANELS_DISABLE_SANITIZE_HTML` is set to `false` by default in the Dockerfiles.  If you need to use Javascript in your dashboards, set this to `true`.  The folder locations and that environment variable should be all you would need to configure although you have the option of looking into adding user groups as well.

[Grafana Provisioning](https://grafana.com/docs/grafana/latest/administration/provisioning/)

In this solution the provisioning dashboard configuration is in `provisioning_dashboards/docker-dashboard.yml` and the datasources are in `datasources/`.  In particular with the datasources, note the `name` since it is used in the preconfigured dashboards, the type (influxdb in this solution) and the database name which is where we have written the data previously.  The datasource enables the preconfigured dashboards to automatically connect and query the InfluxDB databases.

A preconfigured dashboard with panels is in `dashboards\`.  This is a sample to demonstrate how to use the `uid` field to specify a repeatable entry point to Grafana, automatically connecting to a datasource with a query and displaying that result with a preset refresh rate.

# Adding dashboards and panels

The [Grafana getting started guide](https://grafana.com/docs/grafana/latest/guides/getting_started/) provides guidance on panels, dashboards and datasources which are likely the first things you will want to do when working with this solution.

Two primary differences to note between the documentation and using Grafana on Edge with this solution:

1) Any new panels or dashboards that you add in Grafana **must** be exported and added in code.  If you add a dashboard or panel locally it will persist as long as the module doesn't restart.
NB. I've tried adding a bind mount and this does not seem to work.

2) You will notice a difference in the JSON for exporting dashboards if you select "Export for sharing externally".  Thus far, it seems better _not_ to select this.  If you do select it you will see two additional sections `inputs` and `requires`.  Importantly, the `inputs` section will add `DS_` in front of your data sources (even though you have already preprended DS_).  You can remove the `inputs` section **and** you must change all of the inputs through the JSON from, for example, `${DS_DS_INFLUX}` to your already configured `DS_INFLUX`.

3) The first time you open grafana on a device if you do not go to the uid directly, you will likely have to navigate to find the dashboard as it will not show on the homepage. 

__UID__: At the very bottom of a dashboard file there is a `uid` field.  You can use this to set the URL to go directly to your dashboard: http://localhost:3000/d/<myuid>

