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
