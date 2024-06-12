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
        public List<Region> CurrentlyMarkedRegions = new List<Region>();

        public Party() : base()
        {

        }

        public void ClearSkillData()
        {
            foreach (Architect a in Architects)
            {
                a.BodySlamReady = false;
                a.DoubleStrikeReady = false;
                a.DropKickReady = false;
                a.FinaleReady = false;
                a.LegSweepReady = false;
                a.QuickStrikeReady = false;
                a.SeveringStrikeReady = false;

                a.UsedSkills = new List<string>();
                a.ExtraFocusTicks = 0;
            }
        }

        public Party(List<Architect> architects, string type, Architect leader, Location Basee) : base(architects, type, leader, Basee)
        {
            foreach(Architect a in architects)
            {
                //give tje, resources for testing

                // We assume that 'Region' has properties for each harvestable material type.
                var region = a.Location.Region;

                // We'll use a dictionary to map the resource types to their corresponding properties.







                /*
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
                }d
                */

                List<Material> m = new List<Material>() { Game1.GameWorld.Metals[Game1.r.Next(Game1.GameWorld.Metals.Count)] };
                
                if (a.RightHanded)
                {
                    a.RightHandObject = Game1.GenerateRandomWeapon(Game1.GameWorld.Metals[0], "common");
                }
                else
                {
                    a.LeftHandObject = Game1.GenerateRandomWeapon(Game1.GameWorld.Metals[0], "common");
                }

                /*
                a.Inventory.Add(new Object(null, "pickaxe", new List<Material>() { a.Location.HomeCivilization.CulturalMetal }, null));
                a.Inventory.Add(new Object(null, "scythe", new List<Material>() { a.Location.HomeCivilization.CulturalMetal }, null));
                a.Inventory.Add(new Object(null, "axe", new List<Material>() { a.Location.HomeCivilization.CulturalMetal }, null));
                a.Inventory.Add(new Object(null, "shovel", new List<Material>() { a.Location.HomeCivilization.CulturalMetal }, null));
                */

                for (int i = 0; i < 3; i++)
                {
                    int healingItem = Game1.r.Next(1, 4); // Adjust the range based on the number of healing items available

                    switch (healingItem)
                    {
                        case 1:
                            // Adding a salve
                            a.Inventory.Add(new Object(null, "salve", new List<Material>() { a.Location.Region.HarvestableFiber }, null));
                            break;
                        case 2:
                            // Adding a bandage
                            a.Inventory.Add(new Object(null, "bandage", new List<Material>() { a.Location.HomeCivilization.CulturalCloth }, null));
                            break;
                        case 3:
                            // Adding a vitality vial
                            a.Inventory.Add(new Object(null, "vial", new List<Material>() { Game1.GameWorld.Glass, Game1.GameWorld.Vitalium }, null));
                            break;
                    }
                }

            }
        }
    }
}
