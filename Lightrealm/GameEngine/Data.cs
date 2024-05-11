using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lightrealm.GameEngine
{
    internal class Data
    {
        public List<string> AnimalSizes = new List<string>() { "miniscule", "smaller", "small", "medium", "humanoid", "large", "huge" };
        public List<string> AllSizes = new List<string>() { "ethereal", "miniscule", "smaller", "small", "medium", "humanoid", "large", "huge", "colossal", "archancient" };
        public List<string> AllWeapons = new List<string>
        {
            "sword", "greatsword", "axe", "greataxe", "knife",
            "rapier", "spear", "pike",
            "mace", "hammer", "shield",
            "whip", "flail", "chain"
        };
        public List<string> WeaponTypes = new List<string>() { "sword", "greatsword", "battle axe", "greataxe", "rapier", "spear", "pike", "mace", "hammer", "shield", "whip", "scourge" };

        public List<string> Domains = new List<string>
        {
            "shadows",
            "life",
            "death",
            "time",
            "stars",
            "heat",
            "void",
            "storms",
            "lore",
            "mind",
            "soul",
            "body",
            "space",
            "reality",
            "chaos",
            "order",
            "nature",
            "earth",
            "water",
            "fire",
            "air",
            "dreams",
            "music",
            "war",
            "peace",
            "fate",
            "luck",
            "craftsmanship",
            "wisdom",
            "mountains",
            "forests",
            "seas",
            "rivers",
            "deserts",
            "skies",
            "twilight",
            "dusk",
            "dawn",
            "justice",
            "mercy",
            "vengeance",
            "joy",
            "beauty",
            "fear",
            "courage",
            "mystery",
            "knowledge",
            "exploration",
            "civilization",
            "wilderness",
            "magic",
            "art",
            "celebration",
            "silence",
            "echoes",
            "decay",
            "balance",
            "creation",
            "destruction",
            "power",
            "eternity",
            "nightmares",
            "sacrifice",
            "stability",
            "change",
            "harmony",
            "discord",
            "vision",
            "memory",
            "truth",
            "deception",
            "hope",
            "despair",
            "wealth",
            "poverty",
            "disease",
            "youth",
            "beginnings",
            "endings",
            "exile",
            "theft",
            "victory",
            "defeat",
            "secrets",
            "ruin"
        };

        public List<string> CantDuckBodyParts = new List<string>
        {
            "left lower leg",
            "right lower leg",
            "left upper leg",
            "right upper leg",
            "left foot",
            "right foot"
        };

        public List<string> CantJumpBodyParts = new List<string>
        {
            "head",
            "neck",
            "torso"
        };


        public Dictionary<string, List<Material>> MaterialsFromColors = new Dictionary<string, List<Material>>
        {
            { "maroon", new List<Material>{ new Material("mahogany", "wood", 1, 1), new Material("crimson_beetle", "insect", 1, 1), new Material("rust", "metal", 1, 1) } },
            { "red", new List<Material>{ new Material("rose", "plant", 1, 1), new Material("redtulip", "plant", 1, 1), new Material("clay", "sediment", 1, 1) } },
            { "orange", new List<Material>{ new Material("citrus", "plant", 1, 1), new Material("amber", "plant", 1, 1), new Material("bronzelily", "plant", 1, 1) } },
            { "yellow", new List<Material>{ new Material("emberflare", "plant", 1, 1), new Material("honey", "plant", 1, 1), new Material("lemon", "plant", 1, 1) } },
            { "limegreen", new List<Material>{ new Material("lime_peel", "plant", 1, 1), new Material("emerald_grass", "plant", 1, 1), new Material("verdantwing feather", "feather", 1, 1) } },
            { "green", new List<Material>{ new Material("lichen", "plant", 1, 1), new Material("cactus", "plant", 1, 1), new Material("moss", "plant", 1, 1) } },
            { "lightblue", new List<Material>{ new Material("glimmerplume feather", "feather", 1, 1), new Material("slush", "stone", 1, 1), new Material("aquamarine", "gemstone", 1, 1) } },
            { "cyan", new List<Material>{ new Material("algae", "plant", 1, 1), new Material("turquoise", "gemstone", 1, 1), new Material("electric_eel_skin", "leather", 1, 1) } },
            { "blue", new List<Material>{ new Material("blueberry_juice", "fruit", 1, 1), new Material("sapphire_gem", "gem", 1, 1), new Material("deep_ocean_silt", "sediment", 1, 1) } },
            { "purple", new List<Material>{ new Material("royal_grapes", "fruit", 1, 1), new Material("amethyst_crystal", "gem", 1, 1), new Material("mystic_flower", "plant", 1, 1) } },
            { "magenta", new List<Material>{ new Material("wild_berry_blend", "fruit", 1, 1), new Material("pink_petals", "plant", 1, 1), new Material("rose_quartz", "gem", 1, 1) } },
            { "coral", new List<Material>{ new Material("coral_branch", "coral", 1, 1), new Material("sea_anemone", "animal", 1, 1), new Material("tropical_shell", "shell", 1, 1) } },
            { "white", new List<Material>{ new Material("pure_snowflake", "ice", 1, 1), new Material("moonstone", "gem", 1, 1), new Material("cloud_feathers", "feather", 1, 1) } },
            { "gray", new List<Material>{ new Material("ashen_soil", "sediment", 1, 1), new Material("smoky_quartz", "gem", 1, 1), new Material("stormy_cloud", "cloud", 1, 1) } },
            { "black", new List<Material>{ new Material("obsidian_rock", "rock", 1, 1), new Material("midnight_rose", "plant", 1, 1), new Material("shadowy_silk", "fabric", 1, 1) } },
            { "brown", new List<Material>{ new Material("earthy_bark", "wood", 1, 1), new Material("cocoa_beans", "plant", 1, 1), new Material("hazel_nuts", "nut", 1, 1) } }

            //fix this later but i dont want to right now
        };

        public List<string> Headwear = new List<string>() { "none", "none", "none", "large hat", "small hat", "hood" };
        public List<string> Neckwear = new List<string>() { "none", "none", "none", "amulet", "amulet/amulet/amulet", "flair" };
        public List<string> Handwear = new List<string>() { "none", "none", "none", "left glove/right glove", "left wristwrap/right wristwrap" };
        public List<string> Bodywear = new List<string>() { "shortsleeve shirt", "longsleeve shirt", "shortsleeve shirt", "longsleeve shirt", "uppershirt", "straps", "shortsleeve shirt/cape", "longsleeve shirt/cape", "straps/cape", };
        public List<string> Legwear = new List<string>() { "pants", "pants", "shorts", "kilt/pants", "kilt", "kilt/wraps" };
        public List<string> Footwear = new List<string>() { "none", "left boot/right boot", "left boot/right boot", "left boot/right boot", "left shoe/right shoe", "left shoe/right shoe" };

        public List<string> Colors = new List<string>
        {
            "maroon",
            "red",
            "orange",
            "yellow",
            "limegreen",
            "green",
            "lightblue",
            "cyan",
            "blue",
            "purple",
            "magenta",
            "coral",
            "white",
            "gray",
            "black",
            "brown"
        };

        public Dictionary<string, string> IndustryToProfession = new Dictionary<string, string>()
        {
            {"textiles", "weaver"},
            {"spices", "merchant"},
            {"metal", "blacksmith"},
            {"jewelry", "craftsman"},
            {"tools", "blacksmith"},
            {"military", "commander"},
            {"tea", "merchant"},
            {"coffee", "merchant"},
            {"wood", "carpenter"},
            {"ceramics", "potter"},
            {"glassmaking", "craftsman"},
            {"dye", "craftsman"},
            {"waspkeeping", "scholar"},
            {"fuel", "miner"},
            {"masonry", "mason"}
        };


        public List<string> LightingStyles = new List<string> { "none", "none", "none", "none", "none", "candles", "candles", "candles", "candles", "a lone torch in each room", "several braziers", "an oil lamp", "a candelabra", "an oil lantern", "a blazing fireplace" };
        public List<string> AllSpells = new List<string>() { "water bolt", "chaos flare", "concentrated ignition", "tremor", "immobile illusion", "shadow veil", "mobile illusion", "reactive illusion", "truthfulness", "rise", "hold", "forcethrow", "shatter", "clone", "intercept", "expel", "extract", "emergent growth", "animate", "immortalize", "raise", "resurrect" };
        public List<string> AllLegendarySpells = new List<string>() { "ethereal rupture", "emergence", "eternal bind", "expunge", "echo" };

        public List<string> PossibleMagicalItems = new List<string>() { "chalice", "scepter", "lantern", "bracelet", "left gauntlet", "staff", "amulet", "hourglass", "locket", "orb" };

        public List<string> WeightedRandomArchitectProfessions = new List<string>() { "commander", "craftsman", "craftsman", "craftsman", "mercenary", "mercenary", "mercenary", "musician", "musician", "elder", "prophet", "trader", "trader", "anarchist", "political figure", "scholar", "scholar", "scholar", "scholar" };
        public List<string> WeightedRandomNormalProfessions = new List<string>() { "soldier", "peasant", "peasant", "peasant", "blacksmith", "miller", "baker", "merchant", "brewer", "brewer", "tanner", "tailor", "carpenter", "mason", "scribe", "butcher", "fisherman", "weaver", "potter", "miner", "miner", "no profession", "no profession", "no profession", "no profession" };

        public List<string> ArchitectProfessions = new List<string>() { "commander", "craftsman", "mercenary", "musician", "elder", "prophet", "trader", "anarchist", "political figure", "scholar" };
        public List<string> Sexes = new List<string>() { "male", "female" };

        public List<string> DeathCauses = new List<string>() { " fell to their death ", " drowned ", " died of cancer ", " burned ", " misoperated dangerous equipment ", " died of sickness ", " starved to death ", " dehydrated ", " choked to death ", " was killed by a wild animal " };

        public List<string> Industries = new List<string>() { "textiles", "spices", "metal", "jewelry", "tools", "military", "tea", "coffee", "wood", "ceramics", "glassmaking", "dye", "waspkeeping", "fuel", "masonry" };

        public List<string> StructureTypes = new List<string>
        {
            "house",
            "shrine",
            "library",
            "tavern",
            "forge",
            "watchtower",
            "market",
            "bighouse"
        };

        public List<string> FirstNames = new List<string>();
        public List<string> LastNames = new List<string>();
        public List<string> Words = new List<string>();
        public List<string> Syllables = new List<string>();
        public List<string> NameSuffixes = new List<string>();
        public List<string> ClothItemTypes = new List<string>();
        public List<string> MetalItemTypes = new List<string>();
        public List<string> GlassItemTypes = new List<string>();
        public List<string> StoneWoodItemTypes = new List<string>();

        public List<string> MagicSchools = new List<string>() { "conjuration", "perception", "spatial", "fractal", "necromantic" };
        public List<string> CultureSchools = new List<string>() { "music", "artistry", "choreography", "theater", "literature" };
        public List<string> ScienceSchools = new List<string>() { "engineering", "mathematics", "biology", "chemistry", "physical" };

        public List<string> WrittenObjectTypes = new List<string>() { "scroll", "book", "scroll", "book", "scroll", "book", "waxtablet", "sheet" };

        public List<string> RPGBookNamePrefixes = new List<string>
        {
            "Chronicles of", "Explorations in", "Wonders of", "Musings on", "Revelations about", "Delights in", "Ramblings on", "Adventures in", "Discoveries in", "Secrets of", "Quests for", "Journeys in", "Studies on", "Inquiries on", "Observations of", "Mysteries in", "Legends of", "Myths and", "Echoes of", "Histories of", "Whispers from", "Enigmas of", "Chronicles from", "Odysseys through", "Sagas of", "Dreams of", "Enchantments in", "Fables of", "Chronicles of", "Wonders in", "Musings on", "Revelations about", "Delights in", "Ramblings on", "Adventures in", "Discoveries of", "Secrets from", "Quests for", "Journeys through", "Studies of", "Inquiries about", "Observations from", "Mysteries in", "Legends from", "Myths of", "Echoes from", "Histories in", "Whispers about", "Enigmas of", "Chronicles in"
        };
        public List<string> RPGBookNameSuffixes = new List<string>
        {
            ": A Grand Exploration", ": An Epic Journey", ": An Unusual Encounter", ": A Baffling Conundrum", ": A Curious Revelation", ": A Whimsical Quest", ": A Mysterious Adventure", ": An Enchanted Odyssey", ": A Secret Chronicle", ": A Mythical Encounter", ": A Puzzling Expedition", ": A Fascinating Discovery", ": A Remarkable Study", ": A Bewildering Investigation", ": A Legendary Chronicle", ": An Enigmatic Tale", ": A Magical Quest", ": A Mystical Journey", ": An Ancient Discovery", ": A Timeless Exploration", ": A Surprising Revelation", ": A Hidden Mystery", ": An Astonishing Saga", ": An Illuminating Narrative", ": An Uncharted Journey", ": A Legendary Exploration", ": A Mythical Adventure", ": A Remarkable Investigation", ": An Unfolding Mystery", ": A Mysterious Chronicle", ": A Whimsical Discovery", ": An Enchanted Quest", ": A Curious Journey", ": A Thrilling Expedition", ": An Epic Odyssey", ": A Wondrous Revelation", ": An Intriguing Inquiry", ": A Timeless Chronicle", ": A Bewildering Exploration", ": A Puzzling Discovery", ": A Journey of Legends", ": A Study in Wonder", ": An Enigma Unveiled", ": A Quest for Secrets", ": A Tale of Wonders", ": A Mythical Chronicle", ": An Enchanted Adventure", ": A Mysterious Revelation", ": An Odyssey Beyond", ": A Grand Adventure", " for the Curious Mind", " at Your Fingertips", " a Masterwork", " Made Simple", ": Secrets Revealed", ": In-Depth Insights", ": a Comprehensive Manual", " in a Nutshell", ": the Expert's Perspective", " Essentials", ", Demystified", ", The Complete Handbook", ": Mastering the Art", " Unveiled", " a Masterwork", " Made Simple", ": Secrets Revealed", ": In-Depth Insights", ": a Comprehensive Manual", " in a Nutshell", ": the Expert's Perspective", " Essentials", ", Demystified", ", The Complete Handbook", ": Mastering the Art", " Unveiled"
        };

        public Data()
        {
        }

        public void LoadContent(ContentManager Content)
        {
            string dataPath = string.Concat(Content.RootDirectory, "\\data\\");
            FirstNames = File.ReadAllLines(string.Concat(dataPath, "names.txt")).ToList();
            LastNames = File.ReadAllLines(string.Concat(dataPath, "last-names.txt")).ToList();
            Words = File.ReadAllLines(string.Concat(dataPath, "words.txt")).ToList();
            Syllables = File.ReadAllLines(string.Concat(dataPath, "syllables.txt")).ToList();
            NameSuffixes = File.ReadAllLines(string.Concat(dataPath, "namesuffixes.txt")).ToList();
        }
    }
}
