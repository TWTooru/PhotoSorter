### Task 5: Refactor Form1 (View) - FULL FEATURE RETENTION

**Files:**
- Modify: `photo/photo/Core/Interfaces/IMainView.cs`
- Modify: `photo/photo/Core/Presenters/MainPresenter.cs`
- Modify: `photo/photo.Tests/Core/Presenters/MainPresenterTests.cs`
- Modify: `photo/photo/Form1.cs`
- Modify: `photo/photo/Program.cs`

**Context:**
The original plan vastly oversimplified the UI. You must refactor `Form1.cs` to use MVP but you **MUST NOT DELETE** any of the existing view-layer features:
- Zoom and Pan logic (`_transformMatrix`, mouse events)
- Paint events and EXIF overlays (`_picDisplay.Paint`)
- Custom buttons, rounded corners, theme toggle (`btnThemeToggle_Click`)
- File operations (Purge, Remove Raw, Rotate, etc.)

**Step 1: Expand IMainView and MainPresenter**
Update `IMainView` to add all necessary events that the original `Form1` supported:
- `PurgeRequested`, `RemoveRawRequested`, `RotateLeftRequested`, `RotateRightRequested`
(If theme toggle is purely UI state, it can stay entirely in `Form1` without an interface event, but if it affects domain, expose it).
Move file-based operations (like EXIF parsing, Purging, Removing RAW files, Rotating) out of `Form1` and into `MainPresenter` or `PhotoSession`.
Update `IMainView` to include methods for passing data to the View (e.g., `UpdateExifData(string camera, string lens, ...)`) so the View just renders it.
**IMPORTANT:** Any new domain logic moved to `MainPresenter` or `PhotoSession` MUST be 100% unit tested in `MainPresenterTests.cs` using the mocked `IFileSystemService`.

**Step 2: Refactor Form1.cs**
Implement the expanded `IMainView`.
- Remove the direct file/folder `Directory.GetFiles` and `File.Delete`/`File.Move` logic.
- Replace `_currentIndex` and `_jpgFiles` state with calls to the Presenter via events.
- DO NOT delete the rendering, theme, or zoom/pan logic. The goal is to separate *what* to show (Presenter) from *how* to show it (View).

**Step 3: Wire up in Program.cs**
Initialize `FileSystemService`, `PhotoSession`, `Form1`, and `MainPresenter`. Run the application.

**Step 4: Verify**
Run tests: `dotnet test photo/photo.Tests`
Build project: `dotnet build photo/photo/photo.csproj`

**Step 5: Commit**
`git add .`
`git commit -m "refactor: integrate MVP into Form1 retaining all original features"`
