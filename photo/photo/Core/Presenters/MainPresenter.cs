using System;
using System.Drawing;
using System.IO;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using photo.Core.Interfaces;
using photo.Core.Models;

namespace photo.Core.Presenters
{
    public class MainPresenter : IDisposable
    {
        private readonly IMainView _view;
        private readonly PhotoSession _session;

        public MainPresenter(IMainView view, PhotoSession session)
        {
            _view = view;
            _session = session;

            _view.NextRequested += OnNextRequested;
            _view.PreviousRequested += OnPreviousRequested;
            _view.DeleteRequested += OnDeleteRequested;
            _view.UndoRequested += OnUndoRequested;
            _view.FolderSelected += OnFolderSelected;
            
            _view.PurgeRequested += OnPurgeRequested;
            _view.RemoveRawRequested += OnRemoveRawRequested;
            _view.RotateRequested += OnRotateRequested;
            _view.MoveToStagingRequested += OnMoveToStagingRequested;
        }

        private void OnNextRequested(object? sender, EventArgs e)
        {
            _session.Next();
            UpdateViewImage();
        }

        private void OnPreviousRequested(object? sender, EventArgs e)
        {
            _session.Previous();
            UpdateViewImage();
        }

        private void OnDeleteRequested(object? sender, EventArgs e)
        {
            if (_session.DeleteToRecycleBin())
            {
                _view.ShowNotification("已移至垃圾桶", Color.FromArgb(180, 220, 50, 50));
                UpdateViewImage();
            }
        }

        private void OnUndoRequested(object? sender, EventArgs e)
        {
            if (_session.Undo())
            {
                _view.ShowNotification("已復原", Color.FromArgb(180, 40, 167, 69));
                UpdateViewImage();
            }
        }

        private void OnMoveToStagingRequested(object? sender, EventArgs e)
        {
            if (_session.MoveToStaging())
            {
                _view.ShowNotification("移至暫存區", Color.FromArgb(180, 255, 215, 0));
                UpdateViewImage();
            }
        }

        private void OnFolderSelected(object? sender, string folderPath)
        {
            try
            {
                _session.InitializeWorkspace(folderPath);
                UpdateViewImage();
            }
            catch (Exception ex)
            {
                _view.ShowMessage($"載入錯誤: {ex.Message}");
            }
        }

        private void OnPurgeRequested(object? sender, EventArgs e)
        {
            int deletedCount = _session.Purge();
            _view.ShowNotification("暫存已清空", Color.FromArgb(180, 220, 50, 50));
            _view.ShowMessage($"清理完成，共刪除 {deletedCount} 個檔案。");
        }

        private void OnRemoveRawRequested(object? sender, EventArgs e)
        {
            int count = _session.RemoveRaw();
            _view.ShowNotification("RAW 檔案已清理", Color.FromArgb(180, 50, 50, 220));
            _view.ShowMessage($"清理完成，共移除 {count} 個孤立 RAW 檔案。");
        }

        private void OnRotateRequested(object? sender, bool cw)
        {
            _session.RotateCurrent(cw);
            UpdateViewImage();
        }

        private void UpdateViewImage()
        {
            if (_session.CurrentIndex >= 0 && _session.CurrentIndex < _session.JpgFiles.Count)
            {
                string filePath = _session.JpgFiles[_session.CurrentIndex];
                int rotation = _session.GetRotation(filePath);
                
                ExtractAndSendExif(filePath);
                _view.DisplayImage(filePath, rotation);
            }
            else
            {
                _view.UpdateExifData(string.Empty);
                _view.DisplayImage(string.Empty, 0);
            }
        }

        private void ExtractAndSendExif(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !_session.FileSystem.FileExists(filePath))
            {
                _view.UpdateExifData("無 EXIF 資訊");
                return;
            }

            try
            {
                using var stream = _session.FileSystem.OpenRead(filePath);
                var directories = ImageMetadataReader.ReadMetadata(stream);
                var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();

                string make = ifd0?.GetDescription(ExifDirectoryBase.TagMake) ?? "";
                string model = ifd0?.GetDescription(ExifDirectoryBase.TagModel) ?? "";
                string fullModel = $"{make} {model}".Trim();
                if (string.IsNullOrEmpty(fullModel)) fullModel = "Unknown";

                string shutter = subIfd?.GetDescription(ExifDirectoryBase.TagExposureTime) ?? "Unknown";
                string aperture = subIfd?.GetDescription(ExifDirectoryBase.TagFNumber)?.Replace("f/", "") ?? "Unknown";
                string iso = subIfd?.GetDescription(ExifDirectoryBase.TagIsoEquivalent) ?? "Unknown";
                string focal = subIfd?.GetDescription(ExifDirectoryBase.TagFocalLength)?.Replace(" mm", "") ?? "Unknown";

                _view.UpdateExifData($"{fullModel} | {shutter}s | f/{aperture} | ISO {iso} | {focal}mm");
            }
            catch
            {
                _view.UpdateExifData("無 EXIF 資訊");
            }
        }

        public void Dispose()
        {
            _view.NextRequested -= OnNextRequested;
            _view.PreviousRequested -= OnPreviousRequested;
            _view.DeleteRequested -= OnDeleteRequested;
            _view.UndoRequested -= OnUndoRequested;
            _view.FolderSelected -= OnFolderSelected;
            _view.PurgeRequested -= OnPurgeRequested;
            _view.RemoveRawRequested -= OnRemoveRawRequested;
            _view.RotateRequested -= OnRotateRequested;
            _view.MoveToStagingRequested -= OnMoveToStagingRequested;
        }
    }
}
