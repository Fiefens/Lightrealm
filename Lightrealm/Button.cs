using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lightrealm
{
    public class Button
    {
        public string Text = "";
        public Rectangle Hitbox;            // in "virtual" coordinates (2560×1440 space)
        public List<string> GameStates = new List<string>();
        public bool InvisibleLock = false;

        public bool Visible
        {
            get
            {
                if (InvisibleLock)
                    return false;
                return GameStates.Contains(Game1.GameState) || GameStates.Contains("buttonwildcard");
            }
        }

        // Constructor
        public Button(string text, int X, int Y, int Width, List<string> gameStates)
        {
            Hitbox = new Rectangle(X, Y, Width, 64); // 64 high in virtual space
            Text = text;
            GameStates = gameStates;

            // Add to global list
            Game1.Buttons.Add(this);
        }

        // ---------------------------------------
        // 1. Adjust Mouse to the Virtual Space
        // ---------------------------------------
        private Point GetAdjustedMousePosition()
        {
            // 1. Real window dimensions
            float windowWidth = Game1.PreferredBackBufferWidth;
            float windowHeight = Game1.PreferredBackBufferHeight;

            // 2. The same scale factors you used in spriteBatch.Begin(...)
            float scaleX = windowWidth / 2560f;
            float scaleY = windowHeight / 1440f;

            // 3. Same offset (letterboxing/pillarboxing)
            int offsetX = (int)((windowWidth - 2560f * scaleX) / 2);
            int offsetY = (int)((windowHeight - 1440f * scaleY) / 2);

            // 4. Invert the transform:
            //    ScreenSpace -> VirtualSpace
            //      mouseX_in_virtual = (mouseX - offsetX) / scaleX
            //      mouseY_in_virtual = (mouseY - offsetY) / scaleY
            int realMouseX = Game1.currentMouseState.X;
            int realMouseY = Game1.currentMouseState.Y;

            float virtX = (realMouseX - offsetX) / scaleX;
            float virtY = (realMouseY - offsetY) / scaleY;

            return new Point((int)virtX, (int)virtY);
        }

        // ---------------------------------------
        // 2. Draw: No Manual Scaling!
        // ---------------------------------------
        //    We just draw using our 'Hitbox' in
        //    the *virtual* coordinate system.
        //    The spriteBatch's scale matrix
        //    will handle the visual scaling.
        // ---------------------------------------
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible)
                return;

            // Check hover by using adjusted mouse
            Point adjustedMousePos = GetAdjustedMousePosition();
            bool hovered = Hitbox.Contains(adjustedMousePos);
            Color buttonColor = hovered ? Color.White : new Color(200, 200, 200);




            if (hovered && Text.StartsWith("Maximum Autosave Age"))
            {
                string ageText = Game1.MaxAutosaveAge switch
                {
                    0 => "never autosave",
                    99999 => "always autosave",
                    _ => $"only autosave if the world's starting age is {Game1.MaxAutosaveAge} years old or less"
                };

                spriteBatch.DrawString(Game1.Shibafont, $"Your game will {ageText}.", new Vector2(50, 1320), Color.White);
                
                if (Game1.MaxAutosaveAge != 99999)
                {
                    spriteBatch.DrawString(Game1.Shibafont, "If saving games takes too long on your device, consider setting this higher, or set it to \"Never Autosave\".", new Vector2(50, 1350), Color.White);
                }
            }
            else if (hovered && Text.StartsWith("Edit Appearance"))
            {
                if (!((Game1.MostRecentPartyTurnArchitect.Room != null ? Game1.MostRecentPartyTurnArchitect.Room.Objects : Game1.MostRecentPartyTurnArchitect.Block.Objects).Any(o => o.Type == "chromaweaver")))
                {
                    spriteBatch.DrawString(Game1.Shibafont, "You need to be near a chromaweaver.", new Vector2(50, 1400), Color.White);
                }
            }
            else if (hovered && Text.StartsWith("Path To Exit"))
            {
                Game1.DrawCenteredTextAtPosition(spriteBatch, "Order your party to locate the exit and", 2235, 70, Game1.BabyShibafont, Color.White);
                Game1.DrawCenteredTextAtPosition(spriteBatch, "use all turns to leave the structure", 2235, 100, Game1.BabyShibafont, Color.White);
            }
            else if (hovered && Text == ("Ascendant"))
            {
                Game1.DrawCenteredTextAtPosition(spriteBatch, "This mode provides no guidance.", 2060, 700, Game1.BabyShibafont, Color.White);
                Game1.DrawCenteredTextAtPosition(spriteBatch, "Please familiarize yourself with Chronicle before attempting.", 2060, 720, Game1.BabyShibafont, Color.White);
            }
            else if (hovered && Text.StartsWith("Sweep Structure Loot"))
            {
                if(Game1.MostRecentPartyTurnArchitect != null && Game1.MostRecentPartyTurnArchitect.Room != null && Game1.MostRecentPartyTurnArchitect.Structure.Type == "market")
                {
                    Game1.DrawCenteredTextAtPosition(spriteBatch, "With certain guards outside, this", 2235, 70, Game1.BabyShibafont, Color.Pink);
                    Game1.DrawCenteredTextAtPosition(spriteBatch, "action might be unwise.", 2235, 100, Game1.BabyShibafont, Color.Pink);
                }
                else if (Game1.MostRecentPartyTurnArchitect != null && Game1.MostRecentPartyTurnArchitect.Room != null && Game1.MostRecentPartyTurnArchitect.Structure.Type == "prism")
                {
                    Game1.DrawCenteredTextAtPosition(spriteBatch, "With certain mines around, this", 2235, 70, Game1.BabyShibafont, Color.Pink);
                    Game1.DrawCenteredTextAtPosition(spriteBatch, "action might be unwise.", 2235, 100, Game1.BabyShibafont, Color.Pink);
                }
                else
                {
                    Game1.DrawCenteredTextAtPosition(spriteBatch, "Order your party to carry ALL useful items", 2235, 70, Game1.BabyShibafont, Color.White);
                    Game1.DrawCenteredTextAtPosition(spriteBatch, "inside this building outside of the building", 2235, 100, Game1.BabyShibafont, Color.White);
                    Game1.DrawCenteredTextAtPosition(spriteBatch, "This might take a while if the structure is large", 2235, 130, Game1.BabyShibafont, Color.Pink);
                }
            }
            else if (hovered && Text.StartsWith("Resume the Cycle") && GameStates[0] == "dead")
            {
                Game1.DrawCenteredText(spriteBatch, "You will re-enter your world as a new vessel.", 1000, Game1.BabyShibafont, Color.White);
                Game1.DrawCenteredText(spriteBatch, "Your dead character's impact will remain in the world.", 1030, Game1.BabyShibafont, Color.White);
            }
            else if (hovered && Text.StartsWith("Assemble Raft"))
            {
                Game1.DrawCenteredTextAtPosition(spriteBatch, "Craft a heavy raft that allows slow water travel.", 2150, 900, Game1.BabyShibafont, Color.Cyan);
            }

            else if (hovered && Text.StartsWith("Return To Title") && GameStates[0] == "dead")
            {
                if(Game1.CombatSimActive)
                {
                }
                else
                {
                    Game1.DrawCenteredText(spriteBatch, "Save the world and return to title. You may re-enter the world from the Load Game screen.", 1000, Game1.BabyShibafont, Color.White);
                    Game1.DrawCenteredText(spriteBatch, "Your dead character's impact will remain in the world.", 1030, Game1.BabyShibafont, Color.White);
                }
            }


            // Button texture info
            int segmentWidth = Game1.ButtonT.Width / 3;
            int buttonHeight = Game1.ButtonT.Height;

            Rectangle leftSource = new Rectangle(0, 0, segmentWidth, buttonHeight);
            Rectangle centerSource = new Rectangle(segmentWidth, 0, segmentWidth, buttonHeight);
            Rectangle rightSource = new Rectangle(Game1.ButtonT.Width - segmentWidth, 0, segmentWidth, buttonHeight);

            // Draw the 3-slice button
            spriteBatch.Draw(
                Game1.ButtonT,
                new Rectangle(Hitbox.X, Hitbox.Y, segmentWidth, Hitbox.Height),
                leftSource,
                buttonColor
            );

            int centerX = Hitbox.X + segmentWidth;
            int remainingWidth = Hitbox.Width - 2 * segmentWidth;
            for (int x = centerX; x < centerX + remainingWidth; x += segmentWidth)
            {
                int drawWidth = Math.Min(segmentWidth, centerX + remainingWidth - x);
                spriteBatch.Draw(
                    Game1.ButtonT,
                    new Rectangle(x, Hitbox.Y, drawWidth, Hitbox.Height),
                    new Rectangle(centerSource.X, centerSource.Y, drawWidth, buttonHeight),
                    buttonColor
                );
            }

            spriteBatch.Draw(
                Game1.ButtonT,
                new Rectangle(Hitbox.X + Hitbox.Width - segmentWidth, Hitbox.Y, segmentWidth, Hitbox.Height),
                rightSource,
                buttonColor
            );

            // Centered text in the button
            Vector2 textSize = Game1.Shibafont.MeasureString(Text);
            Vector2 textPos = new Vector2(
                Hitbox.X + (Hitbox.Width - textSize.X) / 2f,
                Hitbox.Y + (Hitbox.Height - textSize.Y) / 2f
            );

            spriteBatch.DrawString(Game1.Shibafont, Text, textPos, Color.White);
        }

        // ---------------------------------------
        // 3. Clicked & Pressed: Same principle
        // ---------------------------------------
        public bool Clicked()
        {
            if (!Visible)
                return false;

            Point adjustedMousePos = GetAdjustedMousePosition();

            bool mouseJustPressed =
                Game1.previousMouseState.LeftButton == ButtonState.Released &&
                Game1.currentMouseState.LeftButton == ButtonState.Pressed;

            if (mouseJustPressed && Hitbox.Contains(adjustedMousePos))
            {
                Game1.MenuSelect.Play(volume: Game1.SoundVolume / 100f, pitch: 0.0f, pan: 0.0f);
                return true;
            }
            return false;
        }

        public bool Pressed()
        {
            if (!Visible)
                return false;

            Point adjustedMousePos = GetAdjustedMousePosition();

            bool mouseIsDown = (Game1.currentMouseState.LeftButton == ButtonState.Pressed);

            return mouseIsDown && Hitbox.Contains(adjustedMousePos);
        }
    }
}
