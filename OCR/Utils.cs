using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OCR
{
    public class Utils
    {
        private const int PixelSize = 4;
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

        public static unsafe void ApplyMonoInvert(Bitmap bmp)
        {
            var bitmapdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < bitmapdata.Height; y++)
            {
                byte* destPixels = (byte*)bitmapdata.Scan0 + (y * bitmapdata.Stride);
                for (int x = 0; x < bitmapdata.Width; x++)
                {
                    var xPixelSize = x * PixelSize;
                    byte pixelValue = (destPixels[xPixelSize] == 0) ? 255 : 0;
                    destPixels[xPixelSize] = pixelValue;         // B
                    destPixels[xPixelSize + 1] = pixelValue; // G
                    destPixels[xPixelSize + 2] = pixelValue; // R
                    //destPixels[xPixelSize + 3] = pixelValue; // A
                }
            }
            bmp.UnlockBits(bitmapdata);
        }

        public static unsafe void ApplyInvert(Bitmap bmp)
        {
            var bitmapdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < bitmapdata.Height; y++)
            {
                byte* destPixels = (byte*)bitmapdata.Scan0 + (y * bitmapdata.Stride);
                for (int x = 0; x < bitmapdata.Width; x++)
                {
                    var xPixelSize = x * PixelSize;
                    destPixels[xPixelSize] = (destPixels[xPixelSize] == 0) ? 255 : 0;         // B
                    destPixels[xPixelSize + 1] = (destPixels[xPixelSize + 1] == 0) ? 255 : 0; // G
                    destPixels[xPixelSize + 2] = (destPixels[xPixelSize + 2] == 0) ? 255 : 0; // R
                    //destPixels[xPixelSize + 3] = pixelValue; // A
                }
            }
            bmp.UnlockBits(bitmapdata);
        }

        public static unsafe void ApplyThreshold(int threshhold, Bitmap bmp)
        {

            var bitmapdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < bitmapdata.Height; y++)
            {
                byte* destPixels = (byte*)bitmapdata.Scan0 + (y * bitmapdata.Stride);
                for (int x = 0; x < bitmapdata.Width; x++)
                {
                    var xPixelSize = x * PixelSize;
                    byte isLuminous = (destPixels[xPixelSize] * 0.07 + destPixels[xPixelSize + 1] * 0.72 + destPixels[xPixelSize + 2] * 0.21 > threshhold) ? (byte)255 : (byte)0;
                    destPixels[xPixelSize] = isLuminous;         // B
                    destPixels[xPixelSize + 1] = isLuminous; // G
                    destPixels[xPixelSize + 2] = isLuminous; // R
                    //destPixels[xPixelSize + 3] = isLuminous; // A
                }
            }
            bmp.UnlockBits(bitmapdata);
        }

        public static Bitmap ApplyCrop(Bitmap src, Rectangle cropRect)
        {
            Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(src, new Rectangle(0, 0, target.Width, target.Height),
                                 cropRect,
                                 GraphicsUnit.Pixel);
            }

            return target;
        }

        public static unsafe void BlueTextColorPass(Bitmap bmp)
        {
            var bitmapdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < bitmapdata.Height; y++)
            {
                byte* destPixels = (byte*)bitmapdata.Scan0 + (y * bitmapdata.Stride);
                for (int x = 0; x < bitmapdata.Width; x++)
                {
                    var xPixelSize = x * PixelSize;
                    //Since League has a very blue background, the same approach as for red text cant be used.
                    //Instead, any pixel with a sufficiently large red value will be filtered out 
                    if ((destPixels[xPixelSize + 2] > 7))
                    {
                        destPixels[xPixelSize] = 255;         // B
                        destPixels[xPixelSize + 1] = 255; // G
                        destPixels[xPixelSize + 2] = 255; // R
                    }

                }
            }
            bmp.UnlockBits(bitmapdata);
        }

        public static unsafe void WhiteTextColorPass(Bitmap bmp)
        {
            var bitmapdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < bitmapdata.Height; y++)
            {
                byte* destPixels = (byte*)bitmapdata.Scan0 + (y * bitmapdata.Stride);
                for (int x = 0; x < bitmapdata.Width; x++)
                {
                    var xPixelSize = x * PixelSize;
                    //Since the text received here isnt purely white but is often tinted, just mask very dark pixels. Since the text is so bright, this should be fine
                    if (destPixels[xPixelSize] + destPixels[xPixelSize + 1] + destPixels[xPixelSize + 2] < 180)
                    {
                        destPixels[xPixelSize] = 255;     // B
                        destPixels[xPixelSize + 1] = 255; // G
                        destPixels[xPixelSize + 2] = 255; // R
                    }

                }
            }
            bmp.UnlockBits(bitmapdata);
        }

        public static unsafe void ApplyBrightnessColorMask(Bitmap bmp)
        {
            var bitmapdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);


            short brightestPixel = 0;
            //Find brightest Color in the region
            for (int y = 0; y < bitmapdata.Height; y++)
            {
                byte* destPixels = (byte*)bitmapdata.Scan0 + (y * bitmapdata.Stride);
                for (int x = 0; x < bitmapdata.Width; x++)
                {
                    var xPixelSize = x * PixelSize;
                    var brightness = destPixels[xPixelSize] + destPixels[xPixelSize + 1] + destPixels[xPixelSize + 2];
                    if (brightness > brightestPixel)
                    {
                        brightestPixel = (short) brightness;
                    }
                }
            }

            //Darken from the brightest Pixel to hopefully get the full numbers
            brightestPixel -= 150;

            //Filter Pixels based on brightest Color just calculated
            for (int y = 0; y < bitmapdata.Height; y++)
            {
                byte* destPixels = (byte*)bitmapdata.Scan0 + (y * bitmapdata.Stride);
                for (int x = 0; x < bitmapdata.Width; x++)
                {
                    var xPixelSize = x * PixelSize;
                    if (destPixels[xPixelSize] + destPixels[xPixelSize + 1] + destPixels[xPixelSize + 2] < brightestPixel)
                    {
                        destPixels[xPixelSize] = 255;     // B
                        destPixels[xPixelSize + 1] = 255; // G
                        destPixels[xPixelSize + 2] = 255; // R
                        destPixels[xPixelSize + 3] = 255; // A
                    }
                }
            }


            bmp.UnlockBits(bitmapdata);
        }

        public static unsafe void RedTextColorPass(Bitmap bmp)
        {
            var bitmapdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < bitmapdata.Height; y++)
            {
                byte* destPixels = (byte*)bitmapdata.Scan0 + (y * bitmapdata.Stride);
                for (int x = 0; x < bitmapdata.Width; x++)
                {
                    var xPixelSize = x * PixelSize;
                    //If the Blue or Green component are larger than the Red component or the pixel is very dark, make the pixel white. Output is hopefully red text on white background
                    if ((destPixels[xPixelSize + 2] < destPixels[xPixelSize + 1] || destPixels[xPixelSize + 2] < destPixels[xPixelSize]) || destPixels[xPixelSize + 2] <= 10)
                    {
                        destPixels[xPixelSize] = 255;         // B
                        destPixels[xPixelSize + 1] = 255; // G
                        destPixels[xPixelSize + 2] = 255; // R
                    }

                }
            }
            bmp.UnlockBits(bitmapdata);
        }


        public static Bitmap ApplyUpscale(float upscaleValue, Bitmap bmp)
        {
            return new Bitmap(bmp, new Size((int)(bmp.Width * upscaleValue), (int)(bmp.Height * upscaleValue)));
        }

        public static void ApplyUpscale(float upscaleValue, Rectangle rect)
        {
            rect.X = (int)(rect.X * upscaleValue);
            rect.Y = (int)(rect.Y * upscaleValue);
            rect.Width = (int)(rect.Width * upscaleValue);
            rect.Height = (int)(rect.Height * upscaleValue);
        }

        public static void ApplySharpen(ref Bitmap bmp, int weight)
        {
            ConvolutionMatrix m = new ConvolutionMatrix();
            m.Apply(0);
            m.Pixel = weight;
            m.TopMid = m.MidLeft = m.MidRight = m.BottomMid = -2;
            m.Factor = weight - 8;

            Convolution C = new Convolution();
            C.Matrix = m;
            C.Convolution3x3(ref bmp);
        }

        public static unsafe void ApplyFullOpaque(Bitmap bmp)
        {
            var bitmapdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < bitmapdata.Height; y++)
            {
                byte* destPixels = (byte*)bitmapdata.Scan0 + (y * bitmapdata.Stride);
                for (int x = 0; x < bitmapdata.Width; x++)
                {
                    var xPixelSize = x * PixelSize;
                    //If the Pixel is very transparent, just make it black
                    if (destPixels[xPixelSize + 3] <= 128)
                    {
                        destPixels[xPixelSize] = 0;     // B
                        destPixels[xPixelSize + 1] = 0; // G
                        destPixels[xPixelSize + 2] = 0; // R
                    }
                    //Remove Transparency
                    destPixels[xPixelSize + 3] = 255;   // A
                }
            }
            bmp.UnlockBits(bitmapdata);
        }

        public static unsafe void RemoveGold(Bitmap bmp)
        {
            var bitmapdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < bitmapdata.Height; y++)
            {
                byte* destPixels = (byte*)bitmapdata.Scan0 + (y * bitmapdata.Stride);
                for (int x = 0; x < bitmapdata.Width; x++)
                {
                    var xPixelSize = x * PixelSize;
                    //Remove Gold colors. Try to not remove Brown
                    if ((destPixels[xPixelSize + 2] >= 125 && destPixels[xPixelSize + 2] <= 135) && destPixels[xPixelSize + 1] >= 90 && destPixels[xPixelSize + 1] <= 110 && (destPixels[xPixelSize] < 20))
                    {
                        destPixels[xPixelSize] = 0;     // B
                        destPixels[xPixelSize + 1] = 0; // G
                        destPixels[xPixelSize + 2] = 0; // R
                    }
                }
            }
            bmp.UnlockBits(bitmapdata);
        }

        public static unsafe Color CalculateAverageColor(Bitmap bm)
        {
            int width = bm.Width;
            int height = bm.Height;
            int red = 0;
            int green = 0;
            int blue = 0;
            int minDiversion = 15; // drop pixels that do not differ by at least minDiversion between color values (white, gray or black)
            int dropped = 0; // keep track of dropped pixels
            long[] totals = new long[] { 0, 0, 0 };

            BitmapData srcData = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadOnly, bm.PixelFormat);
            int stride = srcData.Stride;
            IntPtr Scan0 = srcData.Scan0;

            unsafe
            {
                byte* p = (byte*)(void*)Scan0;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int idx = (y * stride) + x * PixelSize;
                        red = p[idx + 2];
                        green = p[idx + 1];
                        blue = p[idx];
                        if (Math.Abs(red - green) > minDiversion || Math.Abs(red - blue) > minDiversion || Math.Abs(green - blue) > minDiversion)
                        {
                            totals[2] += red;
                            totals[1] += green;
                            totals[0] += blue;
                        }
                        else
                        {
                            dropped++;
                        }
                    }
                }
            }

            int count = width * height - dropped;
            if (count == 0)
                count = 1;
            int avgR = (int)(totals[2] / count);
            int avgG = (int)(totals[1] / count);
            int avgB = (int)(totals[0] / count);

            return Color.FromArgb(avgR, avgG, avgB);
        }
    }

    public class ConvolutionMatrix
    {
        public ConvolutionMatrix()
        {
            Pixel = 1;
            Factor = 1;
        }

        public void Apply(int Val)
        {
            TopLeft = TopMid = TopRight = MidLeft = MidRight = BottomLeft = BottomMid = BottomRight = Pixel = Val;
        }

        public int TopLeft { get; set; }

        public int TopMid { get; set; }

        public int TopRight { get; set; }

        public int MidLeft { get; set; }

        public int MidRight { get; set; }

        public int BottomLeft { get; set; }

        public int BottomMid { get; set; }

        public int BottomRight { get; set; }

        public int Pixel { get; set; }

        public int Factor { get; set; }

        public int Offset { get; set; }
    }

    public class Convolution
    {
        public ConvolutionMatrix Matrix { get; set; }
        public void Convolution3x3(ref Bitmap bmp)
        {
            int Factor = Matrix.Factor;

            if (Factor == 0) return;

            int TopLeft = Matrix.TopLeft;
            int TopMid = Matrix.TopMid;
            int TopRight = Matrix.TopRight;
            int MidLeft = Matrix.MidLeft;
            int MidRight = Matrix.MidRight;
            int BottomLeft = Matrix.BottomLeft;
            int BottomMid = Matrix.BottomMid;
            int BottomRight = Matrix.BottomRight;
            int Pixel = Matrix.Pixel;
            int Offset = Matrix.Offset;

            Bitmap TempBmp = (Bitmap)bmp.Clone();

            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData TempBmpData = TempBmp.LockBits(new Rectangle(0, 0, TempBmp.Width, TempBmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                byte* TempPtr = (byte*)TempBmpData.Scan0.ToPointer();

                int Pix = 0;
                int Stride = bmpData.Stride;
                int DoubleStride = Stride * 2;
                int Width = bmp.Width - 2;
                int Height = bmp.Height - 2;
                int stopAddress = (int)ptr + bmpData.Stride * bmpData.Height;

                for (int y = 0; y < Height; ++y)
                    for (int x = 0; x < Width; ++x)
                    {
                        Pix = (((((TempPtr[2] * TopLeft) + (TempPtr[5] * TopMid) + (TempPtr[8] * TopRight)) +
                          ((TempPtr[2 + Stride] * MidLeft) + (TempPtr[5 + Stride] * Pixel) + (TempPtr[8 + Stride] * MidRight)) +
                          ((TempPtr[2 + DoubleStride] * BottomLeft) + (TempPtr[5 + DoubleStride] * BottomMid) + (TempPtr[8 + DoubleStride] * BottomRight))) / Factor) + Offset);

                        if (Pix < 0) Pix = 0;
                        else if (Pix > 255) Pix = 255;

                        ptr[5 + Stride] = (byte)Pix;

                        Pix = (((((TempPtr[1] * TopLeft) + (TempPtr[4] * TopMid) + (TempPtr[7] * TopRight)) +
                              ((TempPtr[1 + Stride] * MidLeft) + (TempPtr[4 + Stride] * Pixel) + (TempPtr[7 + Stride] * MidRight)) +
                              ((TempPtr[1 + DoubleStride] * BottomLeft) + (TempPtr[4 + DoubleStride] * BottomMid) + (TempPtr[7 + DoubleStride] * BottomRight))) / Factor) + Offset);

                        if (Pix < 0) Pix = 0;
                        else if (Pix > 255) Pix = 255;

                        ptr[4 + Stride] = (byte)Pix;

                        Pix = (((((TempPtr[0] * TopLeft) + (TempPtr[3] * TopMid) + (TempPtr[6] * TopRight)) +
                              ((TempPtr[0 + Stride] * MidLeft) + (TempPtr[3 + Stride] * Pixel) + (TempPtr[6 + Stride] * MidRight)) +
                              ((TempPtr[0 + DoubleStride] * BottomLeft) + (TempPtr[3 + DoubleStride] * BottomMid) + (TempPtr[6 + DoubleStride] * BottomRight))) / Factor) + Offset);

                        if (Pix < 0) Pix = 0;
                        else if (Pix > 255) Pix = 255;

                        ptr[3 + Stride] = (byte)Pix;

                        ptr += 3;
                        TempPtr += 3;
                    }
            }

            bmp.UnlockBits(bmpData);
            TempBmp.UnlockBits(TempBmpData);
        }
    }
}
