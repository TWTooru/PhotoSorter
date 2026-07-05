using System.Diagnostics;
using System.Drawing.Drawing2D;
using photo.Core.Interfaces;

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
    private const string ThemeSettingsFile = "theme.cfg";
    
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
    private readonly SolidBrush _fileNameBgBrush = new SolidBrush(Color.FromArgb(150, 0, 0, 0));

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
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var font = new Font("Segoe UI Emoji", 22F);
                var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
                var rect = btn.ClientRectangle;
                rect.Offset(0, yOffset); 
                TextRenderer.DrawText(g, icon, font, rect, Color.Black, flags);
            };
        }

        StyleButton(_btnMaybe, Color.FromArgb(255, 202, 40), Color.FromArgb(255, 179, 0), "⭐", 1);
        StyleButton(_btnTrash, Color.FromArgb(239, 83, 80), Color.FromArgb(211, 47, 47), "🗑️", 4);
        
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
        // Add if needed, currently not used in original Form1 directly to a status bar.
    }

    public void UpdateExifData(string exifInfo)
    {
        if (InvokeRequired) { Invoke(new Action(() => UpdateExifData(exifInfo))); return; }
        _exifInfo = exifInfo;
        _picDisplay.Invalidate();
    }

    public void DisplayImage(string imagePath, int rotation)
    {
        if (InvokeRequired) { Invoke(new Action(() => DisplayImage(imagePath, rotation))); return; }
        
        var old = _picDisplay.Image; 
        _picDisplay.Image = null;
        old?.Dispose();

        _currentFilePath = imagePath;

        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)) 
        {
            _picDisplay.Invalidate();
            return;
        }

        try {
            using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
            var img = Image.FromStream(stream);
            if (Array.IndexOf(img.PropertyIdList, 0x0112) > -1) {
                int orientation = img.GetPropertyItem(0x0112)!.Value![0];
                RotateFlipType rf = RotateFlipType.RotateNoneFlipNone;
                switch (orientation) { case 3: rf = RotateFlipType.Rotate180FlipNone; break; case 6: rf = RotateFlipType.Rotate90FlipNone; break; case 8: rf = RotateFlipType.Rotate270FlipNone; break; }
                if (rf != RotateFlipType.RotateNoneFlipNone) img.RotateFlip(rf);
            }
            
            for (int i = 0; i < rotation; i++) img.RotateFlip(RotateFlipType.Rotate90FlipNone);
            
            _picDisplay.Image = img;
            ResetZoom(); 
            _picDisplay.Invalidate();
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
            else if (e.KeyCode == Keys.Q) PreviousRequested?.Invoke(this, EventArgs.Empty);
            else if (e.KeyCode == Keys.E) NextRequested?.Invoke(this, EventArgs.Empty);
        };
    }

    private void btnThemeToggle_Click(object sender, EventArgs e) { _isDarkMode = !_isDarkMode; UpdateThemeColors(); SaveThemeSettings(); }
    private void LoadThemeSettings() { try { if (File.Exists(ThemeSettingsFile)) _isDarkMode = File.ReadAllText(ThemeSettingsFile).Trim() == "1"; } catch { } }
    private void SaveThemeSettings() { try { File.WriteAllText(ThemeSettingsFile, _isDarkMode ? "1" : "0"); } catch { } }
}
