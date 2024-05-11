using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Lightrealm.GameEngine
{
    class GameInput
    {
        KeyboardState previousState;
        KeyboardState currentState;

        public GameInput()
        {
            previousState = Keyboard.GetState();
            currentState = Keyboard.GetState();
        }

        public void Update(GameTime gameTime)
        {
            previousState = currentState;
            currentState = Keyboard.GetState();
        }

        // Several game updates can happen while a key is pressed, even if we only meant to hit it once.  IsKeyDown is useful for 
        // movement keys, or any sustained action.
        public bool IsKeyDown(Keys key) { return currentState.IsKeyDown(key); }
        public bool IsKeyUp(Keys key) { return currentState.IsKeyUp(key); }

        // For things like menu keys, or keys that toggle, we only want to know the frame that it was first pressed in.  So this will return false on any frame other than the first even if the
        // user holds the key down for several frames
        public bool WasKeyPressed(Keys key) { return currentState.IsKeyDown(key) && previousState.IsKeyUp(key); }
        public bool WasKeyLetGo(Keys key) { return previousState.IsKeyDown(key) && currentState.IsKeyUp(key); }
    }
}
