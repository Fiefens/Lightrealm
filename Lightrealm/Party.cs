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
                //give tje, resources for testing

                // We assume that 'Region' has properties for each harvestable material type.
                var region = a.Location.Region;

                // We'll use a dictionary to map the resource types to their corresponding properties.
                var resources = new Dictionary<string, Material>
                {
                    {"log", region.HarvestableWood},
                    {"stone", region.HarvestableStone},
                    {"ore", region.HarvestableMetal},
                    {"pile", region.HarvestableSand},
                    {"bunch", region.HarvestableFiber},
                    {"block", region.HarvestableIce}
                };

                foreach (var resource in resources)
                {
                    if (resource.Value != null)  // Check if the resource exists in the region.
                    {
                        for (int i = 0; i < 5; i++)  // Add 5 of each resource.
                        {
                            a.Inventory.Add(new Object(null, resource.Key, new List<Material>() { resource.Value }, null));
                        }
                    }
                }


                List<Material> m = new List<Material>() { Game1.GameWorld.Metals[Game1.r.Next(Game1.GameWorld.Metals.Count)] };
                for (int i = Game1.r.Next(10, 30); i != 0; i--)
                {
                    a.Inventory.Add(new Object(null, "dagger", m, null));
                }

                if (a.RightHanded)
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
