using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Party : Group
    {
        public int MapCursorDistrict { get; set; } = 0;
        public InteractableEvent CurrentEvent = null;

        public List<Architect> IntriguingArchitects = new List<Architect>();

        public Party() : base()
        {

        }

        public Party(List<Architect> architects, string type, Architect leader, Location Basee) : base(architects, type, leader, Basee)
        {
            foreach(Architect a in architects)
            {
                if(a.RightHanded)
                {
                    a.RightHandObject = Game1.GenerateRandomWeapon(Game1.GameWorld.Metals[0], "common");
                }
                else
                {
                    a.LeftHandObject = Game1.GenerateRandomWeapon(Game1.GameWorld.Metals[0], "common");
                }

                for(int i = Game1.r.Next(10,20); i != 0; i--)
                {
                    a.Inventory.Add(new Object(null, "fragment", new List<Material>() { Game1.GameWorld.Vitalium }, null));
                }
            }
        }
    }
}
