# Photo Sorter Packaging Design Specification

## Overview
This document outlines the standard procedure for packaging the `Photo Sorter` (.NET 8 WinForms) project into a standalone executable (`.exe`). The goal is to produce a single file that can be distributed to end-users without requiring them to pre-install the .NET 8 runtime.

## Approach
- **Packaging Format**: Portable Self-Contained Single File `.exe`.
- **Methodology**: On-demand CLI execution via `dotnet publish`. The user prefers not to maintain a separate `.bat` script, and will instead request the AI assistant to run the publish command when needed.

## Execution Command
The following command is the standard way to publish the project:
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

### Parameters Breakdown
- `-c Release`: Compiles the application with optimizations enabled.
- `-r win-x64`: Targets the 64-bit Windows environment.
- `--self-contained true`: Embeds the .NET 8 runtime within the application, ensuring it runs on machines without .NET installed.
- `-p:PublishSingleFile=true`: Bundles the application, its dependencies, and the runtime into a single `.exe` file.
- `-p:IncludeNativeLibrariesForSelfExtract=true`: Ensures that native C/C++ libraries are also embedded inside the single file and extracted at runtime.

## Output Location
Once the command is executed within the `photo\photo` directory, the final executable will be located at:
`photo\photo\bin\Release\net8.0-windows\win-x64\publish\photo.exe`
