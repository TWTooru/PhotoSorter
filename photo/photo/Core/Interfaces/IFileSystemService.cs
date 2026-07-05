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
