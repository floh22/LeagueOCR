using System;
using System.Diagnostics;
using Tesseract;

namespace OCR
{
    public class OCREngine
    {

        private static TesseractEngine _engine;
        public static void Debug()
        {

            _engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default, @"./tessdata/configs.txt");
            var testImagePath = "./GoldRed.png";

            var img = Pix.LoadFromFile(testImagePath);


            var text = GetTextInSubregion(img, new Rect(0, 0, img.Width, img.Height));

            Console.WriteLine("Text (GetText): \r\n{0}", text);



            Console.Write("Press any key to continue . . . ");
            Console.ReadKey(true);
        }


        public static string GetTextInSubregion(Pix image, Rect subRegion)
        {
            try
            {
                string unfilteredOut = _engine.Process(image, subRegion).GetText();
                string cleaned = unfilteredOut.Replace("\n", "").Replace("\r", "").Replace(" ", "");
                return cleaned;
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.WriteLine("Unexpected Error: " + e.Message);
                Console.WriteLine("Details: ");
                Console.WriteLine(e.ToString());
                return "";
            }

        }
    }
}
