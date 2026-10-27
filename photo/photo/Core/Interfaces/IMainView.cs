using System;
using System.Drawing;

namespace photo.Core.Interfaces
{
    public interface IMainView
    {
        void DisplayImage(Image? image, string fileName);
        void UpdateStatus(int currentIndex, int totalPhotos, string currentFileName);
        void ShowMessage(string message);
        void ShowNotification(string message, Color backColor);
        void UpdateExifData(string exifInfo);
        
        event EventHandler NextRequested;
        event EventHandler PreviousRequested;
        event EventHandler DeleteRequested;
        event EventHandler UndoRequested;
        event EventHandler<string> FolderSelected;
        
        event EventHandler PurgeRequested;
        event EventHandler RemoveRawRequested;
        event EventHandler<bool> RotateRequested;
        event EventHandler MoveToStagingRequested;
    }
}
