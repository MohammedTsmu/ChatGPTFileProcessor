using DevExpress.Utils;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraBars.Ribbon.Gallery;
using DevExpress.XtraSplashScreen;// Overlay form
using PdfiumViewer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;


namespace ChatGPTFileProcessor
{
    public partial class PageSelectionForm : Form
    {
        // --- Public API ---
        public int FromPage { get { return (int)spinFrom.Value; } }
        public int ToPage { get { return (int)spinTo.Value; } }

        // داخل الكلاس PageSelectionForm
        private IOverlaySplashScreenHandle _overlayHandle;
        private const int BigListThreshold = 200; // لو عدد العناصر ≥ 200 نعرض Overlay أثناء التظليل

        private bool _overlayScheduled;
        private Control _overlayTarget;
        public string PendingPdfPath { get; set; }
        private bool _firstShownDone;


        /// <summary>Re-initialize the range and visuals (useful when reopening the dialog).</summary>
        public void InitializeSelection(int from, int to)
        {
            this.HideBusyOverlay(); // تأكد من إخفاء الـOverlay لو كان ظاهر


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

        ///// <summary>Load PDF preview thumbnails (async, cancellable). Keeps UI responsive.</summary>
        public async void LoadPdfPreview(string filePath)
        {
            CancelAndDisposeThumbnails();

            _thumbCts = new System.Threading.CancellationTokenSource();
            var token = _thumbCts.Token;

            // مظهر الـGallery
            var g = galleryControl1.Gallery;
            g.ItemImageLayout = DevExpress.Utils.Drawing.ImageLayoutMode.ZoomInside;
            g.ImageSize = new Size(240, 320);
            g.ShowGroupCaption = false;
            g.ShowItemText = true;
            g.ShowItemImage = true;
            g.ItemCheckMode = ItemCheckMode.Multiple;
            g.AllowAllUp = true;
            g.Groups.Clear();
            galleryControl1.Gallery.BackColor = Color.FromArgb(0xF5, 0xF5, 0xF5);

            var group = new GalleryItemGroup();
            g.Groups.Add(group);

            //ShowBusyOverlay(galleryControl1); // ⟵ إظهار overlay أثناء التحميل
            if (galleryControl1.IsHandleCreated && galleryControl1.Visible)
                //ShowBusyOverlay(galleryControl1);
                EnsureOverlayNowOrWhenShown(galleryControl1);
            else if (this.IsHandleCreated && this.Visible)
                EnsureOverlayNowOrWhenShown(this);


            List<Image> images = null;
            try
            {
                images = await System.Threading.Tasks.Task.Run(delegate
                {
                    var pages = new List<Image>();
                    using (var document = PdfDocument.Load(filePath))
                    {
                        int pageCount = document.PageCount;
                        for (int i = 0; i < pageCount; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            var img = document.Render(i, 144, 144, true); // DPI مناسب للمصغرات
                            pages.Add(img);
                        }
                    }
                    return pages;
                }, token);
            }
            catch (OperationCanceledException) { HideBusyOverlay(); return; }
            catch (Exception ex)
            {
                HideBusyOverlay();
                MessageBox.Show(this, "Failed to render PDF preview:\n" + ex.Message, "Preview Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (images == null || images.Count == 0) { HideBusyOverlay(); return; }

            // إضافة العناصر دفعة واحدة بدون إعادة رسم لكل عنصر
            g.BeginUpdate(); // ⟵ تسريع التحديثات الدُفعية
            try
            {
                _loadedImages.AddRange(images);
                for (int i = 0; i < images.Count; i++)
                {
                    var item = new GalleryItem
                    {
                        Image = images[i],
                        Caption = "Page " + (i + 1).ToString(),
                        Tag = i + 1,
                        Hint = "Click to mark range (first click = From, second click = To)"
                    };
                    item.AppearanceCaption.Normal.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                    item.AppearanceCaption.Normal.ForeColor = Color.DimGray;
                    group.Items.Add(item);
                }
            }
            finally
            {
                g.EndUpdate(); // ⟵ تطبيق كل التغييرات مرة وحدة
            }

            // ضبط SpinEdits
            spinFrom.Properties.MinValue = 1;
            spinTo.Properties.MinValue = 1;
            spinFrom.Properties.MaxValue = images.Count;
            spinTo.Properties.MaxValue = images.Count;
            if (spinFrom.Value < 1) spinFrom.Value = 1;
            if (spinTo.Value < 1) spinTo.Value = images.Count;

            UpdateRangeVisuals();

            HideBusyOverlay(); // ⟵ انتهى التحميل
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

            // احسب عدد العناصر لتقرير إن كنا نعرض Overlay
            int total = 0;
            foreach (GalleryItemGroup gg in galleryControl1.Gallery.Groups)
                total += gg.Items.Count;

            bool showBusy = total >= BigListThreshold;
            //if (showBusy) ShowBusyOverlay(galleryControl1);
            if (showBusy)
            {
                if (galleryControl1.IsHandleCreated && galleryControl1.Visible)
                    //ShowBusyOverlay(galleryControl1);
                    EnsureOverlayNowOrWhenShown(galleryControl1);
                else if (this.IsHandleCreated && this.Visible)
                    ShowBusyOverlay(this);
            }


            var g = galleryControl1.Gallery;
            g.BeginUpdate(); // ⟵ تحديث جماعي سريع
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
                if (showBusy) HideBusyOverlay();
            }

            // مرّر أول عنصر محدد إلى الأعلى (التوقيع يتطلب 3 معاملات)
            GalleryItem first = GetFirstCheckedItem();
            if (first != null)
                g.ScrollTo(first, true, VertAlignment.Top);
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


        private void ShowBusyOverlay(Control over = null)
        {
            if (_overlayHandle != null) return;

            Control target = over ?? galleryControl1;

            // إن كان الهدف غير جاهز، جرّب الفورم نفسه
            if (target == null || !target.IsHandleCreated || !target.Visible)
                target = (this.IsHandleCreated && this.Visible) ? (Control)this : null;

            // إن ماكو هدف جاهز حالياً، لا نعرض Overlay الآن (نتجنب الاستثناء)
            if (target == null) return;

            _overlayHandle = SplashScreenManager.ShowOverlayForm(target);
        }


        private void HideBusyOverlay()
        {
            if (_overlayHandle != null)
            {
                DevExpress.XtraSplashScreen.SplashScreenManager.CloseOverlayForm(_overlayHandle);
                _overlayHandle = null;
            }
            _overlayScheduled = false;
            this.Shown -= OnFormFirstShownForOverlay;
        }


        private void EnsureOverlayNowOrWhenShown(Control prefer)
        {
            if (_overlayHandle != null || _overlayScheduled) return;

            Control target = prefer ?? galleryControl1;

            // إذا الهدف جاهز الآن، اعرض مباشرة
            if (target != null && target.IsHandleCreated && target.Visible)
            {
                _overlayHandle = DevExpress.XtraSplashScreen.SplashScreenManager.ShowOverlayForm(target);
                return;
            }

            // جرّب النموذج نفسه إن كان جاهزًا
            if (this.IsHandleCreated && this.Visible)
            {
                _overlayHandle = DevExpress.XtraSplashScreen.SplashScreenManager.ShowOverlayForm(this);
                return;
            }

            // ليس جاهزًا: أجّل حتى أول Shown
            _overlayScheduled = true;
            _overlayTarget = target;
            this.Shown += OnFormFirstShownForOverlay;
        }

        private void OnFormFirstShownForOverlay(object sender, EventArgs e)
        {
            this.Shown -= OnFormFirstShownForOverlay;
            _overlayScheduled = false;

            var target = _overlayTarget ?? this;
            if (_overlayHandle == null)
            {
                if (target != null && target.IsHandleCreated && target.Visible)
                    _overlayHandle = DevExpress.XtraSplashScreen.SplashScreenManager.ShowOverlayForm(target);
                else if (this.IsHandleCreated && this.Visible)
                    _overlayHandle = DevExpress.XtraSplashScreen.SplashScreenManager.ShowOverlayForm(this);
            }
            _overlayTarget = null;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_firstShownDone) return;          // حماية لو انعرضت مرة ثانية
            _firstShownDone = true;

            if (!string.IsNullOrEmpty(PendingPdfPath))
            {
                // نؤجّل خطوة واحدة على UI loop حتى تتأكد كل العناصر اتّرسَمت
                this.BeginInvoke(new Action(() =>
                {
                    // الآن الـForm والـGallery مرئيان ولديهما Handle => Overlay يظهر
                    EnsureOverlayNowOrWhenShown(galleryControl1);
                    LoadPdfPreview(PendingPdfPath);
                }));
            }
        }

    }
}