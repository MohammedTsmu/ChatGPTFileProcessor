using DevExpress.XtraEditors;
using System;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace ChatGPTFileProcessor
{
    public partial class ZoomPreviewForm : XtraForm
    {
        private PictureBox pictureBox;
        private PanelControl panelTools;
        private SimpleButton btnZoomIn;
        private SimpleButton btnZoomOut;
        private SimpleButton btnZoomFit;
        private SimpleButton btnZoom100;
        private LabelControl lblPageInfo;
        private LabelControl lblZoom;

        private Image _currentImage;
        private float _zoomLevel = 1.0f;
        private Point _panOffset = Point.Empty;
        private Point _lastMousePos;
        private bool _isPanning;

        private bool _autoFit = true;

        public ZoomPreviewForm()
        {
            InitializeComponent();
            InitializeEvents();
        }

        private void InitializeComponent()
        {
            this.pictureBox = new PictureBox();
            this.panelTools = new PanelControl();
            this.btnZoomIn = new SimpleButton();
            this.btnZoomOut = new SimpleButton();
            this.btnZoomFit = new SimpleButton();
            this.btnZoom100 = new SimpleButton();
            this.lblPageInfo = new LabelControl();
            this.lblZoom = new LabelControl();

            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelTools)).BeginInit();
            this.panelTools.SuspendLayout();
            this.SuspendLayout();

            // 
            // pictureBox
            // 
            this.pictureBox.BackColor = Color.FromArgb(64, 64, 64);
            this.pictureBox.Dock = DockStyle.Fill;
            this.pictureBox.Location = new Point(0, 0);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new Size(800, 600);
            //this.pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureBox.SizeMode = PictureBoxSizeMode.Normal;
            this.pictureBox.TabIndex = 0;
            this.pictureBox.TabStop = false;

            // 
            // panelTools
            // 
            this.panelTools.Controls.Add(this.lblPageInfo);
            this.panelTools.Controls.Add(this.lblZoom);
            this.panelTools.Controls.Add(this.btnZoomIn);
            this.panelTools.Controls.Add(this.btnZoomOut);
            this.panelTools.Controls.Add(this.btnZoomFit);
            this.panelTools.Controls.Add(this.btnZoom100);
            this.panelTools.Dock = DockStyle.Top;
            this.panelTools.Location = new Point(0, 0);
            this.panelTools.Name = "panelTools";
            this.panelTools.Size = new Size(800, 50);
            this.panelTools.TabIndex = 1;

            // 
            // lblPageInfo
            // 
            this.lblPageInfo.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblPageInfo.Appearance.Options.UseFont = true;
            this.lblPageInfo.Location = new Point(15, 15);
            this.lblPageInfo.Name = "lblPageInfo";
            this.lblPageInfo.Size = new Size(60, 19);
            this.lblPageInfo.TabIndex = 0;
            this.lblPageInfo.Text = "Page 1";

            // 
            // lblZoom
            // 
            this.lblZoom.Appearance.Font = new Font("Segoe UI", 9F);
            this.lblZoom.Appearance.ForeColor = Color.Gray;
            this.lblZoom.Appearance.Options.UseFont = true;
            this.lblZoom.Appearance.Options.UseForeColor = true;
            this.lblZoom.Location = new Point(680, 17);
            this.lblZoom.Name = "lblZoom";
            this.lblZoom.Size = new Size(40, 15);
            this.lblZoom.TabIndex = 5;
            this.lblZoom.Text = "100%";

            // 
            // btnZoomIn
            // 
            this.btnZoomIn.ImageOptions.SvgImage = CreatePlusSvg();
            this.btnZoomIn.ImageOptions.SvgImageSize = new Size(16, 16);
            this.btnZoomIn.Location = new Point(250, 10);
            this.btnZoomIn.Name = "btnZoomIn";
            this.btnZoomIn.Size = new Size(90, 30);
            this.btnZoomIn.TabIndex = 1;
            this.btnZoomIn.Text = "Zoom In";

            // 
            // btnZoomOut
            // 
            this.btnZoomOut.ImageOptions.SvgImage = CreateMinusSvg();
            this.btnZoomOut.ImageOptions.SvgImageSize = new Size(16, 16);
            this.btnZoomOut.Location = new Point(350, 10);
            this.btnZoomOut.Name = "btnZoomOut";
            this.btnZoomOut.Size = new Size(90, 30);
            this.btnZoomOut.TabIndex = 2;
            this.btnZoomOut.Text = "Zoom Out";

            // 
            // btnZoomFit
            // 
            this.btnZoomFit.Location = new Point(450, 10);
            this.btnZoomFit.Name = "btnZoomFit";
            this.btnZoomFit.Size = new Size(80, 30);
            this.btnZoomFit.TabIndex = 3;
            this.btnZoomFit.Text = "Fit";

            // 
            // btnZoom100
            // 
            this.btnZoom100.Location = new Point(540, 10);
            this.btnZoom100.Name = "btnZoom100";
            this.btnZoom100.Size = new Size(80, 30);
            this.btnZoom100.TabIndex = 4;
            this.btnZoom100.Text = "100%";

            // 
            // ZoomPreviewForm
            // 
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(800, 650);
            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.panelTools);
            this.KeyPreview = true;
            this.Name = "ZoomPreviewForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Page Preview";

            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelTools)).EndInit();
            this.panelTools.ResumeLayout(false);
            this.panelTools.PerformLayout();
            this.ResumeLayout(false);
        }

        private void InitializeEvents()
        {
            // Zoom buttons
            btnZoomIn.Click += (s, e) => Zoom(1.25f);
            btnZoomOut.Click += (s, e) => Zoom(0.8f);
            btnZoomFit.Click += (s, e) => ZoomToFit();
            btnZoom100.Click += (s, e) => ZoomTo100();

            // Mouse events
            pictureBox.MouseWheel += PictureBox_MouseWheel;
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseUp += PictureBox_MouseUp;
            pictureBox.Paint += PictureBox_Paint;

            // Keyboard events
            this.KeyDown += ZoomPreviewForm_KeyDown;

            // Handle window resize/maximize
            this.Resize += ZoomPreviewForm_Resize;
            this.SizeChanged += ZoomPreviewForm_SizeChanged;
        }

        private void ZoomPreviewForm_Resize(object sender, EventArgs e)
        {
            // If we're in "fit" mode (not manually zoomed), recalculate fit
            // We can track this with a flag
            if (_autoFit && _currentImage != null)
            {
                ZoomToFit();
            }
        }

        private void ZoomPreviewForm_SizeChanged(object sender, EventArgs e)
        {
            // Force repaint when size changes
            if (pictureBox != null)
            {
                pictureBox.Invalidate();
            }
        }

        public void ShowPreview(Image image, int pageNumber)
        {
            if (_currentImage != null && _currentImage != image)
            {
                _currentImage.Dispose();
            }

            _currentImage = image;
            lblPageInfo.Text = $"Page {pageNumber}";

            _autoFit = true;  // Reset to auto-fit for new image
            ZoomToFit();
        }


        private void Zoom(float factor)
        {
            _zoomLevel *= factor;
            _zoomLevel = Math.Max(0.1f, Math.Min(_zoomLevel, 5.0f));

            _autoFit = false;  // User manually zoomed, disable auto-fit

            ApplyZoom();
        }

        private void ZoomToFit()
        {
            if (_currentImage == null) return;

            // Get actual available space (excluding toolbar)
            int availableWidth = pictureBox.ClientSize.Width;
            int availableHeight = pictureBox.ClientSize.Height;

            // Calculate zoom to fit
            float widthRatio = (float)availableWidth / _currentImage.Width;
            float heightRatio = (float)availableHeight / _currentImage.Height;

            _zoomLevel = Math.Min(widthRatio, heightRatio) * 0.95f; // 95% to add small margin
            _panOffset = Point.Empty;

            _autoFit = true;  // We're in auto-fit mode

            ApplyZoom();
        }

        private void ZoomTo100()
        {
            _zoomLevel = 1.0f;
            _panOffset = Point.Empty;

            _autoFit = false;  // User set specific zoom

            ApplyZoom();
        }

        private void ApplyZoom()
        {
            lblZoom.Text = $"{(int)(_zoomLevel * 100)}%";
            pictureBox.Invalidate();
        }

        private void PictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
                Zoom(1.1f);
            else
                Zoom(0.9f);
        }

        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isPanning = true;
                _lastMousePos = e.Location;
                pictureBox.Cursor = Cursors.Hand;
            }
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                int dx = e.X - _lastMousePos.X;
                int dy = e.Y - _lastMousePos.Y;

                _panOffset.X += dx;
                _panOffset.Y += dy;

                _lastMousePos = e.Location;

                pictureBox.Invalidate();
            }
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            _isPanning = false;
            pictureBox.Cursor = Cursors.Default;
        }

        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;

            // Clear entire background
            g.Clear(pictureBox.BackColor);

            if (_currentImage == null) return;

            // High-quality rendering
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            // Calculate zoomed dimensions
            int zoomedWidth = (int)(_currentImage.Width * _zoomLevel);
            int zoomedHeight = (int)(_currentImage.Height * _zoomLevel);

            // Center the image in available space
            int x = (pictureBox.ClientSize.Width - zoomedWidth) / 2 + _panOffset.X;
            int y = (pictureBox.ClientSize.Height - zoomedHeight) / 2 + _panOffset.Y;

            // Draw the image
            g.DrawImage(_currentImage, x, y, zoomedWidth, zoomedHeight);
        }

        private void ZoomPreviewForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Add:
                case Keys.Oemplus:
                    Zoom(1.25f);
                    e.Handled = true;
                    break;

                case Keys.Subtract:
                case Keys.OemMinus:
                    Zoom(0.8f);
                    e.Handled = true;
                    break;

                case Keys.D0:
                    if (e.Control)
                    {
                        ZoomToFit();
                        e.Handled = true;
                    }
                    break;

                case Keys.D1:
                    if (e.Control)
                    {
                        ZoomTo100();
                        e.Handled = true;
                    }
                    break;

                case Keys.Escape:
                    this.Close();
                    e.Handled = true;
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
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

        // Simple SVG icon creation (fallback - you can use real SVG files)
        private DevExpress.Utils.Svg.SvgImage CreatePlusSvg()
        {
            // This is a placeholder - in real code, load from resources
            return null;
        }

        private DevExpress.Utils.Svg.SvgImage CreateMinusSvg()
        {
            // This is a placeholder - in real code, load from resources
            return null;
        }
    }
}

// ========================================
// END OF ZoomPreviewForm.cs
// ========================================
