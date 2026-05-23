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
        public string Type { get; set; } // city<10000, town <1000, village<200, camp <10
        public Race PrimaryRace { get; set; } = Game1.GameWorld.GetRace("");

        public bool Explored { get; set; } = false;

        public EntityList<District> DistrictsToAdd { get; set; } = new EntityList<District>();

        public EntityList<District> Districts { get; set; } = new EntityList<District>();

        public EntityList<Entity> Embezzlements { get; set; } = new EntityList<Entity>();

        public Structure Market;
        public Structure Library;
        public Structure Prism;

        public bool FellPreviously { get; set; } = false;

        public EntityList<Architect> BurialArchitects { get; set; } = new EntityList<Architect>();

        public string Color { get; set; }

        public string Layout { get; set; } = "";

        public string Dockside { get; set; } = "none";

        public EntityList<Division> Divisions { get; set; } = new EntityList<Division>();

        public bool Active { get; set; } = false;
        public bool IsSavingUpToSettle { get; set; } = false;

        public EntityList<Structure> AllStructures { get; set; } = new EntityList<Structure>();

        public List<string> PrimaryLightingStyles { get; set; } = new List<string>();

        public int Wealth { get; set; } // value is measured in Shobes, an arbitrary unit
        public int PassiveStructuralIncome { get; set; } // value is measured in Shobes, an arbitrary unit

        public EntityList<Objective> Objectives = new EntityList<Objective>();

        public Civilization HomeCivilization;

        public int ColonizationDesire { get; set; }
        public int MaxColonizationDesire { get; set; }

        public bool IsCapitol { get; set; } = false;

        public int AnimalsInNetwork = 0;
        public Race AnimalRace;

        public EntityList<Architect> DebtShibas { get; set; } = new EntityList<Architect>();

        public EntityList<Group> TradersAtThisLocation { get; set; } = new EntityList<Group>();

        public EntityList<Group> TradersAtThisLocationToRemove { get; set; } = new EntityList<Group>();

        public Race GuardianType { get; set; }
        public int GuardiansInNetwork { get; set; }

        public EntityList<Group> GroupsAtThisLocation { get; set; } = new EntityList<Group>();

        public EntityList<Group> GroupsAtThisLocationToRemove { get; set; } = new EntityList<Group>();

        public int X { get; set; }
        public int Z { get; set; }
        public Entity Government;
        public Region Region;

        // THESE VALUES ARE USED IF THE LOCATION IS LOADED

        public int TruePopulation() =>
            Districts.Sum(d => d.UnplacedPopulation + d.DistrictArchitects.Count(c => c.Profession != "trader" && c.IsAlive));



        public Location(string type, Race primaryrace, int population, int wealth, int colonizationDesire, int x, int z, Civilization HomeCiv, Region r, string dockside)
        {
            Region = r;
            Type = type;
            Dockside = dockside;

            if (Type == "spire")
            {
                //if you want a different name scheme use this but whatever for now works
                Name = Game1.GameWorld.GenerateUniqueName("1S" + (Game1.GameWorld.rnd.Next(2, 4)) + "s1w", this, Game1.GameWorld.rnd);
            }
            else
            {
                Name = Game1.GameWorld.GenerateUniqueName("1S" + (Game1.GameWorld.rnd.Next(2, 4)) + "s1w", this, Game1.GameWorld.rnd);
            }

            ReferredToNames.Clear();
            AddReferredToName(Name);
            AddReferredToName(ID.ToString());

            if(Game1.GameWorld.ProcgenStructures.Contains(this.Type))
            {
                Color = new List<string>() { "white", "gray", "black", "brown", "maroon" }[Game1.GameWorld.rnd.Next(5)];
                Layout = new List<string>() { "archway", "hallway", "toroid", "towers", "pyramid" }[Game1.GameWorld.rnd.Next(5)];

                AnimalsInNetwork = Game1.GameWorld.rnd.Next(3, 6); // Some Creatures
                AnimalRace = Game1.GameWorld.WildRaces[Game1.GameWorld.rnd.Next(Game1.GameWorld.WildRaces.Count)];
            }

            PrimaryRace = primaryrace;
            Wealth = wealth;
            ColonizationDesire = colonizationDesire;
            MaxColonizationDesire = colonizationDesire;
            X = x;
            Z = z;
            HomeCivilization = HomeCiv;
            Districts.Add(new District(true, this, population));

            PrimaryLightingStyles = new List<string>
            {
                Game1.LightingStyles[Game1.GameWorld.rnd.Next(Game1.LightingStyles.Count())]
            };

            switch (Type)
            {
                case "commune":
                    GuardiansInNetwork = Game1.GameWorld.rnd.Next(3, 6); // Half of 10-13 rooms
                    break;
                case "stronghold":
                    GuardiansInNetwork = Game1.GameWorld.rnd.Next(8, 15); // MOAR
                    break;
                case "monument":
                    GuardiansInNetwork = Game1.GameWorld.rnd.Next(5, 11); // Half of 20-30 rooms
                    break;
                case "monastery":
                    GuardiansInNetwork = Game1.GameWorld.rnd.Next(0, 2); // Half of 0-4 rooms
                    break;
                case "sanctum":
                    GuardiansInNetwork = Game1.GameWorld.rnd.Next(8, 15); // a lottteee
                    break;
                default:
                    GuardiansInNetwork = 0;
                    break;
            }

            Game1.GameWorld.SubjectsToWriteAbout.Add(this);




            GuardianType = Game1.GameWorld.ConstructRaces[Game1.GameWorld.rnd.Next(Game1.GameWorld.ConstructRaces.Count())];
        }
        public Location()
        {

        }
    }
}
