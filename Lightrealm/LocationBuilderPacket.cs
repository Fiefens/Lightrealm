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

        private Group GovernmentAsGroup => EntityGet<Group>(_governmentId);
        private Architect GovernmentAsArchitect => EntityGet<Architect>(_governmentId);


        public int X { get; set; }
        public int Z { get; set; }

        public int MiscPopulation { get; set; }
        public int ColonizationDesire { get; set; }
        public Race PrimaryRace;
        public Civilization HomeCivilization;
        public Location BaseLocation;

        public string Type { get; set; }
        public EntityList<Object> Artifacts { get; set; } = new EntityList<Object>();

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
