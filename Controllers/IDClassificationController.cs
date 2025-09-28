using IDClassificationNew1.IServices;
using IDClassificationNew1.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace IDClassificationNew1.Controllers
{
    [Route("/[Controller]/[action]")]
    public class IDClassificationController : Controller
    {
        private readonly IOCRService _iOCRService;
        public IDClassificationController(IOCRService iOCRService)
        {
            _iOCRService = iOCRService;
        }
        [HttpGet]
        public async Task<IActionResult> IDClassification()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> IDClassification(IFormFile file)
        {
            try
            {
                //var file = Request.Form.Files[0];
                if (file == null || file.Length==0) 
                {
                    return View(new IDViewModel { lable = "Invalid File", ProbabilityString = "0" });
                }
                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                MLIDClassification.ModelInput IDClassificationModel = new MLIDClassification.ModelInput()
                {
                    ImageSource = fileBytes,
                };

                // Make a single prediction on the sample data and print results.
                var sortedScoresWithLabel = MLIDClassification.PredictAllLabels(IDClassificationModel);

                var hightestPrediction = sortedScoresWithLabel.FirstOrDefault();
                string probabilityString = $"{hightestPrediction.Value * 100:0.##}%";
                double propapility = hightestPrediction.Value * 100;
                var IDViewModel = new IDViewModel
                {
                    lable = hightestPrediction.Key.ToUpper() ,
                    ProbabilityString = probabilityString,
                    Probability_double = propapility,
                };
                if (hightestPrediction.Key.ToUpper()=="ID" && propapility>93)
                {
                    IDViewModel.done = "OK it's Valid";
                }
                var grayPath = await _iOCRService.ReadGrayTextAsync(file);
                float xL = 0.25f; // حذف 5% من يسار 0.05f
                float xR = 0.25f; // حذف 15% من يمين 0.15f
                float yU = 0.20f;  // حذف 20% من فوق  0.2f
                float yD = 0.40f;  // حذف 20% من تحت 0.2f
                string textExtracted = await _iOCRService.ReadCropped_Custom4_TextAsync(grayPath, xL, xR, yU, yD);

                var match = Regex.Match(textExtracted, @"\b\d{3}-\d{4}-\d{7}-\d\b");

                string IDNumber = "";
                if (match.Success)
                {
                    IDNumber = match.Value;
                    //Console.WriteLine($"Extracted: {IDNumber}");
                }
                //return IDNumber;
                IDViewModel.textExtracted = IDNumber;
                return View(IDViewModel);
            }
            catch (Exception ex)
            {
                return View(new IDViewModel { lable = ex.ToString() // أو ex.StackTrace
                                    , ProbabilityString = "0" });
                }

        }

    }
}
