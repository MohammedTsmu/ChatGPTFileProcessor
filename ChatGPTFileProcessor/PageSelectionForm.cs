using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraBars.Ribbon;
using PdfiumViewer;

namespace ChatGPTFileProcessor
{
    public partial class PageSelectionForm : Form
    {
        public int FromPage => (int)spinFrom.Value;
        public int ToPage => (int)spinTo.Value;
        private bool isFirstClick = true;

        public PageSelectionForm()
        {
            InitializeComponent();

            galleryControl1.Gallery.ItemClick += Gallery_ItemClick;
        }


        private void Gallery_ItemClick(object sender, GalleryItemClickEventArgs e)
        {
            int clickedPage = (int)e.Item.Tag;

            if (isFirstClick)
            {
                spinFrom.Value = clickedPage;
                isFirstClick = false;
            }
            else
            {
                spinTo.Value = clickedPage;
                isFirstClick = true;
            }

            // اجعل spinFrom دائمًا أصغر من spinTo
            if (spinFrom.Value > spinTo.Value)
            {
                var temp = spinFrom.Value;
                spinFrom.Value = spinTo.Value;
                spinTo.Value = temp;
            }
        }


        public void LoadPdfPreview(string filePath)
        {
            // 1) تخطيط الصورة داخل العنصر (ZoomInside)
            galleryControl1.Gallery.ItemImageLayout = DevExpress.Utils.Drawing.ImageLayoutMode.ZoomInside;

            // 2) حجم الصورة داخل كل عنصر (عريـض x ارتفاع)
            // يمكنك تعديل الأرقام حسب احتياجك
            galleryControl1.Gallery.ImageSize = new Size(240, 320);

            // 3) لون الخلفية العام (لون فاتح جداً ليبرز الصور)
            galleryControl1.Gallery.BackColor = Color.FromArgb(0xF5, 0xF5, 0xF5);

            // 4) إظهار شريط التمرير عند الحاجة
            galleryControl1.Gallery.ShowScrollBar = DevExpress.XtraBars.Ribbon.Gallery.ShowScrollBar.Auto;

            // 5) إخفاء عنوان المجموعة لأننا نضيف مجموعة واحدة فقط
            galleryControl1.Gallery.ShowGroupCaption = false;

            // 6) إظهار نص العنصر (العنوان أسفل كل صورة)
            galleryControl1.Gallery.ShowItemText = true;

            // 7) إظهار الصورة داخل العنصر
            galleryControl1.Gallery.ShowItemImage = true;

            // 8) مسح أيّ مجموعات سابقة وإضافة مجموعة جديدة
            galleryControl1.Gallery.Groups.Clear();
            var group = new DevExpress.XtraBars.Ribbon.GalleryItemGroup();
            galleryControl1.Gallery.Groups.Add(group);

            // 9) تحميل صور صفحات الـPDF
            var images = ExtractPdfPageImages(filePath);
            for (int i = 0; i < images.Count; i++)
            {
                // عنوان العنصر: "Page 1", "Page 2", ...
                var item = new DevExpress.XtraBars.Ribbon.GalleryItem
                {
                    Image = images[i],
                    Caption = $"Page {i + 1}"
                };
                
                // ضبط خصائص العنصر
                item.AppearanceCaption.Normal.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                item.AppearanceCaption.Hovered.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                item.AppearanceCaption.Normal.ForeColor = Color.DimGray;

                item.Tag = i + 1;
                group.Items.Add(item);
            }

            // 10) ضبط SpinEdit لاختيار النطاق (من وإلى) بناءً على عدد الصفحات
            spinFrom.Properties.MinValue = 1;
            spinFrom.Properties.MaxValue = images.Count;
            spinTo.Properties.MinValue = 1;
            spinTo.Properties.MaxValue = images.Count;
            spinTo.Value = images.Count;

            //// 11) تحسين مظهر SpinEdit (اختياري)
            //spinFrom.Properties.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            //spinFrom.Properties.Appearance.Options.UseFont = true;
            //spinTo.Properties.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            //spinTo.Properties.Appearance.Options.UseFont = true;
        }


        private List<Image> ExtractPdfPageImages(string filePath)
        {

            List<Image> pages = new List<Image>();
            using (var document = PdfDocument.Load(filePath))
            {
                for (int i = 0; i < document.PageCount; i++)
                {
                    var img = document.Render(i, 150, 150, true);
                    pages.Add(img);
                }
            }
            return pages;
        }


    }
}