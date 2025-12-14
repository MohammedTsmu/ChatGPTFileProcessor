using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Microsoft.VSDiagnostics;

namespace ChatGPTFileProcessor.Benchmarks
{
    [CPUUsageDiagnoser]
    public class ImageProcessingBenchmark
    {
        private Image _testImage;
        
        // Cached encoder (optimization)
        private static readonly ImageCodecInfo _jpegEncoder = 
            ImageCodecInfo.GetImageEncoders().First(e => e.MimeType == "image/jpeg");
        
        // Cache for testing
        private Dictionary<int, string> _base64Cache;
        private Dictionary<int, Image> _resizedCache;
        
        [GlobalSetup]
        public void Setup()
        {
            // Create a test image similar to PDF page renders (1280x1600 typical PDF page)
            _testImage = new Bitmap(1280, 1600);
            using (var g = Graphics.FromImage(_testImage))
            {
                g.Clear(Color.White);
                g.DrawString("Test PDF Page Content", 
                    new Font("Arial", 20), 
                    Brushes.Black, 
                    new PointF(100, 100));
            }
            
            _base64Cache = new Dictionary<int, string>();
            _resizedCache = new Dictionary<int, Image>();
        }
        
        [GlobalCleanup]
        public void Cleanup()
        {
            _testImage?.Dispose();
            foreach (var img in _resizedCache.Values)
            {
                img?.Dispose();
            }
        }
        
        [Benchmark(Baseline = true)]
        public string Current_ResizeAndEncode()
        {
            // Simulate current workflow: resize then encode 10 times (for 10 different sections)
            string result = null;
            for (int i = 0; i < 10; i++)
            {
                using (var resized = ResizeForApi(_testImage, 1024))
                {
                    result = ToBase64Jpeg(resized, 80L);
                }
            }
            return result;
        }
        
        [Benchmark]
        public string Optimized_WithCache()
        {
            // Simulates optimized workflow with caching
            string result = null;
            for (int i = 0; i < 10; i++)
            {
                result = GetOrCreateBase64(1, _testImage, 1024, 80L);
            }
            return result;
        }
        
        // Helper methods from Form1.cs
        private static Image ResizeForApi(Image src, int maxWidth = 1280)
        {
            if (src.Width <= maxWidth) return (Image)src.Clone();
            int newHeight = (int)Math.Round(src.Height * (maxWidth / (double)src.Width));
            var bmp = new Bitmap(maxWidth, newHeight);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(src, 0, 0, maxWidth, newHeight);
            }
            return bmp;
        }
        
        private static string ToBase64Jpeg(Image img, long jpegQuality = 85L)
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
        
        // Optimized version using cached encoder
        private static string ToBase64JpegOptimized(Image img, long jpegQuality = 85L)
        {
            using (var ms = new MemoryStream())
            {
                var ep = new EncoderParameters(1);
                ep.Param[0] = new EncoderParameter(Encoder.Quality, jpegQuality);
                img.Save(ms, _jpegEncoder, ep);  // Use cached encoder
                return Convert.ToBase64String(ms.ToArray());
            }
        }
        
        // Cache helper methods
        private string GetOrCreateBase64(int pageNumber, Image sourceImage, int maxWidth, long jpegQuality)
        {
            if (_base64Cache.TryGetValue(pageNumber, out string cached))
                return cached;
            
            Image resized = GetOrCreateResizedImage(pageNumber, sourceImage, maxWidth);
            string base64 = ToBase64JpegOptimized(resized, jpegQuality);
            _base64Cache[pageNumber] = base64;
            
            return base64;
        }
        
        private Image GetOrCreateResizedImage(int pageNumber, Image sourceImage, int maxWidth)
        {
            if (_resizedCache.TryGetValue(pageNumber, out Image cached))
                return cached;
            
            Image resized = ResizeForApi(sourceImage, maxWidth);
            _resizedCache[pageNumber] = resized;
            
            return resized;
        }
    }
}