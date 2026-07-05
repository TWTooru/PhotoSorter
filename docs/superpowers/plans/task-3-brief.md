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
            session.InitializeWorkspace("C:\\test"); // sets current index to 0
            
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
            // Note: Since Task 2 implemented `Next()` and made CurrentIndex setter private,
            // we use the encapsulated method.
            _session.Next();
            
            if (_session.CurrentIndex >= 0 && _session.CurrentIndex < _session.JpgFiles.Count)
            {
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
