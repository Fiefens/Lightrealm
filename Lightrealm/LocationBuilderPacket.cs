using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class LocationBuilderPacket
    {
        public static T Entity<T>(int entityId) where T : Entity
        {
            if (Game1.GameWorld == null || Game1.GameWorld.AllEntities == null)
            {
                return (T)Convert.ChangeType(Game1.TemporaryEntities[entityId], typeof(T));
            }

            return (T)Convert.ChangeType(Game1.GameWorld.AllEntities[entityId], typeof(T));
        }

        public Entity Government { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
        public Race PrimaryRace { get; set; }
        public int MiscPopulation { get; set; }
        public int ColonizationDesire { get; set; }
        public Civilization HomeCivilization { get; set; }
        public string Type { get; set; }
        public List<Object> Artifacts { get; set; }
        public Location BaseLocation { get; set; }
        public string Dockside { get; set; }

        public LocationBuilderPacket(Entity government, int x, int z, string locationType, Race primaryRace, int miscPopulation, int colonizationDesire, Civilization HomeCiv, List<Object> artifacts, Location OriginalLocationYouAreComingFrom, string dockside)
        {
            BaseLocation = OriginalLocationYouAreComingFrom;
            Type = locationType;
            Government = government;
            X = x;
            Z = z;
            PrimaryRace = primaryRace;
            MiscPopulation = miscPopulation;
            ColonizationDesire = colonizationDesire;
            HomeCivilization = HomeCiv;
            Artifacts = artifacts;
            Dockside = dockside;
        }
        public LocationBuilderPacket()
        {

        }
    }
}