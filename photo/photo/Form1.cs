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

    public Form1()
    {
        InitializeComponent();
        LoadThemeSettings();
        ApplyRoundedCorners();
        UpdateThemeColors(); // 初始化主題
        SetupNavigation();
        SetupRotation();
        SetupZoomAndPan();
        SetupTransfer();
        CenterButtons();
        
        _picDisplay.Resize += (s, e) => CenterButtons();
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
                // 取得 JPG 資料夾中所有的檔案名稱 (不含副檔名)
                var jpgNames = new HashSet<string>(
                    Directory.EnumerateFiles(_jpgPath, "*.*", SearchOption.TopDirectoryOnly)
                             .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                                         f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                             .Select(Path.GetFileNameWithoutExtension),
                    StringComparer.OrdinalIgnoreCase
                );

                // 取得 RAW 資料夾中所有的檔案
                var rawFiles = Directory.EnumerateFiles(_rawPath, "*.*", SearchOption.TopDirectoryOnly).ToList();

                foreach (var rawFilePath in rawFiles)
                {
                    string rawName = Path.GetFileNameWithoutExtension(rawFilePath);
                    
                    // 如果 JPG 中沒有對應的名稱，則移動 RAW 檔
                    if (!jpgNames.Contains(rawName))
                    {
                        if (!Directory.Exists(tempPath))
                        {
                            Directory.CreateDirectory(tempPath);
                        }

                        string destPath = Path.Combine(tempPath, Path.GetFileName(rawFilePath));
                        
                        // 若目標已存在同名檔案則先刪除，避免 Move 失敗
                        if (File.Exists(destPath)) File.Delete(destPath);
                        
                        File.Move(rawFilePath, destPath);
                        movedCount++;
                    }
                }
            });

            _pbStatus.Style = ProgressBarStyle.Blocks;
            _pbStatus.Value = 100;
            MessageBox.Show($"轉移完成！\n共移除了 {movedCount} 個RAW檔案至 暫存區。", 
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
        catch { /* 忽略讀取錯誤，使用預設值 */ }
    }

    private void SaveThemeSettings()
    {
        try
        {
            File.WriteAllText(ThemeSettingsFile, _isDarkMode ? "1" : "0");
        }
        catch { /* 忽略儲存錯誤 */ }
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

            // 以滑鼠位置為基準調整偏移量 (考慮置中繪製的特性)
            _imageOffset.X = (e.X - _picDisplay.Width / 2f) * (1 - ratio) + _imageOffset.X * ratio;
            _imageOffset.Y = (e.Y - _picDisplay.Height / 2f) * (1 - ratio) + _imageOffset.Y * ratio;

            _picDisplay.Invalidate();
        };

        _picDisplay.MouseDoubleClick += (s, e) => 
        {
            if (_picDisplay.Image == null) return;
            
            float oldZoom = _zoomFactor;
            float newZoom;

            // 判斷是否已經是 100% (容許一點誤差)
            if (Math.Abs(_zoomFactor - 1.0f) < 0.05f)
            {
                ResetZoom();
                newZoom = _zoomFactor;
            }
            else
            {
                newZoom = 1.0f;
                float ratio = newZoom / oldZoom;
                _imageOffset.X = (e.X - _picDisplay.Width / 2f) * (1 - ratio) + _imageOffset.X * ratio;
                _imageOffset.Y = (e.Y - _picDisplay.Height / 2f) * (1 - ratio) + _imageOffset.Y * ratio;
                _zoomFactor = newZoom;
            }
            _picDisplay.Invalidate();
        };

        _picDisplay.MouseDown += (s, e) => 
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _lastMousePos = e.Location;
            }
        };

        _picDisplay.MouseMove += (s, e) => 
        {
            if (_isDragging)
            {
                _imageOffset.X += (e.X - _lastMousePos.X);
                _imageOffset.Y += (e.Y - _lastMousePos.Y);
                _lastMousePos = e.Location;
                _picDisplay.Invalidate();
            }
        };

        _picDisplay.MouseUp += (s, e) => _isDragging = false;

        _picDisplay.Paint += (s, e) => 
        {
            if (_picDisplay.Image == null) return;

            // 先用背景色刷掉原本 PictureBox 可能自動繪製的內容
            e.Graphics.Clear(_picDisplay.BackColor);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            
            float imgW = _picDisplay.Image.Width * _zoomFactor;
            float imgH = _picDisplay.Image.Height * _zoomFactor;
            
            float x = (_picDisplay.Width - imgW) / 2 + _imageOffset.X;
            float y = (_picDisplay.Height - imgH) / 2 + _imageOffset.Y;

            e.Graphics.DrawImage(_picDisplay.Image, x, y, imgW, imgH);
        };
    }

    private void ResetZoom()
    {
        if (_picDisplay.Image == null) return;

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
        _btnRotateLeft.Click += (s, e) => RotateLeft();
        _btnRotateRight.Click += (s, e) => RotateRight();

        this.KeyDown += (s, e) => 
        {
            if (e.KeyCode == Keys.Q) ShowPreviousImage();
            else if (e.KeyCode == Keys.E) ShowNextImage();
            else if (e.KeyCode == Keys.R) RotateRight();
            else if (e.KeyCode == Keys.Delete) DeleteCurrentImage();
        };
    }

    private void DeleteCurrentImage()
    {
        if (_currentIndex < 0 || _currentIndex >= _jpgFiles.Count) return;

        string currentJpg = _jpgFiles[_currentIndex];
        string fileName = Path.GetFileNameWithoutExtension(currentJpg);
        string tempPath = Path.Combine(_jpgPath, "暫存區");

        try
        {
            // 1. 釋放目前圖片資源，避免檔案鎖定
            var oldImage = _picDisplay.Image;
            _picDisplay.Image = null;
            oldImage?.Dispose();

            if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);

            // 2. 移動 JPG 檔案
            string destJpg = Path.Combine(tempPath, Path.GetFileName(currentJpg));
            if (File.Exists(destJpg)) File.Delete(destJpg);
            File.Move(currentJpg, destJpg);

            // 3. 尋找並移動對應的 RAW 檔案 (若有設定 RAW 路徑)
            if (!string.IsNullOrEmpty(_rawPath) && Directory.Exists(_rawPath))
            {
                var rawFiles = Directory.EnumerateFiles(_rawPath, fileName + ".*")
                                        .Where(f => !f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) && 
                                                    !f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));
                foreach (var rawFile in rawFiles)
                {
                    string destRaw = Path.Combine(tempPath, Path.GetFileName(rawFile));
                    if (File.Exists(destRaw)) File.Delete(destRaw);
                    File.Move(rawFile, destRaw);
                }
            }

            // 4. 更新清單並顯示下一張
            _jpgFiles.RemoveAt(_currentIndex);
            
            if (_jpgFiles.Count == 0)
            {
                _currentIndex = -1;
                _picDisplay.Invalidate();
                MessageBox.Show("所有照片已處理完畢。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // 如果刪除的是最後一張，索引往前移
                if (_currentIndex >= _jpgFiles.Count) _currentIndex = _jpgFiles.Count - 1;
                DisplayImage(_jpgFiles[_currentIndex]);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"移除檔案時發生錯誤: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            // 發生錯誤時嘗試重新顯示
            if (_currentIndex < _jpgFiles.Count) DisplayImage(_jpgFiles[_currentIndex]);
        }
    }

    private void RotateLeft()
    {
        if (_picDisplay.Image == null) return;
        _picDisplay.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
        _picDisplay.Invalidate();
    }

    private void RotateRight()
    {
        if (_picDisplay.Image == null) return;
        _picDisplay.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
        _picDisplay.Invalidate();
    }

    private void CenterButtons()
    {
        _btnPrev.Top = (_picDisplay.Height - _btnPrev.Height) / 2;
        _btnNext.Top = (_picDisplay.Height - _btnNext.Height) / 2;
        
        // 如果視窗大小改變，也要重新計算適合大小
        ResetZoom();
        _picDisplay.Invalidate();
    }


    /// <summary>
    /// 切換暗黑/明亮模式。
    /// </summary>
    private void btnThemeToggle_Click(object sender, EventArgs e)
    {
        _isDarkMode = !_isDarkMode;
        UpdateThemeColors();
        SaveThemeSettings();
    }

    /// <summary>
    /// 根據目前模式更新所有控制項的顏色。
    /// </summary>
    private void UpdateThemeColors()
    {
        Color navBtnColor = Color.FromArgb(102, 0, 0, 0); // 40% 透明度黑色
        
        if (_isDarkMode)
        {
            // 暗黑模式配色
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;

            _picDisplay.BackColor = Color.FromArgb(45, 45, 45);
            _btnThemeToggle.Text = "☀️";
            _btnThemeToggle.BackColor = Color.FromArgb(217, 60, 60, 60);
            _btnThemeToggle.ForeColor = Color.White;

            _btnJpgPath.BackColor = Color.FromArgb(45, 45, 45);
            _btnJpgPath.ForeColor = Color.White;
            _btnJpgPath.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);

            _btnRawPath.BackColor = Color.FromArgb(45, 45, 45);
            _btnRawPath.ForeColor = Color.White;
            _btnRawPath.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
        }
        else
        {
            // 明亮模式配色 (預設)
            BackColor = SystemColors.Control;
            ForeColor = SystemColors.ControlText;

            _picDisplay.BackColor = Color.FromArgb(240, 240, 240);
            _btnThemeToggle.Text = "🌙";
            _btnThemeToggle.BackColor = Color.FromArgb(217, 255, 255, 255);
            _btnThemeToggle.ForeColor = Color.Black;

            _btnJpgPath.BackColor = Color.White;
            _btnJpgPath.ForeColor = Color.Black;
            _btnJpgPath.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);

            _btnRawPath.BackColor = Color.White;
            _btnRawPath.ForeColor = Color.Black;
            _btnRawPath.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
        }

        // 導覽與功能按鈕始終保持半透明黑色風格
        _btnPrev.BackColor = navBtnColor;
        _btnPrev.ForeColor = Color.White;
        _btnNext.BackColor = navBtnColor;
        _btnNext.ForeColor = Color.White;
        _btnRotateLeft.BackColor = navBtnColor;
        _btnRotateLeft.ForeColor = Color.White;
        _btnRotateRight.BackColor = navBtnColor;
        _btnRotateRight.ForeColor = Color.White;
    }

    /// <summary>
    /// 為關鍵 UI 組件應用圓角效果。
    /// </summary>
    private void ApplyRoundedCorners()
    {
        _btnTransfer.Resize += (s, e) => 
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            var radius = 12;
            var rect = _btnTransfer.ClientRectangle;
            
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Width - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Width - radius, rect.Height - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();
            
            _btnTransfer.Region = new Region(path);
        };
    }

    /// <summary>
    /// 處理 JPG 資料夾選取並非同步載入檔案列表。
    /// </summary>
    private async void btnJpgPath_Click(object sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() != DialogResult.OK) return;

        _jpgPath = dialog.SelectedPath;
        _btnJpgPath.Text = _jpgPath;

        await LoadJpgFilesAsync(_jpgPath);
    }

    /// <summary>
    /// 處理 RAW 資料夾選取。
    /// </summary>
    private void btnRawPath_Click(object sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() != DialogResult.OK) return;

        _rawPath = dialog.SelectedPath;
        _btnRawPath.Text = _rawPath;
    }

    /// <summary>
    /// 非同步掃描資料夾內的 JPG 檔案。
    /// </summary>
    /// <param name="path">資料夾路徑。</param>
    private async Task LoadJpgFilesAsync(string path)
    {
        try
        {
            _pbStatus.Style = ProgressBarStyle.Marquee;
            
            _jpgFiles = await Task.Run(() => 
                Directory.EnumerateFiles(path, "*.jpg", SearchOption.TopDirectoryOnly)
                         .Union(Directory.EnumerateFiles(path, "*.jpeg", SearchOption.TopDirectoryOnly))
                         .OrderBy(f => f)
                         .ToList());

            _pbStatus.Style = ProgressBarStyle.Blocks;

            if (_jpgFiles.Count > 0)
            {
                _currentIndex = 0;
                DisplayImage(_jpgFiles[_currentIndex]);
            }
            else
            {
                MessageBox.Show("該資料夾內找不到 JPG 檔案。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            _pbStatus.Style = ProgressBarStyle.Blocks;
            MessageBox.Show($"載入檔案時發生錯誤: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 安全地在 PictureBox 中顯示圖片，避免檔案鎖定，並根據 EXIF 自動旋轉。
    /// </summary>
    /// <param name="filePath">圖片檔案路徑。</param>
    private void DisplayImage(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

        try
        {
            // 釋放前一張圖片的資源
            var oldImage = _picDisplay.Image;
            _picDisplay.Image = null;
            oldImage?.Dispose();

            // 使用 FileStream 讀取，避免鎖定檔案
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var img = Image.FromStream(stream);

            // 處理 EXIF 自動旋轉 (Property ID 0x0112 為 Orientation)
            if (Array.IndexOf(img.PropertyIdList, 0x0112) > -1)
            {
                var prop = img.GetPropertyItem(0x0112);
                int orientation = prop.Value[0];
                RotateFlipType rf = RotateFlipType.RotateNoneFlipNone;

                switch (orientation)
                {
                    case 3: rf = RotateFlipType.Rotate180FlipNone; break;
                    case 6: rf = RotateFlipType.Rotate90FlipNone; break;
                    case 8: rf = RotateFlipType.Rotate270FlipNone; break;
                }

                if (rf != RotateFlipType.RotateNoneFlipNone)
                {
                    img.RotateFlip(rf);
                }
            }

            _picDisplay.Image = img;
            ResetZoom();
            _picDisplay.Invalidate();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"圖片顯示失敗: {ex.Message}");
        }
    }
}
