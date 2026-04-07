using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace photo;

/// <summary>
/// 照片轉移工具的主視窗邏輯。
/// </summary>
public partial class Form1 : Form
{
    private string _rootPath = string.Empty;
    private List<string> _jpgFiles = [];
    private int _currentIndex = -1;
    private bool _isDarkMode = false;
    private const string ThemeSettingsFile = "theme.cfg";
    
    // 縮放與平移相關 (使用 Matrix 模擬 RenderTransform)
    private Matrix _transformMatrix = new Matrix();
    private Matrix _dragStartMatrix = new Matrix();
    private Point _dragStartMousePos;
    private Point _lastMousePos;
    private bool _isDragging = false;
    private Dictionary<string, int> _rotationStates = new(); // 紀錄每張照片的旋轉狀態 (0-3)

    // 優化效能相關
    private bool _isInteracting = false;
    private System.Windows.Forms.Timer _qualityTimer = new();
    private Stopwatch _frameStopwatch = Stopwatch.StartNew();
    private readonly Font _fileNameFont = new Font("Consolas", 10F, FontStyle.Bold);
    private readonly Font _notificationFont = new Font("Segoe UI", 12F, FontStyle.Bold);
    private readonly SolidBrush _fileNameBgBrush = new SolidBrush(Color.FromArgb(150, 0, 0, 0));

    // 復原功能相關
    private class UndoItem
    {
        public string OriginalJpgPath { get; set; } = string.Empty;
        public string TempJpgPath { get; set; } = string.Empty;
        public List<(string original, string temp)> RawPaths { get; set; } = new();
        public int Index { get; set; }
    }
    private Stack<UndoItem> _undoStack = new();

    // 提示訊息相關
    private string _notificationText = string.Empty;
    private Color _notificationColor = Color.Transparent;
    private System.Windows.Forms.Timer _notificationTimer = new();

    public Form1()
    {
        InitializeComponent();
        LoadThemeSettings();
        SetupSystem();
        ApplyRoundedCorners();
        UpdateThemeColors(); // 初始化主題
        SetupNavigation();
        SetupRotation();
        SetupZoomAndPan();
        SetupOrganize();
        SetupTransfer();
        CenterButtons();
        
        _picDisplay.Resize += (s, e) => CenterButtons();
        this.FormClosed += (s, e) => _transformMatrix?.Dispose();
    }

    private void SetupSystem()
    {
        // 開啟雙重緩衝避免閃爍
        typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
          ?.SetValue(_picDisplay, true);

        _notificationTimer.Interval = 3000;
        _notificationTimer.Tick += (s, e) => 
        {
            _notificationText = string.Empty;
            _picDisplay.Invalidate();
            _notificationTimer.Stop();
        };

        _qualityTimer.Interval = 150; // 延遲 150ms 後恢復高品質
        _qualityTimer.Tick += (s, e) => 
        {
            _isInteracting = false;
            _picDisplay.Invalidate();
            _qualityTimer.Stop();
        };
    }

    private void ShowNotification(string message, Color backColor)
    {
        _notificationText = message;
        _notificationColor = backColor;
        _notificationTimer.Stop();
        _notificationTimer.Start();
        _picDisplay.Invalidate();
    }

    private void SetupOrganize()
    {
        _btnOrganize.Click += async (s, e) => 
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                await ExecuteOrganizeAsync(dialog.SelectedPath);
            }
        };
    }

    private async Task ExecuteOrganizeAsync(string path)
    {
        try
        {
            _rootPath = path;
            _btnOrganize.Enabled = false;
            _btnTransfer.Enabled = false;
            _pbStatus.Value = 0;
            _pbStatus.Style = ProgressBarStyle.Marquee;

            await Task.Run(() =>
            {
                string rawDirPath = Path.Combine(_rootPath, "raw");
                if (!Directory.Exists(rawDirPath)) Directory.CreateDirectory(rawDirPath);

                var rawExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
                { 
                    ".3fr", ".ari", ".arw", ".bay", ".crw", ".cr2", ".cr3", ".cap", 
                    ".dcs", ".dcr", ".dng", ".drf", ".eip", ".erf", ".fff", ".iiq", 
                    ".k25", ".kdc", ".mef", ".mos", ".mrw", ".nef", ".nrw", ".obm", 
                    ".orf", ".pef", ".ptx", ".pxn", ".r3d", ".raf", ".raw", ".rwl", 
                    ".rw2", ".rwz", ".sr2", ".srf", ".srw", ".x3f"
                };

                var files = Directory.EnumerateFiles(_rootPath, "*.*", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file);
                    if (rawExtensions.Contains(ext))
                    {
                        string destPath = Path.Combine(rawDirPath, Path.GetFileName(file));
                        if (File.Exists(destPath)) File.Delete(destPath);
                        File.Move(file, destPath);
                    }
                }
            });

            _pbStatus.Style = ProgressBarStyle.Blocks;
            _pbStatus.Value = 100;
            
            await LoadJpgFilesAsync(_rootPath);
            MessageBox.Show("照片整理與 RAW 歸檔已完成！\n現在可以開始挑選照片了。", 
                            "整理完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"整理失敗: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnOrganize.Enabled = true;
            _btnTransfer.Enabled = true;
            _pbStatus.Value = 0;
        }
    }

    private void SetupTransfer()
    {
        _btnTransfer.Click += async (s, e) => await ExecuteTransferAsync();
    }

    private async Task ExecuteTransferAsync()
    {
        if (string.IsNullOrEmpty(_rootPath))
        {
            MessageBox.Show("請先點擊「整理照片」選擇資料夾。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string rawPath = Path.Combine(_rootPath, "raw");
        if (!Directory.Exists(rawPath))
        {
            MessageBox.Show("找不到 raw 資料夾，請先執行整理。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _btnTransfer.Enabled = false;
            _pbStatus.Value = 0;
            _pbStatus.Style = ProgressBarStyle.Marquee;

            int movedCount = 0;
            string tempPath = Path.Combine(_rootPath, "暫存區");

            await Task.Run(() =>
            {
                var jpgNames = new HashSet<string>(
                    Directory.EnumerateFiles(_rootPath, "*.*", SearchOption.TopDirectoryOnly)
                             .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                                         f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                             .Select(Path.GetFileNameWithoutExtension),
                    StringComparer.OrdinalIgnoreCase
                );

                var rawFiles = Directory.EnumerateFiles(rawPath, "*.*", SearchOption.TopDirectoryOnly).ToList();

                foreach (var rawFilePath in rawFiles)
                {
                    string rawName = Path.GetFileNameWithoutExtension(rawFilePath);
                    if (!jpgNames.Contains(rawName))
                    {
                        if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);
                        string destPath = Path.Combine(tempPath, Path.GetFileName(rawFilePath));
                        if (File.Exists(destPath)) File.Delete(destPath);
                        File.Move(rawFilePath, destPath);
                        movedCount++;
                    }
                }
            });

            _pbStatus.Style = ProgressBarStyle.Blocks;
            _pbStatus.Value = 100;
            MessageBox.Show($"共 {movedCount} 個 RAW 檔案移至暫存區。", 
                            "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"執行轉移時發生錯誤: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnTransfer.Enabled = true;
            _pbStatus.Style = ProgressBarStyle.Blocks;
            _pbStatus.Value = 0;
        }
    }

    private void LoadThemeSettings()
    {
        try
        {
            if (File.Exists(ThemeSettingsFile))
            {
                string content = File.ReadAllText(ThemeSettingsFile);
                _isDarkMode = content.Trim() == "1";
            }
        }
        catch { }
    }

    private void SaveThemeSettings()
    {
        try { File.WriteAllText(ThemeSettingsFile, _isDarkMode ? "1" : "0"); }
        catch { }
    }

    private void SetupZoomAndPan()
    {
        _picDisplay.MouseWheel += (s, e) => 
        {
            if (_picDisplay.Image == null) return;
            
            float zoomStep = e.Delta > 0 ? 1.1f : 1 / 1.1f;
            
            var elements = _transformMatrix.Elements;
            float currentZoom = elements[0];
            if (zoomStep > 1 && currentZoom > 20f) return;
            if (zoomStep < 1 && currentZoom < 0.01f) return;

            // 正確的以滑鼠位置為中心縮放:
            // 1. 將滑鼠位置移到原點 (Append 模式)
            _transformMatrix.Translate(-e.X, -e.Y, MatrixOrder.Append);
            // 2. 縮放
            _transformMatrix.Scale(zoomStep, zoomStep, MatrixOrder.Append);
            // 3. 移回原滑鼠位置
            _transformMatrix.Translate(e.X, e.Y, MatrixOrder.Append);
            
            _isInteracting = true;
            _qualityTimer.Stop();
            _qualityTimer.Start();
            _picDisplay.Invalidate(); 
        };

        _picDisplay.MouseDoubleClick += (s, e) => 
        {
            if (_picDisplay.Image == null) return;
            ResetZoom();
            _picDisplay.Invalidate();
        };

        _picDisplay.MouseDown += (s, e) => 
        { 
            if (e.Button == MouseButtons.Left) 
            { 
                _isDragging = true; 
                _dragStartMousePos = e.Location; 
                _dragStartMatrix = _transformMatrix.Clone();
                _picDisplay.Capture = true;
            } 
        };

        _picDisplay.MouseMove += (s, e) => 
        { 
            if (_isDragging) 
            { 
                float dx = e.X - _dragStartMousePos.X;
                float dy = e.Y - _dragStartMousePos.Y;
                
                if (dx == 0 && dy == 0) return;

                // 採用絕對位移計算，完全消除累積誤差
                var newMatrix = _dragStartMatrix.Clone();
                newMatrix.Translate(dx, dy, MatrixOrder.Append);
                
                _transformMatrix.Dispose();
                _transformMatrix = newMatrix;

                _isInteracting = true;
                _qualityTimer.Stop();
                _qualityTimer.Start();
                _picDisplay.Invalidate(); 
            } 
        };

        _picDisplay.MouseUp += (s, e) => 
        { 
            _isDragging = false; 
            _picDisplay.Capture = false;
        };

        _picDisplay.Paint += (s, e) => 
        {
            var g = e.Graphics;
            g.Clear(_picDisplay.BackColor);
            
            if (_picDisplay.Image != null)
            {
                if (_isInteracting)
                {
                    g.InterpolationMode = InterpolationMode.Low;
                    g.CompositingQuality = CompositingQuality.HighSpeed;
                    g.SmoothingMode = SmoothingMode.HighSpeed;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                }
                else
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                }
                
                g.Transform = _transformMatrix;
                // 關鍵修正：指定寬高以避免 GDI+ 根據 DPI 自動縮放
                g.DrawImage(_picDisplay.Image, 0, 0, _picDisplay.Image.Width, _picDisplay.Image.Height);

                g.ResetTransform();
                
                // 繪製左下角檔名
                string fileName = Path.GetFileName(_jpgFiles[_currentIndex]);
                var size = g.MeasureString(fileName, _fileNameFont);
                var rect = new RectangleF(10, _picDisplay.Height - size.Height - 15, size.Width + 10, size.Height + 6);
                g.FillRectangle(_fileNameBgBrush, rect);
                g.DrawString(fileName, _fileNameFont, Brushes.White, rect.X + 5, rect.Y + 3);
            }

            if (!string.IsNullOrEmpty(_notificationText))
            {
                g.ResetTransform();
                var size = g.MeasureString(_notificationText, _notificationFont);
                var rect = new RectangleF(_picDisplay.Width - size.Width - 30, 20, size.Width + 20, size.Height + 10);
                using var brush = new SolidBrush(_notificationColor);
                g.FillRectangle(brush, rect);
                g.DrawString(_notificationText, _notificationFont, Brushes.White, rect.X + 10, rect.Y + 5);
            }
        };
    }

    private void ResetZoom()
    {
        if (_picDisplay.Image == null || _picDisplay.ClientSize.Width <= 0 || _picDisplay.ClientSize.Height <= 0) return;
        
        _transformMatrix.Reset();
        
        var canvasSize = _picDisplay.ClientSize;
        var imageSize = _picDisplay.Image.Size;

        float ratioW = (float)canvasSize.Width / imageSize.Width;
        float ratioH = (float)canvasSize.Height / imageSize.Height;
        float zoom = Math.Min(ratioW, ratioH);
        
        // 縮放
        _transformMatrix.Scale(zoom, zoom);
        
        // 平移到中心 (位移量需考慮縮放後的尺寸)
        float offsetX = (canvasSize.Width - (imageSize.Width * zoom)) / 2f;
        float offsetY = (canvasSize.Height - (imageSize.Height * zoom)) / 2f;
        
        _transformMatrix.Translate(offsetX, offsetY, MatrixOrder.Append);
    }

    private void SetupNavigation()
    {
        _btnPrev.Click += (s, e) => ShowPreviousImage();
        _btnNext.Click += (s, e) => ShowNextImage();
    }

    private void ShowPreviousImage()
    {
        if (_jpgFiles.Count <= 1) return;
        _currentIndex = (_currentIndex - 1 + _jpgFiles.Count) % _jpgFiles.Count;
        DisplayImage(_jpgFiles[_currentIndex]);
    }

    private void ShowNextImage()
    {
        if (_jpgFiles.Count <= 1) return;
        _currentIndex = (_currentIndex + 1) % _jpgFiles.Count;
        DisplayImage(_jpgFiles[_currentIndex]);
    }

    private void SetupRotation()
    {
        _btnRotateLeft.Click += (s, e) => RotateImage(false);
        _btnRotateRight.Click += (s, e) => RotateImage(true);

        this.KeyDown += (s, e) => 
        {
            if (e.Control && e.KeyCode == Keys.Z) UndoLastAction();
            else if (e.KeyCode == Keys.Q) ShowPreviousImage();
            else if (e.KeyCode == Keys.E) ShowNextImage();
            else if (e.KeyCode == Keys.R) RotateImage(true);
            else if (e.KeyCode == Keys.Delete) DeleteCurrentImage();
        };
    }

    private void RotateImage(bool clockwise)
    {
        if (_picDisplay.Image == null) return;
        string path = _jpgFiles[_currentIndex];
        _rotationStates[path] = (_rotationStates.GetValueOrDefault(path, 0) + (clockwise ? 1 : 3)) % 4;
        _picDisplay.Image.RotateFlip(clockwise ? RotateFlipType.Rotate90FlipNone : RotateFlipType.Rotate270FlipNone);
        ResetZoom();
        _picDisplay.Invalidate();
    }

    private void UndoLastAction()
    {
        if (_undoStack.Count == 0) return;
        var item = _undoStack.Pop();
        try
        {
            if (File.Exists(item.TempJpgPath)) File.Move(item.TempJpgPath, item.OriginalJpgPath);
            foreach (var raw in item.RawPaths) { if (File.Exists(raw.temp)) File.Move(raw.temp, raw.original); }
            if (item.Index >= 0 && item.Index <= _jpgFiles.Count)
            {
                _jpgFiles.Insert(item.Index, item.OriginalJpgPath);
                _currentIndex = item.Index;
                DisplayImage(_jpgFiles[_currentIndex]);
                ShowNotification("已復原", Color.FromArgb(180, 40, 167, 69));
            }
        }
        catch (Exception ex) { MessageBox.Show($"復原失敗: {ex.Message}"); }
    }

    private void DeleteCurrentImage()
    {
        if (string.IsNullOrEmpty(_rootPath) || _currentIndex < 0 || _currentIndex >= _jpgFiles.Count) return;
        
        string currentJpg = _jpgFiles[_currentIndex];
        string fileName = Path.GetFileNameWithoutExtension(currentJpg);
        string tempPath = Path.Combine(_rootPath, "暫存區");
        string rawPath = Path.Combine(_rootPath, "raw");
        
        var undoItem = new UndoItem { OriginalJpgPath = currentJpg, Index = _currentIndex };

        try
        {
            var oldImage = _picDisplay.Image;
            _picDisplay.Image = null;
            oldImage?.Dispose();

            if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);
            
            // 移 JPG
            string destJpg = Path.Combine(tempPath, Path.GetFileName(currentJpg));
            if (File.Exists(destJpg)) File.Delete(destJpg);
            File.Move(currentJpg, destJpg);
            undoItem.TempJpgPath = destJpg;

            // 移對應的 RAW (在 raw 子資料夾內)
            if (Directory.Exists(rawPath))
            {
                var rawFiles = Directory.EnumerateFiles(rawPath, fileName + ".*");
                foreach (var rawFile in rawFiles)
                {
                    string destRaw = Path.Combine(tempPath, Path.GetFileName(rawFile));
                    if (File.Exists(destRaw)) File.Delete(destRaw);
                    File.Move(rawFile, destRaw);
                    undoItem.RawPaths.Add((rawFile, destRaw));
                }
            }

            _undoStack.Push(undoItem);
            _jpgFiles.RemoveAt(_currentIndex);
            ShowNotification("已刪除", Color.FromArgb(180, 220, 50, 50));
            
            if (_jpgFiles.Count == 0) { _currentIndex = -1; _picDisplay.Invalidate(); }
            else { if (_currentIndex >= _jpgFiles.Count) _currentIndex = _jpgFiles.Count - 1; DisplayImage(_jpgFiles[_currentIndex]); }
        }
        catch (Exception ex) { MessageBox.Show($"移除失敗: {ex.Message}"); }
    }

    private void CenterButtons()
    {
        _btnPrev.Top = (_picDisplay.Height - _btnPrev.Height) / 2;
        _btnNext.Top = (_picDisplay.Height - _btnNext.Height) / 2;
        ResetZoom();
        _picDisplay.Invalidate();
    }

    private void btnThemeToggle_Click(object sender, EventArgs e)
    {
        _isDarkMode = !_isDarkMode;
        UpdateThemeColors();
        SaveThemeSettings();
    }

    private void UpdateThemeColors()
    {
        Color navBtnColor = Color.FromArgb(102, 0, 0, 0);
        if (_isDarkMode)
        {
            BackColor = Color.FromArgb(30, 30, 30); ForeColor = Color.White;
            _picDisplay.BackColor = Color.FromArgb(45, 45, 45);
            _btnThemeToggle.Text = "☀️";
            _btnThemeToggle.BackColor = Color.FromArgb(217, 60, 60, 60); _btnThemeToggle.ForeColor = Color.White;
            _btnOrganize.BackColor = Color.FromArgb(45, 45, 45); _btnOrganize.ForeColor = Color.White;
        }
        else
        {
            BackColor = SystemColors.Control; ForeColor = SystemColors.ControlText;
            _picDisplay.BackColor = Color.FromArgb(240, 240, 240);
            _btnThemeToggle.Text = "🌙";
            _btnThemeToggle.BackColor = Color.FromArgb(217, 255, 255, 255); _btnThemeToggle.ForeColor = Color.Black;
            _btnOrganize.BackColor = Color.FromArgb(217, 34, 139, 34); _btnOrganize.ForeColor = Color.White;
        }
        _btnPrev.BackColor = navBtnColor; _btnPrev.ForeColor = Color.White;
        _btnNext.BackColor = navBtnColor; _btnNext.ForeColor = Color.White;
        _btnRotateLeft.BackColor = navBtnColor; _btnRotateLeft.ForeColor = Color.White;
        _btnRotateRight.BackColor = navBtnColor; _btnRotateRight.ForeColor = Color.White;
    }

    private void ApplyRoundedCorners()
    {
        _btnTransfer.Resize += (s, e) => 
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            var radius = 12; var rect = _btnTransfer.ClientRectangle;
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Width - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Width - radius, rect.Height - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();
            _btnTransfer.Region = new Region(path);
        };
        _btnOrganize.Resize += (s, e) => 
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            var radius = 12; var rect = _btnOrganize.ClientRectangle;
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Width - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Width - radius, rect.Height - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();
            _btnOrganize.Region = new Region(path);
        };
    }

    private async Task LoadJpgFilesAsync(string path)
    {
        try
        {
            _pbStatus.Style = ProgressBarStyle.Marquee; _undoStack.Clear(); _rotationStates.Clear();
            _jpgFiles = await Task.Run(() => Directory.EnumerateFiles(path, "*.jpg", SearchOption.TopDirectoryOnly).Union(Directory.EnumerateFiles(path, "*.jpeg", SearchOption.TopDirectoryOnly)).OrderBy(f => f).ToList());
            _pbStatus.Style = ProgressBarStyle.Blocks;
            if (_jpgFiles.Count > 0) { _currentIndex = 0; DisplayImage(_jpgFiles[_currentIndex]); }
        }
        catch (Exception ex) { MessageBox.Show($"載入錯誤: {ex.Message}"); }
    }

    private void DisplayImage(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
        try
        {
            var oldImage = _picDisplay.Image; _picDisplay.Image = null; oldImage?.Dispose();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var img = Image.FromStream(stream);

            if (Array.IndexOf(img.PropertyIdList, 0x0112) > -1)
            {
                var prop = img.GetPropertyItem(0x0112);
                int orientation = prop.Value[0];
                RotateFlipType rf = RotateFlipType.RotateNoneFlipNone;
                switch (orientation) { case 3: rf = RotateFlipType.Rotate180FlipNone; break; case 6: rf = RotateFlipType.Rotate90FlipNone; break; case 8: rf = RotateFlipType.Rotate270FlipNone; break; }
                if (rf != RotateFlipType.RotateNoneFlipNone) img.RotateFlip(rf);
            }

            // 恢復這張照片之前的旋轉狀態
            if (_rotationStates.TryGetValue(filePath, out int rotCount))
            {
                for (int i = 0; i < rotCount; i++) img.RotateFlip(RotateFlipType.Rotate90FlipNone);
            }

            _picDisplay.Image = img; ResetZoom(); _picDisplay.Invalidate();
        }
        catch (Exception ex) { Debug.WriteLine($"顯示失敗: {ex.Message}"); }
    }
}
