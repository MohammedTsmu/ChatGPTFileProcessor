using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraBars.Ribbon.Gallery; // مهم للمكتبة
using PdfiumViewer;
using DevExpress.Utils.Drawing;


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
            //if (!File.Exists("pdfium.dll"))
            //{
            //    MessageBox.Show("Missing 'pdfium.dll'. Please ensure it's placed next to the executable.", "Missing Dependency", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return;
            //}


            galleryControl1.Gallery.ItemImageLayout = ImageLayoutMode.ZoomInside;



            galleryControl1.Gallery.ImageSize = new Size(200, 280);
            galleryControl1.Gallery.Groups.Clear();

            var group = new GalleryItemGroup();
            galleryControl1.Gallery.Groups.Add(group);

            var images = ExtractPdfPageImages(filePath);
            for (int i = 0; i < images.Count; i++)
            {
                var item = new GalleryItem(images[i], $"Page {i + 1}", "");
                item.Tag = i + 1;
                group.Items.Add(item);
            }

            spinFrom.Properties.MinValue = 1;
            spinFrom.Properties.MaxValue = images.Count;
            spinTo.Properties.MinValue = 1;
            spinTo.Properties.MaxValue = images.Count;
            spinTo.Value = images.Count;
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