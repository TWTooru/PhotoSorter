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

    public Form1()
    {
        InitializeComponent();
        ApplyRoundedCorners();
        UpdateThemeColors(); // 初始化主題
    }

    /// <summary>
    /// 切換暗黑/明亮模式。
    /// </summary>
    private void btnThemeToggle_Click(object sender, EventArgs e)
    {
        _isDarkMode = !_isDarkMode;
        UpdateThemeColors();
    }

    /// <summary>
    /// 根據目前模式更新所有控制項的顏色。
    /// </summary>
    private void UpdateThemeColors()
    {
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

            _btnPrev.BackColor = Color.FromArgb(217, 60, 60, 60);
            _btnPrev.ForeColor = Color.White;
            _btnNext.BackColor = Color.FromArgb(217, 60, 60, 60);
            _btnNext.ForeColor = Color.White;
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

            _btnPrev.BackColor = Color.FromArgb(217, 255, 255, 255);
            _btnPrev.ForeColor = Color.Black;
            _btnNext.BackColor = Color.FromArgb(217, 255, 255, 255);
            _btnNext.ForeColor = Color.Black;
        }
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
    /// 安全地在 PictureBox 中顯示圖片，避免檔案鎖定。
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

            // 使用 FileStream 讀取，避免鎖定檔案造成後續移動/刪除失敗
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            _picDisplay.Image = Image.FromStream(stream);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"圖片顯示失敗: {ex.Message}");
        }
    }
}
