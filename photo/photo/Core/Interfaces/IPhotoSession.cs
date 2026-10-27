using System.Collections.Generic;

namespace photo.Core.Interfaces
{
    public interface IPhotoSession
    {
        IFileSystemService FileSystem { get; }
        List<string> JpgFiles { get; }
        int CurrentIndex { get; }
        string RootPath { get; }

        void InitializeWorkspace(string folderPath);
        void LoadJpgFiles(string folderPath);
        void Next();
        void Previous();
        int Purge();
        int RemoveRaw();
        void RotateCurrent(bool cw);
        int GetRotation(string filePath);
        bool Undo();
        bool MoveToStaging();
        bool DeleteToRecycleBin();
    }
}
