using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using ChatGPTFileProcessor.Helpers;
using SDImage = System.Drawing.Image;

namespace ChatGPTFileProcessor.Services
{
    /// <summary>
    /// Service for PDF processing operations including image conversion and compression
    /// </summary>
    public class PdfProcessingService
    {
        private readonly int _fromPage;
        private readonly int _toPage;

        /// <summary>
        /// Initializes a new instance of the PdfProcessingService
        /// </summary>
        /// <param name="fromPage">Starting page number (1-based)</param>
        /// <param name="toPage">Ending page number (1-based)</param>
        public PdfProcessingService(int fromPage, int toPage)
        {
            _fromPage = fromPage;
            _toPage = toPage;
        }

        /// <summary>
        /// Converts PDF pages to images
        /// </summary>
        /// <param name="filePath">Path to the PDF file</param>
        /// <param name="dpi">DPI for rendering (default 300)</param>
        /// <returns>List of tuples containing page number and image</returns>
        public List<(int pageNumber, SDImage image)> ConvertPdfToImages(string filePath, int dpi = Constants.HIGH_DPI)
        {
            var pages = new List<(int, SDImage)>();
            using (var document = PdfiumViewer.PdfDocument.Load(filePath))
            {
                int from = Math.Max(0, _fromPage - 1);
                int to = Math.Min(document.PageCount - 1, _toPage - 1);

                for (int i = from; i <= to; i++)
                {
                    // high DPI (300+) for better image quality
                    var img = document.Render(i, dpi, dpi, true);
                    pages.Add((i + 1, img));
                }
            }
            return pages;
        }

        /// <summary>
        /// Resizes an image for API transmission
        /// </summary>
        /// <param name="src">Source image</param>
        /// <param name="maxWidth">Maximum width (default 1280)</param>
        /// <returns>Resized image</returns>
        public static SDImage ResizeForApi(SDImage src, int maxWidth = 1280)
        {
            if (src.Width <= maxWidth) return new Bitmap(src);
            int newHeight = (int)Math.Round(src.Height * (maxWidth / (double)src.Width));
            var bmp = new Bitmap(maxWidth, newHeight);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(src, 0, 0, maxWidth, newHeight);
            }
            return bmp;
        }

        /// <summary>
        /// Converts image to Base64-encoded JPEG with specified quality
        /// </summary>
        /// <param name="img">Source image</param>
        /// <param name="jpegQuality">JPEG quality (default 85)</param>
        /// <returns>Base64-encoded string</returns>
        public static string ToBase64Jpeg(SDImage img, long jpegQuality = 85L)
        {
            using (var ms = new MemoryStream())
            {
                var enc = ImageCodecInfo.GetImageEncoders()
                    .First(e => e.MimeType == "image/jpeg");
                var ep = new EncoderParameters(1);
                ep.Param[0] = new EncoderParameter(Encoder.Quality, jpegQuality);
                img.Save(ms, enc, ep);
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
}
