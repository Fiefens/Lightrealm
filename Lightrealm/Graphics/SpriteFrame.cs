using System.Drawing;

namespace Lightrealm.Graphics
{
    internal class SpriteFrame
    {
        public Rectangle Bounds { get; private set; }

        public SpriteFrame(int x, int y, int size)
        {
            Bounds = new Rectangle(x * size, y * size, size, size);
        }
    }
}
