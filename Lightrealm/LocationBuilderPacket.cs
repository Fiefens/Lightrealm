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

        private int _governmentId;
        public Entity Government
        {
            get => Entity<Entity>(_governmentId);
            set => _governmentId = value?.ID ?? 0;
        }

        public int X { get; set; }
        public int Z { get; set; }

        private int _primaryRaceId;
        public Race PrimaryRace
        {
            get => Entity<Race>(_primaryRaceId);
            set => _primaryRaceId = value?.ID ?? 0;
        }

        public int MiscPopulation { get; set; }
        public int ColonizationDesire { get; set; }

        private int _homeCivilizationId;
        public Civilization HomeCivilization
        {
            get => Entity<Civilization>(_homeCivilizationId);
            set => _homeCivilizationId = value?.ID ?? 0;
        }

        public string Type { get; set; }

        public EntityList<Object> Artifacts { get; set; } = new EntityList<Object>();

        private int _baseLocationId;
        public Location BaseLocation
        {
            get => Entity<Location>(_baseLocationId);
            set => _baseLocationId = value?.ID ?? 0;
        }

        public string Dockside { get; set; }



        public LocationBuilderPacket(Entity government, int x, int z, string locationType, Race primaryRace, int miscPopulation, int colonizationDesire, Civilization HomeCiv, EntityList<Object> artifacts, Location OriginalLocationYouAreComingFrom, string dockside)
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