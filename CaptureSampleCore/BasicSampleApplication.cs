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

using Common;
using Composition.WindowsRuntimeHelpers;
using System;
using System.Drawing;
using System.Numerics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI.Composition;

namespace CaptureSampleCore
{
    public class BasicSampleApplication : IDisposable
    {
        public static Compositor compositor;
        private ContainerVisual root;

        public static SpriteVisual content;
        private CompositionSurfaceBrush brush;

        private IDirect3DDevice device;
        private BasicCapture capture;

        public static CompositionColorBrush aoiBrush;
        private AOIList aoiList;
        public Size CaptureSize;

        public float RenderScale = 0.5f;
        public bool CapturingLoL = false;
        public bool RenderPreview;

        public EventHandler<Size> ContentSizeUpdated;
        public EventHandler CaptureWindowClosed;

        public EventHandler<Bitmap> BitmapCreated;

        public BasicSampleApplication(Compositor c, Size size, bool RenderPreview)
        {
            compositor = c;
            device = Direct3D11Helper.CreateDevice();

            aoiList = new AOIList();
            CaptureSize = size;

            this.RenderPreview = RenderPreview;

            // Setup the root.
            root = compositor.CreateContainerVisual();
            root.RelativeSizeAdjustment = Vector2.One;

            // Setup the content.
            brush = compositor.CreateSurfaceBrush();
            brush.HorizontalAlignmentRatio = 0.5f;
            brush.VerticalAlignmentRatio = 0.5f;
            brush.Stretch = CompositionStretch.Uniform;

            //Setup Aoi brush
            aoiBrush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(0x8A, 0xC2, 0xF1, 0xA3));


            //Drop shadow on preview window
            //var shadow = compositor.CreateDropShadow();
            //shadow.Mask = brush;

            content = compositor.CreateSpriteVisual();
            content.AnchorPoint = new Vector2(0f);
            //content.RelativeOffsetAdjustment = new Vector3(0.5f, 0.5f, 0);
            //content.RelativeSizeAdjustment = Vector2.Zero;
            content.Size = new Vector2(CaptureSize.Width, CaptureSize.Height);
            content.Brush = brush;
            //Disabled drop shadow for now since it really isnt needed and has to be disabled if preview is turned off
            //content.Shadow = shadow;
            root.Children.InsertAtTop(content);

            //Debug area of interest
            //aoiList.OtherAreas.Add(new AreaOfInterest(0, 0, 960, 540));
        }

        public void CapturingLeagueOfLegends()
        {
            CreateAreasOfInterest();
            CapturingLoL = true;
        }

        public void StopCapturingLeagueOfLegends()
        {
            DeleteAllAreasOfInterest();
        }

        public void CreateAreasOfInterest()
        {
            aoiList.Blue_Gold = new AreaOfInterest(757, 15, 90, 25, AOIType.BlueGold);
            aoiList.Red_Gold = new AreaOfInterest(1140, 15, 90, 25, AOIType.RedGold);


            aoiList.GetAllAreaOfInterests().ForEach((aoi) => aoi.Sprite = CreateSprite(aoi));
        }

        private SpriteVisual CreateSprite(AreaOfInterest aoi)
        {
            SpriteVisual s = BasicSampleApplication.compositor.CreateSpriteVisual();
            s.AnchorPoint = new Vector2(0f);
            s.Size = new Vector2(aoi.Rect.Width, aoi.Rect.Height);
            s.Offset = new Vector3(aoi.Rect.X, aoi.Rect.Y, 0);
            s.Brush = BasicSampleApplication.aoiBrush;
            s.Scale = new Vector3(RenderScale, RenderScale, 1);
            s.Offset = new Vector3(aoi.Rect.X * RenderScale, aoi.Rect.Y * RenderScale, 0);

            BasicSampleApplication.content.Children.InsertAtBottom(s);

            return s;
        }


        public void DeleteAllAreasOfInterest()
        {
            content.Children.RemoveAll();
            aoiList.Clear();
        }

        public void UpdateScale(float scale)
        {
            this.RenderScale = scale;
            var newSize = new Vector2(CaptureSize.Width * RenderScale, CaptureSize.Height * RenderScale);
            if (newSize.X < 800 || newSize.Y < 450)
                return;
            content.Size = newSize;
            aoiList.GetAllAreaOfInterests().ForEach((aoi) =>
            {
                aoi.Sprite.Scale = new Vector3(RenderScale, RenderScale, 1);
                aoi.Sprite.Offset = new Vector3(aoi.Rect.X * RenderScale, aoi.Rect.Y * RenderScale, 0);
            });

            OnUpdateContentSize(CaptureSize);
        }

        public void UpdateContentSize(Size newSize)
        {
            var updatedSize = new Vector2(newSize.Width * RenderScale, newSize.Height * RenderScale);
            if (updatedSize.X < 800)
            {
                var x = 800 / updatedSize.X;
                updatedSize.X = 800;
                updatedSize.Y *= x;
            }
            if (updatedSize.Y < 450)
            {
                var y = 450 / updatedSize.Y;
                updatedSize.Y = 450;
                updatedSize.X += y;
            }
            content.Size = updatedSize;
            aoiList.GetAllAreaOfInterests().ForEach((aoi) =>
            {
                aoi.Sprite.Scale = new Vector3(RenderScale, RenderScale, 1);
            });

            CaptureSize = newSize;

            OnUpdateContentSize(CaptureSize);
        }

        protected virtual void OnUpdateContentSize(Size e)
        {
            ContentSizeUpdated?.Invoke(this, e);
        }

        public virtual void OnCaptureWindowClose(GraphicsCaptureItem i, object o)
        {
            CaptureWindowClosed?.Invoke(this, EventArgs.Empty);
        }


        public Visual Visual => root;

        public void Dispose()
        {
            StopCapture();
            compositor = null;
            root.Dispose();
            content.Dispose();
            brush.Dispose();
            device.Dispose();
            aoiBrush.Dispose();
            aoiList.DisposeAll();
        }

        public void StartCaptureFromItem(GraphicsCaptureItem item)
        {
            StopCapture();
            capture = new BasicCapture(device, item, this);

            var surface = capture.CreateSurface(compositor);
            brush.Surface = surface;

            capture.StartCapture();
        }

        public void StopCapture()
        {
            capture?.Dispose();
            brush.Surface = null;
            if (CapturingLoL)
                StopCapturingLeagueOfLegends();
        }

        public AOIList GetAreasOfInterest()
        {
            return aoiList;
        }

        public void RequestCurrentBitmap()
        {
            capture.RequestBitmap = true;
        }
    }
}
