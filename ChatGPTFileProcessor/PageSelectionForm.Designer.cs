namespace ChatGPTFileProcessor
{
    partial class PageSelectionForm
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
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            DevExpress.Utils.SuperToolTip superToolTip1 = new DevExpress.Utils.SuperToolTip();
            DevExpress.Utils.ToolTipItem toolTipItem1 = new DevExpress.Utils.ToolTipItem();
            DevExpress.Utils.SuperToolTip superToolTip2 = new DevExpress.Utils.SuperToolTip();
            DevExpress.Utils.ToolTipItem toolTipItem2 = new DevExpress.Utils.ToolTipItem();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PageSelectionForm));
            this.galleryControl1 = new DevExpress.XtraBars.Ribbon.GalleryControl();
            this.galleryControlClient1 = new DevExpress.XtraBars.Ribbon.GalleryControlClient();
            this.galleryControlClient2 = new DevExpress.XtraBars.Ribbon.GalleryControlClient();
            this.spinFrom = new DevExpress.XtraEditors.SpinEdit();
            this.spinTo = new DevExpress.XtraEditors.SpinEdit();
            this.simpleButton1 = new DevExpress.XtraEditors.SimpleButton();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.panelMiddle = new System.Windows.Forms.Panel();
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            this.panelTop = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.galleryControl1)).BeginInit();
            this.galleryControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.spinFrom.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.spinTo.Properties)).BeginInit();
            this.panelBottom.SuspendLayout();
            this.panelMiddle.SuspendLayout();
            this.panelTop.SuspendLayout();
            this.SuspendLayout();
            // 
            // galleryControl1
            // 
            this.galleryControl1.Controls.Add(this.galleryControlClient1);
            this.galleryControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.galleryControl1.Location = new System.Drawing.Point(0, 0);
            this.galleryControl1.Name = "galleryControl1";
            this.galleryControl1.Padding = new System.Windows.Forms.Padding(5);
            this.galleryControl1.Size = new System.Drawing.Size(1062, 544);
            this.galleryControl1.TabIndex = 0;
            this.galleryControl1.Text = "galleryControl1";
            // 
            // galleryControlClient1
            // 
            this.galleryControlClient1.GalleryControl = this.galleryControl1;
            this.galleryControlClient1.Location = new System.Drawing.Point(6, 6);
            this.galleryControlClient1.Size = new System.Drawing.Size(1029, 532);
            // 
            // galleryControlClient2
            // 
            this.galleryControlClient2.GalleryControl = null;
            this.galleryControlClient2.Location = new System.Drawing.Point(0, 0);
            this.galleryControlClient2.Size = new System.Drawing.Size(0, 0);
            // 
            // spinFrom
            // 
            this.spinFrom.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.spinFrom.EditValue = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.spinFrom.Location = new System.Drawing.Point(312, 10);
            this.spinFrom.Name = "spinFrom";
            this.spinFrom.Properties.Appearance.Font = new System.Drawing.Font("LBC", 12F);
            this.spinFrom.Properties.Appearance.Options.UseFont = true;
            this.spinFrom.Properties.Appearance.Options.UseTextOptions = true;
            this.spinFrom.Properties.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            this.spinFrom.Properties.AppearanceDisabled.Font = new System.Drawing.Font("LBC", 12F);
            this.spinFrom.Properties.AppearanceDisabled.Options.UseFont = true;
            this.spinFrom.Properties.AppearanceDisabled.Options.UseTextOptions = true;
            this.spinFrom.Properties.AppearanceDisabled.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            this.spinFrom.Properties.AppearanceFocused.Font = new System.Drawing.Font("LBC", 12F);
            this.spinFrom.Properties.AppearanceFocused.Options.UseFont = true;
            this.spinFrom.Properties.AppearanceFocused.Options.UseTextOptions = true;
            this.spinFrom.Properties.AppearanceFocused.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            this.spinFrom.Properties.AppearanceReadOnly.Font = new System.Drawing.Font("LBC", 12F);
            this.spinFrom.Properties.AppearanceReadOnly.Options.UseFont = true;
            this.spinFrom.Properties.AppearanceReadOnly.Options.UseTextOptions = true;
            this.spinFrom.Properties.AppearanceReadOnly.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            this.spinFrom.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.spinFrom.Properties.MaskSettings.Set("mask", "d");
            this.spinFrom.Size = new System.Drawing.Size(125, 32);
            toolTipItem1.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            toolTipItem1.Appearance.Options.UseFont = true;
            toolTipItem1.Text = "Enter PDF start Page That AI Will Start Generating Content From";
            superToolTip1.Items.Add(toolTipItem1);
            this.spinFrom.SuperTip = superToolTip1;
            this.spinFrom.TabIndex = 1;
            // 
            // spinTo
            // 
            this.spinTo.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.spinTo.EditValue = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.spinTo.Location = new System.Drawing.Point(312, 48);
            this.spinTo.Name = "spinTo";
            this.spinTo.Properties.Appearance.Font = new System.Drawing.Font("LBC", 12F);
            this.spinTo.Properties.Appearance.Options.UseFont = true;
            this.spinTo.Properties.Appearance.Options.UseTextOptions = true;
            this.spinTo.Properties.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            this.spinTo.Properties.AppearanceDisabled.Font = new System.Drawing.Font("LBC", 12F);
            this.spinTo.Properties.AppearanceDisabled.Options.UseFont = true;
            this.spinTo.Properties.AppearanceFocused.Font = new System.Drawing.Font("LBC", 12F);
            this.spinTo.Properties.AppearanceFocused.Options.UseFont = true;
            this.spinTo.Properties.AppearanceReadOnly.Font = new System.Drawing.Font("LBC", 12F);
            this.spinTo.Properties.AppearanceReadOnly.Options.UseFont = true;
            this.spinTo.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.spinTo.Properties.MaskSettings.Set("mask", "d");
            this.spinTo.Size = new System.Drawing.Size(125, 32);
            toolTipItem2.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            toolTipItem2.Appearance.Options.UseFont = true;
            toolTipItem2.Text = "Enter PDF End Page That AI Will End Generating Content To";
            superToolTip2.Items.Add(toolTipItem2);
            this.spinTo.SuperTip = superToolTip2;
            this.spinTo.TabIndex = 2;
            // 
            // simpleButton1
            // 
            this.simpleButton1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.simpleButton1.Appearance.Font = new System.Drawing.Font("LBC", 12F);
            this.simpleButton1.Appearance.Options.UseFont = true;
            this.simpleButton1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.simpleButton1.ImageOptions.SvgImage = ((DevExpress.Utils.Svg.SvgImage)(resources.GetObject("simpleButton1.ImageOptions.SvgImage")));
            this.simpleButton1.ImageOptions.SvgImageSize = new System.Drawing.Size(25, 25);
            this.simpleButton1.Location = new System.Drawing.Point(669, 23);
            this.simpleButton1.Name = "simpleButton1";
            this.simpleButton1.Size = new System.Drawing.Size(184, 44);
            this.simpleButton1.TabIndex = 3;
            this.simpleButton1.Text = "OK";
            // 
            // panelBottom
            // 
            this.panelBottom.BackColor = System.Drawing.Color.SteelBlue;
            this.panelBottom.Controls.Add(this.labelControl2);
            this.panelBottom.Controls.Add(this.labelControl1);
            this.panelBottom.Controls.Add(this.spinFrom);
            this.panelBottom.Controls.Add(this.simpleButton1);
            this.panelBottom.Controls.Add(this.spinTo);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(0, 583);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(1062, 90);
            this.panelBottom.TabIndex = 4;
            // 
            // labelControl2
            // 
            this.labelControl2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.labelControl2.Appearance.Font = new System.Drawing.Font("LBC", 12F);
            this.labelControl2.Appearance.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.labelControl2.Appearance.Options.UseFont = true;
            this.labelControl2.Appearance.Options.UseForeColor = true;
            this.labelControl2.Location = new System.Drawing.Point(220, 51);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(86, 26);
            this.labelControl2.TabIndex = 5;
            this.labelControl2.Text = "End Page";
            // 
            // labelControl1
            // 
            this.labelControl1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.labelControl1.Appearance.Font = new System.Drawing.Font("LBC", 12F);
            this.labelControl1.Appearance.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Appearance.Options.UseForeColor = true;
            this.labelControl1.Location = new System.Drawing.Point(210, 13);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(96, 26);
            this.labelControl1.TabIndex = 4;
            this.labelControl1.Text = "Start Page";
            // 
            // panelMiddle
            // 
            this.panelMiddle.Controls.Add(this.galleryControl1);
            this.panelMiddle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMiddle.Location = new System.Drawing.Point(0, 39);
            this.panelMiddle.Name = "panelMiddle";
            this.panelMiddle.Size = new System.Drawing.Size(1062, 544);
            this.panelMiddle.TabIndex = 5;
            // 
            // labelControl3
            // 
            this.labelControl3.Appearance.BackColor = System.Drawing.Color.SteelBlue;
            this.labelControl3.Appearance.Font = new System.Drawing.Font("LBC", 12F);
            this.labelControl3.Appearance.ForeColor = System.Drawing.Color.White;
            this.labelControl3.Appearance.Options.UseBackColor = true;
            this.labelControl3.Appearance.Options.UseFont = true;
            this.labelControl3.Appearance.Options.UseForeColor = true;
            this.labelControl3.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
            this.labelControl3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelControl3.Location = new System.Drawing.Point(0, 0);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Padding = new System.Windows.Forms.Padding(5);
            this.labelControl3.Size = new System.Drawing.Size(1062, 36);
            this.labelControl3.TabIndex = 1;
            this.labelControl3.Text = "Please Select The (Start Page) And The (End Page) To Be the Source Of the Generte" +
    "d Content";
            // 
            // panelTop
            // 
            this.panelTop.BackColor = System.Drawing.Color.SteelBlue;
            this.panelTop.Controls.Add(this.labelControl3);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1062, 39);
            this.panelTop.TabIndex = 6;
            // 
            // PageSelectionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 26F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1062, 673);
            this.Controls.Add(this.panelMiddle);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.panelBottom);
            this.Font = new System.Drawing.Font("LBC", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "PageSelectionForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Pages Selection";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.galleryControl1)).EndInit();
            this.galleryControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.spinFrom.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.spinTo.Properties)).EndInit();
            this.panelBottom.ResumeLayout(false);
            this.panelBottom.PerformLayout();
            this.panelMiddle.ResumeLayout(false);
            this.panelTop.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraBars.Ribbon.GalleryControl galleryControl1;
        private DevExpress.XtraBars.Ribbon.GalleryControlClient galleryControlClient1;
        private DevExpress.XtraEditors.SpinEdit spinFrom;
        private DevExpress.XtraEditors.SpinEdit spinTo;
        private DevExpress.XtraEditors.SimpleButton simpleButton1;
        private System.Windows.Forms.Panel panelBottom;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private System.Windows.Forms.Panel panelMiddle;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private System.Windows.Forms.Panel panelTop;
        private DevExpress.XtraBars.Ribbon.GalleryControlClient galleryControlClient2;
    }
}