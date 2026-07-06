using System.Collections.Generic;
using System.IO;

namespace photo.Core.Interfaces
{
    public interface IFileSystemService
    {
        IEnumerable<string> GetFiles(string directoryPath, string searchPattern);
        IEnumerable<string> EnumerateFiles(string path, string searchPattern);
        void MoveFile(string source, string destination);
        void CreateDirectory(string path);
        bool FileExists(string path);
        void DeleteFile(string path);
        bool DirectoryExists(string path);
        void SendToRecycleBin(string path);
        Stream OpenRead(string path);
    }
}
