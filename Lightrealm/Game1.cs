using Lightrealm.Diagnostics;
using Lightrealm.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Newtonsoft.Json.Linq;
using PortAudioSharp;
using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Vosk;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Model = Vosk.Model;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

#pragma warning disable SYSLIB0011

namespace Lightrealm
{
    public class Game1 : Game
    {
        public static string Version = "alpha2";

        public static T GetRandomItem<T>(EntityHashSet<T> set) where T : Entity
        {
            if (set == null || set.Count == 0)
            {
                throw new InvalidOperationException("Cannot select a random item from an empty or null set.");
            }

            int index = r.Next(set.Count);
            var entity = set.ElementAt(index);
            return entity;
        }

        public static void Shuffle<T>(IList<T> list)
        {
            int n = list.Count();
            while (n > 1)
            {
                n--;
                int k = r.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static List<T> ShuffleNew<T>(IList<T> list)
        {
            List<T> shuffledList = new List<T>(list);
            int n = shuffledList.Count();
            while (n > 1)
            {
                n--;
                int k = r.Next(n + 1);
                T value = shuffledList[k];
                shuffledList[k] = shuffledList[n];
                shuffledList[n] = value;
            }
            return shuffledList;
        }

        public static void ShuffleEL<T>(EntityList<T> list) where T : Entity
        {
            int n = list.Count;
            var random = new Random();
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static EntityList<T> ShuffleNewEL<T>(EntityList<T> list) where T : Entity
        {
            var shuffledList = new EntityList<T>(list);
            int n = shuffledList.Count;
            var random = new Random();
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                var value = shuffledList[k];
                shuffledList[k] = shuffledList[n];
                shuffledList[n] = value;
            }
            return shuffledList;
        }


        public static List<Vector2> crds = new List<Vector2>();

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
                return verbs[Game1.r.Next(verbs.Count())]; // Randomly select a verb
            }

            return "attack"; // Default verb if weapon type is not found
        }

        public static List<(Rectangle, Entity)> EntityHitboxes = new List<(Rectangle, Entity)>();

        public static Rectangle RectangleN;
        public static Rectangle RectangleNE;
        public static Rectangle RectangleE;
        public static Rectangle RectangleSE;
        public static Rectangle RectangleS;
        public static Rectangle RectangleSW;
        public static Rectangle RectangleW;
        public static Rectangle RectangleNW;
        public static Rectangle RectangleCenter;

        public static Dictionary<int, Entity> TemporaryEntityLedger = new Dictionary<int, Entity>();
        public static int TemporaryNextUniqueID = 100;

        public bool MassTravelOrderMode = false;

        public List<string> CurrentlyViewingHistory = new List<string>();
        public int HistoricalScrollValue = 0;

        private static bool _isRecording = false;
        private static PortAudioSharp.Stream _stream;
        private static VoskRecognizer _recognizer;
        public static int DeviceNumber;

        public static List<string> MenacingStructures = new List<string>() { "fortress", "monument", "shadecore", "photonexusoutpost", "sanctum", "spire", "stronghold", "tower", "shadegarrison", "photonexuscore", "photonexusgarrison"};

        public MouseState previousMouseState;
        public MouseState currentMouseState;

        public static List<string> OffensiveTasks = new List<string>() { "disabletarget", "killtarget", "sentinel" };


        public Model VoskModel;


        public int CurrentlySelectingRegionIndexOr100 = 100;
        public int MaxRegionIndex = 0;
        public bool ShowSignificant = false;


        public int AscendantCursor = 0;
        public int AscendantPage = 0;
        public Party CurrentlySelectedAscendantParty = null;
        public Architect CurrentlySelectedAscendantArchitect = null;
        public District CurrentlySelectedAscendantDistrict = null;
        public string AscendantActingMode = "";
        public static string AscendantState = "";
        public static string AscendantAction = "";
        public EntityList<Entity> AscendantEntityLedger = new EntityList<Entity>();
        public Entity SelectedAscendantTarget;


        public bool SpeechToText = true;
        public bool GoToLoadingGameAfterLibrariesAreInstalled = false;

        public static List<string> ReqExploreLocations = new List<string>() { "fortress", "monument", "tower", "stronghold" };

        public static List<Entity> ThisList = new List<Entity>();

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public static bool ViewMessageForCustom = false;

        public static List<Material> MaterialsToAddToWorld = new List<Material>();

        public int LatestTraveledDay { get; set; }

        public static List<Entity> CollectAllSubjects(Architect executor, string Modifier)
        {
            List<Entity> subjects = new List<Entity>();

            //divided into constants and into not-constants

            // Nonalive entities around

            foreach (Block b in executor.District.DistrictMap)
            {
                subjects.AddRange(b.Structures);
                subjects.AddRange(b.Objects);

                foreach (Structure s in b.Structures)
                {
                    foreach (Room r in s.Rooms)
                    {
                        subjects.AddRange(r.Objects);
                    }
                }
            }

            //loaded architects

            subjects.AddRange(LoadedArchitects);


            // Add the items in the inventories of people who actually matter
            List<Entity> subjectsToAdd = new List<Entity>();

            foreach (Entity e in subjects)
            {
                if (e is Architect architect &&
        (architect == executor ||
        (architect.Block != null && executor.Block != null && architect.Block == executor.Block && executor.Block.Architects.Contains(architect)) ||
        (executor.Room != null && architect.Room != null && architect.Room == executor.Room)))
                {
                    if (Modifier != "get")
                    {
                        subjectsToAdd.AddRange(architect.Inventory);
                        subjectsToAdd.AddRange(architect.Clothing);
                        subjectsToAdd.AddRange(architect.BodyParts);

                        if (architect.OffHeldObject != null)
                        {
                            subjectsToAdd.Add(architect.OffHeldObject);
                        }
                        if (architect.MainHeldObject != null)
                        {
                            subjectsToAdd.Add(architect.MainHeldObject);
                        }
                    }

                    subjectsToAdd.AddRange(architect.CultureBank);

                    if (GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(architect))
                    {
                        subjectsToAdd.AddRange(architect.ShadowStorage);
                    }
                }
            }

            subjects.AddRange(subjectsToAdd);

            // Entities not around
            foreach (Architect a in GameWorld.AllArchitects)
            {
                if (!subjects.Contains(a))
                {
                    subjects.Add(a);
                }
            }

            foreach (Location l in GameWorld.AllLocations)
            {
                if (!subjects.Contains(l))
                {
                    subjects.Add(l);
                    subjects.AddRange(l.Districts);
                    subjects.AddRange(l.AllStructures);
                }
            }

            foreach (var skillValuePair in executor.XPValues)
            {
                subjects.Add(new Entity(skillValuePair.Item1.ToLower()));
            }

            // Spells known to the architect
            foreach (var spell in GameWorld.AllSpells)
            {
                if (!subjects.Contains(spell))
                {
                    subjects.Add(spell);
                }
            }

            foreach (var skill in GameWorld.AllSkills)
            {
                if (!subjects.Contains(skill))
                {
                    subjects.Add(skill);
                }
            }

            foreach (var spell in GameWorld.AllLegendarySpells)
            {
                if (!subjects.Contains(spell))
                {
                    subjects.Add(spell);
                }
            }

            // Add types of body parts
            var uniqueBodyPartNames = new HashSet<string>();

            foreach (Race r in Game1.GameWorld.Races)
            {
                foreach (string part in r.BodyPartNames)
                {
                    if (uniqueBodyPartNames.Add(part)) // Adds part and returns true if it was not already in the set
                    {
                        subjects.Add(new Entity(part));
                    }
                }
            }


            var extraEntities = GameWorld.ExtraEntities.Except(GameWorld.Domains);
            subjects.AddRange(extraEntities);

            foreach (var domain in GameWorld.Domains)
            {
                if (!subjects.Contains(domain))
                {
                    subjects.Add(domain);
                }
            }

            subjects.AddRange(GameWorld.Blights);
            subjects.Add(GameWorld.DarkDeity);
            subjects.Add(GameWorld.LightDeity);
            subjects.AddRange(GameWorld.AllBodyParts);
            subjects.AddRange(GameWorld.Groups);
            subjects.AddRange(GameWorld.CoreMaterials());
            subjects.AddRange(Game1.GameWorld.Races);
            subjects.Add(GameWorld);

            // Collect the objects hiding inside containers
            List<Entity> allObjects = new List<Entity>();

            // Method to recursively retrieve all objects
            void RetrieveAllObjects(Object obj)
            {
                if (!allObjects.Contains(obj))
                {
                    allObjects.Add(obj);
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

            subjects.AddRange(allObjects);

            MostRecentPartyTurnArchitect.AddReferredToName("myself");
            MostRecentPartyTurnArchitect.AddReferredToName("me");

            return subjects;
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

        public static int CurrentlySelectedWorldAge = 200; //100, 150, 200 (recommended), 250, 300, 350, 400, 450, 500, Until Stopped
        public static int CurrentlySelectedGrievanceType = 0;
        public static int NumberOfCivilizations = 16; //maximum is 16, minimum is 6. Subtract 4 before calculation.
        public static double ProsperityMultiplier = 1; //determines wealth increase, aka general flourishing
        public static int CurrentlySelectedWorldWidth = 128; //max 128
        public static int CurrentlySelectedWorldLength = 128; //max 128

        public static Architect MostRecentPartyTurnArchitect = null;

        public static string GrievanceReason = "";
        public static string GrievanceDoer = "";

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

        public static void MakeObservation(string data, Color color, EntityList<Entity> Entities)
        {
            string capitalizedData = Capitalize(data);
            Observations.Add(new TextStorage(capitalizedData, color, Entities));
            Announcements.Add(new TextStorage(capitalizedData, color, Entities));
        }

        public static void AddMessage(string data, Color color, EntityList<Entity> Entities)
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
            if(Word.Length > 0)
            {
                return Word[0].ToString().ToUpper() + Word.Substring(1);
            }
            else
            {
                return "";
            }
        }

        int CurrentlyAssigningSkill = 7;

        public static List<string> AnimalSizes = new List<string>() { "miniscule", "smaller", "small", "medium", "humanoid", "large", "huge" };
        public static List<string> AllSizes = new List<string>() { "ethereal", "miniscule", "smaller", "small", "medium", "humanoid", "large", "huge", "colossal", "archancient" };

        public bool HasPlayerBeenAttacked(Architect architect)
        {
            if (architect.BodyParts.Contains(StoredAttack.Target))
            {
                return true;
            }
            return false;
        }

        public static string ConvertListToString(List<string> items)
        {
            StringBuilder result = new StringBuilder();

            if (items.Count() == 0)
            {
                return "empty list";
            }
            else if (items.Count() == 1)
            {
                return items[0];
            }
            else
            {
                for (int i = 0; i < items.Count() - 1; i++)
                {
                    result.Append(items[i]);
                    result.Append(", ");
                }

                result.Append("and ");
                result.Append(items[items.Count() - 1]);

                return result.ToString();
            }
        }

        public static Attack StoredAttack = null;
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
            string[] weaponTypes = { "shortsword", "greatsword", "battle axe", "greataxe", "rapier", "spear", "pike", "mace", "hammer", "whip", "scourge" };

            // Randomly select a weapon type
            Random rand = new Random();
            string selectedWeapon = weaponTypes[rand.Next(weaponTypes.Length)];

            // Randomly select a metal Material from the world's Metals list

            // Create and return the new weapon Object
            Object o = new Object(null, selectedWeapon, new EntityList<Material>() { material }, null);
            o.Rarity = Rarity;
            o.ApplyImbuements(0);
            return o;
        }

        public static string ConvertObjectToString(Object obj)
        {
            string materials = string.Join(",", obj.Materials.Select(m => m.Name));
            string containedItems = string.Join(",", obj.ContainedObjects.Select(o => ConvertObjectToString(o)));
            string containedPart = containedItems.Length > 0 ? $"&cont({containedItems})&" : "";

            return $"{obj.Type},{1},{materials}{containedPart}";
        }

        public static EntityList<Object> ConvertStringToObjects(string itemString)
        {
            EntityList<Object> objects = new EntityList<Object>();

            string[] parts = itemString.Split(new[] { ',' }, 3);
            string type = parts[0];
            int count = int.Parse(parts[1]);
            string[] materialsAndContained = parts[2].Split(new[] { "&cont(", ")" }, StringSplitOptions.None);

            // Initialize the materials list
            EntityList<Material> materials = new EntityList<Material>();

            // Split the materials string and convert each one
            string[] materialStrings = materialsAndContained[0].Split(',');

            foreach (string materialName in materialStrings)
            {
                string trimmedName = materialName.Trim();
                if (Game1.GameWorld.Materials.TryGetValue(trimmedName, out Material material))
                {
                    if(!materials.Contains(material))
                    {
                        materials.Add(material);
                    }
                }
            }

            // Create the objects based on the count
            for (int i = 0; i < count; i++)
            {
                Object obj = new Object(null, type, materials, null);

                // If there are contained items, process them recursively
                if (materialsAndContained.Length > 1 && !string.IsNullOrWhiteSpace(materialsAndContained[1]))
                {
                    string containedString = materialsAndContained[1].Trim();
                    if (!string.IsNullOrEmpty(containedString))
                    {
                        string[] containedItems = containedString.Split(new[] { ")," }, StringSplitOptions.None);
                        foreach (var containedItem in containedItems)
                        {
                            string cleanedItem = containedItem.Trim('(', ')', ' ');
                            if (!string.IsNullOrEmpty(cleanedItem))
                            {
                                EntityList<Object> containedObjects = ConvertStringToObjects(cleanedItem);
                                obj.ContainedObjects.AddRange(containedObjects);
                            }
                        }
                    }
                }

                objects.Add(obj);
            }

            return objects;
        }


        public static int CalculateProximityScore(Entity entity, Architect player)
        {
            if (player.CultureBank.Contains(entity))
            {
                return 0;
            }
            // Check if the entity is in the player's hands
            if (player.OffHeldObject == entity || player.MainHeldObject == entity)
            {
                return 1;
            }

            // Check if the entity is in the player's shadow storage
            if (player.ShadowStorage.Contains(entity))
            {
                return 2;
            }

            // Check if the entity is in the player's clothing
            if (player.Clothing.Contains(entity))
            {
                return 3;
            }

            // Check if the entity is in the player's inventory
            if (player.Inventory.Contains(entity))
            {
                return 4;
            }

            // Check if the entity is in the same room as the player
            Room currentRoom = player.Room;
            if (currentRoom != null)
            {
                if (entity is Architect architect && currentRoom.Architects.Contains(architect))
                {
                    return 5;
                }
                else if (entity is Object obj && currentRoom.Objects.Contains(obj))
                {
                    return 5;
                }
            }

            // Check if the entity is in the same block as the player
            Block currentBlock = player.Block;
            if (currentBlock != null)
            {
                if (currentBlock.Architects.Contains(entity) || currentBlock.Objects.Contains(entity) || currentBlock.Structures.Contains(entity)) 
                {
                    return 6;
                }

                foreach (Structure structure in currentBlock.Structures)
                {
                    foreach (Room room in structure.Rooms)
                    {
                        if (room.Objects.Contains(entity) || room.Architects.Contains(entity))
                        {
                            return 6;
                        }
                    }
                }
            }

            District currentDistrict = player.District;
            if (currentDistrict != null)
            {
                if (entity is Architect)
                {
                    if (LoadedArchitects.Contains(entity))
                    {
                        return 7;
                    }
                }
                else if (entity is Object)
                {
                    foreach (Block block in currentDistrict.DistrictMap)
                    {
                        if (block.Objects.Contains(entity))
                        {
                            return 7;
                        }

                        foreach (Structure structure in block.Structures)
                        {
                            foreach (Room room in structure.Rooms)
                            {
                                if (room.Objects.Contains(entity))
                                {
                                    return 7;
                                }
                            }
                        }
                    }
                }
                else if (entity is Structure s && s.Block.District == currentDistrict)
                {
                    return 7;
                }
            }

            // If the entity is a body part, calculate the score based on the creator's proximity
            if (entity is Object bodyPart && bodyPart.Creator is Architect creator)
            {
                return CalculateProximityScore(creator, player);
            }

            // Default score for entities that are farthest away
            return 8;
        }

        public static List<string> GetCommandsForCategory(string category)
        {
            return CommandIDToCategory
                .Where(kvp => kvp.Value.Contains(category))
                .Select(kvp => kvp.Key).ToList()
                ;
        }

        static List<Entity> FilterSubjectsForCommandPart(string commandPart, List<Entity> allSubjects, Architect MostRecentPartyTurnArchitect, out Dictionary<string, Entity> matchedSubjects)
        {
            List<(string referredName, Entity entity, int index)> matchedData = new List<(string, Entity, int)>();
            matchedSubjects = new Dictionary<string, Entity>();

            // Precompute the list of all referred names with proximity scores
            List<(string referredName, Entity entity, int proximityScore)> allReferredNames = new List<(string, Entity, int)>();

            foreach (var subject in allSubjects)
            {
                var names = subject.ReferredToNames.Concat(new[] { subject.Metadata, subject.Name })
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .Where(name => !string.IsNullOrWhiteSpace(name));

                foreach (var name in names)
                {
                    allReferredNames.Add((name.ToLower(), subject, CalculateProximityScore(subject, MostRecentPartyTurnArchitect)));
                }
            }

            // Sort the referred names by proximity score and then by length
            allReferredNames = allReferredNames
                .OrderBy(t => t.proximityScore)
                .ThenByDescending(t => t.referredName.Length)
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


        public static void MessageWorldEdit(Architect Sender, Architect Reciever, string MessageID, EntityList<Entity> Subjects, string Response, EntityList<Location> storedRevealLocations)
        {
            var triggers = new Dictionary<string, List<string>>
            {
                { "ask_them_join", new List<string> { "I would be honored", "If it means I spend" } },
                { "ask_me_join", new List<string> { "Yes, Welcome to", "If it means you" } },
                { "challenge", new List<string> { "I accept your challenge", "I don’t think I can" } },
                { "surrender", new List<string> { "Stay put and do", "I surrender too", "Surrender to my looks?" } },
                { "demand_surrender", new List<string> { "I yield", "Okay! But only to you" } },
                { "ask_name", new List<string> { "My name is" } },
                { "demand_item", new List<string> { "Okay! I'll", "I'll drop it, but" } }
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
                            if (GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Sender) || GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Reciever))
                            {
                                // Ensure the receiver joins the GameWorld.GamePlayerAssociation.ActiveParty
                                if (Reciever.Group != null)
                                {
                                    Reciever.Group.Architects.Remove(Reciever);
                                }
                                if (!GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Reciever))
                                {
                                    GameWorld.GamePlayerAssociation.ActiveParty.Architects.Add(Reciever);

                                    Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Reciever.Name + " joined " + GameWorld.GamePlayerAssociation.ActiveParty.Name + ".", GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Location.Region, new EntityList<Entity>(){Reciever, GameWorld.GamePlayerAssociation.ActiveParty}));
                                }
                                Reciever.Group = null;
                            }
                            else
                            {
                                // Regular group join logic
                                if (Sender.Group == null)
                                {
                                    Sender.Group = new Group(new EntityList<Architect> { Sender, Reciever }, "adventurer", Sender, Sender.Location);
                                }
                                if (Reciever.Group != null)
                                {
                                    Reciever.Group.Architects.Remove(Reciever);
                                }
                                Reciever.Group = Sender.Group;

                                // Log the historical event for AI joining AI group
                                Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Reciever.Name + " joined the group of " + Sender.Group.Name + ".", Sender.Location.Region, new EntityList<Entity>(){Reciever, Sender}));
                            }
                            break;

                        case "ask_me_join":
                            if (GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Sender) || GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Reciever))
                            {
                                // Ensure the sender joins the GameWorld.GamePlayerAssociation.ActiveParty
                                if (Sender.Group != null)
                                {
                                    Sender.Group.Architects.Remove(Sender);
                                }
                                if (!GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Sender))
                                {
                                    GameWorld.GamePlayerAssociation.ActiveParty.Architects.Add(Sender);

                                    Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Sender.Name + " joined " + GameWorld.GamePlayerAssociation.ActiveParty.Name + ".", Sender.Location.Region, new EntityList<Entity>() { Sender, GameWorld.GamePlayerAssociation.ActiveParty }));
                                }
                                Sender.Group = null;
                            }
                            else
                            {
                                // Regular group join logic
                                if (Reciever.Group == null)
                                {
                                    Reciever.Group = new Group(new EntityList<Architect> { Reciever, Sender }, "adventurer", Reciever, Reciever.Location);
                                }
                                if (Sender.Group != null)
                                {
                                    Sender.Group.Architects.Remove(Sender);
                                }
                                Sender.Group = Reciever.Group;

                                // Log the historical event for AI joining AI group
                                Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Sender.Name + " joined the group of " + Reciever.Name + ".", Sender.Location.Region, new EntityList<Entity>() { Sender, Reciever }));
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

                                Sender.CyclesLeftInTask = 1000;
                                Reciever.CyclesLeftInTask = 1000;

                                Sender.ShieldTokens.Add(Reciever);
                                Reciever.ShieldTokens.Add(Sender);

                                Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Sender.Name + " challenged " + Reciever.Name + " in " + Sender.Location.Name + ".", Sender.Location.Region, new EntityList<Entity>(){Sender, Reciever, Sender.Location}));
                            }
                            break;

                        case "surrender":
                            if (triggers["surrender"].Any(trigger => Response.StartsWith(trigger, StringComparison.OrdinalIgnoreCase)))
                            {
                                // Ensure the list is initialized
                                if (Reciever.ArchitectsWhoSurrenderedToMe == null)
                                {
                                    Reciever.ArchitectsWhoSurrenderedToMe = new EntityHashSet<Architect>();
                                }

                                // Add the sender to the receiver's list of those who surrendered to them
                                Reciever.ArchitectsWhoSurrenderedToMe.Add(Sender);

                                Sender.TargetArchitect = null;
                                Sender.Task = "";
                                Sender.CyclesLeftInTask = 0;
                                Sender.SetOpinion(Reciever, -30);

                                Reciever.TargetArchitect = null;
                                Reciever.Task = "";
                                Reciever.CyclesLeftInTask = 0;
                                Reciever.SetOpinion(Sender, -30);
                            }
                            break;

                        case "demand_surrender":
                            if (triggers["demand_surrender"].Any(trigger => Response.StartsWith(trigger, StringComparison.OrdinalIgnoreCase)))
                            {
                                // Add the receiver to the sender's list of those who surrendered to them
                                Sender.ArchitectsWhoSurrenderedToMe.Add(Reciever);
                                Reciever.ArchitectsWhoISurrenderedTo.Add(Sender);

                                Sender.TargetArchitect = null;
                                Sender.Task = "";
                                Sender.CyclesLeftInTask = 0;
                                Sender.SetOpinion(Reciever, -10);

                                Reciever.TargetArchitect = null;
                                Reciever.Task = "";
                                Reciever.CyclesLeftInTask = 0;
                                Reciever.SetOpinion(Sender, -10);
                            }
                            break;
                        
                        case "ask_name":
                            if (triggers["ask_name"].Any(trigger => Response.StartsWith(trigger, StringComparison.OrdinalIgnoreCase)))
                            {
                                Sender.KnownArchitects.Add(Reciever);
                                Reciever.KnownArchitects.Add(Sender);
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
                                    else if (Reciever.OffHeldObject == demandedItem)
                                    {
                                        Reciever.OffHeldObject = null;
                                    }
                                    // Check and remove the item from right hand
                                    else if (Reciever.MainHeldObject == demandedItem)
                                    {
                                        Reciever.MainHeldObject = null;
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

            // Check for storedRevealLocations and update the map accordingly
            if (storedRevealLocations.Count() > 0 && GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Sender) && Response.EndsWith("[Map Updated]"))
            {
                foreach (var location in storedRevealLocations)
                {
                    var region = location.Region;
                    region.Explored = true;
                    region.DeepExplored = true;
                    location.Explored = true;
                    Game1.GameWorld.GamePlayerAssociation.ActiveParty.CurrentlyMarkedRegions.Add(region);
                }
            }
        }



        public static void CalculateAttack(string verb, Architect attacker, Object target, string DefenderAction, Object weapon)
        {
            int Month = ((int)Math.Round((decimal)(GameWorld.Cycle / 24192000)) % 12) + 1;
            int Year = (int)Math.Round((decimal)(GameWorld.Cycle / 290304000), MidpointRounding.ToZero);

            string Date = "(" + Month + "/" + Year + ")";

            EntityList<TextStorage> announcements = new EntityList<TextStorage>()
                    {
                        
                    };

            // Exposure calculation
            if (weapon != null)
            {
                if (attacker.BodyParts.Contains(weapon))
                {
                    weapon.UpdateExposure(25 - attacker.Dexterity);
                }
                else
                {
                    if (attacker.OffHeldObject == weapon)
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
                    else if (attacker.MainHeldObject == weapon)
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

            bool Avoided = false;
            string AvoidFeedback = "";

            Architect TargetArchitect = null;

            if (target.Owner != null && target.IsBodyPart && target.Owner is Architect)
            {
                TargetArchitect = (Architect)(target.Owner);
                TargetArchitect.Focused = true;
                TargetArchitect.CombatCycles = 100;
            }

            if (TargetArchitect != null)
            {
                if (!GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(TargetArchitect))
                {
                    TargetArchitect.ChangeOpinion(attacker, -75);
                }

                string eventText = $"{Date} {attacker.Name} attacked {TargetArchitect.Name}.";
                Event newEvent = new Event(eventText, TargetArchitect.Location.Region, new EntityList<Entity>());
                bool addEvent = true;

                // Check the last ten historical events
                int startIndex = Math.Max(0, GameWorld.HistoricalEvents.Count - 10);
                for (int i = startIndex; i < GameWorld.HistoricalEvents.Count; i++)
                {
                    if (GameWorld.HistoricalEvents[i].EventData == eventText) // Assuming HistoricalEvents[i] is an Event object
                    {
                        addEvent = false;
                        break;
                    }
                }

                if (addEvent)
                {
                    GameWorld.HistoricalEvents.Add(newEvent);
                }
            }


            if (!Game1.OffensiveTasks.Contains(TargetArchitect.Task))
            {
                TargetArchitect.CyclesLeftInTask = 0;
                TargetArchitect.Task = "";
            }

            bool IsPlayerPartyNearby(Architect attacker)
            {
                foreach (Architect partyMember in GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                {
                    if ((attacker.Room != null && attacker.Room == partyMember.Room) ||
                        (attacker.Room == null && attacker.Block == partyMember.Block))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (target != null && TargetArchitect != null)
            {
                attacker.CombatCycles = 50;

                Object DefenderWeapon = null;
                Object DefenderSidearm = null;

                if (TargetArchitect.MainHeldObject != null && TargetArchitect.MainHeldObject.IsWeapon && TargetArchitect.OffHeldObject != null && TargetArchitect.OffHeldObject.IsWeapon)
                {
                    DefenderWeapon = TargetArchitect.MainHeldObject;
                    DefenderSidearm = TargetArchitect.OffHeldObject;
                }
                if (TargetArchitect.MainHeldObject != null && TargetArchitect.MainHeldObject.IsWeapon)
                {
                    DefenderWeapon = TargetArchitect.MainHeldObject;
                }
                else if (TargetArchitect.OffHeldObject != null && TargetArchitect.OffHeldObject.IsWeapon)
                {
                    DefenderWeapon = TargetArchitect.OffHeldObject;
                }

                Object primaryObject = TargetArchitect.MainInteractionAppendage;
                Object secondaryObject = TargetArchitect.OffInteractionAppendage;

                DefenderWeapon = DefenderWeapon ?? primaryObject;
                if (DefenderWeapon == null)
                {
                    DefenderWeapon = TargetArchitect.BodyParts[r.Next(TargetArchitect.BodyParts.Count())];
                }

                DefenderSidearm = DefenderSidearm ?? secondaryObject;
                if (DefenderSidearm == null)
                {
                    DefenderSidearm = TargetArchitect.BodyParts[r.Next(TargetArchitect.BodyParts.Count())];
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

                    DefenderAction = PossibleActions[r.Next(PossibleActions.Count())];
                }

                var successChances = TargetArchitect.CalculateSuccessChances(new Attack(verb, attacker, target, weapon), Game1.GameWorld.ReactionModifierInt, attacker, proficiencyModifier);

                // Falling star logic
                int StarCount = 0;

                if (attacker.PathOfStarsLevel >= 2)
                {
                    StarCount = 1;
                    announcements.Add(new TextStorage($"A star falls from the heavens!", Color.Goldenrod, new EntityList<Entity>(){}));
                }
                else if (attacker.PathOfStarsLevel >= 6)
                {
                    StarCount = 3;
                    announcements.Add(new TextStorage($"Stars fall from the heavens!", Color.Goldenrod, new EntityList<Entity>(){}));
                }

                for (int i = 0; i < StarCount; i++)
                {
                    EntityList<Architect> FightingArchitects = new EntityList<Architect>();
                    foreach (Architect a in attacker.Room != null ? attacker.Room.Architects : attacker.Block.Architects)
                    {
                        if ((a.Task == "killtarget" || a.Task == "disabletarget") && ((GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(attacker) && !GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(TargetArchitect)) || (!GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(attacker) && GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(TargetArchitect))))
                        {
                            FightingArchitects.Add(a);
                        }
                    }

                    Architect TargetArchitectForStar = null;

                    if (!FightingArchitects.Contains(TargetArchitect))
                    {
                        FightingArchitects.Add(TargetArchitect);
                    }

                    if (FightingArchitects.Count() > 1)
                    {
                        if (FightingArchitects.Contains(TargetArchitect))
                        {
                            FightingArchitects.Remove(TargetArchitect);
                        }

                        TargetArchitectForStar = FightingArchitects[r.Next(FightingArchitects.Count())];
                    }
                    else if (FightingArchitects.Count() == 1)
                    {
                        TargetArchitectForStar = FightingArchitects[0];
                    }

                    Object o = new Object(null, "star", new EntityList<Material>() { GameWorld.Energy }, attacker);
                    o.AirborneTarget = TargetArchitectForStar;
                    o.Thrower = attacker;
                    (TargetArchitectForStar.Room != null ? TargetArchitectForStar.Room.Objects : TargetArchitectForStar.Block.Objects).Add(o);
                }

                if (target != null)
                {
                    if ((attacker.Room != null && attacker.Room.Architects.Contains(LoadedArchitects[ArchitectIndex])) ||
                       (attacker.Block != null && attacker.Block.Architects.Contains(LoadedArchitects[ArchitectIndex])))
                    {
                        if (IsPlayerPartyNearby(attacker))
                        {
                            string simplifiedVerb;
                            if (verb.EndsWith("e") || new List<string> { "whip", "hack", "whack", "cut", "bludgeon", "club", "kick", "headbutt", "slap", "jab" }.Contains(verb))
                            {
                                simplifiedVerb = verb + "s";
                            }
                            else
                            {
                                simplifiedVerb = verb + "es";
                            }

                            string message = $"{attacker.ReferredToNames[0]} {simplifiedVerb} {target.ReferredToNames[0]} with {weapon.ReferredToNames[0]}!";
                            announcements.Add(new TextStorage(message, Color.Blue, new EntityList<Entity>() { attacker, weapon }));


                            if (TargetArchitect != null && r.Next(0, 100) < TargetArchitect.ExtraStealth)
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
                                        // Determine which hand (right or left) is holding the weapon used to parry
                                        string parryHand = "";

                                        if (DefenderWeapon != null && DefenderWeapon.IsWeapon)
                                        {
                                            if (TargetArchitect.MainHeldObject == DefenderWeapon)
                                            {
                                                parryHand = TargetArchitect.MainInteractionAppendage.Type;
                                            }
                                            else if (TargetArchitect.OffHeldObject == DefenderWeapon)
                                            {
                                                parryHand = TargetArchitect.OffInteractionAppendage.Type;
                                            }
                                        }
                                        else if (DefenderSidearm != null && DefenderSidearm.IsWeapon)
                                        {
                                            if (TargetArchitect.MainHeldObject == DefenderSidearm)
                                            {
                                                parryHand = TargetArchitect.MainInteractionAppendage.Type;
                                            }
                                            else if (TargetArchitect.OffHeldObject == DefenderSidearm)
                                            {
                                                parryHand = TargetArchitect.OffInteractionAppendage.Type;
                                            }
                                        }

                                        // Apply exposure update based on the identified parrying hand
                                        if (!string.IsNullOrEmpty(parryHand))
                                        {
                                            if (TargetArchitect.FindBodyPart(parryHand) != null)
                                            {
                                                TargetArchitect.FindBodyPart(parryHand).UpdateExposure(15 - TargetArchitect.Dexterity);
                                            }
                                            if (TargetArchitect.FindBodyPart(parryHand) != null)
                                            {
                                                TargetArchitect.FindBodyPart(parryHand).UpdateExposure(10 - TargetArchitect.Dexterity);
                                            }
                                        }

                                        // Handle proficiency and success chances
                                        if ((DefenderWeapon != null && DefenderWeapon.IsWeapon) || (DefenderSidearm != null && DefenderSidearm.IsWeapon))
                                        {
                                            Object usedWeapon = DefenderWeapon ?? DefenderSidearm;
                                            if (usedWeapon != null)
                                            {
                                                switch (usedWeapon.DamageType)
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

                                                int parrySuccessChance = r.Next(0, 101);
                                                int SucChance = successChances.redirect;

                                                if (parrySuccessChance < SucChance)
                                                {
                                                    Avoided = true;
                                                    AvoidFeedback = "The attack is parried by the " + usedWeapon.ReferredToNames[0] + "!";
                                                    TargetArchitect.ChangeXP("parrying", r.Next(1, 4));
                                                    foreach (Imbuement i in TargetArchitect.CurrentlyActiveImbuements)
                                                    {
                                                        if (i.IsTrigger && i.ConditionOrTrigger == "onparry")
                                                        {
                                                            TextStorage result = TargetArchitect.ActivatePower(i.BuffOrResult);
                                                            if (result.Data != "unknown")
                                                            {
                                                                announcements.Add(result);
                                                            }
                                                        }
                                                    }

                                                }
                                                else
                                                {
                                                    AvoidFeedback = "The attack is not parried by the " + usedWeapon.ReferredToNames[0] + "!";
                                                }
                                            }
                                        }
                                        else
                                        {
                                            AvoidFeedback = TargetArchitect.ReferredToNames[0] + " was unable to parry the attack with their arsenal!";
                                        }
                                        break;

                                    case "block":
                                        // Check if the defender has a shield or any other item that could be used for blocking
                                        bool isShield = (DefenderWeapon != null && DefenderWeapon.Type == "shield") ||
                                                        (DefenderSidearm != null && DefenderSidearm.Type == "shield");

                                        // Use successChances.block as the base chance if a shield is used
                                        int baseChance = isShield ? successChances.block : (int)(successChances.block * 0.75); // Apply 25% reduction to base chance if not a shield

                                        string blockingItem = isShield ? (DefenderWeapon != null && DefenderWeapon.Type == "shield" ? DefenderWeapon.ReferredToNames[0] : DefenderSidearm.ReferredToNames[0]) : "item";

                                        // Determine which hand (right or left) is holding the shield or blocking item
                                        string blockHand = "";

                                        if (DefenderWeapon != null && DefenderWeapon.IsWeapon)
                                        {
                                            if (TargetArchitect.MainHeldObject == DefenderWeapon)
                                            {
                                                blockHand = TargetArchitect.MainInteractionAppendage.Type;
                                            }
                                            else if (TargetArchitect.OffHeldObject == DefenderWeapon)
                                            {
                                                blockHand = TargetArchitect.OffInteractionAppendage.Type;
                                            }
                                        }
                                        else if (DefenderSidearm != null && DefenderSidearm.IsWeapon)
                                        {
                                            if (TargetArchitect.MainHeldObject == DefenderSidearm)
                                            {
                                                blockHand = TargetArchitect.MainInteractionAppendage.Type;
                                            }
                                            else if (TargetArchitect.OffHeldObject == DefenderSidearm)
                                            {
                                                blockHand = TargetArchitect.OffInteractionAppendage.Type;
                                            }
                                        }

                                        // Apply exposure update based on the identified blocking hand
                                        if (!string.IsNullOrEmpty(blockHand))
                                        {
                                            if (TargetArchitect.FindBodyPart(blockHand) != null)
                                            {
                                                TargetArchitect.FindBodyPart(blockHand).UpdateExposure(15 - TargetArchitect.Dexterity);
                                            }
                                            if (TargetArchitect.FindBodyPart(blockHand) != null)
                                            {
                                                TargetArchitect.FindBodyPart(blockHand).UpdateExposure(10 - TargetArchitect.Dexterity);
                                            }
                                        }

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
                                                    TextStorage result = TargetArchitect.ActivatePower(i.BuffOrResult);
                                                    if (result.Data != "unknown")
                                                    {
                                                        announcements.Add(result);
                                                    }
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

                                            foreach (Imbuement i in TargetArchitect.CurrentlyActiveImbuements)
                                            {
                                                if (i.IsTrigger && i.ConditionOrTrigger == "onduck")
                                                {
                                                    TextStorage result = TargetArchitect.ActivatePower(i.BuffOrResult);
                                                    if (result.Data != "unknown")
                                                    {
                                                        announcements.Add(result);
                                                    }
                                                }
                                            }


                                            // Apply exposure update for the "head", "neck", and "torso" parts
                                            if (TargetArchitect.FindBodyPart("head") != null)
                                            {
                                                TargetArchitect.FindBodyPart("head").UpdateExposure(15 - TargetArchitect.Dexterity);
                                            }
                                            if (TargetArchitect.FindBodyPart("neck") != null)
                                            {
                                                TargetArchitect.FindBodyPart("neck").UpdateExposure(10 - TargetArchitect.Dexterity);
                                            }
                                            if (TargetArchitect.FindBodyPart("torso") != null)
                                            {
                                                TargetArchitect.FindBodyPart("torso").UpdateExposure(5 - TargetArchitect.Dexterity);
                                            }

                                            // Apply exposure update for parts ending with "shoulder"
                                            foreach (var part in TargetArchitect.BodyParts)
                                            {
                                                if (part.Type.EndsWith("shoulder"))
                                                {
                                                    part.UpdateExposure(12 - TargetArchitect.Dexterity);
                                                }
                                            }
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
                                            foreach (Imbuement i in TargetArchitect.CurrentlyActiveImbuements)
                                            {
                                                if (i.IsTrigger && i.ConditionOrTrigger == "onjump")
                                                {
                                                    TextStorage result = TargetArchitect.ActivatePower(i.BuffOrResult);
                                                    if (result.Data != "unknown")
                                                    {
                                                        announcements.Add(result);
                                                    }
                                                }
                                            }

                                        }
                                        else
                                        {
                                            AvoidFeedback = TargetArchitect.ReferredToNames[0] + " attempted to jump over the attack, but failed!";
                                        }

                                        // Apply exposure update for parts containing "leg" or "foot"
                                        foreach (var part in TargetArchitect.BodyParts)
                                        {
                                            if (part.Type.Contains("foot"))
                                            {
                                                int deposition = Math.Max(10 - TargetArchitect.Dexterity, 1);
                                                part.UpdateExposure(deposition);
                                            }
                                            else if (part.Type.Contains("leg"))
                                            {
                                                int deposition = Math.Max(5 - (TargetArchitect.Dexterity / 2), 1);
                                                part.UpdateExposure(deposition);
                                            }
                                        }

                                        TargetArchitect.CyclesSinceJump = 0;
                                        break;


                                    case "roll":
                                        if (r.Next(0, 101) < successChances.roll)
                                        {
                                            Avoided = true;
                                            AvoidFeedback = TargetArchitect.ReferredToNames[0] + " rolled away from the attack, repositioning himself!";
                                            TargetArchitect.ChangeXP("dodging", r.Next(1, 4));
                                            foreach (Imbuement i in TargetArchitect.CurrentlyActiveImbuements)
                                            {
                                                if (i.IsTrigger && i.ConditionOrTrigger == "onroll")
                                                {
                                                    TextStorage result = TargetArchitect.ActivatePower(i.BuffOrResult);
                                                    if (result.Data != "unknown")
                                                    {
                                                        announcements.Add(result);
                                                    }
                                                }
                                            }

                                        }
                                        else
                                        {
                                            AvoidFeedback = TargetArchitect.ReferredToNames[0] + " failed to roll away from the attack, but repositioned himself!";
                                        }

                                        int repositionAmount = Math.Max(3, TargetArchitect.Dexterity) * 8;
                                        foreach (var part in TargetArchitect.BodyParts)
                                        {
                                            part.UpdateExposure(-1 * repositionAmount);
                                        }

                                        break;


                                    case "disarm":
                                        if (r.Next(0, 101) < successChances.disarm && (attacker.MainHeldObject == weapon || attacker.OffHeldObject == weapon))
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

                                            if (attacker.MainHeldObject == weapon)
                                            {
                                                attacker.MainHeldObject = null;
                                            }
                                            else
                                            {
                                                attacker.OffHeldObject = null;
                                            }
                                        }
                                        else
                                        {
                                            AvoidFeedback = TargetArchitect.ReferredToNames[0] + " attempted to disarm " + attacker.ReferredToNames[0] + ", but could not!";
                                        }
                                        break;

                                    case "redirect":

                                        // Determine the proficiency modifier based on the weapon's damage type
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
                                        }

                                        // Determine the proficiency modifier for the sidearm
                                        if (DefenderSidearm.IsWeapon)
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
                                        }

                                        // Calculate the success chance
                                        int successChance = successChances.redirect + (proficiencyModifier * 4);

                                        if (r.Next(0, 101) < successChance)
                                        {
                                            Avoided = true;
                                            AvoidFeedback = "The attack is redirected by the " + (DefenderWeapon.IsWeapon ? DefenderWeapon.ReferredToNames[0] : DefenderSidearm.ReferredToNames[0]) + "!";
                                            TargetArchitect.ChangeXP("redirection", r.Next(1, 4));

                                            foreach (Imbuement i in TargetArchitect.CurrentlyActiveImbuements)
                                            {
                                                if (i.IsTrigger && i.ConditionOrTrigger == "onredirect")
                                                {
                                                    TextStorage result = TargetArchitect.ActivatePower(i.BuffOrResult);
                                                    if (result.Data != "unknown")
                                                    {
                                                        announcements.Add(result);
                                                    }
                                                }
                                            }

                                        }
                                        else
                                        {
                                            AvoidFeedback = "The redirect of " + (DefenderWeapon.IsWeapon ? DefenderWeapon.ReferredToNames[0] : DefenderSidearm.ReferredToNames[0]) + " failed!";
                                        }

                                        // Apply depositioning to all relevant limbs
                                        int depositionAmount = 20 - attacker.Dexterity;

                                        // Apply deposition to the limbs holding the DefenderWeapon
                                        Object weaponHand = (attacker.MainHeldObject == weapon) ? attacker.MainInteractionAppendage : attacker.OffInteractionAppendage;
                                        weaponHand.UpdateExposure(depositionAmount);

                                        break;

                                    default:
                                        break;

                                }
                            }

                            if (AvoidFeedback != "")
                            {
                                announcements.Add(new TextStorage(AvoidFeedback, Color.HotPink, new EntityList<Entity>(){}));
                            }

                        }
                    }

                    if (!Avoided)
                    {
                        CalculateAndApplyDamage(attacker, weapon, target, announcements);

                        foreach (Imbuement i in attacker.CurrentlyActiveImbuements)
                        {
                            if (i.IsTrigger && i.ConditionOrTrigger == "onattack")
                            {
                                TextStorage result = attacker.ActivatePower(i.BuffOrResult);
                                if (result.Data != "unknown")
                                {
                                    announcements.Add(result);
                                }
                            }
                        }

                    }

                    if (attacker.QuickStrikeReady)
                    {
                        announcements.Add(new TextStorage(attacker.ReferredToNames[0] + " strikes faster than light!", Color.LightCoral, new EntityList<Entity>() { attacker }));
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
                else
                {
                    announcements.Add(new TextStorage($"The attack was unsuccessful", Color.Red, new EntityList<Entity>(){}));
                }
            }
            else if (target != null && TargetArchitect == null)
            {
                CalculateAndApplyDamage(attacker, weapon, target, announcements);
            }

            foreach(TextStorage t in announcements)
            {
                attacker.AnnounceToParty(t.Data, t.Color, t.Entities);
            }

            void CalculateAndApplyDamage(Architect attacker, Object weapon, Object targetObject, EntityList<TextStorage> announcements)
            {
                int DivineMight = attacker.DivineMight > 0 ? 1 : 0;
                if (attacker.DivineMight > 0)
                {
                    attacker.DivineMight--;
                    announcements.Add(new TextStorage("The divine have intervened! The attack is empowered with brilliant energy!", Color.Aquamarine, new EntityList<Entity>(){}));
                    if (attacker.DivineMight == 0)
                    {
                        announcements.Add(new TextStorage("The divine might has worn off!", Color.Aquamarine, new EntityList<Entity>(){}));
                    }
                }

                if (TargetArchitect != null && TargetArchitect.DivineProtection > 0)
                {
                    TargetArchitect.DivineProtection--;
                    announcements.Add(new TextStorage($"The divine have intervened! The attack is avoided by {targetObject.Owner}'s divine protection. It wears thin, though...", Color.Aquamarine, new EntityList<Entity>(){}));
                    if (TargetArchitect.DivineProtection == 0)
                    {
                        announcements.Add(new TextStorage("The divine protection has worn off!", Color.Aquamarine, new EntityList<Entity>(){}));
                    }
                }

                if(TargetArchitect != null)
                {
                    foreach (Imbuement i in TargetArchitect.CurrentlyActiveImbuements)
                    {
                        if (i.IsTrigger && i.ConditionOrTrigger == "ondamage")
                        {
                            TextStorage result = TargetArchitect.ActivatePower(i.BuffOrResult);
                            if (result.Data != "unknown")
                            {
                                announcements.Add(result);
                            }
                        }
                    }


                    // Constants for initial damage calculation
                    double baseDamageMultiplier = 1.0;
                    double strengthMultiplier = 0.08 * attacker.Strength;
                    double proficiencyEffect = (double)proficiencyModifier * (1 + (double)attacker.ExtraAttackPower / 100);

                    int DamageModifier = (int)Math.Round((baseDamageMultiplier + strengthMultiplier) * proficiencyEffect + DivineMight);


                    // Check if the attacker's PathOfBodyLevel is 4 or higher
                    if (attacker.PathOfBodyLevel >= 4 && attacker.BodyParts.Contains(weapon))
                    {
                        DamageModifier += 5;  // Greatly increase the damage
                    }

                    if (attacker.DropKickReady && attacker.CyclesSinceJump <= 30 && (weapon.Type.EndsWith("foot") || weapon.Type.EndsWith("leg")))
                    {
                        announcements.Add(new TextStorage("The attack is a devastating dropkick!", Color.Red, new EntityList<Entity>(){}));
                        DamageModifier *= 3;
                        attacker.DropKickReady = false;
                    }

                    if (attacker.SeveringStrikeReady && targetObject.Owner != null && targetObject.Owner is Architect)
                    {
                        announcements.Add(new TextStorage("The attack severs many critical veins!", Color.Red, new EntityList<Entity>(){}));
                        ((Architect)(targetObject.Owner)).Bleeding += 6;
                        attacker.SeveringStrikeReady = false;
                    }

                    // Check for Radiant Energy channeling at level 6 or higher
                    if (attacker.PathOfBodyLevel >= 6 && attacker.BodyParts.Contains(weapon))
                    {
                        int radiantIncrease = new Random().Next(40, 80);  // Random increase between 40 and 80
                        ((Architect)(targetObject.Owner)).RadiantCycles += radiantIncrease;  // Increase RadiantCycles of the body part's owner

                        // Announcement for Radiant Energy effect
                        announcements.Add(new TextStorage($"{attacker.ReferredToNames[0]} channels a radiant energy into their strike, reducing the target's speed!", Color.Aquamarine, new EntityList<Entity>() { attacker }));
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
                        announcements.Add(new TextStorage("The attack is a destabilizing body slam!", Color.Red, new EntityList<Entity>(){}));
                        ((Architect)(targetObject.Owner)).DestabilizedCycles += (int)Math.Round(attacker.Energy / 2);
                        ((Architect)(targetObject.Owner)).Energy -= Game1.r.Next(5, 10);

                    }
                    else if ((weapon.Type.EndsWith("leg") || weapon.Type.EndsWith("foot")) && attacker.LegSweepReady)
                    {
                        attacker.LegSweepReady = false;
                        announcements.Add(new TextStorage("The attack is a swift leg sweep!", Color.Red, new EntityList<Entity>(){}));
                        ((Architect)(targetObject.Owner)).DestabilizedCycles += (int)Math.Round(attacker.Energy / 2);
                        ((Architect)(targetObject.Owner)).ReactionBoostCycles += 30;
                    }
                }
                else 
                {
                    //not body part ifno

                    // Constants for initial damage calculation
                    double baseDamageMultiplier = 1.0;
                    double strengthMultiplier = 0.08 * attacker.Strength;
                    double proficiencyEffect = (double)proficiencyModifier * (1 + (double)attacker.ExtraAttackPower / 100);

                    int DamageModifier = (int)Math.Round((baseDamageMultiplier + strengthMultiplier) * proficiencyEffect + DivineMight);

                    // Apply Damage to Target
                    announcements.AddRange(
                        targetObject.TakeDamageFromObject(
                            weapon,
                            DamageModifier,
                            attacker,
                            verb
                        )
                    );
                }
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
                            { "escape", "Immediately travel through a random adjacent door, or to an adjacent block. (Initiate with \"initiate escape\")" },
                            { "finale", "If your next attack is fatal, severely damage all nearby hostiles. (Initiate with \"initiate finale\")" },
                            { "concentration", "Increase focus by 1 for 30 seconds. (Initiate with \"initiate concentration\")" },
                            { "body slam", "Your next torso bash attack does extra damage and destabilizes proportional to your energy. (Initiate with \"initiate body slam\")" },
                            { "leg sweep", "Your next leg or foot attack destabilizes a target and gives you 30% more reaction success chance for 3 seconds. (Initiate with \"initiate leg sweep\")" },
                            { "water bolt", "Fire a bolt of water at the target, damaging and extinguishing them. (Cast with \"cast water bolt at ~\")" },
                            { "chaos flare", "Fire a bolt of light and darkness at the target, causing destabilization, burning, and damage. (Cast with \"cast chaos flare at ~\")" },
                            { "ice shock", "Expose a target to an unrelenting swirl of frost. (Cast with \"cast ice shock at ~\")" },
                            { "flash flame", "Concentrate heat energy at the target, igniting them. (Cast with \"cast flash flame at ~\")" },
                            { "tremor", "Destabilizes all architects and objects in the area except for the TARGET. (Cast with \"cast tremor at ~\")" },
                            { "truthfulness", "Permanently forces someone to always tell the truth to the caster. (Cast with \"cast truthfulness at ~\")" },
                            { "rise", "Send the target into the air. (Cast with \"cast rise at ~\")" },
                            { "hold", "Cause an architect to freeze in place, or an airborne object to fall out of the sky. (Cast with \"cast hold at ~\")" },
                            { "force throw", "Throw all subsequent targets of this spell at the first target. (Maximum Spell Targets is 5, Cast with \"cast force throw at ~ and ~ and...\")" },
                            { "shatter", "Destabilize an architect significantly, or break an object into millions of pieces. (Cast with \"cast shatter at ~\")" },
                            { "intercept", "Quickly fractallize an airborne projectile. (Cast with \"cast intercept at ~\")" },
                            { "expel", "Banish an object or a weakened creature to the fractal plane. (Cast with \"cast expel at ~\")" },
                            { "extract", "Return a creature or object from the fractal plane, exactly where it left. (Cast with \"cast extract at ~\")" },
                            { "revive", "Bring a creature back from the dead and grant them immortality. (Cast with \"cast raise at ~\")" },
                            { "resurrect", "Bring a creature back from the dead and grant them immortality. Restore all their body parts to full integrity. (Cast with \"cast resurrect at ~\")" },
                            { "animate", "Raise a corpse as a shade. It joins your party. (Cast with \"cast animate at ~\")" },
                            { "ethereal rupture", "Tear apart the land with a catastrophic ethereal rupture. Target any spellcasting focus to initiate the process. (Cast with \"cast ethereal rupture at ~\")" },
                            { "emergence", "Return a creature from the dead, give them a new body, and teleport them in front of you. (Cast with \"cast emergence at ~\")" },
                            { "eternal bind", "Enslave a creature to your will, regardless of past opinions, relations, and prejudices. (Cast with \"cast eternal bind at ~\")" },
                            { "expunge", "Blot a person, place, thing, or idea from existence. (Cast with \"cast expunge at ~\")" },
                            { "echo", "Create an exact clone of an object or person. A cloned person acts independently. (Cast with \"cast echo at ~\")" },
                            { "emergent growth", "Cover a target in combat-diminishing plants. (Cast with \"cast emergent growth at ~\")" },
                            { "immortalize", "Grant a creature immortality. (Cast with \"cast immortalize at ~\")" }
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
                int prevIndex = (index - 1 + Colors.Count()) % Colors.Count();
                int nextIndex = (index + 1) % Colors.Count();

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


        public static void SaveGame(World world)
        {
            // Create the LightrealmSaves folder if it doesn't exist
            string saveFolder = Path.Combine(DocumentsFolderPath, "LightrealmSaves", world.Name);
            Directory.CreateDirectory(saveFolder);

            // Remove GameWorld from EntityLedger and store its ID
            int gameWorldId = GameWorld.EntityLedger.FirstOrDefault(x => x.Value == GameWorld).Key;
            GameWorld.EntityLedger.Remove(gameWorldId);

            // Save the GameWorld ID in ExtraData
            string extraDataPath = Path.Combine(saveFolder, "ExtraData.txt");
            File.WriteAllText(extraDataPath, gameWorldId.ToString());

            // Save the world
            string worldPath = Path.Combine(saveFolder, $"{world.Name}.bin");
            SerializeObjectToBinaryFile(worldPath, world);

            List<string> versionData = new List<string>() { Version, "Modification of the above line may lead to world corruption." };
            File.WriteAllLines(Path.Combine(saveFolder, $"version.txt"), versionData);

            // Create a list to hold the header and the historical events
            List<string> fileContent = new List<string>
    {
        "Historical Events of " + world.Name,
        "To find events related to a certain subject, try typing in their name.",
        "" // Adding an empty line for better readability
    };

            // Format and add each historical event to the file content
            foreach (var historicalEvent in world.HistoricalEvents)
            {
                int regionIndex = historicalEvent.Region != null ? GameWorld.Realms.IndexOf(historicalEvent.Region.Realm) : 11;
                int significantFlag = historicalEvent.Significant ? 1 : 0;
                string formattedEvent = $"[{regionIndex}/{significantFlag}]{historicalEvent.EventData}";
                fileContent.Add(formattedEvent);
            }

            // Write the content to the file
            File.WriteAllLines(Path.Combine(saveFolder, $"history.txt"), fileContent.ToArray());
        }

        public static void LoadGame(string loadingDirectory)
        {
            // Assuming loadingDirectory is the path to the directory containing all files
            string[] files = Directory.GetFiles(loadingDirectory);

            // Find the world file (assumes the file name starts with "The Continent of ")
            string worldFilePath = files.FirstOrDefault(f => Path.GetFileName(f).StartsWith("The Continent of "));

            if (worldFilePath == null)
            {
                // Handle the case where the world file is not found
                throw new Exception("Invalid Directory? Did you tamper with your save files :(");
            }

            // Find the ExtraData file
            string extraDataPath = files.FirstOrDefault(f => Path.GetFileName(f).Equals("ExtraData.txt"));

            if (extraDataPath == null)
            {
                // Handle the case where the ExtraData file is not found
                throw new Exception("ExtraData file not found.");
            }

            // Load the world
            World loadedWorld = DeserializeObjectFromBinaryFile<World>(worldFilePath);

            // Load the GameWorld ID from ExtraData
            int gameWorldId = int.Parse(File.ReadAllText(extraDataPath));

            GameWorld = loadedWorld;

            // Add GameWorld back to EntityLedger
            GameWorld.EntityLedger[gameWorldId] = GameWorld;

            // Temporary bandaid on this interesting problem
            foreach (Architect a in GameWorld.GamePlayerAssociation.ActiveParty.Architects)
            {
                if (!LoadedArchitects.Contains(a))
                {
                    LoadedArchitects.Add(a);
                }
            }

            MostRecentPartyTurnArchitect = GameWorld.GamePlayerAssociation.ActiveParty.Architects[0];
            foreach (Architect a in GameWorld.GamePlayerAssociation.ActiveParty.Architects)
            {
                a.ReferredToNames.Remove("myself");
                a.ReferredToNames.Remove("me");
            }
            MostRecentPartyTurnArchitect.AddReferredToName("myself");
            MostRecentPartyTurnArchitect.AddReferredToName("me");
        }

        private static void SerializeObjectToJsonFile<T>(string filePath, T obj)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(obj, options);
            File.WriteAllText(filePath, json);
        }

        private static T DeserializeObjectFromJsonFile<T>(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<T>(json);
        }

        //three golden data pieces that run our entire game.

        public static World GameWorld;
        


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

        public static EntityList<TextStorage> Exposition = new EntityList<TextStorage>();

        public static Dictionary<string, string> InvertDoorDirection = new Dictionary<string, string>();

        int TotalCivTries = 0;

        bool AlreadyTriedASearch = false;

        public int FlashTick = 0;

        public string SelectedDirectory = "";

        public bool HasTriedToMoveManually = false;

        public static EntityList<TextStorage> Observations = new EntityList<TextStorage>();
        public static EntityList<TextStorage> Messages = new EntityList<TextStorage>();
        public static EntityList<TextStorage> Announcements = new EntityList<TextStorage>();

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
        
        public static List<string> PossibleMagicalItems = new List<string>() { "chalice", "scepter", "lantern", "staff", "hourglass", "locket", "orb" };

        public Dictionary<string, Texture2D> TileAtlas = new Dictionary<string, Texture2D>();
        public static List<string> WeightedRandomArchitectProfessions = new List<string>() { "commander", "craftsman", "craftsman", "craftsman", "mercenary", "mercenary", "mercenary", "musician", "musician", "elder", "prophet", "trader", "trader", "trader", "trader", "merchant", "merchant", "anarchist", "political figure", "scholar", "scholar", "scholar", "scholar" };
        public static List<string> WeightedRandomNormalProfessions = new List<string>() { "soldier", "peasant", "peasant", "peasant", "blacksmith", "miller", "baker", "merchant", "brewer", "brewer", "tanner", "tailor", "carpenter", "mason", "scribe", "butcher", "fisherman", "weaver", "potter", "miner", "miner", "indolent", "indolent", "indolent", "indolent" };

        public static List<string> ArchitectProfessions = new List<string>() { "commander", "craftsman", "mercenary", "musician", "elder", "prophet", "trader", "anarchist", "political figure", "scholar" };
        public static List<string> Sexes = new List<string>() { "male", "female" };

        public static List<string> DeathCauses = new List<string>() { "fell to their death", "drowned", "burned to death", "starved to death", "dehydrated", "choked to death", "was killed by a wild animal", "bled to death" };

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
        public static List<string> ScienceSchools = new List<string>() { "engineering", "mathematics", "biology", "chemistry", "physics" };

        public static List<string> WrittenObjectTypes = new List<string>() { "scroll", "book", "scroll", "book", "scroll", "book", "waxtablet", "sheet" };

        public List<Keys> KeysNewlyPressed = new List<Keys>();

        public static bool SplitMode = false;

        public EntityList<Material> CoreMaterials = new EntityList<Material>();

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
        Texture2D QuickStartGUI;
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

        public static int CurrentlySelectingSex;
        public static bool CurrentlySelectingHandedness = true;
        public static int CurrentlySelectingRace = 1;

        public bool FancyFont = false;

        public SpriteFont Shibafont
        {
            get
            {
                return FancyFont ? BlackChanceryLarge : DePixelLarge;
            }
        }

        public SpriteFont BabyShibafont
        {
            get
            {
                return FancyFont ? BlackChancerySmall : DePixelSmall;
            }
        }

        public SpriteFont BlackChancerySmall;
        public SpriteFont BlackChanceryLarge;
        public SpriteFont DePixelSmall;
        public SpriteFont DePixelLarge;

        public static Dictionary<string, string> ConvertArchitectToGroupType = new Dictionary<string, string>();
        public static Dictionary<string, string> ConvertProfessionToBuilding = new Dictionary<string, string>();
        public static Dictionary<string, string> ConvertTaskToArchitectPrefix = new Dictionary<string, string>();
        public static Dictionary<string, string> ConvertProfessionToCareerDescription = new Dictionary<string, string>();

        public static Random r = new Random();

        public static Song LightrealmMainTheme;
        public static Song Introspection;

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

        public Texture2D BastionT;
        public Texture2D FortT;

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
        public Texture2D DistrictBastionT;
        public Texture2D DistrictFortT;


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
        public Texture2D BetterCursorT;
        public Texture2D Gradient;
        public Texture2D TitleScreen;
        public Texture2D GuideT;
        public Texture2D ReactionGUIT;
        public Texture2D ReactionGUIHelpT;
        public Texture2D MessageGUIT;
        public Texture2D CmdHelpT;
        public Texture2D BleedT;

        public Texture2D FrameT;
        public Texture2D MoveMapFrameT;
        public Texture2D SpeakingT;
        public Texture2D QuitGuiT;

        public Texture2D ArchitectHere;
        public Texture2D HealthGuiT;

        public Texture2D VibrantBlackT;
        public Texture2D WindsweptBlackT;
        public Texture2D DiminishedBlackT;
        public Texture2D SwirlBlackT;
        public Texture2D VibrantWhiteT;
        public Texture2D WindsweptWhiteT;
        public Texture2D DiminishedWhiteT;
        public Texture2D SwirlWhiteT;
        public Texture2D VibrantGrayT;
        public Texture2D WindsweptGrayT;
        public Texture2D DiminishedGrayT;
        public Texture2D SwirlGrayT;


        int WaitingTicks = 0;
        int EscapeTicks = 0;
        int SaveTicks = 0;
        int LoadGameCursor = 0;

        public static string GameState = "mainscreen";
        public static string HistoryPrompt = "";
        public static string GameMode = "unknown";

        public static string CraftingPhase = "selectrecipe"; //this is the phase you are in. it can also be "selectingredients"
        public static int RecipeIndex = 0; //this is the currently selected recipe in the list. pressing enter switches your crafting phase.
        public EntityList<Object> ObjectsInInventoryUsableForResources = new EntityList<Object>(); //these objects are the ones that can be added to your recipe
        public static int InventoryCraftingIndex = 0; //this is the currently selected item in the crafting objects list. pressing enter adds it to indexes for resources.
        public List<int> IndexesForResources = new List<int>(); //the indexes of the objects you selected.


        public static string FormatList(List<string> items)
        {
            int count = items.Count();
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

        public static string FormatList(EntityList<Entity> items)
        {
            int count = items.Count();
            if (count == 0)
            {
                return "";
            }
            else if (count == 1)
            {
                return items[0].ReferredToNames[0];
            }
            else if (count == 2)
            {
                return $"{items[0].ReferredToNames[0]} and {items[1].ReferredToNames[0]}";
            }
            else
            {
                string lastItem = items[count - 1].ReferredToNames[0];
                string otherItems = string.Join(", ", items.GetRange(0, count - 1).Select(item => item.ReferredToNames[0]));
                return $"{otherItems}, and {lastItem}";
            }
        }


        public static string FormatMaterialList(EntityList<Material> materials)
        {
            return string.Join(" ", materials.Select(m => m.Name));
        }

        public static string RestOfListIncludingThisIndex(List<string> list, int index)
        {
            if (index < 0 || index >= list.Count())
            {
                return string.Empty; // Return an empty string for invalid indices
            }

            // Create a sublist starting from the given index
            List<string> sublist = list.GetRange(index, list.Count() - index);

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
                return (char.ToUpper(SubjectOrSpell[0]) + SubjectOrSpell.Substring(1) + RPGBookNameSuffixes[r.Next(RPGBookNameSuffixes.Count())]);
            }
            else
            {
                return (RPGBookNamePrefixes[r.Next(RPGBookNamePrefixes.Count())] + " " + char.ToUpper(SubjectOrSpell[0]) + SubjectOrSpell.Substring(1));
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

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
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
            else if (a.Target != (null, null, null, null, null, "") && (a.Target.Item1 != a.Location.Region || a.Target.Item2 != a.Location || a.Target.Item3 != a.District || a.Target.Item4 != a.Block || a.Target.Item5 != a.Room))
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
                if (a.ReferredToNames.Count() > 0)
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
        {
            // Initialization logic
            Window.IsBorderless = true;
            IsMouseVisible = false;

            PortAudio.Initialize();

            PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            if(PreferredBackBufferWidth > 1920)
            {
                FancyFont = false;
            }
            else
            {
                FancyFont = false;
            }

            _graphics.PreferredBackBufferWidth = PreferredBackBufferWidth;
            _graphics.PreferredBackBufferHeight = PreferredBackBufferHeight;

            // Define the original corners based on the new data
            Vector2[] originalCorners = new Vector2[]
            {
        new Vector2(266, 1274), new Vector2(311, 1318), // NW
        new Vector2(320, 1273), new Vector2(357, 1318), // N
        new Vector2(366, 1274), new Vector2(403, 1318), // NE
        new Vector2(265, 1328), new Vector2(311, 1365), // W
        new Vector2(365, 1328), new Vector2(403, 1363), // E
        new Vector2(266, 1375), new Vector2(311, 1409), // SW
        new Vector2(319, 1375), new Vector2(357, 1411), // S
        new Vector2(365, 1375), new Vector2(405, 1410), // SE
        new Vector2(319, 1328), new Vector2(357, 1364)  // Center
            };

            // Scale the coordinates
            Vector2[] scaledCorners = new Vector2[originalCorners.Length];
            for (int i = 0; i < originalCorners.Length; i++)
            {
                scaledCorners[i] = new Vector2(
                    originalCorners[i].X * PreferredBackBufferWidth / 2560,
                    originalCorners[i].Y * PreferredBackBufferHeight / 1440
                );
            }

            // Manually define the rectangles based on the scaled coordinates
            RectangleNW = new Rectangle((int)scaledCorners[0].X, (int)scaledCorners[0].Y, (int)(scaledCorners[1].X - scaledCorners[0].X), (int)(scaledCorners[1].Y - scaledCorners[0].Y));
            RectangleN = new Rectangle((int)scaledCorners[2].X, (int)scaledCorners[2].Y, (int)(scaledCorners[3].X - scaledCorners[2].X), (int)(scaledCorners[3].Y - scaledCorners[2].Y));
            RectangleNE = new Rectangle((int)scaledCorners[4].X, (int)scaledCorners[4].Y, (int)(scaledCorners[5].X - scaledCorners[4].X), (int)(scaledCorners[5].Y - scaledCorners[4].Y));
            RectangleW = new Rectangle((int)scaledCorners[6].X, (int)scaledCorners[6].Y, (int)(scaledCorners[7].X - scaledCorners[6].X), (int)(scaledCorners[7].Y - scaledCorners[6].Y));
            RectangleE = new Rectangle((int)scaledCorners[8].X, (int)scaledCorners[8].Y, (int)(scaledCorners[9].X - scaledCorners[8].X), (int)(scaledCorners[9].Y - scaledCorners[8].Y));
            RectangleSW = new Rectangle((int)scaledCorners[10].X, (int)scaledCorners[10].Y, (int)(scaledCorners[11].X - scaledCorners[10].X), (int)(scaledCorners[11].Y - scaledCorners[10].Y));
            RectangleS = new Rectangle((int)scaledCorners[12].X, (int)scaledCorners[12].Y, (int)(scaledCorners[13].X - scaledCorners[12].X), (int)(scaledCorners[13].Y - scaledCorners[12].Y));
            RectangleSE = new Rectangle((int)scaledCorners[14].X, (int)scaledCorners[14].Y, (int)(scaledCorners[15].X - scaledCorners[14].X), (int)(scaledCorners[15].Y - scaledCorners[14].Y));
            RectangleCenter = new Rectangle((int)scaledCorners[16].X, (int)scaledCorners[16].Y, (int)(scaledCorners[17].X - scaledCorners[16].X), (int)(scaledCorners[17].Y - scaledCorners[16].Y));

            _graphics.ApplyChanges();

            // Determine the best way to draw the map
            TileSize = 12;
            TileXDistance = 13;
            TileZDistance = 10;

            // Create save directory if not already
            if (!Directory.Exists(DocumentsFolderPath + "/LightrealmSaves"))
            {
                Directory.CreateDirectory(DocumentsFolderPath + "/LightrealmSaves");
            }

            RecognizedCommands.Add("ask_name", (new List<string> { "ask ~ /p name", "ask ~ for /p name", "ask ~ name" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_directions", (new List<string> { "ask ~ where ~ is", "ask ~ to guide me to ~", "ask ~ the way to ~", "ask ~ how to get to ~", "ask ~ where i can find ~", "ask ~ where to find ~", "ask ~ for directions to ~", "ask ~ how to reach ~" }, new List<string> { "nearby_architect", "entity" }));
            RecognizedCommands.Add("ask_generic_directions", (new List<string> { "ask ~ where a ~ is", "ask ~ where i can find a ~", "ask ~ where to find a ~", "ask ~ where the nearest ~ is", "ask ~ where i could find a ~", "ask ~ where an ~ is", "ask ~ where i can find an ~", "ask ~ where to find an ~" }, new List<string> { "nearby_architect", "structure_type" }));
            RecognizedCommands.Add("ask_about_something", (new List<string> { "ask ~ about ~", "ask ~ for information on ~", "ask ~ what /p know about ~", "ask ~ what /p can tell me about ~" }, new List<string> { "nearby_architect", "entity" }));
            RecognizedCommands.Add("ask_ruler", (new List<string> { "ask ~ about the government", "ask ~ who rules", "ask ~ who the government is", "ask ~ who rules here", "ask ~ who is in charge", "ask ~ who holds power", "ask ~ who is in power" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_trade", (new List<string> { "ask ~ to trade", "ask ~ trade", "ask ~ to trade with me", "ask ~ what /p have for sale", "ask ~ what /p sell", "ask ~ what /p are selling", "ask ~ if /p want to trade", "ask ~ if /p would like to trade" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_them_join", (new List<string> { "ask ~ to join me", "ask ~ to join us", "ask ~ to join me on my quest", "ask ~ to join my group", "ask ~ to join my party", "ask ~ to come with me", "ask ~ to travel with me", "ask ~ to join our quest", "ask ~ to join my quest" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_me_join", (new List<string> { "ask ~ if i can join /p", "ask ~ if /p would let me join", "ask ~ if /p are accepting members", "ask ~ if i can join /p group", "ask ~ if /p will accept me" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_current_structure", (new List<string> { "ask ~ about this building", "ask ~ about this structure", "ask ~ what this building is", "ask ~ what this structure is", "ask ~ for information about this building", "ask ~ for information about this structure" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_location", (new List<string> { "ask ~ about this location", "ask ~ about this site", "ask ~ about this area", "ask ~ about this region" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_profession", (new List<string> { "ask ~ what /p do", "ask ~ what /p do for a living", "ask ~ about /p job", "ask ~ /p job", "ask ~ what /p profession is", "ask ~ about /p profession", "ask ~ about /p career" , "ask ~ what /p do" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("greet", (new List<string> { "say hello to ~", "greet ~", "say hi to ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("farewell", (new List<string> { "say goodbye to ~", "dismiss ~", "say bye to ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("thank", (new List<string> { "thank ~", "say thank you to ~", "express my gratitude to ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("apologize", (new List<string> { "apologize to ~", "tell ~ i'm sorry", "say sorry to ~", "tell ~ i apologize", "tell ~ i am sorry", "ask ~ for forgiveness" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_health", (new List<string> { "ask ~ how /p are feeling", "ask ~ /p health", "ask ~ how /p feel", "ask ~ about /p health", "ask ~ if /p are well", "ask ~ how /p health is", "ask ~ about /p wellbeing" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_news", (new List<string> { "ask ~ what happened recently", "ask ~ the latest", " ask ~ about recent events", "ask ~ what's new" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_story", (new List<string> { "ask ~ /p story", "ask ~ about /p", "ask ~ about /p history", "ask ~ for /p story", "ask ~ to tell /p story", "ask ~ about /p past", "ask ~ about /p life" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_history", (new List<string> { "ask ~ about the history of ~", "ask ~ what happened to ~", "ask ~ about the story of ~", "ask ~ for the story of ~" }, new List<string> { "nearby_architect", "entity" }));
            RecognizedCommands.Add("ask_opinion", (new List<string> { "ask ~ /p opinion on ~", "ask ~ what /p think of ~", "ask ~ what /p think about ~", "ask ~ /p thoughts on ~", "ask ~ about /p relationship with ~", "ask ~ if /p know ~", "ask ~ for /p perspective on ~", "ask ~ how /p feel about ~", "ask ~ for /p take on ~" }, new List<string> { "nearby_architect", "entity" }));
            RecognizedCommands.Add("ask_interests", (new List<string> { "ask ~ what interests /p", "ask ~ about /p interests", "ask ~ about /p hobbies", "ask ~ what hobbies /p have", "ask ~ what /p enjoy", "ask ~ what /p like to do" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_family", (new List<string> { "ask ~ about /p family", "ask ~ if /p has family", "ask ~ if /p has relatives", "ask ~ about /p relatives" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("challenge", (new List<string> { "challenge ~", "challenge ~ to a fight", "challenge ~ to a duel" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("provide_assistance", (new List<string> { "ask ~ if /p need help", "ask ~ if i can help" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("ask_advice", (new List<string> { "ask ~ for advice on ~", "ask ~ advice on ~", "ask ~ for a job", "ask ~ if /p need any assistance", "ask ~ if /p could use my help", "ask ~ if /p requires help", "ask ~ if /p require help", "ask ~ if /p need assistance", "ask ~ if i can be of service" }, new List<string> { "nearby_architect", "entity" }));
            RecognizedCommands.Add("inform_quest", (new List<string> { "tell ~ about my quest", "tell ~ about my goal", "tell ~ about my mission", "tell ~ my goal", "tell ~ my mission", "tell ~ my quest", "share my quest with ~", "share my goal with ~", "share my mission with ~", "explain my goal to ~", "explain my mission to ~", "inform ~ about my quest", "inform ~ about my mission" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("tell_story_about", (new List<string> { "tell ~ about ~", "tell ~ the story of ~", "tell ~ a story about ~", "share with ~ the story of ~" }, new List<string> { "nearby_architect", "entity" }));
            RecognizedCommands.Add("compliment", (new List<string> { "compliment ~", "say something nice about ~", "praise ~", "flatter ~", "flirt with ~", "commend ~", "admire ~"}, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("insult", (new List<string> { "insult ~", "defame ~", "slander ~", "ridicule ~", "disrespect ~", "mock ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("surrender", (new List<string> { "surrender to ~", "yield to ~", "give up to ~", "concede to ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("demand_surrender", (new List<string> { "demand ~ surrender", "demand ~ to surrender", "request ~ surrender", "ask ~ to surrender", "order ~ to surrender", "command ~ to surrender", "tell ~ to surrender" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("demand_item", (new List<string> { "demand from ~ a ~", "demand ~ drop ~", "demand ~ to drop ~", "tell ~ to surrender ~", "tell ~ to drop ~", "tell ~ to drop a ~" }, new List<string> { "nearby_architect", "nearby_architect_object" }));

            // Add all the above ones to RecognizedMessages

            foreach (var kvp in RecognizedCommands)
            {
                RecognizedMessages[kvp.Key] = kvp.Value;
            }

            RecognizedCommands.Add("go_prone", (new List<string> { "go prone", "fall ~", "go on the ground", "get on the ground", "fall over" }, new List<string> {}));
            RecognizedCommands.Add("stand_up", (new List<string> { "stand ~", "get ~ off the ground", "get off the ground" }, new List<string> { }));
            RecognizedCommands.Add("leave_structure", (new List<string> { "leave ~", "exit ~", "leave the structure", "exit the structure", "leave", "leave the building", "exit the building", "exit" }, new List<string> { }));
            RecognizedCommands.Add("enter", (new List<string> { "enter ~", "go inside ~", "go in ~", "go into ~", "go through ~" }, new List<string> { "enterable" }));
            RecognizedCommands.Add("inventory_check", (new List<string> { "check my inventory", "open my inventory", "open my pack", "open my backpack", "search my backpack", "open pack", "open menu", "show menu", "open inventory", "access menu", "display menu", "main menu", "game menu", "menu", "menu open", "open game menu", "show main menu", "access main menu", "menu screen" }, new List<string> { }));
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
    "shove at ~", "slap at ~", "jab at ~", "attack at ~", "attack ~"
}, new List<string> { "nearby_architect" }));

            RecognizedCommands.Add("attack_with_weapon", (new List<string> {
    "slash ~ with ~", "stab ~ with ~", "pierce ~ with ~", "lash ~ with ~",
    "scourge ~ with ~", "whip ~ with ~", "strike ~ with ~", "bash ~ with ~",
    "crush ~ with ~", "whack ~ with ~", "smash ~ with ~", "hack ~ with ~",
    "impale ~ with ~", "cut ~ with ~", "slice ~ with ~", "bludgeon ~ with ~",
    "club ~ with ~", "punch ~ with ~", "kick ~ with ~", "headbutt ~ with ~",
    "shove ~ with ~", "slap ~ with ~", "jab ~ with ~", "attack ~ with ~",
}, new List<string> { "nearby_architect", "hand_object" }));

            RecognizedCommands.Add("attack_specific_body_part", (new List<string> {
    "slash ~ in the ~", "stab ~ in the ~", "pierce ~ in the ~", "lash ~ in the ~",
    "scourge ~ in the ~", "whip ~ in the ~", "strike ~ in the ~", "bash ~ in the ~",
    "crush ~ in the ~", "whack ~ in the ~", "smash ~ in the ~", "hack ~ in the ~",
    "impale ~ in the ~", "cut ~ in the ~", "slice ~ in the ~", "bludgeon ~ in the ~",
    "club ~ in the ~", "punch ~ in the ~", "kick ~ in the ~", "headbutt ~ in the ~",
    "shove ~ in the ~", "slap ~ in the ~", "jab ~ in the ~", "attack ~ in the ~"
}, new List<string> { "nearby_architect", "body_part_type" }));

            RecognizedCommands.Add("attack_body_part_with_item", (new List<string> {
    "slash ~ in the ~ with ~", "stab ~ in the ~ with ~", "pierce ~ in the ~ with ~",
    "lash ~ in the ~ with ~", "scourge ~ in the ~ with ~", "whip ~ in the ~ with ~",
    "strike ~ in the ~ with ~", "bash ~ in the ~ with ~", "crush ~ in the ~ with ~",
    "whack ~ in the ~ with ~", "smash ~ in the ~ with ~", "hack ~ in the ~ with ~",
    "impale ~ in the ~ with ~", "cut ~ in the ~ with ~", "slice ~ in the ~ with ~",
    "bludgeon ~ in the ~ with ~", "club ~ in the ~ with ~", "punch ~ in the ~ with ~",
    "kick ~ in the ~ with ~", "headbutt ~ in the ~ with ~", "shove ~ in the ~ with ~",
    "slap ~ in the ~ with ~", "jab ~ in the ~ with ~", "attack ~ in the ~ with ~"
}, new List<string> { "nearby_architect", "body_part_type", "hand_object" }));

            RecognizedCommands.Add("become_invisible", (new List<string> { "become one with shadow", "become one with the shadow" }, new List<string> { }));
            RecognizedCommands.Add("exit_invisibility", (new List<string> { "exit the shadow", "exit the darkness", "return from the shadow", "return from shadow" }, new List<string> { }));
            //RecognizedCommands.Add("level_up", (new List<string> { "level ~" }, new List<string> { "direction" }));
            RecognizedCommands.Add("engage_target", (new List<string> { "engage ~", "engage with ~", "confront ~", "focus ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("approach_target", (new List<string> { "approach ~", "move closer to ~", "advance towards ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("distance_from_target", (new List<string> { "distance from ~", "move away from ~", "retreat from ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("wield_item", (new List<string> { "take out ~", "unsheath ~", "remove ~", "wield ~", "unholster ~" }, new List<string> { "non_hand_inventory" }));
            RecognizedCommands.Add("pick_up_item", (new List<string> { "grab ~", "get ~", "take ~", "pick ~ ~", "steal ~"}, new List<string> { "nearby_object" }));
            RecognizedCommands.Add("drop_item", (new List<string> { "drop ~", "set ~ on the ground", "place ~ on the ground", "let go of ~" }, new List<string> { "inventory" }));
            RecognizedCommands.Add("place_item_in", (new List<string> { "place ~ in ~", "store ~ in ~", "stash ~ in ~", "put ~ in ~", "place ~ on ~", "place ~ inside ~" }, new List<string> { "inventory", "object" }));
            RecognizedCommands.Add("craft", (new List<string> { "craft", "build", "construct", "create", "forge", "sew", "assemble", "manufacture", "fabricate", "design", "knit", "weave", "shape", "mold", "sculpt", "form", "fashion" }, new List<string> { }));
            RecognizedCommands.Add("take_item_from", (new List<string> { "take ~ from ~", "remove ~ from ~", "retrieve ~ from ~", "get ~ from ~", "extract ~ from ~", "take ~ off of ~", "remove ~ off of ~", "retrieve ~ off of ~", "get ~ off of ~"}, new List<string> { "object", "object" }));
            RecognizedCommands.Add("wear_item", (new List<string> { "wear ~", "put on ~", "don ~" }, new List<string> { "inventory" }));
            RecognizedCommands.Add("remove_worn_item", (new List<string> { "remove ~", "take off ~", "doff ~" }, new List<string> { "clothing" }));
            RecognizedCommands.Add("examine", (new List<string> { "examine ~", "look at ~", "check out ~", "look closer at ~", "inspect ~", "observe ~", "view ~" }, new List<string> { "nearby_target" }));
            RecognizedCommands.Add("give_item", (new List<string> { "give ~ to ~", "offer ~ to ~", "sacrifice ~ to ~" }, new List<string> { "object", "nearby_target" }));
            RecognizedCommands.Add("throw_item", (new List<string> { "throw ~", "toss ~", "fling ~", "hurl ~", "sling ~", "lob ~" }, new List<string> { "hand_object" }));
            RecognizedCommands.Add("throw_item_at", (new List<string>
{
    "throw ~ at ~", "toss ~ at ~", "fling ~ at ~", "throw ~ towards ~", "toss ~ towards ~", "fling ~ towards ~",
    "hurl ~ at ~", "hurl ~ towards ~",
    "sling ~ at ~", "sling ~ towards ~",
    "lob ~ at ~", "lob ~ towards ~",
    "pitch ~ at ~", "pitch ~ towards ~",
    "chuck ~ at ~", "chuck ~ towards ~",
    "launch ~ at ~", "launch ~ towards ~",
    "heave ~ at ~", "heave ~ towards ~"
}, new List<string> { "hand_object", "nearby_target" }));

            RecognizedCommands.Add("cast_spell_at_1", (new List<string> { "cast ~ at ~" }, new List<string> { "spell", "nearby_target" }));
            RecognizedCommands.Add("cast_spell_at_2", (new List<string> { "cast ~ at ~ and ~" }, new List<string> { "spell", "nearby_target", "nearby_target" }));
            RecognizedCommands.Add("cast_spell_at_3", (new List<string> { "cast ~ at ~, ~, and ~", "cast ~ at ~ and ~ and ~" }, new List<string> { "spell", "nearby_target", "nearby_target", "nearby_target" }));
            RecognizedCommands.Add("cast_spell_at_4", (new List<string> { "cast ~ at ~, ~, ~, and ~", "cast ~ at ~ and ~ and ~ and ~" }, new List<string> { "spell", "nearby_target", "nearby_target", "nearby_target", "nearby_target" }));
            RecognizedCommands.Add("cast_spell_at_5", (new List<string> { "cast ~ at ~, ~, ~, ~, and ~", "cast ~ at ~ and ~ and ~ and ~ and ~" }, new List<string> { "spell", "nearby_target", "nearby_target", "nearby_target", "nearby_target", "nearby_target" }));
            RecognizedCommands.Add("cast_spell", (new List<string> { "cast ~" }, new List<string> { "spell" }));

            RecognizedCommands.Add("consume", (new List<string> { "consume ~", "apply ~", "eat ~", "drink ~", "ingest ~", "swallow ~", "devour ~", "use ~", "administer ~" }, new List<string> { "inventory" }));
            RecognizedCommands.Add("ascend", (new List<string> { "ascend" }, new List<string> { }));
            RecognizedCommands.Add("claim_structure", (new List<string> { "claim this building", "claim this structure", "claim this"}, new List<string> { }));



            RecognizedCommands.Add("recall_information", (new List<string> { "remember ~", "list ~", "list all ~", "list my ~", "list learned ~", "display ~", "show all ~" }, new List<string> { "rememberance" }));
            RecognizedCommands.Add("ditch_inventory", (new List<string> { "ditch my inventory", "drop everything", "clear my inventory", "discard all items", "empty my pockets", "drop my inventory" }, new List<string> { }));
            RecognizedCommands.Add("read_object", (new List<string> { "read ~" }, new List<string> { "inventory" }));
            RecognizedCommands.Add("perform_composition", (new List<string> { "recite ~", "sing ~", "perform ~" }, new List<string> { "known_compositions" }));
            RecognizedCommands.Add("write_composition", (new List<string> { "write ~", "write a ~", "write an ~", "compose ~", "compose a ~", "compose an ~" }, new List<string> { "composition_types" }));
            RecognizedCommands.Add("write_about_topic", (new List<string> { "write ~ about ~", "write a ~ about ~", "write an ~ about ~", "compose ~ about ~", "compose a ~ about ~", "compose an ~ about ~", "write ~ on ~", "write a ~ on ~", "write an ~ on ~", "compose ~ on ~", "compose a ~ on ~", "compose an ~ on ~" }, new List<string> { "composition_types", "entity" }));
            RecognizedCommands.Add("tame_creature", (new List<string> { "tame ~", "pacify ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("starstrike", (new List<string> { "starstrike ~" }, new List<string> { "nearby_target" }));
            RecognizedCommands.Add("flamestrike", (new List<string> { "flamestrike ~" }, new List<string> { "nearby_target" }));
            RecognizedCommands.Add("heat_object", (new List<string> { "sear ~" }, new List<string> { "hand_object" }));
            RecognizedCommands.Add("starsmite", (new List<string> { "starsmite ~" }, new List<string> { "nearby_target" }));
            RecognizedCommands.Add("conjure_spark", (new List<string> { "conjure spark" }, new List<string> {  }));
            RecognizedCommands.Add("evoke_strike", (new List<string> { "evoke strike at ~", "evoke beam at ~", "evoke beams at ~", "evoke light at ~" }, new List<string> { "nearby_target" }));
            RecognizedCommands.Add("evoke_blindness", (new List<string> { "evoke blindness" }, new List<string> { }));
            RecognizedCommands.Add("evoke_nexus", (new List<string> { "evoke nexus" }, new List<string> { }));
            RecognizedCommands.Add("evoke_healing", (new List<string> { "evoke healing" }, new List<string> { }));
            RecognizedCommands.Add("inflame", (new List<string> { "inflame" }, new List<string> { }));
            RecognizedCommands.Add("unflame", (new List<string> { "unflame" }, new List<string> { }));
            RecognizedCommands.Add("augment_creature", (new List<string> { "augment ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("raise_dead", (new List<string> { "raise ~" }, new List<string> { "corpse" }));
            RecognizedCommands.Add("fire_spectral_bolt", (new List<string> { "spectralize ~", "spectral bolt ~" }, new List<string> { "nearby_target" }));
            RecognizedCommands.Add("increase_weight", (new List<string> { "increase weight of ~" }, new List<string> { "inventory" }));
            RecognizedCommands.Add("increase_temperature", (new List<string> { "increase temperature of ~" }, new List<string> { "inventory" }));
            RecognizedCommands.Add("increase_aerodynamics", (new List<string> { "increase aerodynamics of ~" }, new List<string> { "inventory" }));
            RecognizedCommands.Add("increase_integrity", (new List<string> { "increase integrity of ~" }, new List<string> { "inventory" }));
            RecognizedCommands.Add("decrease_weight", (new List<string> { "decrease weight of ~" }, new List<string> { "inventory" }));
            RecognizedCommands.Add("decrease_temperature", (new List<string> { "decrease temperature of ~" }, new List<string> { "inventory" }));
            RecognizedCommands.Add("decrease_aerodynamics", (new List<string> { "decrease aerodynamics of ~" }, new List<string> { "inventory" }));
            RecognizedCommands.Add("decrease_integrity", (new List<string> { "decrease integrity of ~" }, new List<string> { "inventory" }));
            RecognizedCommands.Add("liquify", (new List<string> { "liquify ~" }, new List<string> { "nearby_super_target" }));
            RecognizedCommands.Add("split", (new List<string> { "split ~" }, new List<string> { "nearby_object" }));
            RecognizedCommands.Add("blip", (new List<string> { "blip ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("use_skill", (new List<string> { "initiate ~", "use skill ~", "use ~ skill"}, new List<string> { "skill" }));
            RecognizedCommands.Add("reposition", (new List<string> { "reposition", "reset" }, new List<string> { "none" }));
            RecognizedCommands.Add("retract", (new List<string> { "retract ~", "pull back ~", "reposition ~" }, new List<string> { "body_part_type" }));
            RecognizedCommands.Add("free", (new List<string> { "free ~", "unbound ~", "release ~" }, new List<string> { "nearby_architect" }));
            RecognizedCommands.Add("fix_hair", (new List<string> { "fix hair", "fix my hair", "fix flames", "fix my flames"}, new List<string> { }));
            RecognizedCommands.Add("pet", (new List<string> { "pet ~", "rub ~" }, new List<string> { "nearby_architect" }));

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
                { "go_prone", new string[] { "movement" } },
                { "stand_up", new string[] { "movement" } },

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
                { "pet", new string[] { "utility" } },


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
                { "consume", new string[] { "items" } },


                // Creativity
                { "perform_composition", new string[] { "creativity" } },
                { "write_composition", new string[] { "creativity" } },
                { "write_about_topic", new string[] { "creativity" } },
                { "craft", new string[] { "creativity" } },
                { "fix_hair", new string[] { "creativity" } },

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
                { "shiba", "entertainment"},
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
                { "duelist", "military" },
                { "thief", "none" },
                { "archmage", "scholarly" },
                { "beastmaster", "none" },
                { "gardener", "none"},
                { "druidcrafter", "none"},
                { "archdruid", "none"},
                { "salvager", "none"},
                { "constructor", "none"},
                { "scraplord", "none"},
                { "cultist", "none"},
                { "intermediary", "none"},
                { "swashbuckler", "none"},
                { "deadeye", "none"},
                { "captain", "none"},
                { "disruptor", "none"},
                { "bomber", "none"},
                { "inspiration", "none"}
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
                {"shiba", "from being beautiful"},
                {"scholar", "from sharing your studies"},
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
                {"enchanter", "from selling great enchantment"},
                { "gardener", "from your dedication to nature" },
                { "druidcrafter", "from your druidic practices" },
                { "archdruid", "from leading the druidic order" },
                { "salvager", "from salvaging valuable materials" },
                { "constructor", "from constructing various items" },
                { "scraplord", "from ruling the scrapyard" },
                { "cultist", "from serving the dark powers" },
                { "intermediary", "from mediating between worlds" },
                { "swashbuckler", "from your daring exploits" },
                { "deadeye", "from your impeccable aim" },
                { "captain", "from leading your crew" },
                { "disruptor", "from causing chaos" },
                { "bomber", "from your explosive tactics" },
                { "inspiration", "from inspiring rebellion" }
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
                {"sovereign", "core"},
                {"heart", "heart"},
                {"shiba", "tavern"},
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
                {"enchanter", "shrine"},
                {"gardener", "none"},
                {"druidcrafter", "none"},
                {"archdruid", "none"},
                {"salvager", "none"},
                {"constructor", "none"},
                {"scraplord", "none"},
                {"cultist", "none"},
                {"intermediary", "none"},
                {"swashbuckler", "none"},
                {"deadeye", "none"},
                {"captain", "none"},
                {"disruptor", "none"},
                {"bomber", "none"},
                {"inspiration", "none"},
                {"curator", "scaffold"},
                {"artist", "scaffold"},
                {"manager", "scaffold"},
                {"perfectionist", "scaffold"},
                {"cluster", "scaffold"},
                {"brute", "scaffold"}
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
                {"cook", "cooking"},
                {"eating", "eating"},
                {"industry", "working"},
                {"contemplate", "contemplating"},
                {"sentinel", "sentinel"},
                {"bound", "bound"},
                {"study", "studying"},
                {"druidcrafting", "druidcrafting"},
                {"", "idle"},
                {"oversight", "overseeing"},
                {"meditation", "meditating"},
                {"polishing", "polishing"},
                {"vibrating", "vibrating"},
                {"pumping", "pumping"},
                {"convulsing", "convulsing"},
                {"sculpting", "sculpting"},
                {"engraving", "engraving"},
                {"radiating", "radiating"}
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

            FrameCounter = new FrameCounter();
            GameInput = new GameInput();
            base.Initialize();
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

            if(text != "")
            {
                // Append text to your prompt or handle it accordingly
                MostRecentPartyTurnArchitect.Prompt += text + " "; //adding a space seems to be the best approach, because we take it off at the end anyway
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            ContentRoot = Content.RootDirectory;

            ContentPath = Path.GetFullPath("Content");
            string dataPath = string.Concat(ContentPath, "/data/");

            FirstNames = File.ReadAllLines(string.Concat(dataPath, "names.txt")).ToList();
            LastNames = File.ReadAllLines(string.Concat(dataPath, "last-names.txt")).ToList();
            Words = File.ReadAllLines(string.Concat(dataPath, "words.txt")).ToList();
            Syllables = File.ReadAllLines(string.Concat(dataPath, "syllables.txt")).ToList();
            NameSuffixes = File.ReadAllLines(string.Concat(dataPath, "namesuffixes.txt")).ToList();

            FrameT = Content.Load<Texture2D>("gui/frame");
            MoveMapFrameT = Content.Load<Texture2D>("gui/moveframe");
            SpeakingT = Content.Load<Texture2D>("icons/speaking");
            QuitGuiT = Content.Load<Texture2D>("other/quitgui");

            DePixelLarge = Content.Load<SpriteFont>("spritefonts/shibafont2");
            DePixelSmall = Content.Load<SpriteFont>("spritefonts/babyshibafont2");
            BlackChanceryLarge = Content.Load<SpriteFont>("spritefonts/shibafont");
            BlackChancerySmall = Content.Load<SpriteFont>("spritefonts/babyshibafont");

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

            DistrictBastionT = Content.Load<Texture2D>("distmap/bastion");
            DistrictFortT = Content.Load<Texture2D>("distmap/fort");

            TitleScreen = Content.Load<Texture2D>("other/title");
            EmptyTileT = Content.Load<Texture2D>("tiles/emptytile");
            GUI = Content.Load<Texture2D>("gui/gui");
            HelpGUI = Content.Load<Texture2D>("gui/helpgui");
            QuickStartGUI = Content.Load<Texture2D>("gui/gettingstarted");
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

            BastionT = Content.Load<Texture2D>("locationtiles/bastion");
            TileAtlas.Add("bastion", BastionT);
            FortT = Content.Load<Texture2D>("locationtiles/fort");
            TileAtlas.Add("fort", FortT);

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
            BetterCursorT = Content.Load<Texture2D>("other/cursor");
            GuideT = Content.Load<Texture2D>("icons/moveguide");
            HealthGuiT = Content.Load<Texture2D>("icons/healthgui");

            SanctumT = Content.Load<Texture2D>("locationtiles/sanctum");
            TileAtlas.Add("sanctum", SanctumT);

            ArchitectHere = Content.Load<Texture2D>("distmap/architecthere");
            BleedT = Content.Load<Texture2D>("icons/droplet");

            whiteRect = Content.Load<Texture2D>("other/pixel");
            ReactionGUIT = Content.Load<Texture2D>("gui/reaction gui");
            ReactionGUIHelpT = Content.Load<Texture2D>("gui/reaction gui help");
            MessageGUIT = Content.Load<Texture2D>("gui/messageGUI");
            CmdHelpT = Content.Load<Texture2D>("gui/cmdhelp");
            LightrealmMainTheme = Content.Load<Song>("audio/lightrealm main theme (2023)");
            Introspection = Content.Load<Song>("audio/introspection");

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
            CharacterAtlas["brassiere"] = UpperGarmentT = Content.Load<Texture2D>("character art/uppergarment");
            CharacterAtlas["uppershirt female"] = UpperShirtFemaleT = Content.Load<Texture2D>("character art/uppershirt female");
            CharacterAtlas["uppershirt male"] = UpperShirtMaleT = Content.Load<Texture2D>("character art/uppershirt male");
            CharacterAtlas["wraps"] = WrapsT = Content.Load<Texture2D>("character art/wraps");

            VibrantBlackT = Content.Load<Texture2D>("character art/vibrant_black");
            WindsweptBlackT = Content.Load<Texture2D>("character art/windswept_black");
            DiminishedBlackT = Content.Load<Texture2D>("character art/diminished_black");
            SwirlBlackT = Content.Load<Texture2D>("character art/swirl_black");
            VibrantWhiteT = Content.Load<Texture2D>("character art/vibrant_light");
            WindsweptWhiteT = Content.Load<Texture2D>("character art/windswept_light");
            DiminishedWhiteT = Content.Load<Texture2D>("character art/diminished_light");
            SwirlWhiteT = Content.Load<Texture2D>("character art/swirl_light");
            VibrantGrayT = Content.Load<Texture2D>("character art/vibrant_gray");
            WindsweptGrayT = Content.Load<Texture2D>("character art/windswept_gray");
            DiminishedGrayT = Content.Load<Texture2D>("character art/diminished_gray");
            SwirlGrayT = Content.Load<Texture2D>("character art/swirl_gray");

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
            if(GameInput != null && FrameCounter != null)
            {
                GameInput.Update();

                if (GameInput.WasKeyPressed(FrameCounter.Key))
                {
                    FrameCounter.RenderFps = !FrameCounter.RenderFps;
                }
                FrameCounter.Update(gameTime);

            }

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


            FlashTick++;
            if (FlashTick > 99)
            {
                FlashTick = 0;
            }


            //BEFORE WE DO ANYTHING, FIX THE LOADED ARCHITECT THINGY


            void IncrementAndCycleWorld()
            {
                ArchitectIndex++;

                if (ArchitectIndex >= LoadedArchitects.Count())
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

                if (GameWorld.GamePlayerAssociation.ActiveParty.Architects.All(architect => !architect.IsAlive))
                {
                    GameState = "dead";
                }
                else if (LoadedArchitects.Count() == 1)
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
                if(LoadedArchitects.Count() == 0)
                {
                    return;
                }
                void HandleThrownObjects(EntityList<Object> objects, EntityList<Architect> architects)
                {
                    EntityList<Object> objectsToRemove = new EntityList<Object>();

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
                                        targetArchitect.AnnounceToParty(targetArchitect.ReferredToNames[0] + " is enveloped in souls!", Color.Orange, new EntityList<Entity>() { targetArchitect });

                                        if (targetArchitect.Energy <= 0)
                                        {
                                            targetArchitect.IsAlive = false;
                                            targetArchitect.RaiseFromTheDead((Architect)o.Creator, targetArchitect.ReferredToNames[0], ((Architect)o.Creator).PathOfDeathLevel, 2);

                                            if (targetArchitect.IsAlive && !Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(targetArchitect))
                                            {
                                                Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Add(targetArchitect);
                                            }
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

                        // Check if the object should dissipate instead of dropping on the ground
                        if ((o.Materials[0].Type == "metaphysic" || o.Dissipating) && o.Type != "spark")
                        {
                            objectsToRemove.Add(o);
                        }

                        if(objects.Count == 0)
                        {
                            break;
                        }
                    }

                    foreach (var obj in objectsToRemove)
                    {
                        objects.Remove(obj);
                    }
                }



                void HandleGrenadeEffects(Object o, EntityList<Architect> architects, EntityList<Object> objects, Random r)
                {
                    if (o.Type == "spatial grenade")
                    {
                        MakeObservation("The grenade explodes into a portal, sucking everything in!", Color.Purple, new EntityList<Entity>());

                        List<Architect> architectsToRemove = new List<Architect>();

                        foreach (Architect a in architects)
                        {
                            a.IsAlive = false;
                            if (GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a))
                            {
                                architectsToRemove.Add(a);

                                if (GameWorld.GamePlayerAssociation.ActiveParty.Architects.Count() == 0)
                                {
                                    GameState = "dead";
                                }
                            }
                            MakeObservation(a.ReferredToNames[0] + " has been consumed by the portal.", Color.Purple, new EntityList<Entity>() { a });
                        }

                        // Remove the architects after the loop
                        foreach (Architect a in architectsToRemove)
                        {
                            GameWorld.GamePlayerAssociation.ActiveParty.Architects.Remove(a);
                        }

                        objects.Clear();
                        architects.Clear();
                    }

                    else if (o.Type == "lightning grenade")
                    {
                        MakeObservation("The grenade explodes into a swarm of lightning, striking everything around!", Color.Cyan, new EntityList<Entity>());
                        foreach (Architect a in architects)
                        {
                            a.UnconsciousCycles = 1000;
                            if (GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a))
                            {
                                if (GameWorld.GamePlayerAssociation.ActiveParty.Architects.Count() == 0)
                                {
                                    GameState = "dead";
                                }
                            }
                            MakeObservation(a.ReferredToNames[0] + " goes completely unconscious.", Color.Purple, new EntityList<Entity>() { a });
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
                        target.ChangeOpinion(o.Thrower, -75);

                        Object ArchitectBodyPart = target.BodyParts[r.Next(target.BodyParts.Count())];

                        int InitialStagnantObjectIntegrity = ArchitectBodyPart.Integrity;
                        int InitialThrowingObjectIntegrity = o.Integrity;

                        EntityList<TextStorage> ListText = ArchitectBodyPart.TakeDamageFromObject(o, 2 * (o.Thrower.Dexterity + o.Thrower.GetProficiency("throwing")), null, "");

                        foreach(TextStorage t in ListText)
                        {
                            target.AnnounceToParty(t.Data, t.Color, t.Entities);
                        }

                        if (o.Type == "star" && ((Architect)(o.Creator)).PathOfStarsLevel > 4)
                        {
                            target.AnnounceToParty(ArchitectBodyPart.ReferredToNames[0] + " bursts into flames!", Color.Orange, new EntityList<Entity>() { ArchitectBodyPart });
                            target.FireSeconds += r.Next(2, 5);
                        }
                        else if (o.Materials.Contains(GameWorld.Flame))
                        {
                            target.AnnounceToParty(ArchitectBodyPart.ReferredToNames[0] + " bursts into flames!", Color.Orange, new EntityList<Entity>() { ArchitectBodyPart });
                            if (((Architect)(o.Creator)).PathOfHeatLevel >= 8 && ((Architect)(o.Creator)).FireSeconds > 0)
                            {
                                target.FireSeconds += r.Next(2, 5 + (((Architect)(o.Creator)).FireSeconds));
                            }
                            else if (((Architect)(o.Creator)).PathOfHeatLevel >= 6)
                            {
                                target.FireSeconds += r.Next(2, 5);
                            }
                            else
                            {
                                target.FireSeconds += r.Next(0, 4);
                            }
                        }

                        if (o.Integrity < 0 && InitialThrowingObjectIntegrity > 0)
                        {
                            target.AnnounceToParty("The " + o.ReferredToNames[0] + " has been destroyed!", Color.Orange, new EntityList<Entity>() { o });
                        }
                        if (ArchitectBodyPart.Integrity < 0 && InitialStagnantObjectIntegrity > 0)
                        {
                            target.AnnounceToParty("The " + ArchitectBodyPart.ReferredToNames[0] + " has been destroyed!", Color.Orange, new EntityList<Entity>() { ArchitectBodyPart });
                        }
                    }
                }

                void HandleObjectCollision(Object o, Random r)
                {
                    if (o.AirborneTarget is Object targetObject)
                    {
                        int InitialStagnantObjectIntegrity = targetObject.Integrity;
                        int InitialThrowingObjectIntegrity = o.Integrity;

                        o.Integrity = InitialThrowingObjectIntegrity - InitialStagnantObjectIntegrity;
                        targetObject.Integrity = InitialThrowingObjectIntegrity - InitialStagnantObjectIntegrity;

                        o.Thrower.AnnounceToParty("The " + o.ReferredToNames[0] + " has collided into " + targetObject.ReferredToNames[0] + "!", Color.Orange, new EntityList<Entity>() { o, targetObject });
                        
                        EntityList<TextStorage> T = targetObject.TakeDamageFromObject(o, o.Thrower.GetProficiency("throwing") + 3, null, ""); // Simulating damage application

                        foreach(TextStorage t in T)
                        {
                            o.Thrower.AnnounceToParty(t.Data, t.Color, t.Entities);
                        }

                        if (o.Integrity < 0 && InitialThrowingObjectIntegrity > 0)
                        {
                            o.Thrower.AnnounceToParty("The " + o.ReferredToNames[0] + " has been destroyed!", Color.Orange, new EntityList<Entity>() { o });
                        }
                        if (targetObject.Integrity < 0 && InitialStagnantObjectIntegrity > 0)
                        {
                            o.Thrower.AnnounceToParty("The " + targetObject.ReferredToNames[0] + " has been destroyed!", Color.Orange, new EntityList<Entity>() { targetObject });
                        }

                        if(targetObject.IsBodyPart && targetObject.Owner != null)
                        {
                            ((Architect)(targetObject.Owner)).CombatCycles = 100;
                            ((Architect)(o.Thrower)).CombatCycles = 100;
                            ((Architect)(targetObject.Owner)).ChangeOpinion(o.Thrower, -75);
                        }
                    }
                }

                // Iterate through blocks
                for (int x = 0; x < 7; x++)
                {
                    for (int z = 0; z < 7; z++)
                    {
                        var block = LoadedArchitects[ArchitectIndex].District.DistrictMap[x + z * 7];
                        HandleThrownObjects(block.Objects, block.Architects);

                        foreach(Object o in block.Objects)
                        {
                            o.UpdateSelfActionsAndSuch();
                        }
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

                            foreach(Object o in room.Objects)
                            {
                                o.UpdateSelfActionsAndSuch();
                            }
                        }
                    }
                }


                //return fractal objects to reality/update them

                EntityList<Object> FractalObjectsToRemove = new EntityList<Object>();

                foreach (Object o in GameWorld.FractalObjects)
                {
                    o.FractalCycles--;
                    if (o.FractalCycles < 1 && o.RematerializeLocation.Item3.IsLoaded)
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
                            MakeObservation(o.ReferredToNames[0] + " has rematerialized!", Color.Blue, new EntityList<Entity>() { o });
                        }

                        FractalObjectsToRemove.Add(o);
                    }
                }
                foreach (Object o in FractalObjectsToRemove)
                {
                    GameWorld.FractalObjects.Remove(o);
                }
                EntityList<Architect> FractalArchitectsToRemove = new EntityList<Architect>();
                foreach (Architect a in GameWorld.FractalArchitects)
                {
                    a.FractalCycles--;

                    if (a.FractalCycles < 1 && a.RematerializeLocation.Item3.IsLoaded)
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
                                MakeObservation(a.ReferredToNames[0] + " has rematerialized!", Color.Blue, new EntityList<Entity>() { a });
                            }
                        }
                        else
                        {
                            a.Location = a.RematerializeLocation.Item2;
                            a.District = a.RematerializeLocation.Item3;
                            a.District.Architects.Add(a);
                        }

                        if(!LoadedArchitects.Contains(a))
                        {
                            LoadedArchitects.Add(a);
                        }
                    }
                }
                foreach (Architect a in FractalArchitectsToRemove)
                {
                    GameWorld.FractalArchitects.Remove(a);
                }


                //change the random int
                Game1.GameWorld.ReactionModifierInt = r.Next(0, 100000);
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

            if (GameState == "mainscreen" || GameState == "generatingworld" || GameState == "worldgenscreen" || GameState == "placecivilizations" || GameState == "loadinggamemenu" || GameState == "savinggamemenu" || GameState == "generatehistory" || GameState == "choosepreferences" || GameState == "findstartlocation" || GameState == "architectfound" || GameState == "viewhistory")
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

                if (KeysNewlyPressed.Contains(Keys.M) && GameState != "viewhistory")
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
            else if (GameState == "partyturn" || GameState == "travelmenu" || GameState == "ascendant")
            {
                if (MediaPlayer.Volume > 0 && !(MediaPlayer.Queue.ActiveSong == Introspection))
                {
                    MediaPlayer.Volume = MediaPlayer.Volume - 0.004F;
                }
            }

            void SetUpRelevantEntities(string subject)
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
                    case "structure_type":
                        var structures = RelevantEntities.OfType<Structure>();
                        var uniqueStructureTypes = structures.Select(s => s.Type).Distinct();
                        var newEntities = uniqueStructureTypes.Select(type => new Entity(type));
                        RelevantEntities.AddRange(newEntities);
                        break;
                    case "nearby_object":
                        RelevantEntities.AddRange(AllSubjects.OfType<Object>().Where(o => MostRecentPartyTurnArchitect.Block.Objects.Contains(o) || (MostRecentPartyTurnArchitect.Room != null && MostRecentPartyTurnArchitect.Room.Objects.Contains(o))));
                        break;
                    case "nearby_target":
                        RelevantEntities.AddRange(AllSubjects.OfType<Architect>().Where(a => MostRecentPartyTurnArchitect.Block.Architects.Contains(a) || (MostRecentPartyTurnArchitect.Room != null && MostRecentPartyTurnArchitect.Room.Architects.Contains(a))));
                        RelevantEntities.AddRange(AllSubjects.OfType<Object>().Where(o => MostRecentPartyTurnArchitect.Block.Objects.Contains(o) || (MostRecentPartyTurnArchitect.Room != null && MostRecentPartyTurnArchitect.Room.Objects.Contains(o))));
                        if (MostRecentPartyTurnArchitect.OffHeldObject != null)
                        {
                            RelevantEntities.Add(MostRecentPartyTurnArchitect.OffHeldObject);
                        }
                        if (MostRecentPartyTurnArchitect.MainHeldObject != null)
                        {
                            RelevantEntities.Add(MostRecentPartyTurnArchitect.MainHeldObject);
                        }
                        RelevantEntities.AddRange(MostRecentPartyTurnArchitect.Inventory);
                        break;
                    case "nearby_structure":
                        RelevantEntities.AddRange(AllSubjects.OfType<Structure>().Where(s => MostRecentPartyTurnArchitect.Block.Structures.Contains(s)));
                        break;
                    case "nearby_super_target":
                        RelevantEntities.AddRange(AllSubjects.OfType<Architect>().Where(a => MostRecentPartyTurnArchitect.Block.Architects.Contains(a) || (MostRecentPartyTurnArchitect.Room != null && MostRecentPartyTurnArchitect.Room.Architects.Contains(a))));

                        RelevantEntities.AddRange(AllSubjects.OfType<Object>().Where(o => MostRecentPartyTurnArchitect.Block.Objects.Contains(o) || (MostRecentPartyTurnArchitect.Room != null && MostRecentPartyTurnArchitect.Room.Objects.Contains(o))));

                        if (MostRecentPartyTurnArchitect.OffHeldObject != null)
                        {
                            RelevantEntities.Add(MostRecentPartyTurnArchitect.OffHeldObject);
                        }
                        if (MostRecentPartyTurnArchitect.MainHeldObject != null)
                        {
                            RelevantEntities.Add(MostRecentPartyTurnArchitect.MainHeldObject);
                        }
                        RelevantEntities.AddRange(MostRecentPartyTurnArchitect.Inventory);
                        RelevantEntities.AddRange(AllSubjects.OfType<Structure>().Where(s => MostRecentPartyTurnArchitect.Block.Structures.Contains(s)));
                        break;

                    case "composition_types":
                        RelevantEntities.AddRange(AllSubjects.Where(e => e.Metadata == "book" || e.Metadata == "song" || e.Metadata == "poem"));
                        break;
                    case "known_compositions":
                        RelevantEntities.AddRange(MostRecentPartyTurnArchitect.CultureBank);
                        break;
                    case "hand_object":
                        if (MostRecentPartyTurnArchitect.OffHeldObject != null)
                        {
                            RelevantEntities.Add(MostRecentPartyTurnArchitect.OffHeldObject);
                        }
                        if (MostRecentPartyTurnArchitect.MainHeldObject != null)
                        {
                            RelevantEntities.Add(MostRecentPartyTurnArchitect.MainHeldObject);
                        }
                        break;
                    case "non_hand_inventory":
                        RelevantEntities.AddRange(MostRecentPartyTurnArchitect.Inventory);
                        break;
                    case "inventory":
                        if (MostRecentPartyTurnArchitect.OffHeldObject != null)
                        {
                            RelevantEntities.Add(MostRecentPartyTurnArchitect.OffHeldObject);
                        }
                        if (MostRecentPartyTurnArchitect.MainHeldObject != null)
                        {
                            RelevantEntities.Add(MostRecentPartyTurnArchitect.MainHeldObject);
                        }
                        RelevantEntities.AddRange(MostRecentPartyTurnArchitect.Inventory);
                        break;
                    case "corpse":
                        RelevantEntities.AddRange(AllSubjects.OfType<Architect>().Where(a => (MostRecentPartyTurnArchitect.Block.Architects.Contains(a) || (MostRecentPartyTurnArchitect.Room != null && MostRecentPartyTurnArchitect.Room.Architects.Contains(a))) && !a.IsAlive));
                        break;
                    case "spell":

                        // Iterate through the player's inventory objects
                        foreach (var item in MostRecentPartyTurnArchitect.Inventory)
                        {
                            if (item.SpecialKnowledge != null)
                            {
                                var spellEntity = GameWorld.AllSpells.Concat(GameWorld.AllLegendarySpells).FirstOrDefault(spell => spell == item.SpecialKnowledge);
                                if (spellEntity != null)
                                {
                                    RelevantEntities.Add(spellEntity);
                                }
                            }
                        }

                        // Check main hand object
                        if (MostRecentPartyTurnArchitect.MainHeldObject != null && MostRecentPartyTurnArchitect.MainHeldObject.SpecialKnowledge != null)
                        {
                            var spellEntity = GameWorld.AllSpells.Concat(GameWorld.AllLegendarySpells).FirstOrDefault(spell => spell == MostRecentPartyTurnArchitect.MainHeldObject.SpecialKnowledge);
                            if (spellEntity != null)
                            {
                                RelevantEntities.Add(spellEntity);
                            }
                        }

                        // Check off hand object
                        if (MostRecentPartyTurnArchitect.OffHeldObject != null && MostRecentPartyTurnArchitect.OffHeldObject.SpecialKnowledge != null)
                        {
                            var spellEntity = GameWorld.AllSpells.Concat(GameWorld.AllLegendarySpells).FirstOrDefault(spell => spell == MostRecentPartyTurnArchitect.OffHeldObject.SpecialKnowledge);
                            if (spellEntity != null)
                            {
                                RelevantEntities.Add(spellEntity);
                            }
                        }

                        // Check clothing items
                        foreach (var clothingItem in MostRecentPartyTurnArchitect.Clothing)
                        {
                            if (clothingItem.SpecialKnowledge != null)
                            {
                                var spellEntity = GameWorld.AllSpells.Concat(GameWorld.AllLegendarySpells).FirstOrDefault(spell => spell == clothingItem.SpecialKnowledge);
                                if (spellEntity != null)
                                {
                                    RelevantEntities.Add(spellEntity);
                                }
                            }
                        }

                        // Get all spells known by the player and add them as entities
                        foreach (var knownSpell in MostRecentPartyTurnArchitect.SpellsKnown)
                        {
                            var spellEntity = GameWorld.AllSpells.Concat(GameWorld.AllLegendarySpells).FirstOrDefault(spell => spell == knownSpell);
                            if (spellEntity != null)
                            {
                                RelevantEntities.Add(spellEntity);
                            }
                        }
                        break;


                    case "skill":
                        RelevantEntities.AddRange(MostRecentPartyTurnArchitect.SkillsKnown);
                        break;
                    case "direction":
                        RelevantEntities.AddRange(AllSubjects.Where(e => e.Metadata == "north" || e.Metadata == "south" || e.Metadata == "east" || e.Metadata == "west" || e.Metadata == "northeast" || e.Metadata == "southeast" || e.Metadata == "southwest" || e.Metadata == "northwest"));
                        break;
                    case "body_part_type":
                        var uniqueBodyPartNames = Game1.GameWorld.Races
                            .SelectMany(race => race.BodyPartNames)
                            .Distinct();

                        RelevantEntities.AddRange(uniqueBodyPartNames
                            .Select(bpName => AllSubjects.FirstOrDefault(e => e.Metadata == bpName))
                            .Where(e => e != null));
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
                    case "nearby_architect_object":
                        var nearbyArchitects = AllSubjects.OfType<Architect>()
                            .Where(a => (MostRecentPartyTurnArchitect.Block.Architects.Contains(a) || (MostRecentPartyTurnArchitect.Room != null && MostRecentPartyTurnArchitect.Room.Architects.Contains(a))) && a != MostRecentPartyTurnArchitect);

                        foreach (var architect in nearbyArchitects)
                        {
                            if (architect.OffHeldObject != null)
                            {
                                RelevantEntities.Add(architect.OffHeldObject);
                            }
                            if (architect.MainHeldObject != null)
                            {
                                RelevantEntities.Add(architect.MainHeldObject);
                            }
                            RelevantEntities.AddRange(architect.Inventory);
                            RelevantEntities.AddRange(architect.Clothing);
                        }
                        break;

                    default:
                        break;
                }
            }

            KeyboardState ShiftTestState = Keyboard.GetState();

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
                    void InitializeLibraries()
                    {
                        Vosk.Vosk.SetLogLevel(0);
                        ContentPath = Path.GetFullPath("Content");
                        string modelPath = Path.Combine(ContentPath, "vosk-model-en-us-0.22");
                        VoskModel = new Model(modelPath);
                        _recognizer = new VoskRecognizer(VoskModel, 16000.0f);
                        _recognizer.SetMaxAlternatives(0);
                        _recognizer.SetWords(true);
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



                    if (GameState == "mainscreen")
                    {
                        if (KeysNewlyPressed.Contains(Keys.C))
                        {
                            GameState = SpeechToText ? "loadinglibraries" : "worldgenscreen";
                            GameMode = "chronicle";
                        }
                        if (Keyboard.GetState().IsKeyDown(Keys.L))
                        {
                            GameState = "loadinggamemenu";
                        }

                        if (KeysNewlyPressed.Contains(Keys.F))
                        {
                            FancyFont = !FancyFont;
                        }
                    }
                    else if (GameState == "loadinglibraries")
                    {
                        InitializeLibraries();
                        GameState = "choosingaudio";
                    }
                    else if (GameState == "choosingaudio")
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            if (KeysNewlyPressed.Contains(Keys.D1 + i))
                            {
                                DeviceNumber = i;
                                GameState = GoToLoadingGameAfterLibrariesAreInstalled ? "loadinggame" : "worldgenscreen";
                                break;
                            }
                        }
                    }

                    else if (GameState == "worldgenscreen")
                    {
                        // Switch to generating world
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
                            if (CurrentlySelectedGrievanceType >= ThreatTypes.Count()) // Check if index exceeds list length
                            {
                                CurrentlySelectedGrievanceType = 0; // Wrap around to the start
                            }
                        }

                        if (KeysNewlyPressed.Contains(Keys.S))
                        {
                            CurrentlySelectedGrievanceType--;
                            if (CurrentlySelectedGrievanceType < 0) // Check if index goes below 0
                            {
                                CurrentlySelectedGrievanceType = ThreatTypes.Count() - 1; // Wrap around to the end
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
                    }
                    else if (GameState == "savinggame")
                    {
                        SaveGame(GameWorld);
                        GameWorld.GamePlayerAssociation.ActiveParty = null;
                        GameWorld = null;
                        GameState = "mainscreen";
                    }
                    else if (GameState == "loadinggamemenu")
                    {
                        var saveDirectories = Directory.GetDirectories(DocumentsFolderPath + "/LightrealmSaves");
                        int SavesCount = saveDirectories.Count();

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
                                GameState = SpeechToText ? "loadinglibraries" : "loadinggame";
                                SeenTips = true;
                                GoToLoadingGameAfterLibrariesAreInstalled = true;
                                SelectedDirectory = saveDirectories[LoadGameCursor];
                            }

                            if (KeysNewlyPressed.Contains(Keys.H))
                            {
                                CurrentlyViewingHistory = File.ReadAllLines(Path.Combine(saveDirectories[LoadGameCursor], "history.txt")).ToList();
                                GameState = "viewhistory";
                                HistoricalScrollValue = 0;
                                HistoryPrompt = "";
                                MaxRegionIndex = CurrentlyViewingHistory
    .Select(line =>
    {
        int regionIndex;
        string[] parts = line.Trim('[', ']').Split('/');
        if (parts.Length > 0 && int.TryParse(parts[0], out regionIndex) && regionIndex != 11)
        {
            return regionIndex;
        }
        return 0;
    })
    .Max();

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
                    else if (GameState == "viewhistory")
                    {
                        if (currentMouseState.ScrollWheelValue < previousMouseState.ScrollWheelValue)
                        {
                            if (Keyboard.GetState().IsKeyDown(Keys.LeftControl))
                            {
                                HistoricalScrollValue += 10;
                            }
                            else
                            {
                                HistoricalScrollValue += 1;
                            }
                        }
                        else if (currentMouseState.ScrollWheelValue > previousMouseState.ScrollWheelValue)
                        {
                            if (Keyboard.GetState().IsKeyDown(Keys.LeftControl))
                            {
                                HistoricalScrollValue -= 10;
                            }
                            else
                            {
                                HistoricalScrollValue -= 1;
                            }

                            if (HistoricalScrollValue < 0)
                            {
                                HistoricalScrollValue = 0;
                            }
                        }

                        if (KeysNewlyPressed.Contains(Keys.Escape))
                        {
                            GameState = "loadinggamemenu";
                        }

                        foreach (Keys k in KeysNewlyPressed)
                        {
                            if (KeyAtlas.ContainsKey(k) && !Keyboard.GetState().IsKeyDown(Keys.LeftControl))
                            {
                                HistoryPrompt += KeyAtlas[k];
                                HistoricalScrollValue = 0;
                            }
                            else if (k == Keys.Back && HistoryPrompt.Length > 0)
                            {
                                HistoryPrompt = HistoryPrompt.Substring(0, HistoryPrompt.Length - 1);
                                HistoricalScrollValue = 0;
                            }
                            else if (k == Keys.X)
                            {
                                // Set to 0 if '0' is pressed
                                CurrentlySelectingRegionIndexOr100 = 0;
                                HistoricalScrollValue = 0;
                            }
                            else if (k >= Keys.D1 && k <= Keys.D9)
                            {
                                int selectedRegionIndex = k - Keys.D1 + 1;
                                if (selectedRegionIndex <= MaxRegionIndex)
                                {
                                    CurrentlySelectingRegionIndexOr100 = selectedRegionIndex;
                                    HistoricalScrollValue = 0;
                                }
                            }
                            else if (k >= Keys.NumPad1 && k <= Keys.NumPad9)
                            {
                                int selectedRegionIndex = k - Keys.NumPad1 + 1;
                                if (selectedRegionIndex <= MaxRegionIndex)
                                {
                                    CurrentlySelectingRegionIndexOr100 = selectedRegionIndex;
                                    HistoricalScrollValue = 0;
                                }
                            }
                            else if (k == Keys.S)
                            {
                                ShowSignificant = !ShowSignificant;
                                HistoricalScrollValue = 0;
                            }
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
                        GameState = "travelmenu";
                    }
                    else if (GameState == "generatingworld")
                    {
                        GameWorld = new World(CurrentlySelectedWorldWidth, CurrentlySelectedWorldLength, NumberOfCivilizations - 4, CurrentlySelectedWorldAge, ThreatTypes[CurrentlySelectedGrievanceType], ProsperityMultiplier);
                        GameWorld.NextUniqueID = TemporaryNextUniqueID;

                        foreach (var entity in TemporaryEntityLedger)
                        {
                            GameWorld.EntityLedger.Add(entity.Key, entity.Value);
                        }

                        GameState = "placecivilizations";
                    }

                    else if (GameState == "placecivilizations")
                    {
                        if (GameWorld.InitialCivCount > 0)
                        {
                            int TryX = r.Next(GameWorld.Width);
                            int TryZ = r.Next(GameWorld.Length);

                            if (GameWorld.WorldMap[TryX + TryZ * GameWorld.Width].Biome != "ocean" && GameWorld.WorldMap[TryX + TryZ * GameWorld.Width].Biome != "void" && GameWorld.WorldMap[TryX + TryZ * GameWorld.Width].Location == null)
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
                            if (GameMode == "ascendant")
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


                        if (Keyboard.GetState().GetPressedKeys().Count() == 3 && Keyboard.GetState().IsKeyDown(Keys.Q) && Keyboard.GetState().IsKeyDown(Keys.R) && Keyboard.GetState().IsKeyDown(Keys.M))
                        {
                            RickRollCycles++;

                            if (RickRollCycles > 250)
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


                        //TURN OF FSPEED GEN FOR NEXT RELEASE

                        if (GameWorld.Cycle < maxAgeCycles/2)
                        {
                            for (int i = 6; i != 0; i--)
                            {
                                GameWorld.ProgressDays(28, true);
                            }
                        }
                        
                        if (GameWorld.Cycle < maxAgeCycles)
                        {
                            GameWorld.ProgressDays(28, true);
                        }

                        if (GameWorld.Cycle >= maxAgeCycles || KeysNewlyPressed.Contains(Keys.Enter))
                        {
                            // Create a list to hold the header and the historical events
                            List<string> fileContent = new List<string>
                            {
                                "Historical Events of " + GameWorld.Name,
                                "To find events related to a certain subject, start typing to search for it.",
                                "Press CTRL to scroll faster.",
                                "" // Adding an empty line for better readability
                            };

                            // Add the historical events to the list
                            //
                            fileContent.AddRange(GameWorld.HistoricalEvents.Select(e => e.EventData));


                            foreach (Architect a in GameWorld.AllArchitects)
                            {
                                a.UpdateNames();
                            }

                            GameState = "choosegamemode";
                        }
                    }
                    else if (GameState == "choosegamemode")
                    {
                        if(KeysNewlyPressed.Contains(Keys.A))
                        {
                            GameMode = "ascendant";
                            GameState = "choosefounderoptions";
                        }
                        else if (KeysNewlyPressed.Contains(Keys.C))
                        {
                            GameMode = "chronicle";
                            GameState = "choosepreferences";
                        }
                    }
                    else if (GameState == "choosefounderoptions")
                    {
                        if (KeysNewlyPressed.Contains(Keys.D1))
                        {
                            CurrentlySelectingRace = CurrentlySelectingRace + 1;
                            if (CurrentlySelectingRace > GameWorld.HumanoidRaces.Count() - 1)
                            {
                                CurrentlySelectingRace = 1;
                            }
                        }
                        if (KeysNewlyPressed.Contains(Keys.Enter))
                        {
                            GameState = "ascendant";
                            AscendantState = "main";
                            GameWorld.ProgressDays(28, true);

                            foreach (Civilization c in GameWorld.Civilizations)
                            {
                                if (c.PrimaryInhabitantRace == Game1.GameWorld.Races[CurrentlySelectingRace])
                                {
                                    GameWorld.GamePlayerAssociation = new Association(new EntityList<Architect>() { c.Alpha }, "political", c.Alpha, c.Capitol, true);

                                    //TODO: IMPLEMENT MORE RESOURCES FOR YOUR CIV TO WORK WITH HERE, and change the message from "symbol" to "autocrat" or "supreme leader" idk, just signify power levels.

                                    Announcements.Add(new TextStorage("As alpha of " + c.Name + " and founder of " + Game1.GameWorld.GamePlayerAssociation.Name + ", " + c.Alpha.Name + " is an intriguing individual. True power does not come from petty titles, however. Gather influence throughout your lands, and who knows? Perhaps in time your power shall rise and you shall accomplish what those before you could not.", Color.HotPink, new EntityList<Entity>(){}));

                                    MostRecentPartyTurnArchitect = c.Alpha;

                                    GameWorld.GamePlayerAssociation.AscendantX = c.StartX;
                                    GameWorld.GamePlayerAssociation.AscendantZ = c.StartZ;

                                    GameWorld.GamePlayerAssociation.LatestHistoricalAnalysisIndex = GameWorld.HistoricalEvents.Count;


                                    Game1.GameWorld.RevealNearbyTiles(Game1.GameWorld.GamePlayerAssociation.AscendantX, Game1.GameWorld.GamePlayerAssociation.AscendantZ, 24, false);

                                    break;
                                }
                            }
                        }
                    }
                    else if (GameState == "ascendant")
                    {
                        while (GameWorld.HistoricalEvents.Count > GameWorld.GamePlayerAssociation.LatestHistoricalAnalysisIndex)
                        {
                            var (color, isRelevant) = GameWorld.GamePlayerAssociation.TestForRelevance(GameWorld.HistoricalEvents[GameWorld.GamePlayerAssociation.LatestHistoricalAnalysisIndex]);

                            if (isRelevant)
                            {
                                Announcements.Add(new TextStorage(GameWorld.HistoricalEvents[GameWorld.GamePlayerAssociation.LatestHistoricalAnalysisIndex].EventData, color, new EntityList<Entity>(){}));
                            }

                            GameWorld.GamePlayerAssociation.LatestHistoricalAnalysisIndex++;
                        }








                        if (KeysNewlyPressed.Contains(Keys.R) || KeysNewlyPressed.Contains(Keys.T) || KeysNewlyPressed.Contains(Keys.D) || KeysNewlyPressed.Contains(Keys.G) || KeysNewlyPressed.Contains(Keys.C) || KeysNewlyPressed.Contains(Keys.V) || KeysNewlyPressed.Contains(Keys.NumPad7) || KeysNewlyPressed.Contains(Keys.NumPad9) || KeysNewlyPressed.Contains(Keys.NumPad4) || KeysNewlyPressed.Contains(Keys.NumPad6) || KeysNewlyPressed.Contains(Keys.NumPad1) || KeysNewlyPressed.Contains(Keys.NumPad3))
                        {
                            int DeltaX = 0;
                            int DeltaZ = 0;

                            if (GameWorld.GamePlayerAssociation.AscendantZ % 2 == 0)
                            {
                                // Even rows: Offset positions
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
                                // Odd rows: Different offset positions
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

                            int newX = GameWorld.GamePlayerAssociation.AscendantX + DeltaX;
                            int newZ = GameWorld.GamePlayerAssociation.AscendantZ + DeltaZ;

                            // Ensure the new position is within the map boundaries
                            if (newX >= 0 && newX < GameWorld.Width && newZ >= 0 && newZ < GameWorld.Length)
                            {
                                GameWorld.GamePlayerAssociation.AscendantX = newX;
                                GameWorld.GamePlayerAssociation.AscendantZ = newZ;
                            }
                        }

                        // Handle state changes from the "main" state
                        if (AscendantState == "main")
                        {
                            if (KeysNewlyPressed.Contains(Keys.L))
                            {
                                AscendantState = "ledger";
                            }
                            else if (KeysNewlyPressed.Contains(Keys.M))
                            {
                                AscendantState = "management";
                            }
                            else if (KeysNewlyPressed.Contains(Keys.I))
                            {
                                AscendantState = "chooseactinggroup";
                            }
                            else if (KeysNewlyPressed.Contains(Keys.B))
                            {
                                AscendantState = "build";
                            }
                            else if (KeysNewlyPressed.Contains(Keys.P))
                            {
                                AscendantState = "deployment";
                            }

                            else if (KeysNewlyPressed.Contains(Keys.D1))
                            {
                                GameWorld.ProgressDays(1, true);
                            }
                            else if (KeysNewlyPressed.Contains(Keys.D2))
                            {
                                GameWorld.ProgressDays(7, true);
                            }
                            else if (KeysNewlyPressed.Contains(Keys.D3))
                            {
                                GameWorld.ProgressDays(28, true);
                            }
                            else if (KeysNewlyPressed.Contains(Keys.D4))
                            {
                                for(int I = 0; I < 12; I++)
                                {
                                    GameWorld.ProgressDays(28, true);
                                }
                            }
                        }
                        else if (AscendantState == "deployment")
                        {
                            if (KeysNewlyPressed.Contains(Keys.Up))
                            {
                                AscendantCursor--;

                                if (AscendantCursor < 0)
                                {
                                    AscendantCursor = GameWorld.GamePlayerAssociation.Parties.Count - 1;
                                }
                            }
                            else if (KeysNewlyPressed.Contains(Keys.Down))
                            {
                                AscendantCursor++;

                                if (AscendantCursor > GameWorld.GamePlayerAssociation.Parties.Count - 1)
                                {
                                    AscendantCursor = 0;
                                }
                            }
                            else if (KeysNewlyPressed.Contains(Keys.Enter))
                            {
                                CurrentlySelectedAscendantParty = GameWorld.GamePlayerAssociation.Parties[AscendantCursor];
                                AscendantState = "selectrelocation";
                            }
                        }
                        else if (AscendantState == "selectrelocation")
                        {
                            Region r = Game1.GameWorld.WorldMap[Game1.GameWorld.GamePlayerAssociation.AscendantX + Game1.GameWorld.GamePlayerAssociation.AscendantZ * Game1.GameWorld.Width];
                            if (r.Explored && r.Location != null && KeysNewlyPressed.Contains(Keys.Enter))
                            {
                                foreach (Architect a in CurrentlySelectedAscendantParty.Architects)
                                {
                                    a.NextMigrationLocation = r.Location;
                                    GameWorld.ProgressDays(12, true);
                                }

                                AscendantState = "main";
                            }
                        }
                        else if (AscendantState == "management")
                        {
                            int Count = 0;
                            foreach (Party p in Game1.GameWorld.GamePlayerAssociation.Parties)
                            {
                                Count += p.Architects.Count;
                            }

                            if (KeysNewlyPressed.Contains(Keys.Up))
                            {
                                AscendantCursor--;

                                if (AscendantCursor < 0)
                                {
                                    AscendantCursor = Count - 1;
                                }
                            }
                            else if (KeysNewlyPressed.Contains(Keys.Down))
                            {
                                AscendantCursor++;

                                if (AscendantCursor > Count - 1)
                                {
                                    AscendantCursor = 0;
                                }
                            }
                            else if (KeysNewlyPressed.Contains(Keys.Enter))
                            {
                                int Archindex = 0;
                                foreach (Party p in Game1.GameWorld.GamePlayerAssociation.Parties)
                                {
                                    foreach(Architect a in p.Architects)
                                    {
                                        if(Archindex == AscendantCursor)
                                        {
                                            CurrentlySelectedAscendantArchitect = a;
                                            CurrentlySelectedAscendantParty = p;
                                            break;
                                        }
                                        Archindex++;
                                    }
                                }

                                AscendantState = "movetoparty";
                            }
                        }
                        else if (AscendantState == "movetoparty")
                        {
                            if (KeysNewlyPressed.Contains(Keys.Up))
                            {
                                AscendantCursor--;

                                if (AscendantCursor < 0)
                                {
                                    AscendantCursor = GameWorld.GamePlayerAssociation.Parties.Count - 1;
                                }
                            }
                            else if (KeysNewlyPressed.Contains(Keys.Down))
                            {
                                AscendantCursor++;

                                if (AscendantCursor > GameWorld.GamePlayerAssociation.Parties.Count - 1)
                                {
                                    AscendantCursor = 0;
                                }
                            }
                            else if (KeysNewlyPressed.Contains(Keys.Enter))
                            {
                                GameWorld.GamePlayerAssociation.Parties[AscendantCursor].Architects.Add(CurrentlySelectedAscendantArchitect);
                                CurrentlySelectedAscendantParty.Architects.Remove(CurrentlySelectedAscendantArchitect);
                                AscendantState = "main";
                            }
                            else if (KeysNewlyPressed.Contains(Keys.N))
                            {
                                GameWorld.GamePlayerAssociation.Parties.Add(new Party(new EntityList<Architect>() { CurrentlySelectedAscendantArchitect }, "political", CurrentlySelectedAscendantArchitect, GameWorld.GamePlayerAssociation.Parties[0].Base));
                            }
                        }
                        else if (AscendantState == "chooseactinggroup")
                        {
                            int Count = 0;
                            foreach (Party p in Game1.GameWorld.GamePlayerAssociation.Parties)
                            {
                                Count += p.Architects.Count;
                            }

                            if (KeysNewlyPressed.Contains(Keys.Up))
                            {
                                AscendantCursor--;

                                if (AscendantCursor < 0)
                                {
                                    AscendantCursor = Count - 1;
                                }
                            }
                            else if (KeysNewlyPressed.Contains(Keys.Down))
                            {
                                AscendantCursor++;

                                if (AscendantCursor > Count - 1)
                                {
                                    AscendantCursor = 0;
                                }
                            }
                            else if (KeysNewlyPressed.Contains(Keys.Enter))
                            {
                                int Archindex = 0;
                                foreach (Party p in Game1.GameWorld.GamePlayerAssociation.Parties)
                                {
                                    foreach (Architect a in p.Architects)
                                    {
                                        if (Archindex == AscendantCursor)
                                        {
                                            CurrentlySelectedAscendantArchitect = a;
                                            CurrentlySelectedAscendantParty = p;
                                            break;
                                        }
                                        Archindex++;
                                    }
                                }

                                AscendantState = "choosedistrict";
                            }
                        }
                        else if (AscendantState == "choosedistrict")
                        {
                            if (KeysNewlyPressed.Contains(Keys.Up))
                            {
                                AscendantCursor--;

                                if (AscendantCursor < 0)
                                {
                                    AscendantCursor = CurrentlySelectedAscendantParty.Leader.Location.Districts.Count - 1;
                                }
                            }
                            else if (KeysNewlyPressed.Contains(Keys.Down))
                            {
                                AscendantCursor++;

                                if (AscendantCursor > CurrentlySelectedAscendantParty.Leader.Location.Districts.Count - 1)
                                {
                                    AscendantCursor = 0;
                                }
                            }
                            else if (KeysNewlyPressed.Contains(Keys.Enter))
                            {
                                CurrentlySelectedAscendantDistrict = CurrentlySelectedAscendantParty.Leader.Location.Districts[AscendantCursor];

                                AscendantState = "selectactingmode";
                            }
                        }
                        else if (AscendantState == "selectactingmode")
                        {
                            if (KeysNewlyPressed.Contains(Keys.I))
                            {
                                AscendantState = "selectaction";
                            }
                            else if (KeysNewlyPressed.Contains(Keys.D))
                            {
                                AscendantState = "main";

                                GameState = "exposition";
                                Location newLocation = CurrentlySelectedAscendantParty.Leader.Location;
                                District newDistrict = CurrentlySelectedAscendantDistrict;

                                // Create a collective arrival message for the party
                                Exposition.Add(new TextStorage("You arrive at " + newLocation.Name + ", on the outskirts of " + newDistrict.Name + ".", Color.LightBlue, new EntityList<Entity>(){}));
                                Exposition.Add(new TextStorage("Press SPACE to continue...", Color.LightBlue, new EntityList<Entity>(){}));

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
                                int randomBlockIndex = outskirtsBlockIndexes[r.Next(outskirtsBlockIndexes.Count())];
                                Block arrivalBlock = newDistrict.DistrictMap[randomBlockIndex];

                                // Load the new location and update all architects
                                newDistrict.Load();
                                foreach (Architect architect in GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                                {
                                    architect.Location = newLocation;
                                    architect.District = newDistrict;
                                    architect.Block = arrivalBlock;  // Set all architects to the randomly selected outskirts block
                                    architect.TryingToTravel = false;  // Reset travel intent

                                    int Month = ((int)Math.Round((decimal)(Game1.GameWorld.Cycle / 24192000)) % 12) + 1;
                                    int Year = (int)Math.Round((decimal)(Game1.GameWorld.Cycle / 290304000), MidpointRounding.ToZero);
                                    string Date = "(" + Month + "/" + Year + ")";
                                    Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + architect.Name + " began a special operation in " + architect.Location.Name + ".", architect.Location.Region, new EntityList<Entity>(){architect, architect.Location}));
                                }

                            }
                        }
                        else if (AscendantState == "selectaction")
                        {
                            if (KeysNewlyPressed.Contains(Keys.A))
                            {
                                AscendantAction = "artifacttheft";
                                AscendantState = "chooseactioncomponent";

                                AscendantEntityLedger.Clear();
                                
                                foreach(Structure s in CurrentlySelectedAscendantParty.Leader.Location.AllStructures)
                                {
                                    if(s.Block.District == CurrentlySelectedAscendantDistrict)
                                    {
                                        foreach (Object o in s.HistoricalObjects)
                                        {
                                            o.TemporaryStructureStorage = s;
                                            AscendantEntityLedger.Add(o);
                                        }
                                    }
                                }
                            }
                            else if (KeysNewlyPressed.Contains(Keys.R))
                            {
                                AscendantAction = "razebuilding";
                                AscendantState = "chooseactioncomponent";
                                AscendantEntityLedger.Clear();
                                AscendantEntityLedger.AddRange(CurrentlySelectedAscendantParty.Leader.Location.AllStructures.Where(s => s.Block.District == CurrentlySelectedAscendantDistrict));
                            }
                            else if (KeysNewlyPressed.Contains(Keys.T))
                            {
                                AscendantAction = "takeover";
                                AscendantState = "initiateaction";
                            }
                            else if (KeysNewlyPressed.Contains(Keys.D))
                            {
                                AscendantAction = "killassorted";
                                AscendantState = "initiateaction";
                            }
                            else if (KeysNewlyPressed.Contains(Keys.K))
                            {
                                AscendantAction = "kidnapassorted";
                                AscendantState = "initiateaction";
                            }
                            else if (KeysNewlyPressed.Contains(Keys.V))
                            {
                                AscendantAction = "killtarget";
                                AscendantState = "chooseactioncomponent";
                                AscendantEntityLedger.Clear();
                                AscendantEntityLedger.AddRange(CurrentlySelectedAscendantDistrict.Architects);
                            }
                            else if (KeysNewlyPressed.Contains(Keys.Y))
                            {
                                AscendantAction = "kidnaptarget";
                                AscendantState = "chooseactioncomponent";
                                AscendantEntityLedger.Clear();
                                AscendantEntityLedger.AddRange(CurrentlySelectedAscendantDistrict.Architects);
                            }
                            else if (KeysNewlyPressed.Contains(Keys.I))
                            {
                                AscendantAction = "incite";
                                AscendantState = "initiateaction";
                            }
                        }
                        else if (AscendantState == "chooseactioncomponent")
                        {
                            int totalEntities = AscendantEntityLedger.Count;
                            int totalPages = (totalEntities + 9) / 10; // Calculate the total number of pages

                            // Handle selection of entities using number keys 1-9 and 0
                            for (int i = 0; i < 10; i++)
                            {
                                Keys key = (Keys)(i == 9 ? Keys.D0 : Keys.D1 + i);
                                if (KeysNewlyPressed.Contains(key) && (10 * AscendantPage + i) < totalEntities)
                                {
                                    // Select the entity based on the key press
                                    SelectedAscendantTarget = AscendantEntityLedger[10 * AscendantPage + i];
                                    AscendantState = "initiateaction"; // Move to the next state after selection
                                    break;
                                }
                            }

                            // Handle page navigation
                            if (KeysNewlyPressed.Contains(Keys.OemComma)) // Assuming `<` is mapped to OemComma
                            {
                                AscendantPage = Math.Max(0, AscendantPage - 1); // Move to the previous page, but not below 0
                            }
                            else if (KeysNewlyPressed.Contains(Keys.OemPeriod)) // Assuming `>` is mapped to OemPeriod
                            {
                                AscendantPage = Math.Min(totalPages - 1, AscendantPage + 1); // Move to the next page, but not beyond totalPages-1
                            }
                        }

                        else if (AscendantState == "initiateaction")
                        {
                            //construct the correct entities first

                            EntityList<Entity> Entities = new EntityList<Entity>
                            {
                                // Common entities for all actions
                                CurrentlySelectedAscendantDistrict.Location,
                                CurrentlySelectedAscendantDistrict
                            };

                            switch (AscendantAction)
                            {
                                case "artifacttheft":
                                    Entities.Add(((Object)SelectedAscendantTarget).TemporaryStructureStorage);
                                    Entities.Add(SelectedAscendantTarget);
                                    break;

                                case "killtarget":
                                case "kidnaptarget":
                                    Entities.Add(SelectedAscendantTarget);
                                    break;

                                    // No need to handle other cases since they only require the common entities
                            }

                            WorldActionInitiator.InitiateAction(GameWorld, AscendantAction, CurrentlySelectedAscendantParty, Entities);

                            AscendantState = "main";
                        }



                        if (KeysNewlyPressed.Contains(Keys.X))
                        {
                            AscendantState = "main";
                        }
                    }


                    else if (GameState == "choosepreferences")
                    {
                        if (KeysNewlyPressed.Contains(Keys.D1))
                        {
                            CurrentlySelectingRace = CurrentlySelectingRace + 1;
                            if (CurrentlySelectingRace > GameWorld.HumanoidRaces.Count())
                            {
                                CurrentlySelectingRace = 1;
                            }
                        }
                        if (KeysNewlyPressed.Contains(Keys.D2))
                        {
                            CurrentlySelectingSex = CurrentlySelectingSex + 1;
                            if (CurrentlySelectingSex > Sexes.Count() - 1)
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
                            if (CurrentlySelectingHandedness == true)
                            {
                                CurrentlySelectingHandedness = false;
                            }
                            else
                            {
                                CurrentlySelectingHandedness = true;
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

                                if (keyIndex < StatOptions.Count()) // Ensure the key corresponds to an available option
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
                                    if (CurrentlyAssigningSkill == 1 && StatOptions.Count() == 1)
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

                            if (keyIndex != -1 && keyIndex < InspirationsChosen.Count()) // Ensure the key corresponds to an available option
                            {
                                InspirationSelected = InspirationsChosen[keyIndex];
                                InspirationsChosen.RemoveAt(keyIndex); // Remove selected inspiration from options

                                // Add announcement for selected inspiration

                                Announcements.Add(new TextStorage($"Your journey begins here. Direct your character to move, conversate, fight, gather, and whatever else you wish. More must lie beyond the borders of your district...", Color.Pink, new EntityList<Entity>(){}));
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


                        for (int i = 0; i < 30; i++)
                        {
                            foreach (Architect a in GameWorld.AllArchitects)
                            {
                                if (a.IsAlive && a.Location != null && GameWorld.SettlementTypes.Contains(a.Location.Type))
                                {
                                    if (a.Sex == Sexes[CurrentlySelectingSex] &&
                                        a.Race == GameWorld.HumanoidRaces[CurrentlySelectingRace - 1] &&
                                        a.Location.TradersAtThisLocation.Count() > 0 &&
                                        a.Reputation > -40 &&
                                        a.Grievances.Any(g => GameWorld.Calamity.Any(c => c.ID == g.Item1)))
                                    {
                                        var grievance = a.Grievances.First(g => GameWorld.Calamity.Any(c => c.ID == g.Item1));
                                        GrievanceDoer = World.EntityGet<Entity>(grievance.Item1).Name;
                                        GrievanceReason = grievance.Item2;

                                        TheChosenOne = a;
                                        break;
                                    }
                                }

                                if (a.Grievances.Count > 0)
                                {
                                    int shobe = 1;
                                }
                                if (a.Location != null && a.Location.TradersAtThisLocation.Count() > 0)
                                {
                                    int shobe = 1;
                                }
                            }

                            //keep looking IG


                            if (TheChosenOne != null)
                            {
                                break;
                            }

                            GameWorld.ProgressDays(28, true);
                        }


                        if (TheChosenOne != null)
                        {
                            GameState = "architectfound";
                            GameWorld.GamePlayerAssociation = new Association(new EntityList<Architect>() { TheChosenOne }, "adventurer", TheChosenOne, TheChosenOne.Location, true);
                            MostRecentPartyTurnArchitect = TheChosenOne;
                            TheChosenOne.District.Architects.Remove(TheChosenOne);
                            TheChosenOne.Group = GameWorld.GamePlayerAssociation.ActiveParty;

                            if (CurrentlySelectingHandedness == true)
                            {
                                //right handed
                                TheChosenOne.MainInteractionAppendage = TheChosenOne.FindBodyPart("right hand");
                                TheChosenOne.OffInteractionAppendage = TheChosenOne.FindBodyPart("left hand");
                            }
                            else
                            {
                                //left handed
                                TheChosenOne.MainInteractionAppendage = TheChosenOne.FindBodyPart("left hand");
                                TheChosenOne.OffInteractionAppendage = TheChosenOne.FindBodyPart("right hand");
                            }

                            GameWorld.ProgressToNextMorning();

                            GameState = "pickstatpreferences";
                            StatOptions = new List<string>()
                            {
                                "[STR]: Strength (+Melee Power/Speed, +Gathering Efficiency)",
                                "[AGL]: Agility (+Reaction Chance, +Action Speed, +Escape Chance)",
                                "[DEX]: Dexterity (-Limb Exposure, +Throwing Power)",
                                "[END]: Endurance (+Max Energy, +Weight Capacity)",
                                "[CRE]: Creativity (+Craft Quality/Rarity, +Composition Skill)",
                                "[CHA]: Charisma (+Conversation/Persuasion, +Performance Skill)",
                                "[FOC]: Focus (+Non-Perception Magic Effectiveness, Feel Less Pain)"
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
                            GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].District.Load();

                            //place player

                            int BlockX = Game1.r.Next(0, 7);
                            int BlockZ = Game1.r.Next(0, 7);

                            GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Block = GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].District.DistrictMap[BlockX + BlockZ * 7];
                            GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].District.DistrictMap[BlockX + BlockZ * 7].Architects.Add(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0]);

                            string GenderName = "";

                            if (GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Age > 16)
                            {
                                if (GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Sex == "male")
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
                                if (GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Sex == "male")
                                {
                                    GenderName = "boy";
                                }
                                else
                                {
                                    GenderName = "girl";
                                }
                            }

                            Exposition.Add(new TextStorage(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Name + " is a " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Age + "-year-old " + GenderName + " from " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Location.Name + ". ", Color.Blue, new EntityList<Entity>(){}));
                            Exposition.Add(new TextStorage(Capitalize(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Pronoun) + " lives in the " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Location.Districts[Game1.r.Next(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Location.Districts.Count())].Name + " district as a " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Profession + ".", Color.Purple, new EntityList<Entity>(){}));

                            if (GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group != null && GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Architects.Count() > 1)
                            {
                                if (GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Location.Government == GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group)
                                {
                                    if (GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Leader == GameWorld.GamePlayerAssociation.ActiveParty.Architects[0])
                                    {
                                        Exposition.Add(new TextStorage(Capitalize(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Pronoun) + " is the leader of " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Name + ", a " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Type + " group that rules " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Leader.Location.Name + ", with " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Architects.Count() + " members.", Color.Red, new EntityList<Entity>(){}));
                                    }
                                    else
                                    {
                                        Exposition.Add(new TextStorage(Capitalize(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Pronoun) + " is a member of " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Name + ", a " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Type + " group that rules " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Leader.Location.Name + ", with " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Architects.Count() + " members.", Color.Red, new EntityList<Entity>(){}));
                                    }
                                }
                                else
                                {
                                    if (GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Leader == GameWorld.GamePlayerAssociation.ActiveParty.Architects[0])
                                    {
                                        Exposition.Add(new TextStorage(Capitalize(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Pronoun) + " is the leader of " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Name + ", a " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Type + " group based in " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Leader.Location.Name + ", with " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Architects.Count() + " members.", Color.Red, new EntityList<Entity>(){}));
                                    }
                                    else
                                    {
                                        Exposition.Add(new TextStorage(Capitalize(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Pronoun) + " is a member of " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Name + ", a " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Type + " group based in " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Leader.Location.Name + ", with " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Group.Architects.Count() + " members.", Color.Red, new EntityList<Entity>(){}));
                                    }
                                }
                            }

                            if (GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].ScienceStudyPoints == 0 && GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].CultureStudyPoints == 0 && GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].MagicStudyPoints == 0)
                            {
                                Exposition.Add(new TextStorage(Capitalize(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Name + " finds " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].FavoriteScienceField + ", " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].FavoriteCultureField + ", and " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].FavoriteMagicField + " magic very intriguing."), Color.Orange, new EntityList<Entity>(){}));
                            }
                            if (GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].ScienceStudyPoints > 0)
                            {
                                Exposition.Add(new TextStorage(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Name + " studies " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].FavoriteScienceField + " very avidly.", Color.Orange, new EntityList<Entity>(){}));
                            }
                            if (GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].CultureStudyPoints > 0)
                            {
                                Exposition.Add(new TextStorage(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Name + " composes " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].FavoriteCultureField + ", and explores it relentlessly.", Color.Orange, new EntityList<Entity>(){}));
                            }
                            if (GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].MagicStudyPoints > 0)
                            {
                                Exposition.Add(new TextStorage(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Name + " practices " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].FavoriteMagicField + " magic, and dedicates much research to it's pursuit.", Color.Orange, new EntityList<Entity>(){}));
                            }

                            GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Strength = StoredStr;
                            GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Dexterity = StoredDex;
                            GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Agility = StoredAgl;
                            GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Charisma = StoredCha;
                            GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Focus = StoredFoc;
                            GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Creativity = StoredCre;
                            GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Endurance = StoredEnd;

                            GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Energy = GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].MaxEnergy();


                            Exposition.Add(new TextStorage(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Name + " has a deep appreciation for " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].FavoriteCloth.Name + ", " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].FavoriteStone.Name + ", " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].FavoriteWood.Name + ", " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].FavoriteGemstone.Name + ", and " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].FavoriteMetal.Name + ".", Color.Orange, new EntityList<Entity>(){}));
                            Exposition.Add(new TextStorage(
                            Capitalize($"{GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Pronoun} has ") +
                            $"{GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].GetDescription(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Strength)} strength, " +
                            $"{GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].GetDescription(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Agility)} agility, " +
                            $"{GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].GetDescription(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Dexterity)} dexterity, " +
                            $"{GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].GetDescription(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Endurance)} endurance, " +
                            $"{GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].GetDescription(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Creativity)} creativity, " +
                            $"{GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].GetDescription(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Charisma)} charisma, and " +
                            $"{GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].GetDescription(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Focus)} focus.", Color.Purple, new EntityList<Entity>(){}));



                            if (GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].FavoriteBook == null)
                            {
                                Exposition.Add(new TextStorage(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Name + " has been taught to read, but never found any interesting books.", Color.Yellow, new EntityList<Entity>(){}));
                            }
                            else
                            {
                                Exposition.Add(new TextStorage(GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Name + "'s favorite book is " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].FavoriteBook.Name + ", a book primarily on the subject of " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].FavoriteBook.CompositionContent.Subject.Name + ".", Color.Yellow, new EntityList<Entity>(){}));
                            }

                            Exposition.Add(new TextStorage("", Color.Green, new EntityList<Entity>(){}));
                            Exposition.Add(new TextStorage("", Color.Green, new EntityList<Entity>(){}));
                            Exposition.Add(new TextStorage("", Color.Green, new EntityList<Entity>(){}));
                            Exposition.Add(new TextStorage("", Color.Green, new EntityList<Entity>(){}));


                            string expositionText =
                            GameWorld.Calamity[0].Name.Split(' ')[0] + " and " + GameWorld.Calamity[0].PossessivePronoun +
                            " gang of " + CalamityIdeologicalObsessionMapping[GameWorld.CalamityIdeologicalObsession] +
                            " have plagued " + GameWorld.Name + " for decades, but you cannot stand another second. " +
                            "It has been a long time since " + GrievanceDoer + GrievanceReason +
                            ", but as you hear more about the threat of the organization behind them, the memory continues to burden you. " +
                            "Regardless of your passion, your journey will be quite difficult without proper experience and equipment. " +
                            "You've saved up a bit of fragments " + ConvertProfessionToCareerDescription[GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Profession] +
                            ", but the merchants from " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Location.TradersAtThisLocation[0].Base.Name +
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
                                Exposition.Add(new TextStorage(line, Color.Aquamarine, new EntityList<Entity>(){}));
                            }

                            Exposition.Add(new TextStorage("", Color.White, new EntityList<Entity>(){}));
                            Exposition.Add(new TextStorage("Or perhaps, " + GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Name + " can move on, and find a different path.", Color.Magenta, new EntityList<Entity>(){}));
                            Exposition.Add(new TextStorage("", Color.White, new EntityList<Entity>(){}));
                            Exposition.Add(new TextStorage("Press SPACE to continue...", Color.White, new EntityList<Entity>(){}));


                            GameState = "exposition";
                        }
                    }
                    else if (GameState == "partyturn")
                    {

                        if (KeysNewlyPressed.Contains(Keys.F7))
                        {
                            foreach (Architect a in MostRecentPartyTurnArchitect.Room != null ? MostRecentPartyTurnArchitect.Room.Architects : MostRecentPartyTurnArchitect.Block.Architects)
                            {
                                if (a != MostRecentPartyTurnArchitect)
                                {
                                    a.Energy = 0;
                                }
                            }
                        }

                        if (SpeechToText)
                        {
                            if (KeysNewlyPressed.Contains(Keys.RightAlt))
                            {
                                if (!_isRecording)
                                {
                                    // Enabling split mode is kind of required
                                    SplitMode = true;

                                    foreach (Architect a in LoadedArchitects)
                                    {
                                        a.UpdateNames();

                                        if (a.Room != null && a.Block.Architects.Contains(a))
                                        {
                                            int shibe = 1;
                                        }

                                        if (a.Block != null)
                                        {
                                            EntityList<Object> NearbyObjects = a.Room != null ? a.Room.Objects : a.Block.Objects;

                                            foreach (Object o in NearbyObjects)
                                            {
                                                o.UpdateNames();
                                            }
                                        }
                                    }

                                    // Start recording
                                    _recognizer = new VoskRecognizer(VoskModel, 16000.0f);
                                    _recognizer.SetMaxAlternatives(0);
                                    _recognizer.SetWords(true);

                                    PortAudio.Initialize();

                                    var inputParameters = new StreamParameters
                                    {
                                        device = DeviceNumber,
                                        channelCount = 1,
                                        sampleFormat = SampleFormat.Int16,
                                        suggestedLatency = PortAudio.GetDeviceInfo(DeviceNumber).defaultLowInputLatency,
                                        hostApiSpecificStreamInfo = IntPtr.Zero
                                    };

                                    PortAudioSharp.Stream.Callback callback = (IntPtr input, IntPtr output,
                                        UInt32 frameCount,
                                        ref StreamCallbackTimeInfo timeInfo,
                                        StreamCallbackFlags statusFlags,
                                        IntPtr userData) =>
                                    {
                                        short[] samples = new short[frameCount];
                                        Marshal.Copy(input, samples, 0, (int)frameCount);

                                        byte[] byteArray = new byte[samples.Length * sizeof(short)];
                                        Buffer.BlockCopy(samples, 0, byteArray, 0, byteArray.Length);

                                        if (_recognizer.AcceptWaveform(byteArray, byteArray.Length))
                                        {
                                            var result = _recognizer.Result();
                                            DisplayResult(result);
                                        }
                                        else
                                        {
                                            var partialResult = _recognizer.PartialResult();
                                            DisplayResult(partialResult);
                                        }

                                        return StreamCallbackResult.Continue;
                                    };

                                    _stream = new PortAudioSharp.Stream(
                                        inParams: inputParameters,
                                        outParams: null,
                                        sampleRate: 16000,
                                        framesPerBuffer: 256,
                                        streamFlags: StreamFlags.ClipOff,
                                        callback: callback,
                                        userData: IntPtr.Zero
                                    );

                                    _stream.Start();
                                    _isRecording = true;
                                }
                                else if (_isRecording)
                                {
                                    _stream.Stop();
                                    _stream.Dispose();
                                    PortAudio.Terminate();
                                    _isRecording = false;
                                }
                            }
                        }


                        if (currentMouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released && Keyboard.GetState().IsKeyDown(Keys.LeftShift))
                        {
                            crds.Add(new Vector2(currentMouseState.X, currentMouseState.Y));
                        }

                        if (SplitMode && currentMouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
                        {
                            // Calculate the scale factors based on the actual screen resolution
                            float scaleX = (float)_graphics.PreferredBackBufferWidth / 2560f;
                            float scaleY = (float)_graphics.PreferredBackBufferHeight / 1440f;

                            // Scale the mouse position to match the hitbox coordinates
                            Vector2 mousePosition = new Vector2(currentMouseState.X / scaleX, currentMouseState.Y / scaleY);
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


                        if (KeysNewlyPressed.Contains(Keys.RightControl))
                        {
                            if (MassTravelOrderMode == false)
                            {
                                MassTravelOrderMode = true;
                            }
                            else
                            {
                                MassTravelOrderMode = false;
                            }
                        }

                        //yehe les dub chek

                        if (ArchitectIndex > LoadedArchitects.Count() - 1)
                        {
                            ArchitectIndex = 0;
                        }

                        if (KeysNewlyPressed.Contains(Keys.F3))
                        {
                            if (SplitMode)
                            {
                                SplitMode = false;

                                foreach (Architect a in LoadedArchitects)
                                {
                                    a.UpdateNames();
                                    if (a.Block != null)
                                    {
                                        EntityList<Object> NearbyObjects = a.Room != null ? a.Room.Objects : a.Block.Objects;

                                        foreach (Object o in NearbyObjects)
                                        {
                                            o.LatestUpdateCycle--; //this makes sure the object is willing to update
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
                                        EntityList<Object> NearbyObjects = a.Room != null ? a.Room.Objects : a.Block.Objects;

                                        foreach (Object o in NearbyObjects)
                                        {
                                            o.LatestUpdateCycle--; //this makes sure the object is willing to update
                                            o.UpdateNames();
                                        }
                                    }
                                }
                            }
                        }


                        if (!GameWorld.GamePlayerAssociation.ActiveParty.ReceivedPartyAdvice && GameWorld.GamePlayerAssociation.ActiveParty.Architects.Count() > 1)
                        {
                            GameWorld.GamePlayerAssociation.ActiveParty.ReceivedPartyAdvice = true;

                            MakeObservation("While in a party, you will switch between each member and control them manually.", Color.HotPink, new EntityList<Entity>());
                            MakeObservation("Press Right Control or the Lock Symbol by the District Map to toggle Group-Move.", Color.HotPink, new EntityList<Entity>());
                            MakeObservation("While in group move, Numpad/CTRL+QWEADZXC/Click GUI move orders are given to the entire party.", Color.HotPink, new EntityList<Entity>());
                            MakeObservation("When all members are attempting to leave the district, all members will leave the area.", Color.HotPink, new EntityList<Entity>());
                        }


                        if (GameWorld.GamePlayerAssociation.ActiveParty.Architects.Count() == 0)
                        {
                            GameState = "dead";
                        }
                        else if (!(GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(LoadedArchitects[ArchitectIndex])))
                        {
                            GameState = "otherturn";
                        }
                        else if (CommandBuilderStage != "none")
                        {
                            if (CommandBuilderStage == "categories" && KeysNewlyPressed.Count() > 0)
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
                                if (KeysNewlyPressed.Count() > 0)
                                {
                                    var key = KeysNewlyPressed[0];
                                    switch (key)
                                    {
                                        case Keys.D1:
                                            index = 0;
                                            break;
                                        case Keys.D2:
                                            index = 1;
                                            break;
                                        case Keys.D3:
                                            index = 2;
                                            break;
                                        case Keys.D4:
                                            index = 3;
                                            break;
                                        case Keys.D5:
                                            index = 4;
                                            break;
                                        case Keys.D6:
                                            index = 5;
                                            break;
                                        case Keys.D7:
                                            index = 6;
                                            break;
                                        case Keys.D8:
                                            index = 7;
                                            break;
                                        case Keys.D9:
                                            index = 8;
                                            break;
                                        case Keys.D0:
                                            index = 9;
                                            break;
                                        case Keys.Q:
                                            index = 10;
                                            break;
                                        case Keys.W:
                                            index = 11;
                                            break;
                                        case Keys.E:
                                            index = 12;
                                            break;
                                        case Keys.R:
                                            index = 13;
                                            break;
                                        case Keys.T:
                                            index = 14;
                                            break;
                                        case Keys.Y:
                                            index = 15;
                                            break;
                                        case Keys.U:
                                            index = 16;
                                            break;
                                        case Keys.I:
                                            index = 17;
                                            break;
                                        case Keys.O:
                                            index = 18;
                                            break;
                                        case Keys.P:
                                            index = 19;
                                            break;
                                        default:
                                            // Handle invalid keys if necessary
                                            break;
                                    }
                                }

                                if (CommandBuilderStage == "commands" && index >= 0 && index < commands.Count())
                                {
                                    SelectedCommand = commands[index];
                                    // Set up RelevantEntities based on the selected command's required subjects

                                    if (RecognizedCommands[SelectedCommand].Item2.Count > 0)
                                    {
                                        SetUpRelevantEntities(RecognizedCommands[SelectedCommand].Item2[0]);
                                        // Sort RelevantEntities by proximity score
                                        RelevantEntities = RelevantEntities.OrderBy(e => CalculateProximityScore(e, MostRecentPartyTurnArchitect)).ToList();
                                        MaxCommandBuilderPage = (int)Math.Ceiling((decimal)RelevantEntities.Count() / 20);
                                        CurrentCommandBuilderPage = 0;
                                        CommandBuilderStage = "pickingsubjects";
                                    }
                                    else
                                    {
                                        CurrentCommandBuilderPage = 0;
                                        CommandBuilderStage = "execution";
                                    }
                                }
                                else if (CommandBuilderStage == "pickingsubjects")
                                {
                                    bool subjectSelected = false;
                                    var keys = new List<Keys> { Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0, Keys.Q, Keys.W, Keys.E, Keys.R, Keys.T, Keys.Y, Keys.U, Keys.I, Keys.O, Keys.P };

                                    foreach (var key in keys)
                                    {
                                        if (KeysNewlyPressed.Contains(key))
                                        {
                                            int Index = keys.IndexOf(key);
                                            int entityIndex = Index + (CurrentCommandBuilderPage * 20);
                                            if (entityIndex < RelevantEntities.Count())
                                            {
                                                SelectedEntities.Add(RelevantEntities[entityIndex]);
                                                subjectSelected = true;
                                                break;
                                            }
                                        }
                                    }

                                    if (!subjectSelected)
                                    {
                                        if (KeysNewlyPressed.Contains(Keys.A) || KeysNewlyPressed.Contains(Keys.Left))
                                        {
                                            if (CurrentCommandBuilderPage > 0)
                                            {
                                                CurrentCommandBuilderPage--;
                                            }
                                        }
                                        else if (KeysNewlyPressed.Contains(Keys.S) || KeysNewlyPressed.Contains(Keys.Right))
                                        {
                                            if (CurrentCommandBuilderPage < (MaxCommandBuilderPage - 1))
                                            {
                                                CurrentCommandBuilderPage++;
                                            }
                                        }
                                    }

                                    // Check if we have selected all required subjects
                                    if (SelectedEntities.Count() == RecognizedCommands[SelectedCommand].Item2.Count())
                                    {
                                        CommandBuilderStage = "execution";
                                    }
                                    else if (subjectSelected)
                                    {
                                        CurrentSubjectIndex++;
                                        if (CurrentSubjectIndex < RecognizedCommands[SelectedCommand].Item2.Count())
                                        {
                                            // Reset RelevantEntities for the next subject selection
                                            RelevantEntities.Clear();
                                            var nextSubject = RecognizedCommands[SelectedCommand].Item2[CurrentSubjectIndex];
                                            SetUpRelevantEntities(nextSubject);

                                            // Sort RelevantEntities by proximity score
                                            RelevantEntities = RelevantEntities.OrderBy(e => CalculateProximityScore(e, MostRecentPartyTurnArchitect)).ToList();
                                            MaxCommandBuilderPage = (int)Math.Ceiling((decimal)RelevantEntities.Count() / 20);
                                            CurrentCommandBuilderPage = 0;
                                        }
                                    }
                                }

                                if (CommandBuilderStage == "execution")
                                {
                                    CommandProcessor.RunCommand(MostRecentPartyTurnArchitect, SelectedCommand, SelectedEntities, LoadedArchitects, GameWorld, r, "attacks");
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
                            foreach (Architect a in GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                            {
                                a.ReferredToNames.Remove("myself");
                                a.ReferredToNames.Remove("me");
                            }
                            MostRecentPartyTurnArchitect.AddReferredToName("myself");
                            MostRecentPartyTurnArchitect.AddReferredToName("me");

                            if (LoadedArchitects[ArchitectIndex].CooldownCycles == 0 && LoadedArchitects[ArchitectIndex].IsAlive && LoadedArchitects[ArchitectIndex].HoldCycles == 0 && LoadedArchitects[ArchitectIndex].UnconsciousCycles == 0)
                            {
                                foreach (Architect a in LoadedArchitects)
                                {
                                    a.UpdateNames();
                                    if (a.Block != null)
                                    {
                                        EntityList<Object> NearbyObjects = a.Room != null ? a.Room.Objects : a.Block.Objects;

                                        foreach (Object o in NearbyObjects)
                                        {
                                            o.UpdateNames();
                                        }
                                    }
                                }

                                //do this list replacements in split mode


                                if (ThisList.Count() > 0 && SplitMode)
                                {
                                    // Get the first entity in ThisList
                                    var entity = ThisList[0];

                                    // Use regular expressions to find the first instance of " this " surrounded by spaces
                                    var prompt = MostRecentPartyTurnArchitect.Prompt;
                                    var match = Regex.Match(prompt, @"\bthis\b");

                                    if (match.Success)
                                    {
                                        GameWorld.GamePlayerAssociation.ActiveParty.UsedThis = true;

                                        // Replace the first instance of "this" with the referredToName
                                        prompt = prompt.Substring(0, match.Index) + entity.ID.ToString() + prompt.Substring(match.Index + match.Length);

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
                                        Exposition = new EntityList<TextStorage>();

                                        if (KeysNewlyPressed.Contains(Keys.OemTilde) && InInventory == false && Keyboard.GetState().IsKeyUp(Keys.Tab))
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
                                                if (MostRecentPartyTurnArchitect.PromptIndex == 0)
                                                {
                                                    // Save the current prompt before starting to navigate through history
                                                    MostRecentPartyTurnArchitect.SavedPrompt = MostRecentPartyTurnArchitect.Prompt;
                                                    MostRecentPartyTurnArchitect.PromptIndex = MostRecentPartyTurnArchitect.PreviousPrompts.Count(); // Start from the latest prompt
                                                }

                                                if (MostRecentPartyTurnArchitect.PromptIndex > 0)
                                                {
                                                    // Move to the previous prompt
                                                    MostRecentPartyTurnArchitect.PromptIndex -= 1;
                                                    MostRecentPartyTurnArchitect.Prompt = MostRecentPartyTurnArchitect.PreviousPrompts[MostRecentPartyTurnArchitect.PromptIndex];
                                                }
                                            }
                                            else if (k == Keys.Down)
                                            {
                                                if (MostRecentPartyTurnArchitect.PromptIndex != 0)
                                                {
                                                    if (MostRecentPartyTurnArchitect.PromptIndex < MostRecentPartyTurnArchitect.PreviousPrompts.Count() - 1)
                                                    {
                                                        // Move to the next prompt
                                                        MostRecentPartyTurnArchitect.PromptIndex += 1;
                                                        MostRecentPartyTurnArchitect.Prompt = MostRecentPartyTurnArchitect.PreviousPrompts[MostRecentPartyTurnArchitect.PromptIndex];
                                                    }
                                                    else
                                                    {
                                                        // Restore the saved prompt if we reach the initial position
                                                        MostRecentPartyTurnArchitect.PromptIndex = 0;
                                                        MostRecentPartyTurnArchitect.Prompt = MostRecentPartyTurnArchitect.SavedPrompt;
                                                    }
                                                }
                                            }
                                            if (k == Keys.Back && LoadedArchitects[ArchitectIndex].Prompt.Length != 0)
                                            {
                                                if (Keyboard.GetState().IsKeyDown(Keys.LeftControl))
                                                {
                                                    LoadedArchitects[ArchitectIndex].Prompt = "";
                                                }
                                                else
                                                {
                                                    LoadedArchitects[ArchitectIndex].Prompt = LoadedArchitects[ArchitectIndex].Prompt.Substring(0, LoadedArchitects[ArchitectIndex].Prompt.Length - 1);
                                                }
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



                                        if (KeysNewlyPressed.Contains(Keys.Escape) && InInventory == true)
                                        {
                                            InInventory = false;
                                        }

                                        if (MostRecentPartyTurnArchitect.NextMoveOrder != "" && MostRecentPartyTurnArchitect.Structure == null)
                                        {
                                            MostRecentPartyTurnArchitect.Move(MostRecentPartyTurnArchitect.NextMoveOrder);
                                            MostRecentPartyTurnArchitect.NextMoveOrder = "";
                                            IncrementAndCycleWorld();
                                        }
                                        else if (KeysNewlyPressed.Contains(Keys.Enter) || (currentMouseState.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released))
                                        {
                                            GameWorld.GamePlayerAssociation.ActiveParty.RanCommands++;

                                            if (GameWorld.GamePlayerAssociation.ActiveParty.RanCommands == 3 && GameWorld.GamePlayerAssociation.ActiveParty.UsedThis == false)
                                            {
                                                MakeObservation("To speed things up, you can press F3 to enable split mode. Using this, you can replace any instance of the word \"this\" in your prompt with any subject by clicking their name. For instance, try pressing F3, typing \"ask this their name\" and click a nearby person in the sentient list.", Color.LightSkyBlue, new EntityList<Entity>());
                                            }

                                            // Store the current prompt to the history if it's not empty
                                            if (!string.IsNullOrWhiteSpace(MostRecentPartyTurnArchitect.Prompt))
                                            {
                                                MostRecentPartyTurnArchitect.PreviousPrompts.Insert(1, MostRecentPartyTurnArchitect.Prompt); // Insert at the beginning
                                                MostRecentPartyTurnArchitect.PromptIndex = 0; // Reset the index to 0 (empty slate)
                                                MostRecentPartyTurnArchitect.SavedPrompt = ""; // Clear the saved prompt
                                            }

                                            MostRecentPartyTurnArchitect.NextMoveOrder = "";
                                            // Get the original command from the current architect
                                            string originalCommand = LoadedArchitects[ArchitectIndex].Prompt.ToLower().Trim();

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
                                            MostRecentPartyTurnArchitect.PreviousPrompts.Add(MostRecentPartyTurnArchitect.Prompt);

                                            // Determine modifier
                                            string Modifier = "none";
                                            if (commandParts.Length > 0 && (commandParts[0].StartsWith("get") || commandParts[0].StartsWith("grab") || (commandParts[0].StartsWith("take") && !commandParts[0].StartsWith("take off")) || commandParts[0].StartsWith("pick up") || commandParts[0].StartsWith("steal")))
                                            {
                                                Modifier = "get";
                                            }

                                            // Collect all subjects
                                            AllSubjects = CollectAllSubjects(MostRecentPartyTurnArchitect, Modifier);

                                            // body parts pls

                                            foreach (Object o in MostRecentPartyTurnArchitect.BodyParts)
                                            {
                                                o.AddReferredToName("my " + o.Type);
                                            }

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
                                                    return CommandProcessor.RunCommand(executor, commandId, subjects, LoadedArchitects, GameWorld, r, userInput);
                                                }

                                                // This would be called in your game loop or command handling part
                                                bool segmentSuccess = PreprocessAndRunCommand(MostRecentPartyTurnArchitect, processedCommandPart, relevantSubjectsForFirstPart);

                                                // If there are more parts of the command left, update the prompt with the remaining parts
                                                if (commandParts.Length > 1 && segmentSuccess)
                                                {
                                                    string remainingCommand = string.Join(" and ", commandParts.Skip(1)).Trim();
                                                    MostRecentPartyTurnArchitect.Prompt = remainingCommand;
                                                }
                                                else if (LoadedArchitects.Count() > 0)
                                                {
                                                    // If it was the last part or the command segment didn't run successfully, clear the prompt
                                                    MostRecentPartyTurnArchitect.Prompt = "";
                                                }
                                            }
                                        }
                                        else if (MostRecentPartyTurnArchitect.Structure == null)
                                        {
                                            bool altPressed = ShiftTestState.IsKeyDown(Keys.LeftControl);

                                            string StoredDirection = "";

                                            foreach (var key in KeysNewlyPressed)
                                            {
                                                bool isDirectionKey = ValidNumpadKeys.Contains(key);
                                                bool isAltMovementKey = altPressed && (key == Keys.Q || key == Keys.W || key == Keys.E || key == Keys.A || key == Keys.D || key == Keys.Z || key == Keys.X || key == Keys.C);

                                                if (isDirectionKey || isAltMovementKey)
                                                {
                                                    if (directionOffsets.TryGetValue(key, out var offset))
                                                    {
                                                        // Convert key direction to string direction for Move function
                                                        string direction = KeyDirections[key];
                                                        string fullDirection = ConvertToFullDirection(direction);
                                                        MostRecentPartyTurnArchitect.Move(fullDirection);
                                                        StoredDirection = fullDirection;
                                                        IncrementAndCycleWorld();
                                                    }
                                                }
                                            }

                                            bool isMouseClicked = currentMouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released;

                                            if (isMouseClicked)
                                            {
                                                Point mousePosition = currentMouseState.Position;
                                                if (RectangleN.Contains(mousePosition))
                                                {
                                                    MostRecentPartyTurnArchitect.Move("north");
                                                    StoredDirection = "north";
                                                }
                                                else if (RectangleNE.Contains(mousePosition))
                                                {
                                                    MostRecentPartyTurnArchitect.Move("northeast");
                                                    StoredDirection = "northeast";
                                                }
                                                else if (RectangleE.Contains(mousePosition))
                                                {
                                                    MostRecentPartyTurnArchitect.Move("east");
                                                    StoredDirection = "east";
                                                }
                                                else if (RectangleSE.Contains(mousePosition))
                                                {
                                                    MostRecentPartyTurnArchitect.Move("southeast");
                                                    StoredDirection = "southeast";
                                                }
                                                else if (RectangleS.Contains(mousePosition))
                                                {
                                                    MostRecentPartyTurnArchitect.Move("south");
                                                    StoredDirection = "south";
                                                }
                                                else if (RectangleSW.Contains(mousePosition))
                                                {
                                                    MostRecentPartyTurnArchitect.Move("southwest");
                                                    StoredDirection = "southwest";
                                                }
                                                else if (RectangleW.Contains(mousePosition))
                                                {
                                                    MostRecentPartyTurnArchitect.Move("west");
                                                    StoredDirection = "west";
                                                }
                                                else if (RectangleNW.Contains(mousePosition))
                                                {
                                                    MostRecentPartyTurnArchitect.Move("northwest");
                                                    StoredDirection = "northwest";
                                                }
                                                else if (RectangleCenter.Contains(mousePosition))
                                                {
                                                    if (MassTravelOrderMode)
                                                    {
                                                        MassTravelOrderMode = false;
                                                    }
                                                    else
                                                    {
                                                        MassTravelOrderMode = true;
                                                    }
                                                }

                                                if (StoredDirection != "")
                                                {
                                                    IncrementAndCycleWorld();
                                                }
                                            }

                                            if (MassTravelOrderMode)
                                            {
                                                if (StoredDirection != "")
                                                {
                                                    foreach (Architect a in GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                                                    {
                                                        if (a != MostRecentPartyTurnArchitect)
                                                        {
                                                            a.NextMoveOrder = StoredDirection;
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        string ConvertToFullDirection(string shortDirection)
                                        {
                                            switch (shortDirection)
                                            {
                                                case "N":
                                                    return "north";
                                                case "NE":
                                                    return "northeast";
                                                case "E":
                                                    return "east";
                                                case "SE":
                                                    return "southeast";
                                                case "S":
                                                    return "south";
                                                case "SW":
                                                    return "southwest";
                                                case "W":
                                                    return "west";
                                                case "NW":
                                                    return "northwest";
                                                default:
                                                    return shortDirection;
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

                            if (CurrentObjectPage > MaximumObjectPage)
                            {
                                CurrentObjectPage = 0;
                            }

                            if (LoadedArchitects.Count() != 0)
                            {
                                if (PlayerSpendableLevelsLastTick < MostRecentPartyTurnArchitect.SpendableLevels && MostRecentPartyTurnArchitect.SpendableLevels > 0)
                                {
                                    Announcements.Add(new TextStorage("You have leveled up to level " + MostRecentPartyTurnArchitect.Level + ". You have a new spendable level in Inventory.", Color.AliceBlue, new EntityList<Entity>(){}));
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
                            // Create the LightrealmSaves folder if it doesn't exist
                            string saveFolder = Path.Combine(DocumentsFolderPath, "LightrealmSaves", GameWorld.Name);
                            Directory.CreateDirectory(saveFolder);

                            // Create a list to hold the header and the historical events
                            List<string> fileContent = new List<string>
                            {
                                "Historical Events of " + GameWorld.Name,
                                "To find events related to a certain subject, try typing in their name.",
                                "" // Adding an empty line for better readability
                            };

                            // Add the historical events to the list
                            fileContent.AddRange(GameWorld.HistoricalEvents.Select(e => e.EventData));

                            // Write the content to the file
                            File.WriteAllLines(Path.Combine(saveFolder, $"history.txt"), fileContent.ToArray());


                            GameWorld = null;
                            Announcements = new EntityList<TextStorage>();
                            Observations = new EntityList<TextStorage>();
                            Messages = new EntityList<TextStorage>();
                            GameState = "mainscreen";


                            MediaPlayer.Stop();
                            MediaPlayer.Play(LightrealmMainTheme);
                        }
                    }
                    else if (GameState == "otherturn")
                    {
                        StoredAttack = null;

                        // We initialize a loop that continues until we find a party member or trigger a reaction state
                        while (true)
                        {
                            // Ensure the ArchitectIndex wraps around if it exceeds the number of loaded architects
                            ArchitectIndex %= LoadedArchitects.Count();
                            Architect currentArchitect = LoadedArchitects[ArchitectIndex];

                            Attack attack = currentArchitect.UpdateSelfActionsAndSuch();

                            if (attack != null)
                            {
                                if (attack.Attacker.IsAlive == false)
                                {
                                    continue;
                                }

                                if (GameWorld.GamePlayerAssociation.ActiveParty.Architects.Any(a => a.BodyParts.Contains(attack.Target)))
                                {
                                    StoredAttack = attack;
                                    GameState = "reaction";
                                }
                                else
                                {
                                    CalculateAttack(attack.Verb, attack.Attacker, attack.Target, "decideforme", attack.Weapon);
                                }
                            }

                            if (GameState == "reaction") break; // If reaction state is triggered, exit early

                            // If the architect is a party member, switch to party turn and break
                            if (GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(currentArchitect) && !currentArchitect.TemporarilyIncapacitated())
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
                                else if (currentArchitect.MessagesNotRespondedTo.Count() > 0)
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

                            if (GameState != "otherturn")
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

                            if (GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ % 2 == 0)
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

                            if (!(GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + DeltaX > GameWorld.Width || GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + DeltaX < 0 || GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ + DeltaZ > GameWorld.Length || GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ + DeltaZ < 0))
                            {
                                GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX += DeltaX;
                                GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ += DeltaZ;
                            }

                        }


                        if (KeysNewlyPressed.Contains(Keys.Enter))
                        {
                            GameWorld.TriggerRupture(GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX, GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ, MostRecentPartyTurnArchitect, 10);

                            MostRecentPartyTurnArchitect.RuptureMode = false;

                            if (MostRecentPartyTurnArchitect.Location.Region.Biome == "ethereal")
                            {
                                MakeObservation(MostRecentPartyTurnArchitect.Name + " successfully killed themselves in the fractal rift. How embarrassing...", Color.Magenta, new EntityList<Entity>() { LoadedArchitects[ArchitectIndex] });
                                MostRecentPartyTurnArchitect.IsAlive = false;
                                if (GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(MostRecentPartyTurnArchitect))
                                {
                                    GameWorld.GamePlayerAssociation.ActiveParty.Architects.Remove(MostRecentPartyTurnArchitect);

                                    if (GameWorld.GamePlayerAssociation.ActiveParty.Architects.Count() == 0)
                                    {
                                        GameState = "dead";

                                        if (GameWorld.GamePlayerAssociation.ActiveParty.Architects.Count() == 0)
                                        {
                                            GameState = "dead";
                                        }
                                    }
                                }
                            }
                            else
                            {
                                IncrementAndCycleWorld();

                                if (GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(MostRecentPartyTurnArchitect))
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
                        // Check if the target's creator has HoldCycles or UnconsciousCycles
                        if (StoredAttack != null &&
                            ((Architect)(StoredAttack.Target.Creator)).HoldCycles > 0 || ((Architect)(StoredAttack.Target.Creator)).UnconsciousCycles > 0)
                        {
                            CalculateAttack(StoredAttack.Verb, StoredAttack.Attacker, StoredAttack.Target, "sustain", StoredAttack.Weapon);
                            StoredAttack = null;
                        }
                        else if (KeysNewlyPressed.Any(key => key == Keys.S || key == Keys.P || key == Keys.B || key == Keys.D || key == Keys.J || key == Keys.R || key == Keys.N || key == Keys.C))
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
                                    break;
                                case Keys.B:
                                    Action = "block";
                                    break;
                                case Keys.D:
                                    Action = "duck";
                                    break;
                                case Keys.J:
                                    Action = "jump";
                                    break;
                                case Keys.R:
                                    Action = "roll";
                                    break;
                                case Keys.N:
                                    Action = "disarm";
                                    break;
                                case Keys.C:
                                    Action = "redirect";
                                    break;
                                default:
                                    break;
                            }

                            CalculateAttack(StoredAttack.Verb, StoredAttack.Attacker, StoredAttack.Target, Action, StoredAttack.Weapon);
                            StoredAttack = null;
                        }

                        if (StoredAttack == null)
                        {
                            IncrementAndCycleWorld();
                            GameState = "otherturn";
                        }
                    }

                    else if (GameState == "messagereply")
                    {
                        if (MostRecentPartyTurnArchitect.MessagesNotRespondedTo.Count() == 0)
                        {
                            GameState = "partyturn";
                        }
                        else if (MostRecentPartyTurnArchitect.HoldCycles > 0 || MostRecentPartyTurnArchitect.UnconsciousCycles > 0)
                        {
                            // Automatically respond with "does not reply" if the person is unconscious or held
                            AddMessage(MostRecentPartyTurnArchitect.ReferredToNames[0] + " does not reply.", Color.Yellow, new EntityList<Entity>() { MostRecentPartyTurnArchitect });
                            MessageWorldEdit(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Sender, MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Receiver, MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].MessageType, MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Subjects, "", new EntityList<Location>());
                            MostRecentPartyTurnArchitect.MessagesNotRespondedTo.RemoveAt(0);
                            GameState = "otherturn";
                        }
                        else if (KeysNewlyPressed.Any(key => key == Keys.T || key == Keys.M || key == Keys.I || key == Keys.D || key == Keys.F || key == Keys.R || key == Keys.X))
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
                                    // No action, no reply
                                    break;
                                default:
                                    break;
                            }

                            if (Reply != "")
                            {
                                AddMessage(MostRecentPartyTurnArchitect.ReferredToNames[0] + ": " + Reply, new Color(0, 255, 0), new EntityList<Entity>() { MostRecentPartyTurnArchitect }.Union(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Subjects));
                                MessageWorldEdit(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Sender, MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Receiver, MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].MessageID, MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Subjects, Reply, new EntityList<Location>());
                                MostRecentPartyTurnArchitect.MessagesNotRespondedTo.RemoveAt(0);
                                MostRecentPartyTurnArchitect.CooldownCycles += (int)Math.Round(30 / MostRecentPartyTurnArchitect.Speed());
                            }
                            else
                            {
                                AddMessage(MostRecentPartyTurnArchitect.ReferredToNames[0] + " does not reply.", Color.Yellow, new EntityList<Entity>() { MostRecentPartyTurnArchitect });
                                MessageWorldEdit(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Sender, MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Receiver, MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].MessageID, MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Subjects, "", new EntityList<Location>());
                                MostRecentPartyTurnArchitect.MessagesNotRespondedTo.RemoveAt(0);
                            }

                            GameState = "otherturn";
                        }
                    }
                    else if (GameState == "trypickup")
                    {
                        void PickUpItems(int count)
                        {
                            EntityList<Object> itemsToPickUp = (MostRecentPartyTurnArchitect.Room != null ? MostRecentPartyTurnArchitect.Room.Objects : MostRecentPartyTurnArchitect.Block.Objects)
                            .Where(item => item.Type == MostRecentPartyTurnArchitect.TryPickUpItemType && item.Materials.SequenceEqual(MostRecentPartyTurnArchitect.TryPickUpMaterials))
                            .Take(count);


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
                                MakeObservation("You pick up the " + item.ReferredToNames[0] + " and put it in your inventory.", Color.Yellow, new EntityList<Entity>() { item });

                                // Relieve market debt if in a market
                                if (MostRecentPartyTurnArchitect.Structure != null && MostRecentPartyTurnArchitect.Structure.Type == "market")
                                {
                                    MostRecentPartyTurnArchitect.Structure.MarketDebt -= item.Value();
                                }

                                MostRecentPartyTurnArchitect.CooldownCycles += (int)(Math.Round(5 / MostRecentPartyTurnArchitect.Speed()));
                            }
                        }

                        int GetHalfCount(string itemType, EntityHashSet<Material> itemMaterials)
                        {
                            int totalCount = (MostRecentPartyTurnArchitect.Room != null ? MostRecentPartyTurnArchitect.Room.Objects : MostRecentPartyTurnArchitect.Block.Objects)
                                .Count(item => item.Type == itemType && item.Materials.SequenceEqual(itemMaterials));
                            return (int)Math.Ceiling(totalCount / 2.0);
                        }

                        int GetFullCount(string itemType, EntityHashSet<Material> itemMaterials)
                        {
                            return (MostRecentPartyTurnArchitect.Room != null ? MostRecentPartyTurnArchitect.Room.Objects : MostRecentPartyTurnArchitect.Block.Objects)
                                .Count(item => item.Type == itemType && item.Materials.SequenceEqual(itemMaterials));
                        }

                        if (KeysNewlyPressed.Contains(Keys.D1))
                        {
                            PickUpItems(1); // Pick up one item of the selected type and material
                            MostRecentPartyTurnArchitect.TryPickUpItemType = "";
                            MostRecentPartyTurnArchitect.TryPickUpMaterials = new EntityHashSet<Material>();
                            IncrementAndCycleWorld();
                            GameState = "otherturn";
                        }
                        else if (KeysNewlyPressed.Contains(Keys.D2))
                        {
                            PickUpItems(GetHalfCount(MostRecentPartyTurnArchitect.TryPickUpItemType, MostRecentPartyTurnArchitect.TryPickUpMaterials)); // Pick up half of the items of the selected type and material
                            MostRecentPartyTurnArchitect.TryPickUpItemType = "";
                            MostRecentPartyTurnArchitect.TryPickUpMaterials = new EntityHashSet<Material>();
                            IncrementAndCycleWorld();
                            GameState = "otherturn";
                        }
                        else if (KeysNewlyPressed.Contains(Keys.D3))
                        {
                            PickUpItems(GetFullCount(MostRecentPartyTurnArchitect.TryPickUpItemType, MostRecentPartyTurnArchitect.TryPickUpMaterials)); // Pick up all items of the selected type and material
                            MostRecentPartyTurnArchitect.TryPickUpItemType = "";
                            MostRecentPartyTurnArchitect.TryPickUpMaterials = new EntityHashSet<Material>();
                            IncrementAndCycleWorld();
                            GameState = "otherturn";
                        }
                    }

                    else if (GameState == "trydrop")
                    {
                        void DropItems(int count)
                        {
                            EntityList<Object> itemsToDrop = MostRecentPartyTurnArchitect.Inventory
                                .Where(item => item.Type == MostRecentPartyTurnArchitect.TryDropItemType && item.Materials.SequenceEqual(MostRecentPartyTurnArchitect.TryDropMaterials))
                                .Take(count);

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
                                MakeObservation("You drop the " + item.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { item });

                                // Incur market debt if in a market
                                if (MostRecentPartyTurnArchitect.Structure != null && MostRecentPartyTurnArchitect.Structure.Type == "market")
                                {
                                    MostRecentPartyTurnArchitect.Structure.MarketDebt += item.Value();
                                }

                                MostRecentPartyTurnArchitect.CooldownCycles += (int)(Math.Round(5 / MostRecentPartyTurnArchitect.Speed()));
                            }
                        }

                        int GetHalfCount(string itemType, EntityHashSet<Material> itemMaterials)
                        {
                            int totalCount = MostRecentPartyTurnArchitect.Inventory.Count(item => item.Type == itemType && item.Materials.SequenceEqual(itemMaterials));
                            return (int)Math.Ceiling(totalCount / 2.0);
                        }

                        int GetFullCount(string itemType, EntityHashSet<Material> itemMaterials)
                        {
                            return MostRecentPartyTurnArchitect.Inventory.Count(item => item.Type == itemType && item.Materials.SequenceEqual(itemMaterials));
                        }

                        if (KeysNewlyPressed.Contains(Keys.D1))
                        {
                            DropItems(1); // Drop one item of the selected type and material
                            MostRecentPartyTurnArchitect.TryDropItemType = "";
                            MostRecentPartyTurnArchitect.TryDropMaterials = new EntityHashSet<Material>();
                            GameState = "otherturn";
                        }
                        else if (KeysNewlyPressed.Contains(Keys.D2))
                        {
                            DropItems(GetHalfCount(MostRecentPartyTurnArchitect.TryDropItemType, MostRecentPartyTurnArchitect.TryDropMaterials)); // Drop half of the items of the selected type and material
                            MostRecentPartyTurnArchitect.TryDropItemType = "";
                            MostRecentPartyTurnArchitect.TryDropMaterials = new EntityHashSet<Material>();
                            GameState = "otherturn";
                        }
                        else if (KeysNewlyPressed.Contains(Keys.D3))
                        {
                            DropItems(GetFullCount(MostRecentPartyTurnArchitect.TryDropItemType, MostRecentPartyTurnArchitect.TryDropMaterials)); // Drop all items of the selected type and material
                            MostRecentPartyTurnArchitect.TryDropItemType = "";
                            MostRecentPartyTurnArchitect.TryDropMaterials = new EntityHashSet<Material>();
                            GameState = "otherturn";
                        }
                    }




                    else if (GameState == "travelmenu")
                    {
                        Exposition = new EntityList<TextStorage>();

                        if (KeysNewlyPressed.Contains(Keys.A))
                        {
                            GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Width].DeepExplored = true;

                            if (GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Width].Location != null)
                            {
                                GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Width].Location.Explored = true;
                            }
                        }


                        if ((Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.RightControl)) && Keyboard.GetState().IsKeyDown(Keys.S))
                        {
                            GameState = "savinggame";
                        }

                        if (KeysNewlyPressed.Contains(Keys.R) || KeysNewlyPressed.Contains(Keys.T) || KeysNewlyPressed.Contains(Keys.D) || KeysNewlyPressed.Contains(Keys.G) || KeysNewlyPressed.Contains(Keys.C) || KeysNewlyPressed.Contains(Keys.V) || KeysNewlyPressed.Contains(Keys.NumPad7) || KeysNewlyPressed.Contains(Keys.NumPad9) || KeysNewlyPressed.Contains(Keys.NumPad4) || KeysNewlyPressed.Contains(Keys.NumPad6) || KeysNewlyPressed.Contains(Keys.NumPad1) || KeysNewlyPressed.Contains(Keys.NumPad3))
                        {
                            int DeltaX = 0;
                            int DeltaZ = 0;

                            if (GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ % 2 == 0)
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

                            if (!(GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + DeltaX >= GameWorld.Width || GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + DeltaX < 0 || GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ + DeltaZ >= GameWorld.Length || GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ + DeltaZ < 0))
                            {
                                bool isDestinationOcean = GameWorld.WorldMap[(GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + DeltaX) + ((GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ + DeltaZ) * GameWorld.Width)].Biome == "ocean";
                                bool isDestinationVoid = GameWorld.WorldMap[(GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + DeltaX) + ((GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ + DeltaZ) * GameWorld.Width)].Biome == "void";
                                bool isDestinationEthereal = GameWorld.WorldMap[(GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + DeltaX) + ((GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ + DeltaZ) * GameWorld.Width)].Biome == "ethereal";
                                bool isDestinationPort = (!string.IsNullOrEmpty(GameWorld.WorldMap[(GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + DeltaX) + ((GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ + DeltaZ) * GameWorld.Width)].PortName)) || (GameWorld.WorldMap[(GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + DeltaX) + ((GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ + DeltaZ) * GameWorld.Width)].Location != null && GameWorld.WorldMap[(GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + DeltaX) + ((GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ + DeltaZ) * GameWorld.Width)].Location.Type == "cove");
                                bool isCurrentLocationWater = GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + (GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Width)].Biome == "ocean";

                                // Allow moving to an ocean tile if it has a port, and always allow moving from water to any tile
                                if (!isDestinationVoid && !isDestinationEthereal && (isCurrentLocationWater || (!isDestinationOcean || isDestinationPort)))
                                {
                                    GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX += DeltaX;
                                    GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ += DeltaZ;

                                    Game1.GameWorld.RevealNearbyTiles(GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX, GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ, 3, true);

                                    //events lmaoooooz


                                    //THIS ASSSUMES THAT A TILE IS APPROXIMATELY 2.5 KM^2 IN SIZE. THE ENTIRE ISLAND (if 128x128) WOULD BE ABOUT A 300 KILOMETER DIAMETER ISLAND. (not counting the ocean)
                                    //So the world is about the size of Iceland, I suppose.
                                    //It would take about 30 minutes to "walk" 2.5 KM. IF you have increased speed ill make it faster ig. 

                                    //but its based on the weakest link in your party.



                                    GameWorld.Cycle += (int)Math.Round(18000 / GameWorld.GamePlayerAssociation.ActiveParty.Architects.Min(architect => architect.Speed()));

                                    int currentDay = (int)Math.Round((double)GameWorld.Cycle / 864000);

                                    if (currentDay > LatestTraveledDay)
                                    {
                                        GameWorld.ProgressDays(1, false);
                                        LatestTraveledDay = currentDay;
                                    }


                                    if (StoredEvent != null)
                                    {
                                        if (StoredEvent.Region.X != GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX || StoredEvent.Region.Z != GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ)
                                        {
                                            StoredEvent = null;
                                        }
                                    }
                                    else
                                    {
                                        foreach (InteractableEvent e in GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Width].Events)
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

                        if (GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Width].Location == null)
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

                            if (GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Width].Biome == "ocean")
                            {
                                if (GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Width].PortName != "")
                                {
                                    Exposition.Add(new TextStorage("You can leave this port to start sailing.", Color.LightBlue, new EntityList<Entity>(){}));
                                }
                                else
                                {
                                    Exposition.Add(new TextStorage("You are currently sailing.", Color.LightBlue, new EntityList<Entity>(){}));
                                }
                            }
                            else
                            {
                                Exposition.Add(new TextStorage("The area is vacant and beautiful.", Color.Magenta, new EntityList<Entity>(){}));
                                Exposition.Add(new TextStorage("You could gather " + ConvertListToString(biomeDictionary[GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * 128].Biome]) + " here.", Color.Magenta, new EntityList<Entity>(){}));
                                Exposition.Add(new TextStorage("Press [S] to stop here.", Color.Magenta, new EntityList<Entity>(){}));
                            }
                        }

                        if (StoredEvent != null)
                        {
                            Exposition.Clear();
                            Exposition.Add(new TextStorage(StoredEvent.Intrigue, Color.LimeGreen, new EntityList<Entity>(){}));
                            Exposition.Add(new TextStorage("Press [E] to approach.", Color.Red, new EntityList<Entity>(){}));
                        }



                        if (KeysNewlyPressed.Contains(Keys.S) && GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Width].Location == null && !Keyboard.GetState().IsKeyDown(Keys.LeftControl))
                        {
                            GameState = "gathering";
                        }
                        else if (KeysNewlyPressed.Contains(Keys.E) && StoredEvent != null)
                        {
                            GameWorld.GamePlayerAssociation.ActiveParty.CurrentEvent = StoredEvent;
                            GameState = "exposition";
                            StoredEvent = null;
                        }

                        if (KeysNewlyPressed.Contains(Keys.OemComma))
                        {
                            GameWorld.GamePlayerAssociation.ActiveParty.MapCursorDistrict -= 1;
                            if (GameWorld.GamePlayerAssociation.ActiveParty.MapCursorDistrict < 0)
                            {
                                GameWorld.GamePlayerAssociation.ActiveParty.MapCursorDistrict = GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Width].Location.Districts.Count() - 1;
                            }
                        }
                        if (KeysNewlyPressed.Contains(Keys.OemPeriod))
                        {
                            GameWorld.GamePlayerAssociation.ActiveParty.MapCursorDistrict += 1;
                            if (GameWorld.GamePlayerAssociation.ActiveParty.MapCursorDistrict > GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Width].Location.Districts.Count() - 1)
                            {
                                GameWorld.GamePlayerAssociation.ActiveParty.MapCursorDistrict = 0;
                            }
                        }


                        if (KeysNewlyPressed.Contains(Keys.Enter) && GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Width].Location != null)
                        {
                            GameState = "exposition";
                            Location newLocation = GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Width].Location;
                            District newDistrict = newLocation.Districts[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorDistrict];

                            // Create a collective arrival message for the party
                            Exposition.Add(new TextStorage("You arrive at " + newLocation.Name + ", on the outskirts of " + newDistrict.Name + ".", Color.LightBlue, new EntityList<Entity>(){}));
                            Exposition.Add(new TextStorage("Press SPACE to continue...", Color.LightBlue, new EntityList<Entity>(){}));

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
                            int randomBlockIndex = outskirtsBlockIndexes[r.Next(outskirtsBlockIndexes.Count())];
                            Block arrivalBlock = newDistrict.DistrictMap[randomBlockIndex];

                            // Load the new location and update all architects
                            newDistrict.Load();
                            foreach (Architect architect in GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                            {
                                architect.Location = newLocation;
                                architect.District = newDistrict;
                                architect.Block = arrivalBlock;  // Set all architects to the randomly selected outskirts block
                                architect.TryingToTravel = false;  // Reset travel intent

                                int Month = ((int)Math.Round((decimal)(Game1.GameWorld.Cycle / 24192000)) % 12) + 1;
                                int Year = (int)Math.Round((decimal)(Game1.GameWorld.Cycle / 290304000), MidpointRounding.ToZero);
                                string Date = "(" + Month + "/" + Year + ")";
                                Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + architect.Name + " arrived in " + architect.Location.Name + ".", architect.Location.Region, new EntityList<Entity>(){architect, architect.Location}));
                            }
                        }



                    }
                    else if (GameState == "exposition")
                    {
                        if (GameWorld.GamePlayerAssociation.ActiveParty.CurrentEvent != null)
                        {
                            Exposition = new EntityList<TextStorage>
    {
        new TextStorage(GameWorld.GamePlayerAssociation.ActiveParty.CurrentEvent.Info, Color.White, new EntityList<Entity>()),
        new TextStorage("[Y] Approach cautiously.", Color.Yellow, new EntityList<Entity>()),
        new TextStorage("[N] Avoid the encounter.", Color.Yellow, new EntityList<Entity>())
    };

                            if (KeysNewlyPressed.Contains(Keys.Y))
                            {
                                Location newLocation = new Location("clearing", GameWorld.GetRace(""), 0, 0, 0, GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX, GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ, GameWorld.GamePlayerAssociation.ActiveParty.CurrentEvent.HomeCivilization, GameWorld.GamePlayerAssociation.ActiveParty.CurrentEvent.Region, "none");
                                District newDistrict = newLocation.Districts[0];

                                // Define the blocks on the outskirts of a 7x7 district grid
                                List<int> outskirtsBlockIndexes = new List<int>
        {
            // Top row and bottom row
            0, 1, 2, 3, 4, 5, 6, 42, 43, 44, 45, 46, 47, 48,
            // Left column and right column, excluding corners already included
            7, 14, 21, 28, 35, 13, 20, 27, 34, 41
        };

                                // Define the blocks that are not on the outskirts
                                List<int> nonOutskirtsBlockIndexes = new List<int>();
                                for (int i = 0; i < 49; i++)
                                {
                                    if (!outskirtsBlockIndexes.Contains(i))
                                    {
                                        nonOutskirtsBlockIndexes.Add(i);
                                    }
                                }

                                // Randomly pick one block from the outskirts
                                Random r = new Random();
                                int randomOutskirtsBlockIndex = outskirtsBlockIndexes[r.Next(outskirtsBlockIndexes.Count())];
                                Block arrivalBlock = newDistrict.DistrictMap[randomOutskirtsBlockIndex];

                                newDistrict.Load();

                                LoadedArchitects.Clear();

                                // Update all architects in the party
                                foreach (Architect architect in GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                                {
                                    architect.Location = newLocation;
                                    architect.District = newDistrict;
                                    architect.Block = arrivalBlock;  // Set all party architects to the randomly selected outskirts block
                                    architect.TryingToTravel = false;  // Reset travel intent
                                    arrivalBlock.Architects.Add(architect);
                                }

                                // Update all guaranteed architects from the event
                                foreach (Architect a in GameWorld.GamePlayerAssociation.ActiveParty.CurrentEvent.GuaranteedArchitects)
                                {
                                    a.Location = newLocation;
                                    a.District = newDistrict;
                                    // Randomly pick one block from the non-outskirts blocks
                                    int randomNonOutskirtsBlockIndex = nonOutskirtsBlockIndexes[r.Next(nonOutskirtsBlockIndexes.Count())];
                                    Block randomBlock = newDistrict.DistrictMap[randomNonOutskirtsBlockIndex];
                                    a.Block = randomBlock;  // Set the architect to a randomly selected non-outskirts block
                                    a.TryingToTravel = false;  // Reset travel intent
                                    randomBlock.Architects.Add(a);
                                    LoadedArchitects.Add(a);
                                }

                                foreach (Architect a in GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                                {
                                    if (!LoadedArchitects.Contains(a))
                                    {
                                        LoadedArchitects.Add(a);
                                    }
                                }

                                GameState = "partyturn";
                            }
                            else if (KeysNewlyPressed.Contains(Keys.N))
                            {
                                GameWorld.GamePlayerAssociation.ActiveParty.CurrentEvent = null;
                                StoredEvent = null;
                                GameState = "travelmenu";
                                GameWorld.GamePlayerAssociation.ActiveParty.ClearSkillData();
                            }
                        }

                        else
                        {
                            if (KeysNewlyPressed.Contains(Keys.Space))
                            {
                                if (SeenTips == true)
                                {
                                    GameState = "otherturn";
                                    GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].UpdateNames();

                                    if (InspirationSelected != "")
                                    {
                                        Announcements.Add(new TextStorage($"Selected inspiration: {InspirationSelected}", Color.White, new EntityList<Entity>(){}));

                                        if (InspirationSelected == "Learn a random offensive spell.")
                                        {
                                            List<string> offSpells = new List<string> { "expel", "water bolt", "chaos flare", "flash flame", "ice shock" };
                                            var knownSpells = GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].SpellsKnown;

                                            EntityList<Entity> OffensiveSpells = new EntityList<Entity>(
                                                Game1.GameWorld.AllSpells
                                                    .Where(spell => offSpells.Contains(spell.Metadata))
                                                    .Except(knownSpells)
                                            );



                                            if (OffensiveSpells.Any())
                                            {
                                                Random random = new Random();
                                                Entity randomSpell = OffensiveSpells[random.Next(OffensiveSpells.Count())];
                                                knownSpells.Add(randomSpell);
                                                Announcements.Add(new TextStorage($"Learned a new spell: {randomSpell.Metadata}", Color.White, new EntityList<Entity>(){}));
                                                Announcements.Add(new TextStorage(SkillSpellDescriptions[randomSpell.Metadata], Color.Cyan, new EntityList<Entity>(){}));

                                            }
                                            else
                                            {
                                                Announcements.Add(new TextStorage($"Cannot learn one because you somehow know them all...", Color.Red, new EntityList<Entity>(){}));

                                            }
                                        }
                                        else if (InspirationSelected == "Learn a random skill.")
                                        {
                                            EntityList<Entity> knownSkills = GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].SkillsKnown;
                                            EntityList<Entity> availableSkills = GameWorld.AllSkills.Except(knownSkills);
                                            if (availableSkills.Any())
                                            {
                                                Random random = new Random();
                                                Entity randomSkill = availableSkills[random.Next(availableSkills.Count())];
                                                knownSkills.Add(randomSkill);
                                                Announcements.Add(new TextStorage($"Learned a new random skill: {randomSkill.Metadata}", Color.White, new EntityList<Entity>(){}));
                                                Announcements.Add(new TextStorage(SkillSpellDescriptions[randomSkill.Metadata], Color.Cyan, new EntityList<Entity>(){}));
                                            }
                                            else
                                            {
                                                Announcements.Add(new TextStorage($"Cannot learn one because you somehow know them all...", Color.Red, new EntityList<Entity>(){}));
                                            }
                                        }
                                        else if (InspirationSelected == "Gain 2 random stat improvements.")
                                        {
                                            List<string> stats = new List<string> { "Strength", "Dexterity", "Agility", "Endurance", "Charisma", "Focus", "Creativity" };
                                            Random random = new Random();
                                            for (int i = 0; i < 2; i++)
                                            {
                                                if (stats.Count() == 0) break;
                                                int index = random.Next(stats.Count());
                                                string statToIncrease = stats[index];
                                                stats.RemoveAt(index);
                                                switch (statToIncrease)
                                                {
                                                    case "Strength":
                                                        GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Strength += 1;
                                                        Announcements.Add(new TextStorage("Strength increased by 1.", Color.White, new EntityList<Entity>(){}));
                                                        break;
                                                    case "Dexterity":
                                                        GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Dexterity += 1;
                                                        Announcements.Add(new TextStorage("Dexterity increased by 1.", Color.White, new EntityList<Entity>(){}));
                                                        break;
                                                    case "Agility":
                                                        GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Agility += 1;
                                                        Announcements.Add(new TextStorage("Agility increased by 1.", Color.White, new EntityList<Entity>(){}));
                                                        break;
                                                    case "Endurance":
                                                        GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Endurance += 1;
                                                        Announcements.Add(new TextStorage("Endurance increased by 1.", Color.White, new EntityList<Entity>(){}));
                                                        break;
                                                    case "Charisma":
                                                        GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Charisma += 1;
                                                        Announcements.Add(new TextStorage("Charisma increased by 1.", Color.White, new EntityList<Entity>(){}));
                                                        break;
                                                    case "Focus":
                                                        GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Focus += 1;
                                                        Announcements.Add(new TextStorage("Focus increased by 1.", Color.White, new EntityList<Entity>(){}));
                                                        break;
                                                    case "Creativity":
                                                        GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Creativity += 1;
                                                        Announcements.Add(new TextStorage("Creativity increased by 1.", Color.White, new EntityList<Entity>(){}));
                                                        break;
                                                }
                                            }
                                        }
                                        else if (InspirationSelected == "Gain +5 Max Energy.")
                                        {
                                            GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].MaxEnergyInspiration = true;
                                            Announcements.Add(new TextStorage("Your max energy has been increased.", Color.White, new EntityList<Entity>(){}));
                                        }
                                        else if (InspirationSelected == "Obtain a minor magical item.")
                                        {
                                            var magicalItems = GameWorld.MagicalSuperLoot(5);
                                            GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].Inventory.Add(magicalItems);
                                            Announcements.Add(new TextStorage("You have obtained a magical item, an imbued " + magicalItems.ReferredToNames[0] + ".", Color.White, new EntityList<Entity>(){}));
                                        }

                                        InspirationSelected = "";
                                        InspirationsChosen.Clear();
                                    }
                                }
                                else
                                {
                                    SeenTips = true;
                                    Exposition.Clear();
                                    Exposition.Add(new TextStorage("Press F5 for help with vocal/GUI/typed commands and navigating the world.", Color.White, new EntityList<Entity>(){}));
                                    Exposition.Add(new TextStorage("Watch your Energy and Bleeding, dictated by the Heart and its droplets at the top of the screen. Do not let it go dark.", Color.White, new EntityList<Entity>(){}));
                                    Exposition.Add(new TextStorage("Access player information with commands or use \"open menu\" to see all of it.", Color.White, new EntityList<Entity>(){}));
                                    Exposition.Add(new TextStorage("Press CTRL + S to save your game, while outside of a district. Instability is uncommon, but expected, so save your game often.", Color.White, new EntityList<Entity>(){}));
                                    Exposition.Add(new TextStorage("Good luck...", Color.White, new EntityList<Entity>(){}));
                                    Exposition.Add(new TextStorage("", Color.White, new EntityList<Entity>(){}));
                                    Exposition.Add(new TextStorage("", Color.White, new EntityList<Entity>(){}));
                                    Exposition.Add(new TextStorage("", Color.White, new EntityList<Entity>(){}));
                                    Exposition.Add(new TextStorage("", Color.White, new EntityList<Entity>(){}));
                                    Exposition.Add(new TextStorage("", Color.White, new EntityList<Entity>(){}));
                                    Exposition.Add(new TextStorage("", Color.White, new EntityList<Entity>(){}));
                                    Exposition.Add(new TextStorage("", Color.White, new EntityList<Entity>(){}));
                                    Exposition.Add(new TextStorage("Press SPACE to continue...", Color.White, new EntityList<Entity>(){}));
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
                            GameWorld.GamePlayerAssociation.ActiveParty.ClearSkillData();
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
        {"forest", GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].HarvestableWood},
        {"lightforest", GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].HarvestableWood},
        {"taiga", GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].HarvestableWood},
        {"mountain", GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].HarvestableStone},
        {"snowpeak", GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].HarvestableMetal},
        {"desert", GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].HarvestableSand},
        {"plains", GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].HarvestableFiber},
        {"tundra", GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].HarvestableIce},
        {"ice", GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].HarvestableIce}
    };

                        // Mapping materials to the type of resource they represent
                        Dictionary<Material, string> ResourcetoType = new Dictionary<Material, string>()
    {
        {GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].HarvestableWood, "log"},
        {GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].HarvestableStone, "stone"},
        {GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].HarvestableMetal, "ore"},
        {GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].HarvestableSand, "pile"},
        {GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].HarvestableFiber, "bunch"},
        {GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].HarvestableIce, "block"}
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

                        // Get the current biome
                        string currentBiome = GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].Biome;

                        // Iterate through each mapped key to resource type
                        foreach (var resourceKey in resourceKeyMap)
                        {
                            if (KeysNewlyPressed.Contains((Keys)Enum.Parse(typeof(Keys), $"D{resourceKey.Key}")))
                            {
                                // Check if the biome has the corresponding resource
                                if (harvestableMaterials.ContainsKey(currentBiome) && harvestableMaterials[currentBiome].Type.Contains(resourceKey.Value))
                                {
                                    Material resourceMaterial = harvestableMaterials[currentBiome];
                                    string resourceType = ResourcetoType[resourceMaterial];
                                    string toolRequired = gatheringActions.ContainsKey(currentBiome) ? gatheringActions[currentBiome] : null;
                                    bool toolNeeded = true;

                                    foreach (var architect in GameWorld.GamePlayerAssociation.ActiveParty.Architects)
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

                                            // List of break activities for a single architect in the party
                                            List<string> soloBreakActivities = new List<string>()
                                            {
                                                "sleeps for a bit",
                                                "takes a moment to meditate on the current state that is",
                                                "enjoys the solitude",
                                                "practices some skills",
                                                "sketches the surroundings",
                                                "sings quietly to themselves",
                                                "examines the environment",
                                                "reflects on the past",
                                                "stretches and relaxes"
                                            };

                                            // Code inside the harvesting loop
                                            if (count > 0)
                                            {
                                                Exposition.Add(new TextStorage($"{architect.Name} harvested {count} {resourceType}.", Color.LightBlue, new EntityList<Entity>(){}));
                                                for (int i = 0; i < count; i++)
                                                {
                                                    architect.Inventory.Add(new Object(null, resourceType, new EntityList<Material>() { resourceMaterial }, null));
                                                }
                                            }
                                            else
                                            {
                                                // Select a random break activity based on the number of architects in the party
                                                string breakActivity;
                                                if (GameWorld.GamePlayerAssociation.ActiveParty.Architects.Count() == 1)
                                                {
                                                    breakActivity = soloBreakActivities[r.Next(soloBreakActivities.Count())];
                                                }
                                                else
                                                {
                                                    breakActivity = breakActivities[r.Next(breakActivities.Count())];
                                                }
                                                Exposition.Add(new TextStorage($"{architect.Name} {breakActivity}.", Color.LightBlue, new EntityList<Entity>(){}));
                                            }
                                        }
                                    }


                                    if (toolNeeded)
                                    {
                                        Exposition.Add(new TextStorage($"You need a {toolRequired} to harvest {resourceMaterial.Type} here.", Color.Yellow, new EntityList<Entity>(){}));
                                    }
                                }
                                else
                                {
                                    Exposition.Add(new TextStorage($"There is no {resourceKey.Value.ToLower().Replace("harvestable", "")} to harvest in this biome.", Color.Red, new EntityList<Entity>(){}));
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
                                    RecipeIndex = Recipes.Count() - 1;
                                }
                            }
                            else if (KeysNewlyPressed.Contains(Keys.Down) || KeysNewlyPressed.Contains(Keys.NumPad2))
                            {
                                RecipeIndex++;

                                if (RecipeIndex > Recipes.Count() - 1)
                                {
                                    RecipeIndex = 0;
                                }
                            }
                            if (KeysNewlyPressed.Contains(Keys.Enter))
                            {
                                CraftingPhase = "selectingredients";
                                InventoryCraftingIndex = 0;
                                IndexesForResources = new List<int>();  // Clear the previous selection
                            }
                        }
                        else if (CraftingPhase == "selectingredients")
                        {
                            if (KeysNewlyPressed.Contains(Keys.Escape))
                            {
                                CraftingPhase = "selectrecipe";
                                IndexesForResources = new List<int>();
                            }

                            Dictionary<string, string> materialToObjectMap = new Dictionary<string, string>
        {
            {"cloth", "bolt"},
            {"wood", "log"},
            {"stone", "stone"},
            {"metal", "bar"},
            {"ore", "ore"},
            {"gemstone", "cut gem"},
            {"sand", "pile"},
            {"fiber", "bunch"},
            {"ice", "block"},
            {"glass", "sheet"}
        };

                            var currentRecipe = Recipes[RecipeIndex];
                            var currentRecipeMaterials = currentRecipe.Item2
                                .Distinct()
                                .Select(mat => materialToObjectMap[mat])
                                ;

                            EntityList<Object> relevantItems = MostRecentPartyTurnArchitect.Inventory
                                .Where(obj => currentRecipeMaterials.Contains(obj.Type));

                            if (KeysNewlyPressed.Contains(Keys.Up) || KeysNewlyPressed.Contains(Keys.NumPad8))
                            {
                                InventoryCraftingIndex--;

                                if (InventoryCraftingIndex < 0)
                                {
                                    InventoryCraftingIndex = relevantItems.Count() - 1;
                                }
                            }
                            else if (KeysNewlyPressed.Contains(Keys.Down) || KeysNewlyPressed.Contains(Keys.NumPad2))
                            {
                                InventoryCraftingIndex++;

                                if (InventoryCraftingIndex > relevantItems.Count() - 1)
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
                                EntityList<Material> allUsedMaterials = new EntityList<Material>();  // List to store all used materials
                                var requiredItems = currentRecipe.Item2
                                    .GroupBy(x => x)
                                    .ToDictionary(g => g.Key, g => g.Count());

                                bool hasAllIngredients = true;
                                foreach (var requiredItem in requiredItems)
                                {
                                    string requiredType = requiredItem.Key;
                                    int requiredCount = requiredItem.Value;

                                    // Count the selected items of the required type
                                    int selectedCount = IndexesForResources
                                        .Select(index => relevantItems[index])
                                        .Count(obj => obj.Type == materialToObjectMap[requiredType]);

                                    if (selectedCount < requiredCount)
                                    {
                                        hasAllIngredients = false;
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

                                        // Collect indices to be removed after the iteration
                                        var indicesToRemove = new List<int>();

                                        // Find and remove the required number of items from selected indexes in inventory
                                        foreach (var index in IndexesForResources.ToList())
                                        {
                                            var item = relevantItems[index];
                                            if (item.Type == requiredObject)
                                            {
                                                foreach (var material in item.Materials)
                                                {
                                                    // Check if a material with the same name already exists in allUsedMaterials
                                                    if (!allUsedMaterials.Any(m => m.Name == material.Name))
                                                    {
                                                        allUsedMaterials.Add(material); // Add the material of the consumed item if it's not already added
                                                    }
                                                    materialsAdded++; // Increment materialsAdded in either case
                                                }

                                                MostRecentPartyTurnArchitect.Inventory.Remove(item);
                                                indicesToRemove.Add(index); // Add the index to the removal list

                                                // Break out of the loop if the required amount of materials has been added
                                                if (materialsAdded >= requiredCount)
                                                    break;
                                            }
                                        }

                                        // Remove the indices from selected resources
                                        foreach (var index in indicesToRemove)
                                        {
                                            IndexesForResources.Remove(index);
                                        }
                                    }

                                    ItemPickupGuiLines.Clear();

                                    // Add the crafted item to the inventory


                                    if (currentRecipe.Item1 == "sheet")
                                    {
                                        allUsedMaterials.Clear();
                                        allUsedMaterials.Add(GameWorld.Glass);
                                    }


                                    Object o = new Object("", currentRecipe.Item1, allUsedMaterials, MostRecentPartyTurnArchitect);
                                    o.Name = GameWorld.GenerateUniqueName("1S" + r.Next(2, 5) + "sw", o);
                                    MostRecentPartyTurnArchitect.Inventory.Add(o);

                                    int CraftRank = ((int)Math.Round((double)((MostRecentPartyTurnArchitect.Level + MostRecentPartyTurnArchitect.Creativity) / 2)));

                                    o.Rarity = World.GenerateItemRarity(CraftRank);
                                    o.ApplyImbuements(0);

                                    MakeObservation(MostRecentPartyTurnArchitect.Name + " has created " + o.Name + " with a quality rank of " + CraftRank + ".", Color.Coral, new EntityList<Entity>() { MostRecentPartyTurnArchitect, o });
                                    GameState = "otherturn";

                                    if (o.Imbuements.Count() > 0 || o.IsWeapon || o.Name != null)
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

                                        if (o.Imbuements.Count() == 0)
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

                                    MostRecentPartyTurnArchitect.CooldownCycles += 600;
                                }
                                else
                                {
                                    Exposition.Add(new TextStorage("Not enough materials to craft.", Color.Red, new EntityList<Entity>(){}));
                                    CraftingPhase = "selectrecipe";
                                }
                            }
                        }
                    }

                }
            }


            previousState = Keyboard.GetState();

            previousMouseState = currentMouseState;
            currentMouseState = Mouse.GetState();

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
                if (Keyboard.GetState().IsKeyDown(Keys.R))
                {
                    _spriteBatch.End();
                    _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, scaleMatrix);

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

                            // Directly use the ControllingRealm of the region
                            var controllingRealm = GameWorld.WorldMap[index].Realm;
                            if (controllingRealm != null && GameWorld.WorldMap[index].Biome != "void")
                            {
                                // Check if the current tile is the center of the controlling realm
                                if (GameWorld.WorldMap[index].X == controllingRealm.X && GameWorld.WorldMap[index].Z == controllingRealm.Z)
                                {
                                    // Draw the center of the realm as DesertT with the realm's color
                                    _spriteBatch.Draw(DesertT, tileRect, ColorConverter[controllingRealm.Color]);
                                }
                                else
                                {
                                    // Draw the outline of the tile with the color of the controlling realm
                                    _spriteBatch.Draw(OutlineT, tileRect, ColorConverter[controllingRealm.Color]);
                                }
                            }
                        }
                    }

                    _spriteBatch.End();
                    _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, scaleMatrix);
                    return; // Skip the rest of the function if Keys.R is held down
                }



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

                        // Skip unexplored tiles only if in travelmenu or ascendant mode
                        if ((GameState == "travelmenu" || GameState == "ascendant") && !GameWorld.WorldMap[index].Explored)
                        {
                            continue; // Skip drawing unexplored tiles in travel and ascendant modes
                        }

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

                        if (GameState == "ascendant" && GameWorld.GamePlayerAssociation != null)
                        {
                            foreach (var party in GameWorld.GamePlayerAssociation.Parties)
                            {
                                if (party.Leader == null && party.Architects.Count > 0)
                                {
                                    party.Leader = party.Architects[0];
                                }

                                if (party.Leader != null && party.Leader.Location != null)
                                {
                                    if (party.Leader.Location.Region.X == x && party.Leader.Location.Region.Z == z)
                                    {
                                        // Flash tick drawing logic will be moved to the end
                                    }
                                }
                            }
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

                        // Skip unexplored tiles only if in travelmenu or ascendant mode
                        if ((GameState == "travelmenu" || GameState == "ascendant") && !GameWorld.WorldMap[index].Explored)
                        {
                            continue; // Skip drawing unexplored tiles in travel and ascendant modes
                        }

                        // Draw Ownership
                        if (GameWorld.WorldMap[index].Owner != null && GameState == "generatehistory")
                        {
                            _spriteBatch.Draw(OutlineT, tileRect, ColorConverter[GameWorld.WorldMap[index].Owner.Color]);
                        }

                        // Draw the port if it exists
                        if (!string.IsNullOrEmpty(GameWorld.WorldMap[index].PortName))
                        {
                            _spriteBatch.Draw(PortT, tileRect, Color.White);
                        }

                        // Draw locations based on exploration status
                        if (GameWorld.WorldMap[index].Location != null)
                        {
                            var location = GameWorld.WorldMap[index].Location;

                            // Draw the location normally
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



                            if(GameMode == "ascendant" && Game1.GameWorld.GamePlayerAssociation != null)
                            {
                                if(location.Government != null && ((location.Government is Group g && Game1.GameWorld.GamePlayerAssociation.Parties.Contains(g)) || (location.Government is Architect a && Game1.GameWorld.GamePlayerAssociation.Parties.Any(p => p.Architects.Contains(a)))))
                                {
                                    _spriteBatch.Draw(OutlineT, tileRect, new Color(125, 0, 255));
                                }
                                else if(Game1.GameWorld.GamePlayerAssociation.Enemies.Contains(location) || Game1.GameWorld.GamePlayerAssociation.Enemies.Contains(location.Government))
                                {
                                    _spriteBatch.Draw(OutlineT, tileRect, Color.Red);
                                }
                                else
                                {
                                    _spriteBatch.Draw(OutlineT, tileRect, Color.Yellow);
                                }
                            }



                            // Draw the party-highlighted green cursor if applicable
                            if (GameWorld.GamePlayerAssociation != null && GameWorld.GamePlayerAssociation.ActiveParty != null && GameWorld.GamePlayerAssociation.ActiveParty.CurrentlyMarkedRegions.Contains(GameWorld.WorldMap[index]) && FlashTick < 50)
                            {
                                _spriteBatch.Draw(CursorT, tileRect, Color.LimeGreen);
                            }
                        }
                    }
                }

                // Draw trade lines in generatehistory mode
                if (GameState == "generatehistory")
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
                        for (int i = 0; i < routeCenters.Count(); i++)
                        {
                            Vector2 start = routeCenters[i];
                            Vector2 end = routeCenters[(i + 1) % routeCenters.Count()]; // Wrap around for the last point

                            DrawLine(_spriteBatch, start, end, Color.SaddleBrown);
                        }
                    }
                }

                // Highlight regions with interactable events within radius
                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {
                        int radius = 0; // Example: radius of 2 tiles. Adjust this value as needed.

                        bool IsWithinRadius(int centerX, int centerZ, int checkX, int checkZ, int radius)
                        {
                            int x = checkX - (checkZ - (checkZ & 1)) / 2;
                            int z = checkZ;
                            int y = -x - z;

                            int centerX_cube = centerX - (centerZ - (centerZ & 1)) / 2;
                            int centerZ_cube = centerZ;
                            int centerY_cube = -centerX_cube - centerZ_cube;

                            int distance = (Math.Abs(x - centerX_cube) + Math.Abs(y - centerY_cube) + Math.Abs(z - centerZ_cube)) / 2;
                            return distance <= radius;
                        }

                        if (GameWorld != null && GameWorld.GamePlayerAssociation != null && GameWorld.GamePlayerAssociation.ActiveParty != null)
                        {
                            bool isWithinRadius = IsWithinRadius(GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX, GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ, x, z, radius);

                            if (isWithinRadius)
                            {
                                bool hasColossal = false;
                                bool hasAnyEvent = false;

                                foreach (var interactableEvent in GameWorld.WorldMap[x + z * GameWorld.Width].Events)
                                {
                                    if (interactableEvent != null)
                                    {
                                        hasAnyEvent = true;
                                        if (interactableEvent.Type == "colossal")
                                        {
                                            hasColossal = true;
                                            break;
                                        }
                                    }
                                }

                                Color drawColor = hasColossal ? Color.Red : hasAnyEvent ? Color.White : Color.Gray;

                                if (drawColor != Color.Gray)
                                {
                                    _spriteBatch.Draw(ArchitectHere, GameWorld.WorldMap[x + z * GameWorld.Width].BoundingBox(), drawColor);
                                }
                            }
                        }
                    }
                }

                // End the second phase of drawing
                _spriteBatch.End();
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, scaleMatrix);

                // Start of Flash Tick Drawing logic

                
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

                        // Draw cursor if it matches the current tile, with Cyan color
                        bool isCursorOnTile = (GameState == "travelmenu" && GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX == x && GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ == z && GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX != 0 && GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ != 0) ||
                                                (GameState == "ascendant" && GameWorld.GamePlayerAssociation != null && GameWorld.GamePlayerAssociation.AscendantX == x && GameWorld.GamePlayerAssociation.AscendantZ == z);

                        if (isCursorOnTile)
                        {
                            _spriteBatch.Draw(EmptyTileT, tileRect, Color.Black);
                            _spriteBatch.Draw(CursorT, tileRect, Color.Cyan);
                        }

                        if (GameState == "ascendant" && GameWorld.GamePlayerAssociation != null && FlashTick < 50)
                        {
                            foreach (var party in GameWorld.GamePlayerAssociation.Parties)
                            {
                                if (party.Leader == null && party.Architects.Count > 0)
                                {
                                    party.Leader = party.Architects[0];
                                }

                                if (party.Leader != null && party.Leader.Location != null)
                                {
                                    if (party.Leader.Location.Region.X == x && party.Leader.Location.Region.Z == z)
                                    {
                                        // Selected parties are Magenta, Unselected parties are White
                                        Color partyColor = party == CurrentlySelectedAscendantParty ? Color.Magenta : Color.White;
                                        _spriteBatch.Draw(EmptyTileT, tileRect, Color.Black);
                                        _spriteBatch.Draw(CursorT, tileRect, partyColor);
                                    }
                                }
                            }
                        }
                    }
                }

                // End the Flash Tick Drawing logic
                _spriteBatch.End();
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, scaleMatrix);
            }


            void DrawCenteredText(SpriteBatch spriteBatch, string text, int yPosition, SpriteFont Font, Color color)
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

                        // Define constants for time-of-day brightness adjustment
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

                        // Draw cursor or tile
                        if (GameWorld.GamePlayerAssociation.ActiveParty.CurrentlyMarkedRegions.Contains(GameWorld.WorldMap[index]) && FlashTick < 50)
                        {
                            _spriteBatch.Draw(CursorT, tileRect, Color.LimeGreen);
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

                            // Apply additional darkness filter based on the time of day only if necessary
                            finalColor = Color.Lerp(finalColor, new Color(0, 0, 0), darknessLevel);

                            // Draw the region tile with combined elevation, blight, and time-of-day adjusted color
                            _spriteBatch.Draw(TileAtlas[GameWorld.WorldMap[index].Biome], tileRect, finalColor);
                        }
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
                            if (GameWorld.WorldMap[index].Location != null &&
                                (GameState != "travelmenu" || GameWorld.WorldMap[index].Location.Explored))
                            {
                                var location = GameWorld.WorldMap[index].Location;

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
                        bool isWithinRadius = IsWithinRadius(GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX, GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ, worldX, worldZ, radius);

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


            void DrawCharacter(Architect a, int x, int y, double Scale, bool Inverted = false)
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

                // Always draw the mirror texture without inversion
                _spriteBatch.Draw(MirrorT, newRect, Color.White);

                // Add hitbox for the character frame
                if (!IsShowingSkills && !IsShowingSpells && !IsShowingBodyParts)
                {
                    if (SplitMode && GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a))
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
                    return;
                }

                string baseTexture = a.Sex == "female" ? "FemaleT" : "MaleT";
                string race = a.Race.Name.ToLower();

                switch (race)
                {
                    case "luminarch":
                        if (Inverted)
                        {
                            _spriteBatch.Draw(baseTexture == "FemaleT" ? LuminarchFemaleT : LuminarchMaleT, ChosenRect, null, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);
                        }
                        else
                        {
                            _spriteBatch.Draw(baseTexture == "FemaleT" ? LuminarchFemaleT : LuminarchMaleT, ChosenRect, Color.White);
                        }
                        DrawHairTexture(a, "white", ChosenRect, Inverted);
                        break;
                    case "nightfell":
                        if (Inverted)
                        {
                            _spriteBatch.Draw(baseTexture == "FemaleT" ? NightfellFemaleT : NightfellMaleT, ChosenRect, null, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);
                        }
                        else
                        {
                            _spriteBatch.Draw(baseTexture == "FemaleT" ? NightfellFemaleT : NightfellMaleT, ChosenRect, Color.White);
                        }
                        DrawHairTexture(a, "black", ChosenRect, Inverted);
                        break;
                    default:
                        if (Inverted)
                        {
                            _spriteBatch.Draw(baseTexture == "FemaleT" ? ArchaixFemaleT : ArchaixMaleT, ChosenRect, null, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);
                        }
                        else
                        {
                            _spriteBatch.Draw(baseTexture == "FemaleT" ? ArchaixFemaleT : ArchaixMaleT, ChosenRect, Color.White);
                        }
                        DrawHairTexture(a, "gray", ChosenRect, Inverted);
                        break;
                }

                void DrawHairTexture(Architect a, string raceColor, Rectangle rect, bool inverted)
                {
                    Texture2D hairTexture = null;

                    switch (raceColor)
                    {
                        case "white":
                            switch (a.HairID)
                            {
                                case 0:
                                    hairTexture = VibrantWhiteT;
                                    break;
                                case 1:
                                    hairTexture = WindsweptWhiteT;
                                    break;
                                case 2:
                                    hairTexture = DiminishedWhiteT;
                                    break;
                                case 3:
                                    hairTexture = SwirlWhiteT;
                                    break;
                            }
                            break;
                        case "black":
                            switch (a.HairID)
                            {
                                case 0:
                                    hairTexture = VibrantBlackT;
                                    break;
                                case 1:
                                    hairTexture = WindsweptBlackT;
                                    break;
                                case 2:
                                    hairTexture = DiminishedBlackT;
                                    break;
                                case 3:
                                    hairTexture = SwirlBlackT;
                                    break;
                            }
                            break;
                        case "gray":
                            switch (a.HairID)
                            {
                                case 0:
                                    hairTexture = VibrantGrayT;
                                    break;
                                case 1:
                                    hairTexture = WindsweptGrayT;
                                    break;
                                case 2:
                                    hairTexture = DiminishedGrayT;
                                    break;
                                case 3:
                                    hairTexture = SwirlGrayT;
                                    break;
                            }
                            break;
                    }

                    if (hairTexture != null)
                    {
                        if (inverted)
                        {
                            _spriteBatch.Draw(hairTexture, rect, null, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);
                        }
                        else
                        {
                            _spriteBatch.Draw(hairTexture, rect, Color.White);
                        }
                    }
                }

                // Define the drawing order
                var drawOrder = new List<string[]>
    {
        new string[] { "body" },
        new string[] { "undergarment", "brassiere" },
        new string[] { "flair" },  // Draw flair before anything that ends with "shirt"
        new string[] { "leggings", "pants" },  // Always draw these first within this category
        new string[] { "otherClothing" },
        new string[] { "robe" },
        new string[] { "armor" },
        new string[] { "skirt", "kilt" },
        new string[] { "left boot", "right boot" } // Boots are drawn last to be on top
    };

                // Define armor and clothing categories
                var armorItems = new HashSet<string> { "chestplate", "helmet", "left boot", "right boot", "left gauntlet", "right gauntlet", "leggings" };
                var clothingItems = new HashSet<string> { "undergarment", "brassiere", "robe", "shirt", "pants", "leggings", "skirt", "kilt", "hat", "flair" };

                // Group items by their type
                var groupedItems = new Dictionary<string, EntityList<Object>>();
                foreach (var o in a.Clothing)
                {
                    string category = "otherClothing";
                    if (armorItems.Contains(o.Type)) category = "armor";
                    else if (clothingItems.Contains(o.Type)) category = o.Type;

                    if (!groupedItems.ContainsKey(category)) groupedItems[category] = new EntityList<Object>();
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

                                if (CharacterAtlas.ContainsKey(s))
                                {
                                    if (Inverted)
                                    {
                                        _spriteBatch.Draw(CharacterAtlas[s], ChosenRect, null, drawColor, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);
                                    }
                                    else
                                    {
                                        _spriteBatch.Draw(CharacterAtlas[s], ChosenRect, drawColor);
                                    }
                                }
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
                        ;

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
                            ;

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


                if(MostRecentPartyTurnArchitect.BlindCycles > 0)
                {
                    string ind = $"You are currently blind.";
                    Vector2 indpos = new Vector2(startCoords.X, startCoords.Y + line * 25);
                    spriteBatch.DrawString(font, ind, indpos, Color.Yellow);
                }
                else
                {
                    int startIndex = CurrentObjectPage * itemsPerPage;
                    int endIndex = Math.Min(startIndex + itemsPerPage, list.Count());

                    // Draw the page indicator
                    string pageIndicator = $"(page {CurrentObjectPage + 1} of {MaximumObjectPage + 1}, use CTRL + / -)";
                    Vector2 pageIndicatorPosition = new Vector2(startCoords.X, startCoords.Y + line * 25);
                    spriteBatch.DrawString(font, pageIndicator, pageIndicatorPosition, Color.White);
                    line++;

                    pageIndicator = $"Press F3 To Split Items.";
                    pageIndicatorPosition = new Vector2(startCoords.X, startCoords.Y + line * 25);
                    spriteBatch.DrawString(font, pageIndicator, pageIndicatorPosition, Color.White);
                    line++;

                    if(MostRecentPartyTurnArchitect.Structure != null)
                    {
                        pageIndicator = $"[<] denotes a nearby exit.";
                        pageIndicatorPosition = new Vector2(startCoords.X, startCoords.Y + line * 25);
                        spriteBatch.DrawString(font, pageIndicator, pageIndicatorPosition, Color.White);
                        line++;
                    }

                    // Adjust start coordinates for drawing the actual list
                    startCoords = new Vector2(startCoords.X, startCoords.Y + 20); // Move startCoords down by 40 (two lines)

                    for (int i = startIndex; i < endIndex; i++)
                    {
                        var (Description, Count, IndentationLevel, TextColor, Obj) = list[i];

                        string MarketVal = (MostRecentPartyTurnArchitect.Structure != null && MostRecentPartyTurnArchitect.Structure.Type == "market") ? ", (" +Obj.Value() + "* ea.)" : "";

                        string textToDraw = SplitMode ?
                            $"{new string(' ', 4 * IndentationLevel)}{Description}" :
                            $"{new string(' ', 4 * IndentationLevel)}{Description} x{Count}" + MarketVal;

                        Vector2 textPosition = new Vector2(startCoords.X, startCoords.Y + line * 25);
                        spriteBatch.DrawString(font, textToDraw, textPosition, TextColor);

                        // Add hitbox for the object
                        EntityHitboxes.Add((new Rectangle(textPosition.ToPoint(), font.MeasureString(textToDraw).ToPoint()), Obj));

                        line++;
                    }

                    MaximumObjectPage = (int)Math.Round((decimal)(list.Count() / itemsPerPage), 0, MidpointRounding.ToPositiveInfinity);
                }
            }

            // Assuming you have a method to handle user input to navigate pages, you would update CurrentObjectPage accordingly
            // And call PaginateAndDrawList again to redraw the list for the new page


            
            void DrawCenteredTextAtPosition(SpriteBatch spriteBatch, string text, float centerX, float centerY, SpriteFont font, Color c)
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
                spriteBatch.DrawString(font, text, position, c);
            }

            int DrawX = 1400;
            int DrawY = 1100;


            void WorldMouseLogic()
            {
                // Adjust mouse coordinates based on the scale matrix
                Point adjustedMousePosition = new Point(
                    (int)(Mouse.GetState().X / scaleX),
                    (int)(Mouse.GetState().Y / scaleY)
                );

                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {
                        // Calculate the original bounding box
                        Rectangle originalBoundingBox = new Rectangle(
                            (Game1.RegionXMod + x * Game1.TileXDistance) + ((z % 2 == 1) ? Game1.TileXDistance / 2 : 0),
                            Game1.RegionYMod + z * Game1.TileZDistance,
                            Game1.TileSize,
                            Game1.TileSize
                        );

                        // Adjust bounding box based on the scale matrix
                        Rectangle scaledBoundingBox = new Rectangle(
                            (int)(originalBoundingBox.X * scaleX),
                            (int)(originalBoundingBox.Y * scaleY),
                            (int)(originalBoundingBox.Width * scaleX),
                            (int)(originalBoundingBox.Height * scaleY)
                        );

                        if (scaledBoundingBox.Contains(Mouse.GetState().Position))
                        {
                            if (GameWorld.WorldMap[x + z * GameWorld.Width].Location == null)
                            {
                                _spriteBatch.DrawString(BabyShibafont, "No Notable Location, " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome, new Vector2(DrawX, DrawY), Color.White);
                            }
                            else
                            {
                                _spriteBatch.DrawString(BabyShibafont, GameWorld.WorldMap[x + z * GameWorld.Width].Location.Name + ", " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome + " " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.Type + ".", new Vector2(DrawX, DrawY), Color.White);
                                _spriteBatch.DrawString(BabyShibafont,
                                                "Population: " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.TruePopulation() +
                                                (GameWorld.WorldMap[x + z * GameWorld.Width].Location.PrimaryRace != null && GameWorld.WorldMap[x + z * GameWorld.Width].Location.PrimaryRace.Name != "" ?
                                                " (Primarily " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.PrimaryRace.Name + ")" : ""),
                                                new Vector2(DrawX, DrawY + 30),
                                                Color.White);


                                int ArchitectPopulation = 0;
                                int structures = 0;
                                int groups = 0;

                                foreach (Group g in Game1.GameWorld.Groups)
                                {
                                    if (g.Leader.Location == GameWorld.WorldMap[x + z * GameWorld.Width].Location)
                                        groups++;
                                }

                                foreach (District d in GameWorld.WorldMap[x + z * GameWorld.Width].Location.Districts)
                                {
                                    ArchitectPopulation += d.Architects.Count();

                                    for (int DistrictX = 0; DistrictX < 7; DistrictX++)
                                    {
                                        for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                                        {
                                            foreach (Structure s in d.DistrictMap[DistrictX + DistrictZ * 7].Structures)
                                            {
                                                structures++;
                                            }
                                        }
                                    }
                                }

                                _spriteBatch.DrawString(BabyShibafont, "Total Architects: " + ArchitectPopulation, new Vector2(DrawX, DrawY + 60), Color.White);

                                if (GameWorld.WorldMap[x + z * GameWorld.Width].Location.Government == null)
                                {
                                    _spriteBatch.DrawString(BabyShibafont, "No Notable Government", new Vector2(DrawX, DrawY + 90), Color.White);
                                }
                                else
                                {
                                    _spriteBatch.DrawString(BabyShibafont, "Government: " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.Government.Name, new Vector2(DrawX, DrawY + 90), Color.White);
                                }

                                _spriteBatch.DrawString(BabyShibafont, "Colonization Desire: " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.ColonizationDesire, new Vector2(DrawX, DrawY + 120), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Wealth: " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.Wealth, new Vector2(DrawX, DrawY + 150), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Is Saving Up To Settle: " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.IsSavingUpToSettle.ToString(), new Vector2(DrawX, DrawY + 180), Color.White);


                                _spriteBatch.DrawString(BabyShibafont, "Structures: " + structures, new Vector2(DrawX, DrawY + 210), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Distinct Groups: " + groups, new Vector2(DrawX, DrawY + 240), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Districts: " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.Districts.Count(), new Vector2(DrawX, DrawY + 270), Color.White);
                            }
                        }
                    }
                }
            }

            void DrawAnnouncements()
            {
                // Helper function to scale rectangles
                Rectangle ScaleRectangle(Rectangle rect, float scaleX, float scaleY)
                {
                    return new Rectangle(
                        (int)(rect.X * scaleX),
                        (int)(rect.Y * scaleY),
                        (int)(rect.Width * scaleX),
                        (int)(rect.Height * scaleY)
                    );
                }

                int MaxLines;
                int yPos;
                int screenHeight = 1440;

                if (GameState == "dead")
                {
                    MaxLines = 70; // Set the maximum number of lines you want to display
                    yPos = screenHeight - 50; // Initial Y position at the bottom
                }
                else if (GameState == "ascendant")
                {
                    MaxLines = 36; // Set the maximum number of lines you want to display
                    yPos = screenHeight - 100; // Initial Y position at the bottom
                }
                else
                {
                    MaxLines = 26; // Set the maximum number of lines you want to display
                    yPos = screenHeight - 380; // Initial Y position at the bottom
                }

                int MaxLength = 800; // Adjusted MaxLength for smaller font size
                int lineHeight = 20; // Adjusted line height for smaller font size
                int totalLinesDisplayed = 0; // Track the total number of lines displayed

                // Convert the announcements to a list
                EntityList<TextStorage> announcementsList = new EntityList<TextStorage>(Announcements);
                for (int i = announcementsList.Count - 1; i >= 0; i--)
                {
                    TextStorage announcement = announcementsList[i];
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

                    // Draw lines in normal order but start from the bottom
                    for (int j = lines.Count - 1; j >= 0; j--)
                    {
                        if (totalLinesDisplayed < MaxLines)
                        {
                            yPos -= lineHeight;
                            DrawAnnouncementLine(lines[j], yPos, announcement.Color, announcement.Entities);
                            totalLinesDisplayed++;
                        }
                        else
                        {
                            break; // Exit the loop if the maximum number of lines is reached
                        }
                    }

                    if (totalLinesDisplayed >= MaxLines)
                    {
                        break; // Exit the outer loop if the maximum number of lines is reached
                    }
                }

                // Helper function to draw each announcement line and create hitboxes for entity names
                void DrawAnnouncementLine(string text, int yPosition, Color color, EntityList<Entity> entities)
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
            }





            if (GameState == "generatehistory" || GameState == "choosepreferences")
            {
                // This would ideally be within your update or draw loop
                if (currentObject == "")
                {
                    currentObject = Tips[r.Next(Tips.Count())]; // Assuming 'Tips' is a List<string> and 'r' is a Random instance
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
                    currentObject = Tips[r.Next(Tips.Count())];
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
                    string firstName = Game1.FirstNames[Game1.r.Next(Game1.FirstNames.Count())] + Game1.NameSuffixes[Game1.r.Next(Game1.NameSuffixes.Count())];
                    string lastName = ((Game1.LastNames[Game1.r.Next(Game1.LastNames.Count())]).Substring(0, 1)).ToUpper() + (Game1.LastNames[Game1.r.Next(Game1.LastNames.Count())]).Substring(1).ToLower();

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
                DrawCenteredText(_spriteBatch, "Press C to start a new game.", 500, Shibafont, Color.White);
                DrawCenteredText(_spriteBatch, "Press L to load an existing save.", 550, Shibafont, Color.White);


                DrawCenteredText(_spriteBatch, "Press F to toggle the font.", 750, Shibafont, Color.White);

                _spriteBatch.Draw(Astrionalis, new Rectangle(100, 200, 640, 1280), Color.White);
                _spriteBatch.Draw(Celestrioris, new Rectangle(1800, 200, 720, 1270), Color.White);
            }
            else if (GameState == "worldgenscreen")
            {
                _spriteBatch.DrawString(Shibafont, "Press ENTER to start playing with optimal settings.", new Vector2(200, 200), Color.White);
                _spriteBatch.DrawString(Shibafont, "If you wish, use denoted keys to change settings.", new Vector2(200, 250), Color.White);

                if (ViewMessageForCustom)
                {
                    _spriteBatch.DrawString(Shibafont, "Some custom settings are not guaranteed to produce a playable world.", new Vector2(200, 300), Color.OrangeRed);
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

                _spriteBatch.DrawString(Shibafont, "(W/S) Choose Calamity: " + Capitalize(ThreatTypes[CurrentlySelectedGrievanceType]), new Vector2(200, 550), Color.Magenta);

                Dictionary<string, string> threatDescriptions = new Dictionary<string, string>
                {
                    { "non-cataclysmic", "Choose a random, morally-questionable threat, but with a goal that is less apocalyptic. (recommended)" },
                    { "random", "Choose a completely random threat from all options. May destroy/desolate your entire world." },
                    { "dominator", "An organization bent on unjustly taking over the world will inhabit your continent." },
                    { "purifier", "A force of purity will try to erase the entire world into indistinguishable matter." },
                    { "disease", "A dark plague will come upon your land, and be manipulated by a specialist for the destruction of life." },
                    { "killer", "A gang of criminal assassins will try to exterminate all life they can find." },
                    { "kidnapper", "For one reason or another, countless people will be taken from their homes." },
                    { "corruptor", "A depressed individual will ravage the morality and stability of thousands of minds." },
                    { "diplomancer", "A manipulative individual will try to twist the minds of many." },
                    { "inciter", "A powerful persuader will attempt to drive the world into an eternal conflict." },
                    { "power", "An ancient harvester will attempt to concentrate the energy of countless slain individuals." }
                };

                if (threatDescriptions.ContainsKey(ThreatTypes[CurrentlySelectedGrievanceType]))
                {
                    _spriteBatch.DrawString(Shibafont, threatDescriptions[ThreatTypes[CurrentlySelectedGrievanceType]], new Vector2(200, 600), Color.Magenta);
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

                _spriteBatch.DrawString(Shibafont, numberOfCivilizationsText, new Vector2(200, 650), Color.Red);

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

                _spriteBatch.DrawString(Shibafont, "(R/F) Prosperity Multiplier (affects civ growth rate): " + prosperityMultiplierText, new Vector2(200, 700), Color.Cyan);

                /*
                _spriteBatch.DrawString(Shibafont, "(T/G) [BROKEN] World Width (in region tiles, east/west, max 128): " + CurrentlySelectedWorldWidth, new Vector2(200, 750), Color.LimeGreen);
                _spriteBatch.DrawString(Shibafont, "(Y/H) [BROKEN] World Length (in region tiles, north/south, max 128): " + CurrentlySelectedWorldLength, new Vector2(200, 800), Color.Cyan);
                */

                _spriteBatch.DrawString(Shibafont, "Press ENTER to begin world generation.", new Vector2(200, 950), Color.White);
            }


            else if (GameState == "savinggame")
            {
                _spriteBatch.DrawString(Shibafont, "Saving " + GameWorld.Name + " data. This may take half a minute...", new Vector2(200, 200), Color.White);
            }
            else if (GameState == "loadinggamemenu")
            {
                var saveDirectories = Directory.GetDirectories(DocumentsFolderPath + "/LightrealmSaves");
                int SavesCount = saveDirectories.Count();

                if (SavesCount > 0)
                {
                    // Starting directions split into three lines
                    _spriteBatch.DrawString(Shibafont, "Use arrow keys to navigate.", new Vector2(200, 200), Color.White);
                    _spriteBatch.DrawString(Shibafont, "Press Enter to load a savegame.", new Vector2(200, 230), Color.White);
                    _spriteBatch.DrawString(Shibafont, "Press H to open history.", new Vector2(200, 260), Color.White);

                    // Adding three lines of space
                    int startingY = 350;

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
                            _spriteBatch.DrawString(Shibafont, "(>) " + directoryDisplayName, new Vector2(200, startingY + Number * 30), textColor);
                        }
                        else
                        {
                            _spriteBatch.DrawString(Shibafont, "( ) " + directoryDisplayName, new Vector2(200, startingY + Number * 30), textColor);
                        }

                        Number++;
                    }

                    _spriteBatch.DrawString(Shibafont, "Press DELETE to remove a savegame.", new Vector2(200, startingY + Number * 30 + 50), Color.White);
                }
                else
                {
                    _spriteBatch.DrawString(Shibafont, "You have no savegames. You should probably go make one now.", new Vector2(200, 200), Color.White);
                    _spriteBatch.DrawString(Shibafont, "Press ESC to return to title.", new Vector2(200, 230), Color.White);
                }
            }

            else if (GameState == "viewhistory")
            {
                int line = 0;
                bool foundMatchingHistory = false;
                bool foundVisibleMatchingHistory = false;

                // Draw the first 3 lines from the entire history
                for (int i = 0; i < 3; i++)
                {
                    if (CurrentlyViewingHistory.Count() > i)
                    {
                        _spriteBatch.DrawString(BabyShibafont, CurrentlyViewingHistory[i], new Vector2(50, 50 + (line * 30)), Color.White);
                        line++;
                    }
                }

                // Create a new list of filtered historical events based on region, significance, and search prompt
                List<string> filteredHistory = new List<string>();
                for (int i = 3; i < CurrentlyViewingHistory.Count(); i++)
                {
                    string[] parts = CurrentlyViewingHistory[i].Trim('[', ']').Split(']');
                    if (parts.Length == 2)
                    {
                        string[] numbers = parts[0].Split('/');
                        if (numbers.Length == 2 && int.TryParse(numbers[0], out int regionIndex) && int.TryParse(numbers[1], out int significance))
                        {
                            if ((CurrentlySelectingRegionIndexOr100 == 100 || regionIndex == CurrentlySelectingRegionIndexOr100) &&
                                (!ShowSignificant || (ShowSignificant && significance == 1)) &&
                                parts[1].Trim().Contains(HistoryPrompt, StringComparison.OrdinalIgnoreCase))
                            {
                                filteredHistory.Add(parts[1].Trim());
                                foundMatchingHistory = true; // Track if any matching history is found
                            }
                        }
                    }
                }

                // Ensure HistoricalScrollValue is within bounds of the filtered history list
                if (HistoricalScrollValue > filteredHistory.Count - 1)
                {
                    HistoricalScrollValue = Math.Max(0, filteredHistory.Count - 1);
                }

                // Draw up to 50 lines from the filtered history, starting from HistoricalScrollValue
                for (int i = 0; i < 50; i++)
                {
                    int index = i + HistoricalScrollValue;
                    if (index < filteredHistory.Count)
                    {
                        _spriteBatch.DrawString(BabyShibafont, filteredHistory[index], new Vector2(50, 50 + (line * 30)), Color.White);
                        line++;
                        foundVisibleMatchingHistory = true; // Track if any visible matching history is found
                    }
                }

                // Display messages based on the presence of matching history
                if (!foundMatchingHistory)
                {
                    _spriteBatch.DrawString(BabyShibafont, "Nothing related to \"" + HistoryPrompt + "\" happened here.", new Vector2(50, 50 + (line * 30)), Color.White);
                    line++;
                }
                else if (!foundVisibleMatchingHistory)
                {
                    _spriteBatch.DrawString(BabyShibafont, "Looks like you scrolled too far. This empty space is still for you to write...", new Vector2(50, 50 + (line * 30)), Color.White);
                    line++;
                }

                // Draw the search prompt
                _spriteBatch.DrawString(BabyShibafont, "Use scroll to go through events. CTRL + Scroll to scroll faster.", new Vector2(1600, 50), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Start typing to Search: " + HistoryPrompt + "_", new Vector2(1600, 80), Color.White);

                // Draw the instructions at the bottom right
                string regionInstruction = $"LCTRL + 1-{MaxRegionIndex}: Focus Region";
                string cancelInstruction = "LCTRL + X: Unfocus";
                string toggleSignificantInstruction = ShowSignificant ? "LCTRL + S: Show All Events" : "LCTRL + S: Only Show Significant Ones";

                Vector2 regionInstructionSize = BabyShibafont.MeasureString(regionInstruction);
                Vector2 cancelInstructionSize = BabyShibafont.MeasureString(cancelInstruction);
                Vector2 toggleSignificantInstructionSize = BabyShibafont.MeasureString(toggleSignificantInstruction);

                float padding = 10f;
                float bottomRightX = GraphicsDevice.Viewport.Width - padding;
                float bottomRightY = GraphicsDevice.Viewport.Height - padding;

                _spriteBatch.DrawString(BabyShibafont, regionInstruction, new Vector2(bottomRightX - regionInstructionSize.X, bottomRightY - toggleSignificantInstructionSize.Y - cancelInstructionSize.Y - regionInstructionSize.Y), Color.White);
                _spriteBatch.DrawString(BabyShibafont, cancelInstruction, new Vector2(bottomRightX - cancelInstructionSize.X, bottomRightY - toggleSignificantInstructionSize.Y - cancelInstructionSize.Y), Color.White);
                _spriteBatch.DrawString(BabyShibafont, toggleSignificantInstruction, new Vector2(bottomRightX - toggleSignificantInstructionSize.X, bottomRightY - toggleSignificantInstructionSize.Y), Color.White);
            }



            else if (GameState == "deletinggame")
            {
                _spriteBatch.DrawString(Shibafont, "This action cannot be undone. Are you sure?", new Vector2(30, 30), Color.White);
                _spriteBatch.DrawString(Shibafont, "Confirm (CTRL Y)", new Vector2(30, 60), Color.White);
                _spriteBatch.DrawString(Shibafont, "Cancel (CTRL N)", new Vector2(30, 90), Color.White);
            }
            if (GameState == "loadinglibraries")
            {
                int screenWidth = 2560;
                int imageWidth = 744;

                Rectangle destinationRectangle = new Rectangle((screenWidth - imageWidth) / 2, 100, imageWidth, 216);

                _spriteBatch.Draw(TitleScreen, destinationRectangle, Color.White);

                DrawCenteredText(_spriteBatch, "Loading Speech To Text Libraries...", 500, Shibafont, Color.White);

                _spriteBatch.Draw(Astrionalis, new Rectangle(100, 200, 640, 1280), Color.White);
                _spriteBatch.Draw(Celestrioris, new Rectangle(1800, 200, 720, 1270), Color.White);
            }
            else if (GameState == "choosingaudio")
            {
                _spriteBatch.DrawString(Shibafont, "Choose A Microphone:", new Vector2(30, 40), Color.White);

                if (PortAudio.DeviceCount > 0)
                {
                    for (int i = 0; i < Math.Min(10, PortAudio.DeviceCount); i++)
                    {
                        var deviceInfo = PortAudio.GetDeviceInfo(i);
                        _spriteBatch.DrawString(Shibafont, $"[{i + 1}] {deviceInfo.name}", new Vector2(30, 120 + i * 40), Color.White);
                    }
                    _spriteBatch.DrawString(Shibafont, "Press the corresponding number key to select an audio device.", new Vector2(30, 800), Color.White);
                }
                else
                {
                    _spriteBatch.DrawString(Shibafont, "No audio devices detected, try installing or reinstalling a device and restart the game.", new Vector2(30, 120), Color.White);
                }
            }
            else if (GameState == "loadinggame")
            {
                _spriteBatch.DrawString(Shibafont, "Loading save data, this may take half a minute...", new Vector2(200, 200), Color.White);
            }
            else if (GameState == "pickstatpreferences")
            {
                int Line = 1;

                if (StatOptions.Count() == 7)
                {
                    _spriteBatch.DrawString(Shibafont, "Select your character's highest stat ([] denotes a keybind):", new Vector2(100, 100), Color.White);
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
                    int CurrentItemListing = (GameWorld.HistoricalEvents.Count() - i);
                    if (CurrentItemListing >= 0)
                    {
                        _spriteBatch.DrawString(BabyShibafont, GameWorld.HistoricalEvents[CurrentItemListing].EventData, new Vector2(1750, 400 + ((-1) * (20 * i))), Color.White);
                    }
                }

                for (int i = 20; i != 0; i--)
                {
                    int CurrentItemListing = (GameWorld.AbridgedHistoricalEvents.Count() - i);
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
                _spriteBatch.DrawString(BabyShibafont, "Architects, Total: " + GameWorld.AllArchitects.Count() + ", Living: " + GameWorld.LivingArchitects + ", Dead: " + GameWorld.DeadArchitects, new Vector2(1750, 1320), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Distinct Groups: " + GameWorld.Groups.Count(), new Vector2(1750, 1360), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Cultural Works: " + GameWorld.WorksOfCulture, new Vector2(1750, 1400), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Crafts Practiced: " + GameWorld.TotalCrafts, new Vector2(2000, 1400), Color.White);

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
                    _spriteBatch.DrawString(BabyShibafont, "Change preferences or press C to generate more people.", new Vector2(DrawX + 500, 100), Color.White);
                }
                else
                {
                    _spriteBatch.DrawString(BabyShibafont, "Choose your character preferences ([] denotes a keybind):", new Vector2(DrawX + 500, 50), Color.White);
                }

                Dictionary<string, string> BonusDictionary = new Dictionary<string, string>() { { "luminarch", "(+10% Max Energy, -10% Attack Power)" }, { "nightfell", "(-10% Max Energy, +10% Attack Power)" }, { "archaix", "(Average Energy, Average Attack Power)" } };

                _spriteBatch.DrawString(BabyShibafont, "[1] Race: " + Capitalize(Game1.GameWorld.HumanoidRaces[CurrentlySelectingRace - 1].Name) + " " + BonusDictionary[Game1.GameWorld.HumanoidRaces[CurrentlySelectingRace - 1].Name], new Vector2(DrawX + 500, 150), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "[2] Sex: " + Capitalize(Sexes[CurrentlySelectingSex]), new Vector2(DrawX + 500, 200), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "[3] Dominant Hand: " + Capitalize((CurrentlySelectingHandedness ? "Right" : "Left")), new Vector2(DrawX + 500, 250), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Press ENTER to continue...", new Vector2(DrawX + 500, 350), Color.White);

                _spriteBatch.DrawString(BabyShibafont, GameWorld.Name + " (hover over map for more info)", new Vector2(DrawX + 500, DrawY), Color.White);

                WorldMouseLogic();

            }
            else if (GameState == "choosegamemode")
            {
                DrawCenteredText(_spriteBatch, "Choose a Gamemode", 200, Shibafont, Color.White);

                DrawCenteredTextAtPosition(_spriteBatch, "Chronicle", 500, 500, Shibafont, Color.White);
                DrawCenteredTextAtPosition(_spriteBatch, "(Press [C])", 500, 520, BabyShibafont, Color.White);
                DrawCenteredTextAtPosition(_spriteBatch, "Take on the role of an average citizen with a tragic (or petty) backstory", 500, 560, BabyShibafont, Color.White);
                DrawCenteredTextAtPosition(_spriteBatch, "Grow your power through experience, knowledge, skills, and equipment", 500, 580, BabyShibafont, Color.White);
                DrawCenteredTextAtPosition(_spriteBatch, "Take on the threats of the world individually or recruit friends to help you", 500, 600, BabyShibafont, Color.White);
                DrawCenteredTextAtPosition(_spriteBatch, "With enough power, ascend to a throne and switch to Ascendant mode at any time", 500, 620, BabyShibafont, Color.White);

                DrawCenteredTextAtPosition(_spriteBatch, "Ascendant", 2060, 500, Shibafont, Color.White);
                DrawCenteredTextAtPosition(_spriteBatch, "(Press [A])", 2060, 520, BabyShibafont, Color.White);
                DrawCenteredTextAtPosition(_spriteBatch, "Inherit a network of people, places, and resources", 2060, 560, BabyShibafont, Color.White);
                DrawCenteredTextAtPosition(_spriteBatch, "Manage resources, build outposts, grow your association, bring peace or sow evil", 2060, 580, BabyShibafont, Color.White);
                DrawCenteredTextAtPosition(_spriteBatch, "Deploy parties to locations and dictate their actions, down to the finest detail", 2060, 600, BabyShibafont, Color.White);
                DrawCenteredTextAtPosition(_spriteBatch, "Switch to Chronicle mode at any time", 2060, 620, BabyShibafont, Color.White);
            }
            else if (GameState == "choosefounderoptions")
            {
                _spriteBatch.DrawString(BabyShibafont, "Choose your base-of-operations preference ([] denotes a keybind!):", new Vector2(DrawX + 500, 100), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "[1] Civilization Type: " + Game1.GameWorld.Races[CurrentlySelectingRace].Name, new Vector2(DrawX + 500, 150), Color.White);

                DrawWorld();

                _spriteBatch.DrawString(BabyShibafont, "Press ENTER to continue...", new Vector2(DrawX + 500, 300), Color.White);

                _spriteBatch.DrawString(BabyShibafont, GameWorld.Name + " (hover over map for more info)", new Vector2(DrawX + 500, DrawY), Color.White);

                WorldMouseLogic();

            }
            else if (GameState == "ascendant")
            {
                _spriteBatch.Draw(GuideT, new Rectangle(0, 0, 192, 192), Color.White);

                // Draw the world using the new function
                DrawWorld();

                _spriteBatch.DrawString(BabyShibafont, GameWorld.Name + " (hover over map for more info)", new Vector2(DrawX + 500, DrawY), Color.White);

                _spriteBatch.DrawString(BabyShibafont, "Week " + Math.Round((decimal)(GameWorld.Cycle / 6048000)), new Vector2(1900, 1280), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Month " + Math.Round((decimal)(GameWorld.Cycle / 24192000), 0, MidpointRounding.ToNegativeInfinity).ToString(), new Vector2(1900, 1240), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Year " + Math.Round((decimal)(Math.Round((decimal)(GameWorld.Cycle / 290304000), 0, MidpointRounding.ToNegativeInfinity)), 0, MidpointRounding.ToNegativeInfinity), new Vector2(1900, 1200), Color.White);

                _spriteBatch.DrawString(BabyShibafont, GameWorld.GamePlayerAssociation.Name, new Vector2(1900, 200), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "[RTDGCV] Cursor", new Vector2(1900, 240), Color.White);

                DrawAnnouncements();


                // Draw the icon and text for cursor and parties
                int startX = 192; // Start drawing to the right of the GuideT rectangle
                int startY = 0; // Start at the top
                int iconSize = 24;
                int spacingY = 30;

                // Cursor icon and text
                _spriteBatch.Draw(CursorT, new Rectangle(startX, startY, iconSize, iconSize), Color.Cyan);
                _spriteBatch.DrawString(BabyShibafont, "Cursor", new Vector2(startX + iconSize + 5, startY), Color.Cyan);

                startY += spacingY;

                // Active Party icon and text
                _spriteBatch.Draw(CursorT, new Rectangle(startX, startY, iconSize, iconSize), Color.Magenta);
                _spriteBatch.DrawString(BabyShibafont, "Active Party", new Vector2(startX + iconSize + 5, startY), Color.Magenta);

                startY += spacingY;

                // Inactive Party icon and text
                _spriteBatch.Draw(CursorT, new Rectangle(startX, startY, iconSize, iconSize), Color.White);
                _spriteBatch.DrawString(BabyShibafont, "Inactive Party", new Vector2(startX + iconSize + 5, startY), Color.White);

                // Skip a line
                startY += spacingY * 2;

                // Outline icons and text
                _spriteBatch.Draw(OutlineT, new Rectangle(startX, startY, iconSize, iconSize), new Color(125, 0, 255));
                _spriteBatch.DrawString(BabyShibafont, "Controlled", new Vector2(startX + iconSize + 5, startY), new Color(125, 0, 255));

                startY += spacingY;

                _spriteBatch.Draw(OutlineT, new Rectangle(startX, startY, iconSize, iconSize), Color.Yellow);
                _spriteBatch.DrawString(BabyShibafont, "Neutral", new Vector2(startX + iconSize + 5, startY), Color.Yellow);

                startY += spacingY;

                _spriteBatch.Draw(OutlineT, new Rectangle(startX, startY, iconSize, iconSize), Color.Red);
                _spriteBatch.DrawString(BabyShibafont, "Hostile", new Vector2(startX + iconSize + 5, startY), Color.Red);





                int y = 300;

                if (AscendantState == "ledger")
                {
                    // Handle the logic for the "expenses" state
                    _spriteBatch.DrawString(BabyShibafont, "Ledger", new Vector2(1900, y), Color.White);
                }
                else if (AscendantState == "management")
                {
                    // Handle the logic for the "manageAssociates" state

                    _spriteBatch.DrawString(BabyShibafont, "Management", new Vector2(1900, y), Color.White);
                    _spriteBatch.DrawString(BabyShibafont, "Use Arrow Keys and Enter to Select an Individual", new Vector2(1900, y + 30), Color.White);

                    y += 60;

                    int Archindex = 0;

                    foreach (Party p in Game1.GameWorld.GamePlayerAssociation.Parties)
                    {
                        _spriteBatch.DrawString(BabyShibafont, p.Name, new Vector2(1900, y), Color.White);
                        y += 30;

                        foreach(Architect a in p.Architects)
                        {
                            if(Archindex == AscendantCursor)
                            {
                                _spriteBatch.DrawString(BabyShibafont, "   [X] " + a.Name, new Vector2(1900, y), Color.White);
                            }
                            else
                            {
                                _spriteBatch.DrawString(BabyShibafont, "   [ ] " + a.Name, new Vector2(1900, y), Color.White);
                            }
                            Archindex++;
                            y += 30;
                        }
                    }
                }
                else if (AscendantState == "actions")
                {
                    // Handle the logic for the "deployment" state
                    _spriteBatch.DrawString(BabyShibafont, "Select a Party/Location to Act and press Enter.", new Vector2(1900, y), Color.White);
                    y += 30;
                    _spriteBatch.DrawString(BabyShibafont, "Press X to Cancel.", new Vector2(1900, y), Color.White);
                    y += 30;

                    foreach (Party p in GameWorld.GamePlayerAssociation.Parties)
                    {
                        y += 30;
                        if (GameWorld.GamePlayerAssociation.ActiveParty == p)
                        {
                            _spriteBatch.DrawString(BabyShibafont, "[X] " + p.Name + "(" + p.Leader.Location + ")", new Vector2(1900, y), Color.White);
                        }
                        else
                        {
                            _spriteBatch.DrawString(BabyShibafont, "[ ] " + p.Name + "(" + p.Leader.Location + ")", new Vector2(1900, y), Color.White);
                        }
                    }
                }
                else if (AscendantState == "selectaction")
                {
                    // Handle the logic for the "actions" state
                    _spriteBatch.DrawString(BabyShibafont, "Actions", new Vector2(1900, y), Color.White); y += 60;

                    _spriteBatch.DrawString(BabyShibafont, "[A] Steal an Artifact", new Vector2(1900, y), Color.White); y += 30;
                    _spriteBatch.DrawString(BabyShibafont, "[R] Raze a Building", new Vector2(1900, y), Color.White); y += 30;
                    _spriteBatch.DrawString(BabyShibafont, "[T] Sieze Location Control", new Vector2(1900, y), Color.White); y += 30;
                    _spriteBatch.DrawString(BabyShibafont, "[D] Kill Assorted", new Vector2(1900, y), Color.White); y += 30;
                    _spriteBatch.DrawString(BabyShibafont, "[K] Kidnap Assorted", new Vector2(1900, y), Color.White); y += 30;
                    _spriteBatch.DrawString(BabyShibafont, "[V] Kill Target", new Vector2(1900, y), Color.White); y += 30;
                    _spriteBatch.DrawString(BabyShibafont, "[Y] Kidnap Target", new Vector2(1900, y), Color.White); y += 30;
                    _spriteBatch.DrawString(BabyShibafont, "[I] Incite Violence", new Vector2(1900, y), Color.White); y += 30;

                    _spriteBatch.DrawString(BabyShibafont, "[X] Cancel", new Vector2(1900, y), Color.White); y += 30;
                }
                else if (AscendantState == "chooseactioncomponent")
                {
                    
                }
                else if (AscendantState == "build")
                {
                    // Handle the logic for the "build" state
                    _spriteBatch.DrawString(BabyShibafont, "Build", new Vector2(1900, y), Color.White);
                }
                else if (AscendantState == "movetoparty")
                {
                    // Handle the logic for the "deployment" state
                    _spriteBatch.DrawString(BabyShibafont, "Select a party to move " + CurrentlySelectedAscendantArchitect.Name + " to.", new Vector2(1900, y), Color.White);
                    y += 30;
                    _spriteBatch.DrawString(BabyShibafont, "Press N to make a new party.", new Vector2(1900, y), Color.White);
                    y += 30;
                    _spriteBatch.DrawString(BabyShibafont, "Press X to Cancel.", new Vector2(1900, y), Color.White);

                    y += 60;

                    foreach (Party p in GameWorld.GamePlayerAssociation.Parties)
                    {
                        y += 20;
                        if (GameWorld.GamePlayerAssociation.ActiveParty == p)
                        {
                            _spriteBatch.DrawString(BabyShibafont, "[X] " + p.Name, new Vector2(1900, y), Color.White);
                        }
                        else
                        {
                            _spriteBatch.DrawString(BabyShibafont, "[ ] " + p.Name, new Vector2(1900, y), Color.White);
                        }
                    }
                }
                else if (AscendantState == "deployment")
                {
                    // Handle the logic for the "deployment" state
                    _spriteBatch.DrawString(BabyShibafont, "Select a Party to Deploy and press Enter.", new Vector2(1900, y), Color.White);
                    y += 30;
                    _spriteBatch.DrawString(BabyShibafont, "Press X to Cancel.", new Vector2(1900, y), Color.White);
                    y += 30;

                    foreach(Party p in GameWorld.GamePlayerAssociation.Parties)
                    {
                        y += 30;
                        if (GameWorld.GamePlayerAssociation.ActiveParty == p)
                        {
                            _spriteBatch.DrawString(BabyShibafont, "[X] " + p.Name, new Vector2(1900, y), Color.White);
                        }
                        else
                        {
                            _spriteBatch.DrawString(BabyShibafont, "[ ] " + p.Name, new Vector2(1900, y), Color.White);
                        }
                    }
                }
                else if (AscendantState == "chooseactinggroup")
                {
                    // Handle the logic for the "deployment" state
                    _spriteBatch.DrawString(BabyShibafont, "Select a Party to act and press Enter.", new Vector2(1900, y), Color.White);
                    y += 30;
                    _spriteBatch.DrawString(BabyShibafont, "Press X to Cancel.", new Vector2(1900, y), Color.White);
                    y += 30;

                    foreach (Party p in GameWorld.GamePlayerAssociation.Parties)
                    {
                        y += 30;
                        if (GameWorld.GamePlayerAssociation.ActiveParty == p)
                        {
                            _spriteBatch.DrawString(BabyShibafont, "[X] " + p.Name, new Vector2(1900, y), Color.White);
                        }
                        else
                        {
                            _spriteBatch.DrawString(BabyShibafont, "[ ] " + p.Name, new Vector2(1900, y), Color.White);
                        }
                    }
                }
                else if (AscendantState == "choosedistrict")
                {
                    _spriteBatch.DrawString(BabyShibafont, "Select a District", new Vector2(1900, y), Color.White);
                    y += 30;
                    _spriteBatch.DrawString(BabyShibafont, "Use Arrow Keys to Navigate, Enter to Select.", new Vector2(1900, y), Color.White);
                    y += 30;

                    for (int i = 0; i < CurrentlySelectedAscendantParty.Leader.Location.Districts.Count; i++)
                    {
                        string prefix = (i == AscendantCursor) ? "[X]" : "[ ]";
                        _spriteBatch.DrawString(BabyShibafont, $"{prefix} {CurrentlySelectedAscendantParty.Leader.Location.Districts[i].Name}", new Vector2(1900, y), Color.White);
                        y += 30;
                    }
                }
                else if (AscendantState == "selectrelocation")
                {
                    _spriteBatch.DrawString(BabyShibafont, "Use Cursor and Press Enter to Restation your Party", new Vector2(1900, y), Color.White);
                }
                else if (AscendantState == "selectactingmode")
                {
                    _spriteBatch.DrawString(BabyShibafont, "Select Acting Mode", new Vector2(1900, y), Color.White);
                    y += 30;

                    _spriteBatch.DrawString(BabyShibafont, "[D] Directly Control Party", new Vector2(1900, y), Color.White);
                    y += 30;
                    _spriteBatch.DrawString(BabyShibafont, "[I] Issue Automatic Order", new Vector2(1900, y), Color.White);
                }

                else if (AscendantState == "chooseactioncomponent")
                {
                    int totalEntities = AscendantEntityLedger.Count;
                    int totalPages = (totalEntities + 9) / 10; // Calculate the total number of pages

                    // Display instructions
                    _spriteBatch.DrawString(BabyShibafont, "Select an Action Component", new Vector2(1900, y), Color.White);
                    y += 30;
                    _spriteBatch.DrawString(BabyShibafont, "Use Numbers to select.", new Vector2(1900, y), Color.White);
                    y += 30;

                    // Display the current page of entities
                    int startEntity = AscendantPage * 10;
                    int endEntity = Math.Min(startEntity + 10, totalEntities);

                    for (int i = startEntity; i < endEntity; i++)
                    {
                        int displayIndex = i % 10; // This gives values from 0-9

                        // Map 9 to '0' for display purposes
                        string keyDisplay = (displayIndex == 9) ? "0" : (displayIndex + 1).ToString();

                        _spriteBatch.DrawString(BabyShibafont, $"[{keyDisplay}] {AscendantEntityLedger[i].Name}", new Vector2(1900, y), Color.White);
                        y += 30;
                    }

                    // Page navigation
                    _spriteBatch.DrawString(BabyShibafont, $"Page {AscendantPage + 1} of {totalPages}", new Vector2(1900, y), Color.White);
                    y += 30;
                    _spriteBatch.DrawString(BabyShibafont, "[<] Previous Page   [>] Next Page", new Vector2(1900, y), Color.White);
                }

                else if (AscendantState == "main")
                {
                    // The main state logic (default screen with all options)
                    _spriteBatch.DrawString(BabyShibafont, "[E]xpenses", new Vector2(1900, 260), Color.White);
                    _spriteBatch.DrawString(BabyShibafont, "[M]anage Associates", new Vector2(1900, 280), Color.White);
                    _spriteBatch.DrawString(BabyShibafont, "[I]nitiate Action", new Vector2(1900, 300), Color.White);
                    _spriteBatch.DrawString(BabyShibafont, "[B]uild", new Vector2(1900, 320), Color.White);
                    _spriteBatch.DrawString(BabyShibafont, "[P]arty Deployment", new Vector2(1900, 340), Color.White);

                    _spriteBatch.DrawString(BabyShibafont, "[1] Wait One Day", new Vector2(1900, 420), Color.White);
                    _spriteBatch.DrawString(BabyShibafont, "[2] Wait One Week", new Vector2(1900, 440), Color.White);
                    _spriteBatch.DrawString(BabyShibafont, "[3] Wait One Month", new Vector2(1900, 460), Color.White);
                    _spriteBatch.DrawString(BabyShibafont, "[4] Wait One Year", new Vector2(1900, 480), Color.White);
                }

                // Draw world info when cursor hovers over a map tile
                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {
                        if (GameWorld.GamePlayerAssociation.AscendantX == x && GameWorld.GamePlayerAssociation.AscendantZ == z)
                        {
                            if (GameWorld.WorldMap[x + z * GameWorld.Width].Explored == true)
                            {
                                if (GameWorld.WorldMap[x + z * GameWorld.Width].Location == null)
                                {
                                    _spriteBatch.DrawString(BabyShibafont, "No Notable Location, " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome, new Vector2(DrawX, DrawY), Color.White);
                                }
                                else
                                {
                                    _spriteBatch.DrawString(BabyShibafont, GameWorld.WorldMap[x + z * GameWorld.Width].Location.Name + ", " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome + " " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.Type + ".", new Vector2(DrawX, DrawY), Color.White);
                                    _spriteBatch.DrawString(BabyShibafont, "Population: " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.TruePopulation(), new Vector2(DrawX, DrawY + 30), Color.White);

                                    int ArchitectPopulation = 0;
                                    int structures = 0;
                                    int groups = 0;

                                    foreach (Group g in Game1.GameWorld.Groups)
                                    {
                                        if (g.Leader.Location == GameWorld.WorldMap[x + z * GameWorld.Width].Location)
                                            groups++;
                                    }

                                    foreach (District d in GameWorld.WorldMap[x + z * GameWorld.Width].Location.Districts)
                                    {
                                        ArchitectPopulation += d.Architects.Count();

                                        for (int DistrictX = 0; DistrictX < 7; DistrictX++)
                                        {
                                            for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                                            {
                                                foreach (Structure s in d.DistrictMap[DistrictX + DistrictZ * 7].Structures)
                                                {
                                                    structures++;
                                                }
                                            }
                                        }
                                    }

                                    _spriteBatch.DrawString(BabyShibafont, "Total Architects: " + ArchitectPopulation, new Vector2(DrawX, DrawY + 60), Color.White);

                                    if (GameWorld.WorldMap[x + z * GameWorld.Width].Location.Government == null)
                                    {
                                        _spriteBatch.DrawString(BabyShibafont, "No Notable Government", new Vector2(DrawX, DrawY + 90), Color.White);
                                    }
                                    else
                                    {
                                        _spriteBatch.DrawString(BabyShibafont, "Government: " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.Government.Name, new Vector2(DrawX, DrawY + 90), Color.White);
                                    }

                                    _spriteBatch.DrawString(BabyShibafont, "Colonization Desire: " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.ColonizationDesire, new Vector2(DrawX, DrawY + 120), Color.White);
                                    _spriteBatch.DrawString(BabyShibafont, "Wealth: " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.Wealth, new Vector2(DrawX, DrawY + 150), Color.White);
                                    _spriteBatch.DrawString(BabyShibafont, "Is Saving Up To Settle: " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.IsSavingUpToSettle.ToString(), new Vector2(DrawX, DrawY + 180), Color.White);

                                    _spriteBatch.DrawString(BabyShibafont, "Structures: " + structures, new Vector2(DrawX, DrawY + 210), Color.White);
                                    _spriteBatch.DrawString(BabyShibafont, "Distinct Groups: " + groups, new Vector2(DrawX, DrawY + 240), Color.White);
                                    _spriteBatch.DrawString(BabyShibafont, "Districts: " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.Districts.Count(), new Vector2(DrawX, DrawY + 270), Color.White);
                                }
                            }
                            else
                            {
                                _spriteBatch.DrawString(BabyShibafont, "Unexplored", new Vector2(DrawX, DrawY), Color.White);

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
                _spriteBatch.DrawString(BabyShibafont, "Press Enter to Continue...", new Vector2(1750, 160), Color.White);
                _spriteBatch.DrawString(BabyShibafont, GameWorld.Name + " (hover over map for more info)", new Vector2(1750, DrawY), Color.White);

                DrawWorld();

                WorldMouseLogic();
            }
            else if (GameState == "partyturn" || GameState == "reaction" || GameState == "otherturn" || GameState == "messagereply" || GameState == "trypickup" || GameState == "trydrop")
            {
                if (MostRecentPartyTurnArchitect != null)
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
                        string districtText = "D: " + MostRecentPartyTurnArchitect.District.Name + " (" + MostRecentPartyTurnArchitect.Block.X + ", " + MostRecentPartyTurnArchitect.Block.Z + ")";
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



                        string GetAppendageLabel(Object appendage, bool isMain)
                        {
                            if (appendage.Type == "left hand")
                            {
                                return "LH: ";
                            }
                            else if (appendage.Type == "right hand")
                            {
                                return "RH: ";
                            }
                            return isMain ? "M: " : "O: ";
                        }

                        string GetAppendageText(Object appendage, Entity heldObject, bool isMain)
                        {
                            string appendageText = heldObject != null
                                ? GetAppendageLabel(appendage, isMain) + heldObject.ReferredToNames[0]
                                : GetAppendageLabel(appendage, isMain) + "(empty)";

                            if (appendageText.Length > 25)
                            {
                                appendageText = appendageText.Substring(0, 22) + "...";
                            }

                            return appendageText;
                        }


                        // Function to draw the appendage text and create hitboxes
                        void DrawAppendageText(Object appendage, Entity heldObject, bool isMain, Vector2 position)
                        {
                            string appendageText = GetAppendageText(appendage, heldObject, isMain);
                            _spriteBatch.DrawString(Shibafont, appendageText, position, Color.White);
                            Entity appendageEntity = heldObject != null ? heldObject : appendage;
                            EntityHitboxes.Add((new Rectangle(position.ToPoint(), Shibafont.MeasureString(appendageText).ToPoint()), appendageEntity));
                        }

                        // Main interaction appendage
                        Vector2 mainInteractionPosition = new Vector2(70, 240);
                        DrawAppendageText(MostRecentPartyTurnArchitect.MainInteractionAppendage, MostRecentPartyTurnArchitect.MainHeldObject, true, mainInteractionPosition);

                        // Off interaction appendage
                        Vector2 offInteractionPosition = new Vector2(70, 270);
                        DrawAppendageText(MostRecentPartyTurnArchitect.OffInteractionAppendage, MostRecentPartyTurnArchitect.OffHeldObject, false, offInteractionPosition);


                        _spriteBatch.DrawString(Shibafont, "Speed: " + MostRecentPartyTurnArchitect.Speed(), new Vector2(420, 120), Color.White);
                        _spriteBatch.DrawString(Shibafont, "CD: " + MostRecentPartyTurnArchitect.CooldownCycles, new Vector2(420, 150), Color.White);



                        string conditionText = "CND: ";
                        List<(string letter, Color color)> conditionLetters = new List<(string, Color)>();

                        if (MostRecentPartyTurnArchitect.FireSeconds > 0)
                            conditionLetters.Add(("F", Color.Orange));
                        if (MostRecentPartyTurnArchitect.WetCycles > 0)
                            conditionLetters.Add(("W", Color.LightBlue));
                        if (MostRecentPartyTurnArchitect.BlindCycles > 0)
                            conditionLetters.Add(("B", Color.White));
                        if (MostRecentPartyTurnArchitect.DestabilizedCycles > 0)
                            conditionLetters.Add(("D", Color.LightGray));
                        if (MostRecentPartyTurnArchitect.UnconsciousCycles > 0)
                            conditionLetters.Add(("U", Color.Red));
                        if (MostRecentPartyTurnArchitect.RadiantCycles > 0)
                            conditionLetters.Add(("R", Color.Yellow));
                        if (MostRecentPartyTurnArchitect.CloakCycles > 0)
                            conditionLetters.Add(("C", Color.Magenta));
                        if (MostRecentPartyTurnArchitect.FractalCycles > 0)
                            conditionLetters.Add(("F", Color.Green));
                        if (MostRecentPartyTurnArchitect.HoldCycles > 0)
                            conditionLetters.Add(("H", Color.LimeGreen));
                        if (MostRecentPartyTurnArchitect.DismissalCycles > 0)
                            conditionLetters.Add(("D", Color.Purple));

                        Vector2 currentPos = new Vector2(420, 180);

                        _spriteBatch.DrawString(Shibafont, conditionText, currentPos, Color.White);
                        currentPos.X += Shibafont.MeasureString(conditionText).X;

                        foreach (var condition in conditionLetters)
                        {
                            _spriteBatch.DrawString(Shibafont, condition.letter, currentPos, condition.color);
                            currentPos.X += Shibafont.MeasureString(condition.letter).X;
                        }

                        if (MostRecentPartyTurnArchitect.CombatCycles > 0)
                        {
                            _spriteBatch.DrawString(Shibafont, "Evade: " + MostRecentPartyTurnArchitect.EscapeChance(), new Vector2(420, 230), Color.White);
                        }

                        _spriteBatch.DrawString(Shibafont, "Press TAB for More.", new Vector2(420, 270), Color.White);


                        int Line = 0;


                        Vector2 basePosition = new Vector2(647, 1310);

                        string dateTimeString = GameWorld.GetFormattedDateTime();
                        Vector2 dateTimeSize = Shibafont.MeasureString(dateTimeString);
                        Vector2 dateTimePosition = basePosition;
                        _spriteBatch.DrawString(Shibafont, dateTimeString, dateTimePosition, Color.White);

                        Color oColor = GameWorld.IsNightTime() ? new Color(100, 100, 100) : Color.Goldenrod;
                        Vector2 oPosition = new Vector2(dateTimePosition.X + dateTimeSize.X + 10, dateTimePosition.Y);
                        _spriteBatch.DrawString(Shibafont, "O", oPosition, oColor);

                        Vector2 helpPosition = new Vector2(basePosition.X, basePosition.Y + 50);
                        _spriteBatch.DrawString(Shibafont, "Press CTRL and ? or + for Help", helpPosition, Color.White);

                        Rectangle backgroundRect = new Rectangle(50, 1258, 176, 176);

                        //district map

                        _spriteBatch.Draw(FrameT, backgroundRect, Color.White);

                        Rectangle MMR = new Rectangle(250, 1258, 176, 176);

                        _spriteBatch.Draw(MoveMapFrameT, MMR, Color.White);

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

                                    if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures.Count() == 0)
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
                                    else if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures.Count() == 1)
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
                                            case "bastion":
                                                DecidedTexture = DistrictBastionT;
                                                break;
                                            case "fort":
                                                DecidedTexture = DistrictFortT;
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
                                    if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures.Count() > 0)
                                    {
                                        switch (MostRecentPartyTurnArchitect.Location.PrimaryRace.Name)
                                        {
                                            case "nightfell":
                                                BuildingColor = new Color(100, 100, 100);
                                                break;
                                            case "archaix":
                                                BuildingColor = new Color(150, 150, 150);
                                                break;
                                            case "photonexus":
                                                BuildingColor = new Color(200, 200, 255);
                                                break;
                                            case "isofractal":
                                                BuildingColor = ColorConverter[MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures[0].FakeIsofractalColor];
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

                                    if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Architects.Count() > 0 && FlashTick > 50)
                                    {
                                        var aliveArchitects = MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Architects.Where(architect => architect.IsAlive);
                                        if (aliveArchitects.Count() > 0)
                                        {
                                            Color HeavinessColor = Color.White;
                                            if (aliveArchitects.Count() > 10)
                                            {
                                                HeavinessColor = Color.Blue;
                                            }
                                            else if (aliveArchitects.Count() > 5)
                                            {
                                                HeavinessColor = Color.CornflowerBlue;
                                            }
                                            else if (aliveArchitects.Count() > 2)
                                            {
                                                HeavinessColor = Color.LightBlue;
                                            }
                                            else if (aliveArchitects.Count() > 1)
                                            {
                                                HeavinessColor = Color.LightCyan;
                                            }

                                            _spriteBatch.Draw(ArchitectHere, drawRect, HeavinessColor);
                                        }
                                    }

                                }
                            }
                        }

                        DrawAnnouncements();



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

                                if(architect.YLevelInFeet > 0)
                                {
                                    description += (", " + architect.YLevelInFeet.ToString() + " ft. altitude");
                                }

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

                            if (MostRecentPartyTurnArchitect.BlindCycles > 0)
                            {
                                string textToDraw = "You are currently blind.";
                                Vector2 indentCoords = new Vector2(startCoords.X, startCoords.Y + line * 20);
                                spriteBatch.DrawString(font, textToDraw, indentCoords, Color.Yellow);
                                return;
                            }

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

                            // Test market debt
                            if (MostRecentPartyTurnArchitect.Structure.Type == "market")
                            {
                                Color c = Color.White;
                                string message = "Neither you nor the market have unsettled debts.";

                                if (MostRecentPartyTurnArchitect.Structure.MarketDebt < 0)
                                {
                                    c = Color.Red;
                                    message = "You owe the market here " + Math.Abs(MostRecentPartyTurnArchitect.Structure.MarketDebt) + "*. Leaving would be ill-advised.";
                                }
                                else if (MostRecentPartyTurnArchitect.Structure.MarketDebt > 0)
                                {
                                    c = Color.Green;
                                    message = "The market here owes you " + MostRecentPartyTurnArchitect.Structure.MarketDebt + "*.";
                                }

                                DrawCenteredTextAtPosition(_spriteBatch, "Market Ledger", 1856, 150, BabyShibafont, Color.White);
                                DrawSplitText(_spriteBatch, message, 1856, 172, BabyShibafont, c);
                            }

                            void DrawSplitText(SpriteBatch spriteBatch, string text, float x, float y, SpriteFont font, Color color)
                            {
                                // Split the text into words
                                string[] words = text.Split(' ');
                                int middleIndex = words.Length / 2;

                                // First half
                                string firstHalf = string.Join(" ", words.Take(middleIndex));
                                // Second half
                                string secondHalf = string.Join(" ", words.Skip(middleIndex));

                                // Measure text heights
                                Vector2 firstHalfSize = font.MeasureString(firstHalf);
                                Vector2 secondHalfSize = font.MeasureString(secondHalf);

                                // Draw both lines
                                spriteBatch.DrawString(font, firstHalf, new Vector2(x - firstHalfSize.X / 2, y), color);
                                spriteBatch.DrawString(font, secondHalf, new Vector2(x - secondHalfSize.X / 2, y + firstHalfSize.Y + 5), color);
                            }
                        }
                        else
                        {
                            // Get a list of unique architects
                            var uniqueArchitects = GetUniqueArchitects(MostRecentPartyTurnArchitect.Block.Architects);
                            DrawArchitects(_spriteBatch, uniqueArchitects, new Vector2(950, 150), BabyShibafont, 20);

                            List<Tuple<string, Structure>> structureLines = new List<Tuple<string, Structure>>();

                            if (MostRecentPartyTurnArchitect.BlindCycles == 0)
                            {
                                foreach (Structure s in MostRecentPartyTurnArchitect.Block.Structures)
                                {
                                    if (s.Type == "house" || s.Type == "bighouse")
                                    {
                                        Houses++;
                                    }
                                    else
                                    {
                                        string structureText = "(" + s.Type.Substring(0, 1).ToUpper() + ") " + s.Name;
                                        structureLines.Add(Tuple.Create(structureText, s));
                                    }
                                }

                                // Add the houses line if it exists
                                if (Houses == 1)
                                {
                                    structureLines.Insert(0, Tuple.Create("1 house (house 1)", (Structure)null));
                                }
                                else if (Houses > 1)
                                {
                                    structureLines.Insert(0, Tuple.Create(Houses + " houses (house 1-" + Houses + ")", (Structure)null));
                                }
                            }
                            else
                            {
                                structureLines.Add(Tuple.Create("You are currently blind.", (Structure)null));
                            }

                            // Draw all the structure lines
                            Line = 0;
                            foreach (var structureLine in structureLines)
                            {
                                string structureText = structureLine.Item1;
                                Structure structure = structureLine.Item2;
                                Vector2 structurePosition = new Vector2(1700, Line * 30 + 170);
                                _spriteBatch.DrawString(Shibafont, structureText, structurePosition, Color.White);
                                Line++;

                                // Add hitboxes for structures (excluding the "You are currently blind." line and houses line)
                                if (structure != null)
                                {
                                    EntityHitboxes.Add((new Rectangle(structurePosition.ToPoint(), Shibafont.MeasureString(structureText).ToPoint()), structure));
                                }
                            }
                        }


                        if (IsInGui)
                        {
                            _spriteBatch.Draw(MessageGUIT, new Rectangle(0, 0, 2560, 1440), Color.White);
                            int Linee = 0;
                            foreach (string s in ItemPickupGuiLines)
                            {
                                DrawCenteredText(_spriteBatch, s, Linee * 35 + 600, Shibafont, Color.White);
                                Linee++;
                            }
                        }

                        if (GameState == "reaction")
                        {
                            if (Keyboard.GetState().IsKeyDown(Keys.OemQuestion))
                            {
                                _spriteBatch.Draw(ReactionGUIHelpT, new Rectangle(320, 180, 1920, 1080), Color.White);
                            }
                            else
                            {
                                _spriteBatch.Draw(ReactionGUIT, new Rectangle(320, 180, 1920, 1080), Color.White);

                                // Calculate the success chances for the MostRecentPartyTurnArchitect
                                var successChances = MostRecentPartyTurnArchitect.CalculateSuccessChances(StoredAttack, Game1.GameWorld.ReactionModifierInt, StoredAttack.Attacker, StoredAttack.Attacker.GetProficiency(StoredAttack.Weapon.DamageType));

                                // Draw the MostRecentPartyTurnArchitect on the left
                                DrawCharacter((Architect)(StoredAttack.Target.Creator), 900, 820, 0.2);

                                // Draw the attacker on the right, inverted
                                DrawCharacter(StoredAttack.Attacker, 1570, 820, 0.2, Inverted: true);

                                int y = 600;
                                int d = 30;

                                string Exposed = "";

                                if (StoredAttack.Target.Exposure > 49)
                                {
                                    Exposed = "exposed ";
                                }

                                DrawCenteredText(_spriteBatch, StoredAttack.Attacker.ReferredToNames[0] + " is aiming a " + StoredAttack.Verb, y, Shibafont, Color.White);
                                DrawCenteredText(_spriteBatch, "at your " + Exposed + StoredAttack.Target.Type + " with their " + StoredAttack.Weapon.ReferredToNames[0], y + d, Shibafont, Color.White);

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

                                if ((MostRecentPartyTurnArchitect.OffHeldObject != null && MostRecentPartyTurnArchitect.OffHeldObject.IsWeapon) || (MostRecentPartyTurnArchitect.MainHeldObject != null && MostRecentPartyTurnArchitect.MainHeldObject.IsWeapon))
                                {
                                    DrawCenteredText(_spriteBatch, "[P] Parry the Attack (" + RoundToNearestFive(successChances.parry) + "%?)", y + d * 8, Shibafont, Color.White);
                                }
                                if ((MostRecentPartyTurnArchitect.MainHeldObject != null && MostRecentPartyTurnArchitect.MainHeldObject.Type == "shield") || (MostRecentPartyTurnArchitect.OffHeldObject != null && MostRecentPartyTurnArchitect.OffHeldObject.Type == "shield"))
                                {
                                    DrawCenteredText(_spriteBatch, "[B] Block the Attack (" + RoundToNearestFive(successChances.block) + "%?)", y + d * 9, Shibafont, Color.White);
                                }

                                DrawCenteredText(_spriteBatch, "Press ? for detailed info.", y + d * 10, Shibafont, Color.White);
                            }
                        }

                        else if (GameState == "trypickup")
                        {
                            int GetHalfCount(string itemType)
                            {
                                int totalCount = (MostRecentPartyTurnArchitect.Room != null ? MostRecentPartyTurnArchitect.Room.Objects : MostRecentPartyTurnArchitect.Block.Objects)
                                    .Count(item => item.Type == itemType);
                                return (int)Math.Ceiling(totalCount / 2.0);
                            }

                            int GetFullCount(string itemType)
                            {
                                return (MostRecentPartyTurnArchitect.Room != null ? MostRecentPartyTurnArchitect.Room.Objects : MostRecentPartyTurnArchitect.Block.Objects)
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
                            int GetHalfCount(string itemType, EntityHashSet<Material> itemMaterials)
                            {
                                int totalCount = MostRecentPartyTurnArchitect.Inventory
                                    .Count(item => item.Type == itemType && item.Materials.SequenceEqual(itemMaterials));
                                return (int)Math.Ceiling(totalCount / 2.0);
                            }

                            int GetFullCount(string itemType, EntityHashSet<Material> itemMaterials)
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

                        else if (GameState == "messagereply" && MostRecentPartyTurnArchitect.MessagesNotRespondedTo.Count() > 0)
                        {
                            _spriteBatch.Draw(MessageGUIT, new Rectangle(0, 0, 2560, 1440), Color.White);

                            // Draw the MostRecentPartyTurnArchitect on the left
                            DrawCharacter(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Receiver, 675, 300, 0.2);

                            // Draw the message sender on the right, inverted
                            DrawCharacter(MostRecentPartyTurnArchitect.MessagesNotRespondedTo[0].Sender, 1800, 300, 0.2, Inverted: true);

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

                                _spriteBatch.DrawString(BabyShibafont, "[X] Exit", new Vector2(830, 1050), Color.White);
                            }
                            // Draw commands
                            else if (CommandBuilderStage == "commands")
                            {
                                var commands = GetCommandsForCategory(SelectedCategory);
                                var keys = new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" };

                                List<string> commandList = commands.Take(keys.Count()).Select((cmd, index) => $"[{keys[index]}] {cmd}").ToList();
                                DrawListInColumns(commandList, 830, startY, 40, 50, BabyShibafont);

                                _spriteBatch.DrawString(BabyShibafont, "[X] Go Back", new Vector2(830, 1050), Color.White);
                            }
                            // Draw pickingsubjects
                            else if (CommandBuilderStage == "pickingsubjects")
                            {
                                int yPositionLeft = startY; // Starting Y position for the left column
                                int yPositionRight = startY; // Starting Y position for the right column
                                int startIndex = CurrentCommandBuilderPage * 20; // Calculate start index based on the current page

                                // Draw subjects
                                if (RelevantEntities.Count() == 0)
                                {
                                    _spriteBatch.DrawString(BabyShibafont, "No Applicable Subjects", new Vector2(830, yPositionLeft), Color.White);
                                }
                                else
                                {
                                    var keys = new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" };
                                    var subjectList = new List<string>();

                                    for (int i = 0; i < 20 && (startIndex + i) < RelevantEntities.Count(); i++)
                                    {
                                        var entity = RelevantEntities[startIndex + i];
                                        subjectList.Add($"[{keys[i]}] {entity.ReferredToNames[0]}");
                                    }

                                    DrawListInColumns(subjectList, 830, startY, 40, 50, BabyShibafont);
                                }

                                // Draw guides at the bottom
                                _spriteBatch.DrawString(BabyShibafont, "[X] Go Back", new Vector2(830, 1050), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "[A/S] Navigate", new Vector2(1070, 1050), Color.White);
                            }

                        }


                        void DrawListInColumns(List<string> items, int startX, int startY, int lineSpacing, int boundaryPadding, SpriteFont font)
                        {
                            int xPosition = startX;
                            int yPosition = startY;
                            int columnWidth = 530; // Half of the MessageGUIT width

                            int middleIndex = (items.Count() + 1) / 2; // Round up to ensure left column is prioritized

                            // Draw the first half of the items in the first column
                            for (int i = 0; i < middleIndex; i++)
                            {
                                // Draw the item
                                _spriteBatch.DrawString(font, items[i].Replace('_', ' '), new Vector2(xPosition, yPosition), Color.White);

                                yPosition += lineSpacing;
                            }

                            // Reset yPosition for the second column
                            yPosition = startY;
                            xPosition += columnWidth;

                            // Draw the second half of the items in the second column
                            for (int i = middleIndex; i < items.Count(); i++)
                            {
                                // Draw the item
                                _spriteBatch.DrawString(font, items[i].Replace('_', ' '), new Vector2(xPosition, yPosition), Color.White);

                                yPosition += lineSpacing;
                            }
                        }


                        // Declare the static adjustment variable
                        int PositionAdjustment = 400;

                        var sortedArchitects = GameWorld.GamePlayerAssociation.ActiveParty.Architects.OrderBy(a => a.CooldownCycles);
                        int currentX = 1400 + PositionAdjustment; // Apply the adjustment

                        foreach (var architect in sortedArchitects)
                        {
                            if (GameWorld.HumanoidRaces.Contains(architect.Race))
                            {
                                // Draw the character
                                DrawCharacter(architect, currentX, 1200, 0.2);
                                currentX -= 120; // Move to the left for the next character
                            }
                        }

                        if (MostRecentPartyTurnArchitect.CombatCycles > 0)
                        {
                            if (GameWorld.HumanoidRaces.Contains(MostRecentPartyTurnArchitect.Race))
                            {
                                Vector2 bodyPartsPosition = new Vector2(currentX + (PositionAdjustment-200), 1225); // Apply the adjustment

                                // Draw the body parts
                                DrawBodyParts(_spriteBatch, bodyPartsPosition);

                                // Calculate the position for the "Exposure" text
                                float textCenterX = bodyPartsPosition.X + (300 * 0.6f) / 2; // Assuming the width of the rectangle is 300
                                float textCenterY = bodyPartsPosition.Y - 20; // Position the text above the rectangle

                                // Draw the "Exposure" text
                                DrawCenteredTextAtPosition(_spriteBatch, "Exposure", textCenterX, textCenterY, BabyShibafont, Color.White);
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

                            // Calculate the red and blue values based on the exposure (0 to 100 scale)
                            int red = (exposure <= 50) ? (int)(1 + 124 * (exposure / 50f)) : (int)(125 + 130 * ((exposure - 50) / 50f));
                            int blue = (exposure <= 50) ? (int)(1 + 254 * (exposure / 50f)) : 255;

                            Color color = new Color(red, 0, blue);

                            spriteBatch.Draw(texture, position, null, color, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                        }

                        if ((Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.RightControl)) && Keyboard.GetState().IsKeyDown(Keys.OemPlus))
                        {
                            _spriteBatch.Draw(QuickStartGUI, new Rectangle(0, 0, 2560, 1440), Color.White);
                        }
                    }
                    else
                    {
                        _spriteBatch.Draw(InventoryGUI, new Rectangle(0, 0, 2560, 1440), Color.White);

                        DrawCenteredText(_spriteBatch, MostRecentPartyTurnArchitect.Name, 80, BabyShibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "STR: " + MostRecentPartyTurnArchitect.Strength + " / " +
                                                        "AGL: " + MostRecentPartyTurnArchitect.Agility + " / " +
                                                        "DEX: " + MostRecentPartyTurnArchitect.Dexterity + " / " +
                                                        "END: " + MostRecentPartyTurnArchitect.Endurance + " / " +
                                                        "CRE: " + MostRecentPartyTurnArchitect.Creativity + " / " +
                                                        "CHA: " + MostRecentPartyTurnArchitect.Charisma + " / " +
                                                        "FOC: " + MostRecentPartyTurnArchitect.Focus,
                                                        110, BabyShibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "Use \"remember skills\" or \"remember spells\" to list spell or skill information.", 140, BabyShibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "Use tilde (~) and a specific key to get path information.", 170, BabyShibafont, Color.White);

                        int line = 0;

                        var sourceObjects = MostRecentPartyTurnArchitect.Inventory;
                        var structuredList = CondenseAndStructureList(sourceObjects, SplitMode);
                        DrawList(_spriteBatch, structuredList, new Vector2(2100, 100), BabyShibafont, CurrentObjectPage, ItemsPerPage);

                        line = 0;
                        if (MostRecentPartyTurnArchitect.Clothing.Count() == 0)
                        {
                            DrawCenteredTextAtPosition(_spriteBatch, "You have no clothing. Please put some on.", 1625, 425, BabyShibafont, Color.White);
                        }
                        else
                        {
                            foreach (Object o in MostRecentPartyTurnArchitect.Clothing)
                            {
                                float centerY = 425 + 20 * line;
                                string text = o.ReferredToNames[0];

                                // Draw the text
                                DrawCenteredTextAtPosition(_spriteBatch, text, 1625, centerY, BabyShibafont, Color.White);

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
                        int intrigueCount = GameWorld.GamePlayerAssociation.ActiveParty.Intrigue.Count();

                        if (intrigueCount == 0)
                        {
                            DrawCenteredTextAtPosition(_spriteBatch, "Nothing is here yet...", 1625, 1150, BabyShibafont, Color.White);
                        }
                        else
                        {
                            // Calculate the starting index to only draw the last 10 lines
                            int startIndex = intrigueCount > 10 ? intrigueCount - 10 : 0;
                            var intriguesToDraw = GameWorld.GamePlayerAssociation.ActiveParty.Intrigue.Skip(startIndex);

                            foreach (var intrigue in intriguesToDraw)
                            {
                                Vector2 textPosition = new Vector2(1640, 1150 + 20 * line);
                                DrawCenteredTextAtPosition(_spriteBatch, intrigue.Data, (int)textPosition.X, (int)textPosition.Y, BabyShibafont, intrigue.Color);

                                // Create hitbox for the intrigue text
                                Vector2 textSize = Shibafont.MeasureString(intrigue.Data);
                                Rectangle hitbox = new Rectangle(textPosition.ToPoint(), textSize.ToPoint());

                                if (intrigue.Entities.Count > 1)
                                {
                                    EntityHitboxes.Add((hitbox, intrigue.Entities[1]));
                                }
                                line++;
                            }
                        }

                        line = 0;


                        // Check for melded shibas and list them first
                        if (MostRecentPartyTurnArchitect.MeldedShibas.Count() > 0)
                        {
                            string meldedShibasText = MostRecentPartyTurnArchitect.MeldedShibas.Count() == 1 ?
                                "1 shiba has melded with your face, applying unstable buffs." :
                                $"{MostRecentPartyTurnArchitect.MeldedShibas.Count()} shibas have melded with your face, applying unstable buffs.";

                            DrawCenteredTextAtPosition(_spriteBatch, meldedShibasText, 950, 425 + 20 * line, BabyShibafont, Color.White);
                            line++;
                        }

                        // Preprocess descriptions and combine imbuements
                        Dictionary<string, (int Count, int CombinedSecondPower, Imbuement Imbuement, int Contributors)> imbuementData = new Dictionary<string, (int, int, Imbuement, int)>();

                        foreach (Imbuement i in MostRecentPartyTurnArchitect.CurrentlyActiveImbuements)
                        {
                            string description = i.GetDescription();

                            if (i.IsTrigger)
                            {
                                if (imbuementData.ContainsKey(description))
                                {
                                    imbuementData[description] = (imbuementData[description].Count + 1, imbuementData[description].CombinedSecondPower, i, imbuementData[description].Contributors);
                                }
                                else
                                {
                                    imbuementData[description] = (1, i.SecondPower, i, 1);
                                }
                            }
                            else
                            {
                                // Combine non-trigger imbuements with matching properties
                                bool combined = false;
                                foreach (var key in imbuementData.Keys.ToList())
                                {
                                    var existingImbuement = imbuementData[key].Imbuement;
                                    if (!existingImbuement.IsTrigger &&
                                        existingImbuement.ConditionOrTrigger == i.ConditionOrTrigger &&
                                        existingImbuement.BuffOrResult == i.BuffOrResult &&
                                        existingImbuement.FirstPower == i.FirstPower)
                                    {
                                        int newSecondPower = existingImbuement.SecondPower + i.SecondPower;
                                        string newDescription = $"{existingImbuement.GetConditionDescription()}increase {existingImbuement.BuffOrResult} by {newSecondPower}%";
                                        imbuementData.Remove(key);
                                        imbuementData[newDescription] = (1, newSecondPower, i, imbuementData[key].Contributors + 1);
                                        combined = true;
                                        break;
                                    }
                                }

                                if (!combined)
                                {
                                    imbuementData[description] = (1, i.SecondPower, i, 1);
                                }
                            }
                        }

                        // Draw descriptions
                        if (imbuementData.Count > 0)
                        {
                            foreach (var entry in imbuementData)
                            {
                                string description = entry.Key;
                                int count = entry.Value.Count;
                                int combinedSecondPower = entry.Value.CombinedSecondPower;
                                Imbuement imbuement = entry.Value.Imbuement;
                                int contributors = entry.Value.Contributors;

                                if (count > 1)
                                {
                                    description += $" (x{count})";
                                }

                                if (contributors > 1)
                                {
                                    description += $" ({contributors} Contributing)";
                                }

                                while (description.Length > 40)
                                {
                                    int splitIndex = 40;
                                    if (description.Length > 40)
                                    {
                                        splitIndex = description.Substring(0, 40).LastIndexOf(' ');
                                        if (splitIndex == -1) splitIndex = 40;
                                    }

                                    DrawCenteredTextAtPosition(_spriteBatch, description.Substring(0, splitIndex), 950, 425 + 20 * line, BabyShibafont, imbuement.IsTrigger ? Color.Goldenrod : (imbuement.IsSatisfied ? Color.Lime : Color.White));
                                    description = description.Substring(splitIndex).Trim();
                                    line++;
                                }

                                DrawCenteredTextAtPosition(_spriteBatch, description, 950, 425 + 20 * line, BabyShibafont, imbuement.IsTrigger ? Color.Goldenrod : (imbuement.IsSatisfied ? Color.Lime : Color.White));
                                line++;
                            }
                        }
                        else if (MostRecentPartyTurnArchitect.MeldedShibas.Count() == 0)
                        {
                            // If no melded shibas and no imbuements, display "No active imbuements."
                            DrawCenteredTextAtPosition(_spriteBatch, "No active imbuements.", 950, 425, BabyShibafont, Color.White);
                        }


                        // XPValues processing remains unchanged as you already have a check for no entries.

                        line = 0;

                        foreach ((string, int) p in MostRecentPartyTurnArchitect.XPValues)
                        {
                            line++;
                            DrawCenteredTextAtPosition(_spriteBatch, $"{p.Item1}: {ConvertNumberToProficiency(MostRecentPartyTurnArchitect.GetProficiency(p.Item1))}, {MostRecentPartyTurnArchitect.GetXP(p.Item1)} XP", 290, 100 + 20 * line, BabyShibafont, Color.White);
                        }
                        if (line == 0)
                        {
                            DrawCenteredTextAtPosition(_spriteBatch, "No proficiencies...?", 280, 120 + 10 * line, BabyShibafont, Color.White);
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

                            void DrawPathLevels(SpriteBatch spriteBatch, SpriteFont babyShibaFont)
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

                            DrawPathLevels(_spriteBatch, BabyShibafont);
                        }


                        if (!Keyboard.GetState().IsKeyDown(Keys.LeftControl) && (MostRecentPartyTurnArchitect.SpendableLevels > 0 || Keyboard.GetState().IsKeyDown(Keys.OemTilde)))
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

                            int position = 500;

                            // Each path's drawing code:
                            DrawPathLevel(_spriteBatch, BabyShibafont, "[X] Path of Shadow", MostRecentPartyTurnArchitect.PathOfShadowLevel, position, Color.MidnightBlue);
                            position += spacing;

                            DrawPathLevel(_spriteBatch, BabyShibafont, "[L] Path of Life", MostRecentPartyTurnArchitect.PathOfLifeLevel, position, Color.ForestGreen);
                            position += spacing;

                            DrawPathLevel(_spriteBatch, BabyShibafont, "[D] Path of Death", MostRecentPartyTurnArchitect.PathOfDeathLevel, position, Color.DarkRed);
                            position += spacing;

                            DrawPathLevel(_spriteBatch, BabyShibafont, "[A] Path of Stars", MostRecentPartyTurnArchitect.PathOfStarsLevel, position, Color.Gold);
                            position += spacing;

                            DrawPathLevel(_spriteBatch, BabyShibafont, "[H] Path of Heat", MostRecentPartyTurnArchitect.PathOfHeatLevel, position, Color.OrangeRed);
                            position += spacing;

                            DrawPathLevel(_spriteBatch, BabyShibafont, "[B] Path of Body", MostRecentPartyTurnArchitect.PathOfBodyLevel, position, Color.SandyBrown);
                            position += spacing;

                            DrawPathLevel(_spriteBatch, BabyShibafont, "[R] Path of Reality", MostRecentPartyTurnArchitect.PathOfRealityLevel, position, Color.IndianRed);
                            position += spacing;

                            DrawPathLevel(_spriteBatch, BabyShibafont, "[G] Path of Light", MostRecentPartyTurnArchitect.PathOfLightLevel, position, Color.Yellow);
                            position += spacing;

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

                                position = startingPosition;

                                DrawCenteredText(_spriteBatch, "PATH OF SHADOW", position, BabyShibafont, Color.MidnightBlue);
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 AGL ", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 1) ? Color.MidnightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Become slightly harder to see and target.", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 2) ? Color.MidnightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 AGL ", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 3) ? Color.MidnightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: \"become one with shadow\" to go invisible but lose energy. Your items are visible.", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 4) ? Color.MidnightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 AGL ", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 5) ? Color.MidnightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Your possessions become invisible with you.", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 6) ? Color.MidnightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 AGL ", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 7) ? Color.MidnightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Energy loss from invisibility cut in half.", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 8) ? Color.MidnightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 9: +1 AGL ", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 9) ? Color.MidnightBlue : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "PRESS CTRL X TO LEVEL UP THIS PATH.", position, BabyShibafont, Color.White);
                            }

                            if (Keyboard.GetState().IsKeyDown(Keys.L)) // Assuming 'L' is the key for Path of Life
                            {
                                _spriteBatch.Draw(ReactionGUIT, new Rectangle((2560 - newWidth) / 2, (1440 - newHeight) / 2, newWidth, newHeight), Color.White);

                                position = startingPosition;

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
                                DrawCenteredText(_spriteBatch, "LVL 6: Pacify/Tame animals, add to party. Max of Path LVL animals. Use \"pacify ~\"", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 6) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 CHA", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 7) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;

                                DrawCenteredText(_spriteBatch, "LVL 8: Buff/augment your animals with \"augment ~\".", position, BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 8) ? Color.ForestGreen : new Color(40, 40, 40));
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

                                position = startingPosition;

                                // Title
                                DrawCenteredText(_spriteBatch, "PATH OF DEATH", position, BabyShibafont, Color.DarkRed);
                                position += spacing;
                                // Abilities
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 FOC", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 1 ? Color.DarkRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Raise ((LVL/2) rounded down) weakened undead servants with \"raise ~\"", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 2 ? Color.DarkRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 FOC", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 3 ? Color.DarkRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Fire spectral bolts with \"spectralize ~\".", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 4 ? Color.DarkRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 FOC", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 5 ? Color.DarkRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Foes slain by bolts may become undead.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 6 ? Color.DarkRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 FOC", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 7 ? Color.DarkRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Revive on death with 30 energy once a day. Your undead fire spectral bolts.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 8 ? Color.DarkRed : new Color(40, 40, 40));
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

                                position = startingPosition;

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
                                DrawCenteredText(_spriteBatch, "LVL 4: Starstruck creatures ignite.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 4 ? Color.Gold : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 CRE", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 5 ? Color.Gold : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Fire stars from your hands with \"starstrike ~\".", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 6 ? Color.Gold : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 CRE", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 7 ? Color.Gold : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Open a portal to a star and fire a laser with \"starsmite ~\".", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 8 ? Color.Gold : new Color(40, 40, 40));
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

                                position = startingPosition;

                                // Title
                                DrawCenteredText(_spriteBatch, "PATH OF HEAT", position, BabyShibafont, Color.OrangeRed);
                                position += spacing;
                                // Abilities
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 END", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 1 ? Color.OrangeRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Conjure and throw waves of heat with \"flamestrike ~\".", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 2 ? Color.OrangeRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 END", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 3 ? Color.OrangeRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Heat objects you hold, with \"sear ~\" to increase damage. Become immune to fire.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 4 ? Color.OrangeRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 END", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 5 ? Color.OrangeRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Conjure larger waves of heat.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 6 ? Color.OrangeRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 END", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 7 ? Color.OrangeRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Set self on fire at will with \"inflame\". Increases fire abliities.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 8 ? Color.OrangeRed : new Color(40, 40, 40));
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

                                position = startingPosition;

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF BODY", position, BabyShibafont, Color.SandyBrown);
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 STR", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 1 ? Color.SandyBrown : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Increases to all physical stats.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 2 ? Color.SandyBrown : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 STR", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 3 ? Color.SandyBrown : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Increased unarmed melee capabilities and agility.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 4 ? Color.SandyBrown : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 STR", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 5 ? Color.SandyBrown : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Unarmed strikes channel radiant energy.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 6 ? Color.SandyBrown : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 STR", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 7 ? Color.SandyBrown : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Gain a considerable boost to speed.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 8 ? Color.SandyBrown : new Color(40, 40, 40));
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

                                position = startingPosition;

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF LIGHT", position, BabyShibafont, Color.Yellow);
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 AGL. Summon sparks with \"conjure spark\". Maximum of Path Level.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 1 ? Color.Yellow : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Use \"evoke blindness\" to consume a spark and blind nearby hostiles.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 2 ? Color.Yellow : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 AGL", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 3 ? Color.Yellow : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Consume a spark to strike a target with \"evoke strike at ~\".", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 4 ? Color.Yellow : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 AGL", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 5 ? Color.Yellow : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Use a spark out of combat to fully heal all nearby friendlies with \"evoke healing\".", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 6 ? Color.Yellow : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 AGL", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 7 ? Color.Yellow : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Use a spark to conjure a loyal photonexus with \"evoke nexus\".", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 8 ? Color.Yellow : new Color(40, 40, 40));
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

                                position = startingPosition;

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF REALITY", position, BabyShibafont, Color.IndianRed);
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 DEX", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 1 ? Color.IndianRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 2: Increase/Decrease integrity, temperature, aerodynamics, and weight of objects once.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 2 ? Color.IndianRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 DEX", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 3 ? Color.IndianRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 4: Liquify objects and structures with \"liquify ~\"", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 4 ? Color.IndianRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 DEX", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 5 ? Color.IndianRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Duplicate objects with \"split ~\".", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 6 ? Color.IndianRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 DEX", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 7 ? Color.IndianRed : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 8: Remove people from reality with \"blip ~\". Requires time and multiple tries.", position, BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 8 ? Color.IndianRed : new Color(40, 40, 40));
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


                        //character
                        DrawCharacter(MostRecentPartyTurnArchitect, 500, 1200, 0.2);

                    }

                    string[] pronouns = { "he", "she", "it", "they", "him", "her", "them", "that", "his", "their", "himself", "herself", "themself", "themselves" };

                    if (!((Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.LeftControl)) && Keyboard.GetState().IsKeyDown(Keys.OemQuestion)))
                    {
                        // Define colors for different parts
                        Color infrastructureColor = new Color(255, 0, 255); // Pink
                        Color subjectColor = new Color(0, 255, 0); // Green
                        Color completePronounColor = new Color(0, 255, 255); // Cyan
                        Color incompletePronounColor = new Color(0, 0, 139); // Dark blue
                        Color defaultColor = new Color(75, 75, 75); // Gray

                        List<(string part, string type, bool isComplete)> ParseIntoParts(string command, string input)
                        {
                            var commandParts = command.Split(' ');
                            var inputParts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            var parsedParts = new List<(string part, string type, bool isComplete)>();

                            // Merge adjacent non-unique parts in the command
                            var mergedCommandParts = new List<string>();
                            for (int i = 0; i < commandParts.Length; i++)
                            {
                                if (commandParts[i] == "~" || commandParts[i] == "/p")
                                {
                                    mergedCommandParts.Add(commandParts[i]);
                                }
                                else
                                {
                                    string mergedPart = commandParts[i];
                                    while (i + 1 < commandParts.Length && commandParts[i + 1] != "~" && commandParts[i + 1] != "/p")
                                    {
                                        mergedPart += " " + commandParts[++i];
                                    }
                                    mergedCommandParts.Add(mergedPart);
                                }
                            }

                            // Tagging input parts based on merged command parts and pronouns
                            var taggedInputParts = new List<(string part, int groupId)>();
                            int infrastructureId = 0;
                            int subjectId = 1; // Start subjectId from 1 as 0 is used for infrastructure
                            bool lastWasStructure = false;

                            for (int i = 0; i < inputParts.Count(); i++)
                            {
                                bool matched = false;

                                // Check for a match with merged command parts
                                for (int J = i + 1; J <= inputParts.Count(); J++)
                                {
                                    var potentialMatch = string.Join(" ", inputParts.Skip(i).Take(J - i));
                                    if (mergedCommandParts.Contains(potentialMatch, StringComparer.OrdinalIgnoreCase))
                                    {
                                        for (int k = i; k < J; k++)
                                        {
                                            taggedInputParts.Add((inputParts[k], infrastructureId));
                                        }
                                        i = J - 1;
                                        matched = true;
                                        infrastructureId += 2; // Increment by 2 to leave space for the next subject or pronoun ID
                                        lastWasStructure = true;
                                        break;
                                    }
                                }

                                if (!matched)
                                {
                                    // Check for pronoun match
                                    bool isLastWord = i == inputParts.Count() - 1;
                                    bool isUnique = isLastWord
                                        ? pronouns.Any(p => inputParts[i].StartsWith(p, StringComparison.OrdinalIgnoreCase))
                                        : pronouns.Any(p => inputParts[i].Equals(p, StringComparison.OrdinalIgnoreCase)) || mergedCommandParts.Any(cp => cp.Equals(inputParts[i], StringComparison.OrdinalIgnoreCase));

                                    if (isUnique)
                                    {
                                        taggedInputParts.Add((inputParts[i], infrastructureId));
                                        infrastructureId += 2; // Increment by 2 to leave space for the next subject or pronoun ID
                                        lastWasStructure = true;
                                    }
                                    else
                                    {
                                        if (lastWasStructure)
                                        {
                                            subjectId += 2;
                                            lastWasStructure = false;
                                        }
                                        taggedInputParts.Add((inputParts[i], subjectId));
                                    }
                                }
                            }

                            // Merge adjacent non-unique parts in the input
                            var mergedInputParts = new List<string>();
                            for (int i = 0; i < taggedInputParts.Count(); i++)
                            {
                                string mergedPart = taggedInputParts[i].part;
                                while (i + 1 < taggedInputParts.Count() && taggedInputParts[i + 1].groupId == taggedInputParts[i].groupId)
                                {
                                    mergedPart += " " + taggedInputParts[++i].part;
                                }
                                mergedInputParts.Add(mergedPart);
                            }

                            // Parsing logic to align with GetMatchScore's merged logic
                            int j = 0;
                            for (int i = 0; i < mergedCommandParts.Count(); i++)
                            {
                                if (mergedCommandParts[i] == "~")
                                {
                                    if (j < mergedInputParts.Count())
                                    {
                                        parsedParts.Add((mergedInputParts[j], "subject", true));
                                        j++;
                                    }
                                    else
                                    {
                                        parsedParts.Add(("~", "default", false));
                                    }
                                }
                                else if (mergedCommandParts[i] == "/p")
                                {
                                    if (j < mergedInputParts.Count())
                                    {
                                        string currentInput = mergedInputParts[j].ToLower();
                                        if (pronouns.Contains(currentInput))
                                        {
                                            parsedParts.Add((mergedInputParts[j], "completePronoun", true));
                                        }
                                        else
                                        {
                                            // Check if the current input is a prefix of any pronoun
                                            bool isIncompletePronoun = pronouns.Any(p => p.StartsWith(currentInput));
                                            if (isIncompletePronoun)
                                            {
                                                parsedParts.Add((mergedInputParts[j], "incompletePronoun", false));
                                            }
                                            else
                                            {
                                                parsedParts.Add((mergedInputParts[j], "default", false));
                                            }
                                        }
                                        j++;
                                    }
                                    else
                                    {
                                        parsedParts.Add(("/p", "default", false));
                                    }
                                }
                                else
                                {
                                    bool isComplete = j < mergedInputParts.Count() && mergedCommandParts[i].Equals(mergedInputParts[j], StringComparison.OrdinalIgnoreCase);
                                    if (isComplete)
                                    {
                                        parsedParts.Add((mergedCommandParts[i], "infrastructure", true));
                                        j++;
                                    }
                                    else
                                    {
                                        parsedParts.Add((mergedCommandParts[i], "incompleteInfrastructure", false));
                                        j++; // Increment here to ensure the next part is not treated as a subject
                                    }
                                }
                            }

                            // Handle remaining input parts
                            while (j < mergedInputParts.Count())
                            {
                                parsedParts.Add((mergedInputParts[j], "default", false));
                                j++;
                            }

                            return parsedParts;
                        }


                        (int score, bool isMatch, bool isPartialMatch) GetMatchScore(string command, string input)
                        {
                            var commandParts = command.Split(' ');
                            var inputParts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            int score = 0;
                            int infrastructureScore = 0;
                            bool isPartial = false;

                            // Merge adjacent non-unique parts in the command
                            var mergedCommandParts = new List<string>();
                            for (int i = 0; i < commandParts.Length; i++)
                            {
                                if (commandParts[i] == "~" || commandParts[i] == "/p")
                                {
                                    mergedCommandParts.Add(commandParts[i]);
                                }
                                else
                                {
                                    string mergedPart = commandParts[i];
                                    while (i + 1 < commandParts.Length && commandParts[i + 1] != "~" && commandParts[i + 1] != "/p")
                                    {
                                        mergedPart += " " + commandParts[++i];
                                    }
                                    mergedCommandParts.Add(mergedPart);
                                }
                            }

                            // Tagging input parts based on merged command parts and pronouns
                            var taggedInputParts = new List<(string part, int groupId)>();
                            int infrastructureId = 0;
                            int subjectId = 1; // Start subjectId from 1 as 0 is used for infrastructure
                            bool lastWasStructure = false;

                            for (int i = 0; i < inputParts.Length; i++)
                            {
                                bool matched = false;

                                // Check for a match with merged command parts
                                for (int j = i + 1; j <= inputParts.Length; j++)
                                {
                                    var potentialMatch = string.Join(" ", inputParts.Skip(i).Take(j - i));
                                    if (mergedCommandParts.Contains(potentialMatch, StringComparer.OrdinalIgnoreCase))
                                    {
                                        for (int k = i; k < j; k++)
                                        {
                                            taggedInputParts.Add((inputParts[k], infrastructureId));
                                        }
                                        i = j - 1;
                                        matched = true;
                                        infrastructureId += 2; // Increment by 2 to leave space for the next subject or pronoun ID
                                        lastWasStructure = true;
                                        break;
                                    }
                                }

                                if (!matched)
                                {
                                    // Check for pronoun match
                                    bool isLastWord = i == inputParts.Length - 1;
                                    bool isUnique = isLastWord
                                        ? pronouns.Any(p => inputParts[i].StartsWith(p, StringComparison.OrdinalIgnoreCase))
                                        : pronouns.Any(p => inputParts[i].Equals(p, StringComparison.OrdinalIgnoreCase)) || mergedCommandParts.Any(cp => cp.Equals(inputParts[i], StringComparison.OrdinalIgnoreCase));

                                    if (isUnique)
                                    {
                                        taggedInputParts.Add((inputParts[i], infrastructureId));
                                        infrastructureId += 2; // Increment by 2 to leave space for the next subject or pronoun ID
                                        lastWasStructure = true;
                                    }
                                    else
                                    {
                                        if (lastWasStructure)
                                        {
                                            subjectId += 2;
                                            lastWasStructure = false;
                                        }
                                        taggedInputParts.Add((inputParts[i], subjectId));
                                    }
                                }
                            }

                            // Merge adjacent non-unique parts in the input
                            var mergedInputParts = new List<string>();
                            for (int i = 0; i < taggedInputParts.Count(); i++)
                            {
                                string mergedPart = taggedInputParts[i].part;
                                while (i + 1 < taggedInputParts.Count() && taggedInputParts[i + 1].groupId == taggedInputParts[i].groupId)
                                {
                                    mergedPart += " " + taggedInputParts[++i].part;
                                }
                                mergedInputParts.Add(mergedPart);
                            }

                            // Matching logic
                            for (int i = 0; i < mergedCommandParts.Count() && i < mergedInputParts.Count(); i++)
                            {
                                if (mergedCommandParts[i] == "~" || mergedCommandParts[i] == "/p")
                                {
                                    score++;
                                }
                                else if (mergedCommandParts[i].Equals(mergedInputParts[i], StringComparison.OrdinalIgnoreCase))
                                {
                                    int wordsInInfrastructure = mergedInputParts[i].Split(' ').Length;
                                    score++;
                                    infrastructureScore += wordsInInfrastructure;
                                }
                                else if (mergedCommandParts[i].StartsWith(mergedInputParts[i], StringComparison.OrdinalIgnoreCase))
                                {
                                    int wordsInInfrastructure = mergedInputParts[i].Split(' ').Length;
                                    score++;
                                    infrastructureScore += wordsInInfrastructure;
                                }
                                else
                                {
                                    return (score + infrastructureScore, false, isPartial);
                                }

                                if (i == mergedInputParts.Count() - 1 && !mergedCommandParts[i].Equals(mergedInputParts[i], StringComparison.OrdinalIgnoreCase))
                                {
                                    isPartial = true;
                                }
                            }

                            return (score + infrastructureScore, true, isPartial);
                        }



                        // Function to get color for each part
                        Color GetColor(string partType, bool isComplete)
                        {
                            return partType switch
                            {
                                "infrastructure" => isComplete ? infrastructureColor : defaultColor, // Pink or Gray
                                "subject" => subjectColor, // Green
                                "completePronoun" => completePronounColor, // Cyan
                                "incompletePronoun" => incompletePronounColor, // Dark blue
                                _ => defaultColor, // Gray
                            };
                        }

                        void DrawSuggestions(string prompt, List<string> suggestions)
                        {
                            string initialText = "Enter a command. Press F5 For Help: \"I ";
                            Vector2 sizeOfInitialText = Shibafont.MeasureString(initialText);
                            float StartX = 50 + sizeOfInitialText.X;
                            int yOffset = 20;

                            for (int i = 0; i < suggestions.Count(); i++)
                            {
                                string suggestion = suggestions[i];
                                var parts = ParseIntoParts(suggestion, prompt);
                                bool isDimmed = i >= 1;

                                foreach (var (part, type, isComplete) in parts)
                                {
                                    Color color = GetColor(type, isComplete);
                                    if (isDimmed)
                                    {
                                        color = new Color(color.R / 3, color.G / 3, color.B / 3, color.A);
                                    }
                                    _spriteBatch.DrawString(Shibafont, part, new Vector2(StartX, 1200 + (i + 1) * yOffset), color);
                                    StartX += Shibafont.MeasureString(part).X + 5;
                                }

                                StartX = 50 + sizeOfInitialText.X;
                            }
                        }


                        if(!InInventory && !Keyboard.GetState().IsKeyDown(Keys.Tab))
                        {
                            if (MostRecentPartyTurnArchitect.Prompt.Length > 0)
                            {
                                var commandsWithMatchData = RecognizedCommands
                                    .SelectMany(cmd => cmd.Value.Item1.Select(command => new
                                    {
                                        Command = command,
                                        MatchData = GetMatchScore(command, MostRecentPartyTurnArchitect.Prompt)
                                    }))
                                    ;

                                var matchedCommands = commandsWithMatchData
                                    .Where(x => x.MatchData.isMatch || x.MatchData.isPartialMatch)
                                    ;

                                var orderedCommands = matchedCommands
                                    .OrderByDescending(x => x.MatchData.score)
                                    .Take(4)
                                    ;

                                var matchingCommands = orderedCommands
                                    .Select(x => x.Command).ToList()
                                    ;

                                DrawSuggestions(MostRecentPartyTurnArchitect.Prompt, matchingCommands);
                            }

                            // Regular expression to find all numbers in the prompt
                            Regex numberRegex = new Regex(@"\d+");

                            string ModifiedPrompt = MostRecentPartyTurnArchitect.Prompt;

                            // Use a MatchEvaluator to replace each match with the corresponding entity's subject
                            string resultPrompt = numberRegex.Replace(ModifiedPrompt, match =>
                            {
                                // Try to parse the number safely
                                if (int.TryParse(match.Value, out int entityId))
                                {
                                    // Check if the entity exists and has referred names
                                    if (entityId > 10 && Game1.GameWorld.EntityLedger.TryGetValue(entityId, out Entity entity) && entity.ReferredToNames.Count() > 0)
                                    {
                                        return entity.ReferredToNames[0]; // Replace with the subject of the entity
                                    }
                                }
                                // If the number is too large or the entity doesn't meet the criteria, keep the original number
                                return match.Value;
                            });


                            _spriteBatch.DrawString(Shibafont, "Enter a command. Press F5 For Help: \"I " + resultPrompt + "_\"", new Vector2(50, 1200), Color.White);

                            // Display movement description
                            string MovementDescription = "";

                            if (MostRecentPartyTurnArchitect.CurrentlyMovingPlace == "none")
                            {
                                MovementDescription = "You are not moving right now.";
                            }
                            else if (KeyDirections.ContainsValue(MostRecentPartyTurnArchitect.CurrentlyMovingPlace))
                            {
                                MovementDescription = "You are currently headed to the " + MostRecentPartyTurnArchitect.CurrentlyMovingPlace + ".";
                            }
                            else
                            {
                                MovementDescription = "You are not moving right now.";
                            }

                            if (MostRecentPartyTurnArchitect.OnGround)
                            {
                                MovementDescription += " You are currently prone.";
                            }
                            else
                            {
                                MovementDescription += " You are currently standing.";
                            }

                            if (MassTravelOrderMode)
                            {
                                MovementDescription += " Move orders given to entire party.";
                            }

                            _spriteBatch.DrawString(Shibafont, MovementDescription, new Vector2(50, 1150), Color.White);
                        }
                    }

                    //cmd help
                    if (Keyboard.GetState().IsKeyDown(Keys.F5))
                    {
                        _spriteBatch.Draw(CmdHelpT, new Rectangle(0, 0, 2560, 1440), Color.White);
                    }

                    if(Keyboard.GetState().IsKeyDown(Keys.F2) && StoredPortrait != null)
                    {
                        DrawCharacter(StoredPortrait, 75, 600, 0.2);
                    }
                }
                else
                {
                    MostRecentPartyTurnArchitect = GameWorld.GamePlayerAssociation.ActiveParty.Architects[0];
                    foreach(Architect a in GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                    {
                        a.ReferredToNames.Remove("myself");
                        a.ReferredToNames.Remove("me");
                    }
                    MostRecentPartyTurnArchitect.AddReferredToName("myself");
                    MostRecentPartyTurnArchitect.AddReferredToName("me");
                }
            }
            else if (GameState == "dead")
            {
                DrawCenteredText(_spriteBatch, "All members of your party have perished. You have lost influence in the world.", 400, Shibafont, Color.White);
                DrawCenteredText(_spriteBatch, "The history in your save file has been updated.", 450, Shibafont, Color.White);
                DrawCenteredText(_spriteBatch, "Press SPACE to return to the title screen.", 500, Shibafont, Color.White);

                DrawAnnouncements();
            }
            else if (GameState == "travelmenu")
            {
                _spriteBatch.DrawString(BabyShibafont, "Press TAB to zoom out.", new Vector2(DrawX + 500, 10), Color.White);

                _spriteBatch.DrawString(BabyShibafont, "Press CTRL+S to save your game.", new Vector2(DrawX + 500, 30), Color.White);

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
                    _spriteBatch.DrawString(BabyShibafont, "X: " + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX.ToString(), new Vector2(DrawX + 500, 60), Color.White);
                    _spriteBatch.DrawString(BabyShibafont, "Z: " + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ.ToString(), new Vector2(DrawX + 560, 60), Color.White);

                    DrawWorld();
                }
                else
                {
                    DrawWorldSegment(GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX, GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ, 20, 20, 8.0f, 8);
                }

                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {
                        if (x == GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX && z == GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ)
                        {
                            if (GameWorld.WorldMap[x + z * GameWorld.Width].Location == null || GameWorld.WorldMap[x + z * GameWorld.Width].Location.Explored == false)
                            {
                                _spriteBatch.DrawString(BabyShibafont, "No Notable Location, " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome, new Vector2(DrawX + 500, DrawY - 30), Color.White);

                                if (!GameWorld.WorldMap[x + z * GameWorld.Width].DeepExplored)
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
                                if (GameWorld.WorldMap[x + z * GameWorld.Width].Location != null && GameWorld.WorldMap[x + z * GameWorld.Width].Location.Explored == true && ReqExploreLocations.Contains(GameWorld.WorldMap[x + z * GameWorld.Width].Location.Type))
                                {
                                    _spriteBatch.DrawString(BabyShibafont, "You discovered an ominous " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.Type + ".", new Vector2(DrawX + 500, DrawY - 60), Color.Lime);
                                }

                                if (MenacingStructures.Contains(GameWorld.WorldMap[x + z * GameWorld.Width].Location.PrimaryRace.Name + GameWorld.WorldMap[x + z * GameWorld.Width].Location.Type))
                                {
                                    _spriteBatch.DrawString(BabyShibafont, "This location emnates an ominous aura.", new Vector2(DrawX + 500, DrawY), Color.OrangeRed);
                                }

                                _spriteBatch.DrawString(BabyShibafont, GameWorld.WorldMap[x + z * GameWorld.Width].Location.Name + ", " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome + " " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.Type + ".", new Vector2(DrawX + 500, DrawY - 30), Color.White);
                                _spriteBatch.DrawString(BabyShibafont,
     "Population: " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.TruePopulation() +
     (GameWorld.WorldMap[x + z * GameWorld.Width].Location.PrimaryRace != null && GameWorld.WorldMap[x + z * GameWorld.Width].Location.PrimaryRace.Name != "" ?
     " (Primarily " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.PrimaryRace.Name + ")" : ""),
     new Vector2(DrawX + 500, DrawY + 30),
     Color.White);


                                int ArchitectPopulation = 0;
                                int structures = 0;
                                int groups = 0;

                                int DistrictLine = 0;

                                _spriteBatch.DrawString(BabyShibafont, "Districts", new Vector2(DrawX + 900, (DrawY - 30) + DistrictLine * 20), Color.White);
                                DistrictLine++;
                                _spriteBatch.DrawString(BabyShibafont, "(select with < and >)", new Vector2(DrawX + 900, (DrawY - 30) + DistrictLine * 20), Color.White);

                                DistrictLine += 2;

                                foreach (District d in GameWorld.WorldMap[x + z * GameWorld.Width].Location.Districts)
                                {
                                    // Check if MyLocation.Type is in SettlementTypes
                                    bool isSettlementType = GameWorld.SettlementTypes.Contains(GameWorld.WorldMap[x + z * GameWorld.Width].Location.Type);

                                    // REDRAW THE DISTRICT NAMES AND IF THEY'RE DONE DO THE SHIBA CARAT
                                    if (DistrictLine == GameWorld.GamePlayerAssociation.ActiveParty.MapCursorDistrict + 3)
                                    {
                                        if (isSettlementType)
                                        {
                                            _spriteBatch.DrawString(BabyShibafont, "[>]" + d.Name + " (" + d.Industry + ")", new Vector2(DrawX + 900, (DrawY - 30) + DistrictLine * 20), Color.White);
                                        }
                                        else
                                        {
                                            _spriteBatch.DrawString(BabyShibafont, "[>]" + d.Name, new Vector2(DrawX + 900, (DrawY - 30) + DistrictLine * 20), Color.White);
                                        }
                                    }
                                    else
                                    {
                                        if (isSettlementType)
                                        {
                                            if (d.Industry.Length > 4)
                                            {
                                                _spriteBatch.DrawString(BabyShibafont, "[ ]" + d.Name + " (" + d.Industry.Substring(0, 4) + ".)", new Vector2(DrawX + 900, (DrawY - 30) + DistrictLine * 20), Color.White);
                                            }
                                            else
                                            {
                                                _spriteBatch.DrawString(BabyShibafont, "[ ]" + d.Name + " (" + d.Industry + ")", new Vector2(DrawX + 900, (DrawY - 30) + DistrictLine * 20), Color.White);
                                            }
                                        }
                                        else
                                        {
                                            _spriteBatch.DrawString(BabyShibafont, "[ ]" + d.Name, new Vector2(DrawX + 900, (DrawY - 30) + DistrictLine * 20), Color.White);
                                        }
                                    }

                                    DistrictLine++;

                                    foreach (Group g in GameWorld.Groups)
                                    {
                                        if (g.Leader.Location == GameWorld.WorldMap[x + z * GameWorld.Width].Location)
                                        {
                                            groups++;
                                        }
                                    }

                                    ArchitectPopulation += d.Architects.Count();

                                    for (int DistrictX = 0; DistrictX < 7; DistrictX++)
                                    {
                                        for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                                        {
                                            foreach (Structure s in d.DistrictMap[DistrictX + DistrictZ * 7].Structures)
                                            {
                                                structures++;
                                            }
                                        }
                                    }
                                }



                                _spriteBatch.DrawString(BabyShibafont, "Total Architects: " + ArchitectPopulation, new Vector2(DrawX + 500, DrawY + 60), Color.White);

                                if (GameWorld.WorldMap[x + z * GameWorld.Width].Location.Government == null)
                                {
                                    _spriteBatch.DrawString(BabyShibafont, "No Notable Government", new Vector2(DrawX + 500, DrawY + 90), Color.White);
                                }
                                else
                                {
                                    _spriteBatch.DrawString(BabyShibafont, "Government: " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.Government.Name, new Vector2(DrawX + 500, DrawY + 90), Color.White);
                                }

                                _spriteBatch.DrawString(BabyShibafont, "Structures: " + structures, new Vector2(DrawX + 500, DrawY + 120), Color.White);
                                _spriteBatch.DrawString(BabyShibafont, "Districts: " + GameWorld.WorldMap[x + z * GameWorld.Width].Location.Districts.Count(), new Vector2(DrawX + 500, DrawY + 150), Color.White);


                                _spriteBatch.DrawString(BabyShibafont, "Press ENTER to travel to the selected district.", new Vector2(DrawX + 500, DrawY + 210), Color.White);
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
                if (GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].Biome == "forest" || GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].Biome == "lightforest" || GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].Biome == "taiga")
                {
                    _spriteBatch.DrawString(Shibafont, "[1] Harvest Wood", new Vector2(100, 100), Color.LimeGreen);
                }
                else
                {
                    _spriteBatch.DrawString(Shibafont, "[1] Harvest Wood (invalid biome)", new Vector2(100, 100), Color.Red);
                }

                if (GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].Biome == "mountain" || GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].Biome == "snowpeak")
                {
                    _spriteBatch.DrawString(Shibafont, "[2] Harvest Stone", new Vector2(100, 150), Color.LimeGreen);
                }
                else
                {
                    _spriteBatch.DrawString(Shibafont, "[2] Harvest Stone (invalid biome)", new Vector2(100, 150), Color.Red);
                }

                if (GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].Biome == "snowpeak")
                {
                    _spriteBatch.DrawString(Shibafont, "[3] Harvest Metal", new Vector2(100, 200), Color.LimeGreen);
                }
                else
                {
                    _spriteBatch.DrawString(Shibafont, "[3] Harvest Metal (invalid biome)", new Vector2(100, 200), Color.Red);
                }

                if (GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].Biome == "desert")
                {
                    _spriteBatch.DrawString(Shibafont, "[4] Harvest Sand", new Vector2(100, 250), Color.LimeGreen);
                }
                else
                {
                    _spriteBatch.DrawString(Shibafont, "[4] Harvest Sand (invalid biome)", new Vector2(100, 250), Color.Red);
                }

                if (GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].Biome == "plains")
                {
                    _spriteBatch.DrawString(Shibafont, "[5] Harvest Fiber", new Vector2(100, 300), Color.LimeGreen);
                }
                else
                {
                    _spriteBatch.DrawString(Shibafont, "[5] Harvest Fiber (invalid biome)", new Vector2(100, 300), Color.Red);
                }

                if (GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].Biome == "tundra" || GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].Biome == "taiga" || GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].Biome == "mountain" || GameWorld.WorldMap[GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX + GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ * GameWorld.Length].Biome == "snowpeak")
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
                EntityList<TextStorage> reversedExposition = new EntityList<TextStorage>(Exposition);
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
                int endIndex = Math.Min((sectionIndex + 1) * 30, Recipes.Count());

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

                        string Display = Recipes[i].Item1;

                        if(Display == "bolt")
                        {
                            Display = "bolt of cloth";
                        }
                        else if (Display == "sheet")
                        {
                            Display = "sheet of glass";
                        }
                        else if (Display == "bar")
                        {
                            Display = "bar of metal";
                        }

                        _spriteBatch.DrawString(Shibafont, prefix + Display, position, color);
                    }
                }
                else if (CraftingPhase == "selectingredients")
                {
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        string prefix = (i == RecipeIndex) ? "(X) " : "( ) ";
                        Color color = (i == RecipeIndex) ? Color.Yellow : Color.White;
                        Vector2 position = new Vector2(10, 30 * (i - startIndex) + 100);

                        string Display = Recipes[i].Item1;

                        if (Display == "bolt")
                        {
                            Display = "bolt of cloth";
                        }
                        else if (Display == "sheet")
                        {
                            Display = "sheet of glass";
                        }
                        else if (Display == "bar")
                        {
                            Display = "bar of metal";
                        }

                        _spriteBatch.DrawString(Shibafont, prefix + Display, position, color);
                    }

                    Dictionary<string, string> materialToObjectMap = new Dictionary<string, string>
        {
            {"cloth", "bolt"}, {"wood", "log"}, {"stone", "stone"}, {"ore", "ore"},
            {"metal", "bar"}, {"gemstone", "gemstone"}, {"sand", "pile"},
            {"fiber", "bunch"}, {"ice", "block"}, {"glass", "sheet"}
        };

                    var currentRecipeMaterials = Recipes[RecipeIndex].Item2
                        .Distinct()
                        .Select(mat => materialToObjectMap[mat])
                        ;

                    EntityList<Object> relevantItems = MostRecentPartyTurnArchitect.Inventory
                        .Where(obj => currentRecipeMaterials.Contains(obj.Type));

                    int inventorySectionIndex = InventoryCraftingIndex / 30;
                    int inventoryStartIndex = inventorySectionIndex * 30;
                    int inventoryEndIndex = Math.Min((inventorySectionIndex + 1) * 30, relevantItems.Count());

                    if (relevantItems.Count() == 0)
                    {
                        _spriteBatch.DrawString(Shibafont, "You do not have the required materials for this.", new Vector2(960, 100), Color.Red);
                        _spriteBatch.DrawString(Shibafont, "You can gather raw resources at empty regions.", new Vector2(960, 150), Color.Red);
                        _spriteBatch.DrawString(Shibafont, "You can craft bars, bolts, and sheets in this menu.", new Vector2(960, 200), Color.Red);
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

                    // Display required materials and the count of selected materials
                    var requiredItems = Recipes[RecipeIndex].Item2
                        .GroupBy(x => x)
                        .ToDictionary(g => g.Key, g => g.Count());

                    Vector2 requirementPosition = new Vector2(1600, 100);
                    bool hasAllIngredients = true;
                    foreach (var requiredItem in requiredItems)
                    {
                        string requiredType = requiredItem.Key;
                        int requiredCount = requiredItem.Value;

                        // Count the selected items of the required type
                        int selectedCount = IndexesForResources
                            .Select(index => relevantItems[index])
                            .Count(obj => obj.Type == materialToObjectMap[requiredType]);

                        if (selectedCount < requiredCount)
                        {
                            hasAllIngredients = false;
                        }

                        Color textColor = selectedCount >= requiredCount ? Color.Green : Color.Red;
                        string requirementText = $"{requiredType}: {selectedCount}/{requiredCount}";
                        _spriteBatch.DrawString(Shibafont, requirementText, requirementPosition, textColor);
                        requirementPosition.Y += 30;
                    }

                    // Display crafting readiness
                    string readinessText = hasAllIngredients ? "Ready to craft. Press Enter." : "Select More Materials.";
                    Color readinessColor = hasAllIngredients ? Color.Green : Color.Red;
                    _spriteBatch.DrawString(Shibafont, readinessText, new Vector2(1600, requirementPosition.Y + 30), readinessColor);
                }
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                _spriteBatch.Draw(QuitGuiT, new Rectangle(0, 0, 877, 130), Color.White);


                if (GameState == "partyturn")
                {
                    _spriteBatch.DrawString(Shibafont, "Quitting... (" + Math.Round((decimal)((100 - EscapeTicks) / 10)) + ")", new Vector2(10, 10), Color.White);
                    _spriteBatch.DrawString(Shibafont, "Travel outside the district and press CTRL+S to save.", new Vector2(10, 70), Color.White);
                }
                else if (GameState == "travelmenu")
                {
                    _spriteBatch.DrawString(Shibafont, "Quitting... (" + Math.Round((decimal)((100 - EscapeTicks) / 10)) + ")", new Vector2(10, 40), Color.White);
                    _spriteBatch.DrawString(Shibafont, "Press/Hold CTRL-S to save your game.", new Vector2(10, 70), Color.White);
                }
                else
                {
                    _spriteBatch.DrawString(Shibafont, "Quitting... (" + Math.Round((decimal)((100 - EscapeTicks) / 10)) + ")", new Vector2(10, 40), Color.White);
                }
                
            }

            if(FrameCounter != null)
            {
                if (FrameCounter.RenderFps)
                {
                    FrameCounter.Render(_spriteBatch, Shibafont);
                }
            }

            if (SplitMode)
            {
                int topCutOff = 0; // Cut off a small part of the top

                // Adjust the hitboxes
                for (int i = 0; i < EntityHitboxes.Count(); i++)
                {
                    var (rect, entity) = EntityHitboxes[i];
                    rect.Y += topCutOff;
                    rect.Height -= topCutOff;
                    EntityHitboxes[i] = (rect, entity);
                }

                // Check for intersections and adjust
                for (int i = 0; i < EntityHitboxes.Count(); i++)
                {
                    for (int j = i + 1; j < EntityHitboxes.Count(); j++)
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

                if (GameState == "partyturn" || GameState == "otherturn")
                {
                    //handle pull up menus and ThisList

                    int textureWidth = 420;
                    int textureHeight = 560;
                    int visibleHeight = 90;
                    int offsetY = (int)1440 - visibleHeight;

                    // Determine positions (on the right side of the screen)
                    Vector2 skillPosition = new Vector2(2560 - 10 - textureWidth * 3 - 20, offsetY);
                    Vector2 spellPosition = new Vector2(2560 - 10 - textureWidth * 2 - 10, offsetY);
                    Vector2 bodyPartPosition = new Vector2(2560 - 10 - textureWidth, offsetY);

                    // Handle mouse hover and show menus
                    MouseState mouseState = Mouse.GetState();
                    Vector2 mousePosition = new Vector2(mouseState.X / scaleX, mouseState.Y / scaleY);

                    // Check for hover over small textures
                    Rectangle skillRect = new Rectangle(skillPosition.ToPoint(), new Point(textureWidth, visibleHeight));
                    Rectangle spellRect = new Rectangle(spellPosition.ToPoint(), new Point(textureWidth, visibleHeight));
                    Rectangle bodyPartRect = new Rectangle(bodyPartPosition.ToPoint(), new Point(textureWidth, visibleHeight));

                    // Adjust positions if showing
                    if (IsShowingSkills)
                    {
                        skillPosition.Y = 1440 - textureHeight;
                        skillRect = new Rectangle(skillPosition.ToPoint(), new Point(textureWidth, textureHeight));
                    }
                    if (IsShowingSpells)
                    {
                        spellPosition.Y = 1440 - textureHeight;
                        spellRect = new Rectangle(spellPosition.ToPoint(), new Point(textureWidth, textureHeight));
                    }
                    if (IsShowingBodyParts)
                    {
                        bodyPartPosition.Y = 1440 - textureHeight;
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
                        EntityList<Entity> Spells = MostRecentPartyTurnArchitect.SpellsKnown;

                        if(MostRecentPartyTurnArchitect.MainHeldObject != null && MostRecentPartyTurnArchitect.MainHeldObject.SpecialKnowledge != null && GameWorld.AllLegendarySpells.Contains(MostRecentPartyTurnArchitect.MainHeldObject.SpecialKnowledge))
                        {
                            Spells.Add(MostRecentPartyTurnArchitect.MainHeldObject.SpecialKnowledge);
                        }
                        if (MostRecentPartyTurnArchitect.OffHeldObject != null && MostRecentPartyTurnArchitect.OffHeldObject.SpecialKnowledge != null && GameWorld.AllLegendarySpells.Contains(MostRecentPartyTurnArchitect.OffHeldObject.SpecialKnowledge))
                        {
                            Spells.Add(MostRecentPartyTurnArchitect.MainHeldObject.SpecialKnowledge);
                        }

                        DrawTextInMenu(spellPosition, Spells, "Spells");
                    }
                    if (IsShowingBodyParts)
                    {
                        EntityList<Entity> bodyParts = GetUniqueBodyParts(MostRecentPartyTurnArchitect.Room != null ? MostRecentPartyTurnArchitect.Room.Architects : MostRecentPartyTurnArchitect.Block.Architects);
                        DrawTextInMenu(bodyPartPosition, bodyParts, "Body Parts");
                    }

                    void DrawTextInMenu(Vector2 position, EntityList<Entity> items, string itemType)
                    {
                        float startY = position.Y + 100;
                        float offsetX = position.X + 50;
                        float centerX = position.X + textureWidth / 2;

                        if (items.Count() == 0)
                        {
                            _spriteBatch.DrawString(BabyShibafont, $"No Relevant {itemType}.", new Vector2(offsetX, startY), Color.White);
                            return;
                        }

                        int line = 0;
                        foreach (var item in items)
                        {
                            string text = item.ReferredToNames[0];
                            float textY = startY + line * BabyShibafont.LineSpacing;

                            // Draw the text aligned to the left side of the hitbox
                            _spriteBatch.DrawString(BabyShibafont, text, new Vector2(offsetX, textY), Color.White);

                            // Create a Rectangle for the hitbox
                            Vector2 textSize = BabyShibafont.MeasureString(text);
                            Rectangle hitbox = new Rectangle((int)offsetX, (int)textY, (int)textSize.X, (int)textSize.Y);
                            EntityHitboxes.Add((hitbox, item));

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

                    EntityList<Entity> GetUniqueBodyParts(IEnumerable<Architect> architects)
                    {
                        var bodyPartTypes = new HashSet<string>();
                        foreach (var architect in architects)
                        {
                            foreach (var bodyPart in architect.BodyParts)
                            {
                                if (!bodyPartTypes.Contains(bodyPart.Type))
                                {
                                    bodyPartTypes.Add(bodyPart.Type);
                                }
                            }
                        }

                        EntityList<Entity> uniqueBodyParts = Game1.GameWorld.AllBodyParts
                            .Where(bodyPart => bodyPartTypes.Contains(bodyPart.Metadata));

                        return uniqueBodyParts;
                    }

                    if (ThisList.Count() > 0)
                    {
                        int mouseX = Mouse.GetState().X;

                        // Determine the position to draw ThisListT
                        Vector2 texturePosition;
                        if (mouseX < 2560 / 2)
                        {
                            // Mouse is on the left side of the screen, draw ThisListT on the right
                            texturePosition = new Vector2(2560 - 438 - 10, 10); // With 10 pixels of leeway
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
                            if (entity.ReferredToNames != null && entity.ReferredToNames.Count() > 0)
                            {
                                string text = entity.ReferredToNames[0];
                                DrawCenteredTextAtPosition(_spriteBatch, text, textStartX, textStartY, BabyShibafont, Color.White);
                                textStartY += BabyShibafont.LineSpacing; // Move down for the next text
                            }
                        }
                    }
                }
            }


            /*
            _spriteBatch.Draw(whiteRect, RectangleE, Color.White);
            _spriteBatch.Draw(whiteRect, RectangleSE, Color.White);
            _spriteBatch.Draw(whiteRect, RectangleSW, Color.White);
            _spriteBatch.Draw(whiteRect, RectangleS, Color.White);
            _spriteBatch.Draw(whiteRect, RectangleN, Color.White);
            _spriteBatch.Draw(whiteRect, RectangleW, Color.White);
            _spriteBatch.Draw(whiteRect, RectangleNE, Color.White);
            _spriteBatch.Draw(whiteRect, RectangleNW, Color.White);
            */


            _spriteBatch.End();

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            _spriteBatch.Draw(BetterCursorT, new Rectangle(Mouse.GetState().X - 8, Mouse.GetState().Y - 8, 16, 16), Color.White);
            _spriteBatch.End();


            base.Draw(gameTime);
        }
    }
    
    public class EntityConverter : JsonConverter<Entity>
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            WriteIndented = true
        };

        public override Entity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jsonDocument = JsonDocument.ParseValue(ref reader);
            var rootElement = jsonDocument.RootElement;

            if (rootElement.TryGetProperty("EntityType", out JsonElement typeElement))
            {
                var type = typeElement.GetString();
                Type entityType = Type.GetType($"Lightrealm.{type}");

                if (entityType != null)
                {
                    return (Entity)JsonSerializer.Deserialize(rootElement.GetRawText(), entityType, SerializerOptions);
                }
            }

            throw new JsonException("Unable to determine the EntityType for deserialization.");
        }

        public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // Write the $id property
            if (options.ReferenceHandler == ReferenceHandler.Preserve)
            {
                var referenceResolver = options.ReferenceHandler.CreateResolver();
                bool firstReference;
                var referenceId = referenceResolver.GetReference(value, out firstReference);
                writer.WriteString("$id", referenceId);
            }

            writer.WriteString("EntityType", value.EntityType);

            foreach (var property in value.GetType().GetProperties())
            {
                if (property.CanRead)
                {
                    var propertyValue = property.GetValue(value);
                    var propertyName = property.Name;

                    writer.WritePropertyName(propertyName);
                    JsonSerializer.Serialize(writer, propertyValue, propertyValue?.GetType() ?? typeof(object), SerializerOptions);
                }
            }

            writer.WriteEndObject();
        }
    }
}