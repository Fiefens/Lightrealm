using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    public class TitleScreenParticle
    {
        public float X;
        public float Y;

        public int Bounces = 0;

        public bool Gravity;

        public int Alpha;
        public int Brightness;

        public float YVelocity;
        public float XVelocity;

        public TitleScreenParticle(int x, int y, int Alpha, int Brightness, int xVelocity, int yVelocity)
        {
            X = x;
            Y = y;
            this.Alpha = Alpha;
            this.Brightness = Brightness;
            YVelocity = yVelocity;
            XVelocity = xVelocity;
            Random r = new Random();

            if(r.Next(0,2) == 1)
            {
                Gravity = true;
            }
            else
            {
                Gravity = true;
            }
        }
    }
}
