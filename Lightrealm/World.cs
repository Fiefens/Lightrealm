using Lightrealm;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using Color = Microsoft.Xna.Framework.Color;

namespace Lightrealm
{
    [Serializable]

    public class World : Entity
    {
        public T Entity<T>(int entityId) where T : Entity
        {
            if (Game1.GameWorld == null || Game1.GameWorld.AllEntities == null)
            {
                return (T)Convert.ChangeType(Game1.TemporaryEntities[entityId], typeof(T));
            }

            return (T)Convert.ChangeType(Game1.GameWorld.AllEntities[entityId], typeof(T));
        }

        public List<string> ItemTypesInCirculation = new List<string>();

        public int NextUniqueID = 100;

        public Dictionary<int, Entity> AllEntities = new Dictionary<int, Entity>
        {
            { 0, null }
        };

        public Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        public List<string> DupeMats = new List<string>();

        Dictionary <string, string> groupTypes = new Dictionary<string, string>
                    {
                        { "anarchist", "noticed the anarchistic similarities of both their groups and forged them into one" },
                        { "guild", "recognized their mutual goals and merged to form a stronger guild" },
                        { "scholarly", "found their scholarly pursuits aligned and combined their resources" },
                        { "military", "saw the strategic advantage in uniting their forces" },
                        { "political", "realized their political ambitions were better achieved together" },
                        { "entertainment", "decided that merging their talents would entertain more people" }
                    };

        public EntityList<Entity> AllSpells = new EntityList<Entity>()
        {
            new Entity("water bolt"),
            new Entity("chaos flare"),
            new Entity("concentrated ignition"),
            new Entity("tremor"),
            new Entity("ice shock"),
            // new Entity("immobile illusion"),
            // new Entity("shadow veil"),
            // new Entity("mobile illusion"),
            // new Entity("reactive illusion"),
            new Entity("truthfulness"),
            new Entity("rise"),
            new Entity("hold"),
            new Entity("force throw"),
            new Entity("shatter"),
            new Entity("clone"),
            new Entity("intercept"),
            new Entity("expel"),
            new Entity("extract"),
            new Entity("emergent growth"),
            new Entity("animate"),
            new Entity("immortalize"),
            new Entity("revive"),
            new Entity("resurrect")
        };

        public EntityList<Entity> AllBodyParts = new EntityList<Entity>();

        public EntityList<Entity> Domains = new EntityList<Entity>
        {
            new Entity("shadows"),
            new Entity("life"),
            new Entity("death"),
            new Entity("time"),
            new Entity("stars"),
            new Entity("heat"),
            new Entity("void"),
            new Entity("storms"),
            new Entity("lore"),
            new Entity("mind"),
            new Entity("soul"),
            new Entity("body"),
            new Entity("space"),
            new Entity("reality"),
            new Entity("chaos"),
            new Entity("order"),
            new Entity("nature"),
            new Entity("earth"),
            new Entity("water"),
            new Entity("fire"),
            new Entity("air"),
            new Entity("dreams"),
            new Entity("music"),
            new Entity("war"),
            new Entity("peace"),
            new Entity("fate"),
            new Entity("luck"),
            new Entity("craftsmanship"),
            new Entity("wisdom"),
            new Entity("mountains"),
            new Entity("forests"),
            new Entity("seas"),
            new Entity("rivers"),
            new Entity("deserts"),
            new Entity("skies"),
            new Entity("twilight"),
            new Entity("dusk"),
            new Entity("dawn"),
            new Entity("justice"),
            new Entity("mercy"),
            new Entity("vengeance"),
            new Entity("joy"),
            new Entity("beauty"),
            new Entity("fear"),
            new Entity("courage"),
            new Entity("mystery"),
            new Entity("knowledge"),
            new Entity("exploration"),
            new Entity("civilization"),
            new Entity("wilderness"),
            new Entity("magic"),
            new Entity("art"),
            new Entity("celebration"),
            new Entity("silence"),
            new Entity("echoes"),
            new Entity("decay"),
            new Entity("balance"),
            new Entity("creation"),
            new Entity("destruction"),
            new Entity("power"),
            new Entity("eternity"),
            new Entity("nightmares"),
            new Entity("stability"),
            new Entity("change"),
            new Entity("harmony"),
            new Entity("discord"),
            new Entity("vision"),
            new Entity("memory"),
            new Entity("truth"),
            new Entity("deception"),
            new Entity("hope"),
            new Entity("despair"),
            new Entity("wealth"),
            new Entity("poverty"),
            new Entity("disease"),
            new Entity("youth"),
            new Entity("beginnings"),
            new Entity("endings"),
            new Entity("exile"),
            new Entity("theft"),
            new Entity("victory"),
            new Entity("defeat"),
            new Entity("secrets"),
            new Entity("ruin")
        };


        public EntityList<Entity> AllSkills = new EntityList<Entity>
        {
            // new Entity("deflect"), // temporarily removing because it doesn't do anything for you.
            new Entity("dropkick"),
            new Entity("double strike"),
            new Entity("quick strike"),
            new Entity("severing strike"),
            new Entity("backflip"),
            new Entity("escape"),
            new Entity("finale"),
            new Entity("concentration"),
            new Entity("body slam"),
            new Entity("leg sweep")
        };

        public EntityList<Entity> AllLegendarySpells = new EntityList<Entity>()
        {
            new Entity("ethereal rupture"),
            new Entity("emergence"),
            new Entity("eternal bind"),
            new Entity("expunge"),
            new Entity("echo")
        };

        public int ConvertLevelToToughness(int level)
        {
            if (level < 1 || level > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(level), "Level must be between 1 and 10 inclusive.");
            }

            // Convert level (1-10) to toughness (2-16)
            int minLevel = 1;
            int maxLevel = 10;
            int minToughness = 2;
            int maxToughness = 16;

            int toughness = minToughness + (level - minLevel) * (maxToughness - minToughness) / (maxLevel - minLevel);
            return toughness;
        }
        
        public Dictionary<string, string> GenericHatredDictionary = new Dictionary<string, string>()
        {
            {"civilized", "archaix"},
            {"archaix", "shade"},
            {"shade", "druid"},
            {"druid", "scavenger"},
            {"scavenger", "photonexus"},
            {"photonexus", "anarchist"},
            {"anarchist", "cultist"},
            {"cultist", "isofractal"},
            {"isofractal", "pirate"},
            {"pirate", "civilized"}
        };

        public Dictionary<string, EntityList<Material>> MaterialsFromColors = new Dictionary<string, EntityList<Material>>
        {
            { "maroon", new EntityList<Material>{ new Material("mahogany", "wood", 1, 1, "maroon"), new Material("beetle", "insect", 1, 1, "maroon"), new Material("rust", "metal", 1, 1, "maroon") } },
            { "red", new EntityList<Material>{ new Material("rose", "plant", 1, 1, "red"), new Material("tulip", "plant", 1, 1, "red"), new Material("red coral", "sediment", 1, 1, "red") } },
            { "orange", new EntityList<Material>{ new Material("citrus", "plant", 1, 1, "orange"), new Material("amber", "plant", 1, 1, "orange"), new Material("brimstone", "stone", 1, 1, "orange") } },
            { "yellow", new EntityList<Material>{ new Material("emberflare", "plant", 1, 1, "yellow"), new Material("dandelion", "plant", 1, 1, "yellow"), new Material("lemon", "plant", 1, 1, "yellow") } },
            { "limegreen", new EntityList<Material>{ new Material("lime", "plant", 1, 1, "limegreen"), new Material("emerald grass", "plant", 1, 1, "limegreen"), new Material("verdant wing feather", "feather", 1, 1, "limegreen") } },
            { "green", new EntityList<Material>{ new Material("lichen", "plant", 1, 1, "green"), new Material("cactus", "plant", 1, 1, "green"), new Material("moss", "plant", 1, 1, "green") } },
            { "lightblue", new EntityList<Material>{ new Material("feather", "feather", 1, 1, "lightblue"), new Material("slush", "stone", 1, 1, "lightblue"), new Material("orchid", "gemstone", 1, 1, "lightblue") } },
            { "cyan", new EntityList<Material>{ new Material("algae", "plant", 1, 1, "cyan"), new Material("fish oil", "gemstone", 1, 1, "cyan"), new Material("eel skin", "leather", 1, 1, "cyan") } },
            { "blue", new EntityList<Material>{ new Material("blueberry juice", "fruit", 1, 1, "blue"), new Material("crystalline", "gem", 1, 1, "blue"), new Material("ocean silt", "sediment", 1, 1, "blue") } },
            { "purple", new EntityList<Material>{ new Material("royal grapes", "fruit", 1, 1, "purple"), new Material("shattered amethyst", "gem", 1, 1, "purple"), new Material("mystic flower", "plant", 1, 1, "purple") } },
            { "magenta", new EntityList<Material>{ new Material("wild berry", "fruit", 1, 1, "magenta"), new Material("pink petals", "plant", 1, 1, "magenta"), new Material("rose quartz", "gem", 1, 1, "magenta") } },
            { "coral", new EntityList<Material>{ new Material("pink coral", "coral", 1, 1, "coral"), new Material("sea anemone", "animal", 1, 1, "coral"), new Material("tropical shell", "shell", 1, 1, "coral") } },
            { "white", new EntityList<Material>{ new Material("pure snowflake", "ice", 1, 1, "white"), new Material("moonstone", "gem", 1, 1, "white"), new Material("cloud feathers", "feather", 1, 1, "white") } },
            { "gray", new EntityList<Material>{ new Material("ashen soil", "sediment", 1, 1, "gray"), new Material("smoky quartz", "gem", 1, 1, "gray"), new Material("stormy cloud", "cloud", 1, 1, "gray") } },
            { "black", new EntityList<Material>{ new Material("ground obsidian", "rock", 1, 1, "black"), new Material("midnight rose", "plant", 1, 1, "black"), new Material("nightsilk", "fabric", 1, 1, "black") } },
            { "brown", new EntityList<Material>{ new Material("tree bark", "wood", 1, 1, "brown"), new Material("cocoa bean", "plant", 1, 1, "brown"), new Material("hazel nut", "nut", 1, 1, "brown") } }

            //fix this later but I don't want to right now
        };

        public EntityList<Entity> ExtraEntities = new EntityList<Entity>
        {
            new Entity("tavern"), new Entity("prism"), new Entity("well"), new Entity("shrine"),
            new Entity("library"), new Entity("watchtower"), new Entity("forge"), new Entity("market"),
            new Entity("north"), new Entity("south"), new Entity("east"), new Entity("west"),
            new Entity("up"), new Entity("down"), new Entity("southeast"), new Entity("southwest"),
            new Entity("northeast"), new Entity("northwest"), new Entity("shadow storage"),
            new Entity("relationships"), new Entity("mining"), new Entity("combat"),
            new Entity("crafting"), new Entity("trading"), new Entity("stealth"), new Entity("alchemy"),
            new Entity("cooking"), new Entity("fishing"), new Entity("hunting"), new Entity("quests"),
            new Entity("gathering"), new Entity("imbuement"), new Entity("healing"), new Entity("navigation"),
            new Entity("tactics"), new Entity("survival"), new Entity("diplomacy"), new Entity("lockpicking"),
            new Entity("animal taming"), new Entity("herbalism"), new Entity("herbs"), new Entity("blacksmithing"),
            new Entity("tailoring"), new Entity("carpentry"), new Entity("architecture"), new Entity("history"),
            new Entity("sailing"), new Entity("farming"), new Entity("brewing"), new Entity("divination"),
            new Entity("spellcasting"), new Entity("negotiation"), new Entity("investigation"),
            new Entity("potions"), new Entity("archery"), new Entity("swordsmanship"), new Entity("armor crafting"),
            new Entity("thievery"), new Entity("mountaineering"), new Entity("cartography"),
            new Entity("astronomy"), new Entity("necromancy"), new Entity("spatiomancy"),
            new Entity("conjuromancy"), new Entity("fractalmancy"), new Entity("perceptomancy"),
            new Entity("beasts"), new Entity("divinity"), new Entity("illusion"), new Entity("mechanics"),
            new Entity("engineering"), new Entity("book"), new Entity("poem"), new Entity("song"),
            new Entity("spells"), new Entity("skills"), new Entity("all")
        };

        public bool LostPlaced = false;
        public bool FirstNewCivPlaced = false;
        public bool SecondNewCivPlaced = false;
        public bool ThirdNewCivPlaced = false;

        public bool FirstOutcastCivPlaced = false;
        public bool SecondOutcastCivPlaced = false;
        public bool ThirdOutcastCivPlaced = false;
        public int FirstOutcastCivStartYear = 75 + Game1.r.Next(-10, 11);
        public int SecondOutcastCivStartYear = 100 + Game1.r.Next(-10, 11);
        public int ThirdOutcastCivStartYear = 125 + Game1.r.Next(-10, 11);

        public static List<EntityList<Region>> Islands = new List<EntityList<Region>>();
        public static List<EntityList<Region>> PortLocations = new List<EntityList<Region>>();
        public static List<EntityList<Region>> PotentialPorts = new List<EntityList<Region>>();
        public static bool AllPortsBuilt = false;

        public EntityList<Location> AllLocations { get; set; } = new EntityList<Location>();
        public List<string> UnusedCivColors = new List<string>();

        public EntityList<Object> AllWrittenContent = new EntityList<Object>();

        public List<string> CalamityStructures = new List<string>() { "tower", "keep", "monument", "fortress" }; 
        public List<string> ProcgenStructures = new List<string>()
{
    "observatory",
    "library",
    "conservatory",
    "prison",
    "tomb",
    "gallery",
    "armory"
};


        int ContinentalPortMaximum = Game1.r.Next(9, 16);

        public List<string> OutcastCivTypes = new List<string>() { "druid", "pirate", "cultist", "anarchist", "scavenger" };
        public List<string> DecidedOutcastCivs = new List<string>();
        public EntityList<Civilization> OutcastCivs = new EntityList<Civilization>();

        public Architect Hypernexus;
        public Architect Icosidodecahedron;
        public Architect Shadeheart;

        public List<(Civilization, Civilization, int, int)> Wars = new List<(Civilization, Civilization, int, int)>(); // this is a list of Wars, wars are a list of Civilizations and the wins theyve taken.

        public EntityList<Architect> AllArchitects = new EntityList<Architect>();
        public EntityList<Architect> Legends = new EntityList<Architect>();

        public Blight Purity;

        //a world has ONE calamity. This is the guy, he will mess up everrything. But his organization will grow stronger over time.

        public EntityList<Architect> Calamity = new EntityList<Architect>();
        public string CalamityReasoning = "";
        int CalamityStartingYear = Game1.r.Next(30, 80);
        public List<string> CalamityLore = new List<string>();
        public string CalamityIdeologicalObsession = "";

        public int ReactionModifierInt = Game1.r.Next(1, 100000);

        public EntityList<Race> Races = new EntityList<Race>();
        public EntityList<Race> HumanoidRaces = new EntityList<Race>();
        public EntityList<Race> ExtraRaces { get; set; } = new EntityList<Race>();
        public EntityList<Race> ConstructRaces { get; set; } = new EntityList<Race>();
        public EntityList<Race> WildRaces { get; set; } = new EntityList<Race>();

        public EntityList<Entity> DeletedSpells = new EntityList<Entity>();
        public EntityList<Race> DeletedRaces = new EntityList<Race>();
        public EntityList<Composition> DeletedCompositions = new EntityList<Composition>();
        public EntityList<Material> DeletedMaterials = new EntityList<Material>();
        public EntityList<Object> DeletedObjects = new EntityList<Object>();

        public Dictionary<string, Entity> SubjectCatalogue = new Dictionary<string, Entity>() { };

        public int SeaLevel { get; set; }

        public EntityList<Blight> Blights = new EntityList<Blight>();

        public static Architect Unknown; 
        public List<string> SettlementTypes = new List<string>() { "camp", "village", "town", "city" };

        public List<(int, int, int, int)> ChaosPoints = new List<(int, int, int, int)>();

        public int TotalCrafts = 0;


        public static Dictionary<int, List<string>> RarityDistribution = new Dictionary<int, List<string>>
        {
            {1, new List<string> {"common"}},
            {2, new List<string> {"common"}},
            {3, new List<string> {"common", "uncommon"}},
            {4, new List<string> {"common", "uncommon"}},
            {5, new List<string> {"common", "uncommon"}},
            {6, new List<string> {"common", "uncommon", "rare"}},
            {7, new List<string> {"common", "uncommon", "rare"}},
            {8, new List<string> {"uncommon", "rare"}},
            {9, new List<string> {"uncommon", "rare", "epic"}},
            {10, new List<string> {"rare", "epic"}}
        };


        public int Length { get; set; }
        public int Width { get; set; }

        public Region[] WorldMap { get; set; }
        
        public float[] TreeNoiseValues { get; set; }
        public float[,] TrueTreeNoiseValues()
        {
            float[,] TrueTreeNoise = new float[Width, Length];
            int ArrayNum = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {
                    TrueTreeNoise[x, z] = TreeNoiseValues[ArrayNum];
                    ArrayNum++;
                }
            }
            return TrueTreeNoise;
        }

        public float[] ElevationNoiseValues { get; set; }
        public float[,] TrueElevationNoiseValues()
        {
            float[,] TrueElevationNoise = new float[Width, Length];
            int ArrayNum = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {
                    TrueElevationNoise[x, z] = ElevationNoiseValues[ArrayNum];
                    ArrayNum++;
                }
            }
            return TrueElevationNoise;
        }

        public float[] TemperatureNoiseValues { get; set; }
        public float[,] TrueTemperatureNoiseValues()
        {
            float[,] TrueTemperatureNoise = new float[Width, Length];
            int ArrayNum = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {
                    TrueTemperatureNoise[x, z] = TemperatureNoiseValues[ArrayNum];
                    ArrayNum++;
                }
            }
            return TrueTemperatureNoise;
        }

        public float[] RadialArray { get; set; }
        public float[,] TrueRadialArray()
        {
            float[,] TrueRadialArray = new float[Width, Length];
            int ArrayNum = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {
                    TrueRadialArray[x, z] = RadialArray[ArrayNum];
                    ArrayNum++;
                }
            }
            return TrueRadialArray;
        }

        public Race GetRace(string race)
        {
            foreach(Race r in Races)
            {
                if(r.Name == race)
                {
                    return r;
                }
            }
            return null;
        }

        public int LostFoundingYear { get; set; }

        public EntityList<Entity> UndiscoveredSpells { get; set; } = new EntityList<Entity>();
        public EntityList<Entity> UndiscoveredLegendarySpells { get; set; } = new EntityList<Entity>();
        public EntityList<Entity> DiscoveredSpells { get; set; } = new EntityList<Entity>();
        public EntityList<Entity> DiscoveredLegendarySpells { get; set; } = new EntityList<Entity>();

        public List<string> WritingStyles { get; set; } = new List<string> { "profound", "poignant", "thought-provoking", "insightful", "captivating", "masterful", "evocative", "compelling", "engaging", "unique", "innovative", "skillful", "artistic", "authentic", "impactful", "riveting", "meticulous", "expressive" };
        public List<string> WritingMoods { get; set; } = new List<string> { "joyful", "melancholic", "humorous", "mysterious", "reflective", "suspenseful", "inspirational", "eloquent", "soothing", "serious", "optimistic", "nostalgic", "intense", "dark", "hopeful", "whimsical", "enthusiastic", "provocative" };
        public List<string> WritingUnderstandings { get; set; } = new List<string> { "is incredibly easy to understand.", "has some obscurities, but is very simple overall.", "stumbles over some details, but gets the important information well.", "goes off on many unnecessary tangents, but isn't too unreadable.", "floats around the original subject matter, but rambles on about somewhat related, but not important topics.", "is fairly informative, but is full of many extra unrelated opinions.", "seems very coherent, but the topics must be beyond   mind.", "doesn't have a very defined flow, and is rather difficult to read and understand", "has absolutely no coherence whatsoever." };
        public List<string> WriterTypes { get; set; } = new List<string> { "has no idea what they're talking about.", "can't pinpoint many specific instances.", "is well informed on the subject matter.", "really enjoys this subject.", "doesn't care that much about what they are writing about." };

        public int WorksOfCulture { get; set; }

        public int MaxAge = 0;
        public double ProsperityMultiplier = 1.0;
        public string LockedInThreat = "";

        public int LivingArchitects { get; set; }
        public int DeadArchitects { get; set; }

        public EntityList<Architect> Colossals { get; set; } = new EntityList<Architect>();

        public EntityList<Group> Groups { get; set; } = new EntityList<Group>();
        public EntityList<Group> TradingGroups { get; set; } = new EntityList<Group>();
        public EntityList<Group> GroupsToRemove { get; set; } = new EntityList<Group>();

        public Deity LightDeity { get; set; }
        public Deity DarkDeity { get; set; }

        public EntityList<Architect> FractalArchitects = new EntityList<Architect>();
        public EntityList<Object> FractalObjects = new EntityList<Object>();

        public EntityList<Material> Woods { get; set; } = new EntityList<Material>();
        public EntityList<Material> Stones { get; set; } = new EntityList<Material>();
        public EntityList<Material> Metals { get; set; } = new EntityList<Material>();
        public EntityList<Material> SpecialMetals { get; set; } = new EntityList<Material>();
        public EntityList<Material> Cloths { get; set; } = new EntityList<Material>();
        public EntityList<Material> Sheets { get; set; } = new EntityList<Material>();
        public EntityList<Material> Gemstones { get; set; } = new EntityList<Material>();
        public EntityList<Material> Sands { get; set; } = new EntityList<Material>();
        public EntityList<Material> Ices { get; set; } = new EntityList<Material>();
        public EntityList<Material> Fibers { get; set; } = new EntityList<Material>();

        public Material Enchromalite { get; set; } = new Material("enchromalite", "metal", 3, 4, "black");
        public Material Illuminite { get; set; } = new Material("illuminite", "stone", 3, 4, "white");
        public Material Darkstone { get; set; } = new Material("darkstone", "stone", 3, 4, "black");
        public Material Prismite { get; set; } = new Material("prismite", "metal", 3, 4, "white");
        public Material Shadesteel { get; set; } = new Material("shadesteel", "metal", 3, 4, "black");
        public Material Archaeon { get; set; } = new Material("archaeon", "glass", 1337, 5, "white");
        public Material Membrane { get; set; } = new Material("membrane", "membrane", 3, 0, "gray");
        public Material Biocrystal { get; set; } = new Material("biocrystal", "stone", 3, 0, "white");
        public Material Glass { get; set; } = new Material("glass", "stone", 1, 1, "white");
        public Material Clay { get; set; } = new Material("clay", "stone", 1, 1, "brown"); 
        public Material ShadeSludge { get; set; } = new Material("shadesludge", "sludge", 15, 2, "black");
        public Material Coffee { get; set; } = new Material("coffee", "plant", 1, 1, "brown");
        public Material Tea { get; set; } = new Material("tea", "plant", 1, 1, "green");
        public Material Vitalium { get; set; } = new Material("vitalium", "rock", 1, 1, "magenta");
        public Material Paprika { get; set; } = new Material("paprika", "plant", 1, 1, "maroon");
        public Material Salt { get; set; } = new Material("salt", "rock", 1, 1, "white");
        public Material Pepper { get; set; } = new Material("pepper", "plant", 1, 1, "black");
        public Material Isodust { get; set; } = new Material("isodust", "sediment", 1, 1, "magenta");
        public Material Spectre { get; set; } = new Material("spectre", "metaphysic", 1, 1, "cyan");
        public Material Energy { get; set; } = new Material("energy", "metaphysic", 1, 1, "white");
        public Material Flame { get; set; } = new Material("flame", "metaphysic", 1, 1, "orange");
        public Material Void { get; set; } = new Material("void", "metaphysic", 1, 1, "purple");
        public Material Honey { get; set; } = new Material("honey", "animal", 1, 1, "orange");
        public Material Waspwax { get; set; } = new Material("waspwax", "animal", 1, 1, "yellow");

        public double Cycle { get; set; }

        public EntityList<Race> ColossalTypes { get; set; } = new EntityList<Race>();

        public List<string> HistoricalEvents { get; set; } = new List<string>();
        public List<string> AbridgedHistoricalEvents { get; set; } = new List<string>();

        public EntityList<Civilization> Civilizations { get; set; } = new EntityList<Civilization>();
        public int InitialCivCount { get; set; }

        public bool IsNightTime()
        {
            double cycleDurationInSeconds = 0.1;
            double totalSeconds = Cycle * cycleDurationInSeconds;

            // Calculate hours and minutes from total seconds
            double hours = totalSeconds / 3600.0;
            double minutes = (totalSeconds % 3600) / 60.0;

            // Adjust for 24-hour format
            hours = hours % 24;

            // Determine if it's night time (6:00 PM to 6:00 AM)
            return hours >= 18 || hours < 6;
        }


        public EntityList<Object> LootTableMachine(string TableName)
        {
            EntityList<Object> Loot = new EntityList<Object>();

            switch (TableName)
            {
                case "general": //found VERY commonly in any building really, as pilferable loot
                    Loot.AddRange(MisplacedLoot(2, 2));
                    break;
                case "bosstreasure1": //found for <=lvl2 bosses, contains one super loot
                    Loot.AddRange(MisplacedLoot(1, 2));
                    Loot.Add(SuperLoot(2));
                    break;
                case "bosstreasure2": //found for <=lvl4 bosses , contains one super loot
                    Loot.AddRange(MisplacedLoot(1, 4));
                    Loot.Add(SuperLoot(4));
                    break;
                case "bosstreasure3": //found for <=lvl6 bosses , contains one super loot
                    Loot.AddRange(MisplacedLoot(2, 6));
                    Loot.Add(SuperLoot(6));
                    break;
                case "bosstreasure4": //found for <=lvl8 bosses, contains one super loot
                    Loot.AddRange(MisplacedLoot(3, 8));
                    Loot.Add(SuperLoot(8));
                    break;
                case "bosstreasure5": //found for <=lvl10 bosses, contains one super loot
                    Loot.AddRange(MisplacedLoot(5, 10));
                    Loot.Add(SuperLoot(10));
                    break;
                case "magictreasure12": //found uncommonly as a powerful reward, contains magical items this level
                    Loot.AddRange(MisplacedLoot(1, 2));
                    Loot.Add(MagicalSuperLoot(2));
                    break;
                case "magictreasure34": //found uncommonly as a powerful reward, contains magical items this level
                    Loot.AddRange(MisplacedLoot(1, 4));
                    Loot.Add(MagicalSuperLoot(4));
                    break;
                case "magictreasure56": //found uncommonly as a powerful reward, contains magical items this level
                    Loot.AddRange(MisplacedLoot(1, 6));
                    Loot.Add(MagicalSuperLoot(6));
                    break;
                case "magictreasure78": //found uncommonly as a powerful reward, contains magical items this level
                    Loot.AddRange(MisplacedLoot(1, 8));
                    Loot.Add(MagicalSuperLoot(8));
                    break;
                case "magictreasure910": //found uncommonly as a powerful reward, contains magical items for this level
                    Loot.AddRange(MisplacedLoot(1, 10));
                    Loot.Add(MagicalSuperLoot(10));
                    break;
            }

            return Loot;
        }

        public void TriggerRupture(int X, int Z, Architect Activator, int radius)
        {
            int Month = ((int)Math.Round((decimal)(Cycle / 24192000)) % 12) + 1;
            int Year = (int)Math.Round((decimal)(Cycle / 290304000), MidpointRounding.ToZero);

            string Date = "(" + Month + "/" + Year + ")";

            Vector2 activationPoint = new Vector2(X, Z);
            float hexHeight = 1.0f; // Assuming each hexagon has a unit height, adjust as necessary
            float hexWidth = (float)(Math.Sqrt(3) / 2 * hexHeight); // Calculate width based on height

            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {
                    if (WorldMap[x + z * Width].Biome == "void" || (Calamity.Contains(Activator) && WorldMap[x + z * Width].MyLocation != null && CalamityStructures.Contains(WorldMap[x + z * Width].MyLocation.Type)))
                        continue;
                    // Calculate the center position of each hex
                    float offsetX = x * hexWidth + (z % 2) * (hexWidth / 2);
                    float offsetZ = z * (hexHeight * 0.75f); // 0.75 accounts for hex vertical stacking

                    Vector2 currentHexCenter = new Vector2(offsetX, offsetZ);
                    float distance = Vector2.Distance(activationPoint, currentHexCenter);

                    if (distance < radius) // Check if within the radius
                    {
                        // Sporadic effect: decreasing chance with increasing distance
                        float chance = (radius - distance) / radius; // Linear decrease in chance
                        double randomValue = Game1.r.NextDouble(); // Generate a random value between 0.0 and 1.0

                        if (randomValue < chance) // Compare the random value with the calculated chance
                        {
                            WorldMap[x + z * Width].Biome = "ethereal";

                            HistoricalEvents.Add(Date + " " + Activator.Name + " decimated the landscape of the world with a catastrophic ethereal rupture.");

                            if (WorldMap[x + z * Width].MyLocation != null)
                            {
                                HistoricalEvents.Add(Date + " " + WorldMap[x + z * Width].MyLocation.Name + " was consumed in the rupture.");

                                foreach (District d in WorldMap[x + z * Width].MyLocation.Districts)
                                {
                                    foreach (Architect a in d.Architects)
                                    {
                                        HistoricalEvents.Add(Date + " " + a.Name + " was consumed in the rupture.");
                                        a.IsAlive = false;
                                    }
                                }

                                AllLocations.Remove(WorldMap[x + z * Width].MyLocation);
                                WorldMap[x + z * Width].MyLocation = null;
                            }
                        }
                    }
                }
            }
        }




        public static int CalculateDistance(int x1, int z1, int x2, int z2)
        {
            Vector2 p1 = new Vector2(x1, z1);
            Vector2 p2 = new Vector2(x2, z2);
            return (int)Math.Round(Vector2.Distance(p1, p2));
        }


        public Material GetRandomMaterialByStrength(EntityList<Material> materials, int targetStrength)
        {
            // Filter the materials to those within the strength range
            EntityList<Material> filteredMaterials = materials.Where(m => m.Toughness >= targetStrength - 2 && m.Toughness <= targetStrength + 2);

            // If no materials match the criteria, return null or handle appropriately
            if (filteredMaterials.Count == 0)
            {
                return null;
            }

            // Select a random material from the filtered list
            Random random = new Random();
            int index = random.Next(filteredMaterials.Count);

            return filteredMaterials[index];
        }

        public EntityList<Material> CoreMaterials()
        {
            EntityList<Material> Mats = new EntityList<Material>()
            {
                Enchromalite, Illuminite, Darkstone, Prismite, Shadesteel, Archaeon,
                Membrane, Biocrystal, Glass, ShadeSludge, Coffee, Tea, Vitalium,
                Spectre, Energy, Flame
            };

            Mats.AddRange(Woods);
            Mats.AddRange(Stones);
            Mats.AddRange(Metals);
            Mats.AddRange(SpecialMetals);
            Mats.AddRange(Cloths);
            Mats.AddRange(Sheets);
            Mats.AddRange(Gemstones);
            Mats.AddRange(Sands);
            Mats.AddRange(Ices);
            Mats.AddRange(Fibers);

            return Mats;
        }

        public World()
        {
            // Default constructor with no parameters
        }

        public void ClaimSwathOfTerritory(Civilization c, int X, int Z, int Radius)
        {
            List<(int, int)> BannedCoords = new List<(int, int)>
    {
        ((-1 * Radius), (-1 * Radius)),
        (Radius, Radius),
        ((-1 * Radius), Radius),
        (Radius, (-1 * Radius))
    };

            for (int x = Math.Max(0, X - Radius); x <= Math.Min(Width - 1, X + Radius); x++)
            {
                for (int z = Math.Max(0, Z - Radius); z <= Math.Min(Length - 1, Z + Radius); z++)
                {
                    if (!BannedCoords.Contains((x - X, z - Z)))
                    {
                        int index = x + z * Width;
                        if (index < Width * Length && index >= 0)
                        {
                            var tile = WorldMap[index];
                            if (tile.Biome != "void" && tile.Owner == null && (tile.MyLocation == null || tile.MyLocation.Type != "spire"))
                            {
                                tile.Owner = c;
                            }
                        }
                    }
                }
            }
        }

        public void RevealNearbyTiles(int centerX, int centerZ)
        {
            // Define the radius of the detection sphere
            int radius = 3;  // Radius of 2 tiles

            // Convert hex coordinates to cube coordinates
            int ConvertToCubeX(int x, int z) => x - (z - (z & 1)) / 2;
            int ConvertToCubeY(int x, int z) => -x - z;
            int ConvertToCubeZ(int z) => z;

            // Calculate the distance between two points in cube coordinates
            int CubeDistance(int x1, int y1, int z1, int x2, int y2, int z2)
                => (Math.Abs(x1 - x2) + Math.Abs(y1 - y2) + Math.Abs(z1 - z2)) / 2;

            // Iterate over all tiles within the game world
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {
                    int cubeX = ConvertToCubeX(x, z);
                    int cubeY = ConvertToCubeY(cubeX, z);
                    int cubeZ = ConvertToCubeZ(z);

                    int centerX_cube = ConvertToCubeX(centerX, centerZ);
                    int centerY_cube = ConvertToCubeY(centerX_cube, centerZ);
                    int centerZ_cube = ConvertToCubeZ(centerZ);

                    // Calculate distance in cube coordinates
                    int distance = CubeDistance(cubeX, cubeY, cubeZ, centerX_cube, centerY_cube, centerZ_cube);

                    if (distance <= radius)
                    {
                        // Set the tile to explored
                        WorldMap[x + z * Width].Explored = true;

                        // Check if there's a location at this tile and determine if it should be revealed
                        var location = WorldMap[x + z * Width].MyLocation;
                        if (location != null)
                        {
                            // Category: Never revealed
                            if (Game1.ReqExploreLocations.Contains(location.Type))
                            {
                                // Do not reveal
                            }
                            // Category: Revealed when walked onto
                            else if (new[] { "hoard", "sanctum" }.Contains(location.Type) && distance == 0)
                            {
                                location.Explored = true;
                            }
                            // Category: Revealed within one tile
                            else if (new[] { "hallway", "outpost", "toroid", "pyramid", "keep" }.Contains(location.Type) && distance <= 1)
                            {
                                location.Explored = true;
                            }
                            // Category: Revealed in explored radius
                            else if (!Game1.ReqExploreLocations.Contains(location.Type) && !new[] { "hoard", "sanctum", "hallway", "outpost", "toroid", "pyramid", "keep" }.Contains(location.Type))
                            {
                                location.Explored = true;
                            }
                        }
                    }
                }
            }
        }

        public static void CheckAllPortsBuilt(int continentalPortMaximum)
        {
            int totalPorts = 0;
            foreach (var ports in PortLocations)
            {
                totalPorts += ports.Count(port => !string.IsNullOrEmpty(port.PortName));
            }

            bool eachIslandHasPort = Islands.All(island => PortLocations[Islands.IndexOf(island)].Any(port => !string.IsNullOrEmpty(port.PortName)));

            AllPortsBuilt = totalPorts >= continentalPortMaximum && eachIslandHasPort;
        }

        public string GetFormattedDateTime()
        {
            long totalCycles = (long)Cycle;

            // Calculate the year
            int year = (int)(totalCycles / 290304000);
            totalCycles %= 290304000;

            // Calculate the month
            int month = (int)(totalCycles / 24192000) + 1;
            totalCycles %= 24192000;

            // Calculate the day
            int day = (int)(totalCycles / 864000) + 1;
            totalCycles %= 864000;

            // Calculate the hour
            int hour = (int)(totalCycles / 36000);
            totalCycles %= 36000;

            // Calculate the minute
            int minute = (int)(totalCycles / 600);
            totalCycles %= 600;

            // Calculate the second
            int second = (int)(totalCycles / 10);

            return $"{month}/{day}/{year} {hour:D2}:{minute:D2}:{second:D2}";
        }

        public void ProgressToNextMorning()
        {
            double cycleDurationInSeconds = 0.1;
            int totalSeconds = (int)Math.Round(Cycle * cycleDurationInSeconds);

            // Calculate the current hour
            int currentHour = (totalSeconds / 3600) % 24;

            int hoursUntilNext8AM;

            if (currentHour < 8)
            {
                hoursUntilNext8AM = 8 - currentHour;
            }
            else
            {
                hoursUntilNext8AM = 32 - currentHour; // 24 hours to next day + 8 hours
            }

            long cyclesToNext8AM = hoursUntilNext8AM * 36000;
            Cycle += cyclesToNext8AM;
        }

        public World(int width, int length, int civCount, int maxAge, string dedicatedThreat, double prosperityMultiplier)
        {
            foreach (string c in Game1.Colors)
            {
                UnusedCivColors.Add(c);
            }

            string baseName = GenerateUniqueName("1S" + Game1.r.Next(5) + "s", this);

            Name = "The Continent of " + baseName;
            AddReferredToName(Name);
            AddReferredToName(baseName);
            Purity = new Blight(this);

            LockedInThreat = dedicatedThreat;
            MaxAge = maxAge;
            ProsperityMultiplier = prosperityMultiplier;


            for(int i = 0; i < 3; i++)
            {
                int Index = Game1.r.Next(OutcastCivTypes.Count);
                DecidedOutcastCivs.Add(OutcastCivTypes[Index]);
                OutcastCivTypes.RemoveAt(Index);
            }

            //add materials, collapsed
            {
                Woods.Add(new Material("oak", "wood", 3, 0, "brown"));
                Woods.Add(new Material("spruce", "wood", 3, 0, "brown"));
                Woods.Add(new Material("birch", "wood", 3, 0, "white"));
                Woods.Add(new Material("palm", "wood", 3, 0, "brown"));
                Woods.Add(new Material("maple", "wood", 3, 0, "brown"));
                Woods.Add(new Material("walnut", "wood", 3, 0, "brown"));
                Woods.Add(new Material("cedar", "wood", 3, 0, "brown"));
                Woods.Add(new Material("hickory", "wood", 3, 0, "brown"));
                Woods.Add(new Material("ash", "wood", 3, 0, "gray"));
                Woods.Add(new Material("fir", "wood", 3, 0, "green"));

                Stones.Add(new Material("granite", "stone", 3, 0, "gray"));
                Stones.Add(new Material("marble", "stone", 3, 0, "white"));
                Stones.Add(new Material("slate", "stone", 3, 0, "gray"));
                Stones.Add(new Material("sandstone", "stone", 3, 0, "yellow"));
                Stones.Add(new Material("basalt", "stone", 3, 0, "black"));
                Stones.Add(new Material("gneiss", "stone", 3, 0, "gray"));
                Stones.Add(new Material("soapstone", "stone", 3, 0, "gray"));
                Stones.Add(new Material("diorite", "stone", 3, 0, "white"));
                Stones.Add(new Material("mudstone", "stone", 3, 0, "brown"));

                Metals.Add(new Material("lead", "metal", 2, 1, "black"));
                Metals.Add(new Material("zinc", "metal", 2, 1, "white"));
                Metals.Add(new Material("tin", "metal", 4, 2, "gray"));
                Metals.Add(new Material("aluminum", "metal", 4, 2, "gray"));
                Metals.Add(new Material("silver", "metal", 6, 3, "white"));
                Metals.Add(new Material("gold", "metal", 6, 4, "yellow"));
                Metals.Add(new Material("copper", "metal", 8, 2, "brown"));
                Metals.Add(new Material("brass", "metal", 8, 2, "orange"));
                Metals.Add(new Material("nickel", "metal", 10, 3, "gray"));
                Metals.Add(new Material("bronze", "metal", 10, 3, "orange"));
                Metals.Add(new Material("platinum", "metal", 12, 4, "gray"));
                Metals.Add(new Material("palladium", "metal", 12, 4, "gray"));
                Metals.Add(new Material("chromium", "metal", 14, 5, "gray"));
                Metals.Add(new Material("tungsten", "metal", 14, 5, "gray"));
                Metals.Add(new Material("titanium", "metal", 16, 5, "white"));
                Metals.Add(new Material("steel", "metal", 16, 4, "gray"));


                Cloths.Add(new Material("fleece", "cloth", 3, 4, "white"));
                Cloths.Add(new Material("silk", "cloth", 3, 4, "white"));
                Cloths.Add(new Material("cotton", "cloth", 3, 4, "white"));
                Cloths.Add(new Material("linen", "cloth", 3, 4, "white"));
                Cloths.Add(new Material("wool", "cloth", 3, 4, "white"));
                Cloths.Add(new Material("leather", "cloth", 3, 4, "brown"));
                Cloths.Add(new Material("hemp", "cloth", 3, 4, "brown"));
                Cloths.Add(new Material("jute", "cloth", 3, 4, "brown"));

                Sheets.Add(new Material("parchment", "cloth", 3, 4, "white"));
                Sheets.Add(new Material("papyrus", "cloth", 3, 4, "white"));
                Sheets.Add(new Material("enchromalite", "cloth", 3, 4, "white"));

                Gemstones.Add(new Material("ruby", "gemstone", 3, 4, "red"));
                Gemstones.Add(new Material("diamond", "gemstone", 3, 4, "white"));
                Gemstones.Add(new Material("emerald", "gemstone", 3, 4, "green"));
                Gemstones.Add(new Material("sapphire", "gemstone", 3, 4, "blue"));
                Gemstones.Add(new Material("aquamarine", "gemstone", 3, 4, "lightblue"));
                Gemstones.Add(new Material("peridot", "gemstone", 3, 4, "green"));
                Gemstones.Add(new Material("tanzanite", "gemstone", 3, 4, "purple"));
                Gemstones.Add(new Material("turquoise", "gemstone", 3, 4, "cyan"));
                Gemstones.Add(new Material("topaz", "gemstone", 3, 4, "yellow"));
                Gemstones.Add(new Material("opal", "gemstone", 3, 4, "white"));
                Gemstones.Add(new Material("lapis lazuli", "gemstone", 3, 4, "blue"));
                Gemstones.Add(new Material("amethyst", "gemstone", 3, 4, "purple"));

                Sands.Add(new Material("red sand", "sand", 3, 4, "red"));
                Sands.Add(new Material("sand", "sand", 3, 4, "yellow"));
                Sands.Add(new Material("rocky sand", "sand", 3, 4, "brown"));

                Fibers.Add(new Material("raw hemp", "fiber", 3, 4, "brown"));
                Fibers.Add(new Material("flax", "fiber", 3, 4, "brown"));
                Fibers.Add(new Material("raw jute", "fiber", 3, 4, "brown"));

                Ices.Add(new Material("blue ice", "ice", 3, 4, "lightblue"));
                Ices.Add(new Material("crystal ice", "ice", 3, 4, "white"));
                Ices.Add(new Material("clear ice", "ice", 3, 4, "white"));
            }

            

            Length = length;
            Width = width;

            ExtraRaces.Shuffle();

            for (int i = Game1.r.Next(3,6); i != 0; i--)
            {
                Blights.Add(new Blight(this));
            }

            WorldMap = new Region[length * width];

            LostFoundingYear = Game1.r.Next(35, 65);

            Races = new EntityList<Race>();

            List<(string, Material)> LuminarchBodyParts = new List<(string, Material)>
            {
                ("head", Membrane), ("left eye", Membrane), ("right eye", Membrane), ("neck", Membrane),
                ("left shoulder", Membrane), ("right shoulder", Membrane),
                ("left arm", Membrane), ("right arm", Membrane),
                ("left hand", Membrane), ("right hand", Membrane),
                ("left leg", Membrane), ("right leg", Membrane),
                ("left foot", Membrane), ("right foot", Membrane),
                ("torso", Membrane)
            };

            List<(string, Material)> NightfellBodyParts = new List<(string, Material)>
            {
                ("head", Membrane), ("left eye", Membrane), ("right eye", Membrane), ("neck", Membrane),
                ("left shoulder", Membrane), ("right shoulder", Membrane),
                ("left arm", Membrane), ("right arm", Membrane),
                ("left hand", Membrane), ("right hand", Membrane),
                ("left leg", Membrane), ("right leg", Membrane),
                ("left foot", Membrane), ("right foot", Membrane),
                ("torso", Membrane)
            };

            List<(string, Material)> LostBodyParts = new List<(string, Material)>
            {
                ("head", Membrane), ("left eye", Membrane), ("right eye", Membrane), ("neck", Membrane),
                ("left shoulder", Membrane), ("right shoulder", Membrane),
                ("left arm", Membrane), ("right arm", Membrane),
                ("left hand", Membrane), ("right hand", Membrane),
                ("left leg", Membrane), ("right leg", Membrane),
                ("left foot", Membrane), ("right foot", Membrane),
                ("torso", Membrane)
            };

            List<(string, Material)> IsofractalBodyParts = new List<(string, Material)>
            {
                ("core", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass)
            };
            List<(string, Material)> PhotonexusBodyParts = new List<(string, Material)>
            {
                ("core", Metals[15]), ("sphere", Metals[15]), ("sphere", Metals[15]), ("sphere", Metals[15]), ("sphere", Metals[15]), ("sphere", Metals[15]), ("sphere", Metals[15])
            };
            List<(string, Material)> ShadeBodyParts = new List<(string, Material)>
            {
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge)
            };

            List<(string, Material)> MoariBodyParts = new List<(string, Material)>
            {
                ("head", Membrane), ("eye", Membrane), ("neck", Membrane),
                ("left front leg", Membrane), ("right front leg", Membrane),
                ("left back leg", Membrane), ("right back leg", Membrane),
                ("torso", Membrane)
            };

            List<(string, Material)> ShibaBodyParts = new List<(string, Material)>
            {
                ("head", Membrane), ("left eye", Membrane), ("right eye", Membrane), ("neck", Membrane),
                ("left front leg", Membrane), ("right front leg", Membrane),
                ("left back leg", Membrane), ("right back leg", Membrane),
                ("torso", Membrane)
            };

            List<(string, Material)> CassartraeBodyParts = new List<(string, Material)>
            {
                ("core", Metals[15])
            };

            Races.Add(new Race("", "average", new List<(string, Material)>(), "white", new List<string>() { }, new List<string>() { }, 0, "", ""));

            Races.Add(new Race("luminarch", "average", LuminarchBodyParts, "white", new List<string>() {"head", "torso", "neck"}, new List<string>() { "allevil" }, 0, "right hand", "left hand"));
            Races.Add(new Race("nightfell", "average", NightfellBodyParts, "black", new List<string>() { "head", "torso", "neck" }, new List<string>() { "allevil" }, 0, "right hand", "left hand"));
            Races.Add(new Race("archaix", "average", LostBodyParts, "gray", new List<string>() { "head", "torso", "neck" }, new List<string>() { "allevil" }, 0, "right hand", "left hand"));
            HumanoidRaces.AddRange(new EntityList<Race>() { Races[1], Races[2], Races[3] });

            Races.Add(new Race("moari", "large", MoariBodyParts, "white", new List<string>() { "head", "torso", "neck" }, new List<string>() {}, 30, "right front leg", "left front leg"));
            Races.Add(new Race("cassartrae", "smaller", CassartraeBodyParts, "black", new List<string>() { "core" }, new List<string>() { "allevil" }, 30, "core", "core"));
            Races.Add(new Race("debtshiba", "medium", ShibaBodyParts, "orange", new List<string>() { "head", "torso" }, new List<string>() { "indebted" }, 1337, "right front leg", "left front leg"));
            Races.Add(new Race("shiba", "medium", ShibaBodyParts, "orange", new List<string>() { "head", "torso" }, new List<string>() { "allevil" }, 1337, "right front leg", "left front leg"));

            Races.Add(new Race("isofractal", "average", IsofractalBodyParts, "gray", new List<string>() { "core" }, new List<string>() { "allevil" }, 15, "shard", "shard"));
            Races.Add(new Race("photonexus", "small", PhotonexusBodyParts, "gray", new List<string>() { "core" }, new List<string>() { "allunalike" }, 25, "sphere", "sphere"));
            Races.Add(new Race("shade", "average", ShadeBodyParts, "black", new List<string>() { "sludge" }, new List<string>(){ "alllife" }, 5, "sludge", "sludge"));
            ExtraRaces.Add(GetRace("isofractal"));
            ExtraRaces.Add(GetRace("photonexus"));
            ExtraRaces.Add(GetRace("shade"));

            List<(string, Material)> IcosidodecahedronParts = new List<(string, Material)>
            {
                ("core", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass)
            };
            List<(string, Material)> HypernexusBodyParts = new List<(string, Material)>
            {
                ("core", Metals[15]), ("sphere", Metals[15]), ("sphere", Metals[15]), ("sphere", Metals[15]), ("sphere", Metals[15]), ("sphere", Metals[15]), ("sphere", Metals[15])
            };
            List<(string, Material)> ShadeheartBodyParts = new List<(string, Material)>
            {
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge)
            };

            //WE ARENT GIVING THEM OPPOSITION TAGS BECUASE THEY HAVE SPECIAL ATTACK CONDITIONS

            Races.Add(new Race("icosidodecahedron", "huge", IcosidodecahedronParts, "white", new List<string>() { "core" }, new List<string>() { }, 110, "shard", "shard"));
            Races.Add(new Race("hypernexus", "huge", HypernexusBodyParts, "gray", new List<string>() { "core" }, new List<string>() { }, 125, "sphere", "sphere"));
            Races.Add(new Race("shadeheart", "huge", ShadeheartBodyParts, "black", new List<string>() { "sludge" }, new List<string>() { }, 80, "sludge", "sludge"));

            Races.Add(new Race("shadebeast", "large", ShadeBodyParts, "black", new List<string>() { "sludge" }, new List<string>(){ "alllife" }, 30, "sludge", "sludge"));

            var mechanicalParts = new List<string>
            {
                "head",
                "body",
                "tail"
            };

            var pairableParts = new List<string>
            {
                "leg",
                "arm",
                "wing",
                "fin",
                "tentacle"
            };

            for (int i = Game1.r.Next(10, 20); i != 0; i--)
            {
                Material ChosenMetal = Metals[Game1.r.Next(Metals.Count)];
                List<(string, Material)> selectedParts = new List<(string, Material)>();

                // Add non-pairable parts
                foreach (var part in mechanicalParts)
                {
                    selectedParts.Add((part, ChosenMetal));
                }

                // Add pairable parts
                foreach (var part in pairableParts)
                {
                    int count = Game1.r.Next(0, 6); // Randomly decide to add 0-5 different pairable parts
                    if (count == 0) continue;

                    int quantity = (new int[] { 1, 2, 3, 4, 6, 8 })[Game1.r.Next(6)]; // Randomly select the quantity of each part
                    for (int j = 1; j <= quantity; j++)
                    {
                        string label = part;
                        if (quantity == 2)
                        {
                            label = (j == 1) ? $"left {part}" : $"right {part}";
                        }
                        else if (quantity == 3)
                        {
                            label = (j == 1) ? $"front {part}" : (j == 2) ? $"left back {part}" : $"right back {part}";
                        }
                        else if (quantity == 4)
                        {
                            label = (j == 1) ? $"front left {part}" : (j == 2) ? $"front right {part}" : (j == 3) ? $"back left {part}" : $"back right {part}";
                        }
                        else if (quantity == 6 || quantity == 8)
                        {
                            label = $"{part} {j}";
                        }
                        selectedParts.Add((label, ChosenMetal));
                    }
                }

                Race r = new Race("guardian", new List<string> { "medium", "average", "large" }[Game1.r.Next(3)], selectedParts, Game1.Colors[Game1.r.Next(Game1.Colors.Count)], new List<string> { "head", "body" }, new List<string> { "intruders" }, 50, (selectedParts[Game1.r.Next(selectedParts.Count)].Item1), (selectedParts[Game1.r.Next(selectedParts.Count)].Item1));
                r.Name = GenerateUniqueName(Game1.r.Next(1, 6) + "s", r) + " guardian";
                Races.Add(r);
                ConstructRaces.Add(r);
            }


            ColossalTypes.Add(new Race("quetzal", "colossal", new List<(string, Material)>() { ("head", Membrane), ("body", Membrane), ("left wing", Membrane), ("right wing", Membrane), ("tail", Membrane), ("left leg", Biocrystal), ("right leg", Biocrystal) }, Game1.Colors[Game1.r.Next(Game1.Colors.Count)], new List<string>() { "head", "body" }, new List<string>() { "allunalike" }, 50, "right leg", "left leg"));
            ColossalTypes.Add(new Race("wyrm", "colossal", new List<(string, Material)>() { ("head", Membrane), ("body", Membrane), ("front left fin", Membrane), ("front right fin", Membrane), ("back left fin", Membrane), ("back right fin", Membrane), ("tail", Membrane) }, Game1.Colors[Game1.r.Next(Game1.Colors.Count)], new List<string>() { "head", "body" }, new List<string>() { "allunalike" }, 60, "front right fin", "front left fin"));
            ColossalTypes.Add(new Race("serpent", "colossal", new List<(string, Material)>() { ("head", Membrane), ("body", Membrane), ("left front leg", Membrane), ("right front leg", Membrane), ("left back leg", Membrane), ("right back leg", Membrane), ("tail", Membrane) }, Game1.Colors[Game1.r.Next(Game1.Colors.Count)], new List<string>() { "head", "body" }, new List<string>() { "allunalike" }, 70, "right front leg", "left front leg"));
            ColossalTypes.Add(new Race("shobe", "colossal", new List<(string, Material)>() { ("head", Membrane), ("body", Membrane), ("left front leg", Membrane), ("right front leg", Membrane), ("left back leg", Membrane), ("right back leg", Membrane), ("tail", Membrane) }, Game1.Colors[Game1.r.Next(Game1.Colors.Count)], new List<string>() { "head", "body" }, new List<string>() { "allunalike" }, 80, "right front leg", "left front leg"));
            ColossalTypes.Add(new Race("cnidriarch", "colossal", new List<(string, Material)>() { ("bell", Membrane), ("mantle", Membrane) }.Concat(Enumerable.Range(1, 12).Select(i => ($"tentacle{i}", Membrane))).ToList(), Game1.Colors[Game1.r.Next(Game1.Colors.Count)], new List<string>() { "bell", "mantle" }, new List<string>() { "allunalike" }, 50, "tentacle1", "tentacle2"));
            Races.AddRange(ColossalTypes);

            //generate random animal races

            for (int i = Game1.r.Next(15, 25); i != 0; i--)
            {
                List<(string, Material)> BodyParts = new List<(string, Material)>();

                // Randomizing the number of legs, wings, and other body parts
                int Legs = Game1.r.Next(1, 6) * 2;
                int Wings = Game1.r.Next(0, 3); // Fixed syntax error here
                int Horns = RandomChance(10) ? Game1.r.Next(1, 3) : 0; // 10% chance to have 1-2 horns
                int Eyes = Game1.r.Next(1, 6);
                int Antennae = RandomChance(10) ? Game1.r.Next(1, 3) : 0;
                int Tentacles = RandomChance(10) ? Game1.r.Next(1, 5) : 0;
                int Tails = RandomChance(10) ? Game1.r.Next(1, 3) : 0;
                int Humps = RandomChance(10) ? Game1.r.Next(1, 3) : 0;
                int Fins = RandomChance(10) ? Game1.r.Next(1, 4) : 0;
                int Tusks = RandomChance(10) ? Game1.r.Next(1, 3) : 0;
                int Spikes = RandomChance(10) ? Game1.r.Next(1, 6) : 0; // 10% chance to have 1-6 spikes
                int Teeth = RandomChance(80) ? Game1.r.Next(12, 48) : 0;

                // Randomizing other attributes
                string Size = Game1.AnimalSizes[Game1.r.Next(Game1.AnimalSizes.Count)];
                bool HasScales = Game1.r.Next(0, 2) == 1;
                bool HasGills = Game1.r.Next(0, 2) == 1;
                bool HasFur = Game1.r.Next(0, 2) == 1;
                bool HasFeathers = Game1.r.Next(0, 2) == 1;
                bool HasHooves = Game1.r.Next(0, 2) == 1;
                bool HasShell = Game1.r.Next(0, 2) == 1;

                // Always include head and body
                BodyParts.Add(("head", Membrane));
                BodyParts.Add(("body", Membrane));

                // Add extra body parts based on the generated integers
                for (int j = 0; j < Legs; j++)
                    BodyParts.Add(("leg", Membrane));

                for (int j = 0; j < Wings; j++)
                    BodyParts.Add(("wing", Membrane));

                for (int j = 0; j < Horns; j++)
                    BodyParts.Add(("horn", Biocrystal));

                for (int j = 0; j < Eyes; j++)
                    BodyParts.Add(("eye", Membrane));

                for (int j = 0; j < Antennae; j++)
                    BodyParts.Add(("antenna", Membrane));

                for (int j = 0; j < Tentacles; j++)
                    BodyParts.Add(("tentacle", Membrane));

                for (int j = 0; j < Tails; j++)
                    BodyParts.Add(("tail", Membrane));

                for (int j = 0; j < Humps; j++)
                    BodyParts.Add(("hump", Membrane));

                for (int j = 0; j < Fins; j++)
                    BodyParts.Add(("fin", Membrane));

                for (int j = 0; j < Tusks; j++)
                    BodyParts.Add(("tusk", Biocrystal));

                for (int j = 0; j < Spikes; j++)
                    BodyParts.Add(("spike", Biocrystal));

                for (int j = 0; j < Teeth; j++)
                    BodyParts.Add(("tooth", Membrane));

                // Add to list (assuming GenerateUniqueName and Race constructor are defined elsewhere)
                Race r = new Race("", Size, BodyParts, Game1.Colors[Game1.r.Next(Game1.Colors.Count)], new List<string> { "head", "body" }, new List<string>() { "allunalike" }, Game1.r.Next(0,6), BodyParts[Game1.r.Next(BodyParts.Count)].Item1, BodyParts[Game1.r.Next(BodyParts.Count)].Item1);
                r.Name = GenerateUniqueName("1S" + Game1.r.Next(5) + "s", r);
                WildRaces.Add(r);
            }

            // Helper method to determine if a feature is present based on a percentage chance
            bool RandomChance(int percent)
            {
                return Game1.r.Next(0, 100) < percent;
            }

            // Add the RandomSize method and other necessary components as per your game's requirements




            float scale;

            LightDeity = new Deity(GenerateUniqueName("1S"+Game1.r.Next(7, 9)+"s", LightDeity), "light");
            DarkDeity = new Deity(GenerateUniqueName("1S" + Game1.r.Next(7, 9) + "s", DarkDeity), "dark");

            InitialCivCount = civCount;

            ElevationNoiseValues = new float[Width * Length];

            scale = 0.01f;
            SimplexNoise.Noise.Seed = Game1.r.Next(0, 10000000);
            float[,] Array1 = SimplexNoise.Noise.Calc2D(width, length, scale);
            scale = 0.02f;
            SimplexNoise.Noise.Seed = Game1.r.Next(0, 10000000);
            float[,] Array2 = SimplexNoise.Noise.Calc2D(width, length, scale);
            scale = 0.08f;
            SimplexNoise.Noise.Seed = Game1.r.Next(0, 10000000);
            float[,] Array3 = SimplexNoise.Noise.Calc2D(width, length, scale);


            //construct radial array


            RadialArray = new float[Width * Length];
            float GreatestArrayValue = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {
                    float Distance = CalculateDistance(Width / 2, Length / 2, x, z);
                    RadialArray[x + z * Width] = Distance;
                    if (Distance > GreatestArrayValue)
                    {
                        GreatestArrayValue = Distance;
                    }
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {
                    RadialArray[x + z * Width] = (((1 - (RadialArray[x + z * Width] / GreatestArrayValue)) * 256));
                }
            }
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {
                    ElevationNoiseValues[x + z * Width] = ((((Array1[x, z] + Array2[x, z] + Array3[x, z]) / 3) + RadialArray[x + z * Width]) / 2);
                }
            }

            float[] ConvertTo1DArray(float[,] array2D)
            {
                int length = array2D.GetLength(1);
                int width = array2D.GetLength(0);
                float[] array1D = new float[length * width];

                int index = 0;
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < length; z++)
                    {
                        array1D[index++] = array2D[x, z];
                    }
                }

                return array1D;
            }

            scale = 0.08f;
            SimplexNoise.Noise.Seed = Game1.r.Next(0, 10000000);
            float[,] treeNoise2D = SimplexNoise.Noise.Calc2D(width, length, scale);
            TreeNoiseValues = ConvertTo1DArray(treeNoise2D);

            scale = 0.05f;
            SimplexNoise.Noise.Seed = Game1.r.Next(0, 10000000);
            float[,] temperatureNoise2D = SimplexNoise.Noise.Calc2D(width, length, scale);
            TemperatureNoiseValues = ConvertTo1DArray(temperatureNoise2D);


            foreach (Race r in Races)
            {
                r.RaceLetter = Race.GenerateUniqueAbbreviation(r.Name, this.Races);
            }

            //utilize elevation map

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < length; z++)
                {
                    int Elevation = (int)Math.Round(ElevationNoiseValues[x + z * Width] / 2.56);
                    int Temperature = (int)Math.Round(TemperatureNoiseValues[x + z * Width] / 2.56);
                    int Trees = (int)Math.Round(TreeNoiseValues[x + z * Width] / 2.56);

                    string Biome = "";

                    if (RadialArray[x + z * Width] < 70)
                    {
                        Biome = "void";
                    }
                    else if (Elevation > 75)
                    {
                        Biome = "snowpeak";
                    }
                    else if (Elevation > 70)
                    {
                        Biome = "mountain";
                    }
                    else if (Elevation < 50)
                    {
                        Biome = "ocean";
                    }
                    else
                    {
                        if (Temperature > 70)
                        {
                            Biome = "desert";
                        }
                        else if (Temperature < 30)
                        {
                            if (Trees > 50)
                            {
                                Biome = "taiga";
                            }
                            else
                            {
                                Biome = "tundra";
                            }
                        }
                        else
                        {
                            if (Trees > 60)
                            {
                                Biome = "forest";
                            }
                            else if (Trees > 50)
                            {
                                Biome = "lightforest";
                            }
                            else
                            {
                                Biome = "plains";
                            }
                        }
                    }

                    WorldMap[x + z * Width] = new Region(Biome, Elevation, Temperature, x, z, this);

                    //set to purity

                    WorldMap[x + z * Width].Blight = Purity;
                }
            }
        }

        public void ProgressDays(int Days, bool IncreaseCycle)
        {
            Random r = new Random();

            int Month = ((int)Math.Round((decimal)(Cycle / 24192000)) % 12) + 1;
            int Year = (int)Math.Round((decimal)(Cycle / 290304000), MidpointRounding.ToZero);

            string Date = "(" + Month + "/" + Year + ")";

            int CurrentlyCountingArchitects = 0;

            void DetectIslandsAndPorts(Region[] worldMap, int width, List<EntityList<Region>> islands, List<EntityList<Region>> portLocations)
            {
                int length = worldMap.Length / width; // Assuming worldMap.Length is a multiple of width
                bool[] visited = new bool[worldMap.Length];

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < length; y++)
                    {
                        if (!visited[x + y * width] && worldMap[x + y * width].Biome != "ocean" && worldMap[x + y * width].Biome != "void" && worldMap[x + y * width].Biome != "ethereal")
                        {
                            EntityList<Region> currentIsland = new EntityList<Region>();
                            DFS(worldMap, x, y, width, visited, currentIsland);
                            islands.Add(currentIsland);

                            // Find potential port locations for this island
                            EntityList<Region> currentPorts = FindPotentialPorts(worldMap, width, currentIsland);
                            portLocations.Add(currentPorts);
                        }
                    }
                }
            }


            void DFS(Region[] worldMap, int x, int y, int width, bool[] visited, EntityList<Region> currentIsland)
            {
                int length = worldMap.Length / width; // Calculate length based on total size and width
                if (x < 0 || y < 0 || x >= width || y >= length || visited[x + y * width] || worldMap[x + y * width].Biome == "ocean" || worldMap[x + y * width].Biome == "void")
                    return;

                visited[x + y * width] = true; // Mark this region as visited
                currentIsland.Add(worldMap[x + y * width]);

                // Explore neighbors (up, down, left, right)
                DFS(worldMap, x + 1, y, width, visited, currentIsland);
                DFS(worldMap, x - 1, y, width, visited, currentIsland);
                DFS(worldMap, x, y + 1, width, visited, currentIsland);
                DFS(worldMap, x, y - 1, width, visited, currentIsland);
            }

            EntityList<Region> FindPotentialPorts(Region[] worldMap, int width, EntityList<Region> island)
            {
                EntityList<Region> ports = new EntityList<Region>();
                int length = worldMap.Length / width;
                foreach (Region region in island)
                {
                    int x = region.X;
                    int y = region.Z;
                    // Check all four directions for ocean biomes
                    foreach (var (dx, dy) in new (int, int)[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    {
                        int newX = x + dx;
                        int newY = y + dy;

                        if (newX >= 0 && newY >= 0 && newX < width && newY < length && worldMap[newX + newY * width].Biome == "ocean")
                        {
                            if (!ports.Contains(worldMap[newX + newY * width]))
                                ports.Add(worldMap[newX + newY * width]);
                        }
                    }
                }
                return ports;
            }


            // Handle the first month
            if (Cycle == 0)
            {
                foreach(Material m in Game1.MaterialsToAddToWorld)
                {
                    bool Added = Materials.TryAdd(m.Name, m);

                    if(!Added)
                    {
                        DupeMats.Add(m.Name);
                    }
                }

                Unknown = new Architect("Someone lost to time", null, GetRace(""), 0, null, null, null, null, null, null, 0);
                SubjectCatalogue.Add("Someone lost to time", Unknown);

                UndiscoveredSpells.AddRange(Game1.GameWorld.AllSpells);
                UndiscoveredLegendarySpells.AddRange(Game1.GameWorld.AllLegendarySpells);

                foreach (Civilization c in Civilizations)
                {
                    Location l = new Location("village", c.PrimaryInhabiantRace, Game1.r.Next(50, 100), 1000, Game1.r.Next(4, 8), c.StartX, c.StartZ, c, WorldMap[c.StartX + c.StartZ * Width], "none");
                    AllLocations.Add(l);
                    l.IsCapitol = true;
                    c.Capitol = l;
                    ClaimSwathOfTerritory(c, l.X, l.Z, 2);

                    HistoricalEvents.Add(Date + " " + c.Name + " sprung forth into the world as a united culture, manifesting in their capitol " + l.Name + ".");

                    Block chosenBlock = l.Districts[0].DistrictMap[Game1.r.Next(0, 49)];
                    Structure Prism = new Structure("prism", new EntityList<Object>(), new EntityList<Room>(), chosenBlock, new EntityList<Material> { c.CulturalStone }, new List<string>(), new List<string> { Game1.LightingStyles[Game1.r.Next(Game1.LightingStyles.Count)] }, Game1.r.Next(0, 5), Game1.r.Next(0,4), (int)Math.Round(Cycle / 290304000));

                    chosenBlock.Structures.Add(Prism);
                    l.Prism = Prism;

                    for (int i = 0; i < Game1.r.Next(10, 20); i++)
                    {
                        Block ChosenBlock = l.Districts[0].DistrictMap[Game1.r.Next(0, 49)];
                        Structure s = new Structure("house", new EntityList<Object>(), new EntityList<Room>(), ChosenBlock, new EntityList<Material> { c.CulturalWood }, new List<string> { c.CulturalWood.Name }, new List<string> { Game1.LightingStyles[Game1.r.Next(Game1.LightingStyles.Count)] }, Game1.r.Next(0, 5), Game1.r.Next(0, 4), (int)Math.Round(Cycle / 290304000));

                        ChosenBlock.Structures.Add(s);
                    }

                    Block b = l.Districts[0].DistrictMap[r.Next(2, 6) + r.Next(2, 5) * 7];

                    b.Objects.Add(new Object(null, "well", new EntityList<Material> { l.HomeCivilization.CulturalStone }, true, true, null, null, 255, false, null, null, null, false));
                    b.Objects.Add(new Object(null, "shadow storage", new EntityList<Material>() { Shadesteel }, DarkDeity));

                    WorldMap[c.StartX + c.StartZ * Width].MyLocation = l;
                }

                //generate the legendary colossals

                int ColossalCount = Game1.r.Next(8, 12);

                for (int i = 0; i < ColossalCount; i++)
                {

                    Architect a = new Architect("", Game1.Sexes[Game1.r.Next(Game1.Sexes.Count)], ColossalTypes[Game1.r.Next(ColossalTypes.Count)], 0, "end", new EntityList<Object>(), null, null, null, null, 7);

                    string Name = "";

                    if (Game1.r.Next(1, 3) == 1)
                    {
                        Name = GenerateUniqueName("1S" + Game1.r.Next(2, 10) + "s", a);
                    }
                    else
                    {
                        Name = GenerateUniqueName("1S" + Game1.r.Next(2, 5) + "s 1S" + Game1.r.Next(2, 5) + "s", a);
                    }

                    a.Name = Name;
                    a.IsColossal = true;

                    while (WorldMap[a.ColossalMinefieldX + a.ColossalMinefieldZ * Width].Biome == "ocean" || WorldMap[a.ColossalMinefieldX + a.ColossalMinefieldZ * Width].Biome == "void")
                    {
                        a.ColossalMinefieldX = Game1.r.Next(0, Length);
                        a.ColossalMinefieldZ = Game1.r.Next(0, Width);
                    }

                    Colossals.Add(a);

                    InteractableEvent e = new InteractableEvent(WorldMap[a.ColossalMinefieldX + a.ColossalMinefieldZ * Width], 999999, "colossal", null, new EntityList<Architect>() { a });
                    WorldMap[a.ColossalMinefieldX + a.ColossalMinefieldZ * Width].Events.Add(e);

                    HistoricalEvents.Add(string.Concat(Date, " ", a.Name, ", a colossal ", a.Race.Name, ", began lying in wait in the ", Game1.DeterminePointLocation(Width, Length, a.ColossalMinefieldX, a.ColossalMinefieldZ), "."));

                }

                //superlords or whatever

                Hypernexus = new Architect("", "female", GetRace("hypernexus"), Game1.r.Next(5000, 20000), "soverign", new EntityList<Object>(), null, null, null, null, 9);
                Hypernexus.Name = GenerateUniqueArchitectName(Hypernexus);
                Icosidodecahedron = new Architect("", "female", GetRace("icosidodecahedron"), Game1.r.Next(5000, 20000), "soverign", new EntityList<Object>(), null, null, null, null, 9);
                Icosidodecahedron.Name = GenerateUniqueArchitectName(Icosidodecahedron);
                Shadeheart = new Architect("", "female", GetRace("shadeheart"), Game1.r.Next(5000, 20000), "heart", new EntityList<Object>(), null, null, null, null, 9);
                Shadeheart.Name = GenerateUniqueArchitectName(Shadeheart);


                // Initialize lists for islands and port locations
                List<EntityList<Region>> islands = new List<EntityList<Region>>();
                List<EntityList<Region>> portLocations = new List<EntityList<Region>>();
                List<EntityList<Region>> potentialPorts = new List<EntityList<Region>>();

                // Detect islands and potential port locations
                DetectIslandsAndPorts(WorldMap, Width, islands, portLocations);

                foreach (var island in islands)
                {
                    potentialPorts.Add(FindPotentialPorts(WorldMap, Width, island));
                }

                // Assign the detected data to the static variables
                Islands = islands;
                PortLocations = portLocations;
                PotentialPorts = potentialPorts;

            }
            else
            {
                List<LocationBuilderPacket> LocationBuilderPackets = new List<LocationBuilderPacket>();


                //add historical abridged events

                if (AbridgedHistoricalEvents.Count * 10 < Math.Round(Cycle / 290304000, 0, MidpointRounding.ToNegativeInfinity))
                {
                    AbridgedHistoricalEvents.Add(HistoricalEvents[HistoricalEvents.Count - Game1.r.Next(1, 5)]);
                }

                int MonthToDayConstant = (28 / Days);

                //place civilizations that need to be placed.

                void PlaceFancyCiv(Race race)
                {
                    //find a location to start at

                    int FoundX = 0;
                    int FoundZ = 0;
                    int Tries = 0;

                    while (true)
                    {
                        int TryX = r.Next(Width);
                        int TryZ = r.Next(Length);


                        // Check a square of size 5 centered around (TryX, TryZ)
                        bool validLocation = true;
                        for (int i = TryX - 3; i <= TryX + 3 && validLocation; i++)
                        {
                            for (int j = TryZ - 3; j <= TryZ + 3 && validLocation; j++)
                            {
                                // Check if any region inside the area's Region.Location is not equal to null
                                if (i <= 0 || i >= Width || j <= 0 || j >= Length ||
                                    WorldMap[i + j * Width].Biome == "ocean" || WorldMap[i + j * Width].Biome == "void" ||
                                    WorldMap[i + j * Width].MyLocation != null)
                                {
                                    validLocation = false;
                                }
                            }
                        }

                        if (validLocation)
                        {
                            FoundX = TryX;
                            FoundZ = TryZ;
                            break;
                        }

                        Tries++;
                    }

                    Civilization c = new Civilization(race, race.Name, FoundX, FoundZ, this);
                    Civilizations.Add(c);

                    if (race.Name == "photonexus" || race.Name == "shade" || race.Name == "isofractal")
                    {
                        Location l = new Location("core", race, Game1.r.Next(50, 100), 1000, Game1.r.Next(1, 5), c.StartX, c.StartZ, c, WorldMap[c.StartX + c.StartZ * Width], "none");
                        l.IsCapitol = true;
                        AllLocations.Add(l);
                        ClaimSwathOfTerritory(c, l.X, l.Z, 2);

                        if (race.Name == "shade")
                        {
                            Block chosenBlock = l.Districts[0].DistrictMap[Game1.r.Next(0, 49)];
                            Structure heart = new Structure("heart", new EntityList<Object>(), new EntityList<Room>(), chosenBlock, new EntityList<Material> { ShadeSludge }, new List<string> { c.CulturalGemstone.Name }, new List<string> { "veins" }, Game1.r.Next(0, 5), Game1.r.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                            chosenBlock.Structures.Add(heart);
                            l.Prism = heart;

                            for (int i = 0; i < Game1.r.Next(10, 20); i++)
                            {
                                chosenBlock = l.Districts[0].DistrictMap[Game1.r.Next(0, 49)];
                                Structure scum = new Structure("scum", new EntityList<Object>(), new EntityList<Room>(), chosenBlock, new EntityList<Material> { ShadeSludge }, new List<string> { c.CulturalGemstone.Name }, new List<string> { "veins" }, Game1.r.Next(0, 5), Game1.r.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                chosenBlock.Structures.Add(scum);
                            }
                        }
                        else if (race.Name == "isofractal" || race.Name == "photonexus")
                        {
                            // Place the core structure in the center
                            Block centerBlock = l.Districts[0].DistrictMap[24]; // Center block (3,3 in a 7x7 grid)
                            Structure core = new Structure("core", new EntityList<Object>(), new EntityList<Room>(), centerBlock, new EntityList<Material> { c.CulturalGemstone }, new List<string> { c.CulturalGemstone.Name }, new List<string> { "crystals" }, Game1.r.Next(0, 5), Game1.r.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                            centerBlock.Structures.Add(core);
                            l.Prism = core;

                            int scaffoldCount = Game1.r.Next(3, 5); // Reduce the total number of scaffolds
                            HashSet<int> builtBlocks = new HashSet<int> { 24 }; // To avoid duplicate structures

                            for (int i = 0; i < scaffoldCount; i++)
                            {
                                int x = Game1.r.Next(0, 7);
                                int z = Game1.r.Next(0, 7);

                                if (x == 3 && z == 3) continue; // Skip the center block

                                // Create and place the initial scaffold structure
                                int index = x + z * 7;
                                if (!builtBlocks.Contains(index))
                                {
                                    Block chosenBlock = l.Districts[0].DistrictMap[index];
                                    Structure scaffold = new Structure("scaffold", new EntityList<Object>(), new EntityList<Room>(), chosenBlock, new EntityList<Material> { c.CulturalGemstone }, new List<string> { c.CulturalGemstone.Name }, new List<string> { "crystals" }, Game1.r.Next(0, 5), Game1.r.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                    chosenBlock.Structures.Add(scaffold);
                                    builtBlocks.Add(index);
                                }

                                // Reflect across the X-axis
                                int reflectedX = 6 - x;
                                index = reflectedX + z * 7;
                                if (x != 3 && !builtBlocks.Contains(index))
                                {
                                    Block reflectedBlockX = l.Districts[0].DistrictMap[index];
                                    Structure reflectedStructureX = new Structure("scaffold", new EntityList<Object>(), new EntityList<Room>(), reflectedBlockX, new EntityList<Material> { c.CulturalGemstone }, new List<string> { c.CulturalGemstone.Name }, new List<string> { "crystals" }, Game1.r.Next(0, 5), Game1.r.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                    reflectedBlockX.Structures.Add(reflectedStructureX);
                                    builtBlocks.Add(index);
                                }

                                // Reflect across the Z-axis
                                int reflectedZ = 6 - z;
                                index = x + reflectedZ * 7;
                                if (z != 3 && !builtBlocks.Contains(index))
                                {
                                    Block reflectedBlockZ = l.Districts[0].DistrictMap[index];
                                    Structure reflectedStructureZ = new Structure("scaffold", new EntityList<Object>(), new EntityList<Room>(), reflectedBlockZ, new EntityList<Material> { c.CulturalGemstone }, new List<string> { c.CulturalGemstone.Name }, new List<string> { "crystals" }, Game1.r.Next(0, 5), Game1.r.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                    reflectedBlockZ.Structures.Add(reflectedStructureZ);
                                    builtBlocks.Add(index);
                                }

                                // Reflect across both axes
                                if (x != 3 && z != 3)
                                {
                                    index = reflectedX + reflectedZ * 7;
                                    if (!builtBlocks.Contains(index))
                                    {
                                        Block reflectedBlockBoth = l.Districts[0].DistrictMap[index];
                                        Structure reflectedStructureBoth = new Structure("scaffold", new EntityList<Object>(), new EntityList<Room>(), reflectedBlockBoth, new EntityList<Material> { c.CulturalGemstone }, new List<string> { c.CulturalGemstone.Name }, new List<string> { "crystals" }, Game1.r.Next(0, 5), Game1.r.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                        reflectedBlockBoth.Structures.Add(reflectedStructureBoth);
                                        builtBlocks.Add(index);
                                    }
                                }
                            }
                        }

                        l.Districts[0].DistrictMap[Game1.r.Next(2, 6) + Game1.r.Next(2, 5) * 7].Objects.Add(new Object(null, "well", new EntityList<Material> { l.HomeCivilization.CulturalStone }, true, true, null, null, 255, false, null, null, null, false));

                        WorldMap[c.StartX + c.StartZ * Width].MyLocation = l;

                        if (race.Name == "photonexus")
                        {
                            l.HomeCivilization.Citizens.Add(Hypernexus);
                            l.Government = Hypernexus;
                            l.Districts[0].Architects.Add(Hypernexus);
                            HistoricalEvents.Add(Date + " The nexus of perfection, " + l.Name + ", fell to the land, controlled by the sovereign nexus " + Hypernexus.Name + ".");
                        }
                        else if (race.Name == "isofractal")
                        {
                            l.HomeCivilization.Citizens.Add(Icosidodecahedron);
                            l.Government = Icosidodecahedron;
                            l.Districts[0].Architects.Add(Icosidodecahedron);
                            HistoricalEvents.Add(Date + " The prism of expression, " + l.Name + ", was forged as a beacon of reality, manifesting under the control of " + Icosidodecahedron.Name + ".");
                        }
                        else if (race.Name == "shade")
                        {
                            l.HomeCivilization.Citizens.Add(Shadeheart);
                            l.Government = Shadeheart;
                            l.Districts[0].Architects.Add(Shadeheart);
                            HistoricalEvents.Add(Date + " The heart of corruption, " + l.Name + ", erupted from the depths of the world, establishing a cluster of ruinous veins in " + Shadeheart.Name + ".");
                        }
                    }
                }

                void PlaceOutcastCiv(string Type)
                {
                    // List to store all valid locations
                    List<(int X, int Z, string Dockside)> validLocations = new List<(int X, int Z, string Dockside)>();

                    // Iterate over every spot in the world grid
                    for (int TryX = 0; TryX < Width; TryX++)
                    {
                        for (int TryZ = 0; TryZ < Length; TryZ++)
                        {
                            // Check a square of size 5 centered around (TryX, TryZ)
                            bool validLocation = true;
                            for (int i = TryX - 3; i <= TryX + 3 && validLocation; i++)
                            {
                                for (int j = TryZ - 3; j <= TryZ + 3 && validLocation; j++)
                                {
                                    // Check if any region inside the area's Region.Location is not equal to null
                                    if (i <= 0 || i >= Width || j <= 0 || j >= Length ||
                                        WorldMap[i + j * Width].Biome == "void" ||
                                        WorldMap[i + j * Width].MyLocation != null)
                                    {
                                        validLocation = false;
                                    }
                                }
                            }

                            if (validLocation)
                            {
                                bool SpecificConditionsMet = false;
                                string dockside = null;

                                if (Type == "druid")
                                {
                                    SpecificConditionsMet = (WorldMap[TryX + TryZ * Width].Biome == "forest");
                                }
                                else if (Type == "pirate")
                                {
                                    SpecificConditionsMet = (WorldMap[TryX + TryZ * Width].Biome == "ocean" &&
                                        ((WorldMap[(TryX) + (TryZ + 1) * Width].Biome != "ocean") ||
                                         (WorldMap[(TryX) + (TryZ - 1) * Width].Biome != "ocean") ||
                                         (WorldMap[(TryX - 1) + (TryZ) * Width].Biome != "ocean") ||
                                         (WorldMap[(TryX + 1) + (TryZ) * Width].Biome != "ocean")));

                                    if (SpecificConditionsMet)
                                    {
                                        List<string> docksideOptions = new List<string>();
                                        if (WorldMap[(TryX) + (TryZ + 1) * Width].Biome != "ocean")
                                        {
                                            docksideOptions.Add("south");
                                        }
                                        if (WorldMap[(TryX) + (TryZ - 1) * Width].Biome != "ocean")
                                        {
                                            docksideOptions.Add("north");
                                        }
                                        if (WorldMap[(TryX - 1) + (TryZ) * Width].Biome != "ocean")
                                        {
                                            docksideOptions.Add("west");
                                        }
                                        if (WorldMap[(TryX + 1) + (TryZ) * Width].Biome != "ocean")
                                        {
                                            docksideOptions.Add("east");
                                        }

                                        if (docksideOptions.Count > 0)
                                        {
                                            dockside = docksideOptions[Game1.r.Next(docksideOptions.Count)];
                                        }
                                    }
                                }
                                else if (Type == "cultist")
                                {
                                    SpecificConditionsMet = (WorldMap[TryX + TryZ * Width].Biome == "snowpeak");
                                }
                                else if (Type == "anarchist")
                                {
                                    SpecificConditionsMet = (WorldMap[TryX + TryZ * Width].Biome == "desert");
                                }
                                else if (Type == "scavenger")
                                {
                                    SpecificConditionsMet = (WorldMap[TryX + TryZ * Width].Biome != "ocean");
                                }

                                if (SpecificConditionsMet)
                                {
                                    validLocations.Add((TryX, TryZ, dockside));
                                }
                            }
                        }
                    }

                    if (validLocations.Count > 0)
                    {
                        // Pick a random location from the valid locations
                        var (FoundX, FoundZ, FoundDockside) = validLocations[r.Next(validLocations.Count)];

                        Civilization c = new Civilization(GetRace(""), Type, FoundX, FoundZ, this);

                        // Find the perfect specimen
                        EntityList<Architect> PossibleArch = new EntityList<Architect>();
                        foreach (Architect a in AllArchitects)
                        {
                            if (a.Group == null && !Calamity.Contains(a) && a.IsAlive)
                            {
                                PossibleArch.Add(a);
                                break;
                            }
                        }

                        c.Alpha = PossibleArch[r.Next(PossibleArch.Count)];
                        c.Color = "black";

                        Dictionary<string, string> OutcastCivToStructure = new Dictionary<string, string>
        {
            { "druid", "preserve" },
            { "pirate", "cove" },
            { "cultist", "monastery" },
            { "anarchist", "commune" },
            { "scavenger", "hoard" }
        };

                        Civilizations.Add(c);

                        LocationBuilderPacket l = new LocationBuilderPacket(c.Alpha, FoundX, FoundZ, OutcastCivToStructure[Type], GetRace(""), 20, 2, c, new EntityList<Object>(), c.Alpha.HomeLocation, FoundDockside);
                        LocationBuilderPackets.Add(l);
                    }
                    else
                    {
                        HistoricalEvents.Add(Date + " The world was too chaotic for even the " + Type + "s to stand.");
                    }
                }





                if (Cycle / 290304000 > LostFoundingYear && LostPlaced == false)
                {
                    int StartX = 0;
                    int StartZ = 0;

                    while (WorldMap[StartX + StartZ * Width].Biome == "ocean" || WorldMap[StartX + StartZ * Width].Biome == "void" || WorldMap[StartX + StartZ * Width].MyLocation != null)
                    {
                        StartX = r.Next(Width);
                        StartZ = r.Next(Length);
                    }

                    Civilization c = new Civilization(GetRace("archaix"), "archaix", StartX, StartZ, this);
                    Civilizations.Add(c);
                    LostPlaced = true;

                    Location l = new Location("village", GetRace("archaix"), Game1.r.Next(50, 100), 1000, Game1.r.Next(4, 8), c.StartX, c.StartZ, c, WorldMap[c.StartX + c.StartZ * Width], "none");
                    AllLocations.Add(l);
                    l.IsCapitol = true;
                    c.Capitol = l;
                    ClaimSwathOfTerritory(c, l.X, l.Z, 2);

                    Block chosenBlock = l.Districts[0].DistrictMap[Game1.r.Next(0, 49)];
                    Structure Prism = new Structure("prism", new EntityList<Object>(), new EntityList<Room>(), chosenBlock, new EntityList<Material> { c.CulturalStone }, new List<string>(), new List<string> { Game1.LightingStyles[Game1.r.Next(Game1.LightingStyles.Count)] }, Game1.r.Next(0, 5), Game1.r.Next(0, 4), (int)Math.Round(Cycle / 290304000));

                    chosenBlock.Structures.Add(Prism);
                    l.Prism = Prism;

                    for (int i = 0; i < Game1.r.Next(10, 20); i++)
                    {
                        Block ChosenBlock = l.Districts[0].DistrictMap[Game1.r.Next(0, 49)];
                        Structure s = new Structure("house", new EntityList<Object>(), new EntityList<Room>(), ChosenBlock, new EntityList<Material> { c.CulturalWood }, new List<string> { c.CulturalWood.Name }, new List<string> { Game1.LightingStyles[Game1.r.Next(Game1.LightingStyles.Count)] }, Game1.r.Next(0, 5), Game1.r.Next(0, 4), (int)Math.Round(Cycle / 290304000));

                        ChosenBlock.Structures.Add(s);
                    }

                    Block b = l.Districts[0].DistrictMap[r.Next(2, 6) + r.Next(2, 5) * 7];

                    b.Objects.Add(new Object(null, "well", new EntityList<Material> { l.HomeCivilization.CulturalStone }, true, true, null, null, 255, false, null, null, null, false));
                    b.Objects.Add(new Object(null, "shadow storage", new EntityList<Material>() { Shadesteel }, DarkDeity));

                    WorldMap[c.StartX + c.StartZ * Width].MyLocation = l;
                }

                if (Cycle > 14515200000 && FirstNewCivPlaced == false)
                {
                    FirstNewCivPlaced = true;
                    PlaceFancyCiv(ExtraRaces[0]);
                }
                else if (Cycle > 29030400000 && SecondNewCivPlaced == false)
                {
                    SecondNewCivPlaced = true;
                    PlaceFancyCiv(ExtraRaces[1]);
                }
                else if (Cycle > 58060800000 && ThirdNewCivPlaced == false)
                {
                    ThirdNewCivPlaced = true;
                    PlaceFancyCiv(ExtraRaces[2]);
                }



                if (Cycle / 290304000 > FirstOutcastCivStartYear && FirstOutcastCivPlaced == false)
                {
                    FirstOutcastCivPlaced = true;
                    PlaceOutcastCiv(DecidedOutcastCivs[0]);
                }
                else if (Cycle / 290304000 > SecondOutcastCivStartYear && SecondOutcastCivPlaced == false)
                {
                    SecondOutcastCivPlaced = true;
                    PlaceOutcastCiv(DecidedOutcastCivs[1]);
                }
                else if (Cycle / 290304000 > ThirdOutcastCivStartYear && ThirdOutcastCivPlaced == false)
                {
                    ThirdOutcastCivPlaced = true;
                    PlaceOutcastCiv(DecidedOutcastCivs[2]);
                }

                //THE ADVERSARY RISES


                if (Cycle > 290304000 * CalamityStartingYear && Calamity.Count == 0)
                {
                    List<string> FirstPartNames = new List<string>()
                    {
                        "Dusk", "Dark", "Blight", "Day", "Dawn", "Gloom", "Storm",
                        "Frost", "Mist", "Moon",
                        "Rain", "Star", "Sun", "Thorn", "Wind"
                    };
                    List<string> SecondPartNames = new List<string>()
                    {
                        "fall", "fell", "death", "rise", "veil", "thorn", "blaze", "void", "flame", "tide", "wrath", "bane"
                    };

                    List<string> Adjectives = new List<string>()
                    {
                        "Promised", "Inevitable", "Glorified", "Eternal", "Forsaken", "Unseen", "Sacred", "Fallen",
                        "Cursed", "Hallowed", "Forbidden", "Lost", "Ancient", "Endless", "Silent", "Bound", "Veiled",
                        "Doomed", "Cathartic", "Chosen", "Dark", "Collapsing", "Primeval", "Mystic"
                    };

                    var ideologicalReasonings = new Dictionary<string, List<string>>()
                    {
                        {"disease", new List<string>() {"restorebalance", "popcontrol", "revengesomething"}},
                        {"dominator", new List<string>() {"uniteorder", "enforceutopia", "endconflict"}},
                        {"purifier", new List<string>() {"startanew", "ultimatetranquility", "naturesupremacy", "eliminatecorruption"}},
                        {"killer", new List<string>() {"misguidedmercy", "freedomtodo", "notbelonging"}},
                        {"kidnapper", new List<string>() {"divinedeal", "createnewsociety", "harnesstalent"}},
                        {"corruptor", new List<string>() {"toppleorder", "spreadchaos", "newideology"}},
                        {"diplomancer", new List<string>() {"forfun", "personalhatred", "publicwrong"}},
                        {"inciter", new List<string>() {"forgestrongerworld", "profitconflict"}},
                        {"power", new List<string>() {"supremerule", "transcendmorality", "infinitewisdom"}}
                    };

                    if (LockedInThreat == "non-cataclysmic")
                    {
                        CalamityIdeologicalObsession = new List<string>() { "dominator", "killer", "kidnapper", "corruptor", "diplomancer", "inciter", "power" }[r.Next(7)]; //MAKE SURE WE CHANGE THIS BACK R.NEXT(0,9)
                    }
                    else if (LockedInThreat == "random")
                    {
                        CalamityIdeologicalObsession = new List<string>() { "disease", "dominator", "purifier", "killer", "kidnapper", "corruptor", "diplomancer", "inciter", "power" }[r.Next(9)]; //MAKE SURE WE CHANGE THIS BACK R.NEXT(0,9)
                    }
                    else
                    {
                        CalamityIdeologicalObsession = LockedInThreat;
                    }

                    if (ideologicalReasonings.ContainsKey(CalamityIdeologicalObsession))
                    {
                        var reasoningsList = ideologicalReasonings[CalamityIdeologicalObsession];
                        CalamityReasoning = reasoningsList[r.Next(reasoningsList.Count)]; // Select a random reasoning ID from the list
                    }

                    var codeNameThemes = new Dictionary<string, List<string>>
                    {
                        { "disease", new List<string> { "Plague", "Corruption", "Disaster" } },
                        { "dominator", new List<string> { "Tyrant", "Overlord", "Conqueror" } },
                        { "killer", new List<string> { "Exterminator", "Assassin", "Destroyer" } },
                        { "purifier", new List<string> { "Cleanser", "Annihilator", "Eradicator" } },
                        { "kidnapper", new List<string> { "Abductor", "Snatcher", "Harvester" } },
                        { "corruptor", new List<string> { "Defiler", "Degrader", "Influencer" } },
                        { "diplomancer", new List<string> { "Propagandist", "Misinformer", "Deceiver" } },
                        { "inciter", new List<string> { "Agitator", "Provoker", "Instigator", "Accuser" } },
                        { "power", new List<string> { "Beacon", "End", "Monolith" } }
                    };

                    Dictionary<string, string> motivationsExposition = new Dictionary<string, string>
                    {
                        {"restorebalance", "After discovering the terrible imbalance of the world,"},
                        {"popcontrol", "After realizing that life was destroying the entire world,"},
                        {"revengesomething", "Fueled by a personal vendetta against an unforgivable act,"},
                        {"uniteorder", "With a vision to unite the world under a single order,"},
                        {"enforceutopia", "Driven by the belief in creating a perfect society,"},
                        {"endconflict", "Seeking to end all conflicts and bring about lasting peace,"},
                        {"startanew", "Desiring to erase the past and start anew for a better future,"},
                        {"ultimatetranquility", "Chasing the dream of ultimate tranquility and harmony,"},
                        {"naturesupremacy", "Believing in the supremacy of nature over all creations,"},
                        {"eliminatecorruption", "Determined to eliminate corruption and purify society,"},
                        {"misguidedmercy", "Convinced that their actions are a form of mercy,"},
                        {"freedomtodo", "Embracing the freedom to do anything without constraints,"},
                        {"notbelonging", "Feeling an intense sense of not belonging to this world,"},
                        {"divinedeal", "Believing they have struck a divine deal to alter the course of humanity,"},
                        {"createnewsociety", "With the ambition to create a new society from the ashes of the old,"},
                        {"harnesstalent", "Seeing the potential to harness talent for a grand vision,"},
                        {"toppleorder", "Intent on toppling the existing order to make way for chaos,"},
                        {"spreadchaos", "Thriving in the spread of chaos and disorder,"},
                        {"newideology", "Promoting a new ideology believed to be superior to all others,"},
                        {"forfun", "Engaging in manipulation and deceit just for fun,"},
                        {"personalhatred", "Driven by a deep-seated personal hatred and desire for revenge,"},
                        {"publicwrong", "Seeking justice for a public wrong that was never addressed,"},
                        {"forgestrongerworld", "Believing that conflict and challenge forge a stronger world,"},
                        {"profitconflict", "Seeing opportunity for profit in the midst of conflict,"},
                        {"supremerule", "Aspiring to supreme rule and unchecked power,"},
                        {"transcendmorality", "Believing themselves above the constraints of morality,"},
                        {"infinitewisdom", "Convinced of their own infinite wisdom and right to lead,"}
                    };

                    Dictionary<string, List<string>> motivationGoals = new Dictionary<string, List<string>>
                    {
                        { "disease", new List<string> { "sought to flood the world in a terrible plague", "aimed to unleash a devastating epidemic", "planned to spread a lethal contagion" } },
                        { "dominator", new List<string> { "dreamed of ruling with an iron fist", "sought to bring all under their dominion", "aimed to conquer and subjugate the entire world" } },
                        { "purifier", new List<string> { "intended to cleanse the world of existence", "sought to purge the island of all matter", "aimed to eradicate all that existed" } },
                        { "killer", new List<string> { "desired to destroy all life", "chose to execute all life", "sought to deliver their version of misguided, murderous justice" } },
                        { "kidnapper", new List<string> { "planned a terrible abduction spree", "aimed to capture those of value for an unknown purpose", "sought to steal people from their homes by any means necessary" } },
                        { "corruptor", new List<string> { "intended to undermine the very fabric of society", "dreamed of spreading chaos to rebuild from ashes", "sought to corrupt those he deemed responsible for the pain" } },
                        { "diplomancer", new List<string> { "manipulated the world for their personal gain", "sought to deceive and misinform for ultimate control", "aimed to twist the fabric of reason for their personal gain" } },
                        { "inciter", new List<string> { "fueled the flames of conflict for their twisted revenge", "sought to provoke war and pain", "aimed to instigate a great turmoil" } },
                        { "power", new List<string> { "aspired to attain ultimate power", "dreamed of transcending their current limitations", "sought to wield supreme unchallenged magical power" } }
                    };

                    string name = FirstPartNames[r.Next(FirstPartNames.Count)] + SecondPartNames[r.Next(SecondPartNames.Count)] + ", the " + Adjectives[r.Next(Adjectives.Count)] + " " + (codeNameThemes[CalamityIdeologicalObsession])[r.Next(codeNameThemes[CalamityIdeologicalObsession].Count)];
                    Calamity.Add(new Architect(name, Game1.Sexes[r.Next(2)], HumanoidRaces[r.Next(HumanoidRaces.Count)], r.Next(13, 34), "calamity", new EntityList<Object>(), Civilizations[r.Next(Civilizations.Count)].Capitol, null, null, "", 10));
                    Calamity[0].HomeLocation = Calamity[0].Location;
                    Calamity[0].InteractionLocation = Calamity[0].Location;
                    CalamityLore.Add(Calamity[0].Name + " was a " + Calamity[0].Race.Name + " from " + Calamity[0].HomeLocation.Name + ".");

                    Calamity[0].OppositionTags.Add("intruders");

                    Calamity[0].Strength = 12;
                    Calamity[0].Dexterity = 12;
                    Calamity[0].Agility = 12;
                    Calamity[0].Charisma = 12;
                    Calamity[0].Focus = 12;
                    Calamity[0].Creativity = 12;
                    Calamity[0].Endurance = 12;



                    List<string> expositions = new List<string>()
                    {
                        "grew up in a broken home",
                        "was dealt the bad card of life",
                        "lived through a painful parental divorce",
                        "witnessed injustice and corruption from a young age",
                        "felt isolated and misunderstood by peers",
                        "experienced betrayal by a trusted friend",
                        "suffered from chronic poverty and lack of resources",
                        "endured bullying and discrimination",
                        "lost loved ones to a senseless act of violence",
                        "faced rejection and failure repeatedly",
                        "had dreams and aspirations crushed by societal expectations",
                        "had lived through the degredation of their community",
                        "was indoctrinated with radical views from a vulnerable age",
                        "felt powerless in the face of overwhelming adversity"
                    };

                    List<string> DeterminedExpositions = new List<string>();
                    for (int i = r.Next(2, 5); i != 0; i--)
                    {
                        int Index = r.Next(expositions.Count);
                        DeterminedExpositions.Add(expositions[Index]);
                        expositions.RemoveAt(Index);
                    }

                    CalamityLore.Add(Game1.Capitalize(Calamity[0].Pronoun + " " + Game1.FormatList(DeterminedExpositions) + "."));
                    CalamityLore.Add(motivationsExposition[CalamityReasoning] + " " + Calamity[0].Pronoun + " " + motivationGoals[CalamityIdeologicalObsession][r.Next(3)] + ".");

                    //find a spot to build a base

                    bool FoundSpot = false;
                    while (!FoundSpot)
                    {
                        int X = Game1.r.Next(Width);
                        int Z = Game1.r.Next(Length);
                        if (WorldMap[X + Z * Width].Biome != "ocean" && WorldMap[X + Z * Width].Biome != "void" && WorldMap[X + Z * Width].MyLocation == null)
                        {
                            FoundSpot = true;
                            LocationBuilderPacket l = new LocationBuilderPacket(Calamity[0], X, Z, "stronghold", GetRace(""), 0, 0, Calamity[0].HomeLocation.HomeCivilization, new EntityList<Object>(), null, "none");
                            LocationBuilderPackets.Add(l);
                            foreach (string s in CalamityLore)
                            {
                                HistoricalEvents.Add(Date + " " + s);
                            }
                            HistoricalEvents.Add(Date + " Calamity has risen. " + Calamity[0].Name + " is upon you.");
                        }
                    }

                    Calamity[0].IsCalamity = true;
                    Calamity[0].Level = 10;

                    //set some important vars

                    if (CalamityIdeologicalObsession == "disease")
                    {
                        Calamity[0].BlightManipulated = Blights[r.Next(Blights.Count)];

                        if (Calamity[0].BlightManipulated.FoundingYear > Cycle / 290304000)
                        {
                            Calamity[0].BlightManipulated.FoundingYear = (int)(Math.Round(Cycle / 290304000));
                            Calamity[0].BlightManipulated.Spawned = true;

                            HistoricalEvents.Add(Date + " " + Calamity[0].Name + " unleashed " + Calamity[0].BlightManipulated + " in " + Calamity[0].Location + ".");
                            Calamity[0].Location.Region.Blight = Calamity[0].BlightManipulated;
                        }
                        else
                        {
                            HistoricalEvents.Add(Date + " " + Calamity[0].Name + " began assisting the spread of " + Calamity[0].BlightManipulated + " in " + Calamity[0].Location + ".");
                            Calamity[0].Location.Region.Blight = Calamity[0].BlightManipulated;
                        }
                    }
                }


                //THE CALAMITY IS UPON YOU. WHAT IS IT DOING?

                if (Calamity != null)
                {
                    EntityList<Architect> CalamitiesToAdd = new EntityList<Architect>();

                    foreach (Architect Calamitizer in Calamity)
                    {
                        //age

                        Calamitizer.TerminalAge = 999999;

                        Calamitizer.CalamityAge += (1.0 / 12.0);

                        //recruit peoples


                        if (Calamitizer.CalamityAge >= Calamitizer.CalamitySpawnTime && Calamitizer.Level >= 4)
                        {
                            Calamitizer.CalamitySpawnTime = 2140000000; // Prevent future spawns

                            int Count = 0;
                            List<string> Types = new List<string>();

                            // Determine Count and Types based on Calamitizer.Level - 2
                            switch (Calamitizer.Level - 2)
                            {
                                case 8:
                                    Count = r.Next(4, 7);
                                    Types.AddRange(new List<string> { "archbard", "archluminary", "archartificer", "archduelist", "warlock", "sorcerer", "elemental", "hypernexus", "icosidodecahedron", "shadeheart", "necromancer", "spatiomancer", "perceptomancer", "conjumancer", "fractalmancer" });
                                    break;
                                case 6:
                                    Count = r.Next(3, 6);
                                    Types.AddRange(new List<string> { "necromancer", "spatiomancer", "perceptomancer", "conjumancer", "fractalmancer", "embezzler", "beast", "knight", "thief", "archmage", "beastmaster", "duelist", "luminary", "artificer", "bard", "mage", "largebeast", "spy", "diplomancer" });
                                    break;
                                case 4:
                                    Count = r.Next(2, 4);
                                    Types.AddRange(new List<string> { "scout", "animal", "hunter", "mercenary", "magician", "embezzler", "beast", "knight", "thief", "duelist", "luminary", "artificer", "bard", "mage", "largebeast", "spy", "diplomancer" });
                                    break;
                                case 2:
                                    Count = r.Next(2, 6);
                                    Types.AddRange(new List<string> { "scout", "animal", "hunter", "mercenary", "embezzler", "beast", "knight", "thief", "magician" });
                                    break;
                            }

                            for (int i = Count; i != 0; i--)
                            {
                                Architect FoundGuy = null;
                                int searchAttempts = 3; // Number of attempts to search for an existing architect
                                bool foundInWorld = false;

                                List<string> SearchTheWorldTypes = new List<string> { "archartificer", "archbard", "archluminary", "archmage", "artificer", "bard", "icosidodecahedron", "hypernexus", "luminary", "mage", "shadeheart", "sorcerer", "warlock" };
                                List<string> CreateYourOwnTypes = new List<string> { "animal", "beast", "beastmaster", "conjumancer", "diplomancer", "duelist", "elemental", "embezzler", "fractalmancer", "hunter", "knight", "largebeast", "magician", "mercenary", "necromancer", "perceptomancer", "scout", "spatiomancer", "spy", "thief", "archduelist" };

                                bool initialSearchWorld = r.NextDouble() < 0.5; // 50/50 chance to search the world or create your own initially

                                if (initialSearchWorld)
                                {
                                    while (searchAttempts > 0 && !foundInWorld)
                                    {
                                        string ChosenType = SearchTheWorldTypes[r.Next(SearchTheWorldTypes.Count)];

                                        bool Breaking = false;

                                        foreach (Location l in AllLocations)
                                        {
                                            foreach (District d in l.Districts)
                                            {
                                                foreach (Architect a in d.Architects)
                                                {
                                                    if (a.Profession == ChosenType && !Calamity.Contains(a))
                                                    {
                                                        FoundGuy = a;
                                                        d.Architects.Remove(a);
                                                        a.Level = Calamitizer.Level - 2;
                                                        Breaking = true;
                                                        foundInWorld = true;
                                                        break;
                                                    }
                                                }
                                                if (Breaking)
                                                {
                                                    break;
                                                }
                                            }
                                            if (Breaking)
                                            {
                                                break;
                                            }
                                        }

                                        searchAttempts--;
                                    }
                                }

                                if (!foundInWorld)
                                {
                                    // Filter CreateYourOwnTypes based on Calamitizer.Level - 2
                                    List<string> filteredCreateYourOwnTypes = new List<string>();

                                    switch (Calamitizer.Level - 2)
                                    {
                                        case 8:
                                            filteredCreateYourOwnTypes.AddRange(new List<string> { "archbard", "archluminary", "archartificer", "archduelist", "warlock", "sorcerer", "elemental", "necromancer", "spatiomancer", "perceptomancer", "conjumancer", "fractalmancer" });
                                            break;
                                        case 6:
                                            filteredCreateYourOwnTypes.AddRange(new List<string> { "necromancer", "spatiomancer", "perceptomancer", "conjumancer", "fractalmancer", "embezzler", "beast", "knight", "thief", "archmage", "beastmaster", "duelist", "luminary", "artificer", "bard", "mage", "largebeast", "spy", "diplomancer" });
                                            break;
                                        case 4:
                                            filteredCreateYourOwnTypes.AddRange(new List<string> { "scout", "animal", "hunter", "mercenary", "magician", "embezzler", "beast", "knight", "thief", "duelist", "luminary", "artificer", "bard", "mage", "largebeast", "spy", "diplomancer" });
                                            break;
                                        case 2:
                                            filteredCreateYourOwnTypes.AddRange(new List<string> { "scout", "animal", "hunter", "mercenary", "embezzler", "beast", "knight", "thief", "magician" });
                                            break;
                                    }

                                    string ChosenType = filteredCreateYourOwnTypes[r.Next(filteredCreateYourOwnTypes.Count)];

                                    Race R = null;
                                    if (ChosenType == "animal" || ChosenType == "beast" || ChosenType == "largebeast")
                                    {
                                        R = WildRaces[r.Next(WildRaces.Count)];
                                    }
                                    else if (ChosenType == "elemental")
                                    {
                                        R = ConstructRaces[r.Next(ConstructRaces.Count)];
                                    }

                                    FoundGuy = new Architect("", Game1.Sexes[r.Next(Game1.Sexes.Count)], R ?? HumanoidRaces[r.Next(HumanoidRaces.Count)], r.Next(10, 80), ChosenType, new EntityList<Object>(), null, null, null, "", Calamitizer.Level - 2);
                                    FoundGuy.Name = GenerateUniqueArchitectName(FoundGuy);
                                }

                                if (FoundGuy != null)
                                {
                                    FoundGuy.Level = Calamitizer.Level - 2;
                                    string Type = new Dictionary<int, string> { { 2, "keep" }, { 4, "tower" }, { 6, "fortress" }, { 8, "monument" } }[FoundGuy.Level];

                                    if (Calamitizer.BlightManipulated != null)
                                    {
                                        FoundGuy.BlightManipulated = Calamitizer.BlightManipulated;
                                    }

                                    bool Found = false;
                                    int X = 0;
                                    int Z = 0;

                                    while (!Found)
                                    {
                                        X = Game1.r.Next(Width);
                                        Z = Game1.r.Next(Length);

                                        if (WorldMap[X + Z * Width].MyLocation == null && WorldMap[X + Z * Width].Biome != "void" && WorldMap[X + Z * Width].Biome != "ocean" && WorldMap[X + Z * Width].Biome != "ethereal")
                                        {
                                            Found = true;
                                        }
                                    }

                                    FoundGuy.Inventory.AddRange(LootTableMachine("bosstreasure" + Math.Round((double)(FoundGuy.Level / 2), MidpointRounding.ToPositiveInfinity)));

                                    FoundGuy.AssignSpells();

                                    FoundGuy.Master = Calamitizer;
                                    List<string> Relations = new List<string>
            {
                "vanguard",
                "emissary",
                "disciple",
                "informant",
                "conspirator",
                "spy",
                "supplier",
                "strategist",
                "guardian",
                "harbinger",
                "mediator",
                "ally",
                "scout",
                "representative",
                "protector",
                "mentor",
                "liaison",
                "ambassador",
                "negotiator",
                "steward",
                "herald"
            };

                                    FoundGuy.MasterRelation = Relations[r.Next(Relations.Count)];

                                    LocationBuilderPacket l = new LocationBuilderPacket(FoundGuy, X, Z, Type, GetRace(""), 0, 0, Civilizations[r.Next(Civilizations.Count)], LootTableMachine("bosstreasure" + Math.Round((double)(FoundGuy.Level / 2), MidpointRounding.ToPositiveInfinity)), AllLocations[r.Next(AllLocations.Count)], "none");
                                    LocationBuilderPackets.Add(l);

                                    FoundGuy.Strength = Math.Max(FoundGuy.Strength, FoundGuy.Level);
                                    FoundGuy.Dexterity = Math.Max(FoundGuy.Dexterity, FoundGuy.Level);
                                    FoundGuy.Agility = Math.Max(FoundGuy.Agility, FoundGuy.Level);
                                    FoundGuy.Endurance = Math.Max(FoundGuy.Endurance, FoundGuy.Level);
                                    FoundGuy.Creativity = Math.Max(FoundGuy.Creativity, FoundGuy.Level);
                                    FoundGuy.Charisma = Math.Max(FoundGuy.Charisma, FoundGuy.Level);
                                    FoundGuy.Focus = Math.Max(FoundGuy.Focus, FoundGuy.Level);

                                    FoundGuy.IsCalamity = true;

                                    HistoricalEvents.Add(Date + " " + Calamitizer.Name + " recruited " + FoundGuy.Name + " as a " + FoundGuy.MasterRelation + " to serve " + Calamitizer.ObjectivePronoun + " and the almighty " + Calamity[0].Name + ".");
                                    CalamitiesToAdd.Add(FoundGuy);
                                }
                            }
                        }

                        //do actions

                        if (r.Next((30 - (Calamitizer.Level*2)) * MonthToDayConstant) == 0 && Calamity[0].IsAlive)
                        {
                            //determine whether you want to move

                            if (r.Next(10 * MonthToDayConstant) == 1 || (CalamityIdeologicalObsession == "killer" && Calamitizer.InteractionLocation.TruePopulation() == 0))
                            {
                                //find a spot to move to
                                int Tries = 0;
                                while (Tries < 100)
                                {
                                    int XChange = Game1.r.Next(-10, 11);
                                    int ZChange = Game1.r.Next(-10, 11);

                                    if (!(Calamitizer.InteractionLocation.X + XChange < 0 || Calamitizer.InteractionLocation.X + XChange > Width - 1 || Calamitizer.InteractionLocation.Z + ZChange < 0 || Calamitizer.InteractionLocation.Z + ZChange > Length - 1))
                                    {
                                        if (WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].Biome != "ocean" && WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].Biome != "void" && WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].MyLocation != null && SettlementTypes.Contains(WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].MyLocation.Type))
                                        {
                                            HistoricalEvents.Add(Date + " " + Calamitizer.Name + " shifted their focus to " + WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].MyLocation.Name + ".");
                                            Calamitizer.InteractionLocation.LocationHistoricalEvents.Add(Date + " " + Calamitizer.Name + " shifted their focus to " + WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].MyLocation.Name + ".");
                                            Calamitizer.InteractionLocation = WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].MyLocation;
                                            Calamitizer.InteractionLocation.LocationHistoricalEvents.Add(Date + " " + Calamitizer.Name + " shifted their focus to " + Calamitizer.InteractionLocation.Name + ".");
                                            break;
                                        }
                                    }
                                    Tries++;
                                }
                            }
                            else if (!CalamityStructures.Contains(Calamitizer.InteractionLocation.Type))
                            {
                                //inflict pain and suffering

                                int GrievanceChance = 4;

                                void LogEvent(string Event)
                                {
                                    Calamitizer.InteractionLocation.LocationHistoricalEvents.Add(Date + " " + Event);
                                    HistoricalEvents.Add(Date + " " + Event);
                                }

                                District ChosenDistrict = Calamitizer.InteractionLocation.Districts
                                        .Where(d => d.Architects.Count > 0)
                                        .OrderBy(_ => r.Next())
                                        .FirstOrDefault();

                                if (ChosenDistrict != null)
                                {
                                    if (CalamityIdeologicalObsession == "disease")
                                    {
                                        if (Calamitizer.InteractionLocation.Region.Blight != Calamity[0].BlightManipulated)
                                        {
                                            LogEvent(Calamitizer.Name + " deliberately spread the " + Calamitizer.BlightManipulated.Name + " to " + Calamitizer.InteractionLocation.Name + ".");

                                            if (r.Next(30 * MonthToDayConstant) == 1)
                                            {
                                                Calamitizer.InteractionLocation.Region.Blight = Calamity[0].BlightManipulated;
                                                LogEvent(Calamitizer.Name + " fully established a terrible presence of " + Calamity[0].BlightManipulated + " in " + Calamitizer.InteractionLocation.Name + ".");
                                            }
                                        }

                                        foreach (Architect a in ChosenDistrict.Architects)
                                        {
                                            if (r.Next(GrievanceChance) == 1 && a != Calamitizer)
                                            {
                                                a.Grievances.Add((Calamitizer, " plagued " + a.PossessivePronoun + " town, " + a.Location.Name + "."));
                                                Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                            }
                                        }
                                    }
                                    else if (CalamityIdeologicalObsession == "dominator")
                                    {
                                        if (Calamitizer.InteractionLocation.Government == null)
                                        {
                                            LogEvent(Calamitizer.Name + " has peacefully taken control of " + Calamitizer.InteractionLocation.Name + ", as there was no governing body to oppose.");
                                            Calamitizer.TakenLocations.Add(Calamitizer.InteractionLocation);
                                            Calamitizer.InteractionLocation.Government = Calamitizer;
                                            foreach (Architect a in ChosenDistrict.Architects)
                                            {
                                                if (r.Next(GrievanceChance) == 1 && a != Calamitizer)
                                                {
                                                    a.Grievances.Add((Calamitizer, " unjustly took control of " + a.PossessivePronoun + " town, " + a.Location.Name + ""));
                                                    Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                }
                                            }
                                        }
                                        else if (!Calamity.Contains(Calamitizer.InteractionLocation.Government))
                                        {
                                            if (r.Next(2) == 1)
                                            {
                                                LogEvent(Calamitizer.Name + " threatened " + Calamitizer.InteractionLocation.Government.Name + ", the government of " + Calamitizer.InteractionLocation.Name + ", demanding they step down. " + Calamitizer.InteractionLocation.Government.Name + " complied.");
                                            }
                                            else
                                            {
                                                LogEvent(Calamitizer.Name + " threatened " + Calamitizer.InteractionLocation.Government.Name + ", the government of " + Calamitizer.InteractionLocation.Name + ", demanding they step down. " + Calamitizer.InteractionLocation.Government.Name + " refused, and " + Calamitizer.Name + " decided to brutally murder them.");

                                                if (Calamitizer.InteractionLocation.Government is Architect)
                                                {
                                                    Architect govArchitect = (Architect)(Calamitizer.InteractionLocation.Government);
                                                    govArchitect.District.Architects.Remove(govArchitect);
                                                    Calamitizer.KilledPeopleWhoActuallyMatter.Add(govArchitect);
                                                }
                                                else
                                                {
                                                    foreach (Architect a in ((Group)(Calamitizer.InteractionLocation.Government)).Architects)
                                                    {
                                                        a.District.Architects.Remove(a);
                                                        LogEvent(Calamitizer.Name + " killed " + a.Name + " as they were part of " + Calamitizer.InteractionLocation.Government.Name + ".");

                                                        Calamitizer.KilledPeopleWhoActuallyMatter.Add(a);
                                                    }
                                                }
                                            }

                                            foreach (Architect a in ChosenDistrict.Architects)
                                            {
                                                if (a != Calamitizer)
                                                {
                                                    a.Grievances.Add((Calamitizer, " unjustly took control of " + a.PossessivePronoun + " town, " + a.Location.Name + ""));
                                                    Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                }
                                            }

                                            Calamitizer.TakenLocations.Add(Calamitizer.InteractionLocation);
                                            Calamitizer.InteractionLocation.Government = Calamitizer;
                                        }

                                        // Additional grievance logic with tragedy point
                                        foreach (Location l in Calamitizer.TakenLocations)
                                        {
                                            if (r.Next(12 * MonthToDayConstant) == 1)
                                            {
                                                // Choose a random person in the location
                                                EntityList<Architect> potentialGrievants = l.Districts[0].Architects.Where(a => a != Calamitizer);
                                                if (potentialGrievants.Count > 0)
                                                {
                                                    Architect randomPerson = potentialGrievants[r.Next(potentialGrievants.Count)];
                                                    string grievance = $"'s sustained rule brought great hardship and loss of freedom on {randomPerson.PossessivePronoun} town, {l.Name}";
                                                    randomPerson.Grievances.Add((Calamitizer, grievance));
                                                    // Add a tragedy point to the region
                                                    l.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                }
                                            }
                                        }

                                    }
                                    else if (CalamityIdeologicalObsession == "killer")
                                    {
                                        if (Game1.r.Next(12 * MonthToDayConstant) == 1)
                                        {
                                            if (ChosenDistrict.UnplacedPopulation > 0 && r.Next(1, 3) == 1)
                                            {
                                                int InitialPop = ChosenDistrict.UnplacedPopulation;
                                                ChosenDistrict.UnplacedPopulation = Math.Max(0, ChosenDistrict.UnplacedPopulation - 1);

                                                LogEvent(Calamitizer.Name + " killed " + (InitialPop - ChosenDistrict.UnplacedPopulation).ToString() + " people in " + Calamitizer.InteractionLocation.Name + ".");

                                                int DecideAge = r.Next(1, 6);
                                                if (DecideAge == 1)
                                                {
                                                    Calamitizer.KilledChildren += (InitialPop - ChosenDistrict.UnplacedPopulation);
                                                }
                                                else if (DecideAge < 4)
                                                {
                                                    Calamitizer.KilledMen -= (InitialPop - ChosenDistrict.UnplacedPopulation);
                                                }
                                                else
                                                {
                                                    Calamitizer.KilledWomen -= (InitialPop - ChosenDistrict.UnplacedPopulation);
                                                }
                                            }
                                            else if (ChosenDistrict.Architects.Count > 0)
                                            {
                                                for (int i = r.Next(0, 3); i != 0; i--)
                                                {
                                                    int Index = r.Next(ChosenDistrict.Architects.Count);

                                                    if (Calamity.Contains(ChosenDistrict.Architects[Index]) || !ChosenDistrict.Architects[Index].IsAlive)
                                                    {
                                                        continue;
                                                    }

                                                    Architect affectedArchitect = ChosenDistrict.Architects[Index];
                                                    ChosenDistrict.ArchitectsToRemove.Add(affectedArchitect);

                                                    LogEvent(Calamitizer.Name + " assassinated " + affectedArchitect.Name + " in " + Calamitizer.InteractionLocation.Name + ".");

                                                    foreach (Architect a in ChosenDistrict.Architects)
                                                    {
                                                        if (r.Next(GrievanceChance) == 1 && a != affectedArchitect)
                                                        {
                                                            a.Grievances.Add((Calamitizer, " murdered a friend of " + a.Name + ", " + affectedArchitect.Name));
                                                            Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                        }
                                                    }

                                                    Calamitizer.KilledPeopleWhoActuallyMatter.Add(affectedArchitect);
                                                }
                                            }
                                        }
                                    }
                                    else if (CalamityIdeologicalObsession == "kidnapper")
                                    {
                                        if (ChosenDistrict.UnplacedPopulation > 0 && r.Next(1, 9 * MonthToDayConstant) == 1)
                                        {
                                            int InitialPop = ChosenDistrict.UnplacedPopulation;

                                            ChosenDistrict.UnplacedPopulation = Math.Max(0, ChosenDistrict.UnplacedPopulation - 1);

                                            LogEvent(Calamitizer.Name + " kidnapped " + (InitialPop - ChosenDistrict.UnplacedPopulation).ToString() + " person(s) in " + Calamitizer.InteractionLocation.Name + ".");

                                            int DecideAge = r.Next(1, 6);
                                            if (DecideAge == 1)
                                            {
                                                Calamitizer.KidnappedChildren += (InitialPop - ChosenDistrict.UnplacedPopulation);
                                            }
                                            else if (DecideAge < 4)
                                            {
                                                Calamitizer.KidnappedMen += (InitialPop - ChosenDistrict.UnplacedPopulation);
                                            }
                                            else
                                            {
                                                Calamitizer.KidnappedWomen += (InitialPop - ChosenDistrict.UnplacedPopulation);
                                            }
                                            foreach (Architect a in ChosenDistrict.Architects)
                                            {
                                                if (r.Next(GrievanceChance) == 1 && a != Calamitizer)
                                                {
                                                    a.Grievances.Add((Calamitizer, " kidnapped some people from the home of " + a.Name + ", causing distress in their community"));
                                                    Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                }
                                            }
                                        }
                                        else if (ChosenDistrict.Architects.Count > 0 && r.Next(1, 9 * MonthToDayConstant) == 1)
                                        {
                                            for (int i = r.Next(1, 3 * MonthToDayConstant); i != 0; i--)
                                            {
                                                int Index = r.Next(ChosenDistrict.Architects.Count);

                                                Architect affectedArchitect = ChosenDistrict.Architects[Index];
                                                ChosenDistrict.ArchitectsToRemove.Add(affectedArchitect);

                                                LogEvent(Calamitizer.Name + " kidnapped " + affectedArchitect.Name + " in " + Calamitizer.InteractionLocation.Name + ".");

                                                Calamitizer.KidnappedPeopleWhoActuallyMatter.Add(affectedArchitect);
                                                foreach (Architect a in ChosenDistrict.Architects)
                                                {
                                                    if (r.Next(GrievanceChance) == 1 && a != affectedArchitect)
                                                    {
                                                        a.Grievances.Add((Calamitizer, " kidnapped " + affectedArchitect.Name + ", a valued member of " + a.PossessivePronoun + " community"));
                                                        Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else if (CalamityIdeologicalObsession == "corruptor")
                                    {
                                        if (ChosenDistrict.Architects.Count > 0)
                                        {
                                            int Index = r.Next(ChosenDistrict.Architects.Count);

                                            Architect affectedArchitect = ChosenDistrict.Architects[Index];

                                            if (affectedArchitect == Calamitizer)
                                            {
                                                continue;
                                            }

                                            LogEvent(Calamitizer.Name + " corrupted " + affectedArchitect.Name + "'s moral values in " + Calamitizer.InteractionLocation.Name + ".");

                                            affectedArchitect.MoralCompass -= r.Next(10, 20);
                                            affectedArchitect.StabilityCompass -= r.Next(10, 20);

                                            foreach (Architect a in ChosenDistrict.Architects)
                                            {
                                                if (r.Next(GrievanceChance) == 1 && a != affectedArchitect)
                                                {
                                                    if (a == affectedArchitect)
                                                    {
                                                        a.Grievances.Add((Calamitizer, " was noticed by " + a.Name + ", who started to notice an evil difference in " + a.PossessivePronoun + " own psychology"));
                                                    }
                                                    else
                                                    {
                                                        a.Grievances.Add((Calamitizer, " was noticed by " + a.Name + ", who began to notice an evil difference in " + affectedArchitect.Name + ""));
                                                    }

                                                    Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                }
                                            }
                                        }
                                    }
                                    else if (CalamityIdeologicalObsession == "diplomancer")
                                    {
                                        if (ChosenDistrict.Architects.Count > 0)
                                        {
                                            int Index = r.Next(ChosenDistrict.Architects.Count);

                                            Architect affectedArchitect = ChosenDistrict.Architects[Index];

                                            if (affectedArchitect == Calamitizer)
                                            {
                                                continue;
                                            }

                                            affectedArchitect.MoralCompass -= r.Next(15, 35);

                                            LogEvent(Calamitizer.Name + " influenced " + affectedArchitect.Name + "'s values towards evil in " + Calamitizer.InteractionLocation.Name + ".");
                                            foreach (Architect a in ChosenDistrict.Architects)
                                            {
                                                if (r.Next(GrievanceChance) == 1 && a != affectedArchitect)
                                                {
                                                    a.Grievances.Add((Calamitizer, " was noticed by " + a.Name + ", who began to see a major change in " + affectedArchitect.Name + " towards evil"));
                                                    Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                }
                                            }
                                        }
                                    }
                                    else if (CalamityIdeologicalObsession == "inciter")
                                    {
                                        if (ChosenDistrict.Architects.Count > 0)
                                        {
                                            int Index = r.Next(ChosenDistrict.Architects.Count);
                                            LogEvent(Calamitizer.Name + " spread propaganda, attempting to influence " + Calamitizer.InteractionLocation.HomeCivilization.Name + " into a conflict.");

                                            Calamitizer.InteractionLocation.HomeCivilization.WakeUpAndChooseViolencePoints += r.Next(0, 5 / MonthToDayConstant);

                                            if (Calamitizer.InteractionLocation.HomeCivilization.WakeUpAndChooseViolencePoints > 200)
                                            {
                                                Civilization c = Civilizations[r.Next(Civilizations.Count)];

                                                Wars.Add((c, Calamitizer.InteractionLocation.HomeCivilization, 0, 0));

                                                foreach (Architect a in AllArchitects)
                                                {
                                                    if (a.HomeLocation != null && (a.HomeLocation.HomeCivilization == c || a.HomeLocation.HomeCivilization == Calamitizer.InteractionLocation.HomeCivilization))
                                                    {
                                                        if (r.Next(GrievanceChance / 2) == 1)
                                                        {
                                                            a.Grievances.Add((Calamitizer, " caused a war that ruined the stability of " + a.Name + "'s life"));
                                                            Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                        }
                                                    }
                                                }

                                                Calamitizer.InteractionLocation.HomeCivilization.WakeUpAndChooseViolencePoints = 0;
                                            }

                                            Architect affectedArchitect = ChosenDistrict.Architects[Index];
                                            affectedArchitect.MoralCompass -= r.Next(1, 3);
                                            affectedArchitect.StabilityCompass -= r.Next(1, 3);
                                        }
                                    }
                                    else if (CalamityIdeologicalObsession == "power")
                                    {
                                        if(r.Next(1,50*MonthToDayConstant) == 1)
                                        {
                                            if (ChosenDistrict.UnplacedPopulation > 0 && r.Next(1, 3) == 1)
                                            {
                                                int InitialPop = ChosenDistrict.UnplacedPopulation;
                                                ChosenDistrict.UnplacedPopulation = Math.Max(0, ChosenDistrict.UnplacedPopulation - 1);

                                                LogEvent(Calamitizer.Name + " killed " + (InitialPop - ChosenDistrict.UnplacedPopulation).ToString() + " people in " + Calamitizer.InteractionLocation.Name + ", and harvested their energy.");

                                                int DecideAge = r.Next(1, 6);
                                                if (DecideAge == 1)
                                                {
                                                    Calamitizer.KilledChildren += (InitialPop - ChosenDistrict.UnplacedPopulation);
                                                }
                                                else if (DecideAge < 4)
                                                {
                                                    Calamitizer.KilledMen -= (InitialPop - ChosenDistrict.UnplacedPopulation);
                                                }
                                                else
                                                {
                                                    Calamitizer.KilledWomen -= (InitialPop - ChosenDistrict.UnplacedPopulation);
                                                }
                                                foreach (Architect a in ChosenDistrict.Architects)
                                                {
                                                    if (r.Next(GrievanceChance) == 1 && a != Calamitizer)
                                                    {
                                                        a.Grievances.Add((Calamitizer, " harvested energy, causing the death of many in " + a.PossessivePronoun + " town, " + Calamitizer.InteractionLocation.Name + ""));
                                                        Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                    }
                                                }
                                            }
                                            if (ChosenDistrict.Architects.Count > 0)
                                            {
                                                for (int i = r.Next(0, 3); i != 0; i--)
                                                {
                                                    int Index = r.Next(ChosenDistrict.Architects.Count);

                                                    Architect affectedArchitect = ChosenDistrict.Architects[Index];

                                                    if (affectedArchitect == Calamitizer)
                                                    {
                                                        continue;
                                                    }

                                                    ChosenDistrict.ArchitectsToRemove.Add(affectedArchitect);

                                                    LogEvent(Calamitizer.Name + " assassinated " + affectedArchitect.Name + " in " + Calamitizer.InteractionLocation.Name + ", and harvested his energy.");

                                                    Calamitizer.KilledPeopleWhoActuallyMatter.Add(affectedArchitect);
                                                    foreach (Architect a in ChosenDistrict.Architects)
                                                    {
                                                        if (r.Next(GrievanceChance) == 1 && a != affectedArchitect)
                                                        {
                                                            a.Grievances.Add((Calamitizer, " murdered and harvested energy from " + affectedArchitect.Name + ", a good friend of theirs"));
                                                            Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        if (Calamitizer.KilledChildren + Calamitizer.KilledMen + Calamitizer.KilledWomen + Calamitizer.KilledPeopleWhoActuallyMatter.Count > 100 && Calamitizer.SpellsKnown.Count < 3)
                                        {
                                            Calamitizer.SpellsKnown = Game1.GameWorld.AllSpells.Union(Game1.GameWorld.AllLegendarySpells).ToList();
                                            Calamitizer.Focus = 15;
                                            LogEvent("After harvesting enough energy and renouncing the deities of the land, " + Calamitizer.Name + " became infused with unfathomable power from an unknown origin, but continued on to tempt the universe further.");
                                        }
                                    }
                                    else if (CalamityIdeologicalObsession == "purifier")
                                    {
                                        if (r.Next(1, 4 * MonthToDayConstant) == 1)
                                        {
                                            // Collect all valid regions with Biome "void" or "ethereal"
                                            List<(int x, int z)> validRegions = new List<(int, int)>();

                                            for (int x = 0; x < Width; x++)
                                            {
                                                for (int z = 0; z < Length; z++)
                                                {
                                                    Region region = WorldMap[x + z * Width];
                                                    if (region.Biome == "void" || region.Biome == "ethereal")
                                                    {
                                                        validRegions.Add((x, z));
                                                    }
                                                }
                                            }

                                            if (validRegions.Count > 0)
                                            {
                                                // Pick a random region from the valid regions
                                                var (ruptureX, ruptureZ) = validRegions[r.Next(validRegions.Count)];

                                                TriggerRupture(ruptureX, ruptureZ, Calamitizer, r.Next(1, 6)); // small radius between 1 and 3

                                                // Scan nearby regions within a certain range but outside the immediate rupture radius
                                                int scanRadius = 8; // Arbitrary scan radius
                                                for (int x = Math.Max(0, ruptureX - scanRadius); x <= Math.Min(Width - 1, ruptureX + scanRadius); x++)
                                                {
                                                    for (int z = Math.Max(0, ruptureZ - scanRadius); z <= Math.Min(Length - 1, ruptureZ + scanRadius); z++)
                                                    {
                                                        if (CalculateDistance(ruptureX, ruptureZ, x, z) > 3 && CalculateDistance(ruptureX, ruptureZ, x, z) <= scanRadius)
                                                        {
                                                            Location nearbyLocation = WorldMap[x + z * Width].MyLocation;
                                                            if (nearbyLocation != null)
                                                            {
                                                                foreach (District district in nearbyLocation.Districts)
                                                                {
                                                                    foreach (Architect architect in district.Architects)
                                                                    {
                                                                        if (r.Next(GrievanceChance) == 1 && architect != Calamitizer)
                                                                        {
                                                                            architect.Grievances.Add((Calamitizer, " caused a rupture near " + architect.Name + "'s district."));
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // No valid region found, skip rupture triggering
                                                continue;
                                            }
                                        }
                                    }

                                    foreach (Architect a in ChosenDistrict.ArchitectsToRemove)
                                    {
                                        ChosenDistrict.Architects.Remove(a);

                                        if (a.Group != null)
                                        {
                                            a.Group.Architects.Remove(a);
                                            a.Group = null;
                                        }

                                        if (ChosenDistrict.Location.Government == a)
                                        {
                                            ChosenDistrict.Location.Government = null;
                                        }

                                        if (CalamityIdeologicalObsession == "kidnapper")
                                        {
                                            a.NextMigrationLocation = Calamitizer.HomeLocation;
                                            a.Bound = true;

                                            if (ChosenDistrict.Location.Government == a)
                                            {
                                                LogEvent(Calamitizer.Name + " kidnapped the ruler of " + ChosenDistrict.Location.Name + ", " + a.Name + ", causing a minor power struggle.");
                                            }
                                        }
                                    }
                                    ChosenDistrict.ArchitectsToRemove = new EntityList<Architect>();
                                }

                            }
                        }
                    }

                    foreach (Architect a in CalamitiesToAdd)
                    {
                        if(!a.OppositionTags.Contains("intruders"))
                        {
                            a.OppositionTags.Add("intruders");
                        }

                        Calamity.Add(a);
                    }
                }

                foreach (Civilization c in Civilizations)
                {
                    if (HumanoidRaces.Contains(c.PrimaryInhabiantRace))
                    {
                        c.CyclesTillElection -= 864000 * Days;
                        if (c.CyclesTillElection < 0 && c.Citizens.Count > 0)
                        {
                            // Filter candidates whose home location matches the civilization's capital
                            var eligibleCandidates = c.Citizens.Where(citizen => citizen.Location == c.Capitol).ToList();
                            if (eligibleCandidates.Count > 0)
                            {
                                var highestReputation = eligibleCandidates.Max(citizen => citizen.Reputation);
                                var candidates = eligibleCandidates.Where(citizen => citizen.Reputation == highestReputation).ToList();

                                Random rnd = new Random();
                                Architect selectedAlpha = candidates[rnd.Next(candidates.Count)];

                                Architect OldAlpha = c.Alpha;
                                if (OldAlpha != null)
                                {
                                    OldAlpha.Profession = "prestiged";
                                }
                                c.Alpha = selectedAlpha;

                                if (OldAlpha != selectedAlpha)
                                {
                                    if (OldAlpha != null)
                                    {
                                        HistoricalEvents.Add(Date + " " + OldAlpha.Name + " ended his service as the alpha of " + c.Name + ".");
                                    }
                                    HistoricalEvents.Add(Date + " The civilization of " + c.Name + " elected a new alpha, " + c.Alpha.Name + ".");
                                    c.Alpha.Profession = "alpha";
                                }

                                c.CyclesTillElection = c.ElectionFrequency;
                            }
                        }
                    }
                }


                //summon blight

                foreach (Blight b in Blights)
                {
                    if (!b.Spawned && Cycle > (double)((double)b.FoundingYear * (double)290304000))
                    {
                        while (!b.Spawned)
                        {
                            // 35% chance to try spawning at a spire
                            if (r.Next(1, 101) <= 35)
                            {
                                for (int x = 0; x < Width; x++)
                                {
                                    for (int z = 0; z < Length; z++)
                                    {
                                        // Check for spire and suitable conditions
                                        if (WorldMap[x + z * Width].Biome != "void" &&
                                            WorldMap[x + z * Width].Biome != "ocean" &&
                                            WorldMap[x + z * Width].MyLocation != null &&
                                            (WorldMap[x + z * Width].MyLocation.Type == "spire" && WorldMap[x + z * Width].MyLocation.Government != null))
                                        {
                                            b.Spawned = true;
                                            WorldMap[x + z * Width].Blight = b;

                                            if (((Architect)(WorldMap[x + z * Width].MyLocation.Government)).Profession == "sorcerer")
                                            {
                                                HistoricalEvents.Add(String.Concat(Date, " ", WorldMap[x + z * Width].MyLocation.Government.Name, ", sorcerer of ", LightDeity.Name, " \"blessed\" the land with the " + b.Name + " from ", WorldMap[x + z * Width].MyLocation.Name, "."));
                                            }
                                            else
                                            {
                                                HistoricalEvents.Add(String.Concat(Date, " ", WorldMap[x + z * Width].MyLocation.Government.Name, ", warlock of ", DarkDeity.Name, " \"cursed\" the land with the " + b.Name + " from ", WorldMap[x + z * Width].MyLocation.Name, "."));
                                            }
                                            break;
                                        }
                                    }
                                    if (b.Spawned) break;
                                }
                            }

                            // If not spawned at a spire, default to random location
                            if (!b.Spawned)
                            {
                                int SpawnTryX = r.Next(Width);
                                int SpawnTryZ = r.Next(Length);

                                // Existing logic for spawning at a random location
                                if (WorldMap[SpawnTryX + SpawnTryZ * Width].Biome != "void" &&
                                    WorldMap[SpawnTryX + SpawnTryZ * Width].Biome != "ocean"
                                    ) // Higher spawn chance at spires
                                {
                                    b.Spawned = true;
                                    WorldMap[SpawnTryX + SpawnTryZ * Width].Blight = b;

                                    //keep history logic just in case it just so happens to start there.

                                    if (WorldMap[SpawnTryX + SpawnTryZ * Width].MyLocation != null)
                                    {
                                        if (WorldMap[SpawnTryX + SpawnTryZ * Width].MyLocation.Type == "spire")
                                        {
                                            if (((Architect)(WorldMap[SpawnTryX + SpawnTryZ * Width].MyLocation.Government)).Profession == "sorcerer")
                                            {
                                                HistoricalEvents.Add(String.Concat(Date, " ", WorldMap[SpawnTryX + SpawnTryZ * Width].MyLocation.Government.Name, ", sorcerer of ", LightDeity.Name, " \"blessed\" the land with the " + b.Name + " from ", WorldMap[SpawnTryX + SpawnTryZ * Width].MyLocation, "."));
                                            }
                                            else
                                            {
                                                HistoricalEvents.Add(String.Concat(Date, " ", WorldMap[SpawnTryX + SpawnTryZ * Width].MyLocation.Government.Name, ", warlock of ", DarkDeity.Name, " \"cursed\" the land with the " + b.Name + " from ", WorldMap[SpawnTryX + SpawnTryZ * Width].MyLocation, "."));
                                            }
                                        }
                                        else
                                        {
                                            HistoricalEvents.Add(String.Concat(Date, " Something began to spread in ", WorldMap[SpawnTryX + SpawnTryZ * Width].MyLocation.Name, ", corrupting all it saw fit. The locals named the blight ", b.Name, "."));
                                        }
                                    }
                                    else
                                    {
                                        HistoricalEvents.Add(String.Concat(Date, " A terrible force of nature brought about the blight of ", b.Name, "."));
                                    }
                                }
                            }
                        }
                    }
                }



                //spread blight
                foreach (Region R in WorldMap)
                {
                    if (R.Blight != Purity)
                    {
                        if (r.Next(1, 600 * MonthToDayConstant) == 1)
                        {
                            //pick a random number between 1-4 and spread cardinally
                            int Spread = r.Next(4);
                            if (Spread == 1)
                            {
                                //spread north if available
                                if (R.X > 0)
                                {
                                    WorldMap[(R.X) + (R.Z - 1) * Width].Blight = WorldMap[R.X + R.Z * Width].Blight;
                                }
                            }
                            else if (Spread == 2)
                            {
                                //spread east if available
                                if (R.X < Width - 1)
                                {
                                    WorldMap[(R.X + 1) + (R.Z) * Width].Blight = WorldMap[R.X + R.Z * Width].Blight;
                                }
                            }
                            else if (Spread == 3)
                            {
                                //spread south if available
                                if (R.Z < Length - 1)
                                {
                                    WorldMap[(R.X) + (R.Z + 1) * Width].Blight = WorldMap[R.X + R.Z * Width].Blight;
                                }
                            }
                            else
                            {
                                //spread west if available
                                if (R.X > 0)
                                {
                                    WorldMap[(R.X - 1) + (R.Z) * Width].Blight = WorldMap[R.X + R.Z * Width].Blight;
                                }
                            }
                        }
                    }
                }

                //try to merge a group together

                // 2% chance to pick a group type
                if (Game1.r.Next(1, 50 * MonthToDayConstant) == 1)
                {
                    // Pick a random group type from the predefined list
                    var selectedGroupType = groupTypes.Keys.ElementAt(Game1.r.Next(groupTypes.Count));

                    // Find all groups of the selected type
                    var groupsOfType = Groups
                        .Where(g => g.Type == selectedGroupType)
                        .GroupBy(g => g.Leader.Location)
                        .Where(g => g.Count() > 1)
                        .SelectMany(g => g)
                        .ToList();

                    // Randomly shuffle the groups to ensure randomness
                    groupsOfType = groupsOfType.OrderBy(g => Game1.r.Next()).ToList();

                    // Attempt to merge the first pair found sharing the same location
                    for (int i = 0; i < groupsOfType.Count() - 1; i++)
                    {
                        var g1 = groupsOfType[i];
                        var g2 = groupsOfType[i + 1];

                        if (g1.Leader.Location == g2.Leader.Location)
                        {
                            // Merge groups g1 and g2
                            HistoricalEvents.Add($"{Date} {g1.Name} and {g2.Name} started talking about merging their groups.");

                            Group mergedGroup = new Group(new EntityList<Architect>(), g1.Type, g2.Leader, g1.Leader.Location);
                            EntityList<Architect> joiners = new EntityList<Architect>();
                            EntityList<Architect> leavers = new EntityList<Architect>();

                            IEnumerable<Architect> allArchitects = g1.Architects.Union(g2.Architects);

                            foreach (Architect architect in allArchitects)
                            {
                                if (Game1.r.Next(1, 10) == 1 && architect != mergedGroup.Leader)
                                {
                                    HistoricalEvents.Add($"{Date} {architect.Name} disagreed with the idea of merging groups and left them both to settle it themselves.");
                                    leavers.Add(architect);
                                }
                                else
                                {
                                    joiners.Add(architect);
                                }
                            }

                            foreach (Architect architect in joiners)
                            {
                                architect.Group = mergedGroup;
                                mergedGroup.Architects.Add(architect);
                            }

                            foreach (Architect architect in leavers)
                            {
                                architect.Group = null;
                                g1.Leader.Location.Districts[0].DistrictMap[Game1.r.Next(0, 49)].Architects.Add(architect);
                            }

                            mergedGroup.Reputation = (g1.Reputation + g2.Reputation) / 2;

                            HistoricalEvents.Add($"{Date} {g1.Name} and {g2.Name} {groupTypes[g1.Type]}, going under the name {mergedGroup.Name}.");

                            Groups.Remove(g1);
                            Groups.Remove(g2);
                            Groups.Add(mergedGroup);
                            g1.Leader.Location.GroupsAtThisLocation.Remove(g1);
                            g1.Leader.Location.GroupsAtThisLocation.Remove(g2);
                            g1.Leader.Location.GroupsAtThisLocation.Add(mergedGroup);

                            break;
                        }
                    }
                }

                //age groups

                foreach (Group g in Groups)
                {
                    g.DaysOld += Days;

                    if(g.WaitingForCooldownToTrade == true)
                    {
                        if(r.Next(1,10) == 1)
                        {
                            g.WaitingForCooldownToTrade = false;
                        }
                    }
                }

                void SummonNewUnit(Location l)
                {
                    // Find fit men/maybe women
                    EntityList<Architect> allArchitects = new EntityList<Architect>();

                    // Iterate over all districts and add their architects to the new list
                    foreach (var district in l.Districts)
                    {
                        allArchitects.AddRange(district.Architects);
                    }

                    // Sort the architects by Strength * Dexterity * Agility
                    allArchitects = allArchitects
                        .OrderByDescending(a => a.Strength * a.Dexterity * a.Agility);


                    // Decide a size in both regulars and normals
                    int totalSoldiers = (l.TruePopulation() / 100) + 4;
                    int importantSoldiers = (int)(totalSoldiers * 0.4);

                    EntityList<Architect> selectedArchitects = new EntityList<Architect>();
                    Random rand = new Random();


                    // Iterate through the sorted list of architects and select important people
                    foreach (var architect in allArchitects)
                    {
                        if (selectedArchitects.Count >= importantSoldiers)
                        {
                            break;
                        }

                        if (architect.Sex == "male" && rand.Next(2) == 0)
                        {
                            selectedArchitects.Add(architect);
                        }
                        else if (architect.Sex == "female" && rand.Next(5) == 0)
                        {
                            selectedArchitects.Add(architect);
                        }
                    }

                    if (selectedArchitects.Count > 0)
                    {
                        Unit u = new Unit(selectedArchitects[0], selectedArchitects, totalSoldiers - importantSoldiers, l);

                        l.Units.Add(u);

                        HistoricalEvents.Add(string.Concat(Date + " A new squad called " + u.Name + ", led by " + u.Leader.Name + ", was forged in " + l.Name + "."));
                        l.LocationHistoricalEvents.Add(string.Concat(Date + " A new squad called " + u.Name + ", led by " + u.Leader.Name + ", was forged in " + l.Name + "."));

                        foreach (Architect a in u.Architects)
                        {
                            HistoricalEvents.Add(string.Concat(Date + " " + a.Name + " joined " + u.Name + "."));
                            l.LocationHistoricalEvents.Add(string.Concat(Date + " " + a.Name + " joined " + u.Name + "."));
                        }
                    }
                }





                //in peace


                foreach (Location l in AllLocations)
                {
                    //develop squads slowly
                    if(SettlementTypes.Contains(l.Type))
                    {
                        if (r.Next(1, 10000 * MonthToDayConstant) < (l.TruePopulation() - (l.Units.Count * 250)))
                        {
                            SummonNewUnit(l);
                        }
                    }
                }

                //declare WAR

                Location GetCapitol(Civilization civilization, EntityList<Location> allLocations)
                {
                    return allLocations.FirstOrDefault(l => l.IsCapitol && l.HomeCivilization.Type == civilization.Type);
                }

                foreach (Civilization c in Civilizations)
                {
                    string PrimaryHaterType = GenericHatredDictionary[c.WarType];
                    Location currentCapitol = GetCapitol(c, AllLocations);

                    if (currentCapitol == null) continue;

                    foreach (Civilization hatedCivilization in Civilizations)
                    {
                        if (hatedCivilization.WarType == PrimaryHaterType)
                        {
                            Location hatedCapitol = GetCapitol(hatedCivilization, AllLocations);

                            if (hatedCapitol == null) continue;

                            double distance = Vector2.Distance(
                                new Vector2(currentCapitol.Region.X, currentCapitol.Region.Z),
                                new Vector2(hatedCapitol.Region.X, hatedCapitol.Region.Z)
                            );

                            if (distance < 20)
                            {
                                // Increment hatred points based on distance
                                int hatredPoints = distance < 5 ? 5 : (distance < 10 ? 4 : (distance < 15 ? 3 : 2));
                                int initialHatredPoints = c.HatredPoints.ContainsKey(hatedCivilization.Type) ? c.HatredPoints[hatedCivilization.Type] : 0;

                                if (c.HatredPoints.ContainsKey(hatedCivilization.Type))
                                {
                                    c.HatredPoints[hatedCivilization.Type] += hatredPoints;
                                }
                                else
                                {
                                    c.HatredPoints[hatedCivilization.Type] = hatredPoints;
                                }

                                // Check for historical events and war declarations
                                int totalHatredPoints = c.HatredPoints[hatedCivilization.Type];

                                if (initialHatredPoints < 250 && totalHatredPoints >= 250)
                                {
                                    HistoricalEvents.Add(String.Concat(Date, " ", c.Name, ", a ", c.Type, " society, and ", hatedCivilization.Name, ", a ", hatedCivilization.Type, " society, slowly grew suspicious of each other."));
                                }
                                else if (initialHatredPoints < 500 && totalHatredPoints >= 500)
                                {
                                    HistoricalEvents.Add(String.Concat(Date, " Hostility between ", c.Name, ", a ", c.Type, " society, and ", hatedCivilization.Name, ", a ", hatedCivilization.Type, " society, started to increase."));
                                }
                                else if (initialHatredPoints < 750 && totalHatredPoints >= 750)
                                {
                                    HistoricalEvents.Add(String.Concat(Date, " Tensions between ", c.Name, ", a ", c.Type, " society, and ", hatedCivilization.Name, ", a ", hatedCivilization.Type, " society, rose very highly."));
                                }
                                else if (initialHatredPoints < 1000 && totalHatredPoints >= 1000)
                                {
                                    HistoricalEvents.Add(String.Concat(Date, " ", c.Name, ", a ", c.Type, " society, declared war on ", hatedCivilization.Name, ", a ", hatedCivilization.Type, " society!"));
                                }

                            }
                        }
                    }
                }



                //in war

                foreach ((Civilization, Civilization, int, int) War in Wars)
                {
                    // procure new squads
                    EntityList<Location> Civ1LocationsThatHaveMilitary = new EntityList<Location>();
                    EntityList<Location> Civ2LocationsThatHaveMilitary = new EntityList<Location>();

                    foreach (Location l in AllLocations)
                    {
                        if (l.HomeCivilization == War.Item1 || l.HomeCivilization == War.Item2)
                        {
                            if (r.Next(1, 1000 * MonthToDayConstant) < (l.TruePopulation() - (l.Units.Count * 250)))
                            {
                                SummonNewUnit(l);
                            }
                        }

                        if (l.HomeCivilization == War.Item1 && l.Units.Count > 0)
                        {
                            Civ1LocationsThatHaveMilitary.Add(l);
                        }
                        else if (l.HomeCivilization == War.Item2 && l.Units.Count > 0)
                        {
                            Civ2LocationsThatHaveMilitary.Add(l);
                        }
                    }

                    // then FIGHT
                    foreach (Location L1 in Civ1LocationsThatHaveMilitary)
                    {
                        if (r.Next(1, 5) == 1)
                        {
                            // we are using this one
                            foreach (Location L2 in Civ2LocationsThatHaveMilitary)
                            {
                                if (L1.Units.Count == 0 || L2.Units.Count == 0)
                                {
                                    // this means they lost them in another fight and can no longer fight.
                                    break;
                                }

                                if (r.Next(1, 5) == 1)
                                {
                                    // ok so this is a fight
                                    // first, decide units
                                    Unit unit1 = L1.Units[r.Next(L1.Units.Count)];
                                    Unit unit2 = L2.Units[r.Next(L2.Units.Count)];

                                    // find a location
                                    Vector2 BattleCenter = new Vector2(
                                        (float)Math.Round((L1.Region.X + L2.Region.X) / 2.0) + 3,
                                        (float)Math.Round((L1.Region.Z + L2.Region.Z) / 2.0) - 3
                                    );

                                    Region BattleRegion = WorldMap[(int)(BattleCenter.X + BattleCenter.Y * Width)];

                                    List<string> Data = unit1.Fight(unit2, BattleRegion);

                                    foreach (string s in Data)
                                    {
                                        HistoricalEvents.Add(Date + " " + s);
                                    }

                                    // Check if any units should disband
                                    if (unit1.Architects.Count == 0 && unit1.OtherSoldiers == 0)
                                    {
                                        L1.Units.Remove(unit1);
                                    }

                                    if (unit2.Architects.Count == 0 && unit2.OtherSoldiers == 0)
                                    {
                                        L2.Units.Remove(unit2);
                                    }
                                }
                            }
                        }
                    }
                }


                //age

                foreach(Architect V in AllArchitects)
                {
                    if (V.Age > V.TerminalAge && !V.IsImmortal && V.IsAlive)
                    {
                        V.IsAlive = false;

                        if (V.DoIDieOfOldAge)
                        {
                            if (V.Location != null)
                            {
                                HistoricalEvents.Add(string.Concat(Date, " ", V.Name, " died of old age at ", V.Age, " in ", V.Location.Name, "."));
                            }
                            else
                            {
                                HistoricalEvents.Add(string.Concat(Date, " ", V.Name, " died of old age at ", V.Age, "."));
                            }
                        }
                        else
                        {
                            if (V.Location != null)
                            {
                                HistoricalEvents.Add(string.Concat(Date, " ", V.Name, " ", Game1.DeathCauses[Game1.r.Next(Game1.DeathCauses.Count)], " at ", V.Age, " in ", V.Location.Name, "."));
                            }
                            else
                            {
                                HistoricalEvents.Add(string.Concat(Date, " ", V.Name, " ", Game1.DeathCauses[Game1.r.Next(Game1.DeathCauses.Count)], " at ", V.Age, "."));
                            }
                        }


                        if (V.Group != null)
                        {
                            V.Group.ArchitectsToRemove.Add(V);
                        }

                        if(V.District != null)
                        {
                            V.District.Architects.Remove(V);
                        }
                    }
                }
                



                // Loop through the world map and update locations

                foreach (Location location in AllLocations)
                {
                    if(location.Government is Architect z && z.IsAlive == false)
                    {
                        location.Government = null;
                    }

                    // Efficiently add traders to this location
                    location.TradersAtThisLocation.AddRange(location.TradersAtThisLocationToAdd);
                    location.TradersAtThisLocationToAdd.Clear();

                    int WealthIncrease = 50; // Assuming this is used later in your code

                    // Only proceed if location is core or garrison, has enough wealth, and a rare condition is met
                    if ((location.Type == "core" || location.Type == "garrison") && location.Wealth > 10000 && r.Next(1, 500) == 1)
                    {
                        // Simplify direction decision using a more direct approach
                        int direction = r.Next(4); // 0: Right, 1: Left, 2: Up, 3: Down

                        // Calculate new position based on the direction
                        int NewX = location.X + (direction == 0 ? 1 : direction == 1 ? -1 : 0);
                        int NewZ = location.Z + (direction == 2 ? 1 : direction == 3 ? -1 : 0);

                        // Check if the new position is within bounds and not a "void" biome
                        bool isValidPosition = NewX > 0 && NewX < Width && NewZ > 0 && NewZ < Length;
                        bool isNotVoid = isValidPosition && WorldMap[NewX + NewZ * Width].MyLocation == null && WorldMap[NewX + NewZ * Width].Biome != "void";

                        if (isValidPosition && isNotVoid)
                        {
                            // Create and add the new location if conditions are met
                            LocationBuilderPacket l = new LocationBuilderPacket(
                                location.Government, NewX, NewZ, "garrison", location.PrimaryRace,
                                r.Next(5, 10), 0, location.HomeCivilization, new EntityList<Object>(), location, "none"
                            );
                            LocationBuilderPackets.Add(l);
                            location.Wealth -= 10000; // Deduct wealth as the location is being expanded
                        }
                    }

                    if (SettlementTypes.Contains(location.Type))
                    {
                        foreach (District d in location.Districts)
                        {
                            for (int DistrictX = 0; DistrictX < 7; DistrictX++)
                            {
                                for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                                {
                                    foreach (Structure s in d.DistrictMap[DistrictX + DistrictZ * 7].Structures)
                                    {
                                        if (s.Type == "forge")
                                        {
                                            WealthIncrease += 1;
                                        }
                                        else if (s.Type == "market")
                                        {
                                            WealthIncrease += 2;
                                        }
                                    }
                                }
                            }

                            //establish an industry and do industry things :O

                            if (d.Industry == null && (location.Type == "village" || location.Type == "town" || location.Type == "city"))
                            {
                                d.Industry = Game1.Industries[r.Next(Game1.Industries.Count)];
                                HistoricalEvents.Add(string.Concat(d.Name, " in ", location.Name, " dedicated themselves to the industry of ", d.Industry, "."));
                                location.LocationHistoricalEvents.Add(string.Concat(d.Name, " in ", location.Name, " dedicated themselves to the industry of ", d.Industry, "."));
                            }

                            //foreach district with an industry, use it to increase the capitol's wealth bwzhaaahahahahahahahahahaaaa just kidding its actually the location that theyre based around

                            if (d.Industry != null && r.Next(1, 20) == 1)
                            {
                                d.SupplyLocation(1);
                            }
                        }
                    }

                    location.Wealth = location.Wealth + (int)Math.Round(WealthIncrease * ProsperityMultiplier);


                    //create interactable events based on history


                    int IEDecider = r.Next(1, 500 * MonthToDayConstant);

                    int LX = location.X + r.Next(-5, 6);
                    int LZ = location.Z + r.Next(-5, 6);

                    if (LX >= 0 && LX < Width && LZ >= 0 && LZ < Width && WorldMap[LX + LZ * Width].MyLocation == null && WorldMap[LX + LZ * Width].Biome != "void" && WorldMap[LX + LZ * Width].Biome != "ocean" && new string[] { "town", "city", "camp", "village" }.Contains(location.Type))
                    {
                        string DecidedType = "";

                        EntityList<Architect> GuarranteedArch = new EntityList<Architect>();

                        switch (IEDecider)
                        {
                            case int decider when decider < 2:
                                DecidedType = "bandits";
                                for (int Arch = r.Next(4, 8); Arch != 0; Arch--)
                                {
                                    Architect AA = new Architect("", Game1.Sexes[r.Next(2)], location.HomeCivilization.PrimaryInhabiantRace, r.Next(13, 39), "bandit", new EntityList<Object>(), null, null, null, "", 3);
                                    AA.KitOutArchitect("bandit");
                                    AA.Name = Game1.GameWorld.GenerateUniqueArchitectName(AA);
                                    AA.OppositionTags.Add("intruders");
                                    GuarranteedArch.Add(AA);
                                }
                                break;
                            case int decider when decider < 3:
                                DecidedType = "shadebeast";
                                Architect SB = new Architect("", Game1.Sexes[r.Next(2)], GetRace("shadebeast"), r.Next(Year), "shadebeast", new EntityList<Object>(), null, null, null, "", 3);
                                SB.Name = Game1.GameWorld.GenerateUniqueArchitectName(SB);
                                GuarranteedArch.Add(SB);
                                break;
                            case int decider when decider < 4:
                                DecidedType = "construct";
                                Architect CN = new Architect("", Game1.Sexes[r.Next(2)], ConstructRaces[r.Next(ConstructRaces.Count)], r.Next(Year), "construct", new EntityList<Object>(), null, null, null, "", 3);
                                CN.Name = Game1.GameWorld.GenerateUniqueArchitectName(CN);
                                GuarranteedArch.Add(CN);
                                break;
                            case int decider when decider < 5:
                                DecidedType = "wildcreatures";
                                Race DecidedRace = WildRaces[r.Next(WildRaces.Count)];

                                for (int Arch = r.Next(4, 8); Arch != 0; Arch--)
                                {
                                    Architect AA = new Architect("", Game1.Sexes[r.Next(2)], DecidedRace, r.Next(13, 39), "beast", new EntityList<Object>(), null, null, null, "", 2);
                                    AA.Name = Game1.GameWorld.GenerateUniqueArchitectName(AA);
                                    GuarranteedArch.Add(AA);
                                }
                                break;
                            case int decider when decider < 6 && TradingGroups.Count > 0:
                                DecidedType = "traders";
                                Group TradingGroup = TradingGroups[r.Next(TradingGroups.Count)];
                                GuarranteedArch = TradingGroup.Architects; //updates if the group updates\
                                break;
                            case int decider when decider < 7:
                                DecidedType = "vagabond";
                                Architect VB = new Architect("", Game1.Sexes[r.Next(2)], HumanoidRaces[r.Next(HumanoidRaces.Count)], r.Next(13, 39), "vagabond", new EntityList<Object>(), null, null, null, "", 3);
                                VB.KitOutArchitect("vagabond");
                                VB.Name = Game1.GameWorld.GenerateUniqueArchitectName(VB);
                                GuarranteedArch.Add(VB);
                                break;
                            case int decider when decider < 8:
                                DecidedType = "shiba";
                                Architect SI = new Architect("", Game1.Sexes[r.Next(2)], GetRace("shiba"), r.Next(13, 39), "shiba", new EntityList<Object>(), null, null, null, "", 3);
                                SI.Name = Game1.GameWorld.GenerateUniqueArchitectName(SI);
                                GuarranteedArch.Add(SI);
                                break;
                            case int decider when decider < 9:
                                DecidedType = "adventurer";
                                Architect AD = new Architect("", Game1.Sexes[r.Next(2)], HumanoidRaces[r.Next(HumanoidRaces.Count)], r.Next(13, 39), "adventurer", new EntityList<Object>(), null, null, null, "", 3);
                                AD.KitOutArchitect("adventurer");
                                AD.Name = Game1.GameWorld.GenerateUniqueArchitectName(AD);
                                GuarranteedArch.Add(AD);
                                break;
                            case int decider when decider < 10:
                                DecidedType = "priest";
                                Architect PR = new Architect("", Game1.Sexes[r.Next(2)], HumanoidRaces[r.Next(HumanoidRaces.Count)], r.Next(13, 39), "priest", new EntityList<Object>(), null, null, null, "", 1);
                                PR.KitOutArchitect("priest");
                                PR.Name = Game1.GameWorld.GenerateUniqueArchitectName(PR);
                                GuarranteedArch.Add(PR);
                                break;
                        }


                        if (DecidedType != "")
                        {
                            WorldMap[LX + LZ * Width].Events.Add(new InteractableEvent(WorldMap[LX + LZ * Width], r.Next(28, 54), DecidedType, location.HomeCivilization, GuarranteedArch));
                        }
                    }

                    // Traders do stuff

                    foreach (Group g in location.TradersAtThisLocation)
                    {
                        if (location.Market != null)
                        {
                            double currentCycle = this.Cycle;

                            // Check if it's time for the group to move to the next location
                            double timeSinceLastMoved = currentCycle - g.CycleLastMoved;
                            if (timeSinceLastMoved >= r.Next(21600000, 30240000))
                            {
                                // Move to the next location in the trade route
                                if (g.TradeRoute.Count > 1)
                                {
                                    int travelIndex = (g.TradeRoute.IndexOf(location) + 1) % g.TradeRoute.Count;

                                    location.TradersAtThisLocationToRemove.Add(g);
                                    g.TradeRoute[travelIndex].TradersAtThisLocation.Add(g);

                                    g.Architects.ForEach(A => A.NextMigrationLocation = g.TradeRoute[travelIndex]);

                                    g.CycleLastMoved = currentCycle;
                                }
                            }

                            // Check if it's time for the group to trade at the current location
                            double timeSinceLastTraded = currentCycle - g.CycleLastTraded;
                            if (timeSinceLastTraded >= r.Next(12960000, 17280000))
                            {
                                // Trade at the current location
                                int tradeCount = r.Next(20, 40);

                                var availableCaravanItems = g.CaravanItems;
                                var availableLocationItems = location.Market.Block.District.GeneralItemsWeHave;

                                if (availableLocationItems.Count >= 5)
                                {
                                    var tradeItems = Enumerable.Range(0, tradeCount)
                                        .Select(_ => new
                                        {
                                            CaravanItem = availableCaravanItems[r.Next(availableCaravanItems.Count)],
                                            LocationItem = availableLocationItems[r.Next(availableLocationItems.Count)]
                                        })
                                        .ToList();

                                    tradeItems.ForEach(tradeItem =>
                                    {
                                        availableCaravanItems.Remove(tradeItem.CaravanItem);
                                        availableLocationItems.Add(tradeItem.CaravanItem);

                                        availableLocationItems.Remove(tradeItem.LocationItem);
                                        availableCaravanItems.Add(tradeItem.LocationItem);

                                        var caravanItemType = tradeItem.CaravanItem.Split(',')[0];
                                        var locationItemType = tradeItem.LocationItem.Split(',')[0];

                                        if (!ItemTypesInCirculation.Contains(caravanItemType))
                                        {
                                            ItemTypesInCirculation.Add(caravanItemType);
                                        }
                                        if (!ItemTypesInCirculation.Contains(locationItemType))
                                        {
                                            ItemTypesInCirculation.Add(locationItemType);
                                        }
                                    });

                                    g.CycleLastTraded = currentCycle;

                                    double newCycle = Cycle + (Days * 864000);
                                    double currentYear = (int)Math.Round((decimal)(currentCycle / 290304000));
                                    double newYear = (int)Math.Round((decimal)(newCycle / 290304000));

                                    if ((newYear / 10) > (currentYear / 10))
                                    {
                                        g.WaitingForCooldownToTrade = true;

                                        if (r.Next(1, 3) == 1 && g.TradeRoute.Count <= g.MaxTradeRouteLength)
                                        {
                                            var newTradeLocations = AllLocations
                                                .Where(l => Vector2.Distance(new Vector2(l.X, l.Z), new Vector2(location.X, location.Z)) < 20 &&
                                                            !g.TradeRoute.Contains(l) &&
                                                            SettlementTypes.Contains(l.Type))
                                                .ToList();

                                            if (newTradeLocations.Any())
                                            {
                                                var newTradeLocation = newTradeLocations[r.Next(newTradeLocations.Count)];
                                                HistoricalEvents.Add($"{Date} {g.Name} added {newTradeLocation.Name} to their list of trading partners.");
                                                g.TradeRoute.Add(newTradeLocation);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    foreach (Group g in location.TradersAtThisLocationToRemove)
                    {
                        location.TradersAtThisLocation.Remove(g);
                    }
                    location.TradersAtThisLocationToRemove = new EntityList<Group>();


                    if (!AllPortsBuilt && r.Next(500 * MonthToDayConstant) == 0 && TradingGroups.Count > 0)
                    {
                        // Identify the biggest island
                        EntityList<Region> biggestIsland = Islands.OrderByDescending(island => island.Count).FirstOrDefault();

                        // Initialize a counter for ports on the biggest island based on current port conditions
                        int biggestIslandIndex = Islands.IndexOf(biggestIsland);
                        int portsOnBiggestIsland = PortLocations[biggestIslandIndex].Count(port => !string.IsNullOrEmpty(port.PortName));

                        bool portBuilt = false;

                        for (int i = 0; i < Islands.Count; i++)
                        {
                            var island = Islands[i];
                            var potentialPorts = PotentialPorts[i];
                            bool islandHasPort = PortLocations[i].Any(port => !string.IsNullOrEmpty(port.PortName));

                            if (island == biggestIsland && portsOnBiggestIsland < ContinentalPortMaximum)
                            {
                                // For the biggest island, allow adding ports until it reaches ContinentalPortMaximum
                                var availablePorts = potentialPorts.Where(port => string.IsNullOrEmpty(port.PortName)).ToList();
                                if (availablePorts.Any())
                                {
                                    var portLocation = availablePorts[r.Next(availablePorts.Count)];
                                    portLocation.PortName = GenerateUniqueName("1S7s", portLocation);

                                    HistoricalEvents.Add($"{Date} A new port named {portLocation.PortName} was built by {TradingGroups[r.Next(TradingGroups.Count)].Name} to facilitate trade.");

                                    portBuilt = true;
                                    portsOnBiggestIsland++; // Update the counter for ports on the biggest island
                                    potentialPorts.Remove(portLocation); // Remove the port location from potential ports
                                }
                            }
                            else if (!islandHasPort)
                            {
                                // For other islands, build only one port if there isn't already one
                                var availablePorts = potentialPorts.Where(port => string.IsNullOrEmpty(port.PortName)).ToList();
                                if (availablePorts.Any())
                                {
                                    var portLocation = availablePorts[r.Next(availablePorts.Count)];
                                    portLocation.PortName = GenerateUniqueName("1S7s", portLocation);

                                    if (TradingGroups.Count > 0)
                                    {
                                        HistoricalEvents.Add($"{Date} A new port named {portLocation.PortName} was built by {TradingGroups[r.Next(TradingGroups.Count)].Name} to facilitate trade.");
                                    }
                                    else
                                    {
                                        HistoricalEvents.Add($"{Date} A new port named {portLocation.PortName} was built to facilitate trade.");
                                    }

                                    portBuilt = true;
                                    potentialPorts.Remove(portLocation); // Remove the port location from potential ports
                                }
                            }

                            if (portBuilt) break; // Exit the loop once a port is built
                        }

                        // Check if all ports are built
                        CheckAllPortsBuilt(ContinentalPortMaximum);
                    }



                    //Handle architect/architect group actions regardless of district
                    //this is the meat of the history

                    EntityList<Architect> ArchitectsAtLocation = new EntityList<Architect>();
                    foreach (District d in location.Districts)
                    {
                        ArchitectsAtLocation.AddRange(d.Architects);
                    }


                    //other actions favorable or not, based on allignment
                    //gather "forces" in the location. each "force" is assigned a power, values, and name which demonstrates its ability to act based on resources and such and tells you who did it. 

                    List<Force> Forces = new List<Force>();

                    foreach (Architect a in ArchitectsAtLocation)
                    {
                        //salary

                        a.Wealth += r.Next(0, 3);

                        if (a.Group == null || r.Next(1, 3) == 1) //architects are less likely to act by themselves if they have friends they might act with, but they still can :)
                        {
                            Forces.Add(new Force(a.Name, a.Profession, 1, a.MoralCompass, a.StabilityCompass, a.PropertyValue, a.FamilyValue, a.PowerValue, a.MoneyValue, a.KnowledgeValue, a.SpiritualityValue, a.ProwessValue, a.PatriotismValue, a.CourageValue, a.CreativityValue, a));
                        }
                    }
                    foreach (Group g in location.GroupsAtThisLocation)
                    {
                        //salary

                        g.Wealth += g.Architects.Count * r.Next(0, 4);

                        if (g.Architects.Count > 0)
                        {
                            //set group values
                            {
                                // Initialize accumulators for each value for non-leaders
                                int totalMoralCompass = 0, totalStabilityCompass = 0, totalPropertyValue = 0,
                                    totalFamilyValue = 0, totalPowerValue = 0, totalMoneyValue = 0,
                                    totalKnowledgeValue = 0, totalSpiritualityValue = 0, totalProwessValue = 0,
                                    totalPatriotismValue = 0, totalCourageValue = 0;

                                int nonLeaderCount = g.Architects.Count(a => (g.Leader != a)); // Assuming there's always one leader

                                foreach (var architect in g.Architects)
                                {
                                    // Skip the leader in this loop
                                    if (g.Leader == architect) continue;

                                    // Accumulate values for non-leaders
                                    totalMoralCompass += architect.MoralCompass;
                                    totalStabilityCompass += architect.StabilityCompass;
                                    totalPropertyValue += architect.PropertyValue;
                                    totalFamilyValue += architect.FamilyValue;
                                    totalPowerValue += architect.PowerValue;
                                    totalMoneyValue += architect.MoneyValue;
                                    totalKnowledgeValue += architect.KnowledgeValue;
                                    totalSpiritualityValue += architect.SpiritualityValue;
                                    totalProwessValue += architect.ProwessValue;
                                    totalPatriotismValue += architect.PatriotismValue;
                                    totalCourageValue += architect.CourageValue;
                                }

                                // Find the leader (assuming there's exactly one leader)
                                var leader = g.Architects.FirstOrDefault(a => (g.Leader != a));

                                // Calculate average for non-leaders, ensure no division by zero
                                double avgMoralCompass = nonLeaderCount > 0 ? (double)totalMoralCompass / nonLeaderCount : 0;
                                double avgStabilityCompass = nonLeaderCount > 0 ? (double)totalStabilityCompass / nonLeaderCount : 0;
                                double avgPropertyValue = nonLeaderCount > 0 ? (double)totalPropertyValue / nonLeaderCount : 0;
                                double avgFamilyValue = nonLeaderCount > 0 ? (double)totalFamilyValue / nonLeaderCount : 0;
                                double avgPowerValue = nonLeaderCount > 0 ? (double)totalPowerValue / nonLeaderCount : 0;
                                double avgMoneyValue = nonLeaderCount > 0 ? (double)totalMoneyValue / nonLeaderCount : 0;
                                double avgKnowledgeValue = nonLeaderCount > 0 ? (double)totalKnowledgeValue / nonLeaderCount : 0;
                                double avgSpiritualityValue = nonLeaderCount > 0 ? (double)totalSpiritualityValue / nonLeaderCount : 0;
                                double avgProwessValue = nonLeaderCount > 0 ? (double)totalProwessValue / nonLeaderCount : 0;
                                double avgPatriotismValue = nonLeaderCount > 0 ? (double)totalPatriotismValue / nonLeaderCount : 0;
                                double avgCourageValue = nonLeaderCount > 0 ? (double)totalCourageValue / nonLeaderCount : 0;

                                // Assuming leader is not null before accessing its properties
                                if (leader != null)
                                {
                                    // Calculate final group values, combining leader's values (50% weight) with non-leader averages
                                    g.MoralCompass = (int)((leader.MoralCompass + avgMoralCompass) / 2);
                                    g.StabilityCompass = (int)((leader.StabilityCompass + avgStabilityCompass) / 2);
                                    g.PropertyValue = (int)((leader.PropertyValue + avgPropertyValue) / 2);
                                    g.FamilyValue = (int)((leader.FamilyValue + avgFamilyValue) / 2);
                                    g.PowerValue = (int)((leader.PowerValue + avgPowerValue) / 2);
                                    g.MoneyValue = (int)((leader.MoneyValue + avgMoneyValue) / 2);
                                    g.KnowledgeValue = (int)((leader.KnowledgeValue + avgKnowledgeValue) / 2);
                                    g.SpiritualityValue = (int)((leader.SpiritualityValue + avgSpiritualityValue) / 2);
                                    g.ProwessValue = (int)((leader.ProwessValue + avgProwessValue) / 2);
                                    g.PatriotismValue = (int)((leader.PatriotismValue + avgPatriotismValue) / 2);
                                    g.CourageValue = (int)((leader.CourageValue + avgCourageValue) / 2);
                                }
                            }

                            Forces.Add(new Force(g.Name, g.Type + " group", g.Architects.Count, g.MoralCompass, g.StabilityCompass, g.PropertyValue, g.FamilyValue, g.PowerValue, g.MoneyValue, g.KnowledgeValue, g.SpiritualityValue, g.ProwessValue, g.PatriotismValue, g.CourageValue, g.CreativityValue, g));
                        }
                    }

                    //ok now we have all our forces lets do stuff with them

                    void LogForceAction(string Event)
                    {
                        HistoricalEvents.Add(Date + " " + Event);
                        location.LocationHistoricalEvents.Add(Date + " " + Event);
                    }

                    void ReputationChange(Force f, int Change)
                    {
                        if (f.Base is Architect)
                        {
                            ((Architect)f.Base).Reputation += Change;
                        }
                        else if (f.Base is Group)
                        {
                            ((Group)f.Base).Reputation += Change;
                        }
                        return;
                    }

                    foreach (Force f in Forces)
                    {
                        int AmbitionRank = 200; //decrease this to increase the likelihood of forces acting

                        if (ArchitectsAtLocation.Count > 0)
                        {
                            if (f.MoneyValue >= 3 && f.StabilityCompass < 40 && r.Next(AmbitionRank) == 1)
                            {
                                string industry = location.Districts[r.Next(location.Districts.Count)].Industry;
                                string profession = Game1.IndustryToProfession[industry];

                                if (industry == "military" || industry == "waspkeeping")
                                {
                                    industry += " supplies";
                                }

                                LogForceAction(f.Name + " stole " + industry + " from " + location.Name + ".");
                                ReputationChange(f, -3);
                                location.Wealth -= r.Next(50, 200) * f.Power;

                                foreach (District d in location.Districts)
                                {
                                    foreach (Architect a in d.Architects)
                                    {
                                        if (a.Profession == profession)
                                        {
                                            a.Grievances.Add((f.Base, "stole " + industry + ", affecting " + a.PossessivePronoun + " livelihood"));
                                        }
                                    }
                                }
                            }

                            else if (f.MoneyValue >= 4 && f.StabilityCompass < 40 && r.Next(AmbitionRank) == 1)
                            {
                                LogForceAction(f.Name + " set up means to embezzle extra funding from " + location.Name + "'s governmental structure.");
                                ReputationChange(f, -1);
                                location.Embezzlements.Add((f.Base, f.Power + r.Next(4, 8)));

                                if (location.Government is Group)
                                {
                                    foreach (Architect a in ((Group)location.Government).Architects)
                                    {
                                        a.Grievances.Add((f.Base, " embezzled funds from the government, undermining " + a.PossessivePronoun + " authority and trust"));
                                    }
                                }
                                else if (location.Government is Architect)
                                {
                                    ((Architect)location.Government).Grievances.Add((f.Base, " embezzled funds, undermining the governance"));
                                }
                            }

                            else if (f.MoneyValue >= 4 && f.KnowledgeValue >= 1 && f.StabilityCompass < 30 && r.Next(AmbitionRank * 5) == 1 && SettlementTypes.Contains(location.Type))
                            {
                                if (location.Prism.HistoricalObjects.Count > 1)
                                {
                                    Object o = location.Prism.HistoricalObjects[r.Next(location.Prism.HistoricalObjects.Count)];
                                    if (r.Next(f.Power) > r.Next(3, 20))
                                    {
                                        LogForceAction($"{f.Name} attempted to steal {o.Name} from the prism in {location.Name}. The plan succeeded.");
                                        ReputationChange(f, -10);
                                        if (f.Base is Group)
                                        {
                                            ((Group)f.Base).Leader.Inventory.Add(o);
                                            location.Prism.HistoricalObjects.Remove(o);
                                        }
                                        else
                                        {
                                            ((Architect)f.Base).Inventory.Add(o);
                                        }
                                    }
                                    else
                                    {
                                        LogForceAction($"{f.Name} attempted to steal {o.Name} from the prism in {location.Name}. The plan failed, but {f.Name} escaped.");
                                        ReputationChange(f, -10);
                                    }

                                    if (location.Government is Group)
                                    {
                                        foreach (Architect a in ((Group)location.Government).Architects)
                                        {
                                            a.Grievances.Add((f.Base, " attempted to steal " + o.Name + ", a treasured artifact, impacting the heritage and pride of " + location.Name + "."));
                                        }
                                    }
                                    else if (location.Government is Architect)
                                    {
                                        ((Architect)location.Government).Grievances.Add((f.Base, " attempted to steal " + o.Name + ", a treasured artifact, impacting the heritage and pride of " + location.Name + "."));
                                    }
                                    // Additional grievance to the creator of the artifact if known
                                    // Additional grievance to the creator of the artifact if known
                                    if (o.Creator != null)
                                    {
                                        if (o.Creator is Architect)
                                        {
                                            // If the creator is an individual architect
                                            ((Architect)(o.Creator)).Grievances.Add((f.Base, " stole " + o.Name + ", a creation of great cultural significance."));
                                        }
                                        else if (o.Creator is Group)
                                        {
                                            // If the creator is a group, add a grievance to each member of the group
                                            foreach (Architect member in ((Group)(o.Creator)).Architects)
                                            {
                                                member.Grievances.Add((f.Base, " stole " + o.Name + ", a creation of great cultural significance."));
                                            }
                                        }
                                    }

                                }
                            }

                            else if (f.CreativityValue >= 3 && r.Next(AmbitionRank * 150) == 1 && SettlementTypes.Contains(location.Type))
                            {
                                //craftsmanship

                                int totalMetals = Metals.Count;
                                int midpoint = totalMetals / 2; // This will give the midpoint of the list
                                Material Metal = Metals[r.Next(midpoint)]; //only use the first half of the materials

                                List<string> metalObjects = new List<string>
                                        {
                                            "shortsword",
                                            "knife",
                                            "greatsword",
                                            "battle axe",
                                            "axe",
                                            "greataxe",
                                            "rapier",
                                            "spear",
                                            "pike",
                                            "pickaxe",
                                            "mace",
                                            "hammer",
                                            "shield",
                                            "whip",
                                            "scourge",
                                            "flail",
                                            "chain",
                                            "urn",
                                            "pot",
                                            "helmet",
                                            "forge",
                                            "jar",
                                            "bottle",
                                            "mug",
                                            "bowl",
                                            "cup",
                                            "keg",
                                            "chest",
                                            "barrel",
                                            "bin",
                                            "door",
                                            "small chalice",
                                            "big chalice",
                                            "altar",
                                            "table",
                                            "chair"
                                        };

                                Object o = new Object("", metalObjects[r.Next(metalObjects.Count)], new EntityList<Material>() { Metal }, f.Base);
                                o.Name = GenerateUniqueName("1W" + r.Next(4, 7) + "s", o);

                                int Decider = r.Next(1, 4);

                                if (Decider == 1)
                                {
                                    location.Prism.HistoricalObjects.Add(o);
                                    LogForceAction($"{f.Name} created the legendary {o.Type} {o.Name} in {location.Name}. They stored it in the prism of {location.Name} for safekeeping.");
                                    ReputationChange(f, 5);
                                }
                                else if (Decider == 2)
                                {
                                    if (f.Base is Group)
                                    {
                                        ((Group)f.Base).Leader.Inventory.Add(o);
                                        LogForceAction($"{f.Name} created the legendary {o.Type} {o.Name} in {location.Name}. {((Group)f.Base).Leader.Name} held onto it for safekeeping.");
                                        ReputationChange(f, 5);
                                    }
                                    else
                                    {
                                        ((Architect)f.Base).Inventory.Add(o);
                                        LogForceAction($"{f.Name} created the legendary {o.Type} {o.Name} in {location.Name}. {((Architect)f.Base).Name} held onto it for safekeeping.");
                                        ReputationChange(f, 5);
                                    }
                                }
                                else
                                {
                                    Architect Buyer = ArchitectsAtLocation[r.Next(ArchitectsAtLocation.Count)];
                                    int Price = r.Next(0, Buyer.Wealth);
                                    Buyer.Inventory.Add(o);
                                    Buyer.Wealth -= Price;

                                    if (f.Base is Group)
                                    {
                                        ((Group)f.Base).Wealth += Price;
                                        LogForceAction($"{f.Name} created the legendary {o.Type} {o.Name} in {location.Name}. {((Group)f.Base).Leader.Name} sold it to {Buyer.Name} for {Price}.");
                                        ReputationChange(f, 5);
                                    }
                                    else
                                    {
                                        ((Architect)f.Base).Wealth += Price;
                                        LogForceAction($"{f.Name} created the legendary {o.Type} {o.Name} in {location.Name}. {((Architect)f.Base).Pronoun} sold it to {Buyer.Name} for {Price}.");
                                        ReputationChange(f, 5);
                                    }
                                }
                            }
                            else if (f.Base is Architect && f.FamilyValue >= 0 && r.Next(AmbitionRank * 5) == 1 && ((Architect)(f.Base)).Spouse == null)
                            {
                                bool CaresAboutMarriageRace = true;
                                if (r.Next(15) == 1)
                                {
                                    CaresAboutMarriageRace = false;
                                }

                                foreach (Architect a in ArchitectsAtLocation)
                                {
                                    // Skip if the same architect or if sex is the same or if theyre already married lul
                                    if (a.Spouse != null || a == f.Base || a.Sex == ((Architect)f.Base).Sex) continue;

                                    // Check for similarity in compasses within 40 (twice as lenient)
                                    bool similarCompasses = Math.Abs(a.MoralCompass - f.MoralCompass) < 40 && Math.Abs(a.StabilityCompass - f.StabilityCompass) < 40;

                                    // Check for at least 3 values within 4 of each other (fewer required, twice as lenient)
                                    int similarValuesCount = 0;
                                    similarValuesCount += Math.Abs(a.PropertyValue - f.PropertyValue) < 4 ? 1 : 0;
                                    similarValuesCount += Math.Abs(a.FamilyValue - f.FamilyValue) < 4 ? 1 : 0;
                                    similarValuesCount += Math.Abs(a.PowerValue - f.PowerValue) < 4 ? 1 : 0;
                                    similarValuesCount += Math.Abs(a.MoneyValue - f.MoneyValue) < 4 ? 1 : 0;
                                    similarValuesCount += Math.Abs(a.KnowledgeValue - f.KnowledgeValue) < 4 ? 1 : 0;
                                    similarValuesCount += Math.Abs(a.SpiritualityValue - f.SpiritualityValue) < 4 ? 1 : 0;
                                    similarValuesCount += Math.Abs(a.ProwessValue - f.ProwessValue) < 4 ? 1 : 0;
                                    similarValuesCount += Math.Abs(a.PatriotismValue - f.PatriotismValue) < 4 ? 1 : 0;
                                    similarValuesCount += Math.Abs(a.CourageValue - f.CourageValue) < 4 ? 1 : 0;

                                    // Evaluate race consideration if applicable
                                    bool raceConsideration = CaresAboutMarriageRace ? a.Race == ((Architect)f.Base).Race : true;

                                    if (similarCompasses && similarValuesCount >= 3 && raceConsideration)
                                    {
                                        a.Spouse = ((Architect)(f.Base));
                                        ((Architect)(f.Base)).Spouse = a;

                                        // Initialize a variable to hold the name of the shrine if one is found
                                        string shrineName = string.Empty;

                                        // Iterate through all structures in the location to find a shrine
                                        foreach (var structure in location.AllStructures)
                                        {
                                            if (structure.Type == "shrine")
                                            {
                                                shrineName = structure.Name;
                                                break; // Exit the loop once a shrine is found
                                            }
                                        }

                                        // Check if a shrine was found and adjust the log message accordingly
                                        if (!string.IsNullOrEmpty(shrineName))
                                        {
                                            LogForceAction($"{f.Name} and {a.Name} got married at {shrineName} in {location.Name}.");
                                        }
                                        else
                                        {
                                            LogForceAction($"{f.Name} and {a.Name} got married in {location.Name}.");
                                        }
                                        break; // Exit the loop once a shrine is found
                                    }
                                }
                            }

                            else if (f.Base is Architect && ((Architect)f.Base).Spouse != null && ((Architect)f.Base).HadChildren == false)
                            {
                                ((Architect)f.Base).HadChildren = true;
                                ((Architect)f.Base).Spouse.HadChildren = true;

                                static int GenerateChildrenNumber(Random r)
                                {
                                    double lambda = 0.2; // Adjust this parameter to tweak the distribution
                                    double uniformRandom = r.NextDouble();
                                    double skewedRandom = -Math.Log(1 - uniformRandom) / lambda;

                                    // Ensure the number is within our desired range but skewed towards lower numbers
                                    int children = (int)Math.Floor(skewedRandom);

                                    // Cap the result at 30 to allow for incredibly uncommon scenarios but not exceed it
                                    if (children > 30) children = 30;

                                    // Ensure at least 1 child
                                    if (children < 1) children = 1;

                                    return children;
                                }

                                int Children = GenerateChildrenNumber(r);

                                int ImportantChildren = r.Next(0, Children / 2);

                                if (ImportantChildren == 0)
                                {
                                    LogForceAction(f.Base.Name + " and " + ((Architect)(f.Base)).Spouse.Name + " had " + Children + " children, but none of them actually matter.");
                                    location.Districts[r.Next(location.Districts.Count)].UnplacedPopulation += Children;
                                }
                                else
                                {
                                    Race ChildRace;

                                    if (((Architect)f.Base).Race == ((Architect)f.Base).Spouse.Race)
                                    {
                                        ChildRace = ((Architect)f.Base).Race;
                                    }
                                    else
                                    {
                                        ChildRace = GetRace("archaix");
                                    }

                                    List<string> ImportantChildrenNames = new List<string>();

                                    for (int i = ImportantChildren; i != 0; i--)
                                    {
                                        Architect a = new Architect("", Game1.Sexes[r.Next(2)], ChildRace, 0, "child", new EntityList<Object>(), ((Architect)f.Base).Location, ((Architect)f.Base).District, ((Architect)f.Base).Block, "", 0);
                                        a.Name = GenerateUniqueArchitectName(a);
                                        location.Districts[r.Next(location.Districts.Count)].Architects.Add(a);
                                        ImportantChildrenNames.Add(a.Name);
                                    }
                                    location.Districts[r.Next(location.Districts.Count)].UnplacedPopulation += (Children - ImportantChildren);

                                    LogForceAction(f.Base.Name + " and " + ((Architect)(f.Base)).Spouse.Name + " had " + Children + " children. The ones that actually matter are " + Game1.FormatList(ImportantChildrenNames) + ".");
                                }
                            }

                        }

                    }


                    //hahahahaaha embezzlementeeee
                    List<(Entity, int)> EmbezzlToRemove = new List<(Entity, int)>();
                    foreach ((Entity, int) e in location.Embezzlements)
                    {
                        if (r.Next(1, 10 * MonthToDayConstant) == 1)
                        {
                            if (e.Item1 is Architect)
                            {
                                ((Architect)e.Item1).Wealth += e.Item2;
                                location.Wealth -= e.Item2;
                            }
                            else
                            {
                                ((Group)e.Item1).Wealth += e.Item2;
                                location.Wealth -= e.Item2;
                            }
                        }


                        if (r.Next(1, 100 * MonthToDayConstant) == 0)
                        {
                            HistoricalEvents.Add("A loophole in the governmental structure of " + location.Name + " was discovered and fixed, and " + e.Item1.Name + " lost some ability to embezzle funding.");
                            EmbezzlToRemove.Add(e);
                        }
                    }
                    foreach ((Entity, int) E in EmbezzlToRemove)
                    {
                        location.Embezzlements.Remove(E);
                    }





                    //iterate through districts

                    foreach (District d in location.Districts)
                    {
                        int DistrictMaxPopulation = 0;


                        foreach(Structure s in location.AllStructures)
                        {
                            if(s.Block.District == d && s.Type.EndsWith("house"))
                            {
                                DistrictMaxPopulation += 4;
                            }
                        }

                        //Handle population increase in the district


                        if (location.Region.Blight != Purity && SettlementTypes.Contains(location.Type))
                        {
                            if (d.UnplacedPopulation != 0)
                            {
                                d.UnplacedPopulation = Math.Max(d.UnplacedPopulation - r.Next(0, 4), 0);

                                if (d.UnplacedPopulation == 0)
                                {
                                    HistoricalEvents.Add($"{Date} {location.Name} fell to the {location.Region.Blight.Name}.");
                                    location.LocationHistoricalEvents.Add($"{location.Name} fell to the {location.Region.Blight.Name}.");
                                }
                            }
                            foreach (Architect a in d.Architects)
                            {
                                if (r.Next(1, 70 * MonthToDayConstant) == 1)
                                {
                                    d.ArchitectsToRemove.Add(a);

                                    HistoricalEvents.Add($"{Date} {a.Name} died to the {location.Region.Blight.Name} in {location.Name}.");
                                    location.LocationHistoricalEvents.Add($"{Date} {a.Name} died to the {location.Region.Blight.Name} in {location.Name}.");
                                }
                            }
                        }



                        int BirthProbabilityMod = 100; // higher is less likely, decrease the chance of procreation

                        if (d.Population() < DistrictMaxPopulation && location.TruePopulation() > 4)
                        {
                            // Calculate birth probability dynamically based on the original function
                            double baseProbability = 1.0; // Set a base probability
                            double populationFactor = 1 + (d.Population() / 100.0); // Adjust based on population (modify the factor as needed)
                            double monthToDayConstant = 28.0 / Days; // Adjust probability to account for days instead of a month

                            double birthProbability = (baseProbability / populationFactor) * monthToDayConstant;

                            int births = 0;

                            // Calculate births using a random number and the probability
                            for (int i = 0; i < d.Population(); i++)
                            {
                                if (Game1.r.NextDouble() < birthProbability / BirthProbabilityMod)
                                {
                                    births++;
                                }
                            }

                            // Update the unplaced population with the calculated births
                            d.UnplacedPopulation += births;
                        }




                        //Designate the Construction of a new District

                        if (d.UnplacedPopulation + d.Architects.Count > 150)
                        {
                            int Movers = Game1.r.Next(65, 85);

                            District NewD = new District(false, location, Movers);
                            d.UnplacedPopulation = d.UnplacedPopulation - Movers;

                            foreach (Architect a in d.Architects)
                            {
                                if (Game1.r.Next(1, 4) == 1 && a.Group != location.Government)
                                {
                                    NewD.Architects.Add(a);
                                    d.ArchitectsToRemove.Add(a);
                                    Movers--;
                                }
                            }
                            foreach (Architect a in d.ArchitectsToRemove)
                            {
                                d.Architects.Remove(a);
                            }
                            d.ArchitectsToRemove = new EntityList<Architect>();

                            //go ahead and build a bunch of houses in the new district instead of moving stuff everywhere.

                            if (SettlementTypes.Contains(d.Location.Type) || d.Location.Type == "core" || d.Location.Type == "garrison")
                            {
                                for (int i = 0; i < Game1.r.Next(6, 10); i++)
                                {
                                    Block ChosenBlock = NewD.DistrictMap[Game1.r.Next(0, 49)];
                                    Structure s = new Structure("house", new EntityList<Object>(), new EntityList<Room>(), ChosenBlock, new EntityList<Material> { location.HomeCivilization.CulturalWood }, new List<string> { location.HomeCivilization.CulturalWood.Name }, new List<string> { Game1.LightingStyles[Game1.r.Next(Game1.LightingStyles.Count)] }, Game1.r.Next(0, 5), Game1.r.Next(0, 4), (int)Math.Round(Cycle / 290304000));

                                    ChosenBlock.Structures.Add(s);
                                }
                            }

                            //well
                            NewD.DistrictMap[r.Next(2, 6) + r.Next(2, 5) * 7].Objects.Add(new Object(null, "well", new EntityList<Material> { location.HomeCivilization.CulturalStone }, true, true, null, null, 255, false, null, null, null, false));

                            HistoricalEvents.Add($"{Date} {location.Name} segmented off a plot of land to a new district, {NewD.Name}, dedicated to {NewD.Industry}.");
                            location.LocationHistoricalEvents.Add($"{Date} {location.Name} segmented off a plot of land to a new district, {NewD.Name}, dedicated to {NewD.Industry}.");

                            location.DistrictsToAdd.Add(NewD);
                        }


                        //Handle elevation of a member of the population to Architect

                        int decider = Game1.r.Next(1, 75 * MonthToDayConstant);

                        if (decider == 1 && d.UnplacedPopulation > 0)
                        {
                            bool Ismale = true;
                            if (Game1.r.Next(1, 3) == 1)
                            {
                                Ismale = false;
                            }

                            string Role = "";
                            string Destiny = "";
                            Race Race;


                            if (Game1.r.Next(1, 20) == 1 || location.PrimaryRace == null || location.PrimaryRace == GetRace(""))
                            {
                                Race = HumanoidRaces[Game1.r.Next(HumanoidRaces.Count)];
                            }
                            else
                            {
                                Race = location.PrimaryRace;
                            }

                            //decide if the creature will be magical

                            int DestinyDecider = Game1.r.Next(1, 300);

                            if (DestinyDecider < 5 && Race == GetRace("nightfell"))
                            {
                                Destiny = "warlock";
                            }
                            else if (DestinyDecider < 7 && Race == GetRace("luminarch"))
                            {
                                Destiny = "sorcerer";
                            }
                            else if (DestinyDecider < 8)
                            {
                                Destiny = "parasite";
                            }

                            string gender = Ismale ? "male" : "female";

                            string role = Game1.WeightedRandomArchitectProfessions[Game1.r.Next(Game1.WeightedRandomArchitectProfessions.Count)];
                            Role = role;

                            Architect architect = new Architect("", gender, Race, Game1.r.Next(14, 60), Role, new EntityList<Object>(), location, d, null, Destiny, 1);
                            location.HomeCivilization.Citizens.Add(architect);
                            string Name = GenerateUniqueArchitectName(architect);
                            architect.Name = Name;

                            HistoricalEvents.Add($"{Date} {Name} became an influential {Role} in {location.Name}");
                            location.LocationHistoricalEvents.Add($"{Date} {Name} became an influential {Role} in {location.Name}");

                            if (r.Next(100) == 1)
                            {
                                HistoricalEvents.Add($"{Date} {Name} possessed an unrivaled spirit and determination.");
                                location.LocationHistoricalEvents.Add($"{Date} {Name} possessed an unrivaled spirit and determination.");

                                Legends.Add(architect);
                            }

                            d.Architects.Add(architect);

                            d.UnplacedPopulation = d.UnplacedPopulation - 1;
                        }



                        //LEGENDS

                        LegendsManager.ManageLegends(this, Days);




                        foreach (Structure s in location.AllStructures)
                        {
                            if (s.Type == "forge")
                            {
                                WealthIncrease += 1;
                            }
                            else if (s.Type == "market")
                            {
                                WealthIncrease += 2;
                            }
                        }

                        //ok so THIS stuff is all gonna happen once per district

                        //Handle the creation of a Group
                        //make a group
                        //new group
                        //i cant find this area all the time fsr lolloooolooolol

                        if (d.Architects.Count > 0 && SettlementTypes.Contains(location.Type))
                        {
                            foreach (Architect a in d.Architects)
                            {
                                if (a.Group == null)
                                {
                                    // Check if the Architect's profession can form a group
                                    string groupType = Game1.ConvertArchitectToGroupType[a.Profession];
                                    if (groupType != "none")
                                    {
                                        int GroupsAlreadyLikeThis = 0;

                                        foreach (Group g in location.GroupsAtThisLocation)
                                        {
                                            if (g.Type == groupType)
                                            {
                                                GroupsAlreadyLikeThis++;
                                            }
                                        }

                                        if (GroupsAlreadyLikeThis <= Math.Round((decimal)location.TruePopulation() / 500, MidpointRounding.ToNegativeInfinity))
                                        {
                                            int chance = (groupType == "trade") ? 3000 * MonthToDayConstant : 1000 * MonthToDayConstant;
                                            if (Game1.r.Next(1, chance) == 1 && a.Profession != "prophet")
                                            {
                                                if (groupType != "trade" || location.Market != null)
                                                {
                                                    Group g = new Group(new EntityList<Architect>(), groupType, a, location);
                                                    a.Group = g;
                                                    g.Architects.Add(a);
                                                    Groups.Add(g);

                                                    location.GroupsAtThisLocation.Add(g);
                                                    if (g.Type == "trade")
                                                    {
                                                        // Add a single new item with a random count between 10 and 20
                                                        int itemCount = r.Next(10, 20);
                                                        string itemString = $"bar,{itemCount},{location.HomeCivilization.CulturalMetal.Name}";

                                                        g.CaravanItems.Add(itemString);

                                                        location.TradersAtThisLocation.Add(g);
                                                        g.TradeRoute.Add(location);
                                                        TradingGroups.Add(g);
                                                    }

                                                    HistoricalEvents.Add($"{Date} {a.Name} founded {g.Name}, a {g.Type} group, in {location.Name}");

                                                    a.GroupLoyalty = 5;
                                                    location.LocationHistoricalEvents.Add($"{Date} {a.Name} founded {g.Name}, a {g.Type} group, in {location.Name}");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }


                        foreach (Architect a in d.ArchitectsToRemove)
                        {
                            d.Architects.Remove(a);
                        }
                        d.ArchitectsToRemove = new EntityList<Architect>();

                        //Handle civilization leadership changes

                        if (location.Government == null && SettlementTypes.Contains(location.Type))
                        {
                            foreach (Group g in location.GroupsAtThisLocation)
                            {
                                if (g.Type != "trade")
                                {
                                    HistoricalEvents.Add(string.Concat(Date, " ", g.Name, " took power in ", location.Name, "."));

                                    location.LocationHistoricalEvents.Add(string.Concat(Date, " ", g.Name, " took power in ", location.Name, "."));
                                    location.Government = g;

                                    foreach (Architect a in g.Architects)
                                    {
                                        if (a.District != location.Districts[0])
                                        {
                                            a.District.Architects.Remove(a);
                                            location.Districts[0].Architects.Add(a);
                                            a.District = location.Districts[0];
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        else if (location.Government == null && SettlementTypes.Contains(location.Type))
                        {

                        }
                        else if (location.Government is Group && SettlementTypes.Contains(location.Type))
                        {
                            if (((Group)location.Government).Type != "political")
                            {
                                foreach (Group g in location.GroupsAtThisLocation)
                                {
                                    if (g.Type == "political")
                                    {
                                        HistoricalEvents.Add(string.Concat(Date, " ", g.Name, " took power from ", location.Government.Name, " thanks to their credibility and support in ", location.Name, "."));

                                        location.LocationHistoricalEvents.Add(string.Concat(Date, " ", g.Name, " took power from ", location.Government.Name, " thanks to their credibility and support in ", location.Name, "."));
                                        location.Government = g;

                                        foreach (Architect a in g.Architects)
                                        {
                                            if (a.District != location.Districts[0])
                                            {
                                                a.District.Architects.Remove(a);
                                                location.Districts[0].Architects.Add(a);
                                                a.District = location.Districts[0];
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                        }

                        //Handle architect/architect group actions IN THE DISTRICT

                        // Study and write books
                        int Threshold = 500;

                        foreach (Architect a in d.Architects)
                        {
                            if ((a.Profession == "scholar" && a.IsStudying) || a.Profession == "warlock" || a.Profession == "sorcerer")
                            {
                                switch (a.ScholarType)
                                {
                                    case "mage":
                                        a.MagicStudyPoints += Game1.r.Next(0, 7);
                                        break;
                                    case "engineer":
                                        a.ScienceStudyPoints += Game1.r.Next(0, 7);
                                        break;
                                    case "entertainer":
                                        a.CultureStudyPoints += Game1.r.Next(0, 7);
                                        break;
                                    case "artificer":
                                        a.ScienceStudyPoints += Game1.r.Next(0, 4);
                                        a.MagicStudyPoints += Game1.r.Next(0, 4);
                                        break;
                                    case "bard":
                                        a.CultureStudyPoints += Game1.r.Next(0, 4);
                                        a.MagicStudyPoints += Game1.r.Next(0, 4);
                                        break;
                                    case "sage":
                                        a.CultureStudyPoints += Game1.r.Next(0, 4);
                                        a.ScienceStudyPoints += Game1.r.Next(0, 4);
                                        break;
                                    case "luminary":
                                        a.ScienceStudyPoints += Game1.r.Next(0, 2);
                                        a.CultureStudyPoints += Game1.r.Next(0, 2);
                                        a.MagicStudyPoints += Game1.r.Next(0, 2);
                                        break;
                                    default:
                                        // Handle unknown ScholarType, if needed
                                        break;
                                }

                                // Decide to write
                                if ((a.Profession == "scholar" && Game1.r.Next(1, (Math.Max(500 - (a.MagicStudyPoints + a.ScienceStudyPoints + a.CultureStudyPoints), 50)) * MonthToDayConstant) == 1) ||
                                    ((a.Profession == "sorcerer" || a.Profession == "warlock") && (Game1.r.Next(1, 50) * MonthToDayConstant) == 1))
                                {
                                    string writingType = "";
                                    int totalWeight = a.MagicStudyPoints + a.ScienceStudyPoints + a.CultureStudyPoints;
                                    if (totalWeight == 0) totalWeight = 1;  // Prevent division by zero

                                    // Calculate probabilities based on relative weight of each study point
                                    int magicWeight = (a.MagicStudyPoints * 100) / totalWeight;
                                    int scienceWeight = (a.ScienceStudyPoints * 100) / totalWeight;
                                    int cultureWeight = (a.CultureStudyPoints * 100) / totalWeight;

                                    // Use a random number to decide based on weights
                                    int randomChoice = Game1.r.Next(100);

                                    bool shouldWriteBook = (a.SpellsKnown.Count > 0) && (r.Next(2) == 0);

                                    if (shouldWriteBook)
                                    {
                                        // 50 percent chance to ignore and write a book
                                        writingType = "book";
                                    }
                                    else if ((a.Profession == "sorcerer" || a.Profession == "warlock") && (r.Next(5) != 0))
                                    {
                                        writingType = "book";
                                    }
                                    else if (randomChoice < magicWeight)
                                    {
                                        writingType = "poem";
                                    }
                                    else if (randomChoice < magicWeight + scienceWeight)
                                    {
                                        writingType = "book";
                                    }
                                    else
                                    {
                                        writingType = "song";
                                    }


                                    Composition newWork = null;

                                    if (writingType == "book" && Game1.r.NextDouble() < 0.5)
                                    {
                                        // 50% chance to write a history book about a specific subject
                                        Entity subject = GenerateRandomSubject();
                                        if (subject != null && HistoricalEvents.Any(historicalEvent => historicalEvent.Contains(subject.Name)))
                                        {
                                            newWork = new Composition(writingType, a, subject);
                                        }
                                    }
                                    else
                                    {
                                        // 50% chance to write about a general domain
                                        Entity domainEntity = a.AlignedDomains[Game1.r.Next(a.AlignedDomains.Count)];
                                        newWork = new Composition(writingType, a, domainEntity);
                                    }

                                    if (newWork != null)
                                    {
                                        string Spell = "";

                                        if (writingType == "book")
                                        {
                                            string ObjectType = new List<string>() { "scroll", "scroll", "sheet", "book", "book" }[r.Next(5)];

                                            Object o = new Object(newWork.Name, ObjectType, new EntityList<Material>() { d.Location.HomeCivilization != null ? d.Location.HomeCivilization.CulturalCloth : Cloths[r.Next(Cloths.Count)] }, a);

                                            if (a.Profession == "sorcerer" || a.Profession == "warlock")
                                            {
                                                a.HomeLocation.AllStructures[0].HistoricalObjects.Add(o);
                                            }
                                            else
                                            {
                                                a.StudyBuilding.HistoricalObjects.Add(o);
                                            }
                                            o.CompositionContent = newWork;
                                            AllWrittenContent.Add(o);

                                            if (a.SpellsKnown.Count > 0 && r.Next(5) == 1)
                                            {
                                                o.SpecialKnowledge = a.SpellsKnown[r.Next(a.SpellsKnown.Count)];
                                                Spell = o.SpecialKnowledge.Metadata;

                                                o.Materials.Clear();
                                                o.Materials.Add(Enchromalite);
                                            }
                                        }
                                        else
                                        {
                                            a.CultureBank.Add(newWork); // Storing poems and songs in the CultureBank
                                        }


                                        WorksOfCulture++;

                                        // Log historical event
                                        HistoricalEvents.Add($"{Date} {a.Name} authored a {writingType} titled '{newWork.Name}' in {location.Name}.");
                                        location.LocationHistoricalEvents.Add($"{Date} {a.Name} authored a {writingType} titled '{newWork.Name}' in {location.Name}.");

                                        if (Spell != "")
                                        {
                                            HistoricalEvents.Add($"{Date} {newWork.Name} contained the secret of {Spell}.");
                                            location.LocationHistoricalEvents.Add($"{Date} {newWork.Name} contained the secret of {Spell}.");
                                        }
                                    }
                                }

                                // Archelevation
                                if (!a.ScholarType.StartsWith("arch"))
                                {
                                    if (a.ScholarType == "mage" && a.MagicStudyPoints > Threshold)
                                    {
                                        a.ScholarType = "archmage";
                                        HistoricalEvents.Add($"{Date} {a.Name} became an archmage in {location.Name}");
                                        location.LocationHistoricalEvents.Add($"{Date} {a.Name} became an archmage in {location.Name}");
                                    }
                                    else if (a.ScholarType == "engineer" && a.ScienceStudyPoints > Threshold)
                                    {
                                        a.ScholarType = "archengineer";
                                        HistoricalEvents.Add($"{Date} {a.Name} became an archengineer in {location.Name}");
                                        location.LocationHistoricalEvents.Add($"{Date} {a.Name} became an archengineer in {location.Name}");
                                    }
                                    else if (a.ScholarType == "entertainer" && a.CultureStudyPoints > Threshold)
                                    {
                                        a.ScholarType = "archentertainer";
                                        HistoricalEvents.Add($"{Date} {a.Name} became an archentertainer in {location.Name}");
                                        location.LocationHistoricalEvents.Add($"{Date} {a.Name} became an archentertainer in {location.Name}");
                                    }
                                    else if (a.ScholarType == "artificer" && a.MagicStudyPoints > Threshold / 2 && a.ScienceStudyPoints > Threshold / 2)
                                    {
                                        a.ScholarType = "archartificer";
                                        HistoricalEvents.Add($"{Date} {a.Name} became an archartificer in {location.Name}");
                                        location.LocationHistoricalEvents.Add($"{Date} {a.Name} became an archartificer in {location.Name}");
                                    }
                                    else if (a.ScholarType == "bard" && a.MagicStudyPoints > Threshold / 2 && a.CultureStudyPoints > Threshold / 2)
                                    {
                                        a.ScholarType = "archbard";
                                        HistoricalEvents.Add($"{Date} {a.Name} became an archbard in {location.Name}");
                                        location.LocationHistoricalEvents.Add($"{Date} {a.Name} became an archbard in {location.Name}");
                                    }
                                    else if (a.ScholarType == "sage" && a.CultureStudyPoints > Threshold / 2 && a.ScienceStudyPoints > Threshold / 2)
                                    {
                                        a.ScholarType = "archsage";
                                        HistoricalEvents.Add($"{Date} {a.Name} became an archsage in {location.Name}");
                                        location.LocationHistoricalEvents.Add($"{Date} {a.Name} became an archsage in {location.Name}");
                                    }
                                    else if (a.ScholarType == "luminary" && a.CultureStudyPoints > Threshold / 3 && a.ScienceStudyPoints > Threshold / 3 && a.MagicStudyPoints > Threshold / 3)
                                    {
                                        a.ScholarType = "archluminary";
                                        HistoricalEvents.Add($"{Date} {a.Name} became an archluminary in {location.Name}");
                                        location.LocationHistoricalEvents.Add($"{Date} {a.Name} became an archluminary in {location.Name}");
                                    }
                                }

                                // Mage scholars learn spells, each can only discover one in their lifetime, but can learn more from others.
                                if ((a.ScholarType == "mage" || a.ScholarType == "artificer" || a.ScholarType == "luminary" || a.ScholarType == "bard" || a.ScholarType == "archmage" || a.ScholarType == "archartificer" || a.ScholarType == "archluminary" || a.ScholarType == "archbard") && !a.DiscoveredASpell && UndiscoveredSpells.Count > 0 && a.MagicStudyPoints > 200)
                                {
                                    int SpellID = Game1.r.Next(UndiscoveredSpells.Count);
                                    a.SpellsKnown.Add(UndiscoveredSpells[SpellID]);
                                    location.LocationHistoricalEvents.Add($"{Date} {a.Name} discovered the secret of {UndiscoveredSpells[SpellID]} in {location.Name}");
                                    HistoricalEvents.Add($"{Date} {a.Name} discovered the secret of {UndiscoveredSpells[SpellID]} in {location.Name}");
                                    DiscoveredSpells.Add(UndiscoveredSpells[SpellID]);
                                    UndiscoveredSpells.RemoveAt(SpellID);
                                    a.DiscoveredASpell = true;
                                }
                            }

                            // Destiny
                            List<string> PossibleInfusionSpells = new List<string>();

                            if (a.Age >= a.DestinyArrivalYear && a.Profession != a.Destiny && (a.Destiny == "sorcerer" || a.Destiny == "warlock") && a.IsImmortal == false /*this will prevent multiple infusions from happening*/)
                            {
                                a.AssignSpells();

                                if (a.Destiny == "warlock")
                                {
                                    a.Profession = a.Destiny;
                                    HistoricalEvents.Add($"{Date} {a.Name} was infused with incredible power by {DarkDeity.Name} in {location.Name}, blessed to become an immortal warlock tied to his service.");
                                    location.LocationHistoricalEvents.Add($"{Date} {a.Name} was infused with incredible power by {DarkDeity.Name} in {location.Name}, blessed to become an immortal warlock tied to his service.");
                                    a.GroupLoyalty = -10;
                                    a.IsImmortal = true;
                                    a.Inventory.AddRange(LootTableMachine("magictreasure34"));
                                }
                                else if (a.Destiny == "sorcerer")
                                {
                                    a.Profession = a.Destiny;
                                    HistoricalEvents.Add($"{Date} {a.Name} was infused with incredible power by {LightDeity.Name} in {location.Name}, blessed to become an eternal sorcerer tied to his service.");
                                    location.LocationHistoricalEvents.Add($"{Date} {a.Name} was infused with incredible power by {LightDeity.Name} in {location.Name}, blessed to become an eternal sorcerer tied to his service.");
                                    a.GroupLoyalty = -10;
                                    a.Inventory.AddRange(LootTableMachine("magictreasure34"));
                                    a.IsImmortal = true;
                                }
                            }
                        }

                        Entity GenerateRandomSubject()
                        {
                            var random = new Random();
                            EntityList<Entity> subjects = new EntityList<Entity>();

                            subjects.AddRange(AllArchitects);
                            subjects.AddRange(AllLocations);
                            subjects.AddRange(AllLocations.SelectMany(loc => loc.AllStructures));
                            subjects.AddRange(AllLocations.SelectMany(loc => loc.AllStructures.SelectMany(structure => structure.HistoricalObjects)));

                            return subjects[random.Next(subjects.Count)];
                        }

                        //also make sure that architects change their group loyalty based on actions

                        foreach (Architect a in d.Architects)
                        {
                            // Count population
                            CurrentlyCountingArchitects++;

                            if (a.Profession == "scholar" && !a.IsStudying)
                            {
                                bool FoundLibrary = false;

                                // Search for a library within the current district
                                if (d.Location.Library != null)
                                {
                                    FoundLibrary = true;
                                    a.IsStudying = true;
                                    a.StudyBuilding = d.Location.Library;
                                    HistoricalEvents.Add($"{Date} {a.Name} began studying at {d.Location.Library.Name} in {location.Name}");
                                }

                                // If no library found, search other locations
                                if (!FoundLibrary)
                                {
                                    foreach (var Location in AllLocations)
                                    {
                                        if (Location.HomeCivilization == a.HomeLocation.HomeCivilization && Location.Library != null)
                                        {
                                            a.NextMigrationLocation = Location;
                                            a.IsStudying = true;
                                            a.StudyBuilding = Location.Library;
                                            string migrationEvent = $"{Date} {a.Name} heard of {Location.Library.Name}, a library in {Location.Name}, and migrated there to study.";
                                            string studyEvent = $"{Date} {a.Name} began studying at {Location.Library.Name} in {Location.Name}.";
                                            HistoricalEvents.Add(migrationEvent);
                                            Location.LocationHistoricalEvents.Add(migrationEvent);
                                            HistoricalEvents.Add(studyEvent);
                                            Location.LocationHistoricalEvents.Add(studyEvent);
                                            FoundLibrary = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        foreach (Architect a in d.ArchitectsToAdd)
                        {
                            d.Architects.Add(a);
                        }
                        d.ArchitectsToAdd = new EntityList<Architect>();


                        // TODO: Handle architect/architect group stability
                        // also utilize group loyalty


                        // TODO: Handle the disbanding of a Group
                        foreach (Group g in location.GroupsAtThisLocation)
                        {
                            if (g.Architects.Count == 0)
                            {
                                HistoricalEvents.Add(string.Concat(Date, " ", g.Name, " collapsed in ", location.Name, " due to running out of passionate members."));
                                location.LocationHistoricalEvents.Add(string.Concat(Date, " ", g.Name, " collapsed in ", location.Name, " due to running out of passionate members."));
                                GroupsToRemove.Add(g);
                                if (location.Government == g)
                                {
                                    location.Government = null;
                                }
                            }
                            else if (g.Stability < 1)
                            {
                                HistoricalEvents.Add(string.Concat(Date, " ", g.Name, " collapsed in ", location.Name, " due to a disagreement of values."));
                                location.LocationHistoricalEvents.Add(string.Concat(Date, " ", g.Name, " collapsed in ", location.Name, " due to a disagreement of values."));
                                foreach (Architect a in g.Architects)
                                {
                                    a.Group = null;
                                }
                                GroupsToRemove.Add(g);
                                if (location.Government == g)
                                {
                                    location.Government = null;
                                }
                            }
                            else if (g.MonthsOld > 60 && g.Architects.Count <= 1 && location.Government != g)
                            {
                                HistoricalEvents.Add(string.Concat(Date, " ", g.Leader.Name, " disbanded ", g.Name, " to become an individual practitioner in ", location.Name, "."));
                                location.LocationHistoricalEvents.Add(string.Concat(Date, " ", g.Leader.Name, " disbanded ", g.Name, " to become an individual practitioner in ", location.Name, "."));
                                g.Leader.Group = null;
                                GroupsToRemove.Add(g);
                            }
                            else if (g.MonthsOld > 180 && g.Architects.Count == 2 && location.Government != g)
                            {
                                HistoricalEvents.Add(string.Concat(Date, " ", g.Architects[0].Name, " and ", g.Architects[1].Name, " disbanded ", g.Name, " and stopped traveling together in ", location.Name, "."));
                                location.LocationHistoricalEvents.Add(string.Concat(Date, " ", g.Architects[0].Name, " and ", g.Architects[1].Name, " disbanded ", g.Name, " and stopped traveling together in ", location.Name, "."));
                                g.Leader.Group = null;
                                GroupsToRemove.Add(g);
                            }
                        }
                        foreach (Group g in GroupsToRemove)
                        {
                            Groups.Remove(g);
                            location.GroupsAtThisLocation.Remove(g);
                        }
                        GroupsToRemove = new EntityList<Group>();

                        foreach (Group G in location.GroupsAtThisLocation)
                        {
                            foreach (Architect a in G.ArchitectsToRemove)
                            {
                                a.Group = null;
                                G.Architects.Remove(a);
                            }
                        }

                        foreach (Group g in location.GroupsAtThisLocation)
                        {
                            g.ArchitectsToRemove = new EntityList<Architect>();
                        }

                        //Handle recruiting of Architects to a group


                        foreach (Group g in location.GroupsAtThisLocation)
                        {
                            foreach (Architect a in d.Architects)
                            {
                                if (a.Group == null && Game1.ConvertArchitectToGroupType[a.Profession] == g.Type && (!g.ArchitectsWhoDeclined.Contains(a)))
                                {
                                    if (Game1.r.Next(1, 7) == 1)
                                    {
                                        // denial
                                        if (Game1.r.Next(1, 3) == 1)
                                        {
                                            HistoricalEvents.Add(string.Concat(Date, " ", a.Name, " requested to join ", g.Name, ", but was denied."));
                                            g.ArchitectsWhoDeclined.Add(a);
                                        }
                                        else
                                        {
                                            HistoricalEvents.Add(string.Concat(Date, " ", a.Name, " was invited to join ", g.Name, ", but decided against it."));
                                            g.ArchitectsWhoDeclined.Add(a);
                                        }
                                    }
                                    else
                                    {
                                        // acceptance
                                        if (Game1.r.Next(1, 3) == 1)
                                        {
                                            HistoricalEvents.Add(string.Concat(Date, " ", a.Name, " requested to join ", g.Name, ", and was accepted."));
                                            g.Architects.Add(a);
                                            d.ArchitectsToRemove.Add(a);
                                            a.Group = g;
                                            a.GroupLoyalty = 3;
                                        }
                                        else
                                        {
                                            HistoricalEvents.Add(string.Concat(Date, " ", g.Name, " requested that ", a.Name, " join them, and ", a.Name, " accepted."));
                                            g.Architects.Add(a);
                                            d.ArchitectsToRemove.Add(a);
                                            a.Group = g;
                                            a.GroupLoyalty = 3;
                                        }
                                    }
                                }
                            }
                            foreach (Architect a in d.ArchitectsToRemove)
                            {
                                d.Architects.Remove(a);
                            }
                        }

                        // TODO: Handle Architects leaving groups due to loyalty loss or low loyalty and boredom

                        foreach (Architect a in d.Architects)
                        {
                            if (a.Group != null)
                            {
                                if (a.GroupLoyalty <= 0)
                                {
                                    HistoricalEvents.Add(string.Concat(Date, " ", a.Name, " left ", a.Group.Name, " due to a disagreement with their values."));
                                    a.Group.ArchitectsToRemove.Add(a);
                                    a.Group = null;
                                }
                                else if (a.GroupLoyalty <= 2 && Game1.r.Next(1, 100) == 1)
                                {
                                    HistoricalEvents.Add(string.Concat(Date, " ", a.Name, " left ", a.Group.Name, " due to boredom."));
                                    a.Group.ArchitectsToRemove.Add(a);
                                    a.Group = null;
                                }
                            }
                        }

                        foreach (Group g in location.GroupsAtThisLocation)
                        {
                            foreach (Architect a in g.ArchitectsToRemove)
                            {
                                g.Architects.Remove(a);
                            }
                            g.ArchitectsToRemove.Clear();
                        }


                        d.ArchitectsToAdd = new EntityList<Architect>();

                        // TODO: Handle structure building based on population and number of structures, and number of structures city can support.

                        if (!location.IsSavingUpToSettle)
                        {
                            if (location.Wealth > 5000)
                            {
                                if (SettlementTypes.Contains(location.Type))
                                {
                                    int Build = 0;

                                    if (d.IsPrimary)
                                    {
                                        Build = Game1.r.Next(1, 3);
                                    }
                                    else
                                    {
                                        Build = Game1.r.Next(1, 7);
                                    }

                                    if (Build == 1)
                                    {
                                        int BuildingDecider = Game1.r.Next(1, 30);
                                        string BuildingType = "";
                                        int Windows = 0;
                                        int llOf5 = 0;

                                        List<string> LightingMethods = new List<string>();
                                        List<string> PrimarySmells = new List<string>();
                                        EntityList<Material> Materials = new EntityList<Material>();


                                        if (BuildingDecider < 15)
                                        {
                                            BuildingType = "house";
                                            Materials.Add(location.HomeCivilization.CulturalWood);
                                            Windows = Game1.r.Next(0, 2);
                                            location.Wealth = location.Wealth - 1000;
                                        }
                                        else if (BuildingDecider < 17)
                                        {
                                            BuildingType = "shrine";
                                            Windows = Game1.r.Next(0, 3);
                                            Materials.Add(location.HomeCivilization.CulturalStone);
                                            location.Wealth = location.Wealth - 2000;
                                        }
                                        else if (BuildingDecider < 19 && location.Library == null)
                                        {
                                            BuildingType = "library";
                                            Windows = Game1.r.Next(0, 4);
                                            Materials.Add(location.HomeCivilization.CulturalWood);
                                            location.Wealth = location.Wealth - 2000;
                                        }
                                        else if (BuildingDecider < 21)
                                        {
                                            BuildingType = "tavern";
                                            Windows = Game1.r.Next(0, 2);
                                            Materials.Add(location.HomeCivilization.CulturalWood);
                                            location.Wealth = location.Wealth - 2500;
                                        }
                                        else if (BuildingDecider < 22)
                                        {
                                            BuildingType = "forge";
                                            Windows = Game1.r.Next(0, 2);
                                            Materials.Add(location.HomeCivilization.CulturalStone);
                                            location.Wealth = location.Wealth - 3000;
                                        }
                                        else if (BuildingDecider < 23)
                                        {
                                            BuildingType = "watchtower";
                                            Windows = 0;
                                            Materials.Add(location.HomeCivilization.CulturalWood);
                                            Materials.Add(location.HomeCivilization.CulturalStone);
                                            location.Wealth = location.Wealth - 2500;
                                        }
                                        else if (location.Market == null)
                                        {
                                            BuildingType = "market";
                                            Windows = 0;
                                            Materials.Add(location.HomeCivilization.CulturalWood);
                                            Materials.Add(location.HomeCivilization.CulturalCloth);
                                            location.Wealth = location.Wealth - 2500;
                                        }
                                        else
                                        {
                                            BuildingType = "bighouse";
                                            Windows = Game1.r.Next(0, 5);
                                            Materials.Add(location.HomeCivilization.CulturalWood);
                                            location.Wealth = location.Wealth - 1500;
                                        }

                                        while (LightingMethods.Count == 0)
                                        {
                                            foreach (string S in location.PrimaryLightingStyles)
                                            {
                                                if (Game1.r.Next(1, 4) != 1)
                                                {
                                                    LightingMethods.Add(S);
                                                }
                                            }
                                        }


                                        //find an owner if you want lul
                                        EntityList<Group> PotentialGroups = new EntityList<Group>();

                                        if (BuildingType == "shrine")
                                        {
                                            foreach (Group g in location.GroupsAtThisLocation)
                                            {
                                                if (g.Type == "religious")
                                                {
                                                    PotentialGroups.Add(g);
                                                }
                                            }
                                        }
                                        else if (BuildingType == "market")
                                        {
                                            foreach (Group g in location.GroupsAtThisLocation)
                                            {
                                                if (g.Type == "trade")
                                                {
                                                    PotentialGroups.Add(g);
                                                }
                                            }
                                        }
                                        else if (BuildingType == "watchtower")
                                        {
                                            foreach (Group g in location.GroupsAtThisLocation)
                                            {
                                                if (g.Type == "mercenary" || g.Type == "military")
                                                {
                                                    PotentialGroups.Add(g);
                                                }
                                            }
                                        }
                                        else if (BuildingType != "bighouse")
                                        {
                                            foreach (Group g in location.GroupsAtThisLocation)
                                            {
                                                PotentialGroups.Add(g);
                                            }
                                        }

                                        Block DecidedBlock = d.DistrictMap[Game1.r.Next(0, 49)];

                                        Structure s = new Structure(BuildingType, new EntityList<Object>(), new EntityList<Room>(), DecidedBlock, Materials, PrimarySmells, LightingMethods, llOf5, Windows, (int)Math.Round(Cycle / 290304000));

                                        if (s.Type == "market")
                                        {
                                            location.Market = s;
                                        }
                                        else if (s.Type == "library")
                                        {
                                            location.Library = s;
                                        }

                                        if (PotentialGroups.Count == 0)
                                        {
                                            s.Owner = null;
                                        }
                                        else
                                        {
                                            s.Owner = PotentialGroups[Game1.r.Next(PotentialGroups.Count)];
                                        }

                                        if (s.Type != "house" && s.Type != "bighouse")
                                        {
                                            if (s.Owner == null)
                                            {
                                                HistoricalEvents.Add(string.Concat(Date, " ", s.Name, ", a ", s.Type, ", was founded by the people of ", location.Name));
                                            }
                                            else
                                            {
                                                HistoricalEvents.Add(string.Concat(Date, " ", s.Name, ", a ", s.Type, ", was founded by ", s.Owner.Name));
                                            }
                                        }

                                        DecidedBlock.Structures.Add(s);
                                    }
                                }
                            }
                        }

                        // TODO: Handle architect/architect group creating a new location

                        bool breaken = false;

                        //regular architects leaving to go make sites

                        foreach (Group g in location.GroupsAtThisLocation)
                        {
                            if (location.Government != g && location.Wealth > 10000 && location.IsSavingUpToSettle && location.TruePopulation() > 100 && location.ColonizationDesire > 0 && new List<string> { "town", "city", "camp", "village" }.Contains(location.Type))
                            {
                                //start looking for a new location to find

                                int Attempts = 0;

                                while (Attempts < 10)
                                {
                                    int XChange = Game1.r.Next(-5, 6);
                                    int ZChange = Game1.r.Next(-5, 6);

                                    if (!(location.Region.X + XChange < 0 || location.Region.X + XChange > Width - 1 || location.Region.Z + ZChange < 0 || location.Region.Z + ZChange > Length - 1))
                                    {
                                        if (WorldMap[(location.Region.X + XChange) + (location.Region.Z + ZChange) * Width].Biome != "ocean" && WorldMap[(location.Region.X + XChange) + (location.Region.Z + ZChange) * Width].Biome != "void" && WorldMap[(location.Region.X + XChange) + (location.Region.Z + ZChange) * Width].Biome != "snowpeak" && WorldMap[(location.Region.X + XChange) + (location.Region.Z + ZChange) * Width].Biome != "mountain" && WorldMap[(location.Region.X + XChange) + (location.Region.Z + ZChange) * Width].MyLocation == null && WorldMap[(location.Region.X + XChange) + (location.Region.Z + ZChange) * Width].Blight == Purity)
                                        {
                                            // Check if group has at least one member from the humanoid races
                                            bool hasHumanoid = g.Architects.Any(a => HumanoidRaces.Contains(a.Race));

                                            if (hasHumanoid)
                                            {
                                                // Rest of the existing code for settling...
                                                // Ensure primary race is one of the humanoid races present
                                                Race primaryRace = g.Architects.First(a => HumanoidRaces.Contains(a.Race)).Race;

                                                // Code for wealth deduction, race count, and other settlement logic
                                                int PopulationFollowing = Game1.r.Next(location.TruePopulation() / 10, location.TruePopulation() / 5);

                                                if (g.Architects.Count() * 200 > 15000)
                                                {
                                                    location.Wealth -= 14500;
                                                }
                                                else
                                                {
                                                    location.Wealth -= g.Architects.Count() * 200;
                                                }

                                                // Dictionary to keep track of race counts
                                                Dictionary<Race, int> raceCounts = new Dictionary<Race, int>();

                                                // Counting the races
                                                foreach (Architect a in g.Architects)
                                                {
                                                    if (!raceCounts.ContainsKey(a.Race))
                                                    {
                                                        raceCounts[a.Race] = 0;
                                                    }
                                                    raceCounts[a.Race]++;
                                                }

                                                // Now primaryRace holds the Race object with the highest count

                                                LocationBuilderPacket l = new LocationBuilderPacket(g, location.Region.X + XChange, location.Region.Z + ZChange, "camp", primaryRace, PopulationFollowing, location.MaxColonizationDesire - 1, location.HomeCivilization, new EntityList<Object>(), location, "none");
                                                LocationBuilderPackets.Add(l);

                                                // Adjusting population following the settlement
                                                if (d.UnplacedPopulation > PopulationFollowing)
                                                {
                                                    d.UnplacedPopulation = (d.UnplacedPopulation - PopulationFollowing) + 5;
                                                }
                                                else
                                                {
                                                    d.UnplacedPopulation = 5;
                                                }

                                                location.ColonizationDesire = location.ColonizationDesire - 1;
                                                location.GroupsAtThisLocationToRemove.Add(g);
                                                location.IsSavingUpToSettle = false;

                                                breaken = true;
                                                break;
                                            }
                                            else
                                            {
                                                continue; // Continue to the next iteration if no settlement occurs
                                            }
                                        }
                                    }

                                    Attempts++;
                                }
                                if (breaken == true)
                                {
                                    break;
                                }
                            }
                        }
                        foreach (Group g in location.GroupsAtThisLocationToRemove)
                        {
                            location.GroupsAtThisLocation.Remove(g);
                        }

                        //warlock or sorcerer goes to build a spire

                        foreach (Architect a in d.Architects)
                        {
                            if (!a.IsCalamity && (a.Profession == "warlock" || a.Profession == "sorcerer") && location.Type != "spire" && a.BuiltSpire == false)
                            {
                                //hunt an unnoccupied location

                                a.BuiltSpire = true;

                                bool Found = false;

                                int X = 0;
                                int Z = 0;

                                while (!Found)
                                {
                                    X = Game1.r.Next(Width);
                                    Z = Game1.r.Next(Length);

                                    if (WorldMap[X + Z * Width].MyLocation == null && WorldMap[X + Z * Width].Biome != "void")
                                    {
                                        Found = true;
                                    }
                                }

                                LocationBuilderPacket l = new LocationBuilderPacket(a, X, Z, "spire", GetRace(""), 0, 0, a.Location.HomeCivilization, new EntityList<Object>(), location, "none");
                                LocationBuilderPackets.Add(l);
                            }
                        }

                        //adventuring group builds an outpost to store their items
                        EntityList<Group> AdvGroups = new EntityList<Group>();
                        foreach (Group g in Groups)
                        {
                            if (g.Type == "mercenary")
                            {
                                AdvGroups.Add(g);
                            }
                        }
                        foreach (Group g in location.GroupsAtThisLocation)
                        {
                            if (g.Type == "mercenary" && g.Architects.Count >= 3)
                            {
                                if (g.Base.Type != "outpost")
                                {
                                    //find a location
                                    List<(int, int)> PossibleLocations = new List<(int, int)>();

                                    for (int SearchingX = -10; SearchingX < 10; SearchingX++)
                                    {
                                        for (int SearchingZ = -10; SearchingZ < 10; SearchingZ++)
                                        {
                                            if (g.Leader.Location.X + SearchingX > 0 && g.Leader.Location.X + SearchingX < Width && g.Leader.Location.Z + SearchingZ > 0 && g.Leader.Location.Z + SearchingZ < Length)
                                            {
                                                if (WorldMap[(g.Leader.Location.X + SearchingX) + (g.Leader.Location.Z + SearchingZ) * Length].MyLocation == null && WorldMap[(g.Leader.Location.X + SearchingX) + (g.Leader.Location.Z + SearchingZ) * Length].Biome != "ocean" && WorldMap[(g.Leader.Location.X + SearchingX) + (g.Leader.Location.Z + SearchingZ) * Length].Biome != "void")
                                                {
                                                    PossibleLocations.Add((g.Leader.Location.X + SearchingX, g.Leader.Location.Z + SearchingZ));
                                                }
                                            }
                                        }
                                    }

                                    if (PossibleLocations.Count > 0)
                                    {
                                        (int, int) Coords = PossibleLocations[Game1.r.Next(PossibleLocations.Count)];
                                        LocationBuilderPacket l = new LocationBuilderPacket(g, Coords.Item1, Coords.Item2, "outpost", GetRace(""), 0, 0, g.Leader.Location.HomeCivilization, new EntityList<Object>(), location, "none");
                                        location.GroupsAtThisLocationToRemove.Add(g);

                                        foreach(Architect a in g.Architects)
                                        {
                                            a.Level = 4;
                                            a.Strength = Math.Max(4, a.Strength);
                                        }

                                        if (location.Government == g)
                                        {
                                            location.Government = null;
                                        }
                                        LocationBuilderPackets.Add(l);
                                    }
                                }
                            }
                        }

                        foreach (Group g in location.GroupsAtThisLocationToRemove)
                        {
                            location.GroupsAtThisLocation.Remove(g);
                        }
                        location.GroupsAtThisLocationToRemove = new EntityList<Group>();



                        //colossal creates a legendary artifact and builds a sanctum to protect it

                        foreach (Architect a in Colossals)
                        {
                            if (!a.HasMadeALegendaryArtifact && r.Next(1, 10000 * MonthToDayConstant) == 1 && UndiscoveredLegendarySpells.Count > 1)
                            {
                                Entity Spell = UndiscoveredLegendarySpells[r.Next(UndiscoveredLegendarySpells.Count)];
                                UndiscoveredLegendarySpells.Remove(Spell);
                                DiscoveredLegendarySpells.Add(Spell);

                                Object o = new Object("", Game1.PossibleMagicalItems[r.Next(Game1.PossibleMagicalItems.Count)], new EntityList<Material> { Metals[r.Next(Metals.Count)] }, false, false, null, a, 5, false, null, null, null, false);
                                o.Name = GenerateUniqueName("1S" + Game1.r.Next(2, 4) + "s1w", o);
                                o.SpecialKnowledge = Spell;
                                o.Owner = a;

                                string MagicPhrase = "";

                                if (Spell.Metadata == "ethereal rupture")
                                {
                                    MagicPhrase = "ravaging the land with fractal exposure";
                                }
                                else if (Spell.Metadata == "emergence")
                                {
                                    MagicPhrase = "summoning the fallen";
                                }
                                else if (Spell.Metadata == "eternal bind")
                                {
                                    MagicPhrase = "enslaving all to its will";
                                }
                                else if (Spell.Metadata == "expunge")
                                {
                                    MagicPhrase = "banishing legends and the memories of them";
                                }
                                else if (Spell.Metadata == "echo")
                                {
                                    MagicPhrase = "assembling an echo of a legend";
                                }

                                HistoricalEvents.Add(string.Concat(Date, " ", a.Name, " created ", o.Name, ", a legendary ", o.Materials[0].Name, " ", o.Type, " capable of ", MagicPhrase, "."));

                                //find a sanctum location

                                bool Found = false;

                                int X = 0;
                                int Z = 0;

                                while (!Found)
                                {
                                    X = Game1.r.Next(Width);
                                    Z = Game1.r.Next(Length);

                                    if (WorldMap[X + Z * Width].MyLocation == null && WorldMap[X + Z * Width].Biome != "void" && WorldMap[X + Z * Width].Biome != "ocean")
                                    {
                                        Found = true;
                                    }
                                }

                                LocationBuilderPacket l = new LocationBuilderPacket(a, X, Z, "sanctum", GetRace(""), 0, 0, null, new EntityList<Object> { o }, location, "none");
                                LocationBuilderPackets.Add(l);
                            }
                        }


                        //start saving up to settle


                        if (Game1.r.Next(1, 50 * MonthToDayConstant) == 1 && location.TruePopulation() > 120 && location.ColonizationDesire > 0)
                        {
                            location.IsSavingUpToSettle = true;
                        }
                        if (location.TruePopulation() < 100)
                        {
                            location.IsSavingUpToSettle = false;
                        }

                        foreach (Architect a in d.ArchitectsToRemove)
                        {
                            d.Architects.Remove(a);
                        }
                        d.ArchitectsToRemove = new EntityList<Architect>();

                        //Architects die lul

                        foreach (Architect a in d.Architects)
                        {
                            //immortality boons

                            if (a.IsImmortal && a.RecievedImmortalityBuff == false)
                            {
                                a.DoIDieOfOldAge = false;
                                a.RecievedImmortalityBuff = true;
                                a.TerminalAge = a.TerminalAge + (Game1.r.Next(40, 200));
                            }
                        }

                        foreach (Architect a in d.ArchitectsToRemove)
                        {
                            d.Architects.Remove(a);
                        }
                        d.ArchitectsToRemove = new EntityList<Architect>();


                        foreach (Group g in location.GroupsAtThisLocation)
                        {
                            foreach (Architect a in g.ArchitectsToRemove)
                            {
                                g.Architects.Remove(a);
                            }
                        }
                        foreach (Group g in GroupsToRemove)
                        {
                            Groups.Remove(g);
                            location.GroupsAtThisLocation.Remove(g);
                        }
                        GroupsToRemove = new EntityList<Group>();



                        // TODO: Handle Architect/architect groups moving into/taking structures.

                        // TODO: Handle craftsmen making stuff and instruments
                        // TODO: Handle musicians creating new musical forms
                        // TODO: Handle travel. Remember to remove the group's leader from the location when the leader moves and do the FIFTY OTHER THING  RWHIOWEH RIHIDSAFBISUOEFWSEY



                    }

                    //Handle location type changes

                    if (location.Type == "camp" && location.TruePopulation() > 10)
                    {
                        location.Type = "village";
                    }
                    if (location.Type == "village" && location.TruePopulation() > 200)
                    {
                        location.Type = "town";
                    }
                    if (location.Type == "town" && location.TruePopulation() > 500)
                    {
                        location.Type = "city";
                    }



                    //captiols build special procgen sites


                    if (r.Next(1, 1000 * MonthToDayConstant) == 1 && location.IsCapitol && SettlementTypes.Contains(location.Type))
                    {
                        // Decide what type of structure you're going to make
                        string SType = new List<string>() { "observatory", "library", "conservatory", "prison", "tomb", "gallery", "armory" }[r.Next(7)];

                        // List to store all valid locations
                        List<(int, int)> validLocations = new List<(int, int)>();

                        // Search for valid locations
                        for (int SearchingX = -10; SearchingX <= 10; SearchingX++)
                        {
                            for (int SearchingZ = -10; SearchingZ <= 10; SearchingZ++)
                            {
                                int X = location.X + SearchingX;
                                int Z = location.Z + SearchingZ;
                                if (X >= 0 && X < Width && Z >= 0 && Z < Length && WorldMap[X + Z * Width].MyLocation == null && WorldMap[X + Z * Width].Biome != "void")
                                {
                                    validLocations.Add((X, Z));
                                }
                            }
                        }

                        // If there are valid locations, pick a random one
                        if (validLocations.Count > 0)
                        {
                            var selectedLocation = validLocations[r.Next(validLocations.Count)];
                            int selectedX = selectedLocation.Item1;
                            int selectedZ = selectedLocation.Item2;

                            // Decide people to send there
                            EntityList<Architect> PossibleArchitects = new EntityList<Architect>();

                            foreach (District d in location.Districts)
                            {
                                foreach (Architect a in d.Architects)
                                {
                                    if (!a.IsCalamity && !a.IsColossal /*lol a serpent was tryna build an observatory :( */ && !(location.Government is Architect && (Architect)location.Government == a) && a.Group == null)
                                    {
                                        PossibleArchitects.Add(a);
                                    }
                                }
                            }

                            EntityList<Architect> DecidedArchitects = new EntityList<Architect>();
                            int Amount = r.Next(1, 6);

                            if (PossibleArchitects.Count >= Amount)
                            {
                                for (int i = 0; i < Amount; i++)
                                {
                                    int index = r.Next(PossibleArchitects.Count);
                                    DecidedArchitects.Add(PossibleArchitects[index]);
                                    PossibleArchitects.RemoveAt(index);
                                }
                            }
                            else
                            {
                                DecidedArchitects = new EntityList<Architect>(PossibleArchitects);
                            }

                            if (DecidedArchitects.Count > 0)
                            {
                                LocationBuilderPacket l = new LocationBuilderPacket(DecidedArchitects[0], selectedX, selectedZ, SType, GetRace(""), 0, r.Next(3), DecidedArchitects[0].HomeLocation.HomeCivilization, new EntityList<Object>(), location, "none");
                                LocationBuilderPackets.Add(l);
                            }
                        }
                    }


                    //update outcast civ data

                    Dictionary<string, string> OutcastCivToStructure = new Dictionary<string, string>
                            {
                                { "druid", "preserve" },
                                { "pirate", "cove" },
                                { "cultist", "monastery" },
                                { "anarchist", "commune" },
                                { "scavenger", "hoard" }
                            };

                    string OutcastCivType = "";

                    foreach (var kvp in OutcastCivToStructure)
                    {
                        if (kvp.Value == location.Type)
                        {
                            OutcastCivType = kvp.Key;
                            break;
                        }
                    }

                    if (OutcastCivType != "") //this also means that theyre an outcast civ in general
                    {
                        //recruitment

                        // Define the outcast civilization types and their professions
                        Dictionary<string, List<string>> outcastProfessions = new Dictionary<string, List<string>>()
                        {
                            { "druid", new List<string> { "gardener", "druidcrafter", "archdruid" } },
                            { "scavenger", new List<string> { "salvager", "constructor", "scraplord" } },
                            { "cultist", new List<string> { "cultist", "priest", "intermediary" } },
                            { "pirate", new List<string> { "swashbuckler", "deadeye", "captain" } },
                            { "anarchist", new List<string> { "disruptor", "bomber", "inspiration" } }
                        };

                        if (r.Next(1, 100 * MonthToDayConstant) == 1)
                        {
                            if (r.Next(2) == 0)
                            {
                                HistoricalEvents.Add(Date + " A group of " + OutcastCivType + "s migrated to " + location.Name + ".");
                                location.LocationHistoricalEvents.Add(Date + " A group of " + OutcastCivType + "s migrated to " + location.Name + ".");
                                location.Districts[0].UnplacedPopulation += r.Next(15, 30);
                            }
                            else
                            {
                                EntityList<Architect> PossibleArch = new EntityList<Architect>();
                                foreach (Architect a in AllArchitects)
                                {
                                    if (a.Group == null && !Calamity.Contains(a) && a.IsAlive && a.Location != null && a.Location.HomeCivilization != null && a.Profession != "sorcerer" && a.Profession != "warlock")
                                    {
                                        PossibleArch.Add(a);
                                    }
                                }

                                if (PossibleArch.Count > 0)
                                {
                                    Architect Migrator = PossibleArch[r.Next(PossibleArch.Count)];
                                    Migrator.NextMigrationLocation = location;

                                    HistoricalEvents.Add(Date + " " + Migrator.Name + " felt called by the " + OutcastCivType + "s of " + location.Name + " and decided to migrate there.");
                                    location.LocationHistoricalEvents.Add(Date + " " + Migrator.Name + " felt called by the " + OutcastCivType + "s of " + location.Name + " and decided to migrate there.");

                                    // Assign profession based on OutcastCivType
                                    if (outcastProfessions.ContainsKey(OutcastCivType))
                                    {
                                        List<string> professions = outcastProfessions[OutcastCivType];
                                        int professionRoll = r.Next(100);

                                        if (professionRoll < 80)
                                        {
                                            Migrator.Profession = professions[0];
                                        }
                                        else if (professionRoll < 95)
                                        {
                                            Migrator.Profession = professions[1];
                                        }
                                        else
                                        {
                                            Migrator.Profession = professions[2];
                                        }
                                    }

                                    if (OutcastCivType == "druid")
                                    {
                                        Migrator.Clothing.Clear();

                                        if (Migrator.Sex == "female")
                                        {
                                            Migrator.Clothing.Add(new Object(null, "brassiere", new EntityList<Material>() { Fibers[r.Next(Fibers.Count)] }, null));
                                        }
                                        Migrator.Clothing.Add(new Object(null, "undergarment", new EntityList<Material>() { Fibers[r.Next(Fibers.Count)] }, null));

                                        Migrator.AddCulturalClothing(Migrator.Location.HomeCivilization.CulturalHeadwear, Fibers[r.Next(Fibers.Count)]);
                                        Migrator.AddCulturalClothing(Migrator.Location.HomeCivilization.CulturalNeckwear, Fibers[r.Next(Fibers.Count)]);
                                        Migrator.AddCulturalClothing(Migrator.Location.HomeCivilization.CulturalBodywear, Fibers[r.Next(Fibers.Count)]);
                                        Migrator.AddCulturalClothing(Migrator.Location.HomeCivilization.CulturalLegwear, Fibers[r.Next(Fibers.Count)]);
                                        Migrator.AddCulturalClothing(Migrator.Location.HomeCivilization.CulturalHandwear, Fibers[r.Next(Fibers.Count)]);
                                        Migrator.AddCulturalClothing(Migrator.Location.HomeCivilization.CulturalFootwear, Fibers[r.Next(Fibers.Count)]);
                                    }
                                }
                            }
                        }


                        //spread

                        if (location.TruePopulation() > 80 && location.ColonizationDesire > 0)
                        {
                            // List to store all valid locations
                            List<(int X, int Z, string Dockside)> validLocations = new List<(int X, int Z, string Dockside)>();

                            int centerX = location.X;
                            int centerZ = location.Z;

                            // Iterate over a 9x9 area centered at location.X and location.Z
                            for (int TryX = centerX - 4; TryX <= centerX + 4; TryX++)
                            {
                                for (int TryZ = centerZ - 4; TryZ <= centerX + 4; TryZ++)
                                {
                                    // Check a square of size 5 centered around (TryX, TryZ)
                                    bool validLocation = true;
                                    for (int i = TryX - 3; i <= TryX + 3 && validLocation; i++)
                                    {
                                        for (int j = TryZ - 3; j <= TryZ + 3 && validLocation; j++)
                                        {
                                            // Check if any region inside the area's Region.Location is not equal to null
                                            if (i < 0 || i >= Width || j < 0 || j >= Length ||
                                                WorldMap[i + j * Width].Biome == "void" ||
                                                WorldMap[i + j * Width].MyLocation != null)
                                            {
                                                validLocation = false;
                                            }
                                        }
                                    }

                                    if (validLocation)
                                    {
                                        bool SpecificConditionsMet = false;
                                        string dockside = null;

                                        if (OutcastCivType == "druid")
                                        {
                                            SpecificConditionsMet = (WorldMap[TryX + TryZ * Width].Biome == "forest");
                                        }
                                        else if (OutcastCivType == "pirate")
                                        {
                                            SpecificConditionsMet = (WorldMap[TryX + TryZ * Width].Biome == "ocean" &&
                                                ((WorldMap[(TryX) + (TryZ + 1) * Width].Biome != "ocean") ||
                                                 (WorldMap[(TryX) + (TryZ - 1) * Width].Biome != "ocean") ||
                                                 (WorldMap[(TryX - 1) + (TryZ) * Width].Biome != "ocean") ||
                                                 (WorldMap[(TryX + 1) + (TryZ) * Width].Biome != "ocean")));

                                            if (SpecificConditionsMet)
                                            {
                                                List<string> docksideOptions = new List<string>();
                                                if (WorldMap[(TryX) + (TryZ + 1) * Width].Biome != "ocean")
                                                {
                                                    docksideOptions.Add("south");
                                                }
                                                if (WorldMap[(TryX) + (TryZ - 1) * Width].Biome != "ocean")
                                                {
                                                    docksideOptions.Add("north");
                                                }
                                                if (WorldMap[(TryX - 1) + (TryZ) * Width].Biome != "ocean")
                                                {
                                                    docksideOptions.Add("west");
                                                }
                                                if (WorldMap[(TryX + 1) + (TryZ) * Width].Biome != "ocean")
                                                {
                                                    docksideOptions.Add("east");
                                                }

                                                if (docksideOptions.Count > 0)
                                                {
                                                    dockside = docksideOptions[Game1.r.Next(docksideOptions.Count)];
                                                }
                                            }
                                        }
                                        else if (OutcastCivType == "cultist")
                                        {
                                            SpecificConditionsMet = (WorldMap[TryX + TryZ * Width].Biome == "snowpeak");
                                        }
                                        else if (OutcastCivType == "anarchist")
                                        {
                                            SpecificConditionsMet = (WorldMap[TryX + TryZ * Width].Biome == "desert");
                                        }
                                        else if (OutcastCivType == "scavenger")
                                        {
                                            SpecificConditionsMet = (WorldMap[TryX + TryZ * Width].Biome != "ocean");
                                        }

                                        if (SpecificConditionsMet)
                                        {
                                            validLocations.Add((TryX, TryZ, dockside));
                                        }
                                    }
                                }
                            }

                            if (validLocations.Count > 0)
                            {
                                var (FoundX, FoundZ, FoundDockside) = validLocations[Game1.r.Next(validLocations.Count)];
                                LocationBuilderPacket l = new LocationBuilderPacket(null, FoundX, FoundZ, location.Type, GetRace(""), r.Next(4, 10), location.MaxColonizationDesire - 1, location.HomeCivilization, new EntityList<Object>(), location, FoundDockside);
                                LocationBuilderPackets.Add(l);

                            }
                            else
                            {
                                // We won't do anything this time, it's cool
                            }
                        }



                    }




                    //districts you waited to place

                    foreach (District d in location.DistrictsToAdd)
                    {
                        location.Districts.Add(d);
                    }

                    location.DistrictsToAdd = new EntityList<District>();
                }

                //Place the locations you waited to place

                // Pre-process to ensure unique (X, Z) coordinates for LocationBuilderPackets
                HashSet<(int, int)> usedCoordinates = new HashSet<(int, int)>();

                foreach (LocationBuilderPacket l in LocationBuilderPackets)
                {
                    // Ensure the initial coordinates are unique and valid
                    while (usedCoordinates.Contains((l.X, l.Z)) || l.X <= 0 || l.X >= Width || l.Z <= 0 || l.Z >= Length || WorldMap[l.X + l.Z * Width].Biome == "void" || WorldMap[l.X + l.Z * Width].MyLocation != null)
                    {
                        // Adjust X and Z by small amounts
                        l.X = Math.Max(1, Math.Min(Width - 1, l.X + Game1.r.Next(-1, 2)));
                        l.Z = Math.Max(1, Math.Min(Length - 1, l.Z + Game1.r.Next(-1, 2)));
                    }
                    usedCoordinates.Add((l.X, l.Z));
                }


                foreach (LocationBuilderPacket l in LocationBuilderPackets)
                {
                    //build locations
                    Location NewLocation = new Location(l.Type, l.PrimaryRace, l.MiscPopulation, Game1.r.Next(1000, 4000), l.ColonizationDesire, l.X, l.Z, l.HomeCivilization, WorldMap[l.X + l.Z * Width], l.Dockside);

                    if (l.Type == "camp")
                    {
                        HistoricalEvents.Add(string.Concat(Date, " After preparing for years, ", l.Government.Name, " left ", l.BaseLocation.Name, " with a following of ", l.MiscPopulation, " people and founded ", NewLocation.Name, "."));
                    }
                    else if (l.Type == "spire")
                    {
                        HistoricalEvents.Add(string.Concat(Date, " ", l.Government.Name, " left ", l.BaseLocation.Name, " and constructed a glorious spire, ", NewLocation.Name));
                        ((Architect)l.Government).OppositionTags.Add("intruders");
                        ((Architect)l.Government).HomeLocation = NewLocation;
                    }
                    else if (l.Type == "sanctum")
                    {
                        HistoricalEvents.Add(string.Concat(Date, " ", l.Government.Name, " constructed ", NewLocation.Name, " to house ", l.Artifacts[0].Name, "."));
                    }
                    else if (l.Type == "outpost")
                    {
                        foreach (Architect a in ((Group)l.Government).Architects)
                        {
                            a.OppositionTags.Add("intruders");
                        }
                    }
                    else if (CalamityStructures.Contains(l.Type))
                    {
                        HistoricalEvents.Add(Date + " " + l.Government.Name + " constructed " + NewLocation.Name + " to base his operations.");
                        ((Architect)(l.Government)).InteractionLocation = NewLocation;

                        ((Architect)(l.Government)).KitOutArchitect(((Architect)(l.Government)).Profession);
                    }
                    else if (l.Type == "preserve")
                    {
                        if (l.BaseLocation != null && l.BaseLocation.Type == l.Type)
                        {
                            HistoricalEvents.Add(string.Concat(Date, " ", l.BaseLocation.Name, ", expanded their influence to a new preserve, ", NewLocation.Name, "."));
                        }
                        else
                        {
                            HistoricalEvents.Add(string.Concat(Date, " ", l.Government.Name, ", distraught about the destructive nature of the energy people around him, sought to build ", NewLocation.Name, " to preserve part of the island."));
                            NewLocation.IsCapitol = true;
                        }
                    }
                    else if (l.Type == "cove")
                    {
                        if (l.BaseLocation != null && l.BaseLocation.Type == l.Type)
                        {
                            HistoricalEvents.Add(string.Concat(Date, " ", l.BaseLocation.Name, ", expanded their influence to a new cove, ", NewLocation.Name, "."));
                        }
                        else
                        {
                            HistoricalEvents.Add(string.Concat(Date, " ", l.Government.Name, ", desiring the great wealth of the surrounding trade, built ", NewLocation.Name, " to base a massive piracy operation."));
                            NewLocation.IsCapitol = true;
                        }
                    }
                    else if (l.Type == "monastery")
                    {
                        if (l.BaseLocation != null && l.BaseLocation.Type == l.Type)
                        {
                            HistoricalEvents.Add(string.Concat(Date, " ", l.BaseLocation.Name, ", expanded their influence to a new monastery, ", NewLocation.Name, "."));
                        }
                        else
                        {
                            HistoricalEvents.Add(string.Concat(Date, " ", l.Government.Name, ", in awe of a beautiful creature, constructed ", NewLocation.Name, " to honor it and its legacy."));
                            NewLocation.IsCapitol = true;
                        }
                    }
                    else if (l.Type == "commune")
                    {
                        if (l.BaseLocation != null && l.BaseLocation.Type == l.Type)
                        {
                            HistoricalEvents.Add(string.Concat(Date, " ", l.BaseLocation.Name, ", expanded their influence to a new commune, ", NewLocation.Name, "."));
                        }
                        else
                        {
                            HistoricalEvents.Add(string.Concat(Date, " ", l.Government.Name, ", in hatred of the regulations of society, decided to construct ", NewLocation.Name, ", a commune of complete freedom and expression."));
                            NewLocation.IsCapitol = true;
                        }
                    }
                    else if (l.Type == "hoard")
                    {
                        if (l.BaseLocation != null && l.BaseLocation.Type == l.Type)
                        {
                            HistoricalEvents.Add(string.Concat(Date, " ", l.BaseLocation.Name, ", expanded their influence to a new hoard, ", NewLocation.Name, "."));
                        }
                        else
                        {
                            HistoricalEvents.Add(string.Concat(Date, " ", l.Government.Name, " began to tear apart the land for its treasures, and constructed ", NewLocation.Name, " to recruit others and scavenge the entire continent."));
                            NewLocation.IsCapitol = true;
                        }
                    }
                    else if (ProcgenStructures.Contains(l.Type))
                    {
                        NewLocation.Layout = new List<string>() { "hallway", "archway", "pyramid", "toroid", "towers" }[r.Next(5)];

                        int SX = Game1.r.Next(2, 5);
                        int SZ = Game1.r.Next(2, 5);

                        string c = new List<string>() { "brown", "black", "white", "gray", "maroon" }[Game1.r.Next(5)];

                        NewLocation.Color = c;

                        Structure s = new Structure(NewLocation.Type, l.Artifacts, new EntityList<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new EntityList<Material>() { l.HomeCivilization.CulturalStone }, new List<string>(), new List<string> { "lanterns" }, 3, 5, (int)Math.Round(Cycle / 290304000));

                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                    }

                    AllLocations.Add(NewLocation);
                    NewLocation.UnplacedArtifacts = l.Artifacts;
                    NewLocation.Government = l.Government;

                    if (NewLocation.Type == "camp")
                    {
                        ClaimSwathOfTerritory(NewLocation.HomeCivilization, l.X, l.Z, 2);
                    }

                    WorldMap[l.X + l.Z * Width].MyLocation = NewLocation;
                    WorldMap[l.X + l.Z * Width].Events = new EntityList<InteractableEvent>();

                    if (l.Government is Group)
                    {
                        NewLocation.GroupsAtThisLocation.Add((Group)l.Government);

                        foreach (Architect a in ((Group)l.Government).Architects)
                        {
                            a.NextMigrationLocation = NewLocation;
                        }

                        ((Group)l.Government).Base = NewLocation;
                    }
                    else if (l.Government is Architect && ((Architect)l.Government).Profession != "soverign" && ((Architect)l.Government).Profession != "heart")
                    {
                        ((Architect)l.Government).NextMigrationLocation = NewLocation;
                    }

                    //special structures need special stuff
                    if (l.Type == "camp")
                    {
                        //well
                        int SX = Game1.r.Next(2, 5);
                        int SZ = Game1.r.Next(2, 5);
                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Objects.Add(new Object(null, "well", new EntityList<Material> { NewLocation.HomeCivilization.CulturalStone }, true, true, null, null, 255, false, null, null, null, false));

                        //prism
                        Block chosenBlock = NewLocation.Districts[0].DistrictMap[Game1.r.Next(0, 49)];
                        Structure Prism = new Structure("prism", l.Artifacts, new EntityList<Room>(), chosenBlock, new EntityList<Material> { NewLocation.HomeCivilization.CulturalStone }, new List<string>(), new List<string> { Game1.LightingStyles[Game1.r.Next(Game1.LightingStyles.Count)] }, Game1.r.Next(0, 5), Game1.r.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                        Prism.Name = GenerateUniqueName("1W 1S2s", Prism);
                        NewLocation.Prism = Prism;

                        chosenBlock.Structures.Add(Prism);
                    }
                    if (l.Type == "spire")
                    {
                        int SX = Game1.r.Next(2, 5);
                        int SZ = Game1.r.Next(2, 5);

                        //material

                        Material m;

                        if (l.Government is Architect)
                        {
                            if (((Architect)l.Government).Race == GetRace("nightfell"))
                            {
                                m = Darkstone;
                            }
                            else
                            {
                                m = Illuminite;
                            }
                        }
                        else
                        {
                            if (((Group)l.Government).Leader.Race == GetRace("nightfell"))
                            {
                                m = Darkstone;
                            }
                            else
                            {
                                m = Illuminite;
                            }
                        }

                        Structure s = new Structure("spire", l.Artifacts, new EntityList<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new EntityList<Material>() { m }, new List<string>(), new List<string> { "crystals" }, 3, 0, (int)Math.Round(Cycle / 290304000));

                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                    }
                    else if (l.Type == "outpost")
                    {
                        int SX = Game1.r.Next(2, 5);
                        int SZ = Game1.r.Next(2, 5);

                        Material m = Stones[Game1.r.Next(Stones.Count)];

                        Structure s = new Structure("outpost", l.Artifacts, new EntityList<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new EntityList<Material>() { m }, new List<string>(), new List<string> { "torches" }, 3, 5, (int)Math.Round(Cycle / 290304000));

                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                    }
                    else if (l.Type == "sanctum")
                    {
                        int SX = Game1.r.Next(2, 5);
                        int SZ = Game1.r.Next(2, 5);

                        Structure s = new Structure("sanctum", l.Artifacts, new EntityList<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new EntityList<Material>() { Archaeon }, new List<string>(), new List<string> { "crystals" }, 3, 999, (int)Math.Round(Cycle / 290304000));

                        ((Architect)l.Government).HomeLocation = NewLocation;

                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                    }
                    else if (CalamityStructures.Contains(l.Type))
                    {
                        int SX = Game1.r.Next(2, 5);
                        int SZ = Game1.r.Next(2, 5);

                        Structure s = new Structure(l.Type, l.Artifacts, new EntityList<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new EntityList<Material>() { Stones[r.Next(Stones.Count)] }, new List<string>(), new List<string> { "torches" }, 3, 0, (int)Math.Round(Cycle / 290304000));

                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                    }
                    else if (l.Type == "monastery")
                    {
                        int SX = Game1.r.Next(2, 5);
                        int SZ = Game1.r.Next(2, 5);

                        Structure s = new Structure(l.Type, l.Artifacts, new EntityList<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new EntityList<Material>() { Stones[r.Next(Stones.Count)] }, new List<string>(), new List<string> { "candles" }, 3, 0, (int)Math.Round(Cycle / 290304000));

                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                    }
                    else if (l.Type == "commune")
                    {
                        int SX = Game1.r.Next(2, 5);
                        int SZ = Game1.r.Next(2, 5);

                        Structure s = new Structure(l.Type, l.Artifacts, new EntityList<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new EntityList<Material>() { Stones[r.Next(Stones.Count)] }, new List<string>(), new List<string> { "torches" }, 3, 0, (int)Math.Round(Cycle / 290304000));

                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                    }
                    else if (l.Type == "hoard")
                    {
                        int Count = r.Next(4, 8);
                        for (int i = 0; i < Count; i++)
                        {
                            int SX = Game1.r.Next(2, 5);
                            int SZ = Game1.r.Next(2, 5);

                            Structure s = new Structure(l.Type, l.Artifacts, new EntityList<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new EntityList<Material>() { Stones[r.Next(Stones.Count)] }, new List<string>(), new List<string> { "none" }, 3, 0, (int)Math.Round(Cycle / 290304000));

                            NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                        }
                    }
                    else if (l.Type == "cove")
                    {
                        int structureCount = Game1.r.Next(8, 13); // Generate a random number of structures between 8 and 12

                        for (int i = 0; i < structureCount; i++)
                        {
                            int SX = Game1.r.Next(7); // Random X coordinate within the 7x7 grid
                            int SZ = Game1.r.Next(7); // Random Z coordinate within the 7x7 grid

                            Block selectedBlock = NewLocation.Districts[0].DistrictMap[SX + SZ * 7];
                            string structureType = (selectedBlock.Biome == "ocean") ? "ship" : "dock";

                            Structure s = new Structure(
                                structureType,
                                new EntityList<Object>(),
                                new EntityList<Room>(),
                                selectedBlock,
                                new EntityList<Material>() { Woods[Game1.r.Next(Woods.Count)] },
                                new List<string>(),
                                new List<string> { "none" },
                                3,
                                0, (int)Math.Round(Cycle / 290304000)
                            );

                            if (l.Artifacts.Count > 0)
                            {
                                s.HistoricalObjects.AddRange(l.Artifacts);
                                l.Artifacts.Clear();
                            }

                            selectedBlock.Structures.Add(s);
                        }
                    }
                    if (l.Type == "garrison")
                    {
                        if (l.PrimaryRace.Name == "shade")
                        {
                            for (int i = 0; i < Game1.r.Next(10, 20); i++)
                            {
                                Block chosenBlock = NewLocation.Districts[0].DistrictMap[Game1.r.Next(0, 49)];
                                Structure scum = new Structure("scum", new EntityList<Object>(), new EntityList<Room>(), chosenBlock, new EntityList<Material> { ShadeSludge }, new List<string> { l.HomeCivilization.CulturalGemstone.Name }, new List<string> { "veins" }, Game1.r.Next(0, 5), Game1.r.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                chosenBlock.Structures.Add(scum);
                            }
                        }
                        else if (l.PrimaryRace.Name == "isofractal" || l.PrimaryRace.Name == "photonexus")
                        {
                            int scaffoldCount = Game1.r.Next(3, 5); // Reduce the total number of scaffolds
                            HashSet<int> builtBlocks = new HashSet<int>(); // To avoid duplicate structures

                            for (int i = 0; i < scaffoldCount; i++)
                            {
                                int x = Game1.r.Next(0, 7);
                                int z = Game1.r.Next(0, 7);

                                if (x == 3 && z == 3) continue; // Skip the center block

                                // Create and place the initial scaffold structure
                                int index = x + z * 7;
                                if (!builtBlocks.Contains(index))
                                {
                                    Block chosenBlock = NewLocation.Districts[0].DistrictMap[index];
                                    Structure scaffold = new Structure("scaffold", new EntityList<Object>(), new EntityList<Room>(), chosenBlock, new EntityList<Material> { l.HomeCivilization.CulturalGemstone }, new List<string> { l.HomeCivilization.CulturalGemstone.Name }, new List<string> { "crystals" }, Game1.r.Next(0, 5), Game1.r.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                    chosenBlock.Structures.Add(scaffold);
                                    builtBlocks.Add(index);
                                }

                                // Reflect across the X-axis
                                int reflectedX = 6 - x;
                                index = reflectedX + z * 7;
                                if (x != 3 && !builtBlocks.Contains(index))
                                {
                                    Block reflectedBlockX = NewLocation.Districts[0].DistrictMap[index];
                                    Structure reflectedStructureX = new Structure("scaffold", new EntityList<Object>(), new EntityList<Room>(), reflectedBlockX, new EntityList<Material> { l.HomeCivilization.CulturalGemstone }, new List<string> { l.HomeCivilization.CulturalGemstone.Name }, new List<string> { "crystals" }, Game1.r.Next(0, 5), Game1.r.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                    reflectedBlockX.Structures.Add(reflectedStructureX);
                                    builtBlocks.Add(index);
                                }

                                // Reflect across the Z-axis
                                int reflectedZ = 6 - z;
                                index = x + reflectedZ * 7;
                                if (z != 3 && !builtBlocks.Contains(index))
                                {
                                    Block reflectedBlockZ = NewLocation.Districts[0].DistrictMap[index];
                                    Structure reflectedStructureZ = new Structure("scaffold", new EntityList<Object>(), new EntityList<Room>(), reflectedBlockZ, new EntityList<Material> { l.HomeCivilization.CulturalGemstone }, new List<string> { l.HomeCivilization.CulturalGemstone.Name }, new List<string> { "crystals" }, Game1.r.Next(0, 5), Game1.r.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                    reflectedBlockZ.Structures.Add(reflectedStructureZ);
                                    builtBlocks.Add(index);
                                }

                                // Reflect across both axes
                                if (x != 3 && z != 3)
                                {
                                    index = reflectedX + reflectedZ * 7;
                                    if (!builtBlocks.Contains(index))
                                    {
                                        Block reflectedBlockBoth = NewLocation.Districts[0].DistrictMap[index];
                                        Structure reflectedStructureBoth = new Structure("scaffold", new EntityList<Object>(), new EntityList<Room>(), reflectedBlockBoth, new EntityList<Material> { l.HomeCivilization.CulturalGemstone }, new List<string> { l.HomeCivilization.CulturalGemstone.Name }, new List<string> { "crystals" }, Game1.r.Next(0, 5), Game1.r.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                        reflectedBlockBoth.Structures.Add(reflectedStructureBoth);
                                        builtBlocks.Add(index);
                                    }
                                }
                            }
                        }
                    }

                    NewLocation.ReferredToNames = new List<string>() { NewLocation.Name };
                }


                //do all migration
                foreach (Architect a in AllArchitects)
                {
                    if (a.NextMigrationLocation != null)
                    {
                        if(a.District != null)
                        {
                            a.District.ArchitectsToRemove.Add(a);
                        }
                        a.District = a.NextMigrationLocation.Districts[Game1.r.Next(a.NextMigrationLocation.Districts.Count)];
                        a.District.ArchitectsToAdd.Add(a);
                        a.Location = a.NextMigrationLocation;
                        a.HomeLocation = a.Location;

                        if (new List<string>() { "commune", "mound", "monastery", "outpost" }.Contains(a.NextMigrationLocation.Type) || CalamityStructures.Contains(a.NextMigrationLocation.Type))
                        {
                            if (Game1.r.Next(3) == 1 || CalamityStructures.Contains(a.NextMigrationLocation.Type) && a.Diplomakitted == false)
                            {
                                a.KitOutArchitect("warriorpower" + a.Level);
                                a.Diplomakitted = true;
                            }

                            if(!a.OppositionTags.Contains("intruders") && a.Bound == false)
                            {
                                a.OppositionTags.Add("intruders");
                            }
                        }
                        
                        a.NextMigrationLocation = null;

                    }
                }

                // Get rid of some dead people

                EntityList<Architect> ArchToRemove = new EntityList<Architect>();
                EntityList<InteractableEvent> EventsToRemove = new EntityList<InteractableEvent>();

                for (int x = 0; x < Game1.GameWorld.Width; x++)
                {
                    for (int z = 0; z < Game1.GameWorld.Width; z++)
                    {
                        var cell = Game1.GameWorld.WorldMap[x + z * Game1.GameWorld.Width];

                        // Filter events that will have no architects after removal of dead ones
                        var filteredEvents = cell.Events.Where(e =>
                        {
                            e.GuaranteedArchitects.RemoveAll(a => !a.IsAlive);
                            return e.GuaranteedArchitects.Count > 0;
                        });

                        // Update cell events to only include those that still have architects
                        cell.Events = filteredEvents;
                    }
                }



                //clean up architect locations
                for (int x = 0; x < Width; x++)
                {
                    for (int z = 0; z < Length; z++)
                    {
                        if (WorldMap[x + z * Width].MyLocation != null)
                        {
                            foreach (District d in WorldMap[x + z * Width].MyLocation.Districts)
                            {
                                foreach (Architect a in d.ArchitectsToAdd)
                                {
                                    d.Architects.Add(a);
                                }
                                foreach (Architect a in d.ArchitectsToRemove)
                                {
                                    d.Architects.Remove(a);
                                }
                                d.ArchitectsToAdd.Clear();
                                d.ArchitectsToRemove.Clear();
                            }
                        }
                    }
                }

                //polish off locations i hope

                for (int x = 0; x < Width; x++)
                {
                    for (int z = 0; z < Length; z++)
                    {
                        if (WorldMap[x + z * Width].MyLocation != null)
                        {
                            foreach (District d in WorldMap[x + z * Width].MyLocation.Districts)
                            {
                                foreach (Architect a in d.Architects)
                                {
                                    a.District = d;
                                    a.Location = d.Location;
                                }
                            }
                        }
                    }
                }


            }

            if(IncreaseCycle)
            {
                Cycle += Days * 864000;
            }

            LivingArchitects = CurrentlyCountingArchitects;
            DeadArchitects = AllArchitects.Count - LivingArchitects;
        }


        public Object SuperLoot(int Level)
        {
            List<string> usefulstuff = new List<string>
{
    "shortsword",
    "knife",
    "greatsword",
    "battle axe",
    "axe",
    "greataxe",
    "rapier",
    "spear",
    "pike",
    "mace",
    "hammer",
    "shield",
    "whip",
    "scourge",
    "flail",
    "chain",
    "large hat",
    "small hat",
    "hood",
    "cape",
    "amulet",
    "flair",
    "left glove",
    "right glove",
    "shortsleeve shirt",
    "longsleeve shirt",
    "uppershirt",
    "straps",
    "pants",
    "shorts",
    "kilt",
    "wraps",
    "left boot",
    "right boot",
    "left shoe",
    "right shoe",
    "helmet",
    "chestplate",
    "left gauntlet",
    "right gauntlet"
};

            string ChosenItem = usefulstuff[Game1.r.Next(usefulstuff.Count)];

            EntityList<Material> Materials = new EntityList<Material>
            {
                Metals[Game1.r.Next(Metals.Count)],
                Cloths[Game1.r.Next(Cloths.Count)]
            };

            while (true)
            {
                if (Game1.r.Next(0, 2) == 0)
                {
                    break;
                }

                int Chooser = Game1.r.Next(1, 4);

                if (Chooser == 1)
                {
                    Materials.Add(Gemstones[Game1.r.Next(Gemstones.Count)]);
                }
                else if (Chooser == 2)
                {
                    Materials.Add(Stones[Game1.r.Next(Stones.Count)]);
                }
                else
                {
                    Materials.Add(Woods[Game1.r.Next(Woods.Count)]);
                }
            }

            Object o = new Object(null, ChosenItem, Materials, null);
            o.Rarity = GenerateItemRarity(Level);
            o.ApplyImbuements(1);
            o.IsMagical = true;
            o.Name = GenerateUniqueName("1S" + Game1.r.Next(4) + "s1w", o);
            return (o);
        }

        public Object MagicalSuperLoot(int Level)
        {
            List<string> potentialMagicalItems = new List<string>
            {
                "amulet",
                "bottle",
                "jar",
                "small mug",
                "small bowl",
                "small cup",
                "knife",
                "chain",
                "candle",
                "scroll",
                "small chalice",
                "left gauntlet",
                "right gauntlet",
                "orb",
            };

            Dictionary<string, List<string>> itemCommonNouns = new Dictionary<string, List<string>>
            {
                { "amulet", new List<string> { "charm", "talisman", "pendant" } },
                { "bottle", new List<string> { "flask", "vial", "decanter" } },
                { "jar", new List<string> { "canister", "urn", "container" } },
                { "small mug", new List<string> { "tankard", "cup", "stein" } },
                { "small bowl", new List<string> { "basin", "dish", "plate" } },
                { "small cup", new List<string> { "goblet", "chalice", "beaker" } },
                { "knife", new List<string> { "blade", "dagger", "stiletto" } },
                { "chain", new List<string> { "links", "shackle", "cord" } },
                { "candle", new List<string> { "beacon", "light", "flame" } },
                { "scroll", new List<string> { "parchment", "manuscript", "folio" } },
                { "small chalice", new List<string> { "cup", "goblet", "vessel" } },
                { "left gauntlet", new List<string> { "glove", "hand", "fist" } },
                { "right gauntlet", new List<string> { "glove", "hand", "fist" } },
                { "orb", new List<string> { "sphere", "globe", "crystal" } },
            };

            string ChosenItem = potentialMagicalItems[Game1.r.Next(potentialMagicalItems.Count)];
            List<string> commonNouns = itemCommonNouns[ChosenItem];
            string ChosenCommonNoun = Game1.Capitalize(commonNouns[Game1.r.Next(commonNouns.Count)]);
            string Subject = Game1.Capitalize(Game1.Words[Game1.r.Next(Game1.Words.Count)]);

            List<string> namingFormats = new List<string>
            {
                "The {0} of {1}",
                "{1} {0}",
                "{0} of {1}",
                "The {1}'s {0}",
                "{1}'s {0}",
            };

            string format = namingFormats[Game1.r.Next(namingFormats.Count)];
            string name = string.Format(format, ChosenCommonNoun, Subject);

            EntityList<Material> Materials = new EntityList<Material>
            {
                Metals[Game1.r.Next(Metals.Count)]
            };

            while (true)
            {
                if (Game1.r.Next(0, 2) == 0)
                {
                    break;
                }

                int Chooser = Game1.r.Next(1, 4);

                if (Chooser == 1)
                {
                    Materials.Add(Gemstones[Game1.r.Next(Gemstones.Count)]);
                }
                else if (Chooser == 2)
                {
                    Materials.Add(Stones[Game1.r.Next(Stones.Count)]);
                }
                else
                {
                    Materials.Add(Woods[Game1.r.Next(Woods.Count)]);
                }
            }

            Object o = new Object(null, ChosenItem, Materials, null);
            o.Name = name;

            o.Rarity = GenerateItemRarity(Level);
            o.IsMagical = true;
            o.ApplyImbuements(1);
            return (o);
        }

        public EntityList<Object> MisplacedLoot(int Quality, int Level)
        {
            EntityList<Object> list = new EntityList<Object>();

            // Initial fragments addition
            for (int i = Game1.r.Next(1, 20); i != 0; i--)
            {
                list.Add(new Object(null, "fragment", new EntityList<Material>() { Vitalium }, null));
            }

            // Loot generation based on Quality
            for (int q = 0; q < Quality; q++)
            {
                // Generate a weapon with random material
                if (Game1.r.Next(1, 100) <= 10) // 10% chance
                {
                    var weapons = new List<string> { "shortsword", "knife", "greatsword", "battle axe", "axe", "greataxe", "rapier", "spear", "pike", "mace", "hammer", "shield", "whip", "scourge", "flail", "chain" };
                    list.Add(new Object(null, weapons[Game1.r.Next(weapons.Count)], new EntityList<Material>() { GetRandomMaterialByStrength(Metals, ConvertLevelToToughness(Level)) }, null));
                }

                if (Game1.r.Next(1, 100) <= 4) //4 percent chance
                {
                    EntityList<Material> m = new EntityList<Material>() { GetRandomMaterialByStrength(Metals, ConvertLevelToToughness(Level)) };

                    for (int i = Game1.r.Next(3, 9); i != 0; i--)
                    {
                        list.Add(new Object(null, "dagger", m, null));
                    }
                }

                // Generate a piece of armor with random material
                if (Game1.r.Next(1, 100) <= 5) // 5% chance for armor
                {
                    var armors = new List<string> { "helmet", "chestplate", "left gauntlet", "right gauntlet", "pants", "cape", "left glove", "right glove", "left boot", "right boot" };
                    list.Add(new Object(null, armors[Game1.r.Next(armors.Count)], new EntityList<Material>() { GetRandomMaterialByStrength(Metals, ConvertLevelToToughness(Level)) }, null));
                }

                // Generate a piece of clothing with random material
                if (Game1.r.Next(1, 100) <= 5) // 5% chance for clothing
                {
                    var clothings = new List<string> { "large hat", "small hat", "hood", "shortsleeve shirt", "longsleeve shirt", "uppershirt", "straps", "shorts", "kilt", "wraps", "left shoe", "right shoe" };
                    list.Add(new Object(null, clothings[Game1.r.Next(clothings.Count)], new EntityList<Material>() { Cloths[Game1.r.Next(Cloths.Count)] }, null));
                }


                // Generate a piece of jewelry with random material
                if (Game1.r.Next(1, 100) <= 15) // 15% chance
                {
                    var jewelry = new List<string> { "amulet", "flair", "cut gem", "gem" };
                    list.Add(new Object(null, jewelry[Game1.r.Next(jewelry.Count)], new EntityList<Material>() { Gemstones[Game1.r.Next(Gemstones.Count)], Metals[Game1.r.Next(Metals.Count)] }, null));
                }

                // Generate a household item with random material
                if (Game1.r.Next(1, 100) <= 30) // 30% chance
                {
                    var householdItems = new List<string> { "small pot", "big mug", "small cup", "big bowl" };
                    list.Add(new Object(null, householdItems[Game1.r.Next(householdItems.Count)], new EntityList<Material>() { Stones[Game1.r.Next(Stones.Count)] }, null));
                }

                // Generate a piece of clothing with random material
                if (Game1.r.Next(1, 100) <= 20) // 20% chance
                {
                    var clothes = new List<string> { "cape", "hood", "longsleeve shirt", "pants", "shortsleeve shirt", "uppershirt", "straps", "shorts", "kilt", "wraps", "left glove", "right glove", "large hat", "small hat", "right boot", "left boot", "right shoe", "left shoe" };
                    list.Add(new Object(null, clothes[Game1.r.Next(clothes.Count)], new EntityList<Material>() { Cloths[Game1.r.Next(Cloths.Count)] }, null));
                }

                // Generate a scroll bearing wisdom of a past age

                if (Game1.r.Next(20) == 1)
                {
                    Object o = new Object(null, "skill scroll", new EntityList<Material>() { Cloths[Game1.r.Next(Cloths.Count)] }, null);
                    o.SpecialKnowledge = Game1.GameWorld.AllSkills[Game1.r.Next(Game1.GameWorld.AllSkills.Count)];
                    list.Add(o);
                }

                if (Game1.r.Next(1, 100) <= 20) // 10% chance for healing item
                {
                    var healingItems = new List<string> { "salve", "bandage", "vial" };
                    string selectedHealingItem = healingItems[Game1.r.Next(healingItems.Count)];

                    switch (selectedHealingItem)
                    {
                        case "salve":
                            list.Add(new Object(null, "salve", new EntityList<Material>() { Fibers[Game1.r.Next(Fibers.Count)] }, null));
                            break;
                        case "bandage":
                            list.Add(new Object(null, "bandage", new EntityList<Material>() { Cloths[Game1.r.Next(Cloths.Count)] }, null));
                            break;
                        case "vial":
                            list.Add(new Object(null, "vial", new EntityList<Material>() { Glass, Vitalium }, null));
                            break;
                            // Add more healing items if needed
                    }
                }
            }

            return list;
        }

        public string CountEntities(Dictionary<string, Entity> subjectCatalogue)
        {
            // Dictionary to store the count of each entity type
            Dictionary<string, int> entityTypeCount = new Dictionary<string, int>();

            // Iterate through the SubjectCatalogue
            foreach (var keyValue in subjectCatalogue)
            {
                var entity = keyValue.Value;

                // Check for null values
                if (entity != null)
                {
                    string entityType = entity.GetType().Name;

                    // Increment the count for the entity type
                    if (entityTypeCount.ContainsKey(entityType))
                    {
                        entityTypeCount[entityType]++;
                    }
                    else
                    {
                        entityTypeCount[entityType] = 1;
                    }
                }
            }

            // Create the result string
            string result = string.Join(", ",
                entityTypeCount.Select(kv => $"{kv.Value} {kv.Key}s"));

            return result;
        }

        public string GenerateUniqueName(string pattern, Entity e)
        {
            string GenerateName(int WordsToCombine, bool Caps)
            {
                string word = string.Empty;
                Random random = new Random();

                for (int i = 0; i < WordsToCombine; i++)
                {
                    word += Game1.Words[random.Next(Game1.Words.Count)];
                }

                if (Caps)
                {
                    word = word.Substring(0, 1).ToUpper() + word.Substring(1);
                }

                return word;
            }

            string GenerateSyllableName(int SyllablesCount, bool Caps)
            {
                string word = string.Empty;
                for (int i = 0; i < SyllablesCount; i++)
                {
                    word += Game1.Syllables[Game1.r.Next(Game1.Syllables.Count)];
                }

                if (Caps)
                {
                    word = word.Substring(0, 1).ToUpper() + word.Substring(1);
                }

                return word;
            }

            string generatedName = "";
            int repeatCount = 0;

            do
            {
                generatedName = ""; // Reset generatedName for each attempt
                foreach (char c in pattern)
                {
                    if (char.IsDigit(c))
                    {
                        repeatCount = repeatCount * 10 + (c - '0');
                    }
                    else
                    {
                        if (repeatCount == 0)
                        {
                            repeatCount = 1;
                        }

                        switch (c)
                        {
                            case 'S':
                                generatedName += GenerateSyllableName(repeatCount, true);
                                break;
                            case 'W':
                                generatedName += GenerateName(repeatCount, true);
                                break;
                            case 's':
                                generatedName += GenerateSyllableName(repeatCount, false);
                                break;
                            case 'w':
                                generatedName += GenerateName(repeatCount, false);
                                break;
                            case ' ':
                                generatedName += ' ';
                                break;
                        }
                        repeatCount = 0; // Reset repeat count after processing
                    }
                }
            } while (SubjectCatalogue.ContainsKey(generatedName)); // Continue if name is not unique

            if (!(e is World)) // Exception for 'World' because it's handled manually
            {
                SubjectCatalogue.Add(generatedName, e);
            }

            return generatedName;
        }


        public string GenerateUniqueArchitectName(Entity e)
        {
            string generatedName = "";

            while (SubjectCatalogue.ContainsKey(generatedName))
            {
                string firstName = Game1.FirstNames[Game1.r.Next(Game1.FirstNames.Count)] + Game1.NameSuffixes[Game1.r.Next(Game1.NameSuffixes.Count)];
                string lastName = ((Game1.LastNames[Game1.r.Next(Game1.LastNames.Count)]).Substring(0, 1)).ToUpper() + (Game1.LastNames[Game1.r.Next(Game1.LastNames.Count)]).Substring(1).ToLower();
                //OK so this system litterally takes a last name from the list, and then takes a random letter from a random last name and replaces the first letter. Its not supposed to do that, BUT IT WORKS SO WELL WHAT

                generatedName = $"{firstName} {lastName}";
            }

            SubjectCatalogue.Add(generatedName, e);
            return generatedName;
        }

        public static string GenerateItemRarity(int level)
        {
            if (!RarityDistribution.ContainsKey(level))
                return "Invalid level";

            var rarities = RarityDistribution[level];
            var rarityWeights = new List<int>();

            foreach (var rarity in rarities)
            {
                switch (rarity)
                {
                    case "common":
                        rarityWeights.Add(level < 8 ? 100 - (level - 1) * 10 : 0); // Decreases as level increases, until 8
                        break;
                    case "uncommon":
                        rarityWeights.Add(level < 8 ? 10 + (level - 1) * 10 : 60 + (level - 8) * 10); // Increases as level increases
                        break;
                    case "rare":
                        rarityWeights.Add(level <= 7 ? 20 + (level - 6) * 15 : 80 + (level - 8) * 10); // More likely at higher levels
                        break;
                    case "epic":
                        rarityWeights.Add(100); // Always most rare
                        break;
                }
            }

            // Select a rarity based on the weighted chances
            int totalWeight = rarityWeights.Sum();
            int randomNumber = Game1.r.Next(0, totalWeight);
            int weightSum = 0;

            for (int i = 0; i < rarities.Count; i++)
            {
                weightSum += rarityWeights[i];
                if (randomNumber < weightSum)
                    return rarities[i];
            }

            return "Error: Rarity selection failed";
        }
    }
}