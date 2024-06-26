using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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

        public bool ReceivedPartyAdvice = false;

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
                a.HairID = Game1.r.Next(0,2);


                List<Material> m = new List<Material>() { Game1.GameWorld.Metals[Game1.r.Next(Game1.GameWorld.Metals.Count)] };

                a.MainHeldObject = Game1.GenerateRandomWeapon(Game1.GameWorld.Metals[0], "common");

                int Count = Game1.r.Next(10, 21);

                for (int i = 0; i < Count; i++)
                {
                    a.Inventory.Add(new Object(null, "fragment", new List<Material>() { Game1.GameWorld.Vitalium }, null));
                }

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
