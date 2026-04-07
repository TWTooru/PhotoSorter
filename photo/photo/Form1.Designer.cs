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
        _btnRotateLeft = new Button();
        _btnRotateRight = new Button();
        _pbStatus = new ProgressBar();
        _btnOrganize = new Button();
        _btnTransfer = new Button();
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
        _picDisplay.SizeMode = PictureBoxSizeMode.Normal;
        _picDisplay.TabIndex = 0;
        _picDisplay.TabStop = false;
        _picDisplay.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        // 
        // _btnPrev
        // 
        _btnPrev.BackColor = Color.FromArgb(102, 0, 0, 0);
        _btnPrev.FlatAppearance.BorderSize = 0;
        _btnPrev.FlatAppearance.MouseOverBackColor = Color.FromArgb(150, 0, 0, 0);
        _btnPrev.FlatStyle = FlatStyle.Flat;
        _btnPrev.ForeColor = Color.White;
        _btnPrev.Location = new Point(0, 206);
        _btnPrev.Name = "_btnPrev";
        _btnPrev.Size = new Size(64, 128);
        _btnPrev.TabIndex = 1;
        _btnPrev.Text = "<";
        _btnPrev.UseVisualStyleBackColor = false;
        _btnPrev.Visible = true;
        _btnPrev.Anchor = AnchorStyles.Left;
        // 
        // _btnNext
        // 
        _btnNext.BackColor = Color.FromArgb(102, 0, 0, 0);
        _btnNext.FlatAppearance.BorderSize = 0;
        _btnNext.FlatAppearance.MouseOverBackColor = Color.FromArgb(150, 0, 0, 0);
        _btnNext.FlatStyle = FlatStyle.Flat;
        _btnNext.ForeColor = Color.White;
        _btnNext.Location = new Point(1488, 206);
        _btnNext.Name = "_btnNext";
        _btnNext.Size = new Size(64, 128);
        _btnNext.TabIndex = 2;
        _btnNext.Text = ">";
        _btnNext.UseVisualStyleBackColor = false;
        _btnNext.Visible = true;
        _btnNext.Anchor = AnchorStyles.Right;
        // 
        // _pbStatus
        // 
        _pbStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _pbStatus.Location = new Point(16, 572);
        _pbStatus.Name = "_pbStatus";
        _pbStatus.Size = new Size(1552, 24);
        _pbStatus.TabIndex = 3;
        // 
        // _btnOrganize
        // 
        _btnOrganize.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnOrganize.BackColor = Color.FromArgb(217, 34, 139, 34);
        _btnOrganize.FlatAppearance.BorderSize = 0;
        _btnOrganize.FlatAppearance.MouseDownBackColor = Color.FromArgb(255, 20, 100, 20);
        _btnOrganize.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 46, 184, 46);
        _btnOrganize.FlatStyle = FlatStyle.Flat;
        _btnOrganize.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        _btnOrganize.ForeColor = Color.White;
        _btnOrganize.Location = new Point(1152, 612);
        _btnOrganize.Name = "_btnOrganize";
        _btnOrganize.Size = new Size(200, 88);
        _btnOrganize.TabIndex = 4;
        _btnOrganize.Text = "📦 整理照片";
        _btnOrganize.UseVisualStyleBackColor = false;
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
        // _btnRotateLeft
        // 
        _btnRotateLeft.BackColor = Color.FromArgb(102, 0, 0, 0);
        _btnRotateLeft.FlatAppearance.BorderSize = 0;
        _btnRotateLeft.FlatAppearance.MouseOverBackColor = Color.FromArgb(150, 0, 0, 0);
        _btnRotateLeft.FlatStyle = FlatStyle.Flat;
        _btnRotateLeft.ForeColor = Color.White;
        _btnRotateLeft.Font = new Font("Segoe UI", 12F);
        _btnRotateLeft.Location = new Point(1416, 472);
        _btnRotateLeft.Name = "_btnRotateLeft";
        _btnRotateLeft.Size = new Size(64, 64);
        _btnRotateLeft.TabIndex = 10;
        _btnRotateLeft.Text = "⟲";
        _btnRotateLeft.UseVisualStyleBackColor = false;
        _btnRotateLeft.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        // 
        // _btnRotateRight
        // 
        _btnRotateRight.BackColor = Color.FromArgb(102, 0, 0, 0);
        _btnRotateRight.FlatAppearance.BorderSize = 0;
        _btnRotateRight.FlatAppearance.MouseOverBackColor = Color.FromArgb(150, 0, 0, 0);
        _btnRotateRight.FlatStyle = FlatStyle.Flat;
        _btnRotateRight.ForeColor = Color.White;
        _btnRotateRight.Font = new Font("Segoe UI", 12F);
        _btnRotateRight.Location = new Point(1488, 472);
        _btnRotateRight.Name = "_btnRotateRight";
        _btnRotateRight.Size = new Size(64, 64);
        _btnRotateRight.TabIndex = 11;
        _btnRotateRight.Text = "⟳";
        _btnRotateRight.UseVisualStyleBackColor = false;
        _btnRotateRight.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(9F, 19F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1584, 762);
        Controls.Add(_btnThemeToggle);
        Controls.Add(_btnTransfer);
        Controls.Add(_btnOrganize);
        Controls.Add(_pbStatus);
        Controls.Add(_picDisplay);
        _picDisplay.Controls.Add(_btnPrev);
        _picDisplay.Controls.Add(_btnNext);
        _picDisplay.Controls.Add(_btnRotateLeft);
        _picDisplay.Controls.Add(_btnRotateRight);
        Name = "Form1";
        Text = "快速挑選照片";
        KeyPreview = true;
        ((System.ComponentModel.ISupportInitialize)_picDisplay).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private PictureBox _picDisplay;
    private Button _btnPrev;
    private Button _btnNext;
    private Button _btnRotateLeft;
    private Button _btnRotateRight;
    private ProgressBar _pbStatus;
    private Button _btnOrganize;
    private Button _btnTransfer;
    private Button _btnThemeToggle;
}
