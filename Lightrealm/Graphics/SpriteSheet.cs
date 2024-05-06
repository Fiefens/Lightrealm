using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Lightrealm.Graphics
{
    internal class SpriteSheet
    {
        public Texture2D Texture { get; set; }
        public Dictionary<string, Rectangle> Frames { get; private set; }

        public SpriteSheet(Texture2D texture)
        {
            Texture = texture;
            Frames = new Dictionary<string, Rectangle>();
        }

        public void Add(string name, int x, int y, int tileSize)
        {
            if (Frames.ContainsKey(name))
            {
                throw new Exception(name + " already exists in tilemape");
            }
            else
            {
                Frames.Add(name, new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize));
            }
        }
        public Rectangle Get(string name)
        {
            //tile-mountian
            if (Frames.TryGetValue(name, out Rectangle rect))
            {
                return rect;
            }
            else
            {
                throw new Exception(name + " does not exist in tilemape");
            }
        }
    }
}
