# Peakboard Extensions

This repository contains community and official extensions for [Peakboard](https://peakboard.com), built on top of the Peakboard Extension Kit.

## What Are Peakboard Extensions?

Peakboard Extensions are custom plugins that extend Peakboard Designer with additional data sources, functions, and integrations. They are built as .NET DLLs using the [Peakboard Extension Kit](https://www.nuget.org/packages/Peakboard.ExtensionKit) NuGet package. Extensions allow you to connect Peakboard to systems, databases, APIs, and hardware devices that are not covered by the built-in data sources.

Each extension is packaged as a ZIP file containing the compiled DLL, its dependencies, and an `extension.xml` metadata file. Extensions can be installed directly in Peakboard Designer without modifying the Peakboard Box itself -- they are automatically deployed with your dashboard.

For more details on how extensions work and how to build your own, see:
- [Extension Basics -- Peakboard Help](https://help.peakboard.com/data_sources/Extension/en-Extension.html)
- [Manage Extensions -- Peakboard Help](https://help.peakboard.com/data_sources/Extension/en-ManageExtension.html)
- [Plug-in, Baby -- The Ultimate Guide to Building Your Own Peakboard Extensions](https://how-to-dismantle-a-peakboard-box.com/Plug-in-Baby-The-ultimate-guide-to-build-your-own-Peakboard-extensions-The-Basics.html)

## Installation

1. Download the `.zip` file from the `Binary` or `Binaries` folder of the desired extension.
2. In Peakboard Designer, go to **Data > Add data source > Manage extensions**.
3. Click **Add custom extension** and select the downloaded ZIP file.
4. Restart Peakboard Designer.
5. The new data source is now available under **Extensions** when adding a data source.

## Extensions

### Database Connectors

| Extension | Description |
|-----------|-------------|
| [Actian Zen](Actian%20Zen/) | Connector for Actian Zen (formerly Pervasive SQL) databases |
| [Databricks](Databricks/) | Read and write data from Databricks cloud data platform |
| [Dataverse](Dataverse/) | Microsoft Dataverse connector for Power Platform (FetchXML support) |
| [DB2](DB2/) | IBM DB2 database connector (requires IBM i Access Client Solutions) |
| [Firebird](Firebird/) | Connector for Firebird open-source SQL databases |
| [InfluxDB](InfluxDB/) | InfluxDB time-series database connector (read and write) |
| [Ingres](Ingres/) | Connector for Ingres SQL databases |
| [MPDV](MPDV/) | MPDV Manufacturing Execution System (MES) connector |
| [MySQL](MySQL/) | MySQL database connector |
| [PostgreSQL](PostgreSQL/) | PostgreSQL database connector |

### Cloud and SaaS Integrations

| Extension | Description |
|-----------|-------------|
| [GraphExtension](GraphExtension/) | Microsoft Graph API connector for Office 365 and Azure AD |
| [HubSpot](HubSpot/) | HubSpot CRM integration |
| [Microsoft Dynamics 365](MicrosoftDynamics365/) | Dynamics 365 ERP/CRM data access |
| [Monday](Monday/) | Monday.com work management platform (GraphQL and board-based queries) |
| [Smartsheet](Smartsheet/) | Smartsheet project management platform integration |
| [Tableau Token Generator](TableauTokenGenerator/) | Generate embedding tokens for Tableau dashboards |
| [Trello](Trello/) | Trello boards and cards integration |
| [Yarooms](Yarooms/) | Yarooms meeting room management system |

### IoT, Hardware, and Industrial

| Extension | Description |
|-----------|-------------|
| [BacNet](BacNet/) | BACnet building automation protocol (device discovery, property reading) |
| [CAS](CAS/) | CAS weighing scale integration (BLE, serial, PDN ECR) |
| [FritzSmartHome](FritzSmartHome/) | AVM Fritz smart home device control (thermostats) |
| [GettHMI](GettHMI/) | GETT BlackLine Smart Panel PC button integration |
| [Hue](Hue/) | Philips Hue smart lighting control |
| [MifareReader](MifareReader/) | NFC/RFID Mifare Classic card reader (uTrust 3700F, ACR122U) |
| [MQTTServer](MQTTServer/) | Turn the Peakboard Box into an MQTT server |
| [POSPrinter](POSPrinter/) | POS printer support (ESC/POS and Zebra ZPL protocols) |
| [Proglove](Proglove/) | ProGlove wearable barcode scanner integration |
| [SerialBarcodeScanner](SerialBarcodeScanner/) | Serial COM port barcode scanner integration |
| [SickExtension](SickExtension/) | SICK sensor and scanner integration |
| [WERMASmartMonitor](WERMASmartMonitor/) | WERMA signal tower monitoring system |
| [WheelMe](WheelMe/) | wheel.me autonomous robot fleet management |
| [Woutex](Woutex/) | Woutex e-Ink display integration |
| [Xminnov](Xminnov/) | Xminnov RFID tag reader |

### Monitoring and Network

| Extension | Description |
|-----------|-------------|
| [CheckMk](CheckMk/) | Check_MK IT monitoring system integration |
| [LDAP](LDAP/) | LDAP directory service queries |
| [NetworkFiles](NetworkFiles/) | Network file system access |

### AI and Scripting

| Extension | Description |
|-----------|-------------|
| [GPT](GPT/) | OpenAI GPT API integration for AI-powered data |
| [Python](Python/) | Execute Python scripts that produce tabular data |

### Utilities

| Extension | Description |
|-----------|-------------|
| [AcmeData](AcmeData/) | Random sample data generator for demo and testing purposes |
| [DesktopToolbox](DesktopToolbox/) | Desktop session info and browser URL opener |
| [FixResolution](FixResolution/) | Automatic 4K display scaling for Peakboard Runtime |

### Samples

The [Samples](Samples/) folder contains beginner-friendly examples for learning the Extension Kit:

| Sample | Description |
|--------|-------------|
| [CatFacts](Samples/CatFacts/) | Simple fixed list with user-adjustable properties (beginner) |
| [CalcDemo](Samples/CalcDemo/) | Basic calculation demo showing list properties |
| [CustomFunction](Samples/CustomFunction/) | How to create Lua-callable custom functions (MD5 hash) |
| [AirportConditions](Samples/AirportConditions/) | Custom dialog and UI for airport weather data |
| [SQLServer](Samples/SQLServer/) | Full database extension example (reference only) |

## Building Extensions from Source

Each extension contains the source code in a `SourceCode` or `SourceCodeNew` folder.

1. Open the `.sln` file in Visual Studio.
2. Restore NuGet packages (ensure `Peakboard.ExtensionKit` is available).
3. Build the project. The output DLL, `extension.xml`, and dependencies are your extension.
4. Package all output files into a single ZIP file.
5. Install the ZIP in Peakboard Designer via **Manage Extensions**.

For a step-by-step guide, see the [Extension Kit tutorial series](https://how-to-dismantle-a-peakboard-box.com/Plug-in-Baby-The-ultimate-guide-to-build-your-own-Peakboard-extensions-The-Basics.html).

## Support

If you encounter any issues, please contact [support@peakboard.com](mailto:support@peakboard.com).
