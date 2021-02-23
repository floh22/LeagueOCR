using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Composition;

namespace Common
{
    public class AreaOfInterest
    {

        public Rectangle Rect;
        public SpriteVisual Sprite;

        public AOIType type;

        public string CurrentContent;

        public void Dispose()
        {
            Sprite.Dispose();
        }

        public AreaOfInterest(int X, int Y, int Width, int Height, AOIType type)
        {
            this.Rect = new Rectangle(X, Y, Width, Height);
            this.type = type;
        }
    }

    public enum AOIType
    {
        BlueGold,
        RedGold
    }
}
