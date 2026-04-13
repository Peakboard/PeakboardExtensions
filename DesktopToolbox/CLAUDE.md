# DesktopToolbox - Peakboard Extension

## Project Overview
A Peakboard extension that exposes desktop/session information and utility functions. Built with the Peakboard ExtensionKit SDK (v4.0.1) targeting .NET 8.0 (x64).

## Build & Deploy

```bash
# Build
cd SourceCodeNew/DesktopToolbox
dotnet build -c Release

# Deploy — zip the output into Binary/DesktopToolbox.zip
cd bin/Release/net8.0
# Include: DesktopToolbox.dll, DesktopToolbox.pdb, Extension.xml
# Do NOT include .deps.json or .runtimeconfig.json
```

Always rebuild and redeploy the zip after any code change.

## Project Structure

- `SourceCodeNew/DesktopToolbox/` — C# source code
  - `DesktopToolboxExtension.cs` — Extension entry point (inherits `ExtensionBase`)
  - `DesktopInformationCustomList.cs` — Custom list providing desktop info columns and functions (inherits `CustomListBase`)
  - `Extension.xml` — Extension catalog manifest (ID, version, entry class)
  - `DesktopToolbox.png` — Icon (embedded resource)
- `Binary/DesktopToolbox.zip` — Deployable extension package

## Architecture

- **One extension class** (`DesktopToolboxExtension`) registers one or more custom lists.
- **Custom lists** define columns via `GetColumnsOverride`, return data via `GetItemsOverride`, and handle callable functions via `ExecuteFunctionOverride`.
- Each column added to `GetColumnsOverride` must also be populated in `GetItemsOverride`.
- Functions are defined in `GetDefinitionOverride` and dispatched by name in `ExecuteFunctionOverride`.

## Important Rules

- **Every successful build must be followed by a deploy to `Binary/DesktopToolbox.zip`** — never build without deploying. This is implicit and does not need to be requested by the user.
- **When a function or custom list changes** (new columns, removed columns, new functions, changed function signatures, new custom lists):
  - Update `README.md` to reflect the change.
  - Increment the version number by 0.1 in both `Extension.xml` and `DesktopToolboxExtension.cs`.

## Conventions

- Namespace: `DesktopToolbox`
- Version is tracked in both `Extension.xml` and `DesktopToolboxExtension.cs` — keep them in sync.
- `PropertyInputPossible = false` — this list takes no user-configured properties.
- Platform: x64 only.
