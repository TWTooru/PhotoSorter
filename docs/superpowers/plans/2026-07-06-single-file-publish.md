# Single-File Publish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Package the Photo Sorter application into a single `.exe` file.

**Architecture:** We are using `dotnet publish` to build a self-contained single-file executable for Windows x64.

**Tech Stack:** .NET 8, Windows Forms, CLI

## Global Constraints

- Run the publish command inside the `photo/photo` directory where the `.csproj` is located.
- The output must be a single file including native libraries.

---

### Task 1: Execute Publish Command

**Files:**
- Modify: none (CLI action only)

**Interfaces:**
- Consumes: `photo/photo/photo.csproj`
- Produces: `photo/photo/bin/Release/net8.0-windows/win-x64/publish/photo.exe`

- [x] **Step 1: Execute the `dotnet publish` command**

Run:
```bash
cd photo/photo
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Expected: Command completes successfully.

- [x] **Step 2: Verify the executable exists**

Run:
```bash
Test-Path photo/photo/bin/Release/net8.0-windows/win-x64/publish/photo.exe
```

Expected: `True`
