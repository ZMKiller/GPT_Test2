# Homeless To Millionaire Scripts

This folder contains C# scripts for the **Homeless To Millionaire** game prototype. The scripts were written for Unity and can also be compiled as a .NET project.

## Building

The provided `HomelessToMillionaire.csproj` file allows building all scripts with the .NET SDK. You will need the .NET 6.0 SDK installed:

```bash
dotnet build Bomj/HomelessToMillionaire.csproj
```

Newtonsoft.Json is referenced via NuGet. When building inside Unity, make sure that the `Newtonsoft.Json` package is available in the project.

## Status

These scripts are an early prototype and some systems are incomplete. They should compile, but many gameplay features are stubbed or lack integration.
