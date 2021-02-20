//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using Composition.WindowsRuntimeHelpers;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Drawing;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI.Composition;

namespace CaptureSampleCore
{
    public class BasicCapture : IDisposable
    {
        private GraphicsCaptureItem item;
        private Direct3D11CaptureFramePool framePool;
        private GraphicsCaptureSession session;
        private SizeInt32 lastSize;

        private BasicSampleApplication appWindow;

        private IDirect3DDevice device;
        private SharpDX.Direct3D11.Device d3dDevice;
        private SharpDX.DXGI.SwapChain1 swapChain;

        private bool newWindow = true;
        public bool RequestBitmap = false;

        public BasicCapture(IDirect3DDevice d, GraphicsCaptureItem i, BasicSampleApplication a)
        {
            item = i;
            device = d;
            d3dDevice = Direct3D11Helper.CreateSharpDXDevice(device);
            this.appWindow = a;

            var dxgiFactory = new SharpDX.DXGI.Factory2();
            var description = new SharpDX.DXGI.SwapChainDescription1()
            {
                Width = item.Size.Width,
                Height = item.Size.Height,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Stereo = false,
                SampleDescription = new SharpDX.DXGI.SampleDescription()
                {
                    Count = 1,
                    Quality = 0
                },
                Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
                BufferCount = 2,
                Scaling = SharpDX.DXGI.Scaling.Stretch,
                SwapEffect = SharpDX.DXGI.SwapEffect.FlipSequential,
                AlphaMode = SharpDX.DXGI.AlphaMode.Premultiplied,
                Flags = SharpDX.DXGI.SwapChainFlags.None
            };
            swapChain = new SharpDX.DXGI.SwapChain1(dxgiFactory, d3dDevice, ref description);

            framePool = Direct3D11CaptureFramePool.Create(
                device,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                2,
                i.Size);
            session = framePool.CreateCaptureSession(i);
            lastSize = i.Size;

            framePool.FrameArrived += OnFrameArrived;
            i.Closed += new Windows.Foundation.TypedEventHandler<GraphicsCaptureItem, object>(appWindow.OnCaptureWindowClose);
        }

        public void Dispose()
        {
            session?.Dispose();
            framePool?.Dispose();
            swapChain?.Dispose();
            d3dDevice?.Dispose();
        }

        public void StartCapture()
        {
            session.StartCapture();
            newWindow = true;
        }

        public ICompositionSurface CreateSurface(Compositor compositor)
        {
            return compositor.CreateCompositionSurfaceForSwapChain(swapChain);
        }

        private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            var newSize = false;

            using (var frame = sender.TryGetNextFrame())
            {

                if (frame.ContentSize.Width != lastSize.Width ||
                    frame.ContentSize.Height != lastSize.Height)
                {
                    // The thing we have been capturing has changed size.
                    // We need to resize the swap chain first, then blit the pixels.
                    // After we do that, retire the frame and then recreate the frame pool.
                    newSize = true;
                    lastSize = frame.ContentSize;
                    swapChain.ResizeBuffers(
                        2, 
                        lastSize.Width, 
                        lastSize.Height, 
                        SharpDX.DXGI.Format.B8G8R8A8_UNorm, 
                        SharpDX.DXGI.SwapChainFlags.None);
                }

                using (var backBuffer = swapChain.GetBackBuffer<SharpDX.Direct3D11.Texture2D>(0))
                using (var bitmap = Direct3D11Helper.CreateSharpDXTexture2D(frame.Surface))
                {
                    d3dDevice.ImmediateContext.CopyResource(bitmap, backBuffer);
                    if(RequestBitmap)
                    {
                        RequestBitmap = false;
                        var tempBitmap = new System.Drawing.Bitmap(bitmap.Description.Width, bitmap.Description.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        BitmapFromTexture(tempBitmap, bitmap);
                        appWindow.BitmapCreated(this, tempBitmap);
                    }
                }


            } // Retire the frame.

            swapChain.Present(0, SharpDX.DXGI.PresentFlags.None);

            if (newWindow)
            {
                appWindow.UpdateContentSize(new Size(lastSize.Width, lastSize.Height));
                newWindow = false;
            }

            if (newSize)
            {
                framePool.Recreate(
                    device,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    2,
                    lastSize);
                appWindow.UpdateContentSize(new Size(lastSize.Width, lastSize.Height));
            }
        }

        public bool BitmapFromTexture(System.Drawing.Bitmap fastBitmap, SharpDX.Direct3D11.Texture2D texture)
        {
            if (texture == null)
                return false;

            Rectangle rect = new Rectangle(0, 0, fastBitmap.Width, fastBitmap.Height);
            System.Drawing.Imaging.BitmapData bmpData = fastBitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, fastBitmap.PixelFormat);


            using (SharpDX.Direct3D11.Texture2D stage = CreateStagingTexture(bmpData.Width, bmpData.Height))
            {

                d3dDevice.ImmediateContext.CopyResource(texture, stage);
                DataBox dataBox = d3dDevice.ImmediateContext.MapSubresource(stage, 0, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out DataStream dsIn);
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
                    d3dDevice.ImmediateContext.UnmapSubresource(stage, 0);
                }
                fastBitmap.UnlockBits(bmpData);
                dsIn.Dispose();
            }
            return true;
        }

        public Size GetWindowSize()
        {
            return new Size(lastSize.Width, lastSize.Height);
        }

        public Texture2D CreateStagingTexture(int width, int height)
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

            return new Texture2D(d3dDevice, textureDesc);
        }

    }
}
