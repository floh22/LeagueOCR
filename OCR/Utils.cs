using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCR
{
    class Utils
    {
        public static Bitmap ConvertToBitmap(string fileName)
        {
            Bitmap bitmap;
            using (Stream bmpStream = System.IO.File.Open(fileName, System.IO.FileMode.Open))
            {
                Image image = Image.FromStream(bmpStream);

                bitmap = new Bitmap(image);

            }
            return bitmap;
        }

        public static unsafe void ApplyContrast(double contrast, Bitmap bmp)
        {
            byte[] contrast_lookup = new byte[256];
            double newValue = 0;
            double c = (100.0 + contrast) / 100.0;

            c *= c;

            for (int i = 0; i < 256; i++)
            {
                newValue = (double)i;
                newValue /= 255.0;
                newValue -= 0.5;
                newValue *= c;
                newValue += 0.5;
                newValue *= 255;

                if (newValue < 0)
                    newValue = 0;
                if (newValue > 255)
                    newValue = 255;
                contrast_lookup[i] = (byte)newValue;
            }

            var bitmapdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            int PixelSize = 4;

            for (int y = 0; y < bitmapdata.Height; y++)
            {
                byte* destPixels = (byte*)bitmapdata.Scan0 + (y * bitmapdata.Stride);
                for (int x = 0; x < bitmapdata.Width; x++)
                {
                    var xPixelSize = x * PixelSize;
                    destPixels[xPixelSize] = contrast_lookup[destPixels[xPixelSize]];         // B
                    destPixels[xPixelSize + 1] = contrast_lookup[destPixels[xPixelSize + 1]]; // G
                    destPixels[xPixelSize + 2] = contrast_lookup[destPixels[xPixelSize + 2]]; // R
                    //destPixels[xPixelSize + 3] = contrast_lookup[destPixels[xPixelSize + 3]]; // A
                }
            }
            bmp.UnlockBits(bitmapdata);
        }

        public static unsafe void ApplyThreshold(double threshhold, Bitmap bmp)
        {

            var bitmapdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            int PixelSize = 4;

            for (int y = 0; y < bitmapdata.Height; y++)
            {
                byte* destPixels = (byte*)bitmapdata.Scan0 + (y * bitmapdata.Stride);
                for (int x = 0; x < bitmapdata.Width; x++)
                {
                    var xPixelSize = x * PixelSize;
                    byte isLuminous = (destPixels[xPixelSize] * 0.07 + destPixels[xPixelSize + 1] * 0.72 + destPixels[xPixelSize + 2] * 0.21 > threshhold)? (byte) 255 : (byte) 0;
                    destPixels[xPixelSize] = isLuminous;         // B
                    destPixels[xPixelSize + 1] = isLuminous; // G
                    destPixels[xPixelSize + 2] = isLuminous; // R
                    //destPixels[xPixelSize + 3] = isLuminous; // A
                }
            }
            bmp.UnlockBits(bitmapdata);
        }
    }
}
