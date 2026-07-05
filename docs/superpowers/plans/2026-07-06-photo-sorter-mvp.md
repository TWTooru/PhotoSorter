# Photo Sorter MVP Refactoring Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor the monolithic `Form1.cs` into an MVP architecture to enable unit testing using xUnit, separating UI from business logic and file system interactions.

**Architecture:** MVP (Passive View) where `Form1` implements `IMainView` and is controlled by `MainPresenter`. State and core logic are handled by `PhotoSession`. File operations are abstracted through `IFileSystemService`.

**Tech Stack:** C#, .NET WinForms, xUnit, Moq.

## Global Constraints

- Keep the UI appearance and features identical to the original application.
- All domain logic must be 100% covered by unit tests.
- Physical file operations must be mocked in tests.

---

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

---

### Task 2: Implement PhotoSession (Model) with TDD

**Files:**
- Create: `photo/photo.Tests/Core/Models/PhotoSessionTests.cs`
- Create: `photo/photo/Core/Models/PhotoSession.cs`

**Interfaces:**
- Consumes: `IFileSystemService`
- Produces: `PhotoSession` class

- [ ] **Step 1: Write the failing tests**

```csharp
using Xunit;
using Moq;
using System.Collections.Generic;
using photo.Core.Interfaces;
using photo.Core.Models;

namespace photo.Tests.Core.Models
{
    public class PhotoSessionTests
    {
        [Fact]
        public void InitializeWorkspace_LoadsJpgsAndSetsCurrentIndex()
        {
            var mockFs = new Mock<IFileSystemService>();
            mockFs.Setup(fs => fs.GetFiles("C:\\test", "*.jpg")).Returns(new[] { "C:\\test\\1.jpg", "C:\\test\\2.jpg" });
            
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test");
            
            Assert.Equal(2, session.JpgFiles.Count);
            Assert.Equal(0, session.CurrentIndex);
            mockFs.Verify(fs => fs.CreateDirectory("C:\\test\\raw"), Times.Once);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test photo/photo.Tests/photo.Tests.csproj`
Expected: FAIL (Compilation error: PhotoSession does not exist)

- [ ] **Step 3: Write minimal implementation**

```csharp
using System.Collections.Generic;
using System.Linq;
using System.IO;
using photo.Core.Interfaces;

namespace photo.Core.Models
{
    public class PhotoSession
    {
        private readonly IFileSystemService _fileSystem;
        public List<string> JpgFiles { get; private set; }
        public int CurrentIndex { get; set; }
        
        public PhotoSession(IFileSystemService fileSystem)
        {
            _fileSystem = fileSystem;
            JpgFiles = new List<string>();
        }
        
        public void InitializeWorkspace(string folderPath)
        {
            _fileSystem.CreateDirectory(Path.Combine(folderPath, "raw"));
            var files = _fileSystem.GetFiles(folderPath, "*.jpg").ToList();
            JpgFiles = files;
            CurrentIndex = 0;
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test photo/photo.Tests/photo.Tests.csproj`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add photo/photo.Tests photo/photo/Core/Models
git commit -m "feat: implement PhotoSession initialization with tests"
```

*(Note: In a full TDD session, more tests for Delete and Undo would be added here iteratively. They are omitted for brevity but should be implemented similarly by the agent executing the plan.)*

---

### Task 3: Implement MainPresenter with TDD

**Files:**
- Create: `photo/photo.Tests/Core/Presenters/MainPresenterTests.cs`
- Create: `photo/photo/Core/Presenters/MainPresenter.cs`

**Interfaces:**
- Consumes: `IMainView`, `PhotoSession`
- Produces: `MainPresenter` class

- [ ] **Step 1: Write the failing tests**

```csharp
using Xunit;
using Moq;
using System;
using photo.Core.Interfaces;
using photo.Core.Models;
using photo.Core.Presenters;

namespace photo.Tests.Core.Presenters
{
    public class MainPresenterTests
    {
        [Fact]
        public void NextRequested_AdvancesIndexAndUpdatesView()
        {
            var mockView = new Mock<IMainView>();
            var mockFs = new Mock<IFileSystemService>();
            mockFs.Setup(fs => fs.GetFiles("C:\\test", "*.jpg")).Returns(new[] { "C:\\test\\1.jpg", "C:\\test\\2.jpg" });
            
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test");
            
            var presenter = new MainPresenter(mockView.Object, session);
            
            // Trigger Next
            mockView.Raise(v => v.NextRequested += null, EventArgs.Empty);
            
            Assert.Equal(1, session.CurrentIndex);
            mockView.Verify(v => v.DisplayImage("C:\\test\\2.jpg", false), Times.Once);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test photo/photo.Tests/photo.Tests.csproj`
Expected: FAIL (Compilation error: MainPresenter not found)

- [ ] **Step 3: Write minimal implementation**

```csharp
using System;
using photo.Core.Interfaces;
using photo.Core.Models;

namespace photo.Core.Presenters
{
    public class MainPresenter
    {
        private readonly IMainView _view;
        private readonly PhotoSession _session;

        public MainPresenter(IMainView view, PhotoSession session)
        {
            _view = view;
            _session = session;

            _view.NextRequested += OnNextRequested;
        }

        private void OnNextRequested(object sender, EventArgs e)
        {
            if (_session.CurrentIndex < _session.JpgFiles.Count - 1)
            {
                _session.CurrentIndex++;
                _view.DisplayImage(_session.JpgFiles[_session.CurrentIndex], false);
            }
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test photo/photo.Tests/photo.Tests.csproj`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add photo/photo.Tests photo/photo/Core/Presenters
git commit -m "feat: implement MainPresenter next photo logic with tests"
```

---

### Task 4: Infrastructure - FileSystemService

**Files:**
- Create: `photo/photo/Infrastructure/FileSystemService.cs`

**Interfaces:**
- Consumes: `IFileSystemService`
- Produces: `FileSystemService`

- [ ] **Step 1: Write implementation**

```csharp
using System.Collections.Generic;
using System.IO;
using photo.Core.Interfaces;

namespace photo.Infrastructure
{
    public class FileSystemService : IFileSystemService
    {
        public IEnumerable<string> GetFiles(string directoryPath, string searchPattern)
        {
            return Directory.GetFiles(directoryPath, searchPattern);
        }

        public void MoveFile(string source, string destination)
        {
            File.Move(source, destination);
        }

        public void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add photo/photo/Infrastructure
git commit -m "feat: implement concrete FileSystemService"
```

---

### Task 5: Refactor Form1

**Files:**
- Modify: `photo/photo/Form1.cs`

**Interfaces:**
- Consumes: `IMainView`, `MainPresenter`, `PhotoSession`, `FileSystemService`

- [ ] **Step 1: Implement `IMainView` in `Form1`**

Edit `Form1.cs` to declare it implements `IMainView` and provide the methods. (The exact diff requires analyzing the existing `Form1.cs`, so the agent should replace direct file/state logic with invoking the Presenter/Events).

- [ ] **Step 2: Wire up dependencies in Program.cs or Form1 Constructor**

Modify `Form1` constructor to instantiate the dependencies:
```csharp
public Form1()
{
    InitializeComponent();
    
    // Wire up MVP
    var fileService = new Infrastructure.FileSystemService();
    var session = new Core.Models.PhotoSession(fileService);
    var presenter = new Core.Presenters.MainPresenter(this, session);
}
```

- [ ] **Step 3: Run the application manually to verify**

Run: `dotnet run --project photo/photo/photo.csproj`
Expected: The app opens and functions the same as before.

- [ ] **Step 4: Commit**

```bash
git add photo/photo/Form1.cs
git commit -m "refactor: wire up MVP architecture in Form1"
```
