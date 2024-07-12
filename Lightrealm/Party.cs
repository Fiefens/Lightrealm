using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Party : Group
    {
        public int MapCursorDistrict { get; set; } = 0;

        public EntityList<TextStorage> Intrigue = new EntityList<TextStorage>();

        private int _currentEventId;
        
        public InteractableEvent CurrentEvent
        {
            get => EntityGet<InteractableEvent>(_currentEventId);
            set => _currentEventId = value?.ID ?? 0;
        }

        public EntityList<Architect> IntriguingArchitects { get; set; } = new EntityList<Architect>();
        public EntityList<Region> CurrentlyMarkedRegions { get; set; } = new EntityList<Region>();

        public bool ReceivedPartyAdvice { get; set; } = false;

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

                a.UsedSkills = new EntityList<Entity>();
                a.ExtraFocusTicks = 0;
            }
        }

        public Party(EntityList<Architect> architects, string type, Architect leader, Location basee) : base(architects, type, leader, basee)
        {
            foreach (Architect a in architects)
            {
                a.HairID = Game1.r.Next(0, 2);

                EntityList<Material> materials = new EntityList<Material> { Game1.GameWorld.Metals[Game1.r.Next(Game1.GameWorld.Metals.Count())] };

                a.MainHeldObject = Game1.GenerateRandomWeapon(Game1.GameWorld.Metals[0], "common");

                int count = Game1.r.Next(10, 21);

                for (int i = 0; i < count; i++)
                {
                    a.Inventory.Add(new Object(null, "fragment", new EntityList<Material> { Game1.GameWorld.Vitalium }, null));
                }

                a.SpellsKnown.AddRange(Game1.GameWorld.AllSpells);
                a.SpellsKnown.AddRange(Game1.GameWorld.AllLegendarySpells);
                a.SkillsKnown.AddRange(Game1.GameWorld.AllSkills);


                a.PathOfBodyLevel = 100;
                a.PathOfDeathLevel = 100;
                a.PathOfHeatLevel = 100;
                a.PathOfLightLevel = 100;
                a.PathOfLifeLevel = 100;
                a.PathOfStarsLevel = 100;
                a.PathOfRealityLevel = 100;
                a.PathOfShadowLevel = 100;

                int healingItem = Game1.r.Next(1, 4); // Adjust the range based on the number of healing items available

                switch (healingItem)
                {
                    case 1:
                        // Adding a salve
                        a.Inventory.Add(new Object(null, "salve", new EntityList<Material> { a.Location.Region.HarvestableFiber }, null));
                        break;
                    case 2:
                        // Adding a bandage
                        a.Inventory.Add(new Object(null, "bandage", new EntityList<Material> { a.Location.HomeCivilization.CulturalCloth }, null));
                        break;
                    case 3:
                        // Adding a vitality vial
                        a.Inventory.Add(new Object(null, "vial", new EntityList<Material> { Game1.GameWorld.Glass, Game1.GameWorld.Vitalium }, null));
                        break;
                }
            }
        }
    }
}
