namespace ChatGPTFileProcessor
{
    partial class PageSelectionForm
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
            this.galleryControl1 = new DevExpress.XtraBars.Ribbon.GalleryControl();
            this.galleryControlClient1 = new DevExpress.XtraBars.Ribbon.GalleryControlClient();
            this.panelTop = new DevExpress.XtraEditors.PanelControl();
            this.lblTitle = new DevExpress.XtraEditors.LabelControl();
            this.lblFileInfo = new DevExpress.XtraEditors.LabelControl();
            this.panelBottom = new DevExpress.XtraEditors.PanelControl();
            this.groupRange = new DevExpress.XtraEditors.GroupControl();
            this.labelFrom = new DevExpress.XtraEditors.LabelControl();
            this.spinFrom = new DevExpress.XtraEditors.SpinEdit();
            this.labelTo = new DevExpress.XtraEditors.LabelControl();
            this.spinTo = new DevExpress.XtraEditors.SpinEdit();
            this.btnSelectAll = new DevExpress.XtraEditors.SimpleButton();
            this.btnClear = new DevExpress.XtraEditors.SimpleButton();
            this.btnOK = new DevExpress.XtraEditors.SimpleButton();
            this.btnCancel = new DevExpress.XtraEditors.SimpleButton();
            this.labelHelp = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.galleryControl1)).BeginInit();
            this.galleryControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.panelTop)).BeginInit();
            this.panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.panelBottom)).BeginInit();
            this.panelBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.groupRange)).BeginInit();
            this.groupRange.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.spinFrom.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.spinTo.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // galleryControl1
            // 
            this.galleryControl1.Controls.Add(this.galleryControlClient1);
            this.galleryControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.galleryControl1.Location = new System.Drawing.Point(0, 98);
            this.galleryControl1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.galleryControl1.Name = "galleryControl1";
            this.galleryControl1.Size = new System.Drawing.Size(1167, 677);
            this.galleryControl1.TabIndex = 0;
            // 
            // galleryControlClient1
            // 
            this.galleryControlClient1.GalleryControl = this.galleryControl1;
            this.galleryControlClient1.Location = new System.Drawing.Point(2, 2);
            this.galleryControlClient1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.galleryControlClient1.Size = new System.Drawing.Size(1142, 673);
            // 
            // panelTop
            // 
            this.panelTop.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.panelTop.Controls.Add(this.lblTitle);
            this.panelTop.Controls.Add(this.lblFileInfo);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1167, 98);
            this.panelTop.TabIndex = 1;
            // 
            // lblTitle
            // 
            this.lblTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.lblTitle.Appearance.Options.UseFont = true;
            this.lblTitle.Appearance.Options.UseForeColor = true;
            this.lblTitle.Location = new System.Drawing.Point(23, 18);
            this.lblTitle.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(236, 37);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Select Page Range";
            // 
            // lblFileInfo
            // 
            this.lblFileInfo.Appearance.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblFileInfo.Appearance.ForeColor = System.Drawing.Color.Gray;
            this.lblFileInfo.Appearance.Options.UseFont = true;
            this.lblFileInfo.Appearance.Options.UseForeColor = true;
            this.lblFileInfo.Location = new System.Drawing.Point(23, 62);
            this.lblFileInfo.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.lblFileInfo.Name = "lblFileInfo";
            this.lblFileInfo.Size = new System.Drawing.Size(107, 23);
            this.lblFileInfo.TabIndex = 1;
            this.lblFileInfo.Text = "No file loaded";
            // 
            // panelBottom
            // 
            this.panelBottom.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.panelBottom.Controls.Add(this.groupRange);
            this.panelBottom.Controls.Add(this.btnSelectAll);
            this.panelBottom.Controls.Add(this.btnClear);
            this.panelBottom.Controls.Add(this.btnOK);
            this.panelBottom.Controls.Add(this.btnCancel);
            this.panelBottom.Controls.Add(this.labelHelp);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(0, 775);
            this.panelBottom.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(1167, 148);
            this.panelBottom.TabIndex = 2;
            // 
            // groupRange
            // 
            this.groupRange.AppearanceCaption.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.groupRange.AppearanceCaption.Options.UseFont = true;
            this.groupRange.Controls.Add(this.labelFrom);
            this.groupRange.Controls.Add(this.spinFrom);
            this.groupRange.Controls.Add(this.labelTo);
            this.groupRange.Controls.Add(this.spinTo);
            this.groupRange.Location = new System.Drawing.Point(23, 12);
            this.groupRange.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupRange.Name = "groupRange";
            this.groupRange.Size = new System.Drawing.Size(408, 123);
            this.groupRange.TabIndex = 0;
            this.groupRange.Text = "Page Range";
            // 
            // labelFrom
            // 
            this.labelFrom.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.labelFrom.Appearance.Options.UseFont = true;
            this.labelFrom.Location = new System.Drawing.Point(18, 49);
            this.labelFrom.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.labelFrom.Name = "labelFrom";
            this.labelFrom.Size = new System.Drawing.Size(37, 20);
            this.labelFrom.TabIndex = 0;
            this.labelFrom.Text = "From:";
            // 
            // spinFrom
            // 
            this.spinFrom.EditValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.spinFrom.Location = new System.Drawing.Point(64, 46);
            this.spinFrom.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.spinFrom.Name = "spinFrom";
            this.spinFrom.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.spinFrom.Properties.Appearance.Options.UseFont = true;
            this.spinFrom.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.spinFrom.Properties.IsFloatValue = false;
            this.spinFrom.Properties.Mask.EditMask = "N00";
            this.spinFrom.Properties.MaxValue = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.spinFrom.Properties.MinValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.spinFrom.Size = new System.Drawing.Size(117, 30);
            this.spinFrom.TabIndex = 1;
            // 
            // labelTo
            // 
            this.labelTo.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.labelTo.Appearance.Options.UseFont = true;
            this.labelTo.Location = new System.Drawing.Point(210, 49);
            this.labelTo.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.labelTo.Name = "labelTo";
            this.labelTo.Size = new System.Drawing.Size(20, 20);
            this.labelTo.TabIndex = 2;
            this.labelTo.Text = "To:";
            // 
            // spinTo
            // 
            this.spinTo.EditValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.spinTo.Location = new System.Drawing.Point(245, 46);
            this.spinTo.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.spinTo.Name = "spinTo";
            this.spinTo.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.spinTo.Properties.Appearance.Options.UseFont = true;
            this.spinTo.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.spinTo.Properties.IsFloatValue = false;
            this.spinTo.Properties.Mask.EditMask = "N00";
            this.spinTo.Properties.MaxValue = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.spinTo.Properties.MinValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.spinTo.Size = new System.Drawing.Size(117, 30);
            this.spinTo.TabIndex = 3;
            // 
            // btnSelectAll
            // 
            this.btnSelectAll.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnSelectAll.Appearance.Options.UseFont = true;
            this.btnSelectAll.Location = new System.Drawing.Point(455, 37);
            this.btnSelectAll.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnSelectAll.Name = "btnSelectAll";
            this.btnSelectAll.Size = new System.Drawing.Size(140, 43);
            this.btnSelectAll.TabIndex = 1;
            this.btnSelectAll.Text = "Select All (Ctrl+A)";
            this.btnSelectAll.Click += new System.EventHandler(this.BtnSelectAll_Click);
            // 
            // btnClear
            // 
            this.btnClear.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnClear.Appearance.Options.UseFont = true;
            this.btnClear.Location = new System.Drawing.Point(607, 37);
            this.btnClear.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(140, 43);
            this.btnClear.TabIndex = 2;
            this.btnClear.Text = "Clear (Esc)";
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);
            // 
            // btnOK
            // 
            this.btnOK.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnOK.Appearance.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnOK.Appearance.ForeColor = System.Drawing.Color.White;
            this.btnOK.Appearance.Options.UseBackColor = true;
            this.btnOK.Appearance.Options.UseFont = true;
            this.btnOK.Appearance.Options.UseForeColor = true;
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(910, 37);
            this.btnOK.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(117, 49);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "OK";
            // 
            // btnCancel
            // 
            this.btnCancel.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnCancel.Appearance.Options.UseFont = true;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(1038, 37);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(117, 49);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            // 
            // labelHelp
            // 
            this.labelHelp.Appearance.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this.labelHelp.Appearance.ForeColor = System.Drawing.Color.Gray;
            this.labelHelp.Appearance.Options.UseFont = true;
            this.labelHelp.Appearance.Options.UseForeColor = true;
            this.labelHelp.Location = new System.Drawing.Point(23, 105);
            this.labelHelp.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.labelHelp.Name = "labelHelp";
            this.labelHelp.Size = new System.Drawing.Size(694, 19);
            this.labelHelp.TabIndex = 5;
            this.labelHelp.Text = "💡 Tip: Click thumbnails to set range, Double-click to preview, Use arrow keys to" +
    " navigate, Press Enter to zoom";
            // 
            // PageSelectionForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(1167, 923);
            this.Controls.Add(this.galleryControl1);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.panelBottom);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "PageSelectionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Select Pages";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.galleryControl1)).EndInit();
            this.galleryControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.panelTop)).EndInit();
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.panelBottom)).EndInit();
            this.panelBottom.ResumeLayout(false);
            this.panelBottom.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.groupRange)).EndInit();
            this.groupRange.ResumeLayout(false);
            this.groupRange.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.spinFrom.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.spinTo.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraBars.Ribbon.GalleryControl galleryControl1;
        private DevExpress.XtraBars.Ribbon.GalleryControlClient galleryControlClient1;
        private DevExpress.XtraEditors.PanelControl panelTop;
        private DevExpress.XtraEditors.LabelControl lblTitle;
        private DevExpress.XtraEditors.LabelControl lblFileInfo;
        private DevExpress.XtraEditors.PanelControl panelBottom;
        private DevExpress.XtraEditors.GroupControl groupRange;
        private DevExpress.XtraEditors.LabelControl labelFrom;
        private DevExpress.XtraEditors.SpinEdit spinFrom;
        private DevExpress.XtraEditors.LabelControl labelTo;
        private DevExpress.XtraEditors.SpinEdit spinTo;
        private DevExpress.XtraEditors.SimpleButton btnSelectAll;
        private DevExpress.XtraEditors.SimpleButton btnClear;
        private DevExpress.XtraEditors.SimpleButton btnOK;
        private DevExpress.XtraEditors.SimpleButton btnCancel;
        private DevExpress.XtraEditors.LabelControl labelHelp;

        // Button click handlers
        private void BtnSelectAll_Click(object sender, System.EventArgs e)
        {
            SelectAllPages();
        }

        private void BtnClear_Click(object sender, System.EventArgs e)
        {
            ClearSelection();
        }
    }
}