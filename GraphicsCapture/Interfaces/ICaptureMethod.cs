using System;

using SharpDX.Direct3D11;
using SharpDX.DXGI;

using Device = SharpDX.Direct3D11.Device;

namespace GraphicsCapture.Interfaces
{
    public interface ICaptureMethod : IDisposable
    {
        bool IsCapturing { get; }

        void StartCapture(IntPtr hWnd, Device device, Factory factory);

        Texture2D TryGetNextFrameAsTexture2D(Device device);

        void StopCapture();

        Windows.Graphics.SizeInt32 GetWindowSize();

        bool BitmapFromTexture(Device device, System.Drawing.Bitmap bitmap, Texture2D texture);

        Texture2D TryGetLastFrameAsTexture2D();
        bool TryGetLastFrameAsBitmap(System.Drawing.Bitmap bitmap);
    }
}