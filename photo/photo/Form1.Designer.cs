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
        _btnOrganize = new Button();
        _btnPurge = new Button();
        _btnThemeToggle = new Button();
        _splitLine = new Panel();
        ((System.ComponentModel.ISupportInitialize)_picDisplay).BeginInit();
        SuspendLayout();
        // 
        // _picDisplay
        // 
        _picDisplay.BackColor = Color.FromArgb(240, 240, 240);
        _picDisplay.Name = "_picDisplay";
        _picDisplay.SizeMode = PictureBoxSizeMode.Normal;
        _picDisplay.TabIndex = 0;
        _picDisplay.TabStop = false;
        // 
        // _btnPrev
        // 
        _btnPrev.BackColor = Color.FromArgb(102, 0, 0, 0);
        _btnPrev.FlatAppearance.BorderSize = 0;
        _btnPrev.FlatStyle = FlatStyle.Flat;
        _btnPrev.ForeColor = Color.White;
        _btnPrev.Name = "_btnPrev";
        _btnPrev.Size = new Size(64, 128);
        _btnPrev.Text = "<";
        // 
        // _btnNext
        // 
        _btnNext.BackColor = Color.FromArgb(102, 0, 0, 0);
        _btnNext.FlatAppearance.BorderSize = 0;
        _btnNext.FlatStyle = FlatStyle.Flat;
        _btnNext.ForeColor = Color.White;
        _btnNext.Name = "_btnNext";
        _btnNext.Size = new Size(64, 128);
        _btnNext.Text = ">";
        // 
        // _btnRotateLeft
        // 
        _btnRotateLeft.BackColor = Color.FromArgb(102, 0, 0, 0);
        _btnRotateLeft.FlatAppearance.BorderSize = 0;
        _btnRotateLeft.FlatStyle = FlatStyle.Flat;
        _btnRotateLeft.ForeColor = Color.White;
        _btnRotateLeft.Name = "_btnRotateLeft";
        _btnRotateLeft.Size = new Size(64, 64);
        _btnRotateLeft.Text = "⟲";
        // 
        // _btnRotateRight
        // 
        _btnRotateRight.BackColor = Color.FromArgb(102, 0, 0, 0);
        _btnRotateRight.FlatAppearance.BorderSize = 0;
        _btnRotateRight.FlatStyle = FlatStyle.Flat;
        _btnRotateRight.ForeColor = Color.White;
        _btnRotateRight.Name = "_btnRotateRight";
        _btnRotateRight.Size = new Size(64, 64);
        _btnRotateRight.Text = "⟳";
        // 
        // _btnOrganize
        // 
        _btnOrganize.BackColor = Color.FromArgb(217, 34, 139, 34);
        _btnOrganize.FlatAppearance.BorderSize = 0;
        _btnOrganize.FlatStyle = FlatStyle.Flat;
        _btnOrganize.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        _btnOrganize.ForeColor = Color.White;
        _btnOrganize.Name = "_btnOrganize";
        _btnOrganize.Size = new Size(200, 88);
        _btnOrganize.Text = "📦 整理照片";
        // 
        // _btnPurge
        // 
        _btnPurge.BackColor = Color.FromArgb(217, 220, 50, 50);
        _btnPurge.FlatAppearance.BorderSize = 0;
        _btnPurge.FlatStyle = FlatStyle.Flat;
        _btnPurge.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        _btnPurge.ForeColor = Color.White;
        _btnPurge.Name = "_btnPurge";
        _btnPurge.Size = new Size(200, 88);
        _btnPurge.Text = "🗑️ 清空暫存";
        // 
        // _btnThemeToggle
        // 
        _btnThemeToggle.FlatStyle = FlatStyle.Flat;
        _btnThemeToggle.Name = "_btnThemeToggle";
        _btnThemeToggle.Size = new Size(48, 40);
        _btnThemeToggle.Text = "🌙";
        _btnThemeToggle.Click += btnThemeToggle_Click;
        // 
        // _splitLine
        // 
        _splitLine.BackColor = Color.FromArgb(200, 200, 200);
        _splitLine.Name = "_splitLine";
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(9F, 19F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1584, 861);
        Controls.Add(_btnThemeToggle);
        Controls.Add(_btnOrganize);
        Controls.Add(_btnPurge);
        Controls.Add(_splitLine);
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
    private Button _btnOrganize;
    private Button _btnPurge;
    private Button _btnThemeToggle;
    private Panel _splitLine;
}
