// ========================================
// ProgressForm.cs
// Progress dialog for PDF loading operations
// ========================================

using DevExpress.XtraEditors;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChatGPTFileProcessor
{
    public partial class ProgressForm : XtraForm
    {
        private ProgressBarControl progressBar;
        private LabelControl lblStatus;
        private SimpleButton btnCancel;
        private LabelControl lblTitle;
        private PanelControl panelMain;

        private int _totalPages;
        private bool _isCancelled;

        public bool IsCancelled => _isCancelled;

        public ProgressForm(int totalPages)
        {
            _totalPages = totalPages;
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeComponent()
        {
            this.panelMain = new PanelControl();
            this.lblTitle = new LabelControl();
            this.progressBar = new ProgressBarControl();
            this.lblStatus = new LabelControl();
            this.btnCancel = new SimpleButton();

            ((System.ComponentModel.ISupportInitialize)(this.panelMain)).BeginInit();
            this.panelMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.progressBar.Properties)).BeginInit();
            this.SuspendLayout();

            // 
            // panelMain
            // 
            this.panelMain.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.panelMain.Controls.Add(this.lblTitle);
            this.panelMain.Controls.Add(this.progressBar);
            this.panelMain.Controls.Add(this.lblStatus);
            this.panelMain.Controls.Add(this.btnCancel);
            this.panelMain.Dock = DockStyle.Fill;
            this.panelMain.Location = new Point(0, 0);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new Size(450, 180);
            this.panelMain.TabIndex = 0;

            // 
            // lblTitle
            // 
            this.lblTitle.Appearance.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            this.lblTitle.Appearance.ForeColor = Color.FromArgb(0, 122, 204);
            this.lblTitle.Appearance.Options.UseFont = true;
            this.lblTitle.Appearance.Options.UseForeColor = true;
            this.lblTitle.Location = new Point(20, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new Size(150, 21);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Loading PDF Pages...";

            // 
            // progressBar
            // 
            this.progressBar.Location = new Point(20, 60);
            this.progressBar.Name = "progressBar";
            this.progressBar.Properties.Maximum = 100;
            this.progressBar.Properties.ProgressViewStyle = DevExpress.XtraEditors.Controls.ProgressViewStyle.Solid;
            this.progressBar.Properties.ShowTitle = true;
            this.progressBar.Size = new Size(410, 30);
            this.progressBar.TabIndex = 1;

            // 
            // lblStatus
            // 
            this.lblStatus.Appearance.Font = new Font("Segoe UI", 9F);
            this.lblStatus.Appearance.ForeColor = Color.Gray;
            this.lblStatus.Appearance.Options.UseFont = true;
            this.lblStatus.Appearance.Options.UseForeColor = true;
            this.lblStatus.Location = new Point(20, 100);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new Size(100, 15);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "Preparing...";

            // 
            // btnCancel
            // 
            this.btnCancel.Appearance.Font = new Font("Segoe UI", 9F);
            this.btnCancel.Appearance.Options.UseFont = true;
            this.btnCancel.Location = new Point(330, 130);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new Size(100, 30);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Click += BtnCancel_Click;

            // 
            // ProgressForm
            // 
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(450, 180);
            this.Controls.Add(this.panelMain);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProgressForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Loading Progress";

            ((System.ComponentModel.ISupportInitialize)(this.panelMain)).EndInit();
            this.panelMain.ResumeLayout(false);
            this.panelMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.progressBar.Properties)).EndInit();
            this.ResumeLayout(false);
        }

        private void InitializeUI()
        {
            progressBar.Properties.Maximum = _totalPages;
            progressBar.Properties.Step = 1;
            progressBar.Position = 0;
        }

        public void UpdateProgress(int currentPage, string statusText)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateProgress(currentPage, statusText)));
                return;
            }

            progressBar.Position = currentPage;
            progressBar.Properties.PercentView = true;

            lblStatus.Text = statusText;

            // Update title bar too
            this.Text = $"Loading Progress - {currentPage}/{_totalPages}";
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            _isCancelled = true;
            this.Close();
        }
    }
}

// ========================================
// END OF ProgressForm.cs
// ========================================
