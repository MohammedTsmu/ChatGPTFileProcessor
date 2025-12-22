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
            this.panelMain = new DevExpress.XtraEditors.PanelControl();
            this.lblTitle = new DevExpress.XtraEditors.LabelControl();
            this.progressBar = new DevExpress.XtraEditors.ProgressBarControl();
            this.lblStatus = new DevExpress.XtraEditors.LabelControl();
            this.btnCancel = new DevExpress.XtraEditors.SimpleButton();
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
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(0, 0);
            this.panelMain.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(525, 222);
            this.panelMain.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.lblTitle.Appearance.Options.UseFont = true;
            this.lblTitle.Appearance.Options.UseForeColor = true;
            this.lblTitle.Location = new System.Drawing.Point(23, 25);
            this.lblTitle.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(194, 28);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Loading PDF Pages...";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(23, 74);
            this.progressBar.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.progressBar.Name = "progressBar";
            this.progressBar.Properties.ProgressViewStyle = DevExpress.XtraEditors.Controls.ProgressViewStyle.Solid;
            this.progressBar.Properties.ShowTitle = true;
            this.progressBar.Size = new System.Drawing.Size(478, 37);
            this.progressBar.TabIndex = 1;
            // 
            // lblStatus
            // 
            this.lblStatus.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblStatus.Appearance.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus.Appearance.Options.UseFont = true;
            this.lblStatus.Appearance.Options.UseForeColor = true;
            this.lblStatus.Location = new System.Drawing.Point(23, 123);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(73, 20);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "Preparing...";
            // 
            // btnCancel
            // 
            this.btnCancel.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnCancel.Appearance.Options.UseFont = true;
            this.btnCancel.Location = new System.Drawing.Point(385, 160);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(117, 37);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            // 
            // ProgressForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(525, 222);
            this.Controls.Add(this.panelMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProgressForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
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
