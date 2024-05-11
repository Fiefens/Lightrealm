using Lightrealm.GameEngine;
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

        public Material CulturalCloth { get; set; }
        public Material CulturalWood { get; set; }
        public Material CulturalStone { get; set; }
        public Material CulturalMetal { get; set; }
        public Material CulturalGemstone { get; set; }
        public Material CulturalSheet { get; set; }

        public int ElectionFrequency = Game1.r.Next(1401520000, 1501520000);

        public int CyclesTillElection = 0;

        public List<Architect> Citizens = new List<Architect>();
        public Architect Alpha = null;

        public List<Civilization> CivsAtWar = new List<Civilization>();
        public int WakeUpAndChooseViolencePoints = 0;

        public string CulturalHeadwear = Engine.Data.Headwear[Game1.r.Next(Engine.Data.Headwear.Count)];
        public string CulturalNeckwear = Engine.Data.Neckwear[Game1.r.Next(Engine.Data.Neckwear.Count)];
        public string CulturalBodywear = Engine.Data.Bodywear[Game1.r.Next(Engine.Data.Bodywear.Count)];
        public string CulturalLegwear = Engine.Data.Legwear[Game1.r.Next(Engine.Data.Legwear.Count)];
        public string CulturalHandwear = Engine.Data.Handwear[Game1.r.Next(Engine.Data.Handwear.Count)];
        public string CulturalFootwear = Engine.Data.Footwear[Game1.r.Next(Engine.Data.Footwear.Count)];

        public World World { get; set; }

        public Location Capitol;

        public string Color;

        public Group Leader { get; set; }

        public Civilization(Race race, int Startx, int Startz, World world)
        {
            Random r = new Random();
            PrimaryInhabiantRace = race;
            World = world;
            Name = World.GenerateUniqueName("1W" + Game1.r.Next(2, 4) + "s", this);
            Name = Name.Substring(0,1).ToUpper() + Name.Substring(1);
            StartX = Startx;
            StartZ = Startz;


            int Index = r.Next(World.UnusedCivColors.Count);
            Color = World.UnusedCivColors[Index];
            World.UnusedCivColors.RemoveAt(Index);

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
