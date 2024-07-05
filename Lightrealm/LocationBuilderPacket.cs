using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class LocationBuilderPacket : Entity
    {
        private int _governmentId;

        [JsonIgnore]
        public Entity Government
        {
            get
            {
                if (GovernmentAsGroup != null)
                {
                    return GovernmentAsGroup;
                }
                else if (GovernmentAsArchitect != null)
                {
                    return GovernmentAsArchitect;
                }
                else
                {
                    return null; // or handle as needed if neither type matches
                }
            }
            set => _governmentId = value?.ID ?? 0;
        }

        [JsonIgnore]
        private Group GovernmentAsGroup => EntityGet<Group>(_governmentId);

        [JsonIgnore]
        private Architect GovernmentAsArchitect => EntityGet<Architect>(_governmentId);

        public int X { get; set; }
        public int Z { get; set; }

        private int _primaryRaceId;

        [JsonIgnore]
        public Race PrimaryRace
        {
            get => EntityGet<Race>(_primaryRaceId);
            set => _primaryRaceId = value?.ID ?? 0;
        }

        public int MiscPopulation { get; set; }
        public int ColonizationDesire { get; set; }

        private int _homeCivilizationId;

        [JsonIgnore]
        public Civilization HomeCivilization
        {
            get => EntityGet<Civilization>(_homeCivilizationId);
            set => _homeCivilizationId = value?.ID ?? 0;
        }

        public string Type { get; set; }
        public EntityList<Object> Artifacts { get; set; } = new EntityList<Object>();

        private int _baseLocationId;

        [JsonIgnore]
        public Location BaseLocation
        {
            get => EntityGet<Location>(_baseLocationId);
            set => _baseLocationId = value?.ID ?? 0;
        }

        public string Dockside { get; set; }

        public LocationBuilderPacket(Entity government, int x, int z, string locationType, Race primaryRace, int miscPopulation, int colonizationDesire, Civilization homeCiv, EntityList<Object> artifacts, Location originalLocationYouAreComingFrom, string dockside)
        {
            BaseLocation = originalLocationYouAreComingFrom;
            Type = locationType;
            Government = government;
            X = x;
            Z = z;
            PrimaryRace = primaryRace;
            MiscPopulation = miscPopulation;
            ColonizationDesire = colonizationDesire;
            HomeCivilization = homeCiv;
            Artifacts = artifacts;
            Dockside = dockside;
        }

        public LocationBuilderPacket()
        {
        }
    }
}
