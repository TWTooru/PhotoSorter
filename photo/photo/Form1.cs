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
    
    // 縮放與平移相關
    private Matrix _transformMatrix = new Matrix();
    private Matrix _dragStartMatrix = new Matrix();
    private Point _dragStartMousePos;
    private bool _isDragging = false;
    private Dictionary<string, int> _rotationStates = new(); // 紀錄每張照片的旋轉狀態 (0-3)

    // 優化效能與字體
    private bool _isInteracting = false;
    private System.Windows.Forms.Timer _qualityTimer = new();
    private readonly Font _fileNameFont = new Font("Consolas", 10F, FontStyle.Bold);
    private readonly Font _notificationFont = new Font("Segoe UI", 12F, FontStyle.Bold);
    private readonly SolidBrush _fileNameBgBrush = new SolidBrush(Color.FromArgb(150, 0, 0, 0));

    // 復原功能
    private class UndoItem
    {
        public string OriginalJpgPath { get; set; } = string.Empty;
        public string TempJpgPath { get; set; } = string.Empty;
        public List<(string original, string temp)> RawPaths { get; set; } = new();
        public int Index { get; set; }
    }
    private Stack<UndoItem> _undoStack = new();

    // 提示訊息
    private string _notificationText = string.Empty;
    private Color _notificationColor = Color.Transparent;
    private System.Windows.Forms.Timer _notificationTimer = new();

    public Form1()
    {
        InitializeComponent();
        LoadThemeSettings();
        SetupSystem();
        ApplyRoundedCorners();
        UpdateThemeColors();
        SetupNavigation();
        SetupRotation();
        SetupZoomAndPan();
        SetupOrganize();
        SetupPurge();
        
        this.Resize += (s, e) => LayoutControls();
        this.FormClosed += (s, e) => _transformMatrix?.Dispose();
    }

    private void LayoutControls()
    {
        int padding = 16;
        int footerHeight = (int)(this.ClientSize.Height * 0.25); 
        int picHeight = this.ClientSize.Height - footerHeight - (padding * 2);

        _picDisplay.Location = new Point(padding, padding);
        _picDisplay.Size = new Size(this.ClientSize.Width - padding * 2, picHeight);

        _splitLine.Location = new Point(0, picHeight + padding + 8);
        _splitLine.Size = new Size(this.ClientSize.Width, 1);

        int footerCenterY = _splitLine.Bottom + (footerHeight / 2);
        
        _btnPrev.Location = new Point(0, (_picDisplay.Height - _btnPrev.Height) / 2);
        _btnNext.Location = new Point(_picDisplay.Width - _btnNext.Width, (_picDisplay.Height - _btnNext.Height) / 2);
        
        _btnRotateLeft.Location = new Point(_picDisplay.Width - 140, _picDisplay.Height - 80);
        _btnRotateRight.Location = new Point(_picDisplay.Width - 70, _picDisplay.Height - 80);

        _btnOrganize.Location = new Point(this.ClientSize.Width - _btnOrganize.Width - padding, footerCenterY - (_btnOrganize.Height / 2));
        _btnPurge.Location = new Point(_btnOrganize.Left - _btnPurge.Width - padding, footerCenterY - (_btnPurge.Height / 2));
        
        _btnThemeToggle.Location = new Point(this.ClientSize.Width - _btnThemeToggle.Width - padding, this.ClientSize.Height - _btnThemeToggle.Height - padding);

        ResetZoom();
        _picDisplay.Invalidate();
    }

    private void SetupSystem()
    {
        typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
          ?.SetValue(_picDisplay, true);

        _notificationTimer.Interval = 3000;
        _notificationTimer.Tick += (s, e) => { _notificationText = string.Empty; _picDisplay.Invalidate(); _notificationTimer.Stop(); };

        _qualityTimer.Interval = 150;
        _qualityTimer.Tick += (s, e) => { _isInteracting = false; _picDisplay.Invalidate(); _qualityTimer.Stop(); };
    }

    private void SetupPurge()
    {
        _btnPurge.Click += async (s, e) => 
        {
            if (string.IsNullOrEmpty(_rootPath)) return;
            var result = MessageBox.Show("確定要清空所有暫存檔案嗎？\n這將永久刪除「JPG暫存」與「raw暫存」且無法復原。", "確認清空", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes) await ExecutePurgeAsync();
        };
    }

    private async Task ExecutePurgeAsync()
    {
        try
        {
            _btnPurge.Enabled = false;
            int deletedCount = 0;
            await Task.Run(() => 
            {
                string[] folders = { "JPG暫存", "raw暫存", "暫存區" };
                foreach (var folder in folders)
                {
                    string path = Path.Combine(_rootPath, folder);
                    if (!Directory.Exists(path)) continue;
                    foreach (var file in Directory.EnumerateFiles(path)) { try { File.Delete(file); deletedCount++; } catch { } }
                    try { if (!Directory.EnumerateFileSystemEntries(path).Any()) Directory.Delete(path); } catch { }
                }
                _undoStack.Clear();
            });
            ShowNotification("暫存已清空", Color.FromArgb(180, 220, 50, 50));
            MessageBox.Show($"清理完成，共刪除 {deletedCount} 個檔案。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show($"清空失敗: {ex.Message}"); }
        finally { _btnPurge.Enabled = true; }
    }

    private void SetupOrganize()
    {
        _btnOrganize.Click += async (s, e) => 
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK) await ExecuteOrganizeAsync(dialog.SelectedPath);
        };
    }

    private async Task ExecuteOrganizeAsync(string path)
    {
        try
        {
            _rootPath = path;
            _btnOrganize.Enabled = false;
            await Task.Run(() =>
            {
                string rawDirPath = Path.Combine(_rootPath, "raw");
                if (!Directory.Exists(rawDirPath)) Directory.CreateDirectory(rawDirPath);
                var rawExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".3fr", ".ari", ".arw", ".bay", ".crw", ".cr2", ".cr3", ".dng", ".nef", ".nrw", ".orf", ".raf", ".raw", ".rw2", ".sr2" };
                foreach (var file in Directory.EnumerateFiles(_rootPath, "*.*"))
                {
                    if (rawExtensions.Contains(Path.GetExtension(file)))
                    {
                        string dest = Path.Combine(rawDirPath, Path.GetFileName(file));
                        if (File.Exists(dest)) File.Delete(dest);
                        File.Move(file, dest);
                    }
                }
            });
            await LoadJpgFilesAsync(_rootPath);
            MessageBox.Show("整理完成！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show($"失敗: {ex.Message}"); }
        finally { _btnOrganize.Enabled = true; }
    }

    private void UpdateThemeColors()
    {
        if (_isDarkMode)
        {
            BackColor = Color.FromArgb(30, 30, 30); ForeColor = Color.White;
            _picDisplay.BackColor = Color.FromArgb(45, 45, 45);
            _btnThemeToggle.Text = "☀️";
            _btnThemeToggle.BackColor = Color.FromArgb(217, 60, 60, 60);
            _btnOrganize.BackColor = Color.FromArgb(45, 45, 45);
            _btnPurge.BackColor = Color.FromArgb(217, 80, 40, 40);
            _splitLine.BackColor = Color.FromArgb(60, 60, 60);
        }
        else
        {
            BackColor = SystemColors.Control; ForeColor = SystemColors.ControlText;
            _picDisplay.BackColor = Color.FromArgb(240, 240, 240);
            _btnThemeToggle.Text = "🌙";
            _btnThemeToggle.BackColor = Color.FromArgb(217, 255, 255, 255);
            _btnOrganize.BackColor = Color.FromArgb(217, 34, 139, 34);
            _btnPurge.BackColor = Color.FromArgb(217, 220, 50, 50);
            _splitLine.BackColor = Color.FromArgb(200, 200, 200);
        }
        Color nav = Color.FromArgb(102, 0, 0, 0);
        _btnPrev.BackColor = _btnNext.BackColor = _btnRotateLeft.BackColor = _btnRotateRight.BackColor = nav;
        _btnPrev.ForeColor = _btnNext.ForeColor = _btnRotateLeft.ForeColor = _btnRotateRight.ForeColor = Color.White;
    }

    private void ApplyRoundedCorners()
    {
        void SetRegion(Control btn) {
            var path = new GraphicsPath(); var r = 12; var rect = btn.ClientRectangle;
            path.AddArc(rect.X, rect.Y, r, r, 180, 90); path.AddArc(rect.Width - r, rect.Y, r, r, 270, 90);
            path.AddArc(rect.Width - r, rect.Height - r, r, r, 0, 90); path.AddArc(rect.X, rect.Height - r, r, r, 90, 90);
            path.CloseFigure(); btn.Region = new Region(path);
        }
        _btnOrganize.Resize += (s, e) => SetRegion(_btnOrganize);
        _btnPurge.Resize += (s, e) => SetRegion(_btnPurge);
    }

    private void ShowNotification(string message, Color backColor)
    {
        _notificationText = message; _notificationColor = backColor;
        _notificationTimer.Stop(); _notificationTimer.Start(); _picDisplay.Invalidate();
    }

    private async Task LoadJpgFilesAsync(string path)
    {
        try
        {
            _undoStack.Clear(); _rotationStates.Clear();
            _jpgFiles = await Task.Run(() => Directory.EnumerateFiles(path, "*.jpg").Union(Directory.EnumerateFiles(path, "*.jpeg")).OrderBy(f => f).ToList());
            if (_jpgFiles.Count > 0) { _currentIndex = 0; DisplayImage(_jpgFiles[_currentIndex]); }
        }
        catch (Exception ex) { MessageBox.Show($"載入錯誤: {ex.Message}"); }
    }

    private void DisplayImage(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
        try {
            var old = _picDisplay.Image; _picDisplay.Image = null; old?.Dispose();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var img = Image.FromStream(stream);
            if (Array.IndexOf(img.PropertyIdList, 0x0112) > -1) {
                int orientation = img.GetPropertyItem(0x0112).Value[0];
                RotateFlipType rf = RotateFlipType.RotateNoneFlipNone;
                switch (orientation) { case 3: rf = RotateFlipType.Rotate180FlipNone; break; case 6: rf = RotateFlipType.Rotate90FlipNone; break; case 8: rf = RotateFlipType.Rotate270FlipNone; break; }
                if (rf != RotateFlipType.RotateNoneFlipNone) img.RotateFlip(rf);
            }
            if (_rotationStates.TryGetValue(filePath, out int rot)) for (int i = 0; i < rot; i++) img.RotateFlip(RotateFlipType.Rotate90FlipNone);
            _picDisplay.Image = img; ResetZoom(); _picDisplay.Invalidate();
        } catch { }
    }

    private void SetupZoomAndPan()
    {
        _picDisplay.MouseWheel += (s, e) => {
            if (_picDisplay.Image == null) return;
            float step = e.Delta > 0 ? 1.1f : 1 / 1.1f;
            _transformMatrix.Translate(-e.X, -e.Y, MatrixOrder.Append);
            _transformMatrix.Scale(step, step, MatrixOrder.Append);
            _transformMatrix.Translate(e.X, e.Y, MatrixOrder.Append);
            _isInteracting = true; _qualityTimer.Stop(); _qualityTimer.Start(); _picDisplay.Invalidate();
        };
        _picDisplay.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { _isDragging = true; _dragStartMousePos = e.Location; _dragStartMatrix = _transformMatrix.Clone(); } };
        _picDisplay.MouseMove += (s, e) => {
            if (_isDragging) {
                var newM = _dragStartMatrix.Clone(); newM.Translate(e.X - _dragStartMousePos.X, e.Y - _dragStartMousePos.Y, MatrixOrder.Append);
                _transformMatrix.Dispose(); _transformMatrix = newM;
                _isInteracting = true; _qualityTimer.Stop(); _qualityTimer.Start(); _picDisplay.Invalidate();
            }
        };
        _picDisplay.MouseUp += (s, e) => _isDragging = false;
        _picDisplay.Paint += (s, e) => {
            var g = e.Graphics; g.Clear(_picDisplay.BackColor);
            if (_picDisplay.Image != null) {
                g.InterpolationMode = _isInteracting ? InterpolationMode.Low : InterpolationMode.HighQualityBicubic;
                g.Transform = _transformMatrix; g.DrawImage(_picDisplay.Image, 0, 0, _picDisplay.Image.Width, _picDisplay.Image.Height);
                g.ResetTransform();
                string fn = Path.GetFileName(_jpgFiles[_currentIndex]); var sz = g.MeasureString(fn, _fileNameFont);
                var r = new RectangleF(10, _picDisplay.Height - sz.Height - 15, sz.Width + 10, sz.Height + 6);
                g.FillRectangle(_fileNameBgBrush, r); g.DrawString(fn, _fileNameFont, Brushes.White, r.X + 5, r.Y + 3);
            }
            if (!string.IsNullOrEmpty(_notificationText)) {
                var sz = g.MeasureString(_notificationText, _notificationFont);
                var r = new RectangleF(_picDisplay.Width - sz.Width - 30, 20, sz.Width + 20, sz.Height + 10);
                using var b = new SolidBrush(_notificationColor); g.FillRectangle(b, r);
                g.DrawString(_notificationText, _notificationFont, Brushes.White, r.X + 10, r.Y + 5);
            }
        };
    }

    private void ResetZoom() {
        if (_picDisplay.Image == null) return;
        _transformMatrix.Reset();
        float z = Math.Min((float)_picDisplay.Width / _picDisplay.Image.Width, (float)_picDisplay.Height / _picDisplay.Image.Height);
        _transformMatrix.Scale(z, z);
        _transformMatrix.Translate((_picDisplay.Width - _picDisplay.Image.Width * z) / 2, (_picDisplay.Height - _picDisplay.Image.Height * z) / 2, MatrixOrder.Append);
    }

    private void SetupNavigation() {
        _btnPrev.Click += (s, e) => { if (_jpgFiles.Count > 1) { _currentIndex = (_currentIndex - 1 + _jpgFiles.Count) % _jpgFiles.Count; DisplayImage(_jpgFiles[_currentIndex]); } };
        _btnNext.Click += (s, e) => { if (_jpgFiles.Count > 1) { _currentIndex = (_currentIndex + 1) % _jpgFiles.Count; DisplayImage(_jpgFiles[_currentIndex]); } };
    }

    private void SetupRotation() {
        _btnRotateLeft.Click += (s, e) => Rotate(false); _btnRotateRight.Click += (s, e) => Rotate(true);
        this.KeyDown += (s, e) => {
            if (e.Control && e.KeyCode == Keys.Z) Undo();
            else if (e.KeyCode == Keys.Delete) DeleteCurrentImage();
            else if (e.KeyCode == Keys.Q) _btnPrev.PerformClick();
            else if (e.KeyCode == Keys.E) _btnNext.PerformClick();
        };
    }

    private void Rotate(bool cw) {
        if (_picDisplay.Image == null) return;
        string p = _jpgFiles[_currentIndex]; _rotationStates[p] = (_rotationStates.GetValueOrDefault(p, 0) + (cw ? 1 : 3)) % 4;
        _picDisplay.Image.RotateFlip(cw ? RotateFlipType.Rotate90FlipNone : RotateFlipType.Rotate270FlipNone);
        ResetZoom(); _picDisplay.Invalidate();
    }

    private void Undo() {
        if (_undoStack.Count == 0) return;
        var item = _undoStack.Pop();
        try {
            if (File.Exists(item.TempJpgPath)) File.Move(item.TempJpgPath, item.OriginalJpgPath);
            foreach (var raw in item.RawPaths) if (File.Exists(raw.temp)) File.Move(raw.temp, raw.original);
            _jpgFiles.Insert(item.Index, item.OriginalJpgPath); _currentIndex = item.Index;
            DisplayImage(_jpgFiles[_currentIndex]); ShowNotification("已復原", Color.FromArgb(180, 40, 167, 69));
        } catch (Exception ex) { MessageBox.Show($"復原失敗: {ex.Message}"); }
    }

    private void DeleteCurrentImage() {
        if (string.IsNullOrEmpty(_rootPath) || _currentIndex < 0 || _currentIndex >= _jpgFiles.Count) return;
        string jpg = _jpgFiles[_currentIndex]; string fn = Path.GetFileNameWithoutExtension(jpg);
        string jpgT = Path.Combine(_rootPath, "JPG暫存"), rawT = Path.Combine(_rootPath, "raw暫存"), rawP = Path.Combine(_rootPath, "raw");
        var item = new UndoItem { OriginalJpgPath = jpg, Index = _currentIndex };
        try {
            var old = _picDisplay.Image; _picDisplay.Image = null; old?.Dispose();
            if (!Directory.Exists(jpgT)) Directory.CreateDirectory(jpgT); if (!Directory.Exists(rawT)) Directory.CreateDirectory(rawT);
            string destJ = Path.Combine(jpgT, Path.GetFileName(jpg)); if (File.Exists(destJ)) File.Delete(destJ);
            File.Move(jpg, destJ); item.TempJpgPath = destJ;
            if (Directory.Exists(rawP)) {
                foreach (var rf in Directory.EnumerateFiles(rawP, fn + ".*")) {
                    string dr = Path.Combine(rawT, Path.GetFileName(rf)); if (File.Exists(dr)) File.Delete(dr);
                    File.Move(rf, dr); item.RawPaths.Add((rf, dr));
                }
            }
            _undoStack.Push(item); _jpgFiles.RemoveAt(_currentIndex); ShowNotification("已刪除", Color.FromArgb(180, 220, 50, 50));
            if (_jpgFiles.Count == 0) { _currentIndex = -1; _picDisplay.Invalidate(); }
            else { if (_currentIndex >= _jpgFiles.Count) _currentIndex = _jpgFiles.Count - 1; DisplayImage(_jpgFiles[_currentIndex]); }
        } catch (Exception ex) { MessageBox.Show($"移除失敗: {ex.Message}"); }
    }

    private void btnThemeToggle_Click(object sender, EventArgs e) { _isDarkMode = !_isDarkMode; UpdateThemeColors(); SaveThemeSettings(); }
    private void LoadThemeSettings() { try { if (File.Exists(ThemeSettingsFile)) _isDarkMode = File.ReadAllText(ThemeSettingsFile).Trim() == "1"; } catch { } }
    private void SaveThemeSettings() { try { File.WriteAllText(ThemeSettingsFile, _isDarkMode ? "1" : "0"); } catch { } }
}
