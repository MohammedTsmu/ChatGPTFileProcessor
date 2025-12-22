using DevExpress.Utils;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraBars.Ribbon.Gallery;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using iText.Kernel.Utils;
using PdfiumViewer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatGPTFileProcessor
{
    public partial class PageSelectionForm : XtraForm
    {
        #region Public API

        /// <summary>Gets the selected starting page number (1-based).</summary>
        public int FromPage => (int)spinFrom.Value;

        /// <summary>Gets the selected ending page number (1-based).</summary>
        public int ToPage => (int)spinTo.Value;

        /// <summary>Gets all selected page ranges (supports multi-range in future).</summary>
        public List<PageRange> SelectedRanges => new List<PageRange>
        {
            new PageRange(FromPage, ToPage)
        };

        #endregion

        #region Private Fields

        // Selection state
        private bool _isFirstClick = true;
        private bool _isInitialized = false;

        // PDF rendering
        private CancellationTokenSource _loadCts;
        private readonly List<Image> _thumbnailImages = new List<Image>();
        private PdfDocument _currentDocument;
        private string _currentPdfPath;

        // Overlay/Progress
        private IOverlaySplashScreenHandle _overlayHandle;
        private const int ProgressThreshold = 50; // Show progress for 50+ pages

        // Zoom/Preview
        private ZoomPreviewForm _zoomForm;
        private const int ThumbnailDpi = 144;
        private const int PreviewDpi = 300;

        // Keyboard navigation
        private int _lastSelectedIndex = -1;

        private System.Windows.Forms.Timer _clickTimer;
        private GalleryItem _pendingClickItem;
        private const int DoubleClickDelay = 300; // milliseconds

        #endregion

        #region Constructor & Initialization

        public PageSelectionForm()
        {
            InitializeComponent();
            InitializeEvents();
            InitializeKeyboardShortcuts();
            ConfigureGallery();

            // Initialize click delay timer
            _clickTimer = new System.Windows.Forms.Timer();
            _clickTimer.Interval = DoubleClickDelay;
            _clickTimer.Tick += ClickTimer_Tick;
        }

        private void InitializeEvents()
        {
            // Gallery events
            galleryControl1.Gallery.ItemClick += Gallery_ItemClick;
            galleryControl1.Gallery.ItemDoubleClick += Gallery_ItemDoubleClick;
            galleryControl1.KeyDown += Gallery_KeyDown;

            // Spin editor events
            spinFrom.EditValueChanged += SpinFrom_ValueChanged;
            spinTo.EditValueChanged += SpinTo_ValueChanged;

            // Form events
            this.Load += PageSelectionForm_Load;
            this.FormClosing += PageSelectionForm_FormClosing;
            this.KeyDown += PageSelectionForm_KeyDown;
        }

        private void InitializeKeyboardShortcuts()
        {
            // Enable key preview so form receives key events first
            this.KeyPreview = true;

            // Handle keys directly in form's KeyDown event
            this.KeyDown += PageSelectionForm_KeyDown;
        }

        private void PageSelectionForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Don't handle if user is typing in spin editor
            if (spinFrom.Focused || spinTo.Focused)
                return;

            // Ctrl+A = Select All
            if (e.Control && e.KeyCode == Keys.A)
            {
                SelectAllPages();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            // Escape = Clear Selection (only if not already handled by dialog)
            if (e.KeyCode == Keys.Escape && this.DialogResult == DialogResult.None)
            {
                ClearSelection();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
        }

        private void ConfigureGallery()
        {
            var g = galleryControl1.Gallery;
            g.ItemImageLayout = DevExpress.Utils.Drawing.ImageLayoutMode.ZoomInside;  // FIXED
            g.ImageSize = new Size(240, 320);
            g.ShowGroupCaption = false;
            g.ShowItemText = true;
            g.ShowItemImage = true;
            g.ItemCheckMode = ItemCheckMode.Multiple;
            g.AllowAllUp = true;
            g.BackColor = Color.FromArgb(0xF5, 0xF5, 0xF5);
            // Removed HoverMode - not available in this version
        }

        private void PageSelectionForm_Load(object sender, EventArgs e)
        {
            _isInitialized = true;
        }

        #endregion

        #region Public Methods - PDF Loading

        /// <summary>
        /// Loads PDF preview asynchronously with progress feedback.
        /// </summary>
        public async Task LoadPdfPreviewAsync(string filePath)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!System.IO.File.Exists(filePath))
                throw new System.IO.FileNotFoundException("PDF file not found", filePath);

            // Cancel any existing load operation
            await CancelCurrentLoadAsync();

            // Cleanup previous data
            CleanupPreviousData();

            _currentPdfPath = filePath;
            _loadCts = new CancellationTokenSource();
            var token = _loadCts.Token;

            // Setup gallery
            var g = galleryControl1.Gallery;
            g.Groups.Clear();
            var group = new GalleryItemGroup();
            g.Groups.Add(group);

            try
            {
                // Load PDF document
                _currentDocument = PdfDocument.Load(filePath);
                int totalPages = _currentDocument.PageCount;

                // Show progress if many pages
                bool useProgress = totalPages >= ProgressThreshold;
                IProgress<int> progress = null;

                if (useProgress)
                {
                    var progressForm = new ProgressForm(totalPages);
                    progressForm.Show(this);
                    progress = new Progress<int>(page =>
                    {
                        if (progressForm.IsDisposed) return;
                        progressForm.UpdateProgress(page, $"Loading page {page} of {totalPages}...");
                    });
                }
                else
                {
                    ShowOverlay();
                }

                // Render thumbnails in background
                var thumbnails = await Task.Run(() =>
                    RenderThumbnails(_currentDocument, totalPages, token, progress), token);

                // Add to gallery
                g.BeginUpdate();
                try
                {
                    _thumbnailImages.AddRange(thumbnails);

                    for (int i = 0; i < thumbnails.Count; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        var item = new GalleryItem
                        {
                            Image = thumbnails[i],
                            Caption = $"Page {i + 1}",
                            Tag = i + 1,
                            Hint = "Click to select range start/end\nDouble-click to preview\nUse arrow keys to navigate"
                        };

                        // Styling
                        item.AppearanceCaption.Normal.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                        item.AppearanceCaption.Normal.ForeColor = Color.FromArgb(0x2C, 0x3E, 0x50);

                        group.Items.Add(item);
                    }
                }
                finally
                {
                    g.EndUpdate();
                }

                // Configure spin editors
                spinFrom.Properties.MinValue = 1;
                spinFrom.Properties.MaxValue = totalPages;
                spinTo.Properties.MinValue = 1;
                spinTo.Properties.MaxValue = totalPages;

                if (spinFrom.Value < 1) spinFrom.Value = 1;
                if (spinTo.Value < 1 || spinTo.Value > totalPages) spinTo.Value = totalPages;

                // Update visuals
                await UpdateRangeVisualsAsync();

                // Update file info label
                UpdateFileInfoLabel(filePath, totalPages);

                // Close progress
                if (useProgress)
                {
                    foreach (Form f in this.OwnedForms)
                    {
                        if (f is ProgressForm pf)
                        {
                            pf.Close();
                            break;
                        }
                    }
                }
                else
                {
                    HideOverlay();
                }
            }
            catch (OperationCanceledException)
            {
                CleanupPreviousData();
                HideOverlay();
            }
            catch (Exception ex)
            {
                HideOverlay();
                XtraMessageBox.Show(this,
                    $"Failed to load PDF preview:\n\n{ex.Message}",
                    "PDF Load Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                CleanupPreviousData();
                throw;
            }
        }

        private List<Image> RenderThumbnails(
            PdfDocument document,
            int pageCount,
            CancellationToken token,
            IProgress<int> progress)
        {
            var thumbnails = new List<Image>(pageCount);

            for (int i = 0; i < pageCount; i++)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    var img = document.Render(i, ThumbnailDpi, ThumbnailDpi, true);
                    thumbnails.Add(img);
                    progress?.Report(i + 1);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to render page {i + 1}: {ex.Message}");

                    var placeholder = new Bitmap(240, 320);
                    using (var gfx = Graphics.FromImage(placeholder))
                    {
                        gfx.Clear(Color.LightGray);
                        gfx.DrawString($"Page {i + 1}\nRender Error",
                            new Font("Arial", 12), Brushes.Red, new PointF(10, 150));
                    }
                    thumbnails.Add(placeholder);
                }
            }

            return thumbnails;
        }

        #endregion

        #region Public Methods - Selection Management

        public void InitializeSelection(int from, int to)
        {
            if (spinFrom.Properties.MaxValue <= 0) return;

            int maxPage = (int)spinTo.Properties.MaxValue;
            from = Math.Max(1, Math.Min(from, maxPage));
            to = Math.Max(1, Math.Min(to, maxPage));

            if (from > to)
            {
                int temp = from;
                from = to;
                to = temp;
            }

            spinFrom.Value = from;
            spinTo.Value = to;

            _ = UpdateRangeVisualsAsync();
            _isFirstClick = true;
        }

        public void SelectAllPages()
        {
            if (spinTo.Properties.MaxValue <= 0) return;

            spinFrom.Value = 1;
            spinTo.Value = spinTo.Properties.MaxValue;

            _ = UpdateRangeVisualsAsync();
        }

        public void ClearSelection()
        {
            var g = galleryControl1.Gallery;
            g.BeginUpdate();
            try
            {
                foreach (GalleryItemGroup gg in g.Groups)
                    foreach (GalleryItem it in gg.Items)
                        it.Checked = false;
            }
            finally
            {
                g.EndUpdate();
            }

            spinFrom.Value = 1;
            spinTo.Value = 1;
            _isFirstClick = true;
        }

        #endregion

        #region Event Handlers - Gallery


        private void Gallery_ItemClick(object sender, GalleryItemClickEventArgs e)
        {
            if (e.Item?.Tag == null) return;

            // Stop any pending single-click action
            _clickTimer.Stop();

            // Store the clicked item
            _pendingClickItem = e.Item;

            // Start timer - if no double-click happens in 300ms, process as single click
            _clickTimer.Start();
        }

        private void ClickTimer_Tick(object sender, EventArgs e)
        {
            _clickTimer.Stop();

            if (_pendingClickItem == null) return;

            // Process as single click (selection)
            int clickedPage = (int)_pendingClickItem.Tag;

            if (_isFirstClick)
            {
                spinFrom.Value = clickedPage;
                _isFirstClick = false;
                _lastSelectedIndex = GetItemIndex(_pendingClickItem);
            }
            else
            {
                spinTo.Value = clickedPage;
                _isFirstClick = true;
                _lastSelectedIndex = -1;
            }

            // Auto-swap if from > to
            if (spinFrom.Value > spinTo.Value)
            {
                decimal temp = spinFrom.Value;
                spinFrom.Value = spinTo.Value;
                spinTo.Value = temp;
            }

            _ = UpdateRangeVisualsAsync();

            _pendingClickItem = null;
        }

        private void Gallery_ItemDoubleClick(object sender, GalleryItemClickEventArgs e)
        {
            // Cancel any pending single-click action
            _clickTimer.Stop();
            _pendingClickItem = null;

            if (e.Item?.Tag == null) return;

            int pageNumber = (int)e.Item.Tag;
            ShowPagePreview(pageNumber);
        }

        private void Gallery_KeyDown(object sender, KeyEventArgs e)
        {
            var items = GetAllItems().ToList();
            if (items.Count == 0) return;

            int currentIndex = _lastSelectedIndex >= 0 ? _lastSelectedIndex : 0;

            switch (e.KeyCode)
            {
                case Keys.Right:
                case Keys.Down:
                    currentIndex = Math.Min(currentIndex + 1, items.Count - 1);
                    SelectItemByIndex(currentIndex, e.Shift);
                    e.Handled = true;
                    break;

                case Keys.Left:
                case Keys.Up:
                    currentIndex = Math.Max(currentIndex - 1, 0);
                    SelectItemByIndex(currentIndex, e.Shift);
                    e.Handled = true;
                    break;

                case Keys.Home:
                    SelectItemByIndex(0, e.Shift);
                    e.Handled = true;
                    break;

                case Keys.End:
                    SelectItemByIndex(items.Count - 1, e.Shift);
                    e.Handled = true;
                    break;

                case Keys.Space:
                    if (currentIndex >= 0 && currentIndex < items.Count)
                    {
                        var item = items[currentIndex];
                        int pageNum = (int)item.Tag;

                        if (_isFirstClick)
                        {
                            spinFrom.Value = pageNum;
                            _isFirstClick = false;
                        }
                        else
                        {
                            spinTo.Value = pageNum;
                            _isFirstClick = true;
                        }

                        _ = UpdateRangeVisualsAsync();
                    }
                    e.Handled = true;
                    break;

                case Keys.Enter:
                    if (currentIndex >= 0 && currentIndex < items.Count)
                    {
                        var item = items[currentIndex];
                        int pageNum = (int)item.Tag;
                        ShowPagePreview(pageNum);
                    }
                    e.Handled = true;
                    break;
            }
        }

        #endregion

        #region Event Handlers - Spin Editors

        private void SpinFrom_ValueChanged(object sender, EventArgs e)
        {
            if (!_isInitialized) return;

            if (spinFrom.Value > spinTo.Value)
            {
                if (spinFrom.Value <= spinTo.Properties.MaxValue)
                    spinTo.Value = spinFrom.Value;
            }

            _ = UpdateRangeVisualsAsync();
        }

        private void SpinTo_ValueChanged(object sender, EventArgs e)
        {
            if (!_isInitialized) return;

            if (spinTo.Value < spinFrom.Value)
            {
                if (spinTo.Value >= spinFrom.Properties.MinValue)
                    spinFrom.Value = spinTo.Value;
            }

            _ = UpdateRangeVisualsAsync();
        }

        #endregion

        #region Visual Update Methods

        private async Task UpdateRangeVisualsAsync()
        {
            int from = FromPage;
            int to = ToPage;

            var g = galleryControl1.Gallery;
            int totalItems = GetAllItems().Count();

            bool showProgress = totalItems >= ProgressThreshold;

            if (showProgress)
                ShowOverlay();

            await Task.Run(() =>
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        g.BeginUpdate();
                        try
                        {
                            foreach (GalleryItemGroup gg in g.Groups)
                            {
                                foreach (GalleryItem it in gg.Items)
                                {
                                    int page = (int)it.Tag;
                                    it.Checked = (page >= from && page <= to);
                                }
                            }
                        }
                        finally
                        {
                            g.EndUpdate();
                        }
                    }));
                }
            });

            if (showProgress)
                HideOverlay();

            ScrollToFirstSelected();
        }

        private void ScrollToFirstSelected()
        {
            var g = galleryControl1.Gallery;

            foreach (GalleryItemGroup gg in g.Groups)
            {
                foreach (GalleryItem it in gg.Items)
                {
                    if (it.Checked)
                    {
                        // FIXED: Use correct VertAlignment enum
                        g.ScrollTo(it, true, DevExpress.Utils.VertAlignment.Top);
                        return;
                    }
                }
            }
        }

        private void UpdateFileInfoLabel(string filePath, int pageCount)
        {
            if (lblFileInfo == null) return;

            var fileInfo = new System.IO.FileInfo(filePath);
            string fileName = fileInfo.Name;
            string fileSize = FormatFileSize(fileInfo.Length);

            lblFileInfo.Text = $"📄 {fileName}  •  {pageCount} pages  •  {fileSize}";
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        #endregion

        #region Preview & Zoom

        private void ShowPagePreview(int pageNumber)
        {
            if (_currentDocument == null) return;
            if (pageNumber < 1 || pageNumber > _currentDocument.PageCount) return;

            try
            {
                var previewImage = _currentDocument.Render(pageNumber - 1, PreviewDpi, PreviewDpi, true);

                if (_zoomForm == null || _zoomForm.IsDisposed)
                {
                    _zoomForm = new ZoomPreviewForm();
                }

                _zoomForm.ShowPreview(previewImage, pageNumber);
                _zoomForm.Show(this);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this,
                    $"Failed to preview page {pageNumber}:\n\n{ex.Message}",
                    "Preview Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Helper Methods

        private IEnumerable<GalleryItem> GetAllItems()
        {
            var g = galleryControl1.Gallery;
            foreach (GalleryItemGroup gg in g.Groups)
                foreach (GalleryItem it in gg.Items)
                    yield return it;
        }

        private int GetItemIndex(GalleryItem item)
        {
            int index = 0;
            foreach (var it in GetAllItems())
            {
                if (it == item) return index;
                index++;
            }
            return -1;
        }

        private void SelectItemByIndex(int index, bool extendSelection)
        {
            var items = GetAllItems().ToList();
            if (index < 0 || index >= items.Count) return;

            var item = items[index];
            int pageNum = (int)item.Tag;

            if (extendSelection && _lastSelectedIndex >= 0)
            {
                int start = Math.Min(_lastSelectedIndex, index);
                int end = Math.Max(_lastSelectedIndex, index);

                spinFrom.Value = (int)items[start].Tag;
                spinTo.Value = (int)items[end].Tag;
            }
            else
            {
                if (_isFirstClick)
                {
                    spinFrom.Value = pageNum;
                    _isFirstClick = false;
                }
                else
                {
                    spinTo.Value = pageNum;
                    _isFirstClick = true;
                }
            }

            _lastSelectedIndex = index;

            // FIXED: Use correct VertAlignment enum
            galleryControl1.Gallery.ScrollTo(item, true, DevExpress.Utils.VertAlignment.Center);

            _ = UpdateRangeVisualsAsync();
        }

        #endregion

        #region Cleanup & Disposal

        private async Task CancelCurrentLoadAsync()
        {
            if (_loadCts != null)
            {
                try
                {
                    _loadCts.Cancel();
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error cancelling load: {ex.Message}");
                }
                finally
                {
                    _loadCts?.Dispose();
                    _loadCts = null;
                }
            }
        }

        private void CleanupPreviousData()
        {
            if (_thumbnailImages.Count > 0)
            {
                foreach (var img in _thumbnailImages)
                {
                    try { img?.Dispose(); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error disposing image: {ex.Message}");
                    }
                }
                _thumbnailImages.Clear();
            }

            if (_currentDocument != null)
            {
                try { _currentDocument.Dispose(); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing PDF document: {ex.Message}");
                }
                _currentDocument = null;
            }

            try
            {
                var g = galleryControl1.Gallery;
                g.BeginUpdate();
                try
                {
                    foreach (GalleryItemGroup gg in g.Groups)
                        gg.Items.Clear();
                    g.Groups.Clear();
                }
                finally
                {
                    g.EndUpdate();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing gallery: {ex.Message}");
            }
        }

        private void PageSelectionForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _ = CancelCurrentLoadAsync();
            CleanupPreviousData();

            if (_zoomForm != null && !_zoomForm.IsDisposed)
            {
                _zoomForm.Close();
                _zoomForm = null;
            }

            // Cleanup timer
            if (_clickTimer != null)
            {
                _clickTimer.Stop();
                _clickTimer.Dispose();
                _clickTimer = null;
            }

            HideOverlay();
        }

        // REMOVED: Dispose override (already in base class)
        // The base XtraForm already has Dispose, so we don't override it

        #endregion

        #region Overlay Management

        private void ShowOverlay()
        {
            if (_overlayHandle != null) return;

            try
            {
                Control target;
                if (galleryControl1.IsHandleCreated && galleryControl1.Visible)
                    target = galleryControl1;
                else
                    target = this;

                if (target.IsHandleCreated && target.Visible)
                {
                    _overlayHandle = SplashScreenManager.ShowOverlayForm(target);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing overlay: {ex.Message}");
            }
        }

        private void HideOverlay()
        {
            if (_overlayHandle != null)
            {
                try
                {
                    SplashScreenManager.CloseOverlayForm(_overlayHandle);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error hiding overlay: {ex.Message}");
                }
                finally
                {
                    _overlayHandle = null;
                }
            }
        }

        #endregion
    }

    #region Supporting Classes

    public class PageRange
    {
        public int From { get; set; }
        public int To { get; set; }

        public PageRange(int from, int to)
        {
            From = from;
            To = to;
        }

        public int PageCount => To - From + 1;

        public override string ToString() => $"Pages {From}-{To}";
    }

    #endregion
}