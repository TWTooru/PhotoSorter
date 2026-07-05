### Task 1: Setup xUnit Project & Interfaces

**Files:**
- Create: `photo/photo.Tests/photo.Tests.csproj`
- Create: `photo/photo/Core/Interfaces/IMainView.cs`
- Create: `photo/photo/Core/Interfaces/IFileSystemService.cs`

**Interfaces:**
- Consumes: `photo.sln`
- Produces: `IMainView`, `IFileSystemService` (Interfaces only)

- [ ] **Step 1: Scaffold xUnit test project**

Run in `photo` directory:
```bash
dotnet new xunit -n photo.Tests
dotnet sln photo.sln add photo.Tests/photo.Tests.csproj
dotnet add photo.Tests/photo.Tests.csproj reference photo/photo.csproj
dotnet add photo.Tests package Moq
```

- [ ] **Step 2: Create `IMainView.cs`**

```csharp
using System;

namespace photo.Core.Interfaces
{
    public interface IMainView
    {
        void DisplayImage(string imagePath, bool isHighQuality);
        void UpdateStatus(int currentIndex, int totalPhotos, string currentFileName);
        void ShowMessage(string message);
        
        event EventHandler NextRequested;
        event EventHandler PreviousRequested;
        event EventHandler DeleteRequested;
        event EventHandler UndoRequested;
        event EventHandler<string> FolderSelected;
    }
}
```

- [ ] **Step 3: Create `IFileSystemService.cs`**

```csharp
using System.Collections.Generic;

namespace photo.Core.Interfaces
{
    public interface IFileSystemService
    {
        IEnumerable<string> GetFiles(string directoryPath, string searchPattern);
        void MoveFile(string source, string destination);
        void CreateDirectory(string path);
        bool FileExists(string path);
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add photo/photo.Tests photo/photo.sln photo/photo/Core
git commit -m "test: scaffold xUnit project and core interfaces"
```
