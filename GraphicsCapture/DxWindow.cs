using System;

using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.Windows;
using GraphicsCapture.Interfaces;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using Resource = SharpDX.Direct3D11.Resource;
using System.Drawing;

namespace GraphicsCapture
{
    public class DxWindow : IDisposable
    {
        private readonly ICaptureMethod _captureMethod;
        private readonly string _title;

        private SharpDX.WIC.ImagingFactory2 wicFactory;

        public DxWindow(string title, ICaptureMethod captureMethod)
        {
            _title = title;
            _captureMethod = captureMethod;

            wicFactory = new SharpDX.WIC.ImagingFactory2();

        }

        public void Dispose()
        {
            _captureMethod?.Dispose();
        }


        public void Show()
        {
            using var form = new RenderForm(_title);

            var width = form.ClientSize.Width;
            var height = form.ClientSize.Height;
            float aspectRatio = width / height;

            bool firstRun = true;

            // create a Device and SwapChain
            var swapChainDescription = new SwapChainDescription
            {
                BufferCount = 2,
                Flags = SwapChainFlags.None,
                IsWindowed = true,
                ModeDescription = new ModeDescription(form.ClientSize.Width, form.ClientSize.Height, new Rational(60, 1), Format.B8G8R8A8_UNorm),
                OutputHandle = form.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.BgraSupport, swapChainDescription, out var device, out var swapChain);
            using var swapChain1 = swapChain.QueryInterface<SwapChain1>();

            // ignore all Windows events
            using var factory = swapChain1.GetParent<Factory>();
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);

            using var vertexShaderByteCode = ShaderBytecode.CompileFromFile("./Shader.fx", "VS", "vs_5_0");
            using var vertexShader = new VertexShader(device, vertexShaderByteCode);

            using var pixelShaderByteCode = ShaderBytecode.CompileFromFile("./Shader.fx", "PS", "ps_5_0");
            using var pixelShader = new PixelShader(device, pixelShaderByteCode);

            using var layout = new InputLayout(device, vertexShaderByteCode, new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 12, 0)
            });

            using var vertexes = Buffer.Create(device, BindFlags.VertexBuffer, new[]
            {
                new Vertex { Position = new RawVector3(-1.0f, 1.0f, 0.5f), TexCoord = new RawVector2(0.0f, 0.0f) },
                new Vertex { Position = new RawVector3(1.0f, 1.0f, 0.5f), TexCoord = new RawVector2(1.0f, 0.0f) },
                new Vertex { Position = new RawVector3(-1.0f, -1.0f, 0.5f), TexCoord = new RawVector2(0.0f, 1.0f) },
                new Vertex { Position = new RawVector3(1.0f, -1.0f, 0.5f), TexCoord = new RawVector2(1.0f, 1.0f) }
            });

            var samplerStateDescription = new SamplerStateDescription
            {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Filter = Filter.MinMagMipLinear
            };

            device.ImmediateContext.InputAssembler.InputLayout = layout;
            device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexes, Utilities.SizeOf<Vertex>(), 0));
            device.ImmediateContext.VertexShader.Set(vertexShader);
            device.ImmediateContext.PixelShader.SetSampler(0, new SamplerState(device, samplerStateDescription));
            device.ImmediateContext.PixelShader.Set(pixelShader);

            // create a first views
            var backBuffer = Resource.FromSwapChain<Texture2D>(swapChain1, 0);
            var renderView = new RenderTargetView(device, backBuffer);

            device.ImmediateContext.Rasterizer.SetViewport(0, 0, form.ClientSize.Width, form.ClientSize.Height);
            device.ImmediateContext.OutputMerger.SetTargets(renderView);

            // listen events (but processed in render loop)
            var isResized = false;
            form.UserResized += (_, __) => isResized = true;

            RenderLoop.Run(form, () =>
            {
                // ReSharper disable AccessToDisposedClosure
                if (!_captureMethod.IsCapturing)
                    _captureMethod.StartCapture(form.Handle, device, factory);

                if (!_captureMethod.IsCapturing)
                    return;

                using var texture2d = _captureMethod.TryGetNextFrameAsTexture2D(device);

                form.AllowUserResizing = false;

                if (firstRun)
                {
                    //Get new dimensions
                    height = _captureMethod.GetWindowSize().Height;
                    width = _captureMethod.GetWindowSize().Width;
                    aspectRatio = width / height;

                    form.Size = new System.Drawing.Size(width, height);

                    //Do not resize in future and force update
                    firstRun = false;
                    isResized = true;
                }

                if (isResized)
                {
                    Utilities.Dispose(ref backBuffer);
                    Utilities.Dispose(ref renderView);

                    swapChain1.ResizeBuffers(swapChainDescription.BufferCount, width, height, Format.Unknown, SwapChainFlags.None);
                    backBuffer = Resource.FromSwapChain<Texture2D>(swapChain1, 0);
                    renderView = new RenderTargetView(device, backBuffer);

                    device.ImmediateContext.Rasterizer.SetViewport(0, 0, width, height);
                    device.ImmediateContext.OutputMerger.SetTargets(renderView);

                    isResized = false;
                }

                // clear view
                device.ImmediateContext.ClearRenderTargetView(renderView, new RawColor4(1.0f, 1.0f, 1.0f, 1.0f));

                if (texture2d != null)
                {
                    using var shaderResourceView = new ShaderResourceView(device, texture2d);
                    device.ImmediateContext.PixelShader.SetShaderResource(0, shaderResourceView);

                    Bitmap outbmp = new Bitmap(texture2d.Description.Width, texture2d.Description.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    _captureMethod.BitmapFromTexture(device, outbmp, texture2d);
                }

                // draw it
                device.ImmediateContext.Draw(4, 0);
                swapChain1.Present(1, PresentFlags.None, new PresentParameters());

                // ReSharper restore AccessToDisposedClosure
            });

            renderView.Dispose();
            backBuffer.Dispose();
            swapChain.Dispose();
            device.Dispose();
        }



    }
}