# Photo Sorter MVP Refactoring Design

## 1. Overview
The goal is to refactor the monolithic WinForms application (`Form1.cs`) into a Model-View-Presenter (MVP) architecture to enable Test-Driven Development (TDD) using xUnit. By decoupling the core business logic from the UI and the physical file system, we can write robust, fast-executing unit tests.

## 2. Architecture: MVP (Passive View)
The architecture splits responsibilities into three distinct layers:
- **View (`IMainView`)**: An abstraction of the UI. It will be implemented by `Form1.cs`. The View is "passive"; it contains zero business logic, only triggering events based on user input and exposing methods to update the display.
- **Presenter (`MainPresenter`)**: The orchestrator. It listens to View events, delegates work to the Model, and updates the View state. 
- **Model (`PhotoSession`)**: Handles the state of the current photo sorting session (current index, list of photos, undo stack) and enforces business rules (like moving corresponding RAW files).

## 3. Core Interfaces & Classes

### 3.1 View Abstraction
```csharp
public interface IMainView
{
    // Output
    void DisplayImage(string imagePath, bool isHighQuality);
    void UpdateStatus(int currentIndex, int totalPhotos, string currentFileName);
    void ShowMessage(string message);
    
    // Input Events
    event EventHandler NextRequested;
    event EventHandler PreviousRequested;
    event EventHandler DeleteRequested;
    event EventHandler UndoRequested;
    event EventHandler<string> FolderSelected;
}
```

### 3.2 File System Abstraction
To prevent unit tests from writing to the physical hard drive, all file I/O operations will be routed through this interface.
```csharp
public interface IFileSystemService
{
    IEnumerable<string> GetFiles(string directoryPath, string searchPattern);
    void MoveFile(string source, string destination);
    void CreateDirectory(string path);
    bool FileExists(string path);
}
```

### 3.3 Domain Model
Encapsulates the state and file-organization logic.
```csharp
public class PhotoSession
{
    public List<string> JpgFiles { get; private set; }
    public int CurrentIndex { get; private set; }
    
    // Core Logic (Dependencies injected via constructor)
    public void DeleteCurrentPhoto(); 
    public void UndoLastDeletion();
    public void InitializeWorkspace(string folderPath);
}
```

## 4. TDD Workflow
When transitioning to implementation, the development will follow these steps:
1. **Model Tests**: Write xUnit tests for `PhotoSession` using a mocked `IFileSystemService` to verify that RAW files are correctly synced, Delete logic works, and the Undo stack behaves as expected.
2. **Presenter Tests**: Write xUnit tests for `MainPresenter` using a mocked `IMainView` to verify that the UI is updated correctly in response to events.
3. **Infrastructure**: Implement the concrete `FileSystemService` wrapping `System.IO`.
4. **UI Refactoring**: Refactor `Form1.cs` to implement `IMainView`, remove its internal business logic, and instantiate the `MainPresenter`.

## 5. File Structure Target
```text
PhotoSorter/
├── Core/
│   ├── Interfaces/
│   │   ├── IMainView.cs
│   │   └── IFileSystemService.cs
│   ├── Models/
│   │   └── PhotoSession.cs
│   └── Presenters/
│       └── MainPresenter.cs
├── Infrastructure/
│   └── FileSystemService.cs
├── UI/
│   └── Form1.cs (Implements IMainView)
```
