namespace photo;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        _picDisplay = new PictureBox();
        _btnPrev = new Button();
        _btnNext = new Button();
        _pbStatus = new ProgressBar();
        _btnJpgPath = new Button();
        _btnRawPath = new Button();
        _btnTransfer = new Button();
        _lblJpg = new Label();
        _lblRaw = new Label();
        _btnThemeToggle = new Button();
        ((System.ComponentModel.ISupportInitialize)_picDisplay).BeginInit();
        SuspendLayout();
        // 
        // _picDisplay
        // 
        _picDisplay.BackColor = Color.FromArgb(240, 240, 240);
        _picDisplay.Location = new Point(16, 16);
        _picDisplay.Name = "_picDisplay";
        _picDisplay.Size = new Size(1552, 540);
        _picDisplay.SizeMode = PictureBoxSizeMode.Zoom;
        _picDisplay.TabIndex = 0;
        _picDisplay.TabStop = false;
        _picDisplay.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        // 
        // _btnPrev
        // 
        _btnPrev.BackColor = Color.FromArgb(217, 255, 255, 255);
        _btnPrev.FlatAppearance.BorderSize = 0;
        _btnPrev.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 255, 255, 255);
        _btnPrev.FlatStyle = FlatStyle.Flat;
        _btnPrev.Location = new Point(0, 206);
        _btnPrev.Name = "_btnPrev";
        _btnPrev.Size = new Size(64, 128);
        _btnPrev.TabIndex = 1;
        _btnPrev.Text = "<";
        _btnPrev.UseVisualStyleBackColor = false;
        // 
        // _btnNext
        // 
        _btnNext.BackColor = Color.FromArgb(217, 255, 255, 255);
        _btnNext.FlatAppearance.BorderSize = 0;
        _btnNext.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 255, 255, 255);
        _btnNext.FlatStyle = FlatStyle.Flat;
        _btnNext.Location = new Point(1488, 206);
        _btnNext.Name = "_btnNext";
        _btnNext.Size = new Size(64, 128);
        _btnNext.TabIndex = 2;
        _btnNext.Text = ">";
        _btnNext.UseVisualStyleBackColor = false;
        // 
        // _pbStatus
        // 
        _pbStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _pbStatus.Location = new Point(16, 572);
        _pbStatus.Name = "_pbStatus";
        _pbStatus.Size = new Size(1552, 24);
        _pbStatus.TabIndex = 3;
        // 
        // _lblJpg
        // 
        _lblJpg.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _lblJpg.Location = new Point(16, 612);
        _lblJpg.Name = "_lblJpg";
        _lblJpg.Size = new Size(100, 40);
        _lblJpg.TabIndex = 4;
        _lblJpg.Text = "JPG 檔案位置";
        _lblJpg.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _btnJpgPath
        // 
        _btnJpgPath.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _btnJpgPath.BackColor = Color.White;
        _btnJpgPath.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
        _btnJpgPath.FlatStyle = FlatStyle.Flat;
        _btnJpgPath.Location = new Point(122, 612);
        _btnJpgPath.Name = "_btnJpgPath";
        _btnJpgPath.Size = new Size(1230, 40);
        _btnJpgPath.TabIndex = 5;
        _btnJpgPath.Text = " 📂 請點擊選擇 JPG 資料夾...";
        _btnJpgPath.TextAlign = ContentAlignment.MiddleLeft;
        _btnJpgPath.UseVisualStyleBackColor = false;
        _btnJpgPath.Click += btnJpgPath_Click;
        // 
        // _lblRaw
        // 
        _lblRaw.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _lblRaw.Location = new Point(16, 660);
        _lblRaw.Name = "_lblRaw";
        _lblRaw.Size = new Size(100, 40);
        _lblRaw.TabIndex = 6;
        _lblRaw.Text = "RAW 檔案位置";
        _lblRaw.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _btnRawPath
        // 
        _btnRawPath.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _btnRawPath.BackColor = Color.White;
        _btnRawPath.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
        _btnRawPath.FlatStyle = FlatStyle.Flat;
        _btnRawPath.Location = new Point(122, 660);
        _btnRawPath.Name = "_btnRawPath";
        _btnRawPath.Size = new Size(1230, 40);
        _btnRawPath.TabIndex = 7;
        _btnRawPath.Text = " 📁 請點擊選擇 RAW 資料夾...";
        _btnRawPath.TextAlign = ContentAlignment.MiddleLeft;
        _btnRawPath.UseVisualStyleBackColor = false;
        _btnRawPath.Click += btnRawPath_Click;
        // 
        // _btnTransfer
        // 
        _btnTransfer.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnTransfer.BackColor = Color.FromArgb(217, 0, 90, 158);
        _btnTransfer.FlatAppearance.BorderSize = 0;
        _btnTransfer.FlatAppearance.MouseDownBackColor = Color.FromArgb(255, 0, 69, 120);
        _btnTransfer.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 0, 120, 212);
        _btnTransfer.FlatStyle = FlatStyle.Flat;
        _btnTransfer.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        _btnTransfer.ForeColor = Color.White;
        _btnTransfer.Location = new Point(1368, 612);
        _btnTransfer.Name = "_btnTransfer";
        _btnTransfer.Size = new Size(200, 88);
        _btnTransfer.TabIndex = 8;
        _btnTransfer.Text = "➔ 執行轉移";
        _btnTransfer.UseVisualStyleBackColor = false;
        // 
        // _btnThemeToggle
        // 
        _btnThemeToggle.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnThemeToggle.BackColor = Color.FromArgb(217, 255, 255, 255);
        _btnThemeToggle.FlatAppearance.BorderSize = 0;
        _btnThemeToggle.FlatStyle = FlatStyle.Flat;
        _btnThemeToggle.Font = new Font("Segoe UI", 12F);
        _btnThemeToggle.Location = new Point(1520, 710);
        _btnThemeToggle.Name = "_btnThemeToggle";
        _btnThemeToggle.Size = new Size(48, 40);
        _btnThemeToggle.TabIndex = 9;
        _btnThemeToggle.Text = "🌙";
        _btnThemeToggle.UseVisualStyleBackColor = false;
        _btnThemeToggle.Click += btnThemeToggle_Click;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(9F, 19F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1584, 762);
        Controls.Add(_btnThemeToggle);
        Controls.Add(_btnTransfer);
        Controls.Add(_btnRawPath);
        Controls.Add(_lblRaw);
        Controls.Add(_btnJpgPath);
        Controls.Add(_lblJpg);
        Controls.Add(_pbStatus);
        Controls.Add(_picDisplay);
        _picDisplay.Controls.Add(_btnPrev);
        _picDisplay.Controls.Add(_btnNext);
        Name = "Form1";
        Text = "快速挑選照片";
        ((System.ComponentModel.ISupportInitialize)_picDisplay).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private PictureBox _picDisplay;
    private Button _btnPrev;
    private Button _btnNext;
    private ProgressBar _pbStatus;
    private Label _lblJpg;
    private Button _btnJpgPath;
    private Label _lblRaw;
    private Button _btnRawPath;
    private Button _btnTransfer;
    private Button _btnThemeToggle;
}
