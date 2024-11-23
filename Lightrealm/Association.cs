using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Association : Entity
    {
        public EntityList<Architect> Associates { get; set; } = new EntityList<Architect>();
        public EntityList<Party> Parties { get; set; } = new EntityList<Party>();
        public Party ActiveParty { get; set; }
        public EntityList<Structure> Residences = new EntityList<Structure>();

        public EntityList<Object> Resources = new EntityList<Object>();


        public bool HasAttacked = false;
        public int RanCommands { get; set; } = 0;
        public bool UsedThis { get; set; } = false;
        public bool ReceivedPartyAdvice { get; set; } = false;


        public int AscendantX = 0;
        public int AscendantZ = 0;

        public int LatestHistoricalAnalysisIndex = 0;

        public (Color, bool) TestForRelevance(Event Event)
        {
            bool isRelevant = false;
            bool hasEnemyRelevance = false;
            bool isLocationRelevant = false;

            foreach (Architect a in Associates)
            {
                if (Event.Entities.Contains(a))
                {
                    isRelevant = true;
                    break;
                }
            }

            if (!isRelevant)
            {
                foreach (Party p in Parties)
                {
                    if (Event.Entities.Contains(p))
                    {
                        isRelevant = true;
                        break;
                    }
                }
            }

            if (!isRelevant)
            {
                foreach (Location l in Game1.GameWorld.AllLocations)
                {
                    if (Event.EventData.Contains(l.Name) && Parties.Any(p => p.Leader.Location == l))
                    {
                        isRelevant = true;
                        isLocationRelevant = true;
                        break;
                    }
                }
            }

            if (!isRelevant && Event.Entities.Contains(this))
            {
                isRelevant = true;
            }

            if (Event.Entities.Any(e => this.Enemies.Contains(e)))
            {
                hasEnemyRelevance = true;
            }

            if (hasEnemyRelevance && isRelevant)
            {
                return (Color.Goldenrod, true);
            }
            else if (hasEnemyRelevance)
            {
                return (Color.Red, true);
            }
            else if (isRelevant)
            {
                if (isLocationRelevant)
                {
                    return (Color.Blue, true);
                }
                return (Color.Green, true);
            }

            return (Color.Transparent, false);
        }



        public Association(EntityList<Architect> architects, string type, Architect leader, Location basee, bool FirstAssoc)
        {
            // Create a new Party
            ActiveParty = new Party(architects, type, leader, basee);
            // Add it to the Parties list
            Parties.Add(ActiveParty);

            Name = Game1.GameWorld.GenerateUniqueName("1S" + Game1.r.Next(1, 3) + "s1w", this);

            // Execute the original Party logic if FirstAssoc is true
            if (FirstAssoc)
            {
                foreach (Architect a in architects)
                {
                    if(Game1.GameWorld.HumanoidRaces.Contains(a.Race))
                    {
                        a.HairID = Game1.r.Next(0, 2);

                        int count = Game1.r.Next(50, 101);

                        for (int i = 0; i < count; i++)
                        {
                            a.Inventory.Add(new Object(null, "fragment", new EntityList<Material> { Game1.GameWorld.Vitalium }, null));
                        }

                        // Uncommented logic as per your original Party constructor
                        int healingItem = Game1.r.Next(1, 4);

                        switch (healingItem)
                        {
                            case 1:
                                a.Inventory.Add(new Object(null, "salve", new EntityList<Material> { a.Location.Region.HarvestableFiber }, null));
                                break;
                            case 2:
                                a.Inventory.Add(new Object(null, "bandage", new EntityList<Material> { a.Location.HomeCivilization.CulturalCloth }, null));
                                break;
                            case 3:
                                a.Inventory.Add(new Object(null, "vial", new EntityList<Material> { Game1.GameWorld.Glass, Game1.GameWorld.Vitalium }, null));
                                break;
                        }
                    }
                }
            }
        }
    }
}
