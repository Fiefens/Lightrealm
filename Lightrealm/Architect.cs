using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Color = Microsoft.Xna.Framework.Color;

namespace Lightrealm
{
    [Serializable]

    public class Architect : Entity
    {
        public static T Entity<T>(int entityId) where T : Entity
        {
            if (Game1.GameWorld == null || Game1.GameWorld.AllEntities == null)
            {
                return (T)Convert.ChangeType(Game1.TemporaryEntities[entityId], typeof(T));
            }

            return (T)Convert.ChangeType(Game1.GameWorld.AllEntities[entityId], typeof(T));
        }

        static List<string> LegendTypes = new List<string>() { "hunter", "adventurer", "assassin", "rogue", "artisan", "diplomat", "enchanter" };

        public string Sex { get; set; }
        public string Pronoun { get; set; }
        public string PossessivePronoun { get; set; }
        public string ObjectivePronoun { get; set; }

        public string FalsifiedName = "";

        public int PulseCharge = 0;

        public int HairID = Game1.r.Next(0, 10) switch
        {
            < 4 => 0,   // 40% chance for HairID to be 0
            < 7 => 1,   // 30% chance for HairID to be 1
            < 9 => 3,   // 20% chance for HairID to be 2
            _ => 2      // 10% chance for HairID to be 3
        };


        public List<string> RevitalizedDates = new List<string>();
        public List<string> SplitDates = new List<string>();

        public string NextMoveOrder = "";

        public string TryDropItemType = "";
        public string TryPickUpItemType = "";
        public List<Material> TryDropMaterials = new List<Material>();
        public List<Material> TryPickUpMaterials = new List<Material>();

        private Dictionary<string, List<Entity>> messageDatabase = new Dictionary<string, List<Entity>>();
        private Dictionary<string, string> responseDatabase = new Dictionary<string, string>();

        public Entity RealityBlipFocus;
        public int RealityFocusTries;

        public List<Message> MessagesNotRespondedTo = new List<Message>();

        public List<(string, Architect)> Intrigue = new List<(string, Architect)>();

        public bool RuptureMode = false;
        public bool PickUpMode = false;

        private string _deathCause = "";

        public string DeathCause
        {
            get { return _deathCause; }
            set
            {
                if (string.IsNullOrEmpty(_deathCause))
                {
                    _deathCause = value;
                }
            }
        }

        public bool BroadcastedDeathMessage = false;

        public bool Crafting = false;

        public bool TryingToTravel = false;

        public int SpellcastingPower = 1;

        public bool DoubleStrikeReady = false;
        public bool QuickStrikeReady = false;
        public bool SeveringStrikeReady = false;
        public bool FinaleReady = false;
        public bool BodySlamReady = false;
        public bool LegSweepReady = false;
        public bool DropKickReady = false;
        public int CyclesSinceJump = 0;
        public int ReactionBoostCycles = 0;

        public int ExtraFocusTicks = 0;

        public int HalfFocusTicks = 0;

        public (Location, District, Block, Structure, Room) SavePoint = (null, null, null, null, null);
        public int SavePointTicks = 0;

        public List<Entity> AlignedDomains = new List<Entity>();

        public List<Composition> CultureBank = new List<Composition>();
        public List<Location> ExploredLocations = new List<Location>();

        public List<Architect> ArchitectsWhoSurrenderedToMe = new List<Architect>();
        public List<Architect> ArchitectsWhoISurrenderedTo = new List<Architect>();
        public List<Architect> ArchitectsWhoIAttemptedToSurrenderTo = new List<Architect>();
        public List<Architect> ArchitectsIWillTellTruthTo = new List<Architect>();

        public bool Bound;

        public double AdventureCooldown = 0;
        public double DiplomacyCooldown = 0;

        Dictionary<string, int> BackupProfessionToLevel = new Dictionary<string, int>
{
    // Level 1
    {"baker", 1},
    {"blacksmith", 1},
    {"brewer", 1},
    {"butcher", 1},
    {"carpenter", 1},
    {"child", 1},
    {"craftsman", 1},
    {"elder", 1},
    {"fisherman", 1},
    {"leader", 1},
    {"mason", 1},
    {"merchant", 1},
    {"miller", 1},
    {"miner", 1},
    {"musician", 1},
    {"indolent", 1},
    {"peasant", 1},
    {"political figure", 1},
    {"potter", 1},
    {"prestiged", 1},
    {"prophet", 1},
    {"scribe", 1},
    {"tailor", 1},
    {"tanner", 1},
    {"trader", 1},
    {"weaver", 1},

    // Level 2
    {"animal", 2},
    {"beast", 2},
    {"embezzler", 2},
    {"hunter", 2},
    {"knight", 2},
    {"magician", 2},
    {"mercenary", 2},
    {"scout", 2},
    {"soldier", 2},
    {"thief", 2},

    // Level 4
    {"artificer", 4},
    {"bard", 4},
    {"duelist", 4},
    {"luminary", 4},
    {"mage", 4},

    // Level 6
    {"alpha", 6},
    {"anarchist", 6},
    {"archmage", 6},
    {"beastmaster", 6},
    {"commander", 6},
    {"diplomancer", 6},
    {"largebeast", 6},
    {"outlaw", 6},
    {"spy", 6},

    // Level 8
    {"archartificer", 8},
    {"archbard", 8},
    {"archduelist", 8},
    {"archluminary", 8},
    {"conjumancer", 8},
    {"elemental", 8},
    {"fractalmancer", 8},
    {"hypernexus", 8},
    {"icosidodecahedron", 8},
    {"necromancer", 8},
    {"perceptomancer", 8},
    {"shadeheart", 8},
    {"sorcerer", 8},
    {"spatiomancer", 8},
    {"warlock", 8}
};

        public Location InteractionLocation = null;

        public string Prompt = "";
        public List<string> PreviousPrompts = new List<string>() { "" }; // Start with an empty slate
        public int PromptIndex = 0;
        public string SavedPrompt = "";

        public bool RecievedBodyPhysicalStatIncrease = false;
        public bool RecievedBodyPhysicalStatIncreaseTwo = false;


        public Dictionary<Architect, int> Distances = new Dictionary<Architect, int>();

        public Entity LegendaryTarget = null;
        public Structure LegendaryTargetStructure = null;
        public int HuntingProgress = 0;

        public List<Architect> ShieldTokens = new List<Architect>();

        public int DivineProtection = 0;
        public int DivineMight = 0;

        public bool Diplomakitted = false;

        public List<(Entity, string)> Grievances = new List<(Entity, string)>();

        public int CooldownCycles = 0;

        public bool IsCalamity = false;
        public bool BuiltSpire = false;

        public void DistanceFromArchitect(Architect otherArchitect, int distanceModifier)
        {
            // Update or initialize distance for this architect to the other
            if (Distances.TryGetValue(otherArchitect, out int currentDistance))
            {
                Distances[otherArchitect] = Math.Clamp(currentDistance + distanceModifier, 0, 5);
            }
            else
            {
                Distances[otherArchitect] = Math.Clamp(distanceModifier, 0, 5);
            }

            // Ensure the other architect also updates its distance list symmetrically
            if (otherArchitect.Distances.TryGetValue(this, out int otherCurrentDistance))
            {
                otherArchitect.Distances[this] = Math.Clamp(otherCurrentDistance + distanceModifier, 0, 5);
            }
            else
            {
                otherArchitect.Distances[this] = Math.Clamp(distanceModifier, 0, 5);
            }
        }


        public int GetDistance(object entity)
        {
            // Check if the entity is an Architect
            if (entity is Architect otherArchitect)
            {
                // Attempt to get the distance from the dictionary
                if (Distances.TryGetValue(otherArchitect, out int distance))
                {
                    // Ensure the other architect has the same distance recorded
                    if (!otherArchitect.Distances.TryGetValue(this, out int otherDistance))
                    {
                        otherArchitect.Distances[this] = distance;
                    }
                    else
                    {
                        // Ensure both distances match, use the lower one if they differ
                        if (otherDistance != distance)
                        {
                            distance = Math.Min(distance, otherDistance);
                            Distances[otherArchitect] = distance;
                            otherArchitect.Distances[this] = distance;
                        }
                    }
                    return distance;
                }
                else
                {
                    // Check if the other architect has a distance to this architect
                    if (otherArchitect.Distances.TryGetValue(this, out int otherDistance))
                    {
                        Distances[otherArchitect] = otherDistance;
                        return otherDistance;
                    }
                    else
                    {
                        // Generate a new random distance if neither has a distance
                        int randomDistance = Game1.r.Next(2, 6);
                        Distances[otherArchitect] = randomDistance;
                        otherArchitect.Distances[this] = randomDistance;
                        return randomDistance;
                    }
                }
            }

            // If the entity is not an Architect, return 0 to automatically succeed
            return 0;
        }

        public int NaturalArmor = 0; //number from 1-100, dependent on race, chance of blocking an attack simply with natural ability

        public int Level = 0;
        public int SpendableLevels;

        public List<Object> Sparks = new List<Object>();

        public Architect UndeadCreator = null;

        public bool Invisible = false;

        public int CombatCycles = 0;

        public int PathOfShadowLevel = 0;
        public int PathOfLifeLevel = 0;
        public int PathOfDeathLevel = 0;
        public int PathOfTimeLevel = 0;
        public int PathOfStarsLevel = 0;
        public int PathOfHeatLevel = 0;
        public int PathOfIllusionsLevel = 0;
        public int PathOfEtherealityLevel = 0;
        public int PathOfVoidLevel = 0;
        public int PathOfStormsLevel = 0;
        public int PathOfForgeLevel = 0;
        public int PathOfLoreLevel = 0;
        public int PathOfMindLevel = 0;
        public int PathOfSoulLevel = 0;
        public int PathOfBodyLevel = 0;
        public int PathOfSpaceLevel = 0;
        public int PathOfRealityLevel = 0;
        public int PathOfLightLevel = 0;

        public Architect Master = null;
        public string MasterRelation = "";

        public Architect Spouse = null;

        public bool Augment = false;

        public List<Object> ShadowStorage = new List<Object>();

        public int Wealth = 0;

        public double CalamityAge = 0;
        public int CalamitySpawnTime = Game1.r.Next(25, 36);

        public bool TriggeredLock = false;
        public int CyclesSinceMoved = 0;


        public Block BlockLastCycle = null;
        public Room RoomLastCycle = null;

        public int BarrierStacks = 0;


        public (Region, Location, District, Block, Structure, Room) RematerializeLocation = (null, null, null, null, null, null);

        public Race Race { get; set; }

        private string _profession;
        public string Profession
        {
            get => _profession;
            set
            {
                if (!LegendTypes.Contains(_profession))
                {
                    _profession = value;
                }
            }
        }

        public double BirthdayCycle { get; set; } = 0;

        public int Age
        {
            get
            {
                double ageInCycles = (double)((Game1.GameWorld != null ? Game1.GameWorld.Cycle : 0) - BirthdayCycle);
                return (int)Math.Round(ageInCycles / 290304000);
            }
        }

        public List<Architect> MeldedShibas = new List<Architect>();

        public bool IsAlive { get; set; } = true;

        public int MoralCompass = 0;
        public int StabilityCompass = 0;
        public int PropertyValue = Math.Max(0, Game1.r.Next(-4, 6));
        public int FamilyValue = Math.Max(0, Game1.r.Next(-4, 6));
        public int PowerValue = Math.Max(0, Game1.r.Next(-4, 6));
        public int MoneyValue = Math.Max(0, Game1.r.Next(-4, 6));
        public int KnowledgeValue = Math.Max(0, Game1.r.Next(-4, 6));
        public int SpiritualityValue = Math.Max(0, Game1.r.Next(-4, 6));
        public int ProwessValue = Math.Max(0, Game1.r.Next(-4, 6));
        public int PatriotismValue = Math.Max(0, Game1.r.Next(-4, 6));
        public int CourageValue = Math.Max(0, Game1.r.Next(-4, 6));
        public int CreativityValue = Math.Max(0, Game1.r.Next(-4, 6));

        public Location NextMigrationLocation { get; set; }

        public List<Imbuement> CurrentlyActiveImbuements = new List<Imbuement>();

        public bool RecievedImmortalityBuff { get; set; }

        public int DistrictPoints = 0;

        public Structure StudyBuilding { get; set; }

        public bool HasMadeALegendaryArtifact = false;

        public bool HadChildren = false;

        public string FavoriteColor { get; set; }
        public Material FavoriteGemstone { get; set; }
        public Material FavoriteStone { get; set; }
        public Material FavoriteWood { get; set; }
        public Material FavoriteMetal { get; set; }
        public Material FavoriteCloth { get; set; }
        public Object FavoriteBook { get; set; }


        public Group Group { get; set; } = null;
        public int GroupLoyalty { get; set; } = -1;
        public int TerminalAge { get; set; } = 0;
        public bool DoIDieOfOldAge { get; set; } = true;

        public bool IsLoadedTrader = false;

        public int Reputation = 0;

        public int PurifiedBurnedCities = 0;
        public Blight BlightManipulated = null;
        public int KilledPeopleWithBlight = 0;
        public List<Location> TakenLocations = new List<Location>();
        public int KilledWomen = 0;
        public int KilledMen = 0;
        public int KilledChildren = 0;
        public int KidnappedWomen = 0;
        public int KidnappedMen = 0;
        public int KidnappedChildren = 0;

        public bool MaxEnergyInspiration = false;

        public List<Architect> KilledPeopleWhoActuallyMatter = new List<Architect>();
        public List<Architect> KidnappedPeopleWhoActuallyMatter = new List<Architect>();

        public int CorruptedCities = 0;
        public int Decieved = 0;
        public string PowerType = "";

        public int Strength;
        public int Dexterity;
        public int Agility;
        public int Endurance;
        public int Creativity;
        public int Charisma;

        private int _focus;

        public int Focus
        {
            get
            {
                int baseFocus;

                if (HalfFocusTicks > 0)
                {
                    baseFocus = (int)Math.Round(((double)(_focus / 2)), 0, MidpointRounding.ToNegativeInfinity);
                }
                else
                {
                    baseFocus = _focus;
                }

                if (ExtraFocusTicks > 0)
                {
                    baseFocus += 1;
                }

                return baseFocus;
            }
            set
            {
                _focus = value;
            }
        }


        private Room _room;
        private Block _block;

        public Location Location { get; set; }
        public District District { get; set; }

        public Structure Structure
        {
            get
            {
                return _room?.Structure;
            }
            set
            {
                // Ignore the set operation for Structure
            }
        }
        public Room Room
        {
            get
            {
                return _room;
            }
            set
            {
                _room = value;
            }
        }
        public Block Block
        {
            get
            {
                if (_block != null)
                {
                    return _block;
                }
                if (_room != null)
                {
                    return _room.Structure.Block;
                }
                return null;
            }
            set
            {
                _block = value;
            }
        }

        public string Size { get; set; } = "average";

        public Location HomeLocation { get; set; }
        public District HomeDistrict { get; set; }
        public Structure HomeStructure { get; set; }

        public string Destiny { get; set; } = "none";
        public int DestinyArrivalYear { get; set; } = 999;

        public string CurrentlyMovingPlace = "none";

        public int MessageCooldown = 0;

        public List<(Architect, int)> ArchitectOpinions { get; set; } = new List<(Architect, int)>();
        public List<Architect> KnownArchitects = new List<Architect>();

        public bool IsStudying { get; set; }
        public bool Loaded { get; set; }

        public List<Object> Clothing = new List<Object>();

        public string ScholarType { get; set; } = "";
        public string FavoriteScienceField { get; set; } = "";
        public string FavoriteCultureField { get; set; } = "";
        public string FavoriteMagicField { get; set; } = "";

        public int MagicStudyPoints { get; set; } = 0;
        public int CultureStudyPoints { get; set; } = 0;
        public int ScienceStudyPoints { get; set; } = 0;

        public List<Entity> SpellsKnown { get; set; } = new List<Entity>();
        public List<Entity> SkillsKnown { get; set; } = new List<Entity>();
        public List<Entity> UsedSkills { get; set; } = new List<Entity>();

        public bool DiscoveredASpell { get; set; } = false;

        public int FireSeconds { get; set; } = 0;
        public int WetCycles { get; set; } = 0;
        public int BlindCycles { get; set; } = 0;
        public int DestabilizedCycles { get; set; } = 0;
        public int UnconsciousCycles { get; set; } = 0;
        public int RadiantCycles { get; set; } = 0;
        public int CloakCycles { get; set; } = 0;
        public int FractalCycles { get; set; } = 0;
        public int HoldCycles { get; set; } = 0;
        public int DismissalCycles { get; set; } = 0;


        public bool OnGround { get; set; } = false;

        public bool IsImmortal { get; set; } = false;
        public bool IsCoveredInPlants { get; set; } = false;

        public int YLevelInFeet { get; set; } = 0;
        public int YVelocity { get; set; } = 0;

        public bool Focused { get; set; } = false;

        public int DaysSinceFood { get; set; } = 0;
        public int DaysSinceLiquid { get; set; } = 0;
        public int NightsSinceSleep { get; set; } = 0;
        public int DaysSinceCoffeeOrTea { get; set; } = 0;
        public int DaysSinceSocialized { get; set; } = 0;
        public int DaysSincePerforming { get; set; } = 0;
        public int DaysSincePlayingGame { get; set; } = 0;

        public Architect TargetArchitect { get; set; }
        public Object TargetObject { get; set; }
        public (Region, Location, District, Block, Room, string) Target { get; set; } = (null, null, null, null, null, "");

        public string Task { get; set; } = "";
        public int CyclesLeftInTask { get; set; } = 0;

        public decimal Energy;
        public int MaxEnergyMod = 0;

        public decimal Bleeding = 0;
        public double Pain = 0;

        public List<Object> BodyParts { get; set; } = new List<Object>();
        public Object MainInteractionAppendage;
        public Object OffInteractionAppendage;
        public List<Object> Inventory { get; set; }
        public Object OffHeldObject { get; set; }
        public Object MainHeldObject { get; set; }

        public bool PrefersCoffeeIfTrue { get; set; } = false;

        public bool IsColossal { get; set; } = false;

        public int ColossalMinefieldX { get; set; }
        public int ColossalMinefieldZ { get; set; }

        public int ExtraShieldEffectiveness = 0;
        public int ExtraAttackPower = 0;
        public int ExtraDodgeChance = 0;
        public int ExtraRedirectionChance = 0;
        public int ExtraBashingResistance = 0;
        public int ExtraSlashingResistance = 0;
        public int ExtraPiercingResistance = 0;
        public int ExtraScourgingResistance = 0;
        public int ExtraStealth = 0;
        public int ExtraEnergyRegen = 0;

        public bool IsofractalThief = false;


        public int ArmorProficiency { get; set; } = 0;

        public List<string> OppositionTags { get; set; } = new List<string>() { };
        public List<Architect> SuperTrustedArchitects { get; set; } = new List<Architect>();

        public List<(string, int)> XPValues { get; set; } = new List<(string, int)>
{
    ("slashing", 0),
    ("piercing", 0),
    ("bashing", 0),
    ("scourging", 0),
    ("dodging", 0),
    ("blocking", 0),
    ("disarming", 0),
    ("redirection", 0),
    ("throwing", 0),
    ("parrying", 0)
};



        public void ChangeXP(string proficiencyName, int xpChange)
        {
            var proficiencyIndex = XPValues.FindIndex(p => p.Item1.Equals(proficiencyName, StringComparison.OrdinalIgnoreCase));
            if (proficiencyIndex != -1)
            {
                // If found, update the XP
                XPValues[proficiencyIndex] = (XPValues[proficiencyIndex].Item1, XPValues[proficiencyIndex].Item2 + xpChange);
            }
        }
        public int GetXP(string proficiencyName)
        {
            // Find the proficiency in the list
            var proficiency = XPValues.FirstOrDefault(p => p.Item1.Equals(proficiencyName, StringComparison.OrdinalIgnoreCase));
            if (proficiency.Equals(default((string, int))))
            {
                return -1; // Return -1 if not found
            }
            else
            {
                // Return the level based on the XP
                return proficiency.Item2;
            }
        }
        public int GetProficiency(string proficiencyName)
        {
            // Find the proficiency in the list
            var proficiency = XPValues.FirstOrDefault(p => p.Item1.Equals(proficiencyName, StringComparison.OrdinalIgnoreCase));
            if (proficiency.Equals(default((string, int))))
            {
                return -1; // Return -1 if not found
            }
            else
            {
                // Return the level based on the XP
                return CalculateLevel(proficiency.Item2);
            }
        }
        private int CalculateLevel(int xp)
        {
            int level = 0;
            int currentThreshold = 20; // Start with the new threshold
            while (xp >= currentThreshold)
            {
                level++;

                // Determine the multiplier based on the cycle for this level
                double multiplier;
                if ((level) % 3 == 0) multiplier = 2.5; // Every 3rd level starting from level 0
                else multiplier = 2.0; // For the other levels

                currentThreshold = (int)(currentThreshold * multiplier);
            }
            return level;
        }
        // Function to add cultural clothing items
        public void AddCulturalClothing(string culturalItems, Material material)
        {
            // Check if the clothing item is "straps" and the sex is female
            bool addUpperShirt = (culturalItems.Trim().ToLower() == "straps" || culturalItems.Trim().ToLower() == "straps/cape") && this.Sex.ToLower() == "female" && (this.HomeLocation == null || this.HomeLocation.HomeCivilization.Type != "druid");

            if (culturalItems != "none")
            {
                // Split the cultural items string into individual items
                string[] items = culturalItems.Split('/');

                // Add each item to the Clothing list
                foreach (string item in items)
                {
                    Object newClothing = new Object(null, item.Trim(), new List<Material>() { material }, null);
                    Clothing.Add(newClothing);

                    // Apply dye to the newly added clothing item
                    ApplyDye(newClothing);

                    // Check for pairs and color them the same
                    if (item.Trim().ToLower().StartsWith("left ") || item.Trim().ToLower().StartsWith("right "))
                    {
                        string pairItem = item.Trim().ToLower().StartsWith("left ") ? "right " + item.Trim().Substring(5) : "left " + item.Trim().Substring(6);
                        Object pairClothing = Clothing.FirstOrDefault(c => c.Type.ToLower() == pairItem.ToLower());

                        if (pairClothing != null)
                        {
                            pairClothing.DyedColor = newClothing.DyedColor;
                        }
                    }
                }

                // If addUpperShirt is true, add "uppershirt" to the Clothing list
                if (addUpperShirt)
                {
                    Object upperShirt = new Object(null, "uppershirt", new List<Material>() { material }, null);
                    Clothing.Add(upperShirt);

                    // Apply dye to the upper shirt
                    ApplyDye(upperShirt);
                }
            }

            // Clear the imbuements for each clothing item
            foreach (Object o in Clothing)
            {
                o.Imbuements.Clear();
            }
        }

        public void ApplyDye(Object clothingItem)
        {
            // Assuming HomeLocation is not null and there are Colors to choose from

            if (HomeLocation != null)
            {
                int decider = Game1.r.Next(100);
                string colorToApply = null;

                if (HomeLocation.HomeCivilization.Type == "druid")
                {
                    colorToApply = "green";
                }
                else if (decider < 60 || clothingItem.Type == "undergarment" || clothingItem.Type == "brassiere")
                {
                    // 60% chance to dye with the HomeCivilization color
                    colorToApply = HomeLocation.HomeCivilization.Color;
                }
                else if (decider < 80)
                {
                    // 20% chance to dye with a related color
                    List<string> relatedColors = Game1.GetFamilyColors(HomeLocation.HomeCivilization.Color);
                    colorToApply = relatedColors[Game1.r.Next(relatedColors.Count)];
                }
                else
                {
                    // 20% chance to not dye at all
                    return;
                }

                // Apply the color to the clothing item
                clothingItem.DyedColor = colorToApply;
            }
        }





        public void SetProficiency(string proficiencyName, int value)
        {
            // Check if the proficiency already exists
            int index = XPValues.FindIndex(p => p.Item1.Equals(proficiencyName, StringComparison.OrdinalIgnoreCase));
            if (index != -1)
            {
                // Update existing proficiency
                XPValues[index] = (proficiencyName, value);
            }
        }

        public void ChangeOpinion(Architect a, int Change)
        {
            int NewOpinion = 0;
            foreach ((Architect, int) Arch in ArchitectOpinions)
            {
                if (Arch.Item1 == a)
                {
                    NewOpinion = Arch.Item2 + Change;
                    ArchitectOpinions.Remove(Arch);
                    ArchitectOpinions.Add((Arch.Item1, NewOpinion));
                    return;
                }
            }
            //this only runs if we didnt find the architect
            ArchitectOpinions.Add((a, Change));
        }
        public int GetOpinion(Architect a)
        {
            foreach ((Architect, int) Arch in ArchitectOpinions)
            {
                if (Arch.Item1 == a)
                {
                    return (Arch.Item2);
                }
            }
            return (0);
        }

        public void SetOpinion(Architect a, int NewOpinion)
        {
            foreach ((Architect, int) Arch in ArchitectOpinions)
            {
                if (Arch.Item1 == a)
                {
                    ArchitectOpinions.Remove(Arch);
                    ArchitectOpinions.Add((Arch.Item1, NewOpinion));
                    return;
                }
            }
            // this only runs if we didn't find the architect
            ArchitectOpinions.Add((a, NewOpinion));
        }




        public void KitOutArchitect(string Type)
        {
            //ACTING location
            Location Location = this.Location != null && Game1.GameWorld.SettlementTypes.Contains(this.Location.Type) ? this.Location : Game1.GameWorld.AllLocations
                                                                    .Where(location => Game1.GameWorld.SettlementTypes.Contains(location.Type) && location.HomeCivilization != null)
                                                                    .OrderBy(_ => Game1.r.Next())
                                                                    .FirstOrDefault();
            if (Location == null)
            {
                return;
            }

            // Adjusted for warrior power
            if (Type.StartsWith("warriorpower"))
            {
                // Extract the warrior power level from the Type string
                int powerLevel = int.Parse(Type.Replace("warriorpower", ""));

                // Calculate the chance to get a piece of armor based on the power level
                int baseChance = 30;
                int chancePerPowerLevel = (100 - baseChance) / 10;
                int chanceToGetArmor = baseChance + (powerLevel * chancePerPowerLevel);

                // Create a weapon
                Material weaponMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                string weaponType = Game1.AllWeapons[Game1.r.Next(Game1.AllWeapons.Count())];
                Object weapon = new Object(null, weaponType, new List<Material>() { weaponMaterial }, null);

                MainHeldObject = weapon;

                // List of possible armor types to create
                List<string> armorTypes = new List<string> { "helmet", "chestplate", "gauntlet", "leggings", "boot" };
                Material armorMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, powerLevel * 2); // Material for the armor

                // Random chance generator
                Random r = new Random();

                foreach (string armorType in armorTypes)
                {
                    // Determine the number of armor pieces to create
                    int pieces = (armorType == "gauntlet" || armorType == "boot") ? 2 : 1;

                    // Chance to create and equip each armor piece based on warrior power level
                    if (r.Next(100) < chanceToGetArmor)
                    {
                        for (int i = 0; i < pieces; i++)
                        {
                            // Append "left" or "right" to the armor type for gauntlets and boots
                            string side = (i == 0) ? "left " : "right ";
                            string fullArmorType = (pieces == 2) ? side + armorType : armorType;

                            // Create armor object
                            Object armor = new Object(null, fullArmorType, new List<Material>() { armorMaterial }, null);

                            // Equip the armor to the architect
                            // This method needs to be implemented according to your game's logic
                            Clothing.Add(armor);
                        }
                    }
                }
            }

            // Priest outfitting
            else if (Type == "priest")
            {
                // Randomize material between Shadesteel or Prismite

                Material m = null;

                if (Race == Game1.GameWorld.GetRace("luminarch"))
                {
                    m = Game1.GameWorld.Prismite;
                }
                else if (Race == Game1.GameWorld.GetRace("nightfell"))
                {
                    m = Game1.GameWorld.Shadesteel;
                }

                Object o = Game1.GameWorld.MagicalSuperLoot(2);

                if (m != null)
                {
                    o.Materials.Clear();
                    o.Materials.Add(m);
                }

                Inventory.Add(o);
            }
            else if (Type == "adventurer")
            {
                // Equip with a weapon
                Material weaponMaterial = FavoriteMetal;
                string weaponType = Game1.AllWeapons[Game1.r.Next(Game1.AllWeapons.Count())];
                Object weapon = new Object(null, weaponType, new List<Material>() { weaponMaterial }, null);

                MainHeldObject = weapon;

                // Inside the 'else if (Type == "adventurer")' block, when equipping with armor
                List<string> armorTypes = new List<string> { "helmet", "chestplate", "left gauntlet", "right gauntlet", "leggings", "left boot", "right boot" };
                foreach (string armorType in armorTypes)
                {
                    if (Game1.r.Next(100) < 75) // 75% chance to equip each armor piece
                    {
                        Material armorMaterial = FavoriteMetal; // Assuming FavoriteMetal is defined elsewhere

                        if (armorType == "gauntlet" || armorType == "boot")
                        {
                            // Create left and right variants for gauntlets or boots
                            Object leftArmor = new Object(null, "left " + armorType, new List<Material>() { armorMaterial }, null);
                            Clothing.Add(leftArmor);

                            Object rightArmor = new Object(null, "right " + armorType, new List<Material>() { armorMaterial }, null);
                            Clothing.Add(rightArmor);
                        }
                        else
                        {
                            // For other armor types, just create one object
                            Object armor = new Object(null, armorType, new List<Material>() { armorMaterial }, null);
                            Clothing.Add(armor);
                        }
                    }
                }



                // Add general adventuring supplies
                // Inside the 'else if (Type == "adventurer")' block, after equipping with armor
                Random random = new Random();

                // Add a cup filled with a drink (50% chance)
                if (random.Next(2) == 0) // 50% chance
                {
                    Material cupMaterial = FavoriteStone; // Material for the cup
                    Material drinkMaterial = random.Next(2) == 0 ? Game1.GameWorld.Coffee : Game1.GameWorld.Tea; // Randomly choose between coffee or tea
                    Object cup = new Object(null, "small cup", new List<Material>() { cupMaterial }, null);
                    cup.ContainedObjects.Add(new Object(null, "drink", new List<Material>() { drinkMaterial }, null));
                    Inventory.Add(cup);
                }

                // Add cut gems (50% chance)
                if (random.Next(2) == 0)
                {
                    Object gem = new Object(null, "cut gem", new List<Material>() { FavoriteGemstone }, null);
                    Inventory.Add(gem);
                }

                // Add extra weapons (50% chance)
                if (random.Next(2) == 0)
                {
                    string WeaponType = Game1.AllWeapons[random.Next(Game1.AllWeapons.Count())];
                    Object extraWeapon = new Object(null, WeaponType, new List<Material>() { FavoriteMetal }, null);
                    Inventory.Add(extraWeapon);
                }

                // Add a wax tablet (50% chance)
                if (random.Next(2) == 0)
                {
                    Object waxTablet = new Object(null, "waxtablet", new List<Material>() { FavoriteWood }, null);
                    Inventory.Add(waxTablet);
                }

                // Add jars (50% chance)
                if (random.Next(2) == 0)
                {
                    Object jar = new Object(null, "jar", new List<Material>() { Game1.GameWorld.Glass }, null);
                    Inventory.Add(jar);
                }

                // Add fragments made of Game1.Vitalium (50% chance)
                if (random.Next(2) == 0)
                {
                    Object fragment = new Object(null, "fragment", new List<Material>() { Game1.GameWorld.Vitalium }, null);
                    Inventory.Add(fragment);
                }

                // Note: For material specifics (/* Specify material */), you should define or select the appropriate materials based on your game's world and logic.


            }
            else if (Type == "archmage")
            {
                // Assigning a staff as a weapon
                Material staffMaterial = Location.HomeCivilization.CulturalWood;
                Object staff = new Object(null, "staff", new List<Material>() { staffMaterial }, null);
                staff.Rarity = "rare";
                staff.ApplyImbuements(1); // Assuming 1 indicates a minor magical enhancement
                MainHeldObject = staff;

                // Wearing a robe made from cultural cloth
                Material robeMaterial = Location.HomeCivilization.CulturalCloth;
                Object robe = new Object(null, "robe", new List<Material>() { robeMaterial }, null);
                Clothing.Add(robe);

                // Carrying a magical tome as an inventory item
                Material bookMaterial = Location.HomeCivilization.CulturalSheet;
                Object book = new Object(null, "book", new List<Material>() { bookMaterial }, null);
                book.Rarity = "uncommon";
                book.ApplyImbuements(2); // Assuming 2 indicates magical texts or spells
                Inventory.Add(book);
            }
            else if (Type == "beastmaster")
            {

                // Assigning a whip as a weapon, indicating control over beasts
                Material whipMaterial = Location.HomeCivilization.CulturalCloth; // Assuming there's a CulturalLeather not listed but logically present
                Object whip = new Object(null, "whip", new List<Material>() { whipMaterial }, null);
                MainHeldObject = whip;

                // Equipping boots for traversing wild terrains
                Material bootsMaterial = Location.HomeCivilization.CulturalCloth; // Assuming again the presence of CulturalLeather
                Object boots = new Object(null, "left boot", new List<Material>() { bootsMaterial }, null);
                Clothing.Add(boots);
                // Repeating for right boot
                boots = new Object(null, "right boot", new List<Material>() { bootsMaterial }, null);
                Clothing.Add(boots);
            }
            else if (Type == "warlock")
            {
                // Assigning a dark tome as a weapon/source of power
                Material tomeMaterial = Location.HomeCivilization.CulturalSheet;
                Object tome = new Object(null, "book", new List<Material>() { tomeMaterial }, null);
                tome.Rarity = "rare";
                tome.ApplyImbuements(3); // Assuming 3 indicates dark or forbidden knowledge
                MainHeldObject = tome;

                // Equipping an amulet as a source of magical protection or power
                Material amuletMaterial = Location.HomeCivilization.CulturalGemstone;
                Object amulet = new Object(null, "amulet", new List<Material>() { amuletMaterial }, null);
                amulet.Rarity = "rare";
                amulet.ApplyImbuements(1); // Minor magical protection
                Clothing.Add(amulet);
            }
            else if (Type.ToLower() == "duelist")
            {
                // Set Duelist's agility
                Agility = 10;

                // List of possible weapon types for a Duelist from the provided object constructor
                List<string> possibleWeapons = new List<string> { "shortsword", "dagger", "rapier", "knife", "shortsword" };

                // Determine the number of weapons to assign (between 2 and 4)
                int numberOfWeapons = Game1.r.Next(2, 5); // Assuming Game1.r is a Random instance

                // Selecting random weapons
                for (int i = 0; i < numberOfWeapons; i++)
                {
                    string weaponType = possibleWeapons[Game1.r.Next(possibleWeapons.Count)];
                    Material weaponMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2); ; // Assuming all weapons are made of the civilization's cultural metal
                    Object weapon = new Object(null, weaponType, new List<Material>() { weaponMaterial }, null);

                    // For the first weapon, assign it to the right hand if right-handed, otherwise add to inventory

                    if (i == 0)
                    {
                        MainHeldObject = weapon;
                    }
                    else if (i == 1)
                    {
                        OffHeldObject = weapon;
                    }
                    else
                    {
                        Inventory.Add(weapon);
                    }
                }

                // Cool items addition - A lightweight cloak for agility and some mystique
                Material cloakMaterial = Location.HomeCivilization.CulturalCloth;
                Object cloak = new Object(null, "cape", new List<Material>() { cloakMaterial }, null);
                cloak.Rarity = "uncommon";
                Clothing.Add(cloak);

                // Adding a small, easily concealable magical item for surprise or utility
                Material gemMaterial = Location.HomeCivilization.CulturalGemstone;
                Object magicalAmulet = new Object(null, "amulet", new List<Material>() { gemMaterial }, null); // A magical amulet for an unexpected advantage
                magicalAmulet.Rarity = "rare";
                magicalAmulet.ApplyImbuements(1); // Imbuement for agility or other duelist-favorable attributes
                Inventory.Add(magicalAmulet);
            }

            else if (Type.ToLower() == "mage")
            {
                // Assigning a wand as a magical implement
                Material orbmat = Location.HomeCivilization.CulturalGemstone;
                Object orb = new Object(null, "orb", new List<Material>() { orbmat }, null);
                orb.Rarity = "rare";
                orb.ApplyImbuements(2); // Assuming this imbues the wand with specific magical properties
                MainHeldObject = orb;

                // Wearing a robe made from cultural cloth, indicative of a mage's status
                Material robeMaterial = Location.HomeCivilization.CulturalCloth;
                Object robe = new Object(null, "robe", new List<Material>() { robeMaterial }, null);
                robe.Rarity = "uncommon";
                robe.ApplyImbuements(1); // Possibly for protection or augmentation of magical abilities
                Clothing.Add(robe);

                // Carrying a magical tome for spells
                Material bookMaterial = Location.HomeCivilization.CulturalSheet;
                Object book = new Object(null, "book", new List<Material>() { bookMaterial }, null);
                book.Rarity = "rare";
                book.ApplyImbuements(3); // Indicating a book of powerful spells or arcane knowledge
                Inventory.Add(book);
            }
            else if (Type.ToLower() == "sorcerer")
            {
                // Equipping with a staff as a primary magical conduit
                Material staffMaterial = Location.HomeCivilization.CulturalWood;
                Object staff = new Object(null, "staff", new List<Material>() { staffMaterial }, null);
                staff.Rarity = "uncommon";
                staff.ApplyImbuements(2); // Assume this enhances magical power
                MainHeldObject = staff;

                // Carrying a tome of spells
                Material tomeMaterial = Location.HomeCivilization.CulturalSheet;
                Object tome = new Object(null, "book", new List<Material>() { tomeMaterial }, null);
                tome.Rarity = "rare";
                tome.ApplyImbuements(0); // Assuming this contains high-level spells
                Inventory.Add(tome);

                // A magical amulet for defense or power boost
                Material amuletMaterial = Location.HomeCivilization.CulturalGemstone;
                Object amulet = new Object(null, "amulet", new List<Material>() { amuletMaterial }, null);
                amulet.Rarity = "rare";
                amulet.ApplyImbuements(0); // Providing magical protection or power boost
                Inventory.Add(amulet);
            }
            else if (Type.ToLower() == "elemental")
            {
                // Carrying a gemstone that resonates with their elemental nature
                Material gemMaterial = Location.HomeCivilization.CulturalGemstone;
                Object elementalGem = new Object(null, "gem", new List<Material>() { gemMaterial }, null);
                elementalGem.Rarity = "rare"; // Elemental gems are precious and powerful
                elementalGem.ApplyImbuements(2); // Assuming this enhances their control over their element
                Inventory.Add(elementalGem);
            }
            else if (Type.ToLower() == "spy")
            {
                // A dagger for close, silent attacks or utility
                Material daggerMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object dagger = new Object(null, "dagger", new List<Material>() { daggerMaterial }, null);
                MainHeldObject = dagger;

                // A cloak for blending into shadows
                Material cloakMaterial = Location.HomeCivilization.CulturalCloth;
                Object cloak = new Object(null, "cape", new List<Material>() { cloakMaterial }, null);
                cloak.Rarity = "uncommon";
                Clothing.Add(cloak);
            }
            else if (Type.ToLower() == "artificer")
            {
                // Equipping with a multi-tool device
                Material toolMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object multiTool = new Object(null, "hammer", new List<Material>() { toolMaterial }, null); // Assuming "small tool" represents a versatile device
                multiTool.Rarity = "rare";
                multiTool.ApplyImbuements(0); // Enhancements for crafting or magical tinkering
                MainHeldObject = multiTool;

                // Carrying a blueprint or schematic for a masterpiece
                Material schematicMaterial = Location.HomeCivilization.CulturalSheet;
                Object schematic = new Object(null, "sheet", new List<Material>() { schematicMaterial }, null);
                schematic.Rarity = "uncommon";
                Inventory.Add(schematic);
            }
            else if (Type.ToLower() == "archartificer")
            {
                // Equipping with a multi-tool device
                Material toolMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object multiTool = new Object(null, "hammer", new List<Material>() { toolMaterial }, null); // Assuming "small tool" represents a versatile device
                multiTool.Rarity = "rare";
                multiTool.ApplyImbuements(0); // Enhancements for crafting or magical tinkering
                MainHeldObject = multiTool;

                // Carrying a blueprint or schematic for a masterpiece
                Material schematicMaterial = Location.HomeCivilization.CulturalSheet;
                Object schematic = new Object(null, "sheet", new List<Material>() { schematicMaterial }, null);
                schematic.Rarity = "uncommon";
                Inventory.Add(schematic);
            }
            else if (Type.ToLower() == "archbard")
            {
                // Equipping with a cloak that enhances their charismatic presence
                Material cloakMaterial = Location.HomeCivilization.CulturalCloth;
                Object cloak = new Object(null, "cape", new List<Material>() { cloakMaterial }, null);
                cloak.Rarity = "uncommon";
                Clothing.Add(cloak);
            }
            else if (Type.ToLower() == "archduelist")
            {
                // Set Archduelist's agility and skill
                Agility = 12; // Assuming modification of stats is permissible here for emphasis

                // Equipping with a masterfully crafted rapier or sword
                Material weaponMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object weapon = new Object(null, "rapier", new List<Material>() { weaponMaterial }, null);
                weapon.Rarity = "epic";
                weapon.ApplyImbuements(3); // For superior finesse and damage
                MainHeldObject = weapon;

                // Carrying a talisman for luck or protection in duels
                Material talismanMaterial = Location.HomeCivilization.CulturalGemstone;
                Object talisman = new Object(null, "amulet", new List<Material>() { talismanMaterial }, null);
                talisman.Rarity = "uncommon";
                Inventory.Add(talisman);
            }
            else if (Type.ToLower() == "archluminary")
            {
                // Bearing a symbol of office or authority
                Material symbolMaterial = Location.HomeCivilization.CulturalGemstone;
                Object symbol = new Object(null, "amulet", new List<Material>() { symbolMaterial }, null);
                symbol.Rarity = "epic";
                symbol.ApplyImbuements(2); // Signifying power or protection
                MainHeldObject = symbol;

                // Wearing a robe that denotes their high rank
                Material robeMaterial = Location.HomeCivilization.CulturalCloth;
                Object robe = new Object(null, "robe", new List<Material>() { robeMaterial }, null);
                robe.Rarity = "rare";
                Clothing.Add(robe);

                // Carrying a tome of ancient wisdom or law
                Material tomeMaterial = Location.HomeCivilization.CulturalSheet;
                Object tome = new Object(null, "book", new List<Material>() { tomeMaterial }, null);
                tome.Rarity = "rare";
                tome.ApplyImbuements(3); // Containing knowledge or spells of governance and influence
                Inventory.Add(tome);
            }
            else if (Type.ToLower() == "bard")
            {
                // Wearing a garment that enhances their allure
                Material garmentMaterial = Location.HomeCivilization.CulturalCloth;
                Object garment = new Object(null, "cape", new List<Material>() { garmentMaterial }, null);
                garment.Rarity = "uncommon";
                Clothing.Add(garment);
            }
            else if (Type.ToLower() == "conjumancer")
            {
                // Holding a summoning crystal
                Material crystalMaterial = Location.HomeCivilization.CulturalGemstone;
                Object crystal = new Object(null, "orb", new List<Material>() { crystalMaterial }, null); // "Orb" used here to represent a crystal with summoning power
                crystal.Rarity = "rare";
                crystal.ApplyImbuements(2); // Assume this enhances summoning power
                MainHeldObject = crystal;

                // Wearing a cloak that conceals their presence, aiding in summoning rituals
                Material cloakMaterial = Location.HomeCivilization.CulturalCloth;
                Object cloak = new Object(null, "cape", new List<Material>() { cloakMaterial }, null);
                cloak.Rarity = "uncommon";
                Clothing.Add(cloak);
            }
            else if (Type.ToLower() == "diplomancer")
            {
                // Bearing a medallion that signifies their diplomatic status
                Material medallionMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object medallion = new Object(null, "amulet", new List<Material>() { medallionMaterial }, null);
                medallion.Rarity = "uncommon";
                Inventory.Add(medallion);

                // Carrying a scroll of truce or treaties
                Material scrollMaterial = Location.HomeCivilization.CulturalSheet;
                Object scroll = new Object(null, "scroll", new List<Material>() { scrollMaterial }, null);
                scroll.Rarity = "common";
                Inventory.Add(scroll);

                // Wearing attire that reflects their diplomatic role
                Material attireMaterial = Location.HomeCivilization.CulturalCloth;
                Object attire = new Object(null, "robe", new List<Material>() { attireMaterial }, null); // "Robe" here symbolizes formal diplomatic wear
                attire.Rarity = "uncommon";
                Clothing.Add(attire);
            }
            else if (Type.ToLower() == "fractalmancer")
            {
                // Holding a fractal orb that embodies their control over patterns
                Material fractalOrbMaterial = Location.HomeCivilization.CulturalGemstone;
                Object fractalOrb = new Object(null, "orb", new List<Material>() { fractalOrbMaterial }, null);
                fractalOrb.Rarity = "rare";
                fractalOrb.ApplyImbuements(3); // Enhancing their reality-manipulating powers
                MainHeldObject = fractalOrb;

                // Wearing attire that seems to shift and change patterns
                Material attireMaterial = Location.HomeCivilization.CulturalCloth;
                Object attire = new Object(null, "robe", new List<Material>() { attireMaterial }, null);
                attire.Rarity = "uncommon";
                Clothing.Add(attire);
            }
            else if (Type.ToLower() == "hunter")
            {
                // Equipped with a knife for skinning and close combat
                Material knifeMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object knife = new Object(null, "knife", new List<Material>() { knifeMaterial }, null);
                knife.Rarity = "common";
                Inventory.Add(knife);

                // Wearing boots suitable for tracking through various terrains
                Material bootsMaterial = Location.HomeCivilization.CulturalCloth; // Assuming for thematic consistency
                Object boots = new Object(null, "left boot", new List<Material>() { bootsMaterial }, null);
                Clothing.Add(boots);
                boots = new Object(null, "right boot", new List<Material>() { bootsMaterial }, null);
                Clothing.Add(boots);
            }
            else if (Type.ToLower() == "knight")
            {
                // Equipped with a sword, the knight's primary weapon
                Material swordMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object sword = new Object(null, "shortsword", new List<Material>() { swordMaterial }, null);
                sword.Rarity = "common";
                MainHeldObject = sword;

                // Wearing a full set of armor for protection
                Material armorMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                List<string> armorPieces = new List<string> { "helmet", "chestplate", "left gauntlet", "right gauntlet", "leggings" };
                foreach (var piece in armorPieces)
                {
                    Object armor = new Object(null, piece, new List<Material>() { armorMaterial }, null);
                    armor.Rarity = "uncommon";
                    Clothing.Add(armor);
                }
            }
            else if (Type.ToLower() == "magician")
            {
                // Holding a summoning crystal
                Material crystalMaterial = Location.HomeCivilization.CulturalGemstone;
                Object crystal = new Object(null, "orb", new List<Material>() { crystalMaterial }, null); // "Orb" used here to represent a crystal with summoning power
                crystal.Rarity = "uncommon";
                crystal.ApplyImbuements(0); // Assume this enhances summoning power
                MainHeldObject = crystal;

                // Wearing a cloak that provides some magical protection
                Material cloakMaterial = Location.HomeCivilization.CulturalCloth;
                Object cloak = new Object(null, "cape", new List<Material>() { cloakMaterial }, null);
                cloak.Rarity = "uncommon";
                Clothing.Add(cloak);
            }
            else if (Type.ToLower() == "mercenary")
            {
                // Equipped with a versatile weapon, such as a sword or axe
                Material weaponMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object weapon = new Object(null, "shortsword", new List<Material>() { weaponMaterial }, null); // "shortsword" can be replaced with "axe" or another weapon based on preference
                weapon.Rarity = "common";
                MainHeldObject = weapon;

                // Wearing durable leather armor for protection and mobility
                Material armorMaterial = Location.HomeCivilization.CulturalMetal;
                Object armor = new Object(null, "chestplate", new List<Material>() { armorMaterial }, null);
                armor.Rarity = "common";
                Clothing.Add(armor);
            }
            else if (Type.ToLower() == "necromancer")
            {
                // Holding a dark tome containing necromantic spells
                Material tomeMaterial = Location.HomeCivilization.CulturalSheet;
                Object tome = new Object(null, "book", new List<Material>() { tomeMaterial }, null);
                tome.Rarity = "rare";
                tome.ApplyImbuements(3); // Enhances dark magic abilities
                MainHeldObject = tome;

                // Wearing a cloak that signifies their macabre affinity
                Material cloakMaterial = Location.HomeCivilization.CulturalCloth;
                Object cloak = new Object(null, "cape", new List<Material>() { cloakMaterial }, null);
                cloak.Rarity = "uncommon";
                Clothing.Add(cloak);
            }
            else if (Type.ToLower() == "perceptomancer")
            {
                // Equipped with an amulet that enhances sensory perception
                Material amuletMaterial = Location.HomeCivilization.CulturalGemstone;
                Object amulet = new Object(null, "amulet", new List<Material>() { amuletMaterial }, null);
                amulet.Rarity = "uncommon";
                amulet.ApplyImbuements(2); // Boosts perceptual abilities
                Inventory.Add(amulet);
            }
            else if (Type.ToLower() == "scout")
            {
                // Carrying a short sword or dagger for self-defense
                Material weaponMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object weapon = new Object(null, "dagger", new List<Material>() { weaponMaterial }, null);
                weapon.Rarity = "common";
                MainHeldObject = weapon;

                // Equipped with light armor for mobility
                Material armorMaterial = Location.HomeCivilization.CulturalMetal;
                Object armor = new Object(null, "chestplate", new List<Material>() { armorMaterial }, null);
                armor.Rarity = "common";
                Clothing.Add(armor);

                // Using a cloak for camouflage
                Object cloak = new Object(null, "cape", new List<Material>() { armorMaterial }, null);
                cloak.Rarity = "common";
                Clothing.Add(cloak);
            }
            else if (Type.ToLower() == "shadeheart")
            {
                //nothing
            }
            else if (Type.ToLower() == "spatiomancer")
            {
                // Equipped with a spatial ring
                Material ringMaterial = Location.HomeCivilization.CulturalGemstone;
                Object ring = new Object(null, "amulet", new List<Material>() { ringMaterial }, null); // Assuming "amulet" can represent a ring in this context
                ring.Rarity = "epic";
                ring.ApplyImbuements(4); // For spatial manipulation and teleportation
                Inventory.Add(ring);

                // Carrying a tome on spatial theories
                Material bookMaterial = Location.HomeCivilization.CulturalSheet;
                Object book = new Object(null, "book", new List<Material>() { bookMaterial }, null);
                book.Rarity = "uncommon";
                Inventory.Add(book);
            }
            else if (Type.ToLower() == "thief")
            {
                // Equipping with a multi-tool device
                Material toolMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object multiTool = new Object(null, "hammer", new List<Material>() { toolMaterial }, null); // Assuming "small tool" represents a versatile device
                multiTool.Rarity = "rare";
                multiTool.ApplyImbuements(0); // Enhancements for crafting or magical tinkering
                MainHeldObject = multiTool;

                // Equipped with a small dagger for self-defense and utility
                Material daggerMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object dagger = new Object(null, "dagger", new List<Material>() { daggerMaterial }, null);
                dagger.Rarity = "common";
                MainHeldObject = dagger;

                // Wearing dark clothing for stealth
                Material clothingMaterial = Location.HomeCivilization.CulturalCloth;
                Object clothing = new Object(null, "robe", new List<Material>() { clothingMaterial }, null);
                clothing.Rarity = "common";
                Clothing.Add(clothing);
            }
        }



        public Architect(string name, string sex, Race race, int age, string role, List<Object> inventory, Location location, District district, Block block, string destiny, int level) //leave level at 0 to autodetermine
        {
            Location = location;
            Block = block;
            District = district;

            HomeDistrict = district;
            HomeLocation = location;

            FalsifiedName = Game1.GameWorld.GenerateUniqueArchitectName(this);

            if (level != 0)
            {
                Level = level;
            }
            else if (role != null)
            {
                Level = BackupProfessionToLevel[role];
            }
            else
            {
                Level = 1;
            }

            Game1.GameWorld.AllArchitects.Add(this);

            MoralCompass = Game1.r.Next(-100, 101); //more is good, less is evil
            StabilityCompass = Game1.r.Next(-100, 101); //more is lawful, less is chaotic

            Name = name;
            Sex = sex;

            if (Location != null)
            {
                if (Location.Library != null)
                {
                    Random random = new Random();
                    var validBooks = Location.Library.HistoricalObjects
                        .Where(o => o.CompositionContent != null)
                        .ToList();

                    if (validBooks.Any())
                    {
                        FavoriteBook = validBooks[random.Next(validBooks.Count)];
                    }
                    else
                    {
                        FavoriteBook = null;
                    }
                }
            }



            if (Game1.GameWorld.HumanoidRaces.Contains(race))
            {
                Material Cloth;

                if (HomeLocation != null && HomeLocation.HomeCivilization != null)
                {
                    Cloth = HomeLocation.HomeCivilization.CulturalCloth;
                }
                else
                {
                    Cloth = Game1.GameWorld.Civilizations[Game1.r.Next(Game1.GameWorld.Civilizations.Count)].CulturalCloth;
                }

                Clothing.Add(new Object(null, "undergarment", new List<Material>() { Cloth }, null));
                ApplyDye(Clothing[0]);

                if (Sex == "female")
                {
                    Clothing.Add(new Object(null, "brassiere", new List<Material>() { Cloth }, null));
                    ApplyDye(Clothing[1]);

                }

                if (Location != null)
                {
                    AddCulturalClothing(Location.HomeCivilization.CulturalHeadwear, Location.HomeCivilization.CulturalCloth);
                    AddCulturalClothing(Location.HomeCivilization.CulturalNeckwear, Location.HomeCivilization.CulturalCloth);
                    AddCulturalClothing(Location.HomeCivilization.CulturalBodywear, Location.HomeCivilization.CulturalCloth);
                    AddCulturalClothing(Location.HomeCivilization.CulturalLegwear, Location.HomeCivilization.CulturalCloth);
                    AddCulturalClothing(Location.HomeCivilization.CulturalHandwear, Location.HomeCivilization.CulturalCloth);
                    AddCulturalClothing(Location.HomeCivilization.CulturalFootwear, Location.HomeCivilization.CulturalCloth);
                }
                else
                {
                    Location L = Game1.GameWorld.AllLocations.Where(location => location.HomeCivilization != null).OrderBy(x => Guid.NewGuid()).FirstOrDefault();

                    if (L != null)
                    {
                        AddCulturalClothing(L.HomeCivilization.CulturalHeadwear, L.HomeCivilization.CulturalCloth);
                        AddCulturalClothing(L.HomeCivilization.CulturalNeckwear, L.HomeCivilization.CulturalCloth);
                        AddCulturalClothing(L.HomeCivilization.CulturalBodywear, L.HomeCivilization.CulturalCloth);
                        AddCulturalClothing(L.HomeCivilization.CulturalLegwear, L.HomeCivilization.CulturalCloth);
                        AddCulturalClothing(L.HomeCivilization.CulturalHandwear, L.HomeCivilization.CulturalCloth);
                        AddCulturalClothing(L.HomeCivilization.CulturalFootwear, L.HomeCivilization.CulturalCloth);
                    }
                }

                if(Location != null && Location.HomeCivilization.Type != "druid")
                {

                    // Define a list of general clothing items that can be added
                    List<string> generalClothingItems = new List<string>
                    {
                        "small hat", "large hat", "hood", "cape", "robe", "amulet", "flair",
                        "left glove", "right glove", "left wristwrap", "right wristwrap",
                        "skirt", "shortsleeve shirt", "longsleeve shirt", "uppershirt",
                        "straps", "pants", "shorts", "kilt", "wraps",
                    };

                    // Add 0-1 or semirarely 2 random general clothing items
                    int numberOfItemsToAdd = Game1.r.NextDouble() < 0.2 ? 2 : Game1.r.Next(2);

                    for (int i = 0; i < numberOfItemsToAdd; i++)
                    {
                        string randomItem;
                        do
                        {
                            randomItem = generalClothingItems[Game1.r.Next(generalClothingItems.Count)];
                        } while (Clothing.Any(c => c.Type == randomItem && randomItem != "amulet"));

                        Clothing.Add(new Object(null, randomItem, new List<Material>() { Cloth }, null));
                    }
                }
            }





            List<int> SkillValues = new List<int>() { 1, 2, 3, 4, 5, 6, 7 };
            // Shuffle the SkillValues list
            List<int> shuffledSkills = SkillValues.OrderBy(x => Game1.r.Next()).ToList();

            // Assign values to each skill
            Strength = shuffledSkills[0];
            Agility = shuffledSkills[1];
            Dexterity = shuffledSkills[2];
            Endurance = shuffledSkills[3];
            Creativity = shuffledSkills[4];
            Charisma = shuffledSkills[5];
            Focus = shuffledSkills[6];

            //give him a ton of alligned domains.
            AlignedDomains = Game1.GameWorld.Domains.OrderBy(x => Guid.NewGuid()).Take(Game1.r.Next(1, 8)).ToList();

            

            if (Sex == "male")
            {
                Pronoun = "he";
                PossessivePronoun = "his";
                ObjectivePronoun = "him";
            }
            else if (Sex == "female")
            {
                Pronoun = "she";
                PossessivePronoun = "her";
                ObjectivePronoun = "her";
            }

            Race = race;
            NaturalArmor = race.NaturalArmor;
            OppositionTags.AddRange(Race.OppositionTags);

            BirthdayCycle = Math.Round(Game1.GameWorld != null ? Game1.GameWorld.Cycle : 0 - age * 290304000.0);

            if (inventory == null)
            {
                Inventory = new List<Object>();
            }
            else
            {
                Inventory = inventory;
            }


            if (role != null && role != "")
            {
                Profession = role;
            }
            else
            {
                Profession = "indolent";
            }


            Destiny = destiny;
            Loaded = false;


            FavoriteCultureField = Game1.CultureSchools[Game1.r.Next(Game1.CultureSchools.Count)];
            FavoriteScienceField = Game1.ScienceSchools[Game1.r.Next(Game1.ScienceSchools.Count)];
            FavoriteMagicField = Game1.MagicSchools[Game1.r.Next(Game1.MagicSchools.Count)];

            FavoriteColor = Game1.Colors[Game1.r.Next(Game1.Colors.Count)];
            FavoriteGemstone = Game1.GameWorld.Gemstones[Game1.r.Next(Game1.GameWorld.Gemstones.Count)];
            FavoriteStone = Game1.GameWorld.Stones[Game1.r.Next(Game1.GameWorld.Stones.Count)];
            FavoriteWood = Game1.GameWorld.Woods[Game1.r.Next(Game1.GameWorld.Woods.Count)];
            FavoriteMetal = Game1.GameWorld.Metals[Game1.r.Next(Game1.GameWorld.Metals.Count)];
            FavoriteCloth = Game1.GameWorld.Cloths[Game1.r.Next(Game1.GameWorld.Cloths.Count)];

            DestinyArrivalYear = Game1.r.Next(18, 45);

            if (Game1.GameWorld.HumanoidRaces.Contains(Race))
            {
                if (Game1.r.Next(1, 5) == 1)
                {
                    DoIDieOfOldAge = false;
                    // Modified system for not dying of old age with logarithmic distribution
                    double rand = Game1.r.NextDouble();
                    TerminalAge = (int)(Age + 2 + Math.Pow(rand, 4) * (120 - Age - 2)); // Increased power to 4 for better skew towards higher ages

                    // Ensure the values are capped properly in case of any boundary issues
                    if (TerminalAge > 120)
                    {
                        TerminalAge = 120;
                    }
                    else if (TerminalAge < Age + 2)
                    {
                        TerminalAge = Age + 2;
                    }
                }
                else
                {
                    DoIDieOfOldAge = true;
                    // Generate terminal age with even higher probability for higher values
                    double rand = Game1.r.NextDouble();
                    TerminalAge = (int)(80 + Math.Pow(rand, 20) * (120 - 80)); // Increased power to 20 for more skew towards higher ages

                    // Ensure the values are capped properly in case of any boundary issues
                    if (TerminalAge > 120)
                    {
                        TerminalAge = 120;
                    }
                    else if (TerminalAge < 80)
                    {
                        TerminalAge = 80;
                    }
                }
            }
            else
            {
                DoIDieOfOldAge = false;
                TerminalAge = 13371337;
            }

            if (Profession == "scholar")
            {
                int ScholarDecider = Game1.r.Next(1, 16);

                if (ScholarDecider < 3)
                {
                    ScholarType = "mage";
                }
                else if (ScholarDecider < 6)
                {
                    ScholarType = "engineer";
                }
                else if (ScholarDecider < 9)
                {
                    ScholarType = "entertainer";
                }
                else if (ScholarDecider < 11)
                {
                    ScholarType = "artificer";
                }
                else if (ScholarDecider < 13)
                {
                    ScholarType = "bard";
                }
                else if (ScholarDecider < 15)
                {
                    ScholarType = "sage";
                }
                else
                {
                    ScholarType = "luminary";
                }
            }

            AddBodyParts();

            foreach (Object o in BodyParts)
            {
                o.IsBodyPart = true;
                o.Owner = this;
                o.Creator = this;
            }


            if(Game1.r.Next(1,10) == 1)
            {
                //reverse hands if this is true, like left handedness
                MainInteractionAppendage = FindBodyPart(Race.OffInteractionAppendage);
                OffInteractionAppendage = FindBodyPart(Race.MainInteractionAppendage);
            }
            else
            {
                //or jsut do this lololololol
                MainInteractionAppendage = FindBodyPart(Race.MainInteractionAppendage);
                OffInteractionAppendage = FindBodyPart(Race.OffInteractionAppendage);
            }

            Energy = MaxEnergy();
        }

        public void AddBodyParts()
        {
            if (Race != null)
            {
                foreach ((string, Material) o in Race.BodyParts)
                {
                    Object O = new Object(Name + "'s " + o.Item1, o.Item1, new List<Material> { o.Item2 }, false, false, null, this, 5, false, null, null, null, false);
                    O.Owner = this;
                    BodyParts.Add(O);
                }
            }
        }

        public Object FindBodyPart(string objectType)
        {
            foreach (var bodyPart in BodyParts)
            {
                if (bodyPart.Type == objectType)
                {
                    return bodyPart;
                }
            }

            return null;
        }

        public string GetNonZeroPathLevels()
        {
            var paths = new List<string>();

            if (PathOfShadowLevel > 0) paths.Add($"Shadow LVL {PathOfShadowLevel}");
            if (PathOfLifeLevel > 0) paths.Add($"Life LVL {PathOfLifeLevel}");
            if (PathOfDeathLevel > 0) paths.Add($"Death LVL {PathOfDeathLevel}");
            if (PathOfTimeLevel > 0) paths.Add($"Time LVL {PathOfTimeLevel}");
            if (PathOfStarsLevel > 0) paths.Add($"Stars LVL {PathOfStarsLevel}");
            if (PathOfHeatLevel > 0) paths.Add($"Heat LVL {PathOfHeatLevel}");
            if (PathOfIllusionsLevel > 0) paths.Add($"Illusions LVL {PathOfIllusionsLevel}");
            if (PathOfEtherealityLevel > 0) paths.Add($"Ethereality LVL {PathOfEtherealityLevel}");
            if (PathOfVoidLevel > 0) paths.Add($"Void LVL {PathOfVoidLevel}");
            if (PathOfStormsLevel > 0) paths.Add($"Storms LVL {PathOfStormsLevel}");
            if (PathOfForgeLevel > 0) paths.Add($"Forge LVL {PathOfForgeLevel}");
            if (PathOfLoreLevel > 0) paths.Add($"Lore LVL {PathOfLoreLevel}");
            if (PathOfMindLevel > 0) paths.Add($"Mind LVL {PathOfMindLevel}");
            if (PathOfSoulLevel > 0) paths.Add($"Soul LVL {PathOfSoulLevel}");
            if (PathOfBodyLevel > 0) paths.Add($"Body LVL {PathOfBodyLevel}");
            if (PathOfSpaceLevel > 0) paths.Add($"Space LVL {PathOfSpaceLevel}");
            if (PathOfRealityLevel > 0) paths.Add($"Reality LVL {PathOfRealityLevel}");
            if (PathOfLightLevel > 0) paths.Add($"Light LVL {PathOfLightLevel}");

            return paths.Count > 0 ? $"PATHS: {string.Join(", ", paths)}" : "No paths have levels above 0.";
        }

        public int EscapeChance()
        {
            int Chance = Math.Min(Agility * 4 + PathOfShadowLevel * 3 + (CyclesSinceMoved / 50) * 2, 100);

            if (DismissalCycles > 0)
            {
                Chance += 40;
            }

            return Math.Min(Chance, 100);
        }

        public void AssignSpells()
        {
            if (Race == Game1.GameWorld.GetRace("debtshiba"))
            {
                SpellcastingPower = 10;
                for (int i = 0; i < 4; i++)
                {
                    AddSpell();
                }
            }
            else if (Profession == "archbard" || Profession == "archluminary" ||
            Profession == "archartificer" || Profession == "archduelist")
            {
                SpellcastingPower = 4;
                for (int i = 0; i < 4; i++)
                {
                    AddSpell();
                }
            }
            else if (Profession == "warlock" || Profession == "sorcerer" ||
                     Profession == "necromancer" || Profession == "spatiomancer" ||
                     Profession == "perceptomancer" || Profession == "conjumancer" ||
                     Profession == "fractalmancer")
            {
                SpellcastingPower = 3;
                for (int i = 0; i < 3; i++)
                {
                    AddSpell();
                }
            }
            else if (Profession == "elemental")
            {
                SpellcastingPower = 3;
                AddSpell();
            }
            else if (Profession == "archmage")
            {
                SpellcastingPower = 2;
                for (int i = 0; i < 2; i++)
                {
                    AddSpell();
                }
            }
            else if (Profession == "mage" || Profession == "magician")
            {
                SpellcastingPower = 1;
                for (int i = 0; i < 2; i++)
                {
                    AddSpell();
                }
            }
        }

        public void AddSpell()
        {
            List<string> OffensiveSpells = new List<string> { "expel", "water bolt", "chaos flare", "concentrated ignition", "tremor", "ice shock" };

            List<Entity> unknownSpells = Game1.GameWorld.AllSpells
                .Where(spell => OffensiveSpells.Contains(spell.Metadata) && !SpellsKnown.Contains(spell))
                .ToList();

            if (unknownSpells.Count > 0)
            {
                SpellsKnown.Add(unknownSpells[Game1.r.Next(unknownSpells.Count)]);
            }
        }


        public double Speed()
        {
            const double BaseSpeed = 1.0; // Normalized average speed
            const double WeightEffectModifier = 0.5; // Constant to adjust the total effect of weight

            int numberOfLegs = BodyParts.Count(part => new[] { "leg", "tentacle", "wing", "fin", "sludge", "sphere", "shard" }
                                              .Any(term => part.Type.Contains(term)));
            double legSpeedModifier = CalculateLegSpeedModifier(numberOfLegs);
            double agilityModifier = CalculateAgilityModifier(Agility); // Adjusted to keep speed not too high
            double totalWeight = Inventory.Sum(item => item.IsMagical ? item.Weight * 0.2 : item.Weight)
                      + Clothing.Sum(item => item.IsMagical ? item.Weight * 0.2 : item.Weight * 0.5);
            double weightPenaltyModifier = CalculateWeightPenaltyModifier(totalWeight, Endurance, WeightEffectModifier);
            double radiantPenalty = RadiantCycles > 0 ? 0.6 : 1.0;
            double bodyPathBonus = PathOfBodyLevel >= 8 ? 1.5 : 1.0;

            double rawSpeed = BaseSpeed * legSpeedModifier * agilityModifier * weightPenaltyModifier * radiantPenalty * bodyPathBonus;

            if (OnGround)
            {
                rawSpeed = (int)Math.Round(rawSpeed / 2);
            }

            return Math.Max(0.1, Math.Round(rawSpeed, 2)); // Rounding to the nearest two decimal places

            double CalculateLegSpeedModifier(int numberOfLegs)
            {
                // Adjusting for new effects of leg counts
                if (numberOfLegs == 0) return 0.1; // 1/10 speed if no legs
                else if (numberOfLegs == 1) return 0.6; // 3/5 speed if one leg
                else if (numberOfLegs == 2) return 1.0; // Full speed if two legs
                else // Diminishing returns for each leg above 2
                {
                    double extraLegsModifier = 0.1 * (numberOfLegs - 2); // 0.1 boost per additional leg
                    return 1.0 + Math.Min(extraLegsModifier, 0.5); // Cap at an additional 0.5
                }
            }

            double CalculateAgilityModifier(int agility)
            {
                // Adjusting the agility effect to start from 0.9 and increase by 0.05 for each point
                double baseModifier = 0.9;
                double increment = 0.05;
                return baseModifier + (agility * increment);
            }


            double CalculateWeightPenaltyModifier(double totalWeight, int endurance, double weightEffectModifier)
            {
                double excessWeight = Math.Max(0, totalWeight - 1000); // Considering weight above 1000g
                double penalty = excessWeight / 10000; // Halved penalty for weight
                double enduranceModifier = 1 - (0.025 * (endurance - 1)); // Halved impact of endurance
                return Math.Max(0.3, 1 - (penalty * enduranceModifier * weightEffectModifier)); // Ensuring a minimum 75% speed
            }
        }



        public string GetDescription(int value)
        {
            switch (value)
            {
                case 0: return "non-existent";
                case 1: return "terrible";
                case 2: return "poor";
                case 3: return "average";
                case 4: return "good";
                case 5: return "excellent";
                case 6: return "superior";
                case 7: return "exceptional";
                case 8: return "unbelievable";
                case 9: return "unfathomable";
                case 10: return "mythical";
                default: return "archancient"; // In case of greater than 10 value
            }
        }
        public string CheckEnergyLevel()
        {
            double ratio = (double)(Energy / MaxEnergy());
            if (ratio == 0)
            {
                return (Pronoun + " is dead.");
            }
            if (ratio < 0.2)
            {
                return (Pronoun + " appears on the verge of collapse.");
            }
            else if (ratio < 0.4)
            {
                return (Pronoun + " is not looking very healthy.");
            }
            else if (ratio < 0.6)
            {
                return (Pronoun + " is looking somewhat dim.");
            }
            else if (ratio < 0.8)
            {
                return (Pronoun + " is looking just fine.");
            }
            else
            {
                return (Pronoun + " looks perfectly healthy.");
            }
        }
        public string DescribeArchitectInventory()
        {
            var inventoryItemCounts = new Dictionary<string, int>();
            var clothingItemCounts = new Dictionary<string, int>();

            // Function to add an item to the dictionary
            void AddItem(Dictionary<string, int> itemCounts, Object item)
            {
                if (item != null && item.ReferredToNames.Any())
                {
                    string itemName = item.ReferredToNames[0];
                    if (!itemCounts.ContainsKey(itemName))
                        itemCounts[itemName] = 0;
                    itemCounts[itemName]++;
                }
            }

            // Add right and left hand objects
            AddItem(inventoryItemCounts, MainHeldObject);
            AddItem(inventoryItemCounts, OffHeldObject);

            // Add inventory items
            foreach (Object o in Inventory)
            {
                AddItem(inventoryItemCounts, o);
            }

            // Add clothing items
            foreach (Object c in Clothing)
            {
                AddItem(clothingItemCounts, c);
            }

            // Describe hand objects
            string DescribeAppendageObject(Object appendageObject, string appendage)
            {
                if (appendageObject != null && appendageObject.ReferredToNames.Any())
                {
                    string article = "aeiouAEIOU".Contains(appendageObject.ReferredToNames[0][0]) ? "an " : "a ";
                    return $"{Pronoun} has {article}{appendageObject.ReferredToNames[0]} in {PossessivePronoun} {appendage}";
                }
                else
                {
                    return $"{PossessivePronoun} {appendage} is empty";
                }
            }

            // Get the dominant and off appendages
            Object dominantAppendage = MainInteractionAppendage; // or whichever property refers to the dominant appendage
            Object dominantHeldObject = MainHeldObject;
            string dominantAppendageName = MainInteractionAppendage.Type;

            Object offAppendage = OffInteractionAppendage;
            Object offHeldObject = OffHeldObject;
            string offAppendageName = OffInteractionAppendage.Type;

            // Append the description
            StringBuilder description = new StringBuilder();
            description.Append(DescribeAppendageObject(dominantHeldObject, dominantAppendageName));
            description.Append(", and ");
            description.Append(DescribeAppendageObject(offHeldObject, offAppendageName));
            description.Append(".");

            // Describe inventory
            if (inventoryItemCounts.Count > 0)
            {
                description.Append($" {Pronoun} is carrying ");
                var inventoryDescriptions = inventoryItemCounts.Select(kvp =>
                {
                    string itemName = kvp.Key;
                    int count = kvp.Value;
                    string article = "aeiouAEIOU".Contains(itemName[0]) ? "an " : "a ";
                    string pluralForm = itemName.EndsWith("s") ? itemName + "es" : itemName + "s";
                    return count == 1 ? $"{article}{itemName}" : $"{count} {pluralForm}";
                }).ToList();

                if (inventoryDescriptions.Count > 1)
                {
                    var lastItem = inventoryDescriptions.Last();
                    inventoryDescriptions.RemoveAt(inventoryDescriptions.Count - 1);
                    description.Append(string.Join(", ", inventoryDescriptions));
                    description.Append(", and " + lastItem);
                }
                else
                {
                    description.Append(inventoryDescriptions[0]);
                }
            }

            // Describe clothing
            if (clothingItemCounts.Count > 0)
            {
                description.Append($". {Game1.Capitalize(Pronoun)} is wearing ");
                var clothingDescriptions = clothingItemCounts.Select(kvp =>
                {
                    string itemName = kvp.Key;
                    int count = kvp.Value;
                    string article = "aeiouAEIOU".Contains(itemName[0]) ? "an " : "a ";
                    string pluralForm = itemName.EndsWith("s") ? itemName + "es" : itemName + "s";
                    return count == 1 ? $"{article}{itemName}" : $"{count} {pluralForm}";
                }).ToList();

                if (clothingDescriptions.Count > 1)
                {
                    var lastClothingItem = clothingDescriptions.Last();
                    clothingDescriptions.RemoveAt(clothingDescriptions.Count - 1);
                    description.Append(string.Join(", ", clothingDescriptions));
                    description.Append(", and " + lastClothingItem);
                }
                else
                {
                    description.Append(clothingDescriptions[0]);
                }
            }

            return description.ToString() + ".";
        }

        public (int sustain, int parry, int block, int duck, int jump, int roll, int disarm, int redirect) CalculateSuccessChances(Attack attack, int reactionModifierInt, Architect Attacker, int attackersProficiency)
        {
            // Assuming you have methods to get these values from imbuements
            int extraDodgeChance = ExtraDodgeChance + (DismissalCycles > 0 ? 5 : 0);
            int extraRedirectionChance = ExtraRedirectionChance;
            int extraShieldEffectiveness = ExtraShieldEffectiveness;

            if (UnconsciousCycles > 0 || HoldCycles > 0)
            {
                return (0, 0, 0, 0, 0, 0, 0, 0);
            }

            int GetResistance(string DamageType)
            {
                return DamageType switch
                {
                    "scourging" => ExtraScourgingResistance,
                    "piercing" => ExtraPiercingResistance,
                    "slashing" => ExtraSlashingResistance,
                    _ => ExtraBashingResistance,
                };
            }

            float resistanceMultiplier = 1 + (GetResistance(attack.Weapon?.DamageType ?? "bashing") / 100f);

            int CalculateChance(int proficiency, int baseChance, int multiplier)
            {
                int chance = baseChance + (proficiency * multiplier);
                chance = (int)(chance * resistanceMultiplier); // Apply resistance
                return Math.Clamp(chance, 0, 100); // Clamp the chance between 0 and 100
            }

            int sustainChance = 0; // Sustain means taking the hit, so 0% chance to evade

            // Target-based modifications
            bool targetAffectsJump = attack.Target.Type.Contains("head") || attack.Target.Type.Contains("neck") || attack.Target.Type.Contains("eye") || attack.Target.Type.Contains("tooth") || attack.Target.Type.Contains("core") || attack.Target.Type.Contains("shoulder");
            bool targetAffectsDuck = attack.Target.Type.Contains("shard") || attack.Target.Type.Contains("leg") || attack.Target.Type.Contains("foot");

            int ModifyChanceBasedOnTarget(int chance, bool decrease)
            {
                return (int)(chance * (decrease ? 0.8 : 1.0));
            }

            // Check if the attack target's type is a necessary body part
            bool isNecessaryBodyPart = this.Race.NecessaryBodyParts.Contains(attack.Target.Type);
            float necessaryBodyPartMultiplier = isNecessaryBodyPart ? 1.15f : 1.0f;

            // Get the target owner as Architect
            Architect targetOwner = (Architect)attack.Target.Owner;

            // Calculate chances with uniqueness
            int parryChance = ApplyRandomMultiplier(CalculateChance(GetProficiency("parrying") + extraRedirectionChance, 50, 4), reactionModifierInt, 1);
            string[] parryWeaponTypes = { "shortsword", "greatsword", "battle axe", "rapier", "mace" };
            // Adjust parry chance if NOT holding parry weapons
            bool leftHandParryWeapon = targetOwner.OffHeldObject != null && parryWeaponTypes.Contains(targetOwner.OffHeldObject.Type);
            bool rightHandParryWeapon = targetOwner.MainHeldObject != null && parryWeaponTypes.Contains(targetOwner.MainHeldObject.Type);

            if (!leftHandParryWeapon) parryChance = (int)(parryChance * 0.9); // 10% reduction for not having left-hand parrying weapon
            if (!rightHandParryWeapon) parryChance = (int)(parryChance * 0.9); // 10% reduction for not having right-hand parrying weapon

            int blockChance = 0;
            if (targetOwner.OffHeldObject?.Type == "shield" || targetOwner.MainHeldObject?.Type == "shield")
            {
                int shieldToughness = (targetOwner.OffHeldObject?.Materials?[0].Toughness ?? 0) + (targetOwner.MainHeldObject?.Materials?[0].Toughness ?? 0);
                blockChance = ApplyRandomMultiplier(CalculateChance(GetProficiency("blocking") + extraShieldEffectiveness, 50 + shieldToughness, 4), reactionModifierInt, 2);
            }

            int baseDuckChance = CalculateChance(GetProficiency("dodging") + extraDodgeChance, 50, 4);
            int baseJumpChance = CalculateChance(GetProficiency("dodging") + extraDodgeChance, 50, 4);

            // Adjust jump and duck chances based on the attack target
            int duckChance = targetAffectsDuck ? ModifyChanceBasedOnTarget(baseDuckChance, true) : baseDuckChance;
            int jumpChance = targetAffectsJump ? ModifyChanceBasedOnTarget(baseJumpChance, true) : baseJumpChance;

            int rollChance = ApplyRandomMultiplier(CalculateChance(GetProficiency("dodging") + extraDodgeChance, 40, 4), reactionModifierInt, 5);
            int disarmChance = ApplyRandomMultiplier(CalculateChance(GetProficiency("disarming"), 0, 6), reactionModifierInt, 6);
            int redirectChance = ApplyRandomMultiplier(CalculateChance(GetProficiency("redirection") + extraRedirectionChance, 50, 4), reactionModifierInt, 7);

            // Apply multipliers
            float multiplier = 1.0f;
            if (DestabilizedCycles > 0) multiplier *= 0.5f; // Reduce chances by half if destabilized
            if (Attacker.IsCoveredInPlants) multiplier *= 1.5f; // Increase chances by 1.5x if attacker is covered in plants

            foreach (Object o in Attacker.Clothing)
            {
                if (!o.IsWearable) multiplier *= 0.95f; // Reduce success chances by 5% for each non-wearable clothing object
            }

            float attackersProficiencyImpact = 1.0f - (attackersProficiency * 0.03f); // Each proficiency level reduces defender's success chance by 3%

            // Apply the multiplier and ensuring the value stays within 0-100
            parryChance = (int)(parryChance * multiplier * attackersProficiencyImpact * necessaryBodyPartMultiplier);
            blockChance = (int)(blockChance * multiplier * attackersProficiencyImpact * necessaryBodyPartMultiplier);
            duckChance = (int)(duckChance * multiplier * attackersProficiencyImpact * necessaryBodyPartMultiplier);
            jumpChance = (int)(jumpChance * multiplier * attackersProficiencyImpact * necessaryBodyPartMultiplier);
            rollChance = (int)(rollChance * multiplier * attackersProficiencyImpact * necessaryBodyPartMultiplier);
            disarmChance = (int)(disarmChance * multiplier * attackersProficiencyImpact * necessaryBodyPartMultiplier);
            redirectChance = (int)(redirectChance * multiplier * attackersProficiencyImpact * necessaryBodyPartMultiplier);

            // Apply Weight Reduction
            int CalculateTotalWeight(IEnumerable<Object> objects)
            {
                int totalWeight = 0;
                foreach (var obj in objects)
                {
                    totalWeight += (int)(obj.Weight);
                    if (obj.ContainedObjects != null && obj.ContainedObjects.Any())
                    {
                        totalWeight += CalculateTotalWeight(obj.ContainedObjects); // Recursively add the weight of contained objects
                    }
                }
                return totalWeight;
            }

            int totalWeight = CalculateTotalWeight(this.Inventory)
                              + CalculateTotalWeight(this.Clothing)
                              + (this.OffHeldObject != null ? CalculateTotalWeight(new[] { this.OffHeldObject }) : 0)
                              + (this.MainHeldObject != null ? CalculateTotalWeight(new[] { this.MainHeldObject }) : 0);

            float weightImpact = 1.0f - (totalWeight / 1000 * 0.02f);
            multiplier *= weightImpact;

            // Apply Exposure
            int exposure = attack.Target.Exposure; // Assuming Exposure is an int property of the Target
            float exposureImpact = 1.0f - (exposure / 100f);

            // Ensuring values are clamped between 0 and 100 after final adjustments
            int backflipBonus = ReactionBoostCycles > 0 ? 30 : 0;

            return (
                sustainChance, // Sustain chance isn't affected by proficiency as it's always 0
                Math.Clamp(parryChance + backflipBonus, 0, 100),
                Math.Clamp(blockChance + backflipBonus, 0, 100),
                Math.Clamp(duckChance + backflipBonus, 0, 100),
                Math.Clamp(jumpChance + backflipBonus, 0, 100),
                Math.Clamp(rollChance + backflipBonus, 0, 100),
                Math.Clamp(disarmChance + backflipBonus, 0, 100),
                Math.Clamp(redirectChance + backflipBonus, 0, 100)
            );
        }


        // Random multiplier function
        private int ApplyRandomMultiplier(int baseChance, int reactionModifierInt, int actionIndex)
        {
            // Create a unique seed based on reactionModifierInt and actionIndex
            int uniqueSeed = reactionModifierInt + actionIndex * 1000; // Ensure actionIndex significantly changes the seed
            Random random = new Random(uniqueSeed);

            // Generate a multiplier in the range of -15 to +15
            int adjustment = random.Next(-15, 16); // Random number between -15 and 15 (inclusive)

            // Apply this adjustment to the base chance
            int adjustedChance = baseChance + adjustment;

            // Ensure the adjusted chance is between 0 and 100
            return Math.Clamp(adjustedChance, 0, 100);
        }


        public bool CanBeNotOnGround()
        {
            // List of valid body part types for standing/flying
            List<string> validTypes = new List<string> { "leg", "tentacle", "wing", "fin", "sludge", "sphere", "shard" };

            // Find all body parts that match the valid types and have Integrity >= 30
            var eligibleParts = BodyParts.Where(bp =>
                validTypes.Any(type => bp.Type.Contains(type)) &&
                bp.Integrity >= 30).ToList();

            // Check if there are at least two such body parts
            return eligibleParts.Count >= 2;
        }

        public int MaxEnergy()
        {
            int Max = (Endurance * 4) + 100;

            if (Race == Game1.GameWorld.GetRace("luminarch"))
            {
                Max += Max * (1 / 10);
            }
            else if (Race == Game1.GameWorld.GetRace("nightfell"))
            {
                Max -= Max * (1 / 10);
            }

            if (Game1.EnergySizeMultipliers.ContainsKey(Size))
            {
                Max = (int)(Max * Game1.EnergySizeMultipliers[Size]);
            }

            Max += MaxEnergyMod;
            Max += MaxEnergyInspiration ? 5 : 0;

            return Max;
        }



        public bool TemporarilyIncapacitated()
        {
            if (CooldownCycles == 0 && FractalCycles == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void DropInventory(bool dropUndergarments)
        {
            List<Object> filteredClothing;
            if (dropUndergarments)
            {
                filteredClothing = new List<Object>(Clothing);
            }
            else
            {
                filteredClothing = Clothing.Where(o => o.Type != "brassiere" && o.Type != "undergarment").ToList();
            }

            if (Room != null)
            {
                Room.Objects.AddRange(Inventory);
                Room.Objects.AddRange(filteredClothing);

                if (MainHeldObject != null)
                {
                    Room.Objects.Add(MainHeldObject);
                    MainHeldObject = null;
                }

                if (OffHeldObject != null)
                {
                    Room.Objects.Add(OffHeldObject);
                    OffHeldObject = null;
                }

                foreach (Object o in Room.Objects)
                {
                    o.Room = Room;
                    o.Block = Room.Structure.Block;
                }
            }
            else if (Block != null)
            {
                Block.Objects.AddRange(Inventory);
                Block.Objects.AddRange(filteredClothing);

                if (MainHeldObject != null)
                {
                    Block.Objects.Add(MainHeldObject);
                }

                if (OffHeldObject != null)
                {
                    Block.Objects.Add(OffHeldObject);
                }

                foreach (Object o in Block.Objects)
                {
                    o.Block = Block;
                }
            }

            if (!dropUndergarments)
            {
                Clothing = Clothing.Where(o => o.Type == "brassiere" || o.Type == "undergarment").ToList();
            }
            else
            {
                Clothing.Clear();
            }
            Inventory.Clear();
        }

        public void RaiseFromTheDead(Architect executor, string referredToName, int pathOfDeathLevel, int minPathOfDeathLevel)
        {
            int effectivePathOfDeathLevel = Math.Max(pathOfDeathLevel, minPathOfDeathLevel);

            // Count undead already in the party
            int shadeCount = 0;
            foreach (Architect a in Game1.GamePlayerParty.Architects)
            {
                if (a.Race == Game1.GameWorld.GetRace("shade"))
                {
                    shadeCount++;
                }
            }

            if (IsAlive == false && (Block == executor.Block && Room == executor.Room) && shadeCount < effectivePathOfDeathLevel)
            {
                Game1.Announcements.Add(new TextStorage(referredToName + " rises with a putrid, dark energy!", Color.Purple, new List<Entity>() { this }));
                IsAlive = true;
                IsImmortal = true;
                UnconsciousCycles = 0;
                HoldCycles = 0;
                Race = Game1.GameWorld.GetRace("shade");
                OppositionTags.Add("alllife");
                Energy = 50;
                Bleeding = 0;
                AddBodyParts();
                MaxEnergyMod = 50;
                UndeadCreator = executor;
                CooldownCycles = 0;

                foreach (Object o in this.BodyParts)
                {
                    o.Integrity = Math.Max(50, o.Integrity);
                }
            }
            else
            {
                Game1.Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple, new List<Entity>()));
            }
        }

        public void UpdateNames()
        {
            bool PlayerKnowsArch = false;
            ClearReferredToNames();

            string TrueProfession = Profession;

            if (IsAlive)
            {
                if(Game1.MostRecentPartyTurnArchitect.KnownArchitects.Contains(this) || Game1.GamePlayerParty.Architects.Contains(this))
                {
                    PlayerKnowsArch = true;
                }

                if (PlayerKnowsArch || Game1.GamePlayerParty.Architects.Contains(this))
                {
                    AddReferredToName(Name);

                    if (!string.IsNullOrEmpty(Task))
                    {
                        AddReferredToName(Game1.ConvertArchitectToDescription(this));
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(Profession))
                    {
                        AddReferredToName("the unknown " + Profession);
                        AddReferredToName("unknown " + Profession);
                        AddReferredToName(Profession);
                    }
                    else
                    {
                        AddReferredToName("indolent");
                        AddReferredToName("unknown indolent");
                        AddReferredToName("the unknown indolent");
                    }

                    if (string.IsNullOrEmpty(Task))
                    {
                        AddReferredToName(Game1.ConvertArchitectToDescription(this));
                    }
                }

                foreach (Object o in BodyParts)
                {
                    o.ClearReferredToNames();

                    foreach (string s in ReferredToNames)
                    {
                        o.AddReferredToName(s + "'s " + o.Type);
                        o.AddReferredToName(s + "' " + o.Type);
                    }
                }

                ReferredToNames.RemoveAll(s => string.IsNullOrEmpty(s));
            }
            else
            {
                if (Game1.LoadedArchitects.Count > 0)
                {
                    foreach ((Architect, int) a in Game1.LoadedArchitects[Game1.ArchitectIndex].ArchitectOpinions)
                    {
                        if (a.Item1 == this)
                        {
                            PlayerKnowsArch = true;
                        }
                    }

                    if (PlayerKnowsArch)
                    {
                        AddReferredToName(Name + ", dead.");
                        AddReferredToName("dead " + Profession);
                    }
                    else
                    {
                        AddReferredToName("dead " + Profession);
                    }

                }
            }

            foreach (Object o in Inventory)
            {
                o.UpdateNames();
            }
            foreach (Object o in BodyParts)
            {
                o.UpdateNames();
            }
            foreach (Object o in Clothing)
            {
                o.UpdateNames();
            }
            OffHeldObject?.UpdateNames();
            MainHeldObject?.UpdateNames();



            if (Game1.SplitMode)
            {
                if (ReferredToNames.Count > 0)
                {
                    string firstName = ReferredToNames[0];
                    ClearReferredToNames();
                    AddReferredToName($"{firstName} ({ID})");
                    AddReferredToName(ID.ToString());
                    AddReferredToName(firstName);
                }
            }
            AddReferredToName(ID.ToString());

            AddReferredToName(FalsifiedName);
        }
























        public void UpdateChildrenLocationsOnOneChildDeath()
        {
            if (this.IsCalamity)
            {
                // Iterate through all Architects in the Calamity list
                foreach (Architect a in Game1.GameWorld.Calamity)
                {
                    // Check if the current Architect is a direct subordinate of this
                    if (a.Master == this && a.IsAlive)
                    {
                        // Update the location of all direct and indirect subordinates
                        UpdateSubordinatesMigrationLocation(a, this);
                    }
                }
            }
        }

        // Recursive function to update the migration location for all subordinates
        public void UpdateSubordinatesMigrationLocation(Architect architect, Architect newMaster)
        {
            // Set the migration location to the new master's location
            architect.NextMigrationLocation = newMaster.Location;

            // Recursively update for all direct subordinates
            foreach (Architect a in Game1.GameWorld.Calamity)
            {
                if (a.Master == architect)
                {
                    UpdateSubordinatesMigrationLocation(a, newMaster);
                }
            }
        }















        public void AnnounceToParty(string announcement, Color color, List<Entity> Entities)
        {
            foreach (Architect a in Game1.GamePlayerParty.Architects)
            {
                if (a.Room == Room && a.Block == Block)
                {
                    Game1.MakeObservation(announcement, color, Entities);
                    break;
                }
            }
        }

        public List<Attack> UpdateSelfActionsAndSuch()
        {

            //reset imbuements

            ExtraShieldEffectiveness = 0;
            ExtraAttackPower = 0;
            ExtraDodgeChance = 0;
            ExtraRedirectionChance = 0;
            ExtraBashingResistance = 0;
            ExtraPiercingResistance = 0;
            ExtraSlashingResistance = 0;
            ExtraScourgingResistance = 0;
            ExtraStealth = 0;
            ExtraEnergyRegen = 0;

            //description pls

            if (Block == null && Room == null)
            {
                //idk whats happening but I actually want to play my game;
                return new List<Attack>();
            }

            foreach (Object b in BodyParts)
            {
                b.Owner = this;
            }

            List<Architect> ArchitectsToUse = (Room != null) ? Room.Architects : Block.Architects;
            List<Attack> Attacks = new List<Attack>();

            //distancing

            // Assume Distances is a Dictionary<Architect, int> for better performance
            Distances ??= new Dictionary<Architect, int>();

            HashSet<Architect> currentArchitects = new HashSet<Architect>(Room?.Architects ?? Block.Architects);

            // Remove outdated distances and update or add new distances
            foreach (var architect in Distances.Keys.Except(currentArchitects).ToList())
            {
                Distances.Remove(architect);
            }

            foreach (Architect a in currentArchitects)
            {
                if (!Distances.TryGetValue(a, out int distanceThisToA))
                {
                    if (!a.Distances.TryGetValue(this, out int distanceAToThis))
                    {
                        // Generate a new random distance if neither has a distance
                        distanceThisToA = Game1.r.Next(2, 6);
                    }
                    else
                    {
                        // Use the distance from a to this
                        distanceThisToA = distanceAToThis;
                    }

                    // Update reciprocal distance
                    a.Distances[this] = distanceThisToA;
                }
                else if (a.Distances.TryGetValue(this, out int distanceAToThis))
                {
                    // Both have a distance, use the lower one
                    distanceThisToA = Math.Min(distanceThisToA, distanceAToThis);
                    a.Distances[this] = distanceThisToA;
                }

                Distances[a] = distanceThisToA;
            }



            if (Pain > 100)
            {
                this.UnconsciousCycles = 500;

                this.Pain = 50;

                AnnounceToParty(this.ReferredToNames[0] + " goes unconscious in pain.", Color.DarkRed, new List<Entity>() { this });
            }
            else if (Game1.r.Next(1, 1000) < (Pain - Focus * 10) && !Race.Name.StartsWith("shade") && IsAlive)
            {
                AnnounceToParty(this.ReferredToNames[0] + " falters in pain!", Color.DarkRed, new List<Entity>() { this });
                CooldownCycles += 5;
            }

            if (Pain > 0 && Game1.r.Next(10) == 0)
            {
                Pain -= 1;
            }


            if (!BroadcastedDeathMessage && IsAlive == false && DeathCause != "")
            {
                int Month = ((int)Math.Round((decimal)(Location.Region.World.Cycle / 24192000)) % 12) + 1;
                int Year = (int)Math.Round((decimal)(Location.Region.World.Cycle / 290304000), MidpointRounding.ToZero);
                string Date = "(" + Month + "/" + Year + ")";

                BroadcastedDeathMessage = true;

                if (Location != null)
                {
                    Location.LocationHistoricalEvents.Add(Date + " " + Name + " " + DeathCause + " in " + Location.Name + ".");
                    Location.Region.World.HistoricalEvents.Add(Date + " " + Name + " " + DeathCause + " in " + Location.Name + ".");
                }
                else
                {
                    Game1.GameWorld.HistoricalEvents.Add(Date + " " + Name + " " + DeathCause + ".");
                }
            }

            //lose all combat cycls if you are out of combat.

            bool NoOneAliveAngy = true;

            if (Game1.GamePlayerParty.Architects.Contains(this))
            {
                foreach (Architect a in ArchitectsToUse)
                {
                    if (!Game1.GamePlayerParty.Architects.Contains(a) && a.IsAlive)
                    {
                        NoOneAliveAngy = false;
                        break;
                    }
                }
                if (NoOneAliveAngy)
                {
                    CombatCycles = 0;
                }
            }

            if (CanBeNotOnGround() == false && OnGround == false)
            {
                OnGround = true;
                AnnounceToParty(ReferredToNames[0] + " has fallen on the ground.", Color.Cyan, new List<Entity>() { this });
            }

            //die lmaoooo

            bool allNecessaryPartsIntact = Race.NecessaryBodyParts.All(requiredType => BodyParts.Any(bp => bp.Type == requiredType && bp.Integrity > 0));

            if (!allNecessaryPartsIntact && Game1.GameWorld.Cycle % 20 == 0)
            {
                Bleeding += 10;
                if (Game1.GamePlayerParty.Architects.Contains(this))
                {
                    AnnounceToParty(this.Name + " is critically wounded and bleeding heavily!", Color.Red, new List<Entity>() { this });
                }
            }

            if (Energy <= 0 && IsAlive == true)
            {
                bool canRevitalize = false;

                int currentYear = (int)(Game1.GameWorld.Cycle / 290304000);
                int currentMonth = ((int)(Game1.GameWorld.Cycle / 24192000) % 12) + 1;
                int currentDay = ((int)(Game1.GameWorld.Cycle / 864000) % 30) + 1;
                string currentDate = $"{currentYear:D4}-{currentMonth:D2}-{currentDay:D2}";

                if (PathOfDeathLevel >= 8)
                {
                    if (RevitalizedDates.Count == 0 || !RevitalizedDates.Contains(currentDate))
                    {
                        DateTime lastRevitalizeDate = DateTime.MinValue;
                        if (RevitalizedDates.Count > 0)
                        {
                            string[] lastDateParts = RevitalizedDates[RevitalizedDates.Count - 1].Split('-');
                            lastRevitalizeDate = new DateTime(int.Parse(lastDateParts[0]), int.Parse(lastDateParts[1]), int.Parse(lastDateParts[2]));
                        }

                        DateTime currentDateTime = new DateTime(currentYear, currentMonth, currentDay);
                        if ((currentDateTime - lastRevitalizeDate).TotalDays >= 7)
                        {
                            canRevitalize = true;
                        }
                    }
                }

                if (canRevitalize)
                {
                    AnnounceToParty(this.Name + " revitalizes in a dark cloud!", Color.DarkRed, new List<Entity>() { this });
                    RevitalizedDates.Add(currentDate);
                    Energy = 30;
                }
                else
                {
                    IsAlive = false;
                    DropInventory(false);

                    if (Game1.GamePlayerParty.Architects.Contains(this))
                    {
                        AnnounceToParty(this.Name + " has fallen. ", Color.Red, new List<Entity>() { this });
                        Game1.GamePlayerParty.Architects.Remove(this);
                    }
                    else
                    {
                        AnnounceToParty(this.Name + " has fallen. ", Color.Goldenrod, new List<Entity>() { this });
                        if (Master != null)
                        {
                            switch (new Random().Next(1, 6))
                            {
                                case 1:
                                    AnnounceToParty(this.Name + ": " + Master.Name + ", forgive me... I could not succeed...", Color.Goldenrod, new List<Entity>() { this, Master });
                                    break;
                                case 2:
                                    AnnounceToParty(this.Name + ": " + Master.Name + ", my journey ends here...", Color.Goldenrod, new List<Entity>() { this, Master });
                                    break;
                                case 3:
                                    AnnounceToParty(this.Name + ": " + Master.Name + ", alas, I have fallen...", Color.Goldenrod, new List<Entity>() { this, Master });
                                    break;
                                case 4:
                                    AnnounceToParty(this.Name + ": " + Master.Name + ", I apologize, I've let you down...", Color.Goldenrod, new List<Entity>() { this, Master });
                                    break;
                                case 5:
                                    AnnounceToParty(this.Name + ": " + Master.Name + ", the end has come for me...", Color.Goldenrod, new List<Entity>() { this, Master });
                                    break;
                            }

                            AnnounceToParty("You sense something odd about the name " + Master.Name + "...", Color.Aqua, new List<Entity>() { Master });
                            AnnounceToParty("[Intrigue Updated]", Color.Aqua, new List<Entity>());

                            foreach (Architect a in Game1.GamePlayerParty.Architects)
                            {
                                if (a.District == District)
                                {
                                    a.Intrigue.Add((Name + " said something about " + Master.Name + ".", Master));
                                    a.Intrigue.Add(("Perhaps someone can tell us more.", Master));
                                }
                            }

                            Master.UpdateChildrenLocationsOnOneChildDeath();
                        }
                        else if (this == Game1.GameWorld.Calamity[0])
                        {
                            AnnounceToParty(this.Name + ": So... This is how it ends...", Color.PaleGoldenrod, new List<Entity>());
                            AnnounceToParty(this.Name + ": My legacy, ending just like that, to a pitiful fool such as yourself.", Color.PaleGoldenrod, new List<Entity>());
                            AnnounceToParty(this.Name + ": My ideology must live on though...", Color.PaleGoldenrod, new List<Entity>());
                            AnnounceToParty(this.Name + ": Won't it...?", Color.PaleGoldenrod, new List<Entity>());
                            AnnounceToParty("The primeval source of evil throughout the land, " + this.Name + ", has finally fallen. The continent is filled with rest and hope.", Color.Coral, new List<Entity>() { this });

                            foreach (Architect a in Game1.GameWorld.Calamity)
                            {
                                if (a.IsAlive)
                                {
                                    a.Master = null;
                                    a.MasterRelation = "";

                                    int Month = ((int)Math.Round((decimal)(Location.Region.World.Cycle / 24192000)) % 12) + 1;
                                    int Year = (int)Math.Round((decimal)(Location.Region.World.Cycle / 290304000), MidpointRounding.ToZero);

                                    string Date = "(" + Month + "/" + Year + ")";

                                    Game1.GameWorld.HistoricalEvents.Add(Date + " " + a.Name + " left behind the calamity that " + this.Name + " had caused.");
                                }
                            }

                            Game1.GameWorld.Calamity.Clear();
                        }

                    }

                    // Nearby people gain stuff when you die
                    foreach (Architect a in Room != null ? Room.Architects : Block.Architects)
                    {
                        if (a != this)
                        {
                            AnnounceToParty(a.Name + " has absorbed some of their essence! ", Color.PaleGoldenrod, new List<Entity>() { a });
                            a.Energy += 20;
                            a.CombatCycles = 0;

                            if (a.Level < Level)
                            {
                                AnnounceToParty(a.Name + " has defeated a powerful foe and has become stronger...", Color.PaleGoldenrod, new List<Entity>() { a });
                                a.Level++;
                                a.SpendableLevels++;
                            }
                        }
                    }
                }
            }



            if (Energy > MaxEnergy())
            {
                Energy = MaxEnergy();
            }

            if (Bleeding > 0)
            {
                if (Game1.r.Next(20) == 0)
                {
                    Energy -= Bleeding;

                    if (Energy <= 0)
                    {
                        DeathCause = "bled to death";
                    }

                    Bleeding -= 1;
                }
            }

            if (CombatCycles == 0 && Energy < MaxEnergy())
            {
                Energy += (decimal)(0.05);
            }

            if (HalfFocusTicks > 0)
            {
                HalfFocusTicks--;
            }

            //racial benefits

            if (Race == Game1.GameWorld.GetRace("nightfell"))
            {
                ExtraAttackPower += 10;
            }
            else if (Race == Game1.GameWorld.GetRace("luminarch"))
            {
                ExtraAttackPower -= 10;
            }

            //path of shadow

            if (PathOfShadowLevel >= 2)
            {
                ExtraStealth += 10;
            }
            if (Invisible && IsAlive)
            {

                //lose this one only if you are low level
                if (PathOfShadowLevel < 8)
                {
                    Energy -= (decimal)(0.2);
                }

                //but then this one lmaoaooooo
                Energy -= (decimal)(0.2);

                if (Energy <= 0)
                {
                    DeathCause = "was lost to the shadows";
                    AnnounceToParty(this.ReferredToNames[0] + " was lost to the shadows.", Color.DarkRed, new List<Entity>() { this });
                }


                if ((Clothing.Count == 0 && Inventory.Count == 0) || PathOfShadowLevel >= 6)
                {
                    ExtraStealth += 1000;
                }
            }

            //test hand items to see if they will burn you
            bool isSameRoomOrBlockMatch = Game1.GamePlayerParty.Architects.Any(architect => architect.Room == this.Room)
                              || Game1.GamePlayerParty.Architects.Any(architect => architect.Block == this.Block);



            //drop items that are too hot

            if (PathOfHeatLevel < 4)
            {
                if (OffHeldObject != null && (OffHeldObject.FireCycles > 0 || OffHeldObject.HeatInCelsius >= 30 + (Focus * 5)))
                {
                    if (isSameRoomOrBlockMatch)
                    {
                        Game1.MakeObservation("The " + OffHeldObject.ReferredToNames[0] + " in " + ReferredToNames[0] + "'s " + Race.OffInteractionAppendage + " is too hot! " + ReferredToNames[0] + " drops it in pain.", Color.Orange, new List<Entity>() { OffHeldObject, this, this });
                    }
                    if (Room != null)
                    {
                        Room.Objects.Add(OffHeldObject);
                        OffHeldObject = null;
                    }
                    else
                    {
                        Block.Objects.Add(OffHeldObject);
                        OffHeldObject = null;
                    }

                }
                if (MainHeldObject != null && (MainHeldObject.FireCycles > 0 || MainHeldObject.HeatInCelsius >= 30 + (Focus * 5)))
                {
                    if (isSameRoomOrBlockMatch)
                    {
                        Game1.MakeObservation("The " + MainHeldObject.ReferredToNames[0] + " in " + ReferredToNames[0] + "'s " + Race.MainInteractionAppendage + " is too hot! " + ReferredToNames[0] + " drops it in pain.", Color.Orange, new List<Entity>() { MainHeldObject, this, this });
                    }
                    if (Room != null)
                    {
                        Room.Objects.Add(MainHeldObject);
                        MainHeldObject = null;
                    }
                    else
                    {
                        Block.Objects.Add(MainHeldObject);
                        MainHeldObject = null;
                    }
                }

                if (Game1.GameWorld.Cycle % 10 == 0)
                {
                    foreach (Object clothingItem in Clothing)
                    {
                        if (clothingItem.FireCycles > 0 || clothingItem.HeatInCelsius >= 30 + (Focus * 5))
                        {
                            Energy -= 1;

                            if (isSameRoomOrBlockMatch)
                            {
                                Game1.MakeObservation("The " + clothingItem.ReferredToNames[0] + " on " + ReferredToNames[0] + " is too hot, and is singing their skin!", Color.Orange, new List<Entity>() { clothingItem, this, this });
                            }

                            if (Energy <= 0)
                            {
                                DeathCause = "was burned to death by their clothing";
                            }
                        }
                    }
                }
            }


            double GetMaterialCoverageMultiplier(Material material)
            {
                switch (material.Type.ToLower())
                {
                    case "metal":
                        return 1.0;
                    case "cloth":
                        return 0.9;
                    case "wood":
                        return 0.8;
                    case "stone":
                        return 0.7;
                    case "glass":
                        return 0.5;
                    default:
                        return 1.0; // Default multiplier
                }
            }

            // Assuming each BodyPart object has a CoverageName property to store the name of the clothing item providing the most coverage

            // Modify the system to also track the clothing item that provides the highest coverage for each body part
            Dictionary<string, (int coverage, string itemName)> highestCoverageByItem = new Dictionary<string, (int coverage, string itemName)>();

            foreach (var clothingItem in this.Clothing)
            {
                // Determine the strongest material for this clothing item
                Material strongestMaterial = clothingItem.Materials.OrderByDescending(m => m.Toughness).First();

                // Calculate coverage values taking material into account
                foreach (var (bodyPart, coverage) in clothingItem.CoverageValues)
                {
                    double materialMultiplier = GetMaterialCoverageMultiplier(strongestMaterial);
                    int adjustedCoverage = (int)((strongestMaterial.Toughness / 2.0) * materialMultiplier * coverage);

                    if (highestCoverageByItem.ContainsKey(bodyPart))
                    {
                        if (highestCoverageByItem[bodyPart].coverage < adjustedCoverage)
                        {
                            highestCoverageByItem[bodyPart] = (adjustedCoverage, clothingItem.ReferredToNames[0]); // Update with higher coverage and item name
                        }
                    }
                    else
                    {
                        highestCoverageByItem.Add(bodyPart, (adjustedCoverage, clothingItem.ReferredToNames[0])); // Add new entry with coverage and item name
                    }
                }
            }

            foreach (var bodyPart in this.BodyParts)
            {
                if (highestCoverageByItem.TryGetValue(bodyPart.Type, out var highestCoverage))
                {
                    bodyPart.Coverage = highestCoverage.coverage;
                    bodyPart.CoverageName = highestCoverage.itemName; // Assign the name of the item providing the highest coverage
                }
            }


            if (CombatCycles > 0)
            {
                CombatCycles--;  // Regular decrement for being in combat

                bool closeEnemies = false;  // Flag to check for close enemies

                foreach (Architect a in ArchitectsToUse)
                {
                    // Check if the Architect is targeting this and within a distance of 5
                    if (a.TargetArchitect == this && (a.Task == "killtarget" || a.Task == "disabletarget") && a.GetDistance(this) <= 5)
                    {
                        closeEnemies = true;
                        break;
                    }
                }

                // If there are no close enemies, decrement CombatCycles again
                if (!closeEnemies && CombatCycles > 0)
                {
                    CombatCycles--;
                }
            }


            CurrentlyActiveImbuements = new List<Imbuement>();

            foreach (Object o in Clothing)
            {
                o.UpdateSelfActionsAndSuch();
                CurrentlyActiveImbuements.AddRange(o.Imbuements);
            }

            foreach (Object o in Inventory)
            {
                o.UpdateSelfActionsAndSuch();
                if (!o.IsWearable)
                {
                    CurrentlyActiveImbuements.AddRange(o.Imbuements);
                }
            }

            if (OffHeldObject != null)
            {
                OffHeldObject.UpdateSelfActionsAndSuch();
                CurrentlyActiveImbuements.AddRange(OffHeldObject.Imbuements);
            }
            if (MainHeldObject != null)
            {
                MainHeldObject.UpdateSelfActionsAndSuch();
                CurrentlyActiveImbuements.AddRange(MainHeldObject.Imbuements);
            }

            foreach (Architect a in MeldedShibas)
            {
                int attributeToIncrease = Game1.r.Next(0, 9);
                switch (attributeToIncrease)
                {
                    case 0:
                        ExtraShieldEffectiveness += 1;
                        break;
                    case 1:
                        ExtraAttackPower += 1;
                        break;
                    case 2:
                        ExtraDodgeChance += 1;
                        break;
                    case 3:
                        ExtraRedirectionChance += 1;
                        break;
                    case 4:
                        ExtraBashingResistance += 1;
                        break;
                    case 5:
                        ExtraPiercingResistance += 1;
                        break;
                    case 6:
                        ExtraSlashingResistance += 1;
                        break;
                    case 7:
                        ExtraScourgingResistance += 1;
                        break;
                    case 8:
                        ExtraStealth += 1;
                        break;
                    default:
                        // Just in case of an unexpected value
                        break;
                }
            }

            if (PathOfBodyLevel >= 2)
            {
                if (!RecievedBodyPhysicalStatIncrease)
                {
                    AnnounceToParty(Name + " has gained increases to all physical stats.", Color.Pink, new List<Entity>() { this });
                    Strength++;
                    Dexterity++;
                    Agility++;
                    Focus++;
                    RecievedBodyPhysicalStatIncrease = true;
                }
            }
            else if (PathOfBodyLevel >= 4)
            {
                if (!RecievedBodyPhysicalStatIncreaseTwo)
                {
                    AnnounceToParty(Name + " has gained an increased agility!", Color.Pink, new List<Entity>() { this });
                    Agility++;
                    Agility++;
                    RecievedBodyPhysicalStatIncreaseTwo = true;
                }
            }

            //go ahead and apply the imbuement effect if its a passive imbuement, but if it isnt then wait for later
            foreach (Imbuement i in CurrentlyActiveImbuements)
            {
                if (!i.IsTrigger)
                {
                    bool MetCondition = false;

                    if (i.ConditionOrTrigger == "multipleenemies")
                    {
                        int Quota = 0;
                        if (Room != null)
                        {
                            foreach (Architect e in Room.Architects)
                            {
                                if (Game1.GamePlayerParty.Architects.Contains(this) && Game1.GamePlayerParty.Architects.Contains(e.TargetArchitect) && (e.Task == "killtarget" || e.Task == "disabletarget"))
                                {
                                    Quota++;
                                }
                            }
                        }
                        else
                        {
                            foreach (Architect e in Block.Architects)
                            {
                                if (Game1.GamePlayerParty.Architects.Contains(this) && Game1.GamePlayerParty.Architects.Contains(e.TargetArchitect) && (e.Task == "killtarget" || e.Task == "disabletarget"))
                                {
                                    Quota++;
                                }
                            }
                        }

                        if (Quota > 1)
                        {
                            MetCondition = true;
                        }
                    }
                    else if (i.ConditionOrTrigger == "grounded")
                    {
                        if (OnGround)
                        {
                            MetCondition = true;
                        }
                    }
                    else if (i.ConditionOrTrigger == "diminished")
                    {
                        if (Energy < i.FirstPower)
                        {
                            MetCondition = true;
                        }
                    }
                    else if (i.ConditionOrTrigger == "lowlight")
                    {
                        if (Room != null)
                        {
                            if (Room.Structure.LightLevelOf5 <= 3)
                            {
                                MetCondition = true;
                            }
                        }
                        else
                        {
                            if (Location.Region.World.IsNightTime())
                            {
                                MetCondition = true;
                            }
                        }
                    }
                    else if (i.ConditionOrTrigger == "stagnant")
                    {
                        if (CyclesSinceMoved >= 70)
                        {
                            MetCondition = true;
                        }
                    }
                    else if (i.ConditionOrTrigger == "maxenergy")
                    {
                        if (Energy == MaxEnergy())
                        {
                            MetCondition = true;
                        }
                    }

                    if (MetCondition)
                    {
                        i.IsSatisfied = true;

                        switch (i.BuffOrResult)
                        {
                            case "+attack":
                                ExtraAttackPower += i.SecondPower; 
                                break;
                            case "+shield":
                                ExtraShieldEffectiveness += i.SecondPower;
                                break;
                            case "+dodge":
                                ExtraDodgeChance += i.SecondPower;
                                break;
                            case "+redirection":
                                ExtraRedirectionChance += i.SecondPower;
                                break;
                            case "+bash":
                                ExtraBashingResistance += i.SecondPower;
                                break;
                            case "+pierce":
                                ExtraPiercingResistance += i.SecondPower;
                                break;
                            case "+slash":
                                ExtraSlashingResistance += i.SecondPower;
                                break;
                            case "+scourge":
                                ExtraScourgingResistance += i.SecondPower;
                                break;
                            case "+stealth":
                                ExtraStealth += 5;
                                break;
                            case "+regen":
                                ExtraEnergyRegen += i.SecondPower;
                                break;
                            default:
                                // Handle unknown BuffOrResult
                                break;
                        }
                    }
                    else
                    {
                        i.IsSatisfied = false;
                    }
                }
            }


            //regenerate

            if(Game1.GameWorld.Cycle % 10 == 0)
            {
                Energy = Math.Min(MaxEnergy(), Energy + ExtraEnergyRegen);
            }

            //Handle A variety of On Tick Effects

            if (WetCycles > 0)
            {
                FireSeconds = 0;
                WetCycles--;
            }
            if (FireSeconds > 0 && Game1.GameWorld.Cycle % 10 == 0)
            {
                if (PathOfHeatLevel < 8)
                {
                    Energy -= FireSeconds;

                    if (Energy <= 0)
                    {
                        DeathCause = "burned to death";
                    }
                }

                FireSeconds--;
            }
            if (BlindCycles > 0)
            {
                BlindCycles--;
            }
            if (DismissalCycles > 0)
            {
                DismissalCycles--;
            }
            if (DestabilizedCycles > 0)
            {
                DestabilizedCycles--;
            }
            if (UnconsciousCycles > 0)
            {
                UnconsciousCycles--;
            }
            if (RadiantCycles > 0)
            {
                RadiantCycles--;
            }
            if (CloakCycles > 0)
            {
                ExtraStealth += 40;
                CloakCycles--;
            }

            if (YLevelInFeet > 0)
            {
                // Adjusting the velocity based on gravity
                YVelocity += (int)(Math.Round(Math.Min(0, (YLevelInFeet - 300) * 0.02f)));

                YLevelInFeet = Math.Min(0, YLevelInFeet - 300);

                if (YLevelInFeet == 0)
                {
                    //impact
                    int DamageInstances = (int)(YVelocity / (0.8));
                    YVelocity = 0;

                    for (int i = 0; i < DamageInstances; i++)
                    {
                        Object o = this.BodyParts[Game1.r.Next(BodyParts.Count)];

                        Game1.MakeObservation(o.ReferredToNames[0] + " is crushed by the impact!", Color.Orange, new List<Entity>() { o });
                        o.Integrity = Math.Max(0, o.Integrity - 25); // Ensure integrity doesn't go below zero
                    }
                }
            }


            IEnumerable<Architect> architects = (Room == null) ? Block.Architects : Room.Architects;


            if ((IsAlive && !Game1.GamePlayerParty.Architects.Contains(this) && UnconsciousCycles == 0 && HoldCycles == 0))
            {
                // Clamp function to ensure a value stays within a specified range
                int Clamp(int value, int min, int max)
                {
                    return Math.Max(min, Math.Min(max, value));
                }

                // Respond to messages
                foreach (Message m in MessagesNotRespondedTo)
                {
                    if (messageDatabase.ContainsKey(m.MessageContent) && m.MessageType == "question")
                    {
                        // Message has been seen before, respond with the same response
                        AnnounceToParty(ReferredToNames[0] + ": " + responseDatabase[m.MessageContent], new Color(0, 255, 0), new List<Entity> { this }.Union(m.Subjects).ToList());
                        continue;
                    }

                    // Determine if the receiver can receive the message based on the new conditions
                    bool bothAreSapient = (Game1.GameWorld.HumanoidRaces.Contains(m.Sender.Race) ||
                                           Game1.GameWorld.ExtraRaces.Contains(m.Sender.Race)) &&
                                          (Game1.GameWorld.HumanoidRaces.Contains(this.Race) ||
                                           Game1.GameWorld.ExtraRaces.Contains(this.Race));

                    bool canReceiveMessage = bothAreSapient || this.PathOfLifeLevel >= 4 || m.Sender.PathOfLifeLevel >= 4;

                    if ((CombatCycles > 0 && !(m.MessageID == "surrender" || m.MessageID == "demand_surrender")) || !canReceiveMessage)
                    {
                        AnnounceToParty(ReferredToNames[0] + " does not reply.", Color.Yellow, new List<Entity>() { this });
                        continue;
                    }

                    int senderOpinion = GetOpinion(m.Sender);
                    int baseChanceToTruth = 60; // Increased base chance to tell the truth
                    int baseChanceToMakeUp = 20; // Decreased base chance to make up a lie
                    int baseChanceToClaimIgnorance = 10;
                    int baseChanceToDerail = 5;
                    int baseChanceToFlatter = 5;

                    // Adjust baseChanceToTruth by Sender.Charisma
                    baseChanceToTruth += m.Sender.Charisma * 3;

                    if (m.Receiver.ArchitectsIWillTellTruthTo.Contains(m.Sender) || m.Sender.ArchitectsWhoSurrenderedToMe.Contains(m.Receiver) || m.MessageContent.StartsWith("Would you tell me where I can find"))
                    {
                        // Always respond truthfully if the sender is in ArchitectsIWillTellTruthTo or if it's a request for directions, or if theyre surrendered always comply.
                        baseChanceToTruth = 100;
                        baseChanceToMakeUp = 0;
                        baseChanceToClaimIgnorance = 0;
                        baseChanceToDerail = 0;
                        baseChanceToFlatter = 0;
                    }
                    else
                    {
                        if (senderOpinion > 50)
                        {
                            baseChanceToTruth += (senderOpinion - 50) / 2;
                            baseChanceToMakeUp -= (senderOpinion - 50) / 4;
                            baseChanceToClaimIgnorance -= (senderOpinion - 50) / 8;
                        }
                        else if (senderOpinion < -50)
                        {
                            baseChanceToTruth -= (-senderOpinion - 50) / 2;
                            baseChanceToMakeUp += (-senderOpinion - 50) / 4;
                            baseChanceToClaimIgnorance += (-senderOpinion - 50) / 8;
                        }
                        else if (senderOpinion == 0)
                        {
                            baseChanceToMakeUp = 5; // Adjust the chance to make up a lie to 5% when the opinion is 0
                        }

                        int focus = m.Receiver.Focus;
                        int charisma = m.Receiver.Charisma;

                        baseChanceToDerail -= (7 - focus) * 2;
                        baseChanceToFlatter += charisma;

                        baseChanceToTruth = Clamp(baseChanceToTruth, 0, 100);
                        baseChanceToMakeUp = Clamp(baseChanceToMakeUp, 0, 100);
                        baseChanceToClaimIgnorance = Clamp(baseChanceToClaimIgnorance, 0, 100);
                        baseChanceToDerail = Clamp(baseChanceToDerail, 0, 100);
                        baseChanceToFlatter = Clamp(baseChanceToFlatter, 0, 100);
                    }

                    // Specific conditions based on message content
                    bool conditionMet = true;
                    switch (m.MessageID)
                    {
                        case "ask_them_join":
                            if (m.Receiver.Group != null || (m.Sender.Group != null && m.Sender.Group.Architects.Count >= 4))
                            {
                                conditionMet = false;
                                baseChanceToTruth = 0;
                                baseChanceToClaimIgnorance = 100;
                            }
                            else if (m.Sender.Group == null)
                            {
                                conditionMet = false;
                                baseChanceToTruth = 0;
                                baseChanceToClaimIgnorance = 100;
                            }
                            break;

                        case "ask_me_join":
                            if (m.Receiver.Group == null || (m.Receiver.Group != null && m.Receiver.Group.Architects.Count >= 4))
                            {
                                conditionMet = false;
                                baseChanceToTruth = 0;
                                baseChanceToClaimIgnorance = 100;
                            }
                            else if (m.Sender.Group != null)
                            {
                                conditionMet = false;
                                baseChanceToTruth = 0;
                                baseChanceToClaimIgnorance = 100;
                            }
                            break;

                        case "demand_item":
                            var demandedItem = m.Subjects[0] as Object;
                            if (demandedItem == null ||
                                (!m.Receiver.Inventory.Contains(demandedItem) &&
                                 !m.Receiver.Clothing.Contains(demandedItem) &&
                                 m.Receiver.OffHeldObject != demandedItem &&
                                 m.Receiver.MainHeldObject != demandedItem))
                            {
                                conditionMet = false;
                                baseChanceToTruth = 0;
                                baseChanceToClaimIgnorance = 100;
                            }
                            else if (m.Receiver.ArchitectsWhoSurrenderedToMe != null && m.Receiver.ArchitectsWhoSurrenderedToMe.Contains(m.Sender))
                            {
                                baseChanceToClaimIgnorance = 0;
                                baseChanceToTruth = 100;
                            }
                            break;

                        case "ask_ruler":
                            if (m.Sender.Location.Government == null)
                            {
                                conditionMet = false;
                                baseChanceToTruth = 0;
                                baseChanceToClaimIgnorance = 100;
                            }
                            break;

                        case "ask_trade":
                            var nearestMarket = m.Sender.Block.FindNearestThing("market");
                            if (nearestMarket.Item2 != m.Receiver.Location)
                            {
                                conditionMet = false;
                                baseChanceToTruth = 0;
                                baseChanceToClaimIgnorance = 100;
                            }
                            break;

                        default:
                            // Other specific conditions can be added here
                            break;
                    }
                    if (!conditionMet)
                    {
                        // Respond with the IgnorantResponse if conditions are not met
                        AnnounceToParty(ReferredToNames[0] + ": " + m.IgnorantResponse, new Color(255, 0, 0), new List<Entity>() { this });
                        Game1.MessageWorldEdit(m.Sender, this, m.MessageID, m.Subjects, m.IgnorantResponse, m.StoredRevealLocations);
                        continue;
                    }

                    int randomNumber = Game1.r.Next(1, 101);
                    string response;
                    Color ResponseColor = Game1.GamePlayerParty.Architects.Contains(m.Sender) ? new Color(0, 255, 0) : new Color(0, 75, 0);

                    if (randomNumber <= baseChanceToTruth)
                    {
                        response = m.PositiveResponse;
                    }
                    else if (randomNumber <= baseChanceToTruth + baseChanceToMakeUp)
                    {
                        response = m.DirectRefusalResponse;
                    }
                    else if (randomNumber <= baseChanceToTruth + baseChanceToMakeUp + baseChanceToClaimIgnorance)
                    {
                        response = m.IgnorantResponse;
                    }
                    else if (randomNumber <= baseChanceToTruth + baseChanceToMakeUp + baseChanceToClaimIgnorance + baseChanceToDerail)
                    {
                        response = m.DerailingResponse;
                    }
                    else
                    {
                        response = m.FlatteringResponse;
                    }

                    AnnounceToParty(ReferredToNames[0] + ": " + response, ResponseColor, new List<Entity> { this }.Union(m.Subjects).ToList());

                    // Store the message and response
                    messageDatabase[m.MessageContent] = m.Subjects;
                    responseDatabase[m.MessageContent] = response;

                    Game1.MessageWorldEdit(m.Sender, m.Receiver, m.MessageID, m.Subjects, response, m.StoredRevealLocations);

                    CooldownCycles += (int)Math.Round(30 / Speed());
                }

                MessagesNotRespondedTo.Clear();
            }


            //actions

            if (IsAlive && CooldownCycles == 0 && !Game1.GamePlayerParty.Architects.Contains(this) && UnconsciousCycles == 0 && HoldCycles == 0 && Race != Game1.GameWorld.GetRace("moari"))
            {
                //opinions

                //have an opinion with everyone in the room


                foreach (Architect a in architects)
                {
                    if (GetOpinion(a) >= 0 && a != this && !(Game1.r.Next(0, 100) < a.ExtraStealth && !a.ArchitectsWhoSurrenderedToMe.Contains(this) && !this.ArchitectsWhoISurrenderedTo.Contains(a)))
                    {
                        int FinalOpinion = 0;
                        bool isOpposed = false;

                        if (OppositionTags.Contains("humanoids") && Location.Region.World.HumanoidRaces.Contains(a.Race))
                        {
                            FinalOpinion = Math.Min(FinalOpinion, -247);
                            isOpposed = true;
                        }

                        if (OppositionTags.Contains("alllife") && !a.Race.Name.Contains("shade"))
                        {
                            FinalOpinion = Math.Min(FinalOpinion, -247);
                            isOpposed = true;
                        }

                        if (OppositionTags.Contains("allsentient"))
                        {
                            FinalOpinion = Math.Min(FinalOpinion, -247);
                            isOpposed = true;
                        }

                        if (OppositionTags.Contains("allunalike") && a.Race != Race)
                        {
                            var hypernexus = Game1.GameWorld.GetRace("hypernexus");
                            var photonexus = Game1.GameWorld.GetRace("photonexus");
                            var isofractal = Game1.GameWorld.GetRace("isofractal");
                            var icosidodecahedron = Game1.GameWorld.GetRace("icosidodecahedron");
                            var shadebeast = Game1.GameWorld.GetRace("shadebeast");
                            var shade = Game1.GameWorld.GetRace("shade");
                            var shadeheart = Game1.GameWorld.GetRace("shadeheart");

                            // Check for specific race exceptions
                            bool exceptionFound = (Race == hypernexus && a.Race == photonexus) ||
                                                  (Race == photonexus && a.Race == hypernexus) ||
                                                  (Race == isofractal && a.Race == icosidodecahedron) ||
                                                  (Race == icosidodecahedron && a.Race == isofractal) ||
                                                  (Race == shadebeast && (a.Race == shade || a.Race == shadeheart)) ||
                                                  (Race == shade && (a.Race == shadebeast || a.Race == shadeheart)) ||
                                                  (Race == shadeheart && (a.Race == shadebeast || a.Race == shade));

                            if (!exceptionFound)
                            {
                                FinalOpinion = Math.Min(FinalOpinion, -247);
                                isOpposed = true;
                            }
                        }

                        if (OppositionTags.Contains("allevil") && (a.Race.Name.Contains("shade") || (a.Group != null && a.Group.Reputation < -40) || a.Reputation < -40))
                        {
                            FinalOpinion = Math.Min(FinalOpinion, -247);
                            isOpposed = true;
                        }

                        if (OppositionTags.Contains("intruders") && (a.HomeLocation != Location || (Game1.GamePlayerParty.CurrentEvent != null && (Game1.GamePlayerParty.CurrentEvent.GuarranteedArchitects.Contains(this) && !Game1.GamePlayerParty.CurrentEvent.GuarranteedArchitects.Contains(a)))))
                        {
                            FinalOpinion = Math.Min(FinalOpinion, -247);
                            isOpposed = true;
                        }

                        if (!isOpposed)
                        {
                            FinalOpinion = Math.Min(FinalOpinion, 1);
                        }

                        ChangeOpinion(a, FinalOpinion);
                    }

                    if (OppositionTags.Contains("indebted") && HomeStructure.MarketDebt <= -1 && Game1.GamePlayerParty.Architects.Contains(a) && a.Structure == null && Game1.r.Next(0, 100) < a.ExtraStealth == false)
                    {
                        ChangeOpinion(a, -247);
                    }
                }




                //delete known spells, race, compositions, etc.
                {
                    SpellsKnown.RemoveAll(item => Game1.GameWorld.DeletedSpells.Contains(item));
                    CultureBank.RemoveAll(item => Game1.GameWorld.DeletedCompositions.Contains(item));
                    Inventory.RemoveAll(item => Game1.GameWorld.DeletedObjects.Contains(item));
                    if (Game1.GameWorld.DeletedObjects.Contains(OffHeldObject))
                        OffHeldObject = null;
                    if (Game1.GameWorld.DeletedObjects.Contains(MainHeldObject))
                        MainHeldObject = null;
                    if (Game1.GameWorld.DeletedRaces.Contains(this.Race))
                    {
                        Race = Game1.GameWorld.GetRace("shade");
                        BodyParts.Clear();
                        AddBodyParts();
                    }
                }

                //stand up

                if (CanBeNotOnGround() == true && OnGround == true)
                {
                    OnGround = false;
                    CooldownCycles += (int)Math.Round((20 - Agility) * Speed());
                    AnnounceToParty(ReferredToNames[0] + " gets back up.", Color.Cyan, new List<Entity>() { this });
                }


                //attempt to surrender

                if (!Game1.GameWorld.Calamity.Contains(this) && CourageValue < 50 && Energy < 40 && CombatCycles > 0 && (Task == "killtarget" || Task == "disabletarget") && TargetArchitect != null && TargetArchitect.Block == this.Block && TargetArchitect.Room == this.Room && !ArchitectsWhoIAttemptedToSurrenderTo.Contains(TargetArchitect))
                {
                    ArchitectsWhoIAttemptedToSurrenderTo.Add(TargetArchitect);

                    bool canSendMessage = true;

                    // Both parties are in Talker category
                    if ((Game1.GameWorld.HumanoidRaces.Contains(this.Race) || Game1.GameWorld.ExtraRaces.Contains(this.Race)) &&
                        (Game1.GameWorld.HumanoidRaces.Contains(TargetArchitect.Race) || Game1.GameWorld.ExtraRaces.Contains(TargetArchitect.Race)))
                    {
                        canSendMessage = true;
                    }
                    // One party is not in Talker category
                    else if ((Game1.GameWorld.HumanoidRaces.Contains(this.Race) || Game1.GameWorld.ExtraRaces.Contains(this.Race)) &&
                             (!Game1.GameWorld.HumanoidRaces.Contains(TargetArchitect.Race) && !Game1.GameWorld.ExtraRaces.Contains(TargetArchitect.Race)) ||
                             (!Game1.GameWorld.HumanoidRaces.Contains(this.Race) && !Game1.GameWorld.ExtraRaces.Contains(this.Race)) &&
                             (Game1.GameWorld.HumanoidRaces.Contains(TargetArchitect.Race) || Game1.GameWorld.ExtraRaces.Contains(TargetArchitect.Race)))
                    {
                        if (this.PathOfLifeLevel < 4 && TargetArchitect.PathOfLifeLevel < 4)
                        {
                            canSendMessage = false;
                        }
                    }
                    // Both parties are in Silent category
                    else
                    {
                        canSendMessage = false;
                    }

                    if (canSendMessage)
                    {
                        CommandProcessor.SendMessage("surrender", this, TargetArchitect, new List<Entity> { }, Game1.GameWorld);
                    }
                }


                //send messages of your own
                if (Game1.r.Next(1, 15) < Charisma && this.Task != "killtarget" && this.Task != "disabletarget" && MessageCooldown == 0)
                {
                    var ArchList = Room != null ? Room.Architects : Block.Architects;

                    MessageCooldown += Game1.r.Next(50, 100);

                    // Filter out the current Architect instance from ArchList
                    var OtherArchitects = ArchList.Where(arch => arch != this && !(Game1.r.Next(0, 100) < arch.ExtraStealth)).ToList();

                    if (OtherArchitects.Count > 0)
                    {
                        Architect ChosenArchitect = OtherArchitects[Game1.r.Next(OtherArchitects.Count)];

                        bool isTargetPlayerArchitect = Game1.GamePlayerParty.Architects.Contains(ChosenArchitect);

                        // 50 percent chance to ignore messages to a player, so people will talk a lot more often but it won't be annoying
                        if (isTargetPlayerArchitect && Game1.r.Next(2) == 0)
                        {
                            // Skip sending message
                        }
                        else
                        {
                            // Check if sender can communicate with the target based on PathOfLifeLevel and race
                            bool canSendMessage = true;

                            // Both parties are in Talker category
                            if ((Game1.GameWorld.HumanoidRaces.Contains(this.Race) || Game1.GameWorld.ExtraRaces.Contains(this.Race)) &&
                                (Game1.GameWorld.HumanoidRaces.Contains(ChosenArchitect.Race) || Game1.GameWorld.ExtraRaces.Contains(ChosenArchitect.Race)))
                            {
                                canSendMessage = true;
                            }
                            // One party is not in Talker category
                            else if ((Game1.GameWorld.HumanoidRaces.Contains(this.Race) || Game1.GameWorld.ExtraRaces.Contains(this.Race)) &&
                                     (!Game1.GameWorld.HumanoidRaces.Contains(ChosenArchitect.Race) && !Game1.GameWorld.ExtraRaces.Contains(ChosenArchitect.Race)) ||
                                     (!Game1.GameWorld.HumanoidRaces.Contains(this.Race) && !Game1.GameWorld.ExtraRaces.Contains(this.Race)) &&
                                     (Game1.GameWorld.HumanoidRaces.Contains(ChosenArchitect.Race) || Game1.GameWorld.ExtraRaces.Contains(ChosenArchitect.Race)))
                            {
                                if (this.PathOfLifeLevel < 4 && ChosenArchitect.PathOfLifeLevel < 4)
                                {
                                    canSendMessage = false;
                                }
                            }
                            // Both parties are in Silent category
                            else
                            {
                                canSendMessage = false;
                            }

                            if (canSendMessage)
                            {
                                int Decider = Game1.r.Next(100); // Generate a random number between 0 and 99
                                string MType = "";

                                if (Decider < 10) // Greet: 10%
                                {
                                    MType = "greet";
                                }
                                else if (Decider < 20) // Goodbye: 10%
                                {
                                    MType = "farewell";
                                }
                                else if (Decider < 25) // Thank: 5%
                                {
                                    MType = "thank";
                                }
                                else if (Decider < 30) // Apologize: 5%
                                {
                                    MType = "apologize";
                                }
                                else if (Decider < 35) // Ask Health: 5%
                                {
                                    MType = "ask_health";
                                }
                                else if (Decider < 45) // Ask News: 10%
                                {
                                    MType = "ask_news";
                                }
                                else if (Decider < 55) // Ask History: 10%
                                {
                                    MType = "ask_history";
                                }
                                else if (Decider < 65) // Ask Opinion: 10%
                                {
                                    MType = "ask_opinion";
                                }
                                else if (Decider < 85) // Ask Advice: 20%
                                {
                                    MType = "ask_advice";
                                }
                                else if (Decider < 95) // Tell Story: 10%
                                {
                                    MType = "tell_story_about";
                                }
                                else // Compliment: 5%
                                {
                                    MType = "compliment";
                                }

                                //if you're asking news, history, or telling a story don't include domains. otherwise do.

                                List<Entity> AllImportantEntities = new List<Entity>();

                                if (!new List<string>() { "tell_story_about", "ask_news", "ask_history" }.Contains(MType) && Game1.r.Next(2) == 0)
                                {
                                    //chance to add in domains IF you can talk about a domain

                                    foreach (Entity domain in Game1.GameWorld.Domains)
                                    {
                                        AllImportantEntities.Add(domain);
                                    }
                                }
                                else
                                {
                                    AllImportantEntities = (Game1.GameWorld.AllLocations
                                    .SelectMany(location => location.AllStructures.Concat(new List<Entity> { location }))
                                    .Concat(Game1.GameWorld.AllArchitects)).ToList<Entity>();
                                }

                                CommandProcessor.SendMessage(MType, this, ChosenArchitect, new List<Entity> { AllImportantEntities[Game1.r.Next(AllImportantEntities.Count)] }, Game1.GameWorld);
                            }
                        }
                    }
                }
                else if (MessageCooldown > 0)
                {
                    MessageCooldown--;
                }

                //THIS IS SPECIFICALLY FOR FINDING A NEW TARGET.

                Architect KillTarget = null;
                Architect DisableTarget = null;

                //stop tryin to kill iftheyre allready dead lmao
                if ((Task == "killtarget" || Task == "disabletarget") && TargetArchitect.IsAlive == false)
                {
                    Task = "";
                    TargetArchitect = null;
                }

                //set new kill target
                if (Room != null)
                {
                    foreach (Architect a in Room.Architects)
                    {
                        if (GetOpinion(a) < -100 && (Game1.r.Next(100) > a.ExtraStealth))
                        {
                            KillTarget = a;
                        }
                        else if (GetOpinion(a) < -50 && (Game1.r.Next(100) > a.ExtraStealth))
                        {
                            DisableTarget = a;

                        }
                    }
                }
                else
                {
                    foreach (Architect a in Block.Architects)
                    {
                        if (GetOpinion(a) < -100 && (Game1.r.Next(100) > a.ExtraStealth))
                        {
                            KillTarget = a;
                        }
                        else if (GetOpinion(a) < -50 && (Game1.r.Next(100) > a.ExtraStealth))
                        {
                            DisableTarget = a;
                        }
                    }
                }

                //set target properly

                if (KillTarget != null)
                {
                    Task = "killtarget";
                    CyclesLeftInTask = 500;
                    TargetArchitect = KillTarget;
                }
                else if (DisableTarget != null)
                {
                    Task = "disabletarget";
                    CyclesLeftInTask = 500;
                    TargetArchitect = DisableTarget;
                }



                int ChangeInX = 0;
                int ChangeInZ = 0;
                string AlternateMove = "";


                //set task if you need to, or copy your leader's task :)

                if (Group != null && Group.Leader.Loaded)
                {
                    Task = Group.Leader.Task;
                    Target = Group.Leader.Target;
                    TargetArchitect = Group.Leader.TargetArchitect;
                    TargetObject = Group.Leader.TargetObject;
                }
                else if (Task == "" && BlindCycles == 0 && Game1.GameWorld.SettlementTypes.Contains(this.Location.Type) && Race != Game1.GameWorld.GetRace("debtshiba") && !Bound /*cant make judgements if ur blind lol, and cant if you already have a basic job.*/)
                {
                    if (IsLoadedTrader && DaysSinceLiquid < 2 && DaysSinceFood < 2)
                    {
                        Task = "vacanttfortrade";
                        CyclesLeftInTask = 500;
                        Target = Block.FindNearestThing("market");
                    }
                    else
                    {
                        if (Profession == "druidcrafter" || (Profession == "gardener" && Game1.r.Next(3) == 0))
                        {
                            Task = "druidcrafting";
                            CyclesLeftInTask = 500;
                            Target = (Location.Region, Location, District, Block, Room, "");
                        }
                        else if (DaysSinceLiquid > 0 && Task != "drinking")
                        {
                            Task = "drinking";
                            CyclesLeftInTask = 500;

                            if (Game1.r.Next(1, 3) == 1)
                            {
                                Target = Block.FindNearestThing("well");
                            }
                            else
                            {
                                Target = Block.FindNearestThing("tavern");
                            }
                        }
                        if (DaysSinceCoffeeOrTea > 0 && Task != "drinkingcaffeine")
                        {
                            Task = "drinkingcaffeine";
                            CyclesLeftInTask = 500;
                            Target = Block.FindNearestThing("tavern");
                        }
                        else if (DaysSinceFood > 0 && Task != "eating")
                        {
                            Task = "eating";
                            CyclesLeftInTask = 500;
                            Target = Block.FindNearestThing("tavern");
                        }
                        else if (NightsSinceSleep > 0 && Task != "sleeping")
                        {
                            Task = "sleeping";
                            CyclesLeftInTask = 350000;
                            Target = Block.FindNearestThing("house");
                        }
                        else if (Group == Location.Government || this == Location.Government || this == Location.HomeCivilization.Alpha)
                        {
                            Task = "discussion";
                            CyclesLeftInTask = 500;
                            Target = Block.FindNearestThing("prism");
                        }
                        else if (Profession == "scholar")
                        {
                            Task = "study";
                            CyclesLeftInTask = 500;
                            Target = Block.FindNearestThing("library");
                        }
                        else if (DaysSinceSocialized > 0 && Task != "socializing")
                        {
                            Task = "socializing";
                            CyclesLeftInTask = 500;
                            Target = Block.FindNearestThing("tavern");
                        }
                        else if ((DaysSincePerforming > 5 || (Profession == "musician" && DaysSincePerforming > 0)) && Task != "performmusic" && Task != "performdance" && Task != "performtheater" && Task != "performpoetry")
                        {
                            Task = new List<string>() { "performmusic", "performdance", "performtheater", "performpoetry" }[Game1.r.Next(3)];
                            CyclesLeftInTask = 500;
                            if (Game1.r.Next(1, 3) == 1)
                            {
                                Target = Block.FindNearestThing("tavern");
                            }
                            else
                            {
                                Target = Block.FindNearestThing("well");
                            }
                        }
                        else if (Game1.r.Next(1, 5) == 1)
                        {
                            Task = "industry";
                            CyclesLeftInTask = 500;
                            Target = Block.FindNearestThing("house");
                        }
                        else if (Block.FindNearestThing("tavern").Item2 != Location) //pls dont go to a random tavern if you live at something dumb like a spire
                        {
                            //pick random task for the fun of it
                            int TaskDecider = Game1.r.Next(0, 30);

                            if (TaskDecider == 0)
                            {
                                Task = new List<string>() { "performmusic", "performdance", "performtheater", "performpoetry" }[Game1.r.Next(3)];
                                CyclesLeftInTask = 500;
                                Target = Block.FindNearestThing("tavern");
                            }
                            else if (TaskDecider < 7)
                            {
                                Task = "socializing";
                                CyclesLeftInTask = 500;
                                Target = Block.FindNearestThing("tavern");
                            }
                            else if (TaskDecider < 12)
                            {
                                Task = "drinking";
                                CyclesLeftInTask = 500;
                                Target = Block.FindNearestThing("tavern");
                            }
                            else if (TaskDecider < 15)
                            {
                                Task = "drinkingcaffeine";
                                CyclesLeftInTask = 500;
                                Target = Block.FindNearestThing("tavern");
                            }
                            else if (TaskDecider < 25)
                            {
                                Task = "eating";
                                CyclesLeftInTask = 500;
                                Target = Block.FindNearestThing("tavern");
                            }
                            else
                            {
                                Task = "contemplate";
                                Target = Block.FindNearestThing("well");
                                Target = (Target.Item1, Target.Item2, Target.Item3, Target.Item3.DistrictMap[Game1.r.Next(0, 49)], null, Target.Item6);
                                CyclesLeftInTask = 500;
                            }
                        }

                        // Additional check to ensure Target is in the same district
                        if (Target.Item3 != this.District)
                        {
                            Target = Block.FindNearestThing("structure");
                        }
                    }
                }



                if ((Task == "disabletarget" || Task == "killtarget") && ShieldTokens.Contains(TargetArchitect) && TargetArchitect.Energy < 60)
                {
                    AnnounceToParty(ReferredToNames[0] + " has defeated his opponent, proclaiming victory.", Color.DeepPink, new List<Entity>() { this });
                    ShieldTokens.Remove(TargetArchitect);
                    TargetArchitect.ShieldTokens.Remove(this);
                    Task = "";
                    SetOpinion(TargetArchitect, 0);
                    CyclesLeftInTask = 0;
                    TargetArchitect.Task = "";
                    TargetArchitect.CyclesLeftInTask = 0;
                    TargetArchitect = null;
                }
                else if ((Task == "disabletarget" || Task == "killtarget") && TargetArchitect.ShieldTokens.Contains(this) && Energy < 60)
                {
                    AnnounceToParty(ReferredToNames[0] + ": Okay! You win!", Color.DeepPink, new List<Entity>() { this });
                    ShieldTokens.Remove(TargetArchitect);
                    TargetArchitect.ShieldTokens.Remove(this);
                    Task = "";
                    SetOpinion(TargetArchitect, 0);
                    CyclesLeftInTask = 0;
                    TargetArchitect.Task = "";
                    TargetArchitect.CyclesLeftInTask = 0;
                    TargetArchitect = null;
                }



                if (Task == "killtarget" && TargetArchitect != null)
                {
                    //update the targetting system so you go to where they are naturally
                    Target = (TargetArchitect.Location.Region, TargetArchitect.Location, TargetArchitect.District, TargetArchitect.Block, TargetArchitect.Room, "");
                }
                if (Task == "disabletarget" && TargetArchitect != null)
                {
                    Target = (TargetArchitect.Location.Region, TargetArchitect.Location, TargetArchitect.District, TargetArchitect.Block, TargetArchitect.Room, "");
                }

                //have you finished task? then reap the benefits
                if (CyclesLeftInTask <= 0 && (Location.Region, Location, District, Block, Room, "") == Target)
                {
                    //CONGRATULATIONS! you're done with your task, you can reap the benefits.

                    //tasks not listed here don't have benefits
                    switch (Task)
                    {
                        case "drinking":
                            DaysSinceLiquid = 0;
                            break;
                        case "drinkingcaffeine":
                            DaysSinceCoffeeOrTea = 0;
                            DaysSinceLiquid = 0;
                            break;
                        case "sleeping":
                            NightsSinceSleep = 0;
                            break;
                        case "discussion":
                            DaysSinceSocialized = 0;
                            break;
                        case "socializing":
                            DaysSinceSocialized = 0;
                            break;
                        case "performmusic":
                            DaysSincePerforming = 0;
                            break;
                        case "performpoetry":
                            DaysSincePerforming = 0;
                            break;
                        case "performdance":
                            DaysSincePerforming = 0;
                            break;
                        case "performtheater":
                            DaysSincePerforming = 0;
                            break;
                        case "cook":
                            //add cooking stuff <3
                            break;
                        case "industry":
                            var generatedItemStrings = District.GenerateItems(District.Industry, 1);
                            List<Object> generatedItems = new List<Object>();

                            foreach (var itemString in generatedItemStrings)
                            {
                                generatedItems.AddRange(Game1.ConvertStringToObjects(itemString));
                            }

                            if (Room != null)
                            {
                                Room.Objects.AddRange(generatedItems);
                            }
                            else
                            {
                                Block.Objects.AddRange(generatedItems);
                            }
                            break;

                        default:
                            break;
                    }

                    Task = "";
                }


                //otherwise, youre at the right place so either kill someone or finish your task
                else if (Task != "" && (Location.Region, Location, District, Block, Room, "") == Target)
                {
                    string DetermineAttackVerb(string weaponType)
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

                    if (Task == "killtarget" || Task == "disabletarget")
                    {
                        bool CastedASpell = false;
                        int SpellCastingDistance = 3;

                        // Now, you can use ArchitectsToUse to check for proximity, interactions, etc.

                        if (TargetArchitect != null && ArchitectsToUse.Contains(TargetArchitect)) // Check if the target is in the same area
                        {
                            List<string> OffensiveSpells = new List<string> { "expel", "water bolt", "chaos flare", "concentrated ignition", "tremor", "ice shock" };

                            if (Game1.r.Next(50) == 1)
                            {
                                AnnounceToParty(ReferredToNames[0] + " repositions!", Color.MediumPurple, new List<Entity>() { this });

                                CooldownCycles += (int)Math.Round(3 * Speed());

                                foreach (Object o in BodyParts)
                                {
                                    var textStorage = o.UpdateExposure(-(25 + Dexterity));
                                    if (textStorage != null)
                                    {
                                        AnnounceToParty(textStorage.Data, textStorage.Color, textStorage.Entities);
                                    }
                                }
                            }




                            if ((SpellsKnown.Count > 0 && Game1.r.Next(2) == 0) || Profession == "sorcerer" || Profession == "warlock" || Race == Game1.GameWorld.GetRace("debtshiba"))
                            {
                                List<Entity> offensiveSpellsInKit;

                                if (Race == Game1.GameWorld.GetRace("debtshiba"))
                                {
                                    SpellcastingPower = 25;
                                    offensiveSpellsInKit = Game1.GameWorld.AllSpells.Where(spell => OffensiveSpells.Contains(spell.Metadata)).ToList();
                                }
                                else
                                {
                                    offensiveSpellsInKit = SpellsKnown.Where(spell => OffensiveSpells.Contains(spell.Metadata)).ToList();
                                }

                                if (offensiveSpellsInKit.Any() && GetDistance(TargetArchitect) <= SpellCastingDistance)
                                {
                                    for (int i = SpellcastingPower; i != 0; i--)
                                    {
                                        Entity spellToCast = offensiveSpellsInKit[Game1.r.Next(offensiveSpellsInKit.Count)];
                                        CastedASpell = true;
                                        Game1.Announcements.AddRange(CastSpell(spellToCast.Metadata, new List<Entity>() { TargetArchitect }));
                                    }
                                }
                            }

                            if (!CastedASpell) // If no spell was cast, try a melee attack
                            {
                                if (Race.Name.EndsWith("guardian") && Game1.r.Next(4) != 0)
                                {
                                    //use epic ability
                                    List<Object> objects = Room != null ? Room.Objects : Block.Objects;

                                    CooldownCycles += (int)Math.Round(10 / Speed());

                                    string Ability = Race.Powers[Game1.r.Next(Race.Powers.Count)];

                                    if (Ability == "energybolts")
                                    {
                                        for (int i = Game1.r.Next(3, 6); i != 0; i--)
                                        {
                                            Object o = new Object(null, "energy bolt", new List<Material>() { new Material("energy", "energy", 3, 0, "white") }, this);
                                            objects.Add(o);
                                            o.Owner = this;
                                            o.Thrower = this; //whatever...
                                            o.AirborneTarget = TargetArchitect;
                                            o.AirborneCyclesToHitTarget = 15 - Focus;
                                            AnnounceToParty(ReferredToNames[0] + " fires a bolt at " + TargetArchitect.ReferredToNames[0] + "!", Color.Magenta, new List<Entity>() { this, TargetArchitect });
                                        }
                                    }
                                    else if (Ability == "cloaking")
                                    {
                                        CloakCycles += Game1.r.Next(5, 15);
                                        AnnounceToParty(ReferredToNames[0] + " partially phases out of reality!", Color.Magenta, new List<Entity>() { this });
                                    }
                                    else if (Ability == "magneticfield")
                                    {
                                        AnnounceToParty(ReferredToNames[0] + " radiates magnetic energy!", Color.Magenta, new List<Entity>() { this });
                                        foreach (Object o in objects)
                                        {
                                            AnnounceToParty(o.ReferredToNames[0] + " falls to the ground!", Color.Magenta, new List<Entity>() { o });
                                            if (o.Owner != this)
                                            {
                                                o.AirborneTarget = null;
                                                o.AirborneCyclesToHitTarget = 0;
                                                o.AirbornePower = 0;
                                            }
                                        }
                                    }
                                    else if (Ability == "shockwave")
                                    {
                                        AnnounceToParty(ReferredToNames[0] + " radiates an explosive shockwave!", Color.Magenta, new List<Entity>() { this });
                                        foreach (Architect a in ArchitectsToUse)
                                        {
                                            if (!a.Race.Name.EndsWith("guardian"))
                                            {
                                                AnnounceToParty(a.ReferredToNames[0] + " is destabilized!", Color.Magenta, new List<Entity>() { a });
                                                a.DestabilizedCycles += Game1.r.Next(10, 25);
                                            }
                                        }
                                    }
                                    else if (Ability == "slowray")
                                    {
                                        AnnounceToParty(ReferredToNames[0] + " fires an array of brilliant white beams!", Color.Magenta, new List<Entity>() { this });
                                        foreach (Architect a in ArchitectsToUse)
                                        {
                                            if (!a.Race.Name.EndsWith("guardian"))
                                            {
                                                AnnounceToParty(a.ReferredToNames[0] + " is frozen temporarily!", Color.Magenta, new List<Entity>() { this });
                                                a.HoldCycles += Game1.r.Next(4, 8);
                                            }
                                        }
                                    }
                                    else if (Ability == "pulsebash")
                                    {
                                        AnnounceToParty(ReferredToNames[0] + " charges up a devastating energy bash!", Color.Magenta, new List<Entity>() { this });
                                        PulseCharge += 1;

                                        if (PulseCharge == 2)
                                        {
                                            PulseCharge = 0;
                                            AnnounceToParty(TargetArchitect.ReferredToNames[0] + " pulse bashes " + TargetArchitect.ReferredToNames[0] + "!", Color.Magenta, new List<Entity>() { this });
                                            TargetArchitect.DestabilizedCycles += 200;
                                            TargetArchitect.Energy -= 30;
                                        }
                                    }
                                    else if (Ability == "harvest")
                                    {
                                        AnnounceToParty(ReferredToNames[0] + " siphons energy from " + TargetArchitect.ReferredToNames[0] + " with a translucent beam!", Color.Magenta, new List<Entity>() { this, TargetArchitect });
                                        TargetArchitect.Energy -= 5;
                                        if (Energy <= 0)
                                        {
                                            DeathCause = "was siphoned by a construct";
                                        }
                                        this.Energy += 5;
                                    }
                                }
                                else
                                {
                                    Object Weapon;
                                    Object mainHand = MainHeldObject;
                                    Object offHand = OffHeldObject;

                                    if (mainHand != null && mainHand.IsWeapon)
                                    {
                                        Weapon = mainHand;
                                    }
                                    else if (offHand != null && offHand.IsWeapon)
                                    {
                                        Weapon = offHand;
                                    }
                                    else
                                    {
                                        Weapon = BodyParts[Game1.r.Next(BodyParts.Count)]; // Assuming unarmed combat uses body parts as weapons
                                    }

                                    if (Weapon.WeaponMaximumRange >= GetDistance(TargetArchitect))
                                    {
                                        string attackVerb = DetermineAttackVerb(Weapon.DamageType);

                                        // Define base likelihood for each body part
                                        int baseLikelihood = 25;

                                        // Calculate total likelihood for all body parts
                                        int totalLikelihood = TargetArchitect.BodyParts.Sum(bp => baseLikelihood + bp.Exposure);

                                        // Generate a random value between 0 and totalLikelihood
                                        int randomValue = Game1.r.Next(totalLikelihood);

                                        // Determine the selected body part based on the random value
                                        int cumulativeLikelihood = 0;
                                        Object targetBodyPart = TargetArchitect.BodyParts.First(); // Default to the first body part

                                        foreach (var bodyPart in TargetArchitect.BodyParts)
                                        {
                                            cumulativeLikelihood += baseLikelihood + bodyPart.Exposure;
                                            if (randomValue < cumulativeLikelihood)
                                            {
                                                targetBodyPart = bodyPart;
                                                break;
                                            }
                                        }

                                        Attacks.Add(new Attack(attackVerb, this, targetBodyPart, Weapon));
                                    }
                                    else // If neither spell nor weapon attack is possible, approach the target
                                    {
                                        DistanceFromArchitect(TargetArchitect, -2); // Decrease distance by 2
                                        CooldownCycles += (int)(15 / Math.Round(Speed()));

                                        AnnounceToParty(ReferredToNames[0] + " gets closer to " + TargetArchitect.ReferredToNames[0] + "!", Color.DarkMagenta, new List<Entity>() { this, TargetArchitect });
                                    }
                                }

                            }
                        }
                        else
                        {
                            Target = (TargetArchitect.Location.Region, TargetArchitect.Location, TargetArchitect.District, TargetArchitect.Block, TargetArchitect.Room, "");
                        }
                    }
                    else if (Task == "sentinel")
                    {
                        //spawn stuff if youre a heart/core. Those things will start killing.

                        if (CyclesLeftInTask % 10 == 1)
                        {
                            foreach (Architect a in ArchitectsToUse)
                            {
                                if (a.Race != a.Location.PrimaryRace && a != this)
                                {
                                    //if youre an isofractal make sure that theyre a thief

                                    bool KillThemPlease = false;

                                    if (Race.Name == "icosidodecahedron")
                                    {
                                        if ((a.Inventory.Any(item => item.Owner == this) || a.IsofractalThief))
                                        {
                                            a.IsofractalThief = true;
                                            KillThemPlease = true;
                                        }
                                    }
                                    else
                                    {
                                        if ((a.Inventory.Any(item => item.Owner == this)))
                                        {
                                            KillThemPlease = true;
                                        }
                                    }


                                    if (KillThemPlease)
                                    {
                                        //spawn death

                                        int count = Game1.r.Next(1, 4);

                                        AnnounceToParty(this.ReferredToNames[0] + " erupts sentinels from an ancient era!", Color.OrangeRed, new List<Entity>());

                                        Architect A = new Architect("", "male", Location.PrimaryRace, 0, "sentinel", new List<Object>(), Location, District, Block, "none", 4);
                                        A.Name = Game1.GameWorld.GenerateUniqueArchitectName(A);

                                        A.Room = this.Room;
                                        A.Block = this.Block;

                                        if (A.Room != null)
                                        {
                                            A.Room.Architects.Add(A);
                                        }
                                        else
                                        {
                                            A.Block.Architects.Add(A);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (Task == "druidcrafting")
                    {
                        if (CyclesLeftInTask % 10 == 1)
                        {
                            List<Entity> targetEntities = new List<Entity>();
                            string spellName = "emergentgrowth";

                            if (this.Block.Objects.Count > 0)
                            {
                                int randomIndex = Game1.r.Next(this.Block.Objects.Count);
                                targetEntities.Add(this.Block.Objects[randomIndex]);
                            }
                            else if (this.Block.Architects.Count > 0)
                            {
                                int randomIndex = Game1.r.Next(this.Block.Architects.Count);
                                targetEntities.Add(this.Block.Architects[randomIndex]);
                            }

                            if (targetEntities.Count > 0)
                            {
                                List<TextStorage> spellResults = CastSpell(spellName, targetEntities);
                                foreach (var result in spellResults)
                                {
                                    AnnounceToParty(result.Data, result.Color, result.Entities);
                                }
                            }
                        }
                    }


                    CyclesLeftInTask--;
                }

                Dictionary<(int, int), string> CoordsToDirection = new Dictionary<(int, int), string>()
                {
                    {(1, 1), "southeast"},
                    {(1, 0), "east"},
                    {(1, -1), "northeast"},
                    {(0, 1), "south"},
                    {(0, -1), "north"},
                    {(-1, 1), "southwest"},
                    {(-1, 0), "west"},
                    {(-1, -1), "northwest"}
                };

                if (Task != "" && Target != (Location.Region, Location, District, Block, Room, "") && Target != (null, null, null, null, null, "") && BlindCycles == 0)
                {
                    if (Location.Region != Target.Item1 || Location != Target.Item2 || District != Target.Item3)
                    {
                        // Determine direction based on the edge logic
                        string Edge = "";

                        if (Block.X > Block.Z)
                        {
                            if (Block.X + Block.Z > 7)
                            {
                                Edge = "east";
                            }
                            else
                            {
                                Edge = "north";
                            }
                        }
                        else
                        {
                            if (Block.X + Block.Z > 7)
                            {
                                Edge = "south";
                            }
                            else
                            {
                                Edge = "west";
                            }
                        }

                        string direction = Edge switch
                        {
                            "west" => "west",
                            "east" => "east",
                            "north" => "north",
                            "south" => "south",
                            _ => "none"
                        };

                        if (direction != "none")
                        {
                            Move(direction);
                        }
                    }
                    else if (Room != null && Target.Item5 == null)
                    {
                        if(Room.Structure.Rooms.IndexOf(Room) == 0)
                        {
                            AlternateMove = "leavebuilding";
                        }
                        else
                        {
                            AlternateMove = "findexit";
                        }
                    }
                    else if (Target.Item4 != null && Block != Target.Item4)
                    {
                        // Calculate direction based on angle
                        int deltaX = Target.Item4.X - Block.X;
                        int deltaZ = Target.Item4.Z - Block.Z;

                        double angleRadians = Math.Atan2(deltaX, -deltaZ);
                        double angleDegrees = angleRadians * (180.0 / Math.PI);

                        if (angleDegrees < 0)
                        {
                            angleDegrees += 360;
                        }

                        // Determine direction based on the angle
                        string direction = angleDegrees switch
                        {
                            >= 337.5 or < 22.5 => "north",
                            >= 22.5 and < 67.5 => "northeast",
                            >= 67.5 and < 112.5 => "east",
                            >= 112.5 and < 157.5 => "southeast",
                            >= 157.5 and < 202.5 => "south",
                            >= 202.5 and < 247.5 => "southwest",
                            >= 247.5 and < 292.5 => "west",
                            _ => "northwest"
                        };

                        Move(direction);
                    }
                    else if (Block != null && Structure == null && Target.Item5 != null && Block.Structures.Contains(Target.Item5.Structure))
                    {
                        // Enter the structure
                        AlternateMove = Target.Item5.Structure.Name;
                    }
                    else if (Structure != null && Room != Target.Item5)
                    {
                        // Find the nearest door to the target room
                        Door doorToTargetRoom = Room.FindQuickestDoorToRoom(Target.Item5);
                        if (doorToTargetRoom != null)
                        {
                            AlternateMove = doorToTargetRoom.ID.ToString(); // Set the alternate move to the door ID
                        }
                    }
                    else
                    {
                        // No movement needed
                    }
                }

                if (Bound)
                {
                    Task = "bound";
                    CyclesLeftInTask = 9999999;
                    Target = (this.Location.Region, Location, District, Block, Room, "");
                }
                else if (ChangeInX != 0 || ChangeInZ != 0)
                {
                    string direction = CoordsToDirection[(ChangeInX, ChangeInZ)];

                    if (CurrentlyMovingPlace == direction)
                    {
                        // This check is now streamlined to occur only once.
                        if (CombatCycles == 0 || Game1.r.Next(100) <= EscapeChance())
                        {
                            int NewX = Block.X + ChangeInX;
                            int NewZ = Block.Z + ChangeInZ;

                            if (NewX < 0 || NewX > 6 || NewZ < 0 || NewZ > 6)
                            {
                                if ((Target.Item2 != null && Target.Item2 != Location) || (Target.Item3 != null && Target.Item3 != District))
                                {
                                    TriggeredLock = true;
                                    Block.Architects.Remove(this);
                                    Target.Item3.Architects.Add(this);
                                    Block = null;
                                    Location = Target.Item2;
                                    District = Target.Item3;

                                    foreach (Object o in BodyParts)
                                    {
                                        o.UpdateExposure(-9999);
                                    }

                                    if (Game1.LoadedArchitects.Contains(this))
                                    {
                                        Game1.LoadedArchitectsToRemove.Add(this);
                                    }

                                    // Assuming Attacks is a collection that's being returned
                                    return Attacks;
                                }
                                else
                                {
                                    // this means you are trying to leave and didn't go somewhere. Then just hang out lul
                                    Target = Block.FindNearestThing("well");
                                }
                            }
                            else
                            {
                                Block.Architects.Remove(this);
                                CooldownCycles += (int)(Math.Round(25 / Speed()));
                                Block = District.DistrictMap[NewX + NewZ * 7];
                                Block.Architects.Add(this);
                            }

                            CurrentlyMovingPlace = "";
                        }
                        else
                        {
                            // Handle escape failure
                            CooldownCycles += (int)(Math.Round(25 / Speed()));
                            AnnounceToParty(ReferredToNames[0] + " failed to escape!", Color.LimeGreen, new List<Entity>() { this });
                        }
                    }
                    else
                    {
                        // Setting up for a new move attempt in the next cycle or action.
                        if (CombatCycles == 0 || Game1.r.Next(100) <= EscapeChance())
                        {
                            CurrentlyMovingPlace = direction;
                            CooldownCycles += (int)(Math.Round(25 / Speed()));
                            // Optionally, make an observation if it's not the first move attempt.
                            if (CombatCycles != 0)
                            {
                                AnnounceToParty(ReferredToNames[0] + " is preparing to move...", Color.Red, new List<Entity>() { this });
                            }
                        }
                        else
                        {
                            CooldownCycles += (int)(Math.Round(25 / Speed()));
                            AnnounceToParty(ReferredToNames[0] + " failed to escape!", Color.Red, new List<Entity>() { this });
                        }
                    }
                }
                else if (AlternateMove == "leavebuilding")
                {
                    Room.Architects.Remove(this);
                    CooldownCycles += (int)(Math.Round(25 / Speed()));
                    Room = null;
                    Block.Architects.Add(this);
                }
                else if (AlternateMove == "findexit")
                {
                    Door exitDoor = Room.FindQuickestExitDoor();
                    if (exitDoor != null)
                    {
                        if (CombatCycles == 0 || Game1.r.Next(100) <= EscapeChance())
                        {
                            Room.Architects.Remove(this);
                            Room = exitDoor.DestinationRoom;
                            Room.Architects.Add(this);
                            CooldownCycles += (int)(Math.Round(25 / Speed()));
                        }
                        else
                        {
                            AnnounceToParty(ReferredToNames[0] + " struggles to escape, but fails!", Color.OrangeRed, new List<Entity> { this });
                            CooldownCycles += (int)Math.Round(25 / Speed());
                        }
                    }
                    else
                    {
                        AnnounceToParty(ReferredToNames[0] + " cannot escape...", Color.Red, new List<Entity> { this });
                    }
                }

                else if (AlternateMove != "")
                {
                    if (int.TryParse(AlternateMove, out int doorId))
                    {
                        MoveThroughDoor(doorId.ToString());
                        AlternateMove = ""; // Clear alternate move after using it
                    }
                    else if (Target.Item5 != null && Target.Item5.Structure.Name == AlternateMove)
                    {
                        Block.Architects.Remove(this);
                        CooldownCycles += (int)(Math.Round(25 / Speed()));
                        Room = Target.Item5.Structure.Rooms[0];
                        Room.Architects.Add(this);
                    }
                }
            }
            else if (CooldownCycles > 0)
            {
                CooldownCycles--;
            }




            if (BlockLastCycle != Block || RoomLastCycle != Room)
            {
                CyclesSinceMoved = 0;
            }
            else
            {
                CyclesSinceMoved++;
            }

            CyclesSinceJump++;

            if (CyclesSinceJump > 30)
            {
                DropKickReady = false;
            }

            if (ReactionBoostCycles > 0)
            {
                ReactionBoostCycles--;
            }
            if (ExtraFocusTicks > 0)
            {
                ExtraFocusTicks--;
            }

            BlockLastCycle = Block;
            RoomLastCycle = Room;

            //lose track of invisible targets

            if (TargetArchitect != null && !(Game1.r.Next(0, 100) < TargetArchitect.ExtraStealth))
            {
                if (TargetArchitect.PathOfShadowLevel >= 6 || (TargetArchitect.Inventory.Count == 0 && TargetArchitect.Clothing.Count == 0))
                {
                    TargetArchitect = null;
                    Task = "";
                }
            }

            return Attacks;
        }



























































        public Architect()
        {
            //for serialization
        }

        public List<TextStorage> CastSpell(string Spell, List<Entity> Targets) //the list of strings are the announcements
        {
            List<TextStorage> Announcements = new List<TextStorage>();

            string casterName = this.ReferredToNames[0];

            List<string> AgressiveSpells = new List<string>() { "water bolt", "chaos flare", "concentrated ignition", "ice shock", "rise", "hold", "force throw", "shatter", "expel"};

            foreach (Entity e in Targets)
            {
                if (e is Architect a && AgressiveSpells.Contains(Spell))
                {
                    a.CombatCycles = 250;
                    CombatCycles = 250;
                    a.ChangeOpinion(this, -60);
                }
            }


            List<Entity> TargetsToPurge = new List<Entity>();
            foreach (Entity e in Targets)
            {
                if (GetDistance(e) >= 4)
                {
                    Announcements.Add(new TextStorage($"{e.ReferredToNames[0]} is outside of casting range.", Color.Yellow, new List<Entity>() { e }));
                    TargetsToPurge.Add(e);
                }
            }
            foreach (Entity e in TargetsToPurge)
            {
                Targets.Remove(e);
            }

            if (new List<string> { "eternal bind", "ethereal rupture", "echo", "expunge", "emergence" }.Contains(Spell))
            {
                Energy = 1;
                HalfFocusTicks = 5000;
                Announcements.Add(new TextStorage($"You feel incredibly drained...", Color.OrangeRed, new List<Entity>()));
            }
            else
            {
                Energy -= Math.Max(1, (10 - (SpellcastingPower + (Focus / 2))));
            }

            if (Game1.GameWorld.AllLegendarySpells.Any(s => s.Metadata == Spell))
            {
                int Month = ((int)Math.Round((decimal)(Location.Region.World.Cycle / 24192000)) % 12) + 1;
                int Year = (int)Math.Round((decimal)(Location.Region.World.Cycle / 290304000), MidpointRounding.ToZero);

                string Date = "(" + Month + "/" + Year + ")";

                List<string> Names = new List<string>();

                foreach (Entity e in Targets)
                {
                    Names.Add(e.Name);
                }

                Location.Region.World.HistoricalEvents.Add(Date + " " + this.Name + " casted " + Spell + " at " + Game1.FormatList(Names));
            }

            foreach (Entity CurrentTarget in Targets)
            {
                if (Spell == "water bolt")
                {
                    CooldownCycles += (int)Math.Round(15 / Speed());

                    foreach (Architect a in Game1.GamePlayerParty.Architects)
                    {
                        if (a.Room == this.Room && a.Block == this.Block)
                        {
                            Announcements.Add(new TextStorage($"{casterName} curves their hand inwards, accumulating vapor. They hurl the concentrated sphere...", Color.Purple, new List<Entity>() { this }));
                            Announcements.Add(new TextStorage($"It crashes into {CurrentTarget.ReferredToNames[0]}, splashing into them!", Color.Purple, new List<Entity>() { CurrentTarget }));
                            break;
                        }
                    }

                    if (CurrentTarget is Object)
                    {
                        if (((Object)CurrentTarget).FireCycles > 0)
                        {
                            ((Object)CurrentTarget).FireCycles = 0;
                        }
                        ((Object)CurrentTarget).Integrity = ((Object)CurrentTarget).Integrity - 2;
                    }
                    else
                    {
                        if (((Architect)CurrentTarget).FireSeconds > 0)
                        {
                            ((Architect)CurrentTarget).FireSeconds = 0;
                        }
                        foreach (Object o in ((Architect)CurrentTarget).BodyParts)
                        {
                            o.Integrity = o.Integrity - 2;
                        }
                    }
                }
                else if (Spell == "chaos flare")
                {
                    CooldownCycles += (int)Math.Round(15 / Speed());

                    foreach (Architect a in Game1.GamePlayerParty.Architects)
                    {
                        if (a.Room == this.Room && a.Block == this.Block)
                        {
                            Announcements.Add(new TextStorage($"{casterName} makes a fist and jerks their arm inwards, conjuring two spheres of light and dark rotating it. They throw them...", Color.Purple, new List<Entity>() { this }));
                            Announcements.Add(new TextStorage($"They crash into {CurrentTarget.ReferredToNames[0]}, and react explosively!", Color.Purple, new List<Entity>() { CurrentTarget }));
                            break;
                        }
                    }

                    if (CurrentTarget is Object)
                    {
                        ((Object)CurrentTarget).FireCycles += Game1.r.Next(0, 4);
                        ((Object)CurrentTarget).DestabilizedCycles += Game1.r.Next(0, 50);
                        ((Object)CurrentTarget).Integrity = ((Object)CurrentTarget).Integrity - 50;
                    }
                    else
                    {
                        ((Architect)CurrentTarget).FireSeconds += Game1.r.Next(0, 4);
                        ((Architect)CurrentTarget).DestabilizedCycles += Game1.r.Next(0, 50);
                        foreach (Object o in ((Architect)CurrentTarget).BodyParts)
                        {
                            if (Game1.r.Next(0, 2) == 0)
                            {
                                o.Integrity = o.Integrity - 10;
                            }
                        }
                    }
                }
                else if (Spell == "ice shock")
                {
                    CooldownCycles += (int)Math.Round(15 / Speed());

                    foreach (Architect a in Game1.GamePlayerParty.Architects)
                    {
                        if (a.Room == this.Room && a.Block == this.Block)
                        {
                            Announcements.Add(new TextStorage($"{casterName} lifts up frozen particles...", Color.Purple, new List<Entity>() { this }));
                            Announcements.Add(new TextStorage($"A swirl of icy magic envelops {CurrentTarget.ReferredToNames[0]}!", Color.Purple, new List<Entity>() { CurrentTarget }));
                            break;
                        }
                    }

                    if (CurrentTarget is Object)
                    {
                        ((Object)CurrentTarget).WetCycles += Game1.r.Next(0, 100);
                        ((Object)CurrentTarget).DestabilizedCycles += Game1.r.Next(0, 100);
                        ((Object)CurrentTarget).Integrity = ((Object)CurrentTarget).Integrity - 50;
                    }
                    else
                    {
                        ((Architect)CurrentTarget).WetCycles += Game1.r.Next(0, 50);
                        ((Architect)CurrentTarget).DestabilizedCycles += Game1.r.Next(0, 100);
                        foreach (Object o in ((Architect)CurrentTarget).BodyParts)
                        {
                            if (Game1.r.Next(0, 2) == 0)
                            {
                                o.Integrity = o.Integrity - 10;
                            }
                        }
                    }
                }
                else if (Spell == "concentrated ignition")
                {
                    CooldownCycles += (int)Math.Round(15 / Speed());
                    foreach (Architect a in Game1.GamePlayerParty.Architects)
                    {
                        if (a.Room == this.Room && a.Block == this.Block)
                        {
                            Announcements.Add(new TextStorage($"{casterName} holds their palms one over the other facing each other, and gathers heat energy...", Color.Purple, new List<Entity>() { this }));
                            Announcements.Add(new TextStorage($"It quickly dissipates, reassembling itself at {CurrentTarget.ReferredToNames[0]}!", Color.Purple, new List<Entity>() { CurrentTarget }));
                        }
                    }

                    if (CurrentTarget is Object)
                    {
                        ((Object)CurrentTarget).FireCycles += Game1.r.Next(1, 9);
                    }
                    else
                    {
                        ((Architect)CurrentTarget).FireSeconds += Game1.r.Next(1, 9);
                    }
                }
                else if (Spell == "tremor")
                {
                    CooldownCycles += (int)Math.Round(30 / Speed());

                    Announcements.Add(new TextStorage($"{casterName} holds out their hands palms down and shoves into the ground...", Color.Purple, new List<Entity>() { this }));
                    Announcements.Add(new TextStorage($"A massive tremor shakes the ground, but {CurrentTarget.ReferredToNames[0]} is unshaken!", Color.Purple, new List<Entity>() { CurrentTarget }));

                    List<Object> Objects = Room != null ? Room.Objects : Block.Objects;
                    List<Architect> Architects = Room != null ? Room.Architects : Block.Architects;

                    foreach (Object o in Objects)
                    {
                        if (!Targets.Contains(o))
                        {
                            o.DestabilizedCycles += Game1.r.Next(100, 200);
                            Announcements.Add(new TextStorage($"{o.ReferredToNames[0]} is destabilized!", Color.Magenta, new List<Entity>() { o }));
                        }
                    }
                    foreach (Architect a in Architects)
                    {
                        if (!Targets.Contains(a))
                        {
                            a.DestabilizedCycles += Game1.r.Next(100, 200);
                            Announcements.Add(new TextStorage($"{a.ReferredToNames[0]} is destabilized!", Color.Magenta, new List<Entity>() { a }));
                        }
                    }
                }
                else if (Spell == "immobile illusion")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage("You have only decieved yourself.", Color.Purple, new List<Entity>()));
                }
                else if (Spell == "shadow veil")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage("You have only decieved yourself.", Color.Purple, new List<Entity>()));
                }
                else if (Spell == "mobile illusion")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage("You have only decieved yourself.", Color.Purple, new List<Entity>()));
                }
                else if (Spell == "reactive illusion")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage("You have only decieved yourself.", Color.Purple, new List<Entity>()));
                }
                else if (Spell == "truthfulness")
                {
                    CooldownCycles += (int)Math.Round(30 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} waves across {CurrentTarget.ReferredToNames[0]}...", Color.Purple, new List<Entity>() { this, CurrentTarget }));

                    if (CurrentTarget is Object)
                    {
                        Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple, new List<Entity>()));
                    }
                    else if (CurrentTarget is Architect)
                    {
                        Announcements.Add(new TextStorage($"{CurrentTarget.ReferredToNames[0]} looks at you with a loyal complexion...", Color.Purple, new List<Entity>() { CurrentTarget }));

                        ((Architect)CurrentTarget).ArchitectsIWillTellTruthTo.Add(this);
                    }
                }
                else if (Spell == "rise")
                {
                    CooldownCycles += (int)Math.Round(30 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} gestures their hand towards the sky...", Color.Purple, new List<Entity>() { this }));
                    Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " flies into the air!", Color.Purple, new List<Entity>() { CurrentTarget }));

                    if (CurrentTarget is Object)
                    {
                        ((Object)CurrentTarget).YLevelInFeet += Game1.r.Next(30, 50);
                    }
                    else
                    {
                        ((Architect)CurrentTarget).YLevelInFeet += Game1.r.Next(30, 50);
                    }
                }
                else if (Spell == "hold")
                {
                    CooldownCycles += (int)Math.Round(30 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} clenches their fist violently...", Color.Purple, new List<Entity>() { this }));
                    if (CurrentTarget is Object)
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " stagnates.", Color.Purple, new List<Entity>() { CurrentTarget }));
                        ((Object)CurrentTarget).AirborneTarget = null;
                        ((Object)CurrentTarget).AirborneCyclesToHitTarget = 0;
                    }
                    else
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " freezes in time!", Color.Purple, new List<Entity>() { CurrentTarget }));
                        ((Architect)CurrentTarget).HoldCycles = 40 + Focus * 5;
                    }
                }
                else if (Spell == "force throw")
                {
                    CooldownCycles += (int)Math.Round(15 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} clenches their fist at " + CurrentTarget.ReferredToNames[0] + ", gathering material. They thrust it at " + CurrentTarget.ReferredToNames[0] + "...", Color.Purple, new List<Entity>() { this, CurrentTarget, CurrentTarget }));

                    Entity MainTarget = CurrentTarget;
                    Targets.RemoveAt(0);

                    foreach (Entity o in Targets)
                    {
                        if (o == MainTarget)
                        {

                        }
                        else
                        {
                            if (o is Object)
                            {
                                Announcements.Add(new TextStorage(o.ReferredToNames[0] + " flies at " + CurrentTarget.ReferredToNames[0] + "!", Color.Purple, new List<Entity>() { o, CurrentTarget }));
                                ((Object)o).AirborneTarget = MainTarget;
                                ((Object)CurrentTarget).AirborneCyclesToHitTarget = 15 - Focus;
                            }
                            else
                            {
                                Announcements.Add(new TextStorage(o.ReferredToNames[0] + " stumbles, but isnt yielding to your force!", Color.Purple, new List<Entity>() { o }));
                                ((Architect)o).DestabilizedCycles += 50;
                            }
                        }
                    }
                }
                else if (Spell == "shatter")
                {
                    CooldownCycles += (int)Math.Round(30 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} brings his arms inward and swings them outward violently...", Color.Purple, new List<Entity>() { this }));

                    if (CurrentTarget is Object)
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " dissipates across the area!", Color.Purple, new List<Entity>() { CurrentTarget }));
                        Block.Objects.Remove((Object)CurrentTarget);
                    }
                    else
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " struggles to hold together, destabilizing...", Color.Purple, new List<Entity>() { CurrentTarget }));
                        ((Architect)CurrentTarget).DestabilizedCycles += 100;
                    }
                }
                else if (Spell == "intercept")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} reaches their hand towards " + CurrentTarget.ReferredToNames[0] + " and grasps...", Color.Purple, new List<Entity>() { this, CurrentTarget }));

                    if (CurrentTarget is Object && ((Object)CurrentTarget).AirborneTarget != null)
                    {
                        Announcements.Add(new TextStorage(CurrentTarget + " dissapears in a web of fractals!", Color.Purple, new List<Entity>() { CurrentTarget }));
                        ((Object)CurrentTarget).Fractallize(999999);
                    }
                    else
                    {
                        Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple, new List<Entity>()));
                    }
                }
                else if (Spell == "expel")
                {
                    CooldownCycles += (int)Math.Round(20 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} reaches their hand towards " + CurrentTarget.ReferredToNames[0] + " and grasps...", Color.Purple, new List<Entity>() { this, CurrentTarget }));

                    if (CurrentTarget is Object)
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " dissapears in a web of fractals!", Color.Purple, new List<Entity>() { CurrentTarget }));
                        ((Object)CurrentTarget).Fractallize(999999);
                    }
                    else if (CurrentTarget is Architect)
                    {
                        if (((Architect)CurrentTarget).Energy < (((Architect)CurrentTarget).MaxEnergy() / 3))
                        {
                            Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " dissapears in a web of fractals!", Color.Purple, new List<Entity>() { CurrentTarget }));
                            ((Architect)CurrentTarget).Fractallize(999999);
                        }
                        else
                        {
                            Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " resists the fractallization!", Color.Purple, new List<Entity>() { CurrentTarget }));
                        }
                    }
                    else
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " is enveloped in fractals, but does not fade.", Color.Purple, new List<Entity>() { CurrentTarget }));
                    }
                }
                else if (Spell == "extract")
                {
                    CooldownCycles += (int)Math.Round(20 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} speaks the name of " + CurrentTarget.ReferredToNames[0] + "...", Color.Purple, new List<Entity>() { this, CurrentTarget }));

                    if (CurrentTarget is Object && Game1.GameWorld.FractalObjects.Contains((Object)CurrentTarget))
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " reappears in a web of fractals!", Color.Purple, new List<Entity>() { CurrentTarget }));
                        ((Object)CurrentTarget).RematerializeLocation = (Location.Region, Location, District, Block, Structure, Room);
                        ((Object)CurrentTarget).FractalCycles = 0;

                    }
                    else if (CurrentTarget is Architect && Game1.GameWorld.FractalArchitects.Contains((Architect)CurrentTarget))
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " reappears in a web of fractals!", Color.Purple, new List<Entity>() { CurrentTarget }));
                        ((Architect)CurrentTarget).RematerializeLocation = (Location.Region, Location, District, Block, Structure, Room);
                        ((Architect)CurrentTarget).FractalCycles = 0;
                    }
                    else
                    {
                        Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple, new List<Entity>()));
                    }
                }
                else if (Spell == "revive")
                {
                    CooldownCycles += (int)Math.Round(100 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} speaks the name of " + CurrentTarget.ReferredToNames[0] + "...", Color.Purple, new List<Entity>() { this, CurrentTarget }));

                    if (CurrentTarget is Architect && ((Architect)CurrentTarget).IsAlive == false && (((Architect)CurrentTarget).Block == Block && ((Architect)CurrentTarget).Room == this.Room))
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " rises from the dead!", Color.Purple, new List<Entity>() { CurrentTarget }));
                        ((Architect)CurrentTarget).IsAlive = true;
                        ((Architect)CurrentTarget).IsImmortal = true;
                        ((Architect)CurrentTarget).Energy = Math.Min(50, ((Architect)CurrentTarget).MaxEnergy());
                    }
                    else
                    {
                        Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple, new List<Entity>()));
                    }
                }
                else if (Spell == "ressurect")
                {
                    CooldownCycles += (int)Math.Round(500 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} speaks the name of " + CurrentTarget.ReferredToNames[0] + " and meditates...", Color.Purple, new List<Entity>() { this, CurrentTarget }));

                    if (CurrentTarget is Architect && ((Architect)CurrentTarget).IsAlive == false && (((Architect)CurrentTarget).Block == Block && ((Architect)CurrentTarget).Room == this.Room))
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " is surrounded in crystals and returns from the dead!", Color.Purple, new List<Entity>() { CurrentTarget }));
                        ((Architect)CurrentTarget).IsAlive = true;
                        ((Architect)CurrentTarget).IsImmortal = true;
                        ((Architect)CurrentTarget).Energy = 100;

                        foreach (Object o in ((Architect)CurrentTarget).BodyParts)
                        {
                            o.Integrity = 100;
                        }
                    }
                    else
                    {
                        Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple, new List<Entity>()));
                    }
                }
                else if (Spell == "animate")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} conjures a spark of necromantic energy and passes it to " + CurrentTarget.ReferredToNames[0] + "...", Color.Purple, new List<Entity>() { this, CurrentTarget }));

                    if (CurrentTarget is Architect architect)
                    {
                        architect.RaiseFromTheDead(this, CurrentTarget.ReferredToNames[0], PathOfDeathLevel, 2);

                        if (architect.IsAlive)
                        {
                            if (!Game1.GamePlayerParty.Architects.Contains(architect))
                            {
                                Game1.GamePlayerParty.Architects.Add(architect);
                            }
                        }
                    }
                    else
                    {
                        Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple, new List<Entity>()));
                    }
                }

                else if (Spell == "ethereal rupture")
                {
                    RuptureMode = true;
                }
                else if (Spell == "emergence")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} holds out a hand and speaks the name of " + CurrentTarget.ReferredToNames[0] + "...", Color.Purple, new List<Entity>() { this, CurrentTarget }));

                    if (!(CurrentTarget is Architect) || ((Architect)CurrentTarget).IsAlive == true)
                    {
                        Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple, new List<Entity>()));
                    }
                    else
                    {
                        Architect target = (Architect)CurrentTarget;

                        Announcements.Add(new TextStorage(CurrentTarget.Name + " appears in front of them!", Color.Purple, new List<Entity>() { CurrentTarget }));

                        target.Block = Block;

                        target.BodyParts.Clear();
                        target.AddBodyParts();

                        target.Inventory = new List<Object>();
                        target.Clothing = new List<Object>();
                        target.Energy = target.MaxEnergy();

                        if (Room != null)
                        {
                            Room.Architects.Add(target);
                            target.Room = Room;
                        }
                        else
                        {
                            Block.Architects.Add(target);
                        }
                    }
                }
                else if (Spell == "eternal bind")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} stares deeply into " + CurrentTarget.ReferredToNames[0] + "'s eyes...", Color.Purple, new List<Entity>() { this, CurrentTarget }));

                    if ((!(CurrentTarget is Architect) || ((Architect)CurrentTarget).IsAlive == false) && ((Architect)CurrentTarget).Block == Block && ((Architect)CurrentTarget).Room == Room)
                    {
                        Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple, new List<Entity>()));
                    }
                    else
                    {
                        Architect target = (Architect)CurrentTarget;

                        Announcements.Add(new TextStorage(CurrentTarget.Name + " stares back in awe...", Color.Purple, new List<Entity>() { CurrentTarget }));

                        target.ChangeOpinion(this, 999999);

                        if (target.TargetArchitect == this && (target.Task == "killtarget" || target.Task == "disabletarget"))
                        {
                            target.Task = "";
                            target.TargetArchitect = null;
                        }

                        if (!Game1.GamePlayerParty.Architects.Contains(target))
                        {
                            Game1.GamePlayerParty.Architects.Add(target);
                        }
                    }
                }
                else if (Spell == "expunge")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} gestures agressively...", Color.Purple, new List<Entity>() { this }));


                    if (CurrentTarget is Civilization)
                    {
                        Announcements.Add(new TextStorage($"{CurrentTarget.Name} and its legacy have fallen...", Color.Purple, new List<Entity>() { CurrentTarget }));

                        foreach (Location l in Game1.GameWorld.AllLocations)
                        {
                            if (l.HomeCivilization == CurrentTarget)
                            {
                                l.Region.MyLocation = null;
                            }
                        }
                    }
                    else if (Game1.GameWorld.AllSpells.Contains(CurrentTarget))
                    {
                        Announcements.Add(new TextStorage($"The knowledge of {CurrentTarget.Name} has been erased from the land...", Color.Purple, new List<Entity>() { CurrentTarget }));
                        Game1.GameWorld.DeletedSpells.Add(CurrentTarget);

                        foreach (Architect a in Game1.GameWorld.AllArchitects)
                        {
                            a.SpellsKnown.Remove(CurrentTarget);
                        }
                    }
                    else if (Game1.GameWorld.AllLegendarySpells.Contains(CurrentTarget))
                    {
                        Announcements.Add(new TextStorage($"An accursed relic locks this spell away. Perhaps you can find and banish this artifact instead.", Color.Purple, new List<Entity>()));
                    }
                    else if (CurrentTarget is Blight)
                    {
                        Announcements.Add(new TextStorage($"{CurrentTarget.Name} has been entirely purified...", Color.Purple, new List<Entity>() { CurrentTarget }));

                        for (int x = 0; x < Game1.GameWorld.Width; x++)
                        {
                            for (int z = 0; z < Game1.GameWorld.Width; z++)
                            {
                                if (Game1.GameWorld.WorldMap[x + z * Game1.GameWorld.Width].Blight == CurrentTarget)
                                {
                                    Game1.GameWorld.WorldMap[x + z * Game1.GameWorld.Width].Blight = Game1.GameWorld.Purity;
                                }
                            }
                        }
                    }
                    else if (CurrentTarget is Composition)
                    {
                        Announcements.Add(new TextStorage($"The knowledge of {CurrentTarget.Name} has been erased from the land...", Color.Purple, new List<Entity>() { CurrentTarget }));

                        foreach (Architect a in Game1.GameWorld.AllArchitects)
                        {
                            a.CultureBank.Remove((Composition)CurrentTarget);
                        }

                        Game1.GameWorld.DeletedCompositions.Add((Composition)CurrentTarget);
                    }
                    else if (CurrentTarget is Deity)
                    {
                        Announcements.Add(new TextStorage($"You feel an intense pain...", Color.Purple, new List<Entity>()));
                        Energy = 1;
                    }
                    else if (CurrentTarget is District)
                    {
                        Announcements.Add(new TextStorage($"{CurrentTarget.Name} detaches from the ground and levitates into infinite nothing.", Color.Purple, new List<Entity>() { CurrentTarget }));

                        ((District)CurrentTarget).Location.Districts.Remove(((District)CurrentTarget));

                        if (Game1.GamePlayerParty.Architects.Contains(this))
                        {
                            foreach (Architect a in Game1.GamePlayerParty.Architects)
                            {
                                if (a.District == CurrentTarget)
                                {
                                    Game1.MakeObservation(a.Name + " was successfully teleported into oblivion. How embarrassing...", Color.Magenta, new List<Entity>() { a });
                                    a.IsAlive = false;
                                    Game1.GamePlayerParty.Architects.Remove(a);

                                    if (Game1.GamePlayerParty.Architects.Count == 0)
                                    {
                                        Game1.GameState = "dead";

                                        if (Game1.GamePlayerParty.Architects.Count == 0)
                                        {
                                            Game1.GameState = "dead";
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (CurrentTarget is Location)
                    {
                        Announcements.Add(new TextStorage($"{CurrentTarget.Name} detaches from the ground and levitates into infinite nothing.", Color.Purple, new List<Entity>() { CurrentTarget }));

                        ((Location)CurrentTarget).Region.MyLocation = null;

                        if (Game1.GamePlayerParty.Architects.Contains(this))
                        {
                            foreach (Architect a in Game1.GamePlayerParty.Architects)
                            {
                                if (a.Location == (Location)CurrentTarget)
                                {
                                    Game1.MakeObservation(a.Name + " was successfully teleported into oblivion. How embarrassing...", Color.Magenta, new List<Entity>() { a });
                                    a.IsAlive = false;
                                    if (Game1.GamePlayerParty.Architects.Contains(a))
                                    {
                                        Game1.GamePlayerParty.Architects.Remove(a);

                                        if (Game1.GamePlayerParty.Architects.Count == 0)
                                        {
                                            Game1.GameState = "dead";

                                            if (Game1.GamePlayerParty.Architects.Count == 0)
                                            {
                                                Game1.GameState = "dead";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (CurrentTarget is Party)
                    {
                        Announcements.Add(new TextStorage($"Your party has disbanded.", Color.Purple, new List<Entity>()));

                        List<Architect> ArchitectsToBanish = new List<Architect>();
                        foreach (Architect a in Game1.GamePlayerParty.Architects)
                        {
                            if (a != this)
                            {
                                ArchitectsToBanish.Add(a);
                            }
                        }

                        foreach (Architect a in ArchitectsToBanish)
                        {
                            Game1.GamePlayerParty.Architects.Remove(a);
                        }
                    }
                    else if (CurrentTarget is Group)
                    {
                        //disbands the group, removes it from any power, does not kill the members
                        Announcements.Add(new TextStorage($"{CurrentTarget.ReferredToNames[0]}'s relationship has fractured.", Color.Purple, new List<Entity>() { CurrentTarget }));

                        Game1.GameWorld.Groups.Remove((Group)CurrentTarget);
                        Game1.GameWorld.TradingGroups.Remove((Group)CurrentTarget);

                        foreach (Location l in Game1.GameWorld.AllLocations)
                        {
                            if (l.Government == CurrentTarget)
                            {
                                l.Government = null;
                            }
                        }

                        foreach (Architect a in ((Group)CurrentTarget).Architects)
                        {
                            a.Group = null;
                        }
                    }
                    else if (CurrentTarget is Architect)
                    {
                        Announcements.Add(new TextStorage($"{CurrentTarget.ReferredToNames[0]} is banished and forgotten.", Color.Purple, new List<Entity>() { CurrentTarget }));


                        Architect a = (Architect)CurrentTarget;

                        if (District.Architects.Contains(a))
                        {
                            District.Architects.Remove(a);
                        }

                        Game1.GameWorld.AllArchitects.Remove(a);

                        foreach (Architect A in Game1.GameWorld.AllArchitects)
                        {
                            A.ArchitectOpinions.RemoveAll(opinion => opinion.Item1 == a);
                        }

                        if (Game1.GameWorld.Colossals.Contains(a))
                        {
                            Game1.GameWorld.Colossals.Remove(a);
                        }

                        if (Game1.LoadedArchitects.Contains(a))
                        {
                            Game1.LoadedArchitects.Remove(a);
                        }

                        foreach (Location l in Game1.GameWorld.AllLocations)
                        {
                            if (l.Government == a)
                            {
                                l.Government = null;
                            }
                        }

                        for (int x = 0; x < Game1.GameWorld.Width; x++)
                        {
                            for (int z = 0; z < Game1.GameWorld.Width; z++)
                            {
                                foreach (InteractableEvent e in Game1.GameWorld.WorldMap[x + z * Game1.GameWorld.Width].Events)
                                {
                                    if (e.GuarranteedArchitects.Contains(a))
                                    {
                                        e.GuarranteedArchitects.Remove(a);
                                        break;
                                    }
                                }
                            }
                        }

                        a.IsAlive = false;
                        a.Location = null;
                        a.District = null;

                        if (a.Room != null)
                        {
                            a.Room.Architects.Remove(a);
                            a.DropInventory(false);
                            a.Room = null;
                        }
                        else if (a.Block != null)
                        {
                            a.Block.Architects.Remove(a);
                            a.DropInventory(false);
                            a.Block = null;
                        }

                        if (Game1.GamePlayerParty.Architects.Contains(a))
                        {
                            Game1.GamePlayerParty.Architects.Remove(a);
                        }

                        if (a.Group != null)
                        {
                            if (a.Group.Leader == a)
                            {
                                //disbands the group, removes it from any power, does not kill the members
                                Announcements.Add(new TextStorage($"{CurrentTarget.ReferredToNames[0]}'s relationship has fractured.", Color.Purple, new List<Entity>() { CurrentTarget }));

                                Game1.GameWorld.Groups.Remove((Group)CurrentTarget);
                                Game1.GameWorld.TradingGroups.Remove((Group)CurrentTarget);

                                foreach (Location l in Game1.GameWorld.AllLocations)
                                {
                                    if (l.Government == CurrentTarget)
                                    {
                                        l.Government = null;
                                    }
                                }

                                foreach (Architect A in ((Group)CurrentTarget).Architects)
                                {
                                    A.Group = null;
                                }
                            }
                            else
                            {
                                a.Group.Architects.Remove(a);
                            }
                        }

                        if (Game1.GameWorld.Hypernexus == a)
                        {
                            Game1.GameWorld.Hypernexus = null;
                        }
                        if (Game1.GameWorld.Icosidodecahedron == a)
                        {
                            Game1.GameWorld.Icosidodecahedron = null;
                        }
                        if (Game1.GameWorld.Shadeheart == a)
                        {
                            Game1.GameWorld.Shadeheart = null;
                        }

                    }
                    else if (CurrentTarget is Object)
                    {
                        Announcements.Add(new TextStorage($"{CurrentTarget.ReferredToNames[0]} collapses into a singularity.", Color.Purple, new List<Entity>() { CurrentTarget }));

                        //delete it from all architects, historical objects, and other stuff. If it ever exists in the world elsewhere, we will just delete it when it gets loaded.

                        Game1.GameWorld.DeletedObjects.Add((Object)CurrentTarget);

                        foreach (Architect a in Game1.GameWorld.AllArchitects)
                        {
                            if (a.Inventory.Contains(CurrentTarget))
                            {
                                a.Inventory.Remove((Object)CurrentTarget);
                                break;
                            }
                            else if (a.OffHeldObject == CurrentTarget)
                            {
                                a.OffHeldObject = null;
                                break;
                            }
                            else if (a.MainHeldObject == CurrentTarget)
                            {
                                a.MainHeldObject = null;
                                break;
                            }
                        }
                    }
                    else if (CurrentTarget is Material)
                    {
                        //add the material to a list of banished materials. Replace objects iwth these materials with Void Energy, a new material when they update. You cannot cast this spell on Void Material.
                        Announcements.Add(new TextStorage($"{CurrentTarget.ReferredToNames[0]}'s properties have been reduced to void.", Color.Purple, new List<Entity>() { CurrentTarget }));

                        Game1.GameWorld.DeletedMaterials.Add((Material)CurrentTarget);
                    }
                    else if (CurrentTarget is Race)
                    {
                        Announcements.Add(new TextStorage($"The members of {CurrentTarget.ReferredToNames[0]} have been reduced to indistinguishable lifeforms.", Color.Purple, new List<Entity>() { CurrentTarget }));
                        //a banished race makes all members of that race shades. this is stored in a static list.

                        Game1.GameWorld.DeletedRaces.Add((Race)CurrentTarget);
                    }
                    else if (CurrentTarget is Structure)
                    {
                        Announcements.Add(new TextStorage($"{CurrentTarget.Name} vanishes.", Color.Purple, new List<Entity>() { CurrentTarget }));

                        Structure s = ((Structure)CurrentTarget);

                        foreach (Room r in s.Rooms)
                        {
                            foreach (Architect a in r.Architects)
                            {
                                s.Block.Architects.Add(a);
                                a.Room = null;
                                a.Structure = null;
                            }
                            foreach (Object o in r.Objects)
                            {
                                s.Block.Objects.Add(o);
                                o.Room = null;
                            }
                        }

                        if(s.Block.District.IsLoaded)
                        {
                            foreach (Object o in s.HistoricalObjects)
                            {
                                s.Block.Objects.Add(o);
                                o.Room = null;
                            }
                        }
                    }
                    else if (CurrentTarget is World)
                    {
                        Game1.GameState = "mainscreen";
                    }
                    else
                    {
                        Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple, new List<Entity>()));
                    }
                }
                else if (Spell == "echo")
                {
                    Announcements.Add(new TextStorage("You manifest spatial particles...", Color.Purple, new List<Entity>()));

                    if (CurrentTarget is Architect)
                    {
                        Architect Base = (Architect)CurrentTarget;
                        string NameAlteration = Game1.GameWorld.GenerateUniqueName("1S6s", new Entity("this is a tag for a clone"));
                        Architect Clone = new Architect(CurrentTarget.Name + " " + NameAlteration, Base.Sex, Base.Race, Base.Age, Base.Profession, new List<Object>(), Base.Location, Base.District, Base.Block, Base.Destiny, Base.Level);
                        Game1.GameWorld.AllArchitects.Add(Clone);

                        Clone.Clothing.Clear();

                        Clone.MoralCompass = Base.MoralCompass;
                        Clone.StabilityCompass = Base.StabilityCompass;
                        Clone.PropertyValue = Base.PropertyValue;
                        Clone.FamilyValue = Base.FamilyValue;
                        Clone.PowerValue = Base.PowerValue;
                        Clone.MoneyValue = Base.MoneyValue;
                        Clone.KnowledgeValue = Base.KnowledgeValue;
                        Clone.SpiritualityValue = Base.SpiritualityValue;
                        Clone.ProwessValue = Base.ProwessValue;
                        Clone.PatriotismValue = Base.PatriotismValue;
                        Clone.CourageValue = Base.CourageValue;
                        Clone.CreativityValue = Base.CreativityValue;

                        Clone.Dexterity = Base.Dexterity;
                        Clone.Strength = Base.Strength;
                        Clone.Charisma = Base.Charisma;
                        Clone.Focus = Base.Focus;
                        Clone.Endurance = Base.Endurance;
                        Clone.Creativity = Base.Creativity;
                        Clone.Agility = Base.Agility;


                        Clone.CultureBank = new List<Composition>(Base.CultureBank);

                        // Cloning XPValues
                        Clone.XPValues = new List<(string, int)>(Base.XPValues);

                        // Cloning KnownArchitectsAndOpinions
                        Clone.ArchitectOpinions = new List<(Architect, int)>(Base.ArchitectOpinions);

                        if (Base.District.IsLoaded)
                        {
                            if (Base.Room != null)
                            {
                                Base.Room.Architects.Add(Clone);
                            }
                            else
                            {
                                Base.Block.Architects.Add(Clone);
                            }
                        }
                        else
                        {
                            Base.District.Architects.Add(Clone);
                        }


                        Announcements.Add(new TextStorage("An echo of " + CurrentTarget.ReferredToNames[0] + " appears!", Color.Purple, new List<Entity>() { CurrentTarget }));
                    }
                    else if (CurrentTarget is Object)
                    {
                        Announcements.Add(new TextStorage("You manifest spatial particles...", Color.Purple, new List<Entity>()));
                        Object Base = (Object)CurrentTarget;
                        Object Clone = new Object();

                        // Cloning simple properties
                        Clone.Type = Base.Type;
                        Clone.Materials = new List<Material>(Base.Materials); // Assuming Material objects do not need deep cloning
                        Clone.Description = Base.Description;
                        Clone.IsContainer = Base.IsContainer;
                        Clone.ContainedObjects = new List<Object>(Base.ContainedObjects); // Shallow copy of the list
                        Clone.IfTrueUseInIfFalseUseOn = Base.IfTrueUseInIfFalseUseOn;
                        Clone.YLevelInFeet = Base.YLevelInFeet;
                        Clone.YVelocity = Base.YVelocity;
                        Clone.Weight = Base.Weight;
                        Clone.Block = Base.Block;
                        Clone.Room = Base.Room;
                        Clone.HeatInCelsius = Base.HeatInCelsius;
                        Clone.IsConsumable = Base.IsConsumable;
                        Clone.VariableToChange = Base.VariableToChange;
                        Clone.VariableChange = Base.VariableChange;
                        Clone.IsWearable = Base.IsWearable;
                        Clone.Rarity = Base.Rarity;
                        Clone.IsBodyPart = Base.IsBodyPart;
                        Clone.MajorArteryIsSevered = Base.MajorArteryIsSevered;
                        Clone.AirborneTarget = Base.AirborneTarget;
                        Clone.AirbornePower = Base.AirbornePower;
                        Clone.AirborneCyclesToHitTarget = Base.AirborneCyclesToHitTarget;
                        Clone.Creator = Base.Creator;
                        Clone.FireCycles = Base.FireCycles;
                        Clone.WetCycles = Base.WetCycles;
                        Clone.DestabilizedCycles = Base.DestabilizedCycles;
                        Clone.FractalCycles = Base.FractalCycles;
                        Clone.RematerializeLocation = Base.RematerializeLocation;
                        Clone.IsCoveredInPlants = Base.IsCoveredInPlants;
                        Clone.CoverageValues = new List<(string, int)>(Base.CoverageValues);
                        Clone.Coverage = Base.Coverage;
                        Clone.CoverageName = Base.CoverageName;
                        Clone.IsWeapon = Base.IsWeapon;
                        Clone.DamageType = Base.DamageType;
                        Clone.ProjectileAerodynamic = Base.ProjectileAerodynamic;
                        Clone.Strength = Base.Strength;
                        Clone.Dissipating = Base.Dissipating;
                        Clone.Integrity = Base.Integrity;
                        Clone.IsWritable = Base.IsWritable;
                        Clone.SpecialKnowledge = Base.SpecialKnowledge;
                        Clone.IsGeneralGood = Base.IsGeneralGood;

                        // Handling exceptions (deep cloning)
                        Clone.Imbuements = Base.Imbuements.Select(imb => new Imbuement(imb.IsTrigger, imb.ConditionOrTrigger, imb.BuffOrResult, imb.FirstPower, imb.SecondPower)).ToList();
                        Clone.CompositionContent = Base.CompositionContent;
                        Clone.Thrower = Base.Thrower;
                        Clone.Owner = Base.Owner;



                        foreach (Architect a in Game1.GameWorld.AllArchitects)
                        {
                            if (a.District.IsLoaded)
                            {
                                if (a.OffHeldObject == Base)
                                {
                                    if (a.Room != null)
                                    {
                                        a.Room.Objects.Add(Clone);
                                    }
                                    else
                                    {
                                        a.Block.Objects.Add(Clone);
                                    }
                                }
                                else if (a.MainHeldObject == Base)
                                {
                                    if (a.Room != null)
                                    {
                                        a.Room.Objects.Add(Clone);
                                    }
                                    else
                                    {
                                        a.Block.Objects.Add(Clone);
                                    }
                                }
                                else if (a.Inventory.Contains(Base))
                                {
                                    // Adding to the same location in the inventory where the base object was found
                                    int index = a.Inventory.IndexOf(Base);
                                    a.Inventory.Insert(index + 1, Clone); // Insert clone right after the base object
                                }
                                else if (a.Clothing.Contains(Base))
                                {
                                    // If the base object is found in the clothing, we drop the clone on the ground instead of inserting it back into clothing
                                    if (a.Room != null)
                                    {
                                        // If the architect is in a room, add the clone to the room's objects
                                        a.Room.Objects.Add(Clone);
                                    }
                                    else
                                    {
                                        // If the architect is not in a room but is in a block, add the clone to the block's objects
                                        a.Block.Objects.Add(Clone);
                                    }
                                }

                            }
                            else
                            {
                                // When the location is not loaded, and you need to check if any of the conditions for holding or carrying the base object are met
                                bool anyConditionMet = a.OffHeldObject == Base || a.MainHeldObject == Base || a.Inventory.Contains(Base) || a.Clothing.Contains(Base);
                                if (anyConditionMet)
                                {
                                    // Assuming we default to adding to the inventory when the location is not loaded
                                    a.Inventory.Add(Clone);
                                }
                            }
                        }

                        Announcements.Add(new TextStorage("An echo of " + CurrentTarget.ReferredToNames[0] + " appears!", Color.Purple, new List<Entity>() { CurrentTarget }));
                    }
                    else
                    {
                        Announcements.Add(new TextStorage("You manifest spatial particles...", Color.Purple, new List<Entity>()));
                        Announcements.Add(new TextStorage("...but they just aren't strong enough. The spell only works on Architects and Objects.", Color.Purple, new List<Entity>()));
                    }
                }
            }

            foreach (Imbuement i in CurrentlyActiveImbuements)
            {
                if (i.IsTrigger && i.ConditionOrTrigger == "oncast")
                {
                    TextStorage result = ActivatePower(i.BuffOrResult);
                    if (result.Data != "unknown")
                    {
                        Announcements.Add(result);
                    }
                }
            }

            return Announcements;
        }

        public TextStorage ActivatePower(string Power)
        {
            List<Architect> hostiles = new List<Architect>();
            IEnumerable<Architect> architects = Room != null ? Room.Architects : Block.Architects;

            foreach (Architect a in architects)
            {
                if (((a.Task == "killtarget" || a.Task == "disabletarget") && a.TargetArchitect == this) || ((Task == "killtarget" || Task == "disabletarget") && this.TargetArchitect == a))
                {
                    hostiles.Add(a);
                }
            }

            hostiles.Sort((a, b) => a.GetDistance(this).CompareTo(b.GetDistance(this)));

            if (Power == "barrier")
            {
                BarrierStacks++;
                return new TextStorage(ReferredToNames[0] + " generates a barrier stack!", Color.Magenta, new List<Entity>() { this });
            }
            else if (Power == "projectile" && hostiles.Count > 0)
            {
                List<Object> objects = Room != null ? Room.Objects : Block.Objects;
                Architect target = hostiles[0];

                Object o = new Object(null, "energy bolt", new List<Material>() { new Material("energy", "energy", 3, 0, "white") }, this);
                objects.Add(o);
                o.AirborneTarget = target;
                o.Thrower = this;
                o.AirborneCyclesToHitTarget = 15 - Focus;
                return new TextStorage(ReferredToNames[0] + " fires a bolt at " + target.ReferredToNames[0] + "!", Color.Magenta, new List<Entity>() { this, target });
            }
            else if (Power == "ignite" && hostiles.Count > 0)
            {
                Architect target = hostiles[0];
                target.FireSeconds += 3;
                return new TextStorage(ReferredToNames[0] + " ignites " + target.ReferredToNames[0] + "!", Color.Magenta, new List<Entity>() { this, target });
            }
            else if (Power == "destabilize" && hostiles.Count > 0)
            {
                Architect target = hostiles[0];
                target.DestabilizedCycles += 20;
                return new TextStorage(ReferredToNames[0] + " destabilizes " + target.ReferredToNames[0] + "!", Color.Magenta, new List<Entity>() { this, target });
            }
            else if (Power == "dismiss")
            {
                DismissalCycles += 30;
                return new TextStorage(ReferredToNames[0] + " becomes partially intangible!", Color.Magenta, new List<Entity>() { this });
            }
            else
            {
                return new TextStorage("unknown", Color.Magenta, new List<Entity>() { this });
            }
        }


        public void Fractallize(int Cycles)
        {
            DropInventory(true);
            FractalCycles = Cycles;
            RematerializeLocation = (this.Location.Region, this.Location, this.District, this.Block, this.Structure, this.Room);
            Location.Region.World.FractalArchitects.Add(this);

            if (Room != null)
            {
                Room.Architects.Remove(this);
            }
            else if (Block != null)
            {
                Block.Architects.Remove(this);
            }

            if(Game1.LoadedArchitects.Contains(this))
            {
                Game1.LoadedArchitectsToRemove.Add(this);
            }
        }

        public void Move(string direction)
        {
            var directionOffsets = new Dictionary<string, (int dx, int dz)>
            {
                {"north", (0, -1)},
                {"northeast", (1, -1)},
                {"east", (1, 0)},
                {"southeast", (1, 1)},
                {"south", (0, 1)},
                {"southwest", (-1, 1)},
                {"west", (-1, 0)},
                {"northwest", (-1, -1)}
            };

            if (directionOffsets.TryGetValue(direction, out var offset))
            {
                int newX = Block.X + offset.dx;
                int newZ = Block.Z + offset.dz;

                // Handle boundary and travel logic
                if (newX < 0 || newX >= 7 || newZ < 0 || newZ >= 7)
                {
                    if (CurrentlyMovingPlace == direction)
                    {
                        if (Game1.GamePlayerParty.Architects.Contains(this))
                        {
                            TryingToTravel = true;
                            bool allTryingToTravel = Game1.GamePlayerParty.Architects.All(a => a.TryingToTravel);

                            if (allTryingToTravel)
                            {
                                Game1.GameState = "travelmenu";
                                Game1.GamePlayerParty.ClearSkillData();
                                Game1.GamePlayerParty.MapCursorDistrict = 0;
                                Game1.MapCursorX = Location.X;
                                Game1.MapCursorZ = Location.Z;

                                Game1.GameWorld.RevealNearbyTiles(Game1.MapCursorX, Game1.MapCursorZ);

                                Game1.GamePlayerParty.Architects[0].District.Unload();

                                foreach (var architect in Game1.GamePlayerParty.Architects)
                                {
                                    architect.CurrentlyMovingPlace = "none"; // Reset movement place after successful travel
                                }
                            }
                        }
                        else
                        {
                            // Non-party members logic for moving to a new district/location
                            Game1.LoadedArchitects.Remove(this);

                            if (Location != Target.Item2)
                            {
                                NextMigrationLocation = Target.Item2;
                            }
                            else if (District != Target.Item3)
                            {
                                District.Architects.Remove(this);
                                Target.Item3.Architects.Add(this);
                                District = Target.Item3;
                            }
                        }

                        CurrentlyMovingPlace = "none";  // Reset after successful movement
                    }
                    else
                    {
                        // Set or update CurrentlyMovingPlace to the new intended direction for boundary crossing
                        CurrentlyMovingPlace = direction;
                        CooldownCycles += (int)Math.Round(25 / Speed());
                    }
                }
                else
                {
                    // Handle in-district movement
                    if (CurrentlyMovingPlace == direction)
                    {
                        if (CombatCycles == 0 || new Random().Next(100) <= EscapeChance())
                        {
                            Block.Architects.Remove(this);
                            Block = District.DistrictMap[newX + newZ * 7];
                            Block.Architects.Add(this);

                            foreach (Object o in BodyParts)
                            {
                                o.UpdateExposure(-9999);
                            }

                            // Update structures in the new block
                            foreach (Structure s in Block.Structures)
                            {
                                if (s.Type != "house" && s.Type != "bighouse")
                                {
                                    if(Game1.GamePlayerParty.Architects.Contains(this))
                                    {
                                        AnnounceToParty(s.GetStructureDescription(), Color.DarkGray, new List<Entity>() { s });
                                    }
                                }
                            }

                            CurrentlyMovingPlace = "none";  // Reset after successful movement
                            CooldownCycles += (int)Math.Round(25 / Speed());
                        }
                        else
                        {
                            if (Game1.GamePlayerParty.Architects.Contains(this))
                            {
                                AnnounceToParty("You struggle to escape, and fail!", Color.OrangeRed, new List<Entity>());
                            }
                            CooldownCycles += (int)Math.Round(25 / Speed());
                        }
                    }
                    else
                    {
                        // Set or update CurrentlyMovingPlace to the new intended direction
                        CurrentlyMovingPlace = direction;
                        CooldownCycles += (int)Math.Round(25 / Speed());
                    }
                }
            } 
            else
            {
                AnnounceToParty("You can't go that \"way\".", Color.Yellow, new List<Entity>());
            }
        }

        public void MoveThroughDoor(string doorId)
        {
            Door door = Room?.Objects.OfType<Door>().FirstOrDefault(d => d.ID.ToString() == doorId) ??
                        Block?.Objects.OfType<Door>().FirstOrDefault(d => d.ID.ToString() == doorId);
            if (door != null)
            {
                CooldownCycles += (int)(Math.Round(20 / Speed()));
                if ((Room?.Objects.Contains(door) == true || Block?.Objects.Contains(door) == true) && (CombatCycles == 0 || Game1.r.Next(100) <= EscapeChance()))
                {
                    // Announce exit from the current room
                    foreach (var a in Room.Architects.ToList())
                    {
                        if (Game1.GamePlayerParty.Architects.Contains(a))
                        {
                            AnnounceToParty($"{ReferredToNames[0]} exits the room.", Color.OrangeRed, new List<Entity> { this });
                        }
                    }

                    Room?.Architects.Remove(this);
                    Room = door.DestinationRoom;
                    Room.Architects.Add(this);

                    // Announce entry to the new room
                    foreach (var a in Room.Architects.ToList())
                    {
                        if (Game1.GamePlayerParty.Architects.Contains(a))
                        {
                            AnnounceToParty($"{ReferredToNames[0]} enters the room.", Color.Yellow, new List<Entity> { this });
                        }
                    }

                    CooldownCycles += (int)(Math.Round(25 / Speed()));
                }
                else
                {
                    AnnounceToParty($"{ReferredToNames[0]} struggles to escape, but fails!", Color.OrangeRed, new List<Entity> { this });
                    CooldownCycles += (int)Math.Round(25 / Speed());
                }
            }
            else
            {
                AnnounceToParty($"{ReferredToNames[0]} couldn't find anything like that in the area to enter.", Color.Yellow, new List<Entity> { this });
            }
        }
    }
}
