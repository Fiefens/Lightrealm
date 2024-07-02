using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Location : Entity
    {
        public static T Entity<T>(int entityId) where T : Entity
        {
            if (Game1.GameWorld == null || Game1.GameWorld.AllEntities == null)
            {
                return (T)Convert.ChangeType(Game1.TemporaryEntities[entityId], typeof(T));
            }

            return (T)Convert.ChangeType(Game1.GameWorld.AllEntities[entityId], typeof(T));
        }

        public string Type { get; set; } // city<10000, town <1000, village<200, camp <10
        public Race PrimaryRace { get; set; } = Game1.GameWorld.GetRace("");

        public bool Explored { get; set; } = false;

        private List<int> _districtsToAdd = new List<int>();
        public EntityList<District> DistrictsToAdd
        {
            get => new EntityList<District>(_districtsToAdd.Select(id => Entity<District>(id)));
            set => _districtsToAdd = value?.Select(e => e.ID).ToList() ?? new List<int>();
        }

        private List<int> _districts = new List<int>();
        public EntityList<District> Districts
        {
            get => new EntityList<District>(_districts.Select(id => Entity<District>(id)));
            set => _districts = value?.Select(e => e.ID).ToList() ?? new List<int>();
        }

        private List<(int, int)> _embezzlements = new List<(int, int)>();
        public List<(Entity, int)> Embezzlements
        {
            get => _embezzlements.Select(tuple => (Entity<Entity>(tuple.Item1), tuple.Item2)).ToList();
            set => _embezzlements = value.Select(tuple => (tuple.Item1.ID, tuple.Item2)).ToList();
        }

        private int _marketId;
        public Structure Market
        {
            get => Entity<Structure>(_marketId);
            set => _marketId = value?.ID ?? 0;
        }

        private int _libraryId;
        public Structure Library
        {
            get => Entity<Structure>(_libraryId);
            set => _libraryId = value?.ID ?? 0;
        }

        private int _prismId;
        public Structure Prism
        {
            get => Entity<Structure>(_prismId);
            set => _prismId = value?.ID ?? 0;
        }

        public string Color { get; set; }

        public string Layout { get; set; } = "";

        public string Dockside { get; set; } = "none";

        private List<int> _units = new List<int>();
        public EntityList<Unit> Units
        {
            get => new EntityList<Unit>(_units.Select(id => Entity<Unit>(id)));
            set => _units = value?.Select(e => e.ID).ToList() ?? new List<int>();
        }

        public bool Active { get; set; } = false;
        public bool IsSavingUpToSettle { get; set; } = false;

        private List<int> _allStructures = new List<int>();
        public EntityList<Structure> AllStructures
        {
            get => new EntityList<Structure>(_allStructures.Select(id => Entity<Structure>(id)));
            set => _allStructures = value?.Select(e => e.ID).ToList() ?? new List<int>();
        }

        public List<string> PrimaryLightingStyles { get; set; } = new List<string>();

        public List<string> LocationHistoricalEvents { get; set; } = new List<string>();
        public int Wealth { get; set; } // value is measured in Shobes, an arbitrary unit

        private int _homeCivilizationId;
        public Civilization HomeCivilization
        {
            get => Entity<Civilization>(_homeCivilizationId);
            set => _homeCivilizationId = value?.ID ?? 0;
        }

        public int ColonizationDesire { get; set; }
        public int MaxColonizationDesire { get; set; }

        public bool IsCapitol { get; set; } = false;

        private List<int> _debtShibas = new List<int>();
        public EntityList<Architect> DebtShibas
        {
            get => new EntityList<Architect>(_debtShibas.Select(id => Entity<Architect>(id)));
            set => _debtShibas = value?.Select(e => e.ID).ToList() ?? new List<int>();
        }

        private List<int> _tradersAtThisLocation = new List<int>();
        public EntityList<Group> TradersAtThisLocation
        {
            get => new EntityList<Group>(_tradersAtThisLocation.Select(id => Entity<Group>(id)));
            set => _tradersAtThisLocation = value?.Select(e => e.ID).ToList() ?? new List<int>();
        }

        private List<int> _tradersAtThisLocationToRemove = new List<int>();
        public EntityList<Group> TradersAtThisLocationToRemove
        {
            get => new EntityList<Group>(_tradersAtThisLocationToRemove.Select(id => Entity<Group>(id)));
            set => _tradersAtThisLocationToRemove = value?.Select(e => e.ID).ToList() ?? new List<int>();
        }

        private List<int> _tradersAtThisLocationToAdd = new List<int>();
        public EntityList<Group> TradersAtThisLocationToAdd
        {
            get => new EntityList<Group>(_tradersAtThisLocationToAdd.Select(id => Entity<Group>(id)));
            set => _tradersAtThisLocationToAdd = value?.Select(e => e.ID).ToList() ?? new List<int>();
        }

        public Race GuardianType { get; set; }
        public int GuardiansInNetwork { get; set; }

        private List<int> _groupsAtThisLocation = new List<int>();
        public EntityList<Group> GroupsAtThisLocation
        {
            get => new EntityList<Group>(_groupsAtThisLocation.Select(id => Entity<Group>(id)));
            set => _groupsAtThisLocation = value?.Select(e => e.ID).ToList() ?? new List<int>();
        }

        private List<int> _groupsAtThisLocationToRemove = new List<int>();
        public EntityList<Group> GroupsAtThisLocationToRemove
        {
            get => new EntityList<Group>(_groupsAtThisLocationToRemove.Select(id => Entity<Group>(id)));
            set => _groupsAtThisLocationToRemove = value?.Select(e => e.ID).ToList() ?? new List<int>();
        }

        private List<int> _unplacedArtifacts = new List<int>();
        public EntityList<Object> UnplacedArtifacts
        {
            get => new EntityList<Object>(_unplacedArtifacts.Select(id => Entity<Object>(id)));
            set => _unplacedArtifacts = value?.Select(e => e.ID).ToList() ?? new List<int>();
        }

        public int X { get; set; }
        public int Z { get; set; }

        private int _governmentId;
        public Entity Government
        {
            get => Entity<Entity>(_governmentId);
            set => _governmentId = value?.ID ?? 0;
        }

        private int _regionId;
        public Region Region
        {
            get => Entity<Region>(_regionId);
            set => _regionId = value?.ID ?? 0;
        }


        // THESE VALUES ARE USED IF THE LOCATION IS LOADED


        public int TruePopulation()
        {
            int Population = 0;

            foreach(District d in Districts)
            {
                Population = Population + d.UnplacedPopulation;
                foreach(Architect a in d.Architects)
                {
                    if((a.Group != null && a.Group.Type != "trade") || a.Group == null)
                    {
                        Population++;
                    }
                }
            }

            return Population;
        }


        
        public Location(string type, Race primaryrace, int population, int wealth, int colonizationDesire, int x, int z, Civilization HomeCiv, Region r, string dockside)
        {
            Region = r;
            Type = type;
            Dockside = dockside;

            if (Type == "spire")
            {
                //if you want a different name scheme use this but whatever for now works
                Name = r.World.GenerateUniqueName("1S" + (Game1.r.Next(2, 4)) + "s1w", this);
            }
            else
            {
                Name = r.World.GenerateUniqueName("1S" + (Game1.r.Next(2, 4)) + "s1w", this);
            }

            AddReferredToName(Name);

            PrimaryRace = primaryrace;
            Wealth = wealth;
            ColonizationDesire = colonizationDesire;
            MaxColonizationDesire = colonizationDesire;
            X = x;
            Z = z;
            HomeCivilization = HomeCiv;
            Districts.Add(new District(true, this, population));

            if(Game1.GameWorld.SettlementTypes.Contains(this.Type) || this.Type == "spire" )
            {
                Explored = true;
            }

            PrimaryLightingStyles = new List<string>
            {
                Game1.LightingStyles[Game1.r.Next(Game1.LightingStyles.Count)]
            };


            if (new List<string> { "archway", "commune", "stronghold", "monastery", "towers", "sanctum" }.Contains(Type))
            {
                switch (Type)
                {
                    case "archway":
                        GuardiansInNetwork = Game1.r.Next(5, 10); // Half of 10-20 rooms
                        break;
                    case "commune":
                        GuardiansInNetwork = Game1.r.Next(5, 7); // Half of 10-13 rooms
                        break;
                    case "stronghold":
                        GuardiansInNetwork = Game1.r.Next(10, 15); // Half of 20-30 rooms
                        break;
                    case "monastery":
                        GuardiansInNetwork = Game1.r.Next(0, 2); // Half of 0-4 rooms
                        break;
                    case "towers":
                        GuardiansInNetwork = Game1.r.Next(5, 10); // Half of 10-20 rooms
                        break;
                    case "sanctum":
                        GuardiansInNetwork = Game1.r.Next(15, 30); // a lottteee
                        break;
                    default:
                        GuardiansInNetwork = 0;
                        break;
                }

                GuardianType = Game1.GameWorld.ConstructRaces[Game1.r.Next(Game1.GameWorld.ConstructRaces.Count)];
            }

        }
        public Location()
        {

        }
    }
}

