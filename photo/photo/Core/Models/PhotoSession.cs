using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using photo.Core.Interfaces;

namespace photo.Core.Models
{
    public class UndoItem
    {
        public string OriginalJpgPath { get; set; } = string.Empty;
        public string TempJpgPath { get; set; } = string.Empty;
        public List<(string original, string temp)> RawPaths { get; set; } = new();
        public int Index { get; set; }
    }

    public class PhotoSession : IPhotoSession
    {
        private readonly IFileSystemService _fileSystem;
        public IFileSystemService FileSystem => _fileSystem;
        public List<string> JpgFiles { get; private set; }
        public int CurrentIndex { get; private set; }
        public Stack<UndoItem> UndoStack { get; private set; }
        public Dictionary<string, int> RotationStates { get; private set; }
        public string RootPath { get; private set; } = string.Empty;

        public PhotoSession(IFileSystemService fileSystem)
        {
            _fileSystem = fileSystem;
            JpgFiles = new List<string>();
            CurrentIndex = -1;
            UndoStack = new Stack<UndoItem>();
            RotationStates = new Dictionary<string, int>();
        }

        public void InitializeWorkspace(string folderPath)
        {
            if (folderPath == null) throw new ArgumentNullException(nameof(folderPath));
            if (string.IsNullOrWhiteSpace(folderPath)) throw new ArgumentException("Folder path cannot be empty.", nameof(folderPath));

            RootPath = folderPath;
            string rawDirPath = Path.Combine(folderPath, "raw");
            _fileSystem.CreateDirectory(rawDirPath);

            var rawExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".3fr", ".ari", ".arw", ".bay", ".crw", ".cr2", ".cr3", ".dng", ".nef", ".nrw", ".orf", ".raf", ".raw", ".rw2", ".sr2" };
            
            foreach (var file in _fileSystem.EnumerateFiles(folderPath, "*.*"))
            {
                if (rawExtensions.Contains(Path.GetExtension(file)))
                {
                    string dest = Path.Combine(rawDirPath, Path.GetFileName(file));
                    if (_fileSystem.FileExists(dest)) _fileSystem.DeleteFile(dest);
                    _fileSystem.MoveFile(file, dest);
                }
            }

            LoadJpgFiles(folderPath);
        }

        public void LoadJpgFiles(string folderPath)
        {
            UndoStack.Clear();
            RotationStates.Clear();
            JpgFiles = _fileSystem.EnumerateFiles(folderPath, "*.jpg")
                .Concat(_fileSystem.EnumerateFiles(folderPath, "*.jpeg"))
                .Concat(_fileSystem.EnumerateFiles(folderPath, "*.heic"))
                .Concat(_fileSystem.EnumerateFiles(folderPath, "*.heif"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(f => f).ToList();
            CurrentIndex = JpgFiles.Count > 0 ? 0 : -1;
        }

        public void Next()
        {
            if (JpgFiles.Count > 0 && CurrentIndex < JpgFiles.Count - 1)
            {
                CurrentIndex++;
            }
            else if (JpgFiles.Count > 0 && CurrentIndex == -1)
            {
                CurrentIndex = 0;
            }
        }

        public void Previous()
        {
            if (JpgFiles.Count > 0 && CurrentIndex > 0)
            {
                CurrentIndex--;
            }
        }

        public int Purge()
        {
            if (string.IsNullOrEmpty(RootPath)) return 0;
            int deletedCount = 0;
            string[] folders = { "JPG暫存", "raw暫存", "暫存區" };
            foreach (var folder in folders)
            {
                string path = Path.Combine(RootPath, folder);
                if (!_fileSystem.DirectoryExists(path)) continue;
                foreach (var file in _fileSystem.EnumerateFiles(path, "*.*")) 
                { 
                    try { _fileSystem.DeleteFile(file); deletedCount++; } catch { } 
                }
            }
            UndoStack.Clear();
            return deletedCount;
        }

        public int RemoveRaw()
        {
            if (string.IsNullOrEmpty(RootPath)) return 0;
            string rawDirPath = Path.Combine(RootPath, "raw");
            if (!_fileSystem.DirectoryExists(rawDirPath)) return 0;

            int removedCount = 0;
            var currentNumbers = new HashSet<string>(
                JpgFiles.Select(f => Path.GetFileNameWithoutExtension(f)), 
                StringComparer.OrdinalIgnoreCase
            );

            foreach (var rawFile in _fileSystem.EnumerateFiles(rawDirPath, "*.*"))
            {
                string rawName = Path.GetFileNameWithoutExtension(rawFile);
                if (!currentNumbers.Contains(rawName))
                {
                    _fileSystem.SendToRecycleBin(rawFile);
                    removedCount++;
                }
            }
            return removedCount;
        }

        public void RotateCurrent(bool cw)
        {
            if (CurrentIndex >= 0 && CurrentIndex < JpgFiles.Count)
            {
                string p = JpgFiles[CurrentIndex];
                RotationStates.TryGetValue(p, out int currentRotation);
                RotationStates[p] = (currentRotation + (cw ? 1 : 3)) % 4;
            }
        }

        public int GetRotation(string filePath)
        {
            RotationStates.TryGetValue(filePath, out int rotation);
            return rotation;
        }

        public bool Undo()
        {
            if (UndoStack.Count == 0) return false;
            var item = UndoStack.Pop();
            try {
                if (_fileSystem.FileExists(item.TempJpgPath)) _fileSystem.MoveFile(item.TempJpgPath, item.OriginalJpgPath);
                foreach (var raw in item.RawPaths) if (_fileSystem.FileExists(raw.temp)) _fileSystem.MoveFile(raw.temp, raw.original);
                JpgFiles.Insert(item.Index, item.OriginalJpgPath); 
                CurrentIndex = item.Index;
                return true;
            } catch {
                return false;
            }
        }

        public bool MoveToStaging()
        {
            if (string.IsNullOrEmpty(RootPath) || CurrentIndex < 0 || CurrentIndex >= JpgFiles.Count) return false;
            string jpg = JpgFiles[CurrentIndex]; string fn = Path.GetFileNameWithoutExtension(jpg);
            string jpgT = Path.Combine(RootPath, "JPG暫存");
            string rawT = Path.Combine(RootPath, "raw暫存");
            string rawP = Path.Combine(RootPath, "raw");
            
            var item = new UndoItem { OriginalJpgPath = jpg, Index = CurrentIndex };
            try {
                _fileSystem.CreateDirectory(jpgT); 
                _fileSystem.CreateDirectory(rawT);
                
                string destJ = Path.Combine(jpgT, Path.GetFileName(jpg)); 
                if (_fileSystem.FileExists(destJ)) _fileSystem.DeleteFile(destJ);
                _fileSystem.MoveFile(jpg, destJ); 
                item.TempJpgPath = destJ;
                
                if (_fileSystem.DirectoryExists(rawP)) {
                    foreach (var rf in _fileSystem.EnumerateFiles(rawP, fn + ".*")) {
                        string dr = Path.Combine(rawT, Path.GetFileName(rf)); 
                        if (_fileSystem.FileExists(dr)) _fileSystem.DeleteFile(dr);
                        _fileSystem.MoveFile(rf, dr); 
                        item.RawPaths.Add((rf, dr));
                    }
                }
                UndoStack.Push(item); 
                JpgFiles.RemoveAt(CurrentIndex); 
                AdjustIndexAfterRemoval();
                return true;
            } catch {
                return false;
            }
        }

        public bool DeleteToRecycleBin()
        {
            if (string.IsNullOrEmpty(RootPath) || CurrentIndex < 0 || CurrentIndex >= JpgFiles.Count) return false;
            string jpg = JpgFiles[CurrentIndex]; string fn = Path.GetFileNameWithoutExtension(jpg);
            string rawP = Path.Combine(RootPath, "raw");
            
            try {
                _fileSystem.SendToRecycleBin(jpg);
                if (_fileSystem.DirectoryExists(rawP)) {
                    foreach (var rf in _fileSystem.EnumerateFiles(rawP, fn + ".*")) {
                        _fileSystem.SendToRecycleBin(rf);
                    }
                }
                JpgFiles.RemoveAt(CurrentIndex); 
                AdjustIndexAfterRemoval();
                return true;
            } catch {
                return false;
            }
        }

        private void AdjustIndexAfterRemoval()
        {
            if (JpgFiles.Count == 0) { 
                CurrentIndex = -1; 
            }
            else if (CurrentIndex >= JpgFiles.Count) {
                CurrentIndex = JpgFiles.Count - 1;
            }
        }
    }
}
