# Peakboard Extension Samples

This folder contains sample extensions for learning the [Peakboard Extension Kit](https://www.nuget.org/packages/Peakboard.ExtensionKit). The samples are ordered from easy to advanced.

For a step-by-step tutorial, see: [Plug-in, Baby -- The Ultimate Guide to Building Your Own Peakboard Extensions](https://how-to-dismantle-a-peakboard-box.com/Plug-in-Baby-The-ultimate-guide-to-build-your-own-Peakboard-extensions-The-Basics.html)

## How to Run

1. Open the `.sln` file in Visual Studio.
2. Restore NuGet packages (update `Peakboard.ExtensionKit` if needed).
3. Build the project.
4. Package the output DLL, `extension.xml`, and all referenced assemblies into a single ZIP file.
5. In Peakboard Designer, go to **Manage Extensions** and add the ZIP file.
6. Restart Peakboard Designer.

## Samples

### CatFacts (Beginner)
A simple, fixed list with user-adjustable properties. Ideal for exploring the basic concepts of the Extension Kit.

### CalcDemo (Beginner)
A basic calculation demo that shows how to work with list properties and the `CustomListBase` class.

### CustomFunction (Intermediate)
Demonstrates how to create custom Lua-callable functions within an extension. The example provides an MD5 hash function.

### Airport Weather Conditions (Advanced)
Shows how to build a custom dialog and UI for an extension. The user can download weather information from various airports in Germany.

### SQL Server (Reference)
A fully functional database extension example. Intended as a reference for developers building database connectors -- not for production use.
