using System;

namespace GraphicsCapture
{
    internal static class Program
    {
        [STAThread]
        public static void Main()
        {
            using var window = new DxWindow("LoL OCR", new GraphicsCapture());
            window.Show();
        }
    }
}