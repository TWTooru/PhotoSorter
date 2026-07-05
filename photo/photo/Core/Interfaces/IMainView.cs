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
