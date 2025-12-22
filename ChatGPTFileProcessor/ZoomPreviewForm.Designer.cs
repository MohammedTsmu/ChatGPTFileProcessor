// ========================================
// ZoomPreviewForm.Designer.cs
// Proper designer file for Visual Studio
// ========================================

namespace ChatGPTFileProcessor
{
    partial class ZoomPreviewForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            if (disposing)
            {
                if (_currentImage != null)
                {
                    _currentImage.Dispose();
                    _currentImage = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.panelTools = new DevExpress.XtraEditors.PanelControl();
            this.lblZoom = new DevExpress.XtraEditors.LabelControl();
            this.btnZoom100 = new DevExpress.XtraEditors.SimpleButton();
            this.btnZoomFit = new DevExpress.XtraEditors.SimpleButton();
            this.btnZoomOut = new DevExpress.XtraEditors.SimpleButton();
            this.btnZoomIn = new DevExpress.XtraEditors.SimpleButton();
            this.lblPageInfo = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelTools)).BeginInit();
            this.panelTools.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox
            // 
            this.pictureBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox.Location = new System.Drawing.Point(0, 62);
            this.pictureBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(933, 676);
            this.pictureBox.TabIndex = 0;
            this.pictureBox.TabStop = false;
            this.pictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.PictureBox_Paint);
            this.pictureBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PictureBox_MouseDown);
            this.pictureBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PictureBox_MouseMove);
            this.pictureBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PictureBox_MouseUp);
            this.pictureBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.PictureBox_MouseWheel);
            // 
            // panelTools
            // 
            this.panelTools.Controls.Add(this.lblZoom);
            this.panelTools.Controls.Add(this.btnZoom100);
            this.panelTools.Controls.Add(this.btnZoomFit);
            this.panelTools.Controls.Add(this.btnZoomOut);
            this.panelTools.Controls.Add(this.btnZoomIn);
            this.panelTools.Controls.Add(this.lblPageInfo);
            this.panelTools.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTools.Location = new System.Drawing.Point(0, 0);
            this.panelTools.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.panelTools.Name = "panelTools";
            this.panelTools.Size = new System.Drawing.Size(933, 62);
            this.panelTools.TabIndex = 1;
            // 
            // lblZoom
            // 
            this.lblZoom.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblZoom.Appearance.ForeColor = System.Drawing.Color.Gray;
            this.lblZoom.Appearance.Options.UseFont = true;
            this.lblZoom.Appearance.Options.UseForeColor = true;
            this.lblZoom.Location = new System.Drawing.Point(793, 21);
            this.lblZoom.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.lblZoom.Name = "lblZoom";
            this.lblZoom.Size = new System.Drawing.Size(36, 20);
            this.lblZoom.TabIndex = 5;
            this.lblZoom.Text = "100%";
            // 
            // btnZoom100
            // 
            this.btnZoom100.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnZoom100.Appearance.Options.UseFont = true;
            this.btnZoom100.Location = new System.Drawing.Point(630, 12);
            this.btnZoom100.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnZoom100.Name = "btnZoom100";
            this.btnZoom100.Size = new System.Drawing.Size(93, 37);
            this.btnZoom100.TabIndex = 4;
            this.btnZoom100.Text = "100% (Ctrl+1)";
            // 
            // btnZoomFit
            // 
            this.btnZoomFit.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnZoomFit.Appearance.Options.UseFont = true;
            this.btnZoomFit.Location = new System.Drawing.Point(525, 12);
            this.btnZoomFit.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnZoomFit.Name = "btnZoomFit";
            this.btnZoomFit.Size = new System.Drawing.Size(93, 37);
            this.btnZoomFit.TabIndex = 3;
            this.btnZoomFit.Text = "Fit (Ctrl+0)";
            // 
            // btnZoomOut
            // 
            this.btnZoomOut.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnZoomOut.Appearance.Options.UseFont = true;
            this.btnZoomOut.Location = new System.Drawing.Point(408, 12);
            this.btnZoomOut.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnZoomOut.Name = "btnZoomOut";
            this.btnZoomOut.Size = new System.Drawing.Size(105, 37);
            this.btnZoomOut.TabIndex = 2;
            this.btnZoomOut.Text = "Zoom Out (-)";
            // 
            // btnZoomIn
            // 
            this.btnZoomIn.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnZoomIn.Appearance.Options.UseFont = true;
            this.btnZoomIn.Location = new System.Drawing.Point(292, 12);
            this.btnZoomIn.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnZoomIn.Name = "btnZoomIn";
            this.btnZoomIn.Size = new System.Drawing.Size(105, 37);
            this.btnZoomIn.TabIndex = 1;
            this.btnZoomIn.Text = "Zoom In (+)";
            // 
            // lblPageInfo
            // 
            this.lblPageInfo.Appearance.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblPageInfo.Appearance.Options.UseFont = true;
            this.lblPageInfo.Location = new System.Drawing.Point(18, 18);
            this.lblPageInfo.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.lblPageInfo.Name = "lblPageInfo";
            this.lblPageInfo.Size = new System.Drawing.Size(54, 23);
            this.lblPageInfo.TabIndex = 0;
            this.lblPageInfo.Text = "Page 1";
            // 
            // ZoomPreviewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(933, 738);
            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.panelTools);
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MinimumSize = new System.Drawing.Size(600, 400);
            this.Name = "ZoomPreviewForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Page Preview";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelTools)).EndInit();
            this.panelTools.ResumeLayout(false);
            this.panelTools.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox;
        private DevExpress.XtraEditors.PanelControl panelTools;
        private DevExpress.XtraEditors.SimpleButton btnZoomIn;
        private DevExpress.XtraEditors.SimpleButton btnZoomOut;
        private DevExpress.XtraEditors.SimpleButton btnZoomFit;
        private DevExpress.XtraEditors.SimpleButton btnZoom100;
        private DevExpress.XtraEditors.LabelControl lblPageInfo;
        private DevExpress.XtraEditors.LabelControl lblZoom;
    }
}