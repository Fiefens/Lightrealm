using Lightrealm.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace Lightrealm.GameEngine
{
    static class Engine
    {
        public static Rendering Render { get; set; }
        public static GameInput Input { get; set; }
        public static FrameCounter FrameCounter { get; set; }
        public static Data Data { get; set; }
        public static Song LightrealmMainTheme;

        public static void Init()
        {
            Render = new Rendering();
            FrameCounter = new FrameCounter();
            Input = new GameInput();
            Data = new Data();
        }

        public static void LoadContent(GraphicsDevice device, ContentManager Content)
        {
            Render.LoadContent(device, Content);
            Data.LoadContent(Content);
            LightrealmMainTheme = Content.Load<Song>("audio/lightrealm main theme (2023)");
        }

        public static void Update(GameTime gameTime)
        {
            Input.Update(gameTime);
            FrameCounter.Update(gameTime);            
        }

        public static void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            FrameCounter.Render(spriteBatch, Render.BabyShibafont);
        }
    }
}
