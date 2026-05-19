using System.Diagnostics;
using System.Drawing.Drawing2D;
using Microsoft.VisualBasic.FileIO;

namespace photo;

/// <summary>
/// 照片挑選工具的主視窗邏輯。
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
    private Dictionary<string, int> _rotationStates = new();

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

    // 狀態與快取欄位
    private string _exifInfo = string.Empty;
    private string _prefetchedPath = string.Empty;
    private Image? _prefetchedImage = null;

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
        SetupRemoveRaw();
        SetupActionButtons();
        
        this.FormClosed += (s, e) => _transformMatrix?.Dispose();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ResetZoom();
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

    private void SetupActionButtons()
    {
        void StyleButton(Button btn, Color baseColor, Color hoverColor, string icon, int yOffset) {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = hoverColor;
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(100, baseColor);
            btn.BackColor = baseColor;
            btn.Cursor = Cursors.Hand;
            btn.Text = string.Empty; 

            btn.Paint += (s, e) => {
                var g = e.Graphics;
                // 繪製圓形背景的抗鋸齒設定
                g.SmoothingMode = SmoothingMode.AntiAlias;
                
                using var font = new Font("Segoe UI Emoji", 22F);
                var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
                
                // 建立一個微調後的矩形，將圖示向下壓一點點以修正視覺重心
                var rect = btn.ClientRectangle;
                rect.Offset(0, yOffset); 
                
                TextRenderer.DrawText(g, icon, font, rect, Color.Black, flags);
            };
        }

        // 星星垂直居中通常較準，yOffset 設為 1；垃圾桶通常偏高，yOffset 設為 4 或 5
        StyleButton(_btnMaybe, Color.FromArgb(255, 202, 40), Color.FromArgb(255, 179, 0), "⭐", 1);
        StyleButton(_btnTrash, Color.FromArgb(239, 83, 80), Color.FromArgb(211, 47, 47), "🗑️", 4);
        
        _btnMaybe.Click += (s, e) => MoveToStaging();
        _btnTrash.Click += (s, e) => DeleteToRecycleBin();
    }

    private void SetupPurge()
    {
        _btnPurge.Click += async (s, e) => 
        {
            if (string.IsNullOrEmpty(_rootPath)) { MessageBox.Show("請先選擇資料夾。", "提示"); return; }
            var result = MessageBox.Show("確定要清空所有暫存檔案嗎？\n這將永久刪除相關暫存資料夾內容且無法復原。", "確認清空", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
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
                }
                _undoStack.Clear();
            });
            ShowNotification("暫存已清空", Color.FromArgb(180, 220, 50, 50));
            MessageBox.Show($"清理完成，共刪除 {deletedCount} 個檔案。", "成功");
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
            MessageBox.Show("整理完成！", "提示");
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
            _btnRemoveRaw.BackColor = Color.FromArgb(217, 60, 60, 60);
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
            _btnRemoveRaw.BackColor = Color.FromArgb(217, 80, 80, 80);
            _splitLine.BackColor = Color.FromArgb(200, 200, 200);
        }
        Color nav = Color.FromArgb(102, 0, 0, 0);
        _btnPrev.BackColor = _btnNext.BackColor = _btnRotateLeft.BackColor = _btnRotateRight.BackColor = nav;
        _btnPrev.ForeColor = _btnNext.ForeColor = _btnRotateLeft.ForeColor = _btnRotateRight.ForeColor = Color.White;
    }

    private void UpdateExifInfo(Image img)
    {
        try
        {
            string model = GetExifString(img, 0x0110);
            string shutter = GetExifRational(img, 0x829A, true);
            string aperture = GetExifRational(img, 0x829D, false);
            string iso = GetExifShort(img, 0x8827);
            string focal = GetExifRational(img, 0x920A, false);

            _exifInfo = $"{model} | {shutter}s | f/{aperture} | ISO {iso} | {focal}mm";
        }
        catch { _exifInfo = "無 EXIF 資訊"; }
        _picDisplay.Invalidate();
    }

    private string GetExifString(Image img, int id)
    {
        if (Array.IndexOf(img.PropertyIdList, id) < 0) return "Unknown";
        var val = img.GetPropertyItem(id).Value;
        return System.Text.Encoding.ASCII.GetString(val).Trim('\0', ' ');
    }

    private string GetExifShort(Image img, int id)
    {
        if (Array.IndexOf(img.PropertyIdList, id) < 0) return "Unknown";
        var val = img.GetPropertyItem(id).Value;
        return BitConverter.ToUInt16(val, 0).ToString();
    }

    private string GetExifRational(Image img, int id, bool isShutter)
    {
        if (Array.IndexOf(img.PropertyIdList, id) < 0) return "Unknown";
        var val = img.GetPropertyItem(id).Value;
        uint num = BitConverter.ToUInt32(val, 0);
        uint den = BitConverter.ToUInt32(val, 4);
        if (den == 0) return "0";
        if (isShutter && num < den) return $"1/{den / num}";
        return (num / (double)den).ToString("F1").Replace(".0", "");
    }

    private void ApplyRoundedCorners()
    {
        void SetRegion(Control btn, bool circular = false) {
            if (btn.ClientRectangle.Width == 0) return;
            var path = new GraphicsPath();
            if (circular) {
                path.AddEllipse(btn.ClientRectangle);
            } else {
                var r = 12; var rect = btn.ClientRectangle;
                path.AddArc(rect.X, rect.Y, r, r, 180, 90); path.AddArc(rect.Width - r, rect.Y, r, r, 270, 90);
                path.AddArc(rect.Width - r, rect.Height - r, r, r, 0, 90); path.AddArc(rect.X, rect.Height - r, r, r, 90, 90);
                path.CloseFigure();
            }
            btn.Region = new Region(path);
        }

        var controls = new[] { (_btnOrganize, false), (_btnPurge, false), (_btnRemoveRaw, false), (_btnMaybe, true), (_btnTrash, true) };
        foreach (var (ctl, isCirc) in controls) {
            ctl.HandleCreated += (s, e) => SetRegion(ctl, isCirc);
            ctl.Resize += (s, e) => SetRegion(ctl, isCirc);
            if (ctl.IsHandleCreated) SetRegion(ctl, isCirc);
        }
    }

    private void SetupRemoveRaw()
    {
        _btnRemoveRaw.Click += async (s, e) => 
        {
            if (string.IsNullOrEmpty(_rootPath)) { MessageBox.Show("請先選擇資料夾 (點擊整理照片)。", "提示"); return; }
            string rawDirPath = Path.Combine(_rootPath, "raw");
            if (!Directory.Exists(rawDirPath)) { MessageBox.Show("找不到 raw 資料夾。", "提示"); return; }

            var result = MessageBox.Show("確定要移除所有在當前資料夾找不到對應編號的 RAW 檔案嗎？\n這將把檔案移至垃圾桶。", "確認移除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes) await ExecuteRemoveRawAsync(rawDirPath);
        };
    }

    private async Task ExecuteRemoveRawAsync(string rawDirPath)
    {
        try
        {
            _btnRemoveRaw.Enabled = false;
            int removedCount = 0;
            await Task.Run(() => 
            {
                // 取得當前資料夾所有 JPG 的編號 (不含副檔名)
                var currentNumbers = new HashSet<string>(
                    _jpgFiles.Select(f => Path.GetFileNameWithoutExtension(f)), 
                    StringComparer.OrdinalIgnoreCase
                );

                // 檢查 raw 資料夾中的每個檔案
                foreach (var rawFile in Directory.EnumerateFiles(rawDirPath, "*.*"))
                {
                    string rawName = Path.GetFileNameWithoutExtension(rawFile);
                    if (!currentNumbers.Contains(rawName))
                    {
                        // 若不包含該編號，移至垃圾桶
                        FileSystem.DeleteFile(rawFile, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        removedCount++;
                    }
                }
            });
            ShowNotification("RAW 檔案已清理", Color.FromArgb(180, 50, 50, 220));
            MessageBox.Show($"清理完成，共移除 {removedCount} 個孤立 RAW 檔案。", "成功");
        }
        catch (Exception ex) { MessageBox.Show($"移除失敗: {ex.Message}"); }
        finally { _btnRemoveRaw.Enabled = true; }
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
            if (_jpgFiles.Count > 0) { 
                _currentIndex = 0; 
                await LoadThumbnailsAsync();
                DisplayImage(_jpgFiles[_currentIndex]); 
            }
        }
        catch (Exception ex) { MessageBox.Show($"載入錯誤: {ex.Message}"); }
    }

    private void DisplayImage(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
        try {
            var old = _picDisplay.Image; _picDisplay.Image = null;
            
            // 檢查是否有預載入快取
            if (filePath == _prefetchedPath && _prefetchedImage != null) {
                _picDisplay.Image = _prefetchedImage;
                _prefetchedImage = null; _prefetchedPath = string.Empty;
            } else {
                // 如果沒有快取，清理舊快取並手動載入
                _prefetchedImage?.Dispose(); _prefetchedImage = null; _prefetchedPath = string.Empty;
                old?.Dispose();
                _picDisplay.Image = LoadImageInternal(filePath);
            }

            UpdateExifInfo(_picDisplay.Image); 
            UpdateFilmstripSelection(); 
            ResetZoom(); 
            _picDisplay.Invalidate();

            // 觸發預載入下一張
            PrefetchNextImage();
        } catch { }
    }

    private Image LoadImageInternal(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var img = Image.FromStream(stream);
        if (Array.IndexOf(img.PropertyIdList, 0x0112) > -1) {
            int orientation = img.GetPropertyItem(0x0112).Value[0];
            RotateFlipType rf = RotateFlipType.RotateNoneFlipNone;
            switch (orientation) { case 3: rf = RotateFlipType.Rotate180FlipNone; break; case 6: rf = RotateFlipType.Rotate90FlipNone; break; case 8: rf = RotateFlipType.Rotate270FlipNone; break; }
            if (rf != RotateFlipType.RotateNoneFlipNone) img.RotateFlip(rf);
        }
        if (_rotationStates.TryGetValue(filePath, out int rot)) for (int i = 0; i < rot; i++) img.RotateFlip(RotateFlipType.Rotate90FlipNone);
        return img;
    }

    private void PrefetchNextImage()
    {
        if (_jpgFiles.Count <= 1 || _currentIndex < 0) return;
        int nextIdx = (_currentIndex + 1) % _jpgFiles.Count;
        string nextPath = _jpgFiles[nextIdx];

        if (nextPath == _prefetchedPath) return;

        Task.Run(() => {
            try {
                var img = LoadImageInternal(nextPath);
                // 確保預載入期間沒有切換到其他圖片
                if (nextPath == _jpgFiles[(_currentIndex + 1) % _jpgFiles.Count]) {
                    _prefetchedImage?.Dispose();
                    _prefetchedImage = img;
                    _prefetchedPath = nextPath;
                } else {
                    img.Dispose();
                }
            } catch { }
        });
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
                
                // 繪製檔名
                string fn = Path.GetFileName(_jpgFiles[_currentIndex]); var szFn = g.MeasureString(fn, _fileNameFont);
                var rFn = new RectangleF(10, _picDisplay.Height - szFn.Height - 15, szFn.Width + 10, szFn.Height + 6);
                g.FillRectangle(_fileNameBgBrush, rFn); g.DrawString(fn, _fileNameFont, Brushes.White, rFn.X + 5, rFn.Y + 3);

                // 繪製 EXIF (位於檔名上方)
                if (!string.IsNullOrEmpty(_exifInfo)) {
                    var szExif = g.MeasureString(_exifInfo, _fileNameFont);
                    var rExif = new RectangleF(10, rFn.Top - szExif.Height - 5, szExif.Width + 10, szExif.Height + 6);
                    g.FillRectangle(_fileNameBgBrush, rExif); g.DrawString(_exifInfo, _fileNameFont, Brushes.LightGray, rExif.X + 5, rExif.Y + 3);
                }
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
            else if (e.KeyCode == Keys.Delete) DeleteToRecycleBin();
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

    private void MoveToStaging() {
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
            _undoStack.Push(item); _jpgFiles.RemoveAt(_currentIndex); ShowNotification("移至暫存區", Color.FromArgb(180, 255, 215, 0));
            NextImage();
        } catch (Exception ex) { MessageBox.Show($"移動失敗: {ex.Message}"); }
    }

    private void DeleteToRecycleBin() {
        if (string.IsNullOrEmpty(_rootPath) || _currentIndex < 0 || _currentIndex >= _jpgFiles.Count) return;
        string jpg = _jpgFiles[_currentIndex]; string fn = Path.GetFileNameWithoutExtension(jpg);
        string rawP = Path.Combine(_rootPath, "raw");
        try {
            var old = _picDisplay.Image; _picDisplay.Image = null; old?.Dispose();
            FileSystem.DeleteFile(jpg, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            if (Directory.Exists(rawP)) {
                foreach (var rf in Directory.EnumerateFiles(rawP, fn + ".*")) {
                    FileSystem.DeleteFile(rf, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
            }
            _jpgFiles.RemoveAt(_currentIndex); ShowNotification("已移至垃圾桶", Color.FromArgb(180, 220, 50, 50));
            NextImage();
        } catch (Exception ex) { MessageBox.Show($"刪除失敗: {ex.Message}"); }
    }

    private void NextImage() {
        if (_jpgFiles.Count == 0) { _currentIndex = -1; _picDisplay.Invalidate(); }
        else { if (_currentIndex >= _jpgFiles.Count) _currentIndex = _jpgFiles.Count - 1; DisplayImage(_jpgFiles[_currentIndex]); }
    }

    private void btnThemeToggle_Click(object sender, EventArgs e) { _isDarkMode = !_isDarkMode; UpdateThemeColors(); SaveThemeSettings(); }
    private void LoadThemeSettings() { try { if (File.Exists(ThemeSettingsFile)) _isDarkMode = File.ReadAllText(ThemeSettingsFile).Trim() == "1"; } catch { } }
    private void SaveThemeSettings() { try { File.WriteAllText(ThemeSettingsFile, _isDarkMode ? "1" : "0"); } catch { } }

    /// <summary>
    /// 非同步載入縮圖（目前為存根，可根據需求實作縮圖快取邏輯）。
    /// </summary>
    private async Task LoadThumbnailsAsync()
    {
        // 實作縮圖載入邏輯
        await Task.Yield();
    }

    /// <summary>
    /// 更新底部的膠捲（Filmstrip）選取狀態。
    /// </summary>
    private void UpdateFilmstripSelection()
    {
        // 實作膠捲選取邏輯
    }
}
