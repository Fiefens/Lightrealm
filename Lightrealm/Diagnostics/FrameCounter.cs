using Lightrealm.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;


namespace Lightrealm.Diagnostics
{
    class FrameCounter
    {
        private int frames;
        private int framesPerSecond;
        private int totalFrames;
        private int calls;
        private TimeSpan elapsed;

        public Vector2 Location = new Vector2(0, 50);
        public SpriteFont font;

        public Keys Key { get; set; }
        public bool RenderFps { get; set; }

        public FrameCounter()
        {
            frames = 0;
            framesPerSecond = 0;
            totalFrames = 0;
            calls = 0;
            elapsed = TimeSpan.Zero;
            RenderFps = false;
            Key = Keys.F1;
        }

        public void Render(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (RenderFps)
            {
                spriteBatch.DrawString(font, StatusReport(), Location, GetFpsColor());
            }
        }

        public void Update(GameTime gameTime)
        {
            if (Engine.Input.WasKeyPressed(Key))
            {
                RenderFps = !RenderFps;
            }

            frames++;

            elapsed += gameTime.ElapsedGameTime;

            if (elapsed > TimeSpan.FromSeconds(1))
            {
                elapsed -= TimeSpan.FromSeconds(1);
                framesPerSecond = frames;
                totalFrames += framesPerSecond;
                calls++;
                frames = 0;
            }
        }

        public Color GetFpsColor()
        {
            int avg = 0;
            if (calls > 0)
            {
                avg = totalFrames / calls;
            }

            if (avg < 25)
            {
                return Color.Red;
            }
            else if (avg < 45)
            {
                return Color.Yellow;
            }
            else
            {
                return Color.LimeGreen;
            }
        }
        public string StatusReport()
        {
            int avg = 0;

            if (calls != 0)
            {
                avg = totalFrames / calls;
            }
            return string.Concat("FPS : ", framesPerSecond.ToString(), System.Environment.NewLine,
                                  "AVG : ", avg.ToString(), System.Environment.NewLine);
        }
    }
}
