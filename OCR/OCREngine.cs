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
            //_engine.SetVariable("tessedit_do_invert", "0");
        }

        public string GetTextInSubregion(System.Drawing.Bitmap bitmap, System.Drawing.Rectangle subRegion)
        {
            try
            {
                _engine.DefaultPageSegMode = PageSegMode.SingleLine;
                var page = _engine.Process(bitmap, new Rect(subRegion.X, subRegion.Y, subRegion.Width, subRegion.Height));
                string unfilteredOut = page.GetText();
                string cleaned = unfilteredOut.Replace("\n", "").Replace("\r", "").Replace(" ", "");
                page.Dispose();
                bitmap.Dispose();
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

        public string GetGoldInBitmap(System.Drawing.Bitmap bitmap)
        {
            //Include some letters that look similar to numbers just incase that helps Tesseract pick up some edge cases
            //I'm not actually sure this is always helpful but more often than not Tesseract will output nothing instead of misreading
            _engine.SetVariable("tessedit_char_whitelist", "0Oo1lI2345S678B9.,k");

            try
            {
                _engine.DefaultPageSegMode = PageSegMode.SingleBlock;
                var page = _engine.Process(bitmap);
                string unfilteredOut = page.GetText();
                string cleaned = unfilteredOut.Replace("\n", "").Replace("\r", "").Replace(" ", "").Replace("O", "0").Replace("o", "0").Replace("I", "1").Replace("l", "1").Replace(",", ".").Replace("S", "5").Replace("B", "8");

                page.Dispose();
                bitmap.Dispose();
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

        public string GetTimeInBitmap(System.Drawing.Bitmap bitmap)
        {
            //Include some letters that look similar to numbers just incase that helps Tesseract pick up some edge cases
            //I'm not actually sure this is always helpful but more often than not Tesseract will output nothing instead of misreading
            _engine.SetVariable("tessedit_char_whitelist", "0Oo1lI2345S678B9:;");

            try
            {
                _engine.DefaultPageSegMode = PageSegMode.SingleBlock;
                var page = _engine.Process(bitmap);
                string unfilteredOut = page.GetText();
                string cleaned = unfilteredOut.Replace("\n", "").Replace("\r", "").Replace(" ", "").Replace("O", "0").Replace("o", "0").Replace("I", "1").Replace("l", "1").Replace(";", ":").Replace("S", "5").Replace("B", "8");
                page.Dispose();
                bitmap.Dispose();
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

        public string GetTeamInBitmap(System.Drawing.Bitmap bitmap)
        {
            //Reduced set of letters to hopefully help Tesseract filter some false positives
            _engine.SetVariable("tessedit_char_whitelist", "8BRuedl1IhsiNnmtaDrgo0O! ");

            try
            {
                _engine.DefaultPageSegMode = PageSegMode.SingleBlock;
                var page = _engine.Process(bitmap);
                string unfilteredOut = page.GetText();
                string cleaned = unfilteredOut.Replace("\n", "").Replace("\r", "").Replace("I", "l").Replace("1", "l").Replace("8", "B").Replace("0", "o").Replace("O", "o").Replace("!", "");
                page.Dispose();
                bitmap.Dispose();
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
