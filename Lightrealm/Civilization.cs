using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Civilization : Entity
    {
        public Race PrimaryInhabitantRace;
        public int StartX { get; set; }
        public int StartZ { get; set; }
        public string Type { get; set; }
        public string WarType { get; set; }
        public Material CulturalCloth;
        public Material CulturalWood;
        public Material CulturalStone;
        public Material CulturalMetal;
        public Material CulturalGemstone;
        public Material CulturalSheet;


        public int ElectionFrequency { get; set; } = Game1.GameWorld.rnd.Next(1401520000, 1501520000);
        public Dictionary<string, int> HatredPoints { get; set; } = new Dictionary<string, int>();
        public int CyclesTillElection { get; set; } = 0;
        public EntityList<Architect> Citizens { get; set; } = new EntityList<Architect>();
        public Architect Alpha;
        public Location Capitol;


        public int WakeUpAndChooseViolencePoints { get; set; } = 0;
        public EntityList<Division> MillitaryDivisions { get; set; } = new EntityList<Division>();

        public string CulturalHeadwear { get; set; } = Game1.Headwear[Game1.GameWorld.rnd.Next(Game1.Headwear.Count())];
        public string CulturalNeckwear { get; set; } = Game1.Neckwear[Game1.GameWorld.rnd.Next(Game1.Neckwear.Count())];
        public string CulturalBodywear { get; set; } = Game1.Bodywear[Game1.GameWorld.rnd.Next(Game1.Bodywear.Count())];
        public string CulturalLegwear { get; set; } = Game1.Legwear[Game1.GameWorld.rnd.Next(Game1.Legwear.Count())];
        public string CulturalHandwear { get; set; } = Game1.Handwear[Game1.GameWorld.rnd.Next(Game1.Handwear.Count())];
        public string CulturalFootwear { get; set; } = Game1.Footwear[Game1.GameWorld.rnd.Next(Game1.Footwear.Count())];

        public Civilization(Race race, string type, int Startx, int Startz, World world)
        {
            
            PrimaryInhabitantRace = race;
            Game1.GameWorld = world;
            Name = Game1.GameWorld.GenerateUniqueName("1s" + Game1.GameWorld.rnd.Next(2, 6) + "s", this, Game1.GameWorld.rnd);
            Name = Name.Substring(0, 1).ToUpper() + Name.Substring(1);
            StartX = Startx;
            StartZ = Startz;

            if (type == "druid")
            {
                CulturalHeadwear = "none";
                CulturalNeckwear = "none";
                CulturalBodywear = "straps/left wristwrap/right wristwrap";
                CulturalLegwear = "none";
                CulturalHandwear = "none";
                CulturalFootwear = "none";
            }

            Type = type;
            WarType = (type == "luminarch" || type == "nightfell") ? "civilized" : type;

            int Index = Game1.GameWorld.rnd.Next(Game1.GameWorld.UnusedCivColors.Count());

            if (race != world.GetRace(""))
            {
                EntityColor = Game1.GameWorld.UnusedCivColors[Index];
                Game1.GameWorld.UnusedCivColors.RemoveAt(Index);
            }

            CulturalCloth = world.Cloths[Game1.GameWorld.rnd.Next(world.Cloths.Count)];
            CulturalWood = world.Woods[Game1.GameWorld.rnd.Next(world.Woods.Count)];
            CulturalStone = world.Stones[Game1.GameWorld.rnd.Next(world.Stones.Count)];

            var eligibleMetals = world.Metals.Where(m => m.Toughness <= 8).ToList();
            if (eligibleMetals.Any())
            {
                CulturalMetal = eligibleMetals[Game1.GameWorld.rnd.Next(eligibleMetals.Count)];
            }
            else
            {
                CulturalMetal = null; // Or handle case when no eligible metal exists
            }

            CulturalGemstone = world.Gemstones[Game1.GameWorld.rnd.Next(world.Gemstones.Count)];
            CulturalSheet = world.Sheets[Game1.GameWorld.rnd.Next(world.Sheets.Count)];

            CyclesTillElection = ElectionFrequency;
        }

        public Civilization()
        {
            //default constructor for serialization
        }
    }
}
