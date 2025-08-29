using DevExpress.Utils;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraBars.Ribbon.Gallery;
using PdfiumViewer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace ChatGPTFileProcessor
{
    public partial class PageSelectionForm : Form
    {
        // --- Public API ---
        public int FromPage { get { return (int)spinFrom.Value; } }
        public int ToPage { get { return (int)spinTo.Value; } }

        /// <summary>Re-initialize the range and visuals (useful when reopening the dialog).</summary>
        public void InitializeSelection(int from, int to)
        {
            if (spinFrom.Properties.MaxValue <= 0) return;
            int max = (int)spinTo.Properties.MaxValue;
            from = Math.Max(1, Math.Min(from, max));
            to = Math.Max(1, Math.Min(to, max));
            if (from > to) { int t = from; from = to; to = t; }

            spinFrom.Value = from;
            spinTo.Value = to;
            UpdateRangeVisuals();
            _isFirstClick = true;
        }

        /// <summary>Load PDF preview thumbnails (async, cancellable). Keeps UI responsive.</summary>
        public async void LoadPdfPreview(string filePath)
        {
            CancelAndDisposeThumbnails();

            _thumbCts = new CancellationTokenSource();
            var token = _thumbCts.Token;

            // --- Gallery look & feel ---
            var g = galleryControl1.Gallery;
            g.ItemImageLayout = DevExpress.Utils.Drawing.ImageLayoutMode.ZoomInside;
            g.ImageSize = new Size(240, 320);
            g.ShowGroupCaption = false;
            g.ShowItemText = true;
            g.ShowItemImage = true;
            g.ItemCheckMode = ItemCheckMode.Multiple;   // allow multi-check (visual selection)
            g.AllowAllUp = true;
            g.Groups.Clear();

            galleryControl1.Gallery.BackColor = Color.FromArgb(0xF5, 0xF5, 0xF5);

            // --- Create group ---
            var group = new GalleryItemGroup();
            g.Groups.Add(group);

            // --- Generate thumbnails off the UI thread ---
            List<Image> images = null;
            try
            {
                images = await Task.Run(delegate
                {
                    var pages = new List<Image>();
                    using (var document = PdfDocument.Load(filePath))
                    {
                        int pageCount = document.PageCount;
                        for (int i = 0; i < pageCount; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            // Reasonable preview DPI (balance quality/perf)
                            var img = document.Render(i, 144, 144, true);
                            pages.Add(img);
                        }
                    }
                    return pages;
                }, token);
            }
            catch (OperationCanceledException)
            {
                // user changed file or closed form - nothing to do
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to render PDF preview:\n" + ex.Message, "Preview Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (images == null || images.Count == 0) return;

            // --- Populate gallery on UI thread ---
            _loadedImages.AddRange(images);
            for (int i = 0; i < images.Count; i++)
            {
                var item = new GalleryItem();
                item.Image = images[i];
                item.Caption = "Page " + (i + 1).ToString();
                item.Tag = i + 1;
                // Caption styling
                item.AppearanceCaption.Normal.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                item.AppearanceCaption.Normal.ForeColor = Color.DimGray;
                item.Hint = "Click to mark range (first click = From, second click = To)";
                group.Items.Add(item);
            }

            // --- Configure spin editors range ---
            spinFrom.Properties.MinValue = 1;
            spinTo.Properties.MinValue = 1;
            spinFrom.Properties.MaxValue = images.Count;
            spinTo.Properties.MaxValue = images.Count;

            // Default to full range on first load
            if (spinFrom.Value < 1) spinFrom.Value = 1;
            if (spinTo.Value < 1) spinTo.Value = images.Count;

            UpdateRangeVisuals();
        }

        // --- ctor ---
        private bool _isFirstClick = true;
        private CancellationTokenSource _thumbCts;
        private readonly List<Image> _loadedImages = new List<Image>();

        public PageSelectionForm()
        {
            InitializeComponent();

            // Events
            galleryControl1.Gallery.ItemClick += Gallery_ItemClick;
            spinFrom.EditValueChanged += ApplyCheckedRangeFromSpins;
            spinTo.EditValueChanged += ApplyCheckedRangeFromSpins;
        }

        // --- Event handlers ---
        private void Gallery_ItemClick(object sender, GalleryItemClickEventArgs e)
        {
            int clickedPage = (int)e.Item.Tag;
            if (_isFirstClick)
            {
                spinFrom.Value = clickedPage;
                _isFirstClick = false;
            }
            else
            {
                spinTo.Value = clickedPage;
                _isFirstClick = true;
            }

            if (spinFrom.Value > spinTo.Value)
            {
                decimal tmp = spinFrom.Value;
                spinFrom.Value = spinTo.Value;
                spinTo.Value = tmp;
            }

            UpdateRangeVisuals();
        }

        private void ApplyCheckedRangeFromSpins(object sender, EventArgs e)
        {
            UpdateRangeVisuals();
        }

        // --- Visual updates ---
        private void UpdateRangeVisuals()
        {
            int from = (int)spinFrom.Value;
            int to = (int)spinTo.Value;
            if (from > to) { int t = from; from = to; to = t; }

            foreach (GalleryItemGroup gg in galleryControl1.Gallery.Groups)
            {
                foreach (GalleryItem it in gg.Items)
                {
                    int page = (int)it.Tag;
                    it.Checked = (page >= from && page <= to);
                }
            }

            //// Optional: scroll first checked item into view

            GalleryItem first = GetFirstCheckedItem();
            if (first != null)
                galleryControl1.Gallery.ScrollTo(first, true, VertAlignment.Center);  // تمرير المتحركات + محاذاة للأعلى

        }

        private GalleryItem GetFirstCheckedItem()
        {
            foreach (GalleryItemGroup gg in galleryControl1.Gallery.Groups)
                foreach (GalleryItem it in gg.Items)
                    if (it.Checked) return it;
            return null;
        }

        // --- Utilities: Select All / Clear ---
        public void SelectAllPages()
        {
            if (spinTo.Properties.MaxValue <= 0) return;
            spinFrom.Value = 1;
            spinTo.Value = spinTo.Properties.MaxValue;
            UpdateRangeVisuals();
        }

        public void ClearSelection()
        {
            foreach (GalleryItemGroup gg in galleryControl1.Gallery.Groups)
                foreach (GalleryItem it in gg.Items)
                    it.Checked = false;
            spinFrom.Value = 1;
            spinTo.Value = 1;
            _isFirstClick = true;
        }

        // --- Cleanup ---
        private void CancelAndDisposeThumbnails()
        {
            try { if (_thumbCts != null) _thumbCts.Cancel(); }
            catch { /* ignore */ }

            // Dispose previous images to avoid GDI leaks
            if (_loadedImages.Count > 0)
            {
                foreach (var img in _loadedImages) { try { img.Dispose(); } catch { } }
                _loadedImages.Clear();
            }

            // Clear items
            try
            {
                foreach (GalleryItemGroup gg in galleryControl1.Gallery.Groups)
                    gg.Items.Clear();
                galleryControl1.Gallery.Groups.Clear();
            }
            catch { /* ignore */ }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CancelAndDisposeThumbnails();
            base.OnFormClosing(e);
        }
    }
}
