using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using static Common.Utils;
using static LoLOCRHub.Utils;

namespace LoLOCRHub
{
    public class DataManager
    {
        private CacheFolder cacheFolder;

        public Dictionary<DragonType, Color> dragonHashes;
        public Dictionary<DragonType, Color> dragonHashesESport;
        public Dictionary<string, byte[]> itemHashes;
        public Dictionary<SummonerType, byte[]> summonerHashes;

        public DataManager()
        {
            this.cacheFolder = new CacheFolder(System.AppDomain.CurrentDomain.BaseDirectory + "cache", "dragon_icons\\");

            CalcDragonHashes();
        }

        public void CalcDragonHashes()
        {
            dragonHashes = new Dictionary<DragonType, Color>();
            dragonHashesESport = new Dictionary<DragonType, Color>();
            var icons = Directory.GetFiles(cacheFolder.dragonFolder);
            int i = 0;
            icons.ToList().ForEach((iconPath) =>
            {
                var isESport = false;
                Console.WriteLine("Generating Hash for " + Path.GetFileNameWithoutExtension(iconPath));
                string dragonName = "";
                if (iconPath.EndsWith("Large.png"))
                {
                    isESport = true;
                    dragonName = Path.GetFileNameWithoutExtension(iconPath).Replace("Large", "");
                } else
                {
                    dragonName = Path.GetFileNameWithoutExtension(iconPath);
                }
                if (Enum.TryParse(dragonName, out DragonType iconName))
                {
                    using (var bitmap = new Bitmap(iconPath))
                    {
                        //To match the output of the screencapture, remove transparency
                        //Then do the same step to the result as to the image capture, making the result as similar as possible
                        OCR.Utils.ApplyFullOpaque(bitmap);
                        OCR.Utils.ApplyBrightnessColorMask(bitmap);
                        if (isESport)
                            dragonHashesESport.Add(iconName, CreateColorHash(bitmap));
                        else
                            dragonHashes.Add(iconName, CreateColorHash(bitmap));
                    }
                    i++;
                }
            });

            Console.WriteLine("Dragon Hashes Created: " + i);
        }

        private byte[] CreateHashFromBitmap(Bitmap bmp)
        {
            return BitmapToByteArray(new Bitmap(bmp, new Size(16, 16)));
        }

        private Color CreateColorHash(Bitmap bmp)
        {
            return OCR.Utils.CalculateAverageColor(bmp);
        }

        public List<DragonTypeResult> GetClosestDragonType(Bitmap bmp, bool useEsportsTimers)
        {
            //Filter out background
            OCR.Utils.ApplyBrightnessColorMask(bmp);
            //Create hash from input bitmap
            var hash = CreateColorHash(bmp);
            var result = new List<DragonTypeResult>();
            var currentDrakeComparisons = useEsportsTimers ? dragonHashesESport : dragonHashes;
            currentDrakeComparisons.ToList().ForEach(pair =>
            {
                var dist = GetDistance(hash, pair.Value); 
                var confidence = 1 - (float)((float)dist / (255 * 3));
                var res = new DragonTypeResult(pair.Key, dist, confidence);
                result.Add(res);
                Console.WriteLine(res.type + ": " + res.distance + ", " + res.confidence);
            });
            return result;
        }

        public List<bool> CreateBoolListHash(Bitmap bmp)
        {
            List<bool> lResult = new List<bool>();
            //create new image with 16x16 pixel
            Bitmap bmpMin = new Bitmap(bmp, new Size(16, 16));
            for (int j = 0; j < bmpMin.Height; j++)
            {
                for (int i = 0; i < bmpMin.Width; i++)
                {
                    //reduce colors to true / false. Should make it so the darkened on cd shouldnt impact comparison                
                    lResult.Add(bmpMin.GetPixel(i, j).GetBrightness() < 0.5f);
                }
            }
            return lResult;
        }
    }

    public class Utils
    {
        public struct CacheFolder
        {
            public string cacheLocation;
            public string dragonFolder;

            public CacheFolder(string cacheLocation, string dragonFolder)
            {
                this.cacheLocation = cacheLocation;
                this.dragonFolder = Path.Combine(cacheLocation, dragonFolder);
            }
        }

        public struct DragonTypeResult
        {
            public DragonType type;
            public float distance;
            public float confidence;

            public DragonTypeResult(DragonType type, float distance, float confidence)
            {
                this.type = type;
                this.distance = distance;
                this.confidence = confidence;
            }
        }

        public static int GetDistance(byte[] hash1, byte[] hash2)
        {
            int start, distanceSquared;
            if (Vector.IsHardwareAccelerated)
            {
                var sum = Vector<int>.Zero;
                var vec1 = MemoryMarshal.Cast<byte, Vector<byte>>(hash1);
                var vec2 = MemoryMarshal.Cast<byte, Vector<byte>>(hash2);

                for (int i = 0; i < vec1.Length; i++)
                {
                    // widen and hard cast needed here to avoid overflow problems
                    Vector.Widen(vec1[i], out var l1, out var r1);
                    Vector.Widen(vec2[i], out var l2, out var r2);
                    Vector<short> lt1 = Vector.AsVectorInt16(l1), rt1 = Vector.AsVectorInt16(r1);
                    Vector<short> lt2 = Vector.AsVectorInt16(l2), rt2 = Vector.AsVectorInt16(r2);
                    Vector.Widen(lt1 - lt2, out var dl1, out var dl2);
                    Vector.Widen(rt1 - rt2, out var dr1, out var dr2);
                    sum += (dl1 * dl1) + (dl2 * dl2) + (dr1 * dr1) + (dr2 * dr2);
                }
                start = vec1.Length * Vector<byte>.Count;
                distanceSquared = 0;
                for (int i = 0; i < Vector<int>.Count; i++)
                    distanceSquared += sum[i];
            }
            else
            {
                start = distanceSquared = 0;
            }
            for (int i = start; i < hash1.Length; i++)
            {
                var diff = hash1[i] - hash2[i];
                distanceSquared += diff * diff;
            }
            return distanceSquared;
        }

        public static int GetDistance(List<bool> hash1, List<bool> hash2)
        {
            return hash1.Zip(hash2, (i, j) => i == j).Count(eq => eq);
        }

        public static int GetDistance(Color c1, Color c2)
        {
            var vec = GetDistanceAsVector(c1, c2);
            return (int)vec.X + (int)vec.Y + (int)vec.Z;
        }

        public static Vector3 GetDistanceAsVector(Color c1, Color c2)
        {
            return new Vector3(Math.Abs(c1.B - c2.B), Math.Abs(c1.G - c2.G), Math.Abs(c1.R - c2.R));
        }

        public static byte[] BitmapToByteArray(Bitmap bitmap)
        {

            BitmapData bmpdata = null;

            try
            {
                bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                int numbytes = bmpdata.Stride * bitmap.Height;
                byte[] bytedata = new byte[numbytes];
                IntPtr ptr = bmpdata.Scan0;

                Marshal.Copy(ptr, bytedata, 0, numbytes);

                return bytedata;
            }
            finally
            {
                if (bmpdata != null)
                    bitmap.UnlockBits(bmpdata);
            }
        }
    }
}
