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
