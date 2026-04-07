using System.Diagnostics;

namespace photo;

/// <summary>
/// 照片轉移工具的主視窗邏輯。
/// </summary>
public partial class Form1 : Form
{
    private string _jpgPath = string.Empty;
    private string _rawPath = string.Empty;
    private List<string> _jpgFiles = [];
    private int _currentIndex = -1;
    private bool _isDarkMode = false;
    private const string ThemeSettingsFile = "theme.cfg";
    
    // 縮放與平移相關變數
    private float _zoomFactor = 1.0f;
    private PointF _imageOffset = new PointF(0, 0);
    private Point _lastMousePos;
    private bool _isDragging = false;
    private Dictionary<string, int> _rotationStates = new(); // 紀錄每張照片的旋轉狀態 (0-3)

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
        SetupTransfer();
        CenterButtons();
        
        _picDisplay.Resize += (s, e) => CenterButtons();
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
    }

    private void ShowNotification(string message, Color backColor)
    {
        _notificationText = message;
        _notificationColor = backColor;
        _notificationTimer.Stop();
        _notificationTimer.Start();
        _picDisplay.Invalidate();
    }

    private void SetupTransfer()
    {
        _btnTransfer.Click += async (s, e) => await ExecuteTransferAsync();
    }

    private async Task ExecuteTransferAsync()
    {
        if (string.IsNullOrEmpty(_jpgPath) || string.IsNullOrEmpty(_rawPath))
        {
            MessageBox.Show("請先選擇 JPG 與 RAW 資料夾。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _btnTransfer.Enabled = false;
            _pbStatus.Value = 0;
            _pbStatus.Style = ProgressBarStyle.Marquee;

            int movedCount = 0;
            string tempPath = Path.Combine(_jpgPath, "暫存區");

            await Task.Run(() =>
            {
                var jpgNames = new HashSet<string>(
                    Directory.EnumerateFiles(_jpgPath, "*.*", SearchOption.TopDirectoryOnly)
                             .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                                         f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                             .Select(Path.GetFileNameWithoutExtension),
                    StringComparer.OrdinalIgnoreCase
                );

                var rawFiles = Directory.EnumerateFiles(_rawPath, "*.*", SearchOption.TopDirectoryOnly).ToList();

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
            float oldZoom = _zoomFactor;
            if (e.Delta > 0) _zoomFactor *= 1.1f;
            else _zoomFactor /= 1.1f;
            _zoomFactor = Math.Max(0.01f, Math.Min(_zoomFactor, 20f));
            float ratio = _zoomFactor / oldZoom;
            _imageOffset.X = (e.X - _picDisplay.Width / 2f) * (1 - ratio) + _imageOffset.X * ratio;
            _imageOffset.Y = (e.Y - _picDisplay.Height / 2f) * (1 - ratio) + _imageOffset.Y * ratio;
            _picDisplay.Invalidate();
        };

        _picDisplay.MouseDoubleClick += (s, e) => 
        {
            if (_picDisplay.Image == null) return;
            if (Math.Abs(_zoomFactor - 1.0f) < 0.05f) ResetZoom();
            else
            {
                float oldZoom = _zoomFactor;
                _zoomFactor = 1.0f;
                float ratio = _zoomFactor / oldZoom;
                _imageOffset.X = (e.X - _picDisplay.Width / 2f) * (1 - ratio) + _imageOffset.X * ratio;
                _imageOffset.Y = (e.Y - _picDisplay.Height / 2f) * (1 - ratio) + _imageOffset.Y * ratio;
            }
            _picDisplay.Invalidate();
        };

        _picDisplay.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { _isDragging = true; _lastMousePos = e.Location; } };
        _picDisplay.MouseMove += (s, e) => { if (_isDragging) { _imageOffset.X += (e.X - _lastMousePos.X); _imageOffset.Y += (e.Y - _lastMousePos.Y); _lastMousePos = e.Location; _picDisplay.Invalidate(); } };
        _picDisplay.MouseUp += (s, e) => _isDragging = false;

        _picDisplay.Paint += (s, e) => 
        {
            var g = e.Graphics;
            g.Clear(_picDisplay.BackColor);
            if (_picDisplay.Image != null)
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                float imgW = _picDisplay.Image.Width * _zoomFactor;
                float imgH = _picDisplay.Image.Height * _zoomFactor;
                float x = (_picDisplay.Width - imgW) / 2 + _imageOffset.X;
                float y = (_picDisplay.Height - imgH) / 2 + _imageOffset.Y;
                g.DrawImage(_picDisplay.Image, x, y, imgW, imgH);

                // 繪製左下角檔名
                string fileName = Path.GetFileName(_jpgFiles[_currentIndex]);
                using var font = new Font("Consolas", 10F, FontStyle.Bold);
                var size = g.MeasureString(fileName, font);
                var rect = new RectangleF(10, _picDisplay.Height - size.Height - 15, size.Width + 10, size.Height + 6);
                using var brush = new SolidBrush(Color.FromArgb(150, 0, 0, 0));
                g.FillRectangle(brush, rect);
                g.DrawString(fileName, font, Brushes.White, rect.X + 5, rect.Y + 3);
            }

            if (!string.IsNullOrEmpty(_notificationText))
            {
                using var font = new Font("Segoe UI", 12F, FontStyle.Bold);
                var size = g.MeasureString(_notificationText, font);
                var rect = new RectangleF(_picDisplay.Width - size.Width - 30, 20, size.Width + 20, size.Height + 10);
                using var brush = new SolidBrush(_notificationColor);
                g.FillRectangle(brush, rect);
                g.DrawString(_notificationText, font, Brushes.White, rect.X + 10, rect.Y + 5);
            }
        };
    }

    private void ResetZoom()
    {
        if (_picDisplay.Image == null || _picDisplay.Width <= 0 || _picDisplay.Height <= 0) return;
        float ratioW = (float)_picDisplay.Width / _picDisplay.Image.Width;
        float ratioH = (float)_picDisplay.Height / _picDisplay.Image.Height;
        _zoomFactor = Math.Min(ratioW, ratioH);
        _imageOffset = new PointF(0, 0);
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
        if (_currentIndex < 0 || _currentIndex >= _jpgFiles.Count) return;
        string currentJpg = _jpgFiles[_currentIndex];
        string fileName = Path.GetFileNameWithoutExtension(currentJpg);
        string tempPath = Path.Combine(_jpgPath, "暫存區");
        var undoItem = new UndoItem { OriginalJpgPath = currentJpg, Index = _currentIndex };

        try
        {
            var oldImage = _picDisplay.Image;
            _picDisplay.Image = null;
            oldImage?.Dispose();

            if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);
            string destJpg = Path.Combine(tempPath, Path.GetFileName(currentJpg));
            if (File.Exists(destJpg)) File.Delete(destJpg);
            File.Move(currentJpg, destJpg);
            undoItem.TempJpgPath = destJpg;

            if (!string.IsNullOrEmpty(_rawPath) && Directory.Exists(_rawPath))
            {
                var rawFiles = Directory.EnumerateFiles(_rawPath, fileName + ".*")
                                        .Where(f => !f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) && !f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));
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
            _btnJpgPath.BackColor = Color.FromArgb(45, 45, 45); _btnJpgPath.ForeColor = Color.White;
            _btnRawPath.BackColor = Color.FromArgb(45, 45, 45); _btnRawPath.ForeColor = Color.White;
        }
        else
        {
            BackColor = SystemColors.Control; ForeColor = SystemColors.ControlText;
            _picDisplay.BackColor = Color.FromArgb(240, 240, 240);
            _btnThemeToggle.Text = "🌙";
            _btnThemeToggle.BackColor = Color.FromArgb(217, 255, 255, 255); _btnThemeToggle.ForeColor = Color.Black;
            _btnJpgPath.BackColor = Color.White; _btnJpgPath.ForeColor = Color.Black;
            _btnRawPath.BackColor = Color.White; _btnRawPath.ForeColor = Color.Black;
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
    }

    private async void btnJpgPath_Click(object sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() != DialogResult.OK) return;
        _jpgPath = dialog.SelectedPath; _btnJpgPath.Text = _jpgPath;
        await LoadJpgFilesAsync(_jpgPath);
    }

    private void btnRawPath_Click(object sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() != DialogResult.OK) return;
        _rawPath = dialog.SelectedPath; _btnRawPath.Text = _rawPath;
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
