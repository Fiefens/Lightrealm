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
        private int _primaryInhabitantRaceId;

        [JsonIgnore]
        public Race PrimaryInhabitantRace
        {
            get => EntityGet<Race>(_primaryInhabitantRaceId);
            set => _primaryInhabitantRaceId = value?.ID ?? 0;
        }

        public int StartX { get; set; }
        public int StartZ { get; set; }

        public string Type { get; set; }
        public string WarType { get; set; }

        private int _culturalClothId;

        [JsonIgnore]
        public Material CulturalCloth
        {
            get => EntityGet<Material>(_culturalClothId);
            set => _culturalClothId = value?.ID ?? 0;
        }

        private int _culturalWoodId;

        [JsonIgnore]
        public Material CulturalWood
        {
            get => EntityGet<Material>(_culturalWoodId);
            set => _culturalWoodId = value?.ID ?? 0;
        }

        private int _culturalStoneId;

        [JsonIgnore]
        public Material CulturalStone
        {
            get => EntityGet<Material>(_culturalStoneId);
            set => _culturalStoneId = value?.ID ?? 0;
        }

        private int _culturalMetalId;

        [JsonIgnore]
        public Material CulturalMetal
        {
            get => EntityGet<Material>(_culturalMetalId);
            set => _culturalMetalId = value?.ID ?? 0;
        }

        private int _culturalGemstoneId;

        [JsonIgnore]
        public Material CulturalGemstone
        {
            get => EntityGet<Material>(_culturalGemstoneId);
            set => _culturalGemstoneId = value?.ID ?? 0;
        }

        private int _culturalSheetId;

        [JsonIgnore]
        public Material CulturalSheet
        {
            get => EntityGet<Material>(_culturalSheetId);
            set => _culturalSheetId = value?.ID ?? 0;
        }

        public int ElectionFrequency { get; set; } = Game1.r.Next(1401520000, 1501520000);
        public Dictionary<string, int> HatredPoints { get; set; } = new Dictionary<string, int>();
        public int CyclesTillElection { get; set; } = 0;
        public EntityList<Architect> Citizens { get; set; } = new EntityList<Architect>();

        private int _alphaId;

        [JsonIgnore]
        public Architect Alpha
        {
            get => EntityGet<Architect>(_alphaId);
            set => _alphaId = value?.ID ?? 0;
        }

        public int WakeUpAndChooseViolencePoints { get; set; } = 0;
        public EntityList<Unit> UnitsAtCommand { get; set; } = new EntityList<Unit>();

        public string CulturalHeadwear { get; set; } = Game1.Headwear[Game1.r.Next(Game1.Headwear.Count)];
        public string CulturalNeckwear { get; set; } = Game1.Neckwear[Game1.r.Next(Game1.Neckwear.Count)];
        public string CulturalBodywear { get; set; } = Game1.Bodywear[Game1.r.Next(Game1.Bodywear.Count)];
        public string CulturalLegwear { get; set; } = Game1.Legwear[Game1.r.Next(Game1.Legwear.Count)];
        public string CulturalHandwear { get; set; } = Game1.Handwear[Game1.r.Next(Game1.Handwear.Count)];
        public string CulturalFootwear { get; set; } = Game1.Footwear[Game1.r.Next(Game1.Footwear.Count)];

        private int _worldId;

        [JsonIgnore]
        public World World
        {
            get => EntityGet<World>(_worldId);
            set => _worldId = value?.ID ?? 0;
        }

        private int _capitolId;

        [JsonIgnore]
        public Location Capitol
        {
            get => EntityGet<Location>(_capitolId);
            set => _capitolId = value?.ID ?? 0;
        }

        public string Color { get; set; }

        public Civilization(Race race, string type, int Startx, int Startz, World world)
        {
            Random r = new Random();
            PrimaryInhabitantRace = race;
            World = world;
            Name = World.GenerateUniqueName("1s" + Game1.r.Next(2, 6) + "s", this);
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

            int Index = r.Next(World.UnusedCivColors.Count);

            if (race != world.GetRace(""))
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
