using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static System.Collections.Specialized.BitVector32;
using Color = Microsoft.Xna.Framework.Color;

namespace Lightrealm
{
    [Serializable]

    public class Architect : Entity
    {
        static List<string> LegendTypes = new List<string>() { "hunter", "adventurer", "assassin", "rogue", "artisan", "diplomat", "enchanter" };
        public static readonly List<string> ScaryTypes = new List<string>
    {
        "bastion",
        "commune",
        "core",
        "fort",
        "fortress",
        "heart",
        "keep",
        "monastery",
        "monument",
        "hoard",
        "outpost",
        "sanctum",
        "scaffold",
        "scum",
        "spire",
        "stronghold",
        "archway",
        "hallway",
        "toroid",
        "towers",
        "pyramid"
    };
        public string Sex { get; set; }
        public string Pronoun { get; set; }
        public string PossessivePronoun { get; set; }
        public string ObjectivePronoun { get; set; }
        public string FalsifiedName { get; set; } = "";
        public int PulseCharge { get; set; } = 0;


        public List<string> ActiveGifts = new List<string>();


        public void CancelSearch()
        {
            if(Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Any(a => a.SearchRoom != null) || Game1.GameWorld.GamePlayerAssociation.ActiveParty.RoomsUnsearched.Count > 0)
            {
                AnnounceToParty($"The search has been canceled as {Name} encountered danger!", Color.OrangeRed, new EntityList<Entity>() { this });
                foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                {
                    a.SearchRoom = null;
                }
                Game1.GameWorld.GamePlayerAssociation.ActiveParty.RoomsUnsearched.Clear();
            }
        }

        public void RunFragmentCheck()
        {
            // === Convert Fragments to Prisms ===

            var vitaliumFragments = Inventory
                .Where(item => item.Type == "fragment" && item.Name == null && item.Materials[0] == Game1.GameWorld.Vitalium)
                .ToList();

            int totalFragments = vitaliumFragments.Count;

            // Leave at least 50 fragments
            int usableFragments = totalFragments - 50;
            int prismConversions = Math.Max(0, usableFragments / 50);

            for (int set = 0; set < prismConversions; set++)
            {
                for (int i = 0; i < 50; i++)
                {
                    var fragment = vitaliumFragments[set * 50 + i];
                    Inventory.Remove(fragment);

                    Game1.ObjectsToDeleteOnUnload.Add(fragment);
                }

                Inventory.Add(new Object(null, "prism", new EntityList<Material> { Game1.GameWorld.Vitalium }, null));

                if (StepsTaken > 2)
                    CompanionMessage("prisms", "");
            }

            // === Deconvert Prisms to Fragments ===

            // Refresh counts after conversion
            int fragmentCount = Inventory.Count(item =>
                item.Type == "fragment" && item.Name == null && item.Materials[0] == Game1.GameWorld.Vitalium);

            int prismCount = Inventory.Count(item =>
                item.Type == "prism" && item.Name == null && item.Materials[0] == Game1.GameWorld.Vitalium);

            // We want at least 50 fragments. Deconvert prisms if needed.
            int missingFragments = 50 - fragmentCount;

            while (missingFragments > 0 && prismCount >= 1)
            {
                // Remove one prism
                var prismToRemove = Inventory.FirstOrDefault(item =>
                    item.Type == "prism" && item.Name == null && item.Materials[0] == Game1.GameWorld.Vitalium);

                if (prismToRemove == null)
                    break;

                Inventory.Remove(prismToRemove);
                Game1.ObjectsToDeleteOnUnload.Add(prismToRemove);
                prismCount--;

                // Add 50 fragments
                for (int i = 0; i < 50; i++)
                {
                    Inventory.Add(new Object(null, "fragment", new EntityList<Material> { Game1.GameWorld.Vitalium }, null));
                }

                fragmentCount += 50;
                missingFragments = 50 - fragmentCount;
            }
        }




        public string MostRecentHPLossReason = "";

        public static List<(string MainHand, string OffHand)> weaponPairs = new List<(string, string)>
        {
            ("shortsword", "shortsword"),
            ("shortsword", "shield"),
            ("shortsword", "dagger"),
            ("shortsword", ""),
            ("longsword", ""),
            ("longsword", ""),
            ("battle axe", "battle axe"),
            ("battle axe", "shield"),
            ("battle axe", ""),
            ("greataxe", ""),
            ("greataxe", ""),
            ("rapier", ""),
            ("rapier", "shield"),
            ("rapier", "dagger"),
            ("spear", ""),
            ("spear", "spear"),
            ("spear", "shield"),
            ("pike", ""),
            ("pike", ""),
            ("mace", "shield"),
            ("mace", ""),
            ("mace", "mace"),
            ("war hammer", ""),
            ("war hammer", "war hammer"),
            ("war hammer", "shield"),
            ("whip", ""),
            ("whip", "whip"),
            ("scourge", ""),
            ("scourge", "shield"),
            ("whip", "shield")
        };

        public List<string> KitsAcquired = new List<string>();

        public double LastDivinationCycle = 0;
        public int ShrineUsesLeft = 3;

        public (Location, District, Block, Room) NearestTavernThisLoad = (null, null, null, null);

        public bool InvisibleCheck
        {
            get => Game1.r.Next(0, 100) < ExtraStealth;
        }

        public bool BlindCheck
        {
            get => Game1.r.Next(0, 100) > BlindCycles;
        }

        public List<string> Powers = new List<string>();

        public bool SelfPopulated = false;

        public Architect Mother;
        public Architect Father;
        public Architect PaternalGrandMother;
        public Architect PaternalGrandFather;
        public Architect MaternalGrandMother;
        public Architect MaternalGrandFather;
        public EntityList<Architect> Siblings = new EntityList<Architect>();

        public List<int> IDsICanThank = new List<int>();
        public List<int> IDsICanApologizeTo = new List<int>();
        public List<int> IDsICanGoodbye = new List<int>();

        public bool ImportantThisLoad = true;

        public bool PartyActive = true;

        public bool EscapingStructure = false;


        public EntityList<Object> Eyes = new EntityList<Object>();

        public int HealStrikes = 0;

        public Architect ArchitectILookLike;

        public int ClothingValueLastTick = 0;

        public bool InFlight = false;
        public int FlightTicks = 200;

        public int TimeSinceAttacked = 100;


        public bool Significant = false;

        public string Brand;
        public string BrandColor;

        public static List<string> AllPersonalities = new List<string>
    {
        //"tactician",
        "arrogant",
        "sovereign",
        "soldier",
        "caretaker",
        "delusional",
        "laid-back",
        "optimist",
        "cynic",
        "cunning",
        "hothead",
        "survivalist",
        "analytic",
        "idealist",
        "hedonist",
        "competitor",
        "melancholic"
    };
        public List<string> Personalities { get; private set; } = new List<string>();
        public static readonly List<(string, string)> IncompatiblePersonalities = new List<(string, string)>
{
    ("optimist", "melancholic"),
    ("hothead", "laid-back"),
    ("soldier", "delusional"),
    ("survivalist", "dreamer"),
    ("cunning", "sovereign"),
    ("survivalist", "hedonist"),
    ("arrogant", "caretaker"),
    ("hedonist", "caretaker")
};


        public bool ReadyToTriggerMessageForCalamityInfoAfterReturn = false;

        public int LocationsVisited = 1;
        public int StepsTaken = 0;


        public bool HookSupposedToBeAlive = false;



        public int VoiceType;

        public bool TriedAscend = false;

        public string ColossalColoring = "";

        public bool IsNaturalWriter = false;

        public EntityList<Architect> Contacts = new EntityList<Architect>();

        public Unit Unit;

        public bool DeathProcessedStripped = false;

        public string LastCommand = "";
        public List<Entity> LastEntities = new List<Entity>();

        public void StepSound(int Count)
        {
            int AlreadySteps = Game1.SFX.Where(s => Game1.StepSounds.Contains(s)).Count();

            if(AlreadySteps < 2)
            {
                for (int i = 0; i < Count; i++)
                {
                    Game1.SFX.Add(Game1.StepSounds[Game1.GameWorld.rnd.Next(4)]);
                }
            }
        }

        public Entity CarryingEntity;

        public string MovementMode { get; set; } = "walking";

        public static void ShuffleGenList<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Game1.GameWorld.rnd.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        private int _hairID = Game1.GameWorld.rnd.Next(0, 10) switch
        {
            < 4 => 0,   // 40% chance for HairID to be 0
            < 7 => 1,   // 30% chance for HairID to be 1
            < 9 => 3,   // 20% chance for HairID to be 2
            _ => 2      // 10% chance for HairID to be 3
        };

        public int HairID
        {
            get => _hairID;
            set => _hairID = value;
        }


        public bool AbjuredThisDistrict = false;

        public List<double> RevitalizedDates { get; set; } = new List<double>();
        public List<string> SplitDates { get; set; } = new List<string>();
        public (string, Entity) NextMoveOrder { get; set; } = ("", null);
        public string TryDropItemType { get; set; } = "";
        public string TryPickUpItemType { get; set; } = "";
        public EntityList<Material> TryDropMaterials { get; set; } = new EntityList<Material>();
        public EntityList<Material> TryPickUpMaterials { get; set; } = new EntityList<Material>();

        public Dictionary<string, (string, EntityList<Entity>)> ResponseDatabase { get; set; } = new Dictionary<string, (string, EntityList<Entity>)>();

        public string TryTakeItemType { get; set; } = ""; // Item type for taking from a container
        public string TryPlaceItemType { get; set; } = ""; // Item type for placing into a container
        public EntityList<Material> TryTakeMaterials { get; set; } = new EntityList<Material>(); // Materials of the item being taken
        public EntityList<Material> TryPlaceMaterials { get; set; } = new EntityList<Material>(); // Materials of the item being placed
        public Object SelectedContainer { get; set; } = null; // The container the architect is interacting with for taking
        public Object SelectedContainerForPlacing { get; set; } = null; // The container the architect is interacting with for placing

        public bool HasAnyAbility()
        {
            return (PathOfStarsLevel >= 6) ||
                   (PathOfHeatLevel >= 2) ||
                   (PathOfStarsLevel >= 8) ||
                   (PathOfLightLevel >= 1) ||
                   (PathOfDeathLevel >= 4);
        }



        public int CountUniqueAbilities()
        {
            HashSet<string> abilityTypes = new HashSet<string>();

            // Check melee weapons
            if (MainHeldObject != null) abilityTypes.Add("melee");
            if (OffHeldObject != null) abilityTypes.Add("melee");
            if (MainInteractionAppendage != null) abilityTypes.Add("unarmed");
            if (OffInteractionAppendage != null) abilityTypes.Add("unarmed");

            // Check unique abilities
            if (PathOfStarsLevel >= 6) abilityTypes.Add("starstrike");
            if (PathOfHeatLevel >= 2) abilityTypes.Add("flamestrike");
            if (PathOfStarsLevel >= 8) abilityTypes.Add("starsmite");
            if (PathOfLightLevel >= 4) abilityTypes.Add("evoke strike");
            if (PathOfDeathLevel >= 4) abilityTypes.Add("fire spectral bolt");

            // Check spells
            var allowedSpells = new HashSet<string> { "water bolt", "chaos flare", "ice shock", "flash flame" };
            if (SpellsKnown != null)
            {
                foreach (var spell in SpellsKnown)
                {
                    if (allowedSpells.Contains(spell.Name.ToLower()))
                    {
                        abilityTypes.Add(spell.Name.ToLower());
                    }
                }
            }

            return abilityTypes.Count;
        }




        public static Dictionary<string,string> CompMessages = new Dictionary<string, string>
        {
            { "dangerousapproach", "[CLR]: This place seems dangerous. I hope you have a weapon prepared." },
            { "trymakeattack", "[CLR]: That person looks displeased. You can shift click their name to approach and basic attack them, or you can use the \"approach\" and \"directed attack\" actions for more control." },
            { "askaround", "[CLR]: You could ask if anyone needs help with the \"offer assistance\" action in the \"requests\" tab, which could provide a pointer if you're lost." },
            { "lotoffrags", "[CLR]: You have amassed a great deal of fragments. Perhaps you could search for a market to take them off your hands." },
            { "leavedistrictonmapupdate", "[CLR]: Your world map has been updated to mark a different location in the world. You can move to the edge of the district map to leave the district." },
            { "calamityinfoafterreturn", "[CLR]: The intrigue section of the Game Menu has been updated. Perhaps you can ask a scholar about those figures." },
            { "shrine", "[CLR]: This shrine is dedicated to [ITEM], a core of this world. You can offer items of value you may not otherwise need to the altar with the \"give item\" action. Perhaps something will happen." },
            { "craft", "[CLR]: You can craft items at this forge. Resources can be found in empty plots of land in the world travel menu, and you can start crafting at this forge with the \"craft\" action." },
            { "harvest", "[CLR]: You can harvest resources with this tool. Different tools harvest different resources in different regions. If you move to an empty region in travel mode, you can harvest a particular resource there." },
            { "shadowstorage", "[CLR]: You can place items inside shadow fountains, and retrieve them from any shadow fountain in the world." },
            { "chromaweaver", "[CLR]: I wonder what this peculiar device does." },
            { "claimstructure", "[CLR]: If one day you wish to command large groups of people, you can claim empty structures with the \"claim\" action and use \"ascend\" to acquire a more global view." },
            { "haul", "[CLR]: This item is too heavy to pick up normally. If you have empty hands, you may be able to haul it with the \"carry\" action." },
            { "freebound", "[CLR]: This person was tied up by someone. You could use the \"free\" action, but this could be dangerous." },
            { "combatlens", "[CLR]: If you would like a hyper-detailed view of your available combat moves, check the Combat Lens. You can access it with the lens tab on the right of the screen." },
            { "enchromalite", "[CLR]: That [ITEM] is made out of a rare substance. It was probably written by someone important..." },
            { "howtoenter", "[CLR]: You can enter this structure by clicking it's name in the top right menu." },
            { "howtoleave", "[CLR]: When you are ready to leave, you can use the \"leave structure\" action or click the exit door." },
            { "lootplsfrags", "[CLR]: The vitalium fragments you see are a common currency due to their light weight and energetic properties." },
            { "imbuements", "[CLR]: Imbued items in your inventory provide passive or active buffs to your character. Imbued clothing must be worn to take effect." },
            { "switchingimbuements", "[CLR]: You no longer have space for more imbuements. You can swap your imbuements by accessing the game menu, and clicking Manage Imbuements." },
            { "proficiencies", "[CLR]: You have improved your [ITEM] proficiency. Your combat effectiveness will increase with experience." },
            { "reading", "[CLR]: You can read any material with the \"read\" action. It may be full of historical events, magical instructions, worthless opinions, or pleas for help." },
            { "materials", "[CLR]: Materials have differing combat strengths. Use the \"examine\" action determine metal toughness." },
            { "paths", "[CLR]: Soul absorption allows you to unlock new abilities. You can choose an ability path in the game menu to increase your stats, as well as unlock passive and active abilities." },
            { "fallendown", "[CLR]: You have fallen prone. This will significantly decrease your speed. You can stand back up with the \"stand up\" action, but only if the required body parts are stable." },
            { "wear", "[CLR]: You have acquired a new clothing item. You can access the Game Menu and click items in your inventory to wear them." },
            { "consume", "[CLR]: You can press/hold tab and click a consumable item to apply it. Certain healing items cure different health issues." },
            { "recallinfo", "[CLR]: If you end up with more spells or skills than you can care to remember, you can use the Ability View button (to the right of the observations list) to list all possessed abilities." },
            { "repositionorretract", "[CLR]: Your [ITEM] is in a vulnerable position. Click the exposure GUI to reposition all body parts, or shift click the GUI to retract the most exposed part." },
            { "shopping", "[CLR]: Each market in the world is different depending on what trade passed through here. Use the trade menu to trade; leave items of equal or greater value, and the debtshibas will let you pass." },
            { "abilities", "[CLR]: You have unlocked a new ability. You can find active abilities in the \"Abilities\" action tab." },
            { "blind", "[CLR]: You are currently blind. Your character cannot acquire new information, but you can still interact with most subjects, with most actions." },
            { "unconscious", "[CLR]: You have gone unconscious. This most often happens due to excessive pain. Your character cannot act until consciousness is restored, or until your death." },
            { "advisemarketleave", "[CLR]: With some basic equipment, head to the edge of the district map (bottom right 7x7 map) to find greater adventure throughout the world." },
            { "leavedistrict", "[CLR]: Traveling outside the district map will allow you to explore the world." },
            { "hookrequiresexamine", "[CLR]: That entity looks rather mysterious. Try examining it with \"examine\"" },
            { "orbheal", "[CLR]: You are rather injured. Before your next combat, consider clicking the orb in the top left to wait until your body repairs itself." },
            { "hookrequiresread", "[CLR]: That writing looks interesting. It might be worth picking up and reading." },
            { "hookrequiresofferassistance", "[CLR]: That individual looks troubled. It might be worth it to use the \"offer assistance\" action on them." },
            { "superiorarmor", "[CLR]: Some of the most powerful in the world have gatekept precious metals in structures like these. Acquiring them wont be easy, though." },
            { "explainplanfoil", "[CLR]: Foiling a plan can be a complex undertaking. Try asking at a library for directions to that plan location. You can then either wait out and incapacitate the executors, or remove/disrupt one of their targets." },
            { "explainfreebondage", "[CLR]: A scholar at a library probably knows what location they are imprisoned at, and someone at that location could probably point you to their exact prison." },
            { "explaineliminatealllife", "[CLR]: You might ask a scholar at a library for directions to that place, then hunt down its inhabitants without prejudice." },
            { "explaindeliver", "[CLR]: A scholar at a library probably knows where both of those entities are." },
            { "explainreactivate", "[CLR]: A scholar at a library probably knows the whereabouts of that device. " },
            { "explaindeactivate", "[CLR]: A scholar at a library probably knows the whereabouts of that device. " },
            { "explainsteal", "[CLR]: Some librarian probably knows the whereabouts of that object." },
            { "explaininterceptandkill", "[CLR]: A scholar at a library probably knows what location this person is hiding at, and someone at that location could probably give further directions." },
            { "explaincapture", "[CLR]: A scholar at a library probably knows what location this person is hiding at, and someone at that location could probably give further directions." },
            { "tabintrigue", "[CLR]: You can open the game menu or press Tab to see your intrigue. This stores data about intriguing pointers you've found in the past that could be worth asking scholars about." },
            { "messagetoyou", "[CLR]: Someone is speaking to you. You can respond in a variety of ways, including not responding at all." },
            { "explaintravel", "[CLR]: Click nearby tiles to travel to them, and use the buttons to the right to interact with the region you are currently at." },
            { "explaingather", "[CLR]: You can gather raw materials by pressing numbers 1-6. This requires a character in your party to have a specific tool." },
            { "rafttravel", "[CLR]: You need to either assemble a raft (click above button) or find a port to travel through the ocean. Raft travel is slower, but this won't matter for most situations." },
            { "debtshibadeath", "[CLR]: Looks like you... underpaid." },
            { "lowenergy", "[CLR]: Your character is significantly low on energy, dictated by the brightness of the orb top left. You can recover it by defeating an enemy, using a vitalium vial, or waiting patiently." },
            { "pain", "[CLR]: Your character has high pain, dictated by the red bar near your energy orb. Your character will falter, losing out on turns, and eventually will go unconsicous. Use salves or wait out of combat to diminish it." },
            { "bleeding", "[CLR]: Your character is severely bleeding. Defeating the immediate threat might not be enough to save you." },
            { "alldeath", "[CLR]: Your character has deceased. Can't win them all. Resume the cycle to re-enter the world as a new vessel, or return to the title and conjure a new world." },
            { "explainquickexit", "[CLR]: You can click \"path to exit\" to quickly find the quickest way to and exit the structure." },
            { "explainloot", "[CLR]: You can quickly carry all items in this structure outside of it to look through with the \"sweep stucture loot\" button." },
            { "reactions", "[CLR]: When you get attacked, you can choose a combat reaction. Press ? for more details, each reaction has a varying chance to succeed and a different effect to your exposure." },
            { "grenade", "[CLR]: You could throw that at someone, but you might not want to be in the room or area where it lands..." },
            { "attacksuccess", "[CLR]: Targeting specific body parts can be more effective depending on your opponent's cover or exposure. If a particular movement or action exposes you, it is likely to expose your opponent in the same way, potentially making certain body parts more vulnerable." },
            { "paindropweapon", "[CLR]: One of your hands or arms was too damaged to sustain the weapon in it. The weapon is now on the floor, don't forget to pick it up if you leave..." },
            { "invocationcrystal", "[CLR]: You can invoke this crystal with the \"invoke crystal\" action." },
            { "prisms", "[CLR]: Fragments you collect naturally assemble into prisms, which are worth 50 fragments each." },
            { "pinkexclamation", "[CLR]: That pink marking on your map intrigues me." },
            { "spelldata", "[CLR]: Spells create effects at the cost of energy. You can target up to five entities, costing more energy per entity. They oftentimes provide more combat utility than direct damage." },
            { "pylondefense", "[CLR]: I spy three pylons off in the distance, heavily defended. I wonder why they're so adamantly protected... taking them down might require pulling out any abilities you can afford." }

        };

        public List<string> SentCompanionMessages = new List<string>();

        public void CompanionMessage(string ID, string Subject)
        {
            if(CompanionColor != "nocomp" && !SentCompanionMessages.Contains(ID))
            {
                if (CompMessages.TryGetValue(ID, out string message))
                {
                    message = message.Replace("[ITEM]", Subject);
                    Game1.CompanionMessages.Add(message.Replace("[CLR]", Game1.FlamingSphereNames[CompanionColor]));
                }
                SentCompanionMessages.Add(ID);
            }
        }

        public bool Historical = true;

        public string CurrentStudyTopic = null;
        public string CurrentContemplationTopic = null;

        public void AssignStudyTopic()
        {
            // Calculate the total study points
            int totalPoints = this.CultureStudyPoints + this.ScienceStudyPoints + this.MagicStudyPoints;

            // Calculate the probability for each category
            double cultureProbability = (double)this.CultureStudyPoints / totalPoints;
            double scienceProbability = (double)this.ScienceStudyPoints / totalPoints;
            double magicProbability = (double)this.MagicStudyPoints / totalPoints;

            // Generate a random number between 0 and 1
            double randomValue = Game1.GameWorld.rnd.NextDouble();

            // Determine which category is selected based on the random value
            if (randomValue < cultureProbability)
            {
                // Select Culture field
                CurrentStudyTopic = FavoriteCultureField;
            }
            else if (randomValue < cultureProbability + scienceProbability)
            {
                CurrentStudyTopic = FavoriteScienceField;
            }
            else
            {
                CurrentStudyTopic = FavoriteMagicField;
            }
        }


        public Boardgame CurrentlyParticipatingGame = null;
        public Entity RealityBlipFocus;

        public int RealityFocusTries { get; set; }
        public List<Message> MessagesNotRespondedTo { get; set; } = new List<Message>();
        public bool RuptureMode { get; set; } = false;
        public bool PickUpMode { get; set; } = false;

        private string _deathCause = "";
        public string DeathCause
        {
            get => _deathCause;
            set
            {
                if (string.IsNullOrEmpty(_deathCause))
                {
                    _deathCause = value;
                }
            }
        }

        public bool BroadcastedDeathMessage { get; set; } = false;
        public bool Crafting { get; set; } = false;
        public bool TryingToTravel { get; set; } = false;
        public int SpellcastingPower { get; set; } = 1;
        public bool DoubleStrikeReady { get; set; } = false;
        public bool QuickStrikeReady { get; set; } = false;
        public bool SeveringStrikeReady { get; set; } = false;
        public bool FinaleReady { get; set; } = false;
        public bool BodySlamReady { get; set; } = false;
        public bool LegSweepReady { get; set; } = false;
        public bool DropKickReady { get; set; } = false;
        public int CyclesSinceJump { get; set; } = 0;
        public int ReactionBoostCycles { get; set; } = 0;
        public int ExtraFocusTicks { get; set; } = 0;
        public int HalfFocusTicks { get; set; } = 0;


        public string TruthfulMannerism = "";
        public string LyingMannerism = "";
        public string UnsureMannerism = "";
        public string DerailingMannerism = "";
        public string FlirtatiousMannerism = "";

        public List<string> GetAllAbilityDescriptions()
        {
            List<string> result = new List<string>();

            // 1. Weapon attacks
            void AddWeaponAttack(Object weapon)
            {
                if (weapon != null && weapon.IsWeapon && !string.IsNullOrEmpty(weapon.DamageType))
                {
                    string verb = weapon.DamageType switch
                    {
                        "slashing" => "Slash",
                        "piercing" => "Pierce",
                        "bashing" => "Bash",
                        "thrashing" => "Thrash",
                        _ => null
                    };

                    if (verb != null && weapon.ReferredToNames.Any())
                    {
                        result.Add($"{verb} attack with {weapon.ReferredToNames[0]}");
                    }
                }
            }

            AddWeaponAttack(this.MainHeldObject);
            AddWeaponAttack(this.OffHeldObject);

            // 2. Skills known (simplified)
            foreach (Entity e in SkillsKnown)
            {
                if (Game1.SimplifiedSkillSpellDescriptions.TryGetValue(e.Name, out string desc))
                {
                    result.Add(desc);
                }
            }

            // 3. Spells known (simplified)
            foreach (Entity e in SpellsKnown)
            {
                if (Game1.SimplifiedSkillSpellDescriptions.TryGetValue(e.Name, out string desc))
                {
                    result.Add(desc);
                }
            }

            // 4. Invocation powers (simplified)
            foreach (string s in Invocations)
            {
                if (Game1.SimplifiedGiftDescriptions.TryGetValue(s, out string desc))
                {
                    result.Add(desc);
                }
            }

            // 5. Path abilities
            result.AddRange(Game1.GetUnlockedPathAbilities(this));

            if (result.Count == 0)
            {
                result.Add("You have no abilities. You might find a weapon before entering combat.");
            }

            return result;
        }



        private (int, int, int, int, int) _savePoint = (0, 0, 0, 0, 0);

        
        public (Location, District, Block, Structure, Room) SavePoint
        {
            get => (_savePoint.Item1 != 0 ? EntityGet<Location>(_savePoint.Item1) : null,
                    _savePoint.Item2 != 0 ? EntityGet<District>(_savePoint.Item2) : null,
                    _savePoint.Item3 != 0 ? EntityGet<Block>(_savePoint.Item3) : null,
                    _savePoint.Item4 != 0 ? EntityGet<Structure>(_savePoint.Item4) : null,
                    _savePoint.Item5 != 0 ? EntityGet<Room>(_savePoint.Item5) : null);
            set => _savePoint = (value.Item1?.ID ?? 0, value.Item2?.ID ?? 0, value.Item3?.ID ?? 0, value.Item4?.ID ?? 0, value.Item5?.ID ?? 0);
        }

        public int SavePointTicks { get; set; } = 0;
        public EntityHashSet<Entity> AlignedDomains { get; set; } = new EntityHashSet<Entity>();
        public EntityList<Composition> CultureBank { get; set; } = new EntityList<Composition>();
        public EntityHashSet<Location> ExploredLocations { get; set; } = new EntityHashSet<Location>();
        public EntityHashSet<Architect> ArchitectsWhoSurrenderedToMe { get; set; } = new EntityHashSet<Architect>();
        public EntityHashSet<Architect> ArchitectsWhoISurrenderedTo { get; set; } = new EntityHashSet<Architect>();
        public EntityHashSet<Architect> ArchitectsWhoIAttemptedToSurrenderTo { get; set; } = new EntityHashSet<Architect>();
        public EntityHashSet<Architect> ArchitectsIWillTellTruthTo { get; set; } = new EntityHashSet<Architect>();
        public bool Bound { get; set; }
        public double AdventureCooldown { get; set; } = 0;
        public double DiplomacyCooldown { get; set; } = 0;

        public Dictionary<string, int> BackupProfessionToLevel { get; set; } = new Dictionary<string, int>
    {
        {"baker", 1},
        {"blacksmith", 1},
        {"brewer", 1},
        {"butcher", 1},
        {"carpenter", 1},
        {"child", 1},
        {"craftsman", 1},
        {"elder", 1},
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
        {"artificer", 4},
        {"bard", 4},
        {"duelist", 4},
        {"luminary", 4},
        {"mage", 4},
        {"alpha", 6},
        {"anarchist", 6},
        {"archmage", 6},
        {"beastmaster", 6},
        {"commander", 6},
        {"diplomancer", 6},
        {"large beast", 6},
        {"outlaw", 6},
        {"spy", 6},
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

        public Location InteractionLocation;

        public string CompanionColor = "nocomp";

        public string Prompt { get; set; } = "";
        public List<string> PreviousPrompts { get; set; } = new List<string>() { "" };
        public int PromptIndex { get; set; } = 0;
        public string SavedPrompt { get; set; } = "";
        public bool RecievedBodyPhysicalStatIncrease { get; set; } = false;
        public bool RecievedDexBonus { get; set; } = false;
        public bool RecievedCurse { get; set; } = false;

        public bool RecievedBodyPhysicalStatIncreaseTwo { get; set; } = false;

        public EntityList<Architect> _architects = new EntityList<Architect>();
        public List<int> _distances = new List<int>();
        public List<float> _rotations = new List<float>(); // Rotations in radians

        public int GetDistance(Entity entity)
        {
            if(entity == this)
            {
                return 0;
            }

            if(Game1.TutorialActive && (entity is Architect A) && ((A.Profession == "mercenary" && this == Game1.MostRecentPartyTurnArchitect) || (this.Profession == "mercenary" && A == Game1.MostRecentPartyTurnArchitect)))

            {
                return 0;
            }

            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (!(entity is Architect targetArchitect))
            {
                return 0;
            }

            if (entity is Architect a)
            {
                if ((this.Room?.Architects.Contains(a) != true && this.Room != null) ||
                    (this.Block?.Architects.Contains(a) != true && this.Room == null))
                {
                    return 10;
                }
            }
            else if (entity is Object o)
            {
                if ((this.Room?.Objects.Contains(o) != true && this.Room != null) ||
                    (this.Block?.Objects.Contains(o) != true && this.Room == null))
                {
                    return 10;
                }
            }

            int index = _architects.IndexOf(targetArchitect);
            if (index >= 0)
            {
                return _distances[index];
            }

            // Automatically assign a random value between 2 and 5 if no distance exists
            int randomDistance = Game1.TutorialActive ? 0 : Game1.GameWorld.rnd.Next(3, 6);
            ModifyDistance(targetArchitect, randomDistance);
            return randomDistance;
        }

        public Dictionary<Architect, int> GetDistances()
        {
            return _architects.ToDictionary(arch => arch, arch => _distances[_architects.IndexOf(arch)]);
        }


        public void ModifyDistance(Architect architect, int distanceChange)
        {
            if (architect == null)
            {
                throw new ArgumentNullException(nameof(architect));
            }

            int index = _architects.IndexOf(architect);
            int newDistance;
            float newRotation;

            if (index >= 0)
            {
                newDistance = _distances[index] + distanceChange;
                if (newDistance < 0)
                {
                    newDistance = 0;
                }
                _distances[index] = newDistance;
                newRotation = _rotations[index]; // Keep the existing rotation
            }
            else
            {
                newDistance = Game1.GameWorld.rnd.Next(3, 6);
                newRotation = (float)(Game1.GameWorld.rnd.NextDouble() * 2 * Math.PI); // Random direction
                _architects.Add(architect);
                _distances.Add(newDistance);
                _rotations.Add(newRotation);
            }

            // Sync with the other architect
            architect.SyncDistance(this, newDistance, (newRotation + (float)Math.PI) % (2 * (float)Math.PI));
        }

        public void SyncDistance(Architect architect, int newDistance, float newRotation)
        {
            if (architect == null)
            {
                throw new ArgumentNullException(nameof(architect));
            }

            int index = _architects.IndexOf(architect);

            if (index >= 0)
            {
                if (newDistance < 0)
                {
                    _architects.RemoveAt(index);
                    _distances.RemoveAt(index);
                    _rotations.RemoveAt(index);
                }
                else
                {
                    _distances[index] = newDistance;
                    _rotations[index] = newRotation;
                }
            }
            else if (newDistance >= 0)
            {
                _architects.Add(architect);
                _distances.Add(newDistance);
                _rotations.Add(newRotation);
            }
        }
        public static List<string> HireableArchs = new List<string>() { "mercenary", "hunter", "soldier", "warrior" };

        public void RemoveDistance(Architect architect)
        {
            if (architect == null)
            {
                throw new ArgumentNullException(nameof(architect));
            }

            int index = _architects.IndexOf(architect);
            if (index >= 0)
            {
                _architects.RemoveAt(index);
                _distances.RemoveAt(index);
                _rotations.RemoveAt(index);
            }

            // Remove the distance and rotation from the other architect as well
            architect.SyncRemoveDistance(this);
        }

        public void SyncRemoveDistance(Architect architect)
        {
            int index = _architects.IndexOf(architect);
            if (index >= 0)
            {
                _architects.RemoveAt(index);
                _distances.RemoveAt(index);
                _rotations.RemoveAt(index);
            }
        }

        public Dictionary<Architect, (int Distance, float Rotation)> GetDistancesWithRotations()
        {
            return _architects.ToDictionary(
                arch => arch,
                arch => (_distances[_architects.IndexOf(arch)], _rotations[_architects.IndexOf(arch)])
            );
        }

        private int _legendaryTarget;

        
        public Entity LegendaryTarget
        {
            get => EntityGet<Entity>(_legendaryTarget);
            set => _legendaryTarget = value?.ID ?? 0;
        }

        private int _legendaryTargetStructure;
        public Structure LegendaryTargetStructure
        {
            get => EntityGet<Structure>(_legendaryTargetStructure);
            set => _legendaryTargetStructure = value?.ID ?? 0;
        }

        public int HuntingProgress { get; set; } = 0;
        public EntityHashSet<Architect> ShieldTokens { get; set; } = new EntityHashSet<Architect>();
        public int DivineProtection { get; set; } = 0;
        public int DivineMight { get; set; } = 0;
        public bool MigrationKitted { get; set; } = false;
        public List<(int, string)> Grievances { get; set; } = new List<(int, string)>();
        public int CooldownCycles { get; set; } = 0;
        public bool IsCalamity { get; set; } = false;
        public bool BuiltSpire { get; set; } = false;
        public int NaturalArmor { get; set; } = 0;
        public int Level { get; set; } = 0;
        public int SpendableLevels { get; set; }
        public EntityList<Object> Sparks { get; set; } = new EntityList<Object>();
        public Architect UndeadCreator;
        public Structure OnTopOfStructure;


        public bool Invisible { get; set; } = false;
        public int CombatCycles { get; set; } = 0;

        public int PathOfShadowLevel { get; set; } = 0;
        public int PathOfLifeLevel { get; set; } = 0;
        public int PathOfRealityLevel { get; set; } = 0;
        public int PathOfLightLevel { get; set; } = 0;
        public int PathOfDeathLevel { get; set; } = 0;
        public int PathOfStarsLevel { get; set; } = 0;
        public int PathOfHeatLevel { get; set; } = 0;
        public int PathOfBodyLevel { get; set; } = 0;

        public string MasterRelation { get; set; } = "";
        public Architect Spouse;
        public Architect Master;


        public bool HasBeenAugmented { get; set; } = false;
        public EntityList<Object> ShadowStorage { get; set; } = new EntityList<Object>();
        public int Wealth { get; set; } = 0;
        public double CalamityAge { get; set; } = 0;
        public int CalamitySpawnTime { get; set; } = Game1.GameWorld.rnd.Next(25, 36);
        public bool TriggeredLock { get; set; } = false;
        public int CyclesSinceMoved { get; set; } = 0;

        public Block BlockLastCycle;
        public Room RoomLastCycle;
        public Race Race;

        public (Location, District, Block, Structure, Room) RematerializeLocation;

        public int BarrierStacks { get; set; } = 0;


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

        public bool AlreadyMadeAGame = false;

        public EntityList<Architect> MeldedShibas  = new EntityList<Architect>();
        public bool IsAlive  = true;
        public int MoralCompass  = 0;
        public int StabilityCompass  = 0;
        public int PropertyValue  = Math.Max(0, Game1.GameWorld.rnd.Next(-4, 6));
        public int FamilyValue  = Math.Max(0, Game1.GameWorld.rnd.Next(-4, 6));
        public int PowerValue  = Math.Max(0, Game1.GameWorld.rnd.Next(-4, 6));
        public int MoneyValue  = Math.Max(0, Game1.GameWorld.rnd.Next(-4, 6));
        public int KnowledgeValue  = Math.Max(0, Game1.GameWorld.rnd.Next(-4, 6));
        public int SpiritualityValue  = Math.Max(0, Game1.GameWorld.rnd.Next(-4, 6));
        public int ProwessValue = Math.Max(0, Game1.GameWorld.rnd.Next(-4, 6));
        public int PatriotismValue = Math.Max(0, Game1.GameWorld.rnd.Next(-4, 6));
        public int WarValue = Math.Max(0, Game1.GameWorld.rnd.Next(-4, 4));
        public int CourageValue = Math.Max(0, Game1.GameWorld.rnd.Next(-4, 6));
        public int CreativityValue = Math.Max(0, Game1.GameWorld.rnd.Next(-4, 6));

        public Location NextMigrationLocation;
        public double NextMindreavingCycle = 0;
        public string MigrationReason = "";
        public EntityList<Imbuement> AllImbuements = new EntityList<Imbuement>();
        public bool RecievedImmortalityBuff;
        public int DistrictPoints = 0;
        public Structure StudyBuilding;
        public bool HasMadeALegendaryArtifact = false;
        public bool HadChildren = false;
        public string FavoriteColor;
        public bool IsUndead = false;
        public Material FavoriteGemstone;
        public Material FavoriteStone;
        public Material FavoriteWood;
        public Material FavoriteMetal;
        public Material FavoriteCloth;
        public Object FavoriteBook;
        public Group Group;

        public int GroupLoyalty  = -1;
        public int TerminalAge  = 0;
        public bool DoIDieOfOldAge  = true;
        public bool IsLoadedTrader  = false;
        public int PurifiedBurnedCities { get; set; } = 0;

        private int _blightManipulated;

        
        public Blight BlightManipulated
        {
            get => EntityGet<Blight>(_blightManipulated);
            set => _blightManipulated = value?.ID ?? 0;
        }

        public int KilledPeopleWithBlight { get; set; } = 0;
        public EntityList<Location> TakenLocations { get; set; } = new EntityList<Location>();
        public int KilledWomen { get; set; } = 0;
        public int KilledMen { get; set; } = 0;
        public int KilledChildren { get; set; } = 0;
        public int KidnappedMen { get; set; } = 0;
        public int KidnappedWomen { get; set; } = 0;
        public int KidnappedChildren { get; set; } = 0;
        public bool MaxEnergyInspiration { get; set; } = false;
        public EntityList<Architect> KilledPeopleWhoActuallyMatter { get; set; } = new EntityList<Architect>();
        public EntityList<Architect> KidnappedPeopleWhoActuallyMatter { get; set; } = new EntityList<Architect>();
        public int CorruptedCities { get; set; } = 0;
        public int Deceived { get; set; } = 0;
        public string PowerType { get; set; } = "";

        public string CombatTag { get; set; } = "";


        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Agility { get; set; }
        public int Endurance { get; set; }
        public int Creativity { get; set; }
        public int Charisma { get; set; }
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

                if(ActiveGifts.Contains("sight"))
                {
                    baseFocus += 1;
                }

                return baseFocus;
            }
            set => _focus = value;
        }

        public Division Division;

        private int _room;
        private int _block;
        private int _district;
        private int _location;


        public bool LootingStructureRightNow;

        public Location Location;
        public District District;

        public bool DistrictMarked = false;
        
        public Location HomeLocation;

        public Room Room;
        public Structure Structure => Room?.Structure;



        public Block Block
        {
            get
            {
                if (_block != 0)
                {
                    return EntityGet<Block>(_block);
                }
                if (_room != 0)
                {
                    return EntityGet<Room>(_room).Structure.Block;
                }
                return null;
            }
            set => _block = value?.ID ?? 0;
        }

        public bool AddToNumberOnReload = false;

        public District HomeDistrict;

        public Structure HomeStructure;


        public string Destiny { get; set; } = "none";
        public int DestinyArrivalYear { get; set; } = 999;
        public string CurrentlyMovingPlace { get; set; } = "none";
        public int MessageCooldown { get; set; } = 50;

        public bool Transient = false;

        public EntityList<Architect> ArchitectsForOpinions = new EntityList<Architect>();
        public List<int> Opinions = new List<int>();

        public void SetOpinion(Architect architect, int opinion)
        {
            if (architect == null)
            {
                throw new ArgumentNullException(nameof(architect));
            }

            int index = ArchitectsForOpinions.IndexOf(architect);
            if (index >= 0)
            {
                Opinions[index] = opinion;
            }
            else
            {
                ArchitectsForOpinions.Add(architect);
                Opinions.Add(opinion);
            }


            if (architect.ArchitectILookLike == this && opinion < 0)
            {
                architect.ArchitectILookLike = architect;
                architect.AnnounceToParty("The illusion has dissipated!", Color.Red, new EntityList<Entity>());
            }
        }

        public void ChangeOpinion(Architect architect, int opinionChange)
        {
            if (architect == null)
            {
                throw new ArgumentNullException(nameof(architect));
            }

            int index = ArchitectsForOpinions.IndexOf(architect);
            if (index >= 0)
            {
                Opinions[index] += opinionChange;
            }
            else
            {
                ArchitectsForOpinions.Add(architect);
                Opinions.Add(opinionChange);
            }


            if(architect.ArchitectILookLike == this && opinionChange < 0 && architect.ArchitectILookLike != architect)
            {
                architect.ArchitectILookLike = architect;
                architect.AnnounceToParty("The illusion has dissipated!", Color.Red, new EntityList<Entity>());
            }
        }

        public int GetOpinion(Architect architect)
        {
            if (architect == null)
            {
                throw new ArgumentNullException(nameof(architect));
            }

            int index = ArchitectsForOpinions.IndexOf(architect);
            return index >= 0 ? Opinions[index] : 0; // Default opinion if not found
        }

        public void RemoveOpinion(Architect architect)
        {
            if (architect == null)
            {
                throw new ArgumentNullException(nameof(architect));
            }

            int index = ArchitectsForOpinions.IndexOf(architect);
            if (index >= 0)
            {
                ArchitectsForOpinions.RemoveAt(index);
                Opinions.RemoveAt(index);
            }
        }

        public EntityList<Architect> KnownArchitects { get; set; } = new EntityList<Architect>();
        public bool IsStudying { get; set; }
        public bool Loaded { get; set; }
        public EntityList<Object> Clothing { get; set; } = new EntityList<Object>();
        public string ScholarType { get; set; } = "";
        public string FavoriteScienceField { get; set; } = "";
        public string FavoriteCultureField { get; set; } = "";
        public string FavoriteMagicField { get; set; } = "";
        public int MagicStudyPoints { get; set; } = 0;
        public int CultureStudyPoints { get; set; } = 0;
        public int ScienceStudyPoints { get; set; } = 0;
        public EntityList<Entity> SpellsKnown { get; set; } = new EntityList<Entity>();
        public EntityList<Entity> SkillsKnown { get; set; } = new EntityList<Entity>();
        public EntityList<Entity> UsedSkills { get; set; } = new EntityList<Entity>();

        public List<string> Invocations { get; set; } = new List<string>();

        public bool AlacrityStatIncrease { get; set; } = false;

        public bool DiscoveredASpell { get; set; } = false;
        public int FireSeconds { get; set; } = 0;

        public bool TerminalTalk = false;
        public bool DieOnResponse = false;


        public int WetCycles { get; set; } = 0;
        public int BlindCycles { get; set; } = 0;
        public int DestabilizedCycles { get; set; } = 0;
        public int UnconsciousCycles { get; set; } = 0;
        public int RadiantCycles { get; set; } = 0;
        public int PlantCycles { get; set; } = 0;
        public int CloakCycles { get; set; } = 0;
        public int BoostedCycles { get; set; } = 0;
        public int FractalCycles { get; set; } = 0;
        public int HoldCycles { get; set; } = 0;
        public int DismissalCycles { get; set; } = 0;
        public bool OnGround { get; set; } = false;
        public bool IsImmortal { get; set; } = false;
        public double YLevelInFeet { get; set; } = 0;
        public double YVelocity { get; set; } = 0;
        public bool Focused { get; set; } = false;
        public int DaysSinceFood { get; set; } = 0;
        public int DaysSinceLiquid { get; set; } = 0;
        public int NightsSinceSleep { get; set; } = 0;
        public int DaysSinceCoffeeOrTea { get; set; } = 0;
        public int DaysSinceSocialized { get; set; } = 0;
        public int DaysSincePerforming { get; set; } = 0;
        public int DaysSincePlayingGame { get; set; } = 0;

        public int HealTryCycles = 0;

        public bool HasBeenKitted = false;

        private int _targetArchitect;
        public Architect TargetArchitect
        {
            get => EntityGet<Architect>(_targetArchitect);
            set
            {
                if (value != null && Game1.GameWorld.rnd.Next(100) < value.ExtraStealth)
                {
                    // Fail to set the architect as the target if the random check fails
                    return;
                }
                _targetArchitect = value?.ID ?? 0;
            }
        }

        public bool TutorialSickness = false;
        public bool PlayingTutorial = false;
        public Object TargetObject;
        public (Location, District, Block, Room) Target;


        public string Task { get; set; } = "";
        public int CyclesLeftInTask { get; set; } = 0;
        public decimal Energy { get; set; }
        public decimal EnergyWhenStarted { get; set; }
        public bool Protected { get; set; }
        public int MaxEnergyMod { get; set; } = 0;
        public decimal Bleeding { get; set; } = 0;
        public double Pain { get; set; } = 0;
        public EntityList<Object> BodyParts { get; set; } = new EntityList<Object>();

        public Object MainInteractionAppendage;
        public Object OffInteractionAppendage;

        public EntityList<Object> Inventory { get; set; } = new EntityList<Object>();

        public Object OffHeldObject;
        public Object MainHeldObject;

        public bool PrefersCoffeeIfTrue { get; set; } = false;
        public bool IsColossal { get; set; } = false;
        public int ColossalMinefieldX { get; set; }
        public int ColossalMinefieldZ { get; set; }
        public double ExtraAttackPowerPercentage { get; set; } = 1.0;

        public double ExtraDodgeChancePercentage { get; set; } = 1.0;
        public double ExtraWeaponReactionChancePercentage { get; set; } = 1.0;
        public double ExtraBashingResistancePercentage { get; set; } = 1.0;
        public double ExtraShieldEffectivenessPercentage { get; set; } = 1.0;
        public double ExtraSlashingResistancePercentage { get; set; } = 1.0;
        public double ExtraPiercingResistancePercentage { get; set; } = 1.0;
        public double ExtraThrashingResistancePercentage { get; set; } = 1.0;

        public int ExtraStealth { get; set; } = 0;
        public int HideValue { get; set; } = 0;
        public int ExtraEnergyRegen { get; set; } = 0;
        public bool IsofractalThief { get; set; } = false;
        public List<string> OppositionTags { get; set; } = new List<string>();
        public EntityList<Architect> SuperTrustedArchitects { get; set; } = new EntityList<Architect>();

        public List<(string, int)> Proficiencies { get; set; } = new List<(string, int)>
    {
        ("slashing", 0),
        ("piercing", 0),
        ("bashing", 0),
        ("thrashing", 0),
        ("dodging", 0),
        ("blocking", 0),
        ("disarming", 0),
        ("redirection", 0),
        ("throwing", 0),
        ("parrying", 0)
    };

        public void ChangeXP(string proficiencyName, int xpChange)
        {
            var proficiencyIndex = Proficiencies.FindIndex(p => p.Item1.Equals(proficiencyName, StringComparison.OrdinalIgnoreCase));
            if (proficiencyIndex != -1)
            {
                var (name, currentXP) = Proficiencies[proficiencyIndex];
                int oldLevel = CalculateLevel(currentXP);

                // Update XP
                int newXP = currentXP + xpChange;
                Proficiencies[proficiencyIndex] = (name, newXP);

                int newLevel = CalculateLevel(newXP);

                // Check if the level has increased
                if (newLevel > oldLevel)
                {
                    CompanionMessage("proficiencies", proficiencyName);
                }
            }
        }
        public void SetXP(string proficiencyName, int newXP)
        {
            var proficiencyIndex = Proficiencies.FindIndex(p => p.Item1.Equals(proficiencyName, StringComparison.OrdinalIgnoreCase));
            if (proficiencyIndex != -1)
            {
                var (name, currentXP) = Proficiencies[proficiencyIndex];
                int oldLevel = CalculateLevel(currentXP);
                int newLevel = CalculateLevel(newXP);

                Proficiencies[proficiencyIndex] = (name, newXP);

                if (newLevel > oldLevel)
                {
                    CompanionMessage("proficiencies", proficiencyName);
                }
            }
        }

        public int GetXP(string proficiencyName)
        {
            // Find the proficiency in the list
            var proficiency = Proficiencies.FirstOrDefault(p => p.Item1.Equals(proficiencyName, StringComparison.OrdinalIgnoreCase));
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
        // Given XP, return level
        private int CalculateLevel(int xp)
        {
            int level = 1;
            double requiredXP = 25;

            while (xp >= requiredXP)
            {
                level++;
                double multiplier = (level - 1) % 3 == 0 ? 2.5 : 2.0;
                requiredXP *= multiplier;
            }

            return level - 1;
        }

        public void UpdateProficienciesToCurrentLevel()
        {
            int currentLevel = 1;
            double requiredXP = 25;

            // Calculate the total XP of the character
            int totalXP = Proficiencies.Sum(p => p.Item2);

            // Calculate the current level based on total XP
            while (totalXP >= requiredXP)
            {
                currentLevel++;
                double multiplier = (currentLevel - 1) % 3 == 0 ? 2.5 : 2.0;
                requiredXP *= multiplier;
            }

            // Update each proficiency to match the current level if needed
            foreach (var proficiency in Proficiencies.ToList())
            {
                string proficiencyName = proficiency.Item1;
                int proficiencyXP = proficiency.Item2;

                int proficiencyLevel = 1;
                requiredXP = 25;

                // Calculate the current level of the proficiency based on its XP
                while (proficiencyXP >= requiredXP)
                {
                    proficiencyLevel++;
                    double multiplier = (proficiencyLevel - 1) % 3 == 0 ? 2.5 : 2.0;
                    requiredXP *= multiplier;
                }

                // Only update if the proficiency's level is less than the current level
                if (proficiencyLevel < currentLevel)
                {
                    // Calculate the target XP for the current level
                    int targetXP = 0;
                    requiredXP = 25;
                    for (int i = 1; i <= currentLevel; i++)
                    {
                        targetXP += (int)requiredXP;
                        double multiplier = (i - 1) % 3 == 0 ? 2.5 : 2.0;
                        requiredXP *= multiplier;
                    }

                    SetXP(proficiencyName, targetXP);
                }
            }
        }




        // Gets the proficiency level based on XP
        public int GetProficiency(string proficiencyName)
        {
            var proficiency = Proficiencies.FirstOrDefault(p => p.Item1.Equals(proficiencyName, StringComparison.OrdinalIgnoreCase));
            if (proficiency.Equals(default((string, int))))
                return -1;

            return CalculateLevel(proficiency.Item2);
        }

        // Function to add cultural clothing items
        public void AddCulturalClothing(string culturalItems, Material material)
        {
            if (Sex == null)
            {
                Sex = "male";
                return;
            }

            // Check if the clothing item is "straps" and the sex is female
            Civilization civ = this.HomeLocation?.HomeCivilization
                               ?? Game1.GameWorld.Civilizations[Game1.GameWorld.rnd.Next(Game1.GameWorld.Civilizations.Count)];

            bool addUpperShirt =
                (culturalItems.Trim().ToLower() == "straps" || culturalItems.Trim().ToLower() == "straps/cape") &&
                this.Sex.ToLower() == "female" &&
                civ.Type != "druid";

            if (culturalItems != "none")
            {
                // Split the cultural items string into individual items
                string[] items = culturalItems.Split('/');

                // Add each item to the Clothing list
                foreach (string item in items)
                {
                    Object newClothing = new Object(null, item.Trim(), new EntityList<Material>() { material }, null);
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

                    newClothing.Imbuements.Clear();
                }

                // If addUpperShirt is true, add "uppershirt" to the Clothing list
                if (addUpperShirt)
                {
                    Object upperShirt = new Object(null, "uppershirt", new EntityList<Material>() { material }, null);
                    Clothing.Add(upperShirt);

                    // Apply dye to the upper shirt
                    ApplyDye(upperShirt);

                    upperShirt.Imbuements.Clear();
                }
            }
        }

        public void ApplyDye(Object clothingItem)
        {
            // Assuming HomeLocation is not null and there are Colors to choose from

            if (HomeLocation != null && HomeLocation.HomeCivilization != null)
            {
                int decider = Game1.GameWorld.rnd.Next(100);
                string colorToApply = null;

                if (HomeLocation.HomeCivilization.Type == "druid")
                {
                    colorToApply = "green";
                }
                else if (decider < 60 || clothingItem.Type == "undergarment" || clothingItem.Type == "brassiere")
                {
                    // 60% chance to dye with the HomeCivilization color
                    colorToApply = HomeLocation.HomeCivilization.EntityColor;
                }
                else if (decider < 80)
                {
                    // 20% chance to dye with a related color
                    List<string> relatedColors = Game1.GetFamilyColors(HomeLocation.HomeCivilization.EntityColor);
                    colorToApply = relatedColors[Game1.GameWorld.rnd.Next(relatedColors.Count())];
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

        public void Enter(Entity subject, bool OrderedTo)
        {
            HideValue = 0;
            CooldownCycles += (int)(Math.Round(20 / Speed));

            CurrentlyMovingPlace = "none";


            if(Game1.MostRecentPartyTurnArchitect == this)
            {

                if (subject is Structure SS && SS.Type == "market")
                {
                    CompanionMessage("shopping", "");
                }

                if ((Game1.GameWorld.Calamity.Count > 0 && Game1.GameWorld.Calamity[0].IsAlive) == false && !Game1.LoadedArchitects.Any(a => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a)))
                {
                    CompanionMessage("claimstructure", "");
                }

                if (subject is Structure)
                {
                    CompanionMessage("howtoleave", "");
                }

                if (Room != null && Room.Objects.Any(o => o.Type == "fragment") && Structure.Type != "market")
                {
                    CompanionMessage("lootplsfrags", "");
                }
                if (Room != null && Room.Objects.Any(o => o.Imbuements.Count > 0) && Structure.Type != "market")
                {
                    CompanionMessage("lootplsfrags", "");
                }

                if(subject is Object o && o.Type == "exit door")
                {
                    CompanionMessage("advisemarketleave", "");
                }

                //store extra moves

                if(Game1.MassTravelOrderMode && !OrderedTo)
                {
                    foreach(Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                    {
                        if(a != Game1.MostRecentPartyTurnArchitect)
                        {
                            a.NextMoveOrder = ("enter", subject);
                        }
                    }
                }
            }


            bool PlaySound = Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Any(a => a.Block == this.Block && a.Room == this.Room);

            if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this) && (Game1.GameWorld.GamePlayerAssociation.ActiveParty.RoomsUnsearched.Count > 0 || SearchRoom != null))
                PlaySound = false;

            if (OnTopOfStructure != null)
            {
                if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                    AnnounceToParty("You need to get down from " + OnTopOfStructure + ", first.", Color.Yellow, new EntityList<Entity>());
                return;
            }

            if (subject is Structure s)
            {
                if (s.Reinforced)
                {
                    if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                        AnnounceToParty("The door won't budge. You might need to bash it down.", Color.Yellow, new EntityList<Entity>());
                    return;
                }
                if (s.Block != Block)
                {
                    if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                        AnnounceToParty("That structure is not nearby.", Color.Yellow, new EntityList<Entity>());
                    return;
                }
                if (Structure == null)
                {
                    if (Game1.GameWorld.rnd.Next(100) <= EscapeChance() || CombatCycles == 0)
                    {
                        Room = s.Rooms[0];
                        Block.Architects.Remove(this);
                        Room.Architects.Add(this);
                        CooldownCycles += (int)(Math.Round(25 / Speed));
                        CombatCycles = 0;
                        HealStrikes = 0;

                        if (PlaySound)
                        {
                            Game1.SFX.Add(Game1.GameWorld.rnd.Next(2) == 0 ? Game1.DoorSound2 : Game1.DoorSound1);
                            StepSound(1);
                        }

                        foreach (Object o in BodyParts)
                        {
                            o.UpdateExposure(-9999);
                        }


                        if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.RoomsUnsearched.Count == 0 && SearchRoom == null)
                        {
                            Game1.SwitchState("exposition", false);
                            Game1.Exposition.Add(new TextStorage(Name + " enters " + s.Name + ", a " + s.Type + ".", Color.LightBlue, new EntityList<Entity>()));
                            Game1.StructureExposition = true;
                        }

                        if (s.PrimarySmells.Count > 0)
                        {
                            Game1.Exposition.Add(new TextStorage("The fresh scent of " + s.PrimarySmells[0] + " fills the area.", Color.Yellow, new EntityList<Entity>()));
                        }
                        if (s.Type == "shrine" && s.Rooms.Any(r => r.Objects.Any(o => o.Type == "altar")))
                        {
                            Game1.Exposition.Add(new TextStorage("An altar lies in the grand hall of this shrine. Perhaps you could offer it something?", Color.Yellow, new EntityList<Entity>()));
                        }

                        List<string> validEnterLocations = new List<string> { "entershrine", "enterlibrary", "entertavern", "enterforge", "entermarket", "enteroutpost", "enterfort", "enterbastion", "enterfortress", "entertower", "enterkeep", "entermonument", "enterstronghold", "entersanctum" };
                        string enterString = "enter" + s.Type.ToLower();

                        if (validEnterLocations.Contains(enterString) && Game1.GameWorld.GamePlayerAssociation.ActiveParty.RoomsUnsearched.Count == 0)
                        {
                            Architect announcer = Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects[Game1.GameWorld.rnd.Next(Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Count)];
                            announcer.TryComment(enterString, 75);
                        }

                        if (s.LightLevelOf5 == 0)
                            Game1.Exposition.Add(new TextStorage("The room is completely dark.", Color.Gray, new EntityList<Entity>()));
                        else if (s.LightLevelOf5 == 1)
                            Game1.Exposition.Add(new TextStorage("The room has very dim lighting.", Color.Gray, new EntityList<Entity>()));
                        else if (s.LightLevelOf5 == 2)
                            Game1.Exposition.Add(new TextStorage("Its not too bright, but manageable.", Color.DarkGray, new EntityList<Entity>()));
                        else if (s.LightLevelOf5 == 3)
                            Game1.Exposition.Add(new TextStorage("The room seems moderately lit.", Color.White, new EntityList<Entity>()));
                        else if (s.LightLevelOf5 == 4)
                            Game1.Exposition.Add(new TextStorage("The room is bright and clear.", Color.LightYellow, new EntityList<Entity>()));
                        else if (s.LightLevelOf5 == 5)
                            Game1.Exposition.Add(new TextStorage("The room is very well lit throughout.", Color.Yellow, new EntityList<Entity>()));

                        if (s.Type == "shrine")
                        {
                            CompanionMessage("shrine", s.PrayingDeity.Name);
                        }
                        if (s.Type == "forge")
                        {
                            CompanionMessage("craft", "");
                        }
                    }
                    else
                    {
                        if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                            AnnounceToParty("You are cut off from your escape attempt.", Color.OrangeRed, new EntityList<Entity>());
                        CooldownCycles += (int)Math.Round(25 / Speed);
                    }
                }
            }
            else if (subject is Door door && (Room?.Objects ?? Block.Objects).Contains(door))
            {
                if (door.DestinationRoom.Structure == Structure)
                {
                    if (door.Reinforced)
                    {
                        if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                            AnnounceToParty("The door won't budge. You might need to bash it down.", Color.Yellow, new EntityList<Entity>());
                    }
                    else if (Game1.GameWorld.rnd.Next(100) <= EscapeChance() || CombatCycles == 0)
                    {
                        Room.Architects.Remove(this);
                        Room = door.DestinationRoom;
                        Room.Architects.Add(this);
                        CombatCycles = 0;
                        CooldownCycles += (int)(Math.Round(25 / Speed));
                        HealStrikes = 0;

                        if (PlaySound)
                        {
                            Game1.SFX.Add(Game1.GameWorld.rnd.Next(2) == 0 ? Game1.DoorSound2 : Game1.DoorSound1);
                            StepSound(1);
                        }

                        if (Room.Objects.Any(o => o.Type == "pylon" && o.AnnouncedSelfThisLoad == false))
                        {
                            Object o = Room.Objects.First(o => o.Type == "pylon" && o.AnnouncedSelfThisLoad == false);
                            AnnounceToParty("This pylon radiates with a repulsive energy you seem drawn to destroy...", Color.Gray, new EntityList<Entity>());
                            o.AnnouncedSelfThisLoad = true;
                        }

                        foreach (Object o in BodyParts)
                        {
                            o.UpdateExposure(-9999);
                        }

                        return;
                    }
                    else
                    {
                        if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                            AnnounceToParty("You are cut off from your escape attempt.", Color.OrangeRed, new EntityList<Entity>());
                        CooldownCycles += (int)Math.Round(25 / Speed);
                    }
                }
                else
                {
                    if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                        AnnounceToParty("This " + door.Type + " has been dislocated.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (subject is Object obj && obj.Type == "exit door" && Room != null)
            {
                if (obj.Reinforced)
                {
                    if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                        AnnounceToParty("The door won't budge. You might need to bash it down.", Color.Yellow, new EntityList<Entity>());
                }
                else if (Game1.GameWorld.rnd.Next(100) <= EscapeChance() || CombatCycles == 0)
                {
                    CooldownCycles += (int)(Math.Round(25 / Speed));
                    HealStrikes = 0;
                    CombatCycles = 0;

                    Exit();

                    
                    if(PlaySound)
                    {
                        Game1.SFX.Add(Game1.GameWorld.rnd.Next(2) == 0 ? Game1.DoorSound2 : Game1.DoorSound1);
                        StepSound(1);
                    }

                    foreach (Object o in BodyParts)
                    {
                        o.UpdateExposure(-9999);
                    }

                    if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.RoomsUnsearched.Count == 0 && SearchRoom == null)
                        Game1.Exposition.Add(new TextStorage(Name + " exits through the " + obj.Type + ".", Color.Blue, new EntityList<Entity>()));

                    if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.RoomsUnsearched.Count == 0 && SearchRoom == null)
                        Game1.SwitchState("exposition", false);
                    return;
                }
            }

            else
            {

                if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                    AnnounceToParty("The specified subject does not lead you anywhere.", Color.Yellow, new EntityList<Entity>());
            }



            if(this.Room != null)
            {
                if(Room.Structure.Rooms.IndexOf(Room) > 2)
                {
                    CompanionMessage("explainquickexit", "");
                }

                if(ScaryTypes.Contains(this.Block.District.Location.Type) && Game1.LoadedArchitects.All(a => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a) || a.IsAlive == false))
                {
                    CompanionMessage("explainloot", "");
                }

                if(Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                {
                    if (this.Room.Objects.Any(o => o.Materials[0].Name == "enchromalite"))
                    {
                        CompanionMessage("enchromalite", this.Room.Objects.First(o => o.Materials[0].Name == "enchromalite").Name);
                    }
                }
            }


            

        }

        public void Exit()
        {
            // Determine if sounds should be played
            bool PlaySound = false;
            if (
                (this.Room == null && this.Block.Architects.Any(t => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(t))) ||
                (this.Room != null && this.Room.Architects.Any(t => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(t))))
            {
                PlaySound = true;
            }

            Room.Architects.Remove(this);
            Room = null;
            Block.Architects.Add(this);

            if (PlaySound)
            {
                Game1.SFX.Add(Game1.GameWorld.rnd.Next(2) == 0 ? Game1.DoorSound2 : Game1.DoorSound1);
                StepSound(2);
            }

            // Announcement
            if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.RoomsUnsearched.Count == 0)
                AnnounceToParty($"{Name} exits through the exit door.", Color.Blue, new EntityList<Entity>());
        }

        public void AddRandomPersonalities()
        {
            List<string> shuffled = AllPersonalities.OrderBy(x => Game1.GameWorld.rnd.Next()).ToList();

            // Add 1 or 2 personalities, ensuring no duplicates and no incompatible pairs
            int countToAdd = Game1.GameWorld.rnd.Next(1, 3); // 1 or 2
            foreach (var personality in shuffled)
            {
                if (!Personalities.Contains(personality) && countToAdd > 0)
                {
                    // Check for incompatibility with already added personalities
                    bool isCompatible = Personalities.All(existing =>
                        !IncompatiblePersonalities.Contains((existing, personality)) &&
                        !IncompatiblePersonalities.Contains((personality, existing))
                    );

                    if (isCompatible)
                    {
                        Personalities.Add(personality);
                        countToAdd--;
                    }
                }
            }
        }

        public void RuinInvisibility()
        {
            if(Invisible)
            {
                Invisible = false;
                AnnounceToParty(ReferredToNames[0] + " loses their invisibility!", Color.Purple, new EntityList<Entity>() { this });
                ExtraStealth = 0;
            }
        }

        public void TryComment(string messageType, int PercentOfTime)
        {
            if (Game1.GameWorld.rnd.Next(100) < PercentOfTime && !(Personalities == null || Personalities.Count == 0) && this.IsAlive && this.UnconsciousCycles == 0 && Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
            {
                string personality = Personalities[Game1.GameWorld.rnd.Next(Personalities.Count)];

                if (Game1.GameWorld.rnd.Next(6) == 1)
                {
                    personality = "generic";
                }

                string response = "";

                Game1.SFX.Add(Game1.TalkSounds[VoiceType]);

                switch (messageType.ToLower())
                {
                    case "startup":
                        string DName = Game1.GameWorld.Calamity.Count > 0 ? Game1.GameWorld.Calamity[0].Name.Split(' ')[0] : new[] { "evil", "iniquity", "depravity", "darkneess", "malevolence", "malice", "corruption" }[Game1.GameWorld.rnd.Next(7)];

                        switch (personality)
                        {
                            case "generic": response = new[] { $"Time to set forth. {DName} will fall.", $"A long road ahead, but {DName} won't see another day.", $"This is where it begins. The hunt for {DName}.", $"No more waiting. {DName}, your time is up.", $"{DName} can't go on much longer.", $"Adventure awaits. Where is {DName}...", $"Will my efforts be enough to stop {DName}...", $"{DName}... it certainly has been a while.", $"No turning back. {DName}, prepare yourself." }[Game1.GameWorld.rnd.Next(9)]; break;
                            case "tactician": response = new[] { $"A plan is only as good as its execution. {DName} won't escape.", $"Patience and preparation. {DName} will fall in time.", $"This isn't a battle, it's a campaign. {DName} is just the first target." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { $"It hardly feels fair. {DName} doesn't stand a chance.", $"I'd wish {DName} luck, but that won't change the outcome.", $"{DName} will not last much longer." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { $"I walk a path of destiny. {DName} is merely an obstacle.", $"The world will be better once {DName} is gone.", $"I am defined by my triumph. {DName} will be mine." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { $"Orders or not, {DName} is a threat. And threats get eliminated.", $"Discipline, training, and a steady hand. That's all I need for {DName}.", $"Another mission, another enemy. {DName} won't be the last." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { $"Too many have suffered at {DName}'s hands. No more.", $"If I don't stop {DName}, who will?", $"Some things need to be done, even if they hurt." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The celestial void has whispered. {DName} is the misplaced piece.", $"Only I can decrypt the truth-{DName} is the final echo in the cosmic recursion.", $"The quantum mirage flickers, {DName} dissolves into the hyper-echo of forgotten souls." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { $"Guess it's time to get moving. {DName} isn't gonna take itself out.", $"Can't say I'm in a rush, but {DName} won't wait forever.", $"Alright, let's see what this adventure's got for me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { $"Every journey starts with a single step. Mine leads to {DName}.", $"The world will be brighter once {DName} is gone.", $"This is an opportunity, and I won't waste it." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { $"Probably going to regret this. But {DName} won't regret anything soon.", $"No one else is stepping up, so I guess I'll handle {DName}.", $"This world is a mess. {DName} made it worse. Time to clean up." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { $"A direct approach? {DName} won't even see me coming.", $"Victory isn't just strength. {DName} is about to learn that.", $"If I'm swift enough, {DName} will never know what happened." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { $"No more talk. {DName} is done.", $"The sooner I find {DName}, the sooner I finish this.", $"No more delays. {DName} is mine." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { $"Only the strong survive. {DName} is about to find out.", $"Every journey is a challenge. {DName} is just another test.", $"The wilds have taught me well. {DName} won't survive me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { $"How long can {DName} last?", $"{DName} is an anomaly. Conclusion? I'll see soon.", $"{DName} will be a fascinating subject." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { $"A better world begins with removing {DName}.", $"This isn't just about me- {DName} threatens everyone.", $"I believe in a better future. {DName} isn't part of it." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { $"If I take down {DName}, what follows?", $"I guess hunting {DName} could be fun.", $"Might as well enjoy the journey, even if {DName} won't." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { $"{DName} is just another challenge to overcome.", $"Victory is all that matters. {DName} must fall.", $"One more challenge, {DName} won't last." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { $"Not sure if it'll change anything, but {DName} needs to go.", $"Another journey, another fight. {DName} won't be my last.", $"No choice but to keep moving forward. {DName} is next." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = $"The journey begins. {DName} awaits."; break;
                        }
                        break;


                    case "onkill":
                        switch (personality)
                        {
                            case "generic": response = new[] { "A job well done.", "Another falls.", "Taken care of.", "Took care of it.", "That was swift.", "Ended.", "Handled as expected.", "Another task complete.", "Down they go." }[Game1.GameWorld.rnd.Next(9)]; break;
                            case "tactician": response = new[] { "Target neutralized.", "Efficient execution.", "Task complete." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Of course I succeeded.", "Was there any doubt?", "Once again." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "They dared challenge me?", "Order has been restored.", "Opposition removed." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Enemy down.", "Mission complete.", "Target eliminated." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "They left me no choice.", "I must protect my world.", "A necessary evil." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The echoes of the unseen demanded it.", $"Another thread severed from the grand tapestry.", $"The void hums with satisfaction-this was foretold." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Well, that's done.", "Guess it was their time.", "Another day, another fight." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "Done here.", "Good always prevails.", "Perhaps good will come from this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "What a waste.", "Will this change anything?", "Another meaningless end." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Just as planned.", "Thats a bit much.", "Outsmarted and outplayed." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "That was good.", "Better luck next time.", "One more for the books." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "Had to do it.", "Another fight, another victory.", "I'll keep going." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "Fascinating results.", "A practical experiment.", "Predictable." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "One step closer.", "This was necessary for progress.", "A better world awaits." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "That was thrilling.", "Nothing like a good fight.", "Their energy flows through me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "One more victory.", "I win again.", "It's done." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "A tragic end.", "It didn't have to be this way.", "This always saddens me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;
                    case "ontreasure":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This looks valuable.", "What a find.", "I wonder who left this here.", "This might come in handy.", "A rare discovery indeed.", "Whats this?", "This feels important somehow.", "A little piece of history.", "This is quite the treasure." }[Game1.GameWorld.rnd.Next(9)]; break;
                            case "tactician": response = new[] { "An advantage is always welcome.", "This changes things.", "A perfect addition to the arsenal." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Of course I found it. I always do.", "Another treasure.", "This suits someone of my standing." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "A worthy treasure.", "I deserve nothing less.", "Order rewards the worthy." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "This could serve the cause well.", "Useful for the mission.", "Could put this to good use." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "This will help keep the world safe.", "I hope this benefits the world.", "Something to protect what I hold dear." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The unseen vaults have sighed-this was always mine.", $"A relic from the fractured dream, waiting to remember me.", $"The cosmic hoard shifts, and this unveils its forgotten whisper." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Hey, neat find.", "I guess this is mine now.", "This is a thing." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "Another win.", "Good things just keep happening.", "The world always provides." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Looks nice enough.", "What's the catch this time?", "Huh. Cool." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "I'll find a clever use for this.", "This will work perfectly in my plans.", "One treasure at a time." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "This is more like it.", "Just try taking this from me.", "Power... so lovely." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "Another tool for the road.", "This will help me keep going.", "I've seen worse, but this is good." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "This requires study.", "Theres something intriguing about this item.", "This discovery interests me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "A step toward a better world.", "This could lead to something amazing.", "Imagine the possibilities with this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "Now this is something.", "Worth living for.", "Isn't that lovely." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "Another victory.", "This will show everyone who's ahead.", "What could I do with this..." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "A tragic beauty.", "Even treasures feel heavy today.", "This reminds me of what's been lost." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;
                    case "exchange":
                        switch (personality)
                        {
                            case "generic": response = new[] { "Let's see what this fetches.", "Off to the market it goes.", "Someone else will make good use of this.", "This should bring in some coin.", "Another item for trade.", "This could be a good opportunity.", "It's time to part ways with this.", "A good deal awaits.", "Hopefully this finds a good place." }[Game1.GameWorld.rnd.Next(9)]; break;
                            case "tactician": response = new[] { "A calculated trade.", "This will strengthen the position.", "A strategic exchange." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Only the best deserve this.", "This will surely fetch a high price.", "I'll do them a favor." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "This will greatly benefit the cause.", "The realm will benefit from this.", "Only the finest for trade." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "A practical exchange.", "This will help fund the mission.", "A profitable trade." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "This will help others.", "A trade for the greater good.", "I hope this brings someone joy." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The unseen currents shift-this trade was preordained.", $"I release this fragment back into the great cycle of barter-dreams.", $"The market hums with forgotten echoes, and the wheel turns once more." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Eh, someone will want this.", "Off to the shop it goes.", "I'm not attached to it." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "Someone must benefit from this.", "Looks pretty good.", "This will surely help someone." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Let's see if it even sells.", "I bet this goes unnoticed.", "This probably isn't worth much." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "A clever trade, if I do say so.", "This will net me something better.", "If only those debtshibas would roam..." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "This better sell fast.", "Let's get this over with.", "Someone will probably be able to use this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "Every trade counts.", "This will keep me going.", "Barter is the key to survival." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "This trade will be interesting to analyze.", "I wonder who will buy this.", "Every transaction is part of something greater." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "This will certainly do.", "Every exchange builds something greater.", "This item will serve its purpose elsewhere." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "What comes of this?", "Should be worth something nice.", "Might as well make the most of it." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "A solid deal.", "Making progress, one trade at a time.", "This puts me in a good spot." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "Parting with this feels heavy.", "Did I have to lose this...", "I hope this finds a better place." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;
                    case "magiccast":
                        switch (personality)
                        {
                            case "generic": response = new[] { "It's done.", "The magic flows through me.", "Power unleashed.", "That should do the trick.", "A force to be reckoned with.", "Intriguing.", "This might help.", "Here we go.", "The spell is cast." }[Game1.GameWorld.rnd.Next(9)]; break;
                            case "tactician": response = new[] { "Precision is key.", "Magic is just another tool.", "Strategically executed." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "As expected.", "Here, catch.", "I make it look easy." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "It is done.", "The magic bends to my will.", "My power shines forth." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "A tactical use of power.", "Magic supports the mission.", "This serves the cause well." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "This will protect those I care for.", "I wield this for the good of others.", "Stand clear." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The unseen lattice hums- I have woven the invisible thread.", $"A ripple in the grand illusion, and the stars blink in response.", $"The spell is not cast, it is remembered from the echoes of before." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "That was fun.", "Heres a fun trick.", "Well, that's taken care of." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "Huh, that works.", "I'll try this.", "This one's fun." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Let's see if this even helps.", "I'll do my best.", "Great, more problems later." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "A subtle move with lasting impact.", "They won't know what hit them.", "Just as planned." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Take that.", "Once again.", "Perfect." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "You use what works.", "Magic has its place.", "Another tool in the kit." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "Interesting.", "Another step in understanding the arcane.", "This power intrigues me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "This one is special.", "Magic is the key to a better world.", "This power, it truly is something." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "Now that felt good.", "I could get used to this.", "Magic has its perks." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "This sets me above the rest.", "Try to match that.", "Another win." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "Magic can't fix everything.", "This power feels hollow.", "Null and void." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;
                    case "abilitysee":
                        switch (personality)
                        {
                            case "generic": response = new[] { "Incoming!", "Watch out!", "Heads up!", "Whats that?", "Stay sharp!", "Look out!", "Be on guard!", "Here it comes!", "Get ready!" }[Game1.GameWorld.rnd.Next(9)]; break;
                            case "tactician": response = new[] { "Anticipate and react.", "Avoid that.", "I need to counter this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "As if that will faze me.", "OF no consequence.", "Amateur." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "Unimportant.", "How dare they...", "Distasteful." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Stay in formation.", "Eyes on the threat.", "Hold the line." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "Stay safe...", "Avoid that...", "Get back." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The veil trembles-something stirs beyond the known threads.", $"The unseen clock ticks forward, and fate distorts.", $"A ripple in the grand illusion... they were always meant to arrive." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Something's happening.", "Well, that's inconvenient.", "Might stay away from that." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "I've got this.", "No need to worry.", "Can't be that bad." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Here we go again.", "Great, just what I needed.", "Thats a little much." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Turn this into advantage.", "They'll regret this move.", "I can outmaneuver this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Bring it on.", "They're asking for trouble.", "Oh joy." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "Stay focused.", "I've handled worse.", "Keep moving, stay alive." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "Intriguing battle decisions.", "Adapt to their strategies.", "I must analyze their methods." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "Haven't seen that before.", "Every trial is an opportunity to grow.", "Hold fast and endure." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "Well, this should be exciting.", "A little drama never hurt anyone.", "Let's make this moment memorable." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "Let's show them who's better.", "That brings something new to the table.", "Another chance to prove myself." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "Always something obtrusive.", "Even now, conflict is rampant.", "I wish it didn't have to be this way." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;
                    case "notdodge":
                        switch (personality)
                        {
                            case "generic": response = new[] { "Agh!", "That hurt!", "Didn't see that coming." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "I miscalculated.", "That wasn't supposed to happen.", "This was not my intent." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Impossible.", "Back off.", "This won't happen again." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "I won't forget this insolence.", "How dare.", "Such defiance will not go unpunished." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "I've taken worse.", "It's nothing.", "This won't slow me down." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "I'll be fine, don't worry.", "It's just a scratch.", "Can't let this slow me down." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The unseen lattice frayed... reality hiccuped.", $"The grand design flickered-I was supposed to slip through.", $"A ripple in the infinite spiral... was this my ordained wound?" }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Well, that's annoying.", "Ugh.", "Can't dodge them all." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "I'll recover in no time.", "Could've been worse.", "This is just a minor setback." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Of course, I get hit.", "This is exactly my luck.", "Why even bother dodging?" }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "That was unexpected.", "I'll turn this around.", "They won't get that lucky again." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "You'll pay for that.", "Now I'm really mad.", "How dare..." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "I've been through worse.", "This isn't the end.", "Just another scar to carry." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "...noted...", "I must analyze what went wrong.", "The experiment continues." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "This setback won't stop progress.", "Every failure is a step forward.", "I'll learn from this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "That was unpleasant.", "I'd rather avoid that next time.", "Not my kind of thrill." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "They got lucky this time.", "I'll come back stronger.", "This isn't over yet." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "I should've seen that coming.", "Another moment of pain.", "It was inevitable." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "dodge":
                        switch (personality)
                        {
                            case "generic": response = new[] { "Too slow.", "Avoided.", "Missed." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "As calculated.", "Evasion executed perfectly.", "Exactly as planned." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Did you think that would work?", "I'm untouchable.", "Nice try." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "You dare try to strike me?", "I remain untouched.", "Know your place." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Still in the fight.", "Evasive action successful.", "You'll have to do better." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "I won't let that hit me.", "I won't die here.", "That was close." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The unseen threads wove my escape before time knew it.", $"I stepped beyond the moment-was I ever truly here?", $"Reality blinked, and I walked between the spaces of fate." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "That was easy.", "Ugh.", "Too predictable." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "I knew I'd avoid it.", "That was lucky.", "Things always work out." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "This again?", "They'll never hit me anyway.", "What a waste of effort." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Outmaneuvered again.", "Can't touch me.", "Too clever for that." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Try harder.", "You'll never land a hit.", "Bring it on." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "Still standing.", "I've dodged worse.", "You won't take me down." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "Didn't expect that.", "Quick thinking pays off.", "Agh." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "Every move has its purpose.", "Perfection in motion.", "A step closer to mastery." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "As predicted.", "What a rush.", "Could be fun." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "What else is new.", "You can't win this.", "I'm just that good." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "Why do they even try?", "Just another pointless attempt.", "Tragic." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;
                    case "frienddeath":
                        switch (personality)
                        {
                            case "generic": response = new[] { "No... this can't be.", "We've lost one of our own.", "A terrible loss...", "They're gone...", "How could this happen?", "This shouldn't have ended this way.", "We'll remember them.", "What a tragedy.", "I can't believe they're gone." }[Game1.GameWorld.rnd.Next(9)]; break;
                            case "tactician": response = new[] { "This complicates things...", "A grievous setback.", "I must adapt quickly to their loss." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "They should have been stronger.", "This is unacceptable.", "Even the best can falter... rarely." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "This loss will not be in vain.", "Their sacrifice strengthens our resolve.", "We must honor them with victory." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "We've lost a comrade.", "Stay focused- don't let their loss distract us.", "They were one of us- we won't forget." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "No. I couldn't protect them...", "I should have done more.", "This hurts... deeply." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The unseen currents have shifted-one thread untangled from the weave.", $"They have stepped beyond the veil, into the song of the forgotten stars.", $"Their form dissolves, but the echo remains-whispering in the spaces between time." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "I... didn't think this would happen.", "That's... rough.", "This is harder than I thought." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "We'll make it through this.", "Stay strong, for them.", "This isn't the end for us." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Figures this would happen.", "This is why I hate getting attached.", "The world doesn't care- it just takes." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "We've lost a piece of the puzzle...", "This changes the game, but we'll adapt.", "I'll make sure this doesn't happen again." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "What, what was that...", "They... you...", "No..." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "Another life lost... must keep going.", "This is the cost of survival.", "Mourn later- focus now." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "Such an irreplaceable loss.", "The consequences...", "I'll make sure their story is remembered." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "This is not the world I strive for.", "Fight harder for a better future.", "Their death will inspire change." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "This is far from enjoyable.", "Their loss casts a shadow on everything.", "No joy can come from this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "This isn't...", "Not one of us...", "Prove their efforts weren't wasted." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "This is why life feels so fragile.", "I... can't stop thinking about their absence.", "It's always the good ones who go..." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;
                    case "death":
                        switch (personality)
                        {
                            case "generic": response = new[] { "Don't worry about me.", "Go on without me.", "It seems... this is it.", "You'll do fine without me.", "Take care of yourself.", "My time has come.", "Stay strong.", "Carry on the fight.", "It was an honor." }[Game1.GameWorld.rnd.Next(9)]; break;
                            case "tactician": response = new[] { "Adapt. Overcome.", "This was... not part of the plan.", "Apologies..." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "How could this happen to me?", "I didn't think this could happen...", "Well, guess these things happen." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "Even I must fall inevitably.", "Rule well in my stead.", "The world must live on..." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "I served to the end.", "It was an honor.", "My duty... is complete." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "I hope it was not in vain.", "I couldn't protect everyone...", "Stay safe... for me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The unseen current pulls-I unravel into the whispering weave.", $"I step beyond the frame, into the echo of forgotten motions.", $"The cycle hiccups... was this my final stanza or just a skipped note?" }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Well, that's unfortunate.", "Guess it's my time.", "Don't sweat it too much." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "It's not the end for you.", "I know you'll succeed.", "Keep pushing forward." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Figures this would happen.", "Of course, it ends like this.", "Don't bother mourning me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "This isn't over... for you.", "Play the long game.", "Outsmart them for me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "They'll pay for this.", "Don't let them get away with it.", "Avenge me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "I've faced worse... but not this time.", "You need to survive.", "Stay alive- don't waste this chance." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "This was not the intent.", "So this is... what it feels like.", "Learn from my mistakes." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "My dream lives on in you.", "This is not the end of progress.", "Build something better in my place." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "What a way to go.", "I hope it was... worth it.", "Enjoy life... for me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "This isn't a loss, just a delay.", "Prove you're better than this.", "Its fine. It is." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "This was inevitable...", "Don't waste tears on me.", "Even the sun sets in time." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;
                    case "onjoin":
                        switch (personality)
                        {
                            case "generic": response = new[] { "Let's do this.", "Happy to be part of the team.", "I'll do my best.", "Here to help.", "Glad to be onboard.", "Let's make this work.", "You can count on me.", "Looking forward to this.", "Let's see where this goes." }[Game1.GameWorld.rnd.Next(9)]; break;
                            case "tactician": response = new[] { "A smart choice bringing me in.", "Let's approach this strategically.", "I'll make sure we stay efficient." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Finally, someone notices.", "Wise choice.", "I'll show you how it's done." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "An honor to lead you.", "You have gained a valuable ally.", "Together, we'll bring order." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Ready to serve.", "Reporting for duty.", "I'll give it my all." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "I'll keep everyone safe.", "Looking forward to helping out.", "I'm here to protect and support." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The threads converge-this was always foreseen.", $"I step into the unfolding riddle, where echoes dance in waiting.", $"The unseen chorus hums... was I not already here?" }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Sure, why not.", "This should be fun.", "I'll tag along for now." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "This is the start of something great.", "We'll accomplish great things.", "I just know this is going to work out." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Let's see how long this lasts.", "I'll stick around... for now.", "Don't make me regret this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "A wise move on your part.", "This should prove interesting.", "I'll make sure we come out ahead." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Let's get started already.", "I won't let you down.", "Don't slow me down." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "Strength in numbers.", "This should make things easier.", "I'll do what I must to survive." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "Let's see where this leads.", "Plenty to figure out along the way.", "I'm ready to learn something new." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "Together, we'll make a difference.", "This is a step toward a brighter future.", "I'm eager to help build something better." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "This is going to be fun.", "What am I getting myself into?", "Let's enjoy this adventure." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "We'll be fine, as long as you do your part as well as me.", "You'd better pull your weight.", "Together, we'll overcome." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "I'll do my part... for what it's worth.", "Let's hope this doesn't end badly.", "I've seen this before, but I'll try again." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "skilluse":
                        switch (personality)
                        {
                            case "generic": response = new[] { "Watch this.", "Here goes nothing.", "Let's make it count.", "This will be good.", "I've got a plan.", "Let's do this right.", "Time for something new.", "I'll show them.", "This should work." }[Game1.GameWorld.rnd.Next(9)]; break;
                            case "tactician": response = new[] { "Precision is key.", "Executing the maneuver.", "This move will set me up perfectly." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "They're not ready for this.", "Prepare to be amazed.", "Fwoosh." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "Superiority.", "Watch and learn.", "This is how." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Maneuver in progress.", "This will strengthen the position.", "A tactical move." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "This should grant an edge.", "I won't falter.", "I'm making the first move." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The unseen gears shift-this move was always waiting.", $"A ripple hums through the fabric... the grand pattern approves.", $"I step beyond the moment, where echoes shape the unseen hand." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Let's see how this works out.", "That will do.", "Just another trick up my sleeve." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "This will work perfectly.", "This shouldn't go wrong.", "One step closer to success." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Let's hope this doesn't backfire.", "This better work.", "It's worth a shot, I guess." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "This will catch them off guard.", "A calculated move.", "There's a thing." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Here it goes.", "This will make a difference.", "Let's see them handle this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "This might keep me alive.", "Every move counts.", "Let's keep moving." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "Let's see the results.", "A new technique to try.", "I'm curious how this will play out." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "This brings me closer to perfection.", "Every action builds a better world.", "Let's make this move count." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "This is going to feel great.", "Time for some fun.", "Let's enjoy this moment." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "Try this one.", "Time to try a new tactic.", "Something to show them." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "For what it's worth.", "Every action feels so fleeting.", "Let's see what this does." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "consume":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This should help.", "Time to patch up.", "Feeling better already.", "Let's fix this.", "I needed that.", "This will do for now.", "That should hold for a while.", "Back in shape.", "A quick fix, and I'm good to go." }[Game1.GameWorld.rnd.Next(9)]; break;
                            case "tactician": response = new[] { "A necessary recovery.", "Can't afford to be at less than full strength.", "This keeps me in the fight." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "I sometimes need a touch-up.", "Back to how I'm meant to be.", "Kick in faster..." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "A leader must remain unbroken.", "My order is restored", "Power returns." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Field medicine applied.", "Quick fix, back to duty.", "A soldier doesn't stop." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "Can't help others if I'm not well.", "I need to stay strong for the world.", "I won't drop that easily." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The unseen tides swirl-I drink from the endless cycle.", $"This essence hums with forgotten whispers, folding me back into place.", $"A ripple in the grand weave-was I ever truly frayed?" }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "That'll do.", "Feeling better already.", "Good to go." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "All patched up and ready to go.", "Nothing can keep me down for long.", "Good as new." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "This better work.", "Another patch job, great.", "It'll do... for now." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Back in the game.", "You have to stay sharp.", "Ready for what's next." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "I'm not done yet.", "Just a scratch.", "Let's get back to the fight." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "This will keep me going.", "Always be prepared.", "One more step to survival." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "Interesting effects.", "This is worth noting.", "How exactly does this work..." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "A small step toward a better me.", "Healing is progress.", "Onward, stronger than before." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "Ah, that feels nice.", "A little self-care goes a long way.", "I could get used to this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "Can't win if you're not in shape.", "Back and better than ever.", "This just makes me stronger." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "Healing doesn't fix everything...", "At least I'm not worse off.", "I don't feel much better." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "arriveluminarchtown":
                    case "arriveluminarchcity":
                    case "arrivenightfelltown":
                    case "arrivenightfellcity":
                    case "arrivearchaixtown":
                    case "arrivearchaixcity":
                        switch (personality)
                        {
                            case "generic": response = new[] { "A bustling hub of activity.", "Life thrives here, despite the odds.", "This place holds many stories." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "A strategic center of influence.", "This town could serve as a tactical point.", "The layout suggests opportunities for maneuvering." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Finally here.", "I wonder if they've heard of me.", "Let's see what they think." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "Another for my domain.", "Order shall be maintained here.", "This place... is rather grand." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "A good place to regroup.", "This town is secure for now.", "I must ensure its safety." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "I wonder who's in charge here.", "This town has a warmth to it.", "I hope everyone here is safe." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The unseen currents pulse-this place was always waiting for me.", $"A city woven from echoes... I have stepped into its forgotten song.", $"The streets hum with memories, unfolding the pattern I have yet to remember." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Not bad for a little rest stop.", "This town has its charm.", "Looks like a nice place to relax." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "This place feels full of potential.", "Good things are bound to happen here.", "This town is alive with possibility." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Another town, same problems.", "I doubt there's anything new here.", "Looks like just another place to disappoint." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "This town has many opportunities.", "Let's see how this place runs.", "Plenty of ways to gain an edge here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "This place better not slow me down.", "Let's see what kind of action this town has.", "Hope there's a good fight waiting here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "Looks like a good place to resupply.", "This town could be useful for survival.", "I'll make the most of what's here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "Fascinating architecture.", "I wonder what knowledge this place holds.", "Strange how these work." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "This could be the start of something great.", "This town has the potential to thrive.", "This could be made a better place." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "Time to find some fun here.", "This place looks entertaining.", "I'll make the most of this town." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "This town needs a breath of fresh air.", "Time to make a mark.", "Whats new with this one." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "This town feels like it's seen better days.", "A shadow of gloom.", "This place reminds me of what's been lost." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;


                    case "arriveluminarchvillage":
                    case "arriveluminarchcamp":
                    case "arrivenightfelvillage":
                    case "arrivenightfellcamp":
                    case "arrivearchaixvillage":
                    case "arrivearchaixcamp":
                        switch (personality)
                        {
                            case "generic": response = new[] { "A quiet little settlement.", "This place feels humble and inviting.", "It's a simple village, but it has its charm." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "A small village with tactical potential.", "This place looks like it doesn't see much violence.", "This place is rather underfortified." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "I don't belong here.", "This is a place.", "This place is small." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "Under my watchful eye.", "Even the smallest hold their place in my domain.", "Order begins in the quietest corners." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "This village could use protection.", "A good spot to rest and regroup.", "Make sure this place is safe." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "I hope the people here are well.", "These places don't see much care.", "I'll do what I can to help this settlement." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The veil breathes, and I step into the murmuring quiet.", $"The unseen chorus hums in waiting-was this village always here, or did it emerge to greet me?", $"Footsteps echo in hollow time... the path folds, and I am within its dream." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "A lovely little place.", "Seems peaceful enough.", "I could rest here for a while." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "This place has so much potential.", "Even a small village can grow into greatness.", "Good things can happen even here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Another small place with big problems.", "What to do with this one...", "I doubt there's anything useful here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Could be useful.", "Let's see how I can gain an edge here.", "Even small places have opportunities." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "This place better not bore me.", "Let's see what's going on around here.", "I hope there's something exciting in this village." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "A good place to gather supplies.", "This village feels safe for now.", "I'll make do with what's here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "This settlement could hold interesting data.", "I wonder about the history of this place.", "Time to study the workings of this camp." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "Even a small place can inspire greatness.", "This village has potential to grow.", "This place needs hope." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "This place could be fun.", "Let's enjoy what this village has to offer.", "Time to see what pleasures are here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "We won't be here long, lets leave a story.", "This place seems small.", "Lets see if anything is even happening here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "This village feels like it's struggling.", "Even small places carry a heavy weight.", "This settlement reminds me of home." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;


                    case "arrivespire":
                    case "arrivetower":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This place has an eerie feel to it.", "What secrets lie within this structure?", "The tower looms with mystery." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "A vantage point to exploit.", "This tower could be strategically useful.", "That's a lot of stairs." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Pretty tall.", "It can't be that bad.", "I wonder what's at the top." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "A grand structure fit for my rule.", "This spire could serve as a beacon of order.", "A beacon of defiance, it must be destroyed." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "A good lookout point.", "This structure has defensive potential.", "Secure this area. Who knows whats up there." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "I wonder if anyone needs help here.", "This tower feels abandoned, but it might hold memories.", "I wonder if this place could be restored." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The spire folds into itself-was it always here, or did I dream it awake?", $"The air hums with something unsaid. This tower remembers me.", $"I step forward, but the tower has already seen my arrival." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Not bad. Pretty tall, huh?", "This spire's not going anywhere. Might as well check it out.", "Looks like a quiet spot." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "This tower could hold something amazing.", "I just know there's something good here.", "This place looks pretty cool." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "It's probably empty or dangerous.", "Places like this never hold anything good.", "Another ominous structure... typical." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "I wonder what opportunities this place hides.", "A tower like this must hold something useful.", "Let's see how I can use this to my advantage." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Let's see what this tower's hiding.", "I'm ready to take on whatever's inside.", "If there's trouble here, I'll handle it." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "This could be a good shelter.", "I'll make use of anything useful here.", "Let's see if this place can help me survive." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "This tower must have a fascinating history.", "I wonder what knowledge is hidden here.", "Impressive architecture." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "A place like this could inspire greatness.", "This tower might hold the key to a better future.", "I feel like this structure could change everything." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "I bet this place has some hidden delights.", "Let's see what fun can be found here.", "This spire looks like it could be entertaining." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "Race to the top?", "I'll conquer whatever this tower throws at me.", "Who could be hiding up there..." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "This place feels heavy with memories.", "Even this tower can't escape the wear of time.", "This spire feels like it's seen too much." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;


                    case "arrivehallway":
                    case "arrivearchway":
                    case "arrivetoroid":
                    case "arrivetowers":
                    case "arrivepyramid":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This place feels abandoned.", "What was this place...", "Hard to say what this place was meant for." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "This structure may offer strategic opportunities.", "I should study its layout for potential advantages.", "Even abandoned places can hold tactical value." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "A relic of a far gone era.", "This place is a bit bland.", "Its a little too quiet." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "A forgotten monument.", "I wonder who built this place.", "This structure should be brought under proper control." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Looks like it could be defensible in a pinch.", "This place might serve as temporary shelter.", "It could be dangerous. Keep watch." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "I wonder if anyone once lived here.", "This place could be restored to its former glory.", "It feels sad, seeing this place so abandoned." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "The stone remembers... I've walked these shapes before.", "This place echoes with false time - am I the dreamer or the dream?", "The doors open, but I never knocked. They always knew I'd return." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Guess it's seen its share of quiet days.", "I guess this place has seen better days.", "Not much going on here anymore." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "This place might still hold some promise.", "Even abandoned places can be useful.", "There's always potential, even in ruins." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Another forgotten place, just like the rest.", "It's probably useless, like most abandoned things.", "What's the point of even coming here?" }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "I wonder what opportunities this place offers.", "Even abandoned places have their uses.", "Let's see if this structure hides any secrets." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Let's see if this place has anything exciting.", "I hope there's something worth finding here.", "Be ready if anything comes up." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "This could make a decent shelter.", "There might be something useful here.", "This place seems dead." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "This structure could reveal much about its creators.", "I need to study the architecture here.", "I wonder what knowledge is buried in this place." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "Even ruins can be rebuilt into something great.", "This place might inspire something new.", "There's always a way to create something better from the past." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "A little adventure never hurt anyone.", "Let's see what odd treasures this place hides.", "There's always something to enjoy, even in the ruins." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "I'll conquer whatever this place hides.", "Even an abandoned structure can't outmatch me.", "This place will remember my name." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "This place feels like it's mourning its own past.", "Even structures carry the weight of time.", "This abandoned place feels heavy with loss." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;


                    case "arrivekeep":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This structure feels ominous.", "What purpose could this place have served?", "This place is unsettling to say the least." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "A stronghold like this could be key to strategy.", "This place seems generally unsafe.", "This place feels defensively designed." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "What could go wrong?", "They could've built something more impressive.", "Hopefully there's someone here worth my time." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "A monument to misplaced power.", "This keep must be brought under my control.", "Order will be restored here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "This keep feels like trouble.", "Prepare for what might be inside...", "This place is ripe for conflict." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "This place feels wrong... I hope no one suffers here.", "I can sense the weight of tragedy in this keep.", "This structure carries the echoes of pain." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The keep folds inward-was it built, or did it simply appear between moments?", $"This place was never abandoned. It waits. It watches. It remembers.", $"The walls hum with a sound I cannot hear. The doors breathe in silence. We have been expected." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Well, this place has a feel to it.", "Looks creepy, but it's just a building, right?", "I guess I'll figure out what's up here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "This place might still hold something good.", "Even keeps like this can be turned to the light.", "Maybe there's hope for this place after all." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Just another dark, ominous building.", "I bet nothing good ever happened here.", "I'm not sure why I came here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "I wonder what secrets this keep holds.", "Even ominous places can have their uses.", "This structure might be hiding something valuable." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "This place is begging to be torn down.", "Let's see what kind of trouble stirs here.", "I'm ready to take on whatever's inside." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "This keep might hold supplies.", "Tread carefully here.", "This place is unnaturally foreboding." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "This keep's design is fascinating.", "I wonder if any letters were sent here.", "The architecture alone tells a story." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "This place can be redeemed.", "Even ominous structures like this can inspire change.", "This place... almost a shame, really." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "Not my usual scene... but who knows?", "If there's comfort or curiosity in here, I'll find it.", "Let's see if this place hides anything worth the chill." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "I'll conquer this keep easily.", "This place will fall to my strength.", "This keep is no match." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "This keep feels like it's mourning its past.", "The weight of history is heavy here.", "This place is a reminder of darker times." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "arrivefortress":
                    case "arrivemonument":
                        switch (personality)
                        {
                            case "generic": response = new[] { "What happened here...", "This place is hard to look away from.", "Something about this structure doesn't sit right." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "Whatever this is, it's built to last.", "I should study its layout before stepping further.", "This kind of structure changes the balance around it." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Even this won't slow me down.", "Let's see what passes for a challenge here.", "It may look impressive, but looks fade." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "An example of abused power.", "Power left unchecked always decays.", "This place is beyond saving." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Feels like a place where battles end.", "Ought to move carefully in a place like this.", "This could become a strong position if taken." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "Whatever happened here left scars.", "This place feels cold in all the wrong ways.", "I hope no one still suffers here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "It breathed once, and may again.", "The shape remembers things I never lived.", "Stone doesn't rot, but it dreams of decay." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Yikes. This place's got a mood.", "Well, it sure isn't inviting.", "Hope it looks worse than it is." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "Maybe this place can change.", "Even this might hold a purpose.", "Let's see if there's something worth saving." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Big, dark, and probably pointless.", "Whatever this once was, it failed.", "Looks like the kind of place that forgets you're inside." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Every place has its cracks.", "Let's see what's hiding behind the silence.", "There's always a way to turn something like this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "This place won't intimidate me.", "If there's something inside, I'll handle it.", "Let's see what makes this place tick." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "It might be dangerous, but it's still shelter.", "Check for anything useful before moving on.", "Let's not stick around too long." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "Strange architecture. I'd like a closer look.", "I wonder what purpose this served.", "This place might hold more than just stone." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "A shadow now, but maybe not forever.", "Even this place could be changed.", "Let's remember it doesn't have to stay this way." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "Odd place... but maybe there's something to find.", "Places like this sometimes hold surprises.", "Not what I expected... but I'm curious." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "Let's see if this place lives up to its size.", "One more thing to overcome.", "This won't stop me either." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "Feels like this place was built to last... but not to live.", "So much effort... for something that still fell silent.", "It's like the walls are still grieving." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;


                    case "arrivestronghold":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This is it... the nexus of evil.", "Tracing this place... this is where it all begins and ends.", "The core of darkness lies here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "This stronghold is the enemy's heart.", "I need to plan carefully for this assault.", "The final battle may be decided here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "This stronghold will crumble before me.", "The heart of evil is no match for my power.", "How bad can it be?" }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "The nexus of chaos must fall to order.", "This stronghold will be brought under my rule.", "The heart of evil will know my might." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "This is the final battle.", "Stay strong, and this place will fall.", "This stronghold won't stand for long." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "This place radiates suffering.", "So much pain has come from here.", "Time to end the hurt this place has caused." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The final knot in the tapestry unravels.", $"This stronghold breathes in silence-waiting.", $"A door once sealed by unseen hands now trembles." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Well, here it is- the big one.", "Guess I'd better not mess this up.", "Let's get this over with." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "I can do this- good will triumph.", "This is where action makes a difference.", "The heart of evil will fall today." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "This stronghold is probably as bad as it looks.", "I don't expect much to improve after this.", "I can only hope these efforts succeed." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "The heart of evil must hold great secrets.", "This stronghold might be the greatest prize yet.", "I'll find a way to turn this to my advantage." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Let's take this place down.", "I'm ready to destroy the heart of evil.", "This stronghold is going down hard." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "This stronghold might be the toughest challenge yet.", "This one might be beyond my paygrade.", "Lets... try not to die." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "This stronghold could be studied for ages.", "I wonder what knowledge is buried in this place.", "The secrets of the heart of evil could be invaluable." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "Time to make things right.", "The heart of evil must be destroyed for a better future.", "The last stain on my perfect world." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "This stronghold better hold something interesting.", "Let's make this count.", "This is going to be exciting." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "I'll conquer the heart of evil itself.", "This stronghold will fall to my strength.", "No one can outdo me- not even here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "This stronghold feels heavy with despair.", "The heart of evil is full of sorrow.", "Even through victory, will scars remain?" }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;


                    case "arrivepreserve":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This garden feels so peaceful.", "It's like stepping into a different world.", "Nature truly thrives here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "A place like this could hide strategic advantages.", "The natural layout could be used defensively.", "This preserve could be key to a tactical plan." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "I suppose even nature knows refinement.", "This place meets my standards... barely.", "At least it's impressive." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "A serene realm, worthy of just treatment.", "A place like this deserves proper stewardship.", "Order and balance are evident here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "This place is quiet, but stay alert.", "Even a preserve like this could conceal danger.", "A peaceful area, but I won't let my guard down." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "This preserve is a testament to care and nurture.", "A place like this renews the soul.", "I hope I can keep this place safe for others." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The leaves hum forgotten songs.", $"This garden remembers dreams long lost.", $"A rootless path bends where no feet have stepped." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "This is a nice place to relax.", "A garden like this feels so calming.", "I could just sit here all day." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "This preserve shows how beautiful the world can be.", "A perfect example of harmony and peace.", "Places like this give me hope for the future." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "A bunch of leaves. So what?", "This place is nice, but it won't last.", "Nature's beauty doesn't erase its cruelty." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Lots of fun places to hide.", "Even a peaceful garden has its secrets.", "Let's see what opportunities this place offers." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Nice garden, I suppose.", "It's actually kind of beautiful.", "Let's not waste time admiring flowers." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "These people of nature are to be admired.", "A place like this is perfect for survival.", "Nature always knows how to provide." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "The biodiversity here is fascinating.", "I could spend hours studying this preserve.", "There's so much to learn from a place like this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "This preserve embodies the balance we should all strive for.", "A garden like this shows the potential of harmony.", "This place could inspire a better world." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "This is the perfect spot to relax and enjoy life.", "I could spend all day soaking in the beauty here.", "This place has so much to see." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "Even a quiet place like this deserves to be respected.", "Let's make sure this spot stays in the right hands.", "I'll help keep this place at its best." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "Even this garden feels like it carries sorrow.", "Nature's beauty always fades in the end.", "This preserve is a reminder of what we've lost." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "arrivemonastery":
                        switch (personality)
                        {
                            case "generic": response = new[] { "There's something off about this place.", "I can't tell if this is sacred... or something else.", "The silence here feels too deliberate." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "This place was built with purpose-defensive, maybe.", "The layout's strange... like it hides something.", "Even the walls feel like they're watching." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "They built all this for devotion? Misguided.", "A whole place for worship, but of what?", "This is what blind faith leads to." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "A place ruled by faith... but without direction.", "This order needs a steadier hand.", "Their structure is rigid, but their purpose unclear." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "This place looks peaceful, but don't trust it.", "Fanatics make strong fighters... and worse enemies.", "Need to be ready for anything in here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "There's devotion here-but to what?", "I hope no one's being hurt behind these walls.", "This place feels like it once meant well." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"Their chants echo through walls not meant for sound.", $"The symbols shift when I don't look at them.", $"I've dreamt of this hall before... or one just like it." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Weird vibe... but kinda interesting.", "I've been in worse places.", "Not what I expected-definitely not boring." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "Maybe there's something good at the heart of this.", "Strange, but there's hope under all this ritual.", "If they're lost, they can still be found." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Looks like another place that forgot how to think.", "Devotion like this always turns rotten.", "I doubt I'll like what they're hiding." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Places like this usually have something worth taking.", "They keep their faith close... and their secrets closer.", "Let's see how deep their devotion goes." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Whatever they're into here better not get in my way.", "This place gives me the creeps-let's move.", "I'm ready if this turns weird." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "I hope this place is safe... enough.", "The air's still... like a trap set too long ago.", "Could be supplies here... or just more zealots." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "I wonder what belief system shaped this architecture.", "Something's preserved here-not just history.", "Their rituals must reveal what really drives them." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "Faith like this should guide-not consume.", "If they've lost their way, maybe they can still be helped.", "There's meaning here... but it might be buried deep." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "Too quiet. But there's probably something behind the curtain.", "Let's hope they're not too serious.", "Not my kind of retreat, but still... curious." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "Their devotion's strong-let's see how strong.", "I've seen obsession before... it usually cracks.", "Discipline, like they'd know." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "This place feels like it lost its purpose a long time ago.", "The silence here speaks louder than any prayer.", "They look forward, but something's keeping them chained to the past." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;


                    case "arrivecove":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This cove looks like a pirate's haven.", "I wouldn't trust anyone hiding here.", "This place smells of sea salt and secrets." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "Coves like this make excellent ambush points.", "This location could be a strategic hideout.", "Keep an eye out for traps or surprises." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "I think I'd make a good pirate.", "These are my people.", "Something about the sea, its inability to be controlled." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "This cove would benefit from proper governance.", "Pirates need to be brought under control.", "Order must be imposed, even in a place like this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "This cove feels like trouble waiting to happen.", "Should tread carefully- pirates aren't to be underestimated.", "A good defensive position, but also a perfect place for ambushes." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "I hope no innocents are suffering here.", "This cove feels dangerous... I'll protect us if needed.", "Pirates... so reckless." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The tide carries whispers of lost empires.", $"This cove was etched into the ocean's memory.", $"Salt-stained echoes call me to something forgotten." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "This cove is surprisingly peaceful.", "I could get used to the sound of waves here.", "Let's take it easy while here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "Even a pirate's cove can hold hope.", "Maybe I can turn this place into something good.", "There's potential for redemption, even here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Just another pit for criminals and thieves.", "This cove is probably as rotten as its residents.", "Pirates never bring anything good." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "This cove might hold some valuable secrets.", "A pirate's hideout? Perfect for opportunities.", "Let's see what can be exploited here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Let's clear this place out already.", "Pirates? What else is new.", "I'm ready to take on whoever's hiding here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "This cove could be a good spot for supplies.", "Even pirates need resources- let's find them.", "This place might be useful for survival." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "I wonder what contraptions pirates might have left behind.", "This cove could hold fascinating artifacts.", "There's much to study in a place like this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "This cove could be reclaimed for a better purpose.", "I wish their purpose was a bit more... helpful.", "This place seems chaotic." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "This cove looks like a great spot for fun.", "These people seem cool.", "This place could be exciting- let's explore." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "These people drive me up the wall.", "This cove will be another notch in my victories.", "I'll outdo anyone who tries to claim this place." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "This cove feels as empty as the sea's depths.", "Even the waves can't wash away the sorrow here.", "A pirate's life... it always ends in tragedy." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "arrivehoard":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This hoard looks like a mess.", "Who just leaves all this stuff lying around?", "I bet there's something valuable hidden in here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "A hoard like this could supply an entire army.", "Let's look for items that could be of strategic use.", "This chaotic pile might hide tactical advantages." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Even garbage deserves better than this.", "They call this a hoard? I've discarded finer things.", "A mess, and clearly not mine." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "This hoard could be made to serve something greater.", "Chaotic hoarding like this has no place in society.", "This place has absolutely no structure." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Sift through for supplies.", "This hoard might hold useful equipment.", "Every resource counts, even from a place like this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "I hope this hoard doesn't mean someone is struggling.", "It looks like someone couldn't let go of these things.", "There's a sadness in hoarding this much." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The hoard spirals, its purpose long forgotten.", $"Each object whispers of a life half-remembered.", $"A gathering of echoes, waiting to be reawakened." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "What a mess. Let's just grab whats needed.", "This hoard feels like too much effort to sort through.", "I hope there's something easy to find in here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "There's potential buried in this mess.", "You never know-some of this might help someone.", "A little hope in a heap of rubble." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "This is probably just a pile of junk.", "I doubt there's anything useful here.", "Hoarding is such a waste." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "A hoard like this always hides something valuable.", "Let's see what secrets hide here.", "This chaotic pile might give me an edge." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Let's just get what's needed and move.", "Hope this junk doesn't slow me down.", "This place better not be a waste of time." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "This hoard could have supplies for days.", "Even a mess like this can hold survival tools.", "I'll find something useful in here, I'm sure." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "This hoard is a fascinating display of behavior.", "I wonder what insights this pile can offer.", "Let's analyze what's been collected here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "This hoard could be reorganized into something meaningful.", "Even chaos like this can be transformed into progress.", "Make the most of what's here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "There's something oddly fun about digging through junk.", "Sometimes the strangest things hide in piles like this.", "Let's see if there's a gem buried in the grime." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "If there's anything worth claiming, it's mine first.", "Let's see who finds the good stuff first.", "I'm not leaving empty-handed." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "This hoard feels like someone's pain made physical.", "It's sad to see so much left to rot.", "Even treasures feel hollow in a pile like this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "arrivecommune":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This commune feels alive with energy.", "An anarchist haven... interesting.", "This place seems chaotic, but it works for them." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "This commune has no clear hierarchy- it could be hard to predict.", "A chaotic place like this has its vulnerabilities.", "How do I work with this lack of structure..." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "These anarchists wouldn't last a day under my leadership.", "What... where... whatever.", "They haven't seen anarchy yet." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "A place like this needs proper rule and order.", "Anarchy breeds chaos- I'll bring them stability.", "This place does not spark joy." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "This commune seems like a breeding ground for unrest.", "Anarchists can be unpredictable- stay sharp.", "This place might harbor dangerous individuals." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "I hope this commune fosters kindness, not just chaos.", "People here seem to care for each other, at least.", "Even in chaos, there can be hope." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"The air trembles with unseen voices.", $"This commune was woven into fate before time began.", $"A chorus of forgotten anarchy sings in the wind." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "This commune looks like a chill place to hang out.", "People here seem to do their own thing- I like that.", "No rules? Sounds like my kind of place." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "This commune shows how people can work together.", "Even without structure, there's hope for harmony.", "This place proves that good can come from chaos." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Anarchy never lasts- it'll fall apart eventually.", "This place looks like its going to explode.", "I doubt anything meaningful comes from this place." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "A chaotic commune like this is full of opportunities.", "How does one even hide here...", "There's always a way to manipulate chaos." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Anarchists...", "This commune is just asking for trouble.", "Its a bit much." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "This commune might have supplies.", "Even a chaotic place like this can help.", "I'll bet they've figured out how to make do with little." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "This commune is a fascinating social experiment.", "I wonder how they manage to function without hierarchy.", "There's much to study about this anarchist haven." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "This commune shows that people can thrive without rules.", "There's beauty in how they live freely.", "Anarchists prove that the world can change for the better." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "This commune must know how to have fun.", "I bet they throw some wild parties here.", "A place with no rules? Count me in." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "Let's see if anyone here actually earns their freedom.", "They thrive on chaos-I thrive on challenge.", "If they've built something real, I'll match it and then some." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "This commune feels like it's on the edge of collapse.", "Even freedom has its burdens.", "The chaos here feels heavy, not liberating." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;


                    case "arrivephotonexuscore":
                    case "arrivephotonexusgarrison":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This place feels too precise to be natural.", "Everything here is... perfectly in place.", "Even the walls feel like they're evaluating me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "Nothing's flawless, even this place.", "Their design is impressive-maybe too much so.", "A society like this doesn't leave room for mistakes." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Perfect, huh? I've seen better.", "All this shine, and still missing the mark.", "I don't need to compete with this-it already lost." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "Their order is effective-but soulless.", "A system this rigid can't last forever.", "They could use guidance with a touch of humanity." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "This place feels like a fortress dressed as paradise.", "Whatever's behind that symmetry, don't trust it.", "Eyes open. Everything here's too perfect." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "It's beautiful, but I don't see any warmth.", "If they care, it's buried under layers of precision.", "I wonder if anyone here feels at home." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { $"I hear the walls hum-too perfect to be silent.", $"Light bends too evenly. Something's pretending.", $"Even the air feels rehearsed." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Clean lines. No clutter. No fun.", "Yeah... not really my scene.", "Is there somewhere less... polished..." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "They've built something incredible here.", "Even if it's intimidating, there's beauty in what they've done.", "At least the place looks ordered." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "They built perfection. Probably buried the cost.", "Give it time-this kind of place always cracks.", "I don't buy the flawless act." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "If they're guarding something, it's worth finding.", "A society this polished has blind spots.", "Let's not waste the chance to learn how they work." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Perfect or not, I'm not backing down.", "If they try to push me out, I'll push back.", "This place feels like it expects me to fail." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "Not much comfort here, but I'll push through.", "No room for mistakes in a place like this.", "Stay sharp. This isn't known terrain." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "Their systems must be flawless by design.", "I'd love to study what keeps this place running.", "They've engineered something few could replicate." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "They've done what others only dream of... but at what cost?", "True perfection should include humanity.", "This place inspires me-but it also worries me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "Too clean. Too quiet.", "Perfection's dull when it forgets to be fun.", "They've got beauty, but no flavor." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "If they think they've won, they haven't met me.", "Perfection's not THAT impressive.", "Let's see how they handle real pressure." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "It's all flawless... and strangely cold.", "Perfection only reminds me how far we are from it.", "This place feels like it's hiding its loneliness." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;


                    case "arriveshadecore":
                    case "arriveshadegarrison":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This place isn't right-nothing about it feels natural.", "The darkness here seeps into everything.", "I don't think I was meant to see this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "This environment is chaotic; finding a path will be difficult.", "Every inch of this place feels hostile.", "How do I even get through..." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Even this monstrosity can't match my resilience.", "Its... honestly disgusting.", "This place might intimidate others, but not me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "Some things lie beyond rulership... this might be one of them.", "This place doesn't need a leader-it needs containment.", "Even I would not claim dominion over this nightmare." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "This is enemy territory-stay focused.", "This place bleeds hostility. Be ready for anything.", "It's like walking into a battlefield, but worse." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "There's no kindness in this place. Just pain.", "I can't tell if anything here ever lived-or just suffered.", "This isn't a wound. It's a disease." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "The abyss sings in silent dissonance, folding reality inward.", "Shadows coil and bloom-this is the echo of an unspoken void.", "I have seen this place before, in dreams that never belonged to me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "This place hums wrong. I don't like it.", "I've seen weird, but this? This is alive in a bad way.", "Don't love it. Don't want to stay." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "There must be a way through this.", "Even in the worst dark, people survive.", "There's still something worth saving out there-this doesn't change that." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "This place reeks of death and failure.", "Should one even be here?", "Why bother with a place like this?" }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Even in chaos, there are opportunities.", "It can't all be bad, just needs a little manuevering...", "Darkness like this hides secrets-I intend to find them." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "This place burns just to look at-I'll carve a path through it.", "Whatever lives here wants a fight. I'll give it one.", "I'll burn this nightmare to the ground if I have to." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "This place feels like death, but I've survived worse.", "The key here is to keep moving and stay alive.", "Even in darkness, survival is possible." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "This living environment is fascinating, though horrifying.", "I wonder what drives this biological nightmare.", "I must study how this place sustains itself." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "Even this darkness could one day give way to light.", "I can't believe the world lets this place stand.", "This must be destroyed." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "No joy, no pleasure-just misery.", "Why did I come here again...", "Let's leave this nightmare as soon as possible." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "If this place is testing me, it picked the wrong opponent.", "I'll stand taller than whatever nightmare shaped this.", "I won't be outmatched-not even by this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "This place feels like it knows nothing but despair.", "The sorrow here is suffocating.", "Why does this place exist to spread misery?" }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;



                    case "arriveisofractalcore":
                    case "arriveisofractalgarrison":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This place is overwhelming... but beautiful.", "The colors and lights here are mesmerizing.", "It's like stepping into a living work of art." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "Amid all this beauty, there must be structure.", "The creativity here could mask vulnerabilities.", "A bit flashy." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "I guess its something.", "This place is loud with color, but silent on meaning.", "Its pretty impressive, for a bunch of flashing lights." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "Such unbridled expression needs purpose, not just beauty.", "A realm like this deserves guidance, not restriction.", "They flourish-but without someone to anchor it, it may drift." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "This place feels disorganized despite its beauty.", "All this creativity could hide strategic insights.", "Stay alert- don't get lost in the lights." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "The freedom here feels welcoming and warm.", "It's wonderful to see a place so alive with creativity.", "This society values expression above all- refreshing." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "Fractals fold upon themselves-reality untangles into color.", "The prism knows my name; it whispers light into my form.", "This is the nexus where thought crystallizes into echoes of infinity." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "This is certainly somewhere. I could get used to this.", "Huh. This place's cool.", "No rush, let's just soak it all in." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "This place proves that creativity can thrive.", "A society built on freedom- it's inspiring.", "There's so much hope in a place like this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "All these colors feel a bit much.", "It's all flash-what are they really hiding under the shine?", "I wonder how long this freedom lasts before chaos sets in." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Such openness could be exploited.", "Freedom like this has its advantages- if you know how to use it.", "Let's see if their creativity includes caution." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "All these lights and colors-it's overstimulating.", "Feels like they forgot about function with all this flair.", "This place is a distraction, I need to focus." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "This place is so different, but it feels safe.", "Creativity like this could inspire survival strategies.", "Even here, survival is the ultimate expression." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "The light refractions here are fascinating.", "Such creative structures warrant deeper study.", "I must learn the science behind their designs." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "This is what the world could be- alive with creativity.", "A society where everyone can express themselves- it's beautiful.", "This place feels like a glimpse into a better future." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "Now this is my kind of place.", "All this beauty, here to be enjoyed.", "This place is all about living in the moment- I love it." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "Flashing lights, whatever.", "This place is impressive, but I can do better.", "They think this is the pinnacle of expression? Watch me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "All this beauty feels bittersweet somehow.", "A place this alive only reminds me of what's been lost.", "Even in all this light, I feel the weight of sorrow." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;
                    case "entershrine":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This place feels sacred.", "There's something special about this shrine.", "It's quiet... almost reverent." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "I wonder what purpose this shrine serves.", "Even places like this may hold strategic value.", "A shrine might have been pivotal in their culture." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "A place of worship? I suppose some need that.", "Whatever they honored here... I've outgrown it.", "Shrines are rarely built for those who truly deserve them." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "A symbol of values... perhaps once guided by order.", "This shrine speaks to the will of those who built it.", "Even without leadership, some still find purpose." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "This shrine must've meant something to someone who fought.", "Places like this last longer than any battle.", "Silent, but not forgotten-that's what this place feels like." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "I hope this shrine has brought peace to those who needed it.", "Places like this are meant for reflection and healing.", "I feel a sense of calm here- let's not disturb it." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "The shrine breathes. It hums in forgotten tongues.", "Time folds here-each stone a memory unwritten.", "This place is not still; it waits, it watches, it whispers." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Nice and peaceful here.", "A shrine, huh? Looks like a good spot to rest.", "I guess some folks find meaning in places like this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "It's good to see a place made with care.", "Faith like this helps people hold on.", "I think this shrine gave someone strength." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "If shrines fixed anything, we'd have fewer problems.", "Hope doesn't change reality.", "Another place built for comfort, not answers." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "I wonder what secrets this shrine might hold.", "Shrines often have hidden meanings- or hidden treasures.", "This place feels significant." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Alright, it's a shrine. Let's keep moving.", "This isn't really my scene.", "I don't see how this helps, but fine." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "I hear these shrines grant blessings for special offerings.", "A place like this must have helped people endure.", "Even in hardship, faith built this shrine." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "I wonder what rituals were performed here.", "The architecture of this shrine is fascinating.", "Studying this shrine might reveal its origins." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "This shrine represents people striving for something greater.", "Faith like this can bring people together.", "Yearning for progress... at least one can hope." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "Quiet places like this make me uneasy.", "It's peaceful, I'll give it that.", "Not exciting, but I can appreciate the calm." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "Not bad craftsmanship, really.", "It's a fine shrine-not that I'm impressed easily.", "I've seen bigger, but this has style." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "This place feels like it remembers something lost.", "There's comfort here, and maybe a little grief.", "It's peaceful... but not without weight." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "enterlibrary":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This place is filled with knowledge.", "So many books... where do I even start?", "I bet there's a lot to learn here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "Libraries like this hold invaluable strategies.", "So much information, this is my place.", "Information is power- let's take advantage of it." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "I'll be impressed if I find anything.", "I hope something in this library is interesting.", "Let's see if any of this is actually worth reading." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "A collection of wisdom is a mark of a great civilization.", "Libraries are the pillars of a thriving society.", "This place must have been important to its people." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "A library? Not really my scene, but it might have maps.", "Books don't win battles, but knowledge helps.", "Let's see if there's anything about tactics in here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "Books like these must have brought comfort to many.", "A library is a sanctuary of knowledge and peace.", "This place feels calm- let's respect it." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "The books are awake. They breathe knowledge into the void.", "This library does not hold words-it whispers fate.", "The ink is alive, the pages fold time. I have seen this before." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "A library, huh? Might as well take it easy here.", "Books aren't really my thing, but this place is nice.", "Let's hang out for a bit- no rush." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "This place holds so many ideas worth exploring.", "There's potential here-I just need to find it.", "Something in here might be just what I need." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Books full of useless knowledge, I bet.", "I doubt there's anything here worth the time.", "Another monument to people chasing unattainable dreams." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Books like these could hold hidden advantages.", "Information is the greatest tool- let's use it well.", "This library might have secrets waiting to be uncovered." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Not really my style, but maybe there's something useful.", "If there's anything exciting in here, I want to find it fast.", "Fine, let's check it out... but no lectures." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "I don't see these too often.", "Where do I begin.", "Knowledge is power, though I wonder what lies here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "A treasure trove of knowledge. I must explore it all.", "Books like these are invaluable to understanding the world.", "I could spend a lifetime studying this library." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "This library is proof of humanity's thirst for knowledge.", "So many ideas waiting to be rediscovered.", "Places like this show what we're capable of." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "Not really my idea of fun, but it's relaxing.", "Books aren't my thing, but the quiet is nice.", "This place could use some music to liven it up." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "If there's something rare here, I'll be the one to find it.", "Let's see what stands out in this collection.", "Even here, there's a way to come out ahead." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "So many books filled with forgotten dreams.", "A library like this feels lonely, despite the knowledge.", "All this knowledge... and yet, so much remains lost." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "entertavern":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This place seems lively.", "A good spot to rest and relax.", "The atmosphere here is welcoming." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "Taverns are good places to gather information.", "Keep your ears open- people talk freely here.", "A tavern like this can be a strategic advantage." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Let's see if anyone here is worth the time.", "These places usually have fun people.", "I wonder if anyone will recognize me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "A tavern is the heart of any settlement.", "The people's spirits rise in places like this.", "This place reflects the strength of the community." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "A good place to rest before the next mission.", "Taverns like this are great for unwinding.", "A drink or two won't hurt the morale." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "Everyone seems happy here- it's good to see.", "Taverns are where people connect and bond.", "A lively place like this lifts the spirit." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "The ale is woven from echoes of forgotten time.", "This is not a tavern-this is the first breath of a dreaming god.", "The laughter here bends the fabric of fate... I must listen." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "This is my kind of place.", "A coffee and some good company- what else do you need?", "Let's just relax and enjoy the atmosphere." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "A tavern is always full of joy and laughter.", "This place is so lively- it's wonderful to see.", "Good times are sure to be had here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Just another tavern full of false cheer.", "I wonder how long the laughter lasts after the drinks run out.", "Places like this are all the same- empty distractions." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Taverns are great for overhearing useful information.", "Let's see if anyone here can be... persuaded.", "I wonder what secrets this place holds." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "This place better not start any trouble with me.", "Taverns like this always have someone asking for a fight.", "Let's grab a drink and see who's brave enough to challenge me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "Taverns are good for gathering supplies and stories.", "The atmosphere here is rather lovely.", "Even survivalists need a warm place now and then." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "I wonder what local customs are practiced here.", "Taverns like this reflect the culture of the region.", "I could learn much about people in a place like this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "Taverns show how people come together as a community.", "This place is alive with possibility and connection.", "A tavern like this reflects the best parts of society." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "Now this is my kind of place- food, drinks, and fun.", "Taverns like this are made for enjoying life to the fullest.", "I'm going to have a great time here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "Some of these have games.", "Nothing like a good bar fight.", "Let's leave a mark-one way or another." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "Taverns are always so full of laughter... but it feels hollow.", "I wonder how many people here are hiding their sorrow.", "Even in places like this, the weight of life remains." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "enterforge":
                        switch (personality)
                        {
                            case "generic": response = new[] { "The heat in here is intense.", "You can almost feel the work that's been done here.", "This place hums with craft and purpose." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "A forge supports every effort, not just war.", "The right tools here could change my plans.", "Craftsmanship is just another form of preparation." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "I'll be surprised if anything here meets my expectations.", "Let's hope their work is as refined as I am.", "At least they tried to build something lasting." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "This forge is part of what holds a society together.", "Without places like this, no society endures.", "Labor here serves more than war-it builds legacy." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Forged gear is only as good as the hands that use it.", "Let's see if they make tools worth carrying into battle.", "I've seen plenty of forges-let's hope this one delivers." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "You can tell a lot about people by what they craft.", "This forge feels honest-built for creating, not destroying.", "I hope what's made here helps someone live, not just fight." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "The forge remembers. The flame whispers names.", "Each ember here carries a purpose not yet spoken.", "The tools dream of their future before they're shaped." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Looks like a lot of effort goes on in here.", "As long as someone else handles the heavy lifting, I'm good.", "I'll just watch the sparks fly-no need to break a sweat." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "This place is full of creative potential.", "What's built here could change lives.", "Every tool made here starts with hope." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "All this work, and half of it ends up in ruins.", "People shape tools-then use them to break things.", "Smells like desperation and rusted ambition." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "With the right leverage, a forge can shift power.", "What's made here could be used for more than it seems.", "Let's see who's making what, and why." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "All this heat-feels like home.", "As long as something gets made, I'm in.", "Let's see what's cooking besides metal." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "A forge means tools, and tools mean staying alive.", "This is the kind of place that keeps people going.", "Lets see what they can make." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "This is where materials meet method.", "I'd love to observe their forging techniques.", "Every hammer strike here shapes more than metal." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "There's beauty in turning raw matter into something purposeful.", "This forge is proof of what hands and hearts can create.", "Craft like this is what builds the future." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "Hot, loud, and smells like work... not really my vibe.", "I'll take the finished product, not the process.", "I respect the effort-just don't make me touch an anvil." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "Impressive work, honestly.", "Its hot in here.", "What to create here..." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "A forge is where dreams take shape... or burn out.", "You can feel the weight of effort in every corner.", "It's a place for making things, but it still feels lonely." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;



                    case "entermarket":
                        switch (personality)
                        {
                            case "generic": response = new[] { "The market's full of activity.", "So much to see and buy here.", "A bustling place for trade and commerce." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "Markets like this are hubs for information and resources.", "Let's see if theres aaything helpful for sale.", "Commerce fuels strategy- let's make the most of it." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Hopefully this market offers something that doesn't waste my time.", "Maybe some of it could be useful.", "Let's see if anything here deserves a second glance." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "A thriving market is the sign of a prosperous society.", "This place reflects the strength of its people.", "Commerce like this keeps a civilization alive." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Markets like this are good for resupplying.", "Could find something useful for the road.", "Let's stock up and move on quickly." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "Markets bring people together- it's heartwarming to see.", "So many things here could help on the journey.", "Let's see if there's anything I need." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "The stars guided me to this place of trade.", "I wonder if the merchants here follow the cosmic balance.", "The market hums with the energy of countless deals." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Markets are always fun to stroll through.", "Take it easy and see what catches the eye.", "No rush- this place has plenty to explore." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "Markets like this are full of opportunity.", "There's so much to discover and enjoy here.", "Let's see what treasures lie here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Another market full of overpriced junk.", "Is anything here worth the time?", "Commerce always feels so hollow to me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Markets are perfect for making deals and gathering information.", "Might have a bargain or two.", "This place is full of opportunities for the clever." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Markets are always so noisy.", "I don't have the patience for haggling.", "Let's grab what I need and get out of here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "Markets are great for restocking essentials.", "Need to find supplies to keep going.", "This place might have just what I need." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "I wonder what unique goods or knowledge this market holds.", "Markets like this reflect the culture of the area.", "So much to observe and study here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "Markets show the power of collaboration and trade.", "This place is a testament to human ingenuity.", "A thriving market is a symbol of progress." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "Theres a lot to choose from.", "Theres some nifty stuff here.", "Might be something nice here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "Let's see if this market has anything that matches my standards.", "Economics is quite the intricate game.", "This place is bustling." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "Markets, always so fleeting and transient.", "All these goods, yet they don't ease the weight of life.", "This place feels more like a distraction than a solution." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "enteroutpost":
                    case "enterfort":
                    case "enterbastion":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This place feels dangerous.", "It's clear someone doesn't want visitors here.", "A fortress like this spells trouble." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "Structures like this are perfect for ambushes.", "Scout the area and plan accordingly.", "This fort is likely crawling with enemies- stay alert." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Whoever controls this place will regret it when I'm done.", "A fortress like this couldn't stop someone like me.", "They should have stayed in exile." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "This place reeks of rebellion and disorder.", "An outlaw fortress- such defiance must be crushed.", "No ruler should tolerate the presence of such chaos." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "This is enemy territory- tread carefully.", "Structures like this are often heavily fortified.", "Let's be ready for a fight- this isn't friendly ground." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "This place feels like it's seen too much violence.", "I hope no one's been forced to call this place home.", "Be careful- this fortress doesn't feel safe." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "The stone is old... but the shape isn't.", "It breathes between the cracks, quietly watching.", "I walked in, and the corridors rearranged themselves behind me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "This place looks like more trouble than it's worth.", "I don't think I'm welcome here.", "Let's keep it quick and avoid any unnecessary fights." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "Even a place like this could hold something good.", "Let's hope this fortress isn't as bad as it looks.", "Maybe I'll find something useful here after all." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "A place like this only exists to make life harder.", "I doubt anything good ever comes out of fortresses like this.", "Places like this don't get built without someone expecting trouble." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "A fortress like this has to have vulnerabilities.", "Let's see how this place can help.", "Outlaws or not, they won't outsmart." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "If someone's itching for a fight, they'll get one.", "Looks like a good place for a fight.", "Outlaws. Ugh." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "I'll need to be resourceful to get through this.", "Fortresses like this are dangerous but survivable.", "Find what is needed and get out." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "I wonder what kind of technology or strategies they use here.", "Fortresses like this often reveal fascinating details about their builders.", "This place could be a treasure trove of knowledge." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "A fortress like this shows how far some will go for power.", "We should strive to build a better world without places like this.", "This place is a stark reminder of why change is necessary." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "Not my usual kind of scene, but who knows-maybe there's something interesting tucked away.", "If I find something shiny or forbidden, it might be worth the gloom.", "This place could surprise me... or bore me to death." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "Let's show who's really in control.", "This fortress won't stand a chance against me.", "Let's see if these outlaws can handle a real challenge." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "Places like this are filled with sadness and loss.", "This fortress feels like a monument to pain.", "I can't help but wonder how many lives were ruined here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "enterfortress":
                    case "entertower":
                    case "enterkeep":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This place gives me the chills.", "Stay focused.", "Something about this keep doesn't feel right." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "A building like this could hide many dangers.", "Be prepared for whatever lies ahead.", "Scout the area carefully before moving further." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "How bad can it be?", "Alright, whos hiding here...", "Let's make this quick." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "This keep feels like a den of rebellion.", "Order must be restored here.", "A structure like this belongs under proper rule." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Stay vigilant- keeps like this are built to defend.", "Walking into potential danger- keep sharp.", "This could be a fortified stronghold- proceed with caution." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "I don't like the feeling of this place.", "Let's be careful- this place could be rather dangerous.", "A place like this could hide all sorts of dangers." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "The limitless sky sees this place.", "I feel lost in this limitless sky.", "I sense dark whispers coming from within." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "This place looks foreboding.", "Let's just get in and out without any trouble.", "I'm not worried- it's just another creepy keep." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "It'l be fine.", "Every challenge makes me stronger.", "A way through this structure will emerge." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Keeps like this are always bad news.", "I'd bet anything this place is trouble.", "This keep is out to get me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Let's see what secrets this building is hiding.", "There's always a way through.", "Keeps like this often have vulnerabilities- keep an eye out." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Whatever's inside can't be that bad.", "This structure doesn't intimidate me.", "I'm ready for whatever's waiting inside." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "Places like this test your will to survive.", "Let's stay cautious and get through this.", "A structure like this is dangerous, but I'll endure." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "I wonder what stories this place holds.", "The design here could reveal something interesting.", "Odd choice of design." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "This building reminds me why I fight.", "I dream this place will one day be great again.", "Structures like this can symbolize either power or oppression." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "This place is too dark for my liking.", "I'd rather be anywhere else right now.", "Let's hurry up- I'm not staying here long." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "What is happening here...", "I'm ready to prove myself here.", "This keep is just another challenge to overcome." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "This building feels heavy with sorrow.", "I can't help but think of the lives lost here.", "Places like this are reminders of darker times." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "entermonument":
                    case "enterstronghold":
                    case "entersanctum":
                        switch (personality)
                        {
                            case "generic": response = new[] { "This place is drenched in danger- stay sharp.", "Be ready for anything.", "The air here feels heavy..." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "Proceed with extreme caution.", "Places like this demand careful strategy- don't rush.", "Even I don't know what to do here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Let's see if this place can live up to its ominous reputation.", "I'm not afraid of whatever lurks here.", "This monument of terror won't break someone like me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "This place is a stronghold of chaos- it must be subdued.", "Such oppressive structures are an affront to order.", "This sanctum reeks of defiance against true rule." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Stay focused. We don't know whats ahead.", "This is hostile territory- be ready for combat.", "Its... dark." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "I can't shake the feeling that this place is alive... and hostile.", "Don't get lost. This place is a mess.", "This place is suffocating- be careful." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "The archaic balance is not as is. The world is spinning.", "This structure defies the balance of the cosmos.", "The limitless sky is limited- this place is deeply wrong." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Yeah... this place doesn't feel friendly.", "I'd rather not stick around here too long.", "Let's just get through this in one piece." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "Stay focused. There must be a way through.", "Even in a place like this, there's hope.", "Keep your spirit up." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "This place is designed to kill me- great.", "I'm sure nothing good will come of this.", "Places like this always end in disaster." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "What fun will this place hold...", "This place is dangerous, but that means it has value.", "Can't be too much to handle." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Whatever's in here better be ready for a fight.", "What will this one be...", "I'll deal with it personally." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "Seems like a death trap- keep your wits about you.", "Nothing can't be overcome.", "Danger or not, I'll make it through this." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "This place feels like it holds dark secrets.", "I wonder what history or purpose shaped this structure.", "Even in danger, there's knowledge to be gained here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "No place for this in my world.", "This monument is a symbol of oppression- let's overcome it.", "Even in the darkest places, there is hope." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "This place is way too grim for my liking.", "Let's get out of here before it ruins my mood.", "I'd rather be anywhere but here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "This place thinks it can intimidate? Well... maybe it can.", "No challenge here cannot be overcame.", "No structure is invincible." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "This place feels like a graveyard of hope.", "It's hard not to feel the weight of despair here.", "Places like this are why the world feels so broken." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "useshadow":
                        switch (personality)
                        {
                            case "generic": response = new[] { "The shadows bend to my will.", "Darkness moves in ways unseen.", "I become one with the shadows." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "The shadows create the perfect cover.", "Hidden paths emerge through the dark.", "Darkness provides the advantage." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Even shadows obey me.", "My mastery extends to the unseen.", "None wield the dark with as much finesse" }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "Even the shadows understand authority.", "The shadows serve my reign.", "Even the night bends to authority." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Shadows hide us and strike with precision.", "The dark is a battlefield of its own.", "Moving unseen through the cover of night." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "Shadows protect from prying eyes.", "The dark offers safety in troubled times.", "I shield myself within the quiet of night." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "The whispers of the shadows guide me.", "I am cloaked in the will of the unseen.", "The dark embraces me as its own." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Shadows are useful for staying out of trouble.", "I'll just blend in and take it easy.", "The dark has its perks." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "The shadows are allies if you trust them.", "Even the dark has its purpose.", "This feels like a step in the right direction." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Nothing good comes from the shadows.", "This feels like trouble waiting to happen.", "The dark hides too many secrets." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Shadows are the perfect tools for deception.", "I'll use the dark to outwit them.", "The unseen holds endless potential." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Let the shadows come- I'm ready for anything.", "Darkness doesn't scare me.", "Behold the power of the unseen." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "The shadows keep me safe if used properly.", "Darkness can be an ally in the wild.", "Move carefully- the dark can hide danger." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "Fascinating how shadows shift with intention.", "The manipulation of darkness warrants further study.", "There's much to learn from the properties of the unseen." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "Even the shadows can serve a higher purpose.", "Darkness complements the light in perfect balance.", "This is a step toward understanding the unseen." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "The dark has its own beauty.", "This feels like indulging in a hidden pleasure.", "Shadows make everything more mysterious." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "The shadows can be mastered.", "No one can rival my control of the unseen.", "The dark is my edge over the competition." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "The shadows remind me of things long gone.", "There's something sorrowful about the unseen.", "The dark feels heavy with forgotten memories." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "uselife":
                        switch (personality)
                        {
                            case "generic": response = new[] { "The energy flows into the living.", "Life bends to my will.", "Power surges through creation." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "This energy enhances my strength.", "Life itself can be a strategic tool.", "The manipulation of creatures is a calculated move." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Even life yields to my command.", "I control the very essence of creation.", "You stand in the presence of life made will." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "Life flourishes under proper rule.", "Order brings vitality to all things.", "The energy of life serves its rightful ruler." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Life energy strengthens us.", "Harnessing life ensures survival.", "Vitality is the key to enduring battle." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "Life energy heals and protects.", "This power is meant to nurture and sustain.", "The essence of life must be cherished." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "The whispers of life guide my actions.", "The enhancement the living bends fragments of soul.", "Life energy hums with cosmic intent." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Life energy is soothing- it feels nice.", "This is simple, natural power.", "Let's not overthink it- just go with the flow." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "Life energy brings hope to everything it touches.", "This is a gift of vitality and renewal.", "The power of life always brings something positive." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Life energy won't fix everything.", "This feels like a temporary solution.", "Nothing lasts forever, not even life." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Life energy can be a powerful tool in the right hands.", "This power is really something.", "The manipulation of life is as much strategy as power." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "This energy feels unstoppable- it's incredible.", "Life energy is raw power- I can feel it.", "Let's see what this energy can do in action." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "Life energy keeps me going when nothing else will.", "This is the essence of survival.", "Harnessing life is the key to enduring any challenge." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "The properties of life energy are fascinating.", "This is a perfect example of bio-energetic manipulation.", "I must document the effects of this energy." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "Life energy represents growth and progress.", "This is a step toward a better future.", "The essence of life must be used for the greater good." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "This energy feels amazing- it's so invigorating.", "Life energy is pure indulgence.", "This is the kind of power I could get used to." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "With life energy, I can't be stopped.", "This power gives me the edge over anyone.", "Life energy fuels my drive to win." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "Life energy feels fleeting, like all things.", "Even the power of life can't erase sorrow.", "There's a sadness in the beauty of life." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "usedeath":
                        switch (personality)
                        {
                            case "generic": response = new[] { "Death answers my call.", "The energy of the end bends to me.", "I wield the power that ends all things." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "Even death can be a tool in the right hands.", "This power disrupts and dismantles with precision.", "Death itself becomes a calculated advantage." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Even death bows to my superiority.", "I hold the reins to life's inevitable end.", "Even death obeys with my whisper." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "Death enforces the law of all things.", "Even in death, there is order.", "I command the final justice of existence." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Death is a weapon like any other.", "This power turns the tide in my favor.", "Even the reaper fights for the mission." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "This power is a grim necessity.", "I take no pleasure in wielding death.", "May this energy protect those I care for." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "The shadows of death whisper their secrets to me.", "The cosmos can grant what I cannot.", "I walk the line between life and the end." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Death is just another part of the cycle.", "I'll use this power and keep it simple.", "No need to overthink it- it just works." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "Even in death, there is purpose.", "This power can bring about renewal.", "Death is but a step toward something greater." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Death is the only certainty.", "This power only serves to remind of the end.", "Everything ends, not anymore." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Death is a tool for the clever.", "With this power, the end becomes an opportunity.", "Let's see how this energy can shift the balance." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Let's see them face the power of death.", "This energy is raw destruction.", "Behold the true meaning of fear." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "Death is a part of survival's harsh truth.", "Death ensures life.", "Even death can be turned into an advantage." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "The properties of death energy are fascinating.", "I must study the effects of this power closely.", "This manipulation has implications far beyond life itself." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "Even death can pave the way for progress.", "This power displays balance.", "I wield this energy to create a better future." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "This power feels... intoxicating.", "Even death has its allure.", "There's a dark thrill in wielding this energy." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "I'll use even death to come out on top.", "This power gives me an undeniable edge.", "Nothing can stand in my way- not even the end." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "This energy feels like the weight of sorrow.", "Even in power, this reminds me of loss.", "The end is always present- this just brings it closer." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "usestars":
                        switch (personality)
                        {
                            case "generic": response = new[] { "The stars align to grant me power.", "Starlight bends to my will.", "The energy of the cosmos flows through me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "The stars provide strategic clarity.", "This cosmic energy enhances my plans.", "I'll use the stars to guide the next move." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "The stars knew I'd arrive eventually.", "Not everyone can handle power like this.", "Looks like the cosmos made the right choice." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "The stars themselves recognize true authority.", "This is the power of the heavens, wielded by a ruler.", "The cosmos serves its rightful sovereign." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Starlight illuminates the battlefield.", "This energy strengthens my resolve.", "The stars lead towards victory." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "The stars breathe through me.", "This energy feels nurturing and warm.", "I'll use this power to keep everyone safe." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "The stars whisper their secrets to me.", "This energy was foretold by the cosmos.", "I am one with the heavens." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "The stars just do their thing, and I go with it.", "This cosmic energy feels familiar enough.", "It's nice to have the universe on my side." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "The stars shine hope into everything.", "This energy reminds me that I'm never alone.", "The cosmos always provides a light in the dark." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "The stars might be shining, but I doubt they care.", "Cosmic energy feels distant and indifferent.", "Even the heavens don't change much down here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "The stars are a powerful tool if used wisely.", "Cosmic energy has potential for clever strategies.", "Let's see how the heavens bend to my will." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Let the stars bring the heat.", "This energy feels unstoppable.", "The cosmos is a force, and I'm ready to use it." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "The stars guide through the night.", "Cosmic energy ensures the way is found.", "Even in the wilderness, the stars are constant." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "The mechanics of starlight are fascinating.", "I wonder what properties this energy holds.", "Cosmic manipulation is a marvel of physics." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "The stars remind of endless possibilities.", "This energy feels like a step toward a brighter future.", "Starlight symbolizes progress and hope." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "The stars make everything feel so grand.", "This energy is like indulging in cosmic luxury.", "I could get used to wielding the power of the cosmos." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "To wield cosmic power...", "The stars are just another tool to win.", "This merely adds to my radiance." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "The stars feel distant, like a dream out of reach.", "Even the heavens can't dispel this weight.", "Starlight is beautiful, but it feels so far away." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "useheat":
                        switch (personality)
                        {
                            case "generic": response = new[] { "The heat obeys my command.", "Energy burns brighter within me.", "Feel the raw power of the flames." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "Heat is a weapon and a shield.", "Controlled fire can turn the tide of battle.", "Let's see how they handle this intensity." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Only I can wield heat with such precision.", "I am the master of flame and fury.", "I think I'm a bit hot for this room." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "The heat radiates the power of my rule.", "Even the flames recognize authority.", "Let this fire cleanse and assert order." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "This heat will drive back the enemy.", "The flames forge strength in battle.", "Controlled fire supports the mission." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "The warmth of this energy protects me.", "Fire can be destructive, but also nurturing.", "This heat will shield those I care for." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "The flames speak their secrets to me.", "The limitless suns ignite my path with fire.", "The core of the universe refracts through me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "This heat feels nice... and useful.", "Fire's handy for getting things done.", "Let's not burn out, though." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "The warmth of fire brings hope.", "This energy fuels progress.", "Even the smallest spark can ignite change." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Fire consumes everything in its path.", "Let's hope this doesn't backfire- literally.", "Heat is just another force that doesn't care." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "The flames are tools for clever tactics.", "Fire can illuminate opportunities.", "Let's see how they handle the heat." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Feel the burn.", "This fire is unstoppable- just like me.", "I'll turn up the heat and watch them squirm." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "Fire sustains the soul.", "This heat will keep me going.", "Controlled energy like this is essential." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "The properties of heat are fascinating.", "Thermodynamics at work- marvelous.", "I must analyze the behavior of this energy." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "Fire symbolizes transformation and progress.", "This energy will light the way forward.", "Heat brings renewal through destruction." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "This heat feels incredible- pure energy.", "Fire like this is a thrill to wield.", "Nothing beats the rush of controlling flames." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "Let's see who can handle the heat.", "This fire gives me the edge I need.", "No one can outmatch my control over flames." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "The heat feels consuming, almost overwhelming.", "Fire always reminds me of what it destroys.", "Even flames carry a sense of sorrow." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "usebody":
                        switch (personality)
                        {
                            case "generic": response = new[] { "My body becomes a weapon of pure energy.", "I channel energy into my strikes.", "This power flows through me, enhancing my every move." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "Energy precision strengthens every strike.", "Channeling this power makes every move count.", "Even bodily energy can be strategically devastating." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Even my body radiates unmatched power.", "Witness the strength of my energy-infused strikes.", "This energy... can be a bit much." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "This energy exemplifies the strength of a ruler.", "Even my strikes uphold the law of power.", "My body channels the authority of the realm." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Energy enhances every attack with precision.", "My body becomes a weapon for the mission.", "This power ensures I remain battle-ready." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "This power helps me protect those I care for.", "Make every move with someone in mind.", "Energy flows into every move, ensuring safety." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "The energy within guides my every action.", "Force of will channels its strength into me.", "Flow as all things flow." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "This energy makes everything feel easier.", "Let's keep it simple- just channel and strike.", "No need to overthink it, just let it flow." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "This power feels like a gift of hope.", "Energy like this brings out the best in us.", "With this strength, anything feels possible." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Energy doesn't change the fact that life's hard.", "This power will fade eventually, like everything else.", "It's useful for now, but I don't trust it to last." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Energy like this opens up new possibilities.", "Let's use this power to outsmart them.", "Every move becomes a calculated advantage." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Feel the power in every strike.", "I'm unstoppable with this energy.", "Let's see them handle this raw strength." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "This energy keeps me fighting.", "Every strike ensures survival.", "This power is essential for staying alive." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "The dynamics of bodily energy are fascinating.", "This is an extraordinary application of internal power.", "This amplification warrants further analysis." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "This energy shows how we can push past limits.", "Strength like this can build a better future.", "Every strike feels like progress in motion." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "This energy feels incredible- I love it.", "Every strike is a thrill.", "The power can be purely felt..." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "With this energy, I'll crush the competition.", "No one can match my strength now.", "Every strike puts me closer to victory." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "Even with this power, it feels hollow.", "This energy only reminds me of what I lack.", "Every strike feels heavy with sorrow." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "usereality":
                        switch (personality)
                        {
                            case "generic": response = new[] { "Reality bends at my will.", "I reshape the world as I see fit.", "This power warps everything around me." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "Changing reality grants a significant edge.", "I'll use this to disrupt their plans.", "The battlefield is mine to reshape." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Reality itself yields to my superiority.", "I bend existence to my desires.", "Only I can command such power over the world." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "Order is restored as I rewrite reality.", "The world must bow to the rule of my will.", "This power enforces the balance of my reign." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "Reality shifts to my will.", "This power turns the tide of battle.", "Even the world itself fights for the mission." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "I'll reshape reality to protect those I love.", "This power helps keep everyone safe.", "The world bends, but my care remains constant." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "Self guides my hand as I rewrite existence.", "Reality whispers secrets only I can hear.", "I align the cosmos to bend the world." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "Reality changes, but I'll stay the same.", "This is pretty handy, honestly.", "Let's not overdo it- just a little tweak here." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "This power shows what's possible in the world.", "I'll reshape reality into something better.", "This energy feels like hope made tangible." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Changing reality doesn't change its flaws.", "The world bends, but it's still imperfect.", "This is just another tool to survive in a broken world." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Reality is a tool for those who know how to use it.", "I'll reshape the world to fit my strategy.", "Let's see how they handle this new reality." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "I'll tear reality apart if I have to.", "This power feels unstoppable.", "Let's see them handle a world I've changed." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "The world bends to ensure I survive.", "Reality shifts, but survival remains constant.", "This power is just another tool to stay alive." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "Reality's laws are fascinating to manipulate.", "This power defies conventional understanding.", "I must study the effects of this energy closely." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "This power lets me reshape the world into a better one.", "Reality bends, but my vision for progress remains.", "I'll use this to create something greater for everyone." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "Changing reality feels exhilarating.", "This power is pure indulgence.", "Reality bends to make life more thrilling." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "Even reality can't hold me back.", "I'll reshape the world to ensure my victory.", "This power puts me leagues ahead of the rest." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "Even with this power, the world feels heavy.", "Changing reality doesn't fix the weight it carries.", "This energy is amazing, but it feels so fleeting." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;

                    case "uselight":
                        switch (personality)
                        {
                            case "generic": response = new[] { "Light bends to my will.", "I channel brilliance into power.", "The energy of light illuminates my path." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "tactician": response = new[] { "Light creates opportunities in the darkness.", "This energy reveals new paths.", "The brilliance of light becomes a strategic advantage." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "arrogant": response = new[] { "Only I can wield light with such mastery.", "Even light itself recognizes my superiority.", "Witness the brilliance of my unparalleled power." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "sovereign": response = new[] { "This light symbolizes the clarity of my rule.", "The brilliance of order shines through this power.", "Even the radiance of light serves my command." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "soldier": response = new[] { "This light guides through battle.", "The brilliance of light strengthens resolve.", "Even the darkest paths are illuminated by this power." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "caretaker": response = new[] { "This light will guide me.", "The energy of light feels warm and nurturing.", "I'll use this brilliance to safeguard those I care for." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "delusional": response = new[] { "The light hums with ancient whispers.", "This brilliance feels like the will of the universe.", "Energy itself fuels this radiant energy." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "laid-back": response = new[] { "This light is pretty handy.", "I'll just let it flow and see what happens.", "It's nice to have a little brightness in all this chaos." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "optimist": response = new[] { "This light reminds me there's always hope.", "The brilliance of this energy feels uplifting.", "Even the darkest places can be brightened by this power." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cynic": response = new[] { "Light's just another energy- don't read into it.", "This brilliance fades like everything else.", "Even light doesn't fix the darkness for long." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "cunning": response = new[] { "Light can be a weapon for the clever.", "This brilliance creates opportunities to exploit.", "Let's see how this radiance can shift the odds." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hothead": response = new[] { "Feel the burn of this light.", "This brilliance is unstoppable.", "I'll shine brighter than anyone." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "survivalist": response = new[] { "This light guides to safety.", "Brilliance like this ensures survival.", "Even the darkest nights can be navigated with this energy." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "analytic": response = new[] { "The properties of light are endlessly fascinating.", "I must study how this brilliance affects its surroundings.", "This energy could revolutionize understanding of optics." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "idealist": response = new[] { "This brilliance feels like the start of something greater.", "Light represents progress and hope.", "Illuminating a path forward." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "hedonist": response = new[] { "This brilliance feels incredible to wield.", "Light like this is pure indulgence.", "The radiance of this energy is thrilling." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "competitor": response = new[] { "This light outshines all.", "This brilliance gives me the upper hand.", "The intensity of this light is unmatched." }[Game1.GameWorld.rnd.Next(3)]; break;
                            case "melancholic": response = new[] { "Even this brilliance feels dim in the grand scheme.", "The light is beautiful, but it doesn't fill the void.", "This radiance feels fleeting, like everything else." }[Game1.GameWorld.rnd.Next(3)]; break;
                            default: response = "No response available."; break;
                        }
                        break;


                    default: response = "Message type not recognized."; break;
                }


                if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                {
                    AnnounceToParty(this.Name.Split().First() + ": " + response, new Color(0, 255, 125), new EntityList<Entity>());
                }
            }
        }




        public void Perform(string type)
        {
            if (type != "song" && type != "poem")
            {
                throw new ArgumentException("Invalid type. Must be either 'song' or 'poem'.");
            }

            // Search for all compositions in the game world with the specified type
            EntityList<Composition> compositionsToPerform = new EntityList<Composition>(Game1.GameWorld.EntityLedger.Values
            .OfType<Composition>()
            .Where(c => c.Type == type)
            .ToList());


            if (compositionsToPerform.Count == 0)
            {
                AnnounceToParty($"{this.Name} finishes improvising a {type}. It sounds alright.", Color.Pink, new EntityList<Entity>() { this });
                return;
            }

            // Pick a random composition from the list
            Composition compositionToPerform = compositionsToPerform[Game1.GameWorld.rnd.Next(compositionsToPerform.Count)];

            // Announce the performance to the party
            string action = type == "song" ? "singing" : "reciting";
            AnnounceToParty($"{this.ReferredToNames[0]} finishes their {action} of {compositionToPerform.Name}. {compositionToPerform.GetCompleteWorkDescription()}", Color.Green, new EntityList<Entity>() { this });

            // Determine the list of architects based on the location of the Executor
            var architects = this.Room == null ? this.Block.Architects : this.Room.Architects;

            // Randomly select a subset of architects to react, between 1 and 6
            int numReactions = Math.Min(Game1.GameWorld.rnd.Next(1, 7), architects.Count());
            EntityList<Architect> reactingArchitects = architects.ShuffleNew().Take(numReactions);

            // React to performance in the vicinity
            foreach (var architect in reactingArchitects)
            {
                if (architect != this && !Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(architect) && Game1.GameWorld.HumanoidRaces.Contains(architect.Race))
                {
                    int randomModifier = Game1.GameWorld.rnd.Next(-2, 3);  // Random number from -2 to 2
                    int score = this.Charisma + randomModifier;
                    score = Math.Clamp(score, 0, 9); // Ensure score is within 0-9

                    // Determine the reaction based on the score
                    string reaction;
                    switch (score)
                    {
                        case 0:
                            reaction = "Go find a merchant, peddler.";
                            break;
                        case 1:
                            reaction = "I've heard better from my shiba.";
                            break;
                        case 2:
                            reaction = "Keep practicing... far away from here.";
                            break;
                        case 3:
                            reaction = "Hmm, I guess everyone starts somewhere.";
                            break;
                        case 4:
                            reaction = "Not the worst I've endured.";
                            break;
                        case 5:
                            reaction = "Average, but you could improve.";
                            break;
                        case 6:
                            reaction = "Quite decent, I must say!";
                            break;
                        case 7:
                            reaction = "That was actually quite engaging!";
                            break;
                        case 8:
                            reaction = "Impressive performance, truly!";
                            break;
                        case 9:
                            reaction = "Astonishing! You've truly mastered your craft!";
                            break;
                        default:
                            reaction = "How indescribable... I'm unsure how to put my amazement into words.";
                            break;
                    }

                    // Display the reaction
                    AnnounceToParty(architect.ReferredToNames[0] + ": " + reaction, Color.Pink, new EntityList<Entity>() { architect });
                }
            }

            foreach(Architect a in reactingArchitects)
            {
                if(Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a))
                {
                    AnnounceToParty(a.Name + " has learned the " + compositionToPerform.Type + " " + compositionToPerform.Name + ".", Color.LimeGreen, new EntityList<Entity>() { compositionToPerform });
                }
            }
        }

        public void SetProficiencyLevel(string proficiencyName, int level)
        {
            // Calculate XP based on level
            int xp = 0;
            int currentThreshold = 20; // Start threshold
            for (int i = 0; i < level; i++)
            {
                double multiplier = (i + 1) % 3 == 0 ? 2.5 : 2.0; // Determine multiplier
                xp += currentThreshold;
                currentThreshold = (int)(currentThreshold * multiplier);
            }

            // Check if the proficiency already exists
            int index = Proficiencies.FindIndex(p => p.Item1.Equals(proficiencyName, StringComparison.OrdinalIgnoreCase));
            if (index != -1)
            {
                // Update existing proficiency
                Proficiencies[index] = (proficiencyName, xp);
            }
            else
            {
                // Add new proficiency
                Proficiencies.Add((proficiencyName, xp));
            }
        }

        public static HashSet<string> warriorExcludedTypes = new HashSet<string>
{
    "priest", "warlock", "mage", "sorcerer", "duelist", "elemental",
    "magician", "shadeheart"
};

        public void KitOutArchitect(string Type)
        {
            //ACTING location
            Location Location = this.Location != null && Game1.GameWorld.SettlementTypes.Contains(this.Location.Type)
       ? this.Location
       : Game1.GameWorld.AllLocations
           .Where(location => Game1.GameWorld.SettlementTypes.Contains(location.Type) && location.HomeCivilization != null)
           .OrderBy(_ => Game1.GameWorld.rnd.Next())
           .FirstOrDefault();

            if (Location == null)
                return;

            if (KitsAcquired.Contains(Type))
                return;

            KitsAcquired.Add(Type);

            string baseType = Type;
            int powerLevel = -1;

            // Check if the end of the string is a number
            Match match = Regex.Match(Type, @"^(.*?)(\d+)$");
            if (match.Success)
            {
                baseType = match.Groups[1].Value.ToLower();
                powerLevel = int.Parse(match.Groups[2].Value);
            }
            else
            {
                baseType = Type.ToLower(); // Normalize case
            }

            bool excludedFromWarriorKit =
    warriorExcludedTypes.Contains(baseType) ||
    baseType.EndsWith("mancer");

            //warriorkit

            if (powerLevel > -1 && !excludedFromWarriorKit)
            {
                int baseChance = 30;
                int chancePerPowerLevel = (100 - baseChance) / 10;
                int chanceToGetArmor = baseChance + (powerLevel * chancePerPowerLevel);

                // Create a weapon
                Material weaponMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Math.Min(16, powerLevel * 2));

                // Pick a random pair
                var selectedPair = weaponPairs[Game1.GameWorld.rnd.Next(weaponPairs.Count)];

                // Main hand weapon
                if (selectedPair.MainHand != "")
                { 
                    Object mainWeapon = new Object(null, selectedPair.MainHand, new EntityList<Material>() { weaponMaterial }, null);
                    MainHeldObject = mainWeapon;
                }

                // Offhand item
                if(selectedPair.OffHand != "")
                {
                    Object offWeapon = new Object(null, selectedPair.OffHand, new EntityList<Material>() { weaponMaterial }, null);
                    OffHeldObject = offWeapon;
                }


                // Armor generation
                List<string> armorTypes = new List<string> { "helmet", "chestplate", "gauntlet", "leggings", "boot" };
                Material armorMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Math.Min(16, powerLevel * 2));

                foreach (string armorType in armorTypes)
                {
                    int pieces = (armorType == "gauntlet" || armorType == "boot") ? 2 : 1;

                    if (Game1.GameWorld.rnd.Next(100) < chanceToGetArmor)
                    {
                        for (int i = 0; i < pieces; i++)
                        {
                            string side = (i == 0) ? "left " : "right ";
                            string fullArmorType = (pieces == 2) ? side + armorType : armorType;

                            Object armor = new Object(null, fullArmorType, new EntityList<Material>() { armorMaterial }, null);
                            Clothing.Add(armor);
                        }
                    }
                }
            }

            void EquipOrReplaceArmor(string armorType, Object armor)
            {
                // Remove any existing armor of the same type
                for (int i = Clothing.Count - 1; i >= 0; i--)
                {
                    if (Clothing[i].Type.ToLower() == armorType.ToLower())
                    {
                        Clothing.RemoveAt(i);
                    }
                }
                Clothing.Add(armor);
            }

            if (Type == "priest")
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

                Object o = null;
                while(o == null || o.IsWeapon == true)
                {
                    o = Game1.GameWorld.MagicalSuperLoot(2);
                }

                if (m != null)
                {
                    o.Materials.Clear();
                    o.Materials.Add(m);
                }

                Inventory.Add(o);
            }
            else if (Type == "adventurer")
            {
                // Weapon added to inventory
                Material weaponMaterial = FavoriteMetal;
                string weaponType = Game1.AllWeapons[Game1.GameWorld.rnd.Next(Game1.AllWeapons.Count())];
                Object weapon = new Object(null, weaponType, new EntityList<Material>() { weaponMaterial }, null);
                Inventory.Add(weapon);

                List<string> armorTypes = new List<string> { "helmet", "chestplate", "left gauntlet", "right gauntlet", "leggings", "left boot", "right boot" };
                foreach (string armorType in armorTypes)
                {
                    if (Game1.GameWorld.rnd.Next(100) < 75)
                    {
                        Material armorMaterial = FavoriteMetal;
                        Object armor = new Object(null, armorType, new EntityList<Material>() { armorMaterial }, null);
                        EquipOrReplaceArmor(armorType, armor);
                    }
                }

                // Supplies...
                if (Game1.GameWorld.rnd.Next(2) == 0)
                {
                    Material cupMaterial = FavoriteStone;
                    Material drinkMaterial = Game1.GameWorld.rnd.Next(2) == 0 ? Game1.GameWorld.Coffee : Game1.GameWorld.Tea;
                    Object cup = new Object(null, "small cup", new EntityList<Material>() { cupMaterial }, null);
                    cup.ContainedObjects.Add(new Object(null, "drink", new EntityList<Material>() { drinkMaterial }, null));
                    Inventory.Add(cup);
                }

                if (Game1.GameWorld.rnd.Next(2) == 0)
                    Inventory.Add(new Object(null, "cut gem", new EntityList<Material>() { FavoriteGemstone }, null));
                if (Game1.GameWorld.rnd.Next(2) == 0)
                    Inventory.Add(new Object(null, Game1.AllWeapons[Game1.GameWorld.rnd.Next(Game1.AllWeapons.Count())], new EntityList<Material>() { FavoriteMetal }, null));
                if (Game1.GameWorld.rnd.Next(2) == 0)
                    Inventory.Add(new Object(null, "wax tablet", new EntityList<Material>() { FavoriteWood }, null));
                if (Game1.GameWorld.rnd.Next(2) == 0)
                    Inventory.Add(new Object(null, "jar", new EntityList<Material>() { Game1.GameWorld.Glass }, null));
                if (Game1.GameWorld.rnd.Next(2) == 0)
                    Inventory.Add(new Object(null, "fragment", new EntityList<Material>() { Game1.GameWorld.Vitalium }, null));
            }

            else if (Type == "archmage")
            {
                // Staff added to inventory
                Material staffMaterial = Location.HomeCivilization.CulturalWood;
                Object staff = new Object(null, "staff", new EntityList<Material>() { staffMaterial }, null);
                staff.Rarity = "rare";
                staff.ApplyImbuements(1);
                Inventory.Add(staff);

                Material robeMaterial = Location.HomeCivilization.CulturalCloth;
                Object robe = new Object(null, "robe", new EntityList<Material>() { robeMaterial }, null);
                EquipOrReplaceArmor("robe", robe);

                Material bookMaterial = Location.HomeCivilization.CulturalSheet;
                Object book = new Object(null, "book", new EntityList<Material>() { bookMaterial }, null);
                book.Rarity = "uncommon";
                book.ApplyImbuements(2);
                Inventory.Add(book);
            }

            else if (Type == "beastmaster")
            {
                Material whipMaterial = Location.HomeCivilization.CulturalCloth;
                Object whip = new Object(null, "whip", new EntityList<Material>() { whipMaterial }, null);
                Inventory.Add(whip);

                Material bootsMaterial = Location.HomeCivilization.CulturalCloth;
                Object leftBoot = new Object(null, "left boot", new EntityList<Material>() { bootsMaterial }, null);
                Object rightBoot = new Object(null, "right boot", new EntityList<Material>() { bootsMaterial }, null);
                EquipOrReplaceArmor("left boot", leftBoot);
                EquipOrReplaceArmor("right boot", rightBoot);
            }

            else if (Type == "warlock")
            {
                Material tomeMaterial = Location.HomeCivilization.CulturalSheet;
                Object tome = new Object(null, "book", new EntityList<Material>() { tomeMaterial }, null);
                tome.Rarity = "rare";
                tome.ApplyImbuements(3);
                Inventory.Add(tome);

                Material amuletMaterial = Location.HomeCivilization.CulturalGemstone;
                Object amulet = new Object(null, "amulet", new EntityList<Material>() { amuletMaterial }, null);
                amulet.Rarity = "rare";
                amulet.ApplyImbuements(1);
                Inventory.Add(amulet);
            }
            else if (Type.ToLower() == "duelist")
            {
                Agility = 10;
                List<string> possibleWeapons = new List<string> { "shortsword", "dagger", "rapier", "knife" };
                int numberOfWeapons = Game1.GameWorld.rnd.Next(2, 5);
                for (int i = 0; i < numberOfWeapons; i++)
                {
                    string weaponType = possibleWeapons[Game1.GameWorld.rnd.Next(possibleWeapons.Count())];
                    Material weaponMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                    Object weapon = new Object(null, weaponType, new EntityList<Material>() { weaponMaterial }, null);
                    Inventory.Add(weapon);
                }

                Material cloakMaterial = Location.HomeCivilization.CulturalCloth;
                Object cloak = new Object(null, "cape", new EntityList<Material>() { cloakMaterial }, null);
                EquipOrReplaceArmor("cape", cloak);

                Material gemMaterial = Location.HomeCivilization.CulturalGemstone;
                Object magicalAmulet = new Object(null, "amulet", new EntityList<Material>() { gemMaterial }, null);
                magicalAmulet.Rarity = "rare";
                magicalAmulet.ApplyImbuements(1);
                Inventory.Add(magicalAmulet);
            }
            else if (Type.ToLower() == "mage")
            {
                Material orbmat = Location.HomeCivilization.CulturalGemstone;
                Object orb = new Object(null, "orb", new EntityList<Material>() { orbmat }, null);
                orb.Rarity = "rare";
                orb.ApplyImbuements(2);
                Inventory.Add(orb);

                Material robeMaterial = Location.HomeCivilization.CulturalCloth;
                Object robe = new Object(null, "robe", new EntityList<Material>() { robeMaterial }, null);
                robe.Rarity = "uncommon";
                robe.ApplyImbuements(1);
                EquipOrReplaceArmor("robe", robe);

                Material bookMaterial = Location.HomeCivilization.CulturalSheet;
                Object book = new Object(null, "book", new EntityList<Material>() { bookMaterial }, null);
                book.Rarity = "rare";
                book.ApplyImbuements(3);
                Inventory.Add(book);
            }

            else if (Type.ToLower() == "sorcerer")
            {
                Material staffMaterial = Location.HomeCivilization.CulturalWood;
                Object staff = new Object(null, "staff", new EntityList<Material>() { staffMaterial }, null);
                staff.Rarity = "uncommon";
                staff.ApplyImbuements(2);
                Inventory.Add(staff);

                Material tomeMaterial = Location.HomeCivilization.CulturalSheet;
                Object tome = new Object(null, "book", new EntityList<Material>() { tomeMaterial }, null);
                tome.Rarity = "rare";
                tome.ApplyImbuements(0);
                Inventory.Add(tome);

                Material amuletMaterial = Location.HomeCivilization.CulturalGemstone;
                Object amulet = new Object(null, "amulet", new EntityList<Material>() { amuletMaterial }, null);
                amulet.Rarity = "rare";
                amulet.ApplyImbuements(0);
                Inventory.Add(amulet);
            }

            else if (Type.ToLower() == "elemental")
            {
                Material gemMaterial = Location.HomeCivilization.CulturalGemstone;
                Object elementalGem = new Object(null, "gem", new EntityList<Material>() { gemMaterial }, null);
                elementalGem.Rarity = "rare";
                elementalGem.ApplyImbuements(2);
                Inventory.Add(elementalGem);
            }

            else if (Type.ToLower() == "spy")
            {
                Material daggerMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object dagger = new Object(null, "dagger", new EntityList<Material>() { daggerMaterial }, null);
                Inventory.Add(dagger);

                Material cloakMaterial = Location.HomeCivilization.CulturalCloth;
                Object cloak = new Object(null, "cape", new EntityList<Material>() { cloakMaterial }, null);
                EquipOrReplaceArmor("cape", cloak);
            }

            else if (Type.ToLower() == "artificer")
            {
                Material toolMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object multiTool = new Object(null, "war hammer", new EntityList<Material>() { toolMaterial }, null);
                multiTool.Rarity = "rare";
                multiTool.ApplyImbuements(0);
                Inventory.Add(multiTool);

                Material schematicMaterial = Location.HomeCivilization.CulturalSheet;
                Object schematic = new Object(null, "sheet", new EntityList<Material>() { schematicMaterial }, null);
                schematic.Rarity = "uncommon";
                Inventory.Add(schematic);
            }
            else if (Type.ToLower() == "archartificer")
            {
                Material toolMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object multiTool = new Object(null, "war hammer", new EntityList<Material>() { toolMaterial }, null);
                multiTool.Rarity = "rare";
                multiTool.ApplyImbuements(0);
                Inventory.Add(multiTool);

                Material schematicMaterial = Location.HomeCivilization.CulturalSheet;
                Object schematic = new Object(null, "sheet", new EntityList<Material>() { schematicMaterial }, null);
                schematic.Rarity = "uncommon";
                Inventory.Add(schematic);
            }

            else if (Type.ToLower() == "archbard")
            {
                Material cloakMaterial = Location.HomeCivilization.CulturalCloth;
                Object cloak = new Object(null, "cape", new EntityList<Material>() { cloakMaterial }, null);
                cloak.Rarity = "uncommon";
                EquipOrReplaceArmor("cape", cloak);
            }

            else if (Type.ToLower() == "archduelist")
            {
                Agility = 12;

                Material weaponMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object weapon = new Object(null, "rapier", new EntityList<Material>() { weaponMaterial }, null);
                weapon.Rarity = "epic";
                weapon.ApplyImbuements(3);
                Inventory.Add(weapon);

                Material talismanMaterial = Location.HomeCivilization.CulturalGemstone;
                Object talisman = new Object(null, "amulet", new EntityList<Material>() { talismanMaterial }, null);
                talisman.Rarity = "uncommon";
                Inventory.Add(talisman);
            }

            else if (Type.ToLower() == "archluminary")
            {
                Material symbolMaterial = Location.HomeCivilization.CulturalGemstone;
                Object symbol = new Object(null, "amulet", new EntityList<Material>() { symbolMaterial }, null);
                symbol.Rarity = "epic";
                symbol.ApplyImbuements(2);
                Inventory.Add(symbol);

                Material robeMaterial = Location.HomeCivilization.CulturalCloth;
                Object robe = new Object(null, "robe", new EntityList<Material>() { robeMaterial }, null);
                robe.Rarity = "rare";
                EquipOrReplaceArmor("robe", robe);

                Material tomeMaterial = Location.HomeCivilization.CulturalSheet;
                Object tome = new Object(null, "book", new EntityList<Material>() { tomeMaterial }, null);
                tome.Rarity = "rare";
                tome.ApplyImbuements(3);
                Inventory.Add(tome);
            }

            else if (Type.ToLower() == "bard")
            {
                Material garmentMaterial = Location.HomeCivilization.CulturalCloth;
                Object garment = new Object(null, "cape", new EntityList<Material>() { garmentMaterial }, null);
                garment.Rarity = "uncommon";
                EquipOrReplaceArmor("cape", garment);
            }

            else if (Type.ToLower() == "conjumancer")
            {
                Material crystalMaterial = Location.HomeCivilization.CulturalGemstone;
                Object crystal = new Object(null, "orb", new EntityList<Material>() { crystalMaterial }, null);
                crystal.Rarity = "rare";
                crystal.ApplyImbuements(2);
                Inventory.Add(crystal);

                Material cloakMaterial = Location.HomeCivilization.CulturalCloth;
                Object cloak = new Object(null, "cape", new EntityList<Material>() { cloakMaterial }, null);
                cloak.Rarity = "uncommon";
                EquipOrReplaceArmor("cape", cloak);
            }

            else if (Type.ToLower() == "diplomancer")
            {
                Material medallionMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object medallion = new Object(null, "amulet", new EntityList<Material>() { medallionMaterial }, null);
                medallion.Rarity = "uncommon";
                Inventory.Add(medallion);

                Material scrollMaterial = Location.HomeCivilization.CulturalSheet;
                Object scroll = new Object(null, "scroll", new EntityList<Material>() { scrollMaterial }, null);
                scroll.Rarity = "common";
                Inventory.Add(scroll);

                Material attireMaterial = Location.HomeCivilization.CulturalCloth;
                Object attire = new Object(null, "robe", new EntityList<Material>() { attireMaterial }, null);
                attire.Rarity = "uncommon";
                EquipOrReplaceArmor("robe", attire);
            }

            else if (Type.ToLower() == "fractalmancer")
            {
                Material orbMaterial = Location.HomeCivilization.CulturalGemstone;
                Object orb = new Object(null, "orb", new EntityList<Material>() { orbMaterial }, null);
                orb.Rarity = "rare";
                orb.ApplyImbuements(3);
                Inventory.Add(orb);

                Material attireMaterial = Location.HomeCivilization.CulturalCloth;
                Object attire = new Object(null, "robe", new EntityList<Material>() { attireMaterial }, null);
                attire.Rarity = "uncommon";
                EquipOrReplaceArmor("robe", attire);
            }

            else if (Type.ToLower() == "hunter")
            {
                Material knifeMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object knife = new Object(null, "knife", new EntityList<Material>() { knifeMaterial }, null);
                knife.Rarity = "common";
                Inventory.Add(knife);

                Material bootsMaterial = Location.HomeCivilization.CulturalCloth;
                EquipOrReplaceArmor("left boot", new Object(null, "left boot", new EntityList<Material>() { bootsMaterial }, null));
                EquipOrReplaceArmor("right boot", new Object(null, "right boot", new EntityList<Material>() { bootsMaterial }, null));
            }

            else if (Type.ToLower() == "knight")
            {
                Material swordMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object sword = new Object(null, "shortsword", new EntityList<Material>() { swordMaterial }, null);
                sword.Rarity = "common";
                Inventory.Add(sword);

                Material armorMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                EquipOrReplaceArmor("helmet", new Object(null, "helmet", new EntityList<Material>() { armorMaterial }, null));
                EquipOrReplaceArmor("chestplate", new Object(null, "chestplate", new EntityList<Material>() { armorMaterial }, null));
            }

            else if (Type.ToLower() == "magician")
            {
                Material crystalMaterial = Location.HomeCivilization.CulturalGemstone;
                Object orb = new Object(null, "orb", new EntityList<Material>() { crystalMaterial }, null);
                orb.Rarity = "uncommon";
                orb.ApplyImbuements(0);
                Inventory.Add(orb);

                Material cloakMaterial = Location.HomeCivilization.CulturalCloth;
                Object cloak = new Object(null, "cape", new EntityList<Material>() { cloakMaterial }, null);
                cloak.Rarity = "uncommon";
                EquipOrReplaceArmor("cape", cloak);
            }

            else if (Type.ToLower() == "mercenary")
            {
                Material weaponMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object weapon = new Object(null, "shortsword", new EntityList<Material>() { weaponMaterial }, null);
                weapon.Rarity = "common";
                Inventory.Add(weapon);

                Material armorMaterial = Location.HomeCivilization.CulturalMetal;
                Object chestplate = new Object(null, "chestplate", new EntityList<Material>() { armorMaterial }, null);
                chestplate.Rarity = "common";
                EquipOrReplaceArmor("chestplate", chestplate);
            }

            else if (Type.ToLower() == "necromancer")
            {
                Material tomeMaterial = Location.HomeCivilization.CulturalSheet;
                Object tome = new Object(null, "book", new EntityList<Material>() { tomeMaterial }, null);
                tome.Rarity = "rare";
                tome.ApplyImbuements(3);
                Inventory.Add(tome);

                Material cloakMaterial = Location.HomeCivilization.CulturalCloth;
                Object cloak = new Object(null, "cape", new EntityList<Material>() { cloakMaterial }, null);
                cloak.Rarity = "uncommon";
                EquipOrReplaceArmor("cape", cloak);
            }

            else if (Type.ToLower() == "perceptomancer")
            {
                Material amuletMaterial = Location.HomeCivilization.CulturalGemstone;
                Object amulet = new Object(null, "amulet", new EntityList<Material>() { amuletMaterial }, null);
                amulet.Rarity = "uncommon";
                amulet.ApplyImbuements(2);
                Inventory.Add(amulet);
            }

            else if (Type.ToLower() == "scout")
            {
                Material weaponMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object weapon = new Object(null, "dagger", new EntityList<Material>() { weaponMaterial }, null);
                weapon.Rarity = "common";
                Inventory.Add(weapon);

                Material armorMaterial = Location.HomeCivilization.CulturalMetal;
                EquipOrReplaceArmor("chestplate", new Object(null, "chestplate", new EntityList<Material>() { armorMaterial }, null));

                Object cloak = new Object(null, "cape", new EntityList<Material>() { armorMaterial }, null);
                EquipOrReplaceArmor("cape", cloak);
            }

            else if (Type.ToLower() == "spatiomancer")
            {
                Material ringMaterial = Location.HomeCivilization.CulturalGemstone;
                Object ring = new Object(null, "amulet", new EntityList<Material>() { ringMaterial }, null);
                ring.Rarity = "epic";
                ring.ApplyImbuements(4);
                Inventory.Add(ring);

                Material bookMaterial = Location.HomeCivilization.CulturalSheet;
                Object book = new Object(null, "book", new EntityList<Material>() { bookMaterial }, null);
                book.Rarity = "uncommon";
                Inventory.Add(book);
            }

            else if (Type.ToLower() == "thief")
            {
                Material toolMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object tool = new Object(null, "war hammer", new EntityList<Material>() { toolMaterial }, null);
                tool.Rarity = "rare";
                tool.ApplyImbuements(0);
                Inventory.Add(tool);

                Material daggerMaterial = Game1.GameWorld.GetRandomMaterialByStrength(Game1.GameWorld.Metals, Level * 2);
                Object dagger = new Object(null, "dagger", new EntityList<Material>() { daggerMaterial }, null);
                dagger.Rarity = "common";
                Inventory.Add(dagger);

                Material clothingMaterial = Location.HomeCivilization.CulturalCloth;
                Object robe = new Object(null, "robe", new EntityList<Material>() { clothingMaterial }, null);
                robe.Rarity = "common";
                EquipOrReplaceArmor("robe", robe);
            }

            this.HasBeenKitted = true;
        }

        public void PopulateSelf(bool includeClothing)
        {
            //PHASE 1: BODY PARTS

            if (SelfPopulated)
                return;

            if (Race != null)
            {
                for (int i = 0; i < Race.BodyPartNames.Count(); i++)
                {
                    string partName = Race.BodyPartNames[i];
                    Material partMaterial = Game1.GameWorld.Materials[Race.BodyPartMaterials[i]];

                    Object O = new Object(null, partName, new EntityList<Material> { partMaterial }, false, false, null, this, 5, false, null, null, null, false);
                    BodyParts.Add(O);
                }
            }

            foreach (Object o in BodyParts)
            {
                o.IsBodyPart = true;
                o.Owner = this;
                o.Creator = this;
                o.UpdateNames(true, this, false);
            }


            if (Game1.GameWorld.rnd.Next(1, 10) == 1)
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




            //PHASE 2: CLOTHING

            if (includeClothing)
            {
                Material Cloth;

                if (HomeLocation != null && HomeLocation.HomeCivilization != null)
                {
                    Cloth = HomeLocation.HomeCivilization.CulturalCloth;
                }
                else
                {
                    Cloth = Game1.GameWorld.Civilizations[Game1.GameWorld.rnd.Next(Game1.GameWorld.Civilizations.Count())].CulturalCloth;
                }

                // Add undergarment and store its reference
                var undergarment = new Object(null, "undergarment", new EntityList<Material>() { Cloth }, null);
                Clothing.Add(undergarment);
                undergarment.Imbuements.Clear();
                ApplyDye(undergarment);

                if (Sex == "female")
                {
                    // Add brassiere and store its reference
                    var brassiere = new Object(null, "brassiere", new EntityList<Material>() { Cloth }, null);
                    Clothing.Add(brassiere);
                    brassiere.Imbuements.Clear();
                    ApplyDye(brassiere);
                }


                if (Location != null && Location.HomeCivilization != null)
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
                    Location L = Game1.GameWorld.AllLocations
                        .Where(location => location.HomeCivilization != null && Game1.GameWorld.SettlementTypes.Contains(location.Type))
                        .OrderBy(_ => Game1.GameWorld.rnd.Next())
                        .First();

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

                if (Location != null && Location.HomeCivilization != null && Location.HomeCivilization.Type != "druid")
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
                    int numberOfItemsToAdd = Game1.GameWorld.rnd.NextDouble() < 0.2 ? 2 : Game1.GameWorld.rnd.Next(2);

                    for (int i = 0; i < numberOfItemsToAdd; i++)
                    {
                        string randomItem;
                        do
                        {
                            randomItem = generalClothingItems[Game1.GameWorld.rnd.Next(generalClothingItems.Count())];
                        } while (Clothing.Any(c => c.Type == randomItem && randomItem != "amulet"));

                        Clothing.Add(new Object(null, randomItem, new EntityList<Material>() { Cloth }, null));
                    }
                }
            }


            if(HomeLocation != null && HomeLocation.HomeCivilization != null && HomeLocation.HomeCivilization.Type == "druid")
            {
                Clothing.Clear();

                if (Sex == "female")
                {
                    Clothing.Add(new Object(null, "brassiere", new EntityList<Material>() { Game1.GameWorld.Fibers[Game1.GameWorld.rnd.Next(Game1.GameWorld.Fibers.Count())] }, null));
                }
                Clothing.Add(new Object(null, "undergarment", new EntityList<Material>() { Game1.GameWorld.Fibers[Game1.GameWorld.rnd.Next(Game1.GameWorld.Fibers.Count())] }, null));



                AddCulturalClothing(Location.HomeCivilization.CulturalHeadwear, Game1.GameWorld.Fibers[Game1.GameWorld.rnd.Next(Game1.GameWorld.Fibers.Count())]);
                AddCulturalClothing(Location.HomeCivilization.CulturalNeckwear, Game1.GameWorld.Fibers[Game1.GameWorld.rnd.Next(Game1.GameWorld.Fibers.Count())]);
                AddCulturalClothing(Location.HomeCivilization.CulturalBodywear, Game1.GameWorld.Fibers[Game1.GameWorld.rnd.Next(Game1.GameWorld.Fibers.Count())]);
                AddCulturalClothing(Location.HomeCivilization.CulturalLegwear, Game1.GameWorld.Fibers[Game1.GameWorld.rnd.Next(Game1.GameWorld.Fibers.Count())]);
                AddCulturalClothing(Location.HomeCivilization.CulturalHandwear, Game1.GameWorld.Fibers[Game1.GameWorld.rnd.Next(Game1.GameWorld.Fibers.Count())]);
                AddCulturalClothing(Location.HomeCivilization.CulturalFootwear, Game1.GameWorld.Fibers[Game1.GameWorld.rnd.Next(Game1.GameWorld.Fibers.Count())]);

                foreach(Object o in Clothing)
                {
                    o.DyedColor = "green";
                }

                SpellsKnown.Add(Game1.GameWorld.AllSpells.First(e => e.Metadata == "emergent growth"));
            }



            SelfPopulated = true;
        }

        public void DepopulateSelf()
        {
            foreach (Object o in Clothing)
                o.Delete();

            Clothing.Clear();

            foreach (Object o in BodyParts)
                o.Delete();

            BodyParts.Clear();
        }


        public Architect(string name, string sex, Race race, int age, string role, EntityList<Object> inventory, Location location, District district, Block block, string destiny, int level, bool Historical) //leave level at 0 to autodetermine
        {
            Location = location;
            Block = block;
            District = district;

            ArchitectILookLike = this;

            

            HomeDistrict = district;
            HomeLocation = location;

            Brand = Game1.BrandIDs[Game1.GameWorld.rnd.Next(Game1.BrandIDs.Count)];
            BrandColor = Game1.Colors[Game1.GameWorld.rnd.Next(Game1.Colors.Count)];

            IsNaturalWriter = Game1.GameWorld.rnd.Next(20) == 1 ? true : false;

            VoiceType = Game1.GameWorld.rnd.Next(7);

            while (ColossalColoring != "" && ColossalColoring != "black" && ColossalColoring != "gray")
            {
                ColossalColoring = Game1.Colors[Game1.GameWorld.rnd.Next(Game1.Colors.Count)];
            }

            AddRandomPersonalities();

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
            UpdateProficienciesToCurrentLevel();


            this.Historical = Historical;
            if(Historical)
            {
                Game1.GameWorld.AllHistoricalArchitects.Add(this);
            }



            MoralCompass = Game1.GameWorld.rnd.Next(-100, 101); //more is good, less is evil
            StabilityCompass = Game1.GameWorld.rnd.Next(-100, 101); //more is lawful, less is chaotic

            Name = name;
            Sex = sex;



            // Define the mannerisms list
            List<string> mannerisms = new List<string>(Game1.Mannerisms);

            // Shuffle the mannerisms list using Game1.GameWorld.rnd
            var shuffledMannerisms = mannerisms
                .OrderBy(_ => Game1.GameWorld.rnd.Next())
                .ToList();


            // Assign unique values from the shuffled list to each mannerism property
            TruthfulMannerism = shuffledMannerisms[0];
            LyingMannerism = shuffledMannerisms[1];
            UnsureMannerism = shuffledMannerisms[2];
            DerailingMannerism = shuffledMannerisms[3];
            FlirtatiousMannerism = shuffledMannerisms[4];

            if (Location != null)
            {
                if (Location.Library != null)
                {
                    
                    var validBooks = Location.Library.HistoricalObjects
                        .Where(o => o.CompositionContent != null)
                        ;

                    if (validBooks.Any())
                    {
                        FavoriteBook = validBooks[Game1.GameWorld.rnd.Next(validBooks.Count())];
                    }
                    else
                    {
                        FavoriteBook = null;
                    }
                }
            }


            if (District != null)
            {
                int number = Game1.GameWorld.rnd.Next(1, 5);

                // Convert HashSet to a shuffled list, excluding the current architect
                Contacts = new EntityList<Architect>(
                    District.DistrictArchitects
                        .Where(a => a != this) // Exclude current architect
                        .OrderBy(_ => Game1.GameWorld.rnd.Next()) // Shuffle the architects randomly
                        .Take(number) // Take up to the desired number of architects
                );

                // Now Contacts contains 1-4 unique architects excluding "this"
            }




            List<int> SkillValues = new List<int>() { 1, 2, 3, 4, 5, 6, 7 };
            // Shuffle the SkillValues list
            ShuffleGenList(SkillValues);

            // Assign values to each skill
            Strength = SkillValues[0];
            Agility = SkillValues[1];
            Dexterity = SkillValues[2];
            Endurance = SkillValues[3];
            Creativity = SkillValues[4];
            Charisma = SkillValues[5];
            Focus = SkillValues[6];

            var shuffledDomains = new EntityList<Entity>(Game1.GameWorld.Domains).ShuffleNew();

            int domainCount = Game1.GameWorld.rnd.Next(1, 8);
            AlignedDomains = new EntityHashSet<Entity>(shuffledDomains.Take(domainCount));

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

            if (Race != null)
                Powers.AddRange(Race.Powers);

            NaturalArmor = race.NaturalArmor;
            OppositionTags.AddRange(Race.OppositionTags);

            BirthdayCycle = Math.Round((Game1.GameWorld != null ? Game1.GameWorld.Cycle : 0) - age * 290_304_000.0);

            if (inventory == null)
            {
                Inventory = new EntityList<Object>();
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


            FavoriteCultureField = Game1.CultureSchools[Game1.GameWorld.rnd.Next(Game1.CultureSchools.Count())];
            FavoriteScienceField = Game1.ScienceSchools[Game1.GameWorld.rnd.Next(Game1.ScienceSchools.Count())];
            FavoriteMagicField = Game1.MagicSchools[Game1.GameWorld.rnd.Next(Game1.MagicSchools.Count())];

            FavoriteColor = Game1.Colors[Game1.GameWorld.rnd.Next(Game1.Colors.Count())];
            FavoriteGemstone = Game1.GameWorld.Gemstones[Game1.GameWorld.rnd.Next(Game1.GameWorld.Gemstones.Count())];
            FavoriteStone = Game1.GameWorld.Stones[Game1.GameWorld.rnd.Next(Game1.GameWorld.Stones.Count())];
            FavoriteWood = Game1.GameWorld.Woods[Game1.GameWorld.rnd.Next(Game1.GameWorld.Woods.Count())];
            FavoriteMetal = Game1.GameWorld.Metals[Game1.GameWorld.rnd.Next(Game1.GameWorld.Metals.Count())];
            FavoriteCloth = Game1.GameWorld.Cloths[Game1.GameWorld.rnd.Next(Game1.GameWorld.Cloths.Count())];

            DestinyArrivalYear = Game1.GameWorld.rnd.Next(18, 45);

            double NextGaussian(SerializableRandom r, double mean = 0, double stdDev = 1)
            {
                double u1 = r.NextDouble(); // Uniform(0,1] random doubles
                double u2 = r.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // Random normal(0,1)
                double randNormal = mean + stdDev * randStdNormal; // Random normal(mean,stdDev)
                return randNormal;
            }

            if (Game1.GameWorld.HumanoidRaces.Contains(Race))
            {
                if (Game1.GameWorld.rnd.Next(1, 5) == 1)
                {
                    DoIDieOfOldAge = false;
                    // Generate a normally distributed value centered around current age + 20
                    double rand = NextGaussian(Game1.GameWorld.rnd, Age + 20, 15); // Mean = Age + 20, StdDev = 15

                    TerminalAge = (int)rand;

                    // Ensure it's at least 5 years more than current age
                    if (TerminalAge < Age + 5)
                    {
                        TerminalAge = Age + 5;
                    }
                }
                else
                {
                    DoIDieOfOldAge = true;
                    // Generate a normally distributed value centered around 100
                    double rand = NextGaussian(Game1.GameWorld.rnd, 100, 20); // Mean = 100, StdDev = 20

                    TerminalAge = (int)rand;

                    // Lightly nudge the value towards the most common natural death age if below 60
                    if (TerminalAge < 60)
                    {
                        TerminalAge = (int)NextGaussian(Game1.GameWorld.rnd, 100, 25); // Re-roll for ages below 60, creating a second peak at 100
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
                int ScholarDecider = Game1.GameWorld.rnd.Next(1, 16);

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

            Energy = MaxEnergy;

            if (Reputation >= 0 && HireableArchs.Contains(this.Profession) && Historical)
                Game1.GameWorld.Hireables.Add(this);

            if(Race.Name.Contains("shiba") || Race.Name == "shobe")
            {
                Charisma = 1337;
            }




            if (Location != null && Name != "")
            {
                Game1.GameWorld.SubjectsToWriteAbout.Add(this);
            }



            if(Race.Name == "debtshiba")
            {
                SpellsKnown.AddRange(Game1.GameWorld.AllSpells);
                Charisma = 1337;
                Dexterity = 1337;
                Strength = 1337;
                Focus = 1337;
                Endurance = 1337;
                Agility = 1337;
                Creativity = 1337;
            }

            UpdateNames();
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
            if (PathOfStarsLevel > 0) paths.Add($"Stars LVL {PathOfStarsLevel}");
            if (PathOfHeatLevel > 0) paths.Add($"Heat LVL {PathOfHeatLevel}");
            if (PathOfBodyLevel > 0) paths.Add($"Body LVL {PathOfBodyLevel}");
            if (PathOfRealityLevel > 0) paths.Add($"Reality LVL {PathOfRealityLevel}");
            if (PathOfLightLevel > 0) paths.Add($"Light LVL {PathOfLightLevel}");

            return paths.Count() > 0 ? $"PATHS: {string.Join(", ", paths)}" : "No paths have levels above 0.";
        }

        public int EscapeChance()
        {
            int Chance = Agility * 4 + PathOfShadowLevel * 2 + (CyclesSinceMoved / 25);

            if (DismissalCycles > 0)
            {
                Chance += 40;
            }

            if(ActiveGifts.Contains("evasion"))
            {
                Chance += 20;
            }

            return Math.Min(Chance, 100);
        }

        public void AssignSpells()
        {
            if (Race == Game1.GameWorld.GetRace("debtshiba"))
            {
                SpellcastingPower = 15;
                for (int i = 0; i < 4; i++)
                {
                    AddSpell();
                }
            }
            else if (Profession == "archbard" || Profession == "archluminary" ||
            Profession == "archartificer")
            {
                SpellcastingPower = 5;
                for (int i = 0; i < 4; i++)
                {
                    AddSpell();
                }
            }
            else if (Profession == "warlock" || Profession == "sorcerer")
            {
                SpellcastingPower = 3;
                Focus = Math.Max(Focus, 8);
                Endurance = Math.Max(Endurance, 6);
                for (int i = 0; i < 4; i++)
                {
                    AddSpell();
                }
            }
            else if (
                     Profession == "necromancer" || Profession == "spatiomancer" ||
                     Profession == "perceptomancer" || Profession == "conjumancer" ||
                     Profession == "fractalmancer")
            {
                SpellcastingPower = 2;
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



            if (SpellcastingPower > 1 && Race.Name != "debtshiba")
            {
                // Helper to handle possessive form
                string possessive = Name.EndsWith("s") ? Name + "'" : Name + "'s";

                List<(string, string, string, string)> Gifts = new List<(string, string, string, string)>
                {
                    (
                        possessive + " Amulet of Extinguishing",
                        "extinguishing",
                        "Wearing this amulet, your energy loss to fire is reduced by 40%.",
                        "aquamarine"
                    ),
                    (
                        possessive + " Amulet of Sight",
                        "sight",
                        "Wearing this amulet, spells no longer blind you and you gain one focus.",
                        "topaz"
                    ),
                    (
                        possessive + " Amulet of Stability",
                        "stability",
                        "Wearing this amulet, destabilization reduces reaction success chances by 30% instead of 50%.",
                        "diamond"
                    ),
                    (
                        possessive + " Amulet of Carnage",
                        "carnage",
                        "Wearing this amulet, you no longer lose out on cycles due to pain, allowing you to perform more actions through it.",
                        "ruby"
                    ),
                    (
                        possessive + " Amulet of Phasing",
                        "phasing",
                        "Wearing this amulet, your evasion is increased, allowing you to escape combat easier.",
                        "tanzanite"
                    )
                };


                (string fullName, string shortName, string description, string material) Gift = Gifts[Game1.GameWorld.rnd.Next(5)];

                Object O = new Object(Gift.fullName, "amulet", new EntityList<Material>() { Game1.GameWorld.Gemstones.First(m => m.Name == Gift.material)}, this);
                O.Description = Gift.description;
                O.AmuletGift = Gift.shortName;
                Clothing.Add(O);
            }
        }

        public void AddSpell()
        {
            List<string> OffensiveSpells = new List<string> { "water bolt", "chaos flare", "flash flame", "ice shock" };

            EntityList<Entity> unknownSpells = Game1.GameWorld.AllSpells
                .Where(spell => OffensiveSpells.Contains(spell.Metadata) && !SpellsKnown.Contains(spell));

            if (unknownSpells.Count() > 0)
            {
                SpellsKnown.Add(unknownSpells[Game1.GameWorld.rnd.Next(unknownSpells.Count())]);
            }
        }


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

        public double Speed = 1.0;

        public double GetSpeed(bool Recursive, bool considerWeight = true)
        {
            if (!Recursive)
            {
                Party p = Game1.GameWorld.GamePlayerAssociation.ActiveParty;
                if (p != null && p.Architects.Contains(this) && Game1.MassTravelOrderMode)
                {
                    double minSpeed = double.MaxValue;

                    foreach (Architect a in p.Architects)
                    {
                        double partySpeed = a.GetSpeed(true, considerWeight);
                        if (partySpeed < minSpeed)
                            minSpeed = partySpeed;
                    }

                    return Math.Max(0.1, Math.Round(minSpeed, 2)); // Slowest speed among party
                }
            }

            const double BaseSpeed = 1.0; // Normalized average speed
            const double WeightEffectModifier = 0.5; // Constant to adjust the total effect of weight

            int numberOfLegs = BodyParts.Count(part => new[] { "leg", "tentacle", "wing", "fin", "sludge", "sphere", "shard" }
                                              .Any(term => part.Type.Contains(term)));
            double legSpeedModifier = CalculateLegSpeedModifier(numberOfLegs);
            double agilityModifier = CalculateAgilityModifier(Agility); // Adjusted to keep speed not too high

            double totalWeight = 0.0;
            if (considerWeight)
            {
                totalWeight = Inventory.Sum(item => item.IsMagical ? item.Weight * 0.2 : item.Weight)
                           + Clothing.Sum(item => item.IsMagical ? item.Weight * 0.2 : item.Weight * 0.8);
            }

            double weightPenaltyModifier = CalculateWeightPenaltyModifier(totalWeight, Endurance, WeightEffectModifier);
            double radiantPenalty = RadiantCycles > 0 ? 0.8 : 1.0;
            double bodyPathBonus = PathOfBodyLevel >= 8 ? 1.3 : 1.0;
            double movementBonus = MovementMode == "sprinting" ? 1.3 : (MovementMode == "walking" ? 1.0 : 0.7);
            double boostedBonus = BoostedCycles > 0 ? 1.5 : 1.0;

            double rawSpeed = BaseSpeed * legSpeedModifier * agilityModifier * weightPenaltyModifier * radiantPenalty * bodyPathBonus * movementBonus * boostedBonus;

            if (OnGround)
            {
                rawSpeed = rawSpeed / (2.0 - (Focus * 0.05));
            }

            return Math.Max(0.1, Math.Round(rawSpeed, 2)); // Rounding to the nearest two decimal places
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
            double ratio = (double)(Energy / MaxEnergy);
            if (ratio == 0 || this.IsAlive == false)
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
            Dictionary<string, int> inventoryItemCounts = new Dictionary<string, int>();
            Dictionary<string, int> clothingItemCounts = new Dictionary<string, int>();

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
                if(c.Type != "undergarment" && c.Type != "brassiere")
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
                List<string> inventoryDescriptions = inventoryItemCounts.Select(kvp =>
                {
                    string itemName = kvp.Key;
                    int count = kvp.Value;
                    string article = "aeiouAEIOU".Contains(itemName[0]) ? "an " : "a ";
                    string pluralForm = itemName.EndsWith("s") ? itemName + "es" : itemName + "s";
                    return count == 1 ? $"{article}{itemName}" : $"{count} {pluralForm}";
                }).ToList();

                if (inventoryDescriptions.Count > 1)
                {
                    string lastItem = inventoryDescriptions.Last();
                    inventoryDescriptions.RemoveAt(inventoryDescriptions.Count - 1);
                    description.Append(string.Join(", ", inventoryDescriptions));
                    description.Append(", and " + lastItem);
                }
                else
                {
                    description.Append(inventoryDescriptions[0]);
                }
            }

            if (description[description.Length - 1] != '.')
            {
                description.Append('.');
            }

            // Describe clothing
            if (clothingItemCounts.Count > 0)
            {
                description.Append($" {Game1.Capitalize(Pronoun)} is wearing ");
                List<string> clothingDescriptions = clothingItemCounts.Select(kvp =>
                {
                    string itemName = kvp.Key;
                    int count = kvp.Value;
                    string article = "aeiouAEIOU".Contains(itemName[0]) ? "an " : "a ";
                    string pluralForm = itemName.EndsWith("s") ? itemName + "es" : itemName + "s";
                    return count == 1 ? $"{article}{itemName}" : $"{count} {pluralForm}";
                }).ToList();

                if (clothingDescriptions.Count > 1)
                {
                    string lastClothingItem = clothingDescriptions.Last();
                    clothingDescriptions.RemoveAt(clothingDescriptions.Count - 1);
                    description.Append(string.Join(", ", clothingDescriptions));
                    description.Append(", and " + lastClothingItem);
                }
                else
                {
                    description.Append(clothingDescriptions[0]);
                }
            }

            if (description[description.Length - 1] != '.')
            {
                description.Append('.');
            }

            return description.ToString();
        }   


        public (int sustain, int parry, int block, int duck, int jump, int roll, int disarm, int redirect) CalculateSuccessChances(Attack attack, int ReactionModifierInt, Architect Attacker, int attackersProficiency)
        {

            if (UnconsciousCycles > 0 || HoldCycles > 0)
                return (0, 0, 0, 0, 0, 0, 0, 0);

            if (!AbjuredThisDistrict && this.Invocations.Contains("abjuration"))
                return (100, 100, 100, 100, 100, 100, 100, 100);

            double GetResistance(string DamageType) => DamageType switch
            {
                "thrashing" => ExtraThrashingResistancePercentage,
                "piercing" => ExtraPiercingResistancePercentage,
                "slashing" => ExtraSlashingResistancePercentage,
                _ => ExtraBashingResistancePercentage,
            };

            double resistanceMultiplier = GetResistance(attack.Weapon?.DamageType ?? "bashing");

            int CalculateChance(double proficiency, int baseChance, int multiplier)
            {
                int chance = (int)Math.Round(baseChance + (proficiency * multiplier));
                chance = (int)(chance * resistanceMultiplier);
                return chance;
            }

            // Prepare raw (unmodified) chances
            int sustainChance = 0;

            bool targetAffectsJump = attack.Target.Type.Contains("head") || attack.Target.Type.Contains("neck") ||
                                     attack.Target.Type.Contains("eye") || attack.Target.Type.Contains("tooth") ||
                                     attack.Target.Type.Contains("core") || attack.Target.Type.Contains("shoulder");
            bool targetAffectsDuck = attack.Target.Type.Contains("shard") || attack.Target.Type.Contains("leg") ||
                                     attack.Target.Type.Contains("foot");

            int ModifyChanceBasedOnTarget(int chance, bool decrease) => (int)(chance * (decrease ? 0.8f : 1.0f));

            bool isNecessaryBodyPart = this.Race.NecessaryBodyParts.Contains(attack.Target.Type);
            float necessaryBodyPartMultiplier = isNecessaryBodyPart ? 1.15f : 1.0f;

            Architect targetOwner = (Architect)attack.Target.Owner;

            int parryChance = ApplyRandomMultiplier(CalculateChance(GetProficiency("parrying") * ExtraWeaponReactionChancePercentage, 50, 4), ReactionModifierInt, 1);
            string[] parryWeaponTypes = { "shortsword", "longsword", "battle axe", "rapier", "mace" };
            bool leftHandParryWeapon = targetOwner.OffHeldObject != null && parryWeaponTypes.Contains(targetOwner.OffHeldObject.Type);
            bool rightHandParryWeapon = targetOwner.MainHeldObject != null && parryWeaponTypes.Contains(targetOwner.MainHeldObject.Type);
            if (!leftHandParryWeapon) parryChance = (int)(parryChance * 0.9f);
            if (!rightHandParryWeapon) parryChance = (int)(parryChance * 0.9f);

            int blockChance = 0;
            if (targetOwner.OffHeldObject?.Type == "shield" || targetOwner.MainHeldObject?.Type == "shield")
            {
                int shieldToughness = (targetOwner.OffHeldObject?.Materials?[0].Toughness ?? 0) +
                                      (targetOwner.MainHeldObject?.Materials?[0].Toughness ?? 0);
                blockChance = ApplyRandomMultiplier(CalculateChance(GetProficiency("blocking") * ExtraShieldEffectivenessPercentage, 50 + shieldToughness, 4), ReactionModifierInt, 2);
            }

            int baseDuckChance = CalculateChance(GetProficiency("dodging") * (ExtraDodgeChancePercentage + (DismissalCycles > 0 ? 0.05 : 0)), 50, 4);
            int baseJumpChance = CalculateChance(GetProficiency("dodging") * (ExtraDodgeChancePercentage + (DismissalCycles > 0 ? 0.05 : 0)), 50, 4);

            int duckChance = targetAffectsDuck ? ModifyChanceBasedOnTarget(baseDuckChance, true) : baseDuckChance;
            int jumpChance = targetAffectsJump ? ModifyChanceBasedOnTarget(baseJumpChance, true) : baseJumpChance;

            int rollChance = ApplyRandomMultiplier(CalculateChance(GetProficiency("dodging")*ExtraDodgeChancePercentage, 30, 4), ReactionModifierInt, 5);
            int disarmChance = ApplyRandomMultiplier(CalculateChance(GetProficiency("disarming"), 15, 6), ReactionModifierInt, 6);
            int redirectChance = ApplyRandomMultiplier(CalculateChance(GetProficiency("redirection") * ExtraWeaponReactionChancePercentage, 40, 4), ReactionModifierInt, 7);

            // === DEFERRED FINAL MODIFIERS ===

            // Base multiplier setup
            float multiplier = 1.0f;
            if (DestabilizedCycles > 0) multiplier *= (ActiveGifts.Contains("stability") ? 0.7f : 0.5f);
            if (Attacker.PlantCycles > 0) multiplier *= 1.3f;

            foreach (Object o in Attacker.Clothing)
                if (!o.IsWearable) multiplier *= 0.95f;

            float attackersProficiencyImpact = 1.0f - (attackersProficiency * 0.03f);
            float blindnessModifier = BlindCycles > 0 ? 0.5f : 1f;

            // Weight
            int CalculateTotalWeight(IEnumerable<Object> objects)
            {
                int totalWeight = 0;
                foreach (var obj in objects)
                {
                    totalWeight += (int)(obj.Weight);
                    if (obj.ContainedObjects != null && obj.ContainedObjects.Any())
                        totalWeight += CalculateTotalWeight(obj.ContainedObjects);
                }
                return totalWeight;
            }

            int totalWeight = CalculateTotalWeight(this.Inventory)
                              + CalculateTotalWeight(this.Clothing)
                              + (this.OffHeldObject != null ? CalculateTotalWeight(new[] { this.OffHeldObject }) : 0)
                              + (this.MainHeldObject != null ? CalculateTotalWeight(new[] { this.MainHeldObject }) : 0);

            float weightImpact = 1.0f - (totalWeight / 1000f * 0.02f);
            multiplier *= weightImpact;

            // Exposure
            float exposureImpact = Math.Clamp(1.0f - (attack.Target.Exposure / 200f), 0f, 1f);
            multiplier *= exposureImpact;

            // Reaction bonus
            int backflipBonus = ReactionBoostCycles > 0 ? 30 : 0;

            // === APPLY FINAL MULTIPLIERS TO CHANCES ===
            float finalModifier = multiplier * attackersProficiencyImpact * necessaryBodyPartMultiplier * blindnessModifier;

            return (
                sustainChance,
                Math.Clamp((int)(parryChance * finalModifier) + backflipBonus, 0, 100),
                Math.Clamp((int)(blockChance * finalModifier) + backflipBonus, 0, 100),
                Math.Clamp((int)(duckChance * finalModifier) + backflipBonus, 0, 100),
                Math.Clamp((int)(jumpChance * finalModifier) + backflipBonus, 0, 100),
                Math.Clamp((int)(rollChance * finalModifier) + backflipBonus, 0, 100),
                Math.Clamp((int)(disarmChance * finalModifier) + backflipBonus, 0, 100),
                Math.Clamp((int)(redirectChance * finalModifier) + backflipBonus, 0, 100)
            );
        }


        // Random multiplier function
        private int ApplyRandomMultiplier(int baseChance, int ReactionModifierInt, int actionIndex)
        {
            // Create a unique seed based on ReactionModifierInt and actionIndex
            int uniqueSeed = ReactionModifierInt + actionIndex * 1000; // Ensure actionIndex significantly changes the seed
            Random random = new Random(uniqueSeed);

            // Generate a multiplier in the range of -15 to +15
            int adjustment = random.Next(-15, 16); // Random number between -15 and 15 (inclusive)

            // Apply this adjustment to the base chance
            int adjustedChance = baseChance + adjustment;

            // Ensure the adjusted chance is between 0 and 100
            return Math.Clamp(adjustedChance, 0, 100);
        }


        public bool CanStand()
        {
            HashSet<string> validTypes = new HashSet<string> { "leg", "tentacle", "wing", "fin", "sludge", "sphere", "shard" };

            return BodyParts
                .Where(bp => validTypes.Any(type => bp.Type.Contains(type)) && bp.Integrity >= 30)
                .Take(2)
                .Count() == 2;
        }


        public int MaxEnergy
        {
            get
            {
                if(TutorialSickness && this.Race.Name != "shiba")
                {
                    return 5;
                }

                int max = (Endurance * 4) + 100;

                if (Race == Game1.GameWorld.GetRace("luminarch"))
                {
                    max += (int)(max * 0.1);
                }
                else if (Race == Game1.GameWorld.GetRace("nightfell"))
                {
                    max -= (int)(max * 0.1);
                }

                if (Game1.EnergySizeMultipliers.ContainsKey(Race.Size))
                {
                    max = (int)(max * Game1.EnergySizeMultipliers[Race.Size]);
                }

                max += MaxEnergyMod;
                max += MaxEnergyInspiration ? 5 : 0;

                if(this.IsUndead)
                {
                    max /= 5;
                }

                return max;
            }
        }

        public Room SearchRoom = null;


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
            EntityList<Object> filteredClothing;
            if (dropUndergarments)
            {
                filteredClothing = new EntityList<Object>(Clothing);
            }
            else
            {
                filteredClothing = Clothing.Where(o => o.Type != "brassiere" && o.Type != "undergarment");
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
                    MainHeldObject = null;
                }

                if (OffHeldObject != null)
                {
                    Block.Objects.Add(OffHeldObject);
                    OffHeldObject = null;
                }

                foreach (Object o in Block.Objects)
                {
                    o.Block = Block;
                }
            }

            if (!dropUndergarments)
            {
                Clothing = Clothing.Where(o => o.Type == "brassiere" || o.Type == "undergarment");
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
            foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
            {
                if (a.Race == Game1.GameWorld.GetRace("shade"))
                {
                    shadeCount++;
                }
            }

            ImportantThisLoad = true;

            if (IsAlive == false && (Block == executor.Block && Room == executor.Room) && shadeCount < effectivePathOfDeathLevel)
            {
                Game1.Announcements.Add(new TextStorage(referredToName + " rises with a putrid, dark energy!", Color.Purple, new EntityList<Entity>() { this }));
                IsAlive = true;
                IsImmortal = true;
                UnconsciousCycles = 0;
                HoldCycles = 0;
                Race = Game1.GameWorld.GetRace("shade");
                OppositionTags.Add("alllife");
                Energy = 50;
                Pain = 0;
                Bleeding = 0; 
                IsUndead = true;
                UndeadCreator = executor;
                CooldownCycles = 0;

                Task = "";
                TargetArchitect = null;
                Target = (Location, District, Block, Room);

                KnownArchitects.Clear();
                Opinions.Clear();
                ArchitectsForOpinions.Clear();

                for(int i = 0; i < 3; i++)
                {
                    BodyParts.Add(new Object(null, "sludge", new EntityList<Material>() { Game1.GameWorld.ShadeSludge}, false, false, null, this, 10, false, null, null, null, false));
                }

                foreach (Object o in this.BodyParts)
                {
                    o.Integrity /= 2;
                    o.Materials.Clear();
                    o.Materials.Add(Game1.GameWorld.ShadeSludge);

                }
            }
            else
            {
                Game1.Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple, new EntityList<Entity>(){}));
            }
        }

        public void TrySendCompMessageForObjective(string Task)
        {
            switch (Task)
            {
                case "prevent plan x from succeeding":
                    CompanionMessage("explainplanfoil", "");
                    break;
                case "free x from bondage":
                    CompanionMessage("explainfreebondage", "");
                    break;
                case "eliminate all life from x":
                    CompanionMessage("explaineliminatealllife", "");
                    break;
                case "deliver y to x":
                    CompanionMessage("explaindeliver", "");
                    break;
                case "reactivate x":
                    CompanionMessage("explainreactivate", "");
                    break;
                case "steal x":
                    CompanionMessage("explainsteal", "");
                    break;
                case "intercept x and kill them":
                    CompanionMessage("explaininterceptandkill", "");
                    break;
                case "capture x":
                    CompanionMessage("explaincapture", "");
                    break;
                case "deactivate x":
                    CompanionMessage("explaindeactivate", "");
                    break;
            }

        }

        public void UpdateNames()
        {
            bool PlayerKnowsArch = false;
            _referredToNames.Clear();

            string TrueProfession = Profession;

            if (Game1.GameWorld.GamePlayerAssociation == null || Game1.GameWorld.GamePlayerAssociation.ActiveParty == null || Game1.GameWorld.GameMode == "ascendant")
            {
                return;
            }

            if (IsAlive)
            {
                if(Game1.MostRecentPartyTurnArchitect.KnownArchitects.Contains(this) || Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                {
                    PlayerKnowsArch = true;
                }

                if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                {
                    string firstName = Name.Split(' ').First();

                    bool firstNameExists = Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects
                        .Any(a => a != this && a.Name.Split(' ').First() == firstName);

                    if (!firstNameExists)
                    {
                        AddReferredToName(firstName);
                    }
                }


                if (PlayerKnowsArch || Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
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
                        AddReferredToName(string.Concat("the unknown ", Profession));
                        AddReferredToName(string.Concat("unknown ", Profession));
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
                /*
                foreach (Object o in BodyParts)
                {
                    o.ClearReferredToNames();

                    foreach (string s in ReferredToNames)
                    {
                        o.AddReferredToName(s + "'s " + o.Type);
                        o.AddReferredToName(s + "' " + o.Type);
                        o.AddReferredToName(o.ID.ToString());
                    }
                }
                */

                ReferredToNames.RemoveAll(s => string.IsNullOrEmpty(s));
            }
            else
            {
                if (Game1.LoadedArchitects.Count() > 0)
                {
                    foreach (var opinion in Game1.LoadedArchitects[Game1.ArchitectIndex].ArchitectsForOpinions)
                    {
                        if (opinion == this)
                        {
                            PlayerKnowsArch = true;
                            break;
                        }
                    }

                    if (PlayerKnowsArch)
                    {
                        AddReferredToName(string.Concat(Name, ", dead.")); 
                        AddReferredToName(string.Concat("dead ", Profession));

                    }
                    else
                    {
                        AddReferredToName(string.Concat("dead ", Profession));
                    }
                }

            }



            if (this.Profession == "scholar")
            {
                for (int i = 0; i < _referredToNames.Count; i++)
                {
                    _referredToNames[i] = _referredToNames[i].Replace("scholar", this.ScholarType);
                }
            }



            if (Game1.MostRecentPartyTurnArchitect == this)
            {
                foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                {
                    a.ReferredToNames.Remove("myself");
                    a.ReferredToNames.Remove("me");
                }
                AddReferredToName("myself");
                AddReferredToName("me");
            }

            if (Game1.SplitMode)
            {
                if (_referredToNames.Count() > 0)
                {
                    string firstName = ReferredToNames[0];
                    _referredToNames.Clear(); 
                    AddReferredToName(string.Concat(firstName, " (", ID.ToString(), ")"));
                    AddReferredToName(ID.ToString());
                    AddReferredToName(firstName);
                }
            }

            //we could have done this line earlier, but we want to make sure the name comes first ONLY if you know the person. But it still ist here if you don't know them.
            if (!_referredToNames.Contains(Name))
                AddReferredToName(Name);

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
                        // Update the location of all direct and indirect subordinates up to 2 levels
                        UpdateSubordinatesMigrationLocation(a, this, 1);
                    }
                }
            }
        }

        // Recursive function to update the migration location for subordinates up to 2 levels
        public void UpdateSubordinatesMigrationLocation(Architect architect, Architect newMaster, int level)
        {
            if(newMaster.Location != null)
            {
                // Set the migration location to the new master's location
                architect.NextMigrationLocation = newMaster.Location;
                architect.MigrationReason = "My new master, " + newMaster.Name + ", awaits me in " + architect.NextMigrationLocation.Name + ".";
                architect.HomeLocation = newMaster.Location;

                // Stop the recursion after 2 levels
                if (level >= 2)
                {
                    return;
                }

                // Recursively update for all direct subordinates
                foreach (Architect a in Game1.GameWorld.Calamity)
                {
                    if (a.Master == architect)
                    {
                        UpdateSubordinatesMigrationLocation(a, newMaster, level + 1);
                    }
                }
            }
        }







        public int SelfMessageTracker = 0;







        public void AnnounceToParty(string announcement, Color color, EntityList<Entity> Entities)
        {
            foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
            {
                if (a.Room == Room && a.Block == Block)
                {
                    Game1.MakeObservation(announcement, color, Entities);
                    break;
                }
            }
        }

        public Attack UpdateSelfActionsAndSuch()
        {
            //reset imbuements

            bool IsConstruct = Game1.GameWorld.ConstructRaces.Contains(this.Race);

            Speed = GetSpeed(false);

            ExtraAttackPowerPercentage = 1.0;
            ExtraShieldEffectivenessPercentage = 1.0;
            ExtraDodgeChancePercentage = 1.0;
            ExtraWeaponReactionChancePercentage = 1.0;
            ExtraBashingResistancePercentage = 1.0;
            ExtraPiercingResistancePercentage = 1.0;
            ExtraSlashingResistancePercentage = 1.0;
            ExtraThrashingResistancePercentage = 1.0;

            ExtraStealth = 0;
            ExtraEnergyRegen = 0;

            //scan for if anyone can see me

            bool PlaySound = false;
            if (
            (this.Room == null && this.Block.Architects.Any(t => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(t))) ||
            (this.Room != null && this.Room.Architects.Any(t => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(t))))
            {
                PlaySound = true;
            }

            //description pls

            if (Block == null && Room == null)
            {
                //idk whats happening but I actually want to play my game;
                return null;
            }


            if(TimeSinceAttacked < 40 && Invocations.Contains("shadows"))
            {
                ExtraStealth += 10;
            }
            TimeSinceAttacked++;


            if (Invocations.Contains("alacrity") && !AlacrityStatIncrease)
            {
                AlacrityStatIncrease = true;

                Focus += 1;
                Charisma += 1;
                Creativity += 1;
            }
            else if (!Invocations.Contains("alacrity") && AlacrityStatIncrease)
            {
                AlacrityStatIncrease = false;

                Focus -= 1;
                Charisma -= 1;
                Creativity -= 1;
            }


            EntityList<Architect> ArchitectsToUse = (Room != null) ? Room.Architects : Block.Architects;
            Attack ReturningAttack = null;

            //distancing

            // HashSet to keep track of current architects in the room or block
            HashSet<Architect> currentArchitects = new HashSet<Architect>(Room?.Architects ?? Block?.Architects);
            currentArchitects.Remove(this);

            // Remove architects that are no longer in the room or block from the distance list
            foreach (var architect in GetDistances().Keys.Except(currentArchitects))
            {
                RemoveDistance(architect);
            }


            if(Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
            {
                if(ArchitectsToUse.Any(a => a.Bound == true && a.IsAlive == true) && this.StepsTaken > 10)
                {
                    CompanionMessage("freebound", "");
                }
                if (OnGround)
                {
                    CompanionMessage("fallendown", "");
                }
                if(SpellsKnown.Count + SkillsKnown.Count >= 2)
                {
                    CompanionMessage("recallinfo", "");
                }

                if(SpellsKnown.Count > 0 && LocationsVisited > 1)
                {
                    CompanionMessage("spelldata", "");
                }

                if (Race.Name == "shiba" && NaturalArmor == 1337)
                {
                    AnnounceToParty("The shiba inu has lost " + PossessivePronoun + " divine protection.", Color.DarkRed, new EntityList<Entity>());
                    NaturalArmor = 30;
                }
            }


            if(MainHeldObject != null && MainHeldObject.Integrity <= 0)
            {
                AnnounceToParty(MainHeldObject.ReferredToNames[0] + " is destroyed!", Color.Orange, new EntityList<Entity>() { MainHeldObject});
                MainHeldObject = null;
            }

            if (OffHeldObject != null && OffHeldObject.Integrity <= 0)
            {
                AnnounceToParty(OffHeldObject.ReferredToNames[0] + " is destroyed!", Color.Orange, new EntityList<Entity>() { OffHeldObject });
                OffHeldObject = null;
            }

            if (((ulong)Math.Round(Game1.GameWorld.Cycle)) % 5 == 0)
            {
                var clothingToRemove = new List<Object>();
                foreach (Object o in Clothing)
                {
                    if (o.Integrity <= 0)
                    {
                        AnnounceToParty(o.ReferredToNames[0] + " is destroyed!", Color.Orange, new EntityList<Entity>() { o });
                        clothingToRemove.Add(o);
                    }


                    ActiveGifts.Clear();
                    if(o.Type == "amulet" && o.AmuletGift != "")
                    {
                        ActiveGifts.Add(o.AmuletGift);
                    }

                }
                foreach (Object o in clothingToRemove)
                {
                    Clothing.Remove(o);
                }

                var inventoryToRemove = new List<Object>();
                foreach (Object o in Inventory)
                {
                    if (o.Integrity <= 0)
                    {
                        AnnounceToParty(o.ReferredToNames[0] + " is destroyed!", Color.Orange, new EntityList<Entity>() { o });
                        inventoryToRemove.Add(o);
                    }
                }
                foreach (Object o in inventoryToRemove)
                {
                    Inventory.Remove(o);
                }
            }



            if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
            {
                // Create a new list with the same contents as Inventory
                EntityList<Object> inv = new EntityList<Object>();
                foreach (Object obj in this.Inventory)
                    inv.Add(obj);

                if (MainHeldObject != null)
                    inv.Add(MainHeldObject);
                if (OffHeldObject != null)
                    inv.Add(OffHeldObject);

                // quest pointers
                for (int i = Game1.LoadedFinalPointers.Count - 1; i >= 0; i--)
                {
                    Entity e = Game1.LoadedFinalPointers[i];
                    if (e.HookedObjective.RequiredInteractionForLast == "pickup" && (this.Inventory.Contains(e) || this.MainHeldObject == e || this.OffHeldObject == e))
                    {
                        AnnounceToParty(e.IAmAFinalPointerThatFollowsAfterThisObjective.FinalMessage.Data, e.IAmAFinalPointerThatFollowsAfterThisObjective.FinalMessage.Color, e.IAmAFinalPointerThatFollowsAfterThisObjective.FinalMessage.Entities);
                        Game1.GameWorld.GamePlayerAssociation.ActiveParty.Intrigue.Add(new TextStorage(e.IAmAFinalPointerThatFollowsAfterThisObjective.FinalIntrigue, Color.Magenta, new EntityList<Entity>()));

                        foreach (Party p in Game1.GameWorld.GamePlayerAssociation.Parties)
                            p.ActiveObjectives.Remove(e.IAmAFinalPointerThatFollowsAfterThisObjective);

                        Game1.LoadedFinalPointers.RemoveAt(i);
                    }
                }



                if (!BroadcastedDeathMessage && IsAlive == false)
                {
                    bool derivedFromRecentLoss = false;

                    if (DeathCause == "" && MostRecentHPLossReason != "")
                    {
                        DeathCause = MostRecentHPLossReason;
                        derivedFromRecentLoss = true;
                    }

                    if (DeathCause != "")
                    {
                        int Month = ((int)Math.Round((decimal)(Game1.GameWorld.Cycle / 24192000)) % 12) + 1;
                        int Year = (int)Math.Round((decimal)(Game1.GameWorld.Cycle / 290304000), MidpointRounding.ToZero);
                        string Date = "(" + Month + "/" + Year + ")";

                        BroadcastedDeathMessage = true;

                        string message;
                        if (Location != null)
                        {
                            message = derivedFromRecentLoss
                                ? $"{Date} {Name} died to {DeathCause} in {Location.Name}."
                                : $"{Date} {Name} {DeathCause} in {Location.Name}.";
                            Game1.GameWorld.HistoricalEvents.Add(new Event(message, Location.Region, new EntityList<Entity>() { this, Location }));
                        }
                        else
                        {
                            message = derivedFromRecentLoss
                                ? $"{Date} {Name} died to {DeathCause}."
                                : $"{Date} {Name} {DeathCause}.";
                            Game1.GameWorld.HistoricalEvents.Add(new Event(message, Game1.GameWorld.WorldMap[10000], new EntityList<Entity>() { this, Location }));
                        }
                    }
                }

            }

            //lose all combat cycls if you are out of combat.

            bool NoOneAliveAngy = true;

            if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
            {
                foreach (Architect a in ArchitectsToUse)
                {
                    if (!Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a) && a.IsAlive)
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


            bool canStand = CanStand();

            if (OnGround == false && canStand == false)
            {
                OnGround = true;
                AnnounceToParty(ReferredToNames[0] + " has fallen on the ground.", Color.Cyan, new EntityList<Entity>() { this });

                if (PlaySound)
                    Game1.SFX.Add(Game1.Cloth);
            }
            else if (canStand == true && OnGround == true && !Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
            {
                OnGround = false;
                CooldownCycles += (int)Math.Round((20 - Agility) * Speed);
                AnnounceToParty(ReferredToNames[0] + " gets back up.", Color.Cyan, new EntityList<Entity>() { this });

                if (PlaySound)
                    Game1.SFX.Add(Game1.Cloth);
            }


            //die lmaoooo



            if (Energy <= 0 && IsAlive == true)
            {
                bool canRevitalize = false;

                int currentYear = (int)(Game1.GameWorld.Cycle / 290304000);
                int currentMonth = ((int)(Game1.GameWorld.Cycle / 24192000) % 12) + 1;
                int currentDay = ((int)(Game1.GameWorld.Cycle / 864000) % 30) + 1;
                string currentDate = $"{currentYear:D4}-{currentMonth:D2}-{currentDay:D2}";

                double currentCycle = Game1.GameWorld.Cycle;
                double oneWeek = 6048000.0; // 1 week in cycles

                if (PathOfDeathLevel >= 8)
                {
                    // If no prior revitalizations or the last one was long enough ago
                    if (RevitalizedDates.Count == 0 || (currentCycle - RevitalizedDates.Last()) >= oneWeek)
                    {
                        canRevitalize = true;
                    }
                }

                if (canRevitalize)
                {
                    AnnounceToParty(this.Name + " revitalizes in a dark cloud!", Color.DarkRed, new EntityList<Entity>() { this });
                    RevitalizedDates.Add(currentCycle);

                    if (PlaySound)
                        Game1.SFX.Add(Game1.Extract);

                    Energy = 30;
                }
                else
                {
                    IsAlive = false;
                    DropInventory(false);
                    int Month = ((int)Math.Round((decimal)(Game1.GameWorld.Cycle / 24192000)) % 12) + 1;
                    int Year = (int)Math.Round((decimal)(Game1.GameWorld.Cycle / 290304000), MidpointRounding.ToZero);
                    string Date = "(" + Month + "/" + Year + ")";

                    if(Game1.TutorialActive && this.Profession == "mercenary")
                    {
                        Game1.ProgressTutorial(30);
                    }


                    if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                    {
                        foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                        {
                            if (a.Room == this.Room && a.Block == this.Block)
                            {
                                if (a == this)
                                {
                                    a.TryComment("death", 100);
                                }
                                else
                                {
                                    a.TryComment("frienddeath", 75);
                                }
                            }
                        }
                    }



                    if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                    {
                        AnnounceToParty(this.Name + " has fallen. ", Color.Red, new EntityList<Entity>() { this });
                        Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Remove(this);

                        if(Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.All(a => a.PartyActive == false))
                        {
                            foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                            {
                                if (a.IsAlive && !a.PartyActive)
                                {
                                    AnnounceToParty(a.Name + " has been automatically reactivated.", Color.Magenta, new EntityList<Entity>() { a });
                                }
                            }
                        }
                    }
                    else
                    {
                        AnnounceToParty(this.Name + " has fallen. ", Color.Goldenrod, new EntityList<Entity>() { this });
                        if (Master != null && this.Location.Government == this)
                        {
                            switch (Game1.GameWorld.rnd.Next(1, 6))
                            {
                                case 1:
                                    AnnounceToParty(this.Name + ": " + Master.Name + ", forgive me... I could not succeed...", Color.Goldenrod, new EntityList<Entity>() { this, Master });
                                    break;
                                case 2:
                                    AnnounceToParty(this.Name + ": " + Master.Name + ", my journey ends here...", Color.Goldenrod, new EntityList<Entity>() { this, Master });
                                    break;
                                case 3:
                                    AnnounceToParty(this.Name + ": " + Master.Name + ", alas, I have fallen...", Color.Goldenrod, new EntityList<Entity>() { this, Master });
                                    break;
                                case 4:
                                    AnnounceToParty(this.Name + ": " + Master.Name + ", I apologize, I've let you down...", Color.Goldenrod, new EntityList<Entity>() { this, Master });
                                    break;
                                case 5:
                                    AnnounceToParty(this.Name + ": " + Master.Name + ", the end has come for me...", Color.Goldenrod, new EntityList<Entity>() { this, Master });
                                    break;
                            }

                            bool AlreadyKnows = false;

                            foreach (TextStorage t in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Intrigue)
                            {
                                if(t.Entities.Contains(Master))
                                {
                                    AlreadyKnows = true;
                                }
                            }

                            if(!AlreadyKnows)
                            {
                                AnnounceToParty("You sense something odd about the name " + Master.Name + "...", Color.Aqua, new EntityList<Entity>() { Master });
                                AnnounceToParty("[Intrigue Updated]", Color.Aqua, new EntityList<Entity>());

                                foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                                {
                                    if (a.District == District)
                                    {
                                        if(Game1.GameWorld.GamePlayerAssociation.ActiveParty.Intrigue.Count == 0)
                                        {
                                            Game1.GameWorld.GamePlayerAssociation.ActiveParty.Intrigue.Add(new TextStorage("Perhaps someone knows who or where these figures are...", Color.LimeGreen, new EntityList<Entity>(){}));
                                        }

                                        Game1.GameWorld.GamePlayerAssociation.ActiveParty.Intrigue.Add(new TextStorage(Name + " mentioned " + Master.Name + " before death.", Color.LimeGreen, new EntityList<Entity>() { this, Master }));
                                        break;
                                    }

                                    a.ReadyToTriggerMessageForCalamityInfoAfterReturn = true;
                                }
                            }

                            if(Game1.GameWorld.GamePlayerAssociation.ActiveParty.Intrigue.Count > 20)
                            {
                                Game1.GameWorld.GamePlayerAssociation.ActiveParty.Intrigue.RemoveAt(0);
                            }

                            Master.UpdateChildrenLocationsOnOneChildDeath();
                        }
                        else if (Game1.GameWorld.Calamity.Count > 0 && this == Game1.GameWorld.Calamity[0])
                        {
                            AnnounceToParty(this.Name + ": So... This is how it ends...", Color.PaleGoldenrod, new EntityList<Entity>());
                            AnnounceToParty(this.Name + ": My legacy ending to a small seedling such as yourself...", Color.PaleGoldenrod, new EntityList<Entity>());
                            AnnounceToParty(this.Name + ": My ideology shall live on though...", Color.PaleGoldenrod, new EntityList<Entity>());
                            AnnounceToParty(this.Name + ": Won't it...?", Color.PaleGoldenrod, new EntityList<Entity>());
                            AnnounceToParty("A primeval source of horror throughout the land, " + this.Name + ", has finally fallen.", Color.Coral, new EntityList<Entity>() { this });

                            MediaPlayer.Play(Game1.Introspection);

                            foreach (Architect a in Game1.GameWorld.Calamity)
                            {
                                if (a.IsAlive)
                                {
                                    a.Master = null;
                                    a.MasterRelation = "";

                                    Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + a.Name + " left behind the calamity that " + this.Name + " had caused.", Game1.GameWorld.AllLocations.First(l => l.Type == "stronghold").Region, new EntityList<Entity>(){a, this}));
                                }
                            }

                            foreach(Architect a in Game1.LoadedArchitects)
                            {
                                if(Game1.GameWorld.ConstructRaces.Contains(a.Race))
                                {
                                    a.UnconsciousCycles = 99999999;
                                    a.Task = "";
                                    a.AnnounceToParty(a.ReferredToNames[0] + " has deactivated.", Color.LimeGreen, new EntityList<Entity>() { a });
                                }
                            }

                            Game1.GameWorld.Calamity.Clear();
                        }

                    }

                    if(Transient)
                    {
                        AnnounceToParty(Name + " fades away.", Color.PaleGoldenrod, new EntityList<Entity>() { this });

                        if (this.Room != null)
                        {
                            Room.ArchitectsToRemove.Add(this);
                        }
                        else
                        {
                            Block.ArchitectsToRemove.Add(this);
                        }
                    }
                    else
                    {
                        // Nearby people gain stuff when you die
                        foreach (Architect a in Room != null ? Room.Architects : Block.Architects)
                        {
                            if (a != this && a.IsAlive && a.CombatTag == "")
                            {
                                AnnounceToParty(a.Name + " has absorbed some of their essence! ", Color.PaleGoldenrod, new EntityList<Entity>() { a });
                                a.Energy += 20;
                                a.CombatCycles = 0;

                                if (a.Level < Level)
                                {
                                    AnnounceToParty(a.Name + " has defeated a powerful foe and has become stronger...", Color.PaleGoldenrod, new EntityList<Entity>() { a });
                                    a.Level++;
                                    a.SpendableLevels++;
                                }
                            }
                        }
                    }
                }
            }


            //please actually die though, regardless of if you're in a party or not.

            if(IsAlive == false)
            {
                Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Remove(this);
            }

            if(Pain < 0)
            {
                Pain = 0;
            }

            if (IsAlive)
            {

                if (((ulong)Math.Round(Game1.GameWorld.Cycle)) % 20 == 0)
                {
                    // extra bleeding if your body parts are jacked up

                    // Precompile regex once for efficiency

                    bool allNecessaryPartsIntact = Race.NecessaryBodyParts.All(requiredType =>
                        BodyParts.Any(bp =>
                        {
                            string normalizedType = Game1.typeNormalizer.Replace(bp.Type, "");
                            return normalizedType == requiredType && bp.Integrity > 0;
                        }));

                    if (!allNecessaryPartsIntact)
                    {
                        Bleeding += 10;
                        if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                        {
                            AnnounceToParty(
                                $"{this.Name} is critically wounded and bleeding heavily!",
                                Color.Red,
                                new EntityList<Entity>() { this }
                            );
                        }
                    }

                }



                if (Pain > 20 && Energy < MaxEnergy - 40 && CombatCycles == 0)
                {
                    CompanionMessage("orbheal", "");
                }
                else if (Pain > 60)
                {
                    CompanionMessage("pain", "");
                }
                else if (Bleeding > 8)
                {
                    CompanionMessage("bleeding", "");
                }



                if (Pain > 100)
                {
                    this.UnconsciousCycles = Math.Max(50, UnconsciousCycles);

                    this.Pain = 50;

                    AnnounceToParty(this.ReferredToNames[0] + " goes unconscious in pain.", Color.DarkRed, new EntityList<Entity>() { this });
                }
                else if (Game1.GameWorld.rnd.Next(1, 1000) < (Pain - Focus * 10) && !Race.Name.StartsWith("shade") && IsAlive && !(ActiveGifts.Contains("carnage")))
                {
                    AnnounceToParty(this.ReferredToNames[0] + " falters in pain!", Color.DarkRed, new EntityList<Entity>() { this });
                    CooldownCycles += 5;
                }

                if (Pain > 0 && Game1.GameWorld.rnd.Next(10) == 0)
                {
                    Pain -= 1;
                }

                //regenerate

                if (((ulong)Math.Round(Game1.GameWorld.Cycle)) % 10 == 0 && !Transient)
                {
                    Energy = Math.Min(MaxEnergy, Energy + ExtraEnergyRegen);
                }

                if(Transient)
                {
                    Energy -= 0.4m;
                    MostRecentHPLossReason = "transience";
                }

                if (((ulong)Math.Round(Game1.GameWorld.Cycle)) % 5 == 0 && !Race.Name.Contains("shade") && this.CombatCycles == 0)
                {
                    foreach (Object o in this.BodyParts)
                    {
                        if (o.Integrity < 100)
                        {
                            o.Integrity++;
                        }
                    }
                }

            }



            if (Energy > MaxEnergy)
            {
                Energy = MaxEnergy;
            }


            if(this.Profession == "calamity" && ((ulong)Math.Round(Game1.GameWorld.Cycle)) % 10 == 0)
            {
                //scan for active pylons

                if (District.PylonBlock1.Objects.Any(o => o.Type == "pylon" && o.Integrity > 0) || District.PylonBlock2.Objects.Any(o => o.Type == "pylon" && o.Integrity > 0) || District.PylonRoom.Objects.Any(o => o.Type == "pylon" && o.Integrity > 0))
                {
                    if (Energy < MaxEnergy / 2)
                    {
                        Energy = MaxEnergy;

                        AnnounceToParty(ReferredToNames[0] + " calls out...", Color.LightPink, new EntityList<Entity>() { this });
                        AnnounceToParty("Dark energy flows into " + ReferredToNames[0] + "! Where are these pylons stationed?", Color.LightPink, new EntityList<Entity>() { this });
                    }

                    Protected = true;
                }
                else
                {
                    Protected = false;
                }
            }



            if (Bleeding > 0)
            {
                if (((ulong)Math.Round(Game1.GameWorld.Cycle)) % 10 == 0)
                {
                    Energy -= Bleeding;
                    MostRecentHPLossReason = "bleeding";

                    if (Energy <= 0)
                    {
                        DeathCause = "bled to death";
                    }

                    Bleeding -= 1;
                }
            }

            if (CombatCycles == 0 && Energy < MaxEnergy)
            {
                Energy += (decimal)(0.05);
            }

            if (HalfFocusTicks > 0)
            {
                HalfFocusTicks--;
            }


            if (MovementMode == "sprinting")
            {
                decimal previousEnergy = Energy;
                Energy -= 0.2m;
                MostRecentHPLossReason = "sprinting energy loss";

                if (previousEnergy > 30 && Energy < 30 && Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                {
                    Game1.Announcements.Add(new TextStorage("You might stop sprinting soon...", Color.Red, new EntityList<Entity>()));
                }
            }

            //racial benefits

            if (Race == Game1.GameWorld.GetRace("nightfell"))
            {
                ExtraAttackPowerPercentage *= 1.1;
            }
            else if (Race == Game1.GameWorld.GetRace("luminarch"))
            {
                ExtraAttackPowerPercentage *= 0.9;
            }
            else if (Invocations.Contains("slashing"))
            {
                ExtraAttackPowerPercentage += 5;
            }

            //path of shadow

            if (PathOfShadowLevel >= 2)
            {
                ExtraStealth += 10;
            }

            if (MovementMode == "sneaking")
            {
                ExtraStealth += 30;
            }

            ExtraStealth += HideValue;

            if (Invisible && IsAlive)
            {
                //lose this one only if you are low level
                if (PathOfShadowLevel < 8)
                {
                    Energy -= (decimal)(0.2);
                }

                //but then this one lmaoaooooo
                Energy -= (decimal)(0.2);
                MostRecentHPLossReason = "invisibility";

                if (Energy <= 0)
                {
                    DeathCause = "was lost to the shadows";
                    AnnounceToParty(this.ReferredToNames[0] + " was lost to the shadows.", Color.DarkRed, new EntityList<Entity>() { this });
                }


                if ((Clothing.Where(o => o.Type != "brassiere" && o.Type != "undergarment").Count == 0 && Inventory.Count() == 0) || PathOfShadowLevel >= 6)
                {
                    ExtraStealth += 1000;
                }
            }


            // Drop stuff you can't hold

            bool isSameRoomOrBlockMatch = Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Any(architect => architect.Room == this.Room)
    || Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Any(architect => architect.Block == this.Block);

            // Handle OffHeldObject and MainHeldObject similarly
            foreach (var held in new[] {
    new { HeldObject = OffHeldObject, InteractionAppendage = OffInteractionAppendage, AppendageName = Race.OffInteractionAppendage, Threshold = 50, FocusAdjusted = true, IsMain = false },
    new { HeldObject = MainHeldObject, InteractionAppendage = MainInteractionAppendage, AppendageName = Race.MainInteractionAppendage, Threshold = 60, FocusAdjusted = false, IsMain = true }
})
            {
                if (held.HeldObject != null)
                {
                    held.InteractionAppendage.OopsIDroppedIt = false;

                    bool dropDueToObject = held.InteractionAppendage.Integrity < held.Threshold;
                    bool dropDueToArm = false;

                    if (held.AppendageName.EndsWith("hand"))
                    {
                        string armName = held.AppendageName.Replace("hand", "arm").ToLower();
                        Object arm = BodyParts.FirstOrDefault(x => x.Type.ToLower() == armName);
                        int threshold = held.FocusAdjusted ? held.Threshold - Focus * 3 : held.Threshold;
                        if (arm != null && arm.Integrity < threshold)
                        {
                            dropDueToArm = true;
                        }
                    }

                    bool shouldDrop = dropDueToObject || dropDueToArm;

                    // Special case for two-handed items
                    if (held.HeldObject.IsTwoHanded)
                    {
                        bool leftHandBroken = OffInteractionAppendage.Integrity < 50;
                        bool rightHandBroken = MainInteractionAppendage.Integrity < 60;

                        bool leftArmBroken = false;
                        bool rightArmBroken = false;

                        if (Race.OffInteractionAppendage.EndsWith("hand"))
                        {
                            string leftArmName = Race.OffInteractionAppendage.Replace("hand", "arm").ToLower();
                            Object leftArm = BodyParts.FirstOrDefault(x => x.Type.ToLower() == leftArmName);
                            if (leftArm != null && leftArm.Integrity < 50)
                            {
                                leftArmBroken = true;
                            }
                        }

                        if (Race.MainInteractionAppendage.EndsWith("hand"))
                        {
                            string rightArmName = Race.MainInteractionAppendage.Replace("hand", "arm").ToLower();
                            Object rightArm = BodyParts.FirstOrDefault(x => x.Type.ToLower() == rightArmName);
                            if (rightArm != null && rightArm.Integrity < 60)
                            {
                                rightArmBroken = true;
                            }
                        }

                        bool leftPairCompromised = leftHandBroken || leftArmBroken;
                        bool rightPairCompromised = rightHandBroken || rightArmBroken;

                        // Drop if one element from each pair is broken
                        shouldDrop = leftPairCompromised && rightPairCompromised;
                    }

                    if (shouldDrop)
                    {
                        if (isSameRoomOrBlockMatch)
                        {
                            AnnounceToParty(ReferredToNames[0] + " can no longer hold the " + held.HeldObject.ReferredToNames[0] + " stably with their " + held.AppendageName + ".", Color.Orange, new EntityList<Entity>() { held.HeldObject, this, this });
                            CompanionMessage("paindropweapon", "");
                            held.InteractionAppendage.OopsIDroppedIt = true;
                        }

                        if (Room != null)
                            Room.Objects.Add(held.HeldObject);
                        else
                            Block.Objects.Add(held.HeldObject);

                        if (held.IsMain)
                            MainHeldObject = null;
                        else
                            OffHeldObject = null;
                    }
                }
            }

            // Drop items that are too hot

            if (PathOfHeatLevel < 4)
            {
                foreach (var held in new[] {
        new { HeldObject = OffHeldObject, InteractionAppendage = OffInteractionAppendage, AppendageName = Race.OffInteractionAppendage, IsMain = false },
        new { HeldObject = MainHeldObject, InteractionAppendage = MainInteractionAppendage, AppendageName = Race.MainInteractionAppendage, IsMain = true }
    })
                {
                    if (held.HeldObject != null && (held.HeldObject.FireSeconds > 0 || held.HeldObject.HeatInCelsius >= 30 + (Focus * 5)))
                    {
                        if (isSameRoomOrBlockMatch)
                        {
                            Game1.MakeObservation("The " + held.HeldObject.ReferredToNames[0] + " in " + ReferredToNames[0] + "'s " + held.AppendageName + " is too hot! " + ReferredToNames[0] + " drops it in pain.", Color.Orange, new EntityList<Entity>() { held.HeldObject, this, this });
                        }

                        if (Room != null)
                            Room.Objects.Add(held.HeldObject);
                        else
                            Block.Objects.Add(held.HeldObject);

                        if (held.IsMain)
                        {
                            MainHeldObject = null;
                            MainInteractionAppendage.OopsIDroppedIt = true;
                        }
                        else
                        {
                            OffHeldObject = null;
                            OffInteractionAppendage.OopsIDroppedIt = true;
                        }
                    }
                }

                if (((ulong)Math.Round(Game1.GameWorld.Cycle)) % 10 == 0 && this.IsAlive)
                {
                    foreach (Object clothingItem in Clothing)
                    {
                        if (clothingItem.FireSeconds > 0 || clothingItem.HeatInCelsius >= 30 + (Focus * 5))
                        {
                            Energy -= 1;
                            MostRecentHPLossReason = "clothing burning";

                            if (isSameRoomOrBlockMatch)
                            {
                                Game1.MakeObservation("The " + clothingItem.ReferredToNames[0] + " on " + ReferredToNames[0] + " is too hot, and is singeing their skin!", Color.Orange, new EntityList<Entity>() { clothingItem, this, this });
                            }

                            if (Energy <= 0)
                            {
                                DeathCause = "was burned to death by their clothing";
                            }
                        }
                    }
                }
            }










            //blindness on low eye integrity

            if (Eyes.Any() && !Eyes.Any(e => e.Integrity < 70) && BlindCycles == 0)
            {
                BlindCycles = 500;
                AnnounceToParty($"{ReferredToNames[0]} has been struck blind!", Color.Red, new EntityList<Entity>());
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


            if(Clothing.Count != ClothingValueLastTick)
            {
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
            }

            ClothingValueLastTick = Clothing.Count;


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
                if (!closeEnemies)
                {
                    CombatCycles--;
                }
            }


            AllImbuements = new EntityList<Imbuement>();

            bool PossessorNearArch = (Room != null ? Room.Architects : Block.Architects).Any(a => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a));

            foreach (Object o in Clothing)
            {
                o.UpdateSelfActionsAndSuch(this, PossessorNearArch);
                AllImbuements.AddRange(o.Imbuements);
            }

            foreach (Object o in Inventory)
            {
                o.UpdateSelfActionsAndSuch(this, PossessorNearArch);
                if (!o.IsWearable)
                {
                    AllImbuements.AddRange(o.Imbuements);
                }
            }

            foreach (Object o in BodyParts)
            {
                o.UpdateSelfActionsAndSuch(this, PossessorNearArch);
            }



            bool appendHandedness = OffHeldObject != null && MainHeldObject != null &&
                        OffHeldObject.Type == MainHeldObject.Type &&
                        OffHeldObject.Materials.SequenceEqual(MainHeldObject.Materials);

            // Detect handedness of each appendage
            bool mainIsLeft = this.MainInteractionAppendage.Type == "left hand";
            bool offIsLeft = this.OffInteractionAppendage.Type == "left hand";

            // Handle OffHeldObject
            if (OffHeldObject != null)
            {
                OffHeldObject.UpdateSelfActionsAndSuch(this, PossessorNearArch);

                if (appendHandedness)
                {
                    string label = offIsLeft ? " [L]" : " [R]";
                    for (int i = 0; i < OffHeldObject._referredToNames.Count; i++)
                    {
                        if (!OffHeldObject._referredToNames[i].EndsWith(label))
                        {
                            OffHeldObject._referredToNames[i] += label;
                        }
                    }
                }

                AllImbuements.AddRange(OffHeldObject.Imbuements);
            }

            // Handle MainHeldObject
            if (MainHeldObject != null)
            {
                MainHeldObject.UpdateSelfActionsAndSuch(this, PossessorNearArch);

                if (appendHandedness)
                {
                    string label = mainIsLeft ? " [L]" : " [R]";
                    for (int i = 0; i < MainHeldObject._referredToNames.Count; i++)
                    {
                        if (!MainHeldObject._referredToNames[i].EndsWith(label))
                        {
                            MainHeldObject._referredToNames[i] += label;
                        }
                    }
                }

                AllImbuements.AddRange(MainHeldObject.Imbuements);
            }



            RunFragmentCheck();




            //activate or deactivate if necessary



            // Activate or deactivate imbuements if necessary

            int ImbuementCap = Creativity + 3;
            int CurrentI = 0;

            // Count active imbuements first
            foreach (Imbuement i in AllImbuements)
            {
                if (i.IsActive)
                    CurrentI++;
            }

            // Disable excess active imbuements
            foreach (Imbuement i in AllImbuements)
            {
                if (CurrentI > ImbuementCap && i.IsActive)
                {
                    i.IsActive = false;
                    CurrentI--;
                }
            }

            if (Game1.AutoTurnOn && CurrentI < ImbuementCap)
            {
                foreach (Imbuement i in AllImbuements)
                {
                    if (!i.IsActive && CurrentI < ImbuementCap) 
                    {
                        i.IsActive = true;
                        CurrentI++;

                        // Stop early if we've reached the cap
                        if (CurrentI >= ImbuementCap)
                            break;
                    }
                }
            }

            int TotalImbuements = AllImbuements.Count;

            if (TotalImbuements > ImbuementCap)
            {
                CompanionMessage("switchingimbuements", "");
            }


            foreach (Architect a in MeldedShibas)
            {
                int attributeToIncrease = Game1.GameWorld.rnd.Next(0, 9);
                switch (attributeToIncrease)
                {
                    case 0:
                        ExtraShieldEffectivenessPercentage += 0.01;
                        break;
                    case 1:
                        ExtraAttackPowerPercentage += 0.01;
                        break;
                    case 2:
                        ExtraDodgeChancePercentage += 0.01;
                        break;
                    case 3:
                        ExtraWeaponReactionChancePercentage += 0.01;
                        break;
                    case 4:
                        ExtraBashingResistancePercentage += 0.01;
                        break;
                    case 5:
                        ExtraPiercingResistancePercentage += 0.01;
                        break;
                    case 6:
                        ExtraSlashingResistancePercentage += 0.01;
                        break;
                    case 7:
                        ExtraThrashingResistancePercentage += 0.01;
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
                    AnnounceToParty(Name + " has gained increases to all physical stats.", Color.Pink, new EntityList<Entity>() { this });
                    Strength++;
                    Dexterity++;
                    Agility++;
                    Focus++;
                    RecievedBodyPhysicalStatIncrease = true;
                }
            }
            if (PathOfBodyLevel >= 4)
            {
                if (!RecievedBodyPhysicalStatIncreaseTwo)
                {
                    AnnounceToParty(Name + " has gained an increased agility!", Color.Pink, new EntityList<Entity>() { this });
                    Agility++;
                    Agility++;
                    RecievedBodyPhysicalStatIncreaseTwo = true;
                }
            }

            // Go ahead and apply the imbuement effect if its a passive imbuement, but if it isn't, then wait for later
            foreach (Imbuement i in AllImbuements)
            {
                bool MetCondition = false;

                // First, check if the condition is met, regardless of whether the imbuement is active
                if (i.ConditionOrTrigger == "multipleenemies")
                {
                    var activeParty = Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects;
                    bool isInParty = activeParty.Contains(this);
                    EntityList<Architect> nearbyArchitects = Room != null ? Room.Architects : Block.Architects;

                    if (isInParty)
                    {
                        int threatCount = 0;
                        foreach (Architect e in nearbyArchitects)
                        {
                            if (e != this && e.IsAlive && (e.Task == "killtarget" || e.Task == "disabletarget") && activeParty.Contains(e.TargetArchitect))
                            {
                                threatCount++;
                            }
                        }
                        if (threatCount >= 2)
                        {
                            MetCondition = true;
                        }
                    }
                    else
                    {
                        if ((Task == "killtarget" || Task == "disabletarget") && activeParty.Contains(TargetArchitect))
                        {
                            int partyInArea = nearbyArchitects.Count(a => activeParty.Contains(a));
                            if (partyInArea >= 2)
                            {
                                MetCondition = true;
                            }
                        }
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
                        if (Game1.GameWorld.IsNightTime())
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
                    if (Energy == MaxEnergy)
                    {
                        MetCondition = true;
                    }
                }

                // Mark as satisfied based on condition, regardless of whether it's active
                if (MetCondition)
                {
                    i.IsSatisfied = true;

                    // Apply the buff only if the imbuement is active
                    if (i.IsActive)
                    {
                        switch (i.BuffOrResult)
                        {
                            case "+attack":
                                ExtraAttackPowerPercentage *= 1 + (i.SecondPower / 100f);
                                break;

                            case "+shield":
                                ExtraShieldEffectivenessPercentage *= 1 + (i.SecondPower / 100f);
                                break;
                            case "+dodge":
                                ExtraDodgeChancePercentage *= 1 + (i.SecondPower / 100f);
                                break;
                            case "+weaponreaction":
                                ExtraWeaponReactionChancePercentage *= 1 + (i.SecondPower / 100f);
                                break;
                            case "+bashpierce":
                                ExtraBashingResistancePercentage *= 1 + (i.SecondPower / 100f);
                                ExtraPiercingResistancePercentage *= 1 + (i.SecondPower / 100f);
                                break;
                            case "+slashthrash":
                                ExtraSlashingResistancePercentage *= 1 + (i.SecondPower / 100f);
                                ExtraThrashingResistancePercentage *= 1 + (i.SecondPower / 100f);
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
                }
                else
                {
                    // If the condition is not met, mark as not satisfied
                    i.IsSatisfied = false;
                }
            }


            //SHOCK MINE

            if (((ulong)Math.Round(Game1.GameWorld.Cycle)) % 10 == 0)
            {
                bool holdingShockMine = (MainHeldObject?.Type == "shock mine") ||
                                        (OffHeldObject?.Type == "shock mine") ||
                                        Inventory.Any(o => o.Type == "shock mine");

                if (holdingShockMine)
                {
                    IDidSomethingBadSoScanForShockMines();
                }
            }


            //Handle A variety of On Tick Effects

            if (WetCycles > 0)
            {
                FireSeconds = 0;
                WetCycles--;
            }
            if (FireSeconds > 0 && ((ulong)Math.Round(Game1.GameWorld.Cycle)) % 10 == 0)
            {
                if (PathOfHeatLevel < 8)
                {
                    Energy -= Math.Min(ActiveGifts.Contains("extinguishing") ? 3 : 5, FireSeconds);
                    MostRecentHPLossReason = "imminent flame engulfment";

                    if (Energy <= 0)
                    {
                        DeathCause = "burned to death";
                    }
                }

                FireSeconds--;
            }
            if (BlindCycles > 0)
            {
                CompanionMessage("blind", "");
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
                CompanionMessage("unconscious", "");
                UnconsciousCycles--;
            }
            if (RadiantCycles > 0)
            {
                RadiantCycles--;
            }
            if (HoldCycles > 0)
            {
                HoldCycles--;
            }
            if (PlantCycles > 0)
            {
                PlantCycles--;
            }
            if (BoostedCycles > 0)
            {
                BoostedCycles--;
            }
            if (CloakCycles > 0)
            {
                ExtraStealth += 20;
                CloakCycles--;
            }


            if(!IsAlive)
            {
                ArchitectILookLike = this;
            }

            if (InFlight == false && (YLevelInFeet > 0 && (this.OnTopOfStructure == null || YLevelInFeet > 30)))
            {
                YVelocity = Math.Min(176.0, YVelocity + 3.2); // Increase YVelocity up to a cap of 27 (over 10 cycles)

                if (this.OnTopOfStructure != null && YLevelInFeet > 30)
                {
                    YLevelInFeet = Math.Max(30, YLevelInFeet - YVelocity / 10.0); // Fall until YLevelInFeet = 30 if on a structure
                }
                else
                {
                    YLevelInFeet = Math.Max(0, YLevelInFeet - YVelocity / 10.0); // Fall until YLevelInFeet = 0 if not on a structure
                }

                if (YLevelInFeet == 0 || (this.OnTopOfStructure != null && YLevelInFeet == 30))
                {
                    // Observation when the entity hits the ground
                    Game1.MakeObservation(ReferredToNames[0] + " hits the ground!", Color.Orange, new EntityList<Entity>() { this });

                    if (PlaySound)
                        Game1.SFX.Add(Game1.OpenInv);

                    // Damage calculation based on YVelocity
                    int DamageInstances = (int)Math.Round(YVelocity / 6.4); // Adjusted to align with YVelocity increments, but cut in half
                    for (int i = 0; i < DamageInstances; i++)
                    {
                        Object o = this.BodyParts[Game1.GameWorld.rnd.Next(BodyParts.Count())];

                        // Observation for body part damage
                        Game1.MakeObservation(o.ReferredToNames[0] + " is bruised by the impact!", Color.Orange, new EntityList<Entity>() { o });

                        // Reduce body part integrity based on YVelocity
                        o.Integrity = (int)Math.Max(0, o.Integrity - YVelocity);
                        Energy -= Game1.GameWorld.rnd.Next(0, 6);
                        MostRecentHPLossReason = "falling from a decent height";
                        Bleeding += Game1.GameWorld.rnd.Next(0, 2);
                        Pain += Game1.GameWorld.rnd.Next(0, 6);
                    }

                    // Reset YVelocity after impact
                    YVelocity = 0;
                }
            }


            if(InFlight == true && YLevelInFeet > 0)
            {
                FlightTicks -= 1;
                if(FlightTicks <= 0)
                {
                    InFlight = false;
                    AnnounceToParty(this.Name + " ran out of flight time!", Color.OrangeRed, new EntityList<Entity>() { this });
                }
            }

            if (YLevelInFeet == 0 && FlightTicks != 200)
            {
                FlightTicks = 200;
                AnnounceToParty(this.Name + " can fly again.", Color.WhiteSmoke, new EntityList<Entity>() { this });
            }

            IEnumerable<Architect> architects = (Room == null) ? Block.Architects : Room.Architects;





            if ((IsAlive && !Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this) && UnconsciousCycles == 0 && HoldCycles == 0))
            {

                // Clamp function to ensure a value stays within a specified range
                int Clamp(int value, int min, int max)
                {
                    return Math.Max(min, Math.Min(max, value));
                }

                // Respond to messages
                foreach (Message m in MessagesNotRespondedTo)
                {
                    // Check if the message has been seen before and is a question
                    bool messageSeenBefore = ResponseDatabase.Any(responseKvP => responseKvP.Key == m.MessageContent);


                    Architect ActualSender = m.Sender;
                    
                    if(m.Sender.ArchitectILookLike != m.Sender)
                    {
                        m.Sender = m.Sender.ArchitectILookLike;
                    }


                    if(Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(m.Sender) && Game1.PlanLoadedArchitects.Contains(this))
                    {
                        //try to find your plan

                        Plan p = Game1.GameWorld.AllFactions
                            .Where(f => f.CurrentPlan != null)
                            .Select(f => f.CurrentPlan)
                            .First(p => p.PlanInitiators.Contains(this));


                        if (p != null)
                        {
                            AnnounceToParty(ReferredToNames[0] + ": Our " + p.Name.Split(' ')[0].ToLower() + " is beyond you, " + m.Receiver.Profession + ". Your presence is tolerated, but do not interfere.", Color.OrangeRed, new EntityList<Entity>() { this });
                            Game1.GameWorld.GamePlayerAssociation.ActiveParty.ReceivedPlanMessageThisLoad = true;
                        }
                    }

                    string Header = "";

                    bool Darken = !Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(m.Sender) && !Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(m.Receiver);

                    if (messageSeenBefore &&
    !(m.Receiver.ArchitectsIWillTellTruthTo.Contains(m.Sender) ||
      m.Sender.ArchitectsWhoSurrenderedToMe.Contains(m.Receiver) ||
      m.MessageContent.StartsWith("Would you tell me where I can find")))
                    {
                        if (ResponseDatabase.Any(KvP => KvP.Key == m.MessageContent))
                        {

                            Header = m.IgnoreHeader ? "" : (ReferredToNames[0] + ": ");

                            AnnounceToParty(Header + ResponseDatabase[m.MessageContent].Item1, new Color(0, 255, 0) * (Darken ? 0.3f : 1.0f), ResponseDatabase[m.MessageContent].Item2);
                        }
                    }


                    // Determine if the receiver can receive the message based on the new conditions
                    bool bothAreSapient = (Game1.GameWorld.HumanoidRaces.Contains(ActualSender.Race) ||
                                           Game1.GameWorld.ExtraRaces.Contains(ActualSender.Race)) &&
                                          (Game1.GameWorld.HumanoidRaces.Contains(this.Race) ||
                                           Game1.GameWorld.ExtraRaces.Contains(this.Race));

                    bool canReceiveMessage = bothAreSapient || this.PathOfLifeLevel >= 4 || m.Sender.PathOfLifeLevel >= 4;

                    if ((CombatCycles > 0 && !(m.MessageID == "surrender" || m.MessageID == "demand_surrender")) || !canReceiveMessage || Bound)
                    {
                        AnnounceToParty(ReferredToNames[0] + " does not reply.", Color.Yellow * (Darken ? 0.3f : 1.0f), new EntityList<Entity>() { this });
                        continue;
                    }

                    int senderOpinion = GetOpinion(m.Sender.ArchitectILookLike);
                    int baseChanceToTruth = 60; // Increased base chance to tell the truth
                    int baseChanceToMakeUp = 20; // Decreased base chance to make up a lie
                    int baseChanceToClaimIgnorance = 10;
                    int baseChanceToDerail = 5;
                    int baseChanceToFlatter = 5;

                    // Adjust baseChanceToTruth by Sender.Charisma
                    baseChanceToTruth += m.Sender.Charisma * 3;

                    if (Game1.ProbablyWillTellTruth.Contains(Profession))
                    {
                        baseChanceToTruth += 15;
                        baseChanceToMakeUp -= 5;
                        baseChanceToClaimIgnorance -= 5;
                    }

                    if (m.Receiver.ArchitectsIWillTellTruthTo.Contains(m.Sender) || m.Sender.ArchitectsWhoSurrenderedToMe.Contains(m.Receiver) || Game1.TutorialActive)
                    {
                        // Always respond truthfully if the sender is in ArchitectsIWillTellTruthTo or if it's a request for directions, or if theyre surrendered always comply.
                        baseChanceToTruth = 100;
                        baseChanceToMakeUp = 0;
                        baseChanceToClaimIgnorance = 0;
                        baseChanceToDerail = 0;
                        baseChanceToFlatter = 0;
                    }
                    else if (m.MessageContent.StartsWith("Would you tell me where I can find") && m.MessageID != "ask_generic_directions")
                    {
                        // List of professions that will always respond truthfully
                        var truthProfessions = new HashSet<string>
                        {
                            "scholar", "mage", "engineer", "entertainer", "artificer", "bard", "sage", "luminary",
                            "warlock", "sorcerer", "necromancer", "spatiomancer", "perceptomancer", "conjumancer",
                            "fractalmancer", "archmage", "magician", "archbard", "archsage", "archluminary", "archartificer", "scribe"
                        };





                        //the person will also tell the truth if the thing is nearby


                        bool Familiar = false;

                        if (
                            Game1.LoadedArchitects.Contains(m.Subjects[0]) ||

                            (m.Subjects[0] is Structure s && s.Block.District == this.District) ||

                            (m.Subjects[0] is Object targetObj &&
                                District.DistrictMap.Any(block =>
                                    block.Structures.Any(structure =>
                                        structure.Rooms.Any(room =>
                                            room.Objects.Contains(targetObj) ||
                                            room.Objects.Any(obj =>
                                                obj.ContainedObjects.Contains(targetObj)
                                            )
                                        )
                                    )
                                )
                            )
                        )
                        {
                            Familiar = true;
                        }


                        if (truthProfessions.Contains(Profession) || Familiar == true)
                        {
                            baseChanceToTruth = 100;
                            baseChanceToMakeUp = 0;
                            baseChanceToClaimIgnorance = 0;
                            baseChanceToDerail = 0;
                            baseChanceToFlatter = 0;
                        }
                        else
                        {
                            baseChanceToTruth = 0;
                            baseChanceToMakeUp = 0;
                            baseChanceToClaimIgnorance = 100;
                            baseChanceToDerail = 0;
                            baseChanceToFlatter = 0;
                        }
                    }
                    else if (m.MessageID == "challenge" && Level == 1)
                    {
                        baseChanceToTruth = 0;
                        baseChanceToMakeUp = 100;
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


                    //normalize

                    int total = baseChanceToTruth + baseChanceToMakeUp + baseChanceToClaimIgnorance + baseChanceToDerail + baseChanceToFlatter;

                    if (total > 0)
                    {
                        float scale = 100f / total;
                        baseChanceToTruth = (int)(baseChanceToTruth * scale);
                        baseChanceToMakeUp = (int)(baseChanceToMakeUp * scale);
                        baseChanceToClaimIgnorance = (int)(baseChanceToClaimIgnorance * scale);
                        baseChanceToDerail = (int)(baseChanceToDerail * scale);
                        baseChanceToFlatter = 100 - (baseChanceToTruth + baseChanceToMakeUp + baseChanceToClaimIgnorance + baseChanceToDerail); // Assign remainder
                    }





                    // Specific conditions based on message content
                    bool conditionMet = true;
                    switch (m.MessageID)
                    {
                        case "ask_them_join":
                            {
                                // If either participant is a debtshiba, always claim ignorance
                                if (m.Receiver.Race.Name == "debtshiba" || m.Sender.Race.Name == "debtshiba")
                                {
                                    conditionMet = false;
                                    baseChanceToTruth = 0;
                                    baseChanceToClaimIgnorance = 100;
                                    break;
                                }

                                // Check if the Receiver already has a group or if the Sender's group is full
                                if (m.Receiver.Group != null || (m.Sender.Group != null && m.Sender.Group.Architects.Count() >= 4))
                                {
                                    conditionMet = false;
                                    baseChanceToTruth = 0;
                                    baseChanceToClaimIgnorance = 100;
                                }
                                // Check for personality conflicts (Receiver is the new recruit, Sender is in the group)
                                else if (m.Sender.Group != null)
                                {
                                    var existingPersonalities = m.Sender.Group.Architects
                                        .SelectMany(a => a.Personalities)
                                        .ToList();
                                    var newPersonalities = m.Receiver.Personalities;

                                    if (existingPersonalities.Intersect(newPersonalities).Any())
                                    {
                                        conditionMet = false;
                                        baseChanceToTruth = 0;
                                        baseChanceToClaimIgnorance = 100;
                                    }
                                }
                                break;
                            }

                        case "ask_to_join":
                            {
                                // If either participant is a debtshiba, always claim ignorance
                                if (m.Receiver.Race.Name == "debtshiba" || m.Sender.Race.Name == "debtshiba")
                                {
                                    conditionMet = false;
                                    baseChanceToTruth = 0;
                                    baseChanceToClaimIgnorance = 100;
                                    break;
                                }

                                // Check if the Receiver's group is full
                                if (m.Receiver.Group == null || (m.Receiver.Group != null && m.Receiver.Group.Architects.Count() >= 4))
                                {
                                    conditionMet = false;
                                    baseChanceToTruth = 0;
                                    baseChanceToClaimIgnorance = 100;
                                }
                                // Check for personality conflicts (Sender is the new recruit, Receiver is in the group)
                                else if (m.Receiver.Group != null)
                                {
                                    var existingPersonalities = m.Receiver.Group.Architects
                                        .SelectMany(a => a.Personalities)
                                        .ToList();
                                    var newPersonalities = m.Sender.Personalities;

                                    if (existingPersonalities.Intersect(newPersonalities).Any())
                                    {
                                        conditionMet = false;
                                        baseChanceToTruth = 0;
                                        baseChanceToClaimIgnorance = 100;
                                    }
                                }
                                break;
                            }

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
                            var nearestMarket = ActualSender.Block.FindNearestThing("market");
                            if (nearestMarket.Item1 != m.Receiver.Location)
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
                        AnnounceToParty(ReferredToNames[0] + ": " + m.IgnorantResponse, new Color(0, 255, 0) * (Darken ? 0.3f : 1.0f), new EntityList<Entity>() { this }.Union(m.ResponseEntitiesForThree));

                        if (PlaySound)
                            Game1.SFX.Add(Game1.TalkSounds[VoiceType]);

                        Game1.MessageWorldEdit(ActualSender, this, m.MessageID, m.Subjects, m.IgnorantResponse, m.StoredRevealLocations, m.StoredKnownArchs);
                        continue;
                    }

                    int randomNumber = Game1.GameWorld.rnd.Next(1, 101);
                    string response;

                    string ResponseType = "";

                    EntityList<Entity> ResponseSubjects = null;

                    if (randomNumber <= baseChanceToTruth)
                    {
                        response = m.PositiveResponse;
                        ResponseType = "truth";
                        ResponseSubjects = m.ResponseEntitiesForOne;
                    }
                    else if (randomNumber <= baseChanceToTruth + baseChanceToMakeUp)
                    {
                        response = m.DirectRefusalResponse;
                        ResponseType = "lie";
                        ResponseSubjects = m.ResponseEntitiesForTwo;
                    }
                    else if (randomNumber <= baseChanceToTruth + baseChanceToMakeUp + baseChanceToClaimIgnorance)
                    {
                        response = m.IgnorantResponse;
                        ResponseType = "ignore";
                        ResponseSubjects = m.ResponseEntitiesForThree;
                    }
                    else if (randomNumber <= baseChanceToTruth + baseChanceToMakeUp + baseChanceToClaimIgnorance + baseChanceToDerail)
                    {
                        response = m.DerailingResponse;
                        ResponseType = "derail";
                        ResponseSubjects = m.ResponseEntitiesForFour;
                    }
                    else
                    {
                        response = m.FlatteringResponse;
                        ResponseType = "flirt";
                        ResponseSubjects = m.ResponseEntitiesForFive;
                    }

                    // 25% chance to announce the mannerism
                    if (Game1.GameWorld.rnd.Next(1, 101) <= 33)
                    {
                        string mannerism = "";
                        Color mannerismColor = (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(m.Sender)) ? Color.Gray * (Darken ? 0.3f : 1.0f) : new Color(100, 100, 100);

                        switch (ResponseType)
                        {
                            case "truth":
                                mannerism = TruthfulMannerism;
                                break;
                            case "lie":
                                mannerism = LyingMannerism;
                                break;
                            case "ignore":
                                mannerism = UnsureMannerism;
                                break;
                            case "derail":
                                mannerism = DerailingMannerism;
                                break;
                            case "flirt":
                                mannerism = FlirtatiousMannerism;
                                break;
                        }

                        AnnounceToParty(ReferredToNames[0] + " " + mannerism + ".", mannerismColor, new EntityList<Entity> { this });
                    }


                    
                    Header = m.IgnoreHeader ? "" : (ReferredToNames[0] + ": ");

                    AnnounceToParty(Header + response, new Color(0,255,0) * (Darken ? 0.3f : 1.0f), new EntityList<Entity> { this }.Union(ResponseSubjects));


                    if(Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(m.Sender))
                    {
                        foreach (Entity E in Game1.LoadedHooks)
                        {
                            if (E.HookedObjective.Hook is Architect a && a == this && a.HookedObjective.RequiredInteraction == "offerassistance")
                            {
                                TextStorage t = new TextStorage(a.HookedObjective.PointerIntrigue, Color.White, new EntityList<Entity>());

                                t.AttachedQuest = E.HookedObjective.ParentQuest;

                                Game1.GameWorld.GamePlayerAssociation.ActiveParty.ActiveObjectives.Add(E.HookedObjective);

                                m.Sender.TrySendCompMessageForObjective(E.HookedObjective.ActualTask);

                                Game1.GameWorld.GamePlayerAssociation.ActiveParty.Intrigue.Add(t);
                                AnnounceToParty("[Intrigue Updated]", Color.Magenta, new EntityList<Entity>());
                            }
                        }
                    }

                    if (PlaySound)
                        Game1.SFX.Add(Game1.TalkSounds[VoiceType]);

                    // Store the message and response
                    if (ResponseType != "flirt")
                    {
                        ResponseDatabase[m.MessageContent] = (response, ResponseSubjects);
                    }
                    Game1.MessageWorldEdit(ActualSender, m.Receiver, m.MessageID, m.Subjects, response, m.StoredRevealLocations, m.StoredKnownArchs);

                    CooldownCycles += (int)Math.Round(30 / Speed);

                    if(DieOnResponse)
                    {
                        Energy = -999;
                        IsAlive = false;
                    }
                }

                MessagesNotRespondedTo.Clear();
            }





            //actions

            string StoredAltMove = "";

           

            if (IsAlive && CooldownCycles == 0 && !Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this) && UnconsciousCycles == 0 && HoldCycles == 0 && Race != Game1.GameWorld.GetRace("moari"))
            {
                //opinions

                //have an opinion with everyone in the room


                foreach (Architect a in architects)
                {
                    if (a.IsAlive == true && GetOpinion(a.ArchitectILookLike) == 0 && a != this && (!a.InvisibleCheck || IsConstruct) && !a.ArchitectILookLike.ArchitectsWhoSurrenderedToMe.Contains(this) && !this.ArchitectsWhoISurrenderedTo.Contains(a.ArchitectILookLike))
                    {
                        int FinalOpinion = 0;
                        bool isOpposed = false;

                        if (OppositionTags.Contains("humanoids") && Game1.GameWorld.HumanoidRaces.Contains(a.ArchitectILookLike.Race))
                        {
                            FinalOpinion = Math.Min(FinalOpinion, -247);
                            isOpposed = true;
                        }

                        if (OppositionTags.Contains("alllife") && !a.ArchitectILookLike.Race.Name.Contains("shade"))
                        {
                            FinalOpinion = Math.Min(FinalOpinion, -247);
                            isOpposed = true;
                        }

                        if (OppositionTags.Contains("allsentient"))
                        {
                            FinalOpinion = Math.Min(FinalOpinion, -247);
                            isOpposed = true;
                        }

                        if (OppositionTags.Contains("allunalike") && a.ArchitectILookLike.Race != Race)
                        {
                            // Early exit: it's fine, don't become upset
                            if (Game1.GameWorld.ProcgenStructures.Contains(Location.Type) &&
                                Game1.GameWorld.WildRaces.Contains(Race) &&
                                a.HomeLocation == this.Location)
                            {
                                // they're both from the wild and from the same location - harmony
                            }
                            else
                            {
                                var hypernexus = Game1.GameWorld.GetRace("hypernexus");
                                var photonexus = Game1.GameWorld.GetRace("photonexus");
                                var isofractal = Game1.GameWorld.GetRace("isofractal");
                                var icosidodecahedron = Game1.GameWorld.GetRace("icosidodecahedron");
                                var shadebeast = Game1.GameWorld.GetRace("shadebeast");
                                var shade = Game1.GameWorld.GetRace("shade");
                                var shadeheart = Game1.GameWorld.GetRace("shadeheart");

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
                        }

                        if (OppositionTags.Contains("allunalikeignoreconstructs") && a.ArchitectILookLike.Race != Race)
                        {

                            if(Game1.GameWorld.ConstructRaces.Contains(a.Race))
                            {
                                //the shiba wont kill his babies
                            }
                            // Early exit: it's fine, don't become upset
                            else if (Game1.GameWorld.ProcgenStructures.Contains(Location.Type) &&
                                Game1.GameWorld.WildRaces.Contains(Race) &&
                                a.HomeLocation == this.Location)
                            {
                                // they're both from the wild and from the same location - harmony
                            }
                            else
                            {
                                var hypernexus = Game1.GameWorld.GetRace("hypernexus");
                                var photonexus = Game1.GameWorld.GetRace("photonexus");
                                var isofractal = Game1.GameWorld.GetRace("isofractal");
                                var icosidodecahedron = Game1.GameWorld.GetRace("icosidodecahedron");
                                var shadebeast = Game1.GameWorld.GetRace("shadebeast");
                                var shade = Game1.GameWorld.GetRace("shade");
                                var shadeheart = Game1.GameWorld.GetRace("shadeheart");

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
                        }

                        if (OppositionTags.Contains("allevil") && (a.ArchitectILookLike.Race.Name.Contains("shade") || (a.ArchitectILookLike.Group != null && a.ArchitectILookLike.Group.Reputation < -40) || a.ArchitectILookLike.Reputation < -40))
                        {
                            FinalOpinion = Math.Min(FinalOpinion, -247);
                            isOpposed = true;
                        }

                        if (OppositionTags.Contains("intruders") && !a.Bound && (a.ArchitectILookLike.HomeLocation != Location || (Game1.GameWorld.GamePlayerAssociation.ActiveParty.CurrentEvent != null && (Game1.GameWorld.GamePlayerAssociation.ActiveParty.CurrentEvent.UnitArchitects.Contains(this) && !Game1.GameWorld.GamePlayerAssociation.ActiveParty.CurrentEvent.UnitArchitects.Contains(a.ArchitectILookLike)))))
                        {
                            FinalOpinion = Math.Min(FinalOpinion, -247);
                            isOpposed = true;
                        }

                        if (!isOpposed)
                        {
                            FinalOpinion = Math.Min(FinalOpinion, 1);
                        }

                        SetOpinion(a.ArchitectILookLike, FinalOpinion);

                        if (FinalOpinion == -247 && Level > 1 && a.StepsTaken > 5)
                            a.CompanionMessage("trymakeattack", "");
                    }

                    if (OppositionTags.Contains("indebted") && HomeStructure != null && HomeStructure.MarketDebtToUs <= -1 && Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a.ArchitectILookLike) && a.Structure == null && (!a.InvisibleCheck || IsConstruct))
                    {
                        SetOpinion(a.ArchitectILookLike, -247);
                    }
                }




                

                //stand up


                //attempt to surrender

                if (!Game1.GameWorld.Calamity.Contains(this) && CourageValue < 50 && Energy < 40 && CombatCycles > 0 && !Game1.TutorialActive && (Task == "killtarget" || Task == "disabletarget") && TargetArchitect != null && TargetArchitect.Block == this.Block && TargetArchitect.Room == this.Room && !ArchitectsWhoIAttemptedToSurrenderTo.Contains(TargetArchitect.ArchitectILookLike))
                {
                    ArchitectsWhoIAttemptedToSurrenderTo.Add(TargetArchitect.ArchitectILookLike);

                    bool canSendMessage = true;

                    // Both parties are in Talker category
                    if ((Game1.GameWorld.HumanoidRaces.Contains(this.Race) || Game1.GameWorld.ExtraRaces.Contains(this.Race)) &&
                        (Game1.GameWorld.HumanoidRaces.Contains(TargetArchitect.ArchitectILookLike.Race) || Game1.GameWorld.ExtraRaces.Contains(TargetArchitect.ArchitectILookLike.Race)))
                    {
                        canSendMessage = true;
                    }
                    // One party is not in Talker category
                    else if ((Game1.GameWorld.HumanoidRaces.Contains(this.Race) || Game1.GameWorld.ExtraRaces.Contains(this.Race)) &&
                             (!Game1.GameWorld.HumanoidRaces.Contains(TargetArchitect.ArchitectILookLike.Race) && !Game1.GameWorld.ExtraRaces.Contains(TargetArchitect.ArchitectILookLike.Race)) ||
                             (!Game1.GameWorld.HumanoidRaces.Contains(this.Race) && !Game1.GameWorld.ExtraRaces.Contains(this.Race)) &&
                             (Game1.GameWorld.HumanoidRaces.Contains(TargetArchitect.ArchitectILookLike.Race) || Game1.GameWorld.ExtraRaces.Contains(TargetArchitect.ArchitectILookLike.Race)))
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
                        CommandProcessor.SendMessage("surrender", this, TargetArchitect, new EntityList<Entity> { }, Game1.GameWorld);
                    }
                }


                //send messages of your own
                //send off messages of your own (why do you always search that)
                if (Game1.GameWorld.rnd.Next(1, 15) < Charisma && this.Task != "killtarget" && this.Task != "disabletarget" && Bound == false && MessageCooldown == 0 && !TutorialSickness)
                {
                    var ArchList = Room != null ? Room.Architects : Block.Architects;

                    MessageCooldown += Game1.GameWorld.rnd.Next(50, 100);

                    // Filter out the current Architect instance from ArchList
                    var OtherArchitects = ArchList.Where(arch => arch != this && (!arch.InvisibleCheck || IsConstruct));

                    if (OtherArchitects.Count() > 0)
                    {
                        Architect ChosenArchitect = OtherArchitects[Game1.GameWorld.rnd.Next(OtherArchitects.Count())];

                        bool isTargetPlayerArchitect = Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(ChosenArchitect);

                        // 50 percent chance to ignore messages to a player, so people will talk a lot more often but it won't be annoying
                        if (isTargetPlayerArchitect && Game1.GameWorld.rnd.Next(2) == 0)
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
                                int Decider = Game1.GameWorld.rnd.Next(100); // Generate a random number between 0 and 99
                                string MType = "";

                                if (Decider < 10) // Greet: 10%
                                {
                                    MType = "greet";
                                    IDsICanGoodbye.Add(ChosenArchitect.ID);
                                }
                                else if (Decider < 20 && IDsICanGoodbye.Contains(ChosenArchitect.ID)) // Goodbye: 10%
                                {
                                    MType = "farewell";
                                }
                                else if (Decider < 25 && IDsICanThank.Contains(ChosenArchitect.ID)) // Thank: 5%
                                {
                                    MType = "thank";
                                }
                                else if (Decider < 30 && IDsICanApologizeTo.Contains(ChosenArchitect.ID)) // Apologize: 5%
                                {
                                    MType = "apologize";
                                }
                                else if (Decider < 35) // Ask Health: 5%
                                {
                                    MType = "ask_health";
                                    IDsICanThank.Add(ChosenArchitect.ID);
                                }
                                else if (Decider < 45) // Ask News: 10%
                                {
                                    MType = "ask_news";
                                    IDsICanThank.Add(ChosenArchitect.ID);
                                    ChosenArchitect.IDsICanApologizeTo.Add(ChosenArchitect.ID);
                                }
                                else if (Decider < 55) // Ask History: 10%
                                {
                                    MType = "ask_history";
                                    IDsICanThank.Add(ChosenArchitect.ID);
                                    ChosenArchitect.IDsICanApologizeTo.Add(ChosenArchitect.ID);
                                }
                                else if (Decider < 65) // Ask Opinion: 10%
                                {
                                    MType = "ask_opinion";
                                    IDsICanThank.Add(ChosenArchitect.ID);
                                    ChosenArchitect.IDsICanApologizeTo.Add(ChosenArchitect.ID);
                                }
                                else if (Decider < 85) // Ask Advice: 20%
                                {
                                    MType = "ask_advice";
                                    IDsICanThank.Add(ChosenArchitect.ID);
                                    ChosenArchitect.IDsICanApologizeTo.Add(ChosenArchitect.ID);
                                }
                                else if (Decider < 95) // Tell Story: 10%
                                {
                                    MType = "tell_story_about";
                                    IDsICanThank.Add(ChosenArchitect.ID);
                                }
                                else // Compliment: 5%
                                {
                                    MType = "compliment";
                                }

                                IDsICanThank = IDsICanThank.Distinct().ToList();

                                //if you're asking news, history, or telling a story don't include domains. otherwise do.

                                EntityList<Entity> AllImportantEntities;

                                if (!new List<string>() { "tell_story_about", "ask_news", "ask_history" }.Contains(MType) && Game1.GameWorld.rnd.Next(2) == 0)
                                {
                                    AllImportantEntities = Game1.AllImportantEntities_Domains;
                                }
                                else
                                {
                                    AllImportantEntities = Game1.AllImportantEntities_Locations;
                                }


                                CommandProcessor.SendMessage(MType, this, ChosenArchitect, new EntityList<Entity> { AllImportantEntities[Game1.GameWorld.rnd.Next(AllImportantEntities.Count())] }, Game1.GameWorld);

                                if (new List<string> { "tell_story_about", "ask_news", "ask_history" }.Contains(MType))
                                {
                                    var architect = ArchitectsToUse.FirstOrDefault(a =>
                                        Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a));

                                    if (architect != null)
                                    {
                                        int Year = (int)Math.Round((decimal)(Game1.GameWorld.Cycle / 290304000), MidpointRounding.ToZero);
                                    }
                                }

                            }
                        }
                    }
                }
                else if (MessageCooldown > 0)
                {
                    MessageCooldown--;
                }

                //THIS IS SPECIFICALLY FOR FINDING A NEW TARGET.

                // Check for potential targets
                Architect killCandidate = null;
                Architect disableCandidate = null;

                if (Room != null)
                {
                    foreach (Architect a in Room.Architects)
                    {
                        if (GetOpinion(a.ArchitectILookLike) < -100 && (!a.InvisibleCheck || IsConstruct))
                        {
                            killCandidate = a;
                        }
                        else if (GetOpinion(a.ArchitectILookLike) < -50 && (!a.InvisibleCheck || IsConstruct))
                        {
                            disableCandidate = a;
                        }
                    }
                }
                else
                {
                    foreach (Architect a in Block.Architects)
                    {
                        if (GetOpinion(a.ArchitectILookLike) < -100 && (!a.InvisibleCheck || IsConstruct))
                        {
                            killCandidate = a;
                        }
                        else if (GetOpinion(a.ArchitectILookLike) < -50 && (!a.InvisibleCheck || IsConstruct))
                        {
                            disableCandidate = a;
                        }
                    }
                }


                if(!Game1.LoadedHooks.Contains(this))
                {
                    // If low level and would've reacted, escape instead
                    if (this.Level <= 1 && (killCandidate != null || disableCandidate != null) && !Game1.GameWorld.WildRaces.Contains(Race) && Game1.CombatSimActive == false)
                    {
                        Task = "escape";
                        CyclesLeftInTask = 1000;
                    }

                    // Set actual targets if not escaping
                    else if (killCandidate != null)
                    {
                        Task = "killtarget";
                        CyclesLeftInTask = 1000;
                        TargetArchitect = killCandidate;
                    }
                    else if (disableCandidate != null)
                    {
                        Task = "disabletarget";
                        CyclesLeftInTask = 1000;
                        TargetArchitect = disableCandidate;
                    }
                }


                //lose track of invisible targets

                if(!IsConstruct)
                {
                    if (((ulong)Math.Round(Game1.GameWorld.Cycle)) % 10 == 0 && TargetArchitect != null && TargetArchitect.InvisibleCheck && !BlindCheck)
                    {
                        AnnounceToParty(this.ReferredToNames[0] + " loses track of " + TargetArchitect.ReferredToNames[0] + "!", Color.Magenta, new EntityList<Entity>() { this, TargetArchitect });
                        TargetArchitect = null;
                        Task = "";
                    }
                }

                string AlternateMove = "";


                // Set task if you need to, or copy your leader's task :)
                if (Group != null && Group.Leader.Loaded && Group.Leader.Task != "")
                {
                    Task = Group.Leader.Task;
                    Target = Group.Leader.Target;
                    TargetArchitect = Group.Leader.TargetArchitect;
                    TargetObject = Group.Leader.TargetObject;
                    CyclesLeftInTask = Group.Leader.CyclesLeftInTask;
                }
                else if (!TutorialSickness && Game1.CombatSimActive == false && Task == "" && BlindCycles == 0 && Game1.GameWorld.SettlementTypes.Contains(this.Location.Type) && Race != Game1.GameWorld.GetRace("debtshiba") && !Bound && !Game1.LoadedHooks.Contains(this) /*no games if you're a quest dummy*/)
                {
                    if (IsLoadedTrader && DaysSinceLiquid < 2 && DaysSinceFood < 2)
                    {
                        Task = "vacantfortrade";
                        CyclesLeftInTask = 600;
                        Target = Block.FindNearestThing("market");
                    }
                    else
                    {
                        if (Profession == "druidcrafter" || (Profession == "gardener" && Game1.GameWorld.rnd.Next(3) == 0))
                        {
                            Task = "druidcrafting";
                        }
                        else if (DaysSinceLiquid > 0 && Task != "drinking")
                        {
                            Task = "drinking";
                        }
                        else if (DaysSinceCoffeeOrTea > 0 && Task != "drinkingcaffeine")
                        {
                            Task = "drinkingcaffeine";
                        }
                        else if (DaysSinceFood > 0 && Task != "eating")
                        {
                            Task = "eating";
                        }
                        else if (NightsSinceSleep > 0 && Task != "sleeping")
                        {
                            Task = "sleeping";
                        }
                        else if (Group == Location.Government || this == Location.Government || this == Location.HomeCivilization.Alpha)
                        {
                            Task = "discussion";
                        }
                        else if (Profession == "scholar")
                        {
                            Task = "study";
                        }
                        else if (DaysSinceSocialized > 0 && Task != "socializing")
                        {
                            Task = "socializing";
                        }
                        else if ((DaysSincePerforming > 5 || (Profession == "musician" && DaysSincePerforming > 0)) && Task != "performmusic" && Task != "performdance" && Task != "performtheater" && Task != "performpoetry")
                        {
                            Task = new List<string>() { "performmusic", "performpoetry" }[Game1.GameWorld.rnd.Next(2)];
                        }
                        else if (Game1.GameWorld.rnd.Next(1, 5) == 1)
                        {
                            Task = "industry";
                        }
                        else if (Game1.GameWorld.rnd.Next(1, 4) == 1 && NearestTavernThisLoad != (null, null, null, null))
                        {
                            // Pick random tavern task
                            int TaskDecider = Game1.GameWorld.rnd.Next(0, 30);

                            if (TaskDecider == 0)
                            {
                                Task = new List<string>() { "performmusic", "performpoetry" }[Game1.GameWorld.rnd.Next(2)];
                            }
                            else if (TaskDecider < 7)
                            {
                                Task = "socializing";
                            }
                            else if (TaskDecider < 12)
                            {
                                Task = "drinking";
                            }
                            else if (TaskDecider < 15)
                            {
                                Task = "drinkingcaffeine";
                            }
                            else if (TaskDecider < 20)
                            {
                                Task = "eating";
                            }
                            else if (TaskDecider < 25)
                            {
                                Task = "waitforgame";
                            }
                            else
                            {
                                Task = "contemplate";
                                CurrentContemplationTopic = Game1.GameWorld.Domains[Game1.GameWorld.rnd.Next(Game1.GameWorld.Domains.Count)].Name;
                            }
                        }
                        else
                        {
                            // Pick random general task
                            int TaskDecider = Game1.GameWorld.rnd.Next(0, 30);

                            if (TaskDecider < 7)
                            {
                                Task = "socializing";
                            }
                            else if (TaskDecider < 12)
                            {
                                Task = "drinking";
                            }
                            else if (TaskDecider < 15)
                            {
                                Task = "drinkingcaffeine";
                            }
                            else if (TaskDecider < 25)
                            {
                                Task = "eating";
                            }
                            else
                            {
                                Task = "contemplate";
                                CurrentContemplationTopic = Game1.GameWorld.Domains[Game1.GameWorld.rnd.Next(Game1.GameWorld.Domains.Count)].Name;
                            }
                        }

                        // Assign target and CyclesLeftInTask based on the chosen task
                        switch (Task)
                        {
                            case "vacantfortrade":
                                Target = Block.FindNearestThing("market");
                                CyclesLeftInTask = 600; //spend a minute before deciding if you want to do something again
                                break;
                            case "druidcrafting":
                                Target = (Location, District, Block, Room);
                                CyclesLeftInTask = 300; //druidcraft for 30 seconds
                                break;
                            case "drinking":
                                Target = Game1.GameWorld.rnd.Next(1, 3) == 1 ? Block.FindNearestThing("well") : Block.FindRandomThingInCurrentDistrict("tavern");
                                CyclesLeftInTask = 300; // drinking a glass or bucketish cup water takes roughly around 30 seconds
                                break;
                            case "drinkingcaffeine":
                                Target = Block.FindRandomThingInCurrentDistrict("tavern");
                                CyclesLeftInTask = 500; // savor caffeine a bit, though. also its hote.
                                break;
                            case "eating":
                                Target = Game1.GameWorld.rnd.Next(0, 3) == 1 ? (Location, District, District.DistrictMap[Game1.GameWorld.rnd.Next(49)], null) : Block.FindRandomThingInCurrentDistrict("tavern");
                                CyclesLeftInTask = 500; // this takes longer for a similar reason, also its food.
                                break;
                            case "sleeping":
                                Target = Block.FindRandomThingInCurrentDistrict("house");
                                CyclesLeftInTask = 350000;
                                break;
                            case "waitforgame":
                                var newTarget = Block.FindRandomThingInCurrentDistrict("tavern");

                                Target = newTarget;

                                if (newTarget.Item4 != null && newTarget.Item4.Structure.Rooms.Count > 0)
                                {
                                    Target = (newTarget.Item1, newTarget.Item2, newTarget.Item3, newTarget.Item4.Structure.Rooms[0]);
                                }

                                CyclesLeftInTask = 1000;
                                break;

                            case "discussion":
                                Target = Block.FindNearestThing("prism");
                                CyclesLeftInTask = 500; //chat chat chat chat
                                break;
                            case "study":
                                Target = Block.FindNearestThing("library");
                                CyclesLeftInTask = 3000; //takes five minutes at minimum
                                AssignStudyTopic();
                                break;
                            case "socializing":
                                Target = Game1.GameWorld.rnd.Next(0, 4) != 1 ? Block.FindRandomThingInCurrentDistrict("tavern") : Block.FindNearestThing("well");
                                CyclesLeftInTask = 300; //conversations don't last too long, but I wnat them going in and out often if its the well.
                                break;
                            case "performmusic":
                            case "performdance":
                            case "performtheater":
                            case "performpoetry":
                                Target = Game1.GameWorld.rnd.Next(1, 3) == 1 ? Block.FindRandomThingInCurrentDistrict("tavern") : Block.FindNearestThing("well");
                                CyclesLeftInTask = 500;
                                break;
                            case "industry":
                                Target = Game1.GameWorld.rnd.Next(0, 3) == 1 ? (Location, District, District.DistrictMap[Game1.GameWorld.rnd.Next(49)], null) : Block.FindRandomThingInCurrentDistrict("house");
                                CyclesLeftInTask = 300; //one single instance might take half a minute
                                break;
                            case "contemplate":
                                Target = Game1.GameWorld.rnd.Next(0, 3) == 1 ? (Location, District, District.DistrictMap[Game1.GameWorld.rnd.Next(49)], null) : Block.FindNearestThing("well");
                                CyclesLeftInTask = 300; //stare off into the sunset for about a minute.
                                break;
                        }

                        // Additional check to ensure Target is in the same district
                        if (Target.Item2 != this.District)
                        {
                            Target = Block.FindNearestThing("structure");
                        }

                        CyclesLeftInTask = Math.Max(CyclesLeftInTask + Game1.GameWorld.rnd.Next(-50, 51), 50);
                    }
                }



                if ((Task == "disabletarget" || Task == "killtarget") && TargetArchitect != null && ShieldTokens.Contains(TargetArchitect) && TargetArchitect.Energy < 60)
                {
                    AnnounceToParty(ReferredToNames[0] + " has defeated his opponent, proclaiming victory.", Color.DeepPink, new EntityList<Entity>() { this });
                    ShieldTokens.Remove(TargetArchitect);
                    TargetArchitect.ShieldTokens.Remove(this);
                    Task = "";
                    SetOpinion(TargetArchitect.ArchitectILookLike, 0);
                    CyclesLeftInTask = 0;
                    TargetArchitect.Task = "";
                    TargetArchitect.CyclesLeftInTask = 0;
                    TargetArchitect = null;
                }
                else if ((Task == "disabletarget" || Task == "killtarget") && TargetArchitect != null && TargetArchitect.ShieldTokens.Contains(this) && Energy < 60)
                {
                    AnnounceToParty(ReferredToNames[0] + ": Okay! You win!", Color.DeepPink, new EntityList<Entity>() { this });
                    ShieldTokens.Remove(TargetArchitect);
                    TargetArchitect.ShieldTokens.Remove(this);
                    Task = "";
                    SetOpinion(TargetArchitect.ArchitectILookLike, 0);
                    CyclesLeftInTask = 0;
                    TargetArchitect.Task = "";
                    TargetArchitect.CyclesLeftInTask = 0;
                    TargetArchitect = null;
                }



                if (Task == "killtarget" && TargetArchitect != null)
                {
                    //update the targetting system so you go to where they are naturally
                    Target = (TargetArchitect.Location, TargetArchitect.District, TargetArchitect.Block, TargetArchitect.Room);
                }
                if (Task == "disabletarget" && TargetArchitect != null)
                {
                    Target = (TargetArchitect.Location, TargetArchitect.District, TargetArchitect.Block, TargetArchitect.Room);
                }
                
                if (Task == "escape")
                {
                    Location l = Game1.GameWorld.AllLocations[Game1.GameWorld.rnd.Next(Game1.GameWorld.AllLocations.Count)];
                    District d = l.Districts[Game1.r.Next(l.Districts.Count)];
                    Block b = d.DistrictMap[Game1.r.Next(49)];

                    Target = (l, d, b, null);
                    MovementMode = "sprinting";
                }

                //have you finished task? then reap the benefits
                if (CyclesLeftInTask <= 0 && (Location, District, Block, Room) == Target)
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
                            Perform("song");
                            break;
                        case "performpoetry":
                            DaysSincePerforming = 0;
                            Perform("poem");
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
                            EntityList<Object> generatedItems = new EntityList<Object>();

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



                //but nope if you're bound you're bound.

                if (Bound)
                {
                    Task = "bound";
                    CyclesLeftInTask = 9999999;
                    Target = (Location, District, Block, Room);
                }



                //otherwise, youre at the right place so either kill someone or finish your task
                else if (Task != "" && (Location, District, Block, Room) == Target)
                {
                    string DetermineAttackVerb(string weaponType)
                    {

                        var verbMappings = new Dictionary<string, List<string>>
                        {
                            {"slashing", new List<string> {"slash", "slice"}},
                            {"piercing", new List<string> {"pierce", "stab"}},
                            {"bashing", new List<string> {"bash", "crush"}},
                            {"thrashing", new List<string> {"scourge", "lash", "thrash"}}
                        };

                        if (verbMappings.ContainsKey(weaponType))
                        {
                            var verbs = verbMappings[weaponType];
                            return verbs[Game1.GameWorld.rnd.Next(verbs.Count())]; // Randomly select a verb
                        }

                        return "attack"; // Default verb if weapon type is not found
                    }

                    if (Task == "killtarget" || Task == "disabletarget")
                    {
                        bool CastedASpell = false;
                        int SpellCastingDistance = 3;

                        // Now, you can use ArchitectsToUse to check for proximity, interactions, etc.

                        if(TargetArchitect != null && TargetArchitect.IsAlive == false)
                        {
                            TargetArchitect = null;
                        }
                        else if (TargetArchitect != null && ArchitectsToUse.Contains(TargetArchitect)) // Check if the target is in the same area
                        {
                            List<string> OffensiveSpells = new List<string> { "water bolt", "chaos flare", "flash flame", "ice shock" };

                            if (Game1.GameWorld.rnd.Next(50) == 1)
                            {
                                AnnounceToParty(ReferredToNames[0] + " repositions!", Color.MediumPurple, new EntityList<Entity>() { this });

                                CooldownCycles += (int)Math.Round(3 * Speed);

                                foreach (Object o in BodyParts)
                                {
                                    var textStorage = o.UpdateExposure(-(25 + Dexterity));
                                    if (textStorage != null)
                                    {
                                        AnnounceToParty(textStorage.Data, textStorage.Color, textStorage.Entities);
                                    }
                                }
                            }




                            if ((SpellsKnown.Count() > 0 && Game1.GameWorld.rnd.Next(2) == 0) || Profession == "sorcerer" || Profession == "warlock" || Race == Game1.GameWorld.GetRace("debtshiba"))
                            {
                                EntityList<Entity> offensiveSpellsInKit;

                                if (Race == Game1.GameWorld.GetRace("debtshiba"))
                                {
                                    offensiveSpellsInKit = SpellsKnown.Where(spell => OffensiveSpells.Contains(spell.Metadata));
                                }
                                else
                                {
                                    offensiveSpellsInKit = SpellsKnown.Where(spell => OffensiveSpells.Contains(spell.Metadata));
                                }

                                if (offensiveSpellsInKit.Any() && GetDistance(TargetArchitect) <= SpellCastingDistance)
                                {
                                    for (int i = SpellcastingPower; i != 0; i--)
                                    {
                                        Entity spellToCast = offensiveSpellsInKit[Game1.GameWorld.rnd.Next(offensiveSpellsInKit.Count())];
                                        CastedASpell = true;

                                        List<TextStorage> text = CastSpell(spellToCast.Metadata, new EntityList<Entity>() { TargetArchitect });

                                        foreach(TextStorage t in text)
                                        {
                                            AnnounceToParty(t.Data, t.Color, t.Entities);
                                        }
                                    }
                                }
                            }

                            if (!CastedASpell) // If no spell was cast, try a melee attack
                            {
                                if (Powers.Count > 0 && Game1.GameWorld.rnd.Next(4) != 0)
                                {
                                    //use epic ability
                                    EntityList<Object> objects = Room != null ? Room.Objects : Block.Objects;

                                    CooldownCycles += (int)Math.Round(25 / Speed);

                                    string Ability = Race.Powers[Game1.GameWorld.rnd.Next(Race.Powers.Count())];

                                    EntityList<Architect> nearbyArchitects = this.Room != null
                                    ? this.Room.Architects
                                    : this.Block != null
                                        ? this.Block.Architects
                                        : new EntityList<Architect>();

                                    bool anyPartyArchitectsPresent = nearbyArchitects
                                        .Any(a => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a));

                                    if (anyPartyArchitectsPresent)
                                    {
                                        Game1.GameWorld.GamePlayerAssociation.ActiveParty.RoomsUnsearched.Clear();

                                        foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                                        {
                                            a.CancelSearch();
                                            a.TryComment("abilitysee", 15);
                                        }
                                    }



                                    if (Ability == "energybolts")
                                    {
                                        for (int i = Game1.GameWorld.rnd.Next(2, 5); i != 0; i--)
                                        {
                                            Object o = new Object(null, "bolt", new EntityList<Material>() {Game1.GameWorld.Energy }, this);
                                            objects.Add(o);
                                            o.Owner = this;
                                            o.Thrower = this; //whatever...
                                            o.AirborneTarget = TargetArchitect;

                                            o.IsConstructBolt = true;

                                            if(TargetArchitect.Invocations.Contains("siphon"))
                                            {
                                                TargetArchitect.AnnounceToParty(TargetArchitect.ReferredToNames[0] + " heals some energy from the power!", Color.Green, new EntityList<Entity>() { TargetArchitect });
                                                TargetArchitect.Energy += 5;
                                            }

                                            o.AirborneCyclesToHitTarget = 15 - Focus;
                                            AnnounceToParty(ReferredToNames[0] + " fires a bolt at " + TargetArchitect.ReferredToNames[0] + "!", Color.Magenta, new EntityList<Entity>() { this, TargetArchitect });
                                        }

                                        if (PlaySound)
                                            Game1.SFX.Add(Game1.StarStrike);
                                    }
                                    else if (Ability == "cloaking")
                                    {
                                        CloakCycles += Game1.GameWorld.rnd.Next(5, 15);
                                        AnnounceToParty(ReferredToNames[0] + " partially phases out of reality!", Color.Magenta, new EntityList<Entity>() { this });

                                        if (PlaySound)
                                            Game1.SFX.Add(Game1.Invisibility);
                                    }
                                    else if (Ability == "magneticfield")
                                    {
                                        AnnounceToParty(ReferredToNames[0] + " radiates magnetic energy!", Color.Magenta, new EntityList<Entity>() { this });
                                        foreach (Object o in objects)
                                        {
                                            if (o.Owner != this)
                                            {
                                                if(o.AirborneTarget != null)
                                                {
                                                    AnnounceToParty(o.ReferredToNames[0] + " falls to the ground!", Color.Magenta, new EntityList<Entity>() { o });
                                                }

                                                o.AirborneTarget = null;
                                                o.AirborneCyclesToHitTarget = 0;
                                                o.AirbornePower = 0;
                                            }
                                        }
                                        if (PlaySound)
                                            Game1.SFX.Add(Game1.Hold);
                                    }
                                    else if (Ability == "shockwave")
                                    {
                                        AnnounceToParty(ReferredToNames[0] + " radiates an explosive shockwave!", Color.Magenta, new EntityList<Entity>() { this });
                                        foreach (Architect a in ArchitectsToUse)
                                        {
                                            if (!a.Race.Name.EndsWith("guardian") && a.HomeLocation != this.Location)
                                            {
                                                AnnounceToParty(a.ReferredToNames[0] + " is destabilized!", Color.Magenta, new EntityList<Entity>() { a });
                                                a.DestabilizedCycles += Game1.GameWorld.rnd.Next(10, 20);


                                                if (a.Invocations.Contains("siphon"))
                                                {
                                                    a.AnnounceToParty(a.ReferredToNames[0] + " heals some energy from the power!", Color.Green, new EntityList<Entity>() { a });
                                                    a.Energy += 5;
                                                }
                                            }
                                        }
                                        if (PlaySound)
                                            Game1.SFX.Add(Game1.Tremor);
                                    }
                                    else if (Ability == "slowray")
                                    {
                                        AnnounceToParty(ReferredToNames[0] + " fires an array of brilliant white beams!", Color.Magenta, new EntityList<Entity>() { this });
                                        foreach (Architect a in ArchitectsToUse)
                                        {
                                            if (!a.Race.Name.EndsWith("guardian") && a.HomeLocation != this.Location)
                                            {
                                                AnnounceToParty(a.ReferredToNames[0] + " is frozen temporarily!", Color.Magenta, new EntityList<Entity>() { this });
                                                a.HoldCycles += Game1.GameWorld.rnd.Next(2, 5);

                                                if (a.Invocations.Contains("siphon"))
                                                {
                                                    a.AnnounceToParty(a.ReferredToNames[0] + " heals some energy from the power!", Color.Green, new EntityList<Entity>() { a });
                                                    a.Energy += 5;
                                                }
                                            }
                                        }

                                        if (PlaySound)
                                            Game1.SFX.Add(Game1.Immortalize);
                                    }
                                    else if (Ability == "pulsebash")
                                    {
                                        AnnounceToParty(ReferredToNames[0] + " charges up a devastating energy bash!", Color.Magenta, new EntityList<Entity>() { this });
                                        PulseCharge += 1;

                                        if (PlaySound)
                                            Game1.SFX.Add(Game1.EvokeSpark);

                                        if (PulseCharge == 5)
                                        {
                                            PulseCharge = 0;
                                            AnnounceToParty(TargetArchitect.ReferredToNames[0] + " pulse bashes " + TargetArchitect.ReferredToNames[0] + "!", Color.Magenta, new EntityList<Entity>() { this });
                                            TargetArchitect.DestabilizedCycles += 200;
                                            TargetArchitect.Energy -= 16;
                                            if (Energy <= 0)
                                            {
                                                DeathCause = "was pulse bashed by a construct";
                                            }


                                            if (TargetArchitect.Invocations.Contains("siphon"))
                                            {
                                                TargetArchitect.AnnounceToParty(TargetArchitect.ReferredToNames[0] + " heals some energy from the power!", Color.Green, new EntityList<Entity>() { TargetArchitect });
                                                TargetArchitect.Energy += 5;
                                            }

                                            if (PlaySound)
                                                Game1.SFX.Add(Game1.Block);
                                        }
                                    }
                                    else if (Ability == "harvest")
                                    {
                                        AnnounceToParty(ReferredToNames[0] + " siphons energy from " + TargetArchitect.ReferredToNames[0] + " with a translucent beam!", Color.Magenta, new EntityList<Entity>() { this, TargetArchitect });
                                        TargetArchitect.Energy -= 4;



                                        if (TargetArchitect.Invocations.Contains("siphon"))
                                        {
                                            TargetArchitect.AnnounceToParty(TargetArchitect.ReferredToNames[0] + " heals some energy from the power!", Color.Green, new EntityList<Entity>() { TargetArchitect });
                                            TargetArchitect.Energy += 4;
                                        }


                                        if (PlaySound)
                                            Game1.SFX.Add(Game1.ConjureSpark);

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

                                    if (mainHand != null && mainHand.IsWeapon && offHand != null && offHand.IsWeapon)
                                    {
                                        // Both are weapons, pick one randomly
                                        Weapon = Game1.GameWorld.rnd.Next(2) == 0 ? mainHand : offHand;
                                    }
                                    else if (mainHand != null && mainHand.IsWeapon)
                                    {
                                        Weapon = mainHand;
                                    }
                                    else if (offHand != null && offHand.IsWeapon)
                                    {
                                        Weapon = offHand;
                                    }
                                    else
                                    {
                                        Weapon = BodyParts[Game1.GameWorld.rnd.Next(BodyParts.Count())]; // Assuming unarmed combat uses body parts as weapons
                                    }

                                    if (Game1.TutorialActive || ((Weapon.WeaponMaximumRange + (Invocations.Contains("slashing") ? 2 : 0)) >= GetDistance(TargetArchitect) && Math.Abs(TargetArchitect.YLevelInFeet - YLevelInFeet) <= 5))
                                    {
                                        string attackVerb = DetermineAttackVerb(Weapon.DamageType);

                                        // Define base likelihood for each body part
                                        int baseLikelihood = 25;

                                        // Determine if we're using piercing or bashing
                                        bool isPiercingOrBashing = Weapon.DamageType == "piercing" || Weapon.DamageType == "bashing";

                                        // Pre-calculate likelihoods
                                        List<(Object bp, int likelihood)> partChances = new();

                                        foreach (var bodyPart in TargetArchitect.BodyParts)
                                        {
                                            int exposureBonus = bodyPart.Exposure;

                                            int integrityPenalty = 0;
                                            if (isPiercingOrBashing)
                                            {
                                                // Scale penalty so low integrity increases chance of hit
                                                // e.g., 100 - Integrity gives higher weight to damaged parts
                                                integrityPenalty = (100 - bodyPart.Integrity) / 2; // Adjust divisor to tune impact
                                            }

                                            int totalLikelihoodForPart = baseLikelihood + exposureBonus + integrityPenalty;
                                            partChances.Add((bodyPart, totalLikelihoodForPart));
                                        }

                                        // Calculate total weight
                                        int totalLikelihood = partChances.Sum(pc => pc.likelihood);

                                        // Random selection
                                        int randomValue = Game1.GameWorld.rnd.Next(totalLikelihood);
                                        int cumulativeLikelihood = 0;
                                        Object targetBodyPart = TargetArchitect.BodyParts.First();

                                        foreach (var pc in partChances)
                                        {
                                            cumulativeLikelihood += pc.likelihood;
                                            if (randomValue < cumulativeLikelihood)
                                            {
                                                targetBodyPart = pc.bp;
                                                break;
                                            }
                                        }


                                        ReturningAttack = new Attack(attackVerb, this, targetBodyPart, Weapon);
                                    }
                                    else if (GetDistance(TargetArchitect) > Weapon.WeaponMaximumRange + (Invocations.Contains("slashing") ? 2 : 0))
                                    {
                                        // Move closer due to distance being too great
                                        ModifyDistance(TargetArchitect, -2);

                                        CooldownCycles += (int)(15 / Math.Round(Speed));

                                        if (PlaySound)
                                        {
                                            StepSound(2);
                                        }

                                        AnnounceToParty(ReferredToNames[0] + " gets closer to " + TargetArchitect.ReferredToNames[0] + "!",
                                            Color.DarkMagenta, new EntityList<Entity>() { this, TargetArchitect });
                                    }
                                    else if (Math.Abs(TargetArchitect.YLevelInFeet - YLevelInFeet) > 5)
                                    {
                                        bool trythrow = false; 

                                        // Target is within weapon range horizontally, but too high or too low vertically
                                        if (TargetArchitect.YLevelInFeet > YLevelInFeet )
                                        {
                                            if(Room == null && OnTopOfStructure == null && TargetArchitect.OnTopOfStructure != null)
                                            {
                                                CommandProcessor.RunCommand(this, "climb", new List<Entity>() { TargetArchitect.OnTopOfStructure }, Game1.LoadedArchitects, Game1.GameWorld, Game1.r, "climb");
                                            }
                                            else
                                            {
                                                trythrow = true;
                                            }
                                        }
                                        else if (TargetArchitect.YLevelInFeet < YLevelInFeet)
                                        {
                                            if (Room == null && OnTopOfStructure != null && TargetArchitect.OnTopOfStructure == null)
                                            {
                                                CommandProcessor.RunCommand(this, "get_down", new List<Entity>() { }, Game1.LoadedArchitects, Game1.GameWorld, Game1.r, "get down");
                                            }
                                            else
                                            {
                                                trythrow = true;
                                            }
                                        }

                                        if(trythrow)
                                        {
                                            Object ThrowObject = null;

                                            if(OffHeldObject != null)
                                            {
                                                ThrowObject = OffHeldObject;
                                                OffHeldObject = null;
                                            }
                                            else if (Inventory.Count > 0)
                                            {
                                                ThrowObject = Inventory[0];
                                                Inventory.Remove(ThrowObject);
                                            }
                                            else if (Clothing.Where(o => o.Materials[0].Type == "metal").Count > 0)
                                            {
                                                ThrowObject = Clothing.Last(o => o.Type != "undergarment" && o.Type == "brassiere");
                                                CommandProcessor.RunCommand(this, "remove_worn_item", new List<Entity>() {ThrowObject}, Game1.LoadedArchitects, Game1.GameWorld, Game1.r, "doff");
                                            }

                                            if(ThrowObject != null)
                                            {
                                                CommandProcessor.RunCommand(this, "throw_item_at", new List<Entity>() { ThrowObject, TargetArchitect }, Game1.LoadedArchitects, Game1.GameWorld, Game1.r, "throw");
                                            }
                                        }
                                    }

                                }
                            }
                        }
                        else if (TargetArchitect != null)
                        {
                            Target = (TargetArchitect.Location, TargetArchitect.District, TargetArchitect.Block, TargetArchitect.Room);
                        }
                    }
                    
                    
                    
                    //sepoarating bc its like yk dude
                    
                    
                    
                    if (Task == "sentinel" || ((Task == "killtarget" || Task == "disabletarget") && ( this == Game1.GameWorld.Hypernexus || this == Game1.GameWorld.Icosidodecahedron || this == Game1.GameWorld.Shadeheart)))
                    {
                        // Initialize a new list for ArchitectsToUse
                        List<Architect> ArchitectsToScan = new List<Architect>();
                        ArchitectsToScan.AddRange(ArchitectsToUse);

                        if (this.Room != null)
                        {
                            ArchitectsToScan.AddRange(this.Room.Structure.Block.Architects);
                            ArchitectsToScan = ArchitectsToScan.Distinct().ToList(); // Ensure uniqueness
                        }

                        if (((ulong)Math.Round(Game1.GameWorld.Cycle)) % 15 == 1)
                        {
                            foreach (Architect a in ArchitectsToScan)
                            {
                                if (a.Race != a.Location.PrimaryRace && a != this)
                                {
                                    bool killThemPlease = false;

                                    if (Race.Name == "icosidodecahedron")
                                    {
                                        if ((a.Inventory.Any(item => item.Owner == this) || a.IsofractalThief))
                                        {
                                            a.IsofractalThief = true;
                                            killThemPlease = true;
                                        }
                                        else if (Task == "killtarget" && TargetArchitect == a)
                                        {
                                            killThemPlease = true;
                                        }
                                    }
                                    else
                                    {
                                        killThemPlease = true;
                                    }

                                    if (killThemPlease)
                                    {
                                        // Spawn sentinel architects
                                        int count = Game1.GameWorld.rnd.Next(1, 3);

                                        AnnounceToParty(this.ReferredToNames[0] + " erupts sentinels from an ancient era!", Color.OrangeRed, new EntityList<Entity>());

                                        if (PlaySound)
                                            Game1.SFX.Add(Game1.ConjureSpark);

                                        for (int i = 0; i < count; i++)
                                        {
                                            Architect A = new Architect("", "male", Location.PrimaryRace, 0, "sentinel", new EntityList<Object>(), Location, District, Block, "none", 4, false);
                                            A.Name = Game1.GameWorld.GenerateUniqueArchitectName(A);

                                            A.PopulateSelf(false);

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

                                            A.Task = "killtarget";
                                            A.TargetArchitect = a;
                                            A.Target = (a.Location, a.District, a.Block, a.Room);
                                            A.CyclesLeftInTask = 500;

                                            A.Transient = true;

                                            A.UpdateNames();

                                            Game1.LoadedArchitects.Add(A);
                                        }

                                        // Exit the loop to avoid spawning more sentinels
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    else if (Task == "druidcrafting")
                    {
                        if (CyclesLeftInTask % 10 == 1)
                        {
                            EntityList<Entity> targetEntities = new EntityList<Entity>();
                            string spellName = "emergent growth";

                            if (this.Block.Objects.Count() > 0)
                            {
                                int randomIndex = Game1.GameWorld.rnd.Next(this.Block.Objects.Count());
                                targetEntities.Add(this.Block.Objects[randomIndex]);
                            }
                            else if (this.Block.Architects.Count() > 0)
                            {
                                int randomIndex = Game1.GameWorld.rnd.Next(this.Block.Architects.Count());
                                targetEntities.Add(this.Block.Architects[randomIndex]);
                            }

                            if (targetEntities.Count() > 0)
                            {
                                List<TextStorage> spellResults = CastSpell(spellName, targetEntities);
                                foreach (var result in spellResults)
                                {
                                    AnnounceToParty(result.Data, result.Color, result.Entities);
                                }
                            }
                        }
                    }
                    else if (Task == "waitforgame")
                    {
                        if (Game1.GameWorld.Games.Count != 0)
                        {
                            foreach (Architect a in architects)
                            {
                                if (a != this && a.Task == "waitforgame")
                                {
                                    Boardgame b = new Boardgame(Game1.GameWorld.Games[Game1.GameWorld.rnd.Next(Game1.GameWorld.Games.Count)], new EntityList<Architect>() { a, this }, Game1.GameWorld.rnd.Next(5, 15));

                                    this.Task = "playgame";
                                    this.CyclesLeftInTask = 90000;
                                    this.CurrentlyParticipatingGame = b;

                                    a.Task = "playgame";
                                    a.CyclesLeftInTask = 90000;
                                    a.CurrentlyParticipatingGame = b;

                                    break;
                                }
                            }
                        }
                        else
                        {
                            CyclesLeftInTask = 0;
                        }
                    }
                    else if (Task == "playgame")
                    {
                        if(CurrentlyParticipatingGame != null)
                        {
                            if (CyclesLeftInTask % 50 == 0)
                            {
                                StringBuilder ThrowawayGarbage = new StringBuilder();

                                // Progress one round of the game
                                string turnResult = CurrentlyParticipatingGame.SimulateTurn(ThrowawayGarbage);  // Capture the result of the turn

                                // Announce the turn result to the party
                                this.AnnounceToParty(turnResult, Color.Orange, new EntityList<Entity>() { this });

                                // Check if the game has concluded
                                if (CurrentlyParticipatingGame.CurrentTurn >= CurrentlyParticipatingGame.MaxTurns)
                                {
                                    var winner = CurrentlyParticipatingGame.playerPoints.OrderByDescending(kvp => kvp.Value).First().Key;

                                    foreach (Architect a in CurrentlyParticipatingGame.Players)
                                    {
                                        a.Task = "";
                                        a.CyclesLeftInTask = 0;
                                        a.CurrentlyParticipatingGame = null;
                                    }
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


                if(!TutorialSickness)
                {
                    //all movement

                    if (Task != "" && Target != (Location, District, Block, Room) && Target != (null, null, null, null) && BlindCycles == 0)
                    {
                        if (Room != null && (Target.Item4 == null || Target.Item4.Structure != Structure || Target.Item1 != Location || Target.Item2 != District))
                        {
                            //oh crap im in a building, but I don't want to be in this one. I should find the exit.

                            if (Room.Structure.Rooms.IndexOf(Room) == 0)
                            {
                                AlternateMove = "leavebuilding";
                            }
                            else
                            {
                                AlternateMove = "findexit";
                            }
                        }
                        else if (Room == null && (Location != Target.Item1 || District != Target.Item2))
                        {
                            //leave if it makes sense to

                            int distWest = Block.X;
                            int distEast = 6 - Block.X;
                            int distNorth = Block.Z;
                            int distSouth = 6 - Block.Z;

                            int minDist = Math.Min(Math.Min(distWest, distEast), Math.Min(distNorth, distSouth));

                            string direction = minDist switch
                            {
                                var d when d == distWest => "west",
                                var d when d == distEast => "east",
                                var d when d == distNorth => "north",
                                var d when d == distSouth => "south",
                                _ => "none"
                            };

                            // Always keep trying to move in that direction,
                            // even after you're already at the edge
                            if (direction != "none")
                            {
                                Move(direction);
                            }
                        }


                        else if (Room == null && Target.Item3 != null && Block != Target.Item3)
                        {
                            //I have a block I'm supposed to go to, but its not my current block. I should be outside of a structure/room when I do this.

                            // Calculate direction based on angle
                            int deltaX = Target.Item3.X - Block.X;
                            int deltaZ = Target.Item3.Z - Block.Z;

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
                        else if (Room == null && Target.Item4 != null && Block.Structures.Contains(Target.Item4.Structure))
                        {
                            //i am in the correct block, I am outside, I am not in a structure, and I am not in a room myself. I should enter the structure and go to room 0.

                            AlternateMove = Target.Item4.Structure.Name;
                        }
                        else if (Structure != null && Room != Target.Item4)
                        {
                            //i am inside a structure, I am not in a block. But my room is not correct. I should try to find the door to the room I want to go to.

                            // Find the nearest door to the target room
                            Door doorToTargetRoom = Room.FindQuickestDoorToRoom(Target.Item4);
                            if (doorToTargetRoom != null)
                            {
                                AlternateMove = doorToTargetRoom.ID.ToString(); // Set the alternate move to the door ID
                            }
                        }
                        else
                        {
                            // No movement needed, you're in the right place.
                        }
                    }

                   
                    if (AlternateMove == "leavebuilding")
                    {
                        // Check if there's an exit door in the room
                        var exitDoor = Room?.Objects.FirstOrDefault(o => o.Type == "exit door");

                        if (exitDoor != null)
                        {
                            if (exitDoor.Reinforced)
                            {
                                AnnounceToParty($"{ReferredToNames[0]} bashes the reinforced exit door!", Color.OrangeRed, new EntityList<Entity> { this });

                                PlaySound = false;
                                if (
                                    (Room == null && Block.Architects.Any(t => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(t))) ||
                                    (Room != null && Room.Architects.Any(t => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(t))))
                                {
                                    PlaySound = true;
                                }

                                if (PlaySound)
                                    Game1.SFX.Add(Game1.Craft);

                                exitDoor.Integrity -= 15;
                                CooldownCycles += (int)(Math.Round(30 / Speed));

                                if (exitDoor.Integrity < 0)
                                {
                                    exitDoor.Integrity = 50;
                                    exitDoor.Reinforced = false;

                                    if (exitDoor.Structure != null)
                                    {
                                        exitDoor.Structure.Reinforced = false;
                                        exitDoor.Structure.DoorIntegrity = 50;
                                    }

                                    AnnounceToParty("The reinforced exit door is broken down and is no longer reinforced!", Color.Red, new EntityList<Entity> { this });
                                }
                            }
                            else
                            {
                                CooldownCycles += (int)(Math.Round(25 / Speed));
                                Exit();
                            }
                        }
                    }

                    else if (AlternateMove == "findexit")
                    {
                        Door exitDoor = Room.FindQuickestExitDoor();
                        if (exitDoor != null)
                        {
                            if (exitDoor.Reinforced)
                            {
                                // Bash the reinforced exit door before attempting to leave
                                AnnounceToParty($"{ReferredToNames[0]} bashes the reinforced exit door!", Color.OrangeRed, new EntityList<Entity> { this });

                                if (PlaySound)
                                    Game1.SFX.Add(Game1.Craft);

                                exitDoor.Integrity -= 15;
                                CooldownCycles += (int)(Math.Round(30 / Speed));

                                if (exitDoor.Integrity < 0)
                                {
                                    // Exit door loses reinforcement and resets integrity
                                    exitDoor.Integrity = 50;
                                    exitDoor.Reinforced = false;

                                    // Break structure reinforcement as well
                                    if (exitDoor.Structure != null)
                                    {
                                        exitDoor.Structure.Reinforced = false;
                                        exitDoor.Structure.DoorIntegrity = 50;
                                    }

                                    AnnounceToParty("The reinforced exit door is broken down and is no longer reinforced!", Color.Red, new EntityList<Entity> { this });
                                }
                            }
                            else
                            {
                                // Move through the door as it's not reinforced
                                Room.Architects.Remove(this);
                                Room = exitDoor.DestinationRoom;
                                Room.Architects.Add(this);
                                CooldownCycles += (int)(Math.Round(25 / Speed));

                                if (PlaySound)
                                {
                                    Game1.SFX.Add(Game1.GameWorld.rnd.Next(2) == 0 ? Game1.DoorSound2 : Game1.DoorSound1);
                                    StepSound(1);
                                }
                            }
                        }
                        else
                        {
                            //BUSTIN OUT A HERE WITH THIS ONE
                            Bash();
                        }
                    }
                    else if (AlternateMove != "")
                    {
                        if (int.TryParse(AlternateMove, out int doorId))
                        {
                            MoveThroughDoor(doorId.ToString());
                            AlternateMove = ""; // Clear alternate move after using it
                        }
                        else if (Target.Item4 != null && Target.Item4.Structure.Name == AlternateMove)
                        {
                            if (Target.Item4.Structure.Reinforced)
                            {
                                // Bash the reinforced structure door before attempting to enter
                                AnnounceToParty($"{ReferredToNames[0]} bashes the reinforced structure door!", Color.OrangeRed, new EntityList<Entity> { this });

                                if (PlaySound)
                                    Game1.SFX.Add(Game1.Craft);

                                Target.Item4.Structure.DoorIntegrity -= 20;
                                CooldownCycles += (int)(Math.Round(30 / Speed));

                                if (Target.Item4.Structure.DoorIntegrity <= 0)
                                {
                                    // Reset structure integrity and remove reinforcement
                                    Target.Item4.Structure.DoorIntegrity = 50;
                                    Target.Item4.Structure.Reinforced = false;
                                    AnnounceToParty("The reinforced structure door is broken down and is no longer reinforced!", Color.Red, new EntityList<Entity> { this });

                                    // Also break the exit door reinforcement
                                    var exitDoor = Target.Item4.Structure.Rooms[0].Objects.FirstOrDefault(o => o.Type == "exit door");
                                    if (exitDoor != null)
                                    {
                                        exitDoor.Reinforced = false;
                                        exitDoor.Integrity = 50;
                                    }
                                }
                            }
                            else
                            {
                                // The door is not reinforced, proceed with entering the structure
                                if (PlaySound)
                                {
                                    Game1.SFX.Add(Game1.GameWorld.rnd.Next(2) == 0 ? Game1.DoorSound2 : Game1.DoorSound1);
                                    StepSound(1);
                                }

                                Block.Architects.Remove(this);
                                CooldownCycles += (int)(Math.Round(25 / Speed));
                                Room = Target.Item4.Structure.Rooms[0];
                                Room.Architects.Add(this);
                                AnnounceToParty($"{ReferredToNames[0]} enters the structure.", Color.Green, new EntityList<Entity> { this });
                            }
                        }
                    }


                    StoredAltMove = AlternateMove;
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

            BlockLastCycle = Block;
            RoomLastCycle = Room;


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

            return ReturningAttack;
        }







        public void Bash()
        {
            if (Room == null) return;

            string message;

            if (Room.Integrity > 3)
                message = $"{ReferredToNames[0]} slams into the wall.";
            else if (Room.Integrity > 2)
                message = $"{ReferredToNames[0]} bashes the wall, and cracks form.";
            else if (Room.Integrity > 1)
                message = $"{ReferredToNames[0]} bashes the wall once more, forming more cracks.";
            else
                message = $"{ReferredToNames[0]} bashes the wall, and crashes through, escaping out the side.";

            AnnounceToParty(message, Color.Orange, new EntityList<Entity>());
            Room.Integrity -= 1;
            CooldownCycles += (int)Math.Round(25 / Speed);
            Game1.SFX.Add(Game1.Swing);

            if (Room.Integrity < 1)
            {
                Room.Architects.Remove(this);
                Room = null;
                Block.Architects.Add(this);
                Game1.SFX.Add(Game1.IceShock);

                //do it again for the people outside the structure.
                AnnounceToParty(message, Color.Orange, new EntityList<Entity>());

                Energy -= 30;
            }
        }




        public Architect()
        {
            //for serialization
        }

        public List<TextStorage> CastSpell(string Spell, EntityList<Entity> Targets) // the list of strings are the announcements
        {
            List<TextStorage> Announcements = new List<TextStorage>();

            RuinInvisibility();

            string casterName = this.ReferredToNames[0];
            Entity ForceThrowTarget = null;

            List<string> AggressiveSpells = new List<string>() { "water bolt", "chaos flare", "flash flame", "ice shock", "rise", "hold", "force throw", "shatter", "expel", "emergent growth" };

            bool PlaySound = false;
            if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this) ||
                (this.Room == null && this.Block.Architects.Any(t => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(t))) ||
                (this.Room != null && this.Room.Architects.Any(t => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(t))))
            {
                PlaySound = true;
            }

            if (Targets.Any(T => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(T)))
            {
                Architect AAA = (Targets.First(T => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(T)) as Architect);
                AAA.CancelSearch();
            }



            EntityList<Entity> TargetsToPurge = new EntityList<Entity>();
            foreach (Entity e in Targets)
            {
                if (!Game1.GameWorld.AllLegendarySpells.Any(e => e.Metadata == Spell) && e is Architect architect && GetDistance(architect) >= 4 && this != e && Spell != "extract")
                {
                    TargetsToPurge.Add(e);

                    if(Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                    {
                        Announcements.Add(new TextStorage($"{e.ReferredToNames[0]} is outside of casting range.", Color.Yellow, new EntityList<Entity>() { e }));
                    }
                }
            }
            foreach (Entity e in TargetsToPurge)
            {
                Targets.Remove(e);
            }

            foreach (Entity e in Targets)
            {
                if (e is Architect a && AggressiveSpells.Contains(Spell))
                {
                    a.CombatCycles = 250;
                    CombatCycles = 250;
                    a.ChangeOpinion(this, -100);
                }

                if (e is Architect A && A.Invocations.Contains("siphoning"))
                {
                    A.AnnounceToParty(A.ReferredToNames[0] + " heals some energy from the spell!", Color.Green, new EntityList<Entity>() { A });
                    A.Energy += 5;
                }

                if(e is Object o && o.Type == "shock mine")
                {
                    Shock(o);
                    Shock(o);
                }
            }

            if (Game1.GameWorld.AllLegendarySpells.Any(e => e.Metadata == Spell) && Targets.Count > 0)
            {
                if (this.Energy != this.MaxEnergy)
                {
                    Announcements.Add(new TextStorage($"You need to be fully energized before casting this.", Color.OrangeRed, new EntityList<Entity>() { }));

                    return (Announcements);
                }
                else
                {
                    Energy = 1;
                    HalfFocusTicks = 5000;
                    Announcements.Add(new TextStorage($"You feel incredibly drained...", Color.OrangeRed, new EntityList<Entity>() { }));
                }
            }
            else
            {
                double baseCost = Math.Max(1, (10 - (SpellcastingPower + (Focus / 2))));
                double totalCost = 0;

                for (int i = 0; i < Targets.Count; i++)
                {
                    totalCost += baseCost * Math.Pow(0.4, i); // each target costs 60% less than the one before
                }

                int EnergyDec = (int)Math.Round(totalCost);

                if (this.Race.Name == "debtshiba")
                    EnergyDec /= 10;

                Energy -= EnergyDec;
            }

            if (Game1.GameWorld.AllLegendarySpells.Any(s => s.Metadata == Spell))
            {
                int Month = ((int)Math.Round((decimal)(Game1.GameWorld.Cycle / 24192000)) % 12) + 1;
                int Year = (int)Math.Round((decimal)(Game1.GameWorld.Cycle / 290304000), MidpointRounding.ToZero);

                string Date = "(" + Month + "/" + Year + ")";

                List<string> Names = new List<string>();

                foreach (Entity e in Targets)
                {
                    Names.Add(e.Name);
                }



                Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + this.Name + " casted " + Spell + " at " + Game1.FormatAndList(Names), Location.Region, new EntityList<Entity>(){this}.Union(Targets)));
            }

            if(Spell == "force throw" && Targets.Count == 1)
            {
                AnnounceToParty($"Force Throw requires at least two targets. The first entity is the spell target, and each other entity selected will be force-thrown at the initial target.", Color.Magenta, new EntityList<Entity>() { });
            }
            else
            {
                if(Targets.Count > 0)
                    TryComment("magiccast", 33);

                foreach (Entity CurrentTarget in Targets)
                {
                    if (Spell == "water bolt")
                    {
                        CooldownCycles += (int)Math.Round(15 / Speed);
                        AnnounceToParty($"{casterName} curves their hand inwards, accumulating vapor. They hurl the concentrated sphere...", Color.Purple, new EntityList<Entity>() { this });
                        AnnounceToParty($"It crashes into {CurrentTarget.ReferredToNames[0]}, splashing into them!", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                        if (CurrentTarget is Object)
                        {
                            ((Object)CurrentTarget).Integrity = ((Object)CurrentTarget).Integrity - Game1.GameWorld.rnd.Next(3, 6 + Focus);
                            ((Object)CurrentTarget).WetCycles += Game1.GameWorld.rnd.Next(50, 100);
                        }
                        else
                        {
                            ((Architect)CurrentTarget).Energy -= Game1.GameWorld.rnd.Next(3, 6 + Focus);
                            ((Architect)CurrentTarget).DestabilizedCycles += Game1.GameWorld.rnd.Next(0, 50);
                            ((Architect)CurrentTarget).WetCycles += Game1.GameWorld.rnd.Next(50, 100);


                            if(!(((Architect)CurrentTarget).ActiveGifts.Contains("sight")))
                                ((Architect)CurrentTarget).BlindCycles += Game1.GameWorld.rnd.Next(20, 40 + Focus*5);

                            foreach (Object o in ((Architect)CurrentTarget).BodyParts)
                            {
                                o.Integrity = o.Integrity - 2;
                            }
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.WaterBolt);
                    }
                    else if (Spell == "chaos flare")
                    {
                        CooldownCycles += (int)Math.Round(15 / Speed);
                        AnnounceToParty($"{casterName} makes a fist and jerks their arm inwards, conjuring two spheres of light and dark rotating it. They throw them...", Color.Purple, new EntityList<Entity>() { this });
                        AnnounceToParty($"They crash into {CurrentTarget.ReferredToNames[0]}, and react explosively!", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                        if (CurrentTarget is Object)
                        {
                            ((Object)CurrentTarget).FireSeconds += Game1.GameWorld.rnd.Next(2, 4);
                            ((Object)CurrentTarget).DestabilizedCycles += Game1.GameWorld.rnd.Next(0, 50);
                            ((Object)CurrentTarget).Integrity = ((Object)CurrentTarget).Integrity - 50;
                        }
                        else
                        {
                            ((Architect)CurrentTarget).FireSeconds += Game1.GameWorld.rnd.Next(2, 4);
                            ((Architect)CurrentTarget).Energy -= Game1.GameWorld.rnd.Next(4, 8 + Focus*2);
                            ((Architect)CurrentTarget).DestabilizedCycles += Game1.GameWorld.rnd.Next(0, 50);
                            foreach (Object o in ((Architect)CurrentTarget).BodyParts)
                            {
                                if (Game1.GameWorld.rnd.Next(0, 2) == 0)
                                {
                                    o.Integrity = o.Integrity - 10+Focus*2;
                                }
                            }
                        }


                        if (PlaySound)
                            Game1.SFX.Add(Game1.ChaosFlare);
                    }
                    else if (Spell == "ice shock")
                    {
                        CooldownCycles += (int)Math.Round(15 / Speed);
                        AnnounceToParty($"{casterName} lifts up frozen particles...", Color.Purple, new EntityList<Entity>() { this });
                        AnnounceToParty($"A swirl of icy magic envelops {CurrentTarget.ReferredToNames[0]}!", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                        if (CurrentTarget is Object)
                        {
                            ((Object)CurrentTarget).WetCycles += Game1.GameWorld.rnd.Next(0, 100 + Focus*10);
                            ((Object)CurrentTarget).DestabilizedCycles += Game1.GameWorld.rnd.Next(0, 100 + Focus*10);
                            ((Object)CurrentTarget).Integrity = ((Object)CurrentTarget).Integrity - 50;
                        }
                        else
                        {
                            ((Architect)CurrentTarget).WetCycles += Game1.GameWorld.rnd.Next(0, 50+Focus*10);
                            ((Architect)CurrentTarget).DestabilizedCycles += Game1.GameWorld.rnd.Next(50, 101+Focus*10);
                            foreach (Object o in ((Architect)CurrentTarget).BodyParts)
                            {
                                if (Game1.GameWorld.rnd.Next(0, 2) == 0)
                                {
                                    o.Integrity = o.Integrity - 10+Focus;
                                }
                            }
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.IceShock);
                    }
                    else if (Spell == "flash flame")
                    {
                        CooldownCycles += (int)Math.Round(15 / Speed);
                        AnnounceToParty($"{casterName} holds their palms one over the other facing each other, and gathers heat energy...", Color.Purple, new EntityList<Entity>() { this });
                        AnnounceToParty($"It quickly dissipates, reassembling itself at {CurrentTarget.ReferredToNames[0]}!", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                        if (CurrentTarget is Object)
                        {
                            ((Object)CurrentTarget).FireSeconds += Game1.r.Next(3,6);
                        }
                        else
                        {
                            ((Architect)CurrentTarget).FireSeconds += Game1.r.Next(3, 6);
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.FlashFlame);
                    }




                    else if (Spell == "emergent growth")
                    {
                        CooldownCycles += (int)Math.Round(15 / Speed);
                        AnnounceToParty($"{casterName} holds out a hand, absorbing green fractal matter...", Color.Purple, new EntityList<Entity>() { this });
                        AnnounceToParty($"It grows into leafy tendrils that surround {CurrentTarget.ReferredToNames[0]}!", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                        if (CurrentTarget is Object)
                        {
                            ((Object)CurrentTarget).PlantCycles += 1000;
                        }
                        else
                        {
                            ((Architect)CurrentTarget).PlantCycles += 100;
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.EmergentGrowth);
                    }
                    else if (Spell == "tremor")
                    {
                        CooldownCycles += (int)Math.Round(30 / Speed);
                        AnnounceToParty($"{casterName} holds out their hands palms down and shoves into the ground...", Color.Purple, new EntityList<Entity>() { this });
                        AnnounceToParty($"A massive tremor shakes the ground, but {CurrentTarget.ReferredToNames[0]} is unshaken!", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                        EntityList<Object> Objects = Room != null ? Room.Objects : Block.Objects;
                        EntityList<Architect> Architects = Room != null ? Room.Architects : Block.Architects;

                        foreach (Object o in Objects)
                        {
                            if (!Targets.Contains(o))
                            {
                                o.DestabilizedCycles += Game1.GameWorld.rnd.Next(100, 200);
                                Announcements.Add(new TextStorage($"{o.ReferredToNames[0]} is destabilized!", Color.Magenta, new EntityList<Entity>() { o }));
                            }
                        }
                        foreach (Architect a in Architects)
                        {
                            if (!Targets.Contains(a))
                            {
                                a.DestabilizedCycles += Game1.GameWorld.rnd.Next(100, 200);
                                Announcements.Add(new TextStorage($"{a.ReferredToNames[0]} is destabilized!", Color.Magenta, new EntityList<Entity>() { a }));
                            }
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.Tremor);
                    }
                    else if (Spell == "immobile illusion" || Spell == "shadow veil" || Spell == "mobile illusion" || Spell == "reactive illusion")
                    {
                        CooldownCycles += (int)Math.Round(5 / Speed);
                        AnnounceToParty("You have only deceived yourself.", Color.Purple, new EntityList<Entity>());

                        if (PlaySound)
                            Game1.SFX.Add(Game1.Invisibility);
                    }
                    else if (Spell == "truthfulness")
                    {
                        CooldownCycles += (int)Math.Round(30 / Speed);
                        AnnounceToParty($"{casterName} waves across {CurrentTarget.ReferredToNames[0]}...", Color.Purple, new EntityList<Entity>() { this, CurrentTarget });

                        if (CurrentTarget is Object)
                        {
                            AnnounceToParty("...but nothing happens.", Color.Purple, new EntityList<Entity>());
                        }
                        else if (CurrentTarget is Architect)
                        {
                            AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} looks at you with a loyal complexion...", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                            ((Architect)CurrentTarget).ArchitectsIWillTellTruthTo.Add(this);
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.Truthfulness);
                    }
                    else if (Spell == "rise")
                    {
                        CooldownCycles += (int)Math.Round(20 / Speed);
                        Energy -= 10;
                        AnnounceToParty($"{casterName} gestures their hand towards the sky...", Color.Purple, new EntityList<Entity>() { this });

                        if (CurrentTarget is Object)
                        {
                            ((Object)CurrentTarget).YLevelInFeet += Game1.GameWorld.rnd.Next(30, 50);
                            AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} flies into the air!", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                        }
                        else
                        {
                            Architect a = ((Architect)CurrentTarget);

                            if (a.Energy < a.MaxEnergy / 2)
                            {
                                ((Architect)CurrentTarget).YLevelInFeet += Game1.GameWorld.rnd.Next(30, 50); 
                                AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} flies into the air!", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                            }
                            else
                            {
                                AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} is too heavy to fly, their energy must be reduced.", Color.Purple, new EntityList<Entity>() { this });

                            }
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.Rise);
                    }
                    else if (Spell == "immortalize")
                    {
                        CooldownCycles += (int)Math.Round(30 / Speed);
                        AnnounceToParty($"{casterName} conjures a magenta light in their hands...", Color.Purple, new EntityList<Entity>() { this });

                        if (CurrentTarget is Architect)
                        {
                            AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} is enveloped in a beautiful purple light!", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                            ((Architect)CurrentTarget).IsImmortal = true;
                        }
                        else
                        {
                            AnnounceToParty($"{casterName} cannot grant {CurrentTarget.ReferredToNames[0]} immortality.", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.Immortalize);
                    }
                    else if (Spell == "hold")
                    {
                        CooldownCycles += (int)Math.Round(30 / Speed);
                        AnnounceToParty($"{casterName} clenches their fist violently...", Color.Purple, new EntityList<Entity>() { this });

                        if (CurrentTarget is Object)
                        {
                            AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} stagnates.", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                            ((Object)CurrentTarget).AirborneTarget = null;
                            ((Object)CurrentTarget).AirborneCyclesToHitTarget = 0;
                        }
                        else if (CurrentTarget is Architect a && a.Energy < 60)
                        {
                            AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} freezes in time!", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                            ((Architect)CurrentTarget).HoldCycles = 40 + Focus * 5;
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.Hold);
                    }
                    if (Spell == "force throw")
                    {
                        CooldownCycles += (int)Math.Round(15 / Speed);

                        if (ForceThrowTarget == null)
                        {
                            ForceThrowTarget = CurrentTarget;
                            AnnounceToParty($"{casterName} clenches their fist at {CurrentTarget.ReferredToNames[0]}, gathering material...", Color.Purple, new EntityList<Entity>() { this, CurrentTarget });
                            AnnounceToParty($"They thrust it at {CurrentTarget.ReferredToNames[0]}!", Color.Purple, new EntityList<Entity>() { this, CurrentTarget });
                        }
                        else
                        {
                            AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} flies at {ForceThrowTarget.ReferredToNames[0]}!", Color.Purple, new EntityList<Entity>() { CurrentTarget, ForceThrowTarget });

                            if (CurrentTarget is Object)
                            {
                                ((Object)CurrentTarget).AirborneTarget = ForceThrowTarget;
                                ((Object)CurrentTarget).AirborneCyclesToHitTarget = 15 - Focus;
                                ((Object)CurrentTarget).Thrower = this;
                                ((Object)CurrentTarget).UpdateNames(true, null, true);
                            }
                            else if (CurrentTarget is Architect)
                            {
                                ((Architect)CurrentTarget).DestabilizedCycles += 50;
                            }
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.ForceThrow);
                    }
                    else if (Spell == "shatter")
                    {
                        CooldownCycles += (int)Math.Round(30 / Speed);
                        AnnounceToParty($"{casterName} brings his arms inward and swings them outward violently...", Color.Purple, new EntityList<Entity>() { this });

                        if (CurrentTarget is Object o)
                        {
                            if(o.Room != null)
                            {
                                o.Room.ObjectsToRemove.Add(o);
                                AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} dissipates across the area!", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                            }
                            else if (o.Block != null)
                            {
                                o.Block.ObjectsToRemove.Add(o);
                                AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} dissipates across the area!", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                            }
                            else if (Game1.LoadedArchitects.Any(a => a.Clothing.Contains(o) || a.Inventory.Contains(o) || a.MainHeldObject == o || a.OffHeldObject == o))
                            {
                                AnnounceToParty($"That object is too difficult to concentrate upon. Try removing it from the person/container first.", Color.Magenta, new EntityList<Entity>() { CurrentTarget });
                            }
                        }
                        else
                        {
                            AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} struggles to hold together, destabilizing...", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                            ((Architect)CurrentTarget).DestabilizedCycles += 100;
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.Shatter);
                    }
                    else if (Spell == "intercept")
                    {
                        AnnounceToParty($"{casterName} reaches their hand towards {CurrentTarget.ReferredToNames[0]} and grasps...", Color.Purple, new EntityList<Entity>() { this, CurrentTarget });

                        if (CurrentTarget is Object && ((Object)CurrentTarget).AirborneTarget != null)
                        {
                            AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} disappears in a web of fractals!", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                            ((Object)CurrentTarget).Fractallize(999999, this);
                        }
                        else
                        {
                            AnnounceToParty("...but nothing happens.", Color.Purple, new EntityList<Entity>());
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.Intercept);
                    }
                    else if (Spell == "expel")
                    {
                        CooldownCycles += (int)Math.Round(20 / Speed);
                        AnnounceToParty($"{casterName} reaches their hand towards {CurrentTarget.ReferredToNames[0]} and grasps...", Color.Purple, new EntityList<Entity>() { this, CurrentTarget });

                        if (CurrentTarget is Object o)
                        {
                            if (Game1.LoadedArchitects.Any(a => a.Clothing.Contains(o) || a.Inventory.Contains(o) || a.MainHeldObject == o || a.OffHeldObject == o))
                            {
                                AnnounceToParty($"That object is too difficult to concentrate upon. Try removing it from the person/container first.", Color.Magenta, new EntityList<Entity>() { CurrentTarget });
                            }
                            else
                            {
                                AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} disappears in a web of fractals!", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                                ((Object)CurrentTarget).Fractallize(999999, this);
                            }
                        }
                        else if (CurrentTarget is Architect)
                        {
                            if (((Architect)CurrentTarget).Energy < (((Architect)CurrentTarget).MaxEnergy / 3))
                            {
                                AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} disappears in a web of fractals!", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                                ((Architect)CurrentTarget).Fractallize(999999);
                            }
                            else
                            {
                                AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} resists the fractallization!", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                            }
                        }
                        else
                        {
                            AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} is enveloped in fractals, but does not fade.", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.Expel);
                    }
                    else if (Spell == "extract")
                    {
                        CooldownCycles += (int)Math.Round(20 / Speed);
                        AnnounceToParty($"{casterName} speaks the name of {CurrentTarget.ReferredToNames[0]}...", Color.Purple, new EntityList<Entity>() { this, CurrentTarget });

                        if (CurrentTarget is Object && Game1.GameWorld.FractalObjects.Contains((Object)CurrentTarget))
                        {
                            AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} reappears in a web of fractals!", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                            ((Object)CurrentTarget).RematerializeLocation = (Location, District, Block, Structure, Room);
                            ((Object)CurrentTarget).FractalCycles = 0;
                        }
                        else if (CurrentTarget is Architect && Game1.GameWorld.FractalArchitects.Contains((Architect)CurrentTarget))
                        {
                            AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} reappears in a web of fractals!", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                            ((Architect)CurrentTarget).RematerializeLocation = (Location, District, Block, Structure, Room);
                            ((Architect)CurrentTarget).FractalCycles = 0;
                        }
                        else
                        {
                            AnnounceToParty("...but nothing happens.", Color.Purple, new EntityList<Entity>());
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.Extract);
                    }
                    else if (Spell == "revive")
                    {
                        CooldownCycles += (int)Math.Round(100 / Speed);
                        AnnounceToParty($"{casterName} speaks the name of {CurrentTarget.ReferredToNames[0]}...", Color.Purple, new EntityList<Entity>() { this, CurrentTarget });

                        if (CurrentTarget is Architect && ((Architect)CurrentTarget).IsAlive == false && (((Architect)CurrentTarget).Block == Block && ((Architect)CurrentTarget).Room == this.Room))
                        {
                            AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} rises from the dead!", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                            ((Architect)CurrentTarget).IsAlive = true;
                            ((Architect)CurrentTarget).IsImmortal = true;
                            ((Architect)CurrentTarget).Energy = Math.Min(50, ((Architect)CurrentTarget).MaxEnergy);

                            if (!Game1.LoadedArchitects.Contains(CurrentTarget))
                            {
                                Game1.LoadedArchitects.Add((Architect)CurrentTarget);
                            }
                        }
                        else
                        {
                            AnnounceToParty("...but nothing happens.", Color.Purple, new EntityList<Entity>());
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.Revive);
                    }
                    else if (Spell == "resurrect")
                    {
                        CooldownCycles += (int)Math.Round(500 / Speed);
                        AnnounceToParty($"{casterName} speaks the name of {CurrentTarget.ReferredToNames[0]} and meditates...", Color.Purple, new EntityList<Entity>() { this, CurrentTarget });

                        if (CurrentTarget is Architect && ((Architect)CurrentTarget).IsAlive == false && (((Architect)CurrentTarget).Block == Block && ((Architect)CurrentTarget).Room == this.Room))
                        {
                            AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} is surrounded in crystals and returns from the dead!", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                            ((Architect)CurrentTarget).IsAlive = true;
                            ((Architect)CurrentTarget).IsImmortal = true;
                            ((Architect)CurrentTarget).Energy = 100;

                            ((Architect)CurrentTarget).ImportantThisLoad = true;

                            foreach (Object o in ((Architect)CurrentTarget).BodyParts)
                            {
                                o.Integrity = 100;
                            }

                            if (!Game1.LoadedArchitects.Contains(CurrentTarget))
                            {
                                Game1.LoadedArchitects.Add((Architect)CurrentTarget);
                            }
                        }
                        else
                        {
                            AnnounceToParty("...but nothing happens.", Color.Purple, new EntityList<Entity>());
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.Resurrect);
                    }
                    else if (Spell == "animate")
                    {
                        CooldownCycles += (int)Math.Round(5 / Speed);
                        AnnounceToParty($"{casterName} conjures a spark of necromantic energy and passes it to {CurrentTarget.ReferredToNames[0]}...", Color.Purple, new EntityList<Entity>() { this, CurrentTarget });

                        if (CurrentTarget is Architect architect)
                        {
                            architect.RaiseFromTheDead(this, CurrentTarget.ReferredToNames[0], PathOfDeathLevel, 2);

                            if (architect.IsAlive)
                            {
                                if (!Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(architect))
                                {
                                    Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Add(architect);
                                }
                            }
                        }
                        else
                        {
                            AnnounceToParty("...but nothing happens.", Color.Purple, new EntityList<Entity>());
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.Animate);
                    }
                    else if (Spell == "ethereal rupture")
                    {
                        RuptureMode = true;
                    }
                    else if (Spell == "emergence")
                    {
                        CooldownCycles += (int)Math.Round(5 / Speed);
                        AnnounceToParty($"{casterName} holds out a hand and speaks the name of {CurrentTarget.ReferredToNames[0]}...", Color.Purple, new EntityList<Entity>() { this, CurrentTarget });

                        if (!(CurrentTarget is Architect) || ((Architect)CurrentTarget).IsAlive == true)
                        {
                            AnnounceToParty("...but nothing happens.", Color.Purple, new EntityList<Entity>());
                        }
                        else
                        {
                            Architect target = (Architect)CurrentTarget;
                            AnnounceToParty($"{CurrentTarget.Name} appears in front of them!", Color.Purple, new EntityList<Entity>() { CurrentTarget });


                            //remove them from any place you might have found them previously

                            target.District.DistrictArchitects.Remove(target);
                            target.Location = this.Location;
                            target.District = this.District;

                            //return them from death

                            target.Block = Block;
                            target.IsAlive = true;
                            target.BodyParts.Clear();
                            target.PopulateSelf(false);
                            target.Inventory = new EntityList<Object>();
                            target.Clothing = new EntityList<Object>();
                            target.Energy = target.MaxEnergy;


                            if (!Game1.LoadedArchitects.Contains(target))
                            {
                                Game1.LoadedArchitects.Add(target);
                            }

                            if (Room != null)
                            {
                                Room.Architects.Add(target);
                                target.Room = Room;

                                foreach(Architect a in Room.Architects)
                                {
                                    this.ModifyDistance(a, 3);
                                }
                            }
                            else
                            {
                                Block.Architects.Add(target);
                                foreach (Architect a in Block.Architects)
                                {
                                    this.ModifyDistance(a, 3);
                                }
                            }
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.Emergence);
                    }
                    else if (Spell == "eternal bind")
                    {
                        CooldownCycles += (int)Math.Round(5 / Speed);
                        AnnounceToParty($"{casterName} stares deeply into {CurrentTarget.ReferredToNames[0]}'s eyes...", Color.Purple, new EntityList<Entity>() { this, CurrentTarget });

                        if ((!(CurrentTarget is Architect) || ((Architect)CurrentTarget).IsAlive == false) && ((Architect)CurrentTarget).Block == Block && ((Architect)CurrentTarget).Room == Room)
                        {
                            AnnounceToParty("...but nothing happens.", Color.Purple, new EntityList<Entity>());
                        }
                        else if(CurrentTarget is Architect a && a.Race.Name == "debtshiba")
                        {
                            AnnounceToParty("One does not simply \"bind\" a debtshiba.", Color.Red, new EntityList<Entity>());
                        }
                        else
                        {
                            Architect target = (Architect)CurrentTarget;
                            AnnounceToParty($"{CurrentTarget.Name}'s expression becomes vacant.", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                            target.ChangeOpinion(this, 999999);

                            if (target.TargetArchitect == this && (target.Task == "killtarget" || target.Task == "disabletarget"))
                            {
                                target.Task = "";
                                target.TargetArchitect = null;
                            }

                            if (!Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(target))
                            {
                                Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Add(target);
                            }

                            if (target.Group != null && target.Group.Leader == target)
                                target.Group.Leader = null;

                            target.Group = null;
                            target.GroupLoyalty = 0;

                            if (Game1.GameWorld.Calamity.Contains(target))
                                Game1.GameWorld.Calamity.Remove(target);

                            target.Master = null;
                            target.Opinions = new List<int>();
                            target.ArchitectsForOpinions = new EntityList<Architect>();

                            target.ArchitectsWhoIAttemptedToSurrenderTo.Clear();
                            target.ArchitectsWhoISurrenderedTo.Clear();
                            target.ArchitectsWhoSurrenderedToMe.Clear();
                        }

                        if (PlaySound)
                            Game1.SFX.Add(Game1.EternalBind);
                    }
                    else if (Spell == "expunge")
                    {
                        CooldownCycles += (int)Math.Round(5 / Speed);
                        AnnounceToParty($"{casterName} gestures aggressively...", Color.Purple, new EntityList<Entity>() { this });


                        CurrentTarget.Expunged = true;

                        if (CurrentTarget is Civilization)
                        {
                            AnnounceToParty($"{CurrentTarget.Name} and its legacy have been erased...", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                            foreach (Location l in Game1.GameWorld.AllLocations.ToList()) // Create a copy to avoid modification during iteration
                            {
                                if (l.HomeCivilization == CurrentTarget)
                                {
                                    // Announce and nullify the region's location
                                    AnnounceToParty($"{l.Name} detaches from the ground and levitates into infinite nothing.", Color.Purple, new EntityList<Entity>() { l });
                                    l.Region.Location = null;

                                    // Handle architects related to this location
                                    if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Any(a => a.Location == l))
                                    {
                                        var architectsToRemove = new List<Architect>();

                                        foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                                        {
                                            if (a.Location == l)
                                            {
                                                Game1.MakeObservation($"{a.Name} was successfully teleported into oblivion. How embarrassing...", Color.Magenta, new EntityList<Entity>() { a });
                                                a.IsAlive = false;
                                                architectsToRemove.Add(a);
                                            }
                                        }

                                        foreach (Architect a in architectsToRemove)
                                        {
                                            Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Remove(a);
                                        }

                                        // If all architects are removed, switch state to "dead"
                                        if (!Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Any())
                                        {
                                            Game1.SwitchState("dead", false);
                                            Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].CompanionMessage("alldeath", "");
                                        }
                                    }
                                }
                            }
                        }
                        else if (CurrentTarget is World || (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(CurrentTarget) && Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Count == 1))
                        {
                            Game1.SwitchState("mainscreen", false);
                            Game1.GameWorld = null;
                            return new List<TextStorage>();
                        }
                        else if (Game1.GameWorld.AllSpells.Contains(CurrentTarget))
                        {
                            AnnounceToParty($"The knowledge of {CurrentTarget.Name} has been erased from the land...", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                            foreach (Architect a in Game1.GameWorld.AllHistoricalArchitects)
                            {
                                a.SpellsKnown.Remove(CurrentTarget);
                            }
                            foreach(Architect a in Game1.LoadedArchitects)
                            {
                                a.SpellsKnown.Remove(CurrentTarget);
                            }
                            foreach(Object o in Game1.GameWorld.AllArtifacts)
                            {
                                if(o.SpecialKnowledge == CurrentTarget)
                                {
                                    o.SpecialKnowledge = null;
                                }
                            }

                            Game1.GameWorld.AllSpells.Remove(CurrentTarget);
                        }
                        else if (Game1.GameWorld.AllSkills.Contains(CurrentTarget))
                        {
                            AnnounceToParty($"The knowledge of {CurrentTarget.Name} has been erased from the land...", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                            foreach (Architect a in Game1.GameWorld.AllHistoricalArchitects)
                            {
                                a.SkillsKnown.Remove(CurrentTarget);
                            }
                            foreach (Architect a in Game1.LoadedArchitects)
                            {
                                a.SkillsKnown.Remove(CurrentTarget);
                            }
                            foreach (Object o in Game1.GameWorld.AllArtifacts)
                            {
                                if (o.SpecialKnowledge == CurrentTarget)
                                {
                                    o.SpecialKnowledge = null;
                                }
                            }

                            Game1.GameWorld.AllSkills.Remove(CurrentTarget);
                        }
                        else if (Game1.GameWorld.AllLegendarySpells.Contains(CurrentTarget))
                        {
                            AnnounceToParty($"An accursed relic locks this spell away. Perhaps you can find and banish this artifact instead.", Color.Purple, new EntityList<Entity>());
                        }
                        else if (CurrentTarget is Blight)
                        {
                            AnnounceToParty($"{CurrentTarget.Name} has been entirely purified...", Color.Purple, new EntityList<Entity>() { CurrentTarget });

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
                            AnnounceToParty($"The knowledge of {CurrentTarget.Name} has been erased from the land...", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                            foreach (Architect a in Game1.GameWorld.AllHistoricalArchitects)
                            {
                                a.CultureBank.Remove((Composition)CurrentTarget);
                            }
                            foreach (Architect a in Game1.LoadedArchitects)
                            {
                                a.CultureBank.Remove((Composition)CurrentTarget);
                            }
                            foreach (Object o in Game1.GameWorld.AllArtifacts)
                            {
                                if(o.CompositionContent == CurrentTarget)
                                {
                                    o.CompositionContent = null;
                                }
                            }
                        }
                        else if (CurrentTarget is Deity)
                        {
                            AnnounceToParty($"You feel an intense pain...", Color.Purple, new EntityList<Entity>());
                            Pain += 1000;
                        }
                        else if (CurrentTarget is District)
                        {
                            AnnounceToParty($"{CurrentTarget.Name} detaches from the ground and levitates into infinite nothing.", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                            ((District)CurrentTarget).Location.Districts.Remove(((District)CurrentTarget));

                            if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                            {
                                var architectsToRemove = new List<Architect>();

                                foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                                {
                                    if (a.District == CurrentTarget)
                                    {
                                        Game1.MakeObservation(a.Name + " was successfully teleported into oblivion. How embarrassing...", Color.Magenta, new EntityList<Entity>() { a });
                                        a.IsAlive = false;
                                        architectsToRemove.Add(a);
                                    }
                                }

                                foreach (Architect a in architectsToRemove)
                                {
                                    Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Remove(a);
                                }

                                if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Count() == 0)
                                {
                                    Game1.SwitchState("dead", false);
                                    Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].CompanionMessage("alldeath", "");
                                }
                            }
                        }
                        else if (CurrentTarget is Location)
                        {
                            AnnounceToParty($"{CurrentTarget.Name} detaches from the ground and levitates into infinite nothing.", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                            ((Location)CurrentTarget).Region.Location = null;

                            if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                            {
                                var architectsToRemove = new List<Architect>();

                                foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                                {
                                    if (a.Location == (Location)CurrentTarget)
                                    {
                                        Game1.MakeObservation(a.Name + " was successfully teleported into oblivion. How embarrassing...", Color.Magenta, new EntityList<Entity>() { a });
                                        a.IsAlive = false;
                                        architectsToRemove.Add(a);
                                    }
                                }

                                foreach (Architect a in architectsToRemove)
                                {
                                    Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Remove(a);
                                }

                                if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Count() == 0)
                                {
                                    Game1.SwitchState("dead", false);
                                    Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].CompanionMessage("alldeath", "");
                                }
                            }
                        }

                        else if (CurrentTarget is Party)
                        {
                            AnnounceToParty($"Your party has disbanded.", Color.Purple, new EntityList<Entity>());

                            EntityList<Architect> ArchitectsToBanish = new EntityList<Architect>();
                            foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                            {
                                if (a != this)
                                {
                                    ArchitectsToBanish.Add(a);
                                }
                            }

                            foreach (Architect a in ArchitectsToBanish)
                            {
                                Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Remove(a);
                            }

                            Game1.GameWorld.GamePlayerAssociation.Parties.Remove(CurrentTarget as Party);
                        }
                        else if (CurrentTarget is Group g)
                        {
                            // disbands the group, removes it from any power, does not kill the members
                            AnnounceToParty($"{CurrentTarget.Name}'s relationship has fractured.", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                            Game1.GameWorld.Groups.Remove((Group)CurrentTarget);
                            Game1.GameWorld.TradingGroups.Remove((Group)CurrentTarget);

                            foreach (Location l in Game1.GameWorld.AllLocations)
                            {
                                if (l.Government == CurrentTarget)
                                {
                                    l.Government = null;
                                }
                                if (l.TradersAtThisLocation.Contains(CurrentTarget))
                                {
                                    l.TradersAtThisLocation.Remove((Group)CurrentTarget);
                                }
                                if (l.GroupsAtThisLocation.Contains(CurrentTarget))
                                {
                                    l.GroupsAtThisLocation.Remove((Group)CurrentTarget);
                                }
                                if (l.Government == CurrentTarget)
                                {
                                    l.Government = null;
                                }
                            }

                            foreach (Architect a in ((Group)CurrentTarget).Architects)
                            {
                                a.Group = null;
                            }

                            foreach(Faction f in Game1.GameWorld.AllFactions)
                            {
                                if(f.SatelliteGroups.Contains(g))
                                {
                                    f.SatelliteGroups.Remove(g);
                                }
                            }
                        }
                        else if (CurrentTarget is Architect)
                        {
                            AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} is banished and forgotten.", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                            Architect a = (Architect)CurrentTarget;

                            if (District.DistrictArchitects.Contains(a))
                            {
                                District.DistrictArchitects.Remove(a);
                            }

                            Game1.GameWorld.AllHistoricalArchitects.Remove(a);

                            foreach (Architect A in Game1.GameWorld.AllHistoricalArchitects)
                            {
                                var indicesToRemove = new List<int>();

                                // Find indices to remove
                                for (int i = 0; i < A.ArchitectsForOpinions.Count; i++)
                                {
                                    if (A.ArchitectsForOpinions[i] == a)
                                    {
                                        indicesToRemove.Add(i);
                                    }
                                }

                                // Remove the indices in reverse order to maintain list integrity
                                for (int i = indicesToRemove.Count - 1; i >= 0; i--)
                                {
                                    int index = indicesToRemove[i];
                                    A.ArchitectsForOpinions.RemoveAt(index);
                                    A.Opinions.RemoveAt(index);
                                }
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

                                l.DebtShibas.Remove(a);
                            }

                            for (int x = 0; x < Game1.GameWorld.Width; x++)
                            {
                                for (int z = 0; z < Game1.GameWorld.Width; z++)
                                {
                                    foreach (Unit e in Game1.GameWorld.WorldMap[x + z * Game1.GameWorld.Width].Units)
                                    {
                                        if (e.UnitArchitects.Contains(a))
                                        {
                                            e.UnitArchitects.Remove(a);
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
                                a.DropInventory(true);
                                a.Room = null;
                            }
                            else if (a.Block != null)
                            {
                                a.Block.Architects.Remove(a);
                                a.DropInventory(true);
                                a.Block = null;
                            }

                            if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a))
                            {
                                Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Remove(a);
                            }

                            if (a.Group != null)
                            {
                                if (a.Group.Leader == a)
                                {
                                    // disbands the group, removes it from any power, does not kill the members
                                    AnnounceToParty($"{CurrentTarget.ReferredToNames[0]}'s relationship has fractured.", Color.Purple, new EntityList<Entity>() { CurrentTarget });

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
                            AnnounceToParty($"{CurrentTarget.ReferredToNames[0]} collapses into a singularity.", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                            List<Architect> Architects = Game1.GameWorld.AllHistoricalArchitects.Union(Game1.LoadedArchitects).ToList();

                            foreach (Architect a in Architects)
                            {
                                if (a.Inventory.Contains(CurrentTarget))
                                {
                                    a.Inventory.Remove((Object)CurrentTarget);
                                    break;
                                }
                                else if (a.Clothing.Contains(CurrentTarget))
                                {
                                    a.Clothing.Remove((Object)CurrentTarget);
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
                                else if (a.BodyParts.Contains(CurrentTarget))
                                {
                                    a.BodyParts.Remove((Object)CurrentTarget);
                                    break;
                                }
                            }

                            foreach(Block b in District.DistrictMap)
                            {
                                b.ObjectsToRemove.Add((Object)CurrentTarget);

                                foreach (Structure s in b.Structures)
                                {
                                    foreach(Room r in s.Rooms)
                                    {
                                        r.ObjectsToRemove.Add((Object)CurrentTarget);
                                    }
                                }
                            }

                            foreach(Location l in Game1.GameWorld.AllLocations)
                            {
                                foreach(Structure s in l.AllStructures)
                                {
                                    s.HistoricalObjects.Remove((Object)CurrentTarget);
                                }
                            }

                            Game1.GameWorld.AllArtifacts.Remove((Object)CurrentTarget);
                        }
                        else if (CurrentTarget is Material)
                        {
                            // add the material to a list of banished materials. Replace objects with these materials with Void Energy, a new material when they update. You cannot cast this spell on Void Material.
                            AnnounceToParty($"{CurrentTarget.Name}'s properties have been reduced to void.", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                            Material m = CurrentTarget as Material;

                            m.Name = "void";
                            m.Type = "metaphysic";
                            m.Toughness = 0;
                            m.Rarity = 0;
                            m.Color = "purple";
                        }
                        else if (CurrentTarget is Race R)
                        {
                            AnnounceToParty($"The members of {CurrentTarget.ReferredToNames[0]} have been reduced to indistinguishable lifeforms.", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                            // a banished race makes all members of that race shades. this is stored in a static list.

                            //when you load an accursed race, its body parts get updated to shade parts.

                            R.Accursed = true;

                            foreach (Architect a in Game1.LoadedArchitects.Union(Game1.GameWorld.AllHistoricalArchitects))
                            {
                                a.SpellsKnown = a.SpellsKnown.Distinct();
                                a.SkillsKnown = a.SkillsKnown.Distinct();

                                foreach (Object o in a.BodyParts)
                                {
                                    o.Owner = a;
                                }

                                if (a.Race.Accursed && a.Race.Name != "shade")
                                {
                                    a.BodyParts.Clear();
                                    a.Race = Game1.GameWorld.GetRace("shade");
                                    a.PopulateSelf(false);
                                }
                            }
                        }
                        else if (CurrentTarget is Structure)
                        {
                            AnnounceToParty($"{CurrentTarget.Name} vanishes.", Color.Purple, new EntityList<Entity>() { CurrentTarget });

                            Structure s = ((Structure)CurrentTarget);

                            foreach (Room r in s.Rooms)
                            {
                                foreach (Architect a in r.Architects)
                                {
                                    s.Block.Architects.Add(a);
                                    a.Room = null;
                                }
                                foreach (Object o in r.Objects)
                                {
                                    s.Block.Objects.Add(o);
                                    o.Room = null;
                                }
                            }

                            if (s.Block.District.IsLoaded)
                            {
                                foreach (Object o in s.HistoricalObjects)
                                {
                                    s.Block.Objects.Add(o);
                                    o.Room = null;
                                }
                            }

                            s.Block.Structures.Remove(s);
                        }
                        else
                        {
                            CurrentTarget.Expunged = false;
                            AnnounceToParty("...but nothing happens.", Color.Purple, new EntityList<Entity>());
                            PlaySound = false;
                        }


                        if (PlaySound)
                            Game1.SFX.Add(Game1.EternalBind);
                    }
                    else if (Spell == "echo")
                    {
                        AnnounceToParty("You manifest spatial particles...", Color.Purple, new EntityList<Entity>());

                        if (CurrentTarget is Architect)
                        {
                            Architect Base = (Architect)CurrentTarget;
                            string NameAlteration = Game1.GameWorld.GenerateUniqueName("1S3s", new Entity("this is a tag for a clone"), Game1.GameWorld.rnd);
                            Architect ArchitectClone = new Architect(
                                Base.Name + " " + NameAlteration,
                                Base.Sex, Base.Race, Base.Age, Base.Profession,
                                new EntityList<Object>(), Base.Location, Base.District, Base.Block,
                                Base.Destiny, Base.Level, Base.Historical
                            );
                            Game1.GameWorld.AllHistoricalArchitects.Add(ArchitectClone);

                            // Core stats
                            ArchitectClone.MoralCompass = Base.MoralCompass;
                            ArchitectClone.StabilityCompass = Base.StabilityCompass;
                            ArchitectClone.PropertyValue = Base.PropertyValue;
                            ArchitectClone.FamilyValue = Base.FamilyValue;
                            ArchitectClone.PowerValue = Base.PowerValue;
                            ArchitectClone.MoneyValue = Base.MoneyValue;
                            ArchitectClone.KnowledgeValue = Base.KnowledgeValue;
                            ArchitectClone.SpiritualityValue = Base.SpiritualityValue;
                            ArchitectClone.ProwessValue = Base.ProwessValue;
                            ArchitectClone.PatriotismValue = Base.PatriotismValue;
                            ArchitectClone.CourageValue = Base.CourageValue;
                            ArchitectClone.CreativityValue = Base.CreativityValue;

                            ArchitectClone.Dexterity = Base.Dexterity;
                            ArchitectClone.Strength = Base.Strength;
                            ArchitectClone.Charisma = Base.Charisma;
                            ArchitectClone.Focus = Base.Focus;
                            ArchitectClone.Endurance = Base.Endurance;
                            ArchitectClone.Agility = Base.Agility;
                            ArchitectClone.Creativity = Base.Creativity;

                            // Skills, spells, memory
                            ArchitectClone.CultureBank = new EntityList<Composition>(Base.CultureBank);
                            ArchitectClone.Proficiencies = new List<(string, int)>(Base.Proficiencies);
                            ArchitectClone.ArchitectsForOpinions = new EntityList<Architect>(Base.ArchitectsForOpinions);
                            ArchitectClone.Opinions = new List<int>(Base.Opinions);

                            ArchitectClone.SpellsKnown = new EntityList<Entity>(Base.SpellsKnown);
                            ArchitectClone.SkillsKnown = new EntityList<Entity>(Base.SkillsKnown);
                            ArchitectClone.UsedSkills = new EntityList<Entity>();
                            ArchitectClone.Invocations = new List<string>(Base.Invocations);
                            ArchitectClone.AllImbuements = new EntityList<Imbuement>(Base.AllImbuements);

                            // Deep clone object-related data
                            if (Base.MainHeldObject != null)
                                ArchitectClone.MainHeldObject = Game1.Clone(Base.MainHeldObject);

                            if (Base.OffHeldObject != null)
                                ArchitectClone.OffHeldObject = Game1.Clone(Base.OffHeldObject);

                            ArchitectClone.Inventory = new EntityList<Object>();
                            foreach (var obj in Base.Inventory)
                                ArchitectClone.Inventory.Add(Game1.Clone(obj));

                            ArchitectClone.Clothing = new EntityList<Object>();
                            foreach (var obj in Base.Clothing)
                                ArchitectClone.Clothing.Add(Game1.Clone(obj));

                            ArchitectClone.BodyParts = new EntityList<Object>();
                            foreach (var obj in Base.BodyParts)
                                ArchitectClone.BodyParts.Add(Game1.Clone(obj));

                            // Interaction appendages (assign by Object.Type)
                            if (Base.MainInteractionAppendage != null)
                                ArchitectClone.MainInteractionAppendage = ArchitectClone.BodyParts.FirstOrDefault(x => x.Type == Base.MainInteractionAppendage.Type);
                            if (Base.OffInteractionAppendage != null)
                                ArchitectClone.OffInteractionAppendage = ArchitectClone.BodyParts.FirstOrDefault(x => x.Type == Base.OffInteractionAppendage.Type);

                            // Combat state
                            ArchitectClone.Sparks = new EntityList<Object>();
                            ArchitectClone.CooldownCycles = 0;
                            ArchitectClone.CombatCycles = 0;
                            ArchitectClone.DoubleStrikeReady = false;
                            ArchitectClone.QuickStrikeReady = false;
                            ArchitectClone.SeveringStrikeReady = false;
                            ArchitectClone.FinaleReady = false;
                            ArchitectClone.BodySlamReady = false;
                            ArchitectClone.LegSweepReady = false;
                            ArchitectClone.DropKickReady = false;

                            // Personality, memory, etc.
                            ArchitectClone.Personalities = new List<string>(Base.Personalities);
                            ArchitectClone.SentCompanionMessages = new List<string>(Base.SentCompanionMessages);
                            ArchitectClone.MessagesNotRespondedTo = new List<Message>();
                            ArchitectClone.KnownArchitects = new EntityList<Architect>(Base.KnownArchitects);

                            // Path levels
                            ArchitectClone.PathOfShadowLevel = Base.PathOfShadowLevel;
                            ArchitectClone.PathOfLifeLevel = Base.PathOfLifeLevel;
                            ArchitectClone.PathOfRealityLevel = Base.PathOfRealityLevel;
                            ArchitectClone.PathOfLightLevel = Base.PathOfLightLevel;
                            ArchitectClone.PathOfDeathLevel = Base.PathOfDeathLevel;
                            ArchitectClone.PathOfStarsLevel = Base.PathOfStarsLevel;
                            ArchitectClone.PathOfHeatLevel = Base.PathOfHeatLevel;
                            ArchitectClone.PathOfBodyLevel = Base.PathOfBodyLevel;
                            // Levels and age
                            ArchitectClone.SpendableLevels = Base.SpendableLevels;
                            ArchitectClone.TerminalAge = Base.TerminalAge;
                            ArchitectClone.Level = Base.Level;
                            ArchitectClone.AlreadyMadeAGame = Base.AlreadyMadeAGame;
                            ArchitectClone.HasMadeALegendaryArtifact = Base.HasMadeALegendaryArtifact;
                            ArchitectClone.HadChildren = Base.HadChildren;

                            // Reset task logic
                            ArchitectClone.CurrentStudyTopic = null;
                            ArchitectClone.CurrentContemplationTopic = null;
                            ArchitectClone.Task = "";
                            ArchitectClone.CyclesLeftInTask = 0;
                            ArchitectClone.NextMoveOrder = ("", null);
                            ArchitectClone.CurrentlyMovingPlace = "";
                            ArchitectClone.NextMigrationLocation = null;
                            ArchitectClone.MigrationReason = "";
                            ArchitectClone.TryingToTravel = false;
                            ArchitectClone.TriedAscend = false;
                            ArchitectClone.MigrationKitted = false;

                            // Body state
                            ArchitectClone.Energy = Base.Energy;
                            ArchitectClone.MaxEnergyMod = Base.MaxEnergyMod;
                            ArchitectClone.Bleeding = Base.Bleeding;
                            ArchitectClone.Pain = Base.Pain;
                            ArchitectClone.WetCycles = Base.WetCycles;
                            ArchitectClone.UnconsciousCycles = Base.UnconsciousCycles;
                            ArchitectClone.DestabilizedCycles = Base.DestabilizedCycles;
                            ArchitectClone.IsImmortal = Base.IsImmortal;
                            ArchitectClone.DeathCause = Base.DeathCause;
                            ArchitectClone.OnGround = Base.OnGround;

                            // Group and division
                            ArchitectClone.Group = Base.Group;
                            ArchitectClone.GroupLoyalty = Base.GroupLoyalty;
                            ArchitectClone.Division = Base.Division;

                            if (ArchitectClone.Group != null)
                                ArchitectClone.Group.Architects.Add(ArchitectClone);

                            // Game state
                            ArchitectClone.Historical = Base.Historical;
                            ArchitectClone.IsAlive = Base.IsAlive;

                            ArchitectClone.ArchitectsWhoSurrenderedToMe = new EntityHashSet<Architect>(Base.ArchitectsWhoSurrenderedToMe);
                            ArchitectClone.ArchitectsWhoISurrenderedTo = new EntityHashSet<Architect>(Base.ArchitectsWhoISurrenderedTo);
                            ArchitectClone.ArchitectsWhoIAttemptedToSurrenderTo = new EntityHashSet<Architect>(Base.ArchitectsWhoIAttemptedToSurrenderTo);
                            ArchitectClone.ArchitectsIWillTellTruthTo = new EntityHashSet<Architect>(Base.ArchitectsIWillTellTruthTo);

                            ArchitectClone.ExploredLocations = new EntityHashSet<Location>(Base.ExploredLocations);
                            ArchitectClone.TakenLocations = new EntityList<Location>(Base.TakenLocations);

                            // Misc data
                            ArchitectClone.Brand = Base.Brand;
                            ArchitectClone.BrandColor = Base.BrandColor;
                            ArchitectClone.LegendaryTarget = null;
                            ArchitectClone.FavoriteBook = Base.FavoriteBook;

                            ArchitectClone.ShadowStorage = new EntityList<Object>();
                            foreach (var obj in Base.ShadowStorage)
                                ArchitectClone.ShadowStorage.Add(Game1.Clone(obj));

                            ArchitectClone.Wealth = 0;

                            ArchitectClone.OppositionTags = new List<string>(Base.OppositionTags);
                            ArchitectClone.SuperTrustedArchitects = new EntityList<Architect>(Base.SuperTrustedArchitects);

                            ArchitectClone.HomeLocation = Base.HomeLocation;
                            ArchitectClone.HomeDistrict = Base.HomeDistrict;
                            ArchitectClone.HomeStructure = Base.HomeStructure;

                            // Final placement
                            if (this.Room != null)
                                this.Room.Architects.Add(ArchitectClone);
                            else
                                this.Block.Architects.Add(ArchitectClone);

                            Game1.LoadedArchitects.Add(ArchitectClone);

                            AnnounceToParty($"An echo of {CurrentTarget.ReferredToNames[0]} appears!", Color.Purple, new EntityList<Entity>() { CurrentTarget });


                            ArchitectClone.UpdateNames();
                        }

                        else if (CurrentTarget is Object)
                        {

                            Object baseObject = (Object)CurrentTarget;
                            Object clone = Game1.Clone(baseObject);

                            if (this.Room != null)
                                this.Room.Objects.Add(clone);
                            else
                                this.Block.Objects.Add(clone);

                            clone.UpdateNames(true, null, true);

                            AnnounceToParty($"An echo of {CurrentTarget.ReferredToNames[0]} appears!", Color.Purple, new EntityList<Entity>() { CurrentTarget });
                        }
                        else
                        {
                            AnnounceToParty("You manifest spatial particles...", Color.Purple, new EntityList<Entity>());
                            AnnounceToParty("...but they just aren't strong enough. The spell only works on Architects and Objects.", Color.Purple, new EntityList<Entity>());
                            PlaySound = false;
                        }


                        if (PlaySound)
                            Game1.SFX.Add(Game1.EternalBind);
                    }


                    if (CurrentTarget is Architect AAAA && Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(AAAA) && this.Race.Name == "debtshiba")
                    {
                        if(AAAA.Energy <= 0)
                        {

                            Game1.CompanionMessages.Clear();
                            AAAA.CompanionMessage("debtshibadeath", "");
                        }
                    }
                }

                foreach (Imbuement i in AllImbuements)
                {
                    if (i.IsTrigger && i.ConditionOrTrigger == "oncast" && i.IsActive)
                    {
                        ActivatePower(i.BuffOrResult);
                    }
                }


            }

            CooldownCycles /= Math.Max(1, SpellcastingPower / 2);


            return Announcements;
        }

        public void ActivatePower(string Power)
        {
            EntityList<Architect> hostiles = new EntityList<Architect>();
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
                AnnounceToParty(ReferredToNames[0] + " generates a barrier stack!", Color.Magenta, new EntityList<Entity>() { this });
            }
            else if (Power == "projectile" && hostiles.Count() > 0)
            {
                EntityList<Object> objects = Room != null ? Room.Objects : Block.Objects;
                Architect target = hostiles[0];

                Object o = new Object(null, "bolt", new EntityList<Material>() { Game1.GameWorld.Energy }, this);
                objects.Add(o);
                o.AirborneTarget = target;
                o.Thrower = this;
                o.AirborneCyclesToHitTarget = 15 - Focus;
                AnnounceToParty(ReferredToNames[0] + " fires a bolt at " + target.ReferredToNames[0] + "!", Color.Magenta, new EntityList<Entity>() { this, target });
            }
            else if (Power == "ignite" && hostiles.Count() > 0)
            {
                Architect target = hostiles[0];
                target.FireSeconds += 2;
                AnnounceToParty(ReferredToNames[0] + " ignites " + target.ReferredToNames[0] + "!", Color.Magenta, new EntityList<Entity>() { this, target });
            }
            else if (Power == "destabilize" && hostiles.Count() > 0)
            {
                Architect target = hostiles[0];
                target.DestabilizedCycles += 20;
                AnnounceToParty(ReferredToNames[0] + " destabilizes " + target.ReferredToNames[0] + "!", Color.Magenta, new EntityList<Entity>() { this, target });
            }
            else if (Power == "dismiss")
            {
                DismissalCycles += 30;
                AnnounceToParty(ReferredToNames[0] + " becomes partially intangible!", Color.Magenta, new EntityList<Entity>() { this });
            }
        }


        public void Fractallize(int Cycles)
        {
            DropInventory(true);
            FractalCycles = Cycles;
            RematerializeLocation = (this.Location, this.District, this.Block, this.Structure, this.Room);
            Game1.GameWorld.FractalArchitects.Add(this);

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
            HideValue = 0;
            TriedAscend = false;
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

            if(YLevelInFeet > 0 && InFlight == false)
            {
                if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                    AnnounceToParty("You are falling too fast to move.", Color.Orange, new EntityList<Entity>());
                return;
            }

            if(this.Structure != null)
            {
                return;
            }

            bool PlaySound = true;
            if (!Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
            {
                PlaySound = false;
            }

            this.StepsTaken++;

            if(this.StepsTaken == 20 && Game1.GameWorld.SettlementTypes.Contains(this.Location.Type))
            {
                CompanionMessage("askaround", "");
            }

            if (directionOffsets.TryGetValue(direction, out var offset))
            {
                int newX = Block.X + offset.dx;
                int newZ = Block.Z + offset.dz;

                // Handle boundary and travel logic
                if (newX < 0 || newX >= 7 || newZ < 0 || newZ >= 7)
                {
                    if (CurrentlyMovingPlace == direction)
                    {
                        if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                        {
                            if (Game1.TutorialActive || Game1.CombatSimActive)
                            {
                                Game1.ProgressTutorial(39);


                                Game1.GameState = Game1.CombatSimActive ? "worldgenscreen" : "mainscreen";

                                //still some cleanup we need to do tho
                                Game1.GameWorld = null;
                                Game1.MostRecentPartyTurnArchitect = null;
                                Game1.TutorialActive = false;
                                Game1.CombatLocation = null;
                                Game1.CombatDistrict = null;
                                Game1.CombatSimActive = false;
                                Game1.RecognizedCommands.Remove("level_up");
                                Game1.GameWorld = null;
                                Game1.MostRecentPartyTurnArchitect = null;
                                Game1.LoadedArchitects = new List<Architect>();

                            }
                            else
                            {
                                TryingToTravel = true;
                                bool allTryingToTravel = Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.All(a => (a.TryingToTravel && a.PartyActive) || a.FractalCycles > 0);


                                //if ALL architects who are ACTIVE are trying to travel and theres at least ONE Who is inactive...


                                if(Game1.GaveInactiveWarningMessage == false && Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.All(a => !a.PartyActive || a.TryingToTravel) && Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Any(a => a.PartyActive == false))
                                {
                                    AnnounceToParty("A member of your party is inactive, and is not yet trying to leave. Reactivate all party members and try to leave with them to successfully escape.", Color.Orange, new EntityList<Entity>());

                                    Game1.GaveInactiveWarningMessage = true;
                                }



                                if (allTryingToTravel)
                                {
                                    if (Game1.GameWorld.GameMode == "chronicle")
                                    {
                                        Game1.SwitchState("travelmenu", false);
                                        Game1.GameWorld.GamePlayerAssociation.ActiveParty.ClearSkillData();
                                        Game1.GameWorld.GamePlayerAssociation.ActiveParty.MapCursorDistrict = 0;
                                        Game1.GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX = Location.X;
                                        Game1.GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ = Location.Z;
                                        Location.Explored = true;
                                        Game1.UpdateTravelButtons();


                                        Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].CompanionMessage("explaintravel", "");

                                        int Year = (int)Math.Round((decimal)(Game1.GameWorld.Cycle / 290304000), MidpointRounding.ToZero);
                                        int Month = ((int)Math.Round((decimal)(Game1.GameWorld.Cycle / 24192000)) % 12) + 1;
                                        string Date = "(" + Month + "/" + Year + ")";

                                        foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                                        {
                                            Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + a.Name + " left " + a.Location.Name + ".", a.Location.Region, new EntityList<Entity>() { a, Location }));
                                        }

                                        Game1.GameWorld.RevealNearbyTiles(Game1.GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX, Game1.GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ, 3, true);

                                        Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].District.Unload();

                                        foreach (Architect architect in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                                        {
                                            architect.CurrentlyMovingPlace = "none"; // Reset movement place after successful travel

                                            architect.AbjuredThisDistrict = false;

                                            architect.Energy = architect.MaxEnergy;
                                            architect.Pain = 0;
                                            architect.Bleeding = 0;
                                            architect.HalfFocusTicks = 0;

                                            foreach(Object o in architect.BodyParts)
                                            {
                                                o.Integrity = 100;
                                            }
                                        }

                                        if ((DateTime.Now - Game1.LastSave).TotalSeconds >= 60) // Ensure at least 2 minute has passed
                                        {
                                            if (Game1.MaxAutosaveAge >= Year)
                                            {
                                                Game1.SwitchState("savinggame", true);
                                                Game1.StateToSwitchToAfterSave = "travelmenu";
                                                Game1.DontSaveGameBecauseWeJustDid = true;
                                            }
                                            else
                                            {
                                                Game1.SwitchState("travelmenu", true);
                                            }
                                        }
                                        else
                                        {
                                            Game1.SwitchState("travelmenu", true); // Skip saving if less than 2 minute has passed
                                        }

                                        Game1.LastSave = DateTime.Now;
                                    }
                                    else
                                    {
                                        Game1.SwitchState("ascendant", false);
                                        Game1.GameWorld.GamePlayerAssociation.ActiveParty.ClearSkillData();

                                        foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                                        {
                                            int Month = ((int)Math.Round((decimal)(Game1.GameWorld.Cycle / 24192000)) % 12) + 1;
                                            int Year = (int)Math.Round((decimal)(Game1.GameWorld.Cycle / 290304000), MidpointRounding.ToZero);
                                            string Date = "(" + Month + "/" + Year + ")";
                                            Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + a.Name + " completed their mission in  " + a.Location.Name + ".", a.Location.Region, new EntityList<Entity>() { a, a.Location }));
                                            a.AbjuredThisDistrict = false;
                                        }

                                        Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].District.Unload();

                                        foreach (var architect in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                                        {
                                            architect.CurrentlyMovingPlace = "none"; // Reset movement place after successful travel
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Non-party members logic for moving to a new district/location
                            Game1.LoadedArchitects.Remove(this);

                            if (this.Unit != null)
                                this.Unit.AllowsReplacement = true ;

                            this.MovementMode = "walking";

                            if (Location != Target.Item1)
                            {
                                NextMigrationLocation = Target.Item1;
                                MigrationReason = "I am going to " + Target.Item1;

                                if (Task == "escape")
                                {
                                    MigrationReason += " to escape from danger";
                                }
                                else if (Target.Item3 != null)
                                {
                                    MigrationReason += " to visit " + Target.Item2.Name;

                                }
                                if (Target.Item4 != null)
                                {
                                    MigrationReason += " for the " + Target.Item4.Structure.Type + " named " + Target.Item4.Structure.Name;
                                }

                                MigrationReason += ".";
                            }
                            else if (District != Target.Item2)
                            {
                                District.DistrictArchitects.Remove(this);
                                District = Target.Item2;
                                District.DistrictArchitects.Add(this);
                            }

                            //wipe memories of architects who are on death row (lol at least death row for the memory save system)

                            for (int i = ArchitectsForOpinions.Count - 1; i >= 0; i--)
                            {
                                Architect a = ArchitectsForOpinions[i];
                                if (a.AddToNumberOnReload || !a.ImportantThisLoad)
                                {
                                    ArchitectsForOpinions.RemoveAt(i);
                                    Opinions.RemoveAt(i);
                                }
                            }



                            this.Block.ArchitectsToRemove.Add(this);
                            this.Task = "";
                            this.CyclesLeftInTask = 0;
                            this.Block = null;
                            this.MovementMode = "walking";
                        }

                        CurrentlyMovingPlace = "none";  // Reset after successful movement
                    }
                    else
                    {
                        if (CombatCycles == 0 || Game1.GameWorld.rnd.Next(100) <= EscapeChance())
                        { 
                            // Set or update CurrentlyMovingPlace to the new intended direction for boundary crossing
                            CurrentlyMovingPlace = direction;
                            CooldownCycles += (int)Math.Round(25 / Speed);
                        }
                    }
                }
                else
                {
                    // Handle in-district movement
                    if (CurrentlyMovingPlace == direction)
                    {
                        if (newX == 0 || newX == 6 || newZ == 0 || newZ == 6)
                        {
                            Game1.ProgressTutorial(38);
                        }

                        if (CombatCycles == 0 || Game1.GameWorld.rnd.Next(100) <= EscapeChance())
                        {
                            Block.Architects.Remove(this);
                            Block = District.DistrictMap[newX + newZ * 7];
                            Block.Architects.Add(this);

                            HealStrikes = 0;

                            foreach (Object o in BodyParts)
                            {
                                o.UpdateExposure(-9999);
                            }
                            CombatCycles = 0;

                            // Update structures in the new block
                            foreach (Structure s in Block.Structures)
                            {
                                if (s.Type != "house" && s.Type != "big house")
                                {
                                    if(Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                                    {
                                        AnnounceToParty(s.GetStructureDescription(), Color.DarkGray, new EntityList<Entity>() { s });
                                    }
                                }
                            }

                            CurrentlyMovingPlace = "none";  // Reset after successful movement
                            CooldownCycles += (int)Math.Round(25 / Speed);

                            if(Block.X == 3 && Block.Z == 3)
                            {
                                Game1.ProgressTutorial(22);
                            }

                            var chromaObject = Block.Objects.FirstOrDefault(o => o.CompositionContent != null && o.Materials[0].Name == "enchromalite");

                            if (Block.Objects.Any(o => o.Type == "shadow fountain") && this.LocationsVisited > 1)
                            {
                                CompanionMessage("shadowstorage", "");
                            }
                            if (Block.Objects.Any(o => o.Type == "chromaweaver"))
                            {
                                CompanionMessage("chromaweaver", "");
                            }
                            if (Block.Objects.Any(o => o.Type == "pylon" && o.AnnouncedSelfThisLoad == false))
                            {
                                Object o = Block.Objects.First(o => o.Type == "pylon" && o.AnnouncedSelfThisLoad == false);
                                AnnounceToParty("This pylon radiates with a repulsive energy you seem drawn to destroy...", Color.Red, new EntityList<Entity>());
                                o.AnnouncedSelfThisLoad = true;
                            }
                            if (Block.Structures.Any(s => s.Type == "forge" || s.Type == "tavern" || s.Type == "prism" || s.Block.District.Location.AllStructures.Count == 1))
                            {
                                CompanionMessage("howtoenter", "");
                            }
                            if (chromaObject != null)
                            {
                                CompanionMessage("enchromalite", chromaObject.Type);
                            }

                            if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                            {
                                foreach (Entity e in Game1.LoadedHooks)
                                {
                                    if ((Block.Architects.Contains(e) || Block.Objects.Contains(e)) && e.AnnouncedSelfThisLoad == false)
                                    {
                                        AnnounceToParty(e.HookedObjective.SightMessage.Data, e.HookedObjective.SightMessage.Color, e.HookedObjective.SightMessage.Entities);
                                        e.AnnouncedSelfThisLoad = true;

                                        if(e.HookedObjective.RequiredInteraction == "offerassistance")
                                            CompanionMessage("hookrequiresofferassistance", "");
                                        if (e.HookedObjective.RequiredInteraction == "read")
                                            CompanionMessage("hookrequiresread", "");
                                        if (e.HookedObjective.RequiredInteraction == "examine")
                                            CompanionMessage("hookrequiresexamine", "");
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                            {
                                AnnounceToParty("You are cut off from your escape attempt.", Color.OrangeRed, new EntityList<Entity>());
                            }
                            CooldownCycles += (int)Math.Round(25 / Speed);
                        }
                    }
                    else
                    {
                        // Set or update CurrentlyMovingPlace to the new intended direction
                        if (CombatCycles == 0 || Game1.GameWorld.rnd.Next(100) <= EscapeChance())
                        {
                            CurrentlyMovingPlace = direction;
                            CooldownCycles += (int)Math.Round(25 / Speed);
                        }
                        else
                        {
                            if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this))
                            {
                                AnnounceToParty("You are cut off from your escape attempt.", Color.OrangeRed, new EntityList<Entity>());
                            }
                            CooldownCycles += (int)Math.Round(25 / Speed);
                        }
                    }


                    if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(this) && this.StepsTaken > 12)
                    {
                        if (LocationsVisited > 1 && this.District.Location.Market != null && this.District.Location.Market.Block.District == this.District &&
                            ((this.Block.X == 1 || this.Block.X == 5) && (this.Block.Z >= 1 && this.Block.Z <= 5)) ||
                            ((this.Block.Z == 1 || this.Block.Z == 5) && (this.Block.X >= 1 && this.Block.X <= 5)))
                        {
                            CompanionMessage("buystuff", "");
                        }
                    }

                }
            } 
            else
            {
                AnnounceToParty("You can't go that \"way\".", Color.Yellow, new EntityList<Entity>());
                PlaySound = false;
            }

            

            if (PlaySound && Game1.GameWorld != null)
            {
                StepSound(1);
            }
        }

        string GetOpposingDirection(string direction)
        {
            return direction switch
            {
                "north" => "south",
                "south" => "north",
                "east" => "west",
                "west" => "east",
                "up" => "down",
                "down" => "up",
                _ => direction
            };
        }


        public void IDidSomethingBadSoScanForShockMines()
        {
            // Scan current room or block
            (this.Room?.Objects ?? this.Block.Objects)
                .Where(o => o.Type == "shock mine")
                .ToList()
                .ForEach(Shock);

            // Check held objects
            if (this.MainHeldObject?.Type == "shock mine")
                Shock(this.MainHeldObject);

            if (this.OffHeldObject?.Type == "shock mine")
                Shock(this.OffHeldObject);

            // Check inventory
            this.Inventory
                .Where(o => o.Type == "shock mine")
                .ToList()
                .ForEach(Shock);
        }


        public void Shock(Object shocker)
        {
            AnnounceToParty(ReferredToNames[0] + " is zapped with overwhelming pain by " + shocker.ReferredToNames[0] + "!", Color.Magenta, new EntityList<Entity>() { this, shocker });

            this.Pain += 200;
            this.Energy /= 3;
            this.Bleeding += 5;
            this.FireSeconds += 3;
            this.DestabilizedCycles += 1000;
            this.BlindCycles += 200;
        }

        public void MoveThroughDoor(string doorId)
        {
            Door door = Room?.Objects.OfType<Door>().FirstOrDefault(d => d.ID.ToString() == doorId) ??
                        Block?.Objects.OfType<Door>().FirstOrDefault(d => d.ID.ToString() == doorId);

            if (door == null)
            {
                AnnounceToParty($"{ReferredToNames[0]} couldn't find anything like that in the area to enter.", Color.Yellow, new EntityList<Entity> { this });
                return;
            }

            if (door.Reinforced)
            {
                AnnounceToParty($"{ReferredToNames[0]} bashes the reinforced door!", Color.OrangeRed, new EntityList<Entity> { this });
                Game1.SFX.Add(Game1.Craft);

                door.Integrity -= 15;
                CooldownCycles += (int)(Math.Round(50 / Speed)); // 20 base + 30 for bashing

                if (door.Integrity < 0)
                {
                    door.Integrity = 50;
                    door.Reinforced = false;
                    AnnounceToParty("The reinforced door is broken down and is no longer reinforced!", Color.Red, new EntityList<Entity> { this });

                    // Break opposing door
                    if (door.DestinationRoom != null)
                    {
                        string opposingDirection = GetOpposingDirection(door.Direction);
                        var pairedDoor = door.DestinationRoom.Objects
                            .OfType<Door>()
                            .FirstOrDefault(d => d.Direction == opposingDirection && d.DestinationRoom == Room);

                        if (pairedDoor != null)
                        {
                            pairedDoor.Reinforced = false;
                            pairedDoor.Integrity = 50;
                            AnnounceToParty($"{pairedDoor.ReferredToNames[0]} is also no longer reinforced!", Color.Orange, new EntityList<Entity> { this });
                        }
                    }
                }
            }
            else
            {
                // Delegate the rest to the core enter logic
                Enter(door, false);
            }
        }


    }
}
