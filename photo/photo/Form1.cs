using System.Diagnostics;
using System.Drawing.Drawing2D;
using photo.Core.Interfaces;
using photo.UI;

namespace photo;

public partial class Form1 : Form, IMainView
{
    public event EventHandler? NextRequested;
    public event EventHandler? PreviousRequested;
    public event EventHandler? DeleteRequested;
    public event EventHandler? UndoRequested;
    public event EventHandler<string>? FolderSelected;
    
    public event EventHandler? PurgeRequested;
    public event EventHandler? RemoveRawRequested;
    public event EventHandler<bool>? RotateRequested;
    public event EventHandler? MoveToStagingRequested;

    private bool _isDarkMode = false;
    private static readonly string ThemeSettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhotoSorter");
    private static readonly string ThemeSettingsFile = Path.Combine(ThemeSettingsDir, "theme.cfg");
    
    // Zoom/Pan
    private Matrix _transformMatrix = new Matrix();
    private Matrix _dragStartMatrix = new Matrix();
    private Point _dragStartMousePos;
    private bool _isDragging = false;

    // View State
    private bool _isInteracting = false;
    private System.Windows.Forms.Timer _qualityTimer = new();
    private readonly Font _fileNameFont = new Font("Consolas", 10F, FontStyle.Bold);
    private readonly Font _notificationFont = new Font("Segoe UI", 12F, FontStyle.Bold);
    private readonly SolidBrush _fileNameBgBrush = new SolidBrush(ThemePalette.FileNameBackground);

    // Notification
    private string _notificationText = string.Empty;
    private Color _notificationColor = Color.Transparent;
    private System.Windows.Forms.Timer _notificationTimer = new();

    // Data from Presenter
    private string _exifInfo = string.Empty;
    private string _currentFilePath = string.Empty;

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
        SetupToolTipsAndDragDrop();
        
        this.FormClosed += (s, e) =>
        {
            _transformMatrix?.Dispose();
            _dragStartMatrix?.Dispose();
            _fileNameFont?.Dispose();
            _notificationFont?.Dispose();
            _fileNameBgBrush?.Dispose();
        };
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
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var font = new Font("Segoe UI Emoji", 22F);
                var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
                var rect = btn.ClientRectangle;
                rect.Offset(0, yOffset); 
                TextRenderer.DrawText(g, icon, font, rect, Color.Black, flags);
            };
        }

        StyleButton(_btnMaybe, ThemePalette.StarYellow, ThemePalette.StarYellowHover, "⭐", 1);
        StyleButton(_btnTrash, ThemePalette.TrashRed, ThemePalette.TrashRedHover, "🗑️", 4);
        
        _btnMaybe.Click += (s, e) => MoveToStagingRequested?.Invoke(this, EventArgs.Empty);
        _btnTrash.Click += (s, e) => DeleteRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SetupPurge()
    {
        _btnPurge.Click += (s, e) => 
        {
            var result = MessageBox.Show("確定要清空所有暫存檔案嗎？\n這將永久刪除相關暫存資料夾內容且無法復原。", "確認清空", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes) PurgeRequested?.Invoke(this, EventArgs.Empty);
        };
    }

    private void SetupOrganize()
    {
        _btnOrganize.Click += (s, e) => 
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK) FolderSelected?.Invoke(this, dialog.SelectedPath);
        };
    }

    private void UpdateThemeColors()
    {
        if (_isDarkMode)
        {
            BackColor = ThemePalette.DarkBackground; ForeColor = ThemePalette.DarkText;
            _picDisplay.BackColor = ThemePalette.DarkSurface;
            _btnThemeToggle.Text = "☀️";
            _btnThemeToggle.BackColor = ThemePalette.DarkButton;
            _btnOrganize.BackColor = ThemePalette.DarkSurface;
            _btnPurge.BackColor = Color.FromArgb(217, 80, 40, 40);
            _btnRemoveRaw.BackColor = ThemePalette.DarkButton;
            _splitLine.BackColor = ThemePalette.DarkSplitLine;
        }
        else
        {
            BackColor = ThemePalette.LightBackground; ForeColor = ThemePalette.LightText;
            _picDisplay.BackColor = ThemePalette.LightSurface;
            _btnThemeToggle.Text = "🌙";
            _btnThemeToggle.BackColor = Color.FromArgb(217, 255, 255, 255);
            _btnOrganize.BackColor = ThemePalette.OrganizeGreen;
            _btnPurge.BackColor = ThemePalette.PurgeRed;
            _btnRemoveRaw.BackColor = ThemePalette.RemoveRawGray;
            _splitLine.BackColor = ThemePalette.LightSplitLine;
        }
        _btnPrev.BackColor = _btnNext.BackColor = _btnRotateLeft.BackColor = _btnRotateRight.BackColor = ThemePalette.NavOverlay;
        _btnPrev.ForeColor = _btnNext.ForeColor = _btnRotateLeft.ForeColor = _btnRotateRight.ForeColor = Color.White;
    }

    private void ApplyRoundedCorners()
    {
        void SetRegion(Control btn, bool circular = false) {
            if (btn.ClientRectangle.Width == 0) return;
            using var path = new GraphicsPath();
            if (circular) {
                path.AddEllipse(btn.ClientRectangle);
            } else {
                var r = 12; var rect = btn.ClientRectangle;
                path.AddArc(rect.X, rect.Y, r, r, 180, 90); path.AddArc(rect.Width - r, rect.Y, r, r, 270, 90);
                path.AddArc(rect.Width - r, rect.Height - r, r, r, 0, 90); path.AddArc(rect.X, rect.Height - r, r, r, 90, 90);
                path.CloseFigure();
            }
            var oldRegion = btn.Region;
            btn.Region = new Region(path);
            oldRegion?.Dispose();
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
        _btnRemoveRaw.Click += (s, e) => 
        {
            var result = MessageBox.Show("確定要移除所有在當前資料夾找不到對應編號的 RAW 檔案嗎？\n這將把檔案移至垃圾桶。", "確認移除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes) RemoveRawRequested?.Invoke(this, EventArgs.Empty);
        };
    }

    public void ShowNotification(string message, Color backColor)
    {
        if (InvokeRequired) { Invoke(new Action(() => ShowNotification(message, backColor))); return; }
        _notificationText = message; _notificationColor = backColor;
        _notificationTimer.Stop(); _notificationTimer.Start(); _picDisplay.Invalidate();
    }
    
    public void ShowMessage(string message)
    {
        if (InvokeRequired) { Invoke(new Action(() => ShowMessage(message))); return; }
        MessageBox.Show(message, "提示");
    }

    public void UpdateStatus(int currentIndex, int totalPhotos, string currentFileName)
    {
        if (InvokeRequired) { Invoke(new Action(() => UpdateStatus(currentIndex, totalPhotos, currentFileName))); return; }
        this.Text = totalPhotos > 0
            ? $"快速挑選照片 — {currentIndex} / {totalPhotos}"
            : "快速挑選照片";
    }

    public void UpdateExifData(string exifInfo)
    {
        if (InvokeRequired) { Invoke(new Action(() => UpdateExifData(exifInfo))); return; }
        _exifInfo = exifInfo;
        _picDisplay.Invalidate();
    }

    public void DisplayImage(Image? image, string fileName)
    {
        if (InvokeRequired) { Invoke(new Action(() => DisplayImage(image, fileName))); return; }
        
        var old = _picDisplay.Image; 
        _picDisplay.Image = null;
        old?.Dispose();

        _currentFilePath = fileName;

        if (image != null)
        {
            _picDisplay.Image = image;
            ResetZoom();
        }

        _picDisplay.Invalidate();
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
        _picDisplay.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { _isDragging = true; _dragStartMousePos = e.Location; _dragStartMatrix?.Dispose(); _dragStartMatrix = _transformMatrix.Clone(); } };
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
                
                string fn = Path.GetFileName(_currentFilePath); 
                if (!string.IsNullOrEmpty(fn)) {
                    var szFn = g.MeasureString(fn, _fileNameFont);
                    var rFn = new RectangleF(10, _picDisplay.Height - szFn.Height - 15, szFn.Width + 10, szFn.Height + 6);
                    g.FillRectangle(_fileNameBgBrush, rFn); g.DrawString(fn, _fileNameFont, Brushes.White, rFn.X + 5, rFn.Y + 3);

                    if (!string.IsNullOrEmpty(_exifInfo)) {
                        var szExif = g.MeasureString(_exifInfo, _fileNameFont);
                        var rExif = new RectangleF(10, rFn.Top - szExif.Height - 5, szExif.Width + 10, szExif.Height + 6);
                        g.FillRectangle(_fileNameBgBrush, rExif); g.DrawString(_exifInfo, _fileNameFont, Brushes.LightGray, rExif.X + 5, rExif.Y + 3);
                    }
                }
            }
            else
            {
                string hint = "點擊右下角「📦 整理照片」或直接將資料夾拖曳至此處";
                var szHint = g.MeasureString(hint, _notificationFont);
                var hintRect = new RectangleF(
                    (_picDisplay.Width - szHint.Width) / 2,
                    (_picDisplay.Height - szHint.Height) / 2,
                    szHint.Width, szHint.Height);
                using var hintBrush = new SolidBrush(_isDarkMode ? ThemePalette.DarkMutedText : ThemePalette.LightMutedText);
                g.DrawString(hint, _notificationFont, hintBrush, hintRect);
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
        _btnPrev.Click += (s, e) => PreviousRequested?.Invoke(this, EventArgs.Empty);
        _btnNext.Click += (s, e) => NextRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SetupRotation() {
        _btnRotateLeft.Click += (s, e) => RotateRequested?.Invoke(this, false); 
        _btnRotateRight.Click += (s, e) => RotateRequested?.Invoke(this, true);
        this.KeyDown += (s, e) => {
            if (e.Control && e.KeyCode == Keys.Z) UndoRequested?.Invoke(this, EventArgs.Empty);
            else if (e.KeyCode == Keys.Delete) DeleteRequested?.Invoke(this, EventArgs.Empty);
            else if (e.KeyCode == Keys.Q || e.KeyCode == Keys.Left) PreviousRequested?.Invoke(this, EventArgs.Empty);
            else if (e.KeyCode == Keys.E || e.KeyCode == Keys.Right) NextRequested?.Invoke(this, EventArgs.Empty);
        };
    }

    private void SetupToolTipsAndDragDrop()
    {
        void HandleDragEnter(object? s, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
                if (files.Length > 0 && Directory.Exists(files[0]))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }

        void HandleDragDrop(object? s, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
                if (files.Length > 0 && Directory.Exists(files[0]))
                {
                    FolderSelected?.Invoke(this, files[0]);
                }
            }
        }

        this.AllowDrop = true;
        this.DragEnter += HandleDragEnter;
        this.DragDrop += HandleDragDrop;

        _picDisplay.AllowDrop = true;
        _picDisplay.DragEnter += HandleDragEnter;
        _picDisplay.DragDrop += HandleDragDrop;

        var toolTip = new ToolTip();
        toolTip.SetToolTip(_btnPrev, "上一張 (Q / ←)");
        toolTip.SetToolTip(_btnNext, "下一張 (E / →)");
        toolTip.SetToolTip(_btnRotateLeft, "向左旋轉 90°");
        toolTip.SetToolTip(_btnRotateRight, "向右旋轉 90°");
        toolTip.SetToolTip(_btnMaybe, "暫存精選 (⭐)");
        toolTip.SetToolTip(_btnTrash, "移至垃圾桶 (Delete / 🗑️)");
        toolTip.SetToolTip(_btnThemeToggle, "切換深色 / 淺色主題");
        toolTip.SetToolTip(_btnOrganize, "選擇照片資料夾 (支援拖曳資料夾至視窗)");
        toolTip.SetToolTip(_btnPurge, "清空所有暫存資料夾");
        toolTip.SetToolTip(_btnRemoveRaw, "移除無對應 JPG 的孤立 RAW 檔");

        this.FormClosed += (s, e) => toolTip.Dispose();
    }

    private void btnThemeToggle_Click(object sender, EventArgs e) { _isDarkMode = !_isDarkMode; UpdateThemeColors(); SaveThemeSettings(); }
    private void LoadThemeSettings() { try { if (File.Exists(ThemeSettingsFile)) _isDarkMode = File.ReadAllText(ThemeSettingsFile).Trim() == "1"; } catch { } }
    private void SaveThemeSettings() { try { Directory.CreateDirectory(ThemeSettingsDir); File.WriteAllText(ThemeSettingsFile, _isDarkMode ? "1" : "0"); } catch { } }
}
