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
        public string Type { get; set; } //city<10000, town <1000, village<200, camp <10
        public Race PrimaryRace { get; set; } = Game1.GameWorld.GetRace("");

        public bool Explored = false;

        public List<District> DistrictsToAdd { get; set; } = new List<District>();
        public List<District> Districts { get; set; } = new List<District>();

        public List<(Entity, int)> Embezzlements = new List<(Entity, int)>();

        public Structure Market;
        public Structure Prism;

        public bool Active { get; set; } = false;
        public bool IsSavingUpToSettle { get; set; } = false;

        public List<Structure> AllStructures { get; set; } = new List<Structure>();

        public List<string> PrimaryLightingStyles { get; set; } = new List<string>();

        public List<string> LocationHistoricalEvents { get; set; } = new List<string>();
        public int Wealth { get; set; } // value is measured in Shobes, an arbitrary unit
        public Civilization HomeCivilization { get; set; }
        public int ColonizationDesire { get; set; }
        public int MaxColonizationDesire { get; set; }

        public bool IsCapital = false;

        public List<Architect> DebtShibas = new List<Architect>();

        public List<Group> TradersAtThisLocation { get; set; } = new List<Group>();
        public List<Group> TradersAtThisLocationToRemove { get; set; } = new List<Group>();
        public List<Group> TradersAtThisLocationToAdd { get; set; } = new List<Group>();


        public List<Group> GroupsAtThisLocation { get; set; } = new List<Group>();
        public List<Group> GroupsAtThisLocationToRemove { get; set; } = new List<Group>();

        public List<Object> UnplacedArtifacts { get; set; } = new List<Object>();

        public int X { get; set; }
        public int Z { get; set; }

        public Entity Government { get; set; }

        public Region Region;

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


        
        public Location(string type, Race primaryrace, int population, int wealth, int colonizationDesire, int x, int z, Civilization HomeCiv, Region r)
        {
            Region = r;
            Type = type;

            if (Type == "spire")
            {
                //if you want a different name scheme use this but whatever for now works
                Name = r.World.GenerateUniqueName("1S" + (Game1.r.Next(2, 4)) + "s1w", this);
            }
            else
            {
                Name = r.World.GenerateUniqueName("1S" + (Game1.r.Next(2, 4)) + "s1w", this);
            }

            PrimaryRace = primaryrace;
            Wealth = wealth;
            ColonizationDesire = colonizationDesire;
            MaxColonizationDesire = colonizationDesire;
            X = x;
            Z = z;
            HomeCivilization = HomeCiv;
            Districts.Add(new District(true, this, population));

            if(Game1.GameWorld.SettlementTypes.Contains(this.Type))
            {
                Explored = true;
            }

            PrimaryLightingStyles = new List<string>
            {
                Game1.LightingStyles[Game1.r.Next(Game1.LightingStyles.Count)]
            };
        }
        public Location()
        {

        }
    }
}

