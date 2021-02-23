using System;
using System.Diagnostics;
using Tesseract;

namespace OCR
{
    public class OCREngine
    {

        private TesseractEngine _engine;

        public OCREngine()
        {
            //_engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default, @"./tessdata/configs.txt");
            _engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.LstmOnly);
            _engine.SetVariable("tessedit_char_whitelist", "01234567890OlI.,ko");
            //_engine.SetVariable("tessedit_do_invert", "0");
        }

        public void Debug()
        {
            var testImagePath = "./GoldRed.png";

            var img = Pix.LoadFromFile(testImagePath);


            var text = GetTextInImage(img, new Rect(0, 0, img.Width, img.Height));

            Console.WriteLine("Text (GetText): \r\n{0}", text);



            Console.Write("Press any key to continue . . . ");
            Console.ReadKey(true);
        }


        public string GetTextInImage(Pix image, Rect subRegion)
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

        public string GetTextInSubregion(System.Drawing.Bitmap bitmap, System.Drawing.Rectangle subRegion)
        {
            try
            {
                _engine.DefaultPageSegMode = PageSegMode.SingleLine;
                string unfilteredOut = _engine.Process(bitmap, new Rect(subRegion.X, subRegion.Y, subRegion.Width, subRegion.Height)).GetText();
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

        public string GetTextInBitmap(System.Drawing.Bitmap bitmap)
        {
            try
            {
                _engine.DefaultPageSegMode = PageSegMode.SingleBlock;
                string unfilteredOut = _engine.Process(bitmap).GetText();
                string cleaned = unfilteredOut.Replace("\n", "").Replace("\r", "").Replace(" ", "").Replace("O", "0").Replace("o", "0").Replace("I", "1").Replace("l", "1").Replace(",", ".");
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
