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

## Prerequisites

`Azure IoT Edge`
`.NET Core 2.1`
`Docker`
`VS Code`
`VS Code Azure IoT Tooling Extension`

## Setup
## Running the sample
## Key concepts


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