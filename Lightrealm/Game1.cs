using Lightrealm.Diagnostics;
using Lightrealm.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vosk;
using Color = Microsoft.Xna.Framework.Color;
using Model = Vosk.Model;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;


#if WINDOWS
using System.Drawing;
using System.Windows.Forms;
#endif


#pragma warning disable SYSLIB0011

namespace Lightrealm
{
    public class Game1 : Game
    {
        public static string Version = "alpha1";

        public static string DetermineAttackVerb(string weaponType)
        {

            var verbMappings = new Dictionary<string, List<string>>
                        {
                            {"slashing", new List<string> {"slash", "slice"}},
                            {"piercing", new List<string> {"pierce", "stab"}},
                            {"bashing", new List<string> {"bash", "crush"}},
                            {"scourging", new List<string> {"scourge", "lash", "strike"}}
                        };

            if (verbMappings.ContainsKey(weaponType))
            {
                var verbs = verbMappings[weaponType];
                return verbs[Game1.r.Next(verbs.Count)]; // Randomly select a verb
            }

            return "attack"; // Default verb if weapon type is not found
        }

        public static List<(Rectangle, Entity)> EntityHitboxes = new List<(Rectangle, Entity)>();

        public static int TemporaryNextUniqueID = 0;

        private static bool _isRecording = false;
        private static WaveInEvent _waveIn;
        private static VoskRecognizer _recognizer;
        public static int DeviceNumber;

        public static List<string> MenacingStructures = new List<string>() { "fortress", "monument", "shadecore", "photonexusoutpost", "spire", "stronghold", "tower", "shadegarrison", "photonexuscore", "photonexusgarrison"};

        public MouseState previousMouseState;
        public MouseState currentMouseState;

        public Model VoskModel;

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        public bool SpeechToText = false;

        public static List<string> ReqExploreLocations = new List<string>() { "fortress", "monument", "tower", "stronghold" };

        public static List<Entity> ThisList = new List<Entity>();

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        public static int MapCursorX { get; set; } = 0;
        public static int MapCursorZ { get; set; } = 0;

        public static bool ViewMessageForCustom = false;

        public static bool EnableTTS;
        public static bool SimplifiedFont;
        public static bool SpeedWorldGen;

        public int LatestTraveledDay { get; set; }

        public static List<Entity> CollectAllSubjects(Architect executor, string Modifier)
        {
            HashSet<Entity> subjects = new HashSet<Entity>(); // Using HashSet to avoid duplicates

            // Entities around
            foreach (Block b in executor.District.DistrictMap)
            {
                subjects.UnionWith(b.Structures);
                subjects.UnionWith(b.Objects);
                subjects.UnionWith(b.Architects);

                foreach (Structure s in b.Structures)
                {
                    foreach (Room r in s.Rooms)
                    {
                        subjects.UnionWith(r.Objects);
                        subjects.UnionWith(r.Architects);
                    }
                }
            }

            // Add the items in the inventories of people who actually matter
            List<Entity> subjectsToAdd = new List<Entity>();

            foreach (Entity e in subjects)
            {
                if (e is Architect architect) // Using pattern matching to cast 'e' to 'Architect'
                {
                    if(Modifier != "get")
                    {
                        subjectsToAdd.AddRange(architect.Inventory);
                        subjectsToAdd.AddRange(architect.Clothing);
                        subjectsToAdd.AddRange(architect.BodyParts);

                        if (architect.LeftHandObject != null)
                        {
                            subjectsToAdd.Add(architect.LeftHandObject);
                        }
                        if (architect.RightHandObject != null)
                        {
                            subjectsToAdd.Add(architect.RightHandObject);
                        }
                    }

                    subjectsToAdd.AddRange(architect.CultureBank);
                }
            }

            subjects.UnionWith(subjectsToAdd);

            // Entities not around
            foreach (Architect a in GameWorld.AllArchitects)
            {
                subjects.Add(a);
            }

            foreach (Location l in GameWorld.AllLocations)
            {
                if (subjects.Add(l))
                {
                    subjects.UnionWith(l.Districts);
                }
            }

            foreach (var skillValuePair in executor.XPValues)
            {
                subjects.Add(new Entity(skillValuePair.Item1.ToLower()));
            }

            // Spells known to the architect
            foreach (var spell in AllSpells)
            {
                subjects.Add(new Entity(spell.ToLower()));
            }
            foreach (var skill in AllSkills)
            {
                subjects.Add(new Entity(skill.ToLower()));
            }
            foreach (var spell in AllLegendarySpells)
            {
                subjects.Add(new Entity(spell.ToLower()));
            }

            // Add types of body parts
            foreach (Race r in GameWorld.Races)
            {
                foreach ((string part, Material material) in r.BodyParts)
                {
                    subjects.Add(new Entity(part));
                }
            }

            List<string> entitiesToAdd = new List<string>
            {
                "tavern", "prism", "well", "shrine", "library", "watchtower", "market",
                "north", "south", "east", "west", "up", "down", "southeast", "southwest",
                "northeast", "northwest", "shadow storage", "relationships", "mining",
                "combat", "crafting", "trading", "stealth", "alchemy", "cooking", "fishing",
                "hunting", "quests", "gathering", "imbuement", "healing", "navigation",
                "tactics", "survival", "diplomacy", "lockpicking", "animal taming", "herbalism",
                "herbs", "blacksmithing", "tailoring", "carpentry", "architecture",
                "history", "sailing", "farming", "brewing", "divination",
                "spellcasting", "negotiation", "investigation", "potions",
                "archery", "swordsmanship", "armor crafting", "thievery",
                "mountaineering", "cartography", "astronomy", "necromancy", "spatiomancy", "conjuromancy", "fractalmancy", "perceptomancy",
                "beasts", "divination", "divinity", "illusion", "mechanics", "engineering",
                "book", "poem", "song",
                "spells", "skills"
            };

            entitiesToAdd = entitiesToAdd.Except(Domains).ToList();
            subjects.UnionWith(entitiesToAdd.Select(entity => new Entity(entity)));
            subjects.UnionWith(Domains.Select(domain => new Entity(domain))); // Ensure all domains are also added as entities if not already covered
            subjects.UnionWith(GameWorld.Blights);
            subjects.Add(GameWorld.DarkDeity);
            subjects.Add(GameWorld.LightDeity);
            subjects.UnionWith(GameWorld.Groups);
            subjects.UnionWith(GameWorld.CoreMaterials());
            subjects.UnionWith(GameWorld.Races);
            subjects.Add(GameWorld);

            // Collect the objects hiding inside containers
            HashSet<Entity> allObjects = new HashSet<Entity>();

            // Method to recursively retrieve all objects
            void RetrieveAllObjects(Object obj)
            {
                if (allObjects.Add(obj))
                {
                    foreach (Entity containedEntity in obj.ContainedObjects)
                    {
                        if (containedEntity is Object containedObject)
                        {
                            RetrieveAllObjects(containedObject);
                        }
                    }
                }
            }

            foreach (Entity e in subjects)
            {
                if (e is Object obj)
                {
                    RetrieveAllObjects(obj);
                }
            }

            subjects.UnionWith(allObjects);

            return subjects.ToList();
        }

        
        public static List<Entity> AllSubjects = new List<Entity>();

        public static Dictionary<string, string> CalamityIdeologicalObsessionMapping = new Dictionary<string, string>()
        {
            {"disease", "specialists"},  // A person who studies or spreads diseases
            {"dominator", "soldiers"},   // A person who leads or controls the group
            {"purifier", "zealots"},    // A person obsessed with cleansing or purifying
            {"killer", "assassins"},    // A person responsible for eliminating threats or enemies
            {"kidnapper", "abductors"}, // A person who takes others by force
            {"corruptor", "saboteurs"}, // A person who corrupts or undermines from within
            {"diplomancer", "diplomancers"},  // A person who manages diplomacy and negotiations
            {"inciter", "agitators"},   // A person who stirs up unrest or rebellion
            {"power", "murderers"}        // A person with significant influence and power
        };

        public static int RickRollCycles = 0;

        public static int PreferredBackBufferWidth = 0;
        public static int PreferredBackBufferHeight = 0;

        public static int RegionXMod = 75;
        public static int RegionYMod = 75;

        public static int TravelRegionXMod = -100;
        public static int TravelRegionYMod = -100;

        public static bool TriedFakeMove = false;

        public static int TicksSinceLoad = 0;

        public static Dictionary<string, (List<string>, List<string>)> RecognizedCommands = new Dictionary<string, (List<string>, List<string>)>();
        public static Dictionary<string, (List<string>, List<string>)> RecognizedMessages = new Dictionary<string, (List<string>, List<string>)>();
        public static List<string> SuggestibleCommands = new List<string>();

        public static List<(string, List<string>)> Recipes = new List<(string, List<string>)>();

        public static int CurrentObjectPage = 0;
        public static int MaximumObjectPage = 0;
        public static int ItemsPerPage = 45;

        public static List<string> InspirationsAvailable = new List<string>() { "Learn a random offensive spell.", "Learn a random skill.", "Gain 2 random stat improvements.", "Gain +5 Max Energy.", "Obtain a minor magical item." };
        public static List<string> InspirationsChosen = new List<string>();
        public static string InspirationSelected = "";

        public static string CommandBuilderStage = "none"; //none, categories, commands, pickingsubjects, 
        public static int CurrentCommandBuilderPage = 0;
        public static int MaxCommandBuilderPage = 0;
        public static string SelectedCategory = "";

        public static int CurrentSubjectIndex = 0;
        public static string SelectedCommand = "";
        public static List<Entity> RelevantEntities = new List<Entity>();
        public static List<Entity> SelectedEntities = new List<Entity>();
        public static Dictionary<string, string[]> CommandIDToCategory = new Dictionary<string, string[]>();


        public static List<string> ThreatTypes = new List<string>() { "non-cataclysmic", "random", "dominator", "purifier", "disease", "killer", "kidnapper", "corruptor", "diplomancer", "inciter", "power" };

        public static int CurrentlySelectedWorldAge = 250; //100, 150, 200, 250 (recommended), 300, 350, 400, 450, 500, Until Stopped
        public static int CurrentlySelectedGrievanceType = 0;
        public static int NumberOfCivilizations = 16; //maximum is 16, minimum is 6. Subtract 4 before calculation.
        public static double ProsperityMultiplier = 1; //determines wealth increase, aka general flourishing
        public static int CurrentlySelectedWorldWidth = 128; //max 128
        public static int CurrentlySelectedWorldLength = 128; //max 128

        public static List<string> Domains = new List<string>
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

        public static Architect MostRecentPartyTurnArchitect = null;

        public static string GrievanceReason = "";

        public List<string> AllEnteredGameStates = new List<string>();

        public static Dictionary<string, Texture2D> CharacterAtlas = new Dictionary<string, Texture2D>();

        public record DrawnObject(Object Item, int Count, List<DrawnObject> NestedObjects);

        public static string ContentRoot = "";

        public string ConvertNumberToProficiency(int prof)
        {
            if (prof < 0)
            {
                return "Invalid proficiency level"; // Handle negative inputs
            }
            else if (prof <= 7)
            {
                switch (prof)
                {
                    case 0:
                        return "Unknowing";
                    case 1:
                        return "Novice";
                    case 2:
                        return "Competent";
                    case 3:
                        return "Skilled";
                    case 4:
                        return "Adept";
                    case 5:
                        return "Advanced";
                    case 6:
                        return "Expert";
                    case 7:
                        return "Proficient";
                    default:
                        // This default case is technically unnecessary because of the outer if conditions
                        // but included for the switch structure completeness
                        return "Invalid proficiency level";
                }
            }
            else // prof >= 8
            {
                int exponent = prof - 7; // Calculate how many levels above 8
                if (exponent == 1)
                {
                    return "Master";
                }
                else
                {
                    return $"Master^{exponent}";
                }
            }
        }

        // For watching/displaying FPS
        FrameCounter FrameCounter;
        GameInput GameInput;

        Texture2D myIconTexture;

        [DllImport("SDL2.dll")]
        private static extern void SDL_SetWindowIcon(IntPtr window, IntPtr icon);

        [DllImport("SDL2.dll")]
        private static extern IntPtr SDL_RWFromMem(IntPtr mem, int size);

        [DllImport("SDL2.dll")]
        private static extern IntPtr IMG_Load_RW(IntPtr src, int freesrc);

        [DllImport("SDL2.dll")]
        private static extern IntPtr SDL_GL_GetCurrentWindow();

        public bool Mute = false;

        public bool SeenTips = false;
        // Assuming you have a list of objects
        List<string> Tips = new List<string> {
            ""
            /*
            "You need to start moving in a direction before you can escape a block.", 
            "Use NUMPAD or ALT QWEADZXC to move without typing.",
            "Lose energy equal to bleeding per turn, then reduce by 1.",
            "Object imbuements require you to wear the item.",
            "Wearing non-clothing items will reduce your reaction chance.",
            "Object imbuements are dependent on rarity.",
            "Level up by defeating an enemy stronger than you.",
            "Type commands to do basically anything.",
            "Armor will protect you from attacks at certain body parts.",
            "Armor coverage is based on material and type.",
            "Do not anger the debtshibas.",
            "Take what you wish from a market, but leave greater value."
            */
        };

        // Initial setup
        string currentObject = "";
        int objectCycles = 0;
        int objectOpacity = 0;

        public int PlayerSpendableLevelsLastTick = 0;

        public static List<string> ItemPickupGuiLines = new List<string>();
        public static bool IsInGui = false;

        public static string FakeName = "";
        public static int NameOpacity = 0;
        public static int NameCycles = 0;

        public static List<Architect> LoadedArchitects = new List<Architect>();
        public static List<Architect> LoadedArchitectsToRemove = new List<Architect>();
        public static int ArchitectIndex = 0;

        public static void MakeObservation(string data, Color color, List<Entity> Entities)
        {
            string capitalizedData = Capitalize(data);
            Observations.Add(new TextStorage(capitalizedData, color, Entities));
            Announcements.Add(new TextStorage(capitalizedData, color, Entities));
        }

        public static void AddMessage(string data, Color color, List<Entity> Entities)
        {
            string capitalizedData = Capitalize(data);
            Messages.Add(new TextStorage(capitalizedData, color, Entities));
            Announcements.Add(new TextStorage(capitalizedData, color, Entities));
        }

        public static int TileSize = 0;
        public static int TileXDistance = 0;
        public static int TileZDistance = 0;

        public static bool InInventory = false;

        List<string> StatOptions = new List<string>();

        Dictionary<Keys, int> KeyInts = new Dictionary<Keys, int>()
        {
            { Keys.D1, 1 },
            { Keys.D2, 2 },
            { Keys.D3, 3 },
            { Keys.D4, 4 },
            { Keys.D5, 5 },
            { Keys.D6, 6 },
            { Keys.D7, 7 },
            { Keys.NumPad1, 1 },
            { Keys.NumPad2, 2 },
            { Keys.NumPad3, 3 },
            { Keys.NumPad4, 4 },
            { Keys.NumPad5, 5 },
            { Keys.NumPad6, 6 },
            { Keys.NumPad7, 7 }
        };

        public int StoredStr;
        public int StoredDex;
        public int StoredEnd;
        public int StoredCha;
        public int StoredFoc;
        public int StoredAgl;
        public int StoredCre;

        public static string Capitalize(string Word)
        {
            return Word[0].ToString().ToUpper() + Word.Substring(1);
        }

        int CurrentlyAssigningSkill = 7;

        public static List<string> AnimalSizes = new List<string>() { "miniscule", "smaller", "small", "medium", "humanoid", "large", "huge" };
        public static List<string> AllSizes = new List<string>() { "ethereal", "miniscule", "smaller", "small", "medium", "humanoid", "large", "huge", "colossal", "archancient" };

        public bool HasPlayerBeenAttacked(Architect architect)
        {
            foreach (var attack in StoredAttacks)
            {
                if (architect.BodyParts.Contains(attack.Target))
                {
                    return true;
                }
            }

            return false;
        }

        public static string ConvertListToString(List<string> items)
        {
            StringBuilder result = new StringBuilder();

            if (items.Count == 0)
            {
                return "empty list";
            }
            else if (items.Count == 1)
            {
                return items[0];
            }
            else
            {
                for (int i = 0; i < items.Count - 1; i++)
                {
                    result.Append(items[i]);
                    result.Append(", ");
                }

                result.Append("and ");
                result.Append(items[items.Count - 1]);

                return result.ToString();
            }
        }

        public static List<Attack> StoredAttacks = new List<Attack>();
        public static List<Message> StoredMessages = new List<Message>();

        public static InteractableEvent StoredEvent = null;

        public static Dictionary<string, Color> ColorConverter = new Dictionary<string, Color>();

        public static List<string> AllWeapons = new List<string>
        {
            "shortsword", "greatsword", "axe","battle axe", "greataxe", "knife",
            "rapier", "spear", "pike",
            "mace", "hammer", "shield",
            "whip", "flail", "chain"
        };

        public static Object GenerateRandomWeapon(Material material, string Rarity)
        {
            // Array of possible weapon types
            string[] weaponTypes = { "shortsword", "greatsword", "battle axe", "greataxe", "rapier", "spear", "pike", "mace", "hammer", "shield", "whip", "scourge" };

            // Randomly select a weapon type
            Random rand = new Random();
            string selectedWeapon = weaponTypes[rand.Next(weaponTypes.Length)];

            // Randomly select a metal Material from the world's Metals list

            // Create and return the new weapon Object
            Object o = new Object(null, selectedWeapon, new List<Material>() { material }, null);
            o.Rarity = Rarity;
            o.ApplyImbuements(0);
            return o;
        }

        public static int CalculateProximityScore(Entity entity, Architect player)
        {
            if (entity is Material)
            {
                return 10; //DO NOT use this
            }

            bool IsInPlayersHands(Entity entity, Architect player)
            {
                return player.LeftHandObject == entity || player.RightHandObject == entity;
            }

            bool IsInPlayersInventory(Entity entity, Architect player)
            {
                return player.Inventory.Contains(entity);
            }

            bool IsInPlayersClothing(Entity entity, Architect player)
            {
                return player.Clothing.Contains(entity);
            }

            bool IsInSameRoom(Entity entity, Room currentRoom)
            {
                if (currentRoom == null)
                {
                    return false;
                }

                if (entity is Architect architect)
                {
                    return architect.Room != null && architect.Room == currentRoom;
                }
                else if (entity is Object obj)
                {
                    return obj.Room != null && obj.Room == currentRoom;
                }
                return false;
            }

            bool IsInSameBlock(Entity entity, Block currentBlock)
            {
                if (currentBlock.Architects.Contains(entity) || currentBlock.Objects.Contains(entity))
                {
                    return true;
                }

                foreach (Structure structure in currentBlock.Structures)
                {
                    if (structure.Rooms.Any(room => room.Objects.Contains(entity) || room.Architects.Contains(entity)))
                    {
                        return true;
                    }
                }

                return false;
            }

            bool IsInSameDistrict(Entity entity, District currentDistrict)
            {
                foreach (Block block in currentDistrict.DistrictMap)
                {
                    if (IsInSameBlock(entity, block))
                    {
                        return true;
                    }
                }

                return false;
            }

            // Check if the entity is a body part and calculate score based on the creator's proximity
            if (entity is Object bodyPart && bodyPart.Creator is Architect creator)
            {
                // Recursively calculate the proximity score based on the creator of the body part
                return CalculateProximityScore(creator, player);
            }

            // Calculate proximity score for an object and its contained objects
            int CalculateObjectProximityScore(Object obj, Architect player)
            {
                int baseScore = 0;

                if (IsInPlayersHands(obj, player))
                {
                    baseScore = 0;
                }
                else if (IsInPlayersClothing(obj, player))
                {
                    baseScore = 1;
                }
                else if (IsInPlayersInventory(obj, player))
                {
                    baseScore = 2;
                }
                else if (IsInSameRoom(obj, player.Room))
                {
                    baseScore = 3;
                }
                else if (IsInSameBlock(obj, player.Block))
                {
                    baseScore = 4;
                }
                else if (IsInSameDistrict(obj, player.District))
                {
                    baseScore = 5;
                }
                else
                {
                    baseScore = 6; // Entity is the farthest away
                }

                // Check contained objects for one level of recursion
                int containedObjectScore = baseScore;
                foreach (var containedObject in obj.ContainedObjects)
                {
                    int newScore = baseScore + 3;
                    containedObjectScore = Math.Max(containedObjectScore, newScore);
                }

                return containedObjectScore;
            }

            if (entity is Object obj)
            {
                return CalculateObjectProximityScore(obj, player);
            }

            if (IsInPlayersHands(entity, player))
            {
                return 0;
            }
            if (IsInPlayersClothing(entity, player))
            {
                return 1;
            }
            if (IsInPlayersInventory(entity, player))
            {
                return 2;
            }
            if (IsInSameRoom(entity, player.Room))
            {
                return 3;
            }
            if (IsInSameBlock(entity, player.Block))
            {
                return 4;
            }
            if (IsInSameDistrict(entity, player.District))
            {
                return 5;
            }

            return 6; // Entity is the farthest away
        }

        public static List<string> GetCommandsForCategory(string category)
        {
            return CommandIDToCategory
                .Where(kvp => kvp.Value.Contains(category))
                .Select(kvp => kvp.Key)
                .ToList();
        }

        static List<Entity> FilterSubjectsForCommandPart(string commandPart, List<Entity> allSubjects, Architect MostRecentPartyTurnArchitect, out Dictionary<string, Entity> matchedSubjects)
        {
            List<(string referredName, Entity entity, int index)> matchedData = new List<(string, Entity, int)>();
            matchedSubjects = new Dictionary<string, Entity>();

            // Calculate proximity score for each entity and sort by score and referredName length
            List<(string referredName, Entity entity, int proximityScore)> allReferredNames = allSubjects
                .SelectMany(subject => subject.ReferredToNames.Concat(new[] { subject.Metadata, subject.Name })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => (name.ToLower(), subject, CalculateProximityScore(subject, MostRecentPartyTurnArchitect))))
                .OrderBy(t => t.Item3)  // First by lowest proximity score
                .ThenByDescending(t => t.Item1.Length)  // Then by longest referredName within each score group
                .ToList();

           string modifiedCommandPart = commandPart.ToLower();

            // Iterate through all referred names and use regex to match in the command part
            foreach (var (referredName, entity, _) in allReferredNames)
            {
                Regex regex = new Regex(@"\b" + Regex.Escape(referredName) + @"\b", RegexOptions.IgnoreCase);
                Match match = regex.Match(modifiedCommandPart);

                while (match.Success)
                {
                    // Collect matching data including position
                    matchedData.Add((referredName, entity, match.Index));
                    // Use a unique placeholder to prevent nested replacements affecting subsequent matches
                    modifiedCommandPart = modifiedCommandPart.Remove(match.Index, referredName.Length).Insert(match.Index, new string('~', referredName.Length));
                    // Continue searching from the end of the last match
                    match = regex.Match(modifiedCommandPart, match.Index + referredName.Length);
                }
            }

            // Sort by original position in the command part
            matchedData = matchedData.OrderBy(m => m.index).ToList();

            // Extract ordered entities and reconstruct the modified command with generic placeholders
            List<Entity> orderedSubjects = new List<Entity>();
            foreach (var data in matchedData)
            {
                orderedSubjects.Add(data.entity);
                matchedSubjects[data.referredName] = data.entity;
                modifiedCommandPart = Regex.Replace(modifiedCommandPart, Regex.Escape(new string('~', data.referredName.Length)), "~", RegexOptions.None, TimeSpan.FromMilliseconds(500));
            }

            return orderedSubjects;
        }


        public static void MessageWorldEdit(Architect Sender, Architect Reciever, string MessageID, List<Entity> Subjects, string Response)
        {
            var triggers = new Dictionary<string, List<string>>
            {
                { "ask_them_join", new List<string> { "I would be honored", "If it means I spend" } },
                { "ask_me_join", new List<string> { "Yes, Welcome to", "If it means you" } },
                { "challenge", new List<string> { "I accept your challenge", "I don’t think I can" } },
                { "surrender", new List<string> { "Stay put and do", "I surrender too", "Surrender to my looks?" } },
                { "demand_surrender", new List<string> { "I yield", "Okay! But only to you" } },
                { "demand_item", new List<string> { "Okay! I’ll", "I’ll drop it, but" } }
            };

            if (triggers.ContainsKey(MessageID))
            {
                int Month = ((int)Math.Round((decimal)(GameWorld.Cycle / 24192000)) % 12) + 1;
                int Year = (int)Math.Round((decimal)(GameWorld.Cycle / 290304000), MidpointRounding.ToZero);

                string Date = "(" + Month + "/" + Year + ")";

                if (triggers[MessageID].Any(trigger => Response.StartsWith(trigger, StringComparison.OrdinalIgnoreCase)))
                {
                    switch (MessageID)
                    {
                        case "ask_them_join":
                            if (GamePlayerParty.Architects.Contains(Sender) || GamePlayerParty.Architects.Contains(Reciever))
                            {
                                // Ensure the receiver joins the GamePlayerParty
                                if (Reciever.Group != null)
                                {
                                    Reciever.Group.Architects.Remove(Reciever);
                                }
                                if (!GamePlayerParty.Architects.Contains(Reciever))
                                {
                                    GamePlayerParty.Architects.Add(Reciever);

                                    Sender.Location.Region.World.HistoricalEvents.Add(Date + " " + Reciever.Name + " joined " + GamePlayerParty.Name + ".");
                                    Sender.Location.LocationHistoricalEvents.Add(Date + " " + Reciever.Name + " joined " + GamePlayerParty.Name + ".");
                                }
                                Reciever.Group = null;
                            }
                            else
                            {
                                // Regular group join logic
                                if (Sender.Group == null)
                                {
                                    Sender.Group = new Group(new List<Architect> { Sender, Reciever }, "adventurer", Sender, Sender.Location);
                                }
                                if (Reciever.Group != null)
                                {
                                    Reciever.Group.Architects.Remove(Reciever);
                                }
                                Reciever.Group = Sender.Group;

                                // Log the historical event for AI joining AI group
                                Sender.Location.Region.World.HistoricalEvents.Add(Date + " " + Reciever.Name + " joined the group of " + Sender.Name + ".");
                                Sender.Location.LocationHistoricalEvents.Add(Date + " " + Reciever.Name + " joined the group of " + Sender.Name + ".");
                            }
                            break;

                        case "ask_me_join":
                            if (GamePlayerParty.Architects.Contains(Sender) || GamePlayerParty.Architects.Contains(Reciever))
                            {
                                // Ensure the sender joins the GamePlayerParty
                                if (Sender.Group != null)
                                {
                                    Sender.Group.Architects.Remove(Sender);
                                }
                                if (!GamePlayerParty.Architects.Contains(Sender))
                                {
                                    GamePlayerParty.Architects.Add(Sender);

                                    Sender.Location.Region.World.HistoricalEvents.Add(Date + " " + Sender.Name + " joined " + GamePlayerParty.Name + ".");
                                    Sender.Location.LocationHistoricalEvents.Add(Date + " " + Sender.Name + " joined " + GamePlayerParty.Name + ".");
                                }
                                Sender.Group = null;
                            }
                            else
                            {
                                // Regular group join logic
                                if (Reciever.Group == null)
                                {
                                    Reciever.Group = new Group(new List<Architect> { Reciever, Sender }, "adventurer", Reciever, Reciever.Location);
                                }
                                if (Sender.Group != null)
                                {
                                    Sender.Group.Architects.Remove(Sender);
                                }
                                Sender.Group = Reciever.Group;

                                // Log the historical event for AI joining AI group
                                Sender.Location.Region.World.HistoricalEvents.Add(Date + " " + Sender.Name + " joined the group of " + Reciever.Name + ".");
                                Sender.Location.LocationHistoricalEvents.Add(Date + " " + Sender.Name + " joined the group of " + Reciever.Name + ".");
                            }
                            break;

                        case "challenge":
                            if (triggers["challenge"].Any(trigger => Response.StartsWith(trigger, StringComparison.OrdinalIgnoreCase)))
                            {
                                // Trigger: Hostility with a Surrender Point and an Honor Point
                                Sender.Task = "disabletarget";
                                Reciever.Task = "disabletarget";

                                Sender.TargetArchitect = Reciever;
                                Reciever.TargetArchitect = Sender;

                                Sender.CyclesLeftInTask = 300;
                                Reciever.CyclesLeftInTask = 300;

                                Sender.ShieldTokens.Add(Reciever);
                                Reciever.ShieldTokens.Add(Sender);

                                Sender.Location.Region.World.HistoricalEvents.Add(Date + " " + Sender.Name + " challenged " + Reciever.Name + " in " + Sender.Location.Name + ".");
                                Sender.Location.LocationHistoricalEvents.Add(Date + " " + Sender.Name + " challenged " + Reciever.Name + " in " + Sender.Location.Name + ".");
                            }
                            break;


                        case "surrender":
                            if (triggers["surrender"].Any(trigger => Response.StartsWith(trigger, StringComparison.OrdinalIgnoreCase)))
                            {
                                // Ensure the list is initialized
                                if (Reciever.ArchitectsWhoSurrenderedToMe == null)
                                {
                                    Reciever.ArchitectsWhoSurrenderedToMe = new List<Architect>();
                                }

                                // Add the sender to the receiver's list of those who surrendered to them
                                Reciever.ArchitectsWhoSurrenderedToMe.Add(Sender);

                                // Placeholder effect: Create a shield token
                                // CreateShieldToken(Sender, Reciever);
                            }
                            break;


                        case "demand_surrender":
                            if (triggers["demand_surrender"].Any(trigger => Response.StartsWith(trigger, StringComparison.OrdinalIgnoreCase)))
                            {
                                // Ensure the list is initialized
                                if (Sender.ArchitectsWhoSurrenderedToMe == null)
                                {
                                    Sender.ArchitectsWhoSurrenderedToMe = new List<Architect>();
                                }

                                // Add the receiver to the sender's list of those who surrendered to them
                                Sender.ArchitectsWhoSurrenderedToMe.Add(Reciever);

                                // Placeholder effect: Handle surrender
                                // HandleSurrender(Sender, Reciever);
                            }
                            break;


                        case "demand_item":
                            if (triggers["demand_item"].Any(trigger => Response.StartsWith(trigger, StringComparison.OrdinalIgnoreCase)))
                            {
                                var demandedItem = Subjects[0] as Object;

                                if (demandedItem != null)
                                {
                                    // Check and remove the item from inventory
                                    if (Reciever.Inventory.Contains(demandedItem))
                                    {
                                        Reciever.Inventory.Remove(demandedItem);
                                    }
                                    // Check and remove the item from clothing
                                    else if (Reciever.Clothing.Contains(demandedItem))
                                    {
                                        Reciever.Clothing.Remove(demandedItem);
                                    }
                                    // Check and remove the item from left hand
                                    else if (Reciever.LeftHandObject == demandedItem)
                                    {
                                        Reciever.LeftHandObject = null;
                                    }
                                    // Check and remove the item from right hand
                                    else if (Reciever.RightHandObject == demandedItem)
                                    {
                                        Reciever.RightHandObject = null;
                                    }
                                    else
                                    {
                                        // If the item is not found, break out
                                        break;
                                    }

                                    // Drop the item in the appropriate location
                                    if (Reciever.Room != null)
                                    {
                                        Reciever.Room.Objects.Add(demandedItem);
                                    }
                                    else
                                    {
                                        Reciever.Block.Objects.Add(demandedItem);
                                    }
                                }
                            }
                            break;



                        default:
                            // No action for other message IDs
                            break;
                    }
                }
            }
        }



        public static void CalculateAttack(string verb, Architect attacker, Object target, string DefenderAction, Object weapon)
        {
            int Month = ((int)Math.Round((decimal)(GameWorld.Cycle / 24192000)) % 12) + 1;
            int Year = (int)Math.Round((decimal)(GameWorld.Cycle / 290304000), MidpointRounding.ToZero);

            string Date = "(" + Month + "/" + Year + ")";

            attacker.CombatCycles = 100;

            //exposure calculation
            if (weapon != null)
            {
                if (attacker.BodyParts.Contains(weapon))
                {
                    weapon.UpdateExposure(25 - attacker.Dexterity);
                }
                else
                {
                    if (attacker.LeftHandObject == weapon)
                    {
                        var leftHand = attacker.FindBodyPart("left hand");
                        var leftArm = attacker.FindBodyPart("left arm");

                        if (leftHand != null)
                        {
                            leftHand.UpdateExposure(25 - attacker.Dexterity);
                        }
                        if (leftArm != null)
                        {
                            leftArm.UpdateExposure(25 - attacker.Dexterity);
                        }
                    }
                    else if (attacker.RightHandObject == weapon)
                    {
                        var rightHand = attacker.FindBodyPart("right hand");
                        var rightArm = attacker.FindBodyPart("right arm");

                        if (rightHand != null)
                        {
                            rightHand.UpdateExposure(25 - attacker.Dexterity);
                        }
                        if (rightArm != null)
                        {
                            rightArm.UpdateExposure(25 - attacker.Dexterity);
                        }
                    }
                }
            }



            int proficiencyModifier;

            if (weapon != null && weapon.IsWeapon)
            {
                switch (weapon.DamageType)
                {
                    case "slashing":
                        proficiencyModifier = attacker.GetProficiency("slashing");
                        attacker.ChangeXP("slashing", r.Next(1, 4));
                        break;
                    case "piercing":
                        proficiencyModifier = attacker.GetProficiency("piercing");
                        attacker.ChangeXP("piercing", r.Next(1, 4));
                        break;
                    case "bashing":
                        proficiencyModifier = attacker.GetProficiency("bashing");
                        attacker.ChangeXP("bashing", r.Next(1, 4));
                        break;
                    case "scourging":
                        proficiencyModifier = attacker.GetProficiency("scourging");
                        attacker.ChangeXP("scourging", r.Next(1, 4));
                        break;
                    default:
                        proficiencyModifier = attacker.GetProficiency("bashing");
                        break;
                }
            }
            else
            {
                proficiencyModifier = (int)Math.Round((double)(attacker.GetProficiency("bashing") / 2), 0, MidpointRounding.ToNegativeInfinity);
            }

            //lets see if the attack gets avoided

            bool Avoided = false;
            string AvoidFeedback = "";

            Architect TargetArchitect = null;

            if(target.Owner != null && target.IsBodyPart && target.Owner is Architect)
            {
                TargetArchitect = (Architect)(target.Owner);
                TargetArchitect.Focused = true;
                TargetArchitect.CombatCycles = 100;
            }

            // Define the new event
            string newEvent = Date + " " + attacker.Name + " attacked " + TargetArchitect.Name + ".";

            // Check if there are at least two events in the HistoricalEvents list
            if (GameWorld.HistoricalEvents.Count < 2 ||
                GameWorld.HistoricalEvents[GameWorld.HistoricalEvents.Count - 1] != newEvent ||
                GameWorld.HistoricalEvents[GameWorld.HistoricalEvents.Count - 2] != newEvent)
            {
                // Add the new event if the last two events are not the same
                GameWorld.HistoricalEvents.Add(newEvent);
                attacker.Location.LocationHistoricalEvents.Add(newEvent);
            }


            if (TargetArchitect != null && !(GamePlayerParty.Architects.Contains(TargetArchitect)))
            {
                TargetArchitect.ChangeOpinion(attacker, -75);
            }
            else
            {

            }

            bool IsPlayerPartyNearby(Architect attacker)
            {
                // Loop through each architect in the GamePlayerParty
                foreach (Architect partyMember in GamePlayerParty.Architects)
                {
                    // Check if they are in the same room or block as the attacker
                    if ((attacker.Room != null && attacker.Room == partyMember.Room) ||
                        (attacker.Block != null && attacker.Block == partyMember.Block))
                    {
                        return true;
                    }
                }
                return false;
            }

            //pls make sure one more time that the weapon is correct

            if (target != null)
            {
                //calculate blcokign paryign and such, make sure we set avoided to true if its blocked.
                Object DefenderWeapon = null;
                Object DefenderSidearm = null;

                if (TargetArchitect.MainHandObject() != null && TargetArchitect.MainHandObject().IsWeapon && TargetArchitect.OffHandObject() != null && TargetArchitect.OffHandObject().IsWeapon)
                {
                    DefenderWeapon = TargetArchitect.MainHandObject();
                    DefenderSidearm = TargetArchitect.OffHandObject();
                }
                if (TargetArchitect.MainHandObject() != null && TargetArchitect.MainHandObject().IsWeapon)
                {
                    DefenderWeapon = TargetArchitect.MainHandObject();
                }
                else if (TargetArchitect.OffHandObject() != null && TargetArchitect.OffHandObject().IsWeapon)
                {
                    DefenderWeapon = TargetArchitect.OffHandObject();
                }

                string primaryHand = TargetArchitect.RightHanded ? "right hand" : "left hand";
                string secondaryHand = TargetArchitect.RightHanded ? "left hand" : "right hand";

                DefenderWeapon = DefenderWeapon ?? TargetArchitect.FindBodyPart(primaryHand);
                if (DefenderWeapon == null)
                {
                    DefenderWeapon = TargetArchitect.BodyParts[r.Next(TargetArchitect.BodyParts.Count)];
                }

                DefenderSidearm = DefenderSidearm ?? TargetArchitect.FindBodyPart(secondaryHand);
                if (DefenderSidearm == null)
                {
                    DefenderSidearm = TargetArchitect.BodyParts[r.Next(TargetArchitect.BodyParts.Count)];
                }


                List<string> CantDuckBodyParts = new List<string>
                {
                    "left lower leg",
                    "right lower leg",
                    "left upper leg",
                    "right upper leg",
                    "left foot",
                    "right foot"
                };

                List<string> CantJumpBodyParts = new List<string>
                {
                    "head",
                    "neck",
                    "torso"
                };

                if (DefenderAction == "decideforme")
                {
                    List<string> PossibleActions = new List<string>()
                    {
                        "sustain",
                        "duck",
                        "jump",
                        "roll"
                    };

                    if ((DefenderWeapon != null && DefenderWeapon.Type == "shield") || (DefenderSidearm != null && DefenderSidearm.Type == "shield"))
                    {
                        PossibleActions.Add("block");
                    }
                    if (DefenderWeapon == null || DefenderWeapon.Type.EndsWith(" hand") || DefenderSidearm == null || DefenderSidearm.Type.EndsWith(" hand"))
                    {
                        PossibleActions.Add("disarm");
                    }
                    if ((DefenderWeapon != null && DefenderWeapon.IsWeapon) || (DefenderSidearm != null && DefenderSidearm.IsWeapon))
                    {
                        PossibleActions.Add("parry");
                    }

                    DefenderAction = PossibleActions[r.Next(PossibleActions.Count)];
                }


                var successChances = TargetArchitect.CalculateSuccessChances(new Attack(verb, attacker, target, weapon), GameWorld.ReactionModifierInt, attacker, proficiencyModifier);

                if (r.Next(0, 100) < TargetArchitect.ExtraStealth)
                {
                    Avoided = true;
                    AvoidFeedback = "The attack slices through a shadow of " + TargetArchitect.ReferredToNames[0] + "'s past self!";
                }
                else
                {
                    switch (DefenderAction)
                    {
                        case "sustain":
                            //nothing happens you just take it lmaoooooo
                            break;

                        case "parry":
                            //test their skill out of 8, multiply by 10, thats the percent chance of success

                            if (DefenderWeapon != null && DefenderWeapon.IsWeapon)
                            {
                                switch (DefenderWeapon.DamageType)
                                {
                                    case "slashing":
                                        proficiencyModifier = TargetArchitect.GetProficiency("slashing");
                                        break;
                                    case "piercing":
                                        proficiencyModifier = TargetArchitect.GetProficiency("piercing");
                                        break;
                                    case "bashing":
                                        proficiencyModifier = TargetArchitect.GetProficiency("bashing");
                                        break;
                                    case "scourging":
                                        proficiencyModifier = TargetArchitect.GetProficiency("scourging");
                                        break;
                                    default:
                                        proficiencyModifier = 0;
                                        break;
                                }

                                if (r.Next(0, 101) < successChances.parry)
                                {
                                    Avoided = true;
                                    AvoidFeedback = "The attack is parried by the " + DefenderWeapon.ReferredToNames[0] + "!";
                                    TargetArchitect.ChangeXP("parrying", r.Next(1, 4));
                                    foreach (Imbuement i in TargetArchitect.CurrentlyActiveImbuements)
                                    {
                                        if (i.IsTrigger && i.ConditionOrTrigger == "onparry")
                                        {
                                            TargetArchitect.ActivatePower(i.BuffOrResult);
                                        }
                                    }
                                }
                                else
                                {
                                    AvoidFeedback = "The attack is not parried by the " + DefenderWeapon.ReferredToNames[0] + "!";
                                }
                            }
                            else if (DefenderSidearm != null && DefenderSidearm.IsWeapon)
                            {
                                switch (DefenderSidearm.DamageType)
                                {
                                    case "slashing":
                                        proficiencyModifier = TargetArchitect.GetProficiency("slashing");
                                        break;
                                    case "piercing":
                                        proficiencyModifier = TargetArchitect.GetProficiency("piercing");
                                        break;
                                    case "bashing":
                                        proficiencyModifier = TargetArchitect.GetProficiency("bashing");
                                        break;
                                    case "scourging":
                                        proficiencyModifier = TargetArchitect.GetProficiency("scourging");
                                        break;
                                    default:
                                        proficiencyModifier = 0;
                                        break;
                                }

                                if (r.Next(0, 101) < successChances.block)
                                {
                                    Avoided = true;
                                    AvoidFeedback = "The attack is parried by the " + DefenderSidearm.ReferredToNames[0] + "!";

                                    foreach (Imbuement i in TargetArchitect.CurrentlyActiveImbuements)
                                    {
                                        if (i.IsTrigger && i.ConditionOrTrigger == "onparry")
                                        {
                                            TargetArchitect.ActivatePower(i.BuffOrResult);
                                        }
                                    }
                                }
                                else
                                {
                                    AvoidFeedback = "The attack is not parried by the " + DefenderSidearm.ReferredToNames[0] + "!";
                                }
                            }
                            else
                            {
                                AvoidFeedback = TargetArchitect.ReferredToNames[0] + " was unable to parry the attack with their arsenal!";
                            }
                            break;

                        case "block":
                            // Check if the defender has a shield or any other item that could be used for blocking
                            bool isShield = DefenderWeapon.Type == "shield" || DefenderSidearm.Type == "shield";
                            // Use successChances.block as the base chance if a shield is used
                            int baseChance = isShield ? successChances.block : (int)(successChances.block * 0.75); // Apply 25% reduction to base chance if not a shield

                            string blockingItem = isShield ? (DefenderWeapon.Type == "shield" ? DefenderWeapon.ReferredToNames[0] : DefenderSidearm.ReferredToNames[0]) : "item";

                            // Calculate the chance of blocking
                            int proficiencyBonus = TargetArchitect.GetProficiency("blocking") * 4;
                            int chanceOfSuccess = baseChance + proficiencyBonus;

                            // If not using a shield, apply a 25% reduction to the total chance of success
                            if (!isShield)
                            {
                                chanceOfSuccess = (int)(chanceOfSuccess * 0.75);
                            }

                            if (r.Next(0, 101) < chanceOfSuccess)
                            {
                                Avoided = true;
                                AvoidFeedback = "The attack was blocked by the " + blockingItem + "!";
                                TargetArchitect.ChangeXP("blocking", r.Next(1, 4));
                                foreach (Imbuement i in TargetArchitect.CurrentlyActiveImbuements)
                                {
                                    if (i.IsTrigger && i.ConditionOrTrigger == "onblock")
                                    {
                                        TargetArchitect.ActivatePower(i.BuffOrResult);
                                    }
                                }
                            }
                            else
                            {
                                AvoidFeedback = "The attack was not blocked by the " + blockingItem + "!";
                            }
                            break;


                        case "duck":
                            if (r.Next(0, 101) < successChances.duck && !CantDuckBodyParts.Contains(target.Type))
                            {
                                Avoided = true;
                                AvoidFeedback = TargetArchitect.ReferredToNames[0] + " ducked under the attack!";
                                TargetArchitect.ChangeXP("dodging", r.Next(1, 4));
                            }
                            else
                            {
                                AvoidFeedback = TargetArchitect.ReferredToNames[0] + " attempted to duck under the attack, but failed!";
                            }
                            break;

                        case "jump":
                            if (r.Next(0, 101) < successChances.jump && !CantJumpBodyParts.Contains(target.Type))
                            {
                                Avoided = true;
                                AvoidFeedback = TargetArchitect.ReferredToNames[0] + " jumped over the attack!";
                                TargetArchitect.ChangeXP("dodging", r.Next(1, 4));
                            }
                            else
                            {
                                AvoidFeedback = TargetArchitect.ReferredToNames[0] + " attempted to jump over the attack, but failed!";
                            }

                            TargetArchitect.CyclesSinceJump = 0;
                            break;

                        case "roll":
                            if (r.Next(0, 101) < successChances.roll)
                            {
                                Avoided = true;
                                AvoidFeedback = TargetArchitect.ReferredToNames[0] + " rolled away from the attack!";
                                TargetArchitect.ChangeXP("dodging", r.Next(1, 4));
                                foreach (Imbuement i in TargetArchitect.CurrentlyActiveImbuements)
                                {
                                    if (i.IsTrigger && i.ConditionOrTrigger == "ondodge")
                                    {
                                        TargetArchitect.ActivatePower(i.BuffOrResult);
                                    }
                                }
                            }
                            else
                            {
                                AvoidFeedback = TargetArchitect.ReferredToNames[0] + " attempted to roll away from the attack, but failed!";
                            }
                            break;

                        case "disarm":
                            if (r.Next(0, 101) < successChances.disarm && (attacker.RightHandObject == weapon || attacker.LeftHandObject == weapon))
                            {
                                AvoidFeedback = TargetArchitect.ReferredToNames[0] + ", while hit, successfully managed to disarm " + attacker.ReferredToNames[0] + "!";
                                TargetArchitect.ChangeXP("disarming", r.Next(1, 4));

                                if (TargetArchitect.Room != null)
                                {
                                    TargetArchitect.Room.Objects.Add(weapon);
                                }
                                else
                                {
                                    TargetArchitect.Block.Objects.Add(weapon);
                                }

                                if (attacker.RightHandObject == weapon)
                                {
                                    attacker.RightHandObject = null;
                                }
                                else
                                {
                                    attacker.LeftHandObject = null;
                                }
                            }
                            else
                            {
                                AvoidFeedback = TargetArchitect.ReferredToNames[0] + " attempted to disarm " + attacker.ReferredToNames[0] + ", but could not!";
                            }
                            break;

                        case "redirect":

                            //REDIRECT WILL BE REPROAGRAMNNED LATER, I DONT CARE ENOUGH RIGHT NOW

                            if (DefenderWeapon.IsWeapon)
                            {
                                switch (DefenderWeapon.DamageType)
                                {
                                    case "slashing":
                                        proficiencyModifier = TargetArchitect.GetProficiency("slashing");
                                        break;
                                    case "piercing":
                                        proficiencyModifier = TargetArchitect.GetProficiency("piercing");
                                        break;
                                    case "bashing":
                                        proficiencyModifier = TargetArchitect.GetProficiency("bashing");
                                        break;
                                    case "scourging":
                                        proficiencyModifier = TargetArchitect.GetProficiency("scourging");
                                        break;
                                    default:
                                        proficiencyModifier = 0;
                                        break;
                                }

                                if (r.Next(0, 101) < successChances.redirect)
                                {
                                    Avoided = true;
                                    AvoidFeedback = "The attack is redirected by the " + DefenderWeapon.ReferredToNames[0] + "!";
                                    TargetArchitect.ChangeXP("redirection", r.Next(1, 4));

                                    foreach (Imbuement i in TargetArchitect.CurrentlyActiveImbuements)
                                    {
                                        if (i.IsTrigger && i.ConditionOrTrigger == "redirect")
                                        {
                                            TargetArchitect.ActivatePower(i.BuffOrResult);
                                        }
                                    }
                                }
                                else
                                {
                                    AvoidFeedback = "The redirect of " + DefenderWeapon.ReferredToNames[0] + " failed!";
                                }
                            }
                            else if (DefenderSidearm.IsWeapon)
                            {
                                switch (DefenderSidearm.DamageType)
                                {
                                    case "slashing":
                                        proficiencyModifier = TargetArchitect.GetProficiency("slashing");
                                        break;
                                    case "piercing":
                                        proficiencyModifier = TargetArchitect.GetProficiency("piercing");
                                        break;
                                    case "bashing":
                                        proficiencyModifier = TargetArchitect.GetProficiency("bashing");
                                        break;
                                    case "scourging":
                                        proficiencyModifier = TargetArchitect.GetProficiency("scourging");
                                        break;
                                    default:
                                        proficiencyModifier = 0;
                                        break;
                                }

                                if (r.Next(0, 101) < 50 + (proficiencyModifier * 4))
                                {
                                    Avoided = true;
                                    AvoidFeedback = "The attack is parried by the " + DefenderSidearm.ReferredToNames[0] + "!";

                                    foreach (Imbuement i in TargetArchitect.CurrentlyActiveImbuements)
                                    {
                                        if (i.IsTrigger && i.ConditionOrTrigger == "redirect")
                                        {
                                            TargetArchitect.ActivatePower(i.BuffOrResult);
                                        }
                                    }
                                }
                                else
                                {
                                    AvoidFeedback = "The attack is not parried by the " + DefenderSidearm.ReferredToNames[0] + "!";
                                }
                            }
                            else
                            {
                                AvoidFeedback = TargetArchitect.ReferredToNames[0] + " was unable to parry the attack with their arsenal!";
                            }
                            break;

                        default:
                            break;
                    }
                }

                //falling star

                int StarCount = 0;

                if (attacker.PathOfStarsLevel >= 2)
                {
                    StarCount = 1;
                    if (IsPlayerPartyNearby(attacker))
                        Observations.Add(new TextStorage($"A star falls from the heavens!", Color.Goldenrod, new List<Entity>()));
                }
                else if (attacker.PathOfStarsLevel >= 6)
                {
                    StarCount = 3;
                    if (IsPlayerPartyNearby(attacker))
                        Observations.Add(new TextStorage($"Stars fall from the heavens!", Color.Goldenrod, new List<Entity>()));
                }

                for (int i = 0; i < StarCount; i++)
                {
                    List<Architect> FightingArchitects = new List<Architect>();
                    foreach (Architect a in attacker.Room.Architects)
                    {
                        if (a.Task == "fighting" && ((GamePlayerParty.Architects.Contains(attacker) && !GamePlayerParty.Architects.Contains(TargetArchitect)) || (!GamePlayerParty.Architects.Contains(attacker) && GamePlayerParty.Architects.Contains(TargetArchitect))))
                        {
                            FightingArchitects.Add(a);
                        }
                    }

                    Architect TargetArchitectForStar = null;

                    if (FightingArchitects.Count > 1)
                    {
                        if (FightingArchitects.Contains(TargetArchitect))
                        {
                            FightingArchitects.Remove(TargetArchitect);
                        }

                        TargetArchitectForStar = FightingArchitects[r.Next(FightingArchitects.Count)];
                    }
                    else if (FightingArchitects.Count == 1)
                    {
                        TargetArchitectForStar = FightingArchitects[0];
                    }

                    Object o = new Object(null, "falling star", new List<Material>() { GameWorld.Energy }, attacker);
                    o.AirborneTarget = TargetArchitectForStar;
                    TargetArchitectForStar.Room.Objects.Add(o);
                }
            }

            if (target != null)
            {
                if ((attacker.Room != null && attacker.Room.Architects.Contains(LoadedArchitects[ArchitectIndex])) ||
                   (attacker.Block != null && attacker.Block.Architects.Contains(LoadedArchitects[ArchitectIndex])))
                {
                    if (IsPlayerPartyNearby(attacker))
                    {
                        string simplifiedVerb;
                        if (verb.EndsWith("e"))
                        {
                            simplifiedVerb = verb + "s";
                        }
                        else
                        {
                            simplifiedVerb = verb + "es";
                        }

                        string message = $"{attacker.ReferredToNames[0]} {simplifiedVerb} {target.ReferredToNames[0]} with {weapon.ReferredToNames[0]}!";
                        Observations.Add(new TextStorage(message, Color.Blue, new List<Entity>() { attacker, weapon }));
                        Announcements.Add(new TextStorage(message, Color.Blue, new List<Entity>() { attacker, weapon }));
                    }

                }

                List<TextStorage> announcements = new List<TextStorage>()
                {
                    new TextStorage(AvoidFeedback, Color.HotPink, new List<Entity>())
                };

                if (!Avoided)
                {
                    CalculateAndApplyDamage(attacker, weapon, target, announcements);
                }

                if (IsPlayerPartyNearby(attacker))
                {
                    Announcements.AddRange(announcements);
                    Observations.AddRange(announcements);
                }
            }
            else
            {
                Observations.Add(new TextStorage($"The attack was unsuccessful", Color.Red, new List<Entity>()));
                Announcements.Add(new TextStorage($"The attack was unsuccessful", Color.Red, new List<Entity>()));
            }

            void CalculateAndApplyDamage(Architect attacker, Object weapon, Object targetObject, List<TextStorage> announcements)
            {
                int DivineMight = attacker.DivineMight > 0 ? 1 : 0;
                if (attacker.DivineMight > 0)
                {
                    attacker.DivineMight--;
                    announcements.Add(new TextStorage("The divine have intervened! The attack is empowered with brilliant energy!", Color.Aquamarine, new List<Entity>()));
                    if (attacker.DivineMight == 0)
                    {
                        announcements.Add(new TextStorage("The divine might has worn off!", Color.Aquamarine, new List<Entity>()));
                    }
                }

                if (TargetArchitect != null && TargetArchitect.DivineProtection > 0)
                {
                    TargetArchitect.DivineProtection--;
                    announcements.Add(new TextStorage($"The divine have intervened! The attack is avoided by {targetObject.Owner}'s divine protection. It wears thin, though...", Color.Aquamarine, new List<Entity>()));
                    if (TargetArchitect.DivineProtection == 0)
                    {
                        announcements.Add(new TextStorage("The divine protection has worn off!", Color.Aquamarine, new List<Entity>()));
                    }
                }
                else
                {
                    // Constants for initial damage calculation
                    double baseDamageMultiplier = 1.0;
                    double strengthMultiplier = 0.1 * attacker.Strength;
                    double proficiencyEffect = proficiencyModifier * (1 + attacker.ExtraAttackPower / 100);
                    int DamageModifier = (int)Math.Round((baseDamageMultiplier + strengthMultiplier) * proficiencyEffect + DivineMight);

                    // Check if the attacker's PathOfBodyLevel is 4 or higher
                    if (attacker.PathOfBodyLevel >= 4 && attacker.BodyParts.Contains(weapon))
                    {
                        DamageModifier += 2;  // Slightly increase the damage
                    }

                    if (attacker.DropKickReady && attacker.CyclesSinceJump <= 30 && (weapon.Type.EndsWith("foot") || weapon.Type.EndsWith("leg")))
                    {
                        announcements.Add(new TextStorage("The attack is a devastating dropkick!", Color.Red, new List<Entity>()));
                        DamageModifier *= 3;
                        attacker.DropKickReady = false;
                    }

                    if(attacker.SeveringStrikeReady && targetObject.Owner != null && targetObject.Owner is Architect)
                    {
                        announcements.Add(new TextStorage("The attack severs many critical veins!", Color.Red, new List<Entity>()));
                        ((Architect)(targetObject.Owner)).Bleeding += 6;
                        attacker.SeveringStrikeReady = false;
                    }

                    // Check for Radiant Energy channeling at level 6 or higher
                    if (attacker.PathOfBodyLevel >= 6)
                    {
                        int radiantIncrease = new Random().Next(10, 41);  // Random increase between 10 and 40
                        ((Architect)(targetObject.Owner)).RadiantCycles += radiantIncrease;  // Increase RadiantCycles of the body part's owner

                        // Announcement for Radiant Energy effect
                        announcements.Add(new TextStorage($"{attacker.ReferredToNames[0]} channels a radiant energy into their strike!", Color.Aquamarine, new List<Entity>() { attacker }));
                    }

                    // Apply Damage to Target
                    announcements.AddRange(
                        targetObject.TakeDamageFromObject(
                            weapon,
                            DamageModifier,
                            attacker,
                            verb
                        )
                    );

                    if (weapon.Type == "torso" && attacker.BodySlamReady)
                    {
                        attacker.BodySlamReady = false;
                        announcements.Add(new TextStorage("The attack is a destabilizing body slam!", Color.Red, new List<Entity>()));
                        ((Architect)(targetObject.Owner)).DestabilizedCycles += (int)Math.Round(attacker.Energy / 2);
                        ((Architect)(targetObject.Owner)).Energy -= Game1.r.Next(5,10);

                    }
                    else if ((weapon.Type.EndsWith("leg")||weapon.Type.EndsWith("foot")) && attacker.LegSweepReady)
                    {
                        attacker.LegSweepReady = false;
                        announcements.Add(new TextStorage("The attack is a swift leg sweep!", Color.Red, new List<Entity>()));
                        ((Architect)(targetObject.Owner)).DestabilizedCycles += (int)Math.Round(attacker.Energy / 2);
                        ((Architect)(targetObject.Owner)).ReactionBoostCycles += 30;
                    }
                }
            }

            if (attacker.QuickStrikeReady)
            {
                Observations.Add(new TextStorage(attacker.ReferredToNames[0] + " strikes faster than light!", Color.LightCoral, new List<Entity>() { attacker }));
                Announcements.Add(new TextStorage(attacker.ReferredToNames[0] + " strikes faster than light!", Color.LightCoral, new List<Entity>() { attacker }));
                attacker.QuickStrikeReady = false;
            }
            else
            {
                // Combat cooldown
                double baseCooldown = 20.0; // Base cooldown in 0.1-second cycles, assuming a default value
                double weaponWeightPenalty = weapon.Weight / 1000; // Example: penalty increases for every kg of weapon weight
                double strengthModifier = Math.Max(0, 1 - (0.1 * (attacker.Strength - 1))); // Reduces penalty based on strength, ensures it's not negative

                // Calculate the final cooldown adjustment from weapon weight, mitigated by strength
                double finalCooldownAdjustment = baseCooldown + (weaponWeightPenalty * strengthModifier);

                // Apply speed factor directly to adjust the cooldown, allowing for increase or decrease
                double finalCooldown = finalCooldownAdjustment * attacker.Speed();

                // Update the attacker's cooldown cycles, ensuring it doesn't fall below a minimum threshold
                attacker.CooldownCycles = (int)Math.Round(Math.Max(1, finalCooldown));
            }
        }

        public static Dictionary<string, string> SkillSpellDescriptions = new Dictionary<string, string>()
                        {
                            { "deflect", "Reverse the thrower and target of the nearest projectile that targets you. (Initiate with \"initiate deflect\")" },
                            { "dropkick", "Use within 3 seconds after jumping. The first foot attack within those 3 seconds has triple attack power. (Initiate with \"initiate dropkick\")" },
                            { "double strike", "The next attack you make is made twice. (Initiate with \"initiate double strike\")" },
                            { "quick strike", "The next attack you make is instantaneous. (Initiate with \"initiate quick strike\")" },
                            { "severing strike", "The next attack you make inflicts +6 Bleeding. (Initiate with \"initiate severing strike\")" },
                            { "backflip", "Increase reaction success chances by 30% for 6 seconds. (Initiate with \"initiate backflip\")" },
                            { "escape", "Immediately travel through a random adjacent door. (Initiate with \"initiate escape\")" },
                            { "finale", "If your next attack is fatal, severely damage all nearby hostiles. (Initiate with \"initiate finale\")" },
                            { "concentration", "Increase focus by 1 for 30 seconds. (Initiate with \"initiate concentration\")" },
                            { "body slam", "Your next torso bash attack does extra damage and destabilizes proportional to your energy. (Initiate with \"initiate body slam\")" },
                            { "leg sweep", "Your next leg or foot attack destabilizes a target and gives you 30% more reaction success chance for 3 seconds. (Initiate with \"initiate leg sweep\")" },
                            { "water bolt", "Fire a bolt of water at the target, damaging and extinguishing them. (Cast with \"cast water bolt at ~\")" },
                            { "chaos flare", "Fire a bolt of light and darkness at the target, causing destabilization, burning, and damage. (Cast with \"cast chaos flare at ~\")" },
                            { "ice shock", "Expose a target to an unrelenting swirl of frost. (Cast with \"cast ice shock at ~\")" },
                            { "concentrated ignition", "Ignites the target. (Cast with \"cast concentrated ignition at ~\")" },
                            { "tremor", "Destabilizes all architects and objects in the area except for the TARGET. (Cast with \"cast tremor at ~\")" },
                            { "truthfulness", "Permanently forces someone to always tell the truth to the caster. (Cast with \"cast truthfulness at ~\")" },
                            { "rise", "Send the target into the air. (Cast with \"cast rise at ~\")" },
                            { "hold", "Cause an architect to freeze in place, or an airborne object to fall out of the sky. (Cast with \"cast hold at ~\")" },
                            { "force throw", "Throw all subsequent targets of this spell at the first target. (Maximum Spell Targets is 5, Cast with \"cast force throw at ~ and ~ and...\")" },
                            { "shatter", "Destabilize an architect significantly, or break an object into millions of pieces. (Cast with \"cast shatter at ~\")" },
                            { "intercept", "Quickly fractallize an airborne projectile. (Cast with \"cast intercept at ~\")" },
                            { "expel", "Banish an object or a weakened creature to the fractal plane. (Cast with \"cast expel at ~\")" },
                            { "extract", "Return a creature or object from the fractal plane, exactly where it left. (Cast with \"cast extract at ~\")" },
                            { "raise", "Bring a creature back from the dead and grant them immortality. (Cast with \"cast raise at ~\")" },
                            { "resurrect", "Bring a creature back from the dead and grant them immortality. Restore all their body parts to full integrity. (Cast with \"cast resurrect at ~\")" },
                            { "animate", "Raise a corpse as a shade. It joins your party. (Cast with \"cast animate at ~\")" }
                        };


        public static List<string> Headwear = new List<string>() { "none", "none", "none", "none", "none", "none", "none", "small hat", "hood", "hood", "hood" };
        public static List<string> Neckwear = new List<string>() { "none", "none", "none", "amulet", "amulet/amulet/amulet", "flair" };
        public static List<string> Handwear = new List<string>() { "none", "none", "none", "left glove/right glove", "left wristwrap/right wristwrap" };
        public static List<string> Bodywear = new List<string>() { "shortsleeve shirt", "longsleeve shirt", "shortsleeve shirt", "shortsleeve shirt/uppershirt", "longsleeve shirt/uppershirt", "longsleeve shirt", "uppershirt", "straps", "shortsleeve shirt", "longsleeve shirt", "shortsleeve shirt", "longsleeve shirt", "uppershirt", "straps", "shortsleeve shirt/cape", "longsleeve shirt/cape", "straps/cape", };
        public static List<string> Legwear = new List<string>() { "pants", "pants", "shorts", "kilt/pants", "kilt", "kilt/wraps" };
        public static List<string> Footwear = new List<string>() { "none", "left boot/right boot", "left boot/right boot", "left boot/right boot", "left shoe/right shoe", "left shoe/right shoe" };

        public static Dictionary<string, double> EnergySizeMultipliers = new Dictionary<string, double>
        {
            { "ethereal", 0.1 },
            { "miniscule", 0.2 },
            { "smaller", 0.5 },
            { "small", 0.75 },
            { "medium", 0.9 },
            { "average", 1.0 },
            { "large", 1.25 },
            { "huge", 1.5 },
            { "colossal", 2.0 },
            { "archancient", 3.0 }
        };

        public List<(int, int, Color, int, int, int)> CivilizationParticles = new List<(int, int, Color, int, int, int)>();

        public static List<string> Colors = new List<string>
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

        public static List<string> GetFamilyColors(string inputColor)
        {
            int index = Colors.IndexOf(inputColor);

            if (index != -1)
            {
                // Ensure not to go out of bounds
                int prevIndex = (index - 1 + Colors.Count) % Colors.Count;
                int nextIndex = (index + 1) % Colors.Count;

                string prevColor = Colors[prevIndex];
                string nextColor = Colors[nextIndex];

                return new List<string> { prevColor, inputColor, nextColor };
            }
            else
            {
                // Color not found in the list
                throw new ArgumentException("Color not found in CivilizationColors list", nameof(inputColor));
            }
        }

        public int CountPlayersInDistrict(District D)
        {
            int Players = 0;

            for (int x = 0; x < 7; x++)
            {
                for (int z = 0; z < 7; z++)
                {
                    foreach (Architect a in D.DistrictMap[x + z * 7].Architects)
                    {
                        Players++;
                    }
                }
            }

            return (Players);

        }
        public string ContentPath;

        public string HighlightingVerb = "";

        public static void SaveGame(Party player, World world)
        {
            // Create the LightrealmSaves folder if it doesn't exist
            string saveFolder = Path.Combine(DocumentsFolderPath, "LightrealmSaves", world.Name);
            Directory.CreateDirectory(saveFolder);

            // Save the world
            string worldPath = Path.Combine(saveFolder, $"{world.Name}.json");
            SerializeObjectToBinaryFile(worldPath, world);

            // Save the player
            string playerPath = Path.Combine(saveFolder, $"{player.Name}.json");
            SerializeObjectToBinaryFile(playerPath, player);

            List<string> versionData = new List<string>() { Version, "Modification of the above line may lead to world corruption." };
            File.WriteAllLines(Path.Combine(saveFolder, $"version.txt"), versionData);

            // Create a list to hold the header and the historical events
            List<string> fileContent = new List<string>
                            {
                                "Historical Events of " + world.Name,
                                "To find events related to a certain subject, search for it with CTRL+F",
                                "" // Adding an empty line for better readability
                            };

            // Add the historical events to the list
            fileContent.AddRange(world.HistoricalEvents);

            // Write the content to the file
            File.WriteAllLines(Path.Combine(saveFolder, $"history.txt"), fileContent.ToArray());
        }

        public static void LoadGame(string loadingDirectory)
        {
            // Assuming loadingDirectory is the path to the directory containing both files
            string[] files = Directory.GetFiles(loadingDirectory);

            // Find the world file (assumes there's only one file with the same name as the directory)
            string worldFilePath = files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == Path.GetFileName(loadingDirectory));

            if (worldFilePath == null)
            {
                // Handle the case where the world file is not found
                throw new Exception("Invalid Directory? Did you tamper with your save files :(");
            }

            // Find the player file (assumes there are only two files in the directory)
            string playerFilePath = files.First(f => f != worldFilePath);

            // Load the world
            World loadedWorld = DeserializeObjectFromBinaryFile<World>(worldFilePath);

            // Load the player
            Party loadedPlayer = DeserializeObjectFromBinaryFile<Party>(playerFilePath);

            GamePlayerParty = loadedPlayer;
            GameWorld = loadedWorld;

            //temporary bandaid on this interesting problem

            foreach (Architect a in GamePlayerParty.Architects)
            {
                if (!LoadedArchitects.Contains(a))
                {
                    LoadedArchitects.Add(a);
                }
            }
        }

        public static World GameWorld;
        public static Party GamePlayerParty;
        public static Civilization GamePlayerCivilization;

        public static string DocumentsFolderPath
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); }
        }
        

        private static void SerializeObjectToBinaryFile(string filePath, object obj)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(fileStream, obj);
            }
        }



        private static T DeserializeObjectFromBinaryFile<T>(string filePath)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return (T)binaryFormatter.Deserialize(fileStream);
            }
        }
        List<Keys> ValidNumpadKeys = new List<Keys>
{
    Keys.NumPad8, // North
    Keys.NumPad9, // Northeast
    Keys.NumPad6, // East
    Keys.NumPad3, // Southeast
    Keys.NumPad2, // South
    Keys.NumPad1, // Southwest
    Keys.NumPad4, // West
    Keys.NumPad7, // Northw
        };

        Dictionary<Keys, (int dx, int dz)> directionOffsets = new Dictionary<Keys, (int dx, int dz)>
{
    // Existing NumPad directions
    { Keys.NumPad8, (0, -1) },  // North
    { Keys.NumPad9, (1, -1) },  // Northeast
    { Keys.NumPad6, (1, 0) },   // East
    { Keys.NumPad3, (1, 1) },   // Southeast
    { Keys.NumPad2, (0, 1) },   // South
    { Keys.NumPad1, (-1, 1) },  // Southwest
    { Keys.NumPad4, (-1, 0) },  // West
    { Keys.NumPad7, (-1, -1) }, // Northwest
    // New ALT + QWEADZXC directions
    { Keys.Q, (-1, -1) }, // Northwest
    { Keys.W, (0, -1) },  // North
    { Keys.E, (1, -1) },  // Northeast
    { Keys.A, (-1, 0) },  // West
    { Keys.D, (1, 0) },   // East
    { Keys.Z, (-1, 1) },  // Southwest
    { Keys.X, (0, 1) },   // South
    { Keys.C, (1, 1) }    // Southeast
};

        Dictionary<Keys, string> KeyDirections = new Dictionary<Keys, string>
{
    // Existing NumPad directions
    { Keys.NumPad8, "north" },
    { Keys.NumPad9, "northeast" },
    { Keys.NumPad6, "east" },
    { Keys.NumPad3, "southeast" },
    { Keys.NumPad2, "south" },
    { Keys.NumPad1, "southwest" },
    { Keys.NumPad4, "west" },
    { Keys.NumPad7, "northwest" },
    // New ALT + QWEADZXC directions
    { Keys.Q, "northwest" },
    { Keys.W, "north" },
    { Keys.E, "northeast" },
    { Keys.A, "west" },
    { Keys.D, "east" },
    { Keys.Z, "southwest" },
    { Keys.X, "south" },
    { Keys.C, "southeast" }
};

        public static List<TextStorage> Exposition = new List<TextStorage>();

        public static Dictionary<string, string> InvertDoorDirection = new Dictionary<string, string>();

        int TotalCivTries = 0;

        bool AlreadyTriedASearch = false;

        public int FlashTick = 0;

        public string SelectedDirectory = "";

        public bool HasTriedToMoveManually = false;

        public static List<TextStorage> Observations = new List<TextStorage>();
        public static List<TextStorage> Messages = new List<TextStorage>();
        public static List<TextStorage> Announcements = new List<TextStorage>();

        public KeyboardState previousState;

        public static Dictionary<string, string> IndustryToProfession = new Dictionary<string, string>()
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
            {"healing", "brewer"},
            {"masonry", "mason"}
        };


        public static List<string> LightingStyles = new List<string> { "nothing", "nothing", "nothing", "nothing", "nothing", "candles", "candles", "candles", "candles", "a lone torch in each room", "several braziers", "an oil lamp", "a candelabra", "an oil lantern", "a blazing fireplace" };
        public static List<string> AllSpells = new List<string>() { "water bolt", "chaos flare", "concentrated ignition", "tremor", "ice shock", /*"immobile illusion", "shadow veil", "mobile illusion", "reactive illusion",*/ "truthfulness", "rise", "hold", "force throw", "shatter", "clone", "intercept", "expel", "extract", "emergent growth", "animate", "immortalize", "raise", "resurrect" };
        public static List<string> AllSkills = new List<string> { "deflect", "dropkick", "double strike", "quick strike", "severing strike", "backflip", "escape", "finale", "concentration", "body slam", "leg sweep" };
        public static List<string> AllLegendarySpells = new List<string>() { "ethereal rupture", "emergence", "eternal bind", "expunge", "echo" };

        public static List<string> PossibleMagicalItems = new List<string>() { "chalice", "scepter", "lantern", "bracelet", "left gauntlet", "staff", "amulet", "hourglass", "locket", "orb" };

        public Dictionary<string, Texture2D> TileAtlas = new Dictionary<string, Texture2D>();
        public static List<string> WeightedRandomArchitectProfessions = new List<string>() { "commander", "craftsman", "craftsman", "craftsman", "mercenary", "mercenary", "mercenary", "musician", "musician", "elder", "prophet", "trader", "trader", "anarchist", "political figure", "scholar", "scholar", "scholar", "scholar" };
        public static List<string> WeightedRandomNormalProfessions = new List<string>() { "soldier", "peasant", "peasant", "peasant", "blacksmith", "miller", "baker", "merchant", "brewer", "brewer", "tanner", "tailor", "carpenter", "mason", "scribe", "butcher", "fisherman", "weaver", "potter", "miner", "miner", "indolent", "indolent", "indolent", "indolent" };

        public static List<string> ArchitectProfessions = new List<string>() { "commander", "craftsman", "mercenary", "musician", "elder", "prophet", "trader", "anarchist", "political figure", "scholar" };
        public static List<string> Sexes = new List<string>() { "male", "female" };

        public static List<string> DeathCauses = new List<string>() { " fell to their death ", " drowned ", " died of cancer ", " burned ", " misoperated dangerous equipment ", " died of sickness ", " starved to death ", " dehydrated ", " choked to death ", " was killed by a wild animal ", " bled to death " };

        public static List<string> Industries = new List<string>() { "textiles", "spices", "metal", "jewelry", "tools", "military", "tea", "coffee", "wood", "ceramics", "glassmaking", "dye", "waspkeeping", "fuel", "masonry", "healing" };

        public static List<string> StructureTypes = new List<string>
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

        public static List<string> FirstNames = new List<string>();
        public static List<string> LastNames = new List<string>();
        public static List<string> Words = new List<string>();
        public static List<string> Syllables = new List<string>();
        public static List<string> NameSuffixes = new List<string>();
        public static List<string> ClothItemTypes = new List<string>();
        public static List<string> MetalItemTypes = new List<string>();
        public static List<string> GlassItemTypes = new List<string>();
        public static List<string> StoneWoodItemTypes = new List<string>();

        public static List<string> MagicSchools = new List<string>() { "conjuration", "perception", "spatial", "fractal", "necromantic" };
        public static List<string> CultureSchools = new List<string>() { "music", "artistry", "choreography", "theater", "literature" };
        public static List<string> ScienceSchools = new List<string>() { "engineering", "mathematics", "biology", "chemistry", "physical" };

        public static List<string> WrittenObjectTypes = new List<string>() { "scroll", "book", "scroll", "book", "scroll", "book", "waxtablet", "sheet" };

        public List<Keys> KeysNewlyPressed = new List<Keys>();

        public static bool SplitMode = false;

        public List<Material> CoreMaterials = new List<Material>();

        public int FindTicks = 0;

        public static PointLocation DeterminePointLocation(int arrayWidth, int arrayLength, int pointX, int pointY)
        {
            // Calculate the width and height of each section
            int sectionWidth = arrayWidth / 3;
            int sectionHeight = arrayLength / 3;

            // Calculate the section in which the point lies
            int sectionX = pointX / sectionWidth;
            int sectionY = pointY / sectionHeight;

            // Map the section coordinates to a PointLocation enum
            switch (sectionX)
            {
                case 0:
                    switch (sectionY)
                    {
                        case 0: return PointLocation.Northwest;
                        case 1: return PointLocation.West;
                        case 2: return PointLocation.Southwest;
                    }
                    break;
                case 1:
                    switch (sectionY)
                    {
                        case 0: return PointLocation.North;
                        case 1: return PointLocation.Center;
                        case 2: return PointLocation.South;
                    }
                    break;
                case 2:
                    switch (sectionY)
                    {
                        case 0: return PointLocation.Northeast;
                        case 1: return PointLocation.East;
                        case 2: return PointLocation.Southeast;
                    }
                    break;
            }

            // Default case, should not be reached if the input is valid
            return PointLocation.Center;
        }

        Texture2D whiteRect;
        Texture2D GUI;
        Texture2D HelpGUI;
        Texture2D InventoryGUI;
        Texture2D ThisListT;

        Texture2D SkillPullUpT;
        Texture2D SpellPullUpT;
        Texture2D BodyPartPullUpT;
        public static bool IsShowingSkills = false;
        public static bool IsShowingSpells = false;
        public static bool IsShowingBodyParts = false;


        public Dictionary<Keys, string> KeyAtlas = new Dictionary<Keys, string>();
        public Dictionary<Keys, string> UpperKeyAtlas = new Dictionary<Keys, string>();

        public static Architect TheChosenOne;
        public static Group TheChosenGroup;

        public static Architect StoredPortrait;

        public static int CurrentlySelectingArchitectProfession;
        public static int CurrentlySelectingSex;
        public static int CurrentlySelectingRace = 1;

        public SpriteFont Shibafont;
        public SpriteFont BabyShibafont;

        public static Dictionary<string, string> ConvertArchitectToGroupType = new Dictionary<string, string>();
        public static Dictionary<string, string> ConvertProfessionToBuilding = new Dictionary<string, string>();
        public static Dictionary<string, string> ConvertTaskToArchitectPrefix = new Dictionary<string, string>();
        public static Dictionary<string, string> ConvertProfessionToCareerDescription = new Dictionary<string, string>();

        public static Random r = new Random();

        public Song LightrealmMainTheme;

        public string ObservationsAndMessages = "both";

        public Texture2D DesertT;
        public Texture2D ForestT;
        public Texture2D LightforestT;
        public Texture2D MountainT;
        public Texture2D OceanT;
        public Texture2D PlainsT;
        public Texture2D SnowpeakT;
        public Texture2D TaigaT;
        public Texture2D TundraT;
        public Texture2D VoidT;
        public Texture2D OutlineT;
        public Texture2D EtherealT;
        public Texture2D EmptyTileT;

        public Texture2D nightfellCampT;
        public Texture2D nightfellVillageT;
        public Texture2D nightfellTownT;
        public Texture2D nightfellCityT;
        public Texture2D LuminarchCampT;
        public Texture2D LuminarchVillageT;
        public Texture2D LuminarchTownT;
        public Texture2D LuminarchCityT;
        public Texture2D LostCampT;
        public Texture2D LostVillageT;
        public Texture2D LostTownT;
        public Texture2D LostCityT;
        public Texture2D PortT;

        public Texture2D PhotonexusOutpostT;
        public Texture2D PhotonexusCoreT;
        public Texture2D IsofractalOutpostT;
        public Texture2D IsofractalCoreT;
        public Texture2D ShadeOutpostT;
        public Texture2D ShadeCoreT;



        public Texture2D SpireT;
        public Texture2D SanctumT;
        public Texture2D OutpostT;

        public Texture2D KeepT;
        public Texture2D TowerT;
        public Texture2D FortressT;
        public Texture2D MonumentT;
        public Texture2D StrongholdT;

        public Texture2D PyramidT;
        public Texture2D ToroidT;
        public Texture2D TowersT;
        public Texture2D HallwayT;
        public Texture2D ArchwayT;

        public Texture2D DistrictBuildingT;
        public Texture2D DistrictEmptyDesertT;
        public Texture2D DistrictEmptyPlainsT;
        public Texture2D DistrictEmptySnowT;
        public Texture2D DistrictEmptyTreesT;
        public Texture2D DistrictEmptyOceanT;
        public Texture2D DistrictManyBuildingsT;
        public Texture2D DistrictSpecialAndBuildingsT;
        public Texture2D DistrictSpecialBuildingT;
        public Texture2D DistrictSpireT;
        public Texture2D DistrictSanctumT;
        public Texture2D DistrictWellT;
        public Texture2D DistrictShadowStorageT;
        public Texture2D DistrictMarketT;
        public Texture2D DistrictMarketSurroundedT;
        public Texture2D DistrictPrismT;

        public Texture2D DistrictArchwayT;
        public Texture2D DistrictCommuneT;
        public Texture2D DistrictDockT;
        public Texture2D DistrictShipT;
        public Texture2D DistrictFortressT;
        public Texture2D DistrictHallwayT;
        public Texture2D DistrictMoundT;
        public Texture2D DistrictCoreT;
        public Texture2D DistrictScaffoldT;
        public Texture2D DistrictKeepT;
        public Texture2D DistrictMonasteryT;
        public Texture2D DistrictMonumentT;
        public Texture2D DistrictOutpostT;
        public Texture2D DistrictPyramidT;
        public Texture2D DistrictHeartT;
        public Texture2D DistrictScumT;
        public Texture2D DistrictStrongholdT;
        public Texture2D DistrictToroidT;
        public Texture2D DistrictTowerT;
        public Texture2D DistrictTowersT;


        public Texture2D AmuletT;
        public Texture2D ArchaixFemaleT;
        public Texture2D ArchaixMaleT;
        public Texture2D CapeT;
        public Texture2D ChestplateT;
        public Texture2D FlairT;
        public Texture2D HelmetT;
        public Texture2D HoodT;
        public Texture2D KiltT;
        public Texture2D LargeHatT;
        public Texture2D LeftBootT;
        public Texture2D LeftGauntletT;
        public Texture2D LeftGloveT;
        public Texture2D LeftShoeT;
        public Texture2D LeftWristwrapT;
        public Texture2D LeggingsT;
        public Texture2D LongsleeveShirtFemaleT;
        public Texture2D LongsleeveShirtMaleT;
        public Texture2D LuminarchFemaleT;
        public Texture2D LuminarchMaleT;
        public Texture2D NightfellFemaleT;
        public Texture2D NightfellMaleT;
        public Texture2D PantsT;
        public Texture2D RightBootT;
        public Texture2D RightGauntletT;
        public Texture2D RightGloveT;
        public Texture2D RightShoeT;
        public Texture2D RightWristwrapT;
        public Texture2D RobeFemaleT;
        public Texture2D RobeMaleT;
        public Texture2D ShortsT;
        public Texture2D ShortsleeveShirtFemaleT;
        public Texture2D ShortsleeveShirtMaleT;
        public Texture2D SkirtT;
        public Texture2D SmallHatT;
        public Texture2D StrapsT;
        public Texture2D UndergarmentT;
        public Texture2D UpperGarmentT;
        public Texture2D UpperShirtFemaleT;
        public Texture2D UpperShirtMaleT;
        public Texture2D WrapsT;

        public Texture2D CoveT;
        public Texture2D CommuneT;
        public Texture2D HoardT;
        public Texture2D PreserveT;
        public Texture2D MonasteryT;

        public Texture2D BaseRepositionGUIT;
        public Texture2D BodyFrameT;
        public Texture2D HeadRepT;
        public Texture2D LeftArmRepT;
        public Texture2D RightArmRepT;
        public Texture2D LeftFootRepT;
        public Texture2D LeftHandRepT;
        public Texture2D RightHandRepT;
        public Texture2D RightFootLeftT;
        public Texture2D RightLegT;
        public Texture2D LeftLegT;
        public Texture2D TorsoT;

        public Texture2D MirrorT;

        public Texture2D Astrionalis;
        public Texture2D Celestrioris;

        public Texture2D CursorT;
        public Texture2D Gradient;
        public Texture2D TitleScreen;
        public Texture2D GuideT;
        public Texture2D ReactionGUIT;
        public Texture2D MessageGUIT;
        public Texture2D CmdHelpT;
        public Texture2D BleedT;

        public Texture2D FrameT;
        public Texture2D SpeakingT;
        public Texture2D QuitGuiT;

        public Texture2D ArchitectHere;
        public Texture2D HealthGuiT;

        int WaitingTicks = 0;
        int EscapeTicks = 0;
        int SaveTicks = 0;
        int LoadTicks = 0;
        int LoadGameCursor = 0;

        public static string GameState = "mainscreen";
        public static string GameMode = "unknown";

        public static string CraftingPhase = "selectrecipe"; //this is the phase you are in. it can also be "selectingredients"
        public static int RecipeIndex = 0; //this is the currently selected recipe in the list. pressing enter switches your crafting phase.
        public List<Object> ObjectsInInventoryUsableForResources = new List<Object>(); //these objects are the ones that can be added to your recipe
        public static int InventoryCraftingIndex = 0; //this is the currently selected item in the crafting objects list. pressing enter adds it to indexes for resources.
        public List<int> IndexesForResources = new List<int>(); //the indexes of the objects you selected.


        public static string FormatList(List<string> items)
        {
            int count = items.Count;
            if (count == 0)
            {
                return "";
            }
            else if (count == 1)
            {
                return items[0];
            }
            else if (count == 2)
            {
                return $"{items[0]} and {items[1]}";
            }
            else
            {
                string lastItem = items[count - 1];
                string otherItems = string.Join(", ", items.GetRange(0, count - 1));
                return $"{otherItems}, and {lastItem}";
            }
        }

        public static string FormatMaterialList(List<Material> materials)
        {
            return string.Join(" ", materials.Select(m => m.Name));
        }


        public static string RestOfListIncludingThisIndex(List<string> list, int index)
        {
            if (index < 0 || index >= list.Count)
            {
                return string.Empty; // Return an empty string for invalid indices
            }

            // Create a sublist starting from the given index
            List<string> sublist = list.GetRange(index, list.Count - index);

            // Join the sublist elements with spaces
            string result = string.Join(" ", sublist);

            return result;
        }

        public static List<string> RPGBookNamePrefixes = new List<string>
        {
            "Chronicles of", "Explorations in", "Wonders of", "Musings on", "Revelations about", "Delights in", "Ramblings on", "Adventures in", "Discoveries in", "Secrets of", "Quests for", "Journeys in", "Studies on", "Inquiries on", "Observations of", "Mysteries in", "Legends of", "Myths and", "Echoes of", "Histories of", "Whispers from", "Enigmas of", "Chronicles from", "Odysseys through", "Sagas of", "Dreams of", "Enchantments in", "Fables of", "Chronicles of", "Wonders in", "Musings on", "Revelations about", "Delights in", "Ramblings on", "Adventures in", "Discoveries of", "Secrets from", "Quests for", "Journeys through", "Studies of", "Inquiries about", "Observations from", "Mysteries in", "Legends from", "Myths of", "Echoes from", "Histories in", "Whispers about", "Enigmas of", "Chronicles in"
        };
        public static List<string> RPGBookNameSuffixes = new List<string>
        {
            ": A Grand Exploration", ": An Epic Journey", ": An Unusual Encounter", ": A Baffling Conundrum", ": A Curious Revelation", ": A Whimsical Quest", ": A Mysterious Adventure", ": An Enchanted Odyssey", ": A Secret Chronicle", ": A Mythical Encounter", ": A Puzzling Expedition", ": A Fascinating Discovery", ": A Remarkable Study", ": A Bewildering Investigation", ": A Legendary Chronicle", ": An Enigmatic Tale", ": A Magical Quest", ": A Mystical Journey", ": An Ancient Discovery", ": A Timeless Exploration", ": A Surprising Revelation", ": A Hidden Mystery", ": An Astonishing Saga", ": An Illuminating Narrative", ": An Uncharted Journey", ": A Legendary Exploration", ": A Mythical Adventure", ": A Remarkable Investigation", ": An Unfolding Mystery", ": A Mysterious Chronicle", ": A Whimsical Discovery", ": An Enchanted Quest", ": A Curious Journey", ": A Thrilling Expedition", ": An Epic Odyssey", ": A Wondrous Revelation", ": An Intriguing Inquiry", ": A Timeless Chronicle", ": A Bewildering Exploration", ": A Puzzling Discovery", ": A Journey of Legends", ": A Study in Wonder", ": An Enigma Unveiled", ": A Quest for Secrets", ": A Tale of Wonders", ": A Mythical Chronicle", ": An Enchanted Adventure", ": A Mysterious Revelation", ": An Odyssey Beyond", ": A Grand Adventure", " for the Curious Mind", " at Your Handtips", " a Masterwork", " Made Simple", ": Secrets Revealed", ": In-Depth Insights", ": a Comprehensive Manual", " in a Nutshell", ": the Expert's Perspective", " Essentials", ", Demystified", ", The Complete Handbook", ": Mastering the Art", " Unveiled", " a Masterwork", " Made Simple", ": Secrets Revealed", ": In-Depth Insights", ": a Comprehensive Manual", " in a Nutshell", ": the Expert's Perspective", " Essentials", ", Demystified", ", The Complete Handbook", ": Mastering the Art", " Unveiled"
        };
        public static string GenerateBookName(string SubjectOrSpell)
        {
            if (r.Next(1, 3) == 1)
            {
                return (char.ToUpper(SubjectOrSpell[0]) + SubjectOrSpell.Substring(1) + RPGBookNameSuffixes[r.Next(RPGBookNameSuffixes.Count)]);
            }
            else
            {
                return (RPGBookNamePrefixes[r.Next(RPGBookNamePrefixes.Count)] + " " + char.ToUpper(SubjectOrSpell[0]) + SubjectOrSpell.Substring(1));
            }
        }

        public static string GetCommandIdFromInput(string input)
        {
            input = input.ToLower(); // Normalize the input
            foreach (var command in RecognizedCommands)
            {
                if (command.Value.Item1.Contains(input)) // Check against the first list in the tuple
                {
                    return command.Key;
                }
            }
            return null; // Return null if no command matches
        }


        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            AllocConsole();
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true }); 
            
        }

        public static string ConvertArchitectToDescription(Architect a)
        {
            string TaskDescription = "";

            if (!a.IsAlive)
            {
                TaskDescription = "dead";
            }
            else if (a.Task == "")
            {
                TaskDescription = ("idle");
            }
            else if (a.Target.Item1 != a.Location.Region || a.Target.Item2 != a.Location || a.Target.Item3 != a.District || a.Target.Item4 != a.Block || a.Target.Item5 != a.Structure)
            {
                if (a.CurrentlyMovingPlace == "north")
                {
                    TaskDescription = ("north-moving");
                }
                else if (a.CurrentlyMovingPlace == "east")
                {
                    TaskDescription = ("east-moving");
                }
                else if (a.CurrentlyMovingPlace == "south")
                {
                    TaskDescription = ("south-moving");
                }
                else if (a.CurrentlyMovingPlace == "west")
                {
                    TaskDescription = ("west-moving");
                }
                else if (a.CurrentlyMovingPlace == "outside")
                {
                    TaskDescription = ("structure-leaving");
                }
                else
                {
                    TaskDescription = ("deliberating");
                    //though it is also possible that below a structure is selected
                }

                foreach (Structure s in a.Block.Structures)
                {
                    if (s.GUID == a.CurrentlyMovingPlace)
                    {
                        TaskDescription = (s.Name + "-entering");
                    }
                }
                if (TaskDescription == "")
                {
                    TaskDescription = ("area-leaving");
                }
            }
            else if (a.CyclesLeftInTask > 0)
            {
                TaskDescription = (ConvertTaskToArchitectPrefix[a.Task]);
            }
            else
            {
                TaskDescription = "";
            }


            if (TaskDescription != "")
            {
                if (a.ReferredToNames.Count > 0)
                {
                    return (a.ReferredToNames[0] + ", " + TaskDescription);
                }
                else
                {
                    return ("");
                }
            }
            else
            {
                return ("");
            }
        }

        protected override void Initialize()
        {// TODO: Add your initialization logic here
            Window.IsBorderless = true;

            PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            _graphics.PreferredBackBufferWidth = PreferredBackBufferWidth;
            _graphics.PreferredBackBufferHeight = PreferredBackBufferHeight;

            _graphics.ApplyChanges();

            //determine the best way to draw the map

            //WE ONLY SUPPORT 16x9 for now, sorry nerds

            //i was going to change this by resolution but im keepign it static for now
            TileSize = 12;
            TileXDistance = 13;
            TileZDistance = 10;

            //create save directory if not already

            if (Directory.Exists(DocumentsFolderPath + "/LightrealmSaves"))
            {
                Directory.CreateDirectory(DocumentsFolderPath + "/LightrealmSaves");
            }

            static void EnsureSettingsFile(string path, ref bool enableTTS, ref bool simplifiedFont, ref bool speedWorldGen, GraphicsDeviceManager graphics)
            {
                // Create directory if it does not exist
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Check the default adapter width and height using _graphics.PreferredBackBufferWidth and _graphics.PreferredBackBufferHeight
                int screenWidth = graphics.PreferredBackBufferWidth;
                int screenHeight = graphics.PreferredBackBufferHeight;

                // Determine default value for simplifiedFont based on screen resolution
                bool defaultSimplifiedFont = screenWidth < 2560 || screenHeight < 1440;

                // Create settings file with default values if it does not exist
                if (!File.Exists(path))
                {
                    File.WriteAllLines(path, new[] { "T:1", $"S:{(defaultSimplifiedFont ? 1 : 0)}", "W:1" });
                }

                // Read settings from file
                string[] settingsLines = File.ReadAllLines(path);
                foreach (string line in settingsLines)
                {
                    string[] setting = line.Split(':');
                    if (setting.Length != 2) continue;

                    switch (setting[0])
                    {
                        case "T":
                            enableTTS = setting[1] == "1";
                            break;
                        case "S":
                            simplifiedFont = setting[1] == "1";
                            break;
                        case "W":
                            speedWorldGen = setting[1] == "1";
                            break;
                    }
                }
            }


            bool Break = false;

            static void SaveSettings(string path, bool enableTTS, bool simplifiedFont, bool speedWorldGen)
            {
                string[] settingsLines =
                {
                    $"T:{(enableTTS ? 1 : 0)}",
                    $"S:{(simplifiedFont ? 1 : 0)}",
                    $"W:{(speedWorldGen ? 1 : 0)}"
                };

                File.WriteAllLines(path, settingsLines);
            }

            string settingsPath = Path.Combine(DocumentsFolderPath, "LightrealmSaves", "settings.txt");

            // Ensure the settings file exists and load settings
            EnableTTS = false;
            SimplifiedFont = false;
            SpeedWorldGen = false;
            EnsureSettingsFile(settingsPath, ref EnableTTS, ref SimplifiedFont, ref SpeedWorldGen, _graphics);

            while (!Break)
            {
                Console.WriteLine("  _     _       _     _                  _           ");
                Console.WriteLine(" | |   (_) __ _| |__ | |_ _ __ ___  __ _| |_ __ ___  ");
                Console.WriteLine(" | |   | |/ _` | '_ \\| __| '__/ _ \\/ _` | | '_ ` _ \\ ");
                Console.WriteLine(" | |___| | (_| | | | | |_| | |  __/ (_| | | | | | | |");
                Console.WriteLine(" |_____|_|\\__, |_| |_|\\__|_|  \\___|\\__,_|_|_| |_| |_|");
                Console.WriteLine("          |___/");
                Console.WriteLine("                                        Launcher");
                Console.WriteLine("\n\n\n");

                Console.WriteLine("Current Settings:");
                Console.WriteLine($"Speech To Text (Press T): {(EnableTTS ? "Enabled (Recommended, will take some time to load)" : "Disabled (Recommended only if you can type like Usain Bolt)")}");
                Console.WriteLine($"Simplified Font (Press F): {(SimplifiedFont ? "Enabled (DePixel)" : "Disabled (Black Chancery)")}");
                Console.WriteLine($"Fast World Generation (Press S): {(SpeedWorldGen ? "Enabled (Recommended, disable if your game crashes during world-gen)" : "Disabled")}\n");
                Console.WriteLine($"Press Space to begin.");

                string key = Console.ReadKey().KeyChar.ToString().ToUpper();

                if (key == "T")
                {
                    EnableTTS = !EnableTTS;
                }
                else if (key == "F")
                {
                    SimplifiedFont = !SimplifiedFont;
                }
                else if (key == "S")
                {
                    SpeedWorldGen = !SpeedWorldGen;
                }
                else if (key == " ")
                {
                    Break = true;
                }

                Console.Clear();
            }

            // Save settings to file
            SaveSettings(settingsPath, EnableTTS, SimplifiedFont, SpeedWorldGen);


            async Task InitializeSpeechToTextAsync()
            {
                SpeechToText = true;
                Console.WriteLine("\nInitializing Databases... One Moment...");
                Console.WriteLine("Press ENTER if this takes longer than 20 seconds.");

                await Task.Run(() =>
                {
                    // Initialize Vosk
                    Vosk.Vosk.SetLogLevel(0);
                    string modelPath = "C:\\Users\\maxpi\\OneDrive\\Desktop\\STT Test Mozilla Thing\\STT Test Mozilla Thing\\vosk-model-en-us-0.22\\";
                    VoskModel = new Model(modelPath);
                    _recognizer = new VoskRecognizer(VoskModel, 16000.0f);
                    _recognizer.SetMaxAlternatives(0);
                    _recognizer.SetWords(true);
                });

                // List available audio devices
                Console.WriteLine("Available Input Devices:");
                for (int i = 0; i < WaveInEvent.DeviceCount; i++)
                {
                    var deviceInfo = WaveInEvent.GetCapabilities(i);
                    Console.WriteLine($"{i}: {deviceInfo.ProductName}");
                }

                // Select audio device
                Console.WriteLine("Please enter the number of the input device you want to use:");
                int deviceNumber;
                while (!int.TryParse(Console.ReadLine(), out deviceNumber) || deviceNumber < 0 || deviceNumber >= WaveInEvent.DeviceCount)
                {
                    Console.WriteLine("Invalid input. Please enter a valid device number:");
                }

                DeviceNumber = deviceNumber;
            }

            if (EnableTTS)
            {
                InitializeSpeechToTextAsync().GetAwaiter().GetResult();
            }
            else
            {
                Console.WriteLine("\nInitializing Databases...");
            }

            RecognizedCommands.Add("ask_name", (new List<string> { "ask ~ /p name", "ask ~ for /p name", "ask ~ name" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_directions", (new List<string> { "ask ~ where ~ is", "ask ~ where i can find ~", "ask ~ where to find ~" }, new List<string> { "nearby_architect", "entity" }));
            RecognizedCommands.Add("ask_generic_directions", (new List<string> { "ask ~ where a ~ is", "ask ~ where i can find a ~", "ask ~ where to find a ~", "ask ~ where the nearest ~ is", "ask ~ where i could find a ~", "ask ~ where an ~ is", "ask ~ where i can find an ~", "ask ~ where to find an ~" }, new List<string> { "nearby_architect", "entity" }));
            RecognizedCommands.Add("ask_about_something", (new List<string> { "ask ~ about ~", "ask ~ for information on ~", "ask ~ what they know about ~", "ask ~ what they can tell me about ~" }, new List<string> { "nearby_architect", "entity" }));
            RecognizedCommands.Add("ask_ruler", (new List<string> { "ask ~ about the government", "ask ~ who rules", "ask ~ who the government is", "ask ~ who rules here" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_trade", (new List<string> { "ask ~ to trade", "ask ~ trade", "ask ~ to trade with me", "ask ~ what they have for sale", "ask ~ what they sell", "ask ~ what they are selling" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_them_join", (new List<string> { "ask ~ to join me", "ask ~ to join us", "ask ~ to join me on my quest", "ask ~ to join my group" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_me_join", (new List<string> { "ask ~ if i can join /p", "ask ~ if /p would let me join", "ask ~ if they are accepting members" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_current_structure", (new List<string> { "ask ~ about this building", "ask ~ about this structure" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_location", (new List<string> { "ask ~ about this location", "ask ~ about this site", "ask ~ about this area" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_profession", (new List<string> { "ask ~ what they do", "ask ~ what they do for a living", "ask ~ about /p job", "ask ~ /p job" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("greet", (new List<string> { "say hello to ~", "greet ~", "say hi to ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("farewell", (new List<string> { "say goodbye to ~", "dismiss ~", "say bye to ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("thank", (new List<string> { "thank ~", "say thank you to ~", "express my gratitude to ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("apologize", (new List<string> { "apologize to ~", "tell ~ i'm sorry", "say sorry to ~", "tell ~ i apologize", "tell ~ i am sorry" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_health", (new List<string> { "ask ~ how they are feeling", "ask ~ their health", "ask ~ how they feel" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_news", (new List<string> { "ask ~ what happened recently", "ask ~ the latest", " ask ~ about recent events" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_story", (new List<string> { "ask ~ /p story", "ask ~ about /p", "ask ~ about /p history" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_history", (new List<string> { "ask ~ about the history of ~", "ask ~ what happened to ~", "ask ~ about the story of ~" }, new List<string> { "nearby_architect", "entity" }));
            RecognizedCommands.Add("ask_opinion", (new List<string> { "ask ~ /p opinion on ~", "ask ~ what /p think of ~", "ask ~ /p thoughts on ~", "ask ~ about /p relationship with ~", "ask ~ if /p know ~" }, new List<string> { "nearby_architect", "entity" }));
            RecognizedCommands.Add("ask_interests", (new List<string> { "ask ~ what interests /p", "ask ~ about /p interests", "ask ~ about /p hobbies", "ask ~ what hobbies /p have" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_family", (new List<string> { "ask ~ about /p family", "ask ~ if /p has family", "ask ~ if /p has relatives", "ask ~ about /p relatives" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("challenge", (new List<string> { "challenge ~", "challenge ~ to a fight", "challenge ~ to a duel" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("provide_assistance", (new List<string> { "ask ~ if they need help", "ask ~ if i can help" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_advice", (new List<string> { "ask ~ for advice on ~", "ask ~ advice on ~" }, new List<string> { "nearby_architect", "entity" }));
            RecognizedCommands.Add("inform_quest", (new List<string> { "tell ~ about my quest", "tell ~ about my goal", "tell ~ about my mission", "tell ~ my goal", "tell ~ my mission", "tell ~ my quest" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("tell_story_about", (new List<string> { "tell ~ about ~", "tell ~ the story of ~", "tell ~ a story about ~" }, new List<string> { "nearby_architect", "entity" }));
            RecognizedCommands.Add("compliment", (new List<string> { "compliment ~", "say something nice about ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("insult", (new List<string> { "insult ~", "defame ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("surrender", (new List<string> { "surrender to ~", "yield to ~", "give up to ~", "concede to ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("demand_surrender", (new List<string> { "demand ~ surrender", "demand ~ to surrender", "request ~ surrender", "ask ~ to surrender" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("demand_item", (new List<string> { "demand from ~ ~ ", "demand from ~ a ~" }, new List<string> { "nearby_architect", "inventory" }));

            // Add all the above ones to RecognizedMessages

            foreach (var kvp in RecognizedCommands)
            {
                RecognizedMessages[kvp.Key] = kvp.Value;
            }

            RecognizedCommands.Add("go_prone", (new List<string> { "go prone", "fall ~", "go on the ground", "fall over" }, new List<string> { "none" }));
            RecognizedCommands.Add("stand_up", (new List<string> { "stand ~", "get ~ off the ground", "get off the ground" }, new List<string> { "none" }));
            RecognizedCommands.Add("leave_structure", (new List<string> { "leave ~", "exit ~", "leave the structure", "exit the structure", "leave", "leave the building", "exit the building", "exit" }, new List<string> { "nearby_structure" }));
            RecognizedCommands.Add("enter", (new List<string> { "enter ~", "go inside ~", "go in ~", "go through ~" }, new List<string> { "enterable" }));
            RecognizedCommands.Add("inventory_check", (new List<string> { "check my inventory", "open my inventory", "open my pack", "open my backpack", "search my backpack", "open pack", "open menu", "show menu", "open inventory", "access menu", "display menu", "main menu", "game menu", "menu", "menu open", "open game menu", "show main menu", "access main menu", "menu screen" }, new List<string> { "none" }));
            RecognizedCommands.Add("move_direction", (new List<string> { "go ~", "travel ~", "move to the ~", "move ~", "go to the ~", "head ~", "head to the ~", "make my way ~", "start heading ~" }, new List<string> { "direction" }));
            RecognizedCommands.Add("basic_attack", (new List<string> {
    "slash ~", "stab ~", "pierce ~", "lash ~", "scourge ~", "whip ~",
    "strike ~", "bash ~", "crush ~", "whack ~", "smash ~", "hack ~",
    "impale ~", "cut ~", "slice ~", "bludgeon ~", "club ~",
    "punch ~", "kick ~", "headbutt ~", "shove ~", "slap ~", "jab ~",
    "slash at ~", "stab at ~", "pierce at ~", "lash at ~", "scourge at ~",
    "whip at ~", "strike at ~", "bash at ~", "crush at ~", "whack at ~",
    "smash at ~", "hack at ~", "impale at ~", "cut at ~", "slice at ~",
    "bludgeon at ~", "club at ~", "punch at ~", "kick at ~", "headbutt at ~",
    "shove at ~", "slap at ~", "jab at ~"
}, new List<string> { "nearby_architect" }));

            RecognizedCommands.Add("attack_with_weapon", (new List<string> {
    "slash ~ with ~", "stab ~ with ~", "pierce ~ with ~", "lash ~ with ~",
    "scourge ~ with ~", "whip ~ with ~", "strike ~ with ~", "bash ~ with ~",
    "crush ~ with ~", "whack ~ with ~", "smash ~ with ~", "hack ~ with ~",
    "impale ~ with ~", "cut ~ with ~", "slice ~ with ~", "bludgeon ~ with ~",
    "club ~ with ~", "punch ~ with ~", "kick ~ with ~", "headbutt ~ with ~",
    "shove ~ with ~", "slap ~ with ~", "jab ~ with ~"
}, new List<string> { "nearby_architect", "hand_object" }));

            RecognizedCommands.Add("attack_specific_body_part", (new List<string> {
    "slash ~ in the ~", "stab ~ in the ~", "pierce ~ in the ~", "lash ~ in the ~",
    "scourge ~ in the ~", "whip ~ in the ~", "strike ~ in the ~", "bash ~ in the ~",
    "crush ~ in the ~", "whack ~ in the ~", "smash ~ in the ~", "hack ~ in the ~",
    "impale ~ in the ~", "cut ~ in the ~", "slice ~ in the ~", "bludgeon ~ in the ~",
    "club ~ in the ~", "punch ~ in the ~", "kick ~ in the ~", "headbutt ~ in the ~",
    "shove ~ in the ~", "slap ~ in the ~", "jab ~ in the ~"
}, new List<string> { "nearby_architect", "body_part_type" }));

            RecognizedCommands.Add("attack_body_part_with_item", (new List<string> {
    "slash ~ in the ~ with ~", "stab ~ in the ~ with ~", "pierce ~ in the ~ with ~",
    "lash ~ in the ~ with ~", "scourge ~ in the ~ with ~", "whip ~ in the ~ with ~",
    "strike ~ in the ~ with ~", "bash ~ in the ~ with ~", "crush ~ in the ~ with ~",
    "whack ~ in the ~ with ~", "smash ~ in the ~ with ~", "hack ~ in the ~ with ~",
    "impale ~ in the ~ with ~", "cut ~ in the ~ with ~", "slice ~ in the ~ with ~",
    "bludgeon ~ in the ~ with ~", "club ~ in the ~ with ~", "punch ~ in the ~ with ~",
    "kick ~ in the ~ with ~", "headbutt ~ in the ~ with ~", "shove ~ in the ~ with ~",
    "slap ~ in the ~ with ~", "jab ~ in the ~ with ~"
}, new List<string> { "nearby_architect", "body_part_type", "hand_object" }));

            RecognizedCommands.Add("become_invisible", (new List<string> { "become one with shadow", "become one with the shadow", "become one with shadows", "become one with the shadows" }, new List<string> { "none" }));
            RecognizedCommands.Add("exit_invisibility", (new List<string> { "exit the shadows", "exit the darkness", "return from the shadows", "return from the shadow", "return from shadow" }, new List<string> { "none" }));
            RecognizedCommands.Add("level_up", (new List<string> { "level ~" }, new List<string> { "direction" }));
            RecognizedCommands.Add("engage_target", (new List<string> { "engage ~", "engage with ~", "confront ~", "focus ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("approach_target", (new List<string> { "approach ~", "move closer to ~", "advance towards ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("distance_from_target", (new List<string> { "distance from ~", "move away from ~", "retreat from ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("wield_item", (new List<string> { "take out ~", "unsheath ~", "remove ~", "wield ~", "unholster ~" }, new List<string> { "non_hand_inventory" }));
            RecognizedCommands.Add("pick_up_item", (new List<string> { "grab ~", "get ~", "take ~", "pick ~ ~", "steal ~"}, new List<string> { "nearby_object" }));
            RecognizedCommands.Add("drop_item", (new List<string> { "drop ~", "set ~ on the ground", "place ~ on the ground", "let go of ~" }, new List<string> { "inventory" }));
            RecognizedCommands.Add("place_item_in", (new List<string> { "place ~ in ~", "store ~ in ~", "stash ~ in ~", "put ~ in ~", "place ~ on ~", "place ~ inside ~" }, new List<string> { "inventory", "object" }));
            RecognizedCommands.Add("craft", (new List<string> { "craft", "build", "construct", "create", "forge", "sew", "assemble", "manufacture", "fabricate", "design", "knit", "weave", "shape", "mold", "sculpt", "form", "fashion" }, new List<string> { "none" }));
            RecognizedCommands.Add("take_item_from", (new List<string> { "take ~ from ~", "remove ~ from ~", "retrieve ~ from ~", "get ~ from ~", "extract ~ from ~", "take ~ off of ~", "remove ~ off of ~", "retrieve ~ off of ~", "get ~ off of ~", "extract ~ off of ~" }, new List<string> { "object", "object" }));
            RecognizedCommands.Add("wear_item", (new List<string> { "wear ~", "put on ~", "don ~" }, new List<string> { "inventory" }));
            RecognizedCommands.Add("remove_worn_item", (new List<string> { "remove ~", "take off ~", "doff ~" }, new List<string> { "clothing" }));
            RecognizedCommands.Add("examine", (new List<string> { "examine ~", "look at ~", "check out ~", "look closer at ~", "inspect ~", "observe ~", "view ~" }, new List<string> { "nearby_target" }));
            RecognizedCommands.Add("give_item", (new List<string> { "give ~ to ~", "offer ~ to ~", "sacrifice ~ to ~" }, new List<string> { "object", "nearby_target" }));
            RecognizedCommands.Add("throw_item", (new List<string> { "throw ~", "toss ~", "fling ~" }, new List<string> { "hand_object" }));
            RecognizedCommands.Add("throw_item_at", (new List<string> { "throw ~ at ~", "toss ~ at ~", "fling ~ at ~", "throw ~ towards ~", "toss ~ towards ~", "fling ~ towards ~" }, new List<string> { "hand_object", "nearby_target" }));
            RecognizedCommands.Add("cast_spell_at_1", (new List<string> { "cast ~ at ~" }, new List<string> { "spell", "entity" }));
            RecognizedCommands.Add("cast_spell_at_2", (new List<string> { "cast ~ at ~ and ~" }, new List<string> { "spell", "entity", "entity" }));
            RecognizedCommands.Add("cast_spell_at_3", (new List<string> { "cast ~ at ~, ~, and ~", "cast ~ at ~ and ~ and ~" }, new List<string> { "spell", "entity", "entity", "entity" }));
            RecognizedCommands.Add("cast_spell_at_4", (new List<string> { "cast ~ at ~, ~, ~, and ~", "cast ~ at ~ and ~ and ~ and ~" }, new List<string> { "spell", "entity", "entity", "entity", "entity" }));
            RecognizedCommands.Add("cast_spell_at_5", (new List<string> { "cast ~ at ~, ~, ~, ~, and ~", "cast ~ at ~ and ~ and ~ and ~ and ~" }, new List<string> { "spell", "entity", "entity", "entity", "entity", "entity" }));
            RecognizedCommands.Add("cast_spell", (new List<string> { "cast ~" }, new List<string> { "spell" }));

            RecognizedCommands.Add("consume", (new List<string> { "consume ~", "apply ~", "eat ~", "drink ~" }, new List<string> { "inventory" }));

            RecognizedCommands.Add("recall_information", (new List<string> { "remember ~", "list ~", "list all ~" }, new List<string> { "rememberance" }));
            RecognizedCommands.Add("ditch_inventory", (new List<string> { "strip", "ditch my inventory", "drop everything" }, new List<string> { "none" }));
            RecognizedCommands.Add("read_object", (new List<string> { "read ~" }, new List<string> { "inventory" }));
            RecognizedCommands.Add("perform_composition", (new List<string> { "recite ~", "sing ~", "perform ~" }, new List<string> { "known_compositions" }));
            RecognizedCommands.Add("write_composition", (new List<string> { "write ~", "write a ~", "write an ~", "compose ~", "compose a ~", "compose an ~" }, new List<string> { "composition_types" }));
            RecognizedCommands.Add("write_about_topic", (new List<string> { "write ~ about ~", "write a ~ about ~", "write an ~ about ~", "compose ~ about ~", "compose a ~ about ~", "compose an ~ about ~", "write ~ on ~", "write a ~ on ~", "write an ~ on ~", "compose ~ on ~", "compose a ~ on ~", "compose an ~ on ~" }, new List<string> { "composition_types", "entity" }));
            RecognizedCommands.Add("tame_creature", (new List<string> { "tame ~", "pacify ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("starstrike", (new List<string> { "starstrike ~" }, new List<string> { "nearby_target" }));
            RecognizedCommands.Add("flamestrike", (new List<string> { "flamestrike ~" }, new List<string> { "nearby_target" }));
            RecognizedCommands.Add("heat_object", (new List<string> { "heat ~" }, new List<string> { "nearby_object" }));
            RecognizedCommands.Add("starsmite", (new List<string> { "starsmite ~" }, new List<string> { "nearby_target" }));
            RecognizedCommands.Add("conjure_spark", (new List<string> { "conjure spark" }, new List<string> { "none" }));
            RecognizedCommands.Add("evoke_strike", (new List<string> { "evoke strike at ~", "evoke beam at ~", "evoke beams at ~", "evoke light at ~" }, new List<string> { "nearby_target" }));
            RecognizedCommands.Add("evoke_blindness", (new List<string> { "evoke blindness" }, new List<string> { "none" }));
            RecognizedCommands.Add("evoke_nexus", (new List<string> { "evoke nexus" }, new List<string> { "none" }));
            RecognizedCommands.Add("evoke_healing", (new List<string> { "evoke healing" }, new List<string> { "none" }));
            RecognizedCommands.Add("inflame", (new List<string> { "inflame" }, new List<string> { "none" }));
            RecognizedCommands.Add("unflame", (new List<string> { "unflame" }, new List<string> { "none" }));
            RecognizedCommands.Add("augment_creature", (new List<string> { "augment ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("raise_dead", (new List<string> { "raise ~" }, new List<string> { "corpse" }));
            RecognizedCommands.Add("fire_spectral_bolt", (new List<string> { "fire spectral bolt at ~", "spectralize ~", "spectral bolt ~" }, new List<string> { "nearby_target" }));
            RecognizedCommands.Add("increase_weight", (new List<string> { "increase weight of ~" }, new List<string> { "nearby_object" }));
            RecognizedCommands.Add("increase_temperature", (new List<string> { "increase temperature of ~" }, new List<string> { "nearby_object" }));
            RecognizedCommands.Add("increase_aerodynamics", (new List<string> { "increase aerodynamics of ~" }, new List<string> { "nearby_object" }));
            RecognizedCommands.Add("increase_integrity", (new List<string> { "increase integrity of ~" }, new List<string> { "nearby_object" }));
            RecognizedCommands.Add("decrease_weight", (new List<string> { "decrease weight of ~" }, new List<string> { "nearby_object" }));
            RecognizedCommands.Add("decrease_temperature", (new List<string> { "decrease temperature of ~" }, new List<string> { "nearby_object" }));
            RecognizedCommands.Add("decrease_aerodynamics", (new List<string> { "decrease aerodynamics of ~" }, new List<string> { "nearby_object" }));
            RecognizedCommands.Add("decrease_integrity", (new List<string> { "decrease integrity of ~" }, new List<string> { "nearby_object" }));
            RecognizedCommands.Add("liquify", (new List<string> { "liquify ~" }, new List<string> { "nearby_target" }));
            RecognizedCommands.Add("split", (new List<string> { "split ~" }, new List<string> { "nearby_object" }));
            RecognizedCommands.Add("blip", (new List<string> { "blip ~" }, new List<string> { "nearby_target" }));
            RecognizedCommands.Add("use_skill", (new List<string> { "initiate ~", "use skill ~", "use ~ skill", "use ~" }, new List<string> { "skill" }));
            RecognizedCommands.Add("reposition", (new List<string> { "reposition", "reset" }, new List<string> { "none" }));
            RecognizedCommands.Add("retract", (new List<string> { "retract ~", "pull back ~" }, new List<string> { "body_part_type" }));
            RecognizedCommands.Add("free", (new List<string> { "free ~", "unbound ~", "release ~" }, new List<string> { "nearby_architect" }));

            // SuggestibleCommands.AddRange(RecognizedCommands.SelectMany(pair => pair.Value));

            SuggestibleCommands.AddRange(RecognizedCommands.SelectMany(pair => pair.Value.Item1));




            CommandIDToCategory = new Dictionary<string, string[]>
            {
                // General
                { "greet", new string[] { "general" } },
                { "farewell", new string[] { "general" } },
                { "thank", new string[] { "general" } },
                { "apologize", new string[] { "general" } },
                { "compliment", new string[] { "general" } },
                { "insult", new string[] { "general" } },

    
                // Questions
                { "ask_name", new string[] { "questions" } },
{ "ask_directions", new string[] { "questions" } },
{ "ask_generic_directions", new string[] { "questions" } },
{ "ask_about_something", new string[] { "questions" } },
{ "ask_ruler", new string[] { "questions" } },
{ "ask_trade", new string[] { "questions" } },
{ "ask_them_join", new string[] { "questions" } },
{ "ask_me_join", new string[] { "questions" } },
{ "ask_current_structure", new string[] { "questions" } },
{ "ask_profession", new string[] { "questions" } },
{ "ask_health", new string[] { "questions" } },
{ "ask_news", new string[] { "questions" } },
{ "ask_story", new string[] { "questions" } },
{ "ask_history", new string[] { "questions" } },
{ "ask_opinion", new string[] { "questions" } },
{ "ask_interests", new string[] { "questions" } },
{ "ask_family", new string[] { "questions" } },
{ "ask_advice", new string[] { "questions" } },


                // Requests
                { "provide_assistance", new string[] { "requests" } },
{ "inform_quest", new string[] { "requests" } },
{ "tell_story_about", new string[] { "requests" } },
{ "surrender", new string[] { "requests" } },
{ "demand_surrender", new string[] { "requests" } },
{ "demand_item", new string[] { "requests" } },
{ "challenge", new string[] { "requests" } },

                // Movement
{ "engage_target", new string[] { "movement" } },
{ "approach_target", new string[] { "movement" } },
{ "distance_from_target", new string[] { "movement" } },
{ "leave_structure", new string[] { "movement" } },
{ "enter", new string[] { "movement" } },
{ "move_direction", new string[] { "movement" } },

                // Offensive
                { "basic_attack", new string[] { "offensive" } },
{ "attack_with_weapon", new string[] { "offensive" } },
{ "attack_specific_body_part", new string[] { "offensive" } },
{ "attack_body_part_with_item", new string[] { "offensive" } },
{ "starstrike", new string[] { "offensive" } },
{ "flamestrike", new string[] { "offensive" } },
{ "evoke_strike", new string[] { "offensive" } },
{ "evoke_blindness", new string[] { "offensive" } },
{ "fire_spectral_bolt", new string[] { "offensive" } },


                // Defensive

{ "evoke_nexus", new string[] { "defensive" } },
{ "evoke_healing", new string[] { "defensive" } },
{ "inflame", new string[] { "defensive" } },
{ "unflame", new string[] { "defensive" } },
{ "augment_creature", new string[] { "defensive" } },
{ "raise_dead", new string[] { "defensive" } },
{ "become_invisible", new string[] { "defensive" } },
{ "exit_invisibility", new string[] { "defensive" } },


                // Utility
                { "increase_weight", new string[] { "utility" } },
{ "increase_temperature", new string[] { "utility" } },
{ "increase_aerodynamics", new string[] { "utility" } },
{ "increase_integrity", new string[] { "utility" } },
{ "decrease_weight", new string[] { "utility" } },
{ "decrease_temperature", new string[] { "utility" } },
{ "decrease_aerodynamics", new string[] { "utility" } },
{ "decrease_integrity", new string[] { "utility" } },
{ "liquify", new string[] { "utility" } },
{ "split", new string[] { "utility" } },
{ "blip", new string[] { "utility" } },


                // Items
                { "wield_item", new string[] { "items" } },
{ "pick_up_item", new string[] { "items" } },
{ "drop_item", new string[] { "items" } },
{ "place_item_in", new string[] { "items" } },
{ "take_item_from", new string[] { "items" } },
{ "wear_item", new string[] { "items" } },
{ "remove_worn_item", new string[] { "items" } },
{ "examine", new string[] { "items" } },
{ "give_item", new string[] { "items" } },
{ "inventory_check", new string[] { "items" } },
{ "recall_information", new string[] { "items" } },
{ "ditch_inventory", new string[] { "items" } },
{ "read_object", new string[] { "items" } },


                // Creativity
                { "perform_composition", new string[] { "creativity" } },
                { "write_composition", new string[] { "creativity" } },
                { "write_about_topic", new string[] { "creativity" } },
                { "craft", new string[] { "creativity" } },

                //Multi
                { "throw_item_at", new string[] { "offensive", "items" } },
                { "heat_object", new string[] { "offensive", "defensive" } },
                { "use_skill", new string[] { "offensive", "defensive" } },
                { "starsmite", new string[] { "offensive", "defensive" } },
                { "conjure_spark", new string[] { "offensive", "defensive" } },
                { "cast_spell_at_1", new string[] { "offensive", "defensive" } },
                { "cast_spell_at_2", new string[] { "offensive", "defensive" } },
                { "cast_spell_at_3", new string[] { "offensive", "defensive" } },
                { "cast_spell_at_4", new string[] { "offensive", "defensive" } },
                { "cast_spell_at_5", new string[] { "offensive", "defensive" } }
            };





            ColorConverter.Add("maroon", Color.Maroon);
            ColorConverter.Add("red", Color.Red);
            ColorConverter.Add("orange", Color.Orange);
            ColorConverter.Add("yellow", Color.Yellow);
            ColorConverter.Add("limegreen", Color.LimeGreen);
            ColorConverter.Add("green", Color.Green);
            ColorConverter.Add("lightblue", Color.LightBlue);
            ColorConverter.Add("cyan", Color.Cyan);
            ColorConverter.Add("blue", Color.Blue);
            ColorConverter.Add("purple", Color.Purple);
            ColorConverter.Add("magenta", Color.Magenta);
            ColorConverter.Add("coral", Color.Coral);
            ColorConverter.Add("white", Color.White);
            ColorConverter.Add("gray", Color.Gray);
            ColorConverter.Add("black", new Color(50, 50, 50));
            ColorConverter.Add("brown", Color.Brown);

            ConvertArchitectToGroupType = new Dictionary<string, string>
            {
                { "warrior", "military" },
                { "mercenary", "mercenary" },
                { "elder", "religious" },
                { "child", "none" },
                { "prophet", "religious" },
                { "commander", "military" },
                { "sentinel", "military" },
                { "trader", "trade" },
                { "leader", "political" },
                { "political figure", "political" },
                { "outlaw", "squad" },
                { "anarchist", "squad" },
                { "craftsman", "guild" },
                { "musician", "entertainment" },
                { "scholar", "scholarly" },
                { "sorcerer", "none" },
                { "warlock", "none" },
                { "alpha", "none" },
                { "prestiged", "none" },
                { "indolent", "none" },
                { "hunter", "none" },
                { "adventurer", "none" },
                { "assassin", "none" },
                { "rogue", "none" },
                { "artisan", "none" },
                { "diplomat", "political" },
                { "enchanter", "scholarly" },
                { "archbard", "entertainment" },
                { "archluminary", "scholarly" },
                { "archartificer", "scholarly" },
                { "archduelist", "military" },
                { "elemental", "none" },
                { "hypernexus", "none" },
                { "icosidodecahedron", "none" },
                { "heart", "none" },
                { "spatiomancer", "none" },
                { "perceptomancer", "none" },
                { "conjumancer", "none" },
                { "fractalmancer", "none" },
                { "embezzler", "none" },
                { "beast", "none" },
                { "largebeast", "none" },
                { "spy", "none" },
                { "diplomancer", "political" },
                { "magician", "entertainment" },
                { "scout", "none" },
                { "animal", "none" },
                { "end", "none" },
                { "soldier", "military" },
                { "peasant", "none" },
                { "merchant", "trade" },
                { "blacksmith", "guild" },
                { "miller", "none" },
                { "baker", "none" },
                { "brewer", "none" },
                { "tanner", "none" },
                { "tailor", "guild" },
                { "bandit", "squad" },
                { "carpenter", "guild" },
                { "mason", "guild" },
                { "scribe", "scholarly" },
                { "butcher", "none" },
                { "fisherman", "none" },
                { "weaver", "guild" },
                { "potter", "none" },
                { "miner", "none" },
                { "construct", "none" },
                { "vagabond", "none" },
                { "priest", "religious" },
                { "shadebeast", "none" },
                { "knight", "military" },
                { "thief", "none" },
                { "archmage", "scholarly" },
                { "beastmaster", "none" },
            };


            ConvertProfessionToCareerDescription = new Dictionary<string, string>
            {
                {"warrior", "from your extensive military career"},
                {"mercenary", "from your previous adventures"},
                {"elder", "from the local temple"},
                {"prophet", "from the local temple"},
                {"commander", "from your commander's salary"},
                {"trader", "from your economic journeys around the continent"},
                {"leader", "from the local taxes"},
                {"political figure", "from the local taxes"},
                {"outlaw", "from some various questionable activities"},
                {"anarchist", "from the chaos you've sown"},
                {"craftsman", "by selling shiba statues"},
                {"musician", "courtesy of ecstatic tavernkeepers"},
                {"scholar", "by sharing your studies"},
                {"sorcerer", "from the deity of light"},
                {"warlock", "from the deity of shadow"},
                {"alpha", "from taxes"},
                {"child", "from your allowance"},
                {"sentinel", "from guarding the core"},
                {"prestiged", "from taxes in the past"},
                {"archbard", "from adoring fans"},
                {"archluminary", "from selling your work"},
                {"archartificer", "from selling your work"},
                {"archduelist", "from the foes you've slain"},
                {"elemental", "from selling your material"},
                {"hypernexus", "from photonexus taxes"},
                {"icosidodecahedron", "from isofractal art"},
                {"heart", "from pure evil"},
                {"spatiomancer", "from your magical craft"},
                {"perceptomancer", "from your magical craft"},
                {"conjumancer", "from your magical craft"},
                {"fractalmancer", "from your magical craft"},
                {"embezzler", "from exploiting your hometown"},
                {"beast", "from om nom noming"},
                {"largebeast", "from OM NOM NOMING"},
                {"spy", "from spying"},
                {"diplomancer", "from diplomancy"},
                {"magician", "from your magical craft"},
                {"scout", "from selling information"},
                {"animal", "from om nom noming"},
                {"hunter", "from selling animal spoils"},
                {"end", "from being awesome"},
                {"soldier", "from your captain's pay"},
                {"peasant", "from farming"},
                {"merchant", "from your exchange business"},
                {"blacksmith", "from blacksmithery"},
                {"miller", "from selling grain"},
                {"baker", "from your delightful pastries"},
                {"brewer", "from your precious caffeine"},
                {"tanner", "from processing leather goods"},
                {"tailor", "from your stylish creations"},
                {"bandit", "from your loot and plunder"},
                {"carpenter", "from building and carpentry"},
                {"mason", "from your stonework"},
                {"scribe", "from writing and documentation"},
                {"butcher", "from your meat products"},
                {"fisherman", "from fishing"},
                {"weaver", "from weaving textiles"},
                {"potter", "from crafting pottery"},
                {"miner", "from extracting valuable minerals"},
                {"construct", "from your programmed tasks"},
                {"indolent", "from odd jobs here and there"},
                {"vagabond", "from wandering the lands"},
                {"priest", "from spiritual guidance and rituals"},
                {"shadebeast", "from lurking in shadows"},
                {"knight", "from your chivalrous deeds"},
                {"thief", "from stealth and theft"},
                {"archmage", "from mastering arcane arts"},
                {"beastmaster", "from training and caring for beasts"},
                {"adventurer", "from various adventures"},
                {"assassin", "from bounty hunting"},
                {"rogue", "from questionable pursuits"},
                {"artisan", "from selling your craft"},
                {"diplomat", "from taxes and fees"},
                {"enchanter", "from selling great enchantment"}
            };

            ConvertProfessionToBuilding = new Dictionary<string, string>
            {
                {"warrior", "watchtower"},
                {"mercenary", "tavern"},
                {"elder", "shrine"},
                {"prophet", "shrine"},
                {"commander", "watchtower"},
                {"trader", "market"},
                {"leader", "library"},
                {"political figure", "library"},
                {"outlaw", "tavern"},
                {"anarchist", "tavern"},
                {"craftsman", "forge"},
                {"musician", "tavern"},
                {"scholar", "library"},
                {"sentinel", "core"},
                {"sorcerer", "spire"},
                {"warlock", "spire"},
                {"duelist", "tower"},
                {"alpha", "prism"},
                {"child", "house"},
                {"prestiged", "house"},
                {"archbard", "tavern"},
                {"archluminary", "library"},
                {"archartificer", "forge"},
                {"archduelist", "watchtower"},
                {"elemental", "none"},
                {"soverign", "core"},
                {"heart", "heart"},
                {"spatiomancer", "library"},
                {"perceptomancer", "library"},
                {"conjumancer", "library"},
                {"fractalmancer", "library"},
                {"embezzler", "tavern"},
                {"beast", "none"},
                {"knight", "watchtower"},
                {"thief", "tavern"},
                {"archmage", "library"},
                {"beastmaster", "none"},
                {"largebeast", "none"},
                {"spy", "tavern"},
                {"diplomancer", "library"},
                {"magician", "shrine"},
                {"scout", "watchtower"},
                {"animal", "none"},
                {"end", "sanctum"},
                {"soldier", "watchtower"},
                {"peasant", "none"},
                {"merchant", "market"},
                {"blacksmith", "forge"},
                {"miller", "none"},
                {"baker", "tavern"},
                {"brewer", "tavern"},
                {"tanner", "none"},
                {"tailor", "none"},
                {"bandit", "none"},
                {"carpenter", "none"},
                {"mason", "none"},
                {"scribe", "library"},
                {"butcher", "none"},
                {"fisherman", "none"},
                {"weaver", "none"},
                {"potter", "none"},
                {"miner", "forge"},
                {"construct", "none"},
                {"indolent", "none"},
                {"vagabond", "none"},
                {"priest", "shrine"},
                {"shadebeast", "heart"},
                {"hunter", "tavern"},
                {"adventurer", "tavern"},
                {"assassin", "tavern"},
                {"rogue", "market"},
                {"artisan", "forge"},
                {"diplomat", "prism"},
                {"enchanter", "shrine"}
            };

            ConvertTaskToArchitectPrefix = new Dictionary<string, string>
            {
                {"killtarget", "fighting"},
                {"disabletarget", "fighting"},
                {"vacanttfortrade", "vacant"},
                {"drinking", "drinking"},
                {"drinkingcaffeine", "drinking"},
                {"sleeping", "sleeping"},
                {"discussion", "discussing"},
                {"socializing", "socializing"},
                {"performmusic", "playing music"},
                {"performpoetry", "reciting"},
                {"performdance", "dancing"},
                {"performtheater", "acting"},
                {"cook", "cooking"},
                {"eating", "eating"},
                {"industry", "working"},
                {"contemplate", "contemplating"},
                {"bound", "bound"},
                {"study", "studying"},
                {"", "idle"}
            };


            // Shouldn't have to do this, just install the TTF on dev machines.
            /*
            // Specify the path to your TTF file
            string fontFilePath = "Content/fonts/BLKCHCRY.TTF";

            // Specify the destination directory in the user's profile
            string userFontsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "fonts");

            // Copy the font file to the user's Fonts directory
            Directory.CreateDirectory(userFontsDirectory);
            File.Copy(fontFilePath, Path.Combine(userFontsDirectory, "BLKCHCRY.TTF"), true);
            */

            //create the key dictionary
            for (Keys key = Keys.A; key <= Keys.Z; key++)
            {
                KeyAtlas[key] = key.ToString().ToLower();
                UpperKeyAtlas[key] = key.ToString().ToUpper();
            }

            KeyAtlas[Keys.Space] = " ";
            UpperKeyAtlas[Keys.Space] = " ";

            for (Keys key = Keys.D0; key <= Keys.D9; key++)
            {
                KeyAtlas[key] = ((int)(key - Keys.D0)).ToString();
            }

            KeyAtlas[Keys.OemQuestion] = "/";
            KeyAtlas[Keys.OemMinus] = "-";
            KeyAtlas[Keys.OemPlus] = "=";
            KeyAtlas[Keys.OemComma] = ",";
            KeyAtlas[Keys.OemPeriod] = ".";
            KeyAtlas[Keys.OemSemicolon] = ";";
            KeyAtlas[Keys.OemQuotes] = "'";
            KeyAtlas[Keys.OemBackslash] = "\\";
            KeyAtlas[Keys.OemOpenBrackets] = "[";
            KeyAtlas[Keys.OemCloseBrackets] = "]";
            KeyAtlas[Keys.OemPipe] = "|";

            UpperKeyAtlas[Keys.OemQuestion] = "?";
            UpperKeyAtlas[Keys.OemMinus] = "_";
            UpperKeyAtlas[Keys.OemPlus] = "+";
            UpperKeyAtlas[Keys.OemComma] = "<";
            UpperKeyAtlas[Keys.OemPeriod] = ">";
            UpperKeyAtlas[Keys.OemSemicolon] = ":";
            UpperKeyAtlas[Keys.OemQuotes] = "\"";
            UpperKeyAtlas[Keys.OemBackslash] = "|";
            UpperKeyAtlas[Keys.OemOpenBrackets] = "{";
            UpperKeyAtlas[Keys.OemCloseBrackets] = "}";
            UpperKeyAtlas[Keys.OemPipe] = "|";

            UpperKeyAtlas[Keys.D1] = "!";
            UpperKeyAtlas[Keys.D2] = "@";
            UpperKeyAtlas[Keys.D3] = "#";
            UpperKeyAtlas[Keys.D4] = "$";
            UpperKeyAtlas[Keys.D5] = "%";
            UpperKeyAtlas[Keys.D6] = "^";
            UpperKeyAtlas[Keys.D7] = "&";
            UpperKeyAtlas[Keys.D8] = "*";
            UpperKeyAtlas[Keys.D9] = "(";
            UpperKeyAtlas[Keys.D0] = ")";

            InvertDoorDirection.Add("north", "south");
            InvertDoorDirection.Add("south", "north");
            InvertDoorDirection.Add("east", "west");
            InvertDoorDirection.Add("west", "east");
            InvertDoorDirection.Add("up", "down");
            InvertDoorDirection.Add("down", "up");

            Console.WriteLine("Initialization complete. Exiting application...");

            FreeConsole();
            FrameCounter = new FrameCounter();
            GameInput = new GameInput();
            base.Initialize();
        }


        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (_recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
            {
                var result = _recognizer.Result();
                DisplayResult(result);
            }
            else
            {
                var partialResult = _recognizer.PartialResult();
                DisplayResult(partialResult);
            }
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                // Handle exceptions here
                Console.WriteLine($"Exception: {e.Exception.Message}");
            }
        }

        private void DisplayResult(string result)
        {
            var jsonResult = JObject.Parse(result);
            var text = jsonResult["text"]?.ToString() ?? string.Empty;

            // Check if the prompt is empty and the text starts with "I " or "i "
            if (string.IsNullOrEmpty(MostRecentPartyTurnArchitect.Prompt) &&
                (text.StartsWith("I ") || text.StartsWith("i ")))
            {
                // Trim "I " or "i " from the start of the text
                text = text.Substring(2);
            }

            // Append text to your prompt or handle it accordingly
            MostRecentPartyTurnArchitect.Prompt += text;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            ContentRoot = Content.RootDirectory;

            // Load your icon texture
            myIconTexture = Content.Load<Texture2D>("icons/Icon");

            // Assuming your icon is a Texture2D loaded from the Content Pipeline
            Texture2D iconTexture = Content.Load<Texture2D>("icons/Icon");

            ContentPath = Path.GetFullPath("Content");
            string dataPath = string.Concat(ContentPath, "\\data\\");

            FirstNames = File.ReadAllLines(string.Concat(dataPath, "names.txt")).ToList();
            LastNames = File.ReadAllLines(string.Concat(dataPath, "last-names.txt")).ToList();
            Words = File.ReadAllLines(string.Concat(dataPath, "words.txt")).ToList();
            Syllables = File.ReadAllLines(string.Concat(dataPath, "syllables.txt")).ToList();
            NameSuffixes = File.ReadAllLines(string.Concat(dataPath, "namesuffixes.txt")).ToList();

            FrameT = Content.Load<Texture2D>("gui/frame");
            SpeakingT = Content.Load<Texture2D>("icons/speaking");
            QuitGuiT = Content.Load<Texture2D>("other/quitgui");

            if (SimplifiedFont)
            {
                Shibafont = Content.Load<SpriteFont>("spritefonts/shibafont2");
                BabyShibafont = Content.Load<SpriteFont>("spritefonts/babyshibafont2");
            }
            else
            {
                Shibafont = Content.Load<SpriteFont>("spritefonts/shibafont");
                BabyShibafont = Content.Load<SpriteFont>("spritefonts/babyshibafont");
            }

            DesertT = Content.Load<Texture2D>("tiles/desert");
            TileAtlas.Add("desert", DesertT);
            ForestT = Content.Load<Texture2D>("tiles/forest");
            TileAtlas.Add("forest", ForestT);
            LightforestT = Content.Load<Texture2D>("tiles/lightforest");
            TileAtlas.Add("lightforest", LightforestT);
            MountainT = Content.Load<Texture2D>("tiles/mountain");
            TileAtlas.Add("mountain", MountainT);
            OceanT = Content.Load<Texture2D>("tiles/ocean");
            TileAtlas.Add("ocean", OceanT);
            PlainsT = Content.Load<Texture2D>("tiles/plains");
            TileAtlas.Add("plains", PlainsT);
            SnowpeakT = Content.Load<Texture2D>("tiles/snowpeak");
            TileAtlas.Add("snowpeak", SnowpeakT);
            TaigaT = Content.Load<Texture2D>("tiles/taiga");
            TileAtlas.Add("taiga", TaigaT);
            TundraT = Content.Load<Texture2D>("tiles/tundra");
            TileAtlas.Add("tundra", TundraT);
            VoidT = Content.Load<Texture2D>("tiles/void");
            TileAtlas.Add("void", VoidT);
            EtherealT = Content.Load<Texture2D>("tiles/ethereal");
            TileAtlas.Add("ethereal", EtherealT);

            PortT = Content.Load<Texture2D>("tiles/port");

            PyramidT = Content.Load<Texture2D>("locationtiles/pyramid");
            TileAtlas.Add("pyramid", PyramidT);
            ToroidT = Content.Load<Texture2D>("locationtiles/toroid");
            TileAtlas.Add("toroid", ToroidT);
            TowersT = Content.Load<Texture2D>("locationtiles/towers");
            TileAtlas.Add("towers", TowersT);
            HallwayT = Content.Load<Texture2D>("locationtiles/hallway");
            TileAtlas.Add("hallway", HallwayT);
            ArchwayT = Content.Load<Texture2D>("locationtiles/archway");
            TileAtlas.Add("archway", ArchwayT);

            DistrictEmptyDesertT = Content.Load<Texture2D>("distmap/emptydesert");
            DistrictEmptyPlainsT = Content.Load<Texture2D>("distmap/emptyplains");
            DistrictEmptySnowT = Content.Load<Texture2D>("distmap/emptysnow");
            DistrictEmptyTreesT = Content.Load<Texture2D>("distmap/emptytrees");
            DistrictBuildingT = Content.Load<Texture2D>("distmap/buildings");
            DistrictManyBuildingsT = Content.Load<Texture2D>("distmap/manybuildings");
            DistrictSpecialAndBuildingsT = Content.Load<Texture2D>("distmap/specialandbuildings");
            DistrictSpecialBuildingT = Content.Load<Texture2D>("distmap/specialbuilding");
            DistrictEmptyOceanT = Content.Load<Texture2D>("distmap/ocean");
            DistrictSpireT = Content.Load<Texture2D>("distmap/spire");
            DistrictSanctumT = Content.Load<Texture2D>("distmap/sanctum");
            DistrictWellT = Content.Load<Texture2D>("distmap/well");
            DistrictShadowStorageT = Content.Load<Texture2D>("distmap/shadowstorage");
            DistrictMarketT = Content.Load<Texture2D>("distmap/market");
            DistrictMarketSurroundedT = Content.Load<Texture2D>("distmap/marketsurrounded");
            DistrictPrismT = Content.Load<Texture2D>("distmap/prism");

            DistrictArchwayT = Content.Load<Texture2D>("distmap/archway");
            DistrictCommuneT = Content.Load<Texture2D>("distmap/commune");
            DistrictDockT = Content.Load<Texture2D>("distmap/dock");
            DistrictShipT = Content.Load<Texture2D>("distmap/ship");
            DistrictFortressT = Content.Load<Texture2D>("distmap/fortress");
            DistrictHallwayT = Content.Load<Texture2D>("distmap/hallway");
            DistrictMoundT = Content.Load<Texture2D>("distmap/mound");
            DistrictCoreT = Content.Load<Texture2D>("distmap/core");
            DistrictScaffoldT = Content.Load<Texture2D>("distmap/scaffold");
            DistrictKeepT = Content.Load<Texture2D>("distmap/keep");
            DistrictMonasteryT = Content.Load<Texture2D>("distmap/monastery");
            DistrictMonumentT = Content.Load<Texture2D>("distmap/monument");
            DistrictOutpostT = Content.Load<Texture2D>("distmap/outpost");
            DistrictPyramidT = Content.Load<Texture2D>("distmap/pyramid");
            DistrictHeartT = Content.Load<Texture2D>("distmap/heart");
            DistrictScumT = Content.Load<Texture2D>("distmap/scum");
            DistrictStrongholdT = Content.Load<Texture2D>("distmap/stronghold");
            DistrictToroidT = Content.Load<Texture2D>("distmap/toroid");
            DistrictTowerT = Content.Load<Texture2D>("distmap/tower");
            DistrictTowersT = Content.Load<Texture2D>("distmap/towers");

            TitleScreen = Content.Load<Texture2D>("other/title");
            EmptyTileT = Content.Load<Texture2D>("tiles/emptytile");
            GUI = Content.Load<Texture2D>("gui/gui");
            HelpGUI = Content.Load<Texture2D>("gui/helpgui");
            InventoryGUI = Content.Load<Texture2D>("gui/inventory gui");
            ThisListT = Content.Load<Texture2D>("gui/thislist");
            SkillPullUpT = Content.Load<Texture2D>("gui/skillpullup");
            SpellPullUpT = Content.Load<Texture2D>("gui/spellpullup");
            BodyPartPullUpT = Content.Load<Texture2D>("gui/bodypartpullup");

            nightfellCampT = Content.Load<Texture2D>("locationtiles/nightfellcamp");
            TileAtlas.Add("nightfellcamp", nightfellCampT);
            nightfellVillageT = Content.Load<Texture2D>("locationtiles/nightfellvillage");
            TileAtlas.Add("nightfellvillage", nightfellVillageT);
            nightfellTownT = Content.Load<Texture2D>("locationtiles/nightfelltown");
            TileAtlas.Add("nightfelltown", nightfellTownT);
            nightfellCityT = Content.Load<Texture2D>("locationtiles/nightfellcity");
            TileAtlas.Add("nightfellcity", nightfellCityT);

            LuminarchCampT = Content.Load<Texture2D>("locationtiles/luminarchcamp");
            TileAtlas.Add("luminarchcamp", LuminarchCampT);
            LuminarchVillageT = Content.Load<Texture2D>("locationtiles/luminarchvillage");
            TileAtlas.Add("luminarchvillage", LuminarchVillageT);
            LuminarchTownT = Content.Load<Texture2D>("locationtiles/luminarchtown");
            TileAtlas.Add("luminarchtown", LuminarchTownT);
            LuminarchCityT = Content.Load<Texture2D>("locationtiles/luminarchcity");
            TileAtlas.Add("luminarchcity", LuminarchCityT);

            LostCampT = Content.Load<Texture2D>("locationtiles/lostcamp");
            TileAtlas.Add("archaixcamp", LostCampT);
            LostVillageT = Content.Load<Texture2D>("locationtiles/lostvillage");
            TileAtlas.Add("archaixvillage", LostVillageT);
            LostTownT = Content.Load<Texture2D>("locationtiles/losttown");
            TileAtlas.Add("archaixtown", LostTownT);
            LostCityT = Content.Load<Texture2D>("locationtiles/lostcity");
            TileAtlas.Add("archaixcity", LostCityT);

            PhotonexusOutpostT = Content.Load<Texture2D>("locationtiles/photonexusoutpost");
            TileAtlas.Add("photonexusgarrison", PhotonexusOutpostT);
            PhotonexusCoreT = Content.Load<Texture2D>("locationtiles/photonexuscore");
            TileAtlas.Add("photonexuscore", PhotonexusCoreT);
            IsofractalOutpostT = Content.Load<Texture2D>("locationtiles/isofractaloutpost");
            TileAtlas.Add("isofractalgarrison", IsofractalOutpostT);
            IsofractalCoreT = Content.Load<Texture2D>("locationtiles/isofractalcore");
            TileAtlas.Add("isofractalcore", IsofractalCoreT);
            ShadeOutpostT = Content.Load<Texture2D>("locationtiles/shadeoutpost");
            TileAtlas.Add("shadegarrison", ShadeOutpostT);
            ShadeCoreT = Content.Load<Texture2D>("locationtiles/shadecore");
            TileAtlas.Add("shadecore", ShadeCoreT);

            OutlineT = Content.Load<Texture2D>("tiles/outline");

            SpireT = Content.Load<Texture2D>("locationtiles/spire");
            TileAtlas.Add("spire", SpireT);
            OutpostT = Content.Load<Texture2D>("locationtiles/outpost");
            TileAtlas.Add("outpost", OutpostT);

            StrongholdT = Content.Load<Texture2D>("locationtiles/stronghold");
            TileAtlas.Add("stronghold", StrongholdT);

            CoveT = Content.Load<Texture2D>("locationtiles/cove");
            TileAtlas.Add("cove", CoveT);
            CommuneT = Content.Load<Texture2D>("locationtiles/commune");
            TileAtlas.Add("commune", CommuneT);
            HoardT = Content.Load<Texture2D>("locationtiles/hoard");
            TileAtlas.Add("hoard", HoardT);
            PreserveT = Content.Load<Texture2D>("locationtiles/preserve");
            TileAtlas.Add("preserve", PreserveT);
            MonasteryT = Content.Load<Texture2D>("locationtiles/monastery");
            TileAtlas.Add("monastery", MonasteryT);

            KeepT = Content.Load<Texture2D>("locationtiles/keep");
            TileAtlas.Add("keep", KeepT);
            TowerT = Content.Load<Texture2D>("locationtiles/tower");
            TileAtlas.Add("tower", TowerT);
            FortressT = Content.Load<Texture2D>("locationtiles/fortress");
            TileAtlas.Add("fortress", FortressT);
            MonumentT = Content.Load<Texture2D>("locationtiles/monument");
            TileAtlas.Add("monument", MonumentT);

            Astrionalis = Content.Load<Texture2D>("other/astrionalis");
            Celestrioris = Content.Load<Texture2D>("other/celestrioris");
            
            CursorT = Content.Load<Texture2D>("tiles/cursor");
            GuideT = Content.Load<Texture2D>("icons/moveguide");
            HealthGuiT = Content.Load<Texture2D>("icons/healthgui");

            SanctumT = Content.Load<Texture2D>("locationtiles/sanctum");
            TileAtlas.Add("sanctum", SanctumT);

            ArchitectHere = Content.Load<Texture2D>("distmap/architecthere");
            BleedT = Content.Load<Texture2D>("icons/droplet");

            whiteRect = Content.Load<Texture2D>("other/pixel");
            ReactionGUIT = Content.Load<Texture2D>("gui/reaction gui");
            MessageGUIT = Content.Load<Texture2D>("gui/messageGUI");
            CmdHelpT = Content.Load<Texture2D>("gui/cmdhelp");
            LightrealmMainTheme = Content.Load<Song>("audio/lightrealm main theme (2023)");

            CharacterAtlas["amulet"] = AmuletT = Content.Load<Texture2D>("character art/amulet");
            CharacterAtlas["archaixfemale"] = ArchaixFemaleT = Content.Load<Texture2D>("character art/archaixfemale");
            CharacterAtlas["archaixmale"] = ArchaixMaleT = Content.Load<Texture2D>("character art/archaixmale");
            CharacterAtlas["cape"] = CapeT = Content.Load<Texture2D>("character art/cape");
            CharacterAtlas["chestplate"] = ChestplateT = Content.Load<Texture2D>("character art/chestplate");
            CharacterAtlas["flair"] = FlairT = Content.Load<Texture2D>("character art/flair");
            CharacterAtlas["helmet"] = HelmetT = Content.Load<Texture2D>("character art/helmet");
            CharacterAtlas["hood"] = HoodT = Content.Load<Texture2D>("character art/hood");
            CharacterAtlas["kilt"] = KiltT = Content.Load<Texture2D>("character art/kilt");
            CharacterAtlas["large hat"] = LargeHatT = Content.Load<Texture2D>("character art/large hat");
            CharacterAtlas["left boot"] = LeftBootT = Content.Load<Texture2D>("character art/left boot");
            CharacterAtlas["left gauntlet"] = LeftGauntletT = Content.Load<Texture2D>("character art/left gauntlet");
            CharacterAtlas["left glove"] = LeftGloveT = Content.Load<Texture2D>("character art/left glove");
            CharacterAtlas["left shoe"] = LeftShoeT = Content.Load<Texture2D>("character art/left shoe");
            CharacterAtlas["left wristwrap"] = LeftWristwrapT = Content.Load<Texture2D>("character art/left wristwrap");
            CharacterAtlas["leggings"] = LeggingsT = Content.Load<Texture2D>("character art/leggings");
            CharacterAtlas["longsleeve shirt female"] = LongsleeveShirtFemaleT = Content.Load<Texture2D>("character art/longsleeve shirt female");
            CharacterAtlas["longsleeve shirt male"] = LongsleeveShirtMaleT = Content.Load<Texture2D>("character art/longsleeve shirt male");
            CharacterAtlas["luminarchfemale"] = LuminarchFemaleT = Content.Load<Texture2D>("character art/luminarchfemale");
            CharacterAtlas["luminarchmale"] = LuminarchMaleT = Content.Load<Texture2D>("character art/luminarchmale");
            CharacterAtlas["nightfellfemale"] = NightfellFemaleT = Content.Load<Texture2D>("character art/nightfellfemale");
            CharacterAtlas["nightfellmale"] = NightfellMaleT = Content.Load<Texture2D>("character art/nightfellmale");
            CharacterAtlas["pants"] = PantsT = Content.Load<Texture2D>("character art/pants");
            CharacterAtlas["right boot"] = RightBootT = Content.Load<Texture2D>("character art/right boot");
            CharacterAtlas["right gauntlet"] = RightGauntletT = Content.Load<Texture2D>("character art/right gauntlet");
            CharacterAtlas["right glove"] = RightGloveT = Content.Load<Texture2D>("character art/right glove");
            CharacterAtlas["right shoe"] = RightShoeT = Content.Load<Texture2D>("character art/right shoe");
            CharacterAtlas["right wristwrap"] = RightWristwrapT = Content.Load<Texture2D>("character art/right wristwrap");
            CharacterAtlas["robe female"] = RobeFemaleT = Content.Load<Texture2D>("character art/robe female");
            CharacterAtlas["robe male"] = RobeMaleT = Content.Load<Texture2D>("character art/robe male");
            CharacterAtlas["shorts"] = ShortsT = Content.Load<Texture2D>("character art/shorts");
            CharacterAtlas["shortsleeve shirt female"] = ShortsleeveShirtFemaleT = Content.Load<Texture2D>("character art/shortsleeve shirt female");
            CharacterAtlas["shortsleeve shirt male"] = ShortsleeveShirtMaleT = Content.Load<Texture2D>("character art/shortsleeve shirt male");
            CharacterAtlas["skirt"] = SkirtT = Content.Load<Texture2D>("character art/skirt");
            CharacterAtlas["small hat"] = SmallHatT = Content.Load<Texture2D>("character art/small hat");
            CharacterAtlas["straps"] = StrapsT = Content.Load<Texture2D>("character art/straps");
            CharacterAtlas["undergarment"] = UndergarmentT = Content.Load<Texture2D>("character art/undergarment");
            CharacterAtlas["uppergarment"] = UpperGarmentT = Content.Load<Texture2D>("character art/uppergarment");
            CharacterAtlas["uppershirt female"] = UpperShirtFemaleT = Content.Load<Texture2D>("character art/uppershirt female");
            CharacterAtlas["uppershirt male"] = UpperShirtMaleT = Content.Load<Texture2D>("character art/uppershirt male");
            CharacterAtlas["wraps"] = WrapsT = Content.Load<Texture2D>("character art/wraps");

            BaseRepositionGUIT = Content.Load<Texture2D>("repositiongui/basegui");
            BodyFrameT = Content.Load<Texture2D>("repositiongui/full");
            HeadRepT = Content.Load<Texture2D>("repositiongui/head");
            LeftArmRepT = Content.Load<Texture2D>("repositiongui/leftarm");
            RightArmRepT = Content.Load<Texture2D>("repositiongui/rightarm");
            LeftFootRepT = Content.Load<Texture2D>("repositiongui/leftfoot");
            LeftHandRepT = Content.Load<Texture2D>("repositiongui/lefthand");
            RightHandRepT = Content.Load<Texture2D>("repositiongui/righthand");
            RightFootLeftT = Content.Load<Texture2D>("repositiongui/rightfoot");
            RightLegT = Content.Load<Texture2D>("repositiongui/rightleg");
            LeftLegT = Content.Load<Texture2D>("repositiongui/leftleg");
            TorsoT = Content.Load<Texture2D>("repositiongui/torso");

            MirrorT = Content.Load<Texture2D>("character art/mirror");

            // Define the file path for 'recipes.txt'
            string filePath = Path.Combine(dataPath, "recipes.txt");

            // Check if the file exists
            if (File.Exists(filePath))
            {
                // Read all lines from the file
                string[] lines = File.ReadAllLines(filePath);

                // Initialize a list to store the parsed data, assuming 'Recipes' is already defined
                Recipes = new List<(string, List<string>)>();

                // Loop through each line
                foreach (string line in lines)
                {
                    // Split the line into parts based on commas
                    string[] parts = line.Split(',');

                    // Check if there are any parts to process
                    if (parts.Length > 0)
                    {
                        string id = parts[0].Trim();  // Trim spaces around the ID
                        List<string> materials = new List<string>();

                        // Loop through each part after the ID to get materials
                        for (int i = 1; i < parts.Length; i++)
                        {
                            materials.Add(parts[i].Trim()); // Trim spaces around each material
                        }

                        // Add the ID and materials to the list of recipes
                        Recipes.Add((id, materials));
                    }
                }
            }
            else
            {
                // Handle the case where the file does not exist
                Console.WriteLine("Recipes file not found.");
            }
        }

        protected override void Update(GameTime gameTime)
        {
            GameInput.Update();

            if (GameInput.WasKeyPressed(FrameCounter.Key))
            {
                FrameCounter.RenderFps = !FrameCounter.RenderFps;
            }
            FrameCounter.Update(gameTime);

            if (!AllEnteredGameStates.Contains(GameState))
            {
                AllEnteredGameStates.Add(GameState);
            }
            if (Keyboard.GetState().IsKeyDown(Keys.F9))
            {
                AllEnteredGameStates.Clear();
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                EscapeTicks++;
                if (EscapeTicks > 100)
                {
                    Exit();
                }
            }
            else
            {
                EscapeTicks = 0;
            }


            //BEFORE WE DO ANYTHING, FIX THE LOADED ARCHITECT THINGY


            void IncrementAndCycleWorld()
            {
                ArchitectIndex++;

                if (ArchitectIndex >= LoadedArchitects.Count)
                {
                    ArchitectIndex = 0;

                    foreach (Architect a in LoadedArchitectsToRemove)
                    {
                        LoadedArchitects.Remove(a);
                    }
                    LoadedArchitectsToRemove = new List<Architect>();

                    TicksSinceLoad++; // Increment the game cycle count
                    GameWorld.Cycle++; // Increment the world cycle
                    UpdateNonPlayerWorld(); // Update the non-player world
                }

                if (GamePlayerParty.Architects.All(architect => !architect.IsAlive))
                {
                    GameState = "dead";
                }
                else if (LoadedArchitects.Count == 1)
                {
                    ArchitectIndex = 0;

                    foreach (Architect a in LoadedArchitectsToRemove)
                    {
                        LoadedArchitects.Remove(a);
                    }
                    LoadedArchitectsToRemove = new List<Architect>();

                    TicksSinceLoad++; // Increment the game cycle count
                    GameWorld.Cycle++; // Increment the world cycle
                    UpdateNonPlayerWorld(); // Update the non-player world
                }
            }


            void UpdateNonPlayerWorld()
            {
                if(LoadedArchitects.Count == 0)
                {
                    return;
                }

                void HandleThrownObjects(List<Object> objects, List<Architect> architects)
                {
                    List<Object> objectsToRemove = new List<Object>();

                    foreach (Object o in objects)
                    {
                        if (o.AirborneTarget != null)
                        {
                            if (o.AirborneCyclesToHitTarget > 0)
                            {
                                o.AirborneCyclesToHitTarget--;
                                continue;
                            }
                            else
                            {
                                if (o.AirborneTarget is Architect targetArchitect)
                                {
                                    if (o.Materials.Contains(GameWorld.Spectre))
                                    {
                                        o.Dissipating = true;
                                        targetArchitect.Energy -= (((Architect)(o.Creator)).Focus * 2) + 10;
                                        Observations.Add(new TextStorage(targetArchitect.ReferredToNames[0] + " is enveloped in souls!", Color.Orange, new List<Entity>() { targetArchitect }));

                                        if (targetArchitect.Energy <= 0)
                                        {
                                            Announcements.Add(new TextStorage(targetArchitect.ReferredToNames[0] + " dies, and rises with a putrid, dark energy!", Color.Purple, new List<Entity>() { targetArchitect }));
                                            ReviveAndTransformToShade(targetArchitect, r, ((Architect)(o.Creator)));
                                        }
                                    }
                                    else
                                    {
                                        HandlePhysicalCollision(targetArchitect, o, r);
                                    }
                                }
                                else // Non-Architect airborne targets handling
                                {
                                    HandleObjectCollision(o, r);
                                }

                                o.AirborneTarget = null;

                                // Grenade effects
                                HandleGrenadeEffects(o, architects, objects, r);
                            }
                        }

                        if (o.Dissipating)
                        {
                            objectsToRemove.Add(o);
                        }
                    }

                    foreach (var obj in objectsToRemove)
                    {
                        objects.Remove(obj);
                    }
                }

                void HandleGrenadeEffects(Object o, List<Architect> architects, List<Object> objects, Random r)
                {
                    if (o.Type == "spatial grenade")
                    {
                        MakeObservation("The grenade explodes into a portal, sucking everything in!", Color.Purple, new List<Entity>());
                        foreach (Architect a in architects)
                        {
                            a.IsAlive = false;
                            if (GamePlayerParty.Architects.Contains(a))
                            {
                                GamePlayerParty.Architects.Remove(a);

                                if (GamePlayerParty.Architects.Count == 0)
                                {
                                    GameState = "dead";
                                }
                            }
                            MakeObservation(a.ReferredToNames[0] + " has been consumed by the portal.", Color.Purple, new List<Entity>() { a });
                        }
                        objects.Clear();
                        architects.Clear();
                    }
                    else if (o.Type == "lightning grenade")
                    {
                        MakeObservation("The grenade explodes into a swarm of lightning, striking everything around!", Color.Purple, new List<Entity>());
                        foreach (Architect a in architects)
                        {
                            a.IsAlive = false;
                            if (GamePlayerParty.Architects.Contains(a))
                            {
                                GamePlayerParty.Architects.Remove(a);

                                if (GamePlayerParty.Architects.Count == 0)
                                {
                                    GameState = "dead";
                                }
                            }
                            MakeObservation(a.ReferredToNames[0] + " is overwhelmed by lightning.", Color.Purple, new List<Entity>() { a });
                        }
                        // Assuming there's no need to clear objects here as they're not destroyed by lightning
                    }
                    // Other grenade types can be added here
                }

                void HandlePhysicalCollision(Architect target, Object o, Random r)
                {
                    if (!o.Materials.Contains(GameWorld.Spectre))
                    {
                        target.CombatCycles = 100;
                        ((Architect)(o.Thrower)).CombatCycles = 100;

                        Object ArchitectBodyPart = target.BodyParts[r.Next(target.BodyParts.Count)];

                        int InitialStagnantObjectIntegrity = ArchitectBodyPart.Integrity;
                        int InitialThrowingObjectIntegrity = o.Integrity;

                        Observations.Add(new TextStorage("The " + o.ReferredToNames[0] + " has collided into " + ArchitectBodyPart.ReferredToNames[0] + "!", Color.Orange, new List<Entity>() { o, ArchitectBodyPart }));

                        Announcements.AddRange(ArchitectBodyPart.TakeDamageFromObject(o, 2 * (o.Thrower.Dexterity + o.Thrower.GetProficiency("throwing")), null, "")); // Assuming a method exists to handle this

                        if (o.Type == "falling star" && ((Architect)(o.Creator)).PathOfStarsLevel > 4)
                        {
                            Observations.Add(new TextStorage(ArchitectBodyPart.ReferredToNames[0] + " bursts into flames!", Color.Orange, new List<Entity>() { ArchitectBodyPart }));
                            target.FireCycles += r.Next(30, 100);
                        }
                        else if (o.Materials.Contains(GameWorld.Flame))
                        {
                            if (((Architect)(o.Creator)).PathOfHeatLevel >= 8 && ((Architect)(o.Creator)).FireCycles > 0)
                            {
                                target.FireCycles += r.Next(40, 80 + (((Architect)(o.Creator)).FireCycles) / 30);
                            }
                            else if (((Architect)(o.Creator)).PathOfHeatLevel >= 6)
                            {
                                target.FireCycles += r.Next(40, 80);
                            }
                            else
                            {
                                target.FireCycles += r.Next(20, 40);
                            }
                        }

                        if (o.Integrity < 0 && InitialThrowingObjectIntegrity > 0)
                        {
                            Observations.Add(new TextStorage("The " + o.ReferredToNames[0] + " has been destroyed!", Color.Orange, new List<Entity>() { o }));
                        }
                        if (ArchitectBodyPart.Integrity < 0 && InitialStagnantObjectIntegrity > 0)
                        {
                            Observations.Add(new TextStorage("The " + ArchitectBodyPart.ReferredToNames[0] + " has been destroyed!", Color.Orange, new List<Entity>() { ArchitectBodyPart }));
                        }
                    }
                }

                void HandleObjectCollision(Object o, Random r)
                {
                    if (o.AirborneTarget is Object targetObject)
                    {
                        int InitialStagnantObjectIntegrity = targetObject.Integrity;
                        int InitialThrowingObjectIntegrity = o.Integrity;

                        targetObject.TakeDamageFromObject(o, o.Thrower.GetProficiency("throwing") + 3, null, ""); // Simulating damage application

                        o.Integrity = InitialThrowingObjectIntegrity - InitialStagnantObjectIntegrity;
                        targetObject.Integrity = InitialThrowingObjectIntegrity - InitialStagnantObjectIntegrity;

                        Observations.Add(new TextStorage("The " + o.ReferredToNames[0] + " has collided into " + targetObject.ReferredToNames[0] + "!", Color.Orange, new List<Entity>() { o, targetObject }));

                        if (o.Integrity < 0 && InitialThrowingObjectIntegrity > 0)
                        {
                            Observations.Add(new TextStorage("The " + o.ReferredToNames[0] + " has been destroyed!", Color.Orange, new List<Entity>() { o }));
                        }
                        if (targetObject.Integrity < 0 && InitialStagnantObjectIntegrity > 0)
                        {
                            Observations.Add(new TextStorage("The " + targetObject.ReferredToNames[0] + " has been destroyed!", Color.Orange, new List<Entity>() { targetObject }));
                        }
                    }
                }

                void ReviveAndTransformToShade(Architect architect, Random r, Architect creator)
                {
                    architect.IsAlive = true;
                    architect.IsImmortal = true;
                    architect.Race = GameWorld.GetRace("shade");
                    architect.OppositionTags.Add("alllife");
                    architect.Energy = 50;

                    int baseMaxEnergy = architect.MaxEnergy(); // This gets the original MaxEnergy value before modification
                    int maxNegativeModAllowed = -baseMaxEnergy; // This is the maximum negative modifier that won't make MaxEnergy negative

                    // Ensure MaxEnergyMod cannot make MaxEnergy go below 0, and also apply your original constraint of not going below -50

                    int maxEnergyMod = Math.Max(maxNegativeModAllowed, -50);

                    architect.MaxEnergyMod = maxEnergyMod;
                    architect.UndeadCreator = creator;

                    foreach (Object O in architect.BodyParts)
                    {
                        O.Integrity = Math.Max(25, O.Integrity);
                    }

                    for (int i = r.Next(1, 4); i != 0; i--)
                    {
                        architect.AddBodyParts(); // Assuming a method exists for this
                    }

                    if (GamePlayerParty.Architects.Contains(creator))
                    {
                        GamePlayerParty.Architects.Add(architect);
                    }
                }


                // Iterate through blocks
                for (int x = 0; x < 7; x++)
                {
                    for (int z = 0; z < 7; z++)
                    {
                        var block = LoadedArchitects[ArchitectIndex].District.DistrictMap[x + z * 7];
                        HandleThrownObjects(block.Objects, block.Architects);
                    }
                }

                // Iterate through rooms in structures
                foreach (var block in LoadedArchitects[ArchitectIndex].District.DistrictMap)
                {
                    foreach (var structure in block.Structures)
                    {
                        foreach (var room in structure.Rooms)
                        {
                            HandleThrownObjects(room.Objects, room.Architects);
                        }
                    }
                }


                //return fractal objects to reality/updatt them

                List<Object> FractalObjectsToRemove = new List<Object>();

                foreach (Object o in GameWorld.FractalObjects)
                {
                    o.FractalCycles--;
                    if (o.FractalCycles < 1)
                    {
                        o.Block = o.RematerializeLocation.Item4;
                        o.Room = o.RematerializeLocation.Item6;

                        if (o.Room != null)
                        {
                            o.Room.Objects.Add(o);
                        }
                        else
                        {
                            o.Block.Objects.Add(o);
                        }

                        if (LoadedArchitects[ArchitectIndex].Room == o.Room && LoadedArchitects[ArchitectIndex].Block == o.Block)
                        {
                            MakeObservation(o.ReferredToNames[0] + " has rematerialized!", Color.Blue, new List<Entity>() { o });
                        }

                        FractalObjectsToRemove.Add(o);
                    }
                }
                foreach (Object o in FractalObjectsToRemove)
                {
                    GameWorld.FractalObjects.Remove(o);
                }
                List<Architect> FractalArchitectsToRemove = new List<Architect>();
                foreach (Architect a in GameWorld.FractalArchitects)
                {
                    a.FractalCycles--;
                    if (a.FractalCycles < 1)
                    {
                        if (a.RematerializeLocation.Item3.IsLoaded)
                        {
                            a.Location = a.RematerializeLocation.Item2;
                            a.District = a.RematerializeLocation.Item3;
                            a.Block = a.RematerializeLocation.Item4;
                            a.Structure = a.RematerializeLocation.Item5;
                            a.Room = a.RematerializeLocation.Item6;

                            if (a.RematerializeLocation.Item6 != null)
                            {
                                a.Room.Architects.Add(a);
                            }
                            else
                            {
                                a.Block.Architects.Add(a);
                            }

                            FractalArchitectsToRemove.Add(a);

                            if (LoadedArchitects[ArchitectIndex].Room == a.Room && LoadedArchitects[ArchitectIndex].Block == a.Block)
                            {
                                MakeObservation(a.ReferredToNames[0] + " has rematerialized!", Color.Blue, new List<Entity>() { a });
                            }
                        }
                        else
                        {
                            a.Location = a.RematerializeLocation.Item2;
                            a.District = a.RematerializeLocation.Item3;
                            a.District.Architects.Add(a);
                        }
                    }
                }
                foreach (Architect a in FractalArchitectsToRemove)
                {
                    GameWorld.FractalArchitects.Remove(a);
                }


                //change the random int
                GameWorld.ReactionModifierInt = r.Next(0, 100000);
            }


            FlashTick++;
            if (FlashTick > 99)
            {
                FlashTick = 0;
            }

            //update the Newly Pressed Key List
            KeysNewlyPressed = Keyboard.GetState().GetPressedKeys().ToList<Keys>();

            List<Keys> KeysToRemove = new List<Keys>();
            foreach (Keys key in KeysNewlyPressed)
            {
                if (previousState.IsKeyDown(key))
                {
                    KeysToRemove.Add(key);
                }
            }
            foreach (Keys key in KeysToRemove)
            {
                KeysNewlyPressed.Remove(key);
            }

            //audio

            if (GameState == "mainscreen" || GameState == "generatingworld" || GameState == "worldgenscreen" || GameState == "placecivilizations" || GameState == "loadinggamemenu" || GameState == "savinggamemenu" || GameState == "generatehistory" || GameState == "choosepreferences" || GameState == "findstartlocation" || GameState == "architectfound")
            {
                if (MediaPlayer.Queue.ActiveSong == null)
                {
                    MediaPlayer.Play(LightrealmMainTheme);
                }

                if (MediaPlayer.Volume < 1 && Mute == false)
                {
                    MediaPlayer.Volume = MediaPlayer.Volume + 0.004F;
                }
                else if (MediaPlayer.Volume > 0 && Mute == true)
                {
                    MediaPlayer.Volume = MediaPlayer.Volume -= 0.02F;
                }

                if (KeysNewlyPressed.Contains(Keys.M))
                {
                    if (Mute)
                    {
                        Mute = false;
                    }
                    else
                    {
                        Mute = true;
                    }
                }
            }
            else if (GameState == "partyturn" || GameState == "travelmenu")
            {
                if (MediaPlayer.Volume > 0)
                {
                    MediaPlayer.Volume = MediaPlayer.Volume - 0.004F;
                }
            }

            void SetUpRelevantEntities(List<string> subjects)
            {
                foreach (var subject in subjects)
                {
                    switch (subject)
                    {
                        case "entity":
                            RelevantEntities.AddRange(AllSubjects.OfType<Entity>());
                            break;
                        case "architect":
                            RelevantEntities.AddRange(AllSubjects.OfType<Architect>());
                            break;
                        case "object":
                            RelevantEntities.AddRange(AllSubjects.OfType<Object>());
                            break;
                        case "door":
                            RelevantEntities.AddRange(AllSubjects.OfType<Door>());
                            break;
                        case "structure":
                            RelevantEntities.AddRange(AllSubjects.OfType<Structure>());
                            break;
                        case "nearby_architect":
                            RelevantEntities.AddRange(AllSubjects.OfType<Architect>().Where(a => MostRecentPartyTurnArchitect.Block.Architects.Contains(a) || (MostRecentPartyTurnArchitect.Room != null && MostRecentPartyTurnArchitect.Room.Architects.Contains(a))));
                            break;
                        case "nearby_object":
                            RelevantEntities.AddRange(AllSubjects.OfType<Object>().Where(o => MostRecentPartyTurnArchitect.Block.Objects.Contains(o) || (MostRecentPartyTurnArchitect.Room != null && MostRecentPartyTurnArchitect.Room.Objects.Contains(o))));
                            break;
                        case "nearby_target":
                            RelevantEntities.AddRange(AllSubjects.OfType<Architect>().Where(a => MostRecentPartyTurnArchitect.Block.Architects.Contains(a) || (MostRecentPartyTurnArchitect.Room != null && MostRecentPartyTurnArchitect.Room.Architects.Contains(a))));
                            RelevantEntities.AddRange(AllSubjects.OfType<Object>().Where(o => MostRecentPartyTurnArchitect.Block.Objects.Contains(o) || (MostRecentPartyTurnArchitect.Room != null && MostRecentPartyTurnArchitect.Room.Objects.Contains(o))));
                            if (MostRecentPartyTurnArchitect.LeftHandObject != null)
                            {
                                RelevantEntities.Add(MostRecentPartyTurnArchitect.LeftHandObject);
                            }
                            if (MostRecentPartyTurnArchitect.RightHandObject != null)
                            {
                                RelevantEntities.Add(MostRecentPartyTurnArchitect.RightHandObject);
                            }
                            RelevantEntities.AddRange(MostRecentPartyTurnArchitect.Inventory);
                            break;
                        case "nearby_structure":
                            RelevantEntities.AddRange(AllSubjects.OfType<Structure>().Where(s => MostRecentPartyTurnArchitect.Block.Structures.Contains(s)));
                            break;
                        case "composition_types":
                            RelevantEntities.AddRange(AllSubjects.Where(e => e.Metadata == "book" || e.Metadata == "song" || e.Metadata == "poem"));
                            break;
                        case "hand_object":
                            if (MostRecentPartyTurnArchitect.LeftHandObject != null)
                            {
                                RelevantEntities.Add(MostRecentPartyTurnArchitect.LeftHandObject);
                            }
                            if (MostRecentPartyTurnArchitect.RightHandObject != null)
                            {
                                RelevantEntities.Add(MostRecentPartyTurnArchitect.RightHandObject);
                            }
                            break;
                        case "non_hand_inventory":
                            RelevantEntities.AddRange(MostRecentPartyTurnArchitect.Inventory);
                            break;
                        case "inventory":
                            if (MostRecentPartyTurnArchitect.LeftHandObject != null)
                            {
                                RelevantEntities.Add(MostRecentPartyTurnArchitect.LeftHandObject);
                            }
                            if (MostRecentPartyTurnArchitect.RightHandObject != null)
                            {
                                RelevantEntities.Add(MostRecentPartyTurnArchitect.RightHandObject);
                            }
                            RelevantEntities.AddRange(MostRecentPartyTurnArchitect.Inventory);
                            break;
                        case "corpse":
                            RelevantEntities.AddRange(AllSubjects.OfType<Architect>().Where(a => (MostRecentPartyTurnArchitect.Block.Architects.Contains(a) || (MostRecentPartyTurnArchitect.Room != null && MostRecentPartyTurnArchitect.Room.Architects.Contains(a))) && !a.IsAlive));
                            break;
                        case "spell":
                            RelevantEntities.AddRange(AllSpells.Select(spell => new Entity(spell)));
                            break;
                        case "skill":
                            RelevantEntities.AddRange(AllSkills.Select(skill => new Entity(skill)));
                            break;
                        case "direction":
                            RelevantEntities.AddRange(AllSubjects.Where(e => e.Metadata == "north" || e.Metadata == "south" || e.Metadata == "east" || e.Metadata == "west" || e.Metadata == "northeast" || e.Metadata == "southeast" || e.Metadata == "southwest" || e.Metadata == "northwest"));
                            break;
                        case "body_part_type":
                            RelevantEntities.AddRange(GameWorld.Races.SelectMany(race => race.BodyParts.Select(bp => bp.Item1)).Distinct().Select(bp => AllSubjects.FirstOrDefault(e => e.Metadata == bp)));
                            break;
                        case "clothing":
                            RelevantEntities.AddRange(MostRecentPartyTurnArchitect.Clothing);
                            break;
                        case "rememberance":
                            RelevantEntities.AddRange(AllSubjects.Where(e => e.Metadata == "spells" || e.Metadata == "skills"));
                            break;
                        case "composition_object_types":
                            RelevantEntities.AddRange(AllSubjects.Where(e => e.Metadata == "book" || e.Metadata == "scroll" || e.Metadata == "waxtablet" || e.Metadata == "sheet"));
                            break;
                        case "enterable":
                            RelevantEntities.AddRange(AllSubjects.OfType<Door>().Where(d => MostRecentPartyTurnArchitect.Block.Objects.Contains(d) || (MostRecentPartyTurnArchitect.Room != null && MostRecentPartyTurnArchitect.Room.Objects.Contains(d))));
                            RelevantEntities.AddRange(AllSubjects.OfType<Structure>().Where(s => MostRecentPartyTurnArchitect.Block.Structures.Contains(s)));
                            RelevantEntities.AddRange(AllSubjects.OfType<Object>().Where(o => o.Type == "exit door" && (MostRecentPartyTurnArchitect.Block.Objects.Contains(o) || (MostRecentPartyTurnArchitect.Room != null && MostRecentPartyTurnArchitect.Room.Objects.Contains(o)))));
                            break;


                        default:
                            break;
                    }

                }
            }


            void ResetCommandBuilder()
            {
                CommandBuilderStage = "none";
                SelectedCategory = "";
                SelectedCommand = "";
                CurrentSubjectIndex = 0;
                RelevantEntities.Clear();
                SelectedEntities.Clear();
            }

            //everything else lul

            if (WaitingTicks > 0)
            {
                WaitingTicks = WaitingTicks - 1;
            }
            else
            {
                if (WaitingTicks == 0)
                {
                    if (GameState == "mainscreen")
                    {
                        if (KeysNewlyPressed.Contains(Keys.C))
                        {
                            GameState = "worldgenscreen";
                            GameMode = "chronicle";
                        }
                        /*
                        else if (KeysNewlyPressed.Contains(Keys.F))
                        {
                            GameState = "generatingworld";
                            GameMode = "founder";
                        }
                        */
                        if ((Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.RightControl)) && Keyboard.GetState().IsKeyDown(Keys.L))
                        {
                            LoadTicks++;
                            if (LoadTicks > 100)
                            {
                                GameState = "loadinggamemenu";
                            }
                        }
                        else
                        {
                            LoadTicks = 0;
                        }
                    }
                    else if (GameState == "worldgenscreen")
                    {
                        //switch to generatingworld

                        if (KeysNewlyPressed.Contains(Keys.Enter))
                        {
                            GameState = "generatingworld";
                        }

                        if (KeysNewlyPressed.Contains(Keys.Q))
                        {
                            ViewMessageForCustom = true;
                            switch (CurrentlySelectedWorldAge)
                            {
                                case 100:
                                    CurrentlySelectedWorldAge = 150;
                                    break;
                                case 150:
                                    CurrentlySelectedWorldAge = 200;
                                    break;
                                case 200:
                                    CurrentlySelectedWorldAge = 250;
                                    break;
                                case 250:
                                    CurrentlySelectedWorldAge = 300;
                                    break;
                                case 300:
                                    CurrentlySelectedWorldAge = 350;
                                    break;
                                case 350:
                                    CurrentlySelectedWorldAge = 400;
                                    break;
                                case 400:
                                    CurrentlySelectedWorldAge = 450;
                                    break;
                                case 450:
                                    CurrentlySelectedWorldAge = 500;
                                    break;
                                case 500:
                                    CurrentlySelectedWorldAge = 10000; // Until Stopped
                                    break;
                                case 10000:
                                    CurrentlySelectedWorldAge = 100; // Wrap around to start
                                    break;
                            }
                        }

                        if (KeysNewlyPressed.Contains(Keys.A))
                        {
                            ViewMessageForCustom = true;
                            switch (CurrentlySelectedWorldAge)
                            {
                                case 150:
                                    CurrentlySelectedWorldAge = 100;
                                    break;
                                case 200:
                                    CurrentlySelectedWorldAge = 150;
                                    break;
                                case 250:
                                    CurrentlySelectedWorldAge = 200;
                                    break;
                                case 300:
                                    CurrentlySelectedWorldAge = 250;
                                    break;
                                case 350:
                                    CurrentlySelectedWorldAge = 300;
                                    break;
                                case 400:
                                    CurrentlySelectedWorldAge = 350;
                                    break;
                                case 450:
                                    CurrentlySelectedWorldAge = 400;
                                    break;
                                case 500:
                                    CurrentlySelectedWorldAge = 450;
                                    break;
                                case 10000: // Until Stopped
                                    CurrentlySelectedWorldAge = 500;
                                    break;
                                case 100:
                                    CurrentlySelectedWorldAge = 10000; // Wrap around to end
                                    break;
                            }
                        }

                        if (KeysNewlyPressed.Contains(Keys.W))
                        {
                            CurrentlySelectedGrievanceType++;
                            if (CurrentlySelectedGrievanceType >= ThreatTypes.Count) // Check if index exceeds list length
                            {
                                CurrentlySelectedGrievanceType = 0; // Wrap around to the start
                            }
                        }

                        if (KeysNewlyPressed.Contains(Keys.S))
                        {
                            CurrentlySelectedGrievanceType--;
                            if (CurrentlySelectedGrievanceType < 0) // Check if index goes below 0
                            {
                                CurrentlySelectedGrievanceType = ThreatTypes.Count - 1; // Wrap around to the end
                            }
                        }
                        // Increment the Number of Civilizations with 'E'
                        if (KeysNewlyPressed.Contains(Keys.E))
                        {
                            NumberOfCivilizations++;
                            if (NumberOfCivilizations > 16) // Cap at 16
                            {
                                NumberOfCivilizations = 16;
                            }
                        }

                        // Decrement the Number of Civilizations with 'D'
                        if (KeysNewlyPressed.Contains(Keys.D))
                        {
                            NumberOfCivilizations--;
                            if (NumberOfCivilizations < 8) // Cap at 8
                            {
                                NumberOfCivilizations = 8;
                            }
                        }

                        // Increment the Prosperity Multiplier with 'R'
                        if (KeysNewlyPressed.Contains(Keys.R))
                        {
                            ViewMessageForCustom = true;
                            double increment = KeysNewlyPressed.Contains(Keys.LeftShift) || KeysNewlyPressed.Contains(Keys.RightShift) ? 1.0 : 0.1;
                            ProsperityMultiplier += increment;
                            if (ProsperityMultiplier > 5.0) // Cap at 5.0
                            {
                                ProsperityMultiplier = 5.0;
                            }
                        }

                        // Decrement the Prosperity Multiplier with 'F'
                        if (KeysNewlyPressed.Contains(Keys.F))
                        {
                            ViewMessageForCustom = true;
                            double decrement = KeysNewlyPressed.Contains(Keys.LeftShift) || KeysNewlyPressed.Contains(Keys.RightShift) ? 1.0 : 0.1;
                            ProsperityMultiplier -= decrement;
                            if (ProsperityMultiplier < 0.0) // Cap at 0.0
                            {
                                ProsperityMultiplier = 0.0;
                            }
                        }

                        /*
                        // Increment the World Width with 'T'
                        if (KeysNewlyPressed.Contains(Keys.T))
                        {
                            CurrentlySelectedWorldWidth += 8;
                            if (CurrentlySelectedWorldWidth > 128) // Cap at 128
                            {
                                CurrentlySelectedWorldWidth = 128;
                            }
                        }

                        // Decrement the World Width with 'G'
                        if (KeysNewlyPressed.Contains(Keys.G))
                        {
                            CurrentlySelectedWorldWidth -= 8;
                            if (CurrentlySelectedWorldWidth < 32) // Cap at 32
                            {
                                CurrentlySelectedWorldWidth = 32;
                            }
                        }

                        // Increment the World Length with 'Y'
                        if (KeysNewlyPressed.Contains(Keys.Y))
                        {
                            CurrentlySelectedWorldLength += 8;
                            if (CurrentlySelectedWorldLength > 128) // Cap at 128
                            {
                                CurrentlySelectedWorldLength = 128;
                            }
                        }

                        // Decrement the World Length with 'H'
                        if (KeysNewlyPressed.Contains(Keys.H))
                        {
                            CurrentlySelectedWorldLength -= 8;
                            if (CurrentlySelectedWorldLength < 32) // Cap at 32
                            {
                                CurrentlySelectedWorldLength = 32;
                            }
                        }
                        */
                    }

                    bool IsCurrentVersion(string directory)
                    {
                        var versionFilePath = Path.Combine(directory, "version.txt");
                        if (File.Exists(versionFilePath))
                        {
                            string firstLine = File.ReadLines(versionFilePath).FirstOrDefault();
                            return firstLine == Version;
                        }
                        return false;
                    }


                    if (GameState == "savinggame")
                    {
                        SaveGame(GamePlayerParty, GameWorld);
                        GameState = "mainscreen";
                    }
                    else if (GameState == "loadinggamemenu")
                    {
                        var saveDirectories = Directory.GetDirectories(DocumentsFolderPath + "/LightrealmSaves").ToList();
                        int SavesCount = saveDirectories.Count;

                        if (SavesCount > 0)
                        {
                            if (KeysNewlyPressed.Contains(Keys.Up))
                            {
                                do
                                {
                                    LoadGameCursor--;
                                    if (LoadGameCursor < 0)
                                    {
                                        LoadGameCursor = SavesCount - 1;
                                    }
                                } while (!IsCurrentVersion(saveDirectories[LoadGameCursor]));
                            }

                            if (KeysNewlyPressed.Contains(Keys.Down))
                            {
                                do
                                {
                                    LoadGameCursor++;
                                    if (LoadGameCursor >= SavesCount)
                                    {
                                        LoadGameCursor = 0;
                                    }
                                } while (!IsCurrentVersion(saveDirectories[LoadGameCursor]));
                            }

                            if (KeysNewlyPressed.Contains(Keys.Enter))
                            {
                                GameState = "loadinggame";
                                SelectedDirectory = saveDirectories[LoadGameCursor];
                            }

                            if (KeysNewlyPressed.Contains(Keys.Delete))
                            {
                                GameState = "deletinggame";
                                SelectedDirectory = saveDirectories[LoadGameCursor];
                            }
                        }

                        if (KeysNewlyPressed.Contains(Keys.Escape))
                        {
                            GameState = "mainscreen";
                        }
                    }

                    else if (GameState == "deletinggame")
                    {
                        if (KeysNewlyPressed.Contains(Keys.Y) && (Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.RightControl)))
                        {
                            string[] files = Directory.GetFiles(SelectedDirectory);

                            LoadGameCursor = 0;

                            foreach (string file in files)
                            {
                                File.Delete(file);
                            }

                            Directory.Delete(SelectedDirectory);
                            GameState = "loadinggamemenu";
                        }
                        else if (KeysNewlyPressed.Contains(Keys.N) && (Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.RightControl)))
                        {
                            GameState = "loadinggamemenu";
                        }
                    }
                    else if (GameState == "loadinggame")
                    {
                        LoadGame(SelectedDirectory);
                        GameState = "partyturn";
                    }
                    else if (GameState == "generatingworld")
                    {
                        GameWorld = new World(CurrentlySelectedWorldWidth, CurrentlySelectedWorldLength, NumberOfCivilizations - 4, CurrentlySelectedWorldAge, ThreatTypes[CurrentlySelectedGrievanceType], ProsperityMultiplier);
                        GameWorld.NextUniqueID = TemporaryNextUniqueID;
                        GameState = "placecivilizations";
                    }
                    else if (GameState == "placecivilizations")
                    {
                        if (GameWorld.InitialCivCount > 0)
                        {
                            int TryX = r.Next(GameWorld.Width);
                            int TryZ = r.Next(GameWorld.Length);

                            if (GameWorld.WorldMap[TryX + TryZ * GameWorld.Width].Biome != "ocean" && GameWorld.WorldMap[TryX + TryZ * GameWorld.Width].Biome != "void" && GameWorld.WorldMap[TryX + TryZ * GameWorld.Width].MyLocation == null)
                            {
                                Race R;
                                if (r.Next(1, 3) == 1)
                                {
                                    R = GameWorld.GetRace("luminarch");
                                }
                                else
                                {
                                    R = GameWorld.GetRace("nightfell");
                                }
                                GameWorld.Civilizations.Add(new Civilization(R, R.Name, TryX, TryZ, GameWorld));
                                WaitingTicks = 3;
                                GameWorld.InitialCivCount = GameWorld.InitialCivCount - 1;
                            }
                            else
                            {
                                TotalCivTries++;
                                if (TotalCivTries > 300)
                                {
                                    throw new Exception("Couldnt place civs? Is the whole world ocean?");
                                }
                            }
                        }
                        else
                        {
                            if (GameMode == "founder")
                            {
                                GameState = "choosefounderoptions";
                            }
                            else
                            {
                                GameState = "generatehistory";
                            }
                        }
                    }
                    else if (GameState == "generatehistory")
                    {
                        // Convert MaxAge from years to cycles
                        double maxAgeCycles = ((double)GameWorld.MaxAge) * ((double)290304000);


                        if(Keyboard.GetState().IsKeyDown(Keys.Q) && Keyboard.GetState().IsKeyDown(Keys.R) && Keyboard.GetState().IsKeyDown(Keys.M))
                        {
                            RickRollCycles++;

                            if(RickRollCycles > 250)
                            {
                                RickRollCycles = -999999;
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                                    UseShellExecute = true
                                });
                                Environment.Exit(100);
                            }
                        }
                        else
                        {
                            RickRollCycles = 0;
                        }


                        
                        if (GameWorld.Cycle < maxAgeCycles)
                        {
                            if(SpeedWorldGen)
                            {
                                if (GameWorld.Cycle < 24192000000)
                                {
                                    for (int i = 0; i < 12; i++)
                                    {
                                        GameWorld.ProgressDays(28, true);
                                    }
                                }
                                else if (GameWorld.Cycle < 24192000000 * 2)
                                {
                                    for (int i = 0; i < 6; i++)
                                    {
                                        GameWorld.ProgressDays(28, true);
                                    }
                                }
                                else
                                {
                                    GameWorld.ProgressDays(28, true);
                                }
                            }
                            else
                            {
                                GameWorld.ProgressDays(28, true);
                            }
                        }

                        if (GameWorld.Cycle >= maxAgeCycles || KeysNewlyPressed.Contains(Keys.Enter))
                        {
                            // Create a list to hold the header and the historical events
                            List<string> fileContent = new List<string>
                            {
                                "Historical Events of " + GameWorld.Name,
                                "To find events related to a certain subject, search for it with CTRL+F",
                                "" // Adding an empty line for better readability
                            };

                            // Add the historical events to the list
                            fileContent.AddRange(GameWorld.HistoricalEvents);

                            // Write the content to the file
                            File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/lightrealmhistory.txt", fileContent.ToArray());
                            GameState = "choosepreferences";
                        }

                    }


                    else if (GameState == "choosefounderoptions")
                    {
                        if (KeysNewlyPressed.Contains(Keys.D1))
                        {
                            CurrentlySelectingRace = CurrentlySelectingRace + 1;
                            if (CurrentlySelectingRace > GameWorld.HumanoidRaces.Count - 1)
                            {
                                CurrentlySelectingRace = 1;
                            }
                        }
                        if (KeysNewlyPressed.Contains(Keys.Enter))
                        {
                            GameState = "founder";
                            GameWorld.ProgressDays(28, true);

                            foreach (Civilization c in GameWorld.Civilizations)
                            {
                                if (c.PrimaryInhabiantRace == GameWorld.Races[CurrentlySelectingRace])
                                {
                                    GamePlayerCivilization = c;
                                    MapCursorX = c.StartX;
                                    MapCursorZ = c.StartZ;
                                    break;
                                }
                            }
                        }
                    }
                    else if (GameState == "founder")
                    {
                        if (KeysNewlyPressed.Contains(Keys.Enter))
                        {
                            GameWorld.ProgressDays(28, true);
                        }
                        if (KeysNewlyPressed.Contains(Keys.R) || KeysNewlyPressed.Contains(Keys.T) || KeysNewlyPressed.Contains(Keys.D) || KeysNewlyPressed.Contains(Keys.G) || KeysNewlyPressed.Contains(Keys.C) || KeysNewlyPressed.Contains(Keys.V) || KeysNewlyPressed.Contains(Keys.NumPad7) || KeysNewlyPressed.Contains(Keys.NumPad9) || KeysNewlyPressed.Contains(Keys.NumPad4) || KeysNewlyPressed.Contains(Keys.NumPad6) || KeysNewlyPressed.Contains(Keys.NumPad1) || KeysNewlyPressed.Contains(Keys.NumPad3))
                        {
                            //use GamePlayer.MapCursorX++; GamePlayer.MapCursorZ++; --, etc.

                            int DeltaX = 0;
                            int DeltaZ = 0;

                            if (MapCursorZ % 2 == 0)
                            {
                                //every other row starting with the top one

                                if (KeysNewlyPressed.Contains(Keys.R) || KeysNewlyPressed.Contains(Keys.NumPad7))
                                {
                                    DeltaX = -1;
                                    DeltaZ = -1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.T) || KeysNewlyPressed.Contains(Keys.NumPad9))
                                {
                                    DeltaZ = -1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.D) || KeysNewlyPressed.Contains(Keys.NumPad4))
                                {
                                    DeltaX = -1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.G) || KeysNewlyPressed.Contains(Keys.NumPad6))
                                {
                                    DeltaX = 1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.C) || KeysNewlyPressed.Contains(Keys.NumPad1))
                                {
                                    DeltaX = -1;
                                    DeltaZ = 1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.V) || KeysNewlyPressed.Contains(Keys.NumPad3))
                                {
                                    DeltaZ = 1;
                                }
                            }
                            else
                            {
                                //every other row starting with the second-to-top one

                                if (KeysNewlyPressed.Contains(Keys.R) || KeysNewlyPressed.Contains(Keys.NumPad7))
                                {
                                    DeltaZ = -1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.T) || KeysNewlyPressed.Contains(Keys.NumPad9))
                                {
                                    DeltaZ = -1;
                                    DeltaX = 1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.D) || KeysNewlyPressed.Contains(Keys.NumPad4))
                                {
                                    DeltaX = -1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.G) || KeysNewlyPressed.Contains(Keys.NumPad6))
                                {
                                    DeltaX = 1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.C) || KeysNewlyPressed.Contains(Keys.NumPad1))
                                {
                                    DeltaZ = 1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.V) || KeysNewlyPressed.Contains(Keys.NumPad3))
                                {
                                    DeltaZ = 1;
                                    DeltaX = 1;
                                }
                            }

                            if (!(MapCursorX + DeltaX > GameWorld.Width || MapCursorX + DeltaX < 0 || MapCursorZ + DeltaZ > GameWorld.Length || MapCursorZ + DeltaZ < 0 || GameWorld.WorldMap[(MapCursorX + DeltaX) + ((MapCursorZ + DeltaZ) * GameWorld.Width)].Biome == "void" || GameWorld.WorldMap[(MapCursorX + DeltaX) + ((MapCursorZ + DeltaZ) * GameWorld.Width)].Biome == "ocean" || GameWorld.WorldMap[(MapCursorX + DeltaX) + ((MapCursorZ + DeltaZ) * GameWorld.Width)].Biome == "ethereal"))
                            {
                                MapCursorX = MapCursorX + DeltaX;
                                MapCursorZ = MapCursorZ + DeltaZ;
                            }
                        }
                    }
                    else if (GameState == "choosepreferences")
                    {
                        if (KeysNewlyPressed.Contains(Keys.D1))
                        {
                            CurrentlySelectingRace = CurrentlySelectingRace + 1;
                            if (CurrentlySelectingRace > GameWorld.HumanoidRaces.Count)
                            {
                                CurrentlySelectingRace = 1;
                            }
                        }
                        if (KeysNewlyPressed.Contains(Keys.D2))
                        {
                            CurrentlySelectingSex = CurrentlySelectingSex + 1;
                            if (CurrentlySelectingSex > Sexes.Count - 1)
                            {
                                CurrentlySelectingSex = 0;
                            }
                        }
                        if (KeysNewlyPressed.Contains(Keys.C))
                        {
                            GameState = "generatehistory";
                            GameWorld.MaxAge += 25;
                        }
                        if (KeysNewlyPressed.Contains(Keys.D3))
                        {
                            CurrentlySelectingArchitectProfession = CurrentlySelectingArchitectProfession + 1;
                            if (CurrentlySelectingArchitectProfession > ArchitectProfessions.Count - 1)
                            {
                                CurrentlySelectingArchitectProfession = 0;
                            }
                        }
                        if (KeysNewlyPressed.Contains(Keys.Enter))
                        {
                            GameState = "findstartlocation";
                        }
                    }

                    else if (GameState == "pickstatpreferences")
                    {
                        foreach (var key in KeysNewlyPressed) // Assuming KeysNewlyPressed is a collection of Keys
                        {
                            if (KeyInts.ContainsKey(key)) // Assuming KeyInts maps a key to its numeric value
                            {
                                int keyIndex = KeyInts[key] - 1; // Convert key to index (considering keys start from 1)

                                if (keyIndex < StatOptions.Count) // Ensure the key corresponds to an available option
                                {
                                    string selectedStat = StatOptions[keyIndex];
                                    StatOptions.RemoveAt(keyIndex); // Remove selected stat from options

                                    // Assign the corresponding stat value based on the decreasing CurrentlyAssigningSkill value
                                    if (selectedStat.StartsWith("[STR]")) StoredStr = CurrentlyAssigningSkill;
                                    else if (selectedStat.StartsWith("[AGL]")) StoredAgl = CurrentlyAssigningSkill;
                                    else if (selectedStat.StartsWith("[DEX]")) StoredDex = CurrentlyAssigningSkill;
                                    else if (selectedStat.StartsWith("[END]")) StoredEnd = CurrentlyAssigningSkill;
                                    else if (selectedStat.StartsWith("[CRE]")) StoredCre = CurrentlyAssigningSkill;
                                    else if (selectedStat.StartsWith("[CHA]")) StoredCha = CurrentlyAssigningSkill;
                                    else if (selectedStat.StartsWith("[FOC]")) StoredFoc = CurrentlyAssigningSkill;

                                    CurrentlyAssigningSkill--; // Decrement after each assignment

                                    // Automatically assign the last stat if this was the second-to-last choice
                                    if (CurrentlyAssigningSkill == 1 && StatOptions.Count == 1)
                                    {
                                        string lastStat = StatOptions[0];
                                        if (lastStat.StartsWith("[STR]") && StoredStr == 0) StoredStr = 1;
                                        else if (lastStat.StartsWith("[AGL]") && StoredAgl == 0) StoredAgl = 1;
                                        else if (lastStat.StartsWith("[DEX]") && StoredDex == 0) StoredDex = 1;
                                        else if (lastStat.StartsWith("[END]") && StoredEnd == 0) StoredEnd = 1;
                                        else if (lastStat.StartsWith("[CRE]") && StoredCre == 0) StoredCre = 1;
                                        else if (lastStat.StartsWith("[CHA]") && StoredCha == 0) StoredCha = 1;
                                        else if (lastStat.StartsWith("[FOC]") && StoredFoc == 0) StoredFoc = 1;
                                        GameState = "pickinspirations"; // Update GameState

                                        InspirationsChosen = InspirationsAvailable.OrderBy(x => Guid.NewGuid()).Take(3).ToList();
                                    }

                                    break; // Exit the loop once a stat is assigned
                                }
                            }
                        }
                    }

                    else if (GameState == "pickinspirations")
                    {
                        foreach (var key in KeysNewlyPressed) // Assuming KeysNewlyPressed is a collection of Keys
                        {
                            int keyIndex = -1;

                            // Map the keys A, B, C to indices 0, 1, 2 respectively
                            if (key == Keys.A) keyIndex = 0;
                            else if (key == Keys.B) keyIndex = 1;
                            else if (key == Keys.C) keyIndex = 2;

                            if (keyIndex != -1 && keyIndex < InspirationsChosen.Count) // Ensure the key corresponds to an available option
                            {
                                InspirationSelected = InspirationsChosen[keyIndex];
                                InspirationsChosen.RemoveAt(keyIndex); // Remove selected inspiration from options

                                // Add announcement for selected inspiration
                                Announcements.Add(new TextStorage($"Selected inspiration: {InspirationSelected}", Color.White, new List<Entity>()));

                                // Move to the next game state
                                GameState = "architectfound"; // Update GameState

                                break; // Exit the loop once an inspiration is assigned
                            }
                        }
                    }


                    else if (GameState == "findstartlocation")
                    {
                        // Try to find a place where someone might be
                        TheChosenOne = null;
                        TheChosenGroup = null;

                        foreach (Architect a in GameWorld.AllArchitects)
                        {
                            if (a.IsAlive && a.Location != null && GameWorld.SettlementTypes.Contains(a.Location.Type))
                            {
                                if (a.Sex == Sexes[CurrentlySelectingSex] &&
                                    a.Race == GameWorld.HumanoidRaces[CurrentlySelectingRace - 1] &&
                                    a.Location.TradersAtThisLocation.Count > 0 &&
                                    a.Reputation > -40 &&
                                    a.Grievances.Any(g => GameWorld.Calamity.Contains(g.Item1)))
                                {
                                    // Store the string of the grievance
                                    GrievanceReason = a.Grievances.First(g => GameWorld.Calamity.Contains(g.Item1)).Item2;

                                    // Additional logic here
                                    // For example, compare histories or ages
                                    TheChosenOne = a;
                                    break;
                                }
                            }
                        }

                        if (TheChosenOne != null)
                        {
                            GameState = "architectfound";
                            GamePlayerParty = new Party(new List<Architect>() { TheChosenOne }, "adventurer", TheChosenOne, TheChosenOne.Location);
                            MostRecentPartyTurnArchitect = TheChosenOne;
                            TheChosenOne.District.Architects.Remove(TheChosenOne);
                            TheChosenOne.Group = GamePlayerParty;

                            GameWorld.ProgressToNextMorning();

                            GameState = "pickstatpreferences";
                            StatOptions = new List<string>()
                            {
                                "[STR]: Strength (+Melee Power, +Bruteforce Task Efficiency)",
                                "[AGL]: Agility (+Reaction Chance, +Action Speed)",
                                "[DEX]: Dexterity (+Throwing Power, +Tool Effectiveness, -Limb Exposure)",
                                "[END]: Endurance (+Max Energy, +Item Carrying Capacity)",
                                "[CRE]: Creativity (+Craft Quality/Rarity, +Writing Skill)",
                                "[CHA]: Charisma (Generally more likable/manipulative depending on intent, +Percep Magic, +Performance)",
                                "[FOC]: Focus (+Non-Percep Magic, Less susceptible to Magic, Feel Less Pain)"
                            };
                            CurrentlyAssigningSkill = 7;
                        }
                        else
                        {
                            GameState = "choosepreferences";
                            AlreadyTriedASearch = true;
                        }
                    }

                    else if (GameState == "architectfound")
                    {
                        if (KeysNewlyPressed.Contains(Keys.Enter))
                        {
                            GamePlayerParty.Architects[0].District.Load();

                            //place player

                            int BlockX = Game1.r.Next(0, 7);
                            int BlockZ = Game1.r.Next(0, 7);

                            GamePlayerParty.Architects[0].Block = LoadedArchitects[ArchitectIndex].District.DistrictMap[BlockX + BlockZ * 7];
                            GamePlayerParty.Architects[0].District.DistrictMap[BlockX + BlockZ * 7].Architects.Add(GamePlayerParty.Architects[0]);

                            string GenderName = "";

                            if (GamePlayerParty.Architects[0].Age > 16)
                            {
                                if (GamePlayerParty.Architects[0].Sex == "male")
                                {
                                    GenderName = "man";
                                }
                                else
                                {
                                    GenderName = "woman";
                                }
                            }
                            else
                            {
                                if (GamePlayerParty.Architects[0].Sex == "male")
                                {
                                    GenderName = "boy";
                                }
                                else
                                {
                                    GenderName = "girl";
                                }
                            }

                            Exposition.Add(new TextStorage(GamePlayerParty.Architects[0].Name + " is a " + GamePlayerParty.Architects[0].Age + "-year-old " + GenderName + " from " + GamePlayerParty.Architects[0].Location.Name + ". ", Color.Blue, new List<Entity>()));
                            Exposition.Add(new TextStorage(Capitalize(GamePlayerParty.Architects[0].Pronoun) + " lives in " + GamePlayerParty.Architects[0].Location.Districts[Game1.r.Next(GamePlayerParty.Architects[0].Location.Districts.Count)].Name + " as a " + GamePlayerParty.Architects[0].Profession + ".", Color.Purple, new List<Entity>()));

                            if (GamePlayerParty.Architects[0].Group != null && GamePlayerParty.Architects[0].Group.Architects.Count > 1)
                            {
                                if (GamePlayerParty.Architects[0].Location.Government == GamePlayerParty.Architects[0].Group)
                                {
                                    if (GamePlayerParty.Architects[0].Group.Leader == GamePlayerParty.Architects[0])
                                    {
                                        Exposition.Add(new TextStorage(Capitalize(GamePlayerParty.Architects[0].Pronoun) + " is the leader of " + GamePlayerParty.Architects[0].Group.Name + ", a " + GamePlayerParty.Architects[0].Group.Type + " group that rules " + GamePlayerParty.Architects[0].Group.Leader.Location.Name + ", with " + GamePlayerParty.Architects[0].Group.Architects.Count + " members.", Color.Red, new List<Entity>()));
                                    }
                                    else
                                    {
                                        Exposition.Add(new TextStorage(Capitalize(GamePlayerParty.Architects[0].Pronoun) + " is a member of " + GamePlayerParty.Architects[0].Group.Name + ", a " + GamePlayerParty.Architects[0].Group.Type + " group that rules " + GamePlayerParty.Architects[0].Group.Leader.Location.Name + ", with " + GamePlayerParty.Architects[0].Group.Architects.Count + " members.", Color.Red, new List<Entity>()));
                                    }
                                }
                                else
                                {
                                    if (GamePlayerParty.Architects[0].Group.Leader == GamePlayerParty.Architects[0])
                                    {
                                        Exposition.Add(new TextStorage(Capitalize(GamePlayerParty.Architects[0].Pronoun) + " is the leader of " + GamePlayerParty.Architects[0].Group.Name + ", a " + GamePlayerParty.Architects[0].Group.Type + " group based in " + GamePlayerParty.Architects[0].Group.Leader.Location.Name + ", with " + GamePlayerParty.Architects[0].Group.Architects.Count + " members.", Color.Red, new List<Entity>()));
                                    }
                                    else
                                    {
                                        Exposition.Add(new TextStorage(Capitalize(GamePlayerParty.Architects[0].Pronoun) + " is a member of " + GamePlayerParty.Architects[0].Group.Name + ", a " + GamePlayerParty.Architects[0].Group.Type + " group based in " + GamePlayerParty.Architects[0].Group.Leader.Location.Name + ", with " + GamePlayerParty.Architects[0].Group.Architects.Count + " members.", Color.Red, new List<Entity>()));
                                    }
                                }
                            }

                            if (GamePlayerParty.Architects[0].ScienceStudyPoints == 0 && GamePlayerParty.Architects[0].CultureStudyPoints == 0 && GamePlayerParty.Architects[0].MagicStudyPoints == 0)
                            {
                                Exposition.Add(new TextStorage(Capitalize(GamePlayerParty.Architects[0].Name + " finds " + GamePlayerParty.Architects[0].FavoriteScienceField + " science, " + GamePlayerParty.Architects[0].FavoriteCultureField + " culture, and " + GamePlayerParty.Architects[0].FavoriteMagicField + " magic very intriguing."), Color.Orange, new List<Entity>()));
                            }
                            if (GamePlayerParty.Architects[0].ScienceStudyPoints > 0)
                            {
                                Exposition.Add(new TextStorage(GamePlayerParty.Architects[0].Name + " likes " + GamePlayerParty.Architects[0].FavoriteScienceField + ", and studies it avidly.", Color.Orange, new List<Entity>()));
                            }
                            if (GamePlayerParty.Architects[0].CultureStudyPoints > 0)
                            {
                                Exposition.Add(new TextStorage(GamePlayerParty.Architects[0].Name + " likes " + GamePlayerParty.Architects[0].FavoriteCultureField + ", and explores it relentlessly.", Color.Orange, new List<Entity>()));
                            }
                            if (GamePlayerParty.Architects[0].MagicStudyPoints > 0)
                            {
                                Exposition.Add(new TextStorage(GamePlayerParty.Architects[0].Name + " likes " + GamePlayerParty.Architects[0].FavoriteMagicField + ", and dedicates much research to it's pursuit.", Color.Orange, new List<Entity>()));
                            }

                            GamePlayerParty.Architects[0].Strength = StoredStr;
                            GamePlayerParty.Architects[0].Dexterity = StoredDex;
                            GamePlayerParty.Architects[0].Agility = StoredAgl;
                            GamePlayerParty.Architects[0].Charisma = StoredCha;
                            GamePlayerParty.Architects[0].Focus = StoredFoc;
                            GamePlayerParty.Architects[0].Creativity = StoredCre;
                            GamePlayerParty.Architects[0].Endurance = StoredEnd;

                            GamePlayerParty.Architects[0].Energy = GamePlayerParty.Architects[0].MaxEnergy();


                            Exposition.Add(new TextStorage(GamePlayerParty.Architects[0].Name + " has a deep appreciation for " + GamePlayerParty.Architects[0].FavoriteCloth.Name + ", " + GamePlayerParty.Architects[0].FavoriteStone.Name + ", " + GamePlayerParty.Architects[0].FavoriteWood.Name + ", " + GamePlayerParty.Architects[0].FavoriteGemstone.Name + ", and " + GamePlayerParty.Architects[0].FavoriteMetal.Name + ".", Color.Orange, new List<Entity>()));
                            Exposition.Add(new TextStorage(
                            Capitalize($"{GamePlayerParty.Architects[0].Pronoun} has ") +
                            $"{GamePlayerParty.Architects[0].GetDescription(GamePlayerParty.Architects[0].Strength)} strength, " +
                            $"{GamePlayerParty.Architects[0].GetDescription(GamePlayerParty.Architects[0].Agility)} agility, " +
                            $"{GamePlayerParty.Architects[0].GetDescription(GamePlayerParty.Architects[0].Dexterity)} dexterity, " +
                            $"{GamePlayerParty.Architects[0].GetDescription(GamePlayerParty.Architects[0].Endurance)} endurance, " +
                            $"{GamePlayerParty.Architects[0].GetDescription(GamePlayerParty.Architects[0].Creativity)} creativity, " +
                            $"{GamePlayerParty.Architects[0].GetDescription(GamePlayerParty.Architects[0].Charisma)} charisma, and " +
                            $"{GamePlayerParty.Architects[0].GetDescription(GamePlayerParty.Architects[0].Focus)} focus.", Color.Purple, new List<Entity>()));



                            if (GamePlayerParty.Architects[0].FavoriteBook == null)
                            {
                                Exposition.Add(new TextStorage(GamePlayerParty.Architects[0].Name + " does not particularly enjoy reading.", Color.Yellow, new List<Entity>()));
                            }
                            else
                            {
                                Exposition.Add(new TextStorage(GamePlayerParty.Architects[0].Name + "'s favorite book is " + GamePlayerParty.Architects[0].FavoriteBook.Name + ", a book on " + GamePlayerParty.Architects[0].FavoriteBook.Subject + ".", Color.Yellow, new List<Entity>()));
                            }

                            Exposition.Add(new TextStorage(GamePlayerParty.Architects[0].Name + " has no living family " + GamePlayerParty.Architects[0].Pronoun + " knows of.", Color.Green, new List<Entity>()));
                            Exposition.Add(new TextStorage("", Color.Green, new List<Entity>()));
                            Exposition.Add(new TextStorage("", Color.Green, new List<Entity>()));
                            Exposition.Add(new TextStorage("", Color.Green, new List<Entity>()));
                            Exposition.Add(new TextStorage("", Color.Green, new List<Entity>()));


                            string expositionText =
                            GameWorld.Calamity[0].Name + " and " + GameWorld.Calamity[0].PossessivePronoun +
                            " gang of " + CalamityIdeologicalObsessionMapping[GameWorld.CalamityIdeologicalObsession] +
                            " have plagued " + GameWorld.Name + " for decades, but you cannot stand another second. " +
                            "It has been a long time since " + GameWorld.Calamity[0].Name + " " + GrievanceReason +
                            ", but the memory continues to burden you. Your revenge will be difficult without proper experience and equipment, though. " +
                            "You've saved up a bit of money " + ConvertProfessionToCareerDescription[GamePlayerParty.Architects[0].Profession] +
                            ", but the merchants from " + GamePlayerParty.Architects[0].Location.TradersAtThisLocation[0].Base.Name +
                            " won't be here forever. Perhaps they can assist you in getting some supplies before you embark on your journey. " +
                            "Do not displease them or their debtshibas, and your quest will be glorious and fortunate.";

                            int maxLength = expositionText.Length / 6; // Calculate approximate length for each line

                            List<string> expositionLines = new List<string>();
                            int currentIndex = 0;
                            while (currentIndex < expositionText.Length)
                            {
                                int nextIndex = Math.Min(currentIndex + maxLength, expositionText.Length);
                                if (nextIndex < expositionText.Length && expositionText[nextIndex] != ' ')
                                {
                                    // Find the last space before the max length
                                    int lastSpace = expositionText.LastIndexOf(' ', nextIndex, maxLength);
                                    if (lastSpace > currentIndex)
                                    {
                                        nextIndex = lastSpace;
                                    }
                                }
                                expositionLines.Add(expositionText.Substring(currentIndex, nextIndex - currentIndex).Trim());
                                currentIndex = nextIndex + 1; // Move past the space
                            }

                            foreach (var line in expositionLines)
                            {
                                Exposition.Add(new TextStorage(line, Color.Aquamarine, new List<Entity>()));
                            }

                            Exposition.Add(new TextStorage("", Color.White, new List<Entity>()));
                            Exposition.Add(new TextStorage("Press SPACE to continue...", Color.White, new List<Entity>()));


                            GameState = "exposition";
                        }
                    }
                    else if (GameState == "partyturn")
                    {
                        if (SpeechToText)
                        {
                            if (KeysNewlyPressed.Contains(Keys.RightAlt))
                            {
                                if(!_isRecording)
                                {
                                    //enabling split mode is kinda required ngl

                                    SplitMode = true;

                                    foreach (Architect a in LoadedArchitects)
                                    {
                                        a.UpdateNames();
                                        if (a.Block != null)
                                        {
                                            List<Object> NearbyObjects = a.Room != null ? a.Room.Objects : a.Block.Objects;

                                            foreach (Object o in NearbyObjects)
                                            {
                                                o.UpdateNames();
                                            }
                                        }
                                    }

                                    //START recording
                                    _recognizer = new VoskRecognizer(VoskModel, 16000.0f);
                                    _recognizer.SetMaxAlternatives(0);
                                    _recognizer.SetWords(true);

                                    _waveIn = new WaveInEvent();
                                    _waveIn.DeviceNumber = DeviceNumber;
                                    _waveIn.WaveFormat = new WaveFormat(16000, 1);

                                    _waveIn.DataAvailable += (sender, args) =>
                                    {
                                        if (_recognizer.AcceptWaveform(args.Buffer, args.BytesRecorded))
                                        {
                                            var result = _recognizer.Result();
                                            DisplayResult(result);
                                        }
                                        else
                                        {
                                            var partialResult = _recognizer.PartialResult();
                                            DisplayResult(partialResult);
                                        }
                                    };

                                    _waveIn.StartRecording();
                                    _isRecording = true;
                                }
                                else if (_isRecording)
                                {
                                    _waveIn.StopRecording();
                                    _isRecording = false;
                                }
                            }
                        }

                        currentMouseState = Mouse.GetState();

                        if (SplitMode && currentMouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
                        {
                            Vector2 mousePosition = new Vector2(currentMouseState.X, currentMouseState.Y);
                            Entity closestEntity = null;
                            float closestDistance = float.MaxValue;

                            foreach (var (rect, entity) in EntityHitboxes)
                            {
                                if (rect.Contains(mousePosition))
                                {
                                    Vector2 rectCenter = new Vector2(rect.Center.X, rect.Center.Y);
                                    float distance = Vector2.Distance(mousePosition, rectCenter);

                                    if (distance < closestDistance)
                                    {
                                        closestDistance = distance;
                                        closestEntity = entity;
                                    }
                                }
                            }

                            if (closestEntity != null)
                            {
                                ThisList.Add(closestEntity);
                            }
                        }

                        //yehe les dub chek

                        if (ArchitectIndex > LoadedArchitects.Count - 1)
                        {
                            ArchitectIndex = 0;
                        }

                        if(KeysNewlyPressed.Contains(Keys.F3))
                        {
                            if(SplitMode)
                            {
                                SplitMode = false;

                                foreach (Architect a in LoadedArchitects)
                                {
                                    a.UpdateNames();
                                    if (a.Block != null)
                                    {
                                        List<Object> NearbyObjects = a.Room != null ? a.Room.Objects : a.Block.Objects;

                                        foreach (Object o in NearbyObjects)
                                        {
                                            o.UpdateNames();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                SplitMode = true;

                                foreach (Architect a in LoadedArchitects)
                                {
                                    a.UpdateNames();
                                    if (a.Block != null)
                                    {
                                        List<Object> NearbyObjects = a.Room != null ? a.Room.Objects : a.Block.Objects;

                                        foreach (Object o in NearbyObjects)
                                        {
                                            o.UpdateNames();
                                        }
                                    }
                                }
                            }    
                        }

                        if (GamePlayerParty.Architects.Count == 0)
                        {
                            GameState = "dead";
                        }
                        else if (!(GamePlayerParty.Architects.Contains(LoadedArchitects[ArchitectIndex])))
                        {
                            GameState = "otherturn";
                        }
                        else if (CommandBuilderStage != "none")
                        {
                            if (CommandBuilderStage == "categories" && KeysNewlyPressed.Count > 0)
                            {
                                var categories = new Dictionary<Keys, string>
        {
            { Keys.D1, "general" },
            { Keys.D2, "questions" },
            { Keys.D3, "requests" },
            { Keys.D4, "movement" },
            { Keys.D5, "offensive" },
            { Keys.D6, "defensive" },
            { Keys.D7, "items" },
            { Keys.D8, "creativity" },
            { Keys.D9, "movement" }
        };

                                if (categories.ContainsKey(KeysNewlyPressed[0]))
                                {
                                    CommandBuilderStage = "commands";
                                    SelectedCategory = categories[KeysNewlyPressed[0]];
                                    CurrentSubjectIndex = 0;
                                }
                            }
                            else if (CommandBuilderStage == "commands" || CommandBuilderStage == "pickingsubjects")
                            {
                                var commands = GetCommandsForCategory(SelectedCategory);
                                int index = -1;

                                // Determine index based on key press (1-0 and Q-P)
                                if (KeysNewlyPressed.Count > 0)
                                {
                                    var key = KeysNewlyPressed[0];
                                    if (key >= Keys.D1 && key <= Keys.D9) index = key - Keys.D1;
                                    else if (key == Keys.D0) index = 9;
                                    else if (key >= Keys.Q && key <= Keys.P) index = 10 + (key - Keys.Q);
                                }

                                if (CommandBuilderStage == "commands" && index >= 0 && index < commands.Count)
                                {
                                    SelectedCommand = commands[index];
                                    // Set up RelevantEntities based on the selected command's required subjects
                                    SetUpRelevantEntities(RecognizedCommands[SelectedCommand].Item2);

                                    // Sort RelevantEntities by proximity score
                                    RelevantEntities = RelevantEntities.OrderBy(e => CalculateProximityScore(e, MostRecentPartyTurnArchitect)).ToList();
                                    MaxCommandBuilderPage = (int)Math.Ceiling((decimal)RelevantEntities.Count / 20);
                                    CurrentCommandBuilderPage = 0;
                                    CommandBuilderStage = "pickingsubjects";
                                }
                                else if (CommandBuilderStage == "pickingsubjects")
                                {
                                    bool subjectSelected = false;

                                    if (index >= 0)
                                    {
                                        int entityIndex = index + (CurrentCommandBuilderPage * 20);
                                        if (entityIndex < RelevantEntities.Count)
                                        {
                                            SelectedEntities.Add(RelevantEntities[entityIndex]);
                                            subjectSelected = true;
                                        }
                                    }
                                    else if (KeysNewlyPressed.Contains(Keys.A) || KeysNewlyPressed.Contains(Keys.Left))
                                    {
                                        if (CurrentCommandBuilderPage > 0)
                                        {
                                            CurrentCommandBuilderPage--;
                                        }
                                    }
                                    else if (KeysNewlyPressed.Contains(Keys.S) || KeysNewlyPressed.Contains(Keys.Right))
                                    {
                                        if (CurrentCommandBuilderPage < MaxCommandBuilderPage)
                                        {
                                            CurrentCommandBuilderPage++;
                                        }
                                    }

                                    // Check if we have selected all required subjects
                                    if (SelectedEntities.Count == RecognizedCommands[SelectedCommand].Item2.Count)
                                    {
                                        CommandBuilderStage = "execution";
                                    }
                                    else if (subjectSelected)
                                    {
                                        CurrentSubjectIndex++;
                                        if (CurrentSubjectIndex < RecognizedCommands[SelectedCommand].Item2.Count)
                                        {
                                            // Reset RelevantEntities for the next subject selection
                                            RelevantEntities.Clear();
                                            var nextSubject = RecognizedCommands[SelectedCommand].Item2[CurrentSubjectIndex];
                                            SetUpRelevantEntities(new List<string> { nextSubject });

                                            // Sort RelevantEntities by proximity score
                                            RelevantEntities = RelevantEntities.OrderBy(e => CalculateProximityScore(e, MostRecentPartyTurnArchitect)).ToList();
                                            MaxCommandBuilderPage = (int)Math.Ceiling((decimal)RelevantEntities.Count / 20);
                                            CurrentCommandBuilderPage = 0;
                                        }
                                    }
                                }

                                if (CommandBuilderStage == "execution")
                                {
                                    CommandProcessor.RunCommand(MostRecentPartyTurnArchitect, SelectedCommand, SelectedEntities, LoadedArchitects, GameWorld, r, GamePlayerParty, "attacks");
                                    ThisList.Clear();
                                    ResetCommandBuilder();
                                }
                            }

                            if (KeysNewlyPressed.Contains(Keys.X))
                            {
                                if (CommandBuilderStage == "commands")
                                {
                                    CommandBuilderStage = "categories";
                                    SelectedCategory = "";
                                }
                                else if (CommandBuilderStage == "pickingsubjects")
                                {
                                    CommandBuilderStage = "commands";
                                    SelectedCommand = "";
                                    CurrentSubjectIndex = 0;
                                    RelevantEntities.Clear();
                                    SelectedEntities.Clear();
                                }
                            }
                            else if (KeysNewlyPressed.Contains(Keys.Z))
                            {
                                // Placeholder for future functionality when Z is pressed
                            }
                        }
                        else if (LoadedArchitects[ArchitectIndex].Crafting)
                        {
                            GameState = "crafting";
                            CraftingPhase = "selectrecipe";
                            RecipeIndex = 0;
                            InventoryCraftingIndex = 0;
                        }
                        else
                        {
                            MostRecentPartyTurnArchitect = LoadedArchitects[ArchitectIndex];

                            if (LoadedArchitects[ArchitectIndex].CooldownCycles == 0)
                            {
                                foreach(Architect a in LoadedArchitects)
                                {
                                    a.UpdateNames();
                                    if(a.Block != null)
                                    {
                                        List<Object> NearbyObjects = a.Room != null ? a.Room.Objects : a.Block.Objects;

                                        foreach (Object o in NearbyObjects)
                                        {
                                            o.UpdateNames();
                                        }
                                    }
                                }

                                //do this list replacements in split mode


                                if (ThisList.Count > 0 && SplitMode)
                                {
                                    // Get the first entity in ThisList
                                    var entity = ThisList[0];
                                    var referredToName = entity.ReferredToNames[0];

                                    // Use regular expressions to find the first instance of " this " surrounded by spaces
                                    var prompt = MostRecentPartyTurnArchitect.Prompt;
                                    var match = Regex.Match(prompt, @"\bthis\b");

                                    if (match.Success)
                                    {
                                        // Replace the first instance of "this" with the referredToName
                                        prompt = prompt.Substring(0, match.Index) + referredToName + prompt.Substring(match.Index + match.Length);

                                        // Update the prompt
                                        MostRecentPartyTurnArchitect.Prompt = prompt;

                                        // Remove the entity from ThisList
                                        ThisList.RemoveAt(0);
                                    }
                                }



                                //update prompt



                                if (!IsInGui)
                                {






                                    if (LoadedArchitects[ArchitectIndex].SpendableLevels > 0 && InInventory == true)
                                    {
                                        if (LoadedArchitects[ArchitectIndex].SpendableLevels > 0 && InInventory == true)
                                        {
                                            if (Keyboard.GetState().IsKeyDown(Keys.LeftControl))
                                            {
                                                if (KeysNewlyPressed.Contains(Keys.X))
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfShadowLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;
                                                    if (LoadedArchitects[ArchitectIndex].PathOfShadowLevel % 2 != 0) // Odd levels
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Agility++; // Level up Agility
                                                    }
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.L))
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfLifeLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;

                                                    if (LoadedArchitects[ArchitectIndex].PathOfLifeLevel % 2 != 0) // Odd levels
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Charisma++; // Level up Charisma
                                                    }

                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.D))
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfDeathLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;

                                                    if (LoadedArchitects[ArchitectIndex].PathOfDeathLevel % 2 != 0) // Odd levels
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Focus++; // Level up Focus
                                                    }

                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.T))
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfTimeLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;

                                                    if (LoadedArchitects[ArchitectIndex].PathOfTimeLevel % 2 != 0) // Odd levels
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Focus++; // Level up Focus
                                                    }
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.A)) // Assuming 'A' for Path of Stars
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfStarsLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;

                                                    if (LoadedArchitects[ArchitectIndex].PathOfStarsLevel % 2 != 0) // Odd levels
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Creativity++; // Level up Creativity
                                                    }
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.H)) // Assuming 'H' for Path of Heat
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfHeatLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;

                                                    if (LoadedArchitects[ArchitectIndex].PathOfHeatLevel % 2 != 0) // Odd levels
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Endurance++; // Level up Endurance
                                                    }
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.I)) // Assuming 'I' for Path of Illusions
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfIllusionsLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;

                                                    if (LoadedArchitects[ArchitectIndex].PathOfIllusionsLevel % 2 != 0) // Odd levels
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Charisma++; // Level up Charisma
                                                    }
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.E)) // Assuming 'E' for Path of Ethereality
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfEtherealityLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;

                                                    if (LoadedArchitects[ArchitectIndex].PathOfEtherealityLevel % 2 != 0) // Odd levels
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Dexterity++; // Level up Dexterity
                                                    }
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.V)) // Assuming 'V' for Path of Void
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfVoidLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;

                                                    if (LoadedArchitects[ArchitectIndex].PathOfVoidLevel % 2 != 0) // Odd levels
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Creativity++; // Level up Creativity
                                                    }
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.S)) // Assuming 'S' for Path of Storms
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfStormsLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;

                                                    if (LoadedArchitects[ArchitectIndex].PathOfStormsLevel % 2 != 0) // Odd levels
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Strength++; // Level up Strength
                                                    }
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.F))
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfForgeLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;

                                                    if (LoadedArchitects[ArchitectIndex].PathOfForgeLevel % 2 != 0) // Odd levels
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Dexterity++; // Level up Dexterity
                                                    }
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.K)) // Assuming 'K' for Path of Lore
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfLoreLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;

                                                    if (LoadedArchitects[ArchitectIndex].PathOfLoreLevel % 2 != 0) // Odd levels
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Creativity++; // Level up Creativity
                                                    }
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.M)) // Assuming 'M' for Path of Mind
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfMindLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;

                                                    if (LoadedArchitects[ArchitectIndex].PathOfMindLevel % 2 != 0) // Odd levels
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Focus++; // Level up Focus
                                                    }
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.U)) // Assuming 'U' for Path of Soul
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfSoulLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;

                                                    if (LoadedArchitects[ArchitectIndex].PathOfSoulLevel % 2 != 0) // Odd levels
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Endurance++; // Level up Endurance
                                                    }
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.B)) // Assuming 'B' for Path of Body
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfBodyLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;

                                                    if (LoadedArchitects[ArchitectIndex].PathOfBodyLevel % 2 != 0) // Odd levels
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Strength++; // Level up Strength
                                                    }
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.P)) // Assuming 'P' for Path of Space
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfSpaceLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;

                                                    if (LoadedArchitects[ArchitectIndex].PathOfSpaceLevel % 2 != 0) // Odd levels
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Strength++; // Level up Strength
                                                    }
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.R)) // Assuming 'R' for Path of Reality
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfRealityLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;

                                                    if (LoadedArchitects[ArchitectIndex].PathOfRealityLevel % 2 != 0) // Odd levels
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Dexterity++; // Level up Dexterity
                                                    }
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.G)) // Assuming 'G' for Path of Light
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfLightLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;

                                                    if (LoadedArchitects[ArchitectIndex].PathOfLightLevel % 2 != 0) // Odd levels
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Agility++; // Level up Agility
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Exposition = new List<TextStorage>();

                                        if(KeysNewlyPressed.Contains(Keys.OemTilde))
                                        {
                                            CommandBuilderStage = "categories";
                                            AllSubjects = CollectAllSubjects(MostRecentPartyTurnArchitect, "none");
                                            SelectedCategory = "";
                                            SelectedEntities = new List<Entity>();
                                            SelectedCommand = "";
                                        }

                                        foreach (Keys k in KeysNewlyPressed)
                                        {
                                            if (k == Keys.Up)
                                            {
                                                LoadedArchitects[ArchitectIndex].Prompt = LoadedArchitects[ArchitectIndex].LastPrompt;
                                            }
                                            if (k == Keys.Back && LoadedArchitects[ArchitectIndex].Prompt.Length != 0)
                                            {
                                                LoadedArchitects[ArchitectIndex].Prompt = LoadedArchitects[ArchitectIndex].Prompt.Substring(0, LoadedArchitects[ArchitectIndex].Prompt.Length - 1);
                                            }
                                            else if (KeyAtlas.ContainsKey(k))
                                            {
                                                if (!Keyboard.GetState().IsKeyDown(Keys.OemTilde) && !Keyboard.GetState().IsKeyDown(Keys.LeftControl) && !Keyboard.GetState().IsKeyDown(Keys.RightControl))
                                                {
                                                    if (Keyboard.GetState().CapsLock == true || Keyboard.GetState().IsKeyDown(Keys.LeftShift) || Keyboard.GetState().IsKeyDown(Keys.RightShift))
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Prompt += UpperKeyAtlas[k];
                                                    }
                                                    else
                                                    {
                                                        LoadedArchitects[ArchitectIndex].Prompt += KeyAtlas[k];
                                                    }
                                                }
                                            }
                                        }

                                        /*
                                        if (KeysNewlyPressed.Contains(Keys.LeftAlt))
                                        {
                                            if (ObservationsAndMessages == "both")
                                            {
                                                ObservationsAndMessages = "observations";
                                            }
                                            else if (ObservationsAndMessages == "observations")
                                            {
                                                ObservationsAndMessages = "messages";
                                            }
                                            else if (ObservationsAndMessages == "messages")
                                            {
                                                ObservationsAndMessages = "both";
                                            }
                                        }
                                        */

                                        

                                        if ((Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.RightControl)) && Keyboard.GetState().IsKeyDown(Keys.S))
                                        {
                                            SaveTicks++;
                                            if (SaveTicks > 100)
                                            {
                                                GameState = "savinggame";
                                            }
                                        }
                                        else
                                        {
                                            SaveTicks = 0;
                                        }

                                        if (KeysNewlyPressed.Contains(Keys.Escape) && InInventory == true)
                                        {
                                            InInventory = false;
                                        }

                                        if (KeysNewlyPressed.Contains(Keys.Enter) || (currentMouseState.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released))
                                        {
                                            // Get the original command from the current architect
                                            string originalCommand = LoadedArchitects[ArchitectIndex].Prompt.ToLower();

                                            // Check if the command starts with "cast"
                                            bool isCastCommand = originalCommand.StartsWith("cast");

                                            // Define command separators
                                            string[] commandSeparators = { " and then ", " and ", ", ", " then " };

                                            // Split the command based on whether it's a cast command or not
                                            string[] commandParts;
                                            if (isCastCommand)
                                            {
                                                // If it's a cast command, keep the original command as a single part
                                                commandParts = new string[] { originalCommand };
                                            }
                                            else
                                            {
                                                // Otherwise, split the command using the defined separators
                                                commandParts = originalCommand.Split(commandSeparators, StringSplitOptions.RemoveEmptyEntries);
                                            }

                                            // Store the last prompt
                                            LoadedArchitects[ArchitectIndex].LastPrompt = LoadedArchitects[ArchitectIndex].Prompt;

                                            // Determine modifier
                                            string Modifier = "none";
                                            if (commandParts.Length > 0 && (commandParts[0].StartsWith("get") || commandParts[0].StartsWith("grab") || commandParts[0].StartsWith("take") || commandParts[0].StartsWith("pick up") || commandParts[0].StartsWith("steal")))
                                            {
                                                Modifier = "get";
                                            }

                                            // Collect all subjects
                                            AllSubjects = CollectAllSubjects(MostRecentPartyTurnArchitect, Modifier);

                                            if (commandParts.Length > 0)
                                            {
                                                // Collect all subjects before processing the command
                                                Dictionary<string, Entity> matchedSubjects;

                                                // Execute the first command part
                                                List<Entity> relevantSubjectsForFirstPart = FilterSubjectsForCommandPart(commandParts[0], AllSubjects, MostRecentPartyTurnArchitect, out matchedSubjects);
                                                string processedCommandPart = commandParts[0];

                                                // Replace recognized subjects with placeholders
                                                foreach (var matchedSubject in matchedSubjects.Keys)
                                                {
                                                    // Use Regex to accurately replace the matched referredName with "~", considering word boundaries
                                                    processedCommandPart = Regex.Replace(processedCommandPart, $@"\b{Regex.Escape(matchedSubject)}\b", "~", RegexOptions.IgnoreCase);
                                                }

                                                static string GetCommandIdFromInput(string input)
                                                {
                                                    input = input.ToLower(); // Normalize the input
                                                    foreach (var command in RecognizedCommands)
                                                    {
                                                        if (command.Value.Item1.Contains(input)) // Check against the first list in the tuple
                                                        {
                                                            return command.Key;
                                                        }
                                                    }
                                                    return null; // Return null if no command matches
                                                }

                                                static bool PreprocessAndRunCommand(Architect executor, string userInput, List<Entity> subjects)
                                                {
                                                    string[] pronouns = { "he", "she", "it", "they", "him", "her", "them", "that", "his", "their", "himself", "herself", "themself", "themselves" };

                                                    foreach (var pronoun in pronouns)
                                                    {
                                                        // Use regular expression to match whole words
                                                        string pattern = @"\b" + Regex.Escape(pronoun) + @"\b";
                                                        userInput = Regex.Replace(userInput, pattern, "/p", RegexOptions.IgnoreCase);
                                                    }

                                                    // Find the command ID from the user input
                                                    string commandId = GetCommandIdFromInput(userInput);

                                                    if (commandId == null)
                                                    {
                                                        commandId = userInput;
                                                    }

                                                    // Assuming `RunCommand` is adapted to accept a command ID
                                                    ThisList.Clear();
                                                    return CommandProcessor.RunCommand(executor, commandId, subjects, LoadedArchitects, GameWorld, r, GamePlayerParty, userInput);
                                                }

                                                // This would be called in your game loop or command handling part
                                                bool segmentSuccess = PreprocessAndRunCommand(LoadedArchitects[ArchitectIndex], processedCommandPart, relevantSubjectsForFirstPart);

                                                // If there are more parts of the command left, update the prompt with the remaining parts
                                                if (commandParts.Length > 1 && segmentSuccess)
                                                {
                                                    string remainingCommand = string.Join(" and ", commandParts.Skip(1)).Trim();
                                                    LoadedArchitects[ArchitectIndex].Prompt = remainingCommand;
                                                }
                                                else if (LoadedArchitects.Count > 0)
                                                {
                                                    // If it was the last part or the command segment didn't run successfully, clear the prompt
                                                    LoadedArchitects[ArchitectIndex].Prompt = "";
                                                }
                                            }
                                        }


                                        else if (LoadedArchitects[ArchitectIndex].Structure == null)
                                        {
                                            bool altPressed = Keyboard.GetState().IsKeyDown(Keys.LeftControl); // Check if Alt key is pressed

                                            foreach (var key in KeysNewlyPressed)
                                            {
                                                bool isDirectionKey = ValidNumpadKeys.Contains(key);
                                                bool isAltMovementKey = altPressed && (key == Keys.Q || key == Keys.W || key == Keys.E || key == Keys.A || key == Keys.D || key == Keys.Z || key == Keys.X || key == Keys.C);

                                                if (isDirectionKey || isAltMovementKey)
                                                {
                                                    if (directionOffsets.TryGetValue(key, out var offset))
                                                    {
                                                        if (LoadedArchitects[ArchitectIndex].CombatCycles == 0 || r.Next(100) <= LoadedArchitects[ArchitectIndex].EscapeChance())
                                                        {
                                                            if (LoadedArchitects[ArchitectIndex].CurrentlyMovingPlace == KeyDirections[key])
                                                            {
                                                                int newX = LoadedArchitects[ArchitectIndex].Block.X + offset.dx;
                                                                int newZ = LoadedArchitects[ArchitectIndex].Block.Z + offset.dz;

                                                                if (newX == -1 || newX == 7 || newZ == -1 || newZ == 7)
                                                                {
                                                                    // Set TryingToTravel and check if all are ready to travel
                                                                    LoadedArchitects[ArchitectIndex].TryingToTravel = true;
                                                                    bool allTryingToTravel = GamePlayerParty.Architects.All(a => a.TryingToTravel);

                                                                    if (allTryingToTravel)
                                                                    {
                                                                        GameState = "travelmenu";
                                                                        GamePlayerParty.ClearSkillData();
                                                                        GamePlayerParty.MapCursorDistrict = 0;
                                                                        MapCursorX = LoadedArchitects[ArchitectIndex].Location.X;
                                                                        MapCursorZ = LoadedArchitects[ArchitectIndex].Location.Z;

                                                                        GameWorld.RevealNearbyTiles(MapCursorX, MapCursorZ);

                                                                        GamePlayerParty.Architects[0].District.Unload();

                                                                        foreach (var architect in GamePlayerParty.Architects)
                                                                        {
                                                                            architect.CurrentlyMovingPlace = "none"; // Reset movement place after successful travel
                                                                        }
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    LoadedArchitects[ArchitectIndex].CooldownCycles += (int)Math.Round(25 / LoadedArchitects[ArchitectIndex].Speed());

                                                                    LoadedArchitects[ArchitectIndex].Block.Architects.Remove(LoadedArchitects[ArchitectIndex]);
                                                                    LoadedArchitects[ArchitectIndex].Block = LoadedArchitects[ArchitectIndex].District.DistrictMap[newX + newZ * 7];
                                                                    foreach (Structure s in LoadedArchitects[ArchitectIndex].Block.Structures)
                                                                    {
                                                                        if (s.Type != "house" && s.Type != "bighouse")
                                                                        {
                                                                            MakeObservation(s.GetStructureDescription(), Color.Gray, new List<Entity>() { s });
                                                                        }
                                                                    }

                                                                    LoadedArchitects[ArchitectIndex].Block.Architects.Add(LoadedArchitects[ArchitectIndex]);
                                                                    LoadedArchitects[ArchitectIndex].CurrentlyMovingPlace = "none"; // Reset movement place after successful movement
                                                                }
                                                            }
                                                            else
                                                            {
                                                                // Set or update CurrentlyMovingPlace to the new intended direction
                                                                LoadedArchitects[ArchitectIndex].CurrentlyMovingPlace = KeyDirections[key];
                                                                LoadedArchitects[ArchitectIndex].CooldownCycles += (int)Math.Round(25 / LoadedArchitects[ArchitectIndex].Speed());
                                                            }
                                                        }
                                                        else
                                                        {
                                                            MakeObservation("You struggle to escape, and fail!", Color.OrangeRed, new List<Entity>());
                                                            LoadedArchitects[ArchitectIndex].CooldownCycles += (int)Math.Round(25 / LoadedArchitects[ArchitectIndex].Speed());
                                                        }
                                                        // Handle cooldown and progress to next architect

                                                        IncrementAndCycleWorld();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (KeysNewlyPressed.Contains(Keys.Space))
                                    {
                                        IsInGui = false;
                                    }
                                }
                            }
                            else
                            {
                                IncrementAndCycleWorld();
                                GameState = "otherturn";
                            }

                            //set party turn for otherturn graphics

                            if (LoadedArchitects.Count != 0)
                            {
                                if (PlayerSpendableLevelsLastTick != MostRecentPartyTurnArchitect.SpendableLevels && MostRecentPartyTurnArchitect.SpendableLevels > 0)
                                {
                                    Announcements.Add(new TextStorage("You have leveled up to level " + MostRecentPartyTurnArchitect.Level + ". You have a new spendable level in Inventory.", Color.AliceBlue, new List<Entity>()));
                                }
                                PlayerSpendableLevelsLastTick = MostRecentPartyTurnArchitect.SpendableLevels;

                                if (Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.RightControl))
                                {
                                    if (KeysNewlyPressed.Contains(Keys.OemPlus)) // Check if the Plus key is newly pressed
                                    {
                                        // Increment the current page if it's not the last page
                                        if (CurrentObjectPage < MaximumObjectPage)
                                        {
                                            CurrentObjectPage++;
                                        }
                                    }
                                    else if (KeysNewlyPressed.Contains(Keys.OemMinus)) // Check if the Minus key is newly pressed
                                    {
                                        // Decrement the current page if it's not the first page
                                        if (CurrentObjectPage > 0)
                                        {
                                            CurrentObjectPage--;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (GameState == "dead")
                    {
                        if (KeysNewlyPressed.Contains(Keys.Space))
                        {
                            GameWorld = null;
                            GamePlayerParty = null;
                            Announcements = new List<TextStorage>();
                            Observations = new List<TextStorage>();
                            Messages = new List<TextStorage>();
                            GameState = "mainscreen";

                            MediaPlayer.Stop();
                            MediaPlayer.Play(LightrealmMainTheme);
                        }
                    }
                    else if (GameState == "otherturn")
                    {
                        StoredAttacks = new List<Attack>();

                        // We initialize a loop that continues until we find a party member or trigger a reaction state
                        while (true)
                        {
                            // Ensure the ArchitectIndex wraps around if it exceeds the number of loaded architects
                            ArchitectIndex %= LoadedArchitects.Count;
                            Architect currentArchitect = LoadedArchitects[ArchitectIndex];

                            List<Attack> newAttacks = currentArchitect.UpdateSelfActionsAndSuch();
                            StoredAttacks.AddRange(newAttacks);

                            foreach (var attack in newAttacks)
                            {
                                if (GamePlayerParty.Architects.Any(a => a.BodyParts.Contains(attack.Target)))
                                {
                                    GameState = "reaction";
                                }
                            }

                            if (GameState == "reaction") break; // If reaction state is triggered, exit early

                            // If the architect is a party member, switch to party turn and break
                            if (GamePlayerParty.Architects.Contains(currentArchitect) && !currentArchitect.TemporarilyIncapacitated())
                            {
                                if (currentArchitect.RuptureMode == true)
                                {
                                    GameState = "triggerrupture";
                                }
                                else if (currentArchitect.TryPickUpItemType != "")
                                {
                                    GameState = "trypickup";
                                }
                                else if (currentArchitect.TryDropItemType != "")
                                {
                                    GameState = "trydrop";
                                }
                                else if (currentArchitect.MessagesNotRespondedTo.Count > 0)
                                {
                                    GameState = "messagereply";
                                }
                                else
                                {
                                    GameState = "partyturn";
                                }

                                break; // Break from the while loop to switch to party turn
                            }

                            IncrementAndCycleWorld();


                            if(GameState != "otherturn")
                            {
                                break;
                            }
                        }
                    }
                    else if (GameState == "triggerrupture")
                    {
                        if (KeysNewlyPressed.Contains(Keys.R) || KeysNewlyPressed.Contains(Keys.T) || KeysNewlyPressed.Contains(Keys.D) || KeysNewlyPressed.Contains(Keys.G) || KeysNewlyPressed.Contains(Keys.C) || KeysNewlyPressed.Contains(Keys.V) || KeysNewlyPressed.Contains(Keys.NumPad7) || KeysNewlyPressed.Contains(Keys.NumPad9) || KeysNewlyPressed.Contains(Keys.NumPad4) || KeysNewlyPressed.Contains(Keys.NumPad6) || KeysNewlyPressed.Contains(Keys.NumPad1) || KeysNewlyPressed.Contains(Keys.NumPad3))
                        {
                            int DeltaX = 0;
                            int DeltaZ = 0;

                            if (MapCursorZ % 2 == 0)
                            {
                                //every other row starting with the top one

                                if (KeysNewlyPressed.Contains(Keys.R) || KeysNewlyPressed.Contains(Keys.NumPad7))
                                {
                                    DeltaX = -1;
                                    DeltaZ = -1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.T) || KeysNewlyPressed.Contains(Keys.NumPad9))
                                {
                                    DeltaZ = -1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.D) || KeysNewlyPressed.Contains(Keys.NumPad4))
                                {
                                    DeltaX = -1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.G) || KeysNewlyPressed.Contains(Keys.NumPad6))
                                {
                                    DeltaX = 1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.C) || KeysNewlyPressed.Contains(Keys.NumPad1))
                                {
                                    DeltaX = -1;
                                    DeltaZ = 1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.V) || KeysNewlyPressed.Contains(Keys.NumPad3))
                                {
                                    DeltaZ = 1;
                                }
                            }
                            else
                            {
                                //every other row starting with the second-to-top one

                                if (KeysNewlyPressed.Contains(Keys.R) || KeysNewlyPressed.Contains(Keys.NumPad7))
                                {
                                    DeltaZ = -1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.T) || KeysNewlyPressed.Contains(Keys.NumPad9))
                                {
                                    DeltaZ = -1;
                                    DeltaX = 1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.D) || KeysNewlyPressed.Contains(Keys.NumPad4))
                                {
                                    DeltaX = -1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.G) || KeysNewlyPressed.Contains(Keys.NumPad6))
                                {
                                    DeltaX = 1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.C) || KeysNewlyPressed.Contains(Keys.NumPad1))
                                {
                                    DeltaZ = 1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.V) || KeysNewlyPressed.Contains(Keys.NumPad3))
                                {
                                    DeltaZ = 1;
                                    DeltaX = 1;
                                }
                            }

                            if (!(MapCursorX + DeltaX > GameWorld.Width || MapCursorX + DeltaX < 0 || MapCursorZ + DeltaZ > GameWorld.Length || MapCursorZ + DeltaZ < 0))
                            {
                                MapCursorX += DeltaX;
                                MapCursorZ += DeltaZ;
                            }

                        }


                        if (KeysNewlyPressed.Contains(Keys.Enter))
                        {
                            GameWorld.TriggerRupture(MapCursorX, MapCursorZ, LoadedArchitects[ArchitectIndex], 10);

                            LoadedArchitects[ArchitectIndex].RuptureMode = false;

                            if (LoadedArchitects[ArchitectIndex].Location.Region.Biome == "ethereal")
                            {
                                MakeObservation(LoadedArchitects[ArchitectIndex].Name + " successfully killed themselves in the fractal rift. How embarrassing...", Color.Magenta, new List<Entity>() { LoadedArchitects[ArchitectIndex] });
                                LoadedArchitects[ArchitectIndex].IsAlive = false;
                                if (GamePlayerParty.Architects.Contains(LoadedArchitects[ArchitectIndex]))
                                {
                                    GamePlayerParty.Architects.Remove(LoadedArchitects[ArchitectIndex]);

                                    if (GamePlayerParty.Architects.Count == 0)
                                    {
                                        GameState = "dead";

                                        if (GamePlayerParty.Architects.Count == 0)
                                        {
                                            GameState = "dead";
                                        }
                                    }
                                }
                            }
                            else
                            {
                                IncrementAndCycleWorld();

                                if (GamePlayerParty.Architects.Contains(LoadedArchitects[ArchitectIndex]))
                                {
                                    GameState = "partyturn";
                                }
                                else
                                {
                                    GameState = "otherturn";
                                }

                            }
                        }
                    }
                    else if (GameState == "reaction")
                    {
                        if (KeysNewlyPressed.Any(key => key == Keys.S || key == Keys.P || key == Keys.B || key == Keys.D || key == Keys.J || key == Keys.R || key == Keys.N || key == Keys.C))
                        {
                            string Action = "";

                            Keys pressedKey = KeysNewlyPressed.First(); // Assuming you want the first pressed key
                            switch (pressedKey)
                            {
                                case Keys.S:
                                    Action = "sustain";
                                    break;
                                case Keys.P:
                                    Action = "parry";
                                    // Handle P key
                                    break;
                                case Keys.B:
                                    Action = "block";
                                    // Handle B key
                                    break;
                                case Keys.D:
                                    Action = "duck";
                                    // Handle D key
                                    break;
                                case Keys.J:
                                    Action = "jump";
                                    // Handle J key
                                    break;
                                case Keys.R:
                                    Action = "roll";
                                    // Handle R key
                                    break;
                                case Keys.N:
                                    Action = "disarm";
                                    // Handle N key
                                    break;
                                case Keys.C:
                                    Action = "redirect";
                                    // Handle C key
                                    break;
                                default:
                                    break;
                            }

                            CalculateAttack(StoredAttacks[0].Verb, StoredAttacks[0].Attacker, StoredAttacks[0].Target, Action, StoredAttacks[0].Weapon);
                            StoredAttacks.RemoveAt(0);
                        }

                        if (StoredAttacks.Count == 0)
                        {
                            GameState = "otherturn";
                        }
                    }
                    else if (GameState == "messagereply")
                    {
                        if (KeysNewlyPressed.Any(key => key == Keys.T || key == Keys.M || key == Keys.I || key == Keys.D || key == Keys.F || key == Keys.R || key == Keys.X))
                        {
                            string Reply = "";

                            Keys pressedKey = KeysNewlyPressed.First(); // Assuming you want the first pressed key
                            switch (pressedKey)
                            {
                                case Keys.T:
                                    Reply = MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].PositiveResponse;
                                    break;
                                case Keys.M:
                                    Reply = MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].DirectRefusalResponse;
                                    break;
                                case Keys.I:
                                    Reply = MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].IgnorantResponse;
                                    break;
                                case Keys.D:
                                    Reply = MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].DerailingResponse;
                                    break;
                                case Keys.F:
                                    Reply = MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].FlatteringResponse;
                                    break;
                                case Keys.R:
                                    break;
                                default:
                                    break;
                            }

                            if(Reply != "")
                            {
                                AddMessage(MostRecentPartyTurnArchitect.ReferredToNames[0] + ": " + Reply, new Color(0, 255, 0), new List<Entity> { MostRecentPartyTurnArchitect }.Union(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Subjects).ToList());
                                MessageWorldEdit(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Sender, MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Receiver, MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].MessageContent, MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Subjects, Reply);
                                MostRecentPartyTurnArchitect.MessagesNotRespondedTo.RemoveAt(0);
                                MostRecentPartyTurnArchitect.CooldownCycles += (int)Math.Round(30 / MostRecentPartyTurnArchitect.Speed());
                            }
                            else
                            {
                                AddMessage(MostRecentPartyTurnArchitect.ReferredToNames[0] + " does not reply.", Color.Yellow, new List<Entity>() { MostRecentPartyTurnArchitect });
                                MessageWorldEdit(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Sender, MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Receiver, MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].MessageContent, MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Subjects, Reply);
                                MostRecentPartyTurnArchitect.MessagesNotRespondedTo.RemoveAt(0);
                            }

                            GameState = "otherturn";
                        }
                    }
                    else if (GameState == "trypickup")
                    {
                        void PickUpItems(int count)
                        {
                            List<Object> itemsToPickUp = (MostRecentPartyTurnArchitect.Room != null ? MostRecentPartyTurnArchitect.Room.Objects : MostRecentPartyTurnArchitect.Block.Objects)
                                .Where(item => item.Type == MostRecentPartyTurnArchitect.TryPickUpItemType && item.Materials.SequenceEqual(MostRecentPartyTurnArchitect.TryPickUpMaterials))
                                .Take(count).ToList();

                            foreach (var item in itemsToPickUp)
                            {
                                item.Room = null;
                                item.Block = null;

                                if (MostRecentPartyTurnArchitect.Room != null)
                                {
                                    MostRecentPartyTurnArchitect.Room.Objects.Remove(item);
                                }
                                else
                                {
                                    MostRecentPartyTurnArchitect.Block.Objects.Remove(item);
                                }
                                MostRecentPartyTurnArchitect.Inventory.Add(item);
                                MakeObservation("You pick up the " + item.ReferredToNames[0] + " and put it in your inventory.", Color.Yellow, new List<Entity>() { item });
                                MostRecentPartyTurnArchitect.CooldownCycles += (int)(Math.Round(5 / MostRecentPartyTurnArchitect.Speed()));
                            }
                        }

                        int GetHalfCount(string itemType, List<Material> itemMaterials)
                        {
                            int totalCount = (MostRecentPartyTurnArchitect.Room != null ? MostRecentPartyTurnArchitect.Room.Objects : MostRecentPartyTurnArchitect.Block.Objects)
                                .Count(item => item.Type == itemType && item.Materials.SequenceEqual(itemMaterials));
                            return (int)Math.Ceiling(totalCount / 2.0);
                        }

                        int GetFullCount(string itemType, List<Material> itemMaterials)
                        {
                            return (MostRecentPartyTurnArchitect.Room != null ? MostRecentPartyTurnArchitect.Room.Objects : MostRecentPartyTurnArchitect.Block.Objects)
                                .Count(item => item.Type == itemType && item.Materials.SequenceEqual(itemMaterials));
                        }

                        if (KeysNewlyPressed.Contains(Keys.D1))
                        {
                            PickUpItems(1); // Pick up one item of the selected type and material
                            MostRecentPartyTurnArchitect.TryPickUpItemType = "";
                            MostRecentPartyTurnArchitect.TryPickUpMaterials = new List<Material>();
                            GameState = "otherturn";
                        }
                        else if (KeysNewlyPressed.Contains(Keys.D2))
                        {
                            PickUpItems(GetHalfCount(MostRecentPartyTurnArchitect.TryPickUpItemType, MostRecentPartyTurnArchitect.TryPickUpMaterials)); // Pick up half of the items of the selected type and material
                            MostRecentPartyTurnArchitect.TryPickUpItemType = "";
                            MostRecentPartyTurnArchitect.TryPickUpMaterials = new List<Material>();
                            GameState = "otherturn";
                        }
                        else if (KeysNewlyPressed.Contains(Keys.D3))
                        {
                            PickUpItems(GetFullCount(MostRecentPartyTurnArchitect.TryPickUpItemType, MostRecentPartyTurnArchitect.TryPickUpMaterials)); // Pick up all items of the selected type and material
                            MostRecentPartyTurnArchitect.TryPickUpItemType = "";
                            MostRecentPartyTurnArchitect.TryPickUpMaterials = new List<Material>();
                            GameState = "otherturn";
                        }
                    }


                    else if (GameState == "trydrop")
                    {
                        void DropItems(int count)
                        {
                            List<Object> itemsToDrop = MostRecentPartyTurnArchitect.Inventory
                                .Where(item => item.Type == MostRecentPartyTurnArchitect.TryDropItemType && item.Materials.SequenceEqual(MostRecentPartyTurnArchitect.TryDropMaterials))
                                .Take(count).ToList();

                            foreach (var item in itemsToDrop)
                            {
                                MostRecentPartyTurnArchitect.Inventory.Remove(item);
                                if (MostRecentPartyTurnArchitect.Room != null)
                                {
                                    MostRecentPartyTurnArchitect.Room.Objects.Add(item);
                                    item.Room = MostRecentPartyTurnArchitect.Room;
                                    item.Block = MostRecentPartyTurnArchitect.Block;
                                }
                                else
                                {
                                    MostRecentPartyTurnArchitect.Block.Objects.Add(item);
                                    item.Block = MostRecentPartyTurnArchitect.Block;
                                }
                                MakeObservation("You drop the " + item.ReferredToNames[0] + ".", Color.Yellow, new List<Entity>() { item });
                                MostRecentPartyTurnArchitect.CooldownCycles += (int)(Math.Round(5 / MostRecentPartyTurnArchitect.Speed()));
                            }
                        }

                        int GetHalfCount(string itemType, List<Material> itemMaterials)
                        {
                            int totalCount = MostRecentPartyTurnArchitect.Inventory.Count(item => item.Type == itemType && item.Materials.SequenceEqual(itemMaterials));
                            return (int)Math.Ceiling(totalCount / 2.0);
                        }

                        int GetFullCount(string itemType, List<Material> itemMaterials)
                        {
                            return MostRecentPartyTurnArchitect.Inventory.Count(item => item.Type == itemType && item.Materials.SequenceEqual(itemMaterials));
                        }

                        if (KeysNewlyPressed.Contains(Keys.D1))
                        {
                            DropItems(1); // Drop one item of the selected type and material
                            MostRecentPartyTurnArchitect.TryDropItemType = "";
                            MostRecentPartyTurnArchitect.TryDropMaterials = new List<Material>();
                            GameState = "otherturn";
                        }
                        else if (KeysNewlyPressed.Contains(Keys.D2))
                        {
                            DropItems(GetHalfCount(MostRecentPartyTurnArchitect.TryDropItemType, MostRecentPartyTurnArchitect.TryDropMaterials)); // Drop half of the items of the selected type and material
                            MostRecentPartyTurnArchitect.TryDropItemType = "";
                            MostRecentPartyTurnArchitect.TryDropMaterials = new List<Material>();
                            GameState = "otherturn";
                        }
                        else if (KeysNewlyPressed.Contains(Keys.D3))
                        {
                            DropItems(GetFullCount(MostRecentPartyTurnArchitect.TryDropItemType, MostRecentPartyTurnArchitect.TryDropMaterials)); // Drop all items of the selected type and material
                            MostRecentPartyTurnArchitect.TryDropItemType = "";
                            MostRecentPartyTurnArchitect.TryDropMaterials = new List<Material>();
                            GameState = "otherturn";
                        }
                    }



                    else if (GameState == "travelmenu")
                    {
                        Exposition = new List<TextStorage>();

                        if (KeysNewlyPressed.Contains(Keys.A))
                        {
                            GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Width].RegionallyExplored = true;

                            if(GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Width].MyLocation != null)
                            {
                                GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Width].MyLocation.Explored = true;
                            }
                        }


                        if (KeysNewlyPressed.Contains(Keys.R) || KeysNewlyPressed.Contains(Keys.T) || KeysNewlyPressed.Contains(Keys.D) || KeysNewlyPressed.Contains(Keys.G) || KeysNewlyPressed.Contains(Keys.C) || KeysNewlyPressed.Contains(Keys.V) || KeysNewlyPressed.Contains(Keys.NumPad7) || KeysNewlyPressed.Contains(Keys.NumPad9) || KeysNewlyPressed.Contains(Keys.NumPad4) || KeysNewlyPressed.Contains(Keys.NumPad6) || KeysNewlyPressed.Contains(Keys.NumPad1) || KeysNewlyPressed.Contains(Keys.NumPad3))
                        {
                            int DeltaX = 0;
                            int DeltaZ = 0;

                            if (MapCursorZ % 2 == 0)
                            {
                                //every other row starting with the top one

                                if (KeysNewlyPressed.Contains(Keys.R) || KeysNewlyPressed.Contains(Keys.NumPad7))
                                {
                                    DeltaX = -1;
                                    DeltaZ = -1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.T) || KeysNewlyPressed.Contains(Keys.NumPad9))
                                {
                                    DeltaZ = -1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.D) || KeysNewlyPressed.Contains(Keys.NumPad4))
                                {
                                    DeltaX = -1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.G) || KeysNewlyPressed.Contains(Keys.NumPad6))
                                {
                                    DeltaX = 1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.C) || KeysNewlyPressed.Contains(Keys.NumPad1))
                                {
                                    DeltaX = -1;
                                    DeltaZ = 1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.V) || KeysNewlyPressed.Contains(Keys.NumPad3))
                                {
                                    DeltaZ = 1;
                                }
                            }
                            else
                            {
                                //every other row starting with the second-to-top one

                                if (KeysNewlyPressed.Contains(Keys.R) || KeysNewlyPressed.Contains(Keys.NumPad7))
                                {
                                    DeltaZ = -1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.T) || KeysNewlyPressed.Contains(Keys.NumPad9))
                                {
                                    DeltaZ = -1;
                                    DeltaX = 1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.D) || KeysNewlyPressed.Contains(Keys.NumPad4))
                                {
                                    DeltaX = -1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.G) || KeysNewlyPressed.Contains(Keys.NumPad6))
                                {
                                    DeltaX = 1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.C) || KeysNewlyPressed.Contains(Keys.NumPad1))
                                {
                                    DeltaZ = 1;
                                }
                                else if (KeysNewlyPressed.Contains(Keys.V) || KeysNewlyPressed.Contains(Keys.NumPad3))
                                {
                                    DeltaZ = 1;
                                    DeltaX = 1;
                                }
                            }

                            if (!(MapCursorX + DeltaX >= GameWorld.Width || MapCursorX + DeltaX < 0 || MapCursorZ + DeltaZ >= GameWorld.Length || MapCursorZ + DeltaZ < 0))
                            {
                                bool isDestinationOcean = GameWorld.WorldMap[(MapCursorX + DeltaX) + ((MapCursorZ + DeltaZ) * GameWorld.Width)].Biome == "ocean";
                                bool isDestinationVoid = GameWorld.WorldMap[(MapCursorX + DeltaX) + ((MapCursorZ + DeltaZ) * GameWorld.Width)].Biome == "void";
                                bool isDestinationEthereal = GameWorld.WorldMap[(MapCursorX + DeltaX) + ((MapCursorZ + DeltaZ) * GameWorld.Width)].Biome == "ethereal";
                                bool isDestinationPort = (!string.IsNullOrEmpty(GameWorld.WorldMap[(MapCursorX + DeltaX) + ((MapCursorZ + DeltaZ) * GameWorld.Width)].PortName)) || (GameWorld.WorldMap[(MapCursorX + DeltaX) + ((MapCursorZ + DeltaZ) * GameWorld.Width)].MyLocation != null && GameWorld.WorldMap[(MapCursorX + DeltaX) + ((MapCursorZ + DeltaZ) * GameWorld.Width)].MyLocation.Type == "cove");
                                bool isCurrentLocationWater = GameWorld.WorldMap[MapCursorX + (MapCursorZ * GameWorld.Width)].Biome == "ocean";

                                // Allow moving to an ocean tile if it has a port, and always allow moving from water to any tile
                                if (!isDestinationVoid && !isDestinationEthereal && (isCurrentLocationWater || (!isDestinationOcean || isDestinationPort)))
                                {
                                    MapCursorX += DeltaX;
                                    MapCursorZ += DeltaZ;

                                    GameWorld.RevealNearbyTiles(MapCursorX, MapCursorZ);

                                    //events lmaoooooz


                                    //THIS ASSSUMES THAT A TILE IS APPROXIMATELY 2.5 KM^2 IN SIZE. THE ENTIRE ISLAND (if 128x128) WOULD BE ABOUT A 300 KILOMETER DIAMETER ISLAND. (not counting the ocean)
                                    //So the world is about the size of Iceland, I suppose.
                                    //It would take about 30 minutes to "walk" 2.5 KM. IF you have increased speed ill make it faster ig. 

                                    //but its based on the weakest link in your party.



                                    GameWorld.Cycle += (int)Math.Round(18000 / GamePlayerParty.Architects.Min(architect => architect.Speed()));

                                    int currentDay = (int)Math.Round((double)GameWorld.Cycle / 864000);

                                    if (currentDay > LatestTraveledDay)
                                    {
                                        GameWorld.ProgressDays(1, false);
                                        LatestTraveledDay = currentDay;
                                    }






                                    if (StoredEvent != null)
                                    {
                                        if (StoredEvent.Region.X != MapCursorX || StoredEvent.Region.Z != MapCursorZ)
                                        {
                                            StoredEvent = null;
                                        }
                                    }
                                    else
                                    {
                                        foreach (InteractableEvent e in GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Width].Events)
                                        {
                                            if (r.Next(1, 4) != 1) //2/3 chance, you might miss it idk
                                            {
                                                //if were successful, DONT do another. break out of the loop.
                                                StoredEvent = e;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                        }

                        if (GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Width].MyLocation == null)
                        {
                            Dictionary<string, List<string>> biomeDictionary = new Dictionary<string, List<string>>();

                            biomeDictionary["forest"] = new List<string> { "wood" };
                            biomeDictionary["lightforest"] = new List<string> { "wood" };
                            biomeDictionary["mountain"] = new List<string> { "stone", "ice" };
                            biomeDictionary["snowpeak"] = new List<string> { "stone", "metal", "ice" };
                            biomeDictionary["desert"] = new List<string> { "sand" };
                            biomeDictionary["plains"] = new List<string> { "fiber" };
                            biomeDictionary["tundra"] = new List<string> { "ice" };
                            biomeDictionary["taiga"] = new List<string> { "ice", "wood" };
                            biomeDictionary["void"] = new List<string> { "nothing" };

                            if (GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Width].Biome == "ocean")
                            {
                                if (GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Width].PortName != "")
                                {
                                    Exposition.Add(new TextStorage("You can leave this port to start sailing.", Color.LightBlue, new List<Entity>()));
                                }
                                else
                                {
                                    Exposition.Add(new TextStorage("You are currently sailing.", Color.LightBlue, new List<Entity>()));
                                }
                            }
                            else
                            {
                                Exposition.Add(new TextStorage("The area is vacant and beautiful.", Color.Magenta, new List<Entity>()));
                                Exposition.Add(new TextStorage("You could gather " + ConvertListToString(biomeDictionary[GameWorld.WorldMap[MapCursorX + MapCursorZ * 128].Biome]) + " here.", Color.Magenta, new List<Entity>()));
                                Exposition.Add(new TextStorage("Press [S] to stop here.", Color.Magenta, new List<Entity>()));
                            }
                        }

                        if (StoredEvent != null)
                        {
                            Exposition.Clear();
                            Exposition.Add(new TextStorage(StoredEvent.Intrigue, Color.LimeGreen, new List<Entity>()));
                            Exposition.Add(new TextStorage("Press [E] to approach.", Color.Red, new List<Entity>()));
                        }



                        if (KeysNewlyPressed.Contains(Keys.S) && GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Width].MyLocation == null)
                        {
                            GameState = "gathering";
                        }
                        else if (KeysNewlyPressed.Contains(Keys.E) && StoredEvent != null)
                        {
                            GamePlayerParty.CurrentEvent = StoredEvent;
                            GameState = "exposition";
                        }

                        if (KeysNewlyPressed.Contains(Keys.OemComma))
                        {
                            GamePlayerParty.MapCursorDistrict -= 1;
                            if (GamePlayerParty.MapCursorDistrict < 0)
                            {
                                GamePlayerParty.MapCursorDistrict = GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Width].MyLocation.Districts.Count - 1;
                            }
                        }
                        if (KeysNewlyPressed.Contains(Keys.OemPeriod))
                        {
                            GamePlayerParty.MapCursorDistrict += 1;
                            if (GamePlayerParty.MapCursorDistrict > GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Width].MyLocation.Districts.Count - 1)
                            {
                                GamePlayerParty.MapCursorDistrict = 0;
                            }
                        }


                        if (KeysNewlyPressed.Contains(Keys.Enter) && GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Width].MyLocation != null)
                        {
                            GameState = "exposition";
                            Location newLocation = GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Width].MyLocation;
                            District newDistrict = newLocation.Districts[GamePlayerParty.MapCursorDistrict];

                            // Create a collective arrival message for the party
                            Exposition.Add(new TextStorage("You arrive at " + newLocation.Name + ", on the outskirts of " + newDistrict.Name + ".", Color.Blue, new List<Entity>()));

                            // Define the blocks on the outskirts of a 7x7 district grid
                            List<int> outskirtsBlockIndexes = new List<int>
                            {
                                // Top row and bottom row
                                0, 1, 2, 3, 4, 5, 6, 42, 43, 44, 45, 46, 47, 48,
                                // Left column and right column, excluding corners already included
                                7, 14, 21, 28, 35, 13, 20, 27, 34, 41
                            };

                            // Randomly pick one block from the outskirts
                            Random r = new Random();
                            int randomBlockIndex = outskirtsBlockIndexes[r.Next(outskirtsBlockIndexes.Count)];
                            Block arrivalBlock = newDistrict.DistrictMap[randomBlockIndex];

                            // Continue with the original logic...
                            // Determine structure counts
                            List<Structure> taverns = new List<Structure>();
                            List<Structure> markets = new List<Structure>();
                            List<Structure> houses = new List<Structure>();

                            // Iterate over all blocks in the district to categorize structures
                            for (int x = 0; x < 7; x++)
                            {
                                for (int z = 0; z < 7; z++)
                                {
                                    foreach (Structure s in newDistrict.DistrictMap[x + z * 7].Structures)
                                    {
                                        switch (s.Type)
                                        {
                                            case "tavern":
                                                taverns.Add(s);
                                                break;
                                            case "market":
                                                markets.Add(s);
                                                break;
                                            case "house":
                                                houses.Add(s);
                                                break;
                                        }
                                    }
                                }
                            }

                            // Add exposition for spires, architect counts, taverns, and markets, similar to previous code...
                            // Load the new location and update all architects
                            newDistrict.Load();
                            foreach (Architect architect in GamePlayerParty.Architects)
                            {
                                architect.Location = newLocation;
                                architect.District = newDistrict;
                                architect.Block = arrivalBlock;  // Set all architects to the randomly selected outskirts block
                                architect.TryingToTravel = false;  // Reset travel intent
                            }
                        }



                    }
                    else if (GameState == "exposition")
                    {
                        if (GamePlayerParty.CurrentEvent != null)
                        {
                            Exposition = new List<TextStorage>
                            {
                                new TextStorage(GamePlayerParty.CurrentEvent.Info, Color.White, new List<Entity>()),
                                new TextStorage("Do you approach? (press Y or N)", Color.Yellow, new List<Entity>())
                            };

                            if (KeysNewlyPressed.Contains(Keys.Y))
                            {
                                MostRecentPartyTurnArchitect.Location = new Location("clearing", GameWorld.GetRace(""), 0, 0, 0, MapCursorX, MapCursorZ, GamePlayerParty.CurrentEvent.HomeCivilization, GamePlayerParty.CurrentEvent.Region, "none");
                                MostRecentPartyTurnArchitect.District = MostRecentPartyTurnArchitect.Location.Districts[0];
                                MostRecentPartyTurnArchitect.Block = MostRecentPartyTurnArchitect.District.DistrictMap[new[] { 0, 1, 2, 3, 4, 5, 6, 7, 13, 14, 20, 21, 27, 28, 34, 35, 41, 42, 43, 44, 45, 46, 47, 48 }[new Random().Next(24)]];

                                MostRecentPartyTurnArchitect.District.Load();

                                LoadedArchitects.Clear();

                                foreach (Architect a in GamePlayerParty.CurrentEvent.GuarranteedArchitects)
                                {
                                    a.Location = MostRecentPartyTurnArchitect.Location;
                                    a.District = MostRecentPartyTurnArchitect.District;
                                    a.Block = MostRecentPartyTurnArchitect.District.DistrictMap[new[] { 0, 1, 2, 3, 4, 5, 6, 7, 13, 14, 20, 21, 27, 28, 34, 35, 41, 42, 43, 44, 45, 46, 47, 48 }[new Random().Next(24)]];
                                    a.Block.Architects.Add(a);
                                    LoadedArchitects.Add(a);
                                }

                                LoadedArchitects.AddRange(GamePlayerParty.Architects);

                                GameState = "partyturn";
                            }
                            else if (KeysNewlyPressed.Contains(Keys.N))
                            {
                                GamePlayerParty.CurrentEvent = null;
                                StoredEvent = null;
                                GameState = "travelmenu";
                                GamePlayerParty.ClearSkillData();
                            }
                        }
                        else
                        {
                            if (KeysNewlyPressed.Contains(Keys.Space))
                            {
                                if (SeenTips == true)
                                {
                                    GameState = "otherturn";
                                    GamePlayerParty.Architects[0].UpdateNames();

                                    if(InspirationSelected != "")
                                    {
                                        if (InspirationSelected == "Learn a random offensive spell.")
                                        {
                                            List<string> OffensiveSpells = new List<string> { "expel", "water bolt", "chaos flare", "concentrated ignition", "tremor", "ice shock" };
                                            var knownSpells = GamePlayerParty.Architects[0].SpellsKnown;
                                            var availableSpells = OffensiveSpells.Except(knownSpells).ToList();
                                            if (availableSpells.Any())
                                            {
                                                Random random = new Random();
                                                string randomSpell = availableSpells[random.Next(availableSpells.Count)];
                                                knownSpells.Add(randomSpell);
                                                Announcements.Add(new TextStorage($"Learned a new spell: {randomSpell}", Color.White, new List<Entity>()));
                                            }
                                        }
                                        else if (InspirationSelected == "Learn a random skill.")
                                        {
                                            var knownSkills = GamePlayerParty.Architects[0].SkillsKnown;
                                            var availableSkills = AllSkills.Except(knownSkills).ToList();
                                            if (availableSkills.Any())
                                            {
                                                Random random = new Random();
                                                string randomSkill = availableSkills[random.Next(availableSkills.Count)];
                                                knownSkills.Add(randomSkill);
                                                Announcements.Add(new TextStorage($"Learned a new random skill: {randomSkill}", Color.White, new List<Entity>()));
                                            }
                                        }
                                        else if (InspirationSelected == "Gain 2 random stat improvements.")
                                        {
                                            List<string> stats = new List<string> { "Strength", "Dexterity", "Agility", "Endurance", "Charisma", "Focus", "Creativity" };
                                            Random random = new Random();
                                            for (int i = 0; i < 2; i++)
                                            {
                                                if (stats.Count == 0) break;
                                                int index = random.Next(stats.Count);
                                                string statToIncrease = stats[index];
                                                stats.RemoveAt(index);
                                                switch (statToIncrease)
                                                {
                                                    case "Strength":
                                                        GamePlayerParty.Architects[0].Strength += 1;
                                                        Announcements.Add(new TextStorage("Strength increased by 1.", Color.White, new List<Entity>()));
                                                        break;
                                                    case "Dexterity":
                                                        GamePlayerParty.Architects[0].Dexterity += 1;
                                                        Announcements.Add(new TextStorage("Dexterity increased by 1.", Color.White, new List<Entity>()));
                                                        break;
                                                    case "Agility":
                                                        GamePlayerParty.Architects[0].Agility += 1;
                                                        Announcements.Add(new TextStorage("Agility increased by 1.", Color.White, new List<Entity>()));
                                                        break;
                                                    case "Endurance":
                                                        GamePlayerParty.Architects[0].Endurance += 1;
                                                        Announcements.Add(new TextStorage("Endurance increased by 1.", Color.White, new List<Entity>()));
                                                        break;
                                                    case "Charisma":
                                                        GamePlayerParty.Architects[0].Charisma += 1;
                                                        Announcements.Add(new TextStorage("Charisma increased by 1.", Color.White, new List<Entity>()));
                                                        break;
                                                    case "Focus":
                                                        GamePlayerParty.Architects[0].Focus += 1;
                                                        Announcements.Add(new TextStorage("Focus increased by 1.", Color.White, new List<Entity>()));
                                                        break;
                                                    case "Creativity":
                                                        GamePlayerParty.Architects[0].Creativity += 1;
                                                        Announcements.Add(new TextStorage("Creativity increased by 1.", Color.White, new List<Entity>()));
                                                        break;
                                                }
                                            }
                                        }
                                        else if (InspirationSelected == "Gain +5 Max Energy.")
                                        {
                                            GamePlayerParty.Architects[0].MaxEnergyInspiration = true;
                                            Announcements.Add(new TextStorage("Your max energy has been increased.", Color.White, new List<Entity>()));
                                        }
                                        else if (InspirationSelected == "Obtain a minor magical item.")
                                        {
                                            var magicalItems = GameWorld.MagicalSuperLoot(5);
                                            GamePlayerParty.Architects[0].Inventory.Add(magicalItems);
                                            Announcements.Add(new TextStorage("You have obtained a magical item.", Color.White, new List<Entity>()));
                                        }

                                        InspirationSelected = "";
                                        InspirationsChosen.Clear();
                                    }
                                }
                                else
                                {
                                    SeenTips = true;
                                    Exposition.Clear();
                                    Exposition.Add(new TextStorage("Press F5 for help with vocal/GUI/typed commands and navigating the world.", Color.White, new List<Entity>()));
                                    Exposition.Add(new TextStorage("Watch your Energy and Bleeding, dictated by the Heart and its droplets at the top of the screen. Do not let it go dark.", Color.White, new List<Entity>()));
                                    Exposition.Add(new TextStorage("Access player information with commands or use \"open menu\" to see all of it.", Color.White, new List<Entity>()));
                                    Exposition.Add(new TextStorage("Good luck...", Color.White, new List<Entity>()));
                                    Exposition.Add(new TextStorage("", Color.White, new List<Entity>()));
                                    Exposition.Add(new TextStorage("", Color.White, new List<Entity>()));
                                    Exposition.Add(new TextStorage("", Color.White, new List<Entity>()));
                                    Exposition.Add(new TextStorage("", Color.White, new List<Entity>()));
                                    Exposition.Add(new TextStorage("", Color.White, new List<Entity>()));
                                    Exposition.Add(new TextStorage("", Color.White, new List<Entity>()));
                                    Exposition.Add(new TextStorage("", Color.White, new List<Entity>()));
                                    Exposition.Add(new TextStorage("Press ENTER to continue...", Color.White, new List<Entity>()));
                                }
                            }
                        }
                    }
                    else if (GameState == "gathering")
                    {
                        if (KeysNewlyPressed.Contains(Keys.X))
                        {
                            // Transition to the travel menu
                            GameState = "travelmenu";
                            GamePlayerParty.ClearSkillData();
                        }

                        // Mapping of biomes to the tools required for gathering
                        var gatheringActions = new Dictionary<string, string> {
                            {"forest", "axe"}, {"lightforest", "axe"}, {"taiga", "axe"},
                            {"mountain", "pickaxe"}, {"snowpeak", "pickaxe"},
                            {"desert", "shovel"},
                            {"plains", "scythe, sword"},
                            {"tundra", "pickaxe"}, {"ice", "pickaxe"}
                        };

                        // Mapping of biomes to harvestable materials
                        var harvestableMaterials = new Dictionary<string, Material> {
                            {"forest", GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].HarvestableWood},
                            {"lightforest", GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].HarvestableWood},
                            {"taiga", GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].HarvestableWood},
                            {"mountain", GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].HarvestableStone},
                            {"snowpeak", GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].HarvestableMetal},
                            {"desert", GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].HarvestableSand},
                            {"plains", GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].HarvestableFiber},
                            {"tundra", GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].HarvestableIce},
                            {"ice", GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].HarvestableIce}
                        };

                        // Mapping materials to the type of resource they represent
                        Dictionary<Material, string> ResourcetoType = new Dictionary<Material, string>()
                        {
                            {GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].HarvestableWood, "log"},
                            {GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].HarvestableStone, "stone"},
                            {GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].HarvestableMetal, "ore"},
                            {GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].HarvestableSand, "pile"},
                            {GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].HarvestableFiber, "bunch"},
                            {GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].HarvestableIce, "block"}
                        };

                        // Mapping of numeric keys to resource types
                        var resourceKeyMap = new Dictionary<int, string> {
                            {1, "wood"},
                            {2, "stone"},
                            {3, "metal"},
                            {4, "sand"},
                            {5, "fiber"},
                            {6, "ice"}
                        };

                        bool HasRequiredTool(Architect architect, string toolRequirements)
                        {
                            if (string.IsNullOrEmpty(toolRequirements))
                            {
                                // If no tool is required, return true
                                return true;
                            }

                            // Split the tool requirements into an array, as some biomes might allow multiple tools
                            string[] requiredTools = toolRequirements.Split(new[] { ", ", " or " }, StringSplitOptions.RemoveEmptyEntries);

                            // Check each tool in the required tools list to see if the architect has it
                            foreach (string requiredTool in requiredTools)
                            {
                                // Assuming the Architect class has an Inventory property that is a list of some kind of Tool or Item objects
                                if (architect.Inventory.Any(item => item.Type.Equals(requiredTool, StringComparison.OrdinalIgnoreCase)))
                                {
                                    // Return true as soon as one required tool is found
                                    return true;
                                }
                            }

                            // Return false if none of the required tools are found in the architect's inventory
                            return false;
                        }


                        // Iterate through each mapped key to resource type
                        foreach (var resourceKey in resourceKeyMap)
                        {
                            if (KeysNewlyPressed.Contains((Keys)Enum.Parse(typeof(Keys), $"D{resourceKey.Key}")))
                            {
                                // Check if the biome has the corresponding resource
                                Material resourceMaterial = harvestableMaterials.FirstOrDefault(hm => hm.Value.Type.Contains(resourceKey.Value)).Value;
                                if (resourceMaterial != null)
                                {
                                    string resourceType = ResourcetoType[resourceMaterial];
                                    string biome = GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome;
                                    string toolRequired = gatheringActions.ContainsKey(biome) ? gatheringActions[biome] : null;
                                    bool toolNeeded = true;

                                    foreach (var architect in GamePlayerParty.Architects)
                                    {
                                        if (HasRequiredTool(architect, toolRequired))
                                        {
                                            toolNeeded = false;
                                            int count = r.Next(0, architect.Strength + 1);

                                            // List of break activities for architects when they don't gather resources
                                            List<string> breakActivities = new List<string>()
                                            {
                                                "sleeps for a bit",
                                                "gossips",
                                                "provides emotional support",
                                                "stands by and supervises",
                                                "ponders the meaning of life",
                                                "whistles a cheerful tune",
                                                "checks their equipment",
                                                "looks around cautiously",
                                                "starts a small chat",
                                                "takes a deep breath"
                                            };

                                            // Code inside the harvesting loop
                                            if (count > 0)
                                            {
                                                Exposition.Add(new TextStorage($"{architect.Name} harvested {count} {resourceType}.", Color.LightBlue, new List<Entity>()));
                                                for (int i = 0; i < count; i++)
                                                {
                                                    architect.Inventory.Add(new Object(null, resourceType, new List<Material>() { resourceMaterial }, null));
                                                }
                                            }
                                            else
                                            {
                                                // Select a random break activity
                                                string breakActivity = breakActivities[r.Next(breakActivities.Count)];
                                                Exposition.Add(new TextStorage($"{architect.Name} {breakActivity}.", Color.LightBlue, new List<Entity>()));
                                            }

                                        }
                                    }

                                    if (toolNeeded)
                                    {
                                        Exposition.Add(new TextStorage($"You need a {toolRequired} to harvest {resourceMaterial.Type} here.", Color.Yellow, new List<Entity>()));
                                    }
                                }
                                else
                                {
                                    Exposition.Add(new TextStorage($"There is no {resourceKey.Value.ToLower().Replace("harvestable", "")} to harvest in this biome.", Color.Red, new List<Entity>()));
                                }
                            }
                        }
                    }
                    else if (GameState == "crafting")
                    {
                        if (CraftingPhase == "selectrecipe")
                        {
                            if (KeysNewlyPressed.Contains(Keys.Escape))
                            {
                                GameState = "partyturn";
                                MostRecentPartyTurnArchitect.Crafting = false;
                            }

                            if (KeysNewlyPressed.Contains(Keys.Up) || KeysNewlyPressed.Contains(Keys.NumPad8))
                            {
                                RecipeIndex--;

                                if (RecipeIndex < 0)
                                {
                                    RecipeIndex = Recipes.Count - 1;
                                }
                            }
                            else if (KeysNewlyPressed.Contains(Keys.Down) || KeysNewlyPressed.Contains(Keys.NumPad2))
                            {
                                RecipeIndex++;

                                if (RecipeIndex > Recipes.Count - 1)
                                {
                                    RecipeIndex = 0;
                                }
                            }
                            if (KeysNewlyPressed.Contains(Keys.Enter))
                            {
                                CraftingPhase = "selectingredients";
                            }
                        }
                        else if (CraftingPhase == "selectingredients")
                        {
                            if (KeysNewlyPressed.Contains(Keys.Escape))
                            {
                                CraftingPhase = "selectrecipe";
                                IndexesForResources = new List<int>();
                            }

                            if (KeysNewlyPressed.Contains(Keys.Up) || KeysNewlyPressed.Contains(Keys.NumPad8))
                            {
                                InventoryCraftingIndex--;

                                if (InventoryCraftingIndex < 0)
                                {
                                    InventoryCraftingIndex = Recipes.Count - 1;
                                }
                            }
                            else if (KeysNewlyPressed.Contains(Keys.Down) || KeysNewlyPressed.Contains(Keys.NumPad2))
                            {
                                InventoryCraftingIndex++;

                                if (InventoryCraftingIndex > Recipes.Count - 1)
                                {
                                    InventoryCraftingIndex = 0;
                                }
                            }
                            else if (KeysNewlyPressed.Contains(Keys.Right) || KeysNewlyPressed.Contains(Keys.NumPad6))
                            {
                                if (!IndexesForResources.Contains(InventoryCraftingIndex))
                                {
                                    IndexesForResources.Add(InventoryCraftingIndex);
                                }
                            }

                            if (KeysNewlyPressed.Contains(Keys.Enter))
                            {
                                // Prepare to check inventory items against the selected recipe
                                var currentRecipe = Recipes[RecipeIndex];
                                bool hasAllIngredients = true;
                                List<Material> allUsedMaterials = new List<Material>();  // List to store all used materials

                                // Dictionary to map material types to object types
                                Dictionary<string, string> materialToObjectMap = new Dictionary<string, string>
                                {
                                    {"cloth", "bolt"},
                                    {"wood", "log"},
                                    {"stone", "stone"},
                                    {"metal", "bar"},
                                    {"gemstone", "gemstone"},
                                    {"sand", "pile"},
                                    {"fiber", "bunch"},
                                    {"ice", "block"},
                                    {"glass", "sheet"}
                                };

                                // Count needed items from the recipe
                                var requiredItems = currentRecipe.Item2
                                    .GroupBy(x => x)
                                    .ToDictionary(g => g.Key, g => g.Count());

                                // Check if the player has all the required items in the inventory
                                foreach (var requiredItem in requiredItems)
                                {
                                    string requiredType = requiredItem.Key;
                                    int requiredCount = requiredItem.Value;
                                    string requiredObject = materialToObjectMap[requiredType];

                                    // Count how many items of the required type and object the player has
                                    int countFound = MostRecentPartyTurnArchitect.Inventory
                                        .Count(obj => obj.Type == requiredObject && obj.Materials[0].Type == requiredType);

                                    if (countFound < requiredCount)
                                    {
                                        hasAllIngredients = false;
                                        break;
                                    }
                                }

                                if (hasAllIngredients)
                                {
                                    // Consume the required items
                                    foreach (var requiredItem in requiredItems)
                                    {
                                        string requiredType = requiredItem.Key;
                                        int requiredCount = requiredItem.Value;
                                        string requiredObject = materialToObjectMap[requiredType];

                                        // Initialize a counter for how many materials have been added
                                        int materialsAdded = 0;

                                        // Find and remove the required number of items from inventory
                                        var itemsToRemove = MostRecentPartyTurnArchitect.Inventory
                                            .Where(obj => obj.Type == requiredObject && obj.Materials[0].Type == requiredType)
                                            .ToList();

                                        foreach (var item in itemsToRemove)
                                        {
                                            foreach (var material in item.Materials)
                                            {
                                                if (material.Type == requiredType && materialsAdded < requiredCount)
                                                {
                                                    allUsedMaterials.Add(material); // Add the material of the consumed item
                                                    materialsAdded++;
                                                }
                                            }
                                            MostRecentPartyTurnArchitect.Inventory.Remove(item);

                                            // Break out of the loop if the required amount of materials has been added
                                            if (materialsAdded >= requiredCount)
                                                break;
                                        }
                                    }


                                    // Add the crafted item to the inventory
                                    Object o = new Object("", currentRecipe.Item1, allUsedMaterials, MostRecentPartyTurnArchitect);
                                    o.Name = GameWorld.GenerateUniqueName("1S" + r.Next(2, 5) + "sw", o);
                                    MostRecentPartyTurnArchitect.Inventory.Add(o);

                                    MakeObservation(MostRecentPartyTurnArchitect.Name + " has created " + o.Name + ".", Color.Coral, new List<Entity>() {MostRecentPartyTurnArchitect, o});
                                    GameState = "otherturn";

                                    if (o.Imbuements.Count > 1 || o.IsWeapon || o.Name != null)
                                    {
                                        IsInGui = true;

                                        if (o.Name != null)
                                        {
                                            ItemPickupGuiLines.Add(Capitalize(o.Name) + ", " + Capitalize(o.Materials[0].Name) + " " + Capitalize(o.Type));
                                        }
                                        else
                                        {
                                            ItemPickupGuiLines.Add(Capitalize(o.Materials[0].Name) + " " + Capitalize(o.Type));
                                        }

                                        if (o.Imbuements.Count == 0)
                                        {
                                            ItemPickupGuiLines.Add("This object has no imbuements.");
                                        }
                                        else
                                        {
                                            List<string> ImbuementDescriptions = new List<string>();
                                            foreach (Imbuement i in o.Imbuements)
                                            {
                                                ImbuementDescriptions.Add(i.GetDescription());
                                            }
                                            ItemPickupGuiLines.Add("This object has some intriguing properties. " + ConvertListToString(ImbuementDescriptions));
                                        }
                                    }

                                    // Update crafting phase or state as necessary
                                    GameState = "otherturn";

                                    MostRecentPartyTurnArchitect.CooldownCycles += (int)Math.Round((600 / MostRecentPartyTurnArchitect.Speed()));
                                }
                                else
                                {
                                    Exposition.Add(new TextStorage("Not enough materials to craft.", Color.Red, new List<Entity>()));
                                    CraftingPhase = "selectrecipe";
                                }
                            }
                        }
                    }


                }
            }


            previousState = Keyboard.GetState();

            previousMouseState = currentMouseState;



            base.Update(gameTime);
        }




        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // Fake Replacement for Resolution Things

            EntityHitboxes.Clear();

            float scaleX = (float)_graphics.PreferredBackBufferWidth / 2560f;
            float scaleY = (float)_graphics.PreferredBackBufferHeight / 1440f;

            Matrix scaleMatrix = Matrix.CreateScale(scaleX, scaleY, 1);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, scaleMatrix);

            void DrawWorld()
            {
                // Variables for elevation-based color adjustment
                float minBrightness = 0.0f; // Minimum brightness for the lowest elevation
                float maxBrightness = 1.55f; // Maximum brightness for the highest elevation

                void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color)
                {
                    Vector2 direction = end - start;
                    float rotation = (float)Math.Atan2(direction.Y, direction.X);
                    float length = direction.Length();

                    spriteBatch.Draw(
                        texture: whiteRect,
                        position: start,
                        sourceRectangle: null,
                        color: color,
                        rotation: rotation,
                        origin: new Vector2(0, 0.5f),
                        scale: new Vector2(length, 1),
                        effects: SpriteEffects.None,
                        layerDepth: 0
                    );
                }

                // Start drawing with additive blend state for region tiles (elevation)
                _spriteBatch.End();
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, null, null, null, null, scaleMatrix);

                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {
                        int index = x + z * GameWorld.Width;
                        Rectangle tileRect = new Rectangle(
                            (RegionXMod + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0),
                            RegionYMod + z * TileZDistance,
                            TileSize,
                            TileSize
                        );

                        if ((GameState == "travelmenu" || GameState == "etherealrupture") && !GameWorld.WorldMap[index].Explored)
                        {
                            continue; // Skip drawing unexplored tiles in travelmode
                        }

                        // Draw cursor or tile
                        if (MapCursorX == x && MapCursorZ == z && MapCursorX != 0 && MapCursorZ != 0)
                        {
                            _spriteBatch.Draw(CursorT, tileRect, Color.White);
                        }
                        else
                        {
                            // Adjust tile brightness based on elevation
                            float elevation = GameWorld.ElevationNoiseValues[index];
                            float normalizedElevation = elevation / 256f;
                            float brightness = MathHelper.Lerp(minBrightness, maxBrightness, normalizedElevation);
                            Color baseColor = new Color(brightness, brightness, brightness); // Base color adjusted by elevation

                            // Apply blight effect on top of the elevation-adjusted base color
                            Color finalColor = GameWorld.WorldMap[index].Blight != GameWorld.Purity ?
                                Color.Lerp(baseColor, new Color(100, 100, 100), 1.0f) : baseColor;

                            // Draw the region tile with combined elevation and blight-adjusted color
                            _spriteBatch.Draw(TileAtlas[GameWorld.WorldMap[index].Biome], tileRect, finalColor);
                        }

                        if (GameState == "generatehistory")
                        {
                            foreach ((int, int) TragedyPoint in GameWorld.WorldMap[index].TragedyPoints)
                            {
                                _spriteBatch.Draw(whiteRect, new Rectangle(GameWorld.WorldMap[index].BoundingBox().Center.X + TragedyPoint.Item1 - 1, GameWorld.WorldMap[index].BoundingBox().Center.Y + TragedyPoint.Item2 - 1, 3, 3), Color.Red);
                            }
                        }
                    }
                }

                // End the first phase of drawing
                _spriteBatch.End();

                // Begin second phase for drawing structures, ports without elevation or blight adjustments
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, scaleMatrix);

                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {
                        int index = x + z * GameWorld.Width;
                        Rectangle tileRect = GameWorld.WorldMap[index].BoundingBox();

                        // Check if the region and the location (if it exists) are both explored
                        if (GameWorld.WorldMap[index].Explored || GameState != "travelmenu")
                        {
                            // Draw Ownership
                            if (GameWorld.WorldMap[index].Owner != null && GameState == "generatehistory")
                            {
                                _spriteBatch.Draw(OutlineT, tileRect, ColorConverter[GameWorld.WorldMap[index].Owner.Color]);
                            }

                            // Draw the port if it exists and the region is explored
                            if (GameWorld.WorldMap[index].PortName != "" &&
                                (GameState != "travelmenu" || GameWorld.WorldMap[index].Explored))
                            {
                                _spriteBatch.Draw(PortT, tileRect, Color.White);
                            }

                            // Draw locations based on exploration status
                            if (GameWorld.WorldMap[index].MyLocation != null &&
                                (GameState != "travelmenu" || GameWorld.WorldMap[index].MyLocation.Explored))
                            {
                                var location = GameWorld.WorldMap[index].MyLocation;

                                if (GameWorld.ProcgenStructures.Contains(location.Type))
                                {
                                    _spriteBatch.Draw(
                                        TileAtlas[location.Layout],
                                        tileRect,
                                        ColorConverter[location.Color]
                                    );
                                }
                                else
                                {
                                    _spriteBatch.Draw(
                                        TileAtlas[location.PrimaryRace.Name + location.Type],
                                        tileRect,
                                        Color.White
                                    );
                                }
                            }
                        }
                    }
                }

                //trade lines

                if(GameState == "generatehistory")
                {
                    foreach (Group g in GameWorld.Groups)
                    {
                        List<Vector2> routeCenters = new List<Vector2>();

                        // Get the center of each region in the trade route
                        foreach (Location location in g.TradeRoute)
                        {
                            Vector2 center = location.Region.BoundingBox().Center.ToVector2();
                            routeCenters.Add(center);
                        }

                        // Draw lines between consecutive locations and connect the last location to the first
                        for (int i = 0; i < routeCenters.Count; i++)
                        {
                            Vector2 start = routeCenters[i];
                            Vector2 end = routeCenters[(i + 1) % routeCenters.Count]; // Wrap around for the last point

                            DrawLine(_spriteBatch, start, end, Color.SaddleBrown);
                        }
                    }

                }

                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {
                        // Define the radius of the detection sphere
                        int radius = 2; // Example: radius of 2 tiles. Adjust this value as needed.

                        // Function to check if a given tile is within the specified radius of the cursor
                        bool IsWithinRadius(int centerX, int centerZ, int checkX, int checkZ, int radius)
                        {
                            // Convert hex coordinates to cube coordinates
                            int x = checkX - (checkZ - (checkZ & 1)) / 2;
                            int z = checkZ;
                            int y = -x - z;

                            int centerX_cube = centerX - (centerZ - (centerZ & 1)) / 2;
                            int centerZ_cube = centerZ;
                            int centerY_cube = -centerX_cube - centerZ_cube;

                            // Calculate distance in cube coordinates
                            int distance = (Math.Abs(x - centerX_cube) + Math.Abs(y - centerY_cube) + Math.Abs(z - centerZ_cube)) / 2;
                            return distance <= radius;
                        }

                        // Usage of IsWithinRadius in your loop (assuming x and z are the coordinates you're iterating over)
                        bool isWithinRadius = IsWithinRadius(MapCursorX, MapCursorZ, x, z, radius);

                        if (isWithinRadius)
                        {
                            // Initialize flag to check for colossal event
                            bool hasColossal = false;
                            bool hasAnyEvent = false;

                            // Scan for InteractableEvent with Type "colossal" in Region.InteractableEvents
                            foreach (var interactableEvent in GameWorld.WorldMap[x + z * GameWorld.Width].Events)
                            {
                                if (interactableEvent != null)
                                {
                                    hasAnyEvent = true; // There's at least one event
                                    if (interactableEvent.Type == "colossal")
                                    {
                                        hasColossal = true; // Found a colossal event
                                        break; // No need to check further
                                    }
                                }
                            }

                            // Decide the color based on the presence of a colossal event
                            Color drawColor = hasColossal ? Color.Red : hasAnyEvent ? Color.White : Color.Gray; // Gray if no events are present

                            if (drawColor != Color.Gray)
                            {
                                _spriteBatch.Draw(ArchitectHere, GameWorld.WorldMap[x + z * GameWorld.Width].BoundingBox(), drawColor);
                            }
                        }
                    }
                }

                // End the second phase of drawing
                _spriteBatch.End();
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, scaleMatrix);
            }

            void DrawCenteredText(SpriteBatch spriteBatch, string text, float yPosition, SpriteFont Font, Color color)
            {
                // Load the Shibafont SpriteFont
                Vector2 textSize = Font.MeasureString(text);

                // Calculate the center position based on the PreferredBackBufferWidth
                float xPosition = (2560 - textSize.X) / 2;

                // Adjust the y position if needed
                Vector2 position = new Vector2(xPosition, yPosition);

                // Draw the text
                spriteBatch.DrawString(Font, text, position, color);
            }

            void DrawWorldSegment(int centerX, int centerZ, int segmentWidth, int segmentHeight, float tileScale, int tileRadius)
            {
                // Variables for elevation-based color adjustment
                float minBrightness = 0.0f; // Minimum brightness for the lowest elevation
                float maxBrightness = 1.55f; // Maximum brightness for the highest elevation

                // Start drawing with additive blend state for region tiles (elevation)
                _spriteBatch.End();
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, null, null, null, null, scaleMatrix);

                int XMod = Game1.TravelRegionXMod;
                int YMod = Game1.TravelRegionYMod;

                int halfSegmentWidth = segmentWidth / 2;
                int halfSegmentHeight = segmentHeight / 2;

                // Define the fixed position for the map cursor
                Rectangle cursorRect = new Rectangle(
                    XMod + halfSegmentWidth * (int)(Game1.TileXDistance * tileScale),
                    YMod + halfSegmentHeight * (int)(Game1.TileZDistance * tileScale),
                    (int)(Game1.TileSize * tileScale),
                    (int)(Game1.TileSize * tileScale)
                );

                // Draw the map cursor
                _spriteBatch.Draw(CursorT, cursorRect, Color.White);

                // Adjust the drawing positions to be relative to the cursor position
                int cursorX = XMod + halfSegmentWidth * (int)(Game1.TileXDistance * tileScale);
                int cursorY = YMod + halfSegmentHeight * (int)(Game1.TileZDistance * tileScale);

                // Determine if an additional offset is needed for odd Z
                int additionalOffset = (centerZ % 2 == 1) ? -(int)(Game1.TileXDistance * tileScale) / 2 : 0;

                for (int x = -halfSegmentWidth; x <= halfSegmentWidth; x++)
                {
                    for (int z = -halfSegmentHeight; z <= halfSegmentHeight; z++)
                    {
                        int worldX = centerX + x;
                        int worldZ = centerZ + z;

                        // Skip tiles outside the world boundaries
                        if (worldX < 0 || worldX >= GameWorld.Width || worldZ < 0 || worldZ >= GameWorld.Length)
                        {
                            continue; // Skip drawing tiles outside the world boundaries
                        }

                        // Skip drawing the tile where the cursor is
                        if (x == 0 && z == 0)
                        {
                            continue; // Skip the tile under the cursor
                        }

                        // Check if the tile is within the specified radius from the center
                        if (Math.Sqrt(x * x + z * z) > tileRadius)
                        {
                            continue; // Skip drawing tiles outside the radius
                        }

                        int index = worldX + worldZ * GameWorld.Width;

                        // Calculate the tile position relative to the cursor
                        int tileX = cursorX + x * (int)(Game1.TileXDistance * tileScale) + ((worldZ % 2 == 1) ? (int)(Game1.TileXDistance * tileScale) / 2 : 0) + additionalOffset;
                        int tileY = cursorY + z * (int)(Game1.TileZDistance * tileScale);

                        Rectangle tileRect = new Rectangle(tileX, tileY, (int)(Game1.TileSize * tileScale), (int)(Game1.TileSize * tileScale));

                        if ((GameState == "travelmenu" || GameState == "etherealrupture") && !GameWorld.WorldMap[index].Explored)
                        {
                            continue; // Skip drawing unexplored tiles in travelmode
                        }

                        // Define constants
                        float minimumDarkness = 0.6f;
                        int sunriseStartHour = 6;
                        int sunriseEndHour = 8;
                        int sunsetStartHour = 16;
                        int sunsetEndHour = 18;

                        double cycleDurationInSeconds = 0.1;
                        double totalSeconds = GameWorld.Cycle * cycleDurationInSeconds;

                        // Calculate hours from total seconds
                        double hours = (totalSeconds / 3600.0) % 24;

                        // Calculate darkness level based on the time of day
                        float darknessLevel = 0.0f;

                        if (hours >= sunriseEndHour && hours < sunsetStartHour)
                        {
                            // Day time (8:00 AM to 4:00 PM)
                            darknessLevel = 0.0f;
                        }
                        else if (hours >= sunsetStartHour && hours < sunsetEndHour)
                        {
                            // Sunset (4:00 PM to 6:00 PM)
                            float transitionProgress = (float)((hours - sunsetStartHour) / (sunsetEndHour - sunsetStartHour));
                            darknessLevel = transitionProgress * minimumDarkness;
                        }
                        else if (hours >= sunsetEndHour || hours < sunriseStartHour)
                        {
                            // Night time (6:00 PM to 6:00 AM)
                            darknessLevel = minimumDarkness;
                        }
                        else if (hours >= sunriseStartHour && hours < sunriseEndHour)
                        {
                            // Sunrise (6:00 AM to 8:00 AM)
                            float transitionProgress = (float)((hours - sunriseStartHour) / (sunriseEndHour - sunriseStartHour));
                            darknessLevel = minimumDarkness * (1.0f - transitionProgress);
                        }

                        // Adjust tile brightness based on elevation
                        float elevation = GameWorld.ElevationNoiseValues[index];
                        float normalizedElevation = elevation / 256f;
                        float brightness = MathHelper.Lerp(minBrightness, maxBrightness, normalizedElevation);
                        Color baseColor = new Color(brightness, brightness, brightness); // Base color adjusted by elevation

                        // Apply blight effect on top of the elevation-adjusted base color
                        Color finalColor = GameWorld.WorldMap[index].Blight != GameWorld.Purity ?
                            Color.Lerp(baseColor, new Color(100, 100, 100), 1.0f) : baseColor;

                        // Apply additional darkness filter based on the time of day only if necessary
                        finalColor = Color.Lerp(finalColor, new Color(0, 0, 0), darknessLevel);

                        // Draw the region tile with combined elevation, blight, and time-of-day adjusted color
                        _spriteBatch.Draw(TileAtlas[GameWorld.WorldMap[index].Biome], tileRect, finalColor);

                    }
                }

                // End the first phase of drawing
                _spriteBatch.End();

                // Begin second phase for drawing structures, ports without elevation or blight adjustments
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, scaleMatrix);

                for (int x = -halfSegmentWidth; x <= halfSegmentWidth; x++)
                {
                    for (int z = -halfSegmentHeight; z <= halfSegmentHeight; z++)
                    {
                        int worldX = centerX + x;
                        int worldZ = centerZ + z;

                        // Skip tiles outside the world boundaries
                        if (worldX < 0 || worldX >= GameWorld.Width || worldZ < 0 || worldZ >= GameWorld.Length)
                        {
                            continue; // Skip drawing tiles outside the world boundaries
                        }

                        // Skip drawing the tile where the cursor is
                        if (x == 0 && z == 0)
                        {
                            continue; // Skip the tile under the cursor
                        }

                        // Check if the tile is within the specified radius from the center
                        if (Math.Sqrt(x * x + z * z) > tileRadius)
                        {
                            continue; // Skip drawing tiles outside the radius
                        }

                        int index = worldX + worldZ * GameWorld.Width;

                        // Calculate the tile position relative to the cursor
                        int tileX = cursorX + x * (int)(Game1.TileXDistance * tileScale) + ((worldZ % 2 == 1) ? (int)(Game1.TileXDistance * tileScale) / 2 : 0) + additionalOffset;
                        int tileY = cursorY + z * (int)(Game1.TileZDistance * tileScale);

                        Rectangle tileRect = new Rectangle(tileX, tileY, (int)(Game1.TileSize * tileScale), (int)(Game1.TileSize * tileScale));

                        // Check if the region and the location (if it exists) are both explored
                        if (GameWorld.WorldMap[index].Explored || GameState != "travelmenu")
                        {
                            // Draw Ownership
                            if (GameWorld.WorldMap[index].Owner != null && GameState == "generatehistory")
                            {
                                _spriteBatch.Draw(OutlineT, tileRect, ColorConverter[GameWorld.WorldMap[index].Owner.Color]);
                            }

                            // Draw the port if it exists and the region is explored
                            if (GameWorld.WorldMap[index].PortName != "" &&
                                (GameState != "travelmenu" || GameWorld.WorldMap[index].Explored))
                            {
                                _spriteBatch.Draw(PortT, tileRect, Color.White);
                            }

                            // Draw locations based on exploration status
                            if (GameWorld.WorldMap[index].MyLocation != null &&
                                (GameState != "travelmenu" || GameWorld.WorldMap[index].MyLocation.Explored))
                            {
                                var location = GameWorld.WorldMap[index].MyLocation;

                                if (GameWorld.ProcgenStructures.Contains(location.Type))
                                {
                                    _spriteBatch.Draw(
                                        TileAtlas[location.Layout],
                                        tileRect,
                                        ColorConverter[location.Color]
                                    );
                                }
                                else
                                {
                                    _spriteBatch.Draw(
                                        TileAtlas[location.PrimaryRace.Name + location.Type],
                                        tileRect,
                                        Color.White
                                    );
                                }
                            }
                        }
                    }
                }

                // End the second phase of drawing
                _spriteBatch.End();

                // Begin third phase for drawing event indicators
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, scaleMatrix);

                for (int x = -halfSegmentWidth; x <= halfSegmentWidth; x++)
                {
                    for (int z = -halfSegmentHeight; z <= halfSegmentHeight; z++)
                    {
                        int worldX = centerX + x;
                        int worldZ = centerZ + z;

                        // Skip tiles outside the world boundaries
                        if (worldX < 0 || worldX >= GameWorld.Width || worldZ < 0 || worldZ >= GameWorld.Length)
                        {
                            continue; // Skip drawing tiles outside the world boundaries
                        }

                        // Skip drawing the tile where the cursor is
                        if (x == 0 && z == 0)
                        {
                            continue; // Skip the tile under the cursor
                        }

                        // Check if the tile is within the specified radius from the center
                        if (Math.Sqrt(x * x + z * z) > tileRadius)
                        {
                            continue; // Skip drawing tiles outside the radius
                        }

                        int index = worldX + worldZ * GameWorld.Width;

                        // Calculate the tile position relative to the cursor
                        int tileX = cursorX + x * (int)(Game1.TileXDistance * tileScale) + ((worldZ % 2 == 1) ? (int)(Game1.TileXDistance * tileScale) / 2 : 0) + additionalOffset;
                        int tileY = cursorY + z * (int)(Game1.TileZDistance * tileScale);

                        Rectangle tileRect = new Rectangle(tileX, tileY, (int)(Game1.TileSize * tileScale), (int)(Game1.TileSize * tileScale));

                        // Define the radius of the detection sphere
                        int radius = 2; // Example: radius of 2 tiles. Adjust this value as needed.

                        // Function to check if a given tile is within the specified radius of the cursor
                        bool IsWithinRadius(int centerX, int centerZ, int checkX, int checkZ, int radius)
                        {
                            // Convert hex coordinates to cube coordinates
                            int x = checkX - (checkZ - (checkZ & 1)) / 2;
                            int z = checkZ;
                            int y = -x - z;

                            int centerX_cube = centerX - (centerZ - (centerZ & 1)) / 2;
                            int centerZ_cube = centerZ;
                            int centerY_cube = -centerX_cube - centerZ_cube;

                            // Calculate distance in cube coordinates
                            int distance = (Math.Abs(x - centerX_cube) + Math.Abs(y - centerY_cube) + Math.Abs(z - centerZ_cube)) / 2;
                            return distance <= radius;
                        }

                        // Usage of IsWithinRadius in your loop (assuming x and z are the coordinates you're iterating over)
                        bool isWithinRadius = IsWithinRadius(MapCursorX, MapCursorZ, worldX, worldZ, radius);

                        if (isWithinRadius)
                        {
                            // Initialize flag to check for colossal event
                            bool hasColossal = false;
                            bool hasAnyEvent = false;

                            // Scan for InteractableEvent with Type "colossal" in Region.InteractableEvents
                            foreach (var interactableEvent in GameWorld.WorldMap[worldX + worldZ * GameWorld.Width].Events)
                            {
                                if (interactableEvent != null)
                                {
                                    hasAnyEvent = true; // There's at least one event
                                    if (interactableEvent.Type == "colossal")
                                    {
                                        hasColossal = true; // Found a colossal event
                                        break; // No need to check further
                                    }
                                }
                            }

                            // Decide the color based on the presence of a colossal event
                            Color drawColor = hasColossal ? Color.Red : hasAnyEvent ? Color.White : Color.Gray; // Gray if no events are present

                            if (drawColor != Color.Gray)
                            {
                                _spriteBatch.Draw(ArchitectHere, new Rectangle(
                                    tileX,
                                    tileY,
                                    (int)(Game1.TileSize * tileScale),
                                    (int)(Game1.TileSize * tileScale)
                                ), drawColor);
                            }
                        }
                    }
                }

                // End the third phase of drawing
                _spriteBatch.End();

                // Reset SpriteBatch settings to normal
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, scaleMatrix);
            }



            void DrawCharacter(Architect a, int x, int y, double Scale)
            {
                // Calculate scaled dimensions
                int scaledWidth = (int)(300 * Scale);
                int scaledHeight = (int)(1000 * Scale);
                Rectangle ChosenRect = new Rectangle(x, y, scaledWidth, scaledHeight);

                // Setup sampler state for smooth scaling if using XNA/MonoGame
                _spriteBatch.GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

                // New dimensions for MirrorT
                int newWidth = (int)(500 * Scale);
                int newHeight = (int)(1150 * Scale);

                // Calculate the new x and y coordinates to center MirrorT with ChosenRect
                int centerX = x + scaledWidth / 2;  // Center X of the original rectangle
                int centerY = y + scaledHeight / 2; // Center Y of the original rectangle

                int newX = centerX - newWidth / 2;  // New X coordinate for centered placement
                int newY = centerY - newHeight / 2; // New Y coordinate for centered placement

                Rectangle newRect = new Rectangle(newX, newY, newWidth, newHeight);

                // Continue with drawing
                _spriteBatch.Draw(MirrorT, newRect, Color.White);

                // Add hitbox for the character frame
                if (!IsShowingSkills)
                {
                    if (SplitMode && GamePlayerParty.Architects.Contains(a))
                    {
                        // Add a hitbox that only encompasses the top half of the original rectangle
                        Rectangle halfRect = new Rectangle(newRect.X, newRect.Y, newRect.Width, newRect.Height / 2);
                        EntityHitboxes.Add((halfRect, a));
                    }
                    else
                    {
                        EntityHitboxes.Add((newRect, a));
                    }
                }

                // Draw character based on their race and sex
                if (!GameWorld.HumanoidRaces.Contains(a.Race))
                {
                    _spriteBatch.Draw(FlairT, ChosenRect, Color.White);
                    return;
                }

                string baseTexture = a.Sex == "female" ? "FemaleT" : "MaleT";
                string race = a.Race.Name.ToLower();

                switch (race)
                {
                    case "luminarch":
                        _spriteBatch.Draw(baseTexture == "FemaleT" ? LuminarchFemaleT : LuminarchMaleT, ChosenRect, Color.White);
                        break;
                    case "nightfell":
                        _spriteBatch.Draw(baseTexture == "FemaleT" ? NightfellFemaleT : NightfellMaleT, ChosenRect, Color.White);
                        break;
                    default:
                        _spriteBatch.Draw(baseTexture == "FemaleT" ? ArchaixFemaleT : ArchaixMaleT, ChosenRect, Color.White);
                        break;
                }

                // Define the drawing order
                var drawOrder = new List<string[]>
                {
                    new string[] { "body" },
                    new string[] { "undergarment", "uppergarment" },
                    new string[] { "flair" },  // Draw flair before anything that ends with "shirt"
                    new string[] { "leggings", "pants" },  // Always draw these first within this category
                    new string[] { "otherClothing" },
                    new string[] { "armor" },
                    new string[] { "skirt", "kilt" },
                    new string[] { "hats" }
                };

                // Define armor and clothing categories
                var armorItems = new HashSet<string> { "chestplate", "helmet", "left boot", "right boot", "left gauntlet", "right gauntlet", "leggings" };
                var clothingItems = new HashSet<string> { "undergarment", "uppergarment", "robe", "shirt", "pants", "leggings", "skirt", "kilt", "hat", "flair" };

                // Group items by their type
                var groupedItems = new Dictionary<string, List<Object>>();
                foreach (var o in a.Clothing)
                {
                    string category = "otherClothing";
                    if (armorItems.Contains(o.Type)) category = "armor";
                    else if (clothingItems.Contains(o.Type)) category = o.Type;

                    if (!groupedItems.ContainsKey(category)) groupedItems[category] = new List<Object>();
                    groupedItems[category].Add(o);
                }

                // Draw items in the specified order
                foreach (var category in drawOrder)
                {
                    foreach (var subCategory in category)
                    {
                        if (groupedItems.ContainsKey(subCategory))
                        {
                            foreach (var o in groupedItems[subCategory])
                            {
                                string s = o.Type;
                                if (s.EndsWith("shirt") || s == "robe")
                                {
                                    s += " " + a.Sex;
                                }

                                Color drawColor = o.DyedColor != "none" ? ColorConverter[o.DyedColor] : ColorConverter[o.Materials[0].Color];
                                _spriteBatch.Draw(CharacterAtlas[s], ChosenRect, drawColor);
                            }
                        }
                    }
                }

                if (_isRecording)
                {
                    int rectaWidth = (int)(100 * Scale);  // Width of the recta rectangle
                    int rectaHeight = (int)(100 * Scale); // Height of the recta rectangle

                    int rectaX = x + scaledWidth;  // Position to the right of the character's rectangle
                    int rectaY = y;  // Close to the top of the character's rectangle

                    Rectangle recta = new Rectangle(rectaX, rectaY, rectaWidth, rectaHeight);

                    _spriteBatch.Draw(SpeakingT, recta, Color.White);
                }
            }



            if (KeysNewlyPressed.Contains(Keys.PageUp) && Keyboard.GetState().IsKeyDown(Keys.LeftAlt))
            {
                int X = Mouse.GetState().X;
                int Z = Mouse.GetState().Y;
            }

            string GenerateUniqueKeyForObject(Object obj)
            {
                // Base key is constructed from the material name, object name (if not null), and type
                var key = obj.ReferredToNames[0];

                // If the object contains other objects, recursively generate keys for the contents of the container
                if (obj.ContainedObjects.Any())
                {
                    var contentsKeys = obj.ContainedObjects
                        .Select(GenerateUniqueKeyForObject) // Recursively generate keys for contained objects
                        .OrderBy(k => k) // Order the keys to ensure consistent string construction
                        .ToList();

                    key += $"[{string.Join(",", contentsKeys)}]"; // Append the sorted, comma-separated keys
                }

                return key;
            }

            Color AdjustColorBrightness(Color color)
            {
                // Adjust the R, G, B values to ensure brightness
                int r = color.R + (255 - color.R) / 5;
                int g = color.G + (255 - color.G) / 5;
                int b = color.B + (255 - color.B) / 5;

                return new Color(r, g, b);
            }

            List<(string Description, int Count, int IndentationLevel, Color TextColor, Object Obj)> CondenseAndStructureList(IEnumerable<Object> objects, bool splitMode, int indentationLevel = 0, bool isShadowStorage = false)
            {
                var result = new List<(string Description, int Count, int IndentationLevel, Color TextColor, Object Obj)>();
                var processedObjects = new HashSet<string>(); // Keep track of processed objects to avoid duplicates

                foreach (var obj in objects)
                {
                    // Handle shadow storage as a special case
                    if (obj.Type == "shadow storage" && !isShadowStorage)
                    {
                        string shadowName = obj.ReferredToNames?.FirstOrDefault() ?? obj.Type;
                        Color shadowColor = AdjustColorBrightness(ColorConverter[obj.DyedColor != "none" ? obj.DyedColor : obj.Materials[0].Color]);
                        result.Add((shadowName, 1, indentationLevel, shadowColor, obj));

                        // Retrieve contents from the shadow storage, marking the recursive call with isShadowStorage = true
                        var shadowContents = LoadedArchitects[ArchitectIndex].ShadowStorage;
                        var structuredShadowContents = CondenseAndStructureList(shadowContents, splitMode, indentationLevel + 1, true);
                        result.AddRange(structuredShadowContents);
                        continue; // Skip the regular processing for this object
                    }

                    string uniqueKey = GenerateUniqueKeyForObject(obj);
                    if (!splitMode && processedObjects.Contains(uniqueKey))
                    {
                        continue; // Skip already processed objects in non-split mode
                    }

                    string description = obj.ReferredToNames?.FirstOrDefault() ?? obj.Type;
                    Color textColor = AdjustColorBrightness(ColorConverter[obj.DyedColor != "none" ? obj.DyedColor : obj.Materials[0].Color]);

                    if (splitMode)
                    {
                        result.Add((description, 1, indentationLevel, textColor, obj));
                    }
                    else
                    {
                        int count = objects.Count(o => GenerateUniqueKeyForObject(o) == uniqueKey);
                        result.Add((description, count, indentationLevel, textColor, obj));
                        processedObjects.Add(uniqueKey);
                    }

                    if (obj.ContainedObjects.Any())
                    {
                        var sortedContainedObjects = obj.ContainedObjects
                            .OrderBy(co => co.ReferredToNames?.FirstOrDefault() ?? co.Type)
                            .ToList();

                        var structuredContainedObjects = CondenseAndStructureList(sortedContainedObjects, splitMode, indentationLevel + 1);
                        result.AddRange(structuredContainedObjects);
                    }
                }

                return result;
            }


            // GetCurrentPageList as you've defined

            // DrawList method adjustment to remove count indicator in split mode
            void DrawList(SpriteBatch spriteBatch, List<(string Description, int Count, int IndentationLevel, Color TextColor, Object Obj)> list, Vector2 startCoords, SpriteFont font, int CurrentObjectPage, int itemsPerPage)
            {
                int line = 0;
                int startIndex = CurrentObjectPage * itemsPerPage;
                int endIndex = Math.Min(startIndex + itemsPerPage, list.Count);

                // Draw the page indicator
                string pageIndicator = $"(page {CurrentObjectPage + 1} of {MaximumObjectPage+1}, use CTRL + / -)";
                Vector2 pageIndicatorPosition = new Vector2(startCoords.X, startCoords.Y + line * 25);
                spriteBatch.DrawString(font, pageIndicator, pageIndicatorPosition, Color.White);
                line++;

                // Draw an empty space
                line++;

                // Adjust start coordinates for drawing the actual list
                startCoords = new Vector2(startCoords.X, startCoords.Y + 40); // Move startCoords down by 40 (two lines)

                for (int i = startIndex; i < endIndex; i++)
                {
                    var (Description, Count, IndentationLevel, TextColor, Obj) = list[i];
                    string textToDraw = SplitMode ?
                        $"{new string(' ', 4 * IndentationLevel)}{Description}" :
                        $"{new string(' ', 4 * IndentationLevel)}{Description} x{Count}";

                    Vector2 textPosition = new Vector2(startCoords.X, startCoords.Y + line * 25);
                    spriteBatch.DrawString(font, textToDraw, textPosition, TextColor);

                    // Add hitbox for the object
                    EntityHitboxes.Add((new Rectangle(textPosition.ToPoint(), font.MeasureString(textToDraw).ToPoint()), Obj));

                    line++;
                }

                MaximumObjectPage = (int)Math.Round((decimal)(list.Count / itemsPerPage), 0, MidpointRounding.ToPositiveInfinity);
            }

            // Assuming you have a method to handle user input to navigate pages, you would update CurrentObjectPage accordingly
            // And call PaginateAndDrawList again to redraw the list for the new page


            
            void DrawCenteredTextAtPosition(SpriteBatch spriteBatch, string text, float centerX, float centerY, SpriteFont font)
            {
                // Measure the size of the text using the provided font
                Vector2 textSize = font.MeasureString(text);

                // Calculate the position to start drawing the text so that it is centered on (centerX, centerY)
                // Adjust the x position to center the text on the specified centerX
                float xPosition = centerX - (textSize.X / 2);

                // Adjust the y position to center the text on the specified centerY
                float yPosition = centerY - (textSize.Y / 2);

                // Create a Vector2 for the adjusted position
                Vector2 position = new Vector2(xPosition, yPosition);

                // Draw the text at the calculated position with the specified font and color
                spriteBatch.DrawString(font, text, position, Color.White);
            }

            int DrawX = 1400;
            int DrawY = 1100;


            void WorldMouseLogic()
            {

                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {

                        if (GameWorld.WorldMap[x + z * GameWorld.Width].BoundingBox().Contains(new Point(Mouse.GetState().X, Mouse.GetState().Y)))
                        {
                            if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation == null)
                            {
                                _spriteBatch.DrawString(BabyShibafont, "No Notable Location, " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome, new Vector2(DrawX, DrawY), Color.White);
                            }
                            else
                            {
                                _spriteBatch.DrawString(BabyShibafont, GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Name + ", " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome + " " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Type + ".", new Vector2(DrawX, DrawY), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Population: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.TruePopulation(), new Vector2(DrawX, DrawY + 30), Color.White);

                                int ArchitectPopulation = 0;
                                int structures = 0;
                                int groups = 0;

                                foreach (Group g in Game1.GameWorld.Groups)
                                {
                                    if (g.Leader.Location == GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation)
                                        groups++;
                                }

                                foreach (District d in GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Districts)
                                {
                                    for (int DistrictX = 0; DistrictX < 7; DistrictX++)
                                    {
                                        for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                                        {
                                            ArchitectPopulation += d.DistrictMap[DistrictX + DistrictZ * 7].Architects.Count;

                                            foreach (Structure s in d.DistrictMap[DistrictX + DistrictZ * 7].Structures)
                                            {
                                                structures++;
                                            }
                                        }
                                    }
                                }

                                _spriteBatch.DrawString(BabyShibafont, "Total Architects: " + ArchitectPopulation, new Vector2(DrawX, DrawY + 60), Color.White);

                                if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government == null)
                                {
                                    _spriteBatch.DrawString(BabyShibafont, "No Notable Government", new Vector2(DrawX, DrawY + 90), Color.White);
                                }
                                else
                                {
                                    _spriteBatch.DrawString(BabyShibafont, "Government: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government.Name, new Vector2(DrawX, DrawY + 90), Color.White);
                                }

                                _spriteBatch.DrawString(BabyShibafont, "Colonization Desire: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.ColonizationDesire, new Vector2(DrawX, DrawY + 120), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Wealth: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Wealth, new Vector2(DrawX, DrawY + 150), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Is Saving Up To Settle: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.IsSavingUpToSettle.ToString(), new Vector2(DrawX, DrawY + 180), Color.White);


                                _spriteBatch.DrawString(BabyShibafont, "Structures: " + structures, new Vector2(DrawX, DrawY + 210), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Distinct Groups: " + groups, new Vector2(DrawX, DrawY + 240), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Districts: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Districts.Count, new Vector2(DrawX, DrawY + 270), Color.White);
                            }
                        }
                    }
                }
            }

            //tips


            if (GameState == "generatehistory" || GameState == "choosepreferences")
            {
                // This would ideally be within your update or draw loop
                if (currentObject == "")
                {
                    currentObject = Tips[r.Next(Tips.Count)]; // Assuming 'Tips' is a List<string> and 'r' is a Random instance
                }

                DrawCenteredText(_spriteBatch, currentObject, 1250, Shibafont, new Color(objectOpacity, 0, 0));

                objectCycles++;

                // Halve the fade in and out times as well as the duration the text is fully visible
                if (objectCycles < 50) // Faster fade in
                {
                    objectOpacity += 5; // Adjust for quicker transition, doubling the rate
                }
                else if (objectCycles > 150) // Faster fade out
                {
                    objectOpacity -= 5; // Adjust for quicker transition, doubling the rate
                }

                if (objectCycles == 100)
                {
                    objectOpacity = 255; // Max opacity reached sooner
                }

                if (objectCycles > 199) // Changes more often
                {
                    currentObject = Tips[r.Next(Tips.Count)];
                    objectCycles = 0;
                    objectOpacity = 0; // Reset opacity for the next object
                }
            }


            if (GameState == "mainscreen")
            {
                int screenWidth = 2560;
                int imageWidth = 744;

                Rectangle destinationRectangle = new Rectangle((screenWidth - imageWidth) / 2, 100, imageWidth, 216);

                _spriteBatch.Draw(TitleScreen, destinationRectangle, Color.White);

                if (FakeName == "")
                {
                    FakeName = GenerateUniqueFakeName();
                }

                string GenerateUniqueFakeName()
                {
                    string firstName = Game1.FirstNames[Game1.r.Next(Game1.FirstNames.Count)] + Game1.NameSuffixes[Game1.r.Next(Game1.NameSuffixes.Count)];
                    string lastName = ((Game1.LastNames[Game1.r.Next(Game1.LastNames.Count)]).Substring(0, 1)).ToUpper() + (Game1.LastNames[Game1.r.Next(Game1.LastNames.Count)]).Substring(1).ToLower();

                    //OK so this system litterally takes a last name from the list, and then takes a random letter from a random last name and replaces the first letter. Its not supposed to do that, BUT IT WORKS SO WELL WHAT

                    return $"{firstName} {lastName}";
                }


                DrawCenteredText(_spriteBatch, "The tale of " + FakeName, 300, Shibafont, new Color(NameOpacity, NameOpacity, NameOpacity));

                NameCycles++;

                if (NameCycles < 50)
                {
                    NameOpacity += 5;
                }
                else if (NameCycles > 150)
                {
                    NameOpacity -= 5;
                }

                if (NameCycles == 100)
                {
                    NameOpacity = 255;
                    //off sync but i cant tell why so ill do this
                }

                if (NameCycles > 199)
                {
                    FakeName = GenerateUniqueFakeName();
                    NameCycles = 0;
                }

                //DrawCenteredText(_spriteBatch, "Press F to start a new Founder game.", 500);
                DrawCenteredText(_spriteBatch, "Press C to start a new game.", 550, Shibafont, Color.White);
                DrawCenteredText(_spriteBatch, "Hold CTRL+L to load an existing save.", 600, Shibafont, Color.White);

                _spriteBatch.Draw(Astrionalis, new Rectangle(100, 200, 640, 1280), Color.White);
                _spriteBatch.Draw(Celestrioris, new Rectangle(1800, 200, 720, 1270), Color.White);
            }
            else if (GameState == "worldgenscreen")
            {
                _spriteBatch.DrawString(Shibafont, "Press ENTER to start playing with optimal settings.", new Vector2(200, 200), Color.White);
                _spriteBatch.DrawString(Shibafont, "If you wish, use denoted keys to change settings.", new Vector2(200, 250), Color.White);
                
                if(ViewMessageForCustom)
                {
                    _spriteBatch.DrawString(Shibafont, "Some custom settings are not guarranteed to produce a playable world.", new Vector2(200, 300), Color.OrangeRed);
                }

                if (CurrentlySelectedWorldAge == 10000)
                {
                    _spriteBatch.DrawString(Shibafont, "(Q/A) World Age: Until Cancelled. (Stack Overflow Inevitable Unless Calamity Eats Your World)", new Vector2(200, 500), Color.Orange);

                }
                else
                {
                    string displayText = "(Q/A) World Age: " + CurrentlySelectedWorldAge;

                    if (CurrentlySelectedWorldAge >= 400)
                    {
                        displayText += " (Stack Overflow Protection Not Guaranteed)";
                    }
                    else if (CurrentlySelectedWorldAge >= 300)
                    {
                        displayText += " (Instability Likely/Inevitable)";
                    }

                    _spriteBatch.DrawString(Shibafont, displayText, new Vector2(200, 500), Color.Orange);
                }

                if (ThreatTypes[CurrentlySelectedGrievanceType] == "random")
                {
                    _spriteBatch.DrawString(Shibafont, "(W/S) Choose Calamity: " + Capitalize(ThreatTypes[CurrentlySelectedGrievanceType]) + " (This could produce a world-simplifying threat. If complexity sounds more fun, choose non-cataclysmic)", new Vector2(200, 550), Color.Magenta);
                }
                else if (ThreatTypes[CurrentlySelectedGrievanceType] == "disease")
                {
                    _spriteBatch.DrawString(Shibafont, "(W/S) Choose Calamity: " + Capitalize(ThreatTypes[CurrentlySelectedGrievanceType]) + " (Warning: This threat will make many settlements rather desolate.)", new Vector2(200, 550), Color.Magenta);
                }
                else if (ThreatTypes[CurrentlySelectedGrievanceType] == "purifier")
                {
                    _spriteBatch.DrawString(Shibafont, "(W/S) Choose Calamity: " + Capitalize(ThreatTypes[CurrentlySelectedGrievanceType]) + " (Warning: This threat will rip apart your continent and history.)", new Vector2(200, 550), Color.Magenta);
                }
                else
                {
                    _spriteBatch.DrawString(Shibafont, "(W/S) Choose Calamity: " + Capitalize(ThreatTypes[CurrentlySelectedGrievanceType]), new Vector2(200, 550), Color.Magenta);
                }


                string numberOfCivilizationsText = "(E/D) Number of Civilizations: " + NumberOfCivilizations;

                if (NumberOfCivilizations == 16)
                {
                    numberOfCivilizationsText += " (maximum)";
                }
                else if (NumberOfCivilizations == 8)
                {
                    numberOfCivilizationsText += " (minimum)";
                }

                _spriteBatch.DrawString(Shibafont, numberOfCivilizationsText, new Vector2(200, 600), Color.Red);


                double roundedProsperityMultiplier = Math.Round(ProsperityMultiplier, 1);
                string prosperityMultiplierText = roundedProsperityMultiplier.ToString("0.0");

                if (roundedProsperityMultiplier == 1.0)
                {
                    prosperityMultiplierText += " (recommended)";
                }
                else if (roundedProsperityMultiplier > 1.5)
                {
                    prosperityMultiplierText += "        Warning: This may make world generation highly unstable.";
                }

                _spriteBatch.DrawString(Shibafont, "(R/F) Prosperity Multiplier (affects civ growth rate): " + prosperityMultiplierText, new Vector2(200, 650), Color.Cyan);

                /*
                _spriteBatch.DrawString(Shibafont, "(T/G) [BROKEN] World Width (in region tiles, east/west, max 128): " + CurrentlySelectedWorldWidth, new Vector2(200, 700), Color.LimeGreen);
                _spriteBatch.DrawString(Shibafont, "(Y/H) [BROKEN] World Length (in region tiles, north/south, max 128): " + CurrentlySelectedWorldLength, new Vector2(200, 750), Color.Cyan);
                */

                _spriteBatch.DrawString(Shibafont, "Press ENTER to begin world generation.", new Vector2(200, 950), Color.White);
            }

            else if (GameState == "savinggame")
            {
                _spriteBatch.DrawString(Shibafont, "Saving " + GameWorld.Name + " data. This may take half a minute...", new Vector2(200, 200), Color.White);
            }
            else if (GameState == "loadinggamemenu")
            {
                var saveDirectories = Directory.GetDirectories(DocumentsFolderPath + "/LightrealmSaves").ToList();
                int SavesCount = saveDirectories.Count;

                if (SavesCount > 0)
                {
                    _spriteBatch.DrawString(Shibafont, "Use arrow keys and ENTER to select a savegame:", new Vector2(200, 200), Color.White);

                    int Number = 0;

                    foreach (string d in saveDirectories)
                    {
                        var versionFilePath = Path.Combine(d, "version.txt");
                        string directoryDisplayName = d;
                        Color textColor = Color.White;

                        if (File.Exists(versionFilePath))
                        {
                            string firstLine = File.ReadLines(versionFilePath).FirstOrDefault();
                            if (firstLine == Version)
                            {
                                textColor = Color.White; // Matching version
                            }
                            else
                            {
                                textColor = Color.Gray;
                                directoryDisplayName += $" (Version Mismatch: Current: {Version}, Required: {firstLine})";
                            }
                        }
                        else
                        {
                            textColor = Color.Gray;
                            directoryDisplayName += " (No Version File)";
                        }

                        if (Number == LoadGameCursor)
                        {
                            _spriteBatch.DrawString(Shibafont, "(>) " + directoryDisplayName, new Vector2(200, 230 + Number * 30), textColor);
                        }
                        else
                        {
                            _spriteBatch.DrawString(Shibafont, "( ) " + directoryDisplayName, new Vector2(200, 230 + Number * 30), textColor);
                        }

                        Number++;
                    }

                    _spriteBatch.DrawString(Shibafont, "Press DELETE to remove a savegame.", new Vector2(200, 1300), Color.White);
                }
                else
                {
                    _spriteBatch.DrawString(Shibafont, "You have no savegames. You should probably go make one now.", new Vector2(200, 200), Color.White);
                    _spriteBatch.DrawString(Shibafont, "Press ESC to return to title.", new Vector2(200, 230), Color.White);
                }
            }



            else if (GameState == "deletinggame")
            {
                _spriteBatch.DrawString(Shibafont, "This action cannot be undone. Are you sure?", new Vector2(30, 30), Color.White);
                _spriteBatch.DrawString(Shibafont, "Confirm (CTRL Y)", new Vector2(30, 60), Color.White);
                _spriteBatch.DrawString(Shibafont, "Cancel (CTRL N)", new Vector2(30, 90), Color.White);
            }
            else if (GameState == "loadinggame")
            {
                _spriteBatch.DrawString(Shibafont, "Loading save data, this may take half a minute...", new Vector2(200, 200), Color.White);
            }
            else if (GameState == "pickstatpreferences")
            {
                int Line = 1;

                if (StatOptions.Count == 7)
                {
                    _spriteBatch.DrawString(Shibafont, "Choose your most prefered stat of these:", new Vector2(100, 100), Color.White);
                }
                else
                {
                    _spriteBatch.DrawString(Shibafont, "Lovely, now choose your next-highest stat:", new Vector2(100, 100), Color.White);
                }

                foreach (string s in StatOptions)
                {
                    _spriteBatch.DrawString(Shibafont, "[" + Line + "] " + s, new Vector2(100, 100 + Line * 50), Color.White);
                    Line++;
                }
            }
            else if (GameState == "pickinspirations")
            {
                int Line = 0;
                char[] options = { 'A', 'B', 'C' };

                _spriteBatch.DrawString(Shibafont, "Choose an Inspiration:", new Vector2(100, 100), Color.White);

                foreach (string s in InspirationsChosen)
                {
                    _spriteBatch.DrawString(Shibafont, "[" + options[Line] + "] " + s, new Vector2(100, 100 + (Line + 1) * 50), Color.White);
                    Line++;
                }
            }

            else if (GameState == "generatingworld")
            {
                DrawCenteredText(_spriteBatch, "Generating Landmass...", 100, Shibafont, Color.White);
            }
            else if (GameState == "generatehistory")
            {
                for (int i = 20; i != 0; i--)
                {
                    int CurrentItemListing = (GameWorld.HistoricalEvents.Count - i);
                    if (CurrentItemListing >= 0)
                    {
                        _spriteBatch.DrawString(BabyShibafont, GameWorld.HistoricalEvents[CurrentItemListing], new Vector2(1750, 400 + ((-1) * (20 * i))), Color.White);
                    }
                }

                for (int i = 20; i != 0; i--)
                {
                    int CurrentItemListing = (GameWorld.AbridgedHistoricalEvents.Count - i);
                    if (CurrentItemListing >= 0)
                    {
                        _spriteBatch.DrawString(BabyShibafont, GameWorld.AbridgedHistoricalEvents[CurrentItemListing], new Vector2(1750, 800 + ((-1) * (20 * i))), Color.White);
                    }
                }

                DrawWorld();

                WorldMouseLogic();


                // Updating cycle counts for drawing week, month, and year
                _spriteBatch.DrawString(BabyShibafont, "Week " + Math.Round((decimal)(GameWorld.Cycle / 6048000)), new Vector2(1750, 1280), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Month " + Math.Round((decimal)(GameWorld.Cycle / 24192000), 0, MidpointRounding.ToNegativeInfinity).ToString(), new Vector2(1750, 1240), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Year " + Math.Round((decimal)(Math.Round((decimal)(GameWorld.Cycle / 290304000), 0, MidpointRounding.ToNegativeInfinity)), 0, MidpointRounding.ToNegativeInfinity), new Vector2(1750, 1200), Color.White);

                // Keeping other text draws as they are since they're not dependent on cycle conversion
                _spriteBatch.DrawString(BabyShibafont, "Architects, Total: " + GameWorld.TotalArchitects + ", Living: " + GameWorld.LivingArchitects + ", Dead: " + GameWorld.DeadArchitects, new Vector2(1750, 1320), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Distinct Groups: " + GameWorld.Groups.Count, new Vector2(1750, 1360), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Written Objects: " + GameWorld.TotalWrittenObjects, new Vector2(1750, 1400), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Crafts Practiced: " + GameWorld.TotalCrafts, new Vector2(1750, 1400), Color.White);

                // Updating the calculation for Month and Year using new cycles
                int Month = (int)Math.Round((decimal)(GameWorld.Cycle / 24192000)) % 12 + 1;
                int Year = (int)Math.Round((decimal)(GameWorld.Cycle / 290304000));

                // Updating the drawing of game world name, month/year display, and pause instruction
                _spriteBatch.DrawString(BabyShibafont, GameWorld.Name + " (hover over map for more info)", new Vector2(1750, DrawY), Color.White);
                _spriteBatch.DrawString(BabyShibafont, Month + "/" + Year, new Vector2(1750, 1160), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Pause Generation with ENTER", new Vector2(1900, 1200), Color.White);

                _spriteBatch.DrawString(
    Shibafont,
    $"Generating {GameWorld.Name}, ({Math.Round((decimal)GameWorld.Cycle / 290304000 / GameWorld.MaxAge * 100, 0, MidpointRounding.ToNegativeInfinity)}%)",
    new Vector2(10, 10),
    Color.White
);


            }
            else if (GameState == "choosepreferences")
            {
                DrawWorld();

                if (AlreadyTriedASearch)
                {
                    _spriteBatch.DrawString(BabyShibafont, "Could not find a character with your preferences.", new Vector2(DrawX + 500, 50), Color.White);
                    _spriteBatch.DrawString(BabyShibafont, "Change preferences or press C to continue generation.", new Vector2(DrawX + 500, 100), Color.White);
                }
                else
                {
                    _spriteBatch.DrawString(BabyShibafont, "Historical events exported to Desktop.", new Vector2(DrawX + 500, 50), Color.White);
                    _spriteBatch.DrawString(BabyShibafont, "Choose your character preferences ([] denotes a keybind):", new Vector2(DrawX + 500, 100), Color.White);
                }

                Dictionary<string, string> BonusDictionary = new Dictionary<string, string>() { { "luminarch", "(+10% Max Energy, -10% Attack Power)" }, { "nightfell", "(-10% Max Energy, +10% Attack Power)" }, { "archaix", "(No Specific Bonuses)" } };

                _spriteBatch.DrawString(BabyShibafont, "[1] Race: " + Capitalize(GameWorld.Races[CurrentlySelectingRace].Name) + " " + BonusDictionary[GameWorld.Races[CurrentlySelectingRace].Name], new Vector2(DrawX + 500, 150), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "[2] Sex: " + Capitalize(Sexes[CurrentlySelectingSex]), new Vector2(DrawX + 500, 200), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Press ENTER to continue...):", new Vector2(DrawX + 500, 300), Color.White);

                _spriteBatch.DrawString(BabyShibafont, GameWorld.Name + " (hover over map for more info)", new Vector2(DrawX + 500, DrawY), Color.White);

                WorldMouseLogic();

            }
            else if (GameState == "choosefounderoptions")
            {
                _spriteBatch.DrawString(BabyShibafont, "Choose your civilization preferences ([] denotes a keybind!):", new Vector2(DrawX + 500, 100), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "[1] Race: " + GameWorld.Races[CurrentlySelectingRace], new Vector2(DrawX + 500, 150), Color.White);

                DrawWorld();

                _spriteBatch.DrawString(BabyShibafont, "Press ENTER to continue...):", new Vector2(DrawX + 500, 300), Color.White);

                _spriteBatch.DrawString(BabyShibafont, GameWorld.Name + " (hover over map for more info)", new Vector2(DrawX + 500, DrawY), Color.White);

                WorldMouseLogic();

            }
            else if (GameState == "founder")
            {
                _spriteBatch.Draw(GuideT, new Rectangle(0, 0, 192, 192), Color.White);

                // Render tiles and update CivilizationParticles
                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {
                        if (MapCursorX == x && MapCursorZ == z)
                        {
                            _spriteBatch.Draw(CursorT, new Rectangle((10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0), 10 + z * TileZDistance, TileSize, TileSize), Color.White);
                        }
                        else
                        {
                            // Draw the tile
                            _spriteBatch.Draw(TileAtlas[GameWorld.WorldMap[x + z * GameWorld.Width].Biome],
                                new Rectangle((10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0), 10 + z * TileZDistance, TileSize, TileSize),
                                new Color(100, 100, 100));

                            if (GameWorld.WorldMap[x + z * GameWorld.Width].Owner != null)
                            {
                                // Draw outline for owned tile
                                _spriteBatch.Draw(OutlineT,
                                    new Rectangle((10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0), 10 + z * TileZDistance, TileSize, TileSize),
                                    ColorConverter[GameWorld.WorldMap[x + z * GameWorld.Width].Owner.Color]);

                                if (GameWorld.WorldMap[x + z * GameWorld.Width].Owner == GamePlayerCivilization)
                                {
                                    if (r.Next(1, 10) == 1)
                                    {
                                        // Add CivilizationParticles
                                        CivilizationParticles.Add((
                                            (10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0),
                                            10 + z * TileZDistance,
                                            ColorConverter[GamePlayerCivilization.Color],
                                            r.Next(1, 4),
                                            r.Next(20, 40),
                                            r.Next(1, 5) // Assuming 1-4 for direction
                                        ));
                                    }
                                }
                            }
                        }


                        if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation != null)
                        {
                            // Draw tile for custom location
                            _spriteBatch.Draw(TileAtlas[GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.PrimaryRace.Name + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Type],
                                new Rectangle((10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0), 10 + z * TileZDistance, TileSize, TileSize),
                                Color.White);
                        }

                    }
                }

                // Update CivilizationParticles
                for (int index = 0; index < CivilizationParticles.Count; index++)
                {
                    var i = CivilizationParticles[index];
                    _spriteBatch.Draw(whiteRect, new Rectangle(i.Item1, i.Item2, i.Item4, i.Item4), i.Item3);

                    if (i.Item6 == 1)
                    {
                        CivilizationParticles[index] = (i.Item1 + 1, i.Item2 + 1, i.Item3, i.Item4, i.Item5 - 1, i.Item6);
                    }
                    else if (i.Item6 == 2)
                    {
                        CivilizationParticles[index] = (i.Item1 + 1, i.Item2 - 1, i.Item3, i.Item4, i.Item5 - 1, i.Item6);
                    }
                    else if (i.Item6 == 3)
                    {
                        CivilizationParticles[index] = (i.Item1 - 1, i.Item2 + 1, i.Item3, i.Item4, i.Item5 - 1, i.Item6);
                    }
                    else if (i.Item6 == 4)
                    {
                        CivilizationParticles[index] = (i.Item1 - 1, i.Item2 - 1, i.Item3, i.Item4, i.Item5 - 1, i.Item6);
                    }
                }

                CivilizationParticles.RemoveAll(i => i.Item5 < 1);
                _spriteBatch.DrawString(BabyShibafont, GameWorld.Name + " (hover over map for more info)", new Vector2(DrawX + 500, DrawY), Color.White);

                _spriteBatch.DrawString(BabyShibafont, "Week " + Math.Round((decimal)(GameWorld.Cycle / 120960)), new Vector2(1900, 1280), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Month " + Math.Round((decimal)(GameWorld.Cycle / 483840), 0, MidpointRounding.ToNegativeInfinity).ToString(), new Vector2(1900, 1240), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Year " + Math.Round((decimal)(Math.Round((decimal)(GameWorld.Cycle / 5806080), 0, MidpointRounding.ToNegativeInfinity)), 0, MidpointRounding.ToNegativeInfinity), new Vector2(1900, 1200), Color.White);


                _spriteBatch.DrawString(Shibafont, GamePlayerCivilization.Name, new Vector2(1900, 200), ColorConverter[GamePlayerCivilization.Color]);
                _spriteBatch.DrawString(BabyShibafont, "Cursor: RTDGCV", new Vector2(1900, 220), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Select: F", new Vector2(1900, 240), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "[S]tructures", new Vector2(1900, 260), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "[M]illitary", new Vector2(1900, 280), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "D[I]stricts", new Vector2(1900, 300), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Res[O]urces", new Vector2(1900, 320), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Di[P]lomacy", new Vector2(1900, 340), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Sett[L]ement", new Vector2(1900, 360), Color.White);

                _spriteBatch.DrawString(BabyShibafont, "Wait and Build up Resources", new Vector2(1900, 420), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "[1] Wait 1 Month", new Vector2(1900, 440), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "[2] Wait 1 Year", new Vector2(1900, 460), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "[3] Wait 10 Years", new Vector2(1900, 480), Color.White);



                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {
                        if (MapCursorX == x && MapCursorZ == z)
                        {
                            if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation == null)
                            {
                                _spriteBatch.DrawString(BabyShibafont, "No Notable Location, " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome, new Vector2(DrawX, DrawY), Color.White);
                            }
                            else
                            {
                                _spriteBatch.DrawString(BabyShibafont, GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Name + ", " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome + " " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Type + ".", new Vector2(DrawX, DrawY), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Population: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.TruePopulation(), new Vector2(DrawX, DrawY + 30), Color.White);

                                int ArchitectPopulation = 0;
                                int structures = 0;
                                int groups = 0;

                                foreach (Group g in Game1.GameWorld.Groups)
                                {
                                    if (g.Leader.Location == GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation)
                                        groups++;
                                }

                                foreach (District d in GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Districts)
                                {
                                    for (int DistrictX = 0; DistrictX < 7; DistrictX++)
                                    {
                                        for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                                        {
                                            ArchitectPopulation += d.DistrictMap[DistrictX + DistrictZ * 7].Architects.Count;

                                            foreach (Structure s in d.DistrictMap[DistrictX + DistrictZ * 7].Structures)
                                            {
                                                structures++;
                                            }
                                        }
                                    }
                                }

                                _spriteBatch.DrawString(BabyShibafont, "Total Architects: " + ArchitectPopulation, new Vector2(DrawX, DrawY + 60), Color.White);

                                if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government == null)
                                {
                                    _spriteBatch.DrawString(BabyShibafont, "No Notable Government", new Vector2(DrawX, DrawY + 90), Color.White);
                                }
                                else
                                {
                                    _spriteBatch.DrawString(BabyShibafont, "Government: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government.Name, new Vector2(DrawX, DrawY + 90), Color.White);
                                }

                                _spriteBatch.DrawString(BabyShibafont, "Colonization Desire: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.ColonizationDesire, new Vector2(DrawX, DrawY + 120), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Wealth: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Wealth, new Vector2(DrawX, DrawY + 150), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Is Saving Up To Settle: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.IsSavingUpToSettle.ToString(), new Vector2(DrawX, DrawY + 180), Color.White);


                                _spriteBatch.DrawString(BabyShibafont, "Structures: " + structures, new Vector2(DrawX, DrawY + 210), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Distinct Groups: " + groups, new Vector2(DrawX, DrawY + 240), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Districts: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Districts.Count, new Vector2(DrawX, DrawY + 270), Color.White);
                            }
                        }
                    }
                }
            }
            else if (GameState == "findstartlocation")
            {
                DrawWorld();

                WorldMouseLogic();
            }
            else if (GameState == "architectfound")
            {
                _spriteBatch.DrawString(BabyShibafont, "Architect Found.", new Vector2(1750, 100), Color.White);
                _spriteBatch.DrawString(BabyShibafont, TheChosenOne.Name, new Vector2(1750, 130), Color.White);
                _spriteBatch.DrawString(BabyShibafont, GameWorld.Name + " (hover over map for more info)", new Vector2(1750, DrawY), Color.White);

                DrawWorld();

                WorldMouseLogic();
            }
            else if (GameState == "partyturn" || GameState == "reaction" || GameState == "otherturn" || GameState == "messagereply" || GameState == "trypickup" || GameState == "trydrop")
            {
                if ((Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.LeftControl)) && Keyboard.GetState().IsKeyDown(Keys.OemQuestion))
                {
                    _spriteBatch.Draw(HelpGUI, new Rectangle(0, 0, 2560, 1440), Color.White);
                }
                else if (!InInventory && !Keyboard.GetState().IsKeyDown(Keys.Tab))
                {
                    _spriteBatch.Draw(GUI, new Rectangle(0, 0, 2560, 1440), Color.White);

                    // Calculate the color intensity based on the current energy relative to MaxEnergy
                    // Using a linear interpolation method to ensure the transition is smooth
                    double energyPercentage = (double)MostRecentPartyTurnArchitect.Energy / MostRecentPartyTurnArchitect.MaxEnergy();
                    int colorIntensity = (int)Math.Round(energyPercentage * 255);

                    // Ensure the color intensity stays within the range [0, 255]
                    // Although Math.Clamp inherently ensures this, it's good to reinforce the valid range in the comment
                    colorIntensity = Math.Clamp(colorIntensity, 0, 255);

                    // Define the rectangle where the energy GUI will be drawn
                    Rectangle energyGuiRect = new Rectangle(620, 80, 200, 200); // Adjusted by 20 right and 65 down (80-15)

                    // Use a gradient from red (low energy) to green (high energy) to represent energy level visually
                    // Red component decreases with energy increase, green component increases with energy increase
                    Color energyColor = new Color(colorIntensity, colorIntensity, colorIntensity, 255); // No blue component

                    // Draw the energy GUI with the calculated color
                    _spriteBatch.Draw(HealthGuiT, energyGuiRect, energyColor);

                    // Drawing droplets for each bleeding stack
                    int dropletSize = 24; // Size of each droplet
                    int startX = 720; // Center X position of the health GUI (620 + half of its width)
                    int startY = 280; // Starting Y position (95 + height of the health GUI + some padding)

                    for (int i = 0; i < MostRecentPartyTurnArchitect.Bleeding; i++)
                    {
                        // Calculate the Y position for the current droplet
                        int posY = startY + (dropletSize * i);

                        // Draw the droplet at the calculated position
                        _spriteBatch.Draw(BleedT, new Rectangle(startX - (dropletSize / 2), posY, dropletSize, dropletSize), Color.White);
                    }

                    EntityHitboxes.Clear();

                    // Draw the architect's name
                    string architectName = MostRecentPartyTurnArchitect.Name;
                    Vector2 architectNamePosition = new Vector2(70, 120);
                    _spriteBatch.DrawString(Shibafont, architectName, architectNamePosition, Color.White);
                    EntityHitboxes.Add((new Rectangle(architectNamePosition.ToPoint(), Shibafont.MeasureString(architectName).ToPoint()), MostRecentPartyTurnArchitect));

                    // Draw the location
                    string locationText = "L: " + MostRecentPartyTurnArchitect.Location.Name;
                    Vector2 locationPosition = new Vector2(70, 150);
                    _spriteBatch.DrawString(Shibafont, locationText, locationPosition, Color.White);
                    EntityHitboxes.Add((new Rectangle(locationPosition.ToPoint(), Shibafont.MeasureString(locationText).ToPoint()), MostRecentPartyTurnArchitect.Location));

                    // Draw the district
                    string districtText = "D: " + MostRecentPartyTurnArchitect.District.Name;
                    Vector2 districtPosition = new Vector2(70, 180);
                    _spriteBatch.DrawString(Shibafont, districtText, districtPosition, Color.White);
                    EntityHitboxes.Add((new Rectangle(districtPosition.ToPoint(), Shibafont.MeasureString(districtText).ToPoint()), MostRecentPartyTurnArchitect.District));

                    // Draw the structure if it's not null
                    if (MostRecentPartyTurnArchitect.Structure != null)
                    {
                        string structureText = "S: " + MostRecentPartyTurnArchitect.Structure.Name + ", R: " + MostRecentPartyTurnArchitect.Structure.Rooms.IndexOf(MostRecentPartyTurnArchitect.Room);
                        Vector2 structurePosition = new Vector2(70, 210);
                        _spriteBatch.DrawString(Shibafont, structureText, structurePosition, Color.White);
                        EntityHitboxes.Add((new Rectangle(structurePosition.ToPoint(), Shibafont.MeasureString(structureText).ToPoint()), MostRecentPartyTurnArchitect.Structure));
                    }

                    // Draw the left hand object or empty text
                    string leftHandText = MostRecentPartyTurnArchitect.LeftHandObject != null
                        ? "LH: " + MostRecentPartyTurnArchitect.LeftHandObject.ReferredToNames[0]
                        : "LH: (empty)";
                    Vector2 leftHandPosition = new Vector2(70, 240);
                    _spriteBatch.DrawString(Shibafont, leftHandText, leftHandPosition, Color.White);
                    Entity leftHandEntity = MostRecentPartyTurnArchitect.LeftHandObject != null
                        ? MostRecentPartyTurnArchitect.LeftHandObject
                        : MostRecentPartyTurnArchitect.FindBodyPart("left hand");
                    EntityHitboxes.Add((new Rectangle(leftHandPosition.ToPoint(), Shibafont.MeasureString(leftHandText).ToPoint()), leftHandEntity));

                    // Draw the right hand object or empty text
                    string rightHandText = MostRecentPartyTurnArchitect.RightHandObject != null
                        ? "RH: " + MostRecentPartyTurnArchitect.RightHandObject.ReferredToNames[0]
                        : "RH: (empty)";
                    Vector2 rightHandPosition = new Vector2(70, 270);
                    _spriteBatch.DrawString(Shibafont, rightHandText, rightHandPosition, Color.White);
                    Entity rightHandEntity = MostRecentPartyTurnArchitect.RightHandObject != null
                        ? MostRecentPartyTurnArchitect.RightHandObject
                        : MostRecentPartyTurnArchitect.FindBodyPart("right hand");
                    EntityHitboxes.Add((new Rectangle(rightHandPosition.ToPoint(), Shibafont.MeasureString(rightHandText).ToPoint()), rightHandEntity));



                    _spriteBatch.DrawString(Shibafont, "Speed: " + MostRecentPartyTurnArchitect.Speed(), new Vector2(420, 120), Color.White);
                    _spriteBatch.DrawString(Shibafont, "CD: " + MostRecentPartyTurnArchitect.CooldownCycles, new Vector2(420, 150), Color.White);

                    if (MostRecentPartyTurnArchitect.CombatCycles > 0)
                    {
                        _spriteBatch.DrawString(Shibafont, "Evade: " + MostRecentPartyTurnArchitect.EscapeChance(), new Vector2(420, 230), Color.White);
                    }

                    _spriteBatch.DrawString(Shibafont, "Press TAB for More.", new Vector2(420, 270), Color.White);


                    int Line = 0;


                    //date/time

                    string dateTimeString = GameWorld.GetFormattedDateTime();
                    Vector2 dateTimeSize = Shibafont.MeasureString(dateTimeString);
                    Vector2 dateTimePosition = new Vector2(250, 1350);
                    _spriteBatch.DrawString(Shibafont, dateTimeString, dateTimePosition, Color.White);
                    Color oColor = GameWorld.IsNightTime() ? new Color(100, 100, 100) : Color.Goldenrod;
                    Vector2 oPosition = new Vector2(dateTimePosition.X + dateTimeSize.X + 10, dateTimePosition.Y);
                    _spriteBatch.DrawString(Shibafont, "O", oPosition, oColor);
                    Rectangle backgroundRect = new Rectangle(50, 1258, 176, 176);

                    //district map

                    _spriteBatch.Draw(FrameT, backgroundRect, Color.White);

                    _spriteBatch.DrawString(Shibafont, "Press CTRL+? for Help", new Vector2(600, 1350), Color.White);

                    for (int DistrictX = 0; DistrictX < 7; DistrictX++)
                    {
                        for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                        {
                            // Define rectangle once for use in all draws
                            Rectangle drawRect = new Rectangle(82 + DistrictX * 16, 1290 + DistrictZ * 16, 16, 16);

                            if (MostRecentPartyTurnArchitect.Block.X == DistrictX && MostRecentPartyTurnArchitect.Block.Z == DistrictZ)
                            {
                                _spriteBatch.Draw(CursorT, drawRect, Color.White);
                            }
                            else
                            {
                                Texture2D DecidedTexture = null;

                                if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures.Count == 0)
                                {
                                    string biome = MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Biome;
                                    if (biome == "desert")
                                    {
                                        DecidedTexture = DistrictEmptyDesertT;
                                    }
                                    else if (biome == "taiga" || biome == "mountain" || biome == "forest" || biome == "lightforest")
                                    {
                                        DecidedTexture = DistrictEmptyTreesT;
                                    }
                                    else if (biome == "tundra" || biome == "snowpeak")
                                    {
                                        DecidedTexture = DistrictEmptySnowT;
                                    }
                                    else if (biome == "ocean" || biome == "water")
                                    {
                                        DecidedTexture = DistrictEmptyOceanT;
                                    }
                                    else if (biome == "plains")
                                    {
                                        DecidedTexture = DistrictEmptyPlainsT;
                                    }
                                }
                                else if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures.Count == 1)
                                {
                                    switch (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures[0].Type)
                                    {
                                        case "bighouse":
                                        case "house":
                                            DecidedTexture = DistrictBuildingT;
                                            break;
                                        case "prism":
                                            DecidedTexture = DistrictPrismT;
                                            break;
                                        case "spire":
                                            DecidedTexture = DistrictSpireT;
                                            break;
                                        case "market":
                                            DecidedTexture = DistrictMarketT;
                                            break;

                                        case "commune":
                                            DecidedTexture = DistrictCommuneT;
                                            break;
                                        case "towers":
                                            DecidedTexture = DistrictTowersT;
                                            break;
                                        case "dock":
                                            DecidedTexture = DistrictDockT;
                                            break;
                                        case "ship":
                                            DecidedTexture = DistrictShipT;
                                            break;
                                        case "fortress":
                                            DecidedTexture = DistrictFortressT;
                                            break;
                                        case "hallway":
                                            DecidedTexture = DistrictHallwayT;
                                            break;
                                        case "mound":
                                            DecidedTexture = DistrictMoundT;
                                            break;
                                        case "core":
                                            DecidedTexture = DistrictCoreT;
                                            break;
                                        case "scaffold":
                                            DecidedTexture = DistrictScaffoldT;
                                            break;
                                        case "keep":
                                            DecidedTexture = DistrictKeepT;
                                            break;
                                        case "monastery":
                                            DecidedTexture = DistrictMonasteryT;
                                            break;
                                        case "monument":
                                            DecidedTexture = DistrictMonumentT;
                                            break;
                                        case "outpost":
                                            DecidedTexture = DistrictOutpostT;
                                            break;
                                        case "sanctum":
                                            DecidedTexture = DistrictSanctumT;
                                            break;
                                        case "heart":
                                            DecidedTexture = DistrictHeartT;
                                            break;
                                        case "scum":
                                            DecidedTexture = DistrictScumT;
                                            break;
                                        case "stronghold":
                                            DecidedTexture = DistrictStrongholdT;
                                            break;


                                        default:
                                            switch (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures[0].Block.District.Location.Layout)
                                            {
                                                case "archway":
                                                    DecidedTexture = DistrictArchwayT;
                                                    break;
                                                case "tower":
                                                    DecidedTexture = DistrictTowerT;
                                                    break;
                                                case "toroid":
                                                    DecidedTexture = DistrictToroidT;
                                                    break;
                                                case "hallway":
                                                    DecidedTexture = DistrictHallwayT;
                                                    break;
                                                case "pyramid":
                                                    DecidedTexture = DistrictPyramidT;
                                                    break;
                                                default:
                                                    DecidedTexture = DistrictSpecialBuildingT;
                                                    break;
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    bool FoundSpecial = false;
                                    bool FoundHouse = false;
                                    bool FoundMarket = false;
                                    bool FoundKeep = false;

                                    foreach (Structure s in MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures)
                                    {
                                        if (s.Type == "prism")
                                        {
                                            FoundKeep = true;
                                        }
                                        else if (s.Type == "bighouse" || s.Type == "house")
                                        {
                                            FoundHouse = true;
                                        }
                                        else if (s.Type == "market")
                                        {
                                            FoundMarket = true;
                                        }
                                        else
                                        {
                                            FoundSpecial = true;
                                        }
                                    }

                                    if (FoundKeep)
                                    {
                                        DecidedTexture = DistrictPrismT;
                                    }
                                    else if (FoundMarket && !FoundSpecial && !FoundHouse)
                                    {
                                        DecidedTexture = DistrictMarketT;
                                    }
                                    else if (FoundMarket)
                                    {
                                        DecidedTexture = DistrictMarketSurroundedT;
                                    }
                                    else if (FoundHouse && !FoundSpecial)
                                    {
                                        DecidedTexture = DistrictManyBuildingsT;
                                    }
                                    else
                                    {
                                        DecidedTexture = DistrictSpecialAndBuildingsT;
                                    }
                                }

                                Color BuildingColor = Color.White; //luminarch or other
                                if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures.Count > 0)
                                {
                                    switch (MostRecentPartyTurnArchitect.Location.PrimaryRace.Name)
                                    {
                                        case "nightfell":
                                            BuildingColor = new Color(100, 100, 100);
                                            break;
                                        case "archaix":
                                            BuildingColor = new Color(150, 150, 150);
                                            break;
                                        case "isofractal":
                                            BuildingColor = new Color(50, 150, 255);
                                            break;
                                        case "photonexus":
                                            BuildingColor = ColorConverter[MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures[0].FakePhotonexusColor];
                                            break;
                                        case "shade":
                                            BuildingColor = Color.White;
                                            if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures.Contains(MostRecentPartyTurnArchitect.Location.Prism))
                                            {
                                                DecidedTexture = DistrictHeartT;
                                            }
                                            else
                                            {
                                                DecidedTexture = DistrictScumT;
                                            }
                                            break;
                                    }
                                }

                                if (DecidedTexture != null)
                                {
                                    _spriteBatch.Draw(DecidedTexture, drawRect, BuildingColor);
                                }

                                foreach (Object o in MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Objects)
                                {
                                    if (o.Type == "well")
                                    {
                                        _spriteBatch.Draw(DistrictWellT, drawRect, Color.White);
                                    }
                                    else if (o.Type == "shadow storage")
                                    {
                                        _spriteBatch.Draw(DistrictShadowStorageT, drawRect, Color.White);
                                    }
                                }

                                if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Architects.Count > 0 && FlashTick > 50)
                                {
                                    var aliveArchitects = MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Architects.Where(architect => architect.IsAlive).ToList();
                                    if (aliveArchitects.Count > 0)
                                    {
                                        Color HeavinessColor = Color.White;
                                        if (aliveArchitects.Count > 10)
                                        {
                                            HeavinessColor = Color.Blue;
                                        }
                                        else if (aliveArchitects.Count > 5)
                                        {
                                            HeavinessColor = Color.CornflowerBlue;
                                        }
                                        else if (aliveArchitects.Count > 2)
                                        {
                                            HeavinessColor = Color.LightBlue;
                                        }
                                        else if (aliveArchitects.Count > 1)
                                        {
                                            HeavinessColor = Color.LightCyan;
                                        }

                                        _spriteBatch.Draw(ArchitectHere, drawRect, HeavinessColor);
                                    }
                                }

                            }
                        }
                    }

                    //debt

                    if (MostRecentPartyTurnArchitect.Structure != null && MostRecentPartyTurnArchitect.Structure.Type == "market")
                    {
                        Color c = Color.Green;
                        if (MostRecentPartyTurnArchitect.Structure.MarketDebt < 0)
                        {
                            c = Color.Red;
                        }
                        else if (MostRecentPartyTurnArchitect.Structure.MarketDebt == 0)
                        {
                            c = Color.White;
                        }

                        _spriteBatch.DrawString(Shibafont, "Owes you: " + MostRecentPartyTurnArchitect.Structure.MarketDebt, new Vector2(220, 1400), c);
                    }

                    int MaxLines = 26;  // Set the maximum number of lines you want to display
                    int MaxLength = 800; // Adjusted MaxLength for smaller font size

                    int screenHeight = 1440;
                    int lineHeight = 20; // Adjusted line height for smaller font size
                    int yPos = screenHeight - 400; // Initial Y position at the bottom

                    int totalLinesDisplayed = 0;  // Track the total number of lines displayed

                    // Convert the reversed announcements to a list
                    List<TextStorage> reversedAnnouncements = new List<TextStorage>(Announcements);
                    reversedAnnouncements.Reverse();

                    foreach (TextStorage announcement in reversedAnnouncements)
                    {
                        List<string> lines = new List<string>();

                        if (BabyShibafont.MeasureString(announcement.Data).X > MaxLength)
                        {
                            // Split announcement into lines if it exceeds MaxLength
                            string[] words = announcement.Data.Split(' ');
                            string currentLine = "";

                            foreach (var word in words)
                            {
                                if (BabyShibafont.MeasureString(currentLine + word).X > MaxLength)
                                {
                                    lines.Add(currentLine.TrimEnd());
                                    currentLine = word + " ";
                                }
                                else
                                {
                                    currentLine += word + " ";
                                }
                            }

                            lines.Add(currentLine.TrimEnd());
                        }
                        else
                        {
                            lines.Add(announcement.Data);
                        }

                        // Draw lines in reverse order
                        for (int i = lines.Count - 1; i >= 0; i--)
                        {
                            if (totalLinesDisplayed < MaxLines)
                            {
                                DrawAnnouncementLine(lines[i], yPos, announcement.Color, announcement.Entities);
                                yPos -= lineHeight;
                                totalLinesDisplayed++;
                            }
                            else
                            {
                                break;  // Exit the loop if the maximum number of lines is reached
                            }
                        }

                        if (totalLinesDisplayed >= MaxLines)
                        {
                            break;  // Exit the outer loop if the maximum number of lines is reached
                        }
                    }

                    // Helper function to draw each announcement line and create hitboxes for entity names
                    void DrawAnnouncementLine(string text, int yPosition, Color color, List<Entity> entities)
                    {
                        _spriteBatch.DrawString(BabyShibafont, text, new Vector2(50, yPosition), color);

                        // Create hitboxes for the longest entity names
                        foreach (var entity in entities)
                        {
                            string longestName = entity.ReferredToNames.OrderByDescending(name => name.Length).FirstOrDefault(name => text.Contains(name));

                            if (!string.IsNullOrEmpty(longestName))
                            {
                                int index = text.IndexOf(longestName);
                                if (index != -1)
                                {
                                    string substringBeforeName = text.Substring(0, index);
                                    Vector2 sizeBeforeName = BabyShibafont.MeasureString(substringBeforeName);
                                    Vector2 positionOfName = new Vector2(50 + sizeBeforeName.X, yPosition);

                                    // Measure the length of the text up to the longest name
                                    Vector2 sizeOfName = BabyShibafont.MeasureString(longestName);
                                    Rectangle hitbox = new Rectangle(positionOfName.ToPoint(), sizeOfName.ToPoint());
                                    hitbox = ScaleRectangle(hitbox, scaleX, scaleY);
                                    EntityHitboxes.Add((hitbox, entity));
                                }
                            }
                        }
                    }




                    // Helper function to scale a rectangle
                    Rectangle ScaleRectangle(Rectangle rect, float scaleX, float scaleY)
                    {
                        return new Rectangle(
                            (int)(rect.X * scaleX),
                            (int)(rect.Y * scaleY),
                            (int)(rect.Width * scaleX),
                            (int)(rect.Height * scaleY)
                        );
                    }

                    // Determine the source collection of objects based on the condition
                    var sourceObjects = MostRecentPartyTurnArchitect.Structure != null ? MostRecentPartyTurnArchitect.Room.Objects : MostRecentPartyTurnArchitect.Block.Objects;

                    // Condense and structure the list from the chosen collection
                    var structuredList = CondenseAndStructureList(sourceObjects, SplitMode);

                    // Draw the structured list
                    DrawList(_spriteBatch, structuredList, new Vector2(2150, 125), BabyShibafont, CurrentObjectPage, ItemsPerPage);

                    int Houses = 0;


                    // Method to get unique architects
                    List<(string description, int count, Architect architect)> GetUniqueArchitects(IEnumerable<Architect> architects)
                    {
                        var uniqueArchitects = new Dictionary<string, (int count, Architect architect)>();
                        foreach (var architect in architects)
                        {
                            if (architect == MostRecentPartyTurnArchitect)
                            {
                                continue;
                            }

                            // Create a description for the architect
                            string description = $"{architect.Race.RaceLetter} {ConvertArchitectToDescription(architect)}, distance {architect.GetDistance(MostRecentPartyTurnArchitect)}";

                            if (uniqueArchitects.ContainsKey(description))
                            {
                                uniqueArchitects[description] = (uniqueArchitects[description].count + 1, uniqueArchitects[description].architect);
                            }
                            else
                            {
                                uniqueArchitects.Add(description, (1, architect));
                            }
                        }
                        return uniqueArchitects.Select(kvp => (kvp.Key, kvp.Value.count, kvp.Value.architect)).ToList();
                    }

                    // Method to draw architects
                    void DrawArchitects(SpriteBatch spriteBatch, List<(string description, int count, Architect architect)> uniqueArchitects, Vector2 startCoords, SpriteFont font, int indentSize)
                    {
                        int line = 0;
                        foreach (var architect in uniqueArchitects)
                        {
                            string textToDraw = $"{architect.description}, x{architect.count}";
                            Vector2 indentCoords = new Vector2(startCoords.X, startCoords.Y + line * 20);
                            spriteBatch.DrawString(font, textToDraw, indentCoords, Color.White);

                            // Create a hitbox for the first entity in the stack or for each individual entity in SplitMode
                            if (SplitMode || architect.count == 1)
                            {
                                EntityHitboxes.Add((new Rectangle(indentCoords.ToPoint(), font.MeasureString(textToDraw).ToPoint()), architect.architect));
                            }
                            else
                            {
                                EntityHitboxes.Add((new Rectangle(indentCoords.ToPoint(), font.MeasureString(textToDraw).ToPoint()), architect.architect));
                            }

                            line++;
                        }
                    }

                    // Draw logic for architects and structures
                    if (MostRecentPartyTurnArchitect.Structure != null)
                    {
                        _spriteBatch.Draw(whiteRect, new Rectangle(1650, 0, 444, 1920), Color.Black);

                        // Get a list of unique architects
                        var uniqueArchitects = GetUniqueArchitects(MostRecentPartyTurnArchitect.Room.Architects);
                        DrawArchitects(_spriteBatch, uniqueArchitects, new Vector2(950, 150), BabyShibafont, 20);
                    }
                    else
                    {
                        // Get a list of unique architects
                        var uniqueArchitects = GetUniqueArchitects(MostRecentPartyTurnArchitect.Block.Architects);
                        DrawArchitects(_spriteBatch, uniqueArchitects, new Vector2(950, 150), BabyShibafont, 20);

                        Line = 0;
                        foreach (Structure s in MostRecentPartyTurnArchitect.Block.Structures)
                        {
                            if (s.Type == "house" || s.Type == "bighouse")
                            {
                                Houses++;
                            }
                            else
                            {
                                Line++;
                                string structureText = "(" + s.Type.Substring(0, 1).ToUpper() + ") " + s.Name;
                                Vector2 structurePosition = new Vector2(1700, Line * 30 + 200);
                                _spriteBatch.DrawString(Shibafont, structureText, structurePosition, Color.White);
                                EntityHitboxes.Add((new Rectangle(structurePosition.ToPoint(), Shibafont.MeasureString(structureText).ToPoint()), s));
                            }
                        }
                    }

                    if (Houses == 1)
                    {
                        _spriteBatch.DrawString(Shibafont, "1 house (house 1)", new Vector2(1700, 180), Color.White);
                    }
                    else if (Houses > 1)
                    {
                        _spriteBatch.DrawString(Shibafont, Houses + " houses (house 1-" + Houses + ")", new Vector2(1700, 180), Color.White);
                    }


                    if (IsInGui)
                    {
                        _spriteBatch.Draw(MessageGUIT, new Rectangle(0, 0, 2560, 1440), Color.White);
                        int Linee = 0;
                        foreach (string s in ItemPickupGuiLines)
                        {
                            DrawCenteredText(_spriteBatch, s, Linee * 20 + 600, Shibafont, Color.White);
                            Linee++;
                        }
                    }

                    if (GameState == "reaction")
                    {
                        _spriteBatch.Draw(ReactionGUIT, new Rectangle(320, 180, 1920, 1080), Color.White);

                        // Calculate the success chances for the MostRecentPartyTurnArchitect
                        var successChances = MostRecentPartyTurnArchitect.CalculateSuccessChances(StoredAttacks[0], GameWorld.ReactionModifierInt, StoredAttacks[0].Attacker, StoredAttacks[0].Attacker.GetProficiency(StoredAttacks[0].Weapon.DamageType));

                        int y = 600;
                        int d = 30;
                        DrawCenteredText(_spriteBatch, StoredAttacks[0].Attacker.ReferredToNames[0] + " is aiming a " + StoredAttacks[0].Verb, y, Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "at your " + StoredAttacks[0].Target.Type + " with their " + StoredAttacks[0].Weapon.ReferredToNames[0], y + d, Shibafont, Color.White);

                        int RoundToNearestFive(int value)
                        {
                            return ((value + 2) / 5) * 5;
                        }


                        DrawCenteredText(_spriteBatch, "[S] Sustain (" + RoundToNearestFive(successChances.sustain) + "% evs.?)", y + d * 2, Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "[D] Duck (" + RoundToNearestFive(successChances.duck) + "% evs.?)", y + d * 3, Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "[J] Jump (" + RoundToNearestFive(successChances.jump) + "% evs.?)", y + d * 4, Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "[R] Roll (" + RoundToNearestFive(successChances.roll) + "% evs.?)", y + d * 5, Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "[N] Disarm (" + RoundToNearestFive(successChances.disarm) + "% evs.?)", y + d * 6, Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "[C] Redirect (" + RoundToNearestFive(successChances.redirect) + "% evs.?)", y + d * 7, Shibafont, Color.White);

                        if ((MostRecentPartyTurnArchitect.LeftHandObject != null && MostRecentPartyTurnArchitect.LeftHandObject.IsWeapon) || (MostRecentPartyTurnArchitect.RightHandObject != null && MostRecentPartyTurnArchitect.RightHandObject.IsWeapon))
                        {
                            DrawCenteredText(_spriteBatch, "[P] Parry the Attack (requires weapon) (" + RoundToNearestFive(successChances.parry) + "%?)", y + d * 8, Shibafont, Color.White);
                        }
                        if ((MostRecentPartyTurnArchitect.RightHandObject != null && MostRecentPartyTurnArchitect.RightHandObject.Type == "shield") || (MostRecentPartyTurnArchitect.LeftHandObject != null && MostRecentPartyTurnArchitect.LeftHandObject.Type == "shield"))
                        {
                            DrawCenteredText(_spriteBatch, "[B] Block the Attack (requires shield) (" + RoundToNearestFive(successChances.block) + "%?)", y + d * 9, Shibafont, Color.White);
                        }

                        DrawCenteredText(_spriteBatch, "Chances calculated with Agility, items carried/Endurance,", y + d * 10, Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "your/opponent skills, attack type, etc.", y + d * 11, Shibafont, Color.White);

                    }
                    else if (GameState == "trypickup")
                    {
                        int GetHalfCount(string itemType)
                        {
                            int totalCount = MostRecentPartyTurnArchitect.Room.Objects
                                .Count(item => item.Type == itemType);
                            return (int)Math.Ceiling(totalCount / 2.0);
                        }

                        int GetFullCount(string itemType)
                        {
                            return MostRecentPartyTurnArchitect.Room.Objects
                                .Count(item => item.Type == itemType);
                        }

                        _spriteBatch.Draw(ReactionGUIT, new Rectangle(320, 180, 1920, 1080), Color.White);

                        int totalCount = GetFullCount(MostRecentPartyTurnArchitect.TryPickUpItemType);
                        int halfCount = GetHalfCount(MostRecentPartyTurnArchitect.TryPickUpItemType);

                        int y = 600;
                        int d = 30;
                        DrawCenteredText(_spriteBatch, "Pick up how many?", y, Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "[1] Pick up 1.", y + d, Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, $"[2] Pick up half ({halfCount}).", y + d * 2, Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, $"[3] Pick up all ({totalCount}).", y + d * 3, Shibafont, Color.White);
                    }

                    else if (GameState == "trydrop")
                    {
                        int GetHalfCount(string itemType, List<Material> itemMaterials)
                        {
                            int totalCount = MostRecentPartyTurnArchitect.Inventory
                                .Count(item => item.Type == itemType && item.Materials.SequenceEqual(itemMaterials));
                            return (int)Math.Ceiling(totalCount / 2.0);
                        }

                        int GetFullCount(string itemType, List<Material> itemMaterials)
                        {
                            return MostRecentPartyTurnArchitect.Inventory
                                .Count(item => item.Type == itemType && item.Materials.SequenceEqual(itemMaterials));
                        }

                        _spriteBatch.Draw(ReactionGUIT, new Rectangle(320, 180, 1920, 1080), Color.White);

                        int totalCount = GetFullCount(MostRecentPartyTurnArchitect.TryDropItemType, MostRecentPartyTurnArchitect.TryDropMaterials);
                        int halfCount = GetHalfCount(MostRecentPartyTurnArchitect.TryDropItemType, MostRecentPartyTurnArchitect.TryDropMaterials);

                        int y = 600;
                        int d = 30;
                        DrawCenteredText(_spriteBatch, "Drop how many?", y, Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "[1] Drop 1.", y + d, Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, $"[2] Drop half ({halfCount}).", y + d * 2, Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, $"[3] Drop all ({totalCount}).", y + d * 3, Shibafont, Color.White);
                    }

                    else if (GameState == "messagereply")
                    {
                        _spriteBatch.Draw(MessageGUIT, new Rectangle(0, 0, 2560, 1440), Color.White);

                        string TruncateText(string text, int maxLength)
                        {
                            if (text.Length <= maxLength)
                                return text;
                            return text.Substring(0, maxLength) + "...";
                        }

                        int y = 600;
                        int d = 30;
                        DrawCenteredText(_spriteBatch, MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Sender.ReferredToNames[0] + " says:", y, Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "\"" + MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].MessageContent + "\"", y + d, Shibafont, Color.White);


                        int TruncationLength = 70;

                        if (MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].MessageType == "question")
                        {
                            DrawCenteredText(_spriteBatch, "[T] Reply Truthfully: \"" + TruncateText(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].PositiveResponse, TruncationLength) + "\"", y + d * 2, Shibafont, Color.White);
                            DrawCenteredText(_spriteBatch, "[M] Make Something Up: \"" + TruncateText(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].DirectRefusalResponse, TruncationLength) + "\"", y + d * 3, Shibafont, Color.White);
                            DrawCenteredText(_spriteBatch, "[I] Claim Ignorance: \"" + TruncateText(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].IgnorantResponse, TruncationLength) + "\"", y + d * 4, Shibafont, Color.White);
                            DrawCenteredText(_spriteBatch, "[D] Derail Conversation: \"" + TruncateText(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].DerailingResponse, TruncationLength) + "\"", y + d * 5, Shibafont, Color.White);
                            DrawCenteredText(_spriteBatch, "[F] Say Something Flattering: \"" + TruncateText(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].FlatteringResponse, TruncationLength) + "\"", y + d * 6, Shibafont, Color.White);
                            DrawCenteredText(_spriteBatch, "[X] Do Not Respond.", y + d * 7, Shibafont, Color.White);
                        }
                        else if (MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].MessageType == "request")
                        {
                            DrawCenteredText(_spriteBatch, "[T] Comply: \"" + TruncateText(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].PositiveResponse, TruncationLength) + "\"", y + d * 2, Shibafont, Color.White);
                            DrawCenteredText(_spriteBatch, "[M] Directly Deny: \"" + TruncateText(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].DirectRefusalResponse, TruncationLength) + "\"", y + d * 3, Shibafont, Color.White);
                            DrawCenteredText(_spriteBatch, "[I] Act Confused: \"" + TruncateText(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].IgnorantResponse, TruncationLength) + "\"", y + d * 4, Shibafont, Color.White);
                            DrawCenteredText(_spriteBatch, "[D] Derail Conversation: \"" + TruncateText(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].DerailingResponse, TruncationLength) + "\"", y + d * 5, Shibafont, Color.White);
                            DrawCenteredText(_spriteBatch, "[F] Say something Flattering: \"" + TruncateText(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].FlatteringResponse, TruncationLength) + "\"", y + d * 6, Shibafont, Color.White);
                            DrawCenteredText(_spriteBatch, "[X] Do not respond.", y + d * 7, Shibafont, Color.White);
                        }
                    }
                    else if (CommandBuilderStage != "none")
                    {
                        // Draw the message GUI
                        _spriteBatch.Draw(MessageGUIT, new Rectangle(320, 180, 1920, 1080), Color.White);

                        // Handle 'X' key press for going back
                        if (KeysNewlyPressed.Contains(Keys.X))
                        {
                            switch (CommandBuilderStage)
                            {
                                case "categories":
                                    CommandBuilderStage = "none";
                                    SelectedCategory = "";
                                    break;
                                case "commands":
                                    CommandBuilderStage = "categories";
                                    SelectedCommand = "";
                                    break;
                                case "pickingsubjects":
                                    CommandBuilderStage = "commands";
                                    SelectedEntities.Clear();
                                    break;
                                case "execution":
                                    CommandBuilderStage = "pickingsubjects";
                                    break;
                                default:
                                    break;
                            }
                        }

                        // Handle 'Z' key press for manual finishing
                        if (KeysNewlyPressed.Contains(Keys.Z))
                        {
                            // Eventually, this will break out so you can modify the command you are currently working on by yourself.
                        }

                        startY = 400; // Consistent starting Y position

                        // Draw categories
                        if (CommandBuilderStage == "categories")
                        {
                            var categories = new Dictionary<int, string>
                            {
                                { 1, "General" },
                                { 2, "Questions" },
                                { 3, "Requests" },
                                { 4, "Movement" },
                                { 5, "Offensive" },
                                { 6, "Defensive" },
                                { 7, "Items" },
                                { 8, "Creativity" },
                                { 9, "Movement" }
                            };

                            DrawListInColumns(categories.Select(c => $"[{c.Key}] {c.Value}").ToList(), 830, startY, 40, 50, BabyShibafont);
                        }
                        // Draw commands
                        else if (CommandBuilderStage == "commands")
                        {
                            var commands = GetCommandsForCategory(SelectedCategory);
                            var keys = new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" };

                            var commandList = commands.Take(keys.Count).Select((cmd, index) => $"[{keys[index]}] {cmd}").ToList();
                            DrawListInColumns(commandList, 830, startY, 40, 50, BabyShibafont);
                        }
                        // Draw pickingsubjects
                        else if (CommandBuilderStage == "pickingsubjects")
                        {
                            int yPositionLeft = startY; // Starting Y position for the left column
                            int yPositionRight = startY; // Starting Y position for the right column
                            int entitiesPerColumn = 10; // 10 entities per column
                            int startIndex = CurrentCommandBuilderPage * 20; // Calculate start index based on the current page

                            // Draw subjects
                            if (RelevantEntities.Count == 0)
                            {
                                _spriteBatch.DrawString(BabyShibafont, "No Applicable Subjects", new Vector2(830, yPositionLeft), Color.White);
                            }
                            else
                            {
                                var subjectList = new List<string>();
                                for (int i = 0; i < 20 && (startIndex + i) < RelevantEntities.Count; i++)
                                {
                                    var entity = RelevantEntities[startIndex + i];
                                    subjectList.Add($"[{i + 1}] {entity.ReferredToNames[0]}");
                                }
                                DrawListInColumns(subjectList, 830, startY, 40, 50, BabyShibafont);
                            }

                            // Draw guides at the bottom
                            _spriteBatch.DrawString(BabyShibafont, "[X] Go Back", new Vector2(830, 1050), Color.White);
                        }
                    }


                    void DrawListInColumns(List<string> items, int startX, int startY, int lineSpacing, int boundaryPadding, SpriteFont font)
                    {
                        int xPosition = startX;
                        int yPosition = startY;
                        int columnWidth = 960; // Half of the MessageGUIT width

                        foreach (var item in items)
                        {
                            // Check if item would get too close to the right boundary of the MessageGUIT
                            if (yPosition + lineSpacing > 1080 - boundaryPadding)
                            {
                                xPosition += columnWidth;
                                yPosition = startY;
                            }

                            // Draw the item
                            _spriteBatch.DrawString(font, item, new Vector2(xPosition, yPosition), Color.White);

                            yPosition += lineSpacing;
                        }
                    }

                    var sortedArchitects = GamePlayerParty.Architects.OrderBy(a => a.CooldownCycles).ToList();
                    int currentX = 1400;

                    foreach (var architect in sortedArchitects)
                    {
                        if (GameWorld.HumanoidRaces.Contains(architect.Race))
                        {
                            // Draw the character
                            DrawCharacter(architect, currentX, 1200, 0.2);
                            currentX -= 150; // Move to the left for the next character
                        }
                    }

                    if (MostRecentPartyTurnArchitect.CombatCycles > 0)
                    {
                        if (GameWorld.HumanoidRaces.Contains(MostRecentPartyTurnArchitect.Race))
                        {
                            Vector2 bodyPartsPosition = new Vector2(currentX + 225, 1225); // Adjust the X position as needed

                            // Draw the body parts
                            DrawBodyParts(_spriteBatch, bodyPartsPosition);

                            // Calculate the position for the "Exposure" text
                            float textCenterX = bodyPartsPosition.X + (300 * 0.6f) / 2; // Assuming the width of the rectangle is 300
                            float textCenterY = bodyPartsPosition.Y - 20; // Position the text above the rectangle

                            // Draw the "Exposure" text
                            DrawCenteredTextAtPosition(_spriteBatch, "Exposure", textCenterX, textCenterY, BabyShibafont);
                        }
                    }

                    // Exposure graph

                    void DrawBodyParts(SpriteBatch spriteBatch, Vector2 position)
                    {
                        // Draw basegui and full at the same position and color
                        spriteBatch.Draw(BaseRepositionGUIT, position, null, Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                        spriteBatch.Draw(BodyFrameT, position, null, Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

                        DrawBodyPart(spriteBatch, HeadRepT, "head", position);
                        DrawBodyPart(spriteBatch, LeftArmRepT, "left arm", position);
                        DrawBodyPart(spriteBatch, RightArmRepT, "right arm", position);
                        DrawBodyPart(spriteBatch, LeftFootRepT, "left foot", position);
                        DrawBodyPart(spriteBatch, LeftHandRepT, "left hand", position);
                        DrawBodyPart(spriteBatch, RightHandRepT, "right hand", position);
                        DrawBodyPart(spriteBatch, RightFootLeftT, "right foot", position);
                        DrawBodyPart(spriteBatch, RightLegT, "right leg", position);
                        DrawBodyPart(spriteBatch, LeftLegT, "left leg", position);
                        DrawBodyPart(spriteBatch, TorsoT, "torso", position);
                    }

                    void DrawBodyPart(SpriteBatch spriteBatch, Texture2D texture, string bodyPartType, Vector2 position)
                    {
                        int exposure = MostRecentPartyTurnArchitect.FindBodyPart(bodyPartType).Exposure;
                        Color color = InterpolateColor(Color.White, new Color(85, 0, 170), exposure / 100f);

                        spriteBatch.Draw(texture, position, null, color, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    }

                    Color InterpolateColor(Color start, Color end, float amount)
                    {
                        return new Color(
                            (byte)(start.R + (end.R - start.R) * amount),
                            (byte)(start.G + (end.G - start.G) * amount),
                            (byte)(start.B + (end.B - start.B) * amount),
                            (byte)(start.A + (end.A - start.A) * amount)
                        );
                    }

                    if (Keyboard.GetState().IsKeyDown(Keys.F2) && StoredPortrait != null)
                    {
                        DrawCharacter(StoredPortrait, 40, 600, 0.2);
                    }

                }
                else
                {
                    _spriteBatch.Draw(InventoryGUI, new Rectangle(0, 0, 2560, 1440), Color.White);

                    int line = 0;

                    var sourceObjects = MostRecentPartyTurnArchitect.Inventory;
                    var structuredList = CondenseAndStructureList(sourceObjects, SplitMode);
                    DrawList(_spriteBatch, structuredList, new Vector2(2100, 100), BabyShibafont, CurrentObjectPage, ItemsPerPage);

                    line = 0;
                    if (MostRecentPartyTurnArchitect.Clothing.Count == 0)
                    {
                        DrawCenteredTextAtPosition(_spriteBatch, "You have no clothing. Please put some on.", 1625, 425, BabyShibafont);
                    }
                    else
                    {
                        foreach (Object o in MostRecentPartyTurnArchitect.Clothing)
                        {
                            float centerY = 425 + 20 * line;
                            string text = o.ReferredToNames[0];

                            // Draw the text
                            DrawCenteredTextAtPosition(_spriteBatch, text, 1625, centerY, BabyShibafont);

                            // Measure the size of the text
                            Vector2 textSize = BabyShibafont.MeasureString(text);

                            // Calculate the position to start drawing the text so that it is centered on (1625, centerY)
                            float xPosition = 1625 - (textSize.X / 2);
                            float yPosition = centerY - (textSize.Y / 2);

                            // Create a Rectangle for the hitbox
                            Rectangle hitbox = new Rectangle((int)xPosition, (int)yPosition, (int)textSize.X, (int)textSize.Y);
                            EntityHitboxes.Add((hitbox, o));

                            line++;
                        }
                    }

                    line = 0;
                    if (MostRecentPartyTurnArchitect.Intrigue.Count == 0)
                    {
                        DrawCenteredTextAtPosition(_spriteBatch, "Nothing is here yet...", 1625, 1150, BabyShibafont);
                    }
                    else
                    {
                        foreach (var intrigue in MostRecentPartyTurnArchitect.Intrigue)
                        {
                            var (text, architect) = intrigue;
                            Vector2 textPosition = new Vector2(1640, 1150 + 15 * line);
                            DrawCenteredTextAtPosition(_spriteBatch, text, (int)textPosition.X, (int)textPosition.Y, BabyShibafont);

                            // Create hitbox for the intrigue text
                            Vector2 textSize = Shibafont.MeasureString(text);
                            Rectangle hitbox = new Rectangle(textPosition.ToPoint(), textSize.ToPoint());
                            EntityHitboxes.Add((hitbox, architect));

                            line++;
                        }
                    }

                    line = 0;
                    if (MostRecentPartyTurnArchitect.CurrentlyActiveImbuements.Count == 0)
                    {
                        DrawCenteredTextAtPosition(_spriteBatch, "No active imbuements.", 950, 425, BabyShibafont);
                    }
                    else
                    {
                        foreach (Imbuement i in MostRecentPartyTurnArchitect.CurrentlyActiveImbuements)
                        {
                            string description = i.GetDescription();
                            while (description.Length > 40)
                            {
                                int splitIndex = 40;
                                if (description.Length > 40)
                                {
                                    splitIndex = description.Substring(0, 40).LastIndexOf(' ');
                                    if (splitIndex == -1) splitIndex = 40;
                                }

                                DrawCenteredTextAtPosition(_spriteBatch, description.Substring(0, splitIndex), 950, 425 + 20 * line, BabyShibafont);
                                description = description.Substring(splitIndex).Trim();
                                line++;
                            }

                            DrawCenteredTextAtPosition(_spriteBatch, description, 950, 425 + 20 * line, BabyShibafont);
                            line++;
                        }
                    }


                    // XPValues processing remains unchanged as you already have a check for no entries.


                    line = 0;

                    foreach ((string, int) p in MostRecentPartyTurnArchitect.XPValues)
                    {
                        line++;
                        DrawCenteredTextAtPosition(_spriteBatch, $"{p.Item1}: {ConvertNumberToProficiency(MostRecentPartyTurnArchitect.GetProficiency(p.Item1))}, {MostRecentPartyTurnArchitect.GetProficiency(p.Item1)} XP", 290, 100 + 20 * line, BabyShibafont);
                    }
                    if (line == 0)
                    {
                        DrawCenteredTextAtPosition(_spriteBatch, "No proficiencies...?", 280, 120 + 10 * line, BabyShibafont);
                    }

                    //level up screen
                    void DrawPathLevel(SpriteBatch spriteBatch, SpriteFont babyShibaFont, string pathName, int pathLevel, int Y, Color color)
                    {
                        string text = pathLevel > 0 ? $"{pathName} Level {pathLevel}" : $"{pathName} (not activated)";
                        DrawCenteredText(_spriteBatch, text, Y, babyShibaFont, pathLevel > 0 ? color : new Color(40, 40, 40));
                    }
                    if (MostRecentPartyTurnArchitect.SpendableLevels > 0)
                    {
                        int newWidth = 1919 + 200; // Increase width by 200
                        int newHeight = 1080 + 200; // Increase height by 200

                        _spriteBatch.Draw(
                            ReactionGUIT,
                            new Rectangle(
                                (2560 - newWidth) / 2, // Centered X
                                (1440 - newHeight) / 2, // Centered Y
                                newWidth,
                                newHeight),
                            Color.White);

                        int boxX = (2560 - newWidth) / 2;
                        int boxY = (1440 - newHeight) / 2;

                        int startingPosition = 500;
                        int spacing = 30;

                        void DrawPathLevels(Party GamePlayerParty, SpriteBatch spriteBatch, SpriteFont babyShibaFont)
                        {
                            int position = startingPosition;

                            // Each path's drawing code:
                            DrawPathLevel(spriteBatch, babyShibaFont, "[X] Path of Shadow", MostRecentPartyTurnArchitect.PathOfShadowLevel, position, Color.MidnightBlue);
                            position += spacing;

                            DrawPathLevel(spriteBatch, babyShibaFont, "[L] Path of Life", MostRecentPartyTurnArchitect.PathOfLifeLevel, position, Color.ForestGreen);
                            position += spacing;

                            DrawPathLevel(spriteBatch, babyShibaFont, "[D] Path of Death", MostRecentPartyTurnArchitect.PathOfDeathLevel, position, Color.DarkRed);
                            position += spacing;

                            DrawPathLevel(spriteBatch, babyShibaFont, "[A] Path of Stars", MostRecentPartyTurnArchitect.PathOfStarsLevel, position, Color.Gold);
                            position += spacing;

                            DrawPathLevel(spriteBatch, babyShibaFont, "[H] Path of Heat", MostRecentPartyTurnArchitect.PathOfHeatLevel, position, Color.OrangeRed);
                            position += spacing;

                            DrawPathLevel(spriteBatch, babyShibaFont, "[B] Path of Body", MostRecentPartyTurnArchitect.PathOfBodyLevel, position, Color.SandyBrown);
                            position += spacing;

                            DrawPathLevel(spriteBatch, babyShibaFont, "[R] Path of Reality", MostRecentPartyTurnArchitect.PathOfRealityLevel, position, Color.IndianRed);
                            position += spacing;

                            DrawPathLevel(spriteBatch, babyShibaFont, "[G] Path of Light", MostRecentPartyTurnArchitect.PathOfLightLevel, position, Color.Yellow);
                            position += spacing;

                            /*
                            DrawPathLevel(spriteBatch, babyShibaFont, "[I] Path of Illusions", MostRecentPartyTurnArchitect.PathOfIllusionsLevel, position, Color.Magenta);
                            position += spacing;

                            DrawPathLevel(spriteBatch, babyShibaFont, "[T] Path of Time", MostRecentPartyTurnArchitect.PathOfTimeLevel, position, Color.SkyBlue);
                            position += spacing;

                            DrawPathLevel(spriteBatch, babyShibaFont, "[E] Path of Ethereality", MostRecentPartyTurnArchitect.PathOfEtherealityLevel, position, Color.LightBlue);
                            position += spacing;

                            DrawPathLevel(spriteBatch, babyShibaFont, "[V] Path of Void", MostRecentPartyTurnArchitect.PathOfVoidLevel, position, Color.DarkSlateBlue);
                            position += spacing;

                            DrawPathLevel(spriteBatch, babyShibaFont, "[S] Path of Storms", MostRecentPartyTurnArchitect.PathOfStormsLevel, position, Color.Cyan);
                            position += spacing;

                            DrawPathLevel(spriteBatch, babyShibaFont, "[F] Path of Forge", MostRecentPartyTurnArchitect.PathOfForgeLevel, position, Color.DarkOrange);
                            position += spacing;

                            DrawPathLevel(spriteBatch, babyShibaFont, "[K] Path of Lore", MostRecentPartyTurnArchitect.PathOfLoreLevel, position, Color.SeaGreen);
                            position += spacing;

                            DrawPathLevel(spriteBatch, babyShibaFont, "[M] Path of Mind", MostRecentPartyTurnArchitect.PathOfMindLevel, position, Color.LightSeaGreen);
                            position += spacing;

                            DrawPathLevel(spriteBatch, babyShibaFont, "[U] Path of Soul", MostRecentPartyTurnArchitect.PathOfSoulLevel, position, Color.MediumPurple);
                            position += spacing;

                            DrawPathLevel(spriteBatch, babyShibaFont, "[P] Path of Space", MostRecentPartyTurnArchitect.PathOfSpaceLevel, position, Color.DarkOrchid);
                            position += spacing;
                            */
                        }

                        DrawPathLevels(GamePlayerParty, _spriteBatch, BabyShibafont);

                        if (!Keyboard.GetState().IsKeyDown(Keys.LeftControl))
                        {
                            if (Keyboard.GetState().IsKeyDown(Keys.X))
                            {
                                _spriteBatch.Draw(
                                    ReactionGUIT,
                                    new Rectangle(
                                        (2560 - newWidth) / 2, // Centered X
                                        (1440 - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                int position = startingPosition;

                                DrawCenteredText(_spriteBatch, "PATH OF SHADOW", position, BabyShibafont, Color.MidnightBlue);
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 AGL ", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 1) ? Color.MidnightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Become harder to see and target.", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 2) ? Color.MidnightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 AGL ", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 3) ? Color.MidnightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Become invisible at the cost of energy with \"become one with shadow\"", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 4) ? Color.MidnightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 AGL ", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 5) ? Color.MidnightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Your possessions become invisible with you.", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 6) ? Color.MidnightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 AGL ", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 7) ? Color.MidnightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Invisibilty no longer causes energy loss.", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 8) ? Color.MidnightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 9: +1 AGL ", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 9) ? Color.MidnightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "PRESS CTRL X TO LEVEL UP THIS PATH.", position, BabyShibafont, Color.White);
                            }

                            if (Keyboard.GetState().IsKeyDown(Keys.L)) // Assuming 'L' is the key for Path of Life
                            {
                                _spriteBatch.Draw(ReactionGUIT, new Rectangle((2560 - newWidth) / 2, (1440 - newHeight) / 2, newWidth, newHeight), Color.White);

                                int position = startingPosition;

                                // Title
                                DrawCenteredText(_spriteBatch, "PATH OF LIFE", position, BabyShibafont, Color.ForestGreen);
                                position += spacing;

                                // Display abilities with conditional coloring based on the level
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 CHA", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 1) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;

                                DrawCenteredText(_spriteBatch, "LVL 2: Gain a constant regeneration buff.", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 2) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;

                                DrawCenteredText(_spriteBatch, "LVL 3: +1 CHA", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 3) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;

                                DrawCenteredText(_spriteBatch, "LVL 4: Full communication with all creatures.", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 4) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 CHA", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 5) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Pacify/Tame animals, add to party. Max of Path LVL animals.", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 6) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 CHA", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 7) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;

                                DrawCenteredText(_spriteBatch, "LVL 8: Buff/augment your animals with \"augument\".", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 8) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;

                                DrawCenteredText(_spriteBatch, "LVL 9: +1 CHA", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 9) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;

                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "PRESS CTRL + L TO LEVEL UP THIS PATH.", position, BabyShibafont, Color.White);
                            }
                            else if (Keyboard.GetState().IsKeyDown(Keys.D))
                            {
                                _spriteBatch.Draw(
                                    ReactionGUIT,
                                    new Rectangle(
                                        (2560 - newWidth) / 2, // Centered X
                                        (1440 - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                int position = startingPosition;

                                // Title
                                DrawCenteredText(_spriteBatch, "PATH OF DEATH", position, BabyShibafont, Color.DarkRed);
                                position += spacing;
                                // Abilities
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 FOC", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 1 ? Color.DarkRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Raise ((LVL/2) rounded down) weakened undead servants.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 2 ? Color.DarkRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 FOC", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 3 ? Color.DarkRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Fire spectral bolts.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 4 ? Color.DarkRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 FOC", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 5 ? Color.DarkRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Foes slain by bolts may become undead.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 6 ? Color.DarkRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 FOC", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 7 ? Color.DarkRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Return to life with 20 energy once a week.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 8 ? Color.DarkRed : new Color(40, 40, 40));
                                position += spacing;
                                // Leveling up instruction
                                DrawCenteredText(_spriteBatch, "PRESS CTRL + D TO LEVEL UP THIS PATH.", position, BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.A)) // Assuming 'A' is the key for Path of Stars
                            {
                                _spriteBatch.Draw(
                                    ReactionGUIT,
                                    new Rectangle(
                                        (2560 - newWidth) / 2, // Centered X
                                        (1440 - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                int position = startingPosition;

                                // Title
                                DrawCenteredText(_spriteBatch, "PATH OF STARS", position, BabyShibafont, Color.Gold);
                                position += spacing;
                                // Abilities
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 CRE", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 1 ? Color.Gold : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Summon a falling star on strike.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 2 ? Color.Gold : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 CRE", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 3 ? Color.Gold : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Starmarked creatures ignite.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 4 ? Color.Gold : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 CRE", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 5 ? Color.Gold : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Fire stars from your hands with \"starstrike ~\".", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 6 ? Color.Gold : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 CRE", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 7 ? Color.Gold : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Open a portal to a star.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 8 ? Color.Gold : new Color(40, 40, 40));
                                position += spacing;
                                // Leveling up instruction
                                DrawCenteredText(_spriteBatch, "PRESS CTRL + A TO LEVEL UP THIS PATH.", position, BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.H)) // Assuming 'H' is the key for Path of Heat
                            {
                                _spriteBatch.Draw(
                                    ReactionGUIT,
                                    new Rectangle(
                                        (2560 - newWidth) / 2, // Centered X
                                        (1440 - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                int position = startingPosition;

                                // Title
                                DrawCenteredText(_spriteBatch, "PATH OF HEAT", position, BabyShibafont, Color.OrangeRed);
                                position += spacing;
                                // Abilities
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 END", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 1 ? Color.OrangeRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Conjure and throw waves of heat.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 2 ? Color.OrangeRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 END", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 3 ? Color.OrangeRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Control heat of objects you touch.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 4 ? Color.OrangeRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 END", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 5 ? Color.OrangeRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Conjure larger waves of heat.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 6 ? Color.OrangeRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 END", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 7 ? Color.OrangeRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Set self on fire at will.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 8 ? Color.OrangeRed : new Color(40, 40, 40));
                                position += spacing;
                                // Leveling up instruction
                                DrawCenteredText(_spriteBatch, "PRESS CTRL + H TO LEVEL UP THIS PATH.", position, BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.B)) // For Path of Body
                            {
                                _spriteBatch.Draw(
                                    ReactionGUIT,
                                    new Rectangle(
                                        (2560 - newWidth) / 2, // Centered X
                                        (1440 - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                int position = startingPosition;

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF BODY", position, BabyShibafont, Color.SandyBrown);
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 STR", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 1 ? Color.SandyBrown : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Increases to all physical stats.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 2 ? Color.SandyBrown : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 STR", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 3 ? Color.SandyBrown : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Greatly increased unarmed melee capabilities.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 4 ? Color.SandyBrown : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 STR", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 5 ? Color.SandyBrown : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Unarmed strikes channel radiant energy.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 6 ? Color.SandyBrown : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 STR", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 7 ? Color.SandyBrown : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Move your body in any physically imaginable way.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 8 ? Color.SandyBrown : new Color(40, 40, 40));
                                position += spacing;
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD B FOR PATH DETAILS. CTRL+B TO LEVEL.", position, BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.G)) // For Path of Light
                            {
                                _spriteBatch.Draw(
                                    ReactionGUIT,
                                    new Rectangle(
                                        (2560 - newWidth) / 2, // Centered X
                                        (1440 - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                int position = startingPosition;

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF LIGHT", position, BabyShibafont, Color.Yellow);
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 AGL", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 1 ? Color.Yellow : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Conjure photons to create a spark.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 2 ? Color.Yellow : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 AGL", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 3 ? Color.Yellow : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Sparks fire radiant beams at enemies.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 4 ? Color.Yellow : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 AGL", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 5 ? Color.Yellow : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Use sparks to heal nearby creatures.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 6 ? Color.Yellow : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 AGL", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 7 ? Color.Yellow : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Create a Photonexus, loyal to you.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 8 ? Color.Yellow : new Color(40, 40, 40));
                                position += spacing;
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD G FOR PATH DETAILS. CTRL+G TO LEVEL.", position, BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.R)) // For Path of Reality
                            {
                                _spriteBatch.Draw(
                                    ReactionGUIT,
                                    new Rectangle(
                                        (2560 - newWidth) / 2, // Centered X
                                        (1440 - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                int position = startingPosition;

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF REALITY", position, BabyShibafont, Color.IndianRed);
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 DEX", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 1 ? Color.IndianRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Alter object properties.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 2 ? Color.IndianRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 DEX", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 3 ? Color.IndianRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Change state of matter by touch.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 4 ? Color.IndianRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 DEX", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 5 ? Color.IndianRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Duplicate objects.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 6 ? Color.IndianRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 DEX", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 7 ? Color.IndianRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Remove objects from reality.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 8 ? Color.IndianRed : new Color(40, 40, 40));
                                position += spacing;
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD R FOR PATH DETAILS. CTRL+R TO LEVEL.", position, BabyShibafont, Color.White);
                            }

                            /*

                            else if (Keyboard.GetState().IsKeyDown(Keys.I)) // For Path of Illusions
                            {
                                _spriteBatch.Draw(
                                    ReactionGUIT,
                                    new Rectangle(
                                        (2560 - newWidth) / 2, // Centered X
                                        (1440 - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                int position = startingPosition;

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF ILLUSIONS", position, BabyShibafont, Color.Magenta);
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 CHA", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfIllusionsLevel >= 1 ? Color.Magenta : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Summon an incorporeal immobile duplicate of yourself or an object.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfIllusionsLevel >= 2 ? Color.Magenta : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 CHA", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfIllusionsLevel >= 3 ? Color.Magenta : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Summon a duplicate of an animate object. Your duplicates move on their own", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfIllusionsLevel >= 4 ? Color.Magenta : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 CHA", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfIllusionsLevel >= 5 ? Color.Magenta : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Control all clones you create.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfIllusionsLevel >= 6 ? Color.Magenta : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 CHA", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfIllusionsLevel >= 7 ? Color.Magenta : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Switch places with a duplicate of yourself at will.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfIllusionsLevel >= 8 ? Color.Magenta : new Color(40, 40, 40));
                                position += spacing;
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD I FOR PATH DETAILS. CTRL+I TO LEVEL.", position, BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.T))
                            {
                                _spriteBatch.Draw(
                                    ReactionGUIT,
                                    new Rectangle(
                                        (2560 - newWidth) / 2, // Centered X
                                        (1440 - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                int position = startingPosition;

                                // Title
                                DrawCenteredText(_spriteBatch, "PATH OF TIME", position, BabyShibafont, Color.SkyBlue);
                                position += spacing;
                                // Abilities
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 FOC", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfTimeLevel >= 1 ? Color.SkyBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Gain some control over your timeline.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfTimeLevel >= 2 ? Color.SkyBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 FOC", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfTimeLevel >= 3 ? Color.SkyBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Reverse a cycle and its events once per day.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfTimeLevel >= 4 ? Color.SkyBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 FOC", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfTimeLevel >= 5 ? Color.SkyBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Accelerate your timeline briefly.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfTimeLevel >= 6 ? Color.SkyBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 FOC", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfTimeLevel >= 7 ? Color.SkyBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Freeze everyone’s timeline but your own.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfTimeLevel >= 8 ? Color.SkyBlue : new Color(40, 40, 40));
                                position += spacing;
                                // Leveling up instruction
                                DrawCenteredText(_spriteBatch, "PRESS CTRL + T TO LEVEL UP THIS PATH.", position, BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.E)) // For Path of Ethereality
                            {
                                _spriteBatch.Draw(
                                    ReactionGUIT,
                                    new Rectangle(
                                        (2560 - newWidth) / 2, // Centered X
                                        (1440 - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                int position = startingPosition;

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF ETHEREALITY", position, BabyShibafont, Color.LightBlue);
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 DEX", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfEtherealityLevel >= 1 ? Color.LightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Take less damage, generally.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfEtherealityLevel >= 2 ? Color.LightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 DEX", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfEtherealityLevel >= 3 ? Color.LightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Enter the ethereal plane briefly.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfEtherealityLevel >= 4 ? Color.LightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 DEX", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfEtherealityLevel >= 5 ? Color.LightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Send objects to the ethereal plane.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfEtherealityLevel >= 6 ? Color.LightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 DEX", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfEtherealityLevel >= 7 ? Color.LightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Instantaneous travel anywhere.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfEtherealityLevel >= 8 ? Color.LightBlue : new Color(40, 40, 40));
                                position += spacing;
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD E FOR PATH DETAILS. CTRL+E TO LEVEL.", position, BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.V)) // For Path of Void
                            {
                                _spriteBatch.Draw(
                                    ReactionGUIT,
                                    new Rectangle(
                                        (2560 - newWidth) / 2, // Centered X
                                        (1440 - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                int position = startingPosition;

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF VOID", position, BabyShibafont, Color.DarkSlateBlue);
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 CRE", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfVoidLevel >= 1 ? Color.DarkSlateBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Create voids that you can store items for later usage.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfVoidLevel >= 2 ? Color.DarkSlateBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 CRE", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfVoidLevel >= 3 ? Color.DarkSlateBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Fire matter projectiles from voids.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfVoidLevel >= 4 ? Color.DarkSlateBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 CRE", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfVoidLevel >= 5 ? Color.DarkSlateBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Compel creatures into voids.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfVoidLevel >= 6 ? Color.DarkSlateBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 CRE", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfVoidLevel >= 7 ? Color.DarkSlateBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Voids last forever and are interconnected.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfVoidLevel >= 8 ? Color.DarkSlateBlue : new Color(40, 40, 40));
                                position += spacing;
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD V FOR PATH DETAILS. CTRL+V TO LEVEL.", position, BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.S)) // For Path of Storms
                            {
                                _spriteBatch.Draw(
                                    ReactionGUIT,
                                    new Rectangle(
                                        (2560 - newWidth) / 2, // Centered X
                                        (1440 - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                int position = startingPosition;

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF STORMS", position, BabyShibafont, Color.Cyan);
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 STR", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStormsLevel >= 1 ? Color.Cyan : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Energy strike on foes.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStormsLevel >= 2 ? Color.Cyan : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 STR", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStormsLevel >= 3 ? Color.Cyan : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Flow with uncontrollable energy.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStormsLevel >= 4 ? Color.Cyan : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 STR", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStormsLevel >= 5 ? Color.Cyan : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Direct energy into objects.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStormsLevel >= 6 ? Color.Cyan : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 STR", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStormsLevel >= 7 ? Color.Cyan : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Gain flight and powerful energy manipulation.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStormsLevel >= 8 ? Color.Cyan : new Color(40, 40, 40));
                                position += spacing;
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD S FOR PATH DETAILS. CTRL+S TO LEVEL.", position, BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.F)) // For Path of Forge
                            {
                                _spriteBatch.Draw(
                                    ReactionGUIT,
                                    new Rectangle(
                                        (2560 - newWidth) / 2, // Centered X
                                        (1440 - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                int position = startingPosition;

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF FORGE", position, BabyShibafont, Color.DarkOrange);
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 DEX", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfForgeLevel >= 1 ? Color.DarkOrange : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Craft any weapon at a forge with the right materials.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfForgeLevel >= 2 ? Color.DarkOrange : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 DEX", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfForgeLevel >= 3 ? Color.DarkOrange : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Weapons you make have an extra imbuement.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfForgeLevel >= 4 ? Color.DarkOrange : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 DEX", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfForgeLevel >= 5 ? Color.DarkOrange : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Your imbuements have more effectiveness.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfForgeLevel >= 6 ? Color.DarkOrange : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 DEX", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfForgeLevel >= 7 ? Color.DarkOrange : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Touch objects to give them three extra imbuements.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfForgeLevel >= 8 ? Color.DarkOrange : new Color(40, 40, 40));
                                position += spacing;
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD F FOR PATH DETAILS. CTRL+F TO LEVEL.", position, BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.K)) // For Path of Lore
                            {
                                _spriteBatch.Draw(
                                    ReactionGUIT,
                                    new Rectangle(
                                        (2560 - newWidth) / 2, // Centered X
                                        (1440 - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                int position = startingPosition;

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF LORE", position, BabyShibafont, Color.SeaGreen);
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 CRE", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLoreLevel >= 1 ? Color.SeaGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Access lore from other lorepathers, containing secrets.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLoreLevel >= 2 ? Color.SeaGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 CRE", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLoreLevel >= 3 ? Color.SeaGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Trade knowledge with people mentally.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLoreLevel >= 4 ? Color.SeaGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 CRE", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLoreLevel >= 5 ? Color.SeaGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Increase max path level by 4.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLoreLevel >= 6 ? Color.SeaGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 CRE", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLoreLevel >= 7 ? Color.SeaGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Access a wellspring of history at will.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLoreLevel >= 8 ? Color.SeaGreen : new Color(40, 40, 40));
                                position += spacing;
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD K FOR PATH DETAILS. CTRL+K TO LEVEL.", position, BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.M)) // For Path of Mind
                            {
                                _spriteBatch.Draw(
                                    ReactionGUIT,
                                    new Rectangle(
                                        (2560 - newWidth) / 2, // Centered X
                                        (1440 - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                int position = startingPosition;

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF MIND", position, BabyShibafont, Color.LightSeaGreen);
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 FOC", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfMindLevel >= 1 ? Color.LightSeaGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Enhanced magical power.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfMindLevel >= 2 ? Color.LightSeaGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 FOC", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfMindLevel >= 3 ? Color.LightSeaGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Decreased magical energy usage.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfMindLevel >= 4 ? Color.LightSeaGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 FOC", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfMindLevel >= 5 ? Color.LightSeaGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Option to double spell effects.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfMindLevel >= 6 ? Color.LightSeaGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 FOC", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfMindLevel >= 7 ? Color.LightSeaGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Triple Spell effects and reduced energy usage.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfMindLevel >= 8 ? Color.LightSeaGreen : new Color(40, 40, 40));
                                position += spacing;
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD M FOR PATH DETAILS. CTRL+M TO LEVEL.", position, BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.U)) // For Path of Soul
                            {
                                _spriteBatch.Draw(
                                    ReactionGUIT,
                                    new Rectangle(
                                        (2560 - newWidth) / 2, // Centered X
                                        (1440 - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                int position = startingPosition;

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF SOUL", position, BabyShibafont, Color.MediumPurple);
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 END", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfSoulLevel >= 1 ? Color.MediumPurple : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Increases to all nonphysical stats.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfSoulLevel >= 2 ? Color.MediumPurple : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 END", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfSoulLevel >= 3 ? Color.MediumPurple : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Greatly increased energy generation.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfSoulLevel >= 4 ? Color.MediumPurple : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 END", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfSoulLevel >= 5 ? Color.MediumPurple : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Exit your body, moving through walls.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfSoulLevel >= 6 ? Color.MediumPurple : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 END", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfSoulLevel >= 7 ? Color.MediumPurple : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Possess a new vessel if you die.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfSoulLevel >= 8 ? Color.MediumPurple : new Color(40, 40, 40));
                                position += spacing;
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD U FOR PATH DETAILS. CTRL+U TO LEVEL.", position, BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.P)) // For Path of Space
                            {
                                _spriteBatch.Draw(
                                    ReactionGUIT,
                                    new Rectangle(
                                        (2560 - newWidth) / 2, // Centered X
                                        (1440 - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                int position = startingPosition;

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF SPACE", position, BabyShibafont, Color.DarkOrchid);
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 STR", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfSpaceLevel >= 1 ? Color.DarkOrchid : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Open a portal for travel.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfSpaceLevel >= 2 ? Color.DarkOrchid : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 STR", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfSpaceLevel >= 3 ? Color.DarkOrchid : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Telekinesis for small objects.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfSpaceLevel >= 4 ? Color.DarkOrchid : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 STR", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfSpaceLevel >= 5 ? Color.DarkOrchid : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Telekinesis for heavier objects.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfSpaceLevel >= 6 ? Color.DarkOrchid : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 STR", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfSpaceLevel >= 7 ? Color.DarkOrchid : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Telekinesis without limits, including self for flight.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfSpaceLevel >= 8 ? Color.DarkOrchid : new Color(40, 40, 40));
                                position += spacing;
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD P FOR PATH DETAILS. CTRL+P TO LEVEL.", position, BabyShibafont, Color.White);
                            }
                            */

                        }
                    }

                    //character
                    DrawCharacter(MostRecentPartyTurnArchitect, 500, 1200, 0.2);

                }

                //draw the prompt regardless of anything, unless you are in the help menu

                if (!((Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.LeftControl)) && Keyboard.GetState().IsKeyDown(Keys.OemQuestion)))
                {
                    List<string> ParseInputIntoParts(string command, string typedText)
                    {
                        var commandParts = command.Split(' ');
                        var inputParts = new List<string>();
                        var typedWords = typedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                        int j = 0;
                        for (int i = 0; i < commandParts.Length; i++)
                        {
                            if (commandParts[i] == "~")
                            {
                                string subject = "";
                                while (j < typedWords.Length && (i + 1 >= commandParts.Length || !commandParts[i + 1].StartsWith(typedWords[j], StringComparison.OrdinalIgnoreCase)))
                                {
                                    subject += typedWords[j] + " ";
                                    j++;
                                }
                                if (subject.Length > 0)
                                {
                                    inputParts.Add(subject.Trim());
                                }
                            }
                            else
                            {
                                if (j < typedWords.Length && commandParts[i].StartsWith(typedWords[j], StringComparison.OrdinalIgnoreCase))
                                {
                                    inputParts.Add(typedWords[j]);
                                    j++;
                                }
                            }
                        }
                        return inputParts;
                    }

                    (int matchScore, bool isMatch, bool isPartialMatch) GetMatchScoreAndValidity(string command, List<string> inputParts)
                    {
                        var commandParts = command.Split(' ');
                        int matchScore = 0;
                        int j = 0; // Index for inputParts

                        for (int i = 0; i < commandParts.Length; i++)
                        {
                            if (commandParts[i] == "~")
                            {
                                if (j < inputParts.Count)
                                {
                                    matchScore++; // Wildcard matches, increase score
                                    j++;
                                }
                            }
                            else
                            {
                                if (j >= inputParts.Count)
                                {
                                    return (matchScore, false, true); // If out of input parts, return partial match
                                }
                                if (!commandParts[i].StartsWith(inputParts[j], StringComparison.OrdinalIgnoreCase))
                                {
                                    return (matchScore, false, false); // If it fails to match, return false
                                }
                                j++;
                                matchScore++; // Increase score for exact part match
                            }
                        }

                        // Ensure all input parts were used, implying full command structure was respected
                        if (j < inputParts.Count) return (0, false, false);

                        return (matchScore, true, false);
                    }

                    Color GetPartColor(string part, string inputPart, bool isCommandPart, bool isSubject, bool isFullyTyped)
                    {
                        if (isSubject)
                        {
                            return new Color(0, 255, 0); // Green for subjects
                        }
                        if (isCommandPart && isFullyTyped)
                        {
                            return new Color(255, 0, 255); // Pink for fully typed command parts
                        }
                        if (isCommandPart)
                        {
                            return new Color(75, 75, 75); // Gray for incomplete command parts
                        }
                        return new Color(75, 75, 75); // Gray for others
                    }

                    if (MostRecentPartyTurnArchitect.Prompt.Length > 0)
                    {
                        string initialText = "Enter a command. Press F5 For Info: \"I ";
                        Vector2 sizeOfInitialText = Shibafont.MeasureString(initialText);
                        float StartX = 50 + sizeOfInitialText.X;

                        var matchingCommands = SuggestibleCommands
                            .Select(cmd => new { Command = cmd, MatchData = GetMatchScoreAndValidity(cmd, ParseInputIntoParts(cmd, MostRecentPartyTurnArchitect.Prompt)) })
                            .Where(x => x.MatchData.isMatch || x.MatchData.isPartialMatch)
                            .OrderByDescending(x => x.MatchData.matchScore)
                            .Take(5)
                            .Select(x => x.Command)
                            .ToList();

                        int yOffset = 20;
                        bool isTopLine = true; // Flag to check if it's the topmost line being drawn
                        for (int i = 0; i < matchingCommands.Count; i++)
                        {
                            string displayCommand = matchingCommands[i];
                            var commandParts = displayCommand.Split(' ');
                            var inputParts = ParseInputIntoParts(displayCommand, MostRecentPartyTurnArchitect.Prompt);

                            int l = 0; // Input parts index
                            for (int k = 0; k < commandParts.Length; k++)
                            {
                                string partToDraw = commandParts[k];
                                bool isWildcard = partToDraw == "~";
                                bool isSubject = false;
                                bool isCommandPart = !isWildcard;
                                bool isFullyTyped = l < inputParts.Count && !isWildcard && commandParts[k].Equals(inputParts[l], StringComparison.OrdinalIgnoreCase);

                                if (isWildcard && l < inputParts.Count)
                                {
                                    var subjectParts = inputParts.Skip(l).TakeWhile(ip => !commandParts.Skip(k + 1).Any(cp => cp.StartsWith(ip, StringComparison.OrdinalIgnoreCase))).ToList();
                                    partToDraw = string.Join(" ", subjectParts);
                                    l += subjectParts.Count; // Adjust index by the number of words matched by wildcard
                                    isSubject = true;
                                    isCommandPart = false;
                                }

                                // Determine the color for each part
                                Color partColor = GetPartColor(commandParts[k], l < inputParts.Count ? inputParts[l] : "", isCommandPart, isSubject, isFullyTyped);

                                _spriteBatch.DrawString(Shibafont, partToDraw, new Vector2(StartX, 1200 + (i + 1) * yOffset), partColor);

                                // Only increment input index if it's a non-wildcard
                                if (!isWildcard && l < inputParts.Count && commandParts[k].StartsWith(inputParts[l], StringComparison.OrdinalIgnoreCase))
                                {
                                    l++;
                                }

                                // Adjust X coordinate for next part
                                StartX += Shibafont.MeasureString(partToDraw).X + 5; // Added 5 pixels for spacing between words
                            }

                            // Reset X position for next command and update top line flag
                            StartX = 50 + sizeOfInitialText.X;
                            isTopLine = false; // Only the first line gets magenta
                        }
                    }

                    _spriteBatch.DrawString(Shibafont, "Enter a command. Press F5 For Help: \"I " + MostRecentPartyTurnArchitect.Prompt + "_\"", new Vector2(50, 1200), Color.White);


                    if (MostRecentPartyTurnArchitect.CurrentlyMovingPlace == "none")
                    {
                        _spriteBatch.DrawString(Shibafont, "You are not moving right now.", new Vector2(50, 1150), Color.White);
                    }
                    else if (KeyDirections.ContainsValue(MostRecentPartyTurnArchitect.CurrentlyMovingPlace))
                    {
                        _spriteBatch.DrawString(Shibafont, "You are currently headed to the " + MostRecentPartyTurnArchitect.CurrentlyMovingPlace, new Vector2(50, 1150), Color.White);
                    }
                    else
                    {
                        _spriteBatch.DrawString(Shibafont, "You are not moving right now.", new Vector2(50, 1150), Color.White);
                    }
                }

                //cmd help

                if(Keyboard.GetState().IsKeyDown(Keys.F5))
                {
                    _spriteBatch.Draw(CmdHelpT, new Rectangle(0, 0, 2560, 1440), Color.White);
                }

            }
            else if (GameState == "dead")
            {
                DrawCenteredText(_spriteBatch, "All members of your party have perished. You have lost influence in the world.", 400, Shibafont, Color.White);
                DrawCenteredText(_spriteBatch, "Press SPACE to return to the title screen.", 450, Shibafont, Color.White);

            }
            else if (GameState == "travelmenu")
            {
                _spriteBatch.DrawString(BabyShibafont, "Press TAB to zoom out.", new Vector2(DrawX+500, 10), Color.White);

                _spriteBatch.Draw(GuideT, new Rectangle(0, 0, 192, 192), Color.White);

                string dateTimeString = GameWorld.GetFormattedDateTime();
                Vector2 dateTimeSize = BabyShibafont.MeasureString(dateTimeString);
                Vector2 dateTimePosition = new Vector2(10, 192 + 10); 
                _spriteBatch.DrawString(BabyShibafont, dateTimeString, dateTimePosition, Color.White);
                Color oColor = GameWorld.IsNightTime() ? new Color(100, 100, 100) : Color.Goldenrod;
                Vector2 oPosition = new Vector2(dateTimePosition.X + dateTimeSize.X + 10, dateTimePosition.Y);
                _spriteBatch.DrawString(BabyShibafont, "O", oPosition, oColor);

                if (Keyboard.GetState().IsKeyDown(Keys.Tab))
                {
                    _spriteBatch.DrawString(BabyShibafont, "X: " + MapCursorX.ToString(), new Vector2(DrawX + 500, 40), Color.White);
                    _spriteBatch.DrawString(BabyShibafont, "Z: " + MapCursorZ.ToString(), new Vector2(DrawX + 560, 40), Color.White);

                    DrawWorld();
                }
                else
                {
                    DrawWorldSegment(MapCursorX, MapCursorZ, 20, 20, 8.0f, 8);
                }

                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {
                        if (x == MapCursorX && z == MapCursorZ)
                        {
                            if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation == null || GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Explored == false)
                            {
                                _spriteBatch.DrawString(BabyShibafont, "No Notable Location, " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome, new Vector2(DrawX + 500, DrawY - 30), Color.White);

                                if (!GameWorld.WorldMap[x + z * GameWorld.Width].RegionallyExplored)
                                {
                                    _spriteBatch.DrawString(BabyShibafont, "Press A to search for unknown locations.", new Vector2(DrawX + 500, DrawY), Color.Pink);
                                }
                                else
                                {
                                    _spriteBatch.DrawString(BabyShibafont, "Your intensive search of this area turned up nothing.", new Vector2(DrawX + 500, DrawY - 60), Color.Yellow);
                                }
                            }
                            else
                            {
                                if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation != null && GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Explored == true && ReqExploreLocations.Contains(GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Type))
                                {
                                    _spriteBatch.DrawString(BabyShibafont, "You discovered an ominous " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Type + ".", new Vector2(DrawX + 500, DrawY - 60), Color.Lime);
                                }

                                if (MenacingStructures.Contains(GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.PrimaryRace.Name + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Type))
                                {
                                    _spriteBatch.DrawString(BabyShibafont, "This location emnates an ominous aura.", new Vector2(DrawX + 500, DrawY), Color.OrangeRed);
                                }

                                _spriteBatch.DrawString(BabyShibafont, GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Name + ", " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome + " " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Type + ".", new Vector2(DrawX + 500, DrawY - 30), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Population: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.TruePopulation(), new Vector2(DrawX + 500, DrawY + 30), Color.White);

                                int ArchitectPopulation = 0;
                                int structures = 0;
                                int groups = 0;

                                int DistrictLine = 0;

                                foreach (District d in GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Districts)
                                {
                                    //REDRAW THE DISTRICT NAMES AND IF TYEYRE DONED DO THE SHIBA CARAT

                                    if (DistrictLine == GamePlayerParty.MapCursorDistrict)
                                    {
                                        if (d.Industry.Length > 4)
                                        {
                                            _spriteBatch.DrawString(Shibafont, ">" + d.Name + " (" + d.Industry.Substring(0, 4) + ".)", new Vector2(DrawX + 900, 1100 + DistrictLine * 20), Color.White);
                                        }
                                        else
                                        {
                                            _spriteBatch.DrawString(Shibafont, ">" + d.Name + " (" + d.Industry + ")", new Vector2(DrawX + 900, 1100 + DistrictLine * 20), Color.White);
                                        }
                                    }
                                    else
                                    {
                                        if (d.Industry.Length > 4)
                                        {
                                            _spriteBatch.DrawString(Shibafont, " " + d.Name + " (" + d.Industry.Substring(0, 4) + ".)", new Vector2(DrawX + 900, 1100 + DistrictLine * 20), Color.White);
                                        }
                                        else
                                        {
                                            _spriteBatch.DrawString(Shibafont, " " + d.Name + " (" + d.Industry + ")", new Vector2(DrawX + 900, 1100 + DistrictLine * 20), Color.White);
                                        }
                                    }
                                    DistrictLine++;


                                    foreach (Group g in GameWorld.Groups)
                                    {
                                        if (g.Leader.Location == GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation)
                                        {
                                            groups++;
                                        }
                                    }
                                    for (int DistrictX = 0; DistrictX < 7; DistrictX++)
                                    {
                                        for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                                        {
                                            ArchitectPopulation = d.DistrictMap[DistrictX + DistrictZ * 7].Architects.Count;

                                            foreach (Structure s in d.DistrictMap[DistrictX + DistrictZ * 7].Structures)
                                            {
                                                structures++;
                                            }
                                        }
                                    }
                                }

                                _spriteBatch.DrawString(BabyShibafont, "Total Architects: " + ArchitectPopulation, new Vector2(DrawX + 500, DrawY + 60), Color.White);

                                if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government == null)
                                {
                                    _spriteBatch.DrawString(BabyShibafont, "No Notable Government", new Vector2(DrawX + 500, DrawY + 90), Color.White);
                                }
                                else
                                {
                                    _spriteBatch.DrawString(BabyShibafont, "Government: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government.Name, new Vector2(DrawX + 500, DrawY + 90), Color.White);
                                }

                                _spriteBatch.DrawString(BabyShibafont, "Colonization Desire: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.ColonizationDesire, new Vector2(DrawX + 500, DrawY + 120), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Wealth: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Wealth, new Vector2(DrawX + 500, DrawY + 150), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Is Saving Up To Settle: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.IsSavingUpToSettle.ToString(), new Vector2(DrawX + 500, DrawY + 180), Color.White);

                                _spriteBatch.DrawString(BabyShibafont, "Structures: " + structures, new Vector2(DrawX + 500, DrawY + 210), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Distinct Groups: " + groups, new Vector2(DrawX + 500, DrawY + 240), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Districts: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Districts.Count, new Vector2(DrawX + 500, DrawY + 270), Color.White);
                            }
                        }
                    }
                }

                int Line = 0;

                foreach (TextStorage t in Exposition)
                {
                    _spriteBatch.DrawString(BabyShibafont, t.Data, new Vector2(DrawX + 500, (DrawY + Line * 20) - 400), t.Color);
                    Line++;
                }
            }
            else if (GameState == "triggerrupture")
            {
                DrawWorld();
                _spriteBatch.DrawString(BabyShibafont, "Use RTDGCV and Enter to position your focus.", new Vector2(DrawX + 500, DrawY + 60), Color.White);
            }
            else if (GameState == "gathering")
            {
                if (GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "forest" || GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "lightforest" || GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "taiga")
                {
                    _spriteBatch.DrawString(Shibafont, "[1] Harvest Wood", new Vector2(100, 100), Color.LimeGreen);
                }
                else
                {
                    _spriteBatch.DrawString(Shibafont, "[1] Harvest Wood (invalid biome)", new Vector2(100, 100), Color.Red);
                }

                if (GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "mountain" || GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "snowpeak")
                {
                    _spriteBatch.DrawString(Shibafont, "[2] Harvest Stone", new Vector2(100, 150), Color.LimeGreen);
                }
                else
                {
                    _spriteBatch.DrawString(Shibafont, "[2] Harvest Stone (invalid biome)", new Vector2(100, 150), Color.Red);
                }

                if (GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "snowpeak")
                {
                    _spriteBatch.DrawString(Shibafont, "[3] Harvest Metal", new Vector2(100, 200), Color.LimeGreen);
                }
                else
                {
                    _spriteBatch.DrawString(Shibafont, "[3] Harvest Metal (invalid biome)", new Vector2(100, 200), Color.Red);
                }

                if (GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "desert")
                {
                    _spriteBatch.DrawString(Shibafont, "[4] Harvest Sand", new Vector2(100, 250), Color.LimeGreen);
                }
                else
                {
                    _spriteBatch.DrawString(Shibafont, "[4] Harvest Sand (invalid biome)", new Vector2(100, 250), Color.Red);
                }

                if (GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "plains")
                {
                    _spriteBatch.DrawString(Shibafont, "[5] Harvest Fiber", new Vector2(100, 300), Color.LimeGreen);
                }
                else
                {
                    _spriteBatch.DrawString(Shibafont, "[5] Harvest Fiber (invalid biome)", new Vector2(100, 300), Color.Red);
                }

                if (GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "tundra" || GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "taiga" || GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "mountain" || GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "snowpeak")
                {
                    _spriteBatch.DrawString(Shibafont, "[6] Harvest Ice", new Vector2(100, 350), Color.LimeGreen);
                }
                else
                {
                    _spriteBatch.DrawString(Shibafont, "[6] Harvest Ice (invalid biome)", new Vector2(100, 350), Color.Red);
                }

                _spriteBatch.DrawString(Shibafont, "[X] Leave", new Vector2(100, 700), Color.Orange);

                int MaxLinesExposition = 15;  // Set the maximum number of lines to display

                int LineExposition = 0;

                // Reverse the Exposition list in place and take the first 15 elements
                List<TextStorage> reversedExposition = new List<TextStorage>(Exposition);
                reversedExposition.Reverse();

                foreach (TextStorage t in reversedExposition.Take(MaxLinesExposition))
                {
                    _spriteBatch.DrawString(Shibafont, t.Data, new Vector2(1000, 50 + LineExposition * 25), t.Color);

                    LineExposition++;
                }
            }
            else if (GameState == "exposition")
            {
                int Line = 0;


                foreach (TextStorage t in Exposition)
                {
                    _spriteBatch.DrawString(Shibafont, t.Data, new Vector2(50, 50 + Line * 30), t.Color);

                    Line++;
                }
            }
            else if (GameState == "crafting")
            {
                int sectionIndex = RecipeIndex / 30;
                int startIndex = sectionIndex * 30;
                int endIndex = Math.Min((sectionIndex + 1) * 30, Recipes.Count);

                // Draw keybind instructions depending on the crafting phase
                string instructions = CraftingPhase == "selectrecipe" ?
                    "Use Arrow Keys to navigate recipes. Press Enter to select. ESC to exit." :
                    "Use Arrow Keys to pick materials. Enter to craft. Right to add. ESC to cancel.";
                _spriteBatch.DrawString(Shibafont, instructions, new Vector2(10, 1040), Color.LightGray);

                if (CraftingPhase == "selectrecipe")
                {
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        string prefix = (i == RecipeIndex) ? "(X) " : "( ) ";
                        Color color = (i == RecipeIndex) ? Color.Yellow : Color.White;
                        Vector2 position = new Vector2(10, 30 * (i - startIndex) + 100);
                        _spriteBatch.DrawString(Shibafont, prefix + Recipes[i].Item1, position, color);
                    }
                }
                else if (CraftingPhase == "selectingredients")
                {
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        string prefix = (i == RecipeIndex) ? "(X) " : "( ) ";
                        Color color = (i == RecipeIndex) ? Color.Yellow : Color.White;
                        Vector2 position = new Vector2(10, 30 * (i - startIndex) + 100);
                        _spriteBatch.DrawString(Shibafont, prefix + Recipes[i].Item1, position, color);
                    }

                    Dictionary<string, string> materialToObjectMap = new Dictionary<string, string>
                    {
                        {"cloth", "bolt"}, {"wood", "log"}, {"stone", "stone"},
                        {"metal", "bar"}, {"gemstone", "gemstone"}, {"sand", "pile"},
                        {"fiber", "bunch"}, {"ice", "block"}, {"glass", "sheet"}
                    };

                    var currentRecipeMaterials = Recipes[RecipeIndex].Item2
                        .Distinct()
                        .Select(mat => materialToObjectMap[mat])
                        .ToList();

                    List<Object> relevantItems = MostRecentPartyTurnArchitect.Inventory
                        .Where(obj => currentRecipeMaterials.Contains(obj.Type))
                        .ToList();

                    int inventorySectionIndex = InventoryCraftingIndex / 30;
                    int inventoryStartIndex = inventorySectionIndex * 30;
                    int inventoryEndIndex = Math.Min((inventorySectionIndex + 1) * 30, relevantItems.Count);

                    if (relevantItems.Count == 0)
                    {
                        _spriteBatch.DrawString(Shibafont, "You do not have the required materials for this.", new Vector2(960, 100), Color.Red);
                        _spriteBatch.DrawString(Shibafont, "You can gather resources at empty regions.", new Vector2(960, 150), Color.Red);
                    }
                    else
                    {
                        for (int i = inventoryStartIndex; i < inventoryEndIndex; i++)
                        {
                            string prefix = (i == InventoryCraftingIndex) ? "(X) " : "( ) ";
                            Color color = (i == InventoryCraftingIndex) ? Color.Lime : (IndexesForResources.Contains(i) ? Color.Green : Color.White);
                            Vector2 position = new Vector2(960, 30 * (i - inventoryStartIndex) + 100);
                            _spriteBatch.DrawString(Shibafont, prefix + relevantItems[i].Materials[0].Name + " " + relevantItems[i].Type, position, color);
                        }
                    }
                }
            }



            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                _spriteBatch.Draw(QuitGuiT, new Rectangle(0, 0, 877, 130), Color.White);

                if (GameState != "partyturn")
                {
                    _spriteBatch.DrawString(Shibafont, "Quitting... (" + Math.Round((decimal)((100 - EscapeTicks) / 10)) + ")", new Vector2(10, 40), Color.White);
                    _spriteBatch.DrawString(Shibafont, "Load a location and hold CTRL-S to save your game.", new Vector2(10, 70), Color.White);
                }
                else
                {
                    _spriteBatch.DrawString(Shibafont, "Quitting... (" + Math.Round((decimal)((100 - EscapeTicks) / 10)) + ")", new Vector2(10, 10), Color.White);
                    _spriteBatch.DrawString(Shibafont, "Press/Hold CTRL-S to save your game.", new Vector2(10, 70), Color.White);
                }
            }


            if (FrameCounter.RenderFps)
            {
                FrameCounter.Render(_spriteBatch, Shibafont);
            }


            if (SplitMode)
            {
                int BorderSize = 1;
                int topCutOff = 0; // Cut off a small part of the top

                // Adjust the hitboxes
                for (int i = 0; i < EntityHitboxes.Count; i++)
                {
                    var (rect, entity) = EntityHitboxes[i];
                    rect.Y += topCutOff;
                    rect.Height -= topCutOff;
                    EntityHitboxes[i] = (rect, entity);
                }

                // Check for intersections and adjust
                for (int i = 0; i < EntityHitboxes.Count; i++)
                {
                    for (int j = i + 1; j < EntityHitboxes.Count; j++)
                    {
                        var (rectA, entityA) = EntityHitboxes[i];
                        var (rectB, entityB) = EntityHitboxes[j];

                        if (rectA.Intersects(rectB))
                        {
                            // Calculate shared border
                            Rectangle intersection = Rectangle.Intersect(rectA, rectB);

                            // Adjust rectangles to create a shared border
                            if (intersection.Width > intersection.Height)
                            {
                                // Horizontal overlap
                                int sharedHeight = intersection.Height / 2;
                                rectA.Height -= sharedHeight;
                                rectB.Y += sharedHeight;
                                rectB.Height -= sharedHeight;
                            }
                            else
                            {
                                // Vertical overlap
                                int sharedWidth = intersection.Width / 2;
                                rectA.Width -= sharedWidth;
                                rectB.X += sharedWidth;
                                rectB.Width -= sharedWidth;
                            }

                            EntityHitboxes[i] = (rectA, entityA);
                            EntityHitboxes[j] = (rectB, entityB);
                        }
                    }
                }
                //handle pull up menus and ThisList

                int screenWidth = _graphics.PreferredBackBufferWidth;
                int screenHeight = _graphics.PreferredBackBufferHeight;
                int textureWidth = 420;
                int textureHeight = 560;
                int visibleHeight = 90;
                int offsetY = screenHeight - visibleHeight;

                // Determine positions (on the right side of the screen)
                Vector2 skillPosition = new Vector2(screenWidth - 10 - textureWidth * 3 - 20, offsetY);
                Vector2 spellPosition = new Vector2(screenWidth - 10 - textureWidth * 2 - 10, offsetY);
                Vector2 bodyPartPosition = new Vector2(screenWidth - 10 - textureWidth, offsetY);

                // Handle mouse hover and show menus
                MouseState mouseState = Mouse.GetState();
                Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);

                // Check for hover over small textures
                Rectangle skillRect = new Rectangle(skillPosition.ToPoint(), new Point(textureWidth, visibleHeight));
                Rectangle spellRect = new Rectangle(spellPosition.ToPoint(), new Point(textureWidth, visibleHeight));
                Rectangle bodyPartRect = new Rectangle(bodyPartPosition.ToPoint(), new Point(textureWidth, visibleHeight));

                // Adjust positions if showing
                if (IsShowingSkills)
                {
                    skillPosition.Y = screenHeight - textureHeight;
                    skillRect = new Rectangle(skillPosition.ToPoint(), new Point(textureWidth, textureHeight));
                }
                if (IsShowingSpells)
                {
                    spellPosition.Y = screenHeight - textureHeight;
                    spellRect = new Rectangle(spellPosition.ToPoint(), new Point(textureWidth, textureHeight));
                }
                if (IsShowingBodyParts)
                {
                    bodyPartPosition.Y = screenHeight - textureHeight;
                    bodyPartRect = new Rectangle(bodyPartPosition.ToPoint(), new Point(textureWidth, textureHeight));
                }

                // Update showing states and adjust positions
                if (skillRect.Contains(mousePosition))
                {
                    IsShowingSkills = true;
                }
                else if (!skillRect.Contains(mousePosition))
                {
                    IsShowingSkills = false;
                    skillPosition.Y = offsetY;
                }

                if (spellRect.Contains(mousePosition))
                {
                    IsShowingSpells = true;
                }
                else if (!spellRect.Contains(mousePosition))
                {
                    IsShowingSpells = false;
                    spellPosition.Y = offsetY;
                }

                if (bodyPartRect.Contains(mousePosition))
                {
                    IsShowingBodyParts = true;
                }
                else if (!bodyPartRect.Contains(mousePosition))
                {
                    IsShowingBodyParts = false;
                    bodyPartPosition.Y = offsetY;
                }

                // Draw the textures
                _spriteBatch.Draw(SkillPullUpT, skillPosition, IsShowingSkills ? Color.White : Color.Gray);
                _spriteBatch.Draw(SpellPullUpT, spellPosition, IsShowingSpells ? Color.White : Color.Gray);
                _spriteBatch.Draw(BodyPartPullUpT, bodyPartPosition, IsShowingBodyParts ? Color.White : Color.Gray);

                // Draw text and hitboxes for each menu when fully shown
                if (IsShowingSkills)
                {
                    DrawTextInMenu(skillPosition, MostRecentPartyTurnArchitect.SkillsKnown, "Skills");
                }
                if (IsShowingSpells)
                {
                    DrawTextInMenu(spellPosition, MostRecentPartyTurnArchitect.SpellsKnown, "Spells");
                }
                if (IsShowingBodyParts)
                {
                    var bodyParts = GetUniqueBodyParts(MostRecentPartyTurnArchitect.Room != null ? MostRecentPartyTurnArchitect.Room.Architects : MostRecentPartyTurnArchitect.Block.Architects);
                    DrawTextInMenu(bodyPartPosition, bodyParts, "Body Parts");
                }


                void DrawTextInMenu(Vector2 position, List<string> items, string itemType)
                {
                    float startY = position.Y + 100;
                    float offsetX = position.X + 50;
                    float centerX = position.X + textureWidth / 2;

                    if (items.Count == 0)
                    {
                        _spriteBatch.DrawString(BabyShibafont, $"No Relevant {itemType}.", new Vector2(offsetX, startY), Color.White);
                        return;
                    }

                    int line = 0;
                    foreach (var item in items)
                    {
                        string text = item;
                        float textY = startY + line * BabyShibafont.LineSpacing;

                        // Draw the text aligned to the left side of the hitbox
                        _spriteBatch.DrawString(BabyShibafont, text, new Vector2(offsetX, textY), Color.White);

                        // Create a Rectangle for the hitbox
                        Vector2 textSize = BabyShibafont.MeasureString(text);
                        Rectangle hitbox = new Rectangle((int)offsetX, (int)textY, (int)textSize.X, (int)textSize.Y);
                        EntityHitboxes.Add((hitbox, new Entity(text)));

                        // Move to the second row if the text would draw offscreen
                        if (textY + textSize.Y > position.Y + textureHeight - 100)
                        {
                            offsetX = centerX;
                            line = 0;
                            startY = position.Y + 100;
                        }
                        else
                        {
                            line++;
                        }
                    }
                }

                List<string> GetUniqueBodyParts(IEnumerable<Architect> architects)
                {
                    var bodyPartTypes = new HashSet<string>();
                    foreach (var architect in architects)
                    {
                        foreach (var bodyPart in architect.BodyParts)
                        {
                            bodyPartTypes.Add(bodyPart.Type);
                        }
                    }
                    return bodyPartTypes.ToList();
                }




                // Draw the hitbox borders
                foreach ((Rectangle rect, Entity entity) in EntityHitboxes)
                {
                    // Calculate the scaled border size
                    int scaledBorderSizeX = (int)(BorderSize * scaleX);
                    int scaledBorderSizeY = (int)(BorderSize * scaleY);

                    // Top Border
                    _spriteBatch.Draw(whiteRect, new Rectangle(rect.X, rect.Y, rect.Width, scaledBorderSizeY), Color.White);

                    // Bottom Border
                    _spriteBatch.Draw(whiteRect, new Rectangle(rect.X, rect.Y + rect.Height - scaledBorderSizeY, rect.Width, scaledBorderSizeY), Color.White);

                    // Left Border
                    _spriteBatch.Draw(whiteRect, new Rectangle(rect.X, rect.Y, scaledBorderSizeX, rect.Height), Color.White);

                    // Right Border
                    _spriteBatch.Draw(whiteRect, new Rectangle(rect.X + rect.Width - scaledBorderSizeX, rect.Y, scaledBorderSizeX, rect.Height), Color.White);
                }

                if (ThisList.Count > 0)
                {
                    int mouseX = Mouse.GetState().X;

                    // Determine the position to draw ThisListT
                    Vector2 texturePosition;
                    if (mouseX < screenWidth / 2)
                    {
                        // Mouse is on the left side of the screen, draw ThisListT on the right
                        texturePosition = new Vector2(screenWidth - 438 - 10, 10); // With 10 pixels of leeway
                    }
                    else
                    {
                        // Mouse is on the right side of the screen, draw ThisListT on the left
                        texturePosition = new Vector2(10, 10); // With 10 pixels of leeway
                    }

                    // Draw ThisListT texture
                    _spriteBatch.Draw(ThisListT, texturePosition, Color.White);

                    // Start drawing referred-to names 100 pixels down from the top of ThisListT
                    float textStartY = texturePosition.Y + 150;
                    float textStartX = texturePosition.X + (438 / 2); // Center of the texture width

                    foreach (var entity in ThisList)
                    {
                        if (entity.ReferredToNames != null && entity.ReferredToNames.Count > 0)
                        {
                            string text = entity.ReferredToNames[0];
                            DrawCenteredTextAtPosition(_spriteBatch, text, textStartX, textStartY, BabyShibafont);
                            textStartY += BabyShibafont.LineSpacing; // Move down for the next text
                        }
                    }
                }

            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}