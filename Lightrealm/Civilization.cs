using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Civilization : Entity
    {
        //nightfell, luminarch, lost
        public Race PrimaryInhabiantRace { get; set; }
        public int StartX { get; set; }
        public int StartZ { get; set; }

        public string Type;
        public string WarType;

        public Material CulturalCloth { get; set; }
        public Material CulturalWood { get; set; }
        public Material CulturalStone { get; set; }
        public Material CulturalMetal { get; set; }
        public Material CulturalGemstone { get; set; }
        public Material CulturalSheet { get; set; }

        public int ElectionFrequency = Game1.r.Next(1401520000, 1501520000);
        public Dictionary<string, int> HatredPoints { get; set; } = new Dictionary<string, int>();

        public int CyclesTillElection = 0;

        public List<Architect> Citizens = new List<Architect>();
        public Architect Alpha = null;

        public int WakeUpAndChooseViolencePoints = 0;

        public List<Unit> UnitsAtCommand = new List<Unit>();

        public string CulturalHeadwear = Game1.Headwear[Game1.r.Next(Game1.Headwear.Count)];
        public string CulturalNeckwear = Game1.Neckwear[Game1.r.Next(Game1.Neckwear.Count)];
        public string CulturalBodywear = Game1.Bodywear[Game1.r.Next(Game1.Bodywear.Count)];
        public string CulturalLegwear = Game1.Legwear[Game1.r.Next(Game1.Legwear.Count)];
        public string CulturalHandwear = Game1.Handwear[Game1.r.Next(Game1.Handwear.Count)];
        public string CulturalFootwear = Game1.Footwear[Game1.r.Next(Game1.Footwear.Count)];

        public World World { get; set; }

        public Location Capitol;

        public string Color;

        public Civilization(Race race, string type, int Startx, int Startz, World world)
        {
            Random r = new Random();
            PrimaryInhabiantRace = race;
            World = world;
            Name = World.GenerateUniqueName("1s" + Game1.r.Next(2, 6) + "s", this);
            Name = Name.Substring(0,1).ToUpper() + Name.Substring(1);
            StartX = Startx;
            StartZ = Startz;

            Type = type;
            WarType = (type == "luminarch" || type == "nightfell") ? "civilized" : type;

            int Index = r.Next(World.UnusedCivColors.Count);

            if(race != world.GetRace(""))
            {
                Color = World.UnusedCivColors[Index];
                World.UnusedCivColors.RemoveAt(Index);
            }

            CulturalCloth = world.Cloths[Game1.r.Next(world.Cloths.Count())];
            CulturalWood = world.Woods[Game1.r.Next(world.Woods.Count())];
            CulturalStone = world.Stones[Game1.r.Next(world.Stones.Count())];
            CulturalMetal = world.Metals[Game1.r.Next(world.Metals.Count())];
            CulturalGemstone = world.Gemstones[Game1.r.Next(world.Gemstones.Count())];
            CulturalSheet = world.Sheets[Game1.r.Next(world.Sheets.Count())];

            CyclesTillElection = ElectionFrequency;
        }

        public Civilization()
        {
            //default constructor for serialization
        }
    }
}
