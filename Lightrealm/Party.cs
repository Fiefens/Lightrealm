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
        public static T Entity<T>(int entityId) where T : Entity
        {
            if (Game1.GameWorld == null || Game1.GameWorld.AllEntities == null)
            {
                return (T)Convert.ChangeType(Game1.TemporaryEntities[entityId], typeof(T));
            }

            return (T)Convert.ChangeType(Game1.GameWorld.AllEntities[entityId], typeof(T));
        }

        public int MapCursorDistrict { get; set; } = 0;

        private int _currentEventId;
        public InteractableEvent CurrentEvent
        {
            get => Entity<InteractableEvent>(_currentEventId);
            set => _currentEventId = value?.ID ?? 0;
        }

        private List<int> _intriguingArchitects = new List<int>();
        public EntityList<Architect> IntriguingArchitects
        {
            get => new EntityList<Architect>(_intriguingArchitects.Select(id => Entity<Architect>(id)));
            set => _intriguingArchitects = value.Select(e => e.ID).ToList();
        }

        private List<int> _currentlyMarkedRegions = new List<int>();
        public EntityList<Region> CurrentlyMarkedRegions
        {
            get => new EntityList<Region>(_currentlyMarkedRegions.Select(id => Entity<Region>(id)));
            set => _currentlyMarkedRegions = value.Select(e => e.ID).ToList();
        }

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

        public Party(EntityList<Architect> architects, string type, Architect leader, Location Basee) : base(architects, type, leader, Basee)
        {
            foreach(Architect a in architects)
            {
                a.HairID = Game1.r.Next(0,2);


                EntityList<Material> m = new EntityList<Material>() { Game1.GameWorld.Metals[Game1.r.Next(Game1.GameWorld.Metals.Count)] };

                a.MainHeldObject = Game1.GenerateRandomWeapon(Game1.GameWorld.Metals[0], "common");

                int Count = Game1.r.Next(10, 21);

                for (int i = 0; i < Count; i++)
                {
                    a.Inventory.Add(new Object(null, "fragment", new EntityList<Material>() { Game1.GameWorld.Vitalium }, null));
                }

                int healingItem = Game1.r.Next(1, 4); // Adjust the range based on the number of healing items available

                switch (healingItem)
                {
                    case 1:
                        // Adding a salve
                        a.Inventory.Add(new Object(null, "salve", new EntityList<Material>() { a.Location.Region.HarvestableFiber }, null));
                        break;
                    case 2:
                        // Adding a bandage
                        a.Inventory.Add(new Object(null, "bandage", new EntityList<Material>() { a.Location.HomeCivilization.CulturalCloth }, null));
                        break;
                    case 3:
                        // Adding a vitality vial
                        a.Inventory.Add(new Object(null, "vial", new EntityList<Material>() { Game1.GameWorld.Glass, Game1.GameWorld.Vitalium }, null));
                        break;
                }
            }
        }
    }
}
