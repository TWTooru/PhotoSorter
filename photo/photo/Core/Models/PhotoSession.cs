using System;
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
        public int CurrentIndex { get; private set; }
        
        public PhotoSession(IFileSystemService fileSystem)
        {
            _fileSystem = fileSystem;
            JpgFiles = new List<string>();
            CurrentIndex = -1;
        }
        
        public void InitializeWorkspace(string folderPath)
        {
            if (folderPath == null) throw new ArgumentNullException(nameof(folderPath));
            if (string.IsNullOrWhiteSpace(folderPath)) throw new ArgumentException("Folder path cannot be empty.", nameof(folderPath));

            _fileSystem.CreateDirectory(Path.Combine(folderPath, "raw"));
            var files = _fileSystem.GetFiles(folderPath, "*.jpg").ToList();
            JpgFiles = files;
            CurrentIndex = files.Count > 0 ? 0 : -1;
        }

        public void Next()
        {
            if (JpgFiles.Count > 0 && CurrentIndex < JpgFiles.Count - 1)
            {
                CurrentIndex++;
            }
        }

        public void Previous()
        {
            if (JpgFiles.Count > 0 && CurrentIndex > 0)
            {
                CurrentIndex--;
            }
        }
    }
}
