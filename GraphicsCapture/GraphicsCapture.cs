using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

using SharpDX.Direct3D11;
using SharpDX.DXGI;

using GraphicsCapture.Interop;

using Device = SharpDX.Direct3D11.Device;
using GraphicsCapture.Interfaces;
using System.Drawing;
using SharpDX;

namespace GraphicsCapture
{
    internal class GraphicsCapture : ICaptureMethod
    {
        private static readonly Guid _graphicsCaptureItemIid = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760");
        private Direct3D11CaptureFramePool _captureFramePool;
        private GraphicsCaptureItem _captureItem;
        private GraphicsCaptureSession _captureSession;

        private Device _device;

        private Windows.Graphics.SizeInt32 windowSize;

        private Texture2D _lastFrameAsTexture2D;

        public GraphicsCapture()
        {
            IsCapturing = false;
        }

        public bool IsCapturing { get; private set; }

        public void Dispose()
        {
            StopCapture();
        }

        public void StartCapture(IntPtr hWnd, Device device, Factory factory)
        {

            _device = device;

            #region GraphicsCapturePicker version

            /*
            var capturePicker = new GraphicsCapturePicker();

            // ReSharper disable once PossibleInvalidCastException
            // ReSharper disable once SuspiciousTypeConversion.Global
            var initializer = (IInitializeWithWindow)(object)capturePicker;
            initializer.Initialize(hWnd);

            _captureItem = capturePicker.PickSingleItemAsync().AsTask().Result;
            */

            #endregion

            #region Window Handle version

            var capturePicker = new WindowPicker();
            var captureHandle = capturePicker.PickCaptureTarget(hWnd);
            if (captureHandle == IntPtr.Zero)
                return;

            _captureItem = CreateItemForWindow(captureHandle);

            #endregion

            if (_captureItem == null)
                return;

            _captureItem.Closed += CaptureItemOnClosed;

            var hr = NativeMethods.CreateDirect3D11DeviceFromDXGIDevice(device.NativePointer, out var pUnknown);
            if (hr != 0)
            {
                StopCapture();
                return;
            }

            var winrtDevice = (IDirect3DDevice) Marshal.GetObjectForIUnknown(pUnknown);
            Marshal.Release(pUnknown);

            _captureFramePool = Direct3D11CaptureFramePool.Create(winrtDevice, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, _captureItem.Size);
            _captureSession = _captureFramePool.CreateCaptureSession(_captureItem);
            _captureSession.StartCapture();
            windowSize = _captureItem.Size;
            IsCapturing = true;
        }

        public Texture2D TryGetNextFrameAsTexture2D(Device device)
        {
            using var frame = _captureFramePool?.TryGetNextFrame();
            if (frame == null)
                return null;

            windowSize = frame.ContentSize;

            // ReSharper disable once SuspiciousTypeConversion.Global
            var surfaceDxgiInterfaceAccess = (IDirect3DDxgiInterfaceAccess) frame.Surface;
            var pResource = surfaceDxgiInterfaceAccess.GetInterface(new Guid("dc8e63f3-d12b-4952-b47b-5e45026a862d"));

            using var surfaceTexture = new Texture2D(pResource); // shared resource
            var texture2dDescription = new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Height = surfaceTexture.Description.Height,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                Width = surfaceTexture.Description.Width
            };
            var texture2d = new Texture2D(device, texture2dDescription);
            device.ImmediateContext.CopyResource(surfaceTexture, texture2d);

            _lastFrameAsTexture2D = texture2d;

            return texture2d;
        }

        public bool BitmapFromTexture(Device device, System.Drawing.Bitmap fastBitmap, Texture2D texture)
        {
            if (texture == null)
                return false;

            Rectangle rect = new Rectangle(0, 0, fastBitmap.Width, fastBitmap.Height);
            System.Drawing.Imaging.BitmapData bmpData = fastBitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, fastBitmap.PixelFormat);


            using (Texture2D stage = CreateStagingTexture(device, bmpData.Width, bmpData.Height))
            {

                device.ImmediateContext.CopyResource(texture, stage);
                DataBox dataBox = device.ImmediateContext.MapSubresource(stage, 0, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out DataStream dsIn);
                int dx = dataBox.RowPitch - bmpData.Stride;
                try
                {
                    using DataStream dsOut = new DataStream(bmpData.Scan0, bmpData.Stride * bmpData.Height, false, true);
                    for (int r = 0; r < bmpData.Height; r++)
                    {
                        dsOut.WriteRange<byte>(dsIn.ReadRange<byte>(bmpData.Stride));
                        dsIn.Position += dx;
                    }
                }
                finally
                {
                    device.ImmediateContext.UnmapSubresource(stage, 0);
                }
                dsIn.Dispose();
            }
            return true;
        }

        public Texture2D CreateStagingTexture(Device device, int width, int height)
        {
            // Create Staging texture CPU-accessible
            var textureDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };

            return new Texture2D(device, textureDesc);
        }

        public void StopCapture() // ...or release resources
        {
            _captureSession?.Dispose();
            _captureFramePool?.Dispose();
            _captureSession = null;
            _captureFramePool = null;
            _captureItem = null;
            IsCapturing = false;
        }

        public Windows.Graphics.SizeInt32 GetWindowSize()
        {
            return windowSize;
        }

        // ReSharper disable once SuspiciousTypeConversion.Global
        private static GraphicsCaptureItem CreateItemForWindow(IntPtr hWnd)
        {
            var factory = WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem));
            var interop = (IGraphicsCaptureItemInterop) factory;
            var pointer = interop.CreateForWindow(hWnd, typeof(GraphicsCaptureItem).GetInterface("IGraphicsCaptureItem").GUID);
            var capture = Marshal.GetObjectForIUnknown(pointer) as GraphicsCaptureItem;
            Marshal.Release(pointer);

            return capture;
        }

        private void CaptureItemOnClosed(GraphicsCaptureItem sender, object args)
        {
            StopCapture();
        }

        public Texture2D TryGetLastFrameAsTexture2D()
        {
            return _lastFrameAsTexture2D;
        }

        public bool TryGetLastFrameAsBitmap(System.Drawing.Bitmap bitmap)
        {
            return BitmapFromTexture(_device, bitmap, _lastFrameAsTexture2D);
        }
    }
}