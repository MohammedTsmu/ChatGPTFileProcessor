using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatGPTFileProcessor
{
    internal class PdfProcessing
    {

    }
    private List<Image> ExtractPdfPageImages(string filePath)
        {
            List<Image> pages = new List<Image>();
            using (var document = PdfiumViewer.PdfDocument.Load(filePath))
            {
                for (int i = 0; i < document.PageCount; i++)
                {
                    var img = document.Render(i, 300, 300, true); // ⬅️ Increase DPI for clarity
                    pages.Add(img);
                }
            }
            return pages;
        }

    }
