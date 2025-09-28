using IDClassificationNew1.IServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text.RegularExpressions;
using Tesseract;

namespace IDClassificationNew1.Services
{
    public class OCRService : IOCRService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _tessdataPath;
        private readonly string _tempFolderPath;
        public OCRService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            _tessdataPath = Path.Combine(Directory.GetCurrentDirectory(), "tessdata");
            _tempFolderPath = Path.Combine(_webHostEnvironment.WebRootPath, "temp");
        }

        public async Task<string> ReadAllTextAsync(IFormFile imageFile)
        {
            var imagePath = await SaveTempImageAsync(imageFile);
            try
            {
                using var engine = new TesseractEngine(_tessdataPath, "ara+eng", EngineMode.Default);
                using var pix = Pix.LoadFromFile(imagePath);
                using var page = engine.Process(pix);
                return page.GetText();
            }
            finally
            {
                DeleteTempFile(imagePath);
            }
        }

        public async Task<string> ReadCropped_MarginXY_TextAsync(/*IFormFile imageFile*/ string grayPath, float marginXRatio, float marginYRatio)
        {
            // //var imagePath = await SaveTempImageAsync(imageFile);
            //var croppedPath = Path.ChangeExtension(Path.GetTempFileName(), ".jpg");
            Directory.CreateDirectory(_tempFolderPath); // يتأكد من وجود المجلد

            var fileName = Guid.NewGuid().ToString() + "_cropped" + ".jpg";
            var croppedPath = Path.Combine(_tempFolderPath, fileName);

            try
            {
                using var image = Image.Load<Rgba32>(grayPath);

                // تحقق من أن الـ ROI داخل أبعاد الصورة
                int imageWidth = image.Width;
                int imageHeight = image.Height;

                // نحسب كم نريد أن نقص من كل جانب
                int marginX = (int)(marginXRatio * imageWidth);
                int marginY = (int)(marginYRatio * imageHeight);

                // أبعاد الصورة بعد القص
                int newWidth = imageWidth - 2 * marginX;
                int newHeight = imageHeight - 2 * marginY;

                // Crop rectangle
                var cropRect = new Rectangle(marginX, marginY, newWidth, newHeight);

                var cropped = image.Clone(ctx => ctx.Crop(cropRect));
                cropped.Save(croppedPath);

                //using var engine = new TesseractEngine(_tessdataPath, "ara+eng", EngineMode.Default);
                using var engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default);
                using var pix = Pix.LoadFromFile(croppedPath);
                using var page = engine.Process(pix);
                return page.GetText();
            }
            finally
            {
                //DeleteTempFile(imagePath);
                DeleteTempFile(croppedPath);
            }
        }

        public async Task<string> ReadCropped_Custom4_TextAsync(/*IFormFile imageFile*/ string grayPath, float xL, float xR, float yU, float yD)
        {
            // //var imagePath = await SaveTempImageAsync(imageFile);
            //var croppedPath = Path.ChangeExtension(Path.GetTempFileName(), ".jpg");
            Directory.CreateDirectory(_tempFolderPath); // يتأكد من وجود المجلد

            var fileName = Guid.NewGuid().ToString() + "_cropped" + ".jpg";
            var croppedPath = Path.Combine(_tempFolderPath, fileName);
            try
            {
                using var image = Image.Load<Rgba32>(grayPath);

                // تحقق من أن الـ ROI داخل أبعاد الصورة
                int imageWidth = image.Width;
                int imageHeight = image.Height;

                // حساب كل الهامش كنسبة
                int left = (int)(xL * imageWidth);
                int right = (int)(xR * imageWidth);
                int top = (int)(yU * imageHeight);
                int bottom = (int)(yD * imageHeight);

                // حساب العرض والارتفاع المتبقي
                int newWidth = imageWidth - left - right;
                int newHeight = imageHeight - top - bottom;

                if (newWidth <= 0 || newHeight <= 0)
                    throw new Exception("Crop dimensions are invalid. Margins too large.");

                var cropRect = new Rectangle(left, top, newWidth, newHeight);

                var cropped = image.Clone(ctx => ctx.Crop(cropRect));
                cropped.Save(croppedPath);

                //using var engine = new TesseractEngine(_tessdataPath, "ara+eng", EngineMode.Default);
                using var engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default);
                using var pix = Pix.LoadFromFile(croppedPath);
                using var page = engine.Process(pix);

                DeleteTempFile(grayPath);
                DeleteTempFile(croppedPath);

                return page.GetText();
            }
            finally
            {

            }
        }

        public async Task<string> ReadGrayTextAsync(IFormFile imageFile)
        {
            var imagePath = await SaveTempImageAsync(imageFile);
            // // var grayPath = Path.ChangeExtension(Path.GetTempFileName(), ".jpg");
            Directory.CreateDirectory(_tempFolderPath); // يتأكد من وجود المجلد

            var fileName = Guid.NewGuid().ToString() + "_gray" + ".jpg";
            var grayPath = Path.Combine(_tempFolderPath, fileName);
            try
            {
                using var image = Image.Load<Rgba32>(imagePath);
                image.Mutate(x => x.Grayscale());
                image.Save(grayPath);
                return grayPath;
                //int width = image.Width;
                //int height = image.Height;


                //float marginX = 0.25f; // حذف 5% من يسار ويمين 0.05f
                //float marginY = 0.30f; // حذف 20% من فوق وتحت 0.2f
                //string textExtracted = await ReadCropped_MarginXY_TextAsync(grayPath, marginX, marginY);

                float xL = 0.25f; // حذف 5% من يسار 0.05f
                float xR = 0.25f; // حذف 15% من يمين 0.15f
                float yU = 0.20f;  // حذف 20% من فوق  0.2f
                float yD = 0.40f;  // حذف 20% من تحت 0.2f
                string textExtracted = await ReadCropped_Custom4_TextAsync(grayPath, xL,xR,yU,yD);

                var match = Regex.Match(textExtracted, @"\b\d{3}-\d{4}-\d{7}-\d\b");

                string IDNumber = "";
                if (match.Success)
                {
                    IDNumber = match.Value;
                    //Console.WriteLine($"Extracted: {IDNumber}");
                }
                return IDNumber;
                ////using var engine = new TesseractEngine(_tessdataPath, "ara+eng", EngineMode.Default);
                //using var engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default);
                //using var pix = Pix.LoadFromFile(grayPath);
                //using var page = engine.Process(pix);
                //return page.GetText();
            }
            finally
            {
                //DeleteTempFile(imagePath);
                //DeleteTempFile(grayPath);
            }
        }

        // أدوات مساعدة
        private async Task<string> SaveTempImageAsync(IFormFile imageFile)
        {
            Directory.CreateDirectory(_tempFolderPath); // يتأكد من وجود المجلد
            var path = Path.Combine(_tempFolderPath, Path.GetFileName(imageFile.FileName));
            using var stream = new FileStream(path, FileMode.Create);
            await imageFile.CopyToAsync(stream);
            return path;
        }

        private void DeleteTempFile(string path)
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }
}

