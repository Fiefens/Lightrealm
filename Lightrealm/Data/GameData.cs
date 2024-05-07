using Lightrealm.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System;
using System.IO;

namespace Lightrealm.Data
{
    static class GameData
    {
        public static GraphicsDevice Device;
        public static Texture2D PixelTexture;
        public static SpriteSheet SpriteSheet;


        public static Texture2D whiteRect;
        public static Texture2D GUI;
        public static Texture2D InventoryGUI;

        public static Texture2D Astrionalis;
        public static Texture2D Celestrioris;

        //public static Texture2D CursorT;
        public static Texture2D Gradient;
        public static Texture2D TitleScreen;
        public static Texture2D GuideT;
        public static Texture2D ReactionGUIT;
        public static Texture2D BleedT;

        public static Texture2D ClockT;
        public static Texture2D SkyT;
        public static Texture2D SunT;
        public static Texture2D MoonT;

        public static Texture2D ArchitectHere;
        public static Texture2D HealthGuiT;

        public static Texture2D myIconTexture;


        public static SpriteFont Shibafont;
        public static SpriteFont BabyShibafont;

        public static Song LightrealmMainTheme;

        public static void LoadContent(GraphicsDevice device, ContentManager Content)
        {
            Device = device;

            LoadPixelTexture();
            LoadSpriteSheet(Content);

            TitleScreen = Content.Load<Texture2D>("title");
            GUI = Content.Load<Texture2D>("gui");
            InventoryGUI = Content.Load<Texture2D>("inventory gui");
            Astrionalis = Content.Load<Texture2D>("astrionalis");
            Celestrioris = Content.Load<Texture2D>("celestrioris");

            Gradient = Content.Load<Texture2D>("gradient");
            //CursorT = Content.Load<Texture2D>("tiles/cursor");
            GuideT = Content.Load<Texture2D>("moveguide");
            HealthGuiT = Content.Load<Texture2D>("healthgui");

            ArchitectHere = Content.Load<Texture2D>("distmap/architecthere");
            BleedT = Content.Load<Texture2D>("droplet");

            whiteRect = Content.Load<Texture2D>("pixel");
            ReactionGUIT = Content.Load<Texture2D>("reaction gui");

            myIconTexture = Content.Load<Texture2D>("Icon");

            ClockT = Content.Load<Texture2D>("clock");
            SkyT = Content.Load<Texture2D>("sky");
            SunT = Content.Load<Texture2D>("sun");
            MoonT = Content.Load<Texture2D>("moon");

            Shibafont = Content.Load<SpriteFont>("shibafont");
            BabyShibafont = Content.Load<SpriteFont>("babyshibafont");


            LightrealmMainTheme = Content.Load<Song>("audio/lightrealm main theme (2023)");


        }


        public static void LoadSpriteSheet(ContentManager Content)
        {
            string path = string.Concat(Content.RootDirectory, "\\graphics\\");
            Texture2D sheet = LoadTextureFromFile(string.Concat(path, "spritesheet.png"));

            SpriteSheet = new SpriteSheet(sheet);

            SpriteSheet.Add("tile-void", 0, 0, 24);
            SpriteSheet.Add("tile-cursor", 1, 0, 24);
            SpriteSheet.Add("tile-emptytile", 2, 0, 24);
            SpriteSheet.Add("tile-cave", 3, 0, 24);
            SpriteSheet.Add("tile-desert", 4, 0, 24);
            SpriteSheet.Add("tile-ethereal", 5, 0, 24);
            SpriteSheet.Add("tile-forest", 6, 0, 24);
            SpriteSheet.Add("tile-lightforest", 7, 0, 24);
            SpriteSheet.Add("tile-mountain", 8, 0, 24);
            SpriteSheet.Add("tile-ocean", 9, 0, 24);
            SpriteSheet.Add("tile-outline", 10, 0, 24);
            SpriteSheet.Add("tile-plains", 11, 0, 24);
            SpriteSheet.Add("tile-port", 12, 0, 24);
            SpriteSheet.Add("tile-snowpeak", 13, 0, 24);
            SpriteSheet.Add("tile-taiga", 14, 0, 24);
            SpriteSheet.Add("tile-tundra", 15, 0, 24);
            SpriteSheet.Add("tile-waterfall", 16, 0, 24);

            SpriteSheet.Add("loc-empty", 0, 4, 16);
            SpriteSheet.Add("loc-fortress", 1, 4, 16);
            SpriteSheet.Add("loc-gateway", 2, 4, 16);
            SpriteSheet.Add("loc-isofractalcore", 3, 4, 16);
           // SpriteSheet.Add("loc-isofractaloutpost", 4, 4, 16);
            SpriteSheet.Add("loc-keep", 5, 4, 16);
           // SpriteSheet.Add("loc-lostcamp", 6, 4, 16);
           // SpriteSheet.Add("loc-lostcity", 7, 4, 16);
           // SpriteSheet.Add("loc-losttown", 8, 4, 16);
           // SpriteSheet.Add("loc-lostvillage", 9, 4, 16);
            SpriteSheet.Add("loc-luminarchcamp", 10, 4, 16);
            SpriteSheet.Add("loc-luminarchcity", 11, 4, 16);
            SpriteSheet.Add("loc-luminarchtown", 12, 4, 16);
            SpriteSheet.Add("loc-luminarchvillage", 13, 4, 16);
            SpriteSheet.Add("loc-monument", 14, 4, 16);
            SpriteSheet.Add("loc-nightfellcamp", 15, 4, 16);
            SpriteSheet.Add("loc-nightfellcity", 16, 4, 16);
            SpriteSheet.Add("loc-nightfelltown", 17, 4, 16);
            SpriteSheet.Add("loc-nightfellvillage", 18, 4, 16);
            SpriteSheet.Add("loc-outpost", 19, 4, 16);
            SpriteSheet.Add("loc-photonexuscore", 20, 4, 16);
           // SpriteSheet.Add("loc-photonexusoutpost", 21, 4, 16);
            SpriteSheet.Add("loc-sanctum", 22, 4, 16);
            SpriteSheet.Add("loc-shadecore", 23, 4, 16);
            SpriteSheet.Add("loc-shadeoutpost", 24, 4, 16);
            SpriteSheet.Add("loc-spire", 25, 4, 16);
            SpriteSheet.Add("loc-stronghold", 26, 4, 16);
            SpriteSheet.Add("loc-tower", 27, 4, 16);


            SpriteSheet.Add("loc-archaixcamp", 6, 4, 16);
            SpriteSheet.Add("loc-archaixcity", 7, 4, 16);
            SpriteSheet.Add("loc-archaixtown", 8, 4, 16);
            SpriteSheet.Add("loc-archaixvillage", 9, 4, 16);

            SpriteSheet.Add("loc-isofractalgarrison", 4, 4, 16);
            SpriteSheet.Add("loc-photonexusgarrison", 21, 4, 16);
            //loc-archaixvillage

            SpriteSheet.Add("map-emptylocation", 0, 4, 32);
            SpriteSheet.Add("map-emptyplains", 1, 4, 32);
            SpriteSheet.Add("map-architecthere", 2, 4, 32);
            SpriteSheet.Add("map-buildings", 3, 4, 32);
            SpriteSheet.Add("map-emptydesert", 4, 4, 32);
            SpriteSheet.Add("map-emptysnow", 5, 4, 32);
            SpriteSheet.Add("map-emptytrees", 6, 4, 32);
            SpriteSheet.Add("map-manybuildings", 7, 4, 32);
            SpriteSheet.Add("map-market", 8, 4, 32);
            SpriteSheet.Add("map-marketsurround", 9, 4, 32);
            SpriteSheet.Add("map-ocean", 10, 4, 32);
            SpriteSheet.Add("map-prism", 11, 4, 32);
            SpriteSheet.Add("map-sanctum", 12, 4, 32);
            SpriteSheet.Add("map-shadowstorage", 13, 4, 32);
            SpriteSheet.Add("map-specialandbuildings", 14, 4, 32);
            SpriteSheet.Add("map-specialbuilding", 15, 4, 32);
            SpriteSheet.Add("map-spire", 0, 5, 32);
            SpriteSheet.Add("map-stronghold", 1, 5, 32);
            SpriteSheet.Add("map-well", 2, 5, 32);
                       

            SpriteSheet.Add("char-base-lost", 1, 5, 32);
            SpriteSheet.Add("char-base-luminarch", 2, 5, 32);
            SpriteSheet.Add("char-base-nightfell", 3, 5, 32);

            SpriteSheet.Add("char-amulet", 1, 6, 32);
            SpriteSheet.Add("char-cape", 2, 6, 32);
            SpriteSheet.Add("char-flair", 3, 6, 32);
            SpriteSheet.Add("char-largehat", 4, 6, 32);
            SpriteSheet.Add("char-smallhat", 5, 6, 32);
        }

        public static void LoadPixelTexture()
        {
            PixelTexture = new Texture2D(Device, 1, 1);
            Color[] color = new Color[1];
            color[0] = Color.White;
            PixelTexture.SetData<Color>(color);
        }

        public static Texture2D LoadTextureFromFile(string filename)
        {
            if (File.Exists(filename))
            {
                Texture2D texture;
                using (FileStream s = File.OpenRead(filename))
                {
                    texture = Texture2D.FromStream(Device, s);
                }
                return texture;
            }
            else
            {
                throw new System.Exception( filename + " not found");
            }
        }
    }
}
