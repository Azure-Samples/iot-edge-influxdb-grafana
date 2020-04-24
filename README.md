---
page_type: sample
languages:
- csharp
products:
- azure-iot-edge
- vs-code
---

# Introduction 
This project two development patterns:

1) A new pattern for IoT Edge module development using dependency injection.  This allows for unit tests and debugging modules without needing to run the simulator.

2) InfluxDB and Grafana working on IoT Edge (see `documentation` folder for more specifics on each)

## Contents

Outline the file contents of the repository. It helps users navigate the codebase, build configuration and any related assets.

| File/folder       | Description                                |
|-------------------|--------------------------------------------|
| `documentation` | Details of InfluxDB and Grafana config |
| `modules`         | IoT Edge module code                       |
| `ModuleWrapper`   | Dependency Injection and configuration     |
| `ModuleWrapperTest` | For ModuleWrapper testing                | 
| `deployment.template.json` | IoT Edge deployment configuration |
| `.gitignore`      | Define what to ignore at commit time.      |
| `CONTRIBUTING.md` | Guidelines for contributing to the sample. |
| `README.md`       | This README file.                          |
| `LICENSE`         | The license for the sample.                |

### Development machine prerequisites

The edge and cloud samples are designed to be built and run together on a development machine in Visual Studio Code using Docker CE, the Azure IoT EdgeHub Dev Tool and the Azure Functions Core Tools. Below are the prerequisites to build and run the sample on a local development machine: 

1. Language SDK
    [.NET Core SDK (2.1)](https://www.microsoft.com/net/download)

2. Docker

    [Docker Community Edition](https://docs.docker.com/install/) - required for Azure IoT Edge module development, deployment and debugging. Docker CE is free, but may require registration with Docker account to download.  Docker on Windows requires Hyper-V support.  Please make sure your Windows version supports Hyper-V.  For Windows 10, Hyper-V is available with the Pro or Enterprise versions.

3. Visual Studio Code and extensions
    
    > **Note**: Extensions can be installed either via links to the Visual Studio Code Marketplace below or by searching extensions by name in the Marketplace from the Extensions tab in Visual Studio Code.

    Install [Visual Studio Code](https://code.visualstudio.com/) first and then add the following extensions:

    - [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) (only required for C# version of sample) - provides C# syntax checking, build and debug support
    - [Azure IoT Tools](https://marketplace.visualstudio.com/items?itemName=vsciot-vscode.azure-iot-tools) - provides Azure IoT Edge development tooling

        > **Note**: Azure IoT Tools is an extension pack that installs 3 extensions that will show up in the Extensions pane in Visual Studio Code - *Azure IoT Hub Toolkit*, *Azure IoT Edge* and *Azure IoT Workbench*.

### Azure Prerequisites 

1. Azure IoT Hub Service

    To run the samples, you will need an Azure subscription and a provisioned Azure IoT Hub service. Every Azure subscription allows one free F1 tier Azure IoT Hub.  The F1 tier Azure IoT is sufficient for this sample. 

    [Create an IoT hub using the Azure portal](https://docs.microsoft.com/en-us/azure/iot-hub/quickstart-send-telemetry-dotnet#create-an-iot-hub)

2. Azure Container Registry (optional)

    This sample can be built and run in the local Azure IoT Edge Simulator without pushing Azure IoT Edge modules to a container registry.  A container registry is only needed when deploying to an actual Azure IoT Edge device.  Any Docker container registry can be used, including DockerHub and Azure Container Registry.

    [Create an Azure Container Registry](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-portal)

## Setup

### Configure Azure IoT Edge development environment

1. Connect your Azure account to Visual Studio Code

   The *Azure IoT Tools* Visual Studio Code extension pack installs a prerequisite *Azure Account* extension if its not already present.  This extension allows Visual Studio Code to connect to your Azure subscription.  For this sample, Visual Studio Code needs to connect to you Azure IoT Hub service.

   Open the command palette and search for *Azure: Sign In*

   Select this command and you will be prompted to sign into your Azure account in a separate browser window.  After sign-in, you should see *Azure:* followed by your login account in the status bar at the bottom of Visual Studio Code.

2. Connect to your Azure IoT Hub

   There are 2 ways to connect to your Azure IoT Hub from within Visual Studio Code:

   Open the command palette and search for *Azure IoT Hub: Select IoT Hub* 

   ​	**or**

   With the Explorer icon in the Visual Studio Code Activity Bar selected, go to the *AZURE IOT HUB* section in the Explorer pane of Visual Studio Code.  Select the "..." to open the Azure IoT Hub context menu.  From the Context Menu, choose *Select IoT Hub*.  

   Both options will open a selection list of available subscriptions at the top of the Visual Studio window.  After selecting your subscription, all available Azure IoT Hubs in your subscription will be presented in another selection list.  After selecting your Azure IoT Hub, the *AZURE IOT HUB** section in the Explorer pane of Visual Studio Code will be populated with configured Devices and Endpoints.  The Devices list will initially be empty for a new Azure IoT Hub.

3. Create an Azure IoT Edge device 

   This sample is designed to run in the Azure IoT Edge Simulator on a local development machine.  However, the Simulator still connects to your Azure IoT Hub service, and therefore needs an Azure IoT Edge device definition in Azure IoT Hub.  You can create an Azure IoT Edge device in the Azure portal, but its easier from Visual Studio Code with the Azure IoT Edge extension installed.

   There are 2 ways to create an Azure IoT Edge device from Visual Studio Code:

   Open the command palette and search for *Azure IoT Hub: Create IoT Edge Device*. 

   ​	**or**

   With the Explorer icon in the Visual Studio Code Activity Bar selected, go to the *AZURE IOT HUB* section in the Explorer pane of Visual Studio Code.  Select the "..." to open the Azure IoT Hub context menu.  From the Context Menu, choose *Create IoT Edge Device*. 

   Both options will open a prompt for you to enter the name of the device.

   > **Note:** There is also a *Azure IoT Hub: Create Device* command.  This creates a basic IoT device definition, which does not support the Azure IoT Edge Runtime and does not work with the Azure IoT Edge Simulator.

4. Configure Azure IoT Edge Simulator to use your Edge Device identity

   Again, there are 2 ways to create setup the Azure IoT Edge Simulator from within Visual Studio Code 

   Open the palette and search for *Azure IoT Edge: Setup IoT Edge Simulator*.  After selecting the command, a list of devices is displayed.  Select the device you created in the previous step. 

    **or**

   With the Explorer icon in the Visual Studio Code Activity Bar selected, go to the *AZURE IOT HUB* section in the Explorer pane of Visual Studio Code.  Expand the Devices list, and right click on the device you created in the previous step to open the Context Menu.  Select *Setup IoT Edge Simulator* from the Context Menu. 
   
   This command will pass your Edge device credentials to the Azure IoT Edge Simulator via a command in the Terminal Window.
   
   > **Note:** If you try to use the *Setup IoT Edge Simulator* command without first connecting to your Azure IoT Hub, you will instead be prompted to enter the connection string for an Azure IoT Hub device.
   
5. Set environment variables

   The Azure IoT Edge solution deployment manifests (*deployment.template.json* and *deployment.debug.template.json*) and module metadata files (*module.json*) support environment variable substitution.  There are 3 environment variable placeholders used in this sample - *$CONTAINER_REGISTRY_USERNAME*, *$CONTAINER_REGISTRY_PASSWORD* and *$CONTAINER_REGISTRY_ADDRESS*.  These are used to specify your container registry address and login credentials.  To run the code in the Azure IoT Edge Simulator, the *$CONTAINER_REGISTRY_ADDRESS* can be set to the Docker local registry container value of *localhost:5000*.  When using the local registry container value, the $CONTAINER_REGISTRY_USERNAME and $CONTAINER_REGISTRY_PASSWORD are not used.  However, since they are defined in the deployment manifests, they must be defined in order to avoid the "Please set registry credential to .env file." warning message on initial load.  
   To protect secrets, *.env* files should not be included in source control. Therefore, this sample includes a *.env.temp* template file that can be renamed to *.env*  or the values can be copied to your .env file.  To build and run the sample in the Azure IoT Edge Simulator, the following values can be used:

   ```
   CONTAINER_REGISTRY_ADDRESS=localhost:5000
   CONTAINER_REGISTRY_USERNAME=<registry username>
   CONTAINER_REGISTRY_PASSWORD=<registry password>
   ```
   > **Note:** *CONTAINER_REGISTRY_USERNAME* and *CONTAINER_REGISTRY_PASSWORD* are not used with the local registry container (*localhost:5000*), but these variables must be defined with any non-empty value. 

   If you wish to deploy the solution to a real Edge device, make sure to set the values to your container registry.    

6. Verify Docker runtime mode (**Windows only**)

   This sample is built to run in an Ubuntu container and requires a Linux Container runtime. If running on Windows, make sure that that Docker CE is running in the default Linux container mode, not Windows container mode. 

   You can do this by right clicking the Docker icon in the system tray.  If the context menu shows "Switch to Windows containers...", Docker is running in Linux container mode. 

## Running the sample

### Environment Variables

Change the name of the `.env.temp` file to `.env`.  For the CONTAINER_* environment names you can use the below for local development.

```
CONTAINER_REGISTRY_ADDRESS=localhost:5000
CONTAINER_REGISTRY_USERNAME=
CONTAINER_REGISTRY_PASSWORD=
```

### Debug mode

To test individual modules that you are developing (eg. not the influx and grafana modules since they are from Docker hub), set the **Environment** variable to **Debug**.  In this sample this allows you to run the `simulator` and `orchestrator` modules.  In VSCode, in the `launch.json` file, this is set for you for each module individually in `Local Debug` mode.

### InfluxDB and Grafana

For instructions on setup for these two modules and how they work, see the `documentation` folder.

### IoT Edge simulator

To test the full solution, right click on the `deployment.template.json` or `deployment.debug.template.json` and select "Build and Run IoT Edge Solution in Simulator."

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.