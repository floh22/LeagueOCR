using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Composition;

namespace CaptureSampleCore
{
    public class AreaOfInterest
    {

        public Rectangle Rect;
        public SpriteVisual Sprite;

        public string CurrentContent;

        public void Dispose()
        {
            Sprite.Dispose();
        }

        public AreaOfInterest(int X, int Y, int Width, int Height)
        {
            this.Rect = new Rectangle(X, Y, Width, Height);
            this.Sprite = CreateSprite();
        }

        private SpriteVisual CreateSprite()
        {
            SpriteVisual s = BasicSampleApplication.compositor.CreateSpriteVisual();
            s.AnchorPoint = new Vector2(0f);
            s.Size = new Vector2(Rect.Width, Rect.Height);
            s.Offset = new Vector3(Rect.X, Rect.Y, 0);
            s.Brush = BasicSampleApplication.aoiBrush;

            BasicSampleApplication.content.Children.InsertAtBottom(s);

            return s;
        }
    }
}
