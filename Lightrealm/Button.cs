using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Lightrealm
{
    public class Button
    {
        public string Text = "";
        public Rectangle Hitbox;
        public List<string> GameStates = new List<string>(); // List of valid game states for visibility

        public bool InvisibleLock = false;

        // Property to check if the button is visible based on the current game state
        public bool Visible
        {
            get
            {   
                if(InvisibleLock)
                {
                    return false;
                }
                return GameStates.Contains(Game1.GameState);
            }
        }

        public Button(string text, int X, int Y, int Width, List<string> gameStates)
        {
            Hitbox = new Rectangle(X, Y, Width, 64); // Fixed height of 64
            Text = text;
            GameStates = gameStates; // Assign valid game states for visibility

            Game1.Buttons.Add(this);
        }

        // Method to draw the button with centered text
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible)
                return;

            // Divide the button texture into thirds
            int segmentWidth = Game1.ButtonT.Width / 3;
            int buttonHeight = Game1.ButtonT.Height; // Assuming height is 64

            // Left, center, and right source rectangles
            Rectangle leftSource = new Rectangle(0, 0, segmentWidth, buttonHeight);
            Rectangle centerSource = new Rectangle(segmentWidth, 0, segmentWidth, buttonHeight);
            Rectangle rightSource = new Rectangle(Game1.ButtonT.Width - segmentWidth, 0, segmentWidth, buttonHeight);

            // Draw the left section
            spriteBatch.Draw(Game1.ButtonT, new Rectangle(Hitbox.X, Hitbox.Y, segmentWidth, Hitbox.Height), leftSource, Color.White);

            // Draw the center section repeatedly to fill the middle width
            int centerX = Hitbox.X + segmentWidth;
            int centerWidth = Hitbox.Width - 2 * segmentWidth;

            for (int x = centerX; x < centerX + centerWidth; x += segmentWidth)
            {
                // If the center width isn't a perfect multiple, handle the remaining width
                int remainingWidth = Math.Min(segmentWidth, centerX + centerWidth - x);
                spriteBatch.Draw(Game1.ButtonT, new Rectangle(x, Hitbox.Y, remainingWidth, Hitbox.Height),
                    new Rectangle(centerSource.X, centerSource.Y, remainingWidth, buttonHeight), Color.White);
            }

            // Draw the right section
            spriteBatch.Draw(Game1.ButtonT, new Rectangle(Hitbox.X + Hitbox.Width - segmentWidth, Hitbox.Y, segmentWidth, Hitbox.Height), rightSource, Color.White);

            // Measure the size of the text using the font
            Vector2 textSize = Game1.Shibafont.MeasureString(Text);

            // Calculate the position to draw the text centered in the button
            float textX = Hitbox.X + (Hitbox.Width / 2) - (textSize.X / 2);
            float textY = Hitbox.Y + (Hitbox.Height / 2) - (textSize.Y / 2);

            // Draw the centered text
            spriteBatch.DrawString(Game1.Shibafont, Text, new Vector2(textX, textY), Color.White);
        }

        public bool Clicked()
        {
            // Calculate the scale factors dynamically based on the back buffer dimensions
            float scaleX = (float)Game1.PreferredBackBufferWidth / 2560f;
            float scaleY = (float)Game1.PreferredBackBufferHeight / 1440f;

            // Adjust the mouse position based on the calculated scale factors
            Point adjustedMousePosition = new Point(
                (int)(Game1.currentMouseState.X / scaleX),
                (int)(Game1.currentMouseState.Y / scaleY)
            );

            if (Game1.previousMouseState.LeftButton == ButtonState.Released &&
                Game1.currentMouseState.LeftButton == ButtonState.Pressed &&
                Hitbox.Contains(adjustedMousePosition) &&
                Visible)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Pressed()
        {
            // Calculate the scale factors dynamically based on the back buffer dimensions
            float scaleX = (float)Game1.PreferredBackBufferWidth / 2560f;
            float scaleY = (float)Game1.PreferredBackBufferHeight / 1440f;

            // Adjust the mouse position based on the calculated scale factors
            Point adjustedMousePosition = new Point(
                (int)(Game1.currentMouseState.X / scaleX),
                (int)(Game1.currentMouseState.Y / scaleY)
            );

            if (Game1.currentMouseState.LeftButton == ButtonState.Pressed &&
                Hitbox.Contains(adjustedMousePosition) &&
                Visible)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
