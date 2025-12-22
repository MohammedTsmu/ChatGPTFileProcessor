// ========================================
// ZoomPreviewForm.cs (UPDATED - NO DESIGNER CODE)
// Full-page preview with zoom and pan capability
// ========================================

using DevExpress.XtraEditors;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChatGPTFileProcessor
{
    public partial class ZoomPreviewForm : XtraForm
    {
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

        private void InitializeEvents()
        {
            btnZoomIn.Click += (s, e) => Zoom(1.25f);
            btnZoomOut.Click += (s, e) => Zoom(0.8f);
            btnZoomFit.Click += (s, e) => ZoomToFit();
            btnZoom100.Click += (s, e) => ZoomTo100();

            this.KeyDown += ZoomPreviewForm_KeyDown;
            this.Resize += ZoomPreviewForm_Resize;
            this.SizeChanged += ZoomPreviewForm_SizeChanged;
        }

        public void ShowPreview(Image image, int pageNumber)
        {
            if (_currentImage != null && _currentImage != image)
            {
                _currentImage.Dispose();
            }

            _currentImage = image;
            lblPageInfo.Text = $"Page {pageNumber}";

            _autoFit = true;
            ZoomToFit();
        }

        private void Zoom(float factor)
        {
            _zoomLevel *= factor;
            _zoomLevel = Math.Max(0.1f, Math.Min(_zoomLevel, 5.0f));

            _autoFit = false;

            ApplyZoom();
        }

        private void ZoomToFit()
        {
            if (_currentImage == null) return;

            float widthRatio = (float)pictureBox.ClientSize.Width / _currentImage.Width;
            float heightRatio = (float)pictureBox.ClientSize.Height / _currentImage.Height;

            _zoomLevel = Math.Min(widthRatio, heightRatio) * 0.95f;
            _panOffset = Point.Empty;

            _autoFit = true;

            ApplyZoom();
        }

        private void ZoomTo100()
        {
            _zoomLevel = 1.0f;
            _panOffset = Point.Empty;

            _autoFit = false;

            ApplyZoom();
        }

        private void ApplyZoom()
        {
            lblZoom.Text = $"{(int)(_zoomLevel * 100)}%";
            pictureBox.Invalidate();
        }

        private void ZoomPreviewForm_Resize(object sender, EventArgs e)
        {
            if (_autoFit && _currentImage != null)
            {
                ZoomToFit();
            }
        }

        private void ZoomPreviewForm_SizeChanged(object sender, EventArgs e)
        {
            pictureBox?.Invalidate();
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

            g.Clear(pictureBox.BackColor);

            if (_currentImage == null) return;

            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            int zoomedWidth = (int)(_currentImage.Width * _zoomLevel);
            int zoomedHeight = (int)(_currentImage.Height * _zoomLevel);

            int x = (pictureBox.ClientSize.Width - zoomedWidth) / 2 + _panOffset.X;
            int y = (pictureBox.ClientSize.Height - zoomedHeight) / 2 + _panOffset.Y;

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
    }
}