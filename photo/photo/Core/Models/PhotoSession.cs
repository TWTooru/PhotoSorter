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
