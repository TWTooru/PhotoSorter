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
        _btnRemoveRaw = new Button();
        _btnMaybe = new Button();
        _btnTrash = new Button();
        _btnThemeToggle = new Button();
        _splitLine = new Panel();
        ((System.ComponentModel.ISupportInitialize)_picDisplay).BeginInit();
        SuspendLayout();
        // 
        // _picDisplay
        // 
        _picDisplay.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _picDisplay.BackColor = Color.FromArgb(240, 240, 240);
        _picDisplay.Location = new Point(16, 16);
        _picDisplay.Name = "_picDisplay";
        _picDisplay.Size = new Size(1552, 650);
        _picDisplay.TabIndex = 0;
        _picDisplay.TabStop = false;
        // 
        // _btnPrev
        // 
        _btnPrev.Anchor = AnchorStyles.Left;
        _btnPrev.BackColor = Color.FromArgb(102, 0, 0, 0);
        _btnPrev.FlatAppearance.BorderSize = 0;
        _btnPrev.FlatStyle = FlatStyle.Flat;
        _btnPrev.ForeColor = Color.White;
        _btnPrev.Location = new Point(16, 277);
        _btnPrev.Name = "_btnPrev";
        _btnPrev.Size = new Size(64, 128);
        _btnPrev.TabIndex = 1;
        _btnPrev.Text = "<";
        _btnPrev.UseVisualStyleBackColor = false;
        // 
        // _btnNext
        // 
        _btnNext.Anchor = AnchorStyles.Right;
        _btnNext.BackColor = Color.FromArgb(102, 0, 0, 0);
        _btnNext.FlatAppearance.BorderSize = 0;
        _btnNext.FlatStyle = FlatStyle.Flat;
        _btnNext.ForeColor = Color.White;
        _btnNext.Location = new Point(1504, 277);
        _btnNext.Name = "_btnNext";
        _btnNext.Size = new Size(64, 128);
        _btnNext.TabIndex = 2;
        _btnNext.Text = ">";
        _btnNext.UseVisualStyleBackColor = false;
        // 
        // _btnRotateLeft
        // 
        _btnRotateLeft.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnRotateLeft.BackColor = Color.FromArgb(102, 0, 0, 0);
        _btnRotateLeft.FlatAppearance.BorderSize = 0;
        _btnRotateLeft.FlatStyle = FlatStyle.Flat;
        _btnRotateLeft.ForeColor = Color.White;
        _btnRotateLeft.Location = new Point(1415, 580);
        _btnRotateLeft.Name = "_btnRotateLeft";
        _btnRotateLeft.Size = new Size(64, 64);
        _btnRotateLeft.TabIndex = 3;
        _btnRotateLeft.Text = "⟲";
        _btnRotateLeft.UseVisualStyleBackColor = false;
        // 
        // _btnRotateRight
        // 
        _btnRotateRight.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnRotateRight.BackColor = Color.FromArgb(102, 0, 0, 0);
        _btnRotateRight.FlatAppearance.BorderSize = 0;
        _btnRotateRight.FlatStyle = FlatStyle.Flat;
        _btnRotateRight.ForeColor = Color.White;
        _btnRotateRight.Location = new Point(1485, 580);
        _btnRotateRight.Name = "_btnRotateRight";
        _btnRotateRight.Size = new Size(64, 64);
        _btnRotateRight.TabIndex = 4;
        _btnRotateRight.Text = "⟳";
        _btnRotateRight.UseVisualStyleBackColor = false;
        // 
        // _btnOrganize
        // 
        _btnOrganize.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnOrganize.BackColor = Color.FromArgb(217, 34, 139, 34);
        _btnOrganize.FlatAppearance.BorderSize = 0;
        _btnOrganize.FlatStyle = FlatStyle.Flat;
        _btnOrganize.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _btnOrganize.ForeColor = Color.White;
        _btnOrganize.Location = new Point(1408, 785);
        _btnOrganize.Name = "_btnOrganize";
        _btnOrganize.Size = new Size(160, 60);
        _btnOrganize.TabIndex = 5;
        _btnOrganize.Text = "📦 整理照片";
        _btnOrganize.UseVisualStyleBackColor = false;
        // 
        // _btnPurge
        // 
        _btnPurge.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnPurge.BackColor = Color.FromArgb(217, 220, 50, 50);
        _btnPurge.FlatAppearance.BorderSize = 0;
        _btnPurge.FlatStyle = FlatStyle.Flat;
        _btnPurge.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _btnPurge.ForeColor = Color.White;
        _btnPurge.Location = new Point(1238, 785);
        _btnPurge.Name = "_btnPurge";
        _btnPurge.Size = new Size(160, 60);
        _btnPurge.TabIndex = 6;
        _btnPurge.Text = "🗑️ 清空暫存";
        _btnPurge.UseVisualStyleBackColor = false;
        // 
        // _btnRemoveRaw
        // 
        _btnRemoveRaw.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnRemoveRaw.BackColor = Color.FromArgb(217, 80, 80, 80);
        _btnRemoveRaw.FlatAppearance.BorderSize = 0;
        _btnRemoveRaw.FlatStyle = FlatStyle.Flat;
        _btnRemoveRaw.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _btnRemoveRaw.ForeColor = Color.White;
        _btnRemoveRaw.Location = new Point(1368, 715);
        _btnRemoveRaw.Name = "_btnRemoveRaw";
        _btnRemoveRaw.Size = new Size(200, 60);
        _btnRemoveRaw.TabIndex = 7;
        _btnRemoveRaw.Text = "移除對應編號RAW";
        _btnRemoveRaw.UseVisualStyleBackColor = false;
        // 
        // _btnMaybe
        // 
        _btnMaybe.Anchor = AnchorStyles.Bottom;
        _btnMaybe.Location = new Point(697, 728);
        _btnMaybe.Name = "_btnMaybe";
        _btnMaybe.Size = new Size(80, 80);
        _btnMaybe.TabIndex = 8;
        // 
        // _btnTrash
        // 
        _btnTrash.Anchor = AnchorStyles.Bottom;
        _btnTrash.Location = new Point(807, 728);
        _btnTrash.Name = "_btnTrash";
        _btnTrash.Size = new Size(80, 80);
        _btnTrash.TabIndex = 9;
        // 
        // _btnThemeToggle
        // 
        _btnThemeToggle.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _btnThemeToggle.FlatStyle = FlatStyle.Flat;
        _btnThemeToggle.Location = new Point(16, 805);
        _btnThemeToggle.Name = "_btnThemeToggle";
        _btnThemeToggle.Size = new Size(48, 40);
        _btnThemeToggle.TabIndex = 10;
        _btnThemeToggle.Text = "🌙";
        _btnThemeToggle.UseVisualStyleBackColor = false;
        _btnThemeToggle.Click += btnThemeToggle_Click;
        // 
        // _splitLine
        // 
        _splitLine.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _splitLine.BackColor = Color.FromArgb(200, 200, 200);
        _splitLine.Location = new Point(0, 685);
        _splitLine.Name = "_splitLine";
        _splitLine.Size = new Size(1584, 1);
        _splitLine.TabIndex = 11;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(9F, 19F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1584, 861);
        Controls.Add(_btnPrev);
        Controls.Add(_btnNext);
        Controls.Add(_btnRotateLeft);
        Controls.Add(_btnRotateRight);
        Controls.Add(_btnOrganize);
        Controls.Add(_btnPurge);
        Controls.Add(_btnRemoveRaw);
        Controls.Add(_btnMaybe);
        Controls.Add(_btnTrash);
        Controls.Add(_btnThemeToggle);
        Controls.Add(_splitLine);
        Controls.Add(_picDisplay);
        KeyPreview = true;
        Name = "Form1";
        Text = "快速挑選照片";
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
    private Button _btnRemoveRaw;
    private Button _btnMaybe;
    private Button _btnTrash;
    private Button _btnThemeToggle;
    private Panel _splitLine;
}
