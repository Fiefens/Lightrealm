using Lightrealm;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics.PackedVector;
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
using System.Text.Json.Serialization;
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
        public SerializableRandom rnd;

        public List<string> ItemTypesInCirculation { get; set; } = new List<string>();

        public int BirthProbabilityMod = 100; // higher is less likely, decrease the chance of procreation
        public int HousingValue = 4;
        public int ArchitectConstant = 75;

        static string DistrictToTestName = "";

        public List<int> RndTracker = new List<int>();
        public string RndString
        {
            get
            {
                return string.Join(" ", RndTracker);
            }
        }
        public Location StoredLocation;

        public EntityList<Architect> RegularMessengers = new EntityList<Architect>();
        public EntityList<Architect> UndergroundMessengers = new EntityList<Architect>();
        public EntityList<Letter> RegularLetters = new EntityList<Letter>();
        public EntityList<Letter> UndergroundLetters = new EntityList<Letter>();

        public EntityList<Entity> StructureTypes = new EntityList<Entity>();

        public int NextUniqueID { get; set; } = 0;

        public Dictionary<string, Material> Materials { get; set; } = new Dictionary<string, Material>();
        public List<string> DupeMats { get; set; } = new List<string>();

        public EntityList<Group> LegacyGroups = new EntityList<Group>();

        public EntityList<Realm> Realms = new EntityList<Realm>();
        public int RealmsCount = 0;

        public Association GamePlayerAssociation;
        public Structure GamePlayerTavern;

        public Dictionary<int, Entity> EntityLedger { get; set; } = new Dictionary<int, Entity>
        {
            { 0, null }
        };

        public Dictionary<string, string> GroupTypes { get; set; } = new Dictionary<string, string>
    {
        { "anarchist", "noticed the anarchistic similarities of both their groups and forged them into one" },
        { "guild", "recognized their mutual goals and merged to form a stronger guild" },
        { "scholarly", "found their scholarly pursuits aligned and combined their resources" },
        { "military", "saw the strategic advantage in uniting their forces" },
        { "political", "realized their political ambitions were better achieved together" },
        { "entertainment", "decided that merging their talents would entertain more people" }
    };

        public EntityList<Entity> AllSpells { get; set; } = new EntityList<Entity>
    {
        new Entity("water bolt"),
        new Entity("chaos flare"),
        new Entity("flash flame"),
        new Entity("tremor"),
        new Entity("ice shock"),
        new Entity("truthfulness"),
        new Entity("rise"),
        new Entity("hold"),
        new Entity("force throw"),
        new Entity("shatter"),
        new Entity("intercept"),
        new Entity("expel"),
        new Entity("extract"),
        new Entity("emergent growth"),
        new Entity("animate"),
        new Entity("immortalize"),
        new Entity("revive"),
        new Entity("resurrect")
    };

        public EntityList<Entity> AllBodyParts { get; set; } = new EntityList<Entity>();

        public EntityList<Entity> Domains { get; set; } = new EntityList<Entity>
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
        new Entity("nature"),
        new Entity("coffee"),
        new Entity("tea"),
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

        public EntityList<Entity> AllSkills { get; set; } = new EntityList<Entity>
        {
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

        public EntityList<Entity> AllLegendarySpells { get; set; } = new EntityList<Entity>
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

            int minLevel = 1;
            int maxLevel = 10;
            int minToughness = 2;
            int maxToughness = 16;

            int toughness = minToughness + (level - minLevel) * (maxToughness - minToughness) / (maxLevel - minLevel);
            return toughness;
        }

        public Dictionary<string, string> GenericHatredDictionary { get; set; } = new Dictionary<string, string>
        {
            {"civilized", ""},
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

        public Dictionary<string, EntityList<Material>> MaterialsFromColors { get; set; } = new Dictionary<string, EntityList<Material>>
        {
            { "maroon", new EntityList<Material>{ new Material("mahogany", "organic", 1, 1, "maroon"), new Material("beetroot", "organic", 1, 1, "maroon"), new Material("rustleaf", "organic", 1, 1, "maroon") } },
            { "red", new EntityList<Material>{ new Material("rose", "organic", 1, 1, "red"), new Material("tulip", "organic", 1, 1, "red"), new Material("coralvine", "organic", 1, 1, "red") } },
            { "orange", new EntityList<Material>{ new Material("citrus", "organic", 1, 1, "orange"), new Material("ambervine", "organic", 1, 1, "orange"), new Material("brimroot", "organic", 1, 1, "orange") } },
            { "yellow", new EntityList<Material>{ new Material("emberleaf", "organic", 1, 1, "yellow"), new Material("dandelion", "organic", 1, 1, "yellow"), new Material("lemon", "organic", 1, 1, "yellow") } },
            { "limegreen", new EntityList<Material>{ new Material("lime", "organic", 1, 1, "limegreen"), new Material("grass", "organic", 1, 1, "limegreen"), new Material("verdantleaf", "organic", 1, 1, "limegreen") } },
            { "green", new EntityList<Material>{ new Material("lichen", "organic", 1, 1, "green"), new Material("cactus", "organic", 1, 1, "green"), new Material("moss", "organic", 1, 1, "green") } },
            { "lightblue", new EntityList<Material>{ new Material("skybud", "organic", 1, 1, "lightblue"), new Material("orchid", "organic", 1, 1, "lightblue"), new Material("slushroot", "organic", 1, 1, "lightblue") } },
            { "cyan", new EntityList<Material>{ new Material("algae", "organic", 1, 1, "cyan"), new Material("seagrass", "organic", 1, 1, "cyan"), new Material("eelvine", "organic", 1, 1, "cyan") } },
            { "blue", new EntityList<Material>{ new Material("blueberry", "organic", 1, 1, "blue"), new Material("crystalvine", "organic", 1, 1, "blue"), new Material("silkweed", "organic", 1, 1, "blue") } },
            { "purple", new EntityList<Material>{ new Material("grape", "organic", 1, 1, "purple"), new Material("deepshroom", "organic", 1, 1, "purple"), new Material("mythrial", "organic", 1, 1, "purple") } },
            { "magenta", new EntityList<Material>{ new Material("berry", "organic", 1, 1, "magenta"), new Material("petal", "organic", 1, 1, "magenta"), new Material("roseleaf", "organic", 1, 1, "magenta") } },
            { "coral", new EntityList<Material>{ new Material("pinkvine", "organic", 1, 1, "coral"), new Material("seaweed", "organic", 1, 1, "coral"), new Material("shellvine", "organic", 1, 1, "coral") } },
            { "white", new EntityList<Material>{ new Material("snowbud", "organic", 1, 1, "white"), new Material("moonvine", "organic", 1, 1, "white"), new Material("cloudleaf", "organic", 1, 1, "white") } },
            { "gray", new EntityList<Material>{ new Material("ashleaf", "organic", 1, 1, "gray"), new Material("smokebud", "organic", 1, 1, "gray"), new Material("stormvine", "organic", 1, 1, "gray") } },
            { "black", new EntityList<Material>{ new Material("obsidian", "organic", 1, 1, "black"), new Material("scarshade", "organic", 1, 1, "black"), new Material("silkbud", "organic", 1, 1, "black") } },
            { "brown", new EntityList<Material>{ new Material("bark", "organic", 1, 1, "brown"), new Material("cocoa", "organic", 1, 1, "brown"), new Material("hazelbud", "organic", 1, 1, "brown") } }
        };


        public EntityList<Entity> ExtraEntities { get; set; } = new EntityList<Entity>
        {
            new Entity("tavern"), new Entity("prism"), new Entity("well"), new Entity("shrine"),
            new Entity("library"), new Entity("watchtower"), new Entity("forge"), new Entity("market"),
            new Entity("north"), new Entity("south"), new Entity("east"), new Entity("west"),
            new Entity("up"), new Entity("down"), new Entity("southeast"), new Entity("southwest"),
            new Entity("northeast"), new Entity("northwest"), new Entity("shadow fountain"),
            new Entity("relationships"), new Entity("mining"), new Entity("combat"),
            new Entity("crafting"), new Entity("trading"), new Entity("stealth"), new Entity("alchemy"),
            new Entity("cooking"), new Entity("fishing"), new Entity("hunting"), new Entity("quests"),
            new Entity("gathering"), new Entity("imbuement"), new Entity("healing"), new Entity("navigation"),
            new Entity("tactics"), new Entity("survival"), new Entity("diplomacy"), new Entity("lockpicking"),
            new Entity("animal taming"), new Entity("herbalism"), new Entity("herbs"), new Entity("blacksmithing"),
            new Entity("tailoring"), new Entity("carpentry"), new Entity("architecture"), new Entity("history"),
            new Entity("sailing"), new Entity("farming"), new Entity("brewing"), new Entity("divination"),
            new Entity("spellcasting"), new Entity("negotiation"),
            new Entity("astronomy"), new Entity("necromancy"), new Entity("spatiomancy"),
            new Entity("conjuromancy"), new Entity("fractalmancy"), new Entity("perceptomancy"),
            new Entity("beasts"), new Entity("divinity"), new Entity("illusion"), new Entity("mechanics"),
            new Entity("engineering"), new Entity("book"), new Entity("poem"), new Entity("song"),
            new Entity("spells"), new Entity("skills"), new Entity("all")
        };

        public bool LostPlaced { get; set; } = false;
        public bool FirstNewCivPlaced { get; set; } = false;
        public bool SecondNewCivPlaced { get; set; } = false;
        public bool ThirdNewCivPlaced { get; set; } = false;

        public bool FirstOutcastCivPlaced { get; set; } = false;
        public bool SecondOutcastCivPlaced { get; set; } = false;
        public bool ThirdOutcastCivPlaced { get; set; } = false;
        public int FirstOutcastCivStartYear { get; set; } = 0;
        public int SecondOutcastCivStartYear { get; set; } = 0;
        public int ThirdOutcastCivStartYear { get; set; } = 0;

        public static List<EntityList<Region>> Islands { get; set; } = new List<EntityList<Region>>();
        public static List<EntityList<Region>> PortLocations { get; set; } = new List<EntityList<Region>>();
        public static List<EntityList<Region>> PotentialPorts { get; set; } = new List<EntityList<Region>>();
        public static bool AllPortsBuilt { get; set; } = false;

        public List<Location> AllLocations { get; set; } = new List<Location>();
        public List<string> UnusedCivColors { get; set; } = new List<string>();

        public EntityList<Object> AllWrittenContent { get; set; } = new EntityList<Object>();

        public List<string> CalamityStructures { get; set; } = new List<string> { "tower", "keep", "monument", "fortress", "stronghold" };
        public List<string> ProcgenStructures { get; set; } = new List<string>
    {
        "observatory",
        "library",
        "conservatory",
        "prison",
        "tomb",
        "gallery",
        "armory"
    };

        public int ContinentalPortMaximum { get; set; } = 0;




        public Object GenerateRandomWeapon(Material material, string Rarity)
        {
            // Array of possible weapon types
            string[] weaponTypes = { "shortsword", "greatsword", "battle axe", "greataxe", "rapier", "spear", "pike", "mace", "hammer", "whip", "scourge" };

            // Randomly select a weapon type
            string selectedWeapon = weaponTypes[rnd.Next(weaponTypes.Length)];

            // Randomly select a metal Material from the world's Metals list

            // Create and return the new weapon Object
            Object o = new Object(null, selectedWeapon, new EntityList<Material>() { material }, null);
            o.Rarity = Rarity;
            o.ApplyImbuements(0);
            return o;
        }






        public List<string> OutcastCivTypes { get; set; } = new List<string> { "druid", "pirate", "cultist", "anarchist", "scavenger" };
        public List<string> DecidedOutcastCivs { get; set; } = new List<string>();
        public EntityList<Civilization> OutcastCivs { get; set; } = new EntityList<Civilization>();

        private int _hypernexus;
        
        public Architect Hypernexus
        {
            get => EntityGet<Architect>(_hypernexus);
            set => _hypernexus = value?.ID ?? 0;
        }

        private int _icosidodecahedron;
        
        public Architect Icosidodecahedron
        {
            get => EntityGet<Architect>(_icosidodecahedron);
            set => _icosidodecahedron = value?.ID ?? 0;
        }

        private int _shadeheart;
        
        public Architect Shadeheart
        {
            get => EntityGet<Architect>(_shadeheart);
            set => _shadeheart = value?.ID ?? 0;
        }

        public List<War> Wars { get; set; } = new List<War>();

        public EntityHashSet<Architect> AllHistoricalArchitects { get; set; } = new EntityHashSet<Architect>();
        public EntityList<Unit> AllUnits { get; set; } = new EntityList<Unit>();
        public EntityList<Architect> Legends { get; set; } = new EntityList<Architect>();

        public string GameMode = "unknown";

        private int _purity;
        
        public Blight Purity
        {
            get => EntityGet<Blight>(_purity);
            set => _purity = value?.ID ?? 0;
        }

        public EntityList<Architect> Calamity { get; set; } = new EntityList<Architect>();
        public string CalamityReasoning { get; set; } = "";
        public int CalamityStartingYear { get; set; } = 0;
        public List<string> CalamityLore { get; set; } = new List<string>();
        public string CalamityIdeologicalObsession { get; set; } = "";

        public int ReactionModifierInt { get; set; } = 0;

        public EntityList<Race> Races { get; set; } = new EntityList<Race>();
        public EntityList<Race> HumanoidRaces { get; set; } = new EntityList<Race>();
        public EntityList<Race> ExtraRaces { get; set; } = new EntityList<Race>();
        public EntityList<Race> ConstructRaces { get; set; } = new EntityList<Race>();
        public EntityList<Race> WildRaces { get; set; } = new EntityList<Race>();

        public EntityList<Entity> DeletedSpells { get; set; } = new EntityList<Entity>();
        public EntityList<Race> DeletedRaces { get; set; } = new EntityList<Race>();
        public EntityList<Composition> DeletedCompositions { get; set; } = new EntityList<Composition>();
        public EntityList<Material> DeletedMaterials { get; set; } = new EntityList<Material>();
        public EntityList<Object> DeletedObjects { get; set; } = new EntityList<Object>();

        public Dictionary<string, Entity> SubjectCatalogue { get; set; } = new Dictionary<string, Entity>();

        public int SeaLevel { get; set; }

        public EntityList<Blight> Blights { get; set; } = new EntityList<Blight>();

        private static int _unknown;
        
        public static Architect Unknown
        {
            get => EntityGetStatic<Architect>(_unknown);
            set => _unknown = value?.ID ?? 0;
        }

        public List<string> SettlementTypes { get; set; } = new List<string> { "camp", "village", "town", "city" };

        public List<(int, int, int, int)> ChaosPoints { get; set; } = new List<(int, int, int, int)>();

        public int TotalCrafts { get; set; } = 0;

        public static Dictionary<int, List<string>> RarityDistribution { get; set; } = new Dictionary<int, List<string>>
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

        public Region[] WorldMap;
        public List<Region> BlightedRegions = new List<Region>();

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
            foreach (Race r in Races)
            {
                if (r.Name == race)
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
        public int TotalLetters { get; set; } = 0;

        public int MaxAge { get; set; } = 0;
        public double ProsperityMultiplier { get; set; } = 1.0;
        public string LockedInThreat { get; set; } = "";

        public int LivingArchitects { get; set; }
        public int DeadArchitects { get; set; }

        public EntityList<Architect> Colossals { get; set; } = new EntityList<Architect>();

        public EntityList<Group> Groups { get; set; } = new EntityList<Group>();
        public EntityList<Group> TradingGroups { get; set; } = new EntityList<Group>();
        public EntityList<Group> GroupsToRemove { get; set; } = new EntityList<Group>();

        public EntityList<Faction> AllFactions { get; set; } = new EntityList<Faction>();

        private int _lightDeity;
        
        public Deity LightDeity
        {
            get => EntityGet<Deity>(_lightDeity);
            set => _lightDeity = value?.ID ?? 0;
        }

        private int _darkDeity;
        
        public Deity DarkDeity
        {
            get => EntityGet<Deity>(_darkDeity);
            set => _darkDeity = value?.ID ?? 0;
        }

        public EntityList<Architect> FractalArchitects { get; set; } = new EntityList<Architect>();
        public EntityList<Object> FractalObjects { get; set; } = new EntityList<Object>();

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

        private int _enchromalite;
        
        public Material Enchromalite
        {
            get => EntityGet<Material>(_enchromalite);
            set => _enchromalite = value?.ID ?? 0;
        }

        private int _illuminite;
        
        public Material Illuminite
        {
            get => EntityGet<Material>(_illuminite);
            set => _illuminite = value?.ID ?? 0;
        }

        private int _darkstone;
        
        public Material Darkstone
        {
            get => EntityGet<Material>(_darkstone);
            set => _darkstone = value?.ID ?? 0;
        }

        private int _prismite;
        
        public Material Prismite
        {
            get => EntityGet<Material>(_prismite);
            set => _prismite = value?.ID ?? 0;
        }

        private int _shadesteel;
        
        public Material Shadesteel
        {
            get => EntityGet<Material>(_shadesteel);
            set => _shadesteel = value?.ID ?? 0;
        }

        private int _archaeon;
        
        public Material Archaeon
        {
            get => EntityGet<Material>(_archaeon);
            set => _archaeon = value?.ID ?? 0;
        }

        private int _membrane;
        
        public Material Membrane
        {
            get => EntityGet<Material>(_membrane);
            set => _membrane = value?.ID ?? 0;
        }

        private int _biocrystal;
        
        public Material Biocrystal
        {
            get => EntityGet<Material>(_biocrystal);
            set => _biocrystal = value?.ID ?? 0;
        }

        private int _glass;
        
        public Material Glass
        {
            get => EntityGet<Material>(_glass);
            set => _glass = value?.ID ?? 0;
        }

        private int _clay;
        
        public Material Clay
        {
            get => EntityGet<Material>(_clay);
            set => _clay = value?.ID ?? 0;
        }

        private int _shadesludge;
        
        public Material ShadeSludge
        {
            get => EntityGet<Material>(_shadesludge);
            set => _shadesludge = value?.ID ?? 0;
        }

        private int _coffee;
        
        public Material Coffee
        {
            get => EntityGet<Material>(_coffee);
            set => _coffee = value?.ID ?? 0;
        }

        private int _tea;
        
        public Material Tea
        {
            get => EntityGet<Material>(_tea);
            set => _tea = value?.ID ?? 0;
        }

        private int _vitalium;
        
        public Material Vitalium
        {
            get => EntityGet<Material>(_vitalium);
            set => _vitalium = value?.ID ?? 0;
        }

        private int _paprika;
        
        public Material Paprika
        {
            get => EntityGet<Material>(_paprika);
            set => _paprika = value?.ID ?? 0;
        }

        private int _salt;
        
        public Material Salt
        {
            get => EntityGet<Material>(_salt);
            set => _salt = value?.ID ?? 0;
        }

        private int _pepper;
        
        public Material Pepper
        {
            get => EntityGet<Material>(_pepper);
            set => _pepper = value?.ID ?? 0;
        }

        private int _isodust;
        
        public Material Isodust
        {
            get => EntityGet<Material>(_isodust);
            set => _isodust = value?.ID ?? 0;
        }

        private int _spectre;
        
        public Material Spectre
        {
            get => EntityGet<Material>(_spectre);
            set => _spectre = value?.ID ?? 0;
        }

        private int _energy;
        
        public Material Energy
        {
            get => EntityGet<Material>(_energy);
            set => _energy = value?.ID ?? 0;
        }

        private int _flame;
        
        public Material Flame
        {
            get => EntityGet<Material>(_flame);
            set => _flame = value?.ID ?? 0;
        }

        private int _void;
        
        public Material Void
        {
            get => EntityGet<Material>(_void);
            set => _void = value?.ID ?? 0;
        }

        private int _honey;
        
        public Material Honey
        {
            get => EntityGet<Material>(_honey);
            set => _honey = value?.ID ?? 0;
        }

        private int _waspwax;
        
        public Material Waspwax
        {
            get => EntityGet<Material>(_waspwax);
            set => _waspwax = value?.ID ?? 0;
        }

        public double Cycle { get; set; }
        public int Seed { get; set; }

        public double LastFunctionRunCycle { get; set; }

        public EntityList<Race> ColossalTypes { get; set; } = new EntityList<Race>();

        public List<Event> HistoricalEvents { get; set; } = new List<Event>();
        public List<Event> SignificantEvents { get; set; } = new List<Event>();
        public List<string> AbridgedHistoricalEvents { get; set; } = new List<string>();

        public List<Ruleset> Games = new List<Ruleset>();
        
        public EntityList<Civilization> Civilizations { get; set; } = new EntityList<Civilization>();
        public int InitialCivCount { get; set; }


        public EntityList<Entity> HyperThreats = new EntityList<Entity>();

        private static T EntityGetStatic<T>(int id) where T : Entity
        {
            return (T)(id != 0 ? Game1.GameWorld.EntityLedger[id] : null);
        }

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

            string Date = $"({Month}/{Year})";

            Vector2 activationPoint = new Vector2(X, Z);

            foreach (var currentRegion in WorldMap)
            {
                // Skip regions that are void or have specific structures and conditions
                if (currentRegion.Biome == "void" ||
                    (Calamity.Contains(Activator) && currentRegion.Location != null && CalamityStructures.Contains(currentRegion.Location.Type)))
                    continue;

                Vector2 currentRegionCenter = new Vector2(currentRegion.X, currentRegion.Z);
                float distance = Vector2.Distance(activationPoint, currentRegionCenter);

                // Only consider regions within the radius
                if (distance <= radius)
                {
                    // Calculate the chance of the tile not being struck
                    float strikeChance = 1 - (distance / radius);

                    // Determine if the tile is struck based on the calculated chance
                    if (rnd.NextDouble() <= strikeChance) // Random.NextDouble() generates a value between 0.0 and 1.0
                    {
                        // Trigger the rupture for this region
                        currentRegion.Biome = "ethereal";

                        HistoricalEvents.Add(new Event($"{Date} {Activator.Name} decimated the landscape of the world with a catastrophic ethereal rupture.", currentRegion, new EntityList<Entity>() { Activator }));

                        if (currentRegion.Location != null)
                        {
                            HistoricalEvents.Add(new Event($"{Date} {currentRegion.Location.Name} was consumed in the rupture.", currentRegion, new EntityList<Entity>() { currentRegion.Location }));

                            foreach (District d in currentRegion.Location.Districts)
                            {
                                foreach (Architect a in d.Architects)
                                {
                                    HistoricalEvents.Add(new Event($"{Date} {a.Name} was consumed in the rupture.", currentRegion, new EntityList<Entity>() { a }));
                                    a.IsAlive = false;
                                }
                            }

                            AllLocations.Remove(currentRegion.Location);
                            currentRegion.Location = null;
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
            if (filteredMaterials.Count() == 0)
            {
                return null;
            }

            // Select a random material from the filtered list
            
            int index = rnd.Next(filteredMaterials.Count());

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
                            if (tile.Biome != "void" && tile.Owner == null && (tile.Location == null || tile.Location.Type != "spire"))
                            {
                                tile.Owner = c;
                            }
                        }
                    }
                }
            }
        }

        public void RevealNearbyTiles(int centerX, int centerZ, int radius, bool Hexagonal)
        {
            // Convert hex coordinates to cube coordinates
            int ConvertToCubeX(int x, int z) => x - (z - (z & 1)) / 2;
            int ConvertToCubeY(int x, int z) => -x - z;
            int ConvertToCubeZ(int z) => z;

            // Calculate the distance between two points in cube coordinates
            int CubeDistance(int x1, int y1, int z1, int x2, int y2, int z2)
                => (Math.Abs(x1 - x2) + Math.Abs(y1 - y2) + Math.Abs(z1 - z2)) / 2;

            // Calculate the distance between two points in square grid
            int SquareDistance(int x1, int z1, int x2, int z2)
                => (int)Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(z1 - z2, 2));

            // Iterate over all tiles within the game world
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {
                    int distance;

                    if (Hexagonal)
                    {
                        int cubeX = ConvertToCubeX(x, z);
                        int cubeY = ConvertToCubeY(cubeX, z);
                        int cubeZ = ConvertToCubeZ(z);

                        int centerX_cube = ConvertToCubeX(centerX, centerZ);
                        int centerY_cube = ConvertToCubeY(centerX_cube, centerZ);
                        int centerZ_cube = ConvertToCubeZ(centerZ);

                        // Calculate distance in cube coordinates
                        distance = CubeDistance(cubeX, cubeY, cubeZ, centerX_cube, centerY_cube, centerZ_cube);
                    }
                    else
                    {
                        // Calculate distance in square grid
                        distance = SquareDistance(x, z, centerX, centerZ);
                    }

                    if (distance <= radius)
                    {
                        // Set the tile to explored
                        WorldMap[x + z * Width].Explored = true;

                        // Check if there's a location at this tile and determine if it should be revealed
                        var location = WorldMap[x + z * Width].Location;
                        if (location != null)
                        {
                            //if ascendant always reveal

                            if(Game1.GameState == "ascendant" && rnd.Next(3) != 0)
                            {
                                location.Explored = true;
                            }



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
        public string GetFormattedTime()
        {
            long totalCycles = (long)Cycle;

            // Skip the year, month, and day calculations
            totalCycles %= 864000;

            // Calculate the hour
            int hour = (int)(totalCycles / 36000);
            totalCycles %= 36000;

            // Calculate the minute
            int minute = (int)(totalCycles / 600);
            totalCycles %= 600;

            // Calculate the second
            int second = (int)(totalCycles / 10);

            // Return the formatted time
            return $"{hour:D2}:{minute:D2}:{second:D2}";
        }


        public string GetFormattedDate()
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

            // Return the formatted date
            return $"{month}/{day}/{year}";
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

        public World(int width, int length, int civCount, int maxAge, string dedicatedThreat, double prosperityMultiplier, int SizeVariant, int seed)
        {
            Seed = seed;

            FirstOutcastCivStartYear = 75 + Game1.TemporaryRand.Next(-10, 11);
            SecondOutcastCivStartYear = 100 + Game1.TemporaryRand.Next(-10, 11);
            ThirdOutcastCivStartYear = 125 + Game1.TemporaryRand.Next(-10, 11);
            CalamityStartingYear = Game1.TemporaryRand.Next(30, 80);
            ReactionModifierInt = Game1.TemporaryRand.Next(1, 100000);
            RealmsCount = Game1.TemporaryRand.Next(5, 8);
            ContinentalPortMaximum = Game1.TemporaryRand.Next(9, 16);

            Enchromalite = new Material("enchromalite", "cloth", 3, 4, "white");
            Materials["enchromalite"] = Enchromalite;

            Illuminite = new Material("illuminite", "metal", 12, 4, "white");
            Materials["illuminite"] = Illuminite;

            Darkstone = new Material("darkstone", "metal", 12, 4, "gray");
            Materials["darkstone"] = Darkstone;

            Prismite = new Material("prismite", "metal", 14, 4, "white");
            Materials["prismite"] = Prismite;

            Shadesteel = new Material("shadesteel", "metal", 14, 4, "gray");
            Materials["shadesteel"] = Shadesteel;

            Archaeon = new Material("archaeon", "glass", 3, 4, "white");
            Materials["archaeon"] = Archaeon;

            Membrane = new Material("membrane", "organic", 3, 4, "white");
            Materials["membrane"] = Membrane;

            Biocrystal = new Material("biocrystal", "organic", 3, 4, "white");
            Materials["biocrystal"] = Biocrystal;

            Glass = new Material("glass", "glass", 3, 4, "white");
            Materials["glass"] = Glass;

            Clay = new Material("clay", "stone", 3, 4, "maroon");
            Materials["clay"] = Clay;

            ShadeSludge = new Material("shadesludge", "organic", 3, 4, "gray");
            Materials["shadesludge"] = ShadeSludge;

            Coffee = new Material("coffee", "organic", 3, 4, "brown");
            Materials["coffee"] = Coffee;

            Tea = new Material("tea", "organic", 3, 4, "green");
            Materials["tea"] = Tea;

            Vitalium = new Material("vitalium", "stone", 3, 4, "magenta");
            Materials["vitalium"] = Vitalium;

            Paprika = new Material("paprika", "organic", 3, 4, "maroon");
            Materials["paprika"] = Paprika;

            Salt = new Material("salt", "stone", 3, 4, "white");
            Materials["salt"] = Salt;

            Pepper = new Material("pepper", "organic", 3, 4, "gray");
            Materials["pepper"] = Pepper;

            Isodust = new Material("isodust", "stone", 3, 4, "purple");
            Materials["isodust"] = Isodust;

            Spectre = new Material("spectre", "metaphysic", 3, 4, "cyan");
            Materials["spectre"] = Spectre;

            Energy = new Material("energy", "metaphysic", 3, 4, "white");
            Materials["energy"] = Energy;

            Flame = new Material("flame", "metaphysic", 3, 4, "orange");
            Materials["flame"] = Flame;

            Void = new Material("void", "metaphysic", 0, 0, "purple");
            Materials["void"] = Void;

            Honey = new Material("honey", "organic", 3, 4, "orange");
            Materials["honey"] = Honey;

            Waspwax = new Material("waspwax", "organic", 3, 4, "yellow");
            Materials["waspwax"] = Waspwax;

            foreach (string c in Game1.Colors)
            {
                UnusedCivColors.Add(c);
            }

            string baseName = GenerateUniqueName(String.Concat("1S", Game1.TemporaryRand.Next(5), "s"), this, Game1.TemporaryRand);

            Name = "The Continent of " + baseName;
            AddReferredToName(Name);
            AddReferredToName(baseName);
            Purity = new Blight(this);

            LockedInThreat = dedicatedThreat;
            MaxAge = maxAge;
            ProsperityMultiplier = prosperityMultiplier;


            for(int i = 0; i < 3; i++)
            {
                int Index = Game1.TemporaryRand.Next(OutcastCivTypes.Count());
                DecidedOutcastCivs.Add(OutcastCivTypes[Index]);
                OutcastCivTypes.RemoveAt(Index);
            }

            //add materials, collapsed
            {

                //ADD IN ORDER OF STRENGTH


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
                Metals.Add(new Material("copper", "metal", 4, 2, "brown"));
                Metals.Add(new Material("silver", "metal", 6, 3, "white"));
                Metals.Add(new Material("gold", "metal", 6, 4, "yellow"));
                Metals.Add(new Material("iron", "metal", 8, 2, "gray"));
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


            Game1.Shuffle(ExtraRaces);

            LostFoundingYear = Game1.TemporaryRand.Next(35, 65);

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

            Races.Add(new Race("", "average", new List<(string, Material)>(), "white", new List<string>() { }, new List<string>() { }, 0, "", "", this));


            Races.Add(new Race("luminarch", "average", LuminarchBodyParts, "white", new List<string>() {"head", "torso", "neck"}, new List<string>() { "allevil" }, 0, "right hand", "left hand", this));
            Races.Add(new Race("nightfell", "average", NightfellBodyParts, "black", new List<string>() { "head", "torso", "neck" }, new List<string>() { "allevil" }, 0, "right hand", "left hand", this));
            Races.Add(new Race("archaix", "average", LostBodyParts, "gray", new List<string>() { "head", "torso", "neck" }, new List<string>() { "allevil" }, 0, "right hand", "left hand", this));
            HumanoidRaces.AddRange(new EntityList<Race>() { Races[1], Races[2], Races[3] });

            Races.Add(new Race("moari", "large", MoariBodyParts, "white", new List<string>() { "head", "torso", "neck" }, new List<string>() {}, 30, "right front leg", "left front leg", this));
            Races.Add(new Race("cassartrae", "smaller", CassartraeBodyParts, "black", new List<string>() { "core" }, new List<string>() { "allevil" }, 30, "core", "core", this));
            Races.Add(new Race("debtshiba", "medium", ShibaBodyParts, "orange", new List<string>() { "head", "torso" }, new List<string>() { "indebted" }, 1337, "right front leg", "left front leg", this));

            Races.Add(new Race("shiba", "medium", ShibaBodyParts, "orange", new List<string>() { "head", "torso" }, new List<string>() { "allevil" }, 1337, "right front leg", "left front leg", this));
            Races.Add(new Race("felae", "small", ShibaBodyParts, "black", new List<string>() { "head", "torso" }, new List<string>() { "allevil" }, 1337, "right front leg", "left front leg", this));

            Races.Add(new Race("isofractal", "average", IsofractalBodyParts, "gray", new List<string>() { "core" }, new List<string>() { "allevil" }, 15, "shard", "shard", this));
            Races.Add(new Race("photonexus", "small", PhotonexusBodyParts, "gray", new List<string>() { "core" }, new List<string>() { "allunalike" }, 25, "sphere", "sphere", this));
            Races.Add(new Race("shade", "average", ShadeBodyParts, "black", new List<string>() { "sludge" }, new List<string>(){ "alllife" }, 5, "sludge", "sludge", this));
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

            Races.Add(new Race("icosidodecahedron", "huge", IcosidodecahedronParts, "white", new List<string>() { "core" }, new List<string>() { }, 110, "shard", "shard", this));
            Races.Add(new Race("hypernexus", "huge", HypernexusBodyParts, "gray", new List<string>() { "core" }, new List<string>() { }, 125, "sphere", "sphere", this));
            Races.Add(new Race("shadeheart", "huge", ShadeheartBodyParts, "black", new List<string>() { "sludge" }, new List<string>() { }, 80, "sludge", "sludge", this));

            Races.Add(new Race("shadebeast", "large", ShadeBodyParts, "black", new List<string>() { "sludge" }, new List<string>(){ "alllife" }, 30, "sludge", "sludge", this));

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

            for (int i = Game1.TemporaryRand.Next(10, 20); i != 0; i--)
            {
                Material ChosenMetal = Metals[Game1.TemporaryRand.Next(Metals.Count())];
                List<(string, Material)> selectedParts = new List<(string, Material)>();

                // Add non-pairable parts
                foreach (var part in mechanicalParts)
                {
                    selectedParts.Add((part, ChosenMetal));
                }

                // Add pairable parts
                foreach (var part in pairableParts)
                {
                    int count = Game1.TemporaryRand.Next(0, 6); // Randomly decide to add 0-5 different pairable parts
                    if (count == 0) continue;

                    int quantity = (new int[] { 1, 2, 3, 4, 6, 8 })[Game1.TemporaryRand.Next(6)]; // Randomly select the quantity of each part
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

                Race r = new Race("guardian", new List<string> { "medium", "average", "large" }[Game1.TemporaryRand.Next(3)], selectedParts, Game1.Colors[Game1.TemporaryRand.Next(Game1.Colors.Count())], new List<string> { "head", "body" }, new List<string> { "intruders" }, 50, (selectedParts[Game1.TemporaryRand.Next(selectedParts.Count())].Item1), (selectedParts[Game1.TemporaryRand.Next(selectedParts.Count())].Item1), this);
                r.Name = GenerateUniqueName(Game1.TemporaryRand.Next(1, 6) + "s", r, Game1.TemporaryRand) + " guardian";
                Races.Add(r);
                ConstructRaces.Add(r);
            }


            ColossalTypes.Add(new Race("quetzal", "colossal", new List<(string, Material)>() { ("head", Membrane), ("body", Membrane), ("left wing", Membrane), ("right wing", Membrane), ("tail", Membrane), ("left leg", Biocrystal), ("right leg", Biocrystal) }, Game1.Colors[Game1.TemporaryRand.Next(Game1.Colors.Count())], new List<string>() { "head", "body" }, new List<string>() { "allunalike" }, 50, "right leg", "left leg", this));
            ColossalTypes.Add(new Race("wyrm", "colossal", new List<(string, Material)>() { ("head", Membrane), ("body", Membrane), ("front left fin", Membrane), ("front right fin", Membrane), ("back left fin", Membrane), ("back right fin", Membrane), ("tail", Membrane) }, Game1.Colors[Game1.TemporaryRand.Next(Game1.Colors.Count())], new List<string>() { "head", "body" }, new List<string>() { "allunalike" }, 60, "front right fin", "front left fin", this));
            ColossalTypes.Add(new Race("serpent", "colossal", new List<(string, Material)>() { ("head", Membrane), ("body", Membrane), ("left front leg", Membrane), ("right front leg", Membrane), ("left back leg", Membrane), ("right back leg", Membrane), ("tail", Membrane) }, Game1.Colors[Game1.TemporaryRand.Next(Game1.Colors.Count())], new List<string>() { "head", "body" }, new List<string>() { "allunalike" }, 70, "right front leg", "left front leg", this));
            ColossalTypes.Add(new Race("shobe", "colossal", new List<(string, Material)>() { ("head", Membrane), ("body", Membrane), ("left front leg", Membrane), ("right front leg", Membrane), ("left back leg", Membrane), ("right back leg", Membrane), ("tail", Membrane) }, Game1.Colors[Game1.TemporaryRand.Next(Game1.Colors.Count())], new List<string>() { "head", "body" }, new List<string>() { "allunalike" }, 80, "right front leg", "left front leg", this));
            ColossalTypes.Add(new Race("cnidriarch", "colossal", new List<(string, Material)>() { ("bell", Membrane), ("mantle", Membrane) }.Concat(Enumerable.Range(1, 12).Select(i => ($"tentacle{i}", Membrane))).ToList(), Game1.Colors[Game1.TemporaryRand.Next(Game1.Colors.Count())], new List<string>() { "bell", "mantle" }, new List<string>() { "allunalike" }, 50, "tentacle1", "tentacle2", this));
            Races.AddRange(ColossalTypes);

            //generate random animal races

            for (int i = Game1.TemporaryRand.Next(15, 25); i != 0; i--)
            {
                List<(string, Material)> BodyParts = new List<(string, Material)>();

                // Randomizing the number of legs, wings, and other body parts
                int Legs = Game1.TemporaryRand.Next(1, 6) * 2;
                int Wings = Game1.TemporaryRand.Next(0, 3); // Fixed syntax error here
                int Horns = RandomChance(10) ? Game1.TemporaryRand.Next(1, 3) : 0; // 10% chance to have 1-2 horns
                int Eyes = Game1.TemporaryRand.Next(1, 6);
                int Antennae = RandomChance(10) ? Game1.TemporaryRand.Next(1, 3) : 0;
                int Tentacles = RandomChance(10) ? Game1.TemporaryRand.Next(1, 5) : 0;
                int Tails = RandomChance(10) ? Game1.TemporaryRand.Next(1, 3) : 0;
                int Humps = RandomChance(10) ? Game1.TemporaryRand.Next(1, 3) : 0;
                int Fins = RandomChance(10) ? Game1.TemporaryRand.Next(1, 4) : 0;
                int Tusks = RandomChance(10) ? Game1.TemporaryRand.Next(1, 3) : 0;
                int Spikes = RandomChance(10) ? Game1.TemporaryRand.Next(1, 6) : 0; // 10% chance to have 1-6 spikes
                int Teeth = RandomChance(80) ? Game1.TemporaryRand.Next(12, 48) : 0;

                // Randomizing other attributes
                string Size = Game1.AnimalSizes[Game1.TemporaryRand.Next(Game1.AnimalSizes.Count())];
                bool HasScales = Game1.TemporaryRand.Next(0, 2) == 1;
                bool HasGills = Game1.TemporaryRand.Next(0, 2) == 1;
                bool HasFur = Game1.TemporaryRand.Next(0, 2) == 1;
                bool HasFeathers = Game1.TemporaryRand.Next(0, 2) == 1;
                bool HasHooves = Game1.TemporaryRand.Next(0, 2) == 1;
                bool HasShell = Game1.TemporaryRand.Next(0, 2) == 1;

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
                Race r = new Race("", Size, BodyParts, Game1.Colors[Game1.TemporaryRand.Next(Game1.Colors.Count())], new List<string> { "head", "body" }, new List<string>() { "allunalike" }, Game1.TemporaryRand.Next(0,6), BodyParts[Game1.TemporaryRand.Next(BodyParts.Count())].Item1, BodyParts[Game1.TemporaryRand.Next(BodyParts.Count())].Item1, this);
                r.Name = GenerateUniqueName("1S" + Game1.TemporaryRand.Next(5) + "s", r, Game1.TemporaryRand);
                WildRaces.Add(r);
            }

            foreach (Race r in Races)
            {
                r.RaceLetter = Race.GenerateUniqueAbbreviation(r.Name, this.Races);
                
                foreach(string s in r.BodyPartNames)
                {
                    if(!AllBodyParts.Any(e => e.Metadata == s))
                    {
                        AllBodyParts.Add(new Entity(s));
                    }
                }
            }

            //utilize elevation map

            // Helper method to determine if a feature is present based on a percentage chance
            bool RandomChance(int percent)
            {
                return Game1.TemporaryRand.Next(0, 100) < percent;
            }

            // Add the RandomSize method and other necessary components as per your game's requirements
            for (int i = Game1.TemporaryRand.Next(3, 6); i != 0; i--)
            {
                Blights.Add(new Blight(this));
            }

            Length = length;
            Width = width;

            WorldMap = new Region[(length * width)];

            float scale;

            LightDeity = new Deity(GenerateUniqueName("1S" + Game1.TemporaryRand.Next(7, 9) + "s", LightDeity, Game1.TemporaryRand), "light");
            DarkDeity = new Deity(GenerateUniqueName("1S" + Game1.TemporaryRand.Next(7, 9) + "s", DarkDeity, Game1.TemporaryRand), "dark");

            InitialCivCount = civCount;

            ElevationNoiseValues = new float[Width * Length];

            scale = 0.01f;
            SimplexNoise.Noise.Seed = seed + 1337;
            float[,] Array1 = SimplexNoise.Noise.Calc2D(width, length, scale);
            scale = 0.02f;
            SimplexNoise.Noise.Seed = seed + 1337*2;
            float[,] Array2 = SimplexNoise.Noise.Calc2D(width, length, scale);
            scale = 0.08f;
            SimplexNoise.Noise.Seed = seed + 1337*3;
            float[,] Array3 = SimplexNoise.Noise.Calc2D(width, length, scale);

            // Construct radial array
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
                    RadialArray[x + z * Width] += 15 * SizeVariant;

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
            SimplexNoise.Noise.Seed = seed + 1337 * 4;
            float[,] treeNoise2D = SimplexNoise.Noise.Calc2D(width, length, scale);
            TreeNoiseValues = ConvertTo1DArray(treeNoise2D);

            scale = 0.05f;
            SimplexNoise.Noise.Seed = seed + 1337 * 5;
            float[,] temperatureNoise2D = SimplexNoise.Noise.Calc2D(width, length, scale);
            TemperatureNoiseValues = ConvertTo1DArray(temperatureNoise2D);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < length; z++)
                {
                    int Elevation = (int)Math.Round(ElevationNoiseValues[x + z * Width] / 2.56);
                    int Temperature = (int)Math.Round(TemperatureNoiseValues[x + z * Width] / 2.56);
                    int Trees = (int)Math.Round(TreeNoiseValues[x + z * Width] / 2.56);

                    string Biome = "";

                    if (RadialArray[x + z * Width] < 76)
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

                    Region region = new Region(Biome, Elevation, Temperature, x, z, this);
                    WorldMap[x + z * Width] = region;
                    
                    region.Blight = Purity;
                }


            }


            rnd = Game1.TemporaryRand;
            Game1.TemporaryRand = null;
        }

        public void ProgressDays(decimal Days, bool IncreaseCycle)
        {
            int Month = ((int)Math.Round((decimal)(Cycle / 24192000)) % 12) + 1;
            int Year = (int)Math.Round((decimal)(Cycle / 290304000), MidpointRounding.ToZero);



            if (RndTracker.Count > 0 && RndTracker.Last() == 354)
            {
                //start tracking

                rnd.IsTracking = true;
            }
            else
            {
                if(rnd.IsTracking == true)
                {
                    //break here to make sure that we were trackign to begin with, then we can view the report on the death one.
                    int ShibeTimeStop = 1;
                }

                rnd.IsTracking = false;
            }





            if (RndTracker.Count < 40) //the amount we will test
                RndTracker.Add(rnd.Next(1000));
            else
            {
                int shibe = 1;
            }


            string Date = "(" + Month + "/" + Year + ")";

            int CurrentlyCountingArchitects = 0;

            void DetectIslandsAndPorts(Region[] worldMap, int width, List<EntityList<Region>> islands, List<EntityList<Region>> portLocations)
            {
                int length = worldMap.Count() / width; // Assuming worldMap.Length is a multiple of width
                bool[] visited = new bool[worldMap.Count()];

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
                int length = worldMap.Count() / width; // Calculate length based on total size and width
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
                int length = worldMap.Count() / width;
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
                foreach (Material m in Game1.MaterialsToAddToWorld)
                {
                    bool Added = Materials.TryAdd(m.Name, m);

                    if (!Added)
                    {
                        DupeMats.Add(m.Name);
                    }
                }

                Unknown = new Architect("Someone lost to time", null, GetRace(""), 0, null, null, null, null, null, null, 0, true);
                SubjectCatalogue.Add("Someone lost to time", Unknown);

                UndiscoveredSpells.AddRange(Game1.GameWorld.AllSpells);
                UndiscoveredLegendarySpells.AddRange(Game1.GameWorld.AllLegendarySpells);

                foreach (Civilization c in Civilizations)
                {
                    Location l = new Location("village", c.PrimaryInhabitantRace, rnd.Next(50, 100), 1000, rnd.Next(5, 8+((int)Math.Round(ProsperityMultiplier))), c.StartX, c.StartZ, c, WorldMap[c.StartX + c.StartZ * Width], "none");
                    AllLocations.Add(l);
                    l.IsCapitol = true;
                    c.Capitol = l;
                    ClaimSwathOfTerritory(c, l.X, l.Z, 2);

                    HistoricalEvents.Add(new Event($"{Date} {c.Name} sprung forth into the world as a united culture, manifesting in their capitol {l.Name}.", l.Region, new EntityList<Entity>() { c, l }));

                    Block chosenBlock = l.Districts[0].DistrictMap[rnd.Next(0, 49)];
                    Structure Prism = new Structure("prism", new EntityList<Object>(), new EntityList<Room>(), chosenBlock, new EntityList<Material> { c.CulturalStone }, new List<string>(), new List<string> { Game1.LightingStyles[rnd.Next(Game1.LightingStyles.Count())] }, rnd.Next(0, 5), rnd.Next(0, 4), (int)Math.Round(Cycle / 290304000));

                    chosenBlock.Structures.Add(Prism);
                    l.Prism = Prism;

                    for (int i = 0; i < rnd.Next(10, 20); i++)
                    {
                        Block ChosenBlock = l.Districts[0].DistrictMap[rnd.Next(0, 49)];
                        Structure s = new Structure("house", new EntityList<Object>(), new EntityList<Room>(), ChosenBlock, new EntityList<Material> { c.CulturalWood }, new List<string> { c.CulturalWood.Name }, new List<string> { Game1.LightingStyles[rnd.Next(Game1.LightingStyles.Count())] }, rnd.Next(0, 5), rnd.Next(0, 4), (int)Math.Round(Cycle / 290304000));

                        ChosenBlock.Structures.Add(s);
                    }

                    Block b = l.Districts[0].DistrictMap[rnd.Next(2, 6) + rnd.Next(2, 5) * 7];

                    b.Objects.Add(new Object(null, "well", new EntityList<Material> { l.HomeCivilization.CulturalStone }, true, true, null, null, 255, false, null, null, null, false));
                    b.Objects.Add(new Object(null, "shadow fountain", new EntityList<Material>() { Shadesteel }, DarkDeity));

                    WorldMap[c.StartX + c.StartZ * Width].Location = l;
                }

                AssignRealms();

                //generate the legendary colossals

                int ColossalCount = rnd.Next(8, 12);

                for (int i = 0; i < ColossalCount; i++)
                {

                    Architect a = new Architect("", Game1.Sexes[rnd.Next(Game1.Sexes.Count())], ColossalTypes[rnd.Next(ColossalTypes.Count())], 0, "end", new EntityList<Object>(), null, null, null, null, 7, true);

                    string Name = "";

                    if (rnd.Next(1, 3) == 1)
                    {
                        Name = GenerateUniqueName(String.Concat("1S", rnd.Next(2, 10), "s"), a, Game1.GameWorld.rnd);
                    }
                    else
                    {
                        Name = GenerateUniqueName(String.Concat("1S", rnd.Next(2, 5), "s 1S", rnd.Next(2, 5), "s"), a, Game1.GameWorld.rnd);
                    }


                    a.Name = Name;
                    a.IsColossal = true;

                    while (WorldMap[a.ColossalMinefieldX + a.ColossalMinefieldZ * Width].Biome == "ocean" || WorldMap[a.ColossalMinefieldX + a.ColossalMinefieldZ * Width].Biome == "void")
                    {
                        a.ColossalMinefieldX = rnd.Next(0, Length);
                        a.ColossalMinefieldZ = rnd.Next(0, Width);
                    }

                    Colossals.Add(a);

                    Unit e = new Unit(WorldMap[a.ColossalMinefieldX + a.ColossalMinefieldZ * Width], "colossal", new EntityList<Architect>() { a });
                    WorldMap[a.ColossalMinefieldX + a.ColossalMinefieldZ * Width].Units.Add(e);

                    AllUnits.Add(e);
                    HistoricalEvents.Add(new Event($"{Date} {a.Name}, a colossal {a.Race.Name}, began lying in wait in the {Game1.DeterminePointLocation(Width, Length, a.ColossalMinefieldX, a.ColossalMinefieldZ)}.", WorldMap[a.ColossalMinefieldX + a.ColossalMinefieldZ * Width], new EntityList<Entity>() { a }));

                }

                //superlords or whatever

                Hypernexus = new Architect("", "female", GetRace("hypernexus"), rnd.Next(5000, 20000), "sovereign", new EntityList<Object>(), null, null, null, null, 9, true);
                Hypernexus.Name = GenerateUniqueArchitectName(Hypernexus);
                Icosidodecahedron = new Architect("", "female", GetRace("icosidodecahedron"), rnd.Next(5000, 20000), "sovereign", new EntityList<Object>(), null, null, null, null, 9, true);
                Icosidodecahedron.Name = GenerateUniqueArchitectName(Icosidodecahedron);
                Shadeheart = new Architect("", "female", GetRace("shadeheart"), rnd.Next(5000, 20000), "heart", new EntityList<Object>(), null, null, null, null, 9, true);
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
                EntityList<LocationBuilderPacket> LocationBuilderPackets = new EntityList<LocationBuilderPacket>();


                //add historical abridged events

                if (AbridgedHistoricalEvents.Count() * 10 < Math.Round(Cycle / 290304000, 0, MidpointRounding.ToNegativeInfinity))
                {
                    AbridgedHistoricalEvents.Add(HistoricalEvents[HistoricalEvents.Count() - rnd.Next(1, 5)].EventData);
                }

                int MonthToDayConstant = (int)Math.Round(28 / Days);

                //place civilizations that need to be placed.

                void PlaceFancyCiv(Race race)
                {
                    //find a location to start at

                    int FoundX = 0;
                    int FoundZ = 0;
                    int Tries = 0;

                    while (Tries <= 100)
                    {
                        int TryX = rnd.Next(Width);
                        int TryZ = rnd.Next(Length);


                        // Check a square of size 5 centered around (TryX, TryZ)
                        bool validLocation = true;
                        for (int i = TryX - 3; i <= TryX + 3 && validLocation; i++)
                        {
                            for (int j = TryZ - 3; j <= TryZ + 3 && validLocation; j++)
                            {
                                // Check if any region inside the area's Region.Location is not equal to null
                                if (i <= 0 || i >= Width || j <= 0 || j >= Length ||
                                    WorldMap[i + j * Width].Biome == "ocean" || WorldMap[i + j * Width].Biome == "void" ||
                                    WorldMap[i + j * Width].Location != null)
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

                    if(FoundX != 0 && FoundZ != 0)
                    {

                        Civilization c = new Civilization(race, race.Name, FoundX, FoundZ, this);
                        Civilizations.Add(c);

                        if (race.Name == "photonexus" || race.Name == "shade" || race.Name == "isofractal")
                        {
                            Location l = new Location("core", race, rnd.Next(50, 100), 1000, rnd.Next(1, 5), c.StartX, c.StartZ, c, WorldMap[c.StartX + c.StartZ * Width], "none");
                            l.IsCapitol = true;
                            c.Capitol = l;
                            AllLocations.Add(l);
                            ClaimSwathOfTerritory(c, l.X, l.Z, 2);

                            if (race.Name == "shade")
                            {
                                Block chosenBlock = l.Districts[0].DistrictMap[rnd.Next(0, 49)];
                                Structure heart = new Structure("heart", new EntityList<Object>(), new EntityList<Room>(), chosenBlock, new EntityList<Material> { ShadeSludge }, new List<string> { c.CulturalGemstone.Name }, new List<string> { "veins" }, rnd.Next(0, 5), rnd.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                chosenBlock.Structures.Add(heart);
                                l.Prism = heart;

                                for (int i = 0; i < rnd.Next(10, 20); i++)
                                {
                                    chosenBlock = l.Districts[0].DistrictMap[rnd.Next(0, 49)];
                                    Structure scum = new Structure("scum", new EntityList<Object>(), new EntityList<Room>(), chosenBlock, new EntityList<Material> { ShadeSludge }, new List<string> { c.CulturalGemstone.Name }, new List<string> { "veins" }, rnd.Next(0, 5), rnd.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                    chosenBlock.Structures.Add(scum);
                                }
                            }
                            else if (race.Name == "isofractal" || race.Name == "photonexus")
                            {
                                // Place the core structure in the center
                                Block centerBlock = l.Districts[0].DistrictMap[24]; // Center block (3,3 in a 7x7 grid)
                                Structure core = new Structure("core", new EntityList<Object>(), new EntityList<Room>(), centerBlock, new EntityList<Material> { c.CulturalGemstone }, new List<string> { c.CulturalGemstone.Name }, new List<string> { "crystals" }, rnd.Next(0, 5), rnd.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                centerBlock.Structures.Add(core);
                                l.Prism = core;

                                int scaffoldCount = rnd.Next(3, 5); // Reduce the total number of scaffolds
                                HashSet<int> builtBlocks = new HashSet<int> { 24 }; // To avoid duplicate structures

                                for (int i = 0; i < scaffoldCount; i++)
                                {
                                    int x = rnd.Next(0, 7);
                                    int z = rnd.Next(0, 7);

                                    if (x == 3 && z == 3) continue; // Skip the center block

                                    // Create and place the initial scaffold structure
                                    int index = x + z * 7;
                                    if (!builtBlocks.Contains(index))
                                    {
                                        Block chosenBlock = l.Districts[0].DistrictMap[index];
                                        Structure scaffold = new Structure("scaffold", new EntityList<Object>(), new EntityList<Room>(), chosenBlock, new EntityList<Material> { c.CulturalGemstone }, new List<string> { c.CulturalGemstone.Name }, new List<string> { "crystals" }, rnd.Next(0, 5), rnd.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                        chosenBlock.Structures.Add(scaffold);
                                        builtBlocks.Add(index);
                                    }

                                    // Reflect across the X-axis
                                    int reflectedX = 6 - x;
                                    index = reflectedX + z * 7;
                                    if (x != 3 && !builtBlocks.Contains(index))
                                    {
                                        Block reflectedBlockX = l.Districts[0].DistrictMap[index];
                                        Structure reflectedStructureX = new Structure("scaffold", new EntityList<Object>(), new EntityList<Room>(), reflectedBlockX, new EntityList<Material> { c.CulturalGemstone }, new List<string> { c.CulturalGemstone.Name }, new List<string> { "crystals" }, rnd.Next(0, 5), rnd.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                        reflectedBlockX.Structures.Add(reflectedStructureX);
                                        builtBlocks.Add(index);
                                    }

                                    // Reflect across the Z-axis
                                    int reflectedZ = 6 - z;
                                    index = x + reflectedZ * 7;
                                    if (z != 3 && !builtBlocks.Contains(index))
                                    {
                                        Block reflectedBlockZ = l.Districts[0].DistrictMap[index];
                                        Structure reflectedStructureZ = new Structure("scaffold", new EntityList<Object>(), new EntityList<Room>(), reflectedBlockZ, new EntityList<Material> { c.CulturalGemstone }, new List<string> { c.CulturalGemstone.Name }, new List<string> { "crystals" }, rnd.Next(0, 5), rnd.Next(0, 4), (int)Math.Round(Cycle / 290304000));
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
                                            Structure reflectedStructureBoth = new Structure("scaffold", new EntityList<Object>(), new EntityList<Room>(), reflectedBlockBoth, new EntityList<Material> { c.CulturalGemstone }, new List<string> { c.CulturalGemstone.Name }, new List<string> { "crystals" }, rnd.Next(0, 5), rnd.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                            reflectedBlockBoth.Structures.Add(reflectedStructureBoth);
                                            builtBlocks.Add(index);
                                        }
                                    }
                                }
                            }

                            l.Districts[0].DistrictMap[rnd.Next(2, 6) + rnd.Next(2, 5) * 7].Objects.Add(new Object(null, "well", new EntityList<Material> { l.HomeCivilization.CulturalStone }, true, true, null, null, 255, false, null, null, null, false));

                            WorldMap[c.StartX + c.StartZ * Width].Location = l;

                            if (race.Name == "photonexus")
                            {
                                l.HomeCivilization.Citizens.Add(Hypernexus);
                                l.Government = Hypernexus;
                                l.Districts[0].Architects.Add(Hypernexus);
                                HistoricalEvents.Add(new Event($"{Date} The nexus of perfection, {l.Name}, fell to the land, controlled by the sovereign nexus {Hypernexus.Name}.", l.Region, new EntityList<Entity>() { l, Hypernexus }));
                            }
                            else if (race.Name == "isofractal")
                            {
                                l.HomeCivilization.Citizens.Add(Icosidodecahedron);
                                l.Government = Icosidodecahedron;
                                l.Districts[0].Architects.Add(Icosidodecahedron);
                                HistoricalEvents.Add(new Event($"{Date} The prism of expression, {l.Name}, was forged as a beacon for a creative reality, manifesting under the control of {Icosidodecahedron.Name}.", l.Region, new EntityList<Entity>() { l, Icosidodecahedron }));
                            }
                            else if (race.Name == "shade")
                            {
                                l.HomeCivilization.Citizens.Add(Shadeheart);
                                l.Government = Shadeheart;
                                l.Districts[0].Architects.Add(Shadeheart);
                                HistoricalEvents.Add(new Event($"{Date} The heart of corruption, {l.Name}, erupted from the depths of the world, establishing a cluster of ruinous veins in {Shadeheart.Name}.", l.Region, new EntityList<Entity>() { l, Shadeheart }));
                            }
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
                                        WorldMap[i + j * Width].Location != null)
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

                                        if (docksideOptions.Count() > 0)
                                        {
                                            dockside = docksideOptions[rnd.Next(docksideOptions.Count())];
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

                    if (validLocations.Count() > 0)
                    {
                        // Pick a random location from the valid locations
                        var (FoundX, FoundZ, FoundDockside) = validLocations[rnd.Next(validLocations.Count())];

                        Civilization c = new Civilization(GetRace(""), Type, FoundX, FoundZ, this);

                        // Find the perfect specimen
                        EntityList<Architect> PossibleArch = new EntityList<Architect>();
                        foreach (Architect a in AllHistoricalArchitects)
                        {
                            if (a.Group == null && !Calamity.Contains(a) && a.IsAlive)
                            {
                                PossibleArch.Add(a);
                                break;
                            }
                        }

                        c.Alpha = PossibleArch[rnd.Next(PossibleArch.Count())];
                        c.EntityColor = "black";

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
                        HistoricalEvents.Add(new Event($"{Date} The world was too chaotic for even the {Type}s to stand.", null, new EntityList<Entity>() { }));
                    }
                }





                if (Cycle / 290304000 > LostFoundingYear && LostPlaced == false)
                {
                    int StartX = 0;
                    int StartZ = 0;

                    while (WorldMap[StartX + StartZ * Width].Biome == "ocean" || WorldMap[StartX + StartZ * Width].Biome == "void" || WorldMap[StartX + StartZ * Width].Location != null)
                    {
                        StartX = rnd.Next(Width);
                        StartZ = rnd.Next(Length);
                    }

                    Civilization c = new Civilization(GetRace("archaix"), "archaix", StartX, StartZ, this);
                    Civilizations.Add(c);
                    LostPlaced = true;

                    Location l = new Location("village", GetRace("archaix"), rnd.Next(50, 100), 1000, rnd.Next(4, 8), c.StartX, c.StartZ, c, WorldMap[c.StartX + c.StartZ * Width], "none");
                    AllLocations.Add(l);
                    l.IsCapitol = true;
                    c.Capitol = l;
                    ClaimSwathOfTerritory(c, l.X, l.Z, 2);

                    Block chosenBlock = l.Districts[0].DistrictMap[rnd.Next(0, 49)];
                    Structure Prism = new Structure("prism", new EntityList<Object>(), new EntityList<Room>(), chosenBlock, new EntityList<Material> { c.CulturalStone }, new List<string>(), new List<string> { Game1.LightingStyles[rnd.Next(Game1.LightingStyles.Count())] }, rnd.Next(0, 5), rnd.Next(0, 4), (int)Math.Round(Cycle / 290304000));

                    chosenBlock.Structures.Add(Prism);
                    l.Prism = Prism;

                    for (int i = 0; i < rnd.Next(10, 20); i++)
                    {
                        Block ChosenBlock = l.Districts[0].DistrictMap[rnd.Next(0, 49)];
                        Structure s = new Structure("house", new EntityList<Object>(), new EntityList<Room>(), ChosenBlock, new EntityList<Material> { c.CulturalWood }, new List<string> { c.CulturalWood.Name }, new List<string> { Game1.LightingStyles[rnd.Next(Game1.LightingStyles.Count())] }, rnd.Next(0, 5), rnd.Next(0, 4), (int)Math.Round(Cycle / 290304000));

                        ChosenBlock.Structures.Add(s);
                    }

                    Block b = l.Districts[0].DistrictMap[rnd.Next(2, 6) + rnd.Next(2, 5) * 7];

                    b.Objects.Add(new Object(null, "well", new EntityList<Material> { l.HomeCivilization.CulturalStone }, true, true, null, null, 255, false, null, null, null, false));
                    b.Objects.Add(new Object(null, "shadow fountain", new EntityList<Material>() { Shadesteel }, DarkDeity));

                    WorldMap[c.StartX + c.StartZ * Width].Location = l;
                }

                long quarterCycle = (MaxAge * 290304000L) / 4;

                if (Cycle > quarterCycle && FirstNewCivPlaced == false)
                {
                    FirstNewCivPlaced = true;
                    PlaceFancyCiv(ExtraRaces[0]);
                }
                else if (Cycle > (quarterCycle * 2) && SecondNewCivPlaced == false)
                {
                    SecondNewCivPlaced = true;
                    PlaceFancyCiv(ExtraRaces[1]);
                }
                else if (Cycle > (quarterCycle * 3) && ThirdNewCivPlaced == false)
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

                double currentYear = (int)Math.Round((decimal)(Cycle / 290304000)); // Current year
                double lastRunYear = (int)Math.Round((decimal)(LastFunctionRunCycle / 290304000)); // Last year the function was run

                if (Cycle > 290304000 * CalamityStartingYear && Calamity.Count() == 0)
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
                        CalamityIdeologicalObsession = new List<string>() { "dominator", "killer", "kidnapper", "corruptor", "diplomancer", "inciter", "power" }[rnd.Next(7)]; //MAKE SURE WE CHANGE THIS BACK rnd.Next(0,9)
                    }
                    else if (LockedInThreat == "random")
                    {
                        CalamityIdeologicalObsession = new List<string>() { "disease", "dominator", "purifier", "killer", "kidnapper", "corruptor", "diplomancer", "inciter", "power" }[rnd.Next(9)]; //MAKE SURE WE CHANGE THIS BACK rnd.Next(0,9)
                    }
                    else
                    {
                        CalamityIdeologicalObsession = LockedInThreat;
                    }

                    if (ideologicalReasonings.ContainsKey(CalamityIdeologicalObsession))
                    {
                        var reasoningsList = ideologicalReasonings[CalamityIdeologicalObsession];
                        CalamityReasoning = reasoningsList[rnd.Next(reasoningsList.Count())]; // Select a random reasoning ID from the list
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

                    string name = String.Concat(
                        FirstPartNames[rnd.Next(FirstPartNames.Count())],
                        SecondPartNames[rnd.Next(SecondPartNames.Count())],
                        " the ",
                        Adjectives[rnd.Next(Adjectives.Count())],
                        " ",
                        (codeNameThemes[CalamityIdeologicalObsession])[rnd.Next(codeNameThemes[CalamityIdeologicalObsession].Count())]
                    );

                    var eligibleCivilizations = Civilizations.Where(c => HumanoidRaces.Contains(c.PrimaryInhabitantRace)).ToList();
                    if (eligibleCivilizations.Count > 0)
                    {
                        var selectedCivilization = eligibleCivilizations[rnd.Next(eligibleCivilizations.Count)];
                        Calamity.Add(new Architect(
                            name,
                            Game1.Sexes[rnd.Next(2)],
                            HumanoidRaces[rnd.Next(HumanoidRaces.Count)],
                            rnd.Next(13, 34),
                            "calamity",
                            new EntityList<Object>(),
                            selectedCivilization.Capitol,
                            null,
                            null,
                            "",
                            10, true
                        ));
                    }
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
                    for (int i = rnd.Next(2, 5); i != 0; i--)
                    {
                        int Index = rnd.Next(expositions.Count());
                        DeterminedExpositions.Add(expositions[Index]);
                        expositions.RemoveAt(Index);
                    }

                    CalamityLore.Add(Game1.Capitalize(Calamity[0].Pronoun + " " + Game1.FormatAndList(DeterminedExpositions) + "."));
                    CalamityLore.Add(motivationsExposition[CalamityReasoning] + " " + Calamity[0].Pronoun + " " + motivationGoals[CalamityIdeologicalObsession][rnd.Next(3)] + ".");

                    //find a spot to build a base

                    bool FoundSpot = false;
                    while (!FoundSpot)
                    {
                        int X = rnd.Next(Width);
                        int Z = rnd.Next(Length);
                        if (WorldMap[X + Z * Width].Biome != "ocean" && WorldMap[X + Z * Width].Biome != "void" && WorldMap[X + Z * Width].Location == null)
                        {
                            FoundSpot = true;
                            LocationBuilderPacket l = new LocationBuilderPacket(Calamity[0], X, Z, "stronghold", GetRace(""), 0, 0, Calamity[0].HomeLocation.HomeCivilization, new EntityList<Object>(), null, "none");
                            LocationBuilderPackets.Add(l);
                            foreach (string s in CalamityLore)
                            {
                                HistoricalEvents.Add(new Event($"{Date} {s}", WorldMap[X + Z * Width], new EntityList<Entity>() { Calamity[0] }));
                            }
                            HistoricalEvents.Add(new Event($"{Date} Calamity has risen. {Calamity[0].Name} is upon you.", WorldMap[X + Z * Width], new EntityList<Entity>() { Calamity[0] }));
                        }
                    }

                    Calamity[0].IsCalamity = true;
                    Calamity[0].Level = 10;

                    //set some important vars

                    if (CalamityIdeologicalObsession == "disease")
                    {
                        Calamity[0].BlightManipulated = Blights[rnd.Next(Blights.Count())];

                        if (Calamity[0].BlightManipulated.FoundingYear > Cycle / 290304000)
                        {
                            Calamity[0].BlightManipulated.FoundingYear = (int)(Math.Round(Cycle / 290304000));
                            Calamity[0].BlightManipulated.Spawned = true;

                            HistoricalEvents.Add(new Event($"{Date} {Calamity[0].Name} unleashed {Calamity[0].BlightManipulated} in {Calamity[0].Location}.", Calamity[0].Location.Region, new EntityList<Entity>() { Calamity[0], Calamity[0].BlightManipulated, Calamity[0].Location }));
                            Calamity[0].Location.Region.Blight = Calamity[0].BlightManipulated;
                            BlightedRegions.Add(Calamity[0].Location.Region);
                        }
                        else
                        {
                            HistoricalEvents.Add(new Event($"{Date} {Calamity[0].Name} began assisting the spread of {Calamity[0].BlightManipulated} in {Calamity[0].Location}.", Calamity[0].Location.Region, new EntityList<Entity>() { Calamity[0], Calamity[0].BlightManipulated, Calamity[0].Location }));
                            Calamity[0].Location.Region.Blight = Calamity[0].BlightManipulated;
                            BlightedRegions.Add(Calamity[0].Location.Region);
                        }
                    }


                    //leave society

                    Calamity[0].Contacts.Clear();

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


                        // recruit peoples

                        if (Calamitizer.CalamityAge >= Calamitizer.CalamitySpawnTime && Calamitizer.Level >= 4)
                        {
                            Calamitizer.CalamitySpawnTime = 2140000000; // Prevent future spawns

                            int Count = 0;
                            List<string> Types = new List<string>();

                            // Determine Count and Types based on Calamitizer.Level - 2
                            switch (Calamitizer.Level - 2)
                            {
                                case 8:
                                    Count = rnd.Next(4, 7);
                                    Types.AddRange(new List<string> { "archbard", "archluminary", "archartificer", "archduelist", "warlock", "sorcerer", "elemental", "hypernexus", "icosidodecahedron", "shadeheart", "necromancer", "spatiomancer", "perceptomancer", "conjumancer", "fractalmancer" });
                                    break;
                                case 6:
                                    Count = rnd.Next(3, 6);
                                    Types.AddRange(new List<string> { "necromancer", "spatiomancer", "perceptomancer", "conjumancer", "fractalmancer", "embezzler", "beast", "knight", "thief", "archmage", "beastmaster", "duelist", "luminary", "artificer", "bard", "mage", "largebeast", "spy", "diplomancer" });
                                    break;
                                case 4:
                                    Count = rnd.Next(2, 4);
                                    Types.AddRange(new List<string> { "scout", "animal", "hunter", "mercenary", "magician", "embezzler", "beast", "knight", "thief", "duelist", "luminary", "artificer", "bard", "mage", "largebeast", "spy", "diplomancer" });
                                    break;
                                case 2:
                                    Count = rnd.Next(2, 6);
                                    Types.AddRange(new List<string> { "scout", "animal", "hunter", "mercenary", "embezzler", "beast", "knight", "thief", "magician" });
                                    break;
                            }

                            List<string> SearchTheWorldTypes = new List<string> { "archartificer", "archbard", "archluminary", "archmage", "artificer", "bard", "luminary", "mage", "sorcerer", "warlock" };
                            List<string> CreateYourOwnTypes = new List<string> { "animal", "beast", "beastmaster", "conjumancer", "diplomancer", "duelist", "elemental", "embezzler", "fractalmancer", "hunter", "knight", "largebeast", "magician", "mercenary", "necromancer", "perceptomancer", "scout", "spatiomancer", "spy", "thief", "archduelist" };

                            for (int i = Count; i != 0; i--)
                            {
                                Architect FoundGuy = null;
                                bool foundInWorld = false;
                                string ChosenType = Types[rnd.Next(Types.Count)];

                                if (SearchTheWorldTypes.Contains(ChosenType))
                                {
                                    // Try to find an existing architect of the chosen type
                                    int searchAttempts = 3;

                                    while (searchAttempts > 0 && !foundInWorld)
                                    {
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
                                    // If not found, create a new architect from CreateYourOwnTypes
                                    List<string> filteredCreateYourOwnTypes = Types.Intersect(CreateYourOwnTypes).ToList();

                                    if (filteredCreateYourOwnTypes.Count == 0)
                                    {
                                        // Default to any CreateYourOwnType if none match the Types list
                                        filteredCreateYourOwnTypes = CreateYourOwnTypes;
                                    }

                                    ChosenType = filteredCreateYourOwnTypes[rnd.Next(filteredCreateYourOwnTypes.Count)];

                                    Race R = null;
                                    if (ChosenType == "animal" || ChosenType == "beast" || ChosenType == "largebeast")
                                    {
                                        R = WildRaces[rnd.Next(WildRaces.Count)];
                                    }
                                    else if (ChosenType == "elemental")
                                    {
                                        R = ConstructRaces[rnd.Next(ConstructRaces.Count)];
                                    }

                                    FoundGuy = new Architect("", Game1.Sexes[rnd.Next(Game1.Sexes.Count)], R ?? HumanoidRaces[rnd.Next(HumanoidRaces.Count)], rnd.Next(10, 80), ChosenType, new EntityList<Object>(), null, null, null, "", Calamitizer.Level - 2, true);
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
                                        X = rnd.Next(Width);
                                        Z = rnd.Next(Length);

                                        if (WorldMap[X + Z * Width].Location == null && WorldMap[X + Z * Width].Biome != "void" && WorldMap[X + Z * Width].Biome != "ocean" && WorldMap[X + Z * Width].Biome != "ethereal")
                                        {
                                            Found = true;
                                        }
                                    }

                                    FoundGuy.Inventory.AddRange(LootTableMachine("bosstreasure" + Math.Round((double)(FoundGuy.Level / 2), MidpointRounding.ToPositiveInfinity)));

                                    FoundGuy.AssignSpells();

                                    FoundGuy.Master = Calamitizer;


                                    //temporarily assign your location to your master's location
                                    FoundGuy.Location = Calamitizer.Location;
                                    FoundGuy.HomeLocation = Calamitizer.HomeLocation;


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

                                    FoundGuy.MasterRelation = Relations[rnd.Next(Relations.Count)];

                                    LocationBuilderPacket l = new LocationBuilderPacket(FoundGuy, X, Z, Type, GetRace(""), 0, 0, Civilizations[rnd.Next(Civilizations.Count)], LootTableMachine("bosstreasure" + Math.Round((double)(FoundGuy.Level / 2), MidpointRounding.ToPositiveInfinity)), AllLocations[rnd.Next(AllLocations.Count)], "none");
                                    LocationBuilderPackets.Add(l);

                                    FoundGuy.Strength = Math.Max(FoundGuy.Strength, FoundGuy.Level);
                                    FoundGuy.Dexterity = Math.Max(FoundGuy.Dexterity, FoundGuy.Level);
                                    FoundGuy.Agility = Math.Max(FoundGuy.Agility, FoundGuy.Level);
                                    FoundGuy.Endurance = Math.Max(FoundGuy.Endurance, FoundGuy.Level);
                                    FoundGuy.Creativity = Math.Max(FoundGuy.Creativity, FoundGuy.Level);
                                    FoundGuy.Charisma = Math.Max(FoundGuy.Charisma, FoundGuy.Level);
                                    FoundGuy.Focus = Math.Max(FoundGuy.Focus, FoundGuy.Level);

                                    FoundGuy.IsCalamity = true;

                                    HistoricalEvents.Add(new Event($"{Date} {Calamitizer.Name} recruited {FoundGuy.Name} as a {FoundGuy.MasterRelation} to serve {Calamitizer.ObjectivePronoun} and the almighty {Calamity[0].Name}.", FoundGuy.Location != null ? FoundGuy.Location.Region : Calamitizer.HomeLocation.Region, new EntityList<Entity>() { Calamitizer, FoundGuy, Calamity[0] }));

                                    FoundGuy.Contacts.Clear();
                                    Calamitizer.Contacts.Add(FoundGuy);
                                    FoundGuy.Contacts.Add(Calamitizer);

                                    CalamitiesToAdd.Add(FoundGuy);
                                }
                            }
                        }

                        // do actions

                        if (rnd.Next((30 - (Calamitizer.Level * 2)) * MonthToDayConstant) == 0 && Calamity[0].IsAlive)
                        {
                            // determine whether you want to move

                            if (rnd.Next(10 * MonthToDayConstant) == 1 || (CalamityIdeologicalObsession == "killer" && Calamitizer.InteractionLocation.TruePopulation() == 0))
                            {
                                // find a spot to move to
                                int Tries = 0;
                                while (Tries < 100)
                                {
                                    int XChange = rnd.Next(-10, 11);
                                    int ZChange = rnd.Next(-10, 11);

                                    if (!(Calamitizer.InteractionLocation.X + XChange < 0 || Calamitizer.InteractionLocation.X + XChange > Width - 1 || Calamitizer.InteractionLocation.Z + ZChange < 0 || Calamitizer.InteractionLocation.Z + ZChange > Length - 1))
                                    {
                                        if (WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].Biome != "ocean" && WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].Biome != "void" && WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].Location != null && SettlementTypes.Contains(WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].Location.Type))
                                        {
                                            HistoricalEvents.Add(new Event($"{Date} {Calamitizer.Name} shifted their focus to {WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].Location.Name}.", WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width], new EntityList<Entity>() { Calamitizer, WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].Location }));
                                            Calamitizer.InteractionLocation = WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].Location;
                                            break;
                                        }
                                    }
                                    Tries++;
                                }
                            }
                            else if (!CalamityStructures.Contains(Calamitizer.InteractionLocation.Type))
                            {
                                // inflict pain and suffering

                                District ChosenDistrict = Calamitizer.InteractionLocation.Districts[rnd.Next(Calamitizer.InteractionLocation.Districts.Count())];

                                if (ChosenDistrict.Population() > 0)
                                {
                                    // Use InitiateAction for spreading disease
                                    if (CalamityIdeologicalObsession == "disease")
                                    {
                                        WorldActionInitiator.InitiateAction(Game1.GameWorld, "spreaddisease", Calamitizer, new EntityList<Entity> { Calamitizer.InteractionLocation, ChosenDistrict });
                                    }
                                    // Use InitiateAction for taking over
                                    else if (CalamityIdeologicalObsession == "dominator")
                                    {
                                        WorldActionInitiator.InitiateAction(Game1.GameWorld, "takeover", Calamitizer, new EntityList<Entity> { Calamitizer.InteractionLocation, ChosenDistrict });
                                    }
                                    // Use InitiateAction for killing
                                    else if (CalamityIdeologicalObsession == "killer" && rnd.Next(3) == 1)
                                    {
                                        WorldActionInitiator.InitiateAction(Game1.GameWorld, "killassorted", Calamitizer, new EntityList<Entity> { Calamitizer.InteractionLocation, ChosenDistrict });
                                    }
                                    // Use InitiateAction for kidnapping
                                    else if (CalamityIdeologicalObsession == "kidnapper")
                                    {
                                        WorldActionInitiator.InitiateAction(Game1.GameWorld, "kidnapassorted", Calamitizer, new EntityList<Entity> { Calamitizer.InteractionLocation, ChosenDistrict });
                                    }
                                    // Use InitiateAction for corrupting
                                    else if (CalamityIdeologicalObsession == "corruptor")
                                    {
                                        Architect targetArchitect = ChosenDistrict.Architects
                                            .Where(a => !Calamity.Contains(a))
                                            .OrderBy(_ => rnd.Next())
                                            .FirstOrDefault();

                                        if (targetArchitect != null)
                                        {
                                            WorldActionInitiator.InitiateAction(Game1.GameWorld, "corrupt", Calamitizer, new EntityList<Entity> { Calamitizer.InteractionLocation, ChosenDistrict, targetArchitect });
                                        }
                                    }

                                    // Use InitiateAction for diplomacy
                                    else if (CalamityIdeologicalObsession == "diplomancer")
                                    {
                                        Architect targetArchitect = ChosenDistrict.Architects
                                            .Where(a => !Calamity.Contains(a))
                                            .OrderBy(_ => rnd.Next())
                                            .FirstOrDefault();

                                        if (targetArchitect != null)
                                        {
                                            WorldActionInitiator.InitiateAction(Game1.GameWorld, "diplomance", Calamitizer, new EntityList<Entity> { Calamitizer.InteractionLocation, ChosenDistrict, targetArchitect });
                                        }
                                    }

                                    // Use InitiateAction for inciting violence
                                    else if (CalamityIdeologicalObsession == "inciter")
                                    {
                                        WorldActionInitiator.InitiateAction(Game1.GameWorld, "incite", Calamitizer, new EntityList<Entity> { Calamitizer.InteractionLocation, ChosenDistrict });
                                    }
                                    // Use InitiateAction for harvesting energy
                                    else if (CalamityIdeologicalObsession == "power")
                                    {
                                        WorldActionInitiator.InitiateAction(Game1.GameWorld, "harvest", Calamitizer, new EntityList<Entity> { Calamitizer.InteractionLocation, ChosenDistrict });
                                    }
                                    // Handle rupturing the area
                                    else if (CalamityIdeologicalObsession == "purifier")
                                    {
                                        WorldActionInitiator.InitiateAction(Game1.GameWorld, "rupture", Calamitizer, new EntityList<Entity> { Calamitizer.InteractionLocation, ChosenDistrict });
                                    }
                                }
                            }

                        }
                    }

                    foreach (Architect a in CalamitiesToAdd)
                    {
                        if (!a.OppositionTags.Contains("intruders"))
                        {
                            a.OppositionTags.Add("intruders");
                        }

                        Calamity.Add(a);
                    }
                }




                //lets do letters

                if(rnd.Next(1,50*MonthToDayConstant) == 1)
                {
                    Architect msg = null;

                    foreach(Architect a in AllHistoricalArchitects)
                    {
                        if(a.Group == null && !Calamity.Contains(a) && a.IsAlive)
                        {
                            msg = a;
                        }
                    }

                    if(msg != null)
                    {
                        if(rnd.Next(2) == 1)
                        {
                            UndergroundMessengers.Add(msg);
                            HistoricalEvents.Add(new Event($"{Date} {msg.Name} became an underground messenger and began traveling the world.", (msg.Location != null ? msg.Location : (msg.HomeLocation != null ? msg.HomeLocation : AllLocations[rnd.Next(AllLocations.Count)])).Region, new EntityList<Entity>() { msg }));
                        }
                        else
                        {
                            RegularMessengers.Add(msg);
                            HistoricalEvents.Add(new Event($"{Date} {msg.Name} became a regular messenger and began traveling the world.", (msg.Location != null ? msg.Location : (msg.HomeLocation != null ? msg.HomeLocation : AllLocations[rnd.Next(AllLocations.Count)])).Region, new EntityList<Entity>() { msg }));
                        }
                    }
                }

                //start delivering letters


                List<(EntityList<Letter> Letters, EntityList<Architect> Messengers, string MessengerType)> messengerGroups = new()
{
    (RegularLetters, RegularMessengers, "regular"),
    (UndergroundLetters, UndergroundMessengers, "underground")
};

                foreach (var (Letters, Messengers, MessengerType) in messengerGroups)
                {
                    foreach (Architect Messenger in Messengers)
                    {
                        if (rnd.Next(1, 330 * MonthToDayConstant) == 1 && Letters.Count > 0)
                        {
                            // Determine how many letters to retrieve (randomized, but not exceeding the available letters)
                            int letterCount = rnd.Next(1, Math.Min(5, Letters.Count) + 1); // Pick up to 5 letters if available
                            List<Letter> selectedLetters = Letters.Take(letterCount).ToList();
                            Letters.RemoveRange(0, letterCount);

                            // Prepare lists for historical event details
                            List<string> senders = new();
                            List<string> recipients = new();

                            foreach (Letter letter in selectedLetters)
                            {
                                // Add to historical event details
                                senders.Add(letter.Author.Name);
                                recipients.Add(letter.Recipient.Name);

                                // Find the location or home location of the recipient
                                Location targetLocation = letter.Recipient is Architect architectRecipient
                                    ? architectRecipient.Location ?? architectRecipient.HomeLocation
                                    : null;

                                // Create the object for the letter
                                Object o = new Object(
                                    $"Letter to {letter.Recipient.Name}",
                                    "scroll",
                                    new EntityList<Material>() { Cloths[rnd.Next(Cloths.Count)] },
                                    letter.Author
                                );

                                // Set the letter content to the object
                                o.LetterContent = letter;

                                // Determine where to store the object
                                if (targetLocation != null && rnd.Next(1, 11) != 1 && targetLocation.AllStructures.Count > 0) // 10% check, if it fails we put it in the recipient's inventory anyway
                                {
                                    // Store in a random structure's historical objects at the location
                                    var structure = targetLocation.AllStructures[rnd.Next(targetLocation.AllStructures.Count)];
                                    structure.HistoricalObjects.Add(o);
                                }
                                else
                                {
                                    // Determine the inventory of the recipient
                                    EntityList<Object> recipientInventory = null;

                                    if (letter.Recipient is Architect recipientArchitect)
                                    {
                                        // Recipient is an Architect
                                        recipientInventory = recipientArchitect.Inventory;
                                    }
                                    else if (letter.Recipient is Group recipientGroup)
                                    {
                                        // Recipient is a Group, use the Leader's inventory
                                        recipientInventory = recipientGroup.Leader.Inventory;
                                    }
                                    else if (letter.Recipient is Faction recipientFaction)
                                    {
                                        // Recipient is a Faction, use a random Satellite Group's Leader's inventory
                                        var randomSatelliteGroup = recipientFaction.SatelliteGroups[rnd.Next(recipientFaction.SatelliteGroups.Count)];
                                        recipientInventory = randomSatelliteGroup.Leader.Inventory;
                                    }

                                    // Add the object to the determined inventory
                                    recipientInventory?.Add(o);
                                }
                            }

                            // Create historical event
                            string sendersSample = string.Join(", ", senders.Take(3)) + (senders.Count > 3 ? ", and others" : "");
                            string recipientsSample = string.Join(", ", recipients.Take(3)) + (recipients.Count > 3 ? ", and others" : "");
                            HistoricalEvents.Add(new Event(
                                $"{Date}: {Messenger.Name} ({MessengerType} messenger) delivered {letterCount} letters. " +
                                $"Notable senders included {sendersSample}, while notable recipients were {recipientsSample}.",
                                Messenger.Location?.Region ?? (Messenger.HomeLocation?.Region ?? AllLocations[rnd.Next(AllLocations.Count)].Region),
                                new EntityList<Entity>() { Messenger }
                            ));
                        }
                    }
                }





                foreach (Civilization c in Civilizations)
                {
                    //abdicate if dead lul

                    if (c.Alpha != null && c.Alpha.IsAlive == false)
                    {
                        HistoricalEvents.Add(new Event($"{Date} {c.Alpha.Name}'s death led {c.Name} to put a new alpha into place.", c.Capitol.Region, new EntityList<Entity>() { c.Alpha, c }));

                        c.Alpha = null;
                    }

                    if (HumanoidRaces.Contains(c.PrimaryInhabitantRace))
                    {
                        c.CyclesTillElection -= (int)Math.Round(864000 * Days);
                        if ((c.Alpha == null || c.CyclesTillElection < 0) && c.Citizens.Count() > 0)
                        {
                            // Filter candidates whose home location matches the civilization's capital and who are alive
                            var eligibleCandidates = c.Citizens.Where(citizen => citizen.Location == c.Capitol && citizen.IsAlive == true);
                            if (eligibleCandidates.Count() > 0)
                            {
                                var highestReputation = eligibleCandidates.Max(citizen => citizen.Reputation);
                                var candidates = eligibleCandidates.Where(citizen => citizen.Reputation == highestReputation);

                                Architect selectedAlpha = candidates[rnd.Next(candidates.Count())];

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
                                        HistoricalEvents.Add(new Event($"{Date} {OldAlpha.Name} ended their service as the alpha of {c.Name}.", c.Capitol.Region, new EntityList<Entity>() { OldAlpha, c }));
                                    }
                                    
                                    
                                    HistoricalEvents.Add(new Event($"{Date} The civilization of {c.Name} elected a new alpha, {c.Alpha.Name}.", c.Capitol.Region, new EntityList<Entity>() { c, c.Alpha }));


                                    





                                    c.Alpha.Profession = "alpha";

                                    if (c.Alpha.Location != c.Capitol)
                                    {
                                        c.Alpha.NextMigrationLocation = c.Capitol;
                                        c.Alpha.MigrationReason = "I am the alpha of " + c.Name + ", and I am traveling to its capitol, " + c.Capitol.Name + ".";
                                    }
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
                            if (rnd.Next(1, 101) <= 35)
                            {
                                for (int x = 0; x < Width; x++)
                                {
                                    for (int z = 0; z < Length; z++)
                                    {
                                        // Check for spire and suitable conditions
                                        if (WorldMap[x + z * Width].Biome != "void" &&
                                            WorldMap[x + z * Width].Biome != "ocean" &&
                                            WorldMap[x + z * Width].Location != null &&
                                            WorldMap[x + z * Width].Location.Type == "spire" && 
                                            WorldMap[x + z * Width].Location.Government != null)
                                        {
                                            b.Spawned = true;
                                            WorldMap[x + z * Width].Blight = b;
                                            BlightedRegions.Add(WorldMap[x + z * Width]);

                                            var relevantEntities = new EntityList<Entity>() { WorldMap[x + z * Width].Location.Government, b, WorldMap[x + z * Width].Location };

                                            if (((Architect)(WorldMap[x + z * Width].Location.Government)).Profession == "sorcerer")
                                            {
                                                relevantEntities.Add(LightDeity);
                                                HistoricalEvents.Add(new Event($"{Date} {WorldMap[x + z * Width].Location.Government.Name}, sorcerer of {LightDeity.Name}, \"blessed\" the land with the {b.Name} from {WorldMap[x + z * Width].Location.Name}.", WorldMap[x + z * Width], relevantEntities));
                                            }
                                            else
                                            {
                                                relevantEntities.Add(DarkDeity);
                                                HistoricalEvents.Add(new Event($"{Date} {WorldMap[x + z * Width].Location.Government.Name}, warlock of {DarkDeity.Name}, \"cursed\" the land with the {b.Name} from {WorldMap[x + z * Width].Location.Name}.", WorldMap[x + z * Width], relevantEntities));
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
                                int SpawnTryX = rnd.Next(Width);
                                int SpawnTryZ = rnd.Next(Length);

                                // Existing logic for spawning at a random location
                                if (WorldMap[SpawnTryX + SpawnTryZ * Width].Biome != "void" &&
                                    WorldMap[SpawnTryX + SpawnTryZ * Width].Biome != "ocean")
                                {
                                    b.Spawned = true;
                                    WorldMap[SpawnTryX + SpawnTryZ * Width].Blight = b;
                                    BlightedRegions.Add(WorldMap[SpawnTryX + SpawnTryZ * Width]);

                                    // Keep history logic just in case it just so happens to start there.
                                    var relevantEntities = new EntityList<Entity>() { b };

                                    if (WorldMap[SpawnTryX + SpawnTryZ * Width].Location != null)
                                    {
                                        relevantEntities.Add(WorldMap[SpawnTryX + SpawnTryZ * Width].Location);

                                        if (WorldMap[SpawnTryX + SpawnTryZ * Width].Location.Type == "spire")
                                        {
                                            relevantEntities.Add(WorldMap[SpawnTryX + SpawnTryZ * Width].Location.Government);

                                            if (((Architect)(WorldMap[SpawnTryX + SpawnTryZ * Width].Location.Government)).Profession == "sorcerer")
                                            {
                                                relevantEntities.Add(LightDeity);
                                                HistoricalEvents.Add(new Event($"{Date} {WorldMap[SpawnTryX + SpawnTryZ * Width].Location.Government.Name}, sorcerer of {LightDeity.Name}, \"blessed\" the land with the {b.Name} from {WorldMap[SpawnTryX + SpawnTryZ * Width].Location.Name}.", WorldMap[SpawnTryX + SpawnTryZ * Width].Location.Region, relevantEntities));
                                            }
                                            else
                                            {
                                                relevantEntities.Add(DarkDeity);
                                                HistoricalEvents.Add(new Event($"{Date} {WorldMap[SpawnTryX + SpawnTryZ * Width].Location.Government.Name}, warlock of {DarkDeity.Name}, cursed the land with the {b.Name} from {WorldMap[SpawnTryX + SpawnTryZ * Width].Location.Name}.", WorldMap[SpawnTryX + SpawnTryZ * Width].Location.Region, relevantEntities));
                                            }
                                        }
                                        else
                                        {
                                            HistoricalEvents.Add(new Event($"{Date} Something began to spread in {WorldMap[SpawnTryX + SpawnTryZ * Width].Location.Name}, corrupting all it saw fit. The locals named the blight {b.Name}.", WorldMap[SpawnTryX + SpawnTryZ * Width].Location.Region, relevantEntities));
                                        }
                                    }
                                    else
                                    {
                                        HistoricalEvents.Add(new Event($"{Date} A terrible force of nature brought about the blight of {b.Name}.", WorldMap[SpawnTryX + SpawnTryZ * Width], relevantEntities));
                                    }
                                }
                            }
                        }
                    }
                }




                //spread blight


                // Create a list to store regions to be added after the iteration
                List<Region> regionsToAdd = new List<Region>();

                foreach (Region R in BlightedRegions)
                {
                    if (rnd.Next(1, 400 * MonthToDayConstant) == 1)
                    {
                        if (R.Blight != Purity)
                        {
                            // pick a random number between 1-4 and spread cardinally
                            int Spread = rnd.Next(4);
                            if (Spread == 1)
                            {
                                // spread north if available
                                if (R.Z > 0)
                                {
                                    WorldMap[(R.X) + (R.Z - 1) * Width].Blight = WorldMap[R.X + R.Z * Width].Blight;
                                    regionsToAdd.Add(WorldMap[(R.X) + (R.Z - 1) * Width]);
                                }
                            }
                            else if (Spread == 2)
                            {
                                // spread east if available
                                if (R.X < Width - 1)
                                {
                                    WorldMap[(R.X + 1) + (R.Z) * Width].Blight = WorldMap[R.X + R.Z * Width].Blight;
                                    regionsToAdd.Add(WorldMap[(R.X + 1) + (R.Z) * Width]);
                                }
                            }
                            else if (Spread == 3)
                            {
                                // spread south if available
                                if (R.Z < Length - 1)
                                {
                                    WorldMap[(R.X) + (R.Z + 1) * Width].Blight = WorldMap[R.X + R.Z * Width].Blight;
                                    regionsToAdd.Add(WorldMap[(R.X) + (R.Z + 1) * Width]);
                                }
                            }
                            else
                            {
                                // spread west if available
                                if (R.X > 0)
                                {
                                    WorldMap[(R.X - 1) + (R.Z) * Width].Blight = WorldMap[R.X + R.Z * Width].Blight;
                                    regionsToAdd.Add(WorldMap[(R.X - 1) + (R.Z) * Width]);
                                }
                            }
                        }
                    }
                }

                // Add new regions to BlightedRegions after the loop
                BlightedRegions.AddRange(regionsToAdd);

                //try to merge a group together

                // 2% chance to pick a group type
                if (rnd.Next(1, 50 * MonthToDayConstant) == 1)
                {
                    // Pick a random group type from the predefined list
                    string selectedGroupType = GroupTypes.Keys.ElementAt(rnd.Next(GroupTypes.Count));

                    // Find all groups of the selected type
                    IEnumerable<Group> groupsOfType = Groups
                        .Where(g => g.Type == selectedGroupType)
                        .GroupBy(g => g.Leader.Location)
                        .Where(g => g.Count() > 1)
                        .SelectMany(g => g);

                    // Randomly shuffle the groups to ensure randomness
                    List<Group> shuffledGroupsOfType = groupsOfType.OrderBy(g => rnd.Next()).ToList();

                    // Attempt to merge the first pair found sharing the same location
                    for (int i = 0; i < shuffledGroupsOfType.Count - 1; i++)
                    {
                        Group g1 = shuffledGroupsOfType[i];
                        Group g2 = shuffledGroupsOfType[i + 1];

                        if (g1.Leader.Location == g2.Leader.Location)
                        {
                            // Merge groups g1 and g2
                            HistoricalEvents.Add(new Event($"{Date} {g1.Name} and {g2.Name} started talking about merging their groups.", g1.Leader.Location.Region, new EntityList<Entity>(){g1, g2}));

                            Group mergedGroup = new Group(new EntityList<Architect>(), g1.Type, g2.Leader, g1.Leader.Location);
                            EntityList<Architect> joiners = new EntityList<Architect>();
                            EntityList<Architect> leavers = new EntityList<Architect>();

                            IEnumerable<Architect> allArchitects = g1.Architects.Union(g2.Architects);

                            foreach (Architect architect in allArchitects)
                            {
                                if (rnd.Next(1, 10) == 1 && architect != mergedGroup.Leader)
                                {
                                    HistoricalEvents.Add(new Event($"{Date} {architect.Name} disagreed with the idea of merging groups and left them both to settle it themselves.", g1.Leader.Location.Region, new EntityList<Entity>(){architect}));
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
                                g1.Leader.Location.Districts[0].DistrictMap[rnd.Next(0, 49)].Architects.Add(architect);
                            }

                            mergedGroup.Reputation = (g1.Reputation + g2.Reputation) / 2;

                            HistoricalEvents.Add(new Event($"{Date} {g1.Name} and {g2.Name} {GroupTypes[g1.Type]}, going under the name {mergedGroup.Name}.", g1.Leader.Location.Region, new EntityList<Entity>(){g1, g2, mergedGroup}));

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
                    g.DaysOld += (double)Days;

                    if(g.WaitingForCooldownToTrade == true)
                    {
                        if(rnd.Next(1,10) == 1)
                        {
                            g.WaitingForCooldownToTrade = false;
                        }
                    }
                }

                void SummonNewDivision(Location l)
                {
                    // Find fit men/maybe women
                    EntityList<Architect> allArchitects = new EntityList<Architect>();

                    // Iterate over all districts and add their architects to the new list
                    foreach (var district in l.Districts)
                    {
                        allArchitects.AddRange(district.Architects.Where(a => a.Division != null));
                    }

                    // Sort the architects by Strength * Dexterity * Agility
                    allArchitects = allArchitects
                        .OrderByDescending(a => a.Strength * a.Dexterity * a.Agility);


                    // Decide a size in both regulars and normals
                    int totalSoldiers = (l.TruePopulation() / 100) + 4;
                    int importantSoldiers = (int)(totalSoldiers * 0.4);

                    EntityList<Architect> selectedArchitects = new EntityList<Architect>();


                    // Iterate through the sorted list of architects and select important people
                    foreach (var architect in allArchitects)
                    {
                        if (selectedArchitects.Count() >= importantSoldiers)
                        {
                            break;
                        }

                        if (architect.Sex == "male" && rnd.Next(2) == 0)
                        {
                            selectedArchitects.Add(architect);
                        }
                        else if (architect.Sex == "female" && rnd.Next(5) == 0)
                        {
                            selectedArchitects.Add(architect);
                        }
                    }

                    if (selectedArchitects.Count() > 0)
                    {
                        Division u = new Division(selectedArchitects[0], selectedArchitects, totalSoldiers - importantSoldiers, l.HomeCivilization);

                        l.Divisions.Add(u);

                        HistoricalEvents.Add(new Event(String.Concat(Date, " A new division called ", u.Name, ", led by ", u.Leader.Name, ", was forged in ", l.Name, "."), l.Region, new EntityList<Entity> { }));

                        foreach (Architect a in u.Architects)
                        {
                            HistoricalEvents.Add(new Event(String.Concat(Date, " ", a.Name, " joined ", u.Name, "."), l.Region, new EntityList<Entity> { }));
                        }
                    }

                }





                //in peace



                foreach (Location l in AllLocations)
                {
                    //develop millitary divisions
                    if(SettlementTypes.Contains(l.Type))
                    {
                        if (rnd.Next(1, 1000 * MonthToDayConstant) < (l.TruePopulation() - (l.Divisions.Count() * 250)))
                        {
                            SummonNewDivision(l);
                        }
                    }
                }

                //declare WAR

                Location GetCapitol(Civilization civilization, List<Location> allLocations)
                {
                    return allLocations.FirstOrDefault(l => l.IsCapitol && l.HomeCivilization.Type == civilization.Type);
                }

                foreach (Civilization c in Civilizations)
                {
                    string PrimaryHaterType = GenericHatredDictionary[c.WarType];
                    Location currentCapitol = GetCapitol(c, AllLocations);

                    if (c.WarType == "civilized") continue;

                    if (currentCapitol == null) continue;

                    foreach (Civilization hatedCivilization in Civilizations)
                    {
                        // Skip self and already at war civilizations
                        if (hatedCivilization == c || Wars.Any(war => (war.Civilization1 == c && war.Civilization2 == hatedCivilization) || (war.Civilization1 == hatedCivilization && war.Civilization2 == c)))
                            continue;

                        if (hatedCivilization.WarType == PrimaryHaterType)
                        {
                            Location hatedCapitol = GetCapitol(hatedCivilization, AllLocations);

                            if (hatedCapitol == null) continue;

                            // Calculate the distance between the capitols
                            double distance = Vector2.Distance(
                                new Vector2(currentCapitol.Region.X, currentCapitol.Region.Z),
                                new Vector2(hatedCapitol.Region.X, hatedCapitol.Region.Z)
                            );

                            // If distance is too far, skip this civilization (you can adjust this threshold)
                            if (distance >= 25) continue;

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

                            // Check for historical events and war declarations based on accumulated hatred points
                            int totalHatredPoints = c.HatredPoints[hatedCivilization.Type];
                            if (initialHatredPoints < 1000 && totalHatredPoints >= 1000)
                            {
                                // Declare war if hatred points exceed threshold
                                War newWar = new War(c, hatedCivilization);
                                Wars.Add(newWar); // Add to the list of active wars

                                c.HatredPoints.Clear();

                                // Log the war as a historical event
                                HistoricalEvents.Add(new Event($"{Date} {c.Name}, a {c.Type} society, declared war on {hatedCivilization.Name}, a {hatedCivilization.Type} society.", currentCapitol.Region, new EntityList<Entity>() { c, hatedCivilization }));
                            }
                        }
                    }
                }



                foreach (War war in Wars)
                {
                    // Procure new squads
                    EntityList<Location> Civ1LocationsThatHaveMilitary = new EntityList<Location>();
                    EntityList<Location> Civ2LocationsThatHaveMilitary = new EntityList<Location>();

                    foreach (Location l in AllLocations)
                    {
                        if (l.HomeCivilization == war.Civilization1 || l.HomeCivilization == war.Civilization2)
                        {
                            if (rnd.Next(1, 100 * MonthToDayConstant) < (l.TruePopulation() - (l.Divisions.Count() * 250)))
                            {
                                SummonNewDivision(l);
                            }
                        }

                        if (l.HomeCivilization == war.Civilization1 && l.Divisions.Count() > 0)
                        {
                            Civ1LocationsThatHaveMilitary.Add(l);
                        }
                        else if (l.HomeCivilization == war.Civilization2 && l.Divisions.Count() > 0)
                        {
                            Civ2LocationsThatHaveMilitary.Add(l);
                        }
                    }

                    // Then FIGHT
                    foreach (Location L1 in Civ1LocationsThatHaveMilitary)
                    {
                        if (rnd.Next(1, 5) == 1)
                        {
                            // Select the opposing location for battle
                            foreach (Location L2 in Civ2LocationsThatHaveMilitary)
                            {
                                if (L1.Divisions.Count() == 0 || L2.Divisions.Count() == 0)
                                {
                                    // No divisions left to fight
                                    break;
                                }

                                if (rnd.Next(1, 5) == 1)
                                {
                                    // Simulate a battle between unit1 and unit2, but now using a frontline region for the battle location

                                    // Select a random region from the frontline, with bias towards the center
                                    Region battleRegion = SelectFrontlineRegion(war.Frontline);

                                    // Select units to fight
                                    Division unit1 = L1.Divisions[rnd.Next(L1.Divisions.Count())];
                                    Division unit2 = L2.Divisions[rnd.Next(L2.Divisions.Count())];

                                    // Simulate the battle at the frontline region
                                    List<string> Data = unit1.Fight(unit2, battleRegion);

                                    foreach (string s in Data)
                                    {
                                        HistoricalEvents.Add(new Event($"{Date} {s}", battleRegion, new EntityList<Entity>() { }));
                                    }

                                    // Check if any units should disband
                                    if (unit1.Architects.Count() == 0 && unit1.OtherSoldiers == 0)
                                    {
                                        L1.Divisions.Remove(unit1);
                                    }

                                    if (unit2.Architects.Count() == 0 && unit2.OtherSoldiers == 0)
                                    {
                                        L2.Divisions.Remove(unit2);
                                    }
                                }
                            }
                        }
                    }

                    // Function to select a frontline region with bias towards the center
                    Region SelectFrontlineRegion(EntityList<Region> frontline)
                    {
                        // If no regions exist in the frontline, return null (this shouldn't happen in normal circumstances)
                        if (frontline == null || frontline.Count == 0)
                            return null;

                        // Calculate bias weight for center
                        int midIndex = frontline.Count / 2;

                        // Biasing the selection toward the center of the frontline
                        // Use a weighted random selection: regions closer to the center are more likely to be selected
                        // The weighting here gives more probability to regions close to the midpoint

                        List<Region> weightedFrontline = new List<Region>();
                        for (int i = 0; i < frontline.Count; i++)
                        {
                            // The closer to the center, the higher the weight (we'll add more instances of the region)
                            int weight = frontline.Count - Math.Abs(midIndex - i);
                            for (int j = 0; j < weight; j++)
                            {
                                weightedFrontline.Add(frontline[i]);
                            }
                        }

                        // Select a random region from the weighted list
                        return weightedFrontline[rnd.Next(weightedFrontline.Count)];
                    }


                }


                //age

                foreach (Architect V in AllHistoricalArchitects)
                {
                    if (V.Age > V.TerminalAge && !V.IsImmortal && V.IsAlive)
                    {
                        V.IsAlive = false;

                        if (V.DoIDieOfOldAge)
                        {
                            if (V.Location != null)
                            {
                                HistoricalEvents.Add(new Event($"{Date} {V.Name} died of old age at {V.Age} in {V.Location.Name}.", V.Location.Region, new EntityList<Entity>(){V, V.Location}));
                            }
                            else
                            {
                                HistoricalEvents.Add(new Event($"{Date} {V.Name} died of old age at {V.Age}.", null, new EntityList<Entity>(){ V }));
                            }
                        }
                        else
                        {
                            if (V.Location != null)
                            {
                                HistoricalEvents.Add(new Event($"{Date} {V.Name} {Game1.DeathCauses[rnd.Next(Game1.DeathCauses.Count())]} at {V.Age} in {V.Location.Name}.", V.Location.Region, new EntityList<Entity>(){ V, V.Location }));
                            }
                            else
                            {
                                HistoricalEvents.Add(new Event($"{Date} {V.Name} {Game1.DeathCauses[rnd.Next(Game1.DeathCauses.Count())]} at {V.Age}.", null, new EntityList<Entity>(){V }));
                            }
                        }

                        if (V.Group != null)
                        {
                            V.Group.ArchitectsToRemove.Add(V);
                        }

                        if (V.District != null)
                        {
                            V.District.Architects.Remove(V);
                        }
                    }
                }



                //go save your GOOD citizens

                foreach (Civilization c in Civilizations)
                {
                    if (rnd.Next(100 * MonthToDayConstant) == 1 && c.MillitaryDivisions.Count > 0)
                    {
                        // Sort the units by CombatStrength in descending order (strongest first)
                        c.MillitaryDivisions.Sort((x, y) => y.CombatStrength().CompareTo(x.CombatStrength()));

                        Division u;

                        if (c.MillitaryDivisions.Count > 3)
                        {
                            // Take the top 3 strongest units and pick a random one
                            var topThreeUnits = c.MillitaryDivisions.Take(3).ToList();
                            u = topThreeUnits[rnd.Next(topThreeUnits.Count)];
                        }
                        else
                        {
                            // If there are 3 or fewer units, take the strongest one
                            u = c.MillitaryDivisions.First();
                        }


                        foreach (Architect a in c.Citizens)
                        {
                            if (a.Reputation > 0 && a.Bound)
                            {
                                WorldActionInitiator.InitiateAction(this, "free", u, new EntityList<Entity>() { a, a.Location, a.District });
                            }
                        }
                    }
                }

                //free your friends

                foreach (Faction f in AllFactions)
                {
                    if (rnd.Next(100 * MonthToDayConstant) == 1)
                    {
                        List<Architect> farchitects = new List<Architect>();

                        foreach (Group g in f.SatelliteGroups)
                        {
                            farchitects.AddRange(g.Architects);
                        }

                        foreach (Architect a in farchitects)
                        {
                            if (a.Bound && f.SatelliteGroups.Any(g => !g.Incapacitated))
                            {
                                // Find a group within the faction that is not incapacitated
                                Group rescuingGroup = f.SatelliteGroups.FirstOrDefault(g => !g.Incapacitated);

                                if (rescuingGroup != null)
                                {
                                    // Initiate the action to free the bound Architect
                                    WorldActionInitiator.InitiateAction(this, "free", rescuingGroup, new EntityList<Entity>() {a.Location, a.District, a});
                                }
                            }
                        }
                    }
                }

                //save friends in  groups

                foreach (Group g in this.Groups)
                {
                    if (rnd.Next(100 * MonthToDayConstant) == 1)
                    {
                        EntityList<Architect> groupArchitects = g.Architects;

                        foreach (Architect a in groupArchitects)
                        {
                            if (a.Bound && this.Groups.Any(grp => !grp.Incapacitated))
                            {
                                // Find a group within the world that is not incapacitated
                                Group rescuingGroup = this.Groups.FirstOrDefault(grp => !grp.Incapacitated);

                                if (rescuingGroup != null)
                                {
                                    // Initiate the action to free the bound Architect
                                    WorldActionInitiator.InitiateAction(this, "free", rescuingGroup, new EntityList<Entity>() { a.Location, a.District, a });
                                }
                            }
                        }
                    }
                }


                // Helper method to format the list of names
                string FormatList(List<string> names)
                {
                    return names.Count switch
                    {
                        0 => "",
                        1 => names[0],
                        2 => $"{names[0]} and {names[1]}",
                        _ => string.Join(", ", names.Take(names.Count - 1)) + $", and {names.Last()}"
                    };
                }



                // Counter-Evil operations

                // Civilizations will litterally hire people to take out other people


                List<string> HireableGroups = new List<string>() { "military", "mercenary", "squad" };
                List<string> HireableArchs = new List<string>() { "mercenary", "hunter", "soldier", "warrior" };
                foreach (Civilization c in Civilizations)
                {
                    if (rnd.Next(100) == 0 && HyperThreats.Count > 0)
                    {
                        List<Entity> Hireables = new List<Entity>();

                        bool Success = true;

                        Hireables.AddRange(Groups.Where(g => HireableGroups.Contains(g.Type) && g.Reputation >= 0 && g.Leader.Location.HomeCivilization == c));
                        Hireables.AddRange(AllHistoricalArchitects.Where(a => HireableArchs.Contains(a.Profession) && a.Reputation >= 0 && a.Location != null && a.Location.HomeCivilization != null && a.Location.HomeCivilization == c));

                        if (Hireables.Count > 0)
                        {
                            Entity e = Hireables[rnd.Next(Hireables.Count)];
                            Entity HyperThreat = HyperThreats[rnd.Next(HyperThreats.Count)];

                            Location location = null;
                            District district = null;
                            Architect harassedArchitect = null;
                            bool isLocationAssault = false;
                            string reasonForAttack = "";

                            if (HyperThreat is Association association)
                            {
                                var possibleLocations = AllLocations.Where(loc => loc.Government != null && association.Parties.Contains(loc.Government)).ToList();
                                if (possibleLocations.Count > 0 && rnd.Next(100) < 80) // 80% chance for Location Assault
                                {
                                    isLocationAssault = true;
                                    location = possibleLocations[rnd.Next(possibleLocations.Count)];
                                    district = location.Districts[rnd.Next(location.Districts.Count)];
                                    reasonForAttack = $"due to their association with a declared enemy {association.Name}";
                                }
                                else
                                {
                                    harassedArchitect = association.Associates[rnd.Next(association.Associates.Count)];
                                    reasonForAttack = $"due to their association with a declared enemy {association.Name}";
                                }
                            }
                            else if (HyperThreat is Group group && group.Architects.Count > 0)
                            {
                                var possibleLocations = AllLocations.Where(loc => loc.Government == group).ToList();

                                if(possibleLocations.Count == 0)
                                {
                                    foreach(Architect a in group.Architects)
                                    {
                                        if(a.Location != null)
                                        {
                                            possibleLocations.Add(a.Location);
                                            break;
                                        }
                                    }
                                }

                                if (possibleLocations.Count > 0 && rnd.Next(100) < 80) // 80% chance for Location Assault
                                {
                                    isLocationAssault = true;
                                    location = possibleLocations[rnd.Next(possibleLocations.Count)];
                                    district = location.Districts[rnd.Next(location.Districts.Count)];
                                    reasonForAttack = $", controlled by a declared enemy {group.Name}";
                                }
                                else if (group.Architects.Count > 0)
                                {
                                    harassedArchitect = group.Architects[rnd.Next(group.Architects.Count)];
                                    location = harassedArchitect.Location;
                                    district = harassedArchitect.District;
                                    reasonForAttack = $"due to their membership in the declared enemy {group.Name}";
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else if (HyperThreat is Architect architect)
                            {
                                var possibleLocations = AllLocations.Where(loc => loc.Government == architect).ToList();
                                if (possibleLocations.Count > 0 && rnd.Next(100) < 20) // 20% chance for Location Assault
                                {
                                    isLocationAssault = true;
                                    location = possibleLocations[rnd.Next(possibleLocations.Count)];
                                    district = location.Districts[rnd.Next(location.Districts.Count)];
                                    reasonForAttack = $", as they govern a declared enemy location, {location.Name}";
                                }
                                else
                                {
                                    harassedArchitect = architect;
                                    location = architect.Location;
                                    district = architect.District;
                                    reasonForAttack = ", a declared enemy";
                                }
                            }
                            else if (HyperThreat is Faction faction)
                            {
                                if (faction.Organized)
                                {
                                    var possibleLocations = faction.Outposts.Where(loc => loc.TruePopulation() > 0).ToList();
                                    if (possibleLocations.Count > 0)
                                    {
                                        isLocationAssault = true;
                                        location = possibleLocations[rnd.Next(possibleLocations.Count)];
                                        district = location.Districts[rnd.Next(location.Districts.Count)];
                                        reasonForAttack = $", as an outpost of {faction.Name}";
                                    }
                                    else
                                    {
                                        var allArchitects = faction.SatelliteGroups.SelectMany(g => g.Architects).ToList();
                                        harassedArchitect = allArchitects[rnd.Next(allArchitects.Count)];
                                        reasonForAttack = $"due to their affiliation with {faction.Name}";
                                    }
                                }
                            }
                            else if (HyperThreat is Location hyperLocation)
                            {
                                isLocationAssault = true; // Directly attack the location
                                location = hyperLocation;
                                district = location.Districts[rnd.Next(location.Districts.Count)];
                                reasonForAttack = ", a believed nexus of evil";
                            }
                            else
                            {
                                Success = false;
                            }
                            if (Success)
                            {
                                if (isLocationAssault)
                                {
                                    // Log the initial event
                                    HistoricalEvents.Add(new Event(String.Concat(Date, " ", c.Name, " hired ", e.Name, " to launch an assault on ", location.Name, reasonForAttack, "."), location.Region, new EntityList<Entity>() { c, e, location }));

                                    // Perform the Generic Attack
                                    WorldActionInitiator.InitiateAction(this, "genericattack", e, new EntityList<Entity> { location, district });
                                }
                                else
                                {
                                    // Log the initial event for Direct Harassment
                                    HistoricalEvents.Add(new Event(String.Concat(Date, " ", c.Name, " hired ", e.Name, " to put an end to ", harassedArchitect.Name, reasonForAttack, "."), location.Region, new EntityList<Entity>() { c, e, harassedArchitect }));

                                    // Determine the type of harassment action: Kidnap or Kill
                                    string harassmentAction = rnd.Next(2) == 0 ? "kidnaptarget" : "killtarget";

                                    if (rnd.Next(2) == 0)
                                    {
                                        harassmentAction = "kidnaptarget";
                                    }
                                    else
                                    {
                                        harassmentAction = "killtarget";
                                    }

                                    // Set up the related entities for the harassment action
                                    EntityList<Entity> harassmentEntities = new EntityList<Entity>() { location, location.Districts[rnd.Next(location.Districts.Count)], harassedArchitect };

                                    WorldActionInitiator.InitiateAction(this, harassmentAction, e, harassmentEntities);
                                }
                            }
                        }
                    }
                }
                // LEGENDS



                WorldSubgroupManager.ManageLegends(this, Days);

                // FACTIONS

                int sixthOfMaxAge = (int)Math.Round(MaxAge / 6.0);  // Calculate one sixth of the maximum age, rounded.
                int numSixthsPassed = Year / sixthOfMaxAge;  // Determine how many sixths of time have passed.

                if (Year % sixthOfMaxAge == 0 && Year != MaxAge && AllFactions.Count < (Year / sixthOfMaxAge))
                {
                    // Ensure 3 factions are organized and 2 are unorganized
                    int organizedCount = AllFactions.Count(f => f.Organized);
                    int unorganizedCount = AllFactions.Count(f => !f.Organized);

                    bool isOrganized;
                    if (organizedCount >= 3)
                    {
                        isOrganized = false;  // If 3 organized factions already exist, make this unorganized
                    }
                    else if (unorganizedCount >= 2)
                    {
                        isOrganized = true;  // If 2 unorganized factions already exist, make this organized
                    }
                    else
                    {
                        isOrganized = rnd.Next(2) == 0;  // Otherwise, randomly choose 50/50
                    }

                    var validArchitects = AllHistoricalArchitects.Where(arch =>
                        arch.HomeLocation != null &&
                        SettlementTypes.Contains(arch.HomeLocation.Type) && 
                        !(
                            (arch.HomeLocation.Government is Architect && arch.HomeLocation.Government == arch) || 
                            (arch.HomeLocation.Government is Group && ((Group)(arch.HomeLocation.Government)).Architects.Contains(arch)) 
                        ) &&
                        HumanoidRaces.Contains(arch.Race)
                    ).ToList();

                    Architect selectedArchitect = validArchitects[rnd.Next(validArchitects.Count)];

                    // Create the new faction

                    Faction f = new Faction(selectedArchitect, isOrganized);

                    HistoricalEvents.Add(new Event(String.Concat(Date, " An ", (isOrganized ? "organized" : "unorganized"), " ", f.CoreValue, " faction emerged, named ", f.Name, "."),
                        selectedArchitect.Location != null ? selectedArchitect.Location.Region : selectedArchitect.HomeLocation.Region,
                        new EntityList<Entity> { }));

                    AllFactions.Add(f);
                }



                // Ok bjut now theyre gonna do stuff lool

                LocationBuilderPackets.AddRange(WorldSubgroupManager.ManageFactions(this, Days));

                // Loop through the world map and update locations

                foreach (Location location in AllLocations)
                {
                    // Government-related conditions
                    if (location.Government is Architect z && z.IsAlive == false)
                    {
                        HistoricalEvents.Add(new Event($"{Date} {location.Name} lost their government {location.Government.Name}.", location.Region, new EntityList<Entity>() { location, location.Government }));
                        location.Government = null;
                    }

                    if (location.Government is Group G && G.Architects.Count == 0)
                    {
                        HistoricalEvents.Add(new Event($"{Date} {location.Name} lost their leader {location.Government.Name}.", location.Region, new EntityList<Entity>() { location, location.Government }));
                        location.Government = null;
                    }

                    // Efficiently add traders to this location
                    location.TradersAtThisLocation.AddRange(location.TradersAtThisLocationToAdd);
                    location.TradersAtThisLocationToAdd.Clear();

                    // Set the base wealth increase for the location (use double for more precise calculations)
                    double WealthIncrease = 50;

                    // Adjust wealth gain based on the MonthToDayConstant
                    WealthIncrease = WealthIncrease / MonthToDayConstant;

                    // Only proceed if the location is core or garrison, has enough wealth, and a rare condition is met
                    if ((location.Type == "core" || location.Type == "garrison") && location.Wealth > 10000 && rnd.Next(1, 500) == 1)
                    {
                        int direction = rnd.Next(4); // Random direction for expansion

                        // Calculate new position based on direction
                        int NewX = location.X + (direction == 0 ? 1 : direction == 1 ? -1 : 0);
                        int NewZ = location.Z + (direction == 2 ? 1 : direction == 3 ? -1 : 0);

                        // Check if the new position is within bounds and not a "void" biome
                        bool isValidPosition = NewX > 0 && NewX < Width && NewZ > 0 && NewZ < Length;
                        bool isNotVoid = isValidPosition && WorldMap[NewX + NewZ * Width].Location == null && WorldMap[NewX + NewZ * Width].Biome != "void";

                        if (isValidPosition && isNotVoid)
                        {
                            // Create and add the new location if conditions are met
                            LocationBuilderPacket l = new LocationBuilderPacket(
                                location.Government, NewX, NewZ, "garrison", location.PrimaryRace,
                                rnd.Next(5, 10), 0, location.HomeCivilization, new EntityList<Object>(), location, "none"
                            );
                            LocationBuilderPackets.Add(l);
                            location.Wealth -= 10000; // Deduct wealth for the expansion
                        }
                    }

                    if (SettlementTypes.Contains(location.Type))
                    {
                        // Increase passive structural income
                        WealthIncrease += (double)location.PassiveStructuralIncome / MonthToDayConstant;

                        foreach (District d in location.Districts)
                        {
                            if (d.Industry == null && location.Type != "camp")
                            {
                                d.Industry = Game1.Industries[rnd.Next(Game1.Industries.Count())];
                                HistoricalEvents.Add(new Event($"{d.Name} in {location.Name} dedicated themselves to the industry of {d.Industry}.", location.Region, new EntityList<Entity>() { d, location }));
                            }
                            if (d.Industry != null && rnd.Next(1, 20) == 1)
                            {
                                d.SupplyLocation(1);
                            }
                        }
                    }

                    // Calculate the total wealth increase based on the adjusted WealthIncrease and ProsperityMultiplier
                    double finalWealthIncrease = WealthIncrease * ProsperityMultiplier;

                    // Round the wealth increase (if the result rounds to 0, round it to 1)
                    int wealthToAdd = Math.Max(1, (int)Math.Round(finalWealthIncrease));

                    // Update the location's wealth
                    location.Wealth += wealthToAdd;

                    int IEDecider = rnd.Next(1, 500 * MonthToDayConstant);

                    // Only proceed if IEDecider passes the threshold
                    if (IEDecider < 10 && SettlementTypes.Contains(location.Type))
                    {
                        // Set up location boundaries
                        int LX = location.X + rnd.Next(-5, 6);
                        int LZ = location.Z + rnd.Next(-5, 6);

                        // Check if the location is valid
                        if (LX >= 0 && LX < Width && LZ >= 0 && LZ < Width
                            )
                        {
                            var targetRegion = WorldMap[LX + LZ * Width];

                            if (targetRegion.Location == null &&
                            targetRegion.Biome != "void" &&
                            targetRegion.Biome != "ocean")
                            {
                                // New random decider for more granularity
                                int SecondaryDecider = rnd.Next(1, 11);
                                string DecidedType = "";
                                EntityList<Architect> GuarranteedArch = new EntityList<Architect>();

                                // Determine the event type based on the decider
                                switch (SecondaryDecider)
                                {
                                    case 1:
                                        DecidedType = "bandits";
                                        for (int Arch = rnd.Next(4, 8); Arch != 0; Arch--)
                                        {
                                            Architect AA = new Architect("", Game1.Sexes[rnd.Next(2)], location.HomeCivilization.PrimaryInhabitantRace, rnd.Next(13, 39), "bandit", new EntityList<Object>(), null, null, null, "", 3, false);
                                            AA.KitOutArchitect("bandit");
                                            AA.Name = Game1.GameWorld.GenerateUniqueArchitectName(AA);
                                            AA.OppositionTags.Add("intruders");
                                            GuarranteedArch.Add(AA);
                                        }
                                        break;
                                    case 2:
                                        DecidedType = "shadebeast";
                                        Architect SB = new Architect("", Game1.Sexes[rnd.Next(2)], GetRace("shadebeast"), rnd.Next(Year), "shadebeast", new EntityList<Object>(), null, null, null, "", 3, false);
                                        SB.Name = Game1.GameWorld.GenerateUniqueArchitectName(SB);
                                        GuarranteedArch.Add(SB);
                                        break;
                                    case 3:
                                        DecidedType = "construct";
                                        Architect CN = new Architect("", Game1.Sexes[rnd.Next(2)], ConstructRaces[rnd.Next(ConstructRaces.Count())], rnd.Next(Year), "construct", new EntityList<Object>(), null, null, null, "", 3, false);
                                        CN.Name = Game1.GameWorld.GenerateUniqueArchitectName(CN);
                                        GuarranteedArch.Add(CN);
                                        break;
                                    case 4:
                                        DecidedType = "wildcreatures";
                                        Race DecidedRace = WildRaces[rnd.Next(WildRaces.Count())];
                                        for (int Arch = rnd.Next(4, 8); Arch != 0; Arch--)
                                        {
                                            Architect AA = new Architect("", Game1.Sexes[rnd.Next(2)], DecidedRace, rnd.Next(13, 39), "beast", new EntityList<Object>(), null, null, null, "", 2, false);
                                            AA.Name = Game1.GameWorld.GenerateUniqueArchitectName(AA);
                                            GuarranteedArch.Add(AA);
                                        }
                                        break;
                                    case 5 when TradingGroups.Count() > 0:
                                        DecidedType = "traders";
                                        Group TradingGroup = TradingGroups[rnd.Next(TradingGroups.Count())];
                                        GuarranteedArch = TradingGroup.Architects; // updates if the group updates
                                        break;
                                    case 6:
                                        DecidedType = "vagabond";
                                        Architect VB = new Architect("", Game1.Sexes[rnd.Next(2)], HumanoidRaces[rnd.Next(HumanoidRaces.Count())], rnd.Next(13, 39), "vagabond", new EntityList<Object>(), null, null, null, "", 3, false);
                                        VB.KitOutArchitect("vagabond");
                                        VB.Name = Game1.GameWorld.GenerateUniqueArchitectName(VB);
                                        GuarranteedArch.Add(VB);
                                        break;
                                    case 7:
                                        DecidedType = "shiba";
                                        Architect SI = new Architect("", Game1.Sexes[rnd.Next(2)], GetRace("shiba"), rnd.Next(13, 39), "shiba", new EntityList<Object>(), null, null, null, "", 3, false);
                                        SI.Name = Game1.GameWorld.GenerateUniqueArchitectName(SI);
                                        GuarranteedArch.Add(SI);
                                        break;
                                    case 8:
                                        DecidedType = "adventurer";
                                        Architect AD = new Architect("", Game1.Sexes[rnd.Next(2)], HumanoidRaces[rnd.Next(HumanoidRaces.Count())], rnd.Next(13, 39), "adventurer", new EntityList<Object>(), null, null, null, "", 3, false);
                                        AD.KitOutArchitect("adventurer");
                                        AD.Name = Game1.GameWorld.GenerateUniqueArchitectName(AD);
                                        GuarranteedArch.Add(AD);
                                        break;
                                    case 9:
                                        DecidedType = "priest";
                                        Architect PR = new Architect("", Game1.Sexes[rnd.Next(2)], HumanoidRaces[rnd.Next(HumanoidRaces.Count())], rnd.Next(13, 39), "priest", new EntityList<Object>(), null, null, null, "", 1, false);
                                        PR.KitOutArchitect("priest");
                                        PR.Name = Game1.GameWorld.GenerateUniqueArchitectName(PR);
                                        GuarranteedArch.Add(PR);
                                        break;
                                }

                                // If an event type was decided, create a unit and assign home location
                                if (DecidedType != "")
                                {
                                    Unit u = new Unit(WorldMap[LX + LZ * Width], DecidedType, GuarranteedArch);
                                    AllUnits.Add(u);
                                    WorldMap[LX + LZ * Width].Units.Add(u);

                                    // Set the home location of each architect to the current location
                                    foreach (Architect a in GuarranteedArch)
                                    {
                                        a.HomeLocation = location;
                                    }
                                }
                            }
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
                            if (timeSinceLastMoved >= rnd.Next(21600000, 30240000))
                            {
                                // Move to the next location in the trade route
                                if (g.TradeRoute.Count() > 1)
                                {
                                    int travelIndex = (g.TradeRoute.IndexOf(location) + 1) % g.TradeRoute.Count();

                                    location.TradersAtThisLocationToRemove.Add(g);
                                    g.TradeRoute[travelIndex].TradersAtThisLocation.Add(g);

                                    g.Architects.ForEach(A => A.NextMigrationLocation = g.TradeRoute[travelIndex]);
                                    g.Architects.ForEach(A => A.MigrationReason = "We are headed to " + g.TradeRoute[travelIndex] + " as part of our trade route.");

                                    g.CycleLastMoved = currentCycle;
                                }
                            }

                            // Check if it's time for the group to trade at the current location
                            double timeSinceLastTraded = currentCycle - g.CycleLastTraded;
                            if (timeSinceLastTraded >= rnd.Next(12960000, 17280000))
                            {
                                // Trade at the current location
                                int tradeCount = rnd.Next(20, 40);

                                List<string> availableCaravanItems = g.CaravanItems;
                                List<string> availableLocationItems = location.Market.Block.District.GeneralItemsWeHave;

                                if (availableLocationItems.Count >= 5)
                                {
                                    var tradeItems = Enumerable.Range(0, tradeCount)
                                        .Select(_ => new
                                        {
                                            CaravanItem = availableCaravanItems[rnd.Next(availableCaravanItems.Count)],
                                            LocationItem = availableLocationItems[rnd.Next(availableLocationItems.Count)]
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

                                    double newCycle = Cycle + ((double)Days * 864000);
                                    double newYear = (int)Math.Round((decimal)(newCycle / 290304000));

                                    if ((newYear / 10) > (currentYear / 10))
                                    {
                                        g.WaitingForCooldownToTrade = true;

                                        if (rnd.Next(1, 3) == 1 && g.TradeRoute.Count <= g.MaxTradeRouteLength)
                                        {
                                            var newTradeLocations = AllLocations
                                                .Where(l => Vector2.Distance(new Vector2(l.X, l.Z), new Vector2(location.X, location.Z)) < 20 &&
                                                            !g.TradeRoute.Contains(l) &&
                                                            SettlementTypes.Contains(l.Type))
                                                .ToList();

                                            if (newTradeLocations.Any())
                                            {
                                                var newTradeLocation = newTradeLocations[rnd.Next(newTradeLocations.Count)];
                                                HistoricalEvents.Add(new Event($"{Date} {g.Name} added {newTradeLocation.Name} to their list of trading partners.", location.Region, new EntityList<Entity>(){g, newTradeLocation}));
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


                    if (!AllPortsBuilt && rnd.Next(500 * MonthToDayConstant) == 0 && TradingGroups.Count() > 0)
                    {
                        // Identify the biggest island
                        EntityList<Region> biggestIsland = Islands.OrderByDescending(island => island.Count()).FirstOrDefault();

                        // Initialize a counter for ports on the biggest island based on current port conditions
                        int biggestIslandIndex = Islands.IndexOf(biggestIsland);
                        int portsOnBiggestIsland = PortLocations[biggestIslandIndex].Count(port => !string.IsNullOrEmpty(port.PortName));

                        bool portBuilt = false;

                        for (int i = 0; i < Islands.Count(); i++)
                        {
                            var island = Islands[i];
                            var potentialPorts = PotentialPorts[i];
                            bool islandHasPort = PortLocations[i].Any(port => !string.IsNullOrEmpty(port.PortName));

                            if (island == biggestIsland && portsOnBiggestIsland < ContinentalPortMaximum)
                            {
                                // For the biggest island, allow adding ports until it reaches ContinentalPortMaximum
                                var availablePorts = potentialPorts.Where(port => string.IsNullOrEmpty(port.PortName));
                                if (availablePorts.Any())
                                {
                                    var portLocation = availablePorts[rnd.Next(availablePorts.Count())];
                                    portLocation.PortName = GenerateUniqueName("1S7s", portLocation, Game1.GameWorld.rnd);

                                    if (TradingGroups.Count() > 0)
                                    {
                                        Group g = TradingGroups[rnd.Next(TradingGroups.Count())];

                                        HistoricalEvents.Add(new Event($"{Date} A new port named {portLocation.PortName} was built by {g.Name} to facilitate trade.", portLocation, new EntityList<Entity>() { g }));
                                    }
                                    else
                                    {
                                        HistoricalEvents.Add(new Event($"{Date} A new port named {portLocation.PortName} was built to facilitate trade.", portLocation, new EntityList<Entity>() { }));
                                    }

                                    portBuilt = true;
                                    portsOnBiggestIsland++; // Update the counter for ports on the biggest island
                                    potentialPorts.Remove(portLocation); // Remove the port location from potential ports
                                }
                            }
                            else if (!islandHasPort)
                            {
                                // For other islands, build only one port if there isn't already one
                                var availablePorts = potentialPorts.Where(port => string.IsNullOrEmpty(port.PortName));
                                if (availablePorts.Any())
                                {
                                    var portLocation = availablePorts[rnd.Next(availablePorts.Count())];
                                    portLocation.PortName = GenerateUniqueName("1S7s", portLocation, Game1.GameWorld.rnd);

                                    if (TradingGroups.Count() > 0)
                                    {
                                        Group g = TradingGroups[rnd.Next(TradingGroups.Count())];

                                        HistoricalEvents.Add(new Event($"{Date} A new port named {portLocation.PortName} was built by {g.Name} to facilitate trade.", portLocation, new EntityList<Entity>() { g }));
                                    }
                                    else
                                    {
                                        HistoricalEvents.Add(new Event($"{Date} A new port named {portLocation.PortName} was built to facilitate trade.", portLocation, new EntityList<Entity>() { }));
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


                    //other actions favorable or not, based on alignment
                    //gather tuples in the location. Each tuple represents an Architect or Group and their relevant values.

                    List<(Entity Entity, string Name, string Profession, int MoralCompass, int StabilityCompass, int PropertyValue, int FamilyValue, int PowerValue, int MoneyValue, int KnowledgeValue, int SpiritualityValue, int ProwessValue, int PatriotismValue, int CourageValue, int CreativityValue)> EntityTuples = new List<(Entity, string, string, int, int, int, int, int, int, int, int, int, int, int, int)>();

                    foreach (Architect a in ArchitectsAtLocation)
                    {
                        // Salary
                        a.Wealth += rnd.Next(0, 3);

                        if (a.Group == null || rnd.Next(1, 3) == 1) // Architects are less likely to act by themselves if they have friends they might act with, but they still can :)
                        {
                            EntityTuples.Add((a, a.Name, a.Profession, a.MoralCompass, a.StabilityCompass, a.PropertyValue, a.FamilyValue, a.PowerValue, a.MoneyValue, a.KnowledgeValue, a.SpiritualityValue, a.ProwessValue, a.PatriotismValue, a.CourageValue, a.CreativityValue));
                        }
                    }

                    foreach (Group g in location.GroupsAtThisLocation)
                    {
                        // Salary
                        g.Wealth += g.Architects.Count() * rnd.Next(0, 4);

                        if (g.Architects.Count() > 0)
                        {
                            // Calculate group values by averaging non-leader values and combining with leader values
                            var leader = g.Leader;
                            var nonLeaders = g.Architects.Where(a => a != leader).ToList();

                            int nonLeaderCount = nonLeaders.Count;

                            int totalMoralCompass = nonLeaders.Sum(a => a.MoralCompass);
                            int totalStabilityCompass = nonLeaders.Sum(a => a.StabilityCompass);
                            int totalPropertyValue = nonLeaders.Sum(a => a.PropertyValue);
                            int totalFamilyValue = nonLeaders.Sum(a => a.FamilyValue);
                            int totalPowerValue = nonLeaders.Sum(a => a.PowerValue);
                            int totalMoneyValue = nonLeaders.Sum(a => a.MoneyValue);
                            int totalKnowledgeValue = nonLeaders.Sum(a => a.KnowledgeValue);
                            int totalSpiritualityValue = nonLeaders.Sum(a => a.SpiritualityValue);
                            int totalProwessValue = nonLeaders.Sum(a => a.ProwessValue);
                            int totalPatriotismValue = nonLeaders.Sum(a => a.PatriotismValue);
                            int totalCourageValue = nonLeaders.Sum(a => a.CourageValue);

                            // Calculate final group values, combining leader's values with non-leader averages
                            int groupMoralCompass = (int)((leader.MoralCompass + (nonLeaderCount > 0 ? (double)totalMoralCompass / nonLeaderCount : 0)) / 2);
                            int groupStabilityCompass = (int)((leader.StabilityCompass + (nonLeaderCount > 0 ? (double)totalStabilityCompass / nonLeaderCount : 0)) / 2);
                            int groupPropertyValue = (int)((leader.PropertyValue + (nonLeaderCount > 0 ? (double)totalPropertyValue / nonLeaderCount : 0)) / 2);
                            int groupFamilyValue = (int)((leader.FamilyValue + (nonLeaderCount > 0 ? (double)totalFamilyValue / nonLeaderCount : 0)) / 2);
                            int groupPowerValue = (int)((leader.PowerValue + (nonLeaderCount > 0 ? (double)totalPowerValue / nonLeaderCount : 0)) / 2);
                            int groupMoneyValue = (int)((leader.MoneyValue + (nonLeaderCount > 0 ? (double)totalMoneyValue / nonLeaderCount : 0)) / 2);
                            int groupKnowledgeValue = (int)((leader.KnowledgeValue + (nonLeaderCount > 0 ? (double)totalKnowledgeValue / nonLeaderCount : 0)) / 2);
                            int groupSpiritualityValue = (int)((leader.SpiritualityValue + (nonLeaderCount > 0 ? (double)totalSpiritualityValue / nonLeaderCount : 0)) / 2);
                            int groupProwessValue = (int)((leader.ProwessValue + (nonLeaderCount > 0 ? (double)totalProwessValue / nonLeaderCount : 0)) / 2);
                            int groupPatriotismValue = (int)((leader.PatriotismValue + (nonLeaderCount > 0 ? (double)totalPatriotismValue / nonLeaderCount : 0)) / 2);
                            int groupCourageValue = (int)((leader.CourageValue + (nonLeaderCount > 0 ? (double)totalCourageValue / nonLeaderCount : 0)) / 2);

                            EntityTuples.Add((g, g.Name, g.Type + " group", groupMoralCompass, groupStabilityCompass, groupPropertyValue, groupFamilyValue, groupPowerValue, groupMoneyValue, groupKnowledgeValue, groupSpiritualityValue, groupProwessValue, groupPatriotismValue, groupCourageValue, leader.CreativityValue));
                        }
                    }

                    foreach (var entityTuple in EntityTuples)
                    {
                        var (entity, name, profession, moralCompass, stabilityCompass, propertyValue, familyValue, powerValue, moneyValue, knowledgeValue, spiritualityValue, prowessValue, patriotismValue, courageValue, creativityValue) = entityTuple;

                        Location targetLocation = location; // Default to the current location

                        // 80% chance to search for a preferred target location within 20 tiles
                        if (rnd.Next(100 * MonthToDayConstant) < 80)
                        {
                            List<Location> nearbyEnemyLocations = new List<Location>();
                            foreach (var preferredLocation in this.PreferredTargetLocations())
                            {
                                // Calculate distance between current location and preferred location
                                int deltaX = preferredLocation.X - location.X;
                                int deltaZ = preferredLocation.Z - location.Z;
                                double distance = Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ);

                                // Check if the location is within 20 tiles and belongs to an enemy faction
                                if (distance <= 20 && (entity.Enemies.Contains(location.Government) || entity.Enemies.Contains(location) || (location.Government is Group g && g.HomeFaction != null && entity.Enemies.Contains(g.HomeFaction))))
                                {
                                    nearbyEnemyLocations.Add(preferredLocation);
                                }
                            }

                            // If there are any valid nearby enemy locations, pick a random one
                            if (nearbyEnemyLocations.Count > 0)
                            {
                                targetLocation = nearbyEnemyLocations[rnd.Next(nearbyEnemyLocations.Count)];
                            }
                        }

                        // Protection logic
                        bool dontAttackYourself = targetLocation.Government == entityTuple.Entity ||
                                           (targetLocation.Government is Group group && group.Architects.Contains(entityTuple.Entity));

                        // Action initiation logic
                        if (ArchitectsAtLocation.Count() > 0)
                        {
                            if (!dontAttackYourself && moneyValue >= 2 && stabilityCompass < 40 && rnd.Next(150 * MonthToDayConstant) == 1) // Theft
                            {
                                WorldActionInitiator.InitiateAction(this, "theft", entity, new EntityList<Entity> { targetLocation });
                            }
                            else if (!dontAttackYourself && moneyValue >= 2 && stabilityCompass < 40 && rnd.Next(160 * MonthToDayConstant) == 1) // Embezzlement
                            {
                                WorldActionInitiator.InitiateAction(this, "embezzlement", entity, new EntityList<Entity> { targetLocation });
                            }
                            else if (!dontAttackYourself && targetLocation.Prism != null && targetLocation.Prism.HistoricalObjects.Count > 0 && moneyValue >= 2 && knowledgeValue >= 1 && stabilityCompass < 50 && rnd.Next(500 * MonthToDayConstant) == 1 && SettlementTypes.Contains(targetLocation.Type)) // Artifact Theft
                            {
                                WorldActionInitiator.InitiateAction(this, "artifacttheft", entity, new EntityList<Entity> { targetLocation, targetLocation.Prism.Block.District, targetLocation.Prism, targetLocation.Prism.HistoricalObjects[rnd.Next(targetLocation.Prism.HistoricalObjects.Count())] });
                            }
                            else if (!dontAttackYourself && powerValue >= 3 && rnd.Next(180 * MonthToDayConstant) == 1) // Takeover
                            {
                                District targetDistrict = targetLocation.Districts[rnd.Next(targetLocation.Districts.Count())];
                                WorldActionInitiator.InitiateAction(this, "takeover", entity, new EntityList<Entity> { targetLocation, targetDistrict });
                            }
                            else if (!dontAttackYourself && prowessValue >= 3 && rnd.Next(170 * MonthToDayConstant) == 1 && targetLocation.AllStructures.Count > 0 && targetLocation.Government != entityTuple.Entity && !(targetLocation.Government is Group g && g.Architects.Contains(entityTuple.Entity))) // Raze Building
                            {
                                Structure targetStructure;
                                if (rnd.Next(100 * MonthToDayConstant) < 40)
                                {
                                    targetStructure = targetLocation.AllStructures[0]; // 40% chance to target the first structure
                                }
                                else
                                {
                                    targetStructure = targetLocation.AllStructures[rnd.Next(targetLocation.AllStructures.Count)];
                                }
                                WorldActionInitiator.InitiateAction(this, "razebuilding", entity, new EntityList<Entity> { targetLocation, targetStructure });
                            }
                            else if (!dontAttackYourself && patriotismValue >= 3 && rnd.Next(190 * MonthToDayConstant) == 1 && targetLocation.HomeCivilization != null) // Incite
                            {
                                District targetDistrict = targetLocation.Districts[rnd.Next(targetLocation.Districts.Count())];
                                WorldActionInitiator.InitiateAction(this, "incite", entity, new EntityList<Entity> { targetLocation, targetDistrict });
                            }
                            // These just use default location because it wouldn't make sense to target an enemy with these
                            else if (creativityValue >= 3 && rnd.Next(200 * MonthToDayConstant) == 1 && SettlementTypes.Contains(location.Type)) // Craftsmanship
                            {
                                WorldActionInitiator.InitiateAction(this, "craftsmanship", entity, new EntityList<Entity> { location });
                            }
                            else if (entity is Architect architect && familyValue >= 0 && rnd.Next(180 * MonthToDayConstant) == 1 && architect.Spouse == null) // Marriage
                            {
                                WorldActionInitiator.InitiateAction(this, "marriage", entity, new EntityList<Entity> { location });
                            }
                            else if (entity is Architect Arch && Arch.Spouse != null && !Arch.HadChildren) // Childbirth
                            {
                                WorldActionInitiator.InitiateAction(this, "childbirth", entity, new EntityList<Entity> { location });
                            }
                        }
                    }




                    const int EmbezzlementValue = 5;

                    // Embezzlement processing
                    EntityList<Entity> embezzlToRemove = new EntityList<Entity>();
                    foreach (Entity e in location.Embezzlements)
                    {
                        if (rnd.Next(1, 10 * MonthToDayConstant) == 1)
                        {
                            if (e is Architect architect)
                            {
                                architect.Wealth += EmbezzlementValue;
                                location.Wealth -= EmbezzlementValue;
                            }
                            else if (e is Group group)
                            {
                                group.Wealth += EmbezzlementValue;
                                location.Wealth -= EmbezzlementValue;
                            }
                        }

                        if (rnd.Next(1, 100 * MonthToDayConstant) == 0)
                        {
                            HistoricalEvents.Add(new Event($"A loophole in the governmental structure of {location.Name} was discovered and fixed, and {e.Name} lost some ability to embezzle funding.", location.Region, new EntityList<Entity>(){location, e}));
                            embezzlToRemove.Add(e);
                        }
                    }
                    foreach (Entity e in embezzlToRemove)
                    {
                        location.Embezzlements.Remove(e);
                    }


                    //iterate through districts

                    foreach (District d in location.Districts)
                    {
                        //Handle population increase in the district

                        if(d.UnplacedPopulation < 0)
                        {
                            int shibe = 1;
                        }

                        if (location.Region.Blight != Purity && SettlementTypes.Contains(location.Type))
                        {
                            if (d.UnplacedPopulation != 0)
                            {
                                d.UnplacedPopulation = Math.Max(d.UnplacedPopulation - rnd.Next(0, 4), 0);

                                if (d.UnplacedPopulation == 0)
                                {
                                    HistoricalEvents.Add(new Event($"{Date} {location.Name} fell to the {location.Region.Blight.Name}.", location.Region, new EntityList<Entity>(){location, location.Region.Blight}));
                                }
                            }
                            foreach (Architect a in d.Architects)
                            {
                                if (rnd.Next(1, 70 * MonthToDayConstant) == 1)
                                {
                                    d.ArchitectsToRemove.Add(a);

                                    HistoricalEvents.Add(new Event($"{Date} {a.Name} died to the {location.Region.Blight.Name} in {location.Name}.", location.Region, new EntityList<Entity>(){a, location.Region.Blight, location}));
                                }
                            }
                        }


                        //was 100 

                        if (d.Population() < d.MaxPopulation && location.TruePopulation() > 4)
                        {
                            // Set base probability for births'

                            double baseProbability = 1.0;

                            // Population factor to reduce birth likelihood as population grows
                            double populationFactor = 1 + (d.Population() / 100.0);

                            // Adjust the birth probability by MTDC
                            double birthProbability = (baseProbability / populationFactor) * (1.0 / MonthToDayConstant);

                            // Apply the BirthProbabilityMod to further adjust
                            birthProbability /= BirthProbabilityMod;

                            int births = 0;

                            // Calculate births based on the adjusted probability
                            for (int i = 0; i < d.Population(); i++)
                            {
                                if (rnd.NextDouble() < birthProbability)
                                {
                                    births++;
                                }
                            }

                            

                            // Update the unplaced population with the calculated births
                            d.UnplacedPopulation += births;
                        }




                        //Designate the Construction of a new District

                        if (d.UnplacedPopulation + d.Architects.Count() > 150 && d.UnplacedPopulation > 80)
                        {
                            int Movers = rnd.Next(65, 85);

                            District NewD = new District(false, location, Movers);
                            d.UnplacedPopulation = d.UnplacedPopulation - Movers;

                            foreach (Architect a in d.Architects)
                            {
                                if (rnd.Next(1, 4) == 1 && a.Group != location.Government)
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
                            d.ArchitectsToRemove = new EntityHashSet<Architect>();

                            //go ahead and build a bunch of houses in the new district instead of moving stuff everywhere.

                            if (SettlementTypes.Contains(d.Location.Type) || d.Location.Type == "core" || d.Location.Type == "garrison")
                            {
                                for (int i = 0; i < rnd.Next(6, 10); i++)
                                {
                                    Block ChosenBlock = NewD.DistrictMap[rnd.Next(0, 49)];
                                    Structure s = new Structure("house", new EntityList<Object>(), new EntityList<Room>(), ChosenBlock, new EntityList<Material> { location.HomeCivilization.CulturalWood }, new List<string> { location.HomeCivilization.CulturalWood.Name }, new List<string> { Game1.LightingStyles[rnd.Next(Game1.LightingStyles.Count())] }, rnd.Next(0, 5), rnd.Next(0, 4), (int)Math.Round(Cycle / 290304000));

                                    ChosenBlock.Structures.Add(s);
                                }
                            }

                            //well
                            NewD.DistrictMap[rnd.Next(2, 6) + rnd.Next(2, 5) * 7].Objects.Add(new Object(null, "well", new EntityList<Material> { location.HomeCivilization.CulturalStone }, true, true, null, null, 255, false, null, null, null, false));

                            HistoricalEvents.Add(new Event($"{Date} {location.Name} segmented off a plot of land to a new district, {NewD.Name}, dedicated to {NewD.Industry}.", location.Region, new EntityList<Entity>(){location, NewD}));

                            location.DistrictsToAdd.Add(NewD); 
                        }


                        //Handle elevation of a member of the population to Architect


                        int decider = rnd.Next(1, ArchitectConstant * MonthToDayConstant);

                        if (decider == 1 && d.UnplacedPopulation > 0)
                        {
                            bool Ismale = true;
                            if (rnd.Next(1, 3) == 1)
                            {
                                Ismale = false;
                            }

                            string Role = "";
                            string Destiny = "";
                            Race Race;


                            if (rnd.Next(1, 20) == 1 || location.PrimaryRace == null || location.PrimaryRace == GetRace(""))
                            {
                                Race = HumanoidRaces[rnd.Next(HumanoidRaces.Count())];
                            }
                            else
                            {
                                Race = location.PrimaryRace;
                            }

                            //decide if the creature will be magical

                            int DestinyDecider = rnd.Next(1, 300);

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

                            string role = Game1.WeightedRandomArchitectProfessions[rnd.Next(Game1.WeightedRandomArchitectProfessions.Count())];
                            Role = role;

                            Architect architect = new Architect("", gender, Race, rnd.Next(14, 60), Role, new EntityList<Object>(), location, d, null, Destiny, 1, true);
                            location.HomeCivilization.Citizens.Add(architect);
                            string Name = GenerateUniqueArchitectName(architect);
                            architect.Name = Name;

                            HistoricalEvents.Add(new Event($"{Date} {Name} became an influential {Role} in {location.Name}", location.Region, new EntityList<Entity>(){architect, location}));

                            if (rnd.Next(100) == 1)
                            {
                                HistoricalEvents.Add(new Event($"{Date} {Name} possessed an unrivaled spirit and determination.", location.Region, new EntityList<Entity>(){architect}));

                                Legends.Add(architect);
                            }

                            d.Architects.Add(architect);

                            d.UnplacedPopulation = d.UnplacedPopulation - 1;
                        }



                        //ok so THIS stuff is all gonna happen once per district

                        //Handle the creation of a Group
                        //make a group
                        //new group
                        //i cant find this area all the time fsr lolloooolooolol

                        if (d.Architects.Count() > 0 && SettlementTypes.Contains(location.Type))
                        {
                            foreach (Architect a in d.Architects)
                            {
                                if (a.Group == null && !(Calamity.Count != 0 && Calamity[0] == a))
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

                                        if (groupType != "trader" && GroupsAlreadyLikeThis <= Math.Round((decimal)location.TruePopulation() / 500, MidpointRounding.ToNegativeInfinity))
                                        {
                                            int chance = (groupType == "trade") ? 3000 * MonthToDayConstant : 1000 * MonthToDayConstant;
                                            if (rnd.Next(1, chance) == 1 && a.Profession != "prophet")
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
                                                        int itemCount = rnd.Next(10, 20);
                                                        string itemString = $"bar,{itemCount},{location.HomeCivilization.CulturalMetal.Name}";

                                                        g.CaravanItems.Add(itemString);

                                                        location.TradersAtThisLocation.Add(g);
                                                        g.TradeRoute.Add(location);
                                                        TradingGroups.Add(g);
                                                    }

                                                    HistoricalEvents.Add(new Event($"{Date} {a.Name} founded {g.Name}, a {g.Type} group, in {location.Name}", location.Region, new EntityList<Entity>(){a, g, location}));

                                                    a.GroupLoyalty = 5;
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
                        d.ArchitectsToRemove = new EntityHashSet<Architect>();

                        //Handle civilization leadership changes

                        if (location.Government == null && SettlementTypes.Contains(location.Type))
                        {
                            foreach (Group g in location.GroupsAtThisLocation)
                            {
                                if (g.Type != "trade")
                                {
                                    HistoricalEvents.Add(new Event($"{Date} {g.Name} took power in {location.Name}.", location.Region, new EntityList<Entity>(){g, location}));

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
                                        HistoricalEvents.Add(new Event($"{Date} {g.Name} took power from {location.Government.Name} thanks to their credibility and support in {location.Name}.", location.Region, new EntityList<Entity>(){g, location}));

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

                            //oh also make games


                            if ((a.CultureStudyPoints > 2000 && rnd.Next(250 * MonthToDayConstant) == 1 && !a.AlreadyMadeAGame) || (rnd.Next(50000 * MonthToDayConstant) == 1 && !a.AlreadyMadeAGame))
                            {
                                a.AlreadyMadeAGame = true;

                                Ruleset g = new Ruleset();

                                HistoricalEvents.Add(new Event(String.Concat(Date, " ", a.Name, " designed a new game, called ", g.Name, "."),
                                    d.Location.Region,
                                    new EntityList<Entity>() { a, g }));

                                Games.Add(g);
                            }

                            if ((a.Profession == "scholar" && a.IsStudying) || a.Profession == "warlock" || a.Profession == "sorcerer")
                            {
                                switch (a.ScholarType)
                                {
                                    case "mage":
                                        a.MagicStudyPoints += rnd.Next(0, 7);
                                        break;
                                    case "engineer":
                                        a.ScienceStudyPoints += rnd.Next(0, 7);
                                        break;
                                    case "entertainer":
                                        a.CultureStudyPoints += rnd.Next(0, 7);
                                        break;
                                    case "artificer":
                                        a.ScienceStudyPoints += rnd.Next(0, 4);
                                        a.MagicStudyPoints += rnd.Next(0, 4);
                                        break;
                                    case "bard":
                                        a.CultureStudyPoints += rnd.Next(0, 4);
                                        a.MagicStudyPoints += rnd.Next(0, 4);
                                        break;
                                    case "sage":
                                        a.CultureStudyPoints += rnd.Next(0, 4);
                                        a.ScienceStudyPoints += rnd.Next(0, 4);
                                        break;
                                    case "luminary":
                                        a.ScienceStudyPoints += rnd.Next(0, 2);
                                        a.CultureStudyPoints += rnd.Next(0, 2);
                                        a.MagicStudyPoints += rnd.Next(0, 2);
                                        break;
                                    default:
                                        // Handle unknown ScholarType, if needed
                                        break;
                                }

                                // Decide to write
                                if ((a.Profession == "scholar" && rnd.Next(1, (Math.Max(500 - (a.MagicStudyPoints + a.ScienceStudyPoints + a.CultureStudyPoints), 50)) * MonthToDayConstant) == 1) ||
                                    ((a.Profession == "sorcerer" || a.Profession == "warlock") && (rnd.Next(1, 50 * MonthToDayConstant)) == 1))
                                {
                                    string writingType = "";
                                    int totalWeight = a.MagicStudyPoints + a.ScienceStudyPoints + a.CultureStudyPoints;
                                    if (totalWeight == 0) totalWeight = 1;  // Prevent division by zero

                                    // Calculate probabilities based on relative weight of each study point
                                    int magicWeight = (a.MagicStudyPoints * 100) / totalWeight;
                                    int scienceWeight = (a.ScienceStudyPoints * 100) / totalWeight;
                                    int cultureWeight = (a.CultureStudyPoints * 100) / totalWeight;

                                    // Use a random number to decide based on weights
                                    int randomChoice = rnd.Next(100);

                                    bool shouldWriteBook = (a.SpellsKnown.Count() > 0) && (rnd.Next(2) == 0);

                                    if (shouldWriteBook)
                                    {
                                        // 50 percent chance to ignore and write a book
                                        writingType = "book";
                                    }
                                    else if ((a.Profession == "sorcerer" || a.Profession == "warlock") && (rnd.Next(5) != 0))
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

                                    if (writingType == "book" && rnd.NextDouble() < 0.5)
                                    {
                                        // 50% chance to write a history book about a specific subject
                                        Entity subject = GenerateRandomSubject();
                                        if (subject != null && HistoricalEvents.Any(historicalEvent => historicalEvent.EventData.Contains(subject.Name)))
                                        {
                                            newWork = new Composition(writingType, a, subject);
                                        }
                                    }
                                    else
                                    {
                                        // 50% chance to write about a general domain
                                        Entity domainEntity = a.AlignedDomains.GetRandomItem();
                                        newWork = new Composition(writingType, a, domainEntity);
                                    }

                                    if (newWork != null)
                                    {
                                        string Spell = "";

                                        if (writingType == "book")
                                        {
                                            string ObjectType = new List<string>() { "scroll", "scroll", "sheet", "book", "book" }[rnd.Next(5)];

                                            Object o = new Object(newWork.Name, ObjectType, new EntityList<Material>() { d.Location.HomeCivilization != null ? d.Location.HomeCivilization.CulturalCloth : Cloths[rnd.Next(Cloths.Count())] }, a);

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

                                            if (a.SpellsKnown.Count() > 0 && rnd.Next(10) == 1)
                                            {
                                                o.SpecialKnowledge = a.SpellsKnown[rnd.Next(a.SpellsKnown.Count())];
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
                                        HistoricalEvents.Add(new Event($"{Date} {a.Name} authored a {writingType} titled '{newWork.Name}' in {location.Name}.", location.Region, new EntityList<Entity>(){a, newWork, location}));

                                        if (Spell != "")
                                        {
                                            HistoricalEvents.Add(new Event($"{Date} {newWork.Name} contained the secret of {Spell}.", location.Region, new EntityList<Entity>(){newWork}));
                                        }
                                    }
                                }

                                // Archelevation
                                if (!a.ScholarType.StartsWith("arch"))
                                {
                                    if (a.ScholarType == "mage" && a.MagicStudyPoints > Threshold)
                                    {
                                        a.ScholarType = "archmage";
                                        HistoricalEvents.Add(new Event($"{Date} {a.Name} became an archmage in {location.Name}", location.Region, new EntityList<Entity>(){a, location}));
                                    }
                                    else if (a.ScholarType == "engineer" && a.ScienceStudyPoints > Threshold)
                                    {
                                        a.ScholarType = "archengineer";
                                        HistoricalEvents.Add(new Event($"{Date} {a.Name} became an archengineer in {location.Name}", location.Region, new EntityList<Entity>(){ a, location }));
                                    }
                                    else if (a.ScholarType == "entertainer" && a.CultureStudyPoints > Threshold)
                                    {
                                        a.ScholarType = "archentertainer";
                                        HistoricalEvents.Add(new Event($"{Date} {a.Name} became an archentertainer in {location.Name}", location.Region, new EntityList<Entity>(){ a, location }));
                                    }
                                    else if (a.ScholarType == "artificer" && a.MagicStudyPoints > Threshold / 2 && a.ScienceStudyPoints > Threshold / 2)
                                    {
                                        a.ScholarType = "archartificer";
                                        HistoricalEvents.Add(new Event($"{Date} {a.Name} became an archartificer in {location.Name}", location.Region, new EntityList<Entity>(){ a, location }));
                                    }
                                    else if (a.ScholarType == "bard" && a.MagicStudyPoints > Threshold / 2 && a.CultureStudyPoints > Threshold / 2)
                                    {
                                        a.ScholarType = "archbard";
                                        HistoricalEvents.Add(new Event($"{Date} {a.Name} became an archbard in {location.Name}", location.Region, new EntityList<Entity>(){ a, location }));
                                    }
                                    else if (a.ScholarType == "sage" && a.CultureStudyPoints > Threshold / 2 && a.ScienceStudyPoints > Threshold / 2)
                                    {
                                        a.ScholarType = "archsage";
                                        HistoricalEvents.Add(new Event($"{Date} {a.Name} became an archsage in {location.Name}", location.Region, new EntityList<Entity>(){ a, location }));
                                    }
                                    else if (a.ScholarType == "luminary" && a.CultureStudyPoints > Threshold / 3 && a.ScienceStudyPoints > Threshold / 3 && a.MagicStudyPoints > Threshold / 3)
                                    {
                                        a.ScholarType = "archluminary";
                                        HistoricalEvents.Add(new Event($"{Date} {a.Name} became an archluminary in {location.Name}", location.Region, new EntityList<Entity>(){ a, location }));
                                    }
                                }

                                // Mage scholars learn spells, each can only discover one in their lifetime, but can learn more from others.
                                if ((a.ScholarType == "mage" || a.ScholarType == "artificer" || a.ScholarType == "luminary" || a.ScholarType == "bard" || a.ScholarType == "archmage" || a.ScholarType == "archartificer" || a.ScholarType == "archluminary" || a.ScholarType == "archbard") && !a.DiscoveredASpell && UndiscoveredSpells.Count() > 0 && a.MagicStudyPoints > 200)
                                {
                                    int SpellID = rnd.Next(UndiscoveredSpells.Count());
                                    a.SpellsKnown.Add(UndiscoveredSpells[SpellID]);
                                    HistoricalEvents.Add(new Event($"{Date} {a.Name} discovered the secret of {UndiscoveredSpells[SpellID].Name} in {location.Name}", location.Region, new EntityList<Entity>(){a, location}));
                                    DiscoveredSpells.Add(UndiscoveredSpells[SpellID]);
                                    UndiscoveredSpells.RemoveAt(SpellID);
                                    a.DiscoveredASpell = true;
                                }
                            }

                            // Destiny
                            List<string> PossibleInfusionSpells = new List<string>();

                            if (a.Age >= a.DestinyArrivalYear && a.Profession != a.Destiny && (a.Destiny == "sorcerer" || a.Destiny == "warlock") && a.IsImmortal == false /*this will prevent multiple infusions from happening*/)
                            {
                                if (a.Destiny == "warlock")
                                {
                                    a.Profession = a.Destiny;
                                    HistoricalEvents.Add(new Event($"{Date} {a.Name} was infused with incredible power by {DarkDeity.Name} in {location.Name}, blessed to become an immortal warlock tied to his service.", location.Region, new EntityList<Entity>(){a, DarkDeity, location}));
                                    a.GroupLoyalty = -10;
                                    a.IsImmortal = true;
                                    a.Level = 6;
                                    a.Inventory.AddRange(LootTableMachine("magictreasure34"));
                                }
                                else if (a.Destiny == "sorcerer")
                                {
                                    a.Profession = a.Destiny;
                                    HistoricalEvents.Add(new Event($"{Date} {a.Name} was infused with incredible power by {LightDeity.Name} in {location.Name}, blessed to become an eternal sorcerer tied to his service.", location.Region, new EntityList<Entity>(){a, LightDeity, location}));
                                    a.GroupLoyalty = -10;
                                    a.Level = 6;
                                    a.Inventory.AddRange(LootTableMachine("magictreasure34"));
                                    a.IsImmortal = true;
                                }


                                a.AssignSpells();
                            }
                        }

                        Entity GenerateRandomSubject()
                        {
                            EntityList<Entity> subjects = new EntityList<Entity>();

                            subjects.AddRange(AllHistoricalArchitects);
                            subjects.AddRange(AllLocations);
                            subjects.AddRange(AllLocations.SelectMany(loc => loc.AllStructures));
                            subjects.AddRange(AllLocations.SelectMany(loc => loc.AllStructures.SelectMany(structure => structure.HistoricalObjects)));

                            return subjects[rnd.Next(subjects.Count())];
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
                                    HistoricalEvents.Add(new Event($"{Date} {a.Name} began studying at {d.Location.Library.Name} in {location.Name}", location.Region, new EntityList<Entity>(){a, location}));
                                }

                                // If no library found, search other locations
                                if (!FoundLibrary)
                                {
                                    foreach (var Location in AllLocations)
                                    {
                                        if (Location.HomeCivilization == a.HomeLocation.HomeCivilization && Location.Library != null)
                                        {
                                            a.NextMigrationLocation = Location;
                                            a.MigrationReason = "I am headed to the library in " + Location.Name + ", " + Location.Library.Name + ", to study.";
                                            a.IsStudying = true;
                                            a.StudyBuilding = Location.Library;
                                            string migrationEvent = $"{Date} {a.Name} heard of {Location.Library.Name}, a library in {Location.Name}, and migrated there to study.";
                                            string studyEvent = $"{Date} {a.Name} began studying at {Location.Library.Name} in {Location.Name}.";
                                            HistoricalEvents.Add(new Event(migrationEvent, location.Region, new EntityList<Entity>(){a, Location.Library, location}));
                                            HistoricalEvents.Add(new Event(studyEvent, location.Region, new EntityList<Entity>(){a, Location.Library, location}));
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
                        d.ArchitectsToAdd = new EntityHashSet<Architect>();


                        // TODO: Handle architect/architect group stability
                        // also utilize group loyalty


                        // TODO: Handle the disbanding of a Group
                        foreach (Group g in location.GroupsAtThisLocation)
                        {
                            //remove dead people

                            List<Architect> architectsToRemove = new List<Architect>();

                            foreach (Architect a in g.Architects)
                            {
                                if (a.IsAlive == false)
                                {
                                    architectsToRemove.Add(a);
                                }
                            }

                            foreach (Architect a in architectsToRemove)
                            {
                                g.Architects.Remove(a);
                                if (g.Leader == a && g.Architects.Count > 0)
                                {
                                    Architect A = g.Architects[rnd.Next(g.Architects.Count)];

                                    HistoricalEvents.Add(new Event($"{Date} {g.Name} lost their leader {a.Name}, and {A.Name} took their place.", location.Region, new EntityList<Entity>(){g, a, A}));

                                    g.Leader = A;
                                }
                            }

                            if (g.Architects.Count() == 0)
                            {
                                HistoricalEvents.Add(new Event($"{Date} {g.Name} collapsed in {location.Name} due to running out of passionate members.", location.Region, new EntityList<Entity>(){g, location}));
                                GroupsToRemove.Add(g);
                                if (location.Government == g)
                                {
                                    location.Government = null;
                                }
                            }
                            else if (g.Stability < 1)
                            {
                                HistoricalEvents.Add(new Event($"{Date} {g.Name} collapsed in {location.Name} due to a disagreement of values.", location.Region, new EntityList<Entity>(){g, location}));
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
                            else if (g.MonthsOld > 60 && g.Architects.Count() <= 1 && location.Government != g)
                            {
                                HistoricalEvents.Add(new Event($"{Date} {g.Leader.Name} disbanded {g.Name} to become an individual practitioner in {location.Name}.", location.Region, new EntityList<Entity>(){g.Leader, g, location}));
                                g.Leader.Group = null;
                                GroupsToRemove.Add(g);
                            }
                            else if (g.MonthsOld > 180 && g.Architects.Count() == 2 && location.Government != g)
                            {
                                HistoricalEvents.Add(new Event($"{Date} {g.Architects[0].Name} and {g.Architects[1].Name} disbanded {g.Name} and stopped traveling together in {location.Name}.", location.Region, new EntityList<Entity>() { g.Architects[0], g.Architects[1], g, location}));
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

                        foreach (Group GG in location.GroupsAtThisLocation)
                        {
                            foreach (Architect a in GG.ArchitectsToRemove)
                            {
                                a.Group = null;
                                GG.Architects.Remove(a);
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
                                if (a.Group == null && (Game1.ConvertArchitectToGroupType[a.Profession] == g.Type || rnd.Next(2) == 1) && (!g.ArchitectsWhoDeclined.Contains(a)))
                                {
                                    if (rnd.Next(1, 7) == 1)
                                    {
                                        // denial
                                        if (rnd.Next(1, 6) == 1)
                                        {
                                            HistoricalEvents.Add(new Event($"{Date} {a.Name} requested to join {g.Name}, but was denied.", location.Region, new EntityList<Entity>(){ a, g }));
                                            g.ArchitectsWhoDeclined.Add(a);
                                        }
                                        else
                                        {
                                            HistoricalEvents.Add(new Event($"{Date} {a.Name} was invited to join {g.Name}, but decided against it.", location.Region, new EntityList<Entity>() { a, g }));
                                            g.ArchitectsWhoDeclined.Add(a);
                                        }
                                    }
                                    else
                                    {
                                        // acceptance
                                        if (rnd.Next(1, 6) == 1)
                                        {
                                            HistoricalEvents.Add(new Event($"{Date} {a.Name} requested to join {g.Name}, and was accepted.", location.Region, new EntityList<Entity>() { a, g }));
                                            g.Architects.Add(a);
                                            g.Leader.Contacts.Add(a);
                                            a.Contacts.Add(g.Leader);
                                            a.Group = g;
                                            a.GroupLoyalty = 3;
                                        }
                                        else
                                        {
                                            HistoricalEvents.Add(new Event($"{Date} {g.Name} requested that {a.Name} join them, and {a.Name} accepted.", location.Region, new EntityList<Entity>() { a, g }));
                                            g.Architects.Add(a);
                                            g.Leader.Contacts.Add(a);
                                            a.Contacts.Add(g.Leader);
                                            a.Group = g;
                                            a.GroupLoyalty = 3;
                                        }
                                    }

                                }
                            }
                        }

                        // TODO: Handle Architects leaving groups due to loyalty loss or low loyalty and boredom

                        foreach (Architect a in d.Architects)
                        {
                            if (a.Group != null)
                            {
                                if (a.GroupLoyalty <= 0)
                                {
                                    HistoricalEvents.Add(new Event($"{Date} {a.Name} left {a.Group.Name} due to a disagreement with their values.", location.Region, new EntityList<Entity>(){a, a.Group}));
                                    a.Group.ArchitectsToRemove.Add(a);
                                    a.Group = null;
                                }
                                else if (a.GroupLoyalty <= 2 && rnd.Next(1, 100) == 1)
                                {
                                    HistoricalEvents.Add(new Event($"{Date} {a.Name} left {a.Group.Name} due to lack of interest.", location.Region, new EntityList<Entity>(){a, a.Group}));
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


                        d.ArchitectsToAdd = new EntityHashSet<Architect>();

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
                                        Build = rnd.Next(1, 3);
                                    }
                                    else
                                    {
                                        Build = rnd.Next(1, 7);
                                    }

                                    if (Build == 1)
                                    {
                                        int BuildingDecider = rnd.Next(1, 30);
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
                                            Windows = rnd.Next(0, 2);
                                            location.Wealth = location.Wealth - 1000;
                                        }
                                        else if (BuildingDecider < 17)
                                        {
                                            BuildingType = "shrine";
                                            Windows = rnd.Next(0, 3);
                                            Materials.Add(location.HomeCivilization.CulturalStone);
                                            location.Wealth = location.Wealth - 2000;
                                        }
                                        else if (BuildingDecider < 19 && location.Library == null)
                                        {
                                            BuildingType = "library";
                                            Windows = rnd.Next(0, 4);
                                            Materials.Add(location.HomeCivilization.CulturalWood);
                                            location.Wealth = location.Wealth - 2000;
                                        }
                                        else if (BuildingDecider < 21 && d.Taverns.Count < (int)Math.Round((decimal)(d.Population() / 75), 0, MidpointRounding.ToPositiveInfinity))
                                        {
                                            BuildingType = "tavern";
                                            Windows = rnd.Next(0, 2);
                                            Materials.Add(location.HomeCivilization.CulturalWood);
                                            location.Wealth = location.Wealth - 2500;
                                            location.PassiveStructuralIncome += 1;
                                        }
                                        else if (BuildingDecider < 22)
                                        {
                                            BuildingType = "forge";
                                            Windows = rnd.Next(0, 2);
                                            Materials.Add(location.HomeCivilization.CulturalStone);
                                            location.Wealth = location.Wealth - 3000;
                                            location.PassiveStructuralIncome += 1;
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
                                            location.PassiveStructuralIncome += 2;
                                        }
                                        else
                                        {
                                            BuildingType = "bighouse";
                                            Windows = rnd.Next(0, 5);
                                            Materials.Add(location.HomeCivilization.CulturalWood);
                                            location.Wealth = location.Wealth - 1500;
                                        }

                                        while (LightingMethods.Count() == 0)
                                        {
                                            foreach (string S in location.PrimaryLightingStyles)
                                            {
                                                if (rnd.Next(1, 4) != 1)
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

                                        Block DecidedBlock = d.DistrictMap[rnd.Next(0, 49)];

                                        Structure s = new Structure(BuildingType, new EntityList<Object>(), new EntityList<Room>(), DecidedBlock, Materials, PrimarySmells, LightingMethods, llOf5, Windows, (int)Math.Round(Cycle / 290304000));

                                        if(s.Type == "tavern")
                                        {
                                            d.Taverns.Add(s);
                                        }
                                        else if (s.Type == "market")
                                        {
                                            location.Market = s;
                                        }
                                        else if (s.Type == "library")
                                        {
                                            location.Library = s;
                                        }

                                        if (PotentialGroups.Count() == 0)
                                        {
                                            s.Owner = null;
                                        }
                                        else
                                        {
                                            s.Owner = PotentialGroups[rnd.Next(PotentialGroups.Count())];
                                        }

                                        if (s.Type != "house" && s.Type != "bighouse")
                                        {
                                            if (s.Owner == null)
                                            {
                                                HistoricalEvents.Add(new Event($"{Date} {s.Name}, a {s.Type}, was founded by the people of {location.Name}.", location.Region, new EntityList<Entity>(){s, location}));
                                            }
                                            else
                                            {
                                                HistoricalEvents.Add(new Event($"{Date} {s.Name}, a {s.Type}, was founded by {s.Owner.Name}.", location.Region, new EntityList<Entity>(){s, s.Owner}));
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
                            if (location.Government != g && location.Wealth > 5000 && location.IsSavingUpToSettle && location.TruePopulation() > 100 && location.ColonizationDesire > 0 && new List<string> { "town", "city", "camp", "village" }.Contains(location.Type))
                            {
                                //start looking for a new location to find

                                int Attempts = 0;

                                while (Attempts < 10)
                                {
                                    int XChange = rnd.Next(-5, 6);
                                    int ZChange = rnd.Next(-5, 6);

                                    if (!(location.Region.X + XChange < 0 || location.Region.X + XChange > Width - 1 || location.Region.Z + ZChange < 0 || location.Region.Z + ZChange > Length - 1))
                                    {
                                        if (WorldMap[(location.Region.X + XChange) + (location.Region.Z + ZChange) * Width].Biome != "ocean" && WorldMap[(location.Region.X + XChange) + (location.Region.Z + ZChange) * Width].Biome != "void" && WorldMap[(location.Region.X + XChange) + (location.Region.Z + ZChange) * Width].Biome != "snowpeak" && WorldMap[(location.Region.X + XChange) + (location.Region.Z + ZChange) * Width].Biome != "mountain" && WorldMap[(location.Region.X + XChange) + (location.Region.Z + ZChange) * Width].Location == null && WorldMap[(location.Region.X + XChange) + (location.Region.Z + ZChange) * Width].Blight == Purity)
                                        {
                                            // Check if group has at least one member from the humanoid races
                                            bool hasHumanoid = g.Architects.Any(a => HumanoidRaces.Contains(a.Race));

                                            if (hasHumanoid)
                                            {
                                                // Rest of the existing code for settling...
                                                // Ensure primary race is one of the humanoid races present
                                                Race primaryRace = g.Architects.First(a => HumanoidRaces.Contains(a.Race)).Race;

                                                // Code for wealth deduction, race count, and other settlement logic
                                                int PopulationFollowing = rnd.Next(location.TruePopulation() / 10, location.TruePopulation() / 5);

                                                if (g.Architects.Count() * 50 > 5000)
                                                {
                                                    location.Wealth -= 5000;
                                                }
                                                else
                                                {
                                                    location.Wealth -= g.Architects.Count() * 50;
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
                                    X = rnd.Next(Width);
                                    Z = rnd.Next(Length);

                                    if (WorldMap[X + Z * Width].Location == null && WorldMap[X + Z * Width].Biome != "void")
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
                            if (g.Type == "mercenary" && g.Architects.Count() >= 3)
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
                                                if (WorldMap[(g.Leader.Location.X + SearchingX) + (g.Leader.Location.Z + SearchingZ) * Length].Location == null && WorldMap[(g.Leader.Location.X + SearchingX) + (g.Leader.Location.Z + SearchingZ) * Length].Biome != "ocean" && WorldMap[(g.Leader.Location.X + SearchingX) + (g.Leader.Location.Z + SearchingZ) * Length].Biome != "void")
                                                {
                                                    PossibleLocations.Add((g.Leader.Location.X + SearchingX, g.Leader.Location.Z + SearchingZ));
                                                }
                                            }
                                        }
                                    }

                                    if (PossibleLocations.Count() > 0)
                                    {
                                        (int, int) Coords = PossibleLocations[rnd.Next(PossibleLocations.Count())];
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
                            if (!a.HasMadeALegendaryArtifact && rnd.Next(1, 10000 * MonthToDayConstant) == 1 && UndiscoveredLegendarySpells.Count() > 1)
                            {
                                Entity Spell = UndiscoveredLegendarySpells[rnd.Next(UndiscoveredLegendarySpells.Count())];
                                UndiscoveredLegendarySpells.Remove(Spell);
                                DiscoveredLegendarySpells.Add(Spell);

                                a.HasMadeALegendaryArtifact = true;

                                Object o = new Object("", Game1.PossibleMagicalItems[rnd.Next(Game1.PossibleMagicalItems.Count())], new EntityList<Material> { Metals[rnd.Next(Metals.Count())] }, false, false, null, a, 5, false, null, null, null, false);
                                o.Name = GenerateUniqueName("1S" + rnd.Next(2, 4) + "s1w", o, Game1.GameWorld.rnd);
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


                                //find a sanctum location

                                bool Found = false;

                                int X = 0;
                                int Z = 0;

                                while (!Found)
                                {
                                    X = rnd.Next(Width);
                                    Z = rnd.Next(Length);

                                    if (WorldMap[X + Z * Width].Location == null && WorldMap[X + Z * Width].Biome != "void" && WorldMap[X + Z * Width].Biome != "ocean")
                                    {
                                        Found = true;
                                    }
                                }


                                HistoricalEvents.Add(new Event($"{Date} {a.Name} created {o.Name}, a legendary {o.Materials[0].Name} {o.Type} capable of {MagicPhrase}.", WorldMap[X + Z*128], new EntityList<Entity>(){a, o}));

                                LocationBuilderPacket l = new LocationBuilderPacket(a, X, Z, "sanctum", GetRace(""), 0, 0, null, new EntityList<Object> { o }, location, "none");
                                LocationBuilderPackets.Add(l);
                            }
                        }


                        //start saving up to settle


                        if (rnd.Next(1, 500 * MonthToDayConstant) == 1 && location.TruePopulation() > 150 && location.ColonizationDesire > 0)
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
                        d.ArchitectsToRemove = new EntityHashSet<Architect>();

                        //Architects die lul

                        foreach (Architect a in d.Architects)
                        {
                            //immortality boons

                            if (a.IsImmortal && a.RecievedImmortalityBuff == false)
                            {
                                a.DoIDieOfOldAge = false;
                                a.RecievedImmortalityBuff = true;
                                a.TerminalAge = a.TerminalAge + (rnd.Next(40, 200));
                            }
                        }

                        foreach (Architect a in d.ArchitectsToRemove)
                        {
                            d.Architects.Remove(a);
                        }
                        d.ArchitectsToRemove = new EntityHashSet<Architect>();


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


                    if (rnd.Next(1, 1000 * MonthToDayConstant) == 1 && location.IsCapitol && SettlementTypes.Contains(location.Type))
                    {
                        // Decide what type of structure you're going to make
                        string SType = new List<string>() { "observatory", "library", "conservatory", "prison", "tomb", "gallery", "armory" }[rnd.Next(7)];

                        // List to store all valid locations
                        List<(int, int)> validLocations = new List<(int, int)>();

                        // Search for valid locations
                        for (int SearchingX = -10; SearchingX <= 10; SearchingX++)
                        {
                            for (int SearchingZ = -10; SearchingZ <= 10; SearchingZ++)
                            {
                                int X = location.X + SearchingX;
                                int Z = location.Z + SearchingZ;
                                if (X >= 0 && X < Width && Z >= 0 && Z < Length && WorldMap[X + Z * Width].Location == null && WorldMap[X + Z * Width].Biome != "void")
                                {
                                    validLocations.Add((X, Z));
                                }
                            }
                        }

                        // If there are valid locations, pick a random one
                        if (validLocations.Count() > 0)
                        {
                            var selectedLocation = validLocations[rnd.Next(validLocations.Count())];
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
                            int Amount = rnd.Next(1, 6);

                            if (PossibleArchitects.Count() >= Amount)
                            {
                                for (int i = 0; i < Amount; i++)
                                {
                                    int index = rnd.Next(PossibleArchitects.Count());
                                    DecidedArchitects.Add(PossibleArchitects[index]);
                                    PossibleArchitects.RemoveAt(index);
                                }
                            }
                            else
                            {
                                DecidedArchitects = new EntityList<Architect>(PossibleArchitects);
                            }

                            if (DecidedArchitects.Count() > 0)
                            {
                                LocationBuilderPacket l = new LocationBuilderPacket(DecidedArchitects[0], selectedX, selectedZ, SType, GetRace(""), 0, rnd.Next(3), DecidedArchitects[0].HomeLocation.HomeCivilization, new EntityList<Object>(), location, "none");
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

                        if (rnd.Next(1, 100 * MonthToDayConstant) == 1)
                        {
                            if (rnd.Next(2) == 0)
                            {
                                HistoricalEvents.Add(new Event($"{Date} A group of {OutcastCivType}s migrated to {location.Name}.", location.Region, new EntityList<Entity>(){location}));
                                location.Districts[0].UnplacedPopulation += rnd.Next(15, 30);
                            }
                            else
                            {
                                EntityList<Architect> PossibleArch = new EntityList<Architect>();
                                foreach (Architect a in AllHistoricalArchitects)
                                {
                                    if (a.Group == null && !Calamity.Contains(a) && a.IsAlive && a.Location != null && a.Location.HomeCivilization != null && a.Profession != "sorcerer" && a.Profession != "warlock")
                                    {
                                        PossibleArch.Add(a);
                                    }
                                }

                                if (PossibleArch.Count() > 0)
                                {
                                    Architect Migrator = PossibleArch[rnd.Next(PossibleArch.Count())];
                                    Migrator.NextMigrationLocation = location;
                                    Migrator.MigrationReason = "I feel called by the " + OutcastCivType + "s of " + location.Name + "."; 

                                    HistoricalEvents.Add(new Event($"{Date} {Migrator.Name} felt called by the {OutcastCivType}s of {location.Name} and decided to migrate there.", location.Region, new EntityList<Entity>(){Migrator, location}));

                                    // Assign profession based on OutcastCivType
                                    if (outcastProfessions.ContainsKey(OutcastCivType))
                                    {
                                        List<string> professions = outcastProfessions[OutcastCivType];
                                        int professionRoll = rnd.Next(100);

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
                                            Migrator.Clothing.Add(new Object(null, "brassiere", new EntityList<Material>() { Fibers[rnd.Next(Fibers.Count())] }, null));
                                        }
                                        Migrator.Clothing.Add(new Object(null, "undergarment", new EntityList<Material>() { Fibers[rnd.Next(Fibers.Count())] }, null));

                                        Migrator.AddCulturalClothing(Migrator.Location.HomeCivilization.CulturalHeadwear, Fibers[rnd.Next(Fibers.Count())]);
                                        Migrator.AddCulturalClothing(Migrator.Location.HomeCivilization.CulturalNeckwear, Fibers[rnd.Next(Fibers.Count())]);
                                        Migrator.AddCulturalClothing(Migrator.Location.HomeCivilization.CulturalBodywear, Fibers[rnd.Next(Fibers.Count())]);
                                        Migrator.AddCulturalClothing(Migrator.Location.HomeCivilization.CulturalLegwear, Fibers[rnd.Next(Fibers.Count())]);
                                        Migrator.AddCulturalClothing(Migrator.Location.HomeCivilization.CulturalHandwear, Fibers[rnd.Next(Fibers.Count())]);
                                        Migrator.AddCulturalClothing(Migrator.Location.HomeCivilization.CulturalFootwear, Fibers[rnd.Next(Fibers.Count())]);
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
                                                WorldMap[i + j * Width].Location != null)
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

                                                if (docksideOptions.Count() > 0)
                                                {
                                                    dockside = docksideOptions[rnd.Next(docksideOptions.Count())];
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

                            if (validLocations.Count() > 0)
                            {
                                var (FoundX, FoundZ, FoundDockside) = validLocations[rnd.Next(validLocations.Count())];
                                LocationBuilderPacket l = new LocationBuilderPacket(null, FoundX, FoundZ, location.Type, GetRace(""), rnd.Next(4, 10), location.MaxColonizationDesire - 1, location.HomeCivilization, new EntityList<Object>(), location, FoundDockside);
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
                    while (usedCoordinates.Contains((l.X, l.Z)) || l.X <= 0 || l.X >= Width || l.Z <= 0 || l.Z >= Length || WorldMap[l.X + l.Z * Width].Biome == "void" || WorldMap[l.X + l.Z * Width].Location != null)
                    {
                        // Adjust X and Z by small amounts
                        l.X = Math.Max(1, Math.Min(Width - 1, l.X + rnd.Next(-1, 2)));
                        l.Z = Math.Max(1, Math.Min(Length - 1, l.Z + rnd.Next(-1, 2)));
                    }
                    usedCoordinates.Add((l.X, l.Z));
                }


                foreach (LocationBuilderPacket l in LocationBuilderPackets)
                {
                    //build locations
                    Location NewLocation = new Location(l.Type, l.PrimaryRace, l.MiscPopulation, rnd.Next(1000, 4000), l.ColonizationDesire, l.X, l.Z, l.HomeCivilization, WorldMap[l.X + l.Z * Width], l.Dockside);

                    if (l.Type == "camp")
                    {
                        HistoricalEvents.Add(new Event($"{Date} After preparing for years, {l.Government.Name} left {l.BaseLocation.Name} with a following of {l.MiscPopulation} people and founded {NewLocation.Name}.", NewLocation.Region, new EntityList<Entity>(){l.Government, l.BaseLocation, NewLocation}));
                    }
                    else if (l.Type == "spire")
                    {
                        HistoricalEvents.Add(new Event($"{Date} {l.Government.Name} left {l.BaseLocation.Name} and constructed a glorious spire, {NewLocation.Name}.", NewLocation.Region, new EntityList<Entity>(){l.Government, l.BaseLocation, NewLocation}));
                        ((Architect)l.Government).OppositionTags.Add("intruders");
                        ((Architect)l.Government).HomeLocation = NewLocation;
                    }
                    else if (l.Type == "sanctum")
                    {
                        HistoricalEvents.Add(new Event($"{Date} {l.Government.Name} constructed {NewLocation.Name} to house {l.Artifacts[0].Name}.", NewLocation.Region, new EntityList<Entity>(){l.Government, NewLocation, l.Artifacts[0]}));
                    }
                    else if (l.Type == "outpost" || l.Type == "bastion" || l.Type == "fort")
                    {
                        foreach (Architect a in ((Group)l.Government).Architects)
                        {
                            a.OppositionTags.Add("intruders");
                        }
                        ((Group)l.Government).Base = NewLocation;
                    }
                    else if (CalamityStructures.Contains(l.Type))
                    {
                        HistoricalEvents.Add(new Event($"{Date} {l.Government.Name} constructed {NewLocation.Name} to base {((Architect)l.Government).PossessivePronoun} operations.", NewLocation.Region, new EntityList<Entity>(){l.Government, NewLocation}));
                        ((Architect)(l.Government)).InteractionLocation = NewLocation;
                        ((Architect)(l.Government)).HomeLocation = NewLocation;
                        ((Architect)(l.Government)).NextMigrationLocation = NewLocation;
                        ((Architect)(l.Government)).MigrationReason = "I have nothing to say to you.";



                        ((Architect)(l.Government)).KitOutArchitect(((Architect)(l.Government)).Profession);
                    }
                    else if (l.Type == "preserve")
                    {
                        if (l.BaseLocation != null && l.BaseLocation.Type == l.Type)
                        {
                            HistoricalEvents.Add(new Event($"{Date} {l.BaseLocation.Name}, expanded their influence to a new preserve, {NewLocation.Name}.", NewLocation.Region, new EntityList<Entity>(){l.BaseLocation, NewLocation}));
                        }
                        else
                        {
                            HistoricalEvents.Add(new Event($"{Date} {l.Government.Name}, distraught about the destructive nature of the energy people around him, sought to build {NewLocation.Name} to preserve part of the island.", NewLocation.Region, new EntityList<Entity>(){l.Government, NewLocation}));
                            NewLocation.IsCapitol = true;
                            NewLocation.HomeCivilization.Capitol = NewLocation;
                        }
                    }
                    else if (l.Type == "cove")
                    {
                        if (l.BaseLocation != null && l.BaseLocation.Type == l.Type)
                        {
                            HistoricalEvents.Add(new Event($"{Date} {l.BaseLocation.Name}, expanded their influence to a new cove, {NewLocation.Name}.", NewLocation.Region, new EntityList<Entity>(){l.BaseLocation, NewLocation}));
                        }
                        else
                        {
                            HistoricalEvents.Add(new Event($"{Date} {l.Government.Name}, desiring the great wealth of the surrounding trade, built {NewLocation.Name} to base a massive piracy operation.", NewLocation.Region, new EntityList<Entity>(){l.Government, NewLocation}));
                            NewLocation.IsCapitol = true;
                            NewLocation.HomeCivilization.Capitol = NewLocation;
                        }
                    }
                    else if (l.Type == "monastery")
                    {
                        if (l.BaseLocation != null && l.BaseLocation.Type == l.Type)
                        {
                            HistoricalEvents.Add(new Event($"{Date} {l.BaseLocation.Name}, expanded their influence to a new monastery, {NewLocation.Name}.", NewLocation.Region, new EntityList<Entity>(){l.BaseLocation, NewLocation}));
                        }
                        else
                        {
                            HistoricalEvents.Add(new Event($"{Date} {l.Government.Name}, in awe of a beautiful creature, constructed {NewLocation.Name} to honor it and its legacy.", NewLocation.Region, new EntityList<Entity>(){l.Government, NewLocation}));
                            NewLocation.IsCapitol = true;
                            NewLocation.HomeCivilization.Capitol = NewLocation;
                        }
                    }
                    else if (l.Type == "commune")
                    {
                        if (l.BaseLocation != null && l.BaseLocation.Type == l.Type)
                        {
                            HistoricalEvents.Add(new Event($"{Date} {l.BaseLocation.Name}, expanded their influence to a new commune, {NewLocation.Name}.", NewLocation.Region, new EntityList<Entity>(){l.BaseLocation, NewLocation}));
                        }
                        else
                        {
                            HistoricalEvents.Add(new Event($"{Date} {l.Government.Name}, in hatred of the regulations of society, decided to construct {NewLocation.Name}, a commune of complete freedom and expression.", NewLocation.Region, new EntityList<Entity>(){l.Government, NewLocation}));
                            NewLocation.IsCapitol = true;
                            NewLocation.HomeCivilization.Capitol = NewLocation;
                        }
                    }
                    else if (l.Type == "hoard")
                    {
                        if (l.BaseLocation != null && l.BaseLocation.Type == l.Type)
                        {
                            HistoricalEvents.Add(new Event($"{Date} {l.BaseLocation.Name}, expanded their influence to a new hoard, {NewLocation.Name}.", NewLocation.Region, new EntityList<Entity>(){l.BaseLocation, NewLocation}));
                        }
                        else
                        {
                            HistoricalEvents.Add(new Event($"{Date} {l.Government.Name} began to tear apart the land for its treasures, and constructed {NewLocation.Name} to recruit others and scavenge the entire continent.", NewLocation.Region, new EntityList<Entity>(){l.Government, NewLocation}));
                            NewLocation.IsCapitol = true;
                            NewLocation.HomeCivilization.Capitol = NewLocation;
                        }
                    }

                    // The rest of the code remains unchanged

                    AllLocations.Add(NewLocation);
                    NewLocation.UnplacedArtifacts = l.Artifacts;

                    NewLocation.Government = l.Government;

                    foreach (Faction f in AllFactions)
                    {
                        if (f.SatelliteGroups.Contains(l.Government))
                        {
                            ((Group)l.Government).Base = NewLocation;
                        }
                    }

                    if (NewLocation.Type == "camp")
                    {
                        ClaimSwathOfTerritory(NewLocation.HomeCivilization, l.X, l.Z, 2);
                    }

                    WorldMap[l.X + l.Z * Width].Location = NewLocation;
                    WorldMap[l.X + l.Z * Width].Units = new EntityList<Unit>();

                    if (l.Government is Group)
                    {
                        NewLocation.GroupsAtThisLocation.Add((Group)l.Government);

                        foreach (Architect a in ((Group)l.Government).Architects)
                        {
                            a.NextMigrationLocation = NewLocation;
                            a.MigrationReason = "I am now part of the government of " + NewLocation + ", and I am traveling there.";
                        }

                        ((Group)l.Government).Base = NewLocation;
                    }
                    else if (l.Government is Architect && ((Architect)l.Government).Profession != "sovereign" && ((Architect)l.Government).Profession != "heart")
                    {
                        ((Architect)l.Government).NextMigrationLocation = NewLocation;
                        ((Architect)l.Government).MigrationReason = "I am now in charge of " + NewLocation + ", and I am traveling there.";
                    }

                    //special structures need special stuff
                    if (l.Type == "camp")
                    {
                        //well
                        int SX = rnd.Next(2, 5);
                        int SZ = rnd.Next(2, 5);
                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Objects.Add(new Object(null, "well", new EntityList<Material> { NewLocation.HomeCivilization.CulturalStone }, true, true, null, null, 255, false, null, null, null, false));

                        //prism
                        Block chosenBlock = NewLocation.Districts[0].DistrictMap[rnd.Next(0, 49)];
                        Structure Prism = new Structure("prism", l.Artifacts, new EntityList<Room>(), chosenBlock, new EntityList<Material> { NewLocation.HomeCivilization.CulturalStone }, new List<string>(), new List<string> { Game1.LightingStyles[rnd.Next(Game1.LightingStyles.Count())] }, rnd.Next(0, 5), rnd.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                        NewLocation.Prism = Prism;

                        chosenBlock.Structures.Add(Prism);
                    }
                    if (l.Type == "spire")
                    {
                        int SX = rnd.Next(2, 5);
                        int SZ = rnd.Next(2, 5);

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
                        int SX = rnd.Next(2, 5);
                        int SZ = rnd.Next(2, 5);

                        Material m = Stones[rnd.Next(Stones.Count())];

                        Structure s = new Structure("outpost", l.Artifacts, new EntityList<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new EntityList<Material>() { m }, new List<string>(), new List<string> { "torches" }, 3, 5, (int)Math.Round(Cycle / 290304000));

                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                    }
                    else if (ProcgenStructures.Contains(l.Type))
                    {
                        int SX = rnd.Next(2, 5);
                        int SZ = rnd.Next(2, 5);

                        Material m = Stones[rnd.Next(Stones.Count())];

                        Structure s = new Structure(l.Type, l.Artifacts, new EntityList<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new EntityList<Material>() { m }, new List<string>(), new List<string> { "torches" }, 3, 5, (int)Math.Round(Cycle / 290304000));

                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                    }
                    else if (l.Type == "bastion" || l.Type == "fort")
                    {
                        int SX = rnd.Next(2, 5);
                        int SZ = rnd.Next(2, 5);

                        Material m = Stones[rnd.Next(Stones.Count())];

                        Structure s = new Structure(l.Type, l.Artifacts, new EntityList<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new EntityList<Material>() { m }, new List<string>(), new List<string> { "torches" }, 3, 5, (int)Math.Round(Cycle / 290304000));

                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);

                        ((Group)(l.Government)).HomeFaction.Outposts.Add(NewLocation);

                        if (l.Type == "bastion")
                        {
                            ((Group)(l.Government)).HomeFaction.Base = NewLocation;
                        }
                    }
                    else if (l.Type == "sanctum")
                    {
                        int SX = rnd.Next(2, 5);
                        int SZ = rnd.Next(2, 5);

                        Structure s = new Structure("sanctum", l.Artifacts, new EntityList<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new EntityList<Material>() { Archaeon }, new List<string>(), new List<string> { "crystals" }, 3, 999, (int)Math.Round(Cycle / 290304000));

                        ((Architect)l.Government).HomeLocation = NewLocation;

                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                    }
                    else if (CalamityStructures.Contains(l.Type))
                    {
                        int SX = rnd.Next(2, 5);
                        int SZ = rnd.Next(2, 5);

                        Structure s = new Structure(l.Type, l.Artifacts, new EntityList<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new EntityList<Material>() { Stones[rnd.Next(Stones.Count())] }, new List<string>(), new List<string> { "torches" }, 3, 0, (int)Math.Round(Cycle / 290304000));

                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                    }
                    else if (l.Type == "monastery")
                    {
                        int SX = rnd.Next(2, 5);
                        int SZ = rnd.Next(2, 5);

                        Structure s = new Structure(l.Type, l.Artifacts, new EntityList<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new EntityList<Material>() { Stones[rnd.Next(Stones.Count())] }, new List<string>(), new List<string> { "candles" }, 3, 0, (int)Math.Round(Cycle / 290304000));

                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                    }
                    else if (l.Type == "commune")
                    {
                        int SX = rnd.Next(2, 5);
                        int SZ = rnd.Next(2, 5);

                        Structure s = new Structure(l.Type, l.Artifacts, new EntityList<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new EntityList<Material>() { Stones[rnd.Next(Stones.Count())] }, new List<string>(), new List<string> { "torches" }, 3, 0, (int)Math.Round(Cycle / 290304000));

                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                    }
                    else if (l.Type == "hoard")
                    {
                        int Count = rnd.Next(4, 8);
                        for (int i = 0; i < Count; i++)
                        {
                            int SX = rnd.Next(2, 5);
                            int SZ = rnd.Next(2, 5);

                            Structure s = new Structure(l.Type, l.Artifacts, new EntityList<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new EntityList<Material>() { Stones[rnd.Next(Stones.Count())] }, new List<string>(), new List<string> { "none" }, 3, 0, (int)Math.Round(Cycle / 290304000));

                            NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                        }
                    }
                    else if (l.Type == "cove")
                    {
                        int structureCount = rnd.Next(8, 13); // Generate a random number of structures between 8 and 12

                        for (int i = 0; i < structureCount; i++)
                        {
                            int SX = rnd.Next(7); // Random X coordinate within the 7x7 grid
                            int SZ = rnd.Next(7); // Random Z coordinate within the 7x7 grid

                            Block selectedBlock = NewLocation.Districts[0].DistrictMap[SX + SZ * 7];
                            string structureType = (selectedBlock.Biome == "ocean") ? "ship" : "dock";

                            Structure s = new Structure(
                                structureType,
                                new EntityList<Object>(),
                                new EntityList<Room>(),
                                selectedBlock,
                                new EntityList<Material>() { Woods[rnd.Next(Woods.Count())] },
                                new List<string>(),
                                new List<string> { "none" },
                                3,
                                0, (int)Math.Round(Cycle / 290304000)
                            );

                            if (l.Artifacts.Count() > 0)
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
                            for (int i = 0; i < rnd.Next(10, 20); i++)
                            {
                                Block chosenBlock = NewLocation.Districts[0].DistrictMap[rnd.Next(0, 49)];
                                Structure scum = new Structure("scum", new EntityList<Object>(), new EntityList<Room>(), chosenBlock, new EntityList<Material> { ShadeSludge }, new List<string> { l.HomeCivilization.CulturalGemstone.Name }, new List<string> { "veins" }, rnd.Next(0, 5), rnd.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                chosenBlock.Structures.Add(scum);
                            }
                        }
                        else if (l.PrimaryRace.Name == "isofractal" || l.PrimaryRace.Name == "photonexus")
                        {
                            int scaffoldCount = rnd.Next(3, 5); // Reduce the total number of scaffolds
                            HashSet<int> builtBlocks = new HashSet<int>(); // To avoid duplicate structures

                            for (int i = 0; i < scaffoldCount; i++)
                            {
                                int x = rnd.Next(0, 7);
                                int z = rnd.Next(0, 7);

                                if (x == 3 && z == 3) continue; // Skip the center block

                                // Create and place the initial scaffold structure
                                int index = x + z * 7;
                                if (!builtBlocks.Contains(index))
                                {
                                    Block chosenBlock = NewLocation.Districts[0].DistrictMap[index];
                                    Structure scaffold = new Structure("scaffold", new EntityList<Object>(), new EntityList<Room>(), chosenBlock, new EntityList<Material> { l.HomeCivilization.CulturalGemstone }, new List<string> { l.HomeCivilization.CulturalGemstone.Name }, new List<string> { "crystals" }, rnd.Next(0, 5), rnd.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                    chosenBlock.Structures.Add(scaffold);
                                    builtBlocks.Add(index);
                                }

                                // Reflect across the X-axis
                                int reflectedX = 6 - x;
                                index = reflectedX + z * 7;
                                if (x != 3 && !builtBlocks.Contains(index))
                                {
                                    Block reflectedBlockX = NewLocation.Districts[0].DistrictMap[index];
                                    Structure reflectedStructureX = new Structure("scaffold", new EntityList<Object>(), new EntityList<Room>(), reflectedBlockX, new EntityList<Material> { l.HomeCivilization.CulturalGemstone }, new List<string> { l.HomeCivilization.CulturalGemstone.Name }, new List<string> { "crystals" }, rnd.Next(0, 5), rnd.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                    reflectedBlockX.Structures.Add(reflectedStructureX);
                                    builtBlocks.Add(index);
                                }

                                // Reflect across the Z-axis
                                int reflectedZ = 6 - z;
                                index = x + reflectedZ * 7;
                                if (z != 3 && !builtBlocks.Contains(index))
                                {
                                    Block reflectedBlockZ = NewLocation.Districts[0].DistrictMap[index];
                                    Structure reflectedStructureZ = new Structure("scaffold", new EntityList<Object>(), new EntityList<Room>(), reflectedBlockZ, new EntityList<Material> { l.HomeCivilization.CulturalGemstone }, new List<string> { l.HomeCivilization.CulturalGemstone.Name }, new List<string> { "crystals" }, rnd.Next(0, 5), rnd.Next(0, 4), (int)Math.Round(Cycle / 290304000));
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
                                        Structure reflectedStructureBoth = new Structure("scaffold", new EntityList<Object>(), new EntityList<Room>(), reflectedBlockBoth, new EntityList<Material> { l.HomeCivilization.CulturalGemstone }, new List<string> { l.HomeCivilization.CulturalGemstone.Name }, new List<string> { "crystals" }, rnd.Next(0, 5), rnd.Next(0, 4), (int)Math.Round(Cycle / 290304000));
                                        reflectedBlockBoth.Structures.Add(reflectedStructureBoth);
                                        builtBlocks.Add(index);
                                    }
                                }
                            }
                        }
                    }

                    NewLocation.ReferredToNames = new List<string>() { NewLocation.Name };
                }


                // Process migrations for architects
                foreach (Architect a in AllHistoricalArchitects)
                {
                    // Skip architects with specific professions
                    if (a.Profession == "sovereign" || a.Profession == "heart")
                    {
                        continue;
                    }

                    // Check if the architect has a migration location and a unit
                    if (a.NextMigrationLocation != null && a.Unit == null)
                    {
                        // Immediate relocation if Days > 20
                        if (Days > 20)
                        {
                            // Remove architect from the current district if applicable
                            if (a.District != null)
                            {
                                a.District.ArchitectsToRemove.Add(a);
                            }

                            // Directly relocate the architect to their next migration location
                            a.District = a.NextMigrationLocation.Districts[rnd.Next(a.NextMigrationLocation.Districts.Count())];
                            a.District.ArchitectsToAdd.Add(a);


                            //HistoricalEvents.Add(new Event(Date + " " + a.Name + " went to " + a.NextMigrationLocation.Name, null, new EntityList<Entity>()));
                            a.Location = a.NextMigrationLocation;


                            // Handle special migration logic
                            if (new List<string> { "commune", "mound", "monastery", "outpost" }.Contains(a.NextMigrationLocation.Type) || CalamityStructures.Contains(a.NextMigrationLocation.Type))
                            {
                                if ((rnd.Next(3) == 1 || CalamityStructures.Contains(a.NextMigrationLocation.Type)) && !a.MigrationKitted)
                                {
                                    a.KitOutArchitect("warriorpower" + a.Level);
                                    a.MigrationKitted = true;
                                }

                                if (!a.OppositionTags.Contains("intruders") && !a.Bound)
                                {
                                    a.OppositionTags.Add("intruders");
                                }
                            }

                            // Clear migration location after relocation
                            a.NextMigrationLocation = null;
                            a.MigrationReason = "";
                            continue; // Skip unit creation and continue with the next architect
                        }
                        // For Days <= 20, create a unit for the architect
                        else
                        {
                            if (a.District != null)
                            {
                                a.District.ArchitectsToRemove.Add(a);
                            }

                            Location l = (a.Location == null) ? AllLocations[rnd.Next(AllLocations.Count)] : a.Location;
                            Unit u = new Unit(l.Region, "traveler", new EntityList<Architect> { a }, a.NextMigrationLocation);
                            a.NextMigrationLocation = null;
                            a.MigrationReason = "";
                            AllUnits.Add(u);
                            l.Region.Units.Add(u);
                        }
                    }
                }

                // List to track units that have reached their destination
                List<Unit> UnitsToRemove = new List<Unit>();
                // Process units only if Days <= 20
                if (Days <= 20)
                {
                    foreach (Unit u in AllUnits)
                    {
                        Region TargetRegion = u.TargetLocation?.Region ?? u.WanderRegion;

                        if (TargetRegion == null)
                        {
                            // Set initial wander region if none exists
                            u.WanderRegion = u.Region;
                            List<(int, int)> PossibleLocations = new List<(int, int)>();

                            // Search for possible wander locations
                            for (int SearchingX = -10; SearchingX <= 10; SearchingX++)
                            {
                                for (int SearchingZ = -10; SearchingZ <= 10; SearchingZ++)
                                {
                                    int newX = u.Region.X + SearchingX;
                                    int newZ = u.Region.Z + SearchingZ;

                                    // Ensure location is within bounds
                                    if (newX >= 0 && newX < Width && newZ >= 0 && newZ < Length)
                                    {
                                        var currentRegion = WorldMap[newX + newZ * Width];
                                        if (currentRegion.Biome != "void")
                                        {
                                            PossibleLocations.Add((newX, newZ));
                                        }
                                    }
                                }
                            }

                            if (PossibleLocations.Count > 0)
                            {
                                var randomLocation = PossibleLocations[rnd.Next(PossibleLocations.Count)];
                                u.WanderRegion = WorldMap[randomLocation.Item1 + randomLocation.Item2 * Width];
                                TargetRegion = u.WanderRegion;
                            }
                        }

                        if (TargetRegion != null)
                        {
                            u.TravelPoints += (int)Math.Round(Days * 864000);

                            // Continue moving unit until target is reached or travel points exhausted
                            (int DeltaX, int DeltaZ)[] EvenRowDirections = new (int, int)[]
{
    (-1, -1), // Northwest
    (0, -1),  // Northeast
    (-1, 0),  // West
    (1, 0),   // East
    (-1, 1),  // Southwest
    (0, 1)    // Southeast
};

                            (int DeltaX, int DeltaZ)[] OddRowDirections = new (int, int)[]
                            {
    (0, -1),  // Northwest
    (1, -1),  // Northeast
    (-1, 0),  // West
    (1, 0),   // East
    (0, 1),   // Southwest
    (1, 1)    // Southeast
                            };

                            // Continue moving unit until target is reached or travel points exhausted
                            while (u.TravelPoints >= 48000)
                            {
                                u.Region.Units.Remove(u); // Remove from current region

                                // Select the appropriate set of directions based on the current row
                                var HexDirections = (u.Region.Z % 2 == 0) ? EvenRowDirections : OddRowDirections;

                                // Calculate the best direction to move
                                (int BestDeltaX, int BestDeltaZ) bestMove = (0, 0);
                                int minDistance = int.MaxValue;

                                foreach (var (DeltaX, DeltaZ) in HexDirections)
                                {
                                    // Calculate the next potential position
                                    int nextX = u.Region.X + DeltaX;
                                    int nextZ = u.Region.Z + DeltaZ;

                                    // Ensure the move is within bounds
                                    if (nextX >= 0 && nextX < Width && nextZ >= 0 && nextZ < Length)
                                    {
                                        // Calculate distance to the target
                                        int distance = Math.Abs(TargetRegion.X - nextX) + Math.Abs(TargetRegion.Z - nextZ);

                                        // Update the best move if this direction is closer
                                        if (distance < minDistance)
                                        {
                                            minDistance = distance;
                                            bestMove = (DeltaX, DeltaZ);
                                        }
                                    }
                                }

                                // Apply the best move
                                int newX = u.Region.X + bestMove.BestDeltaX;
                                int newZ = u.Region.Z + bestMove.BestDeltaZ;

                                // Update the unit's region and deduct travel points
                                u.Region = WorldMap[newX + newZ * Width];
                                u.Region.Units.Add(u);
                                u.TravelPoints -= 36000;

                                // Check if the unit has reached the target
                                if (u.Region.X == TargetRegion.X && u.Region.Z == TargetRegion.Z)
                                {
                                    // Handle arrival logic
                                    foreach (Architect a in u.UnitArchitects)
                                    {
                                        if (a.NextMigrationLocation != null)
                                        {
                                            a.District = a.NextMigrationLocation.Districts[rnd.Next(a.NextMigrationLocation.Districts.Count())];
                                            a.District.ArchitectsToAdd.Add(a);
                                            a.Location = a.NextMigrationLocation;

                                            if (new List<string> { "commune", "mound", "monastery", "outpost" }.Contains(a.NextMigrationLocation.Type) || CalamityStructures.Contains(a.NextMigrationLocation.Type))
                                            {
                                                if ((rnd.Next(3) == 1 || CalamityStructures.Contains(a.NextMigrationLocation.Type)) && !a.MigrationKitted)
                                                {
                                                    a.KitOutArchitect("warriorpower" + a.Level);
                                                    a.MigrationKitted = true;
                                                }

                                                if (!a.OppositionTags.Contains("intruders") && !a.Bound)
                                                {
                                                    a.OppositionTags.Add("intruders");
                                                }
                                            }

                                            a.NextMigrationLocation = null;
                                            a.MigrationReason = "";
                                            a.Unit = null;
                                        }
                                    }

                                    if (u.TargetLocation != null)
                                    {
                                        u.Region.Units.Remove(u); // Remove unit upon arrival
                                        UnitsToRemove.Add(u); // Mark for removal
                                    }
                                    u.TargetLocation = null;
                                    u.WanderRegion = null;
                                    u.TravelPoints = 0;
                                    break;
                                }
                            }
                        }
                    }
                }
                // Remove units that have arrived at their target
                foreach (Unit u in UnitsToRemove)
                {
                    AllUnits.Remove(u);
                }


                // Forget Dead Entities and Clean Up
                List<Entity> combinedEntities = new List<Entity>();
                combinedEntities.AddRange(AllHistoricalArchitects);
                combinedEntities.AddRange(AllFactions);
                combinedEntities.AddRange(AllLocations);
                combinedEntities.AddRange(Groups);
                combinedEntities.AddRange(Civilizations);

                // Mark dead entities and remove incapacitated enemies
                foreach (Entity e in combinedEntities)
                {
                    if (e.Incapacitated) continue;

                    bool isDead = e switch
                    {
                        Architect architect => !architect.IsAlive || architect.Bound,
                        Faction faction => !faction.SatelliteGroups.Any(g => g.Architects.Count > 0),
                        Location location => location.TruePopulation() == 0,
                        Group group => group.Architects.Count == 0,
                        Association association => Game1.GameState != "ascendant",
                        _ => false
                    };

                    if (isDead)
                    {
                        e.Incapacitated = true;
                    }
                }

                // Clean up enemies across all entities
                foreach (Entity e in combinedEntities)
                {
                    e.Enemies.RemoveWhere(enemy => enemy.Incapacitated);
                }
                HyperThreats.RemoveWhere(enemy => enemy.Incapacitated);

                foreach(Location l in AllLocations)
                {
                    foreach (District d in l.Districts)
                    {
                        // Add and remove architects as needed
                        d.Architects.AddRange(d.ArchitectsToAdd);
                        d.Architects.RemoveAll(a => d.ArchitectsToRemove.Contains(a));
                        d.ArchitectsToAdd.Clear();
                        d.ArchitectsToRemove.Clear();

                        // Ensure all architects are properly linked to their district and location
                        foreach (Architect a in d.Architects)
                        {
                            a.District = d;
                            a.Location = d.Location;
                        }
                    }
                }

                // Process units in world map cells
                for (int x = 0; x < Game1.GameWorld.Width; x++)
                {
                    for (int z = 0; z < Game1.GameWorld.Width; z++)
                    {
                        var cell = Game1.GameWorld.WorldMap[x + z * Game1.GameWorld.Width];

                        cell.Units = new EntityList<Unit>(cell.Units.Where(e =>
                        {
                            e.UnitArchitects.RemoveAll(a => !a.IsAlive);
                            return e.UnitArchitects.Count > 0;
                        }));
                    }
                }

                // Hold a council if conditions are met
                if (rnd.Next(100) == 1 && Year > 50)
                {
                    // Gather eligible architects for the council
                    var attendees = new EntityList<Architect>(AllHistoricalArchitects.Where(architect =>
                        !Calamity.Contains(architect) && architect.Reputation > -20 && architect.Profession switch
                        {
                            "elder" => rnd.Next(100) < 10,
                            "commander" => rnd.Next(100) < 5,
                            "leader" => rnd.Next(100) < 10,
                            "political figure" => rnd.Next(100) < 20,
                            "archbard" or "archluminary" or "archartificer" or "archdruid" => rnd.Next(100) < 20,
                            "captain" or "diplomat" => rnd.Next(100) < 60,
                            "alpha" => rnd.Next(100) < 100,
                            _ when architect.Profession.StartsWith("arch") => rnd.Next(100) < 30,
                            _ => false
                        }));

                    if (attendees.Count > 0)
                    {
                        // Pick a random civilization and hold the council
                        Civilization selectedCivilization = Civilizations[rnd.Next(Civilizations.Count)];
                        Location councilLocation = selectedCivilization.Capitol;

                        var sortedAttendees = attendees.OrderByDescending(a => a.Reputation).Take(rnd.Next(2, 5)).ToList();
                        string organizers = FormatList(sortedAttendees.Select(a => a.Name).ToList());

                        HistoricalEvents.Add(new Event(
                            $"{Date} A council was held by the capital of {selectedCivilization.Name}, {councilLocation.Name}. The organizers were {organizers}.",
                            councilLocation.Region,
                            new EntityList<Entity> { selectedCivilization, councilLocation }.Union(attendees),
                            "significant"
                        ));

                        // Find most common enemies across all entities
                        var enemyCounts = combinedEntities
                            .SelectMany(e => e.Enemies)
                            .GroupBy(enemy => enemy)
                            .ToDictionary(g => g.Key, g => g.Count());

                        var mostCommonEnemies = new EntityList<Entity>(enemyCounts.OrderByDescending(kvp => kvp.Value)
                            .Take(rnd.Next(2, 6))
                            .Select(kvp => kvp.Key));

                        string threats = FormatList(mostCommonEnemies.Select(e => e.Name).ToList());

                        HistoricalEvents.Add(new Event(
                            $"{Date} The topic of the council was to handle the threats of {threats}. All were declared grave threats that much attention should be paid.",
                            councilLocation.Region,
                            mostCommonEnemies,
                            "significant"
                        ));

                        HyperThreats.AddRange(mostCommonEnemies);
                        HyperThreats = HyperThreats.Distinct();
                    }
                }


            }

            if (IncreaseCycle)
            {
                Cycle += (double)Days * 864000;
            }



            LivingArchitects = CurrentlyCountingArchitects;
            DeadArchitects = AllHistoricalArchitects.Count() - LivingArchitects;

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

            string ChosenItem = usefulstuff[rnd.Next(usefulstuff.Count())];

            EntityList<Material> Materials = new EntityList<Material>
            {
                Metals[rnd.Next(Metals.Count())],
                Cloths[rnd.Next(Cloths.Count())]
            };

            while (true)
            {
                if (rnd.Next(0, 2) == 0)
                {
                    break;
                }

                int Chooser = rnd.Next(1, 4);

                if (Chooser == 1)
                {
                    Materials.Add(Gemstones[rnd.Next(Gemstones.Count())]);
                }
                else if (Chooser == 2)
                {
                    Materials.Add(Stones[rnd.Next(Stones.Count())]);
                }
                else
                {
                    Materials.Add(Woods[rnd.Next(Woods.Count())]);
                }
            }

            Object o = new Object(null, ChosenItem, Materials, null);
            o.Rarity = GenerateItemRarity(Level);
            o.ApplyImbuements(1);
            o.IsMagical = true;
            o.Name = GenerateUniqueName("1S" + rnd.Next(4) + "s1w", o, Game1.GameWorld.rnd);
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

            string ChosenItem = potentialMagicalItems[rnd.Next(potentialMagicalItems.Count())];
            List<string> commonNouns = itemCommonNouns[ChosenItem];
            string ChosenCommonNoun = Game1.Capitalize(commonNouns[rnd.Next(commonNouns.Count())]);
            string Subject = Game1.Capitalize(Game1.Words[rnd.Next(Game1.Words.Count())]);

            List<string> namingFormats = new List<string>
            {
                "The {0} of {1}",
                "{1} {0}",
                "{0} of {1}",
                "The {1}'s {0}",
                "{1}'s {0}",
            };

            string format = namingFormats[rnd.Next(namingFormats.Count())];
            string name = string.Format(format, ChosenCommonNoun, Subject);

            EntityList<Material> Materials = new EntityList<Material>
            {
                Metals[rnd.Next(Metals.Count())]
            };

            while (true)
            {
                if (rnd.Next(0, 2) == 0)
                {
                    break;
                }

                int Chooser = rnd.Next(1, 4);

                if (Chooser == 1)
                {
                    Materials.Add(Gemstones[rnd.Next(Gemstones.Count())]);
                }
                else if (Chooser == 2)
                {
                    Materials.Add(Stones[rnd.Next(Stones.Count())]);
                }
                else
                {
                    Materials.Add(Woods[rnd.Next(Woods.Count())]);
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
            for (int i = rnd.Next(1, 20); i != 0; i--)
            {
                list.Add(new Object(null, "fragment", new EntityList<Material>() { Vitalium }, null));
            }

            // Loot generation based on Quality
            for (int q = 0; q < Quality; q++)
            {
                // Generate a weapon with random material
                if (rnd.Next(1, 100) <= 10) // 10% chance
                {
                    var weapons = new List<string> { "shortsword", "knife", "greatsword", "battle axe", "axe", "greataxe", "rapier", "spear", "pike", "mace", "hammer", "shield", "whip", "scourge", "flail", "chain" };
                    list.Add(new Object(null, weapons[rnd.Next(weapons.Count())], new EntityList<Material>() { GetRandomMaterialByStrength(Metals, ConvertLevelToToughness(Level)) }, null));
                }

                if (rnd.Next(1, 100) <= 4) //4 percent chance
                {
                    EntityList<Material> m = new EntityList<Material>() { GetRandomMaterialByStrength(Metals, ConvertLevelToToughness(Level)) };

                    for (int i = rnd.Next(3, 9); i != 0; i--)
                    {
                        list.Add(new Object(null, "dagger", m, null));
                    }
                }

                // Generate a piece of armor with random material
                if (rnd.Next(1, 100) <= 5) // 5% chance for armor
                {
                    var armors = new List<string> { "helmet", "chestplate", "left gauntlet", "right gauntlet", "pants", "cape", "left glove", "right glove", "left boot", "right boot" };
                    list.Add(new Object(null, armors[rnd.Next(armors.Count())], new EntityList<Material>() { GetRandomMaterialByStrength(Metals, ConvertLevelToToughness(Level)) }, null));
                }

                // Generate a piece of clothing with random material
                if (rnd.Next(1, 100) <= 5) // 5% chance for clothing
                {
                    var clothings = new List<string> { "large hat", "small hat", "hood", "shortsleeve shirt", "longsleeve shirt", "uppershirt", "straps", "shorts", "kilt", "wraps", "left shoe", "right shoe" };
                    list.Add(new Object(null, clothings[rnd.Next(clothings.Count())], new EntityList<Material>() { Cloths[rnd.Next(Cloths.Count())] }, null));
                }


                // Generate a piece of jewelry with random material
                if (rnd.Next(1, 100) <= 15) // 15% chance
                {
                    var jewelry = new List<string> { "amulet", "flair", "cut gem", "gem" };
                    list.Add(new Object(null, jewelry[rnd.Next(jewelry.Count())], new EntityList<Material>() { Gemstones[rnd.Next(Gemstones.Count())], Metals[rnd.Next(Metals.Count())] }, null));
                }

                // Generate a household item with random material
                if (rnd.Next(1, 100) <= 30) // 30% chance
                {
                    var householdItems = new List<string> { "small pot", "big mug", "small cup", "big bowl" };
                    list.Add(new Object(null, householdItems[rnd.Next(householdItems.Count())], new EntityList<Material>() { Stones[rnd.Next(Stones.Count())] }, null));
                }

                // Generate a piece of clothing with random material
                if (rnd.Next(1, 100) <= 20) // 20% chance
                {
                    var clothes = new List<string> { "cape", "hood", "longsleeve shirt", "pants", "shortsleeve shirt", "uppershirt", "straps", "shorts", "kilt", "wraps", "left glove", "right glove", "large hat", "small hat", "right boot", "left boot", "right shoe", "left shoe" };
                    list.Add(new Object(null, clothes[rnd.Next(clothes.Count())], new EntityList<Material>() { Cloths[rnd.Next(Cloths.Count())] }, null));
                }

                // Generate a scroll bearing wisdom of a past age

                if (rnd.Next(20) == 1)
                {
                    Object o = new Object(null, "skill scroll", new EntityList<Material>() { Cloths[rnd.Next(Cloths.Count())] }, null);
                    o.SpecialKnowledge = Game1.GameWorld.AllSkills[rnd.Next(Game1.GameWorld.AllSkills.Count())];
                    list.Add(o);
                }

                if (rnd.Next(1, 100) <= 20) // 10% chance for healing item
                {
                    var healingItems = new List<string> { "salve", "bandage", "vial" };
                    string selectedHealingItem = healingItems[rnd.Next(healingItems.Count())];

                    switch (selectedHealingItem)
                    {
                        case "salve":
                            list.Add(new Object(null, "salve", new EntityList<Material>() { Fibers[rnd.Next(Fibers.Count())] }, null));
                            break;
                        case "bandage":
                            list.Add(new Object(null, "bandage", new EntityList<Material>() { Cloths[rnd.Next(Cloths.Count())] }, null));
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

        public void AssignRealms()
        {
            // Step 1: Initialize Temporary Realms for Each Civilization
            List<(Civilization civilization, float X, float Z)> temporaryRealms = new List<(Civilization, float, float)>();

            foreach (var civ in Civilizations)
            {
                // Create a temporary realm for each civilization
                temporaryRealms.Add((civ, civ.Capitol.X, civ.Capitol.Z));
            }

            // Step 2: Combine Temporary Realms Until RealmsCount is Reached
            while (temporaryRealms.Count > RealmsCount)
            {
                float minDistance = float.MaxValue;
                int indexToMerge1 = -1, indexToMerge2 = -1;

                // Find the two closest temporary realms
                for (int i = 0; i < temporaryRealms.Count; i++)
                {
                    for (int j = i + 1; j < temporaryRealms.Count; j++)
                    {
                        float distance = CalculateDistance(
                            (int)temporaryRealms[i].X, (int)temporaryRealms[i].Z,  // Cast to int
                            (int)temporaryRealms[j].X, (int)temporaryRealms[j].Z); // Cast to int

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            indexToMerge1 = i;
                            indexToMerge2 = j;
                        }
                    }
                }

                // Merge the two closest realms
                if (indexToMerge1 >= 0 && indexToMerge2 >= 0)
                {
                    var mergedRealm = (
                        temporaryRealms[indexToMerge1].civilization,
                        (temporaryRealms[indexToMerge1].X + temporaryRealms[indexToMerge2].X) / 2,
                        (temporaryRealms[indexToMerge1].Z + temporaryRealms[indexToMerge2].Z) / 2
                    );

                    // Remove the original realms and add the merged one
                    temporaryRealms.RemoveAt(indexToMerge2);
                    temporaryRealms.RemoveAt(indexToMerge1);
                    temporaryRealms.Add(mergedRealm);
                }
            }

            // Step 3: Convert Temporary Realms to Final Realms
            for (int i = 0; i < temporaryRealms.Count; i++)
            {
                var tempRealm = temporaryRealms[i];
                Realm newRealm = new Realm
                {
                    Color = Game1.Colors[(i * 2) % Game1.Colors.Count], // Assign colors cyclically
                    X = (int)tempRealm.X, // Cast to int
                    Z = (int)tempRealm.Z  // Cast to int
                };

                newRealm.Name = GenerateUniqueName("1S3s", newRealm, Game1.GameWorld.rnd);

                Realms.Add(newRealm);
                tempRealm.civilization.Capitol.Region.Realm = newRealm; // Set the controlling realm for the capital
                newRealm.ContainedRegions.Add(tempRealm.civilization.Capitol.Region); // Add the capital region to the realm
            }

            // Step 4: Assign Regions to Realms (Treating all regions the same)
            foreach (var region in WorldMap)
            {
                Realm closestRealm = null;
                float closestDistance = float.MaxValue;

                foreach (var realm in Realms)
                {
                    float distance = CalculateDistance(
                        (int)region.X, (int)region.Z, // Cast to int
                        (int)realm.X, (int)realm.Z); // Cast to int

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestRealm = realm;
                    }
                }

                if (closestRealm != null)
                {
                    closestRealm.ContainedRegions.Add(region);
                    region.Realm = closestRealm; // Assign the controlling realm directly
                }
            }
        }


        private List<Region> GetCivilizationClusters()
        {
            // Identify regions with significant settlement concentrations and group them into clusters
            Dictionary<Region, int> settlementDensityMap = new Dictionary<Region, int>();

            for (int i = 0; i < WorldMap.Count(); i++)
            {
                Region currentRegion = WorldMap[i];
                if (currentRegion.Location != null && SettlementTypes.Contains(currentRegion.Location.Type))
                {
                    int density = CalculateSettlementDensity(currentRegion);
                    settlementDensityMap[currentRegion] = density;
                }
            }

            // Group regions into clusters based on proximity and density
            var clusters = new List<Region>();

            foreach (var region in settlementDensityMap.Keys)
            {
                // Logic to group regions into clusters based on proximity and density
                bool addedToCluster = false;
                foreach (var cluster in clusters)
                {
                    if (CalculateDistance(region.X, region.Z, cluster.X, cluster.Z) < 10) // Example threshold
                    {
                        // Merge region into existing cluster
                        addedToCluster = true;
                        break;
                    }
                }

                if (!addedToCluster)
                {
                    clusters.Add(region);
                }
            }

            // Sort clusters by density and return the centers
            return clusters.OrderByDescending(c => settlementDensityMap[c]).ToList();
        }
        private int CalculateSettlementDensity(Region region)
        {
            int density = 0;
            int radius = 5; // Example radius for nearby settlements

            for (int x = Math.Max(0, region.X - radius); x <= Math.Min(Width - 1, region.X + radius); x++)
            {
                for (int z = Math.Max(0, region.Z - radius); z <= Math.Min(Length - 1, region.Z + radius); z++)
                {
                    Region nearbyRegion = WorldMap[x + z * Width];
                    if (nearbyRegion.Location != null && SettlementTypes.Contains(nearbyRegion.Location.Type))
                    {
                        density++;
                    }
                }
            }

            return density;
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

        public string GenerateUniqueName(string pattern, Entity e, SerializableRandom rand)
        {
            string GenerateName(int WordsToCombine, bool Caps)
            {
                string word = string.Empty;
                

                for (int i = 0; i < WordsToCombine; i++)
                {
                    word += Game1.Words[rand.Next(Game1.Words.Count())];
                }

                if (Caps)
                {
                    word = word.Substring(0, 1).ToUpper() + word.Substring(1);
                }

                return word;
            }

            string GenerateSyllableName(int SyllablesCount, bool Caps, SerializableRandom rand)
            {
                string word = string.Empty;
                for (int i = 0; i < SyllablesCount; i++)
                {
                    word += Game1.Syllables[rand.Next(Game1.Syllables.Count())];
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
                                generatedName += GenerateSyllableName(repeatCount, true, rand);
                                break;
                            case 'W':
                                generatedName += GenerateName(repeatCount, true);
                                break;
                            case 's':
                                generatedName += GenerateSyllableName(repeatCount, false, rand);
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
                string firstName = String.Concat(Game1.FirstNames[rnd.Next(Game1.FirstNames.Count())], Game1.NameSuffixes[rnd.Next(Game1.NameSuffixes.Count())]);
                string lastName = String.Concat((Game1.LastNames[rnd.Next(Game1.LastNames.Count())]).Substring(0, 1).ToUpper(),
                                                 (Game1.LastNames[rnd.Next(Game1.LastNames.Count())]).Substring(1).ToLower());
                //OK so this system literally takes a last name from the list, and then takes a random letter from a random last name and replaces the first letter. It's not supposed to do that, BUT IT WORKS SO WELL WHAT

                generatedName = String.Concat(firstName, " ", lastName);
            }

            SubjectCatalogue.Add(generatedName, e);
            return generatedName;
        }


        public string GenerateItemRarity(int level)
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
            int randomNumber = rnd.Next(0, totalWeight);
            int weightSum = 0;

            for (int i = 0; i < rarities.Count(); i++)
            {
                weightSum += rarityWeights[i];
                if (randomNumber < weightSum)
                    return rarities[i];
            }

            return "Error: Rarity selection failed";
        }
    }
}