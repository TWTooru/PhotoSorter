using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using photo.Core.Interfaces;
using photo.Infrastructure;

namespace photo.Core.Presenters
{
    public class MainPresenter : IDisposable
    {
        private readonly IMainView _view;
        private readonly IPhotoSession _session;
        private readonly IImageLoader _imageLoader;
        private CancellationTokenSource? _loadCts;

        /// <summary>Exposed for test awaiting. Tracks the last image-load task.</summary>
        internal Task? LastLoadTask { get; private set; }

        public MainPresenter(IMainView view, IPhotoSession session, IImageLoader? imageLoader = null)
        {
            _view = view;
            _session = session;
            _imageLoader = imageLoader ?? new ImageLoader(session.FileSystem);

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

        private async void OnNextRequested(object? sender, EventArgs e)
        {
            _session.Next();
            LastLoadTask = UpdateViewImageAsync();
            await LastLoadTask;
        }

        private async void OnPreviousRequested(object? sender, EventArgs e)
        {
            _session.Previous();
            LastLoadTask = UpdateViewImageAsync();
            await LastLoadTask;
        }

        private async void OnDeleteRequested(object? sender, EventArgs e)
        {
            if (_session.DeleteToRecycleBin())
            {
                _view.ShowNotification("已移至垃圾桶", Color.FromArgb(180, 220, 50, 50));
                LastLoadTask = UpdateViewImageAsync();
                await LastLoadTask;
            }
        }

        private async void OnUndoRequested(object? sender, EventArgs e)
        {
            if (_session.Undo())
            {
                _view.ShowNotification("已復原", Color.FromArgb(180, 40, 167, 69));
                LastLoadTask = UpdateViewImageAsync();
                await LastLoadTask;
            }
        }

        private async void OnMoveToStagingRequested(object? sender, EventArgs e)
        {
            if (_session.MoveToStaging())
            {
                _view.ShowNotification("移至暫存區", Color.FromArgb(180, 255, 215, 0));
                LastLoadTask = UpdateViewImageAsync();
                await LastLoadTask;
            }
        }

        private async void OnFolderSelected(object? sender, string folderPath)
        {
            try
            {
                _session.InitializeWorkspace(folderPath);
                LastLoadTask = UpdateViewImageAsync();
                await LastLoadTask;
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

        private async void OnRotateRequested(object? sender, bool cw)
        {
            _session.RotateCurrent(cw);
            LastLoadTask = UpdateViewImageAsync();
            await LastLoadTask;
        }

        /// <summary>
        /// Coordinates image loading asynchronously, reporting status to View
        /// and cancelling any previous load requests.
        /// </summary>
        internal async Task UpdateViewImageAsync()
        {
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = new CancellationTokenSource();
            var token = _loadCts.Token;

            if (_session.CurrentIndex >= 0 && _session.CurrentIndex < _session.JpgFiles.Count)
            {
                string filePath = _session.JpgFiles[_session.CurrentIndex];
                int rotation = _session.GetRotation(filePath);
                string fileName = Path.GetFileName(filePath);

                _view.UpdateStatus(_session.CurrentIndex + 1, _session.JpgFiles.Count, fileName);

                try
                {
                    var (image, exifData) = await _imageLoader.LoadAsync(filePath, rotation, token);

                    if (!token.IsCancellationRequested)
                    {
                        _view.UpdateExifData(exifData);
                        _view.DisplayImage(image, fileName);
                    }
                    else
                    {
                        image?.Dispose();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Newer image requested — cancellation expected
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        _view.UpdateExifData("無 EXIF 資訊");
                        _view.DisplayImage(null, fileName);
                        _view.ShowNotification($"載入失敗: {ex.Message}",
                            Color.FromArgb(180, 220, 50, 50));
                    }
                }
            }
            else
            {
                _view.UpdateExifData(string.Empty);
                _view.UpdateStatus(0, 0, string.Empty);
                _view.DisplayImage(null, string.Empty);
            }
        }

        public void Dispose()
        {
            _loadCts?.Cancel();
            _loadCts?.Dispose();

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
