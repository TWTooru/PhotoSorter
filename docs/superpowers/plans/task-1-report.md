# Task 1 Report

## Status
DONE

## What was implemented
- Created xUnit test project `photo.Tests`.
- Added `Moq` package to `photo.Tests`.
- Fixed target framework in `photo.Tests.csproj` from `net10.0` to `net8.0-windows` so it could reference `photo.csproj`.
- Added project reference from `photo.Tests` to `photo`.
- Added test project to `photo.sln`.
- Created `IMainView.cs` interface in `photo/photo/Core/Interfaces`.
- Created `IFileSystemService.cs` interface in `photo/photo/Core/Interfaces`.

## Tests
- Built and verified scaffolding successfully. Default xUnit tests run successfully.

## Files Changed
- `photo/photo.sln`
- `photo/photo.Tests/photo.Tests.csproj`
- `photo/photo.Tests/UnitTest1.cs` (Created by scaffold)
- `photo/photo/Core/Interfaces/IMainView.cs` (Created)
- `photo/photo/Core/Interfaces/IFileSystemService.cs` (Created)

## Self-Review Findings
Everything requested in the brief was fulfilled, plus a minor necessary adjustment for the TargetFramework to make the Windows Forms project reference succeed.

## Issues/Concerns
No concerns. The interfaces match the requirements exactly.
