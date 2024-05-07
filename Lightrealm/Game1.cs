using Lightrealm.Data;
using Lightrealm.Diagnostics;
using Lightrealm.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

#pragma warning disable SYSLIB0011

namespace Lightrealm
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        public static int MapCursorX { get; set; } = 0;
        public static int MapCursorZ { get; set; } = 0;

        public static bool TriedFakeMove = false;

        public static int TicksSinceLoad = 0;

        public static int CurrentObjectPage = 0;
        public static int MaximumObjectPage = 0;
        public static int ItemsPerPage = 50;

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
        public static Architect MostRecentPartyTurnArchitect = null;

        public static string GrievanceReason = "";

        public List<string> AllEnteredGameStates = new List<string>();

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
        public static int ArchitectIndex = 0;

        public static void MakeObservation(string data, Color color)
        {
            string capitalizedData = CapitalizeFirstLetter(data);
            Observations.Add(new TextStorage(capitalizedData, color));
            Announcements.Add(new TextStorage(capitalizedData, color));
        }

        public static void AddMessage(string data, Color color)
        {
            string capitalizedData = CapitalizeFirstLetter(data);
            Messages.Add(new TextStorage(capitalizedData, color));
            Announcements.Add(new TextStorage(capitalizedData, color));
        }

        private static string CapitalizeFirstLetter(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return data;
            }
            return char.ToUpper(data[0]) + data.Substring(1);
        }


        public static int TileSize = 13;
        public static int TileXDistance = 14;
        public static int TileZDistance = 11;

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

        public static List<string> AnimalSizes = new List<string>() { "miniscule", "smaller", "small", "medium", "humanoid", "large", "huge"};
        public static List<string> AllSizes = new List<string>() { "ethereal", "miniscule", "smaller", "small", "medium", "humanoid", "large", "huge", "colossal", "archancient" };

        public bool HasPlayerBeenAttacked(Architect architect)
        {
            foreach (var attack in StoredAttacks)
            {
                if (architect.ReferredToNames.Contains(attack.Target))
                {
                    return true;
                }
            }

            return false;
        }

        static string ConvertListToString(List<string> items)
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
        public static InteractableEvent StoredEvent = null;

        public static Dictionary<string, Color> ColorConverter = new Dictionary<string, Color>();

        public static List<string> AllWeapons = new List<string>
        {
            "sword", "greatsword", "axe", "greataxe", "knife",
            "rapier", "spear", "pike",
            "mace", "hammer", "shield",
            "whip", "flail", "chain"
        };

        public static Object GenerateRandomWeapon(Material material, string Rarity)
        {
            // Array of possible weapon types
            string[] weaponTypes = { "sword", "greatsword", "battle axe", "greataxe", "rapier", "spear", "pike", "mace", "hammer", "shield", "whip", "scourge" };

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

        public static void CalculateAttack(string verb, Architect attacker, string target, string DefenderAction, Object weapon)
        {
            Object bodyPartInQuestion = null;
            bool actuallyFoundSomething = false;

            attacker.CombatCycles = 50;

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

            foreach (Architect a in attacker.Structure == null ? attacker.Block.Architects : attacker.Room.Architects)
            {
                if (a.ReferredToNames.Contains(target) || a.ReferredToNames.Contains(target + "'"))
                {
                    bodyPartInQuestion = a.BodyParts[Game1.r.Next(a.BodyParts.Count)];
                    a.Focused = true;
                    a.CombatCycles = 2;
                    TargetArchitect = a;
                    break;
                }
                else
                {
                    bodyPartInQuestion = a.BodyParts.FirstOrDefault(b => b.Name == target || b.ReferredToNames.Contains(target));

                    if (bodyPartInQuestion != null)
                    {
                        a.Focused = true;
                        TargetArchitect = a;
                        break;
                    }
                }
            }

            if(TargetArchitect != null && !(GamePlayerParty.Architects.Contains(TargetArchitect)))
            {  
                TargetArchitect.ChangeOpinion(attacker, -75);
            }
            else
            {

            }



            //pls make sure one more time that the weapon is correct

            if(bodyPartInQuestion != null)
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

                if(r.Next(0,100) < TargetArchitect.ExtraStealth)
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
                            if (r.Next(0, 101) < successChances.duck && !CantDuckBodyParts.Contains(bodyPartInQuestion.Type))
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
                            if (r.Next(0, 101) < successChances.jump && !CantJumpBodyParts.Contains(bodyPartInQuestion.Type))
                            {
                                Avoided = true;
                                AvoidFeedback = TargetArchitect.ReferredToNames[0] + " jumped over the attack!";
                                TargetArchitect.ChangeXP("dodging", r.Next(1, 4));
                            }
                            else
                            {
                                AvoidFeedback = TargetArchitect.ReferredToNames[0] + " attempted to jump over the attack, but failed!";
                            }
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
                                    AvoidFeedback = "The parry of " + DefenderWeapon.ReferredToNames[0] + " failed!";
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
                    Observations.Add(new TextStorage($"A star falls from the heavens!", Color.Goldenrod));
                }
                else if (attacker.PathOfStarsLevel >= 6)
                {
                    StarCount = 3;
                    Observations.Add(new TextStorage($"Stars fall from the heavens!", Color.Goldenrod));
                }

                for(int i = 0; i < StarCount; i++)
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

            if (bodyPartInQuestion != null)
            {
                if((attacker.Room != null && attacker.Room.Architects.Contains(LoadedArchitects[ArchitectIndex])) || (attacker.Block != null && attacker.Block.Architects.Contains(LoadedArchitects[ArchitectIndex])))
                {
                    Observations.Add(new TextStorage($"{attacker.Name} {verb.Substring(0, verb.Length)}es {target} with {weapon.ReferredToNames[0]}!", Color.Blue));
                    Announcements.Add(new TextStorage($"{attacker.Name} {verb.Substring(0, verb.Length)}es {target} with {weapon.ReferredToNames[0]}!", Color.Blue));
                }

                List<TextStorage> announcements;
                if (Avoided)
                {
                    announcements = new List<TextStorage>() { new TextStorage(AvoidFeedback, Color.HotPink) };
                }
                else
                {
                    announcements = new List<TextStorage>();
                    announcements.AddRange(bodyPartInQuestion.TakeDamageFromObject(weapon, proficiencyModifier * (1 + attacker.ExtraAttackPower/100)));
                    announcements.Insert(0, new TextStorage(AvoidFeedback, Color.OrangeRed));
                }



                if ((attacker.Room != null && attacker.Room.Architects.Contains(LoadedArchitects[ArchitectIndex])) || (attacker.Block != null && attacker.Block.Architects.Contains(LoadedArchitects[ArchitectIndex])))
                {
                    Announcements.AddRange(announcements);
                    Observations.AddRange(announcements);
                }
                actuallyFoundSomething = true;
            }
            else
            {
                IEnumerable<Object> objects;

                if (attacker.Structure != null)
                {
                    // Attacker is in a room
                    objects = attacker.Room.Objects.Concat(attacker.Block.Objects);
                }
                else
                {
                    // Attacker is in a block
                    objects = attacker.Block.Objects;
                }

                foreach (Object o in objects.Where(o => o.ReferredToNames.Contains(target)))
                {
                    if ((attacker.Room != null && attacker.Room.Architects.Contains(LoadedArchitects[ArchitectIndex])) || (attacker.Block != null && attacker.Block.Architects.Contains(LoadedArchitects[ArchitectIndex])))
                    {
                        Observations.Add(new TextStorage($"{attacker.Name} {verb.Substring(0, verb.Length)}es {o.Name} with {weapon.ReferredToNames[0]}!", Color.Blue));
                        Announcements.Add(new TextStorage($"{attacker.Name} {verb.Substring(0, verb.Length)}es {o.Name} with {weapon.ReferredToNames[0]}!", Color.Blue));
                    }

                    List<TextStorage> announcements;
                    if (Avoided)
                    {
                        announcements = new List<TextStorage>() { new TextStorage("The attack is " + AvoidFeedback + "!", Color.HotPink) };
                    }
                    else
                    {
                        announcements = new List<TextStorage>();

                        int DivineMight = 0;

                        if(attacker.DivineMight > 0)
                        {
                            DivineMight = 1;
                            attacker.DivineMight--;
                            announcements.Add(new TextStorage("The divine have intevened! The attack is empowered in a brilliant energy!", Color.Aquamarine));

                            if (attacker.DivineMight == 0)
                            {
                                announcements.Add(new TextStorage("The divine might has worn off!", Color.Aquamarine));
                            }
                        }

                        if (TargetArchitect.DivineProtection > 0)
                        {
                            TargetArchitect.DivineProtection--;
                            announcements.Add(new TextStorage("The divine have intevened! The attack is avoided by " + TargetArchitect.ReferredToNames[0] + "'s divine protection. It wears thin, though...", Color.Aquamarine));
                            if (TargetArchitect.DivineProtection == 0)
                            {
                                announcements.Add(new TextStorage("The divine protection has worn off!", Color.Aquamarine));
                            }
                        }
                        else
                        {
                            announcements.AddRange(
                                o.TakeDamageFromObject(
                                    weapon,
                                    (int)Math.Round((
                                        (1 + (0.1 * attacker.Strength)) *
                                        (proficiencyModifier * (1 + attacker.ExtraAttackPower / 100))
                                    ) + DivineMight)
                                )
                            );
                        }
                    }
                    if ((attacker.Room != null && attacker.Room.Architects.Contains(LoadedArchitects[ArchitectIndex])) || (attacker.Block != null && attacker.Block.Architects.Contains(LoadedArchitects[ArchitectIndex])))
                    {
                        Announcements.AddRange(announcements);
                    }

                    actuallyFoundSomething = true;
                    break;
                }
            }

            if (!actuallyFoundSomething)
            {
                if ((attacker.Room != null && attacker.Room.Architects.Contains(LoadedArchitects[ArchitectIndex])) || (attacker.Block != null && attacker.Block.Architects.Contains(LoadedArchitects[ArchitectIndex])))
                {
                    Observations.Add(new TextStorage(attacker.Name + " flails around!", Color.Blue));
                    Announcements.Add(new TextStorage(attacker.Name + " flails around!", Color.Blue));
                }
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


        public static Dictionary<string, List<Material>> MaterialsFromColors = new Dictionary<string, List<Material>>
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

        public static List<string> Headwear = new List<string>() { "none", "none", "none", "large hat", "small hat", "hood" };
        public static List<string> Neckwear = new List<string>() { "none", "none", "none", "amulet", "amulet/amulet/amulet", "flair"};
        public static List<string> Handwear = new List<string>() { "none", "none", "none", "left glove/right glove", "left wristwrap/right wristwrap"};
        public static List<string> Bodywear = new List<string>() { "shortsleeve shirt", "longsleeve shirt", "shortsleeve shirt", "longsleeve shirt", "uppershirt", "straps", "shortsleeve shirt/cape", "longsleeve shirt/cape", "straps/cape", };
        public static List<string> Legwear = new List<string>() { "pants", "pants", "shorts", "kilt/pants", "kilt", "kilt/wraps"};
        public static List<string> Footwear = new List<string>() { "none", "left boot/right boot", "left boot/right boot", "left boot/right boot", "left shoe/right shoe", "left shoe/right shoe" };


        public List<(int,int,Color,int,int, int)> CivilizationParticles = new List<(int, int, Color, int, int, int)>();

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

            for(int x = 0; x < 7; x++)
            {
                for (int z = 0; z < 7; z++)
                {
                    foreach(Architect a in D.DistrictMap[x+z*7].Architects)
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
        }

        public static World GameWorld;
        public static Party GamePlayerParty;
        public static Civilization GamePlayerCivilization;

        public static string DocumentsFolderPath
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); }
        }
        static string GetDirectionFromCenterDistrict(int x, int y)
        {
            int centerX = 3; // Center x-coordinate of the 7x7 array
            int centerY = 3; // Center y-coordinate of the 7x7 array

            if (x == centerX && y == centerY)
            {
                return "Center";
            }
            else if (x == centerX && y < centerY)
            {
                return "North";
            }
            else if (x == centerX && y > centerY)
            {
                return "South";
            }
            else if (x < centerX && y == centerY)
            {
                return "West";
            }
            else if (x > centerX && y == centerY)
            {
                return "East";
            }
            else if (x < centerX && y < centerY)
            {
                return "Northwest";
            }
            else if (x < centerX && y > centerY)
            {
                return "Southwest";
            }
            else if (x > centerX && y < centerY)
            {
                return "Northeast";
            }
            else // (x > centerX && y > centerY)
            {
                return "Southeast";
            }
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
            {"masonry", "mason"}
        };


        public static List<string> LightingStyles = new List<string> { "none", "none", "none", "none", "none", "candles", "candles", "candles", "candles", "a lone torch in each room", "several braziers", "an oil lamp", "a candelabra", "an oil lantern", "a blazing fireplace" };
        public static List<string> AllSpells = new List<string>() { "water bolt", "chaos flare", "concentrated ignition", "tremor", "immobile illusion", "shadow veil", "mobile illusion", "reactive illusion", "truthfulness", "rise", "hold", "forcethrow", "shatter", "clone", "intercept", "expel", "extract", "emergent growth", "animate", "immortalize", "raise", "resurrect" };
        public static List<string> AllLegendarySpells = new List<string>() { "ethereal rupture", "emergence", "eternal bind", "expunge", "echo" };

        public static List<string> PossibleMagicalItems = new List<string>() { "chalice", "scepter", "lantern", "bracelet", "left gauntlet", "staff", "amulet", "hourglass", "locket", "orb" };

        //public Dictionary<string, Texture2D> TileAtlas = new Dictionary<string, Texture2D>();
        public static List<string> WeightedRandomArchitectProfessions = new List<string>() { "commander", "craftsman", "craftsman", "craftsman", "mercenary", "mercenary", "mercenary", "musician", "musician", "elder", "prophet", "trader", "trader", "anarchist", "political figure", "scholar", "scholar", "scholar", "scholar" };
        public static List<string> WeightedRandomNormalProfessions = new List<string>() { "soldier", "peasant", "peasant", "peasant", "blacksmith", "miller", "baker", "merchant", "brewer", "brewer", "tanner", "tailor", "carpenter", "mason", "scribe", "butcher", "fisherman", "weaver", "potter", "miner", "miner", "no profession", "no profession", "no profession", "no profession" };

        public static List<string> ArchitectProfessions = new List<string>() { "commander", "craftsman", "mercenary", "musician", "elder", "prophet", "trader", "anarchist", "political figure", "scholar" };
        public static List<string> Sexes = new List<string>() { "male", "female" };

        public static List<string> DeathCauses = new List<string>() { " fell to their death ", " drowned ", " died of cancer ", " burned ", " misoperated dangerous equipment ", " died of sickness ", " starved to death ", " dehydrated ", " choked to death ", " was killed by a wild animal " };

        public static List<string> Industries = new List<string>() { "textiles", "spices", "metal", "jewelry", "tools", "military", "tea", "coffee", "wood", "ceramics", "glassmaking", "dye", "waspkeeping", "fuel", "masonry"};

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

        public Dictionary<Keys, string> KeyAtlas = new Dictionary<Keys, string>();
        public Dictionary<Keys, string> UpperKeyAtlas = new Dictionary<Keys, string>();

        public static Architect TheChosenOne;
        public static Group TheChosenGroup;

        public static int CurrentlySelectingArchitectProfession;
        public static int CurrentlySelectingSex;
        public static int CurrentlySelectingRace = 1;

        public static Dictionary<string, string> ConvertArchitectToGroupType = new Dictionary<string, string>();
        public static Dictionary<string, string> ConvertProfessionToBuilding = new Dictionary<string, string>();
        public static Dictionary<string, string> ConvertTaskToArchitectPrefix = new Dictionary<string, string>();
        public static Dictionary<string, string> ConvertProfessionToCareerDescription = new Dictionary<string, string>();

        public static Random r = new Random();

        public string ObservationsAndMessages = "both";

        /*
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

        */



        int WaitingTicks = 0;
        int EscapeTicks = 0;
        int SaveTicks = 0;
        int LoadTicks = 0;
        int LoadGameCursor = 0;


        public static bool RunCommand(Architect Executor, string Command, List<Entity> Subjects)
        {
            //replace inside command pronouns

            List<Architect> ArchitectsToUse;
            if (Executor.Room != null)
            {
                ArchitectsToUse = Executor.Room.Architects; // Use architects from the room if it's not null
            }
            else
            {
                ArchitectsToUse = Executor.Block.Architects; // Otherwise, use architects from the block
            }

            var waitCommands = new Dictionary<string, int>
            {
                { "wait a second", 1 },
                { "wait one second", 1 },
                { "wait 1 second", 1 },
                { "wait two seconds", 2 },
                { "wait 2 seconds", 2 },
                { "wait three seconds", 3 },
                { "wait 3 seconds", 3 },
                { "wait four seconds", 4 },
                { "wait 4 seconds", 4 },
                { "wait five seconds", 5 },
                { "wait 5 seconds", 5 },
                { "wait six seconds", 6 },
                { "wait 6 seconds", 6 },
                { "wait seven seconds", 7 },
                { "wait 7 seconds", 7 },
                { "wait eight seconds", 8 },
                { "wait 8 seconds", 8 },
                { "wait nine seconds", 9 },
                { "wait 9 seconds", 9 },
                { "wait ten seconds", 10 },
                { "wait 10 seconds", 10 },
                { "wait twenty seconds", 20 },
                { "wait 20 seconds", 20 },
                { "wait thirty seconds", 30 },
                { "wait 30 seconds", 30 },
                { "wait forty seconds", 40 },
                { "wait 40 seconds", 40 },
                { "wait fifty seconds", 50 },
                { "wait 50 seconds", 50 },
                { "wait a minute", 60 },
                { "wait one minute", 60 },
                { "wait 60 seconds", 60 },
                { "wait 2 minutes", 120 },
                { "wait two minutes", 120 },
                { "wait 3 minutes", 180 },
                { "wait three minutes", 180 },
                { "wait 4 minutes", 240 },
                { "wait four minutes", 240 },
                { "wait 5 minutes", 300 },
                { "wait five minutes", 300 }
            };


            var speakingPrefixes = new string[] { "ask", "tell", "explain", "say", "speak", "inquire", "request", "greet" };
            bool isSpeakingCommand = speakingPrefixes.Any(prefix => Command.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            string[] pronouns = { "he", "she", "it", "they", "him", "her", "them", "that" };

            foreach (var pronoun in pronouns)
            {
                // Use regular expression to match whole words
                string pattern = @"\b" + Regex.Escape(pronoun) + @"\b";
                Command = Regex.Replace(Command, pattern, "/p");
            }


            if (new List<string> { "leave ~", "exit ~", "leave the structure", "exit the structure", "leave", "leave the building", "exit the building", "exit" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 20));
                if (Executor.Structure == null)
                {
                    MakeObservation("You are not in a structure.", Color.Yellow);
                }
                else if (Executor.Room != Executor.Structure.Rooms[0])
                {
                    MakeObservation("There is not a door to exit through.", Color.Yellow);
                }
                else 
                {
                    Executor.Room.Architects.Remove(Executor);
                    Executor.Structure = null;
                    Executor.Room = null;
                    Executor.Block.Architects.Add(Executor);
                }
            }
            else if (new List<string> { "enter ~", "go inside ~", "go in ~", "go through ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 20));
                if (LoadedArchitects[ArchitectIndex].Structure != null && Subjects[0] is Door)
                {
                    Executor.Room.Architects.Remove(Executor);
                    Executor.Room = ((Door)Subjects[0]).DestinationRoom;
                    Executor.Room.Architects.Add(Executor);
                }
                else if (LoadedArchitects[ArchitectIndex].Structure == null && Subjects[0] is Structure)
                {
                    Executor.Structure = (Structure)Subjects[0];
                    Executor.Room = ((Structure)Subjects[0]).Rooms[0];
                    Executor.Block.Architects.Remove(Executor);
                    Executor.Structure.Rooms[0].Architects.Add(Executor);

                    Exposition.Add(new TextStorage(Executor.Name + " enters " + ((Structure)Subjects[0]).Name + ", a " + ((Structure)Subjects[0]).Type + ".", Color.Blue));

                    if (((Structure)Subjects[0]).PrimarySmells.Count > 0)
                    {
                        Exposition.Add(new TextStorage("The fresh scent of " + ((Structure)Subjects[0]).PrimarySmells[0] + " fills the area.", Color.Yellow));
                    }
                    if (((Structure)Subjects[0]).Type == "temple" && ((Structure)Subjects[0]).Rooms.Any(room => room.Objects.Any(obj => obj.Type == "altar")))
                    {
                        Exposition.Add(new TextStorage("An altar lies in the grand hall of this temple. Maybe you could offer it something?", Color.Yellow));
                    }

                    GameState = "exposition";
                }
                else
                {
                    MakeObservation("You couldn't find anything like that in the area to enter.", Color.Yellow);
                }
            }
            else if (new List<string> { "go ~", "travel ~", "move to the ~", "move ~", "go to the ~", "head ~", "head to the ~", "make my way ~", "start heading ~" }.Contains(Command))
            {
                // Assuming Subjects[0].Metadata contains the direction (e.g., "north", "south", etc.)
                var stringDirectionOffsets = new Dictionary<string, (int dx, int dz)>
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

                if(!TriedFakeMove)
                {
                    MakeObservation("Some commands have shortcuts. For instance, directional movement can use the NUMPAD or by pressing tilde/QWEADZXC.", Color.Lime);
                    TriedFakeMove = true;
                }

                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 25));

                if (stringDirectionOffsets.TryGetValue(Subjects[0].Metadata, out var offset))
                {
                    if (LoadedArchitects[ArchitectIndex].CombatCycles == 0 || r.Next(100) <= LoadedArchitects[ArchitectIndex].EscapeChance())
                    {
                        if (LoadedArchitects[ArchitectIndex].CurrentlyMovingPlace == Subjects[0].Metadata)
                        {
                            if (LoadedArchitects[ArchitectIndex].CombatCycles != 0) // Adjusted from Executor to maintain consistency in explanation
                            {
                                MakeObservation("You struggle to escape, and succeed!", Color.OrangeRed);
                            }

                            int newX = LoadedArchitects[ArchitectIndex].Block.X + offset.dx;
                            int newZ = LoadedArchitects[ArchitectIndex].Block.Z + offset.dz;

                            if (newX == -1 || newX == 7 || newZ == -1 || newZ == 7)
                            {
                                GameState = "travelmenu";
                                GamePlayerParty.MapCursorDistrict = 0;

                                int PartyArchitectsStillThere = 0;
                                Location LeavingLocation = LoadedArchitects[ArchitectIndex].Location;

                                foreach (Architect a in GamePlayerParty.Architects)
                                {
                                    if (a.Location == LeavingLocation)
                                    {
                                        PartyArchitectsStillThere++;
                                    }
                                }
                                if (PartyArchitectsStillThere == 0)
                                {
                                    LeavingLocation.Unload();
                                }

                                MapCursorX = LoadedArchitects[ArchitectIndex].Location.X;
                                MapCursorZ = LoadedArchitects[ArchitectIndex].Location.Z;
                                GameWorld.RevealNearbyTiles(MapCursorX, MapCursorZ);
                            }
                            else
                            {
                                LoadedArchitects[ArchitectIndex].Block.Architects.Remove(LoadedArchitects[ArchitectIndex]);
                                LoadedArchitects[ArchitectIndex].Block = LoadedArchitects[ArchitectIndex].District.DistrictMap[newX + newZ * 7];

                                foreach (Structure s in LoadedArchitects[ArchitectIndex].Block.Structures)
                                {
                                    if (s.Type != "house" && s.Type != "bighouse")
                                    {
                                        MakeObservation(s.GetStructureDescription(), Color.Aqua);
                                    }
                                }
                                LoadedArchitects[ArchitectIndex].Block.Architects.Add(LoadedArchitects[ArchitectIndex]);
                            }

                            LoadedArchitects[ArchitectIndex].CurrentlyMovingPlace = "none";
                        }
                        else
                        {
                            // Adding a check to only show progress message when in combat
                            if (LoadedArchitects[ArchitectIndex].CombatCycles > 0)
                            {
                                // Assuming a simplistic approach to determine "close" escape attempts
                                int escapeAttempt = r.Next(100);
                                int escapeThreshold = LoadedArchitects[ArchitectIndex].EscapeChance() - 10; // Assuming "close" if within 10% of success

                                if (escapeAttempt <= escapeThreshold)
                                {
                                    MakeObservation("You struggle to escape, and make progress...", Color.OrangeRed);
                                }
                                else
                                {
                                    MakeObservation("You struggle to escape, and fail!", Color.OrangeRed);
                                }
                            }

                            LoadedArchitects[ArchitectIndex].CurrentlyMovingPlace = Subjects[0].Metadata;
                        }
                    }
                    else
                    {
                        MakeObservation("You struggle to escape, and fail!", Color.OrangeRed);
                    }
                }
                else
                {
                    MakeObservation("You can't go that \"way\".", Color.Yellow);
                }
            }

            else if (new List<string> {
                "slash ~", "stab ~", "thrust ~", "smite ~", "pierce ~", "lash ~", "scourge ~", "whip ~",
                "strike ~", "bash ~", "crush ~", "whack ~", "smash ~", "hack ~", "sunder ~", "pierce ~",
                "impale ~", "cut ~", "slice ~", "bludgeon ~", "club ~", "smother ~",
                "slash at ~", "stab at ~", "thrust at ~", "smite at ~", "pierce at ~", "lash at ~", "scourge at ~", "whip at ~",
                "strike at ~", "bash at ~", "crush at ~", "whack at ~", "smash at ~", "hack at ~", "sunder at ~", "pierce at ~",
                "impale at ~", "cut at ~", "slice at ~", "bludgeon at ~", "club at ~", "smother at ~"
            }.Contains(Command))
            {
                //find weapon and then calculate the attack

                Object Weapon;

                // Check the player's main hand based on their handedness
                Object MainHandObject = LoadedArchitects[ArchitectIndex].RightHanded ? LoadedArchitects[ArchitectIndex].MainHandObject() : LoadedArchitects[ArchitectIndex].OffHandObject();
                Object OffHandObject = LoadedArchitects[ArchitectIndex].RightHanded ? LoadedArchitects[ArchitectIndex].OffHandObject() : LoadedArchitects[ArchitectIndex].MainHandObject();

                if (MainHandObject != null && MainHandObject.IsWeapon)
                {
                    // If the main hand has a weapon, use it
                    Weapon = MainHandObject;
                }
                else if (OffHandObject != null && OffHandObject.IsWeapon)
                {
                    // If the off hand has a weapon, use it
                    Weapon = OffHandObject;
                }
                else
                {
                    // If both hands are empty or don't have weapons, find a weapon on the body
                    Weapon = LoadedArchitects[ArchitectIndex].BodyParts[r.Next(LoadedArchitects[ArchitectIndex].BodyParts.Count)];
                }


                if (Weapon.WeaponMaximumRange >= Executor.GetDistance(Subjects[0]))
                {
                    CalculateAttack(Command.Substring(Command.Length - 2), Executor, Subjects[0].ReferredToNames[0], "decideforme", Weapon);
                }
                else
                {
                    Announcements.Add(new TextStorage("You wave your hands around, but you aren't close enough.", Color.Yellow));
                }
            }
            else if (new List<string> {
                "slash ~ with ~", "stab ~ with ~", "thrust ~ with ~", "smite ~ with ~", "pierce ~ with ~", "lash ~ with ~", "scourge ~ with ~", "whip ~ with ~",
                "strike ~ with ~", "bash ~ with ~", "crush ~ with ~", "whack ~ with ~", "smash ~ with ~", "hack ~ with ~", "sunder ~ with ~", "pierce ~ with ~",
                "impale ~ with ~", "cut ~ with ~", "slice ~ with ~", "bludgeon ~ with ~", "club ~ with ~", "smother ~ with ~",
                "slash at ~ with ~", "stab at ~ with ~", "thrust at ~ with ~", "smite at ~ with ~", "pierce at ~ with ~", "lash at ~ with ~", "scourge at ~ with ~", "whip at ~ with ~",
                "strike at ~ with ~", "bash at ~ with ~", "crush at ~ with ~", "whack at ~ with ~", "smash at ~ with ~", "hack at ~ with ~", "sunder at ~ with ~", "pierce at ~ with ~",
                "impale at ~ with ~", "cut at ~ with ~", "slice at ~ with ~", "bludgeon at ~ with ~", "club at ~ with ~", "smother at ~ with ~"
            }.Contains(Command))
            {
                Object Weapon = LoadedArchitects[ArchitectIndex].MainHandObject() == Subjects[1] ? LoadedArchitects[ArchitectIndex].MainHandObject() : LoadedArchitects[ArchitectIndex].OffHandObject() == Subjects[1] ? LoadedArchitects[ArchitectIndex].OffHandObject() : null;

                if (Weapon != null && (Weapon.IsWeapon && Weapon.WeaponMaximumRange >= Executor.GetDistance(Subjects[0])))
                {
                    CalculateAttack(Command.Substring(Command.Length - 2), Executor, Subjects[0].ReferredToNames[0], "decideforme", (Object)(Subjects[1]));
                }
                else if (Weapon == null || !Weapon.IsWeapon)
                {
                    MakeObservation("You need to have that object in your hands.", Color.Yellow);
                }
                else
                {
                    Announcements.Add(new TextStorage("You wave your hands around, but you aren't close enough.", Color.Yellow));
                }
            }

            else if (new List<string> { "check my inventory", "open my inventory", "open my pack", "open my backpack", "search my backpack", "open pack", "open menu",
                "show menu",
                "access menu",
                "display menu",
                "main menu",
                "game menu",
                "menu",
                "pause menu",
                "menu open",
                "open game menu",
                "show main menu",
                "access main menu",
                "menu screen","check my inventory",
                "open my inventory",
                "open my pack",
                "open my backpack",
                "search my backpack",
                "open pack",
                "access inventory",
                "access my inventory",
                "show inventory",
                "show my inventory",
                "check inventory",
                "check backpack",
                "inventory",
                "backpack",
                "view inventory",
                "view my inventory",
                "manage inventory",
                "manage my inventory",
                "inventory check",
                "inventory access",
                "inventory open",
                "open inventory",
                "display inventory",
                "display my inventory",
                "open menu screen",
                "check stats",
                "view stats",
                "show stats",
                "display stats",
                "see stats",
                "stats",
                "character stats",
                "open stats",
                "stats check",
                "examine stats",
                "statistics",
                "view my stats",
                "access stats",
                "my stats",
                "check character",
                "view character",
                "examine character",
                "character info",
                "character details",
                "show character",
                "display character",
                "character profile",
                "open character",
                "character check",
                "look at character",
                "see character",
                "character examination",
                "inspect character" }.Contains(Command))
            {
                InInventory = true;
            }
            else if (new List<string> {
                "slash ~ in the ~", "stab ~ in the ~", "thrust ~ in the ~", "smite ~ in the ~", "pierce ~ in the ~",
                "lash ~ in the ~", "scourge ~ in the ~", "whip ~ in the ~", "strike ~ in the ~", "bash ~ in the ~",
                "crush ~ in the ~", "whack ~ in the ~", "smash ~ in the ~", "hack ~ in the ~", "sunder ~ in the ~",
                "pierce ~ in the ~", "impale ~ in the ~", "cut ~ in the ~", "slice ~ in the ~", "bludgeon ~ in the ~",
                "club ~ in the ~", "smother ~ in the ~"
            }.Contains(Command))
            {
                Object Weapon;

                // Check the player's main hand based on their handedness
                Object mainHand = LoadedArchitects[ArchitectIndex].RightHanded ? LoadedArchitects[ArchitectIndex].MainHandObject() : LoadedArchitects[ArchitectIndex].OffHandObject();
                Object offHand = LoadedArchitects[ArchitectIndex].RightHanded ? LoadedArchitects[ArchitectIndex].OffHandObject() : LoadedArchitects[ArchitectIndex].MainHandObject();

                if (mainHand != null && mainHand.IsWeapon)
                {
                    // If the main hand has a weapon, use it
                    Weapon = mainHand;
                }
                else if (offHand != null && offHand.IsWeapon)
                {
                    // If the off hand has a weapon, use it
                    Weapon = offHand;
                }
                else
                {
                    // If both hands are empty or don't have weapons, find a weapon on the body
                    Weapon = LoadedArchitects[ArchitectIndex].FindBodyPart("");
                }


                if (Subjects[0] is Architect)
                {
                    if (((Architect)(Subjects[0])).BodyParts.Any(bodyPart => bodyPart.Type == Subjects[1].Metadata))
                    {
                        CalculateAttack(Command.Substring(0, Command.Length - 2), Executor, (((Architect)(Subjects[0])).FindBodyPart(Subjects[1].Metadata)).Name, "decideforme", Weapon);
                    }
                    else
                    {
                        MakeObservation("The targetted creature doesn't have one of those, or you are not being specific enough (try left X, right X...?)", Color.Yellow);
                    }
                }
                else
                {
                    MakeObservation("You can't target body parts of an object.", Color.Yellow);
                }
            }
            
            else if (new List<string> {
                "become one with shadow", "become one with the shadow", "become one with shadows", "become one with the shadows"
            }.Contains(Command))
            {
                if (Executor.Invisible)
                {
                    MakeObservation("You are already in the shadows.", Color.Yellow);
                }
                if (Executor.PathOfShadowLevel >= 4)
                {
                    MakeObservation("You enter the darkness.", Color.Gray);
                    Executor.Invisible = true;
                }
                else
                {
                    MakeObservation("You are not experienced enough in the shadows to partake in such a maneuver.", Color.Yellow);
                }
            }
            else if (new List<string> {
                "exit the shadows", "exit the darkness", "return from the shadows", "return from the shadow", "return from shadow"
            }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 5));
                if (Executor.Invisible)
                {
                    MakeObservation("You exit the shadows.", Color.Gray);
                    Executor.Invisible = false;
                }
                else
                {
                    MakeObservation("You are not in the shadows.", Color.Yellow);
                }
            }
            else if (Command == "level ~" && Subjects[0].Metadata == "up")
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 5));
                Executor.Level++;
                Executor.SpendableLevels++;
                MakeObservation("You divine the imbuement of great power.", Color.Yellow);

            }
            else if (new List<string> {
                "slash ~ in the ~ with ~", "stab ~ in the ~ with ~", "thrust ~ in the ~ with ~", "smite ~ in the ~ with ~",
                "pierce ~ in the ~ with ~", "lash ~ in the ~ with ~", "scourge ~ in the ~ with ~", "whip ~ in the ~ with ~",
                "strike ~ in the ~ with ~", "bash ~ in the ~ with ~", "crush ~ in the ~ with ~", "whack ~ in the ~ with ~",
                "smash ~ in the ~ with ~", "hack ~ in the ~ with ~", "sunder ~ in the ~ with ~", "pierce ~ in the ~ with ~",
                "impale ~ in the ~ with ~", "cut ~ in the ~ with ~", "slice ~ in the ~ with ~", "bludgeon ~ in the ~ with ~",
                "club ~ in the ~ with ~", "smother ~ in the ~ with ~"
            }.Contains(Command))
            {
                if (Subjects[0] is Architect)
                {
                    if (((Architect)(Subjects[0])).BodyParts.Any(bodyPart => bodyPart.Type == Subjects[1].Metadata))
                    {
                        if (Subjects[2] is Object)
                        {
                            if (LoadedArchitects[ArchitectIndex].MainHandObject() == Subjects[2] || LoadedArchitects[ArchitectIndex].OffHandObject() == Subjects[2])
                            {
                                CalculateAttack(Command.Substring(Command.Length - 2), Executor, ((Architect)Subjects[0]).FindBodyPart((Subjects[1].Metadata)).Name, "decideforme", (Object)(Subjects[2]));
                            }
                            else
                            {
                                MakeObservation("You need to have that object in your hands.", Color.Yellow);
                            }
                        }
                        else
                        {
                            MakeObservation("You can't attack with something that isn't an object.", Color.Yellow);
                        }
                    }
                    else
                    {
                        MakeObservation("The targetted creature doesn't have one of those, or you are not being specific enough (try left X, right X...?)", Color.Yellow);
                    }
                }
                else
                {
                    MakeObservation("You can't target body parts of an object.", Color.Yellow);
                }
            }
            else if (new List<string> {
    "engage ~", "engage with ~", "confront ~", "focus ~"
}.Contains(Command))
            {
                if (Subjects[0] is Architect targetArchitect)
                {
                    if (ArchitectsToUse.Contains(targetArchitect))
                    {
                        foreach (var architect in ArchitectsToUse)
                        {
                            if (architect == targetArchitect) // The target architect
                            {
                                Executor.DistanceFromArchitect(architect, -1); // Decrease distance by 1
                            }
                            else
                            {
                                Executor.DistanceFromArchitect(architect, 1); // Increase distance with all others by 1
                            }
                        }
                        MakeObservation("You focus your target, shifting distances.", Color.Green);
                        Executor.CooldownCycles += (int)(4 * Math.Round(Executor.Speed()));
                    }
                    else
                    {
                        MakeObservation("The target architect is not in the same area.", Color.Yellow);
                    }
                }
                else
                {
                    MakeObservation("The target is not an architect.", Color.Red);
                }
            }
            else if (new List<string> {
    "approach ~", "move closer to ~", "advance towards ~"
}.Contains(Command))
            {
                if (Subjects[0] is Architect targetArchitect)
                {
                    if (ArchitectsToUse.Contains(targetArchitect))
                    {
                        Executor.DistanceFromArchitect(targetArchitect, -2); // Decrease distance by 2
                        MakeObservation("You move closer to the target.", Color.Green);
                        Executor.CooldownCycles += (int)(4 * Math.Round(Executor.Speed()));
                    }
                    else
                    {
                        MakeObservation("The target architect is not in the same area.", Color.Yellow);
                    }
                }
                else
                {
                    MakeObservation("The target is not an architect.", Color.Red);
                }
            }
            else if (new List<string> {
    "distance from ~", "move away from ~", "retreat from ~"
}.Contains(Command))
            {
                if (Subjects[0] is Architect targetArchitect)
                {
                    if (ArchitectsToUse.Contains(targetArchitect))
                    {
                        Executor.DistanceFromArchitect(targetArchitect, 2); // Increase distance by 2
                        MakeObservation("You increase your distance from the target.", Color.Green);
                        Executor.CooldownCycles += (int)(4 * Math.Round(Executor.Speed()));
                    }
                    else
                    {
                        MakeObservation("The target architect is not in the same area.", Color.Yellow);
                    }
                }
                else
                {
                    MakeObservation("The target is not an architect.", Color.Red);
                }
            }

            else if (new List<string> { "take out ~", "unsheath ~", "remove ~", "wield ~", "unholster ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 5));
                if (Subjects[0] is Object && LoadedArchitects[ArchitectIndex].Inventory.Contains((Object)Subjects[0]))
                {
                    if (LoadedArchitects[ArchitectIndex].RightHanded)
                    {
                        if (LoadedArchitects[ArchitectIndex].RightHandObject == null)
                        {
                            LoadedArchitects[ArchitectIndex].RightHandObject = ((Object)Subjects[0]);
                            LoadedArchitects[ArchitectIndex].Inventory.Remove(((Object)Subjects[0]));
                        }
                        else if (LoadedArchitects[ArchitectIndex].LeftHandObject == null)
                        {
                            LoadedArchitects[ArchitectIndex].LeftHandObject = ((Object)Subjects[0]);
                            LoadedArchitects[ArchitectIndex].Inventory.Remove(((Object)Subjects[0]));
                        }
                        else
                        {
                            MakeObservation("Your hands are full.", Color.Yellow);
                        }
                    }
                    else
                    {
                        if (LoadedArchitects[ArchitectIndex].LeftHandObject == null)
                        {
                            LoadedArchitects[ArchitectIndex].LeftHandObject = ((Object)Subjects[0]);
                            LoadedArchitects[ArchitectIndex].Inventory.Remove(((Object)Subjects[0]));
                        }
                        else if (LoadedArchitects[ArchitectIndex].RightHandObject == null)
                        {
                            LoadedArchitects[ArchitectIndex].LeftHandObject = ((Object)Subjects[0]);
                            LoadedArchitects[ArchitectIndex].Inventory.Remove(((Object)Subjects[0]));
                        }
                        else
                        {
                            MakeObservation("Your hands are full.", Color.Yellow);
                        }
                    }
                }
                else
                {
                    MakeObservation("That is not an object in your inventory.", Color.Yellow);
                }
            }
            else if (new List<string> { "grab ~", "get ~", "take ~", "pick up ~", "steal ~", "place ~ in my inventory", "store ~ in my inventory", "stash ~ in my inventory", "put ~ in my inventory", "place ~ in my pack", "store ~ in my pack", "stash ~ in my pack", "put ~ in my pack", "place ~ in my backpack", "store ~ in my backpack", "stash ~ in my backpack", "put ~ in my backpack" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 5));
                if (LoadedArchitects[ArchitectIndex].Room != null && LoadedArchitects[ArchitectIndex].Room.Objects.Contains(Subjects[0]))
                {
                    MakeObservation("You pick up the " + Subjects[0].ReferredToNames[0] + " and put it in your inventory.", Color.Yellow);
                    LoadedArchitects[ArchitectIndex].Room.Objects.Remove((Object)Subjects[0]);
                    LoadedArchitects[ArchitectIndex].Inventory.Add((Object)Subjects[0]);

                    ((Object)Subjects[0]).Structure = null;
                    ((Object)Subjects[0]).Block = null;

                    if (((Object)Subjects[0]).Imbuements.Count > 1 || ((Object)Subjects[0]).IsWeapon || ((Object)Subjects[0]).Name != null)
                    {
                        IsInGui = true;

                        if (((Object)Subjects[0]).Name != null)
                        {
                            ItemPickupGuiLines.Add(Capitalize(((Object)Subjects[0]).Name) + ", " + Capitalize(((Object)Subjects[0]).Materials[0].Name) + " " + Capitalize(((Object)Subjects[0]).Type));
                        }
                        else
                        {
                            ItemPickupGuiLines.Add(Capitalize(((Object)Subjects[0]).Materials[0].Name) + " " + Capitalize(((Object)Subjects[0]).Type));
                        }

                        if (((Object)Subjects[0]).Imbuements.Count == 0)
                        {
                            ItemPickupGuiLines.Add("This object has no imbuements.");
                        }
                        else
                        {
                            List<string> ImbuementDescriptions = new List<string>();
                            foreach (Imbuement i in ((Object)Subjects[0]).Imbuements)
                            {
                                ImbuementDescriptions.Add(i.GetDescription());
                            }
                            ItemPickupGuiLines.Add("This object has some magical properties. " + ConvertListToString(ImbuementDescriptions));
                        }
                    }

                    if (Executor.Structure != null && Executor.Structure.Type == "market")
                    {
                        Executor.Structure.MarketDebt -= ((Object)(Subjects[0])).Value();
                    }
                }
                else if (LoadedArchitects[ArchitectIndex].Room == null && LoadedArchitects[ArchitectIndex].Block.Objects.Contains(Subjects[0]))
                {
                    MakeObservation("You pick up the " + Subjects[0].ReferredToNames[0] + " and put it in your inventory.", Color.Yellow);
                    LoadedArchitects[ArchitectIndex].Block.Objects.Remove((Object)Subjects[0]);
                    LoadedArchitects[ArchitectIndex].Inventory.Add((Object)Subjects[0]);

                    ((Object)Subjects[0]).Structure = null;
                    ((Object)Subjects[0]).Block = null;
                    ((Object)Subjects[0]).Room = null;

                    if (((Object)Subjects[0]).Imbuements.Count > 1 || ((Object)Subjects[0]).IsWeapon || ((Object)Subjects[0]).Name != null)
                    {
                        IsInGui = true;

                        if (((Object)Subjects[0]).Name != null)
                        {
                            ItemPickupGuiLines.Add(Capitalize(((Object)Subjects[0]).Name) + ", " + Capitalize(((Object)Subjects[0]).Materials[0].Name) + " " + Capitalize(((Object)Subjects[0]).Type));
                        }
                        else
                        {
                            ItemPickupGuiLines.Add(Capitalize(((Object)Subjects[0]).Materials[0].Name) + " " + Capitalize(((Object)Subjects[0]).Type));
                        }

                        if (((Object)Subjects[0]).Imbuements.Count == 0)
                        {
                            ItemPickupGuiLines.Add("This object has no imbuements.");
                        }
                        else
                        {
                            List<string> ImbuementDescriptions = new List<string>();
                            foreach (Imbuement i in ((Object)Subjects[0]).Imbuements)
                            {
                                ImbuementDescriptions.Add(i.GetDescription());
                            }
                            ItemPickupGuiLines.Add("This object has some magical properties. " + ConvertListToString(ImbuementDescriptions));
                        }
                    }
                }
                else if (LoadedArchitects[ArchitectIndex].LeftHandObject == Subjects[0])
                {
                    MakeObservation("You stash the " + Subjects[0].ReferredToNames[0] + ".", Color.Yellow);
                    LoadedArchitects[ArchitectIndex].LeftHandObject = null;
                    LoadedArchitects[ArchitectIndex].Inventory.Add((Object)Subjects[0]);
                }
                else if (LoadedArchitects[ArchitectIndex].RightHandObject == Subjects[0])
                {
                    MakeObservation("You stash the " + Subjects[0].ReferredToNames[0] + ".", Color.Yellow);
                    LoadedArchitects[ArchitectIndex].RightHandObject = null;
                    LoadedArchitects[ArchitectIndex].Inventory.Add((Object)Subjects[0]);
                }
                else
                {
                    MakeObservation("You couldn't find anything like that in the area.", Color.Yellow);
                }
            }
            else if (new List<string> { "drop ~", "set ~ on the ground", "place ~ on the ground", "let go of ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 5));
                bool Found = true;
                if (Subjects[0] == Executor.RightHandObject)
                {
                    Executor.RightHandObject = null;
                }
                else if (Subjects[0] == Executor.LeftHandObject)
                {
                    Executor.LeftHandObject = null;
                }
                else if (Executor.Inventory.Contains(Subjects[0]))
                {
                    Executor.Inventory.Remove((Object)(Subjects[0]));
                }
                else
                {
                    Found = false;
                }

                if (Found)
                {
                    if (Executor.Room != null)
                    {
                        Executor.Room.Objects.Add((Object)(Subjects[0]));
                        MakeObservation("You drop the " + Subjects[0].ReferredToNames[0] + ".", Color.Yellow);
                    }
                    else
                    {
                        Executor.Block.Objects.Add((Object)(Subjects[0]));
                        MakeObservation("You drop the " + Subjects[0].ReferredToNames[0] + ".", Color.Yellow);
                    }

                    if (Executor.Structure != null && Executor.Structure.Type == "market")
                    {
                        Executor.Structure.MarketDebt += ((Object)(Subjects[0])).Value();
                    }
                }
                else
                {
                    MakeObservation("You don't have that.", Color.Yellow);
                }

            }
            else if (new List<string> { "place ~ in ~", "store ~ in ~", "stash ~ in ~", "put ~ in ~", "place ~ on ~", "place ~ inside ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 5));
                if (Subjects[0] == Executor.RightHandObject || Subjects[0] == Executor.LeftHandObject || Executor.Inventory.Contains(Subjects[0]))
                {
                    // Check if the subject is "shadow storage" directly, avoiding the need for it to be an Object
                    if (Subjects[1].Metadata == "shadow storage")
                    {
                        // Shadow storage logic
                        if (!Executor.ShadowStorage.Contains((Object)Subjects[0]))
                        {
                            Executor.ShadowStorage.Add((Object)Subjects[0]);

                            // Optionally clear the object from hands or inventory, but keep it linked to shadow storage
                            if (Subjects[0] == Executor.RightHandObject)
                            {
                                Executor.RightHandObject = null;
                            }
                            else if (Subjects[0] == Executor.LeftHandObject)
                            {
                                Executor.LeftHandObject = null;
                            }
                            else if (Executor.Inventory.Contains(Subjects[0]))
                            {
                                Executor.Inventory.Remove((Object)(Subjects[0]));
                            }

                            MakeObservation("You place the " + Subjects[0].ReferredToNames[0] + " into the shadow storage.", Color.Green);
                        }
                        else
                        {
                            MakeObservation("The item is already in the shadow storage.", Color.Green);
                        }
                    }
                    else if (Subjects[1] is Object subjectObject && subjectObject.IsContainer)
                    {
                        // Handling normal container logic
                        if (Subjects[0] == Executor.RightHandObject)
                        {
                            Executor.RightHandObject = null;
                        }
                        else if (Subjects[0] == Executor.LeftHandObject)
                        {
                            Executor.LeftHandObject = null;
                        }
                        else if (Executor.Inventory.Contains(Subjects[0]))
                        {
                            Executor.Inventory.Remove((Object)(Subjects[0]));
                        }

                        subjectObject.ContainedObjects.Add((Object)Subjects[0]);
                    }
                    else
                    {
                        MakeObservation(Subjects[1].ReferredToNames[0] + " can't hold anything.", Color.Green);
                    }
                }
                else
                {
                    if (Executor.Clothing.Contains(Subjects[1]))
                    {
                        if (Executor.Sex == "male")
                        {
                            MakeObservation("You are going to have to take that off first, sir.", Color.Green);
                        }
                        else
                        {
                            MakeObservation("You are going to have to take that off first, madame.", Color.Green);
                        }
                    }
                    else
                    {
                        MakeObservation("You don't have that.", Color.Green);
                    }
                }
            }
            else if (new List<string> { "savepoint", "pausepoint", "save" }.Contains(Command))
            {
                if(Executor.PathOfTimeLevel >= 0)
                {

                }
            }
            else if (new List<string> { "take ~ from ~", "remove ~ from ~", "retrieve ~ from ~", "get ~ from ~", "extract ~ from ~", "take ~ off of ~", "remove ~ off of  ~", "retrieve ~ off of ~", "get ~ off of ~", "extract ~ off of ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 5));
                if (Subjects[1].Metadata == "shadow storage")
                {
                    if (Executor.Room == null)
                    {
                        foreach (Object o in Executor.Block.Objects)
                        {
                            if (o.Type == "shadow storage")
                            {
                                // Assuming we have a way to check if the object is linked in the shadow storage.
                                if (Executor.ShadowStorage.Contains((Object)Subjects[0]))
                                {
                                    // Assuming the executor can carry the object
                                    if (Executor.RightHandObject == null)
                                    {
                                        Executor.RightHandObject = (Object)Subjects[0];
                                    }
                                    else if (Executor.LeftHandObject == null)
                                    {
                                        Executor.LeftHandObject = (Object)Subjects[0];
                                    }
                                    else
                                    {
                                        Executor.Inventory.Add((Object)Subjects[0]);
                                    }

                                    // Note: The object isn't removed from ShadowStorage to maintain the link.
                                    MakeObservation("You retrieve the " + Subjects[0].ReferredToNames[0] + " from the shadow storage.", Color.Green);
                                }
                                else
                                {
                                    MakeObservation("The shadow storage does not contain that.", Color.Green);
                                }
                                break; // Exit the loop once the shadow storage is processed
                            }
                        }
                    }
                }
                else
                {
                    if (Subjects[1] is Object && ((Object)Subjects[1]).IsContainer && ((Object)Subjects[1]).ContainedObjects.Contains((Object)Subjects[0]))
                    {
                        // Assuming the executor can carry the object
                        if (Executor.RightHandObject == null)
                        {
                            Executor.RightHandObject = (Object)Subjects[0];
                        }
                        else if (Executor.LeftHandObject == null)
                        {
                            Executor.LeftHandObject = (Object)Subjects[0];
                        }
                        else
                        {
                            Executor.Inventory.Add((Object)Subjects[0]);
                        }

                        ((Object)Subjects[1]).ContainedObjects.Remove((Object)Subjects[0]);

                        MakeObservation("You remove the " + Subjects[0].ReferredToNames[0] + " from the " + Subjects[1].ReferredToNames[0] + ".", Color.Green);
                    }
                    else if (!(Subjects[1] is Object) || !((Object)Subjects[1]).IsContainer)
                    {
                        MakeObservation(Subjects[1].ReferredToNames[0] + " is not a container.", Color.Green);
                    }
                    else if (!((Object)Subjects[1]).ContainedObjects.Contains((Object)Subjects[0]))
                    {
                        MakeObservation("The " + Subjects[1].ReferredToNames[0] + " does not contain that.", Color.Green);
                    }
                    else
                    {
                        // Handle case where object cannot be taken for some other reason
                        MakeObservation("Can't take that for some reason.", Color.Green);
                    }
                }
            }
            else if (waitCommands.ContainsKey(Command.ToLower()))
            {
                int secondsToWait = waitCommands[Command.ToLower()];
                Executor.CooldownCycles += secondsToWait * 10;
                string observationMessage = secondsToWait == 1 ? "You wait for one second." : $"You wait for {secondsToWait} seconds.";
                MakeObservation(observationMessage, Color.Green);
            }
            else if (Command.ToLower().StartsWith("wait"))
            {
                MakeObservation("You're too impatient to wait that long.", Color.Red);
            }
            else if (new List<string> { "wear ~", "put on ~", "don ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 20));
                if (Subjects[0] is Object && (Executor.Inventory.Contains(((Object)Subjects[0])) || Executor.RightHandObject == ((Object)Subjects[0]) || Executor.LeftHandObject == ((Object)Subjects[0])))
                {
                    if (((Object)Subjects[0]).IsWearable)
                    {
                        if (Executor.Clothing.Any(c => c.Type == ((Object)Subjects[0]).Type) && ((Object)Subjects[0]).Type != "amulet")
                        {
                            MakeObservation($"You can't wear more than one {((Object)Subjects[0]).Type}, fascist.", Color.Yellow);
                        }
                        else
                        {
                            MakeObservation("You put on the " + Subjects[0].ReferredToNames[0] + ".", Color.Green);

                            if (Executor.Inventory.Contains(((Object)Subjects[0])))
                            {
                                Executor.Inventory.Remove((Object)Subjects[0]);
                            }
                            else if (Executor.LeftHandObject == ((Object)Subjects[0]))
                            {
                                Executor.LeftHandObject = null;
                            }
                            else
                            {
                                Executor.RightHandObject = null;
                            }

                            Executor.Clothing.Add(((Object)Subjects[0]));
                        }
                    }
                    else
                    {
                        if (Executor.Clothing.Count > 0)
                        {
                            MakeObservation("You hang the " + Subjects[0].ReferredToNames[0] + " off of your " + Executor.Clothing[Game1.r.Next(Executor.Clothing.Count)].ReferredToNames[0] + ". You feel disadvantaged.", Color.Green);
                            Executor.Clothing.Add(((Object)Subjects[0]));
                        }
                        else
                        {
                            MakeObservation("You aren't wearing anything to hang it off of...", Color.Green);
                        }
                    }
                }
                else if (Subjects[0] is Architect)
                {
                    if (((Architect)Subjects[0]).Race == GameWorld.GetRace("shiba") && ((Architect)Subjects[0]).Block == Executor.Block && ((Architect)Subjects[0]).Room == Executor.Room)
                    {
                        MakeObservation("You put on the shiba inu. It melds with your face, and you feel an intense euphoria.", Color.Green);
                    }
                    else
                    {
                        MakeObservation("You can't wear that, it's not a shiba inu.", Color.Green);
                    }
                }
                else
                {
                    MakeObservation("You don't have an object like that.", Color.Green);
                }
            }
            else if (new List<string> { "remove ~", "take off ~", "doff ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 20));
                if (Subjects[0] is Object && Executor.Clothing.Contains(((Object)Subjects[0])))
                {
                    MakeObservation("You take off the " + Subjects[0].ReferredToNames[0] + ".", Color.Green);

                    // Remove the item from the Clothing list
                    Executor.Clothing.Remove((Object)Subjects[0]);

                    // Add the item back to the Executor's inventory
                    Executor.Inventory.Add((Object)Subjects[0]);
                }
                else if (Subjects[0] is Architect)
                {
                    if (((Architect)Subjects[0]).Race == GameWorld.GetRace("shiba") && Executor.MeldedShibas.Contains(Subjects[0]))
                    {
                        MakeObservation("You remove the shiba inu from your face, feeling a sense of loss.", Color.Green);
                        Executor.MeldedShibas.Remove(((Architect)Subjects[0]));

                        ((Architect)Subjects[0]).Room = Executor.Room;
                        ((Architect)Subjects[0]).Block = Executor.Block;

                        if (Executor.Room == null)
                        {
                            Executor.Room.Architects.Add((Architect)Subjects[0]);
                        }
                        else
                        {
                            Executor.Block.Architects.Add((Architect)Subjects[0]);
                        }
                    }
                    else
                    {
                        MakeObservation("You can't take that off, it's not a shiba inu.", Color.Green);
                    }
                }
                else
                {
                    MakeObservation("You aren't wearing an object like that.", Color.Green);
                }
            }

            else if (new List<string>()
            {
                "examine ~",
                "look at ~",
                "check out ~",
                "look closer at ~",
                "inspect ~",
                "observe ~",
                "view ~"
            }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 10));
                if (Subjects[0] is Architect)
                {
                    MakeObservation(Subjects[0].ReferredToNames[0], Color.White);
                    MakeObservation(((Architect)Subjects[0]).Race.Description, Color.LimeGreen);
                    MakeObservation(((Architect)Subjects[0]).CheckEnergyLevel(), Color.Magenta);
                    MakeObservation(((Architect)Subjects[0]).DescribeArchitectInventory(), Color.Orange);
                }
                else if (Subjects[0] is Object)
                {
                    if ((LoadedArchitects[ArchitectIndex].Room != null && LoadedArchitects[ArchitectIndex].Room.Objects.Contains(Subjects[0])) || LoadedArchitects[ArchitectIndex].Block.Objects.Contains(Subjects[0]) || (LoadedArchitects[ArchitectIndex].MainHandObject() == Subjects[0] || LoadedArchitects[ArchitectIndex].OffHandObject() == Subjects[0] || LoadedArchitects[ArchitectIndex].Inventory.Contains(Subjects[0])))
                    {
                        MakeObservation(Subjects[0].ReferredToNames[0], Color.White);
                        MakeObservation(((Object)Subjects[0]).Description, Color.White);

                        string Materials = "Materials: ";
                        List<string> materialNames = ((Object)Subjects[0]).Materials.Select(m => m.Name).ToList();

                        if (materialNames.Count > 1)
                        {
                            // Insert "and" before the last element
                            materialNames[^1] = "and " + materialNames[^1];

                            // Join all elements with a comma, except the last one which already has "and"
                            Materials += String.Join(", ", materialNames);
                        }
                        else if (materialNames.Count == 1)
                        {
                            // If there's only one material, just add it
                            Materials += materialNames[0];
                        }

                        MakeObservation(Materials, Color.White);

                        foreach (Imbuement i in ((Object)Subjects[0]).Imbuements)
                        {
                            MakeObservation(i.GetDescription(), Color.Magenta);
                        }

                    }
                }
                else if (Subjects[0] is Structure && LoadedArchitects[ArchitectIndex].Room == null && LoadedArchitects[ArchitectIndex].Block.Structures.Contains(Subjects[0]))
                {

                }
                else
                {
                    MakeObservation("You couldn't find anything like that nearby.", Color.White);
                }
            }
            else if (new List<string> { "give ~ to ~", "offer ~ to ~", "sacrifice ~ to ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 15));
                if (!(Subjects[0] is Object))
                {
                    MakeObservation("You can't give something that isn't an object.", Color.Yellow);
                    return (false);
                }
                else if ((!(Subjects[1] is Object)) && (!(Subjects[1] is Architect)))
                {
                    MakeObservation("You can't give to something that isn't a person or object.", Color.Yellow);
                    return (false);
                }
                else if (!(Executor.Inventory.Contains(Subjects[0])) && !(Executor.LeftHandObject == Subjects[0]) && !(Executor.RightHandObject == Subjects[0]))
                {
                    MakeObservation("You don't have that in your inventory or hands.", Color.Yellow);
                    return (false);
                }
                else if (Executor.RightHandObject == Subjects[0])
                {
                    Executor.RightHandObject = null;
                }
                else if (Executor.LeftHandObject == Subjects[0])
                {
                    Executor.LeftHandObject = null;
                }
                else if (Executor.Inventory.Contains(Subjects[0]))
                {
                    Executor.Inventory.Remove((Object)(Subjects[0]));
                }

                Object GivenObject = ((Object)(Subjects[0]));

                //if we didnt return it means we found a givingobject and something to give it to. we also took it out of their hands.

                if (Subjects[1] is Architect)
                {
                    AddMessage("You: Here, take this.", Color.White);
                    MakeObservation("You give the " + Subjects[1].ReferredToNames[0] + " to " + GivenObject.ReferredToNames[0] + ".", Color.Blue);
                    AddMessage(Subjects[0].ReferredToNames[0] + ": Thank you! I appreciate it!", Color.White);

                    ((Architect)Subjects[1]).Inventory.Add(GivenObject);
                }
                else
                {
                    //subject 1 is an object

                    if (((Object)Subjects[1]).Type == "altar")
                    {
                        MakeObservation("You place your " + Subjects[0].ReferredToNames[0] + " on the " + Subjects[1].ReferredToNames[0] + ". It fizzles...", Color.Yellow);

                        int Quality = 0;

                        if (((Object)Subjects[0]).Materials.Any(obj => obj.Type == "gemstone"))
                        {
                            Quality = 10;
                        }
                        else if (((Object)Subjects[0]).Materials.Any(obj => obj.Type == "metal"))
                        {
                            Quality = 8;
                        }
                        else if (((Object)Subjects[0]).Materials.Any(obj => obj.Type == "glass"))
                        {
                            Quality = 6;
                        }
                        else if (((Object)Subjects[0]).Materials.Any(obj => obj.Type == "stone"))
                        {
                            Quality = 5;
                        }
                        else if (((Object)Subjects[0]).Materials.Any(obj => obj.Type == "cloth"))
                        {
                            Quality = 4;
                        }
                        else if (((Object)Subjects[0]).Materials.Any(obj => obj.Type == "wood"))
                        {
                            Quality = 2;
                        }

                        int Outcome = (Quality * 2) + r.Next(-2, 3);

                        //outcome will be from 0-22, depending on quality
                        string OutcomeString = (new List<string>() { "reject", "reject", "reject", "coffee", "tea", "divineprotection", "double", "lightninggrenade", "icedtea", "icedcoffee", "spatialgrenade", "double", "heal", "double", "divinemight", "learnspell", "double", "convertmaterialtodivine", "divineweapon" })[Outcome];

                        Deity PrayingDeity;
                        if (LoadedArchitects[ArchitectIndex].Structure == null && LoadedArchitects[ArchitectIndex].Structure.Type == "shrine")
                        {
                            PrayingDeity = LoadedArchitects[ArchitectIndex].Structure.PrayingDeity;
                        }
                        else if (Game1.r.Next(1, 3) == 1)
                        {
                            PrayingDeity = Game1.GameWorld.LightDeity;
                        }
                        else
                        {
                            PrayingDeity = Game1.GameWorld.DarkDeity;
                        }

                        switch (OutcomeString)
                        {
                            case "reject":
                                {
                                    MakeObservation("...and absolutely nothing happens.", Color.Red);
                                    break;
                                }
                            case "coffee":
                                {
                                    MakeObservation(PrayingDeity.Name + " has conjured for you a cup of coffee!", Color.Goldenrod);

                                    Object o = new Object(null, "small cup", new List<Material>() { LoadedArchitects[ArchitectIndex].Location.HomeCivilization.CulturalStone }, PrayingDeity);
                                    o.ContainedObjects.Add(new Object(null, "drink", new List<Material> { GameWorld.Coffee }, PrayingDeity));
                                    if (Executor.Room != null)
                                    {
                                        Executor.Room.Objects.Add(o);
                                    }
                                    else
                                    {
                                        Executor.Block.Objects.Add(o);
                                    }
                                    break;
                                }
                            case "tea":
                                {
                                    MakeObservation(PrayingDeity.Name + " has conjured for you a cup of tea!", Color.Goldenrod);

                                    Object o = new Object(null, "small cup", new List<Material>() { LoadedArchitects[ArchitectIndex].Location.HomeCivilization.CulturalStone }, PrayingDeity);
                                    o.ContainedObjects.Add(new Object(null, "drink", new List<Material> { GameWorld.Tea }, PrayingDeity));
                                    if (Executor.Room != null)
                                    {
                                        Executor.Room.Objects.Add(o);
                                    }
                                    else
                                    {
                                        Executor.Block.Objects.Add(o);
                                    }
                                    break;
                                }
                            case "divineprotection":
                                {
                                    // Code for the 'divineprotection' case
                                    MakeObservation(PrayingDeity.Name + " offers you a barrier between the blades of your enemies!", Color.Goldenrod);
                                    Executor.DivineProtection += 5;
                                    break;
                                }
                            case "double":
                                {
                                    // Code for the 'double' case
                                    MakeObservation(PrayingDeity.Name + " has blessed your offering and doubled it!", Color.Goldenrod);
                                    Executor.Block.Objects.Add(new Object(GivenObject.Name, GivenObject.Type, GivenObject.Materials, GivenObject.IfTrueUseInIfFalseUseOn, GivenObject.IsContainer, GivenObject.Content, GivenObject.Creator, GivenObject.Weight, GivenObject.IsGeneralGood, GivenObject.Block, GivenObject.Structure, GivenObject.Room, GivenObject.IsWearable));
                                    Executor.Block.Objects.Add(new Object(GivenObject.Name, GivenObject.Type, GivenObject.Materials, GivenObject.IfTrueUseInIfFalseUseOn, GivenObject.IsContainer, GivenObject.Content, GivenObject.Creator, GivenObject.Weight, GivenObject.IsGeneralGood, GivenObject.Block, GivenObject.Structure, GivenObject.Room, GivenObject.IsWearable));
                                    break;
                                }
                            case "lightninggrenade":
                                {
                                    MakeObservation(PrayingDeity.Name + " has gifted you a strange sphere filled with lightning...", Color.Goldenrod);
                                    Executor.Block.Objects.Add(new Object(null, "lightning grenade", new List<Material>() { GameWorld.Glass }, PrayingDeity));
                                    break;
                                }
                            case "spatialgrenade":
                                {
                                    MakeObservation(PrayingDeity.Name + " has gifted you a strange sphere filled with purple energy...", Color.Goldenrod);
                                    Executor.Block.Objects.Add(new Object(null, "spatial grenade", new List<Material>() { GameWorld.Glass }, PrayingDeity));
                                    break;
                                }
                            case "icedcoffee":
                                {
                                    MakeObservation(PrayingDeity.Name + " has conjured for you a cup of iced coffee!", Color.Goldenrod);

                                    Object o = new Object(null, "small cup", new List<Material>() { LoadedArchitects[ArchitectIndex].Location.HomeCivilization.CulturalStone }, PrayingDeity);
                                    o.ContainedObjects.Add(new Object(null, "drink", new List<Material> { GameWorld.Coffee }, PrayingDeity));
                                    if (Executor.Room != null)
                                    {
                                        Executor.Room.Objects.Add(o);
                                    }
                                    else
                                    {
                                        Executor.Block.Objects.Add(o);
                                    }
                                    break;
                                }
                            case "icedtea":
                                {
                                    MakeObservation(PrayingDeity.Name + " has conjured for you a cup of iced tea!", Color.Goldenrod);

                                    Object o = new Object(null, "small cup", new List<Material>() { LoadedArchitects[ArchitectIndex].Location.HomeCivilization.CulturalStone }, PrayingDeity);
                                    o.ContainedObjects.Add(new Object(null, "drink", new List<Material> { GameWorld.Tea }, PrayingDeity));
                                    if (Executor.Room != null)
                                    {
                                        Executor.Room.Objects.Add(o);
                                    }
                                    else
                                    {
                                        Executor.Block.Objects.Add(o);
                                    }
                                    break;
                                }
                            case "heal":
                                {
                                    MakeObservation(PrayingDeity.Name + " envelops you in a beautiful energy wave!", Color.Goldenrod);
                                    foreach (Object o in Executor.BodyParts)
                                    {
                                        o.Integrity = 100;
                                    }
                                    Executor.Energy = Executor.MaxEnergy();
                                    break;
                                }
                            case "divinemight":
                                {
                                    // Code for the 'divinemight' case
                                    MakeObservation(PrayingDeity.Name + " offers you a burst of power against your mightiest foes!", Color.Goldenrod);
                                    Executor.DivineMight += 12;
                                    break;
                                }
                            case "learnspell":
                                {
                                    // Code for the 'learnspell' case
                                    MakeObservation(PrayingDeity.Name + " attempts to infuse magic into your being...", Color.Goldenrod);
                                    if (r.Next(1, 3) == 1)
                                    {
                                        var randomSpell = GameWorld.DiscoveredSpells[r.Next(GameWorld.DiscoveredSpells.Count)];
                                        if (!LoadedArchitects[ArchitectIndex].SpellsKnown.Contains(randomSpell))
                                        {
                                            MakeObservation("You feel a tremendous pain, followed by a strange, uplifting peace.", Color.Goldenrod);
                                            LoadedArchitects[ArchitectIndex].SpellsKnown.Add(randomSpell);
                                        }
                                        else
                                        {
                                            MakeObservation("You feel a tremendous pain, followed by an intense feeling of dissatisfaction.", Color.Goldenrod);
                                        }
                                    }
                                    else
                                    {
                                        MakeObservation("You feel a tremendous pain, followed by an intense feeling of dissatisfaction.", Color.Goldenrod);
                                    }
                                    break;
                                }
                            case "convertmaterialtodivine":
                                {
                                    GivenObject.Materials.Clear();
                                    MakeObservation(PrayingDeity.Name + " alters your object into a brilliant form!", Color.Goldenrod);

                                    if (PrayingDeity == GameWorld.LightDeity)
                                    {
                                        GivenObject.Materials.Add(GameWorld.Prismite);
                                    }
                                    else
                                    {
                                        GivenObject.Materials.Add(GameWorld.Shadesteel);
                                    }

                                    if (Executor.Room != null)
                                    {
                                        Executor.Room.Objects.Add(GivenObject);
                                    }
                                    else
                                    {
                                        Executor.Block.Objects.Add(GivenObject);
                                    }
                                    break;
                                }
                            case "divineweapon":
                                {
                                    // Code for the 'divineweapon' case

                                    Material WeaponMaterial;

                                    MakeObservation(PrayingDeity.Name + " reshapes your object into an incredible form!", Color.Goldenrod);

                                    if (PrayingDeity == GameWorld.LightDeity)
                                    {
                                        WeaponMaterial = GameWorld.Prismite;
                                    }
                                    else
                                    {
                                        WeaponMaterial = GameWorld.Shadesteel;
                                    }

                                    if (Executor.Room != null)
                                    {
                                        Executor.Room.Objects.Add(GenerateRandomWeapon(WeaponMaterial, "rare"));
                                    }
                                    else
                                    {
                                        Executor.Block.Objects.Add(GenerateRandomWeapon(WeaponMaterial, "rare"));
                                    }

                                    break;
                                }
                            default:
                                {
                                    // Code for any other case that is not specifically handled
                                    break;
                                }
                        }

                    }
                    else
                    {
                        MakeObservation("You place your " + Subjects[1] + " on the " + Subjects[1] + " and wait, patiently. Nothing happens. You pick it back up.", Color.Yellow);
                        Executor.Inventory.Add((Object)(Subjects[1]));
                    }
                }
            }
            else if (new List<string> { "throw ~", "toss ~", "fling ~" }.Contains(Command))
            {
                Object ThrowingObject = null;
                if (Executor.RightHanded)
                {
                    if (Executor.RightHandObject == Subjects[0])
                    {
                        ThrowingObject = Executor.RightHandObject;
                        Executor.RightHandObject = null;
                    }
                    else if (Executor.LeftHandObject == Subjects[0])
                    {
                        ThrowingObject = Executor.LeftHandObject;
                        Executor.LeftHandObject = null;
                    }
                }
                else
                {
                    if (Executor.LeftHandObject == Subjects[0])
                    {
                        ThrowingObject = Executor.LeftHandObject;
                        Executor.LeftHandObject = null;
                    }
                    else if (Executor.RightHandObject == Subjects[0])
                    {
                        ThrowingObject = Executor.RightHandObject;
                        Executor.RightHandObject = null;
                    }
                }

                if (ThrowingObject == null)
                {
                    if (Executor.LeftHandObject == null && Executor.RightHandObject == null)
                    {
                        Observations.Add(new TextStorage("Your hands are empty. You must have an object in your hands to throw it.", Color.Yellow));
                        Announcements.Add(new TextStorage("Your hands are empty. You must have an object in your hands to throw it.", Color.Yellow));
                    }
                    else
                    {
                        Observations.Add(new TextStorage("You do not have an object like that in your hands.", Color.Yellow));
                        Announcements.Add(new TextStorage("You do not have an object like that in your hands.", Color.Yellow));
                    }
                }
                else
                {
                    Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 10));
                    MakeObservation("You fling your " + Subjects[0] + " at nothing. Expectedly, it falls to the ground.", Color.Yellow);
                    Executor.Inventory.Remove((Object)Subjects[0]);

                    if (Executor.Room == null)
                    {
                        Executor.Block.Objects.Add(ThrowingObject);
                    }
                    else
                    {
                        Executor.Room.Objects.Add(ThrowingObject);
                    }
                }
            }
            else if (new List<string> { "throw ~ at ~", "toss ~ at ~", "fling ~ at ~", "throw ~ towards ~", "toss ~ towards ~", "fling ~ towards ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 10));
                Object ThrowingObject = null;

                if (Executor.LeftHandObject == Subjects[0])
                {
                    ThrowingObject = Executor.LeftHandObject;
                    Executor.LeftHandObject = null;
                }
                else if (Executor.RightHandObject == Subjects[0])
                {
                    ThrowingObject = Executor.RightHandObject;
                    Executor.RightHandObject = null;
                }
                else
                {
                    if (Executor.LeftHandObject == null && Executor.RightHandObject == null)
                    {
                        Observations.Add(new TextStorage("Your hands are empty. You must have an object in your hands to throw it.", Color.Yellow));
                        Announcements.Add(new TextStorage("Your hands are empty. You must have an object in your hands to throw it.", Color.Yellow));
                    }
                    else
                    {
                        Observations.Add(new TextStorage("The specified object is not in your hands.", Color.Yellow));
                        Announcements.Add(new TextStorage("The specified object is not in your hands.", Color.Yellow));
                    }
                }

                if (ThrowingObject == null)
                {
                    Observations.Add(new TextStorage("You don't have an object like that in one of your hands.", Color.Yellow));
                    Announcements.Add(new TextStorage("You don't have an object like that in one of your hands.", Color.Yellow));
                }
                else
                {
                    if (ThrowingObject == Executor.LeftHandObject)
                    {
                        Executor.LeftHandObject = null;
                    }
                    else
                    {
                        Executor.RightHandObject = null;
                    }

                    ((Object)Subjects[0]).AirborneTarget = Subjects[1];
                    ((Object)Subjects[0]).Thrower = Executor;
                    ((Object)Subjects[0]).AirbornePower = Executor.Dexterity + Executor.GetDistance(Subjects[0]) + 3;

                    if (Executor.Structure != null)
                    {
                        Executor.Room.Objects.Add((Object)Subjects[0]);
                    }
                    else
                    {
                        Executor.Block.Objects.Add((Object)Subjects[0]);
                    }
                }
            }
            else if (new List<string> { "cast ~ at ~" }.Contains(Command))
            {
                if (Executor.SpellsKnown.Contains(Subjects[0].Metadata) ||
    (Executor.LeftHandObject != null && Executor.LeftHandObject.SpellContained == Subjects[0].Metadata) ||
    Executor.RightHandObject.SpellContained == Subjects[0].Metadata)

                {
                    string Spell = Subjects[0].Metadata;
    
                    Subjects.RemoveAt(0);

                    List<Entity> Targets = new List<Entity>();

                    foreach (Entity e in Subjects)
                    {
                        if (e is Object || e is Architect)
                        {
                            Targets.Add(e);
                        }
                        else
                        {
                            MakeObservation(Spell + " cannot be casted at " + e.ReferredToNames[0] + ".", Color.Yellow);
                        }
                    }

                    if (Targets.Count != 0)
                    {
                        Announcements.AddRange(Executor.CastSpell(Spell, Targets));
                    }
                    else
                    {
                        Observations.Add(new TextStorage("You couldn't find a sufficient target. Spells can only target architects and objects.", Color.Yellow));
                        Announcements.Add(new TextStorage("You couldn't find a sufficient target. Spells can only target architects and objects.", Color.Yellow));
                    }
                }
                else
                {
                    MakeObservation("You don't know a spell like that.", Color.Yellow);
                }
            }
            else if (new List<string> { "cast ~" }.Contains(Command))
            {
                MakeObservation("You fail to concentrate. You will need a point of interest to cast the spell at, even if unnecessary.", Color.Yellow);
            }
            else if (new List<string> { "remember ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 5));
                if (Subjects[0].Metadata == "spells")
                {
                    MakeObservation("Spells Known:", Color.Blue);
                    foreach (string s in Executor.SpellsKnown)
                    {
                        MakeObservation(s, Color.Blue);
                    }
                }
                else
                {
                    MakeObservation("You can't remember that.", Color.Yellow);
                }
            }
            else if (new List<string> { "read ~" }.Contains(Command))
            {
                // Check for the specified object in both hands and inventory
                Object objectToRead = null;
                if (Executor.MainHandObject() != null && Executor.MainHandObject() == Subjects[0])
                {
                    objectToRead = Executor.MainHandObject();
                }
                else if (Executor.OffHandObject() != null && Executor.OffHandObject() == Subjects[0])
                {
                    objectToRead = Executor.OffHandObject();
                }
                else if (Executor.Inventory.Any(item => item == Subjects[0]))
                {
                    objectToRead = Executor.Inventory.First(item => item == Subjects[0]);
                }

                if (objectToRead != null)
                {
                    // Object found, provide a reading outcome
                    MakeObservation("You read " + objectToRead.Name + ". " + objectToRead.Content.getCompleteWorkDescription(), Color.Blue);

                    // Increase the Executor's cooldown cycles based on the content length
                    int contentLength = objectToRead.Content.Sections.Count; // Assuming Content has a Length property representing the number of words or complexity
                    Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 125 * contentLength)); // Adjusted formula to consider content length

                    // Check if the object contains a spell and the Executor can learn it
                    if (!string.IsNullOrEmpty(objectToRead.SpellContained))
                    {
                        MakeObservation("You learned the spell \"" + objectToRead.SpellContained + "\"!", Color.Blue);
                    }
                }
                else
                {
                    // Object not found in hands or inventory
                    MakeObservation("You don't have " + Subjects[0] + " in your hands or inventory.", Color.Red);
                }
            }
            else if (new List<string> { "recite ~", "sing ~", "preform ~" }.Contains(Command))
            {
                string type = Command.Contains("recite") ? "poem" : "song";
                Composition compositionToPerform = Executor.CultureBank.Find(comp => comp.Type == type && comp.Name == Subjects[0].Metadata);

                if (compositionToPerform != null)
                {
                    // Performance successful, provide a description
                    string action = type == "poem" ? "recite" : "sing";
                    MakeObservation($"You {action} " + compositionToPerform.Name + ". " + compositionToPerform.getCompleteWorkDescription(), Color.Blue);
                }
                else
                {
                    // Composition not found in memory
                    MakeObservation("You do not remember a " + type + " named " + Subjects[0] + ".", Color.Red);
                }

                // Determine the list of architects based on the location of the Executor
                var architects = Executor.Room == null ? Executor.Block.Architects : Executor.Room.Architects;

                // Randomly select a subset of architects to react, between 1 and 6
                int numReactions = Math.Min(Game1.r.Next(1, 7), architects.Count);
                List<Architect> reactingArchitects = architects.OrderBy(a => Game1.r.Next()).Take(numReactions).ToList();

                // React to performance in the vicinity
                foreach (var architect in reactingArchitects)
                {
                    // Create a score for each architect based on Executor's charisma and a random modifier
                    int randomModifier = Game1.r.Next(-2, 3);  // Random number from -2 to 2
                    int score = Executor.Charisma + randomModifier;
                    score = Math.Clamp(score, 0, 9); // Ensure score is within 0-9

                    // Determine the reaction based on the score
                    string reaction;
                    switch (score)
                    {
                        case 0:
                            reaction = "Go find a merchant, peddler???";
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
                    AddMessage(architect.Name + ": " + reaction, Color.Magenta);
                }

            }
            else if (Command == "write ~")
            {
                // Decide on type based on the subject provided
                string type = Subjects[0].Metadata.ToLower();
                if (type == "book")
                {
                    // Find a writable object for books
                    Object writableObject = Executor.MainHandObject() != null && Executor.MainHandObject().IsWritable && Executor.MainHandObject().Content == null
                                                ? Executor.MainHandObject()
                                                : Executor.Inventory.FirstOrDefault(item => item.IsWritable && item.Content == null);

                    if (writableObject == null)
                    {
                        MakeObservation("You have nothing suitable for writing in your hands or inventory.", Color.Red);
                    }
                    else
                    {
                        // Create a new Composition without a specific domain
                        Composition newComposition = new Composition(type, Executor, "");
                        writableObject.Content = newComposition;
                        writableObject.Name = newComposition.Name; // Assign the generated book name to the object

                        // Provide detailed feedback to the user
                        MakeObservation("You write a book titled '" + newComposition.Name + "' on whatever you are thinking about at the moment." + newComposition.getCompleteWorkDescription() + " It is now stored in your " + writableObject.Name + ".", Color.Blue);
                    }
                }
                else if (type == "poem" || type == "song")
                {
                    // Create a new Composition in memory for poems or songs
                    Composition newComposition = new Composition(type, Executor, "");
                    Executor.CultureBank.Add(newComposition); // Assuming Executor has a Memory list to store compositions

                    // Provide detailed feedback to the user
                    MakeObservation("You compose a " + type + " titled '" + newComposition.Name + ". " +newComposition.getCompleteWorkDescription() + ". It is now stored in your memory.", Color.Blue);
                }
            }
            else if (Command == "write ~ about ~")
            {
                string domain;

                if (Subjects[1].Metadata == null || Subjects[1].Metadata == "")
                {
                    domain = Subjects[1].Metadata;
                }
                else
                {
                    domain = Subjects[1].Name;
                }
                string type = Subjects[0].Metadata; // This should be either "book", "poem", or "song"

                if (type == "book")
                {
                    // Find a writable object for books
                    Object writableObject = Executor.MainHandObject() != null && Executor.MainHandObject().IsWritable && Executor.MainHandObject().Content == null
                                                ? Executor.MainHandObject()
                                                : Executor.Inventory.FirstOrDefault(item => item.IsWritable && item.Content == null);

                    if (writableObject == null)
                    {
                        MakeObservation("You have nothing suitable for writing in your hands or inventory.", Color.Red);
                    }
                    else
                    {
                        // Create a new Composition with a specific domain
                        Composition newComposition = new Composition(type, Executor, domain);
                        writableObject.Content = newComposition;
                        writableObject.Name = newComposition.Name; // Assign the generated book name to the object

                        // Provide detailed feedback to the user
                        MakeObservation("You write a book titled '" + newComposition.Name + "' about " + domain + ". " + newComposition.getCompleteWorkDescription() + ". It is now stored in your " + writableObject.Name + ".", Color.Blue);
                    }
                }
                else if (type == "poem" || type == "song")
                {
                    // Create a new Composition in memory for poems or songs
                    Composition newComposition = new Composition(type, Executor, domain);
                    Executor.CultureBank.Add(newComposition); // Assuming Executor has a Memory list to store compositions

                    // Provide detailed feedback to the user
                    MakeObservation("You compose a " + type + " titled '" + newComposition.Name + "' about " + domain + ". " + newComposition.getCompleteWorkDescription() + ". It is now stored in your memory.", Color.Blue);
                }
            }

            else if (Command.StartsWith("write ~ about "))
            {
                MakeObservation("You can't write about that because it either doesn't exist or no one cares about it.", Color.Red);
            }
            else if (new List<string> { "tame ~", "pacify ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 15));
                if (Subjects[0] is Architect && ((((Architect)(Subjects[0])).Room == Executor.Room) && (((Architect)(Subjects[0])).Block == Executor.Block)))
                {
                    AddMessage(Executor.Name + ": Wild one, join the ranks of my great conquest.", Color.Green);
                    if (!GameWorld.HumanoidRaces.Contains(((Architect)Subjects[0]).Race) && !GameWorld.ExtraRaces.Contains(((Architect)Subjects[0]).Race))
                    {
                        int ExistingAnimals = 0;
                        foreach (Architect a in GamePlayerParty.Architects)
                        {
                            if (!GameWorld.HumanoidRaces.Contains(a.Race) && !GameWorld.ExtraRaces.Contains(a.Race))
                            {
                                ExistingAnimals++;
                            }
                        }

                        if (Executor.PathOfLifeLevel >= 6 && ExistingAnimals < Executor.PathOfLifeLevel)
                        {
                            AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": *happy shiba noises*", Color.Green);
                            GamePlayerParty.Architects.Add(((Architect)Subjects[0]));
                        }
                        else
                        {
                            AddMessage(((Architect)Subjects[0]).Name + ": *sad shiba noises*", Color.Yellow);
                        }
                    }
                    else
                    {
                        AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": What is this insanity? Calm yourself.", Color.Orange);
                        ((Architect)Subjects[0]).ChangeOpinion(Executor, -20);
                    }
                }
                else
                {
                    MakeObservation("You couldn't find anything like that nearby.", Color.Yellow);
                }
            }
            else if (new List<string> { "starstrike ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 15));
                if (Subjects[0] is Architect targetArchitect && (targetArchitect.Room == Executor.Room && targetArchitect.Block == Executor.Block))
                {
                    MakeObservation("You flick your wrist...", Color.Green);
                    int StarCount = 0;

                    if (Executor.PathOfStarsLevel >= 6)
                    {
                        StarCount = r.Next(2, 4);
                        MakeObservation($"Stars fly from your hands!", Color.Goldenrod);
                    }
                    else
                    {
                        MakeObservation($"...but nothing happens.", Color.Yellow);
                    }

                    for (int i = 0; i < StarCount; i++)
                    {
                        Object o = new Object(null, "falling star", new List<Material>() { GameWorld.Energy }, Executor);
                        o.AirborneTarget = Subjects[0];

                        if(targetArchitect.Room != null)
                        {
                            targetArchitect.Room.Objects.Add(o);
                        }
                        else
                        {
                            targetArchitect.Block.Objects.Add(o);
                        }
                    }
                }
                else
                {
                    MakeObservation("You couldn't find an architect like that nearby.", Color.Yellow);
                }
            }
            else if (new List<string> { "flamestrike ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 15));
                if (Subjects[0] is Architect targetArchitect && (targetArchitect.Room == Executor.Room && targetArchitect.Block == Executor.Block))
                {
                    MakeObservation("You wave...", Color.Green);

                    if (Executor.PathOfHeatLevel >= 2)
                    {
                        MakeObservation($"A large flame emnates from your hand!", Color.Goldenrod);
                        Object o = new Object(null, "wave", new List<Material>() { GameWorld.Flame }, Executor);
                        o.AirborneTarget = Subjects[0];
                        targetArchitect.Room.Objects.Add(o);
                    }
                    else
                    {
                        MakeObservation($"...but nothing happens.", Color.Yellow);
                    }
                }
                else
                {
                    MakeObservation("You couldn't find an architect like that nearby.", Color.Yellow);
                }
            }
            else if (new List<string> { "heat ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 15));
                if (Executor.PathOfHeatLevel >= 4)
                {
                    var targetObject = Subjects[0];

                    if (Executor.LeftHandObject == targetObject || Executor.RightHandObject == targetObject)
                    {
                        MakeObservation("You focus...", Color.Green);
                        ((Object)targetObject).HeatInCelsius += 50;
                        MakeObservation($"The {targetObject.Name} in your hand heats up intensely!", Color.Goldenrod);
                    }
                    else
                    {
                        MakeObservation($"...but you're not holding the intended target.", Color.Yellow);
                    }
                }
                else
                {
                    MakeObservation($"...but your control over heat is not strong enough.", Color.Yellow);
                }
            }

            else if (new List<string> { "starsmite ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 15));
                if (Subjects[0] is Architect targetArchitect && (targetArchitect.Room == Executor.Room && targetArchitect.Block == Executor.Block))
                {
                    MakeObservation("You point and wave...", Color.Green);

                    Executor.Energy -= 5;

                    if (Executor.PathOfStarsLevel >= 8)
                    {
                        MakeObservation($"A swirling vortex appears, and a cosmic energy beam strikes " + Subjects[0].ReferredToNames[0] + "!", Color.Goldenrod);
                        Executor.Energy -= 5;
                        foreach (Object o in targetArchitect.BodyParts)
                        {
                            o.Integrity -= r.Next(10, 40);
                        }
                        targetArchitect.Bleeding += r.Next(2, 6);

                        targetArchitect.ChangeOpinion(Executor, -60);
                    }
                    else
                    {
                        MakeObservation($"...but nothing happens.", Color.Yellow);
                    }
                }
                else
                {
                    MakeObservation("You couldn't find an architect like that nearby.", Color.Yellow);
                }
            }
            else if (new List<string> { "conjure spark" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 5));
                if (Executor.PathOfLightLevel >= 2)
                {
                    MakeObservation("You hold your hand out, collecting light...", Color.Green);
                    MakeObservation("A radiant spark appears!", Color.Green);

                    Object Spark = new Object(null, "spark", new List<Material>() { GameWorld.Energy }, Executor);
                    Executor.Sparks.Add(Spark);

                    if (Executor.Room != null)
                    {
                        Executor.Room.Objects.Add(Spark);
                        Spark.Room = Executor.Room;
                    }
                    else
                    {
                        Executor.Block.Objects.Add(Spark);
                        Spark.Block = Executor.Block;
                    }

                    if (Executor.Sparks.Count > Executor.PathOfLightLevel)
                    {
                        if (Executor.Sparks[0].Room != null)
                        {
                            Executor.Sparks[0].Room.Objects.Remove(Executor.Sparks[0]);
                            Executor.Sparks.RemoveAt(0);
                        }
                        else
                        {
                            Executor.Sparks[0].Block.Objects.Remove(Executor.Sparks[0]);
                            Executor.Sparks.RemoveAt(0);
                        }
                        MakeObservation("You feel a loss of connection to your earliest spark.", Color.Yellow);
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Yellow);
                }
            }
            else if (new List<string> { "evoke strike at ~", "evoke beam at ~", "evoke beams at ~", "evoke light at ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 15));
                Object FoundSpark = null;

                if (Executor.PathOfStarsLevel >= 4)
                {
                    if (Executor.Room != null)
                    {
                        foreach (Object o in Executor.Sparks)
                        {
                            if (o.Room == Executor.Room)
                            {
                                FoundSpark = o;

                                if (Subjects[0] is Architect && ((Architect)(Subjects[0])).Room == Executor.Room)
                                {
                                    Object BP = ((Architect)Subjects[0]).BodyParts[r.Next(((Architect)Subjects[0]).BodyParts.Count)];
                                    BP.Integrity -= r.Next(1, Executor.PathOfStarsLevel * 5);
                                    MakeObservation("The beam pierces through " + BP.ReferredToNames[0] + "!", Color.Magenta);
                                }
                                else if (Subjects[0] is Object && ((Object)(Subjects[0])).Room == Executor.Room)
                                {
                                    ((Object)Subjects[0]).Integrity -= r.Next(1, Executor.PathOfStarsLevel * 5);
                                    MakeObservation("The beam pierces through " + Subjects[0].ReferredToNames[0] + "!", Color.Magenta);
                                }
                                else
                                {
                                    MakeObservation("The beam fails to target properly.", Color.Yellow);
                                }

                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (Object o in Executor.Sparks)
                        {
                            if (o.Block == Executor.Block && o.Room == null)
                            {
                                FoundSpark = o;

                                if (Subjects[0] is Architect && ((Architect)(Subjects[0])).Block == Executor.Block)
                                {
                                    Object BP = ((Architect)Subjects[0]).BodyParts[r.Next(((Architect)Subjects[0]).BodyParts.Count)];
                                    BP.Integrity -= r.Next(1, Executor.PathOfStarsLevel * 5);
                                    MakeObservation("The beam pierces through " + BP.ReferredToNames[0] + "!", Color.Magenta);
                                }
                                else if (Subjects[0] is Object && ((Object)(Subjects[0])).Block == Executor.Block)
                                {
                                    ((Object)Subjects[0]).Integrity -= r.Next(1, Executor.PathOfStarsLevel * 5);
                                    MakeObservation("The beam pierces through " + Subjects[0].ReferredToNames[0] + "!", Color.Magenta);
                                }
                                else
                                {
                                    MakeObservation("The beam fails to target properly.", Color.Yellow);
                                }

                                break;
                            }
                        }
                    }

                    if (FoundSpark == null)
                    {
                        MakeObservation("You couldn't find one of your sparks in the vicinity.", Color.Yellow);
                    }
                    else
                    {
                        Executor.Sparks.Remove(FoundSpark);
                        if (FoundSpark.Room != null)
                        {
                            FoundSpark.Room.Objects.Remove(FoundSpark);
                        }
                        else
                        {
                            FoundSpark.Block.Objects.Remove(FoundSpark);
                        }
                    }
                }
            }
            else if (new List<string> { "evoke blindness" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 15));
                Object FoundSpark = null;

                if (Executor.PathOfStarsLevel >= 2) // Assuming the ability requires a certain level to use
                {
                    // Check for spark in the same room or block
                    foreach (Object o in Executor.Sparks)
                    {
                        if ((Executor.Room != null && o.Room == Executor.Room) || (Executor.Block != null && o.Block == Executor.Block && o.Room == null))
                        {
                            FoundSpark = o;
                            break;
                        }
                    }

                    if (FoundSpark != null)
                    {
                        bool foundArchitects = false;

                        if (Executor.Room != null)
                        {
                            foreach (Architect architect in Executor.Room.Architects)
                            {
                                architect.BlindCycles += 50; // Add 4 BlindCycles to each Architect
                                MakeObservation(architect.Name + " is blinded by the radiance!", Color.Magenta);
                                foundArchitects = true;
                            }
                        }
                        else if (Executor.Block != null)
                        {
                            foreach (Architect architect in Executor.Block.Architects.Where(a => a.Room == null))
                            {
                                architect.BlindCycles += 50;
                                MakeObservation(architect.Name + " is blinded by the radiance!", Color.Magenta);
                                foundArchitects = true;
                            }
                        }

                        if (!foundArchitects)
                        {
                            MakeObservation("No one is blinded...", Color.Yellow);
                        }

                        // Consume the spark
                        Executor.Sparks.Remove(FoundSpark);
                        if (FoundSpark.Room != null)
                        {
                            FoundSpark.Room.Objects.Remove(FoundSpark);
                        }
                        else
                        {
                            FoundSpark.Block.Objects.Remove(FoundSpark);
                        }
                    }
                    else
                    {
                        MakeObservation("You couldn't find one of your sparks in the vicinity.", Color.Yellow);
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red);
                }
            }
            else if (new List<string> { "group travel", "gt", "vote to group travel", "tell the group to follow me"}.Contains(Command))
            {

            }
            else if (new List<string> { "evoke healing" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 15));
                Object FoundSpark = null;

                if (Executor.PathOfStarsLevel >= 6) // Assuming the ability requires a certain level to use
                {
                    // Check for spark in the same room or block
                    foreach (Object o in Executor.Sparks)
                    {
                        if ((Executor.Room != null && o.Room == Executor.Room) || (Executor.Block != null && o.Block == Executor.Block && o.Room == null))
                        {
                            FoundSpark = o;
                            break;
                        }
                    }

                    if (FoundSpark != null)
                    {
                        bool foundArchitects = false;

                        if (Executor.Room != null)
                        {
                            foreach (Architect architect in Executor.Room.Architects)
                            {
                                if (architect.CombatCycles == 0)
                                {
                                    architect.Energy = architect.MaxEnergy(); // Heal to max energy
                                    MakeObservation(architect.Name + " is enveloped in brilliance and fully healed!", Color.Magenta);
                                }
                                else
                                {
                                    MakeObservation(architect.Name + " is too distracted for brilliance.", Color.Yellow);
                                }
                                foundArchitects = true;
                            }
                        }
                        else if (Executor.Block != null)
                        {
                            foreach (Architect architect in Executor.Block.Architects.Where(a => a.Room == null))
                            {
                                if (architect.CombatCycles == 0)
                                {
                                    architect.Energy = architect.MaxEnergy(); // Heal to max energy
                                    MakeObservation(architect.Name + " is enveloped in brilliance and fully healed!", Color.Magenta);
                                }
                                else
                                {
                                    MakeObservation(architect.Name + " is too distracted for brilliance.", Color.Yellow);
                                }
                                foundArchitects = true;
                            }
                        }

                        if (!foundArchitects)
                        {
                            MakeObservation("There is no one to heal...", Color.Yellow);
                        }

                        // Consume the spark
                        Executor.Sparks.Remove(FoundSpark);
                        if (FoundSpark.Room != null)
                        {
                            FoundSpark.Room.Objects.Remove(FoundSpark);
                        }
                        else
                        {
                            FoundSpark.Block.Objects.Remove(FoundSpark);
                        }
                    }
                    else
                    {
                        MakeObservation("You couldn't find one of your sparks in the vicinity.", Color.Yellow);
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red);
                }
            }
            else if (new List<string> { "evoke nexus", "evoke photonexus" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 15));
                Object FoundSpark = null;

                if (Executor.PathOfStarsLevel >= 4) // Assuming the ability requires a certain level to use
                {
                    // Check for spark in the same room or block
                    foreach (Object o in Executor.Sparks)
                    {
                        if ((Executor.Room != null && o.Room == Executor.Room) || (Executor.Block != null && o.Block == Executor.Block && o.Room == null))
                        {
                            FoundSpark = o;
                            break;
                        }
                    }

                    if (FoundSpark != null)
                    {
                        Architect a = new Architect("", Game1.Sexes[r.Next(Game1.Sexes.Count)], Game1.GameWorld.GetRace("photonexus"), 0, "prismancer", new List<Object>(), Executor.Location, Executor.District, Executor.Block, "", 1);
                        GamePlayerParty.Architects.Add(a);
                        MakeObservation("A photonexus appears!", Color.Cyan);

                        if (Executor.Room != null)
                        {
                            Executor.Room.Architects.Add(a);
                        }
                        else if (Executor.Block != null)
                        {
                            Executor.Block.Architects.Add(a);
                        }

                        // Consume the spark
                        Executor.Sparks.Remove(FoundSpark);
                        if (FoundSpark.Room != null)
                        {
                            FoundSpark.Room.Objects.Remove(FoundSpark);
                        }
                        else if (FoundSpark.Block != null)
                        {
                            FoundSpark.Block.Objects.Remove(FoundSpark);
                        }
                    }
                    else
                    {
                        MakeObservation("You couldn't find one of your sparks in the vicinity.", Color.Yellow);
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red);
                }
            }
            else if (new List<string> { "inflame" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 15));
                if (Executor.PathOfHeatLevel >= 8)
                {
                    Executor.FireCycles += 50;
                    MakeObservation("Your flame burns brighter!", Color.Red);
                }
                else
                {
                    MakeObservation("You don't have control over that.", Color.Red);
                }
            }
            else if (new List<string> { "unflame" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 15));
                if (Executor.PathOfHeatLevel >= 8)
                {
                    Executor.FireCycles = 0;
                    MakeObservation("You stop blazing!", Color.Red);
                }
                else
                {
                    MakeObservation("You don't have control over that.", Color.Red);
                }
            }

            else if (new List<string> { "augument ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 15));
                if (Subjects[0] is Architect && ((((Architect)(Subjects[0])).Room == Executor.Room) && (((Architect)(Subjects[0])).Block == Executor.Block)))
                {
                    if (!GameWorld.HumanoidRaces.Contains(((Architect)Subjects[0]).Race) && !GameWorld.ExtraRaces.Contains(((Architect)Subjects[0]).Race) && GamePlayerParty.Architects.Contains(Subjects[0]))
                    {
                        if (Executor.PathOfLifeLevel >= 8)
                        {
                            MakeObservation("You gesture.", Color.Magenta);

                            if (((Architect)Subjects[0]).Augumented == true)
                            {
                                MakeObservation(Subjects[0].ReferredToNames[0] + " already has an augumentation.", Color.Magenta);
                            }
                            else
                            {
                                int Shibe = r.Next(3);

                                if (Shibe == 0)
                                {
                                    MakeObservation(Subjects[0].ReferredToNames + " is enveloped in a golden light, becoming stronger!", Color.Magenta);
                                    ((Architect)Subjects[0]).Strength += 2;
                                }
                                else if (Shibe == 1)
                                {
                                    MakeObservation(Subjects[0].ReferredToNames + " is enveloped in a white light, becoming more agile!", Color.Magenta);
                                    ((Architect)Subjects[0]).Strength += 2;
                                }
                                else if (Shibe == 2)
                                {
                                    MakeObservation(Subjects[0].ReferredToNames + " is enveloped in a red light, becoming more durable!", Color.Magenta);
                                    ((Architect)Subjects[0]).MaxEnergyMod += 30;
                                    ((Architect)Subjects[0]).Energy = ((Architect)Subjects[0]).MaxEnergy();
                                }

                                ((Architect)Subjects[0]).Augumented = true;
                            }
                        }
                        else
                        {
                            MakeObservation("You aren't powerful enough to do that.", Color.Yellow);
                        }
                    }
                    else
                    {
                        MakeObservation("You can't augument humanoids or creatures you don't control.", Color.Yellow);
                    }
                }
                else
                {
                    MakeObservation("You couldn't find anything augumentable like that nearby.", Color.Yellow);
                }
            }
            else if (new List<string> { "raise ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 15));
                MakeObservation("You conjure a spark of dark energy, and speak the name of " + Subjects[0].ReferredToNames[0] + "...", Color.Purple);

                //count undead already i nthe party

                int Shadecount = 0;

                foreach (Architect a in GamePlayerParty.Architects)
                {
                    if (a.Race == GameWorld.GetRace("shade"))
                    {
                        Shadecount++;
                    }
                }

                if (Subjects[0] is Architect && ((Architect)Subjects[0]).IsAlive == false && (((Architect)Subjects[0]).Block == Executor.Block && ((Architect)Subjects[0]).Room == Executor.Room) && Shadecount <= Executor.PathOfDeathLevel)
                {
                    Announcements.Add(new TextStorage(Subjects[0].ReferredToNames[0] + " rises with a putrid, dark energy!", Color.Purple));
                    ((Architect)Subjects[0]).IsAlive = true;
                    ((Architect)Subjects[0]).IsImmortal = true;
                    ((Architect)Subjects[0]).Race = Game1.GameWorld.GetRace("shade");

                    ((Architect)Subjects[0]).OppositionTags.Add("alllife");
                    ((Architect)Subjects[0]).Energy = 50;
                    ((Architect)Subjects[0]).MaxEnergyMod = 50;

                    ((Architect)Subjects[0]).UndeadCreator = Executor;

                    foreach (Object o in ((Architect)Subjects[0]).BodyParts)
                    {
                        o.Integrity = Math.Max(50, o.Integrity);
                    }
                }
                else
                {
                    Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple));
                }
            }
            else if (new List<string> { "fire spectral bolt at ~", "spectralize ~", "spectral bolt ~" }.Contains(Command))
            {
                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 15));
                if (Executor.PathOfDeathLevel >= 4 || (Executor.UndeadCreator != null && Executor.UndeadCreator.PathOfDeathLevel >= 8))
                {
                    if (Subjects[0] is Architect)
                    {
                        Object o = new Object(null, "bolt", new List<Material>() { GameWorld.Spectre }, false, false, null, Executor, 0, false, Executor.Block, Executor.Structure, Executor.Room, false);
                        o.AirborneTarget = Subjects[0];

                        if (Executor.Room != null)
                        {
                            Executor.Room.Objects.Add(o);
                        }
                        else
                        {
                            Executor.Block.Objects.Add(o);
                        }
                    }
                    else
                    {
                        Announcements.Add(new TextStorage("The spirit you pulled from " + GameWorld.DarkDeity + "-knows-where only seeks the living and dead.", Color.Yellow));
                    }
                }
            }

            //THIS IS THE REALM OF MESSAGING BWAHAHAHAHAHAHAHA


            else if (Subjects.Count > 0 && Subjects[0] is Architect && isSpeakingCommand) /*basically required for messages*/
            {
                Color ObservationColor1;
                Color ObservationColor2;

                Executor.CooldownCycles += (int)(Math.Round(Executor.Speed() * 20));

                if (GameWorld.HumanoidRaces.Contains(((Architect)Subjects[0]).Race) || (GameWorld.ExtraRaces.Contains(((Architect)Subjects[0]).Race) && Executor.PathOfLifeLevel >= 2) || Executor.PathOfLifeLevel >= 4)
                {
                    if (Subjects.Contains(LoadedArchitects[ArchitectIndex]) || LoadedArchitects[ArchitectIndex] == Executor)
                    {
                        ObservationColor1 = Color.LimeGreen;
                        ObservationColor2 = Color.Cyan;
                    }
                    else
                    {
                        ObservationColor1 = Color.DarkGreen;
                        ObservationColor2 = Color.DarkCyan;
                    }


                    if (Subjects[0] == LoadedArchitects[ArchitectIndex]) //this means that its a message to the player and hteres differnet code to react to it.
                    {

                    }
                    else
                    {
                        if (new List<string> { "ask ~ /p name", "ask ~ name" }.Contains(Command))
                        {
                            AddMessage(Executor.Name + ": What is your name?", ObservationColor1);
                            AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": My name is " + ((Architect)Subjects[0]).Name + ".", ObservationColor2);
                            ((Architect)Subjects[0]).ChangeOpinion(Executor, 1);
                        }
                        else if (new List<string> { "ask ~ about local troubles", "ask ~ if anything bothers them", "ask ~ if anything is bothering them", "ask ~ if anything is wrong", "ask ~ what they need help with", "ask ~ about local issues", "ask ~ about local problems" }.Contains(Command))
                        {
                            AddMessage(Executor.Name + ": What troubles you today?", ObservationColor1);
                            AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": INSERT TROUBLES HERE.", ObservationColor2);
                        }
                        else if (new List<string> { "ask ~ to join me", "ask ~ if they would join me", "ask ~ if they want to join me", "ask ~ to join my group" }.Contains(Command) || (LoadedArchitects[ArchitectIndex].Group != null && new List<string> { "ask ~ to join " + LoadedArchitects[ArchitectIndex].Group.Name, "ask ~ if they would join " + LoadedArchitects[ArchitectIndex].Group.Name, "ask ~ if they want to join " + LoadedArchitects[ArchitectIndex].Group.Name }.Contains(Command)))
                        {
                            AddMessage(Executor.Name + ": Join me on my quest!", ObservationColor1);

                            if ((GamePlayerParty.Architects.Count - 1) < (1 + Math.Max(0, GamePlayerParty.Architects[0].Level - 2)))
                            {
                                if (((Architect)Subjects[0]).Group != null)
                                {
                                    AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": I apologize. My loyalties are elsewhere.", ObservationColor2);
                                }
                                else
                                {
                                    AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": Yes, I will join you.", ObservationColor2);
                                    AddMessage(Executor.Name + ": Fantastic! Welcome to " + GamePlayerParty.Name + ".", ObservationColor1);

                                    GamePlayerParty.Architects.Add(((Architect)Subjects[0]));
                                }
                            }
                            else
                            {
                                AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": Prove you have what it takes.", ObservationColor2);
                            }
                        }
                        else if (new List<string> { "greet ~", "say hello to ~", "tell ~ hello", "tell ~ hi", "say hi to ~", "tell ~ my name", "tell ~ who I am" }.Contains(Command))
                        {
                            if (((Architect)Subjects[0]).GetOpinion(Executor) == 0)
                            {
                                AddMessage(Executor.Name + ": Hello, there. My name is " + Executor.Name + ".", ObservationColor1);
                                AddMessage(Subjects[0].ReferredToNames[0] + ": Ah, hello there. I am called " + Subjects[0].Name + ".", ObservationColor2);
                                ((Architect)Subjects[0]).ChangeOpinion(Executor, 1);
                            }
                            else
                            {
                                AddMessage(Executor.Name + ": Hello, there, " + Subjects[0].Name + ".", ObservationColor1);
                                AddMessage(Subjects[0].ReferredToNames[0] + ": Hello there, " + Executor.Name + ". It is nice to see you again.", ObservationColor2);
                            }
                        }
                        else if (new List<string> { "ask ~ if i could join them", "ask to join ~", "ask if i could join ~", "ask ~ to join my group" }.Contains(Command))
                        {
                            if (Executor.Group == null)
                            {
                                AddMessage(Executor.Name + ": I would be thrilled to join your group. Would you have me?", ObservationColor1);

                                if (((Architect)Subjects[0]).Group != null)
                                {
                                    AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": Yes, you may join us. Welcome to " + ((Architect)Subjects[0]).Group.Name + ".", ObservationColor2);
                                    Executor.Group = ((Architect)Subjects[0]).Group;
                                }
                                else
                                {
                                    AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": What group? You must be mistaken.", ObservationColor2);
                                }
                            }
                            else
                            {
                                AddMessage(Executor.Name + ": I'd be thrilled to join your group. Would you have me?", ObservationColor1);
                                AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": Are you not associated with " + Executor.Group.Name + "? I can't be sure of your complete loyalty.", ObservationColor2);
                            }
                        }
                        else if (new List<string> { "ask ~ if there is ~ nearby", "ask ~ where ~ is", "ask ~ where the ~ is", "ask ~ where i could find ~", "ask ~ where the nearest ~ is", "ask ~ the location of ~", "ask ~ about the whereabouts of ~", "ask ~ where a nearby ~ is" }.Contains(Command))
                        {
                            string GetDirection(int centerPointX, int centerPointZ, int pointTowardsX, int pointTowardsZ)
                            {
                                int dx = pointTowardsX - centerPointX;
                                int dz = centerPointZ - pointTowardsZ; // Invert the Z calculation

                                // Calculate angle in radians
                                double angle = Math.Atan2(dz, dx);

                                // Convert angle to degrees
                                double degrees = angle * (180.0 / Math.PI);

                                // Normalize the angle to be positive
                                if (degrees < 0)
                                {
                                    degrees += 360.0;
                                }

                                // Determine the cardinal/ordinal direction based on degrees
                                if (degrees >= 22.5 && degrees < 67.5)
                                {
                                    return "northeast";
                                }
                                else if (degrees >= 67.5 && degrees < 112.5)
                                {
                                    return "east";
                                }
                                else if (degrees >= 112.5 && degrees < 157.5)
                                {
                                    return "southeast";
                                }
                                else if (degrees >= 157.5 && degrees < 202.5)
                                {
                                    return "south";
                                }
                                else if (degrees >= 202.5 && degrees < 247.5)
                                {
                                    return "southwest";
                                }
                                else if (degrees >= 247.5 && degrees < 292.5)
                                {
                                    return "west";
                                }
                                else if (degrees >= 292.5 && degrees < 337.5)
                                {
                                    return "northwest";
                                }
                                else
                                {
                                    return "north";
                                }
                            }


                            if (Subjects[1].Metadata != null)
                            {
                                AddMessage(Executor.Name + ": Do you know where I could find " + Subjects[1].Metadata + "?", ObservationColor1);

                                (Region, Location, District, Block, Structure, string) Data = Executor.Block.FindNearestThing(Subjects[1].Metadata);

                                if (Data != (null, null, null, null, null, ""))
                                {
                                    if (Data.Item2 != Executor.Location)
                                    {
                                        string direction = GetDirection(Executor.Location.X, Executor.Location.Z, Data.Item2.X, Data.Item2.Z);
                                        AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": Yes, but you will have to travel to " + Data.Item2.Name + ", which is " + direction + " from here. " + Data.Item6 + " is there, " + Subjects[1].Name + " you might be looking for.", ObservationColor2);
                                    }
                                    else if (Data.Item3 != Executor.District)
                                    {
                                        AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": Yes, in the " + Data.Item3.Name + " district nearby. " + Data.Item6 + " is " + Subjects[1].Name + " you might be looking for.", ObservationColor2);
                                    }
                                    else if (Data.Item4 != Executor.Block)
                                    {
                                        string direction = GetDirection(Executor.Block.X, Executor.Block.Z, Data.Item4.X, Data.Item4.Z);
                                        AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": Yes, " + Data.Item6 + " is not far from here. Just head a few blocks " + direction + ".", ObservationColor2);
                                    }
                                }
                                else
                                {
                                    AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": I have no idea.", ObservationColor2);
                                }
                            }
                            else
                            {
                                //for named entities

                                if (((Architect)Subjects[0]).Profession == "scholar")
                                {
                                    AddMessage(Executor.Name + ": Do you know where I could find " + Subjects[1].Metadata + "?", ObservationColor1);

                                    (Region, Location, District, Block, Structure, string) Data = (null, null, null, null, null, "");

                                    if (Subjects[1] is Architect)
                                    {
                                        Data = (((Architect)Subjects[1]).Location.Region, ((Architect)Subjects[1]).Location, ((Architect)Subjects[1]).District, ((Architect)Subjects[1]).Block, ((Architect)Subjects[1]).Structure, "");
                                    }
                                    else if (Subjects[1] is Structure)
                                    {
                                        Data = (((Structure)Subjects[1]).Block.District.Location.Region, ((Structure)Subjects[1]).Block.District.Location, ((Structure)Subjects[1]).Block.District, ((Structure)Subjects[1]).Block, null, "");
                                    }
                                    else if (Subjects[1] is Object)
                                    {
                                        Data = (((Object)Subjects[1]).Structure.Block.District.Location.Region, ((Object)Subjects[1]).Block.District.Location, ((Object)Subjects[1]).Block.District, ((Object)Subjects[1]).Block, ((Object)Subjects[1]).Structure, "");
                                    }

                                    if (Data != (null, null, null, null, null, ""))
                                    {
                                        if (Data.Item2 != Executor.Location)
                                        {
                                            string direction = GetDirection(Executor.Location.X, Executor.Location.Z, Data.Item2.X, Data.Item2.Z);
                                            AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": Yes, but you will have to travel to " + Data.Item2.Name + ", which is " + direction + " from here. " + Data.Item6 + " is somewhere there.", ObservationColor2);
                                        }
                                        else if (Data.Item3 != Executor.District)
                                        {
                                            AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": Yes, in the " + Data.Item3.Name + " district nearby. " + Data.Item6 + " is there somewhere.", ObservationColor2);
                                        }
                                        else if (Data.Item4 != Executor.Block)
                                        {
                                            if (Data.Item5 == null)
                                            {
                                                string direction = GetDirection(Executor.Block.X, Executor.Block.Z, Data.Item4.X, Data.Item4.Z);
                                                AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": Yes, " + Data.Item6 + " is " + direction + " of here.", ObservationColor2);
                                            }
                                            else
                                            {
                                                string direction = GetDirection(Executor.Block.X, Executor.Block.Z, Data.Item4.X, Data.Item4.Z);
                                                AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": Yes, " + Data.Item6 + " is in " + Data.Item5.Name + ", " + direction + " from here.", ObservationColor2);
                                            }
                                        }
                                        else
                                        {
                                            if (Data.Item5 == null)
                                            {
                                                AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + " points towards " + Subjects[1].Metadata + ".", Color.Blue);
                                                AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": I think you're looking for that.", ObservationColor2);
                                            }
                                            else
                                            {
                                                AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + " points towards " + Data.Item5.Name + ".", Color.Blue);
                                                AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": Right in there.", ObservationColor2);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    AddMessage(Executor.Name + ": Do you know where I could find " + Subjects[1].Metadata + "?", ObservationColor1);
                                    AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": I dont know, go find a scholar.", ObservationColor2);
                                }
                            }
                        }
                        else if (new List<string> { "ask ~ about their expertise", "inquire ~ about their skills", "ask ~ what they're good at", "ask ~ about their skills", "ask ~ what they can do" }.Contains(Command))
                        {
                            AddMessage(Executor.Name + ": What are your skills?", ObservationColor1);

                            (string, int) GreatestProficiency = ("nothing", 0);

                            foreach ((string, int) I in ((Architect)Subjects[0]).XPValues)
                            {
                                if (GreatestProficiency.Item2 < I.Item2)
                                {
                                    GreatestProficiency = I;
                                }
                            }

                            AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": I am skilled at " + GreatestProficiency.Item2 + ".", ObservationColor2);
                        }
                        else if (new List<string> { "ask ~ for advice on ~", "seek ~ counsel on ~", "request ~ guidance regarding ~", "consult ~ about ~", "ask ~ for insight on ~", "ask ~ for their opinion on ~", "seek ~ viewpoint on ~", "query ~ about their thoughts on ~" }.Contains(Command))
                        {
                            AddMessage(Executor.Name + ": I'm not sure about " + Subjects[1].Metadata + ".", ObservationColor2);

                            List<string> Responses = new List<string>() { "It's hopeless.",
"Steer clear of hopeless situations.",
"Ignore things more trouble than they're worth.",
"You might regret your descision, but it must be made nonetheless.",
"Its difficult.",
"Dont set your hopes too high.",
"You can try anything, but I wouldn't expect much.",
"Its a bit of a gray area.",
"What you do might not be for nothing, but don't be too hopeful.",
"There's a glimmer of potential, albeit faint.",
"The future is inevitable, anyway.",
"Nothing is impossible.",
"Everything has its pros and cons, weigh them carefully.",
"With some effort, it might just be worth it.",
"There's a chance you could make it work.",
"It's not hopeless.",
"I'd say there's a fair chance.",
"Don't give into temptation.",
"Your worst fears will be realized, anyway.",
"There's always potential.",
"Just make sure you're taking the right approach.",
"Your plans are worthless.",
"I am confident in your ability to get through.",
"Is it truly promising?",
"Make your choice, but make it quickly.",
"When in doubt, ask for help.",
"Only time will tell, but don't hold your breath.",
"If it feels right, perhaps it's worth the risk.",
"Sometimes, the journey is its own reward.",
"Consider this a test of your resolve.",
"Uncertainty is the soil of growth.",
"In the end, you'll know if it was meant to be.",
"Every choice has its shadow and its light.",
"It might not lead where you expect, but the path is yours.",
"The answer lies within the question itself.",
"Some answers come only after the leap.",
"Doubt can be a guiding star, if you let it.",
"What matters is the choice to move forward.",
"The odds are not in your favor.",
"Consider this a warning rather than advice.",
"Don't be surprised if things don't pan out.",
"Expectations are premeditated resentments.",
"You're likely to come out the other side with scars.",
"You're playing with fire.",
"Don't embark on a journey that's doomed from the start.",
"You may be setting yourself up for failure.",
"This road is paved with good intentions and bad outcomes.",
"It's a battle you're not equipped to win.",
"You're aiming for the heavens with a slingshot.",
"This could be a tale of caution for others.",
"You're likely to end up back where you started, or worse.",
};

                            AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": " + Responses[r.Next(Responses.Count)], ObservationColor2);
                        }
                        else if (new List<string> { "ask ~ about the history of ~", "inquire ~ about past events of ~", "question ~ on the historical background of ~" }.Contains(Command))
                        {
                            // Implement the response logic here
                            AddMessage(Executor.Name + ": Tell me more about " + Subjects[1].Metadata + ".", ObservationColor1);

                            if (Subjects[1] is Location)
                            {
                                AddMessage(Executor.Name + ": Ah yes, in " + ((Location)Subjects[1]).LocationHistoricalEvents[r.Next(((Location)Subjects[1]).LocationHistoricalEvents.Count)], ObservationColor2);
                            }
                            else if (Subjects[1] is Architect)
                            {
                                AddMessage(Executor.Name + ": I don't know them well enough.", ObservationColor2);
                            }
                            else
                            {
                                AddMessage(Executor.Name + ": What? Where?", ObservationColor2);
                            }
                        }
                        else if (new List<string> { "ask ~ if /p need assistance", "ask ~ if /p require assistance", "inquire if ~ needs help", "offer aid to ~", "ask if ~ needs help", "ask ~ if /p needs help", "ask ~ if /p need help" }.Contains(Command))
                        {
                            AddMessage(Executor.Name + ": Do you need help with something?", ObservationColor1);

                            if (Subjects[0] is Architect)
                            {
                                if (((Architect)Subjects[0]).Task == "fighting")
                                {
                                    AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": Help! Im in combat!", ObservationColor2);
                                }
                                else
                                {
                                    AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": Everything is fine.", ObservationColor2);
                                }
                            }
                        }
                        else if (new List<string> { "ask ~ about recent events", "inquire ~ on the latest happenings", "question ~ about current affairs" }.Contains(Command))
                        {
                            AddMessage(Executor.Name + ": Whats been happening recently?", ObservationColor1);

                            if (Executor.Location.LocationHistoricalEvents.Count > 0)
                            {
                                AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": Ah yes, in " + Executor.Location + " in " + Executor.Location.LocationHistoricalEvents.Last(), ObservationColor2);
                            }
                        }
                        else if (new List<string> { "ask ~ to trade", "ask ~ if they want to trade", "ask ~ whats for sale", "ask ~ what they have for sale", "ask ~ if they have anything for trade or sale", "inquire ~ about merchandise", "question ~ on items for sale" }.Contains(Command))
                        {
                            AddMessage(Executor.Name + ": What do you have to trade?", ObservationColor1);

                            if (Executor.Profession == "trader")
                            {
                                if (Executor.Structure.Type == "market")
                                {
                                    AddMessage(((Architect)Subjects[0]).Name + ": I'm selling all of the lovely items here. You can take some items if you leave greater value, which I will ardently track. Do NOT leave the room with debt.", ObservationColor2);
                                }
                                else
                                {
                                    AddMessage(((Architect)Subjects[0]).Name + ": Ask me once I get back to the market...", ObservationColor2);
                                }
                            }
                            else
                            {
                                AddMessage(((Architect)Subjects[0]).Name + ": Go find a merchant, peddler.", ObservationColor2);
                            }
                        }
                        else if (new List<string> { "ask ~ for their story", "inquire ~ about their background", "request ~ to share their history" }.Contains(Command))
                        {
                            AddMessage(Executor.Name + "", ObservationColor2);
                        }
                        else if (new List<string> { "ask ~ how they are feeling", "inquire ~ about their wellbeing", "question ~ on their current mood" }.Contains(Command))
                        {
                            // Implement the response logic here
                        }
                        else if (new List<string> { "ask ~ who rules ~", "ask ~ about the local government", "inquire ~ about the political situation" }.Contains(Command))
                        {
                            // Implement the response logic here
                        }
                        else if (new List<string> { "ask ~ to trade", "propose a trade to ~", "suggest trading with ~" }.Contains(Command))
                        {
                            // Implement the response logic here
                        }
                        else if (new List<string> { "ask ~ permission to stay for the night", "request ~ to lodge overnight", "seek ~ allowance for a night's stay" }.Contains(Command))
                        {
                            // Implement the response logic here
                        }
                        else if (new List<string> { "ask ~ about the economy or trade", "inquire ~ on trade conditions", "question ~ about economic status" }.Contains(Command))
                        {
                            // Implement the response logic here
                        }
                        else if (new List<string> { "ask ~ if they've seen ~ recently", "inquire ~ about recent sightings of ~", "question ~ on the last location of ~" }.Contains(Command))
                        {
                            // Implement the response logic here
                        }
                        else if (new List<string> { "ask ~ to teach me how to ~", "request ~ for instruction on ~", "seek ~ guidance in learning ~" }.Contains(Command))
                        {
                            // Implement the response logic here
                        }
                        else if (new List<string> { "tell ~ to calm down", "advise ~ to relax", "urge ~ to stay calm" }.Contains(Command))
                        {
                            // Implement the response logic here
                        }
                        else if (new List<string> { "tell ~ a story", "narrate a tale to ~", "share a story with ~" }.Contains(Command))
                        {
                            // Implement the response logic here
                        }
                        else if (new List<string> { "tell ~ a joke", "share a laugh with ~", "make ~ laugh with a joke" }.Contains(Command))
                        {
                            // Implement the response logic here
                        }
                        else if (new List<string> { "tell ~ my quest", "reveal my mission to ~", "share the purpose of my journey with ~" }.Contains(Command))
                        {
                            // Implement the response logic here
                        }
                        else if (new List<string> { "tell ~ to follow me", "tell ~ to join me", "command ~ to accompany me" }.Contains(Command))
                        {
                            // Implement the response logic here
                        }
                        else if (new List<string> { "tell ~ to wait here", "instruct ~ to stay put", "ask ~ to remain here" }.Contains(Command))
                        {
                            // Implement the response logic here
                        }
                        else if (new List<string> { "tell ~ to be cautious", "warn ~ to tread carefully", "advise ~ to be wary" }.Contains(Command))
                        {
                            // Implement the response logic here
                        }
                        else if (new List<string> { "tell ~ to flee", "urge ~ to run away", "command ~ to escape" }.Contains(Command))
                        {
                            // Implement the response logic here
                        }
                        else
                        {
                            string message = "You cannot say that... yet... Though I'll try to implement it sometime. I'll store it in your failedsayings.txt in your Documents folder though.";
                            string contentPath = Path.Combine(DocumentsFolderPath, "failedsayings.txt");
                            if (!File.Exists(contentPath))
                            {
                                File.WriteAllText(contentPath, Command);
                            }
                            else
                            {
                                File.AppendAllText(contentPath, Command);
                            }
                            MakeObservation(message, Color.Blue);
                            return false;
                        }

                    }
                }
                else
                {
                    MakeObservation(Subjects[0].ReferredToNames[0] + " can't understand you.", Color.Yellow);
                }

            }
            else
            {
                string observationMessage = "Could not process. Either the command is unimplemented, you cannot access the subject, or the spelling is wrong.";
                string contentPath = Path.Combine(ContentRoot, "commands.txt");
                string failedCommand = "Command";
                if (!File.Exists(contentPath))
                {
                    File.WriteAllText(contentPath, failedCommand + "\n");
                }
                else
                {
                    File.AppendAllText(contentPath, failedCommand + "\n");
                }
                MakeObservation(observationMessage, Color.Blue);

                return false;
            }


            //if we got to this point that means we exited the if statement by running a command successfully, therefore we can return "true"
            return (true);
        }
        public static string GameState = "mainscreen";
        public static string GameMode = "unknown";

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
            List<string> List = new List<string>();
            foreach (Material m in materials)
            {
                List.Add(m.Name);
            }
            return (FormatList(List));
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
            ": A Grand Exploration", ": An Epic Journey", ": An Unusual Encounter", ": A Baffling Conundrum", ": A Curious Revelation", ": A Whimsical Quest", ": A Mysterious Adventure", ": An Enchanted Odyssey", ": A Secret Chronicle", ": A Mythical Encounter", ": A Puzzling Expedition", ": A Fascinating Discovery", ": A Remarkable Study", ": A Bewildering Investigation", ": A Legendary Chronicle", ": An Enigmatic Tale", ": A Magical Quest", ": A Mystical Journey", ": An Ancient Discovery", ": A Timeless Exploration", ": A Surprising Revelation", ": A Hidden Mystery", ": An Astonishing Saga", ": An Illuminating Narrative", ": An Uncharted Journey", ": A Legendary Exploration", ": A Mythical Adventure", ": A Remarkable Investigation", ": An Unfolding Mystery", ": A Mysterious Chronicle", ": A Whimsical Discovery", ": An Enchanted Quest", ": A Curious Journey", ": A Thrilling Expedition", ": An Epic Odyssey", ": A Wondrous Revelation", ": An Intriguing Inquiry", ": A Timeless Chronicle", ": A Bewildering Exploration", ": A Puzzling Discovery", ": A Journey of Legends", ": A Study in Wonder", ": An Enigma Unveiled", ": A Quest for Secrets", ": A Tale of Wonders", ": A Mythical Chronicle", ": An Enchanted Adventure", ": A Mysterious Revelation", ": An Odyssey Beyond", ": A Grand Adventure", " for the Curious Mind", " at Your Fingertips", " a Masterwork", " Made Simple", ": Secrets Revealed", ": In-Depth Insights", ": a Comprehensive Manual", " in a Nutshell", ": the Expert's Perspective", " Essentials", ", Demystified", ", The Complete Handbook", ": Mastering the Art", " Unveiled", " a Masterwork", " Made Simple", ": Secrets Revealed", ": In-Depth Insights", ": a Comprehensive Manual", " in a Nutshell", ": the Expert's Perspective", " Essentials", ", Demystified", ", The Complete Handbook", ": Mastering the Art", " Unveiled"
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

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        public static string ConvertArchitectToDescription(Architect a)
        {
            string TaskDescription = "";

            if(!a.IsAlive)
            {
                TaskDescription = "dead";
            }
            else if (a.Task == "")
            {
                TaskDescription = ("idle");
            }
            else if (a.Target.Item1 != a.Location.Region || a.Target.Item2 != a.Location || a.Target.Item3 != a.District || a.Target.Item4 != a.Block || a.Target.Item5 != a.Structure)
            {
                if (a.InTheProcessOfLeaving)
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
                }
                else
                {
                    TaskDescription = ("deliberating");
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

            
            if(TaskDescription != "")
            {
                if (a.ReferredToNames.Count > 1)
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
            Engine.Init();

            // TODO: Add your initialization logic here
            Window.IsBorderless = true;

            //create save directory if not already

            if(Directory.Exists(DocumentsFolderPath + "/LightrealmSaves"))
            {
                Directory.CreateDirectory(DocumentsFolderPath + "/LightrealmSaves");
            }

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
            ColorConverter.Add("black", Color.Black);
            ColorConverter.Add("brown", Color.Brown);

            _graphics.PreferredBackBufferWidth = 2560;
            _graphics.PreferredBackBufferHeight = 1440;
            _graphics.ApplyChanges();

            ConvertArchitectToGroupType.Add("warrior", "military");
            ConvertArchitectToGroupType.Add("mercenary", "mercenary");
            ConvertArchitectToGroupType.Add("elder", "religious");
            ConvertArchitectToGroupType.Add("child", "none");
            ConvertArchitectToGroupType.Add("prophet", "religious");
            ConvertArchitectToGroupType.Add("commander", "military");
            ConvertArchitectToGroupType.Add("trader", "trade");
            ConvertArchitectToGroupType.Add("leader", "political");
            ConvertArchitectToGroupType.Add("political figure", "political");
            ConvertArchitectToGroupType.Add("outlaw", "squad");
            ConvertArchitectToGroupType.Add("anarchist", "squad");
            ConvertArchitectToGroupType.Add("craftsman", "guild");
            ConvertArchitectToGroupType.Add("musician", "entertainment");
            ConvertArchitectToGroupType.Add("scholar", "scholarly");
            ConvertArchitectToGroupType.Add("sorcerer", "none");
            ConvertArchitectToGroupType.Add("warlock", "none");
            ConvertArchitectToGroupType.Add("alpha", "none");
            ConvertArchitectToGroupType.Add("prestiged", "none");
            ConvertArchitectToGroupType.Add("no profession", "none");

            //youve saved up a bit of money
            ConvertProfessionToCareerDescription.Add("warrior", "from your extensive military career");
            ConvertProfessionToCareerDescription.Add("mercenary", "from your previous adventures");
            ConvertProfessionToCareerDescription.Add("elder", "from the local temple");
            ConvertProfessionToCareerDescription.Add("prophet", "from the local temple");
            ConvertProfessionToCareerDescription.Add("commander", "from your commander's salary");
            ConvertProfessionToCareerDescription.Add("trader", "from your economic journeys around the continent");
            ConvertProfessionToCareerDescription.Add("leader", "from the local taxes");
            ConvertProfessionToCareerDescription.Add("political figure", "from the local taxes");
            ConvertProfessionToCareerDescription.Add("outlaw", "from some various questionable activities");
            ConvertProfessionToCareerDescription.Add("anarchist", "from the chaos you've sown");
            ConvertProfessionToCareerDescription.Add("craftsman", "by selling shiba statues");
            ConvertProfessionToCareerDescription.Add("musician", "courtesy of ecstatic tavernkeepers");
            ConvertProfessionToCareerDescription.Add("scholar", "by sharing your studies");
            ConvertProfessionToCareerDescription.Add("sorcerer", "from the deity of light");
            ConvertProfessionToCareerDescription.Add("warlock", "from the deity of shadow");
            ConvertProfessionToCareerDescription.Add("alpha", "none");
            ConvertProfessionToCareerDescription.Add("prestiged", "none");

            ConvertProfessionToBuilding.Add("warrior", "watchtower");
            ConvertProfessionToBuilding.Add("mercenary", "tavern");
            ConvertProfessionToBuilding.Add("elder", "shrine");
            ConvertProfessionToBuilding.Add("prophet", "shrine");
            ConvertProfessionToBuilding.Add("commander", "watchtower");
            ConvertProfessionToBuilding.Add("trader", "market");
            ConvertProfessionToBuilding.Add("leader", "library");
            ConvertProfessionToBuilding.Add("political figure", "library");
            ConvertProfessionToBuilding.Add("outlaw", "tavern");
            ConvertProfessionToBuilding.Add("anarchist", "tavern");
            ConvertProfessionToBuilding.Add("craftsman", "forge");
            ConvertProfessionToBuilding.Add("musician", "tavern");
            ConvertProfessionToBuilding.Add("scholar", "library");
            ConvertProfessionToBuilding.Add("sorcerer", "none");
            ConvertProfessionToBuilding.Add("warlock", "none");
            ConvertProfessionToBuilding.Add("alpha", "house");
            ConvertProfessionToBuilding.Add("child", "house");
            ConvertProfessionToBuilding.Add("prestiged", "house");

            ConvertProfessionToBuilding.Add("soldier", "watchtower");
            ConvertProfessionToBuilding.Add("peasant", "none");
            ConvertProfessionToBuilding.Add("merchant", "market");
            ConvertProfessionToBuilding.Add("blacksmith", "forge");
            ConvertProfessionToBuilding.Add("miller", "none");
            ConvertProfessionToBuilding.Add("baker", "tavern");
            ConvertProfessionToBuilding.Add("brewer", "tavern");
            ConvertProfessionToBuilding.Add("tanner", "none");
            ConvertProfessionToBuilding.Add("tailor", "none");
            ConvertProfessionToBuilding.Add("carpenter", "none");
            ConvertProfessionToBuilding.Add("mason", "none");
            ConvertProfessionToBuilding.Add("scribe", "library");
            ConvertProfessionToBuilding.Add("butcher", "none");
            ConvertProfessionToBuilding.Add("fisherman", "none");
            ConvertProfessionToBuilding.Add("weaver", "none");
            ConvertProfessionToBuilding.Add("potter", "none");
            ConvertProfessionToBuilding.Add("miner", "forge");
            ConvertProfessionToBuilding.Add("no profession", "none");

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

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            Engine.LoadContent(GraphicsDevice, Content);
            ContentRoot = Content.RootDirectory;


            // Load your icon texture



            // Assuming your icon is a Texture2D loaded from the Content Pipeline
            //Texture2D iconTexture = Content.Load<Texture2D>("Icon");



            ContentPath = Path.GetFullPath("Content");
            string dataPath = string.Concat(ContentPath, "\\data\\");

            FirstNames = File.ReadAllLines(string.Concat(dataPath, "names.txt")).ToList();
            LastNames = File.ReadAllLines(string.Concat(dataPath, "last-names.txt")).ToList();
            Words = File.ReadAllLines(string.Concat(dataPath, "words.txt")).ToList();
            Syllables = File.ReadAllLines(string.Concat(dataPath, "syllables.txt")).ToList();
            NameSuffixes = File.ReadAllLines(string.Concat(dataPath, "namesuffixes.txt")).ToList();



            /*
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
            */

            /*
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
            */




            /*
            EmptyTileT = Content.Load<Texture2D>("tiles/emptytile");
            nightfellCampT = Content.Load<Texture2D>("tiles/locationtiles/nightfellcamp");
            TileAtlas.Add("nightfellcamp", nightfellCampT);
            nightfellVillageT = Content.Load<Texture2D>("tiles/locationtiles/nightfellvillage");
            TileAtlas.Add("nightfellvillage", nightfellVillageT);
            nightfellTownT = Content.Load<Texture2D>("tiles/locationtiles/nightfelltown");
            TileAtlas.Add("nightfelltown", nightfellTownT);
            nightfellCityT = Content.Load<Texture2D>("tiles/locationtiles/nightfellcity");
            TileAtlas.Add("nightfellcity", nightfellCityT);

            LuminarchCampT = Content.Load<Texture2D>("tiles/locationtiles/luminarchcamp");
            TileAtlas.Add("luminarchcamp", LuminarchCampT);
            LuminarchVillageT = Content.Load<Texture2D>("tiles/locationtiles/luminarchvillage");
            TileAtlas.Add("luminarchvillage", LuminarchVillageT);
            LuminarchTownT = Content.Load<Texture2D>("tiles/locationtiles/luminarchtown");
            TileAtlas.Add("luminarchtown", LuminarchTownT);
            LuminarchCityT = Content.Load<Texture2D>("tiles/locationtiles/luminarchcity");
            TileAtlas.Add("luminarchcity", LuminarchCityT);

            LostCampT = Content.Load<Texture2D>("tiles/locationtiles/lostcamp");
            TileAtlas.Add("archaixcamp", LostCampT);
            LostVillageT = Content.Load<Texture2D>("tiles/locationtiles/lostvillage");
            TileAtlas.Add("archaixvillage", LostVillageT);
            LostTownT = Content.Load<Texture2D>("tiles/locationtiles/losttown");
            TileAtlas.Add("archaixtown", LostTownT);
            LostCityT = Content.Load<Texture2D>("tiles/locationtiles/lostcity");
            TileAtlas.Add("archaixcity", LostCityT);

            PhotonexusOutpostT = Content.Load<Texture2D>("tiles/locationtiles/photonexusoutpost");
            TileAtlas.Add("photonexusgarrison", PhotonexusOutpostT);
            PhotonexusCoreT = Content.Load<Texture2D>("tiles/locationtiles/photonexuscore");
            TileAtlas.Add("photonexuscore", PhotonexusCoreT);
            IsofractalOutpostT = Content.Load<Texture2D>("tiles/locationtiles/isofractaloutpost");
            TileAtlas.Add("isofractalgarrison", IsofractalOutpostT);
            IsofractalCoreT = Content.Load<Texture2D>("tiles/locationtiles/isofractalcore");
            TileAtlas.Add("isofractalcore", IsofractalCoreT);
            ShadeOutpostT = Content.Load<Texture2D>("tiles/locationtiles/shadeoutpost");
            TileAtlas.Add("shadegarrison", ShadeOutpostT);
            ShadeCoreT = Content.Load<Texture2D>("tiles/locationtiles/shadecore");
            TileAtlas.Add("shadecore", ShadeCoreT);

            OutlineT = Content.Load<Texture2D>("tiles/outline");

            SpireT = Content.Load<Texture2D>("tiles/locationtiles/spire");
            TileAtlas.Add("spire", SpireT);
            OutpostT = Content.Load<Texture2D>("tiles/locationtiles/outpost");
            TileAtlas.Add("outpost", OutpostT);

            StrongholdT = Content.Load<Texture2D>("tiles/locationtiles/stronghold");
            TileAtlas.Add("stronghold", StrongholdT);


            KeepT = Content.Load<Texture2D>("tiles/locationtiles/keep");
            TileAtlas.Add("keep", KeepT);
            TowerT = Content.Load<Texture2D>("tiles/locationtiles/tower");
            TileAtlas.Add("tower", TowerT);
            FortressT = Content.Load<Texture2D>("tiles/locationtiles/fortress");
            TileAtlas.Add("fortress", FortressT);
            MonumentT = Content.Load<Texture2D>("tiles/locationtiles/monument");
            TileAtlas.Add("monument", MonumentT);
            SanctumT = Content.Load<Texture2D>("tiles/locationtiles/sanctum");
            TileAtlas.Add("sanctum", SanctumT);
            */

            


            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            Engine.Update(gameTime);
                       

            if(!AllEnteredGameStates.Contains(GameState))
            {
                AllEnteredGameStates.Add(GameState);
            }
            if (Engine.Input.WasKeyPressed(Keys.F9))
            {
                AllEnteredGameStates.Clear();
            }

            if (Engine.Input.IsKeyDown(Keys.Escape))
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

            if(ArchitectIndex > LoadedArchitects.Count)
            {
                ArchitectIndex = 0;
            }


            void UpdateNonPlayerWorld()
            {
                void HandleThrownObjects(List<Object> objects, List<Architect> architects)
                {
                    List<Object> objectsToRemove = new List<Object>();

                    foreach (Object o in objects)
                    {
                        if (o.AirborneTarget != null)
                        {
                            if (o.AirborneCyclesToHitTarget != 0)
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
                                        Observations.Add(new TextStorage(targetArchitect.ReferredToNames[0] + " is enveloped in souls!", Color.Orange));

                                        if (targetArchitect.Energy <= 0)
                                        {
                                            Announcements.Add(new TextStorage(targetArchitect.ReferredToNames[0] + " dies, and rises with a putrid, dark energy!", Color.Purple));
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
                        MakeObservation("The grenade explodes into a portal, sucking everything in!", Color.Purple);
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
                            MakeObservation(a.ReferredToNames[0] + " has been consumed by the portal.", Color.Purple);
                        }
                        objects.Clear();
                        architects.Clear();
                    }
                    else if (o.Type == "lightning grenade")
                    {
                        MakeObservation("The grenade explodes into a swarm of lightning, striking everything around!", Color.Purple);
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
                            MakeObservation(a.ReferredToNames[0] + " is overwhelmed by lightning.", Color.Purple);
                        }
                        // Assuming there's no need to clear objects here as they're not destroyed by lightning
                    }
                    // Other grenade types can be added here
                }

                void HandlePhysicalCollision(Architect target, Object o, Random r)
                {
                    if (!o.Materials.Contains(GameWorld.Spectre))
                    {
                        target.CombatCycles = 25;
                        ((Architect)(o.Thrower)).CombatCycles = 25;

                        Object ArchitectBodyPart = target.BodyParts[r.Next(target.BodyParts.Count)];

                        int InitialStagnantObjectIntegrity = ArchitectBodyPart.Integrity;
                        int InitialThrowingObjectIntegrity = o.Integrity;

                        ArchitectBodyPart.TakeDamageFromObject(o, 0); // Assuming a method exists to handle this

                        Observations.Add(new TextStorage("The " + o.Name + " has collided into " + ArchitectBodyPart.Name + "!", Color.Orange));

                        if (o.Type == "falling star" && ((Architect)(o.Creator)).PathOfStarsLevel > 4)
                        {
                            Observations.Add(new TextStorage(ArchitectBodyPart.Name + " bursts into flames!", Color.Orange));
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
                            Observations.Add(new TextStorage("The " + o.Name + " has been destroyed!", Color.Orange));
                        }
                        if (ArchitectBodyPart.Integrity < 0 && InitialStagnantObjectIntegrity > 0)
                        {
                            Observations.Add(new TextStorage("The " + ArchitectBodyPart.Name + " has been destroyed!", Color.Orange));
                        }
                    }
                }

                void HandleObjectCollision(Object o, Random r)
                {
                    if (o.AirborneTarget is Object targetObject)
                    {
                        int InitialStagnantObjectIntegrity = targetObject.Integrity;
                        int InitialThrowingObjectIntegrity = o.Integrity;

                        targetObject.TakeDamageFromObject(o, 0); // Simulating damage application

                        o.Integrity = InitialThrowingObjectIntegrity - InitialStagnantObjectIntegrity;
                        targetObject.Integrity = InitialThrowingObjectIntegrity - InitialStagnantObjectIntegrity;

                        Observations.Add(new TextStorage("The " + o.Name + " has collided into " + targetObject.Name + "!", Color.Orange));

                        if (o.Integrity < 0 && InitialThrowingObjectIntegrity > 0)
                        {
                            Observations.Add(new TextStorage("The " + o.Name + " has been destroyed!", Color.Orange));
                        }
                        if (targetObject.Integrity < 0 && InitialStagnantObjectIntegrity > 0)
                        {
                            Observations.Add(new TextStorage("The " + targetObject.Name + " has been destroyed!", Color.Orange));
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
                        o.Structure = o.RematerializeLocation.Item5;
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
                            MakeObservation(o.ReferredToNames[0] + " has rematerialized!", Color.Blue);
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
                        if (a.RematerializeLocation.Item2.IsLoaded)
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
                                MakeObservation(a.ReferredToNames[0] + " has rematerialized!", Color.Blue);
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

            if (GameState == "mainscreen" || GameState == "generatingworld" || GameState == "placecivilizations" || GameState == "loadinggamemenu" || GameState == "savinggamemenu" || GameState == "generatehistory" || GameState == "choosepreferences" || GameState == "findstartlocation" || GameState == "architectfound")
            {
                if (MediaPlayer.Queue.ActiveSong == null)
                {
                    MediaPlayer.Play(Engine.LightrealmMainTheme);
                }
                
                if(MediaPlayer.Volume < 1 && Mute == false)
                {
                    MediaPlayer.Volume = MediaPlayer.Volume + 0.004F;
                }
                else if(MediaPlayer.Volume > 0 && Mute == true)
                {
                    MediaPlayer.Volume = MediaPlayer.Volume -= 0.02F;
                }

                if(KeysNewlyPressed.Contains(Keys.M))
                {
                    if(Mute)
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
                if(MediaPlayer.Volume > 0)
                {
                    MediaPlayer.Volume = MediaPlayer.Volume - 0.004F;
                }
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
                            GameState = "generatingworld";
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

                    if (GameState == "savinggame")
                    {
                        SaveGame(GamePlayerParty, GameWorld);
                        GameState = "mainscreen";
                    }
                    else if (GameState == "loadinggamemenu")
                    {
                        int SavesCount = Directory.GetDirectories(DocumentsFolderPath + "/LightrealmSaves").Count();


                        if (SavesCount > 0)
                        {
                            if (KeysNewlyPressed.Contains(Keys.Up))
                            {
                                LoadGameCursor--;
                                if (LoadGameCursor < 0)
                                {
                                    LoadGameCursor = SavesCount - 1;
                                }
                            }
                            if (KeysNewlyPressed.Contains(Keys.Down))
                            {
                                LoadGameCursor++;
                                if (LoadGameCursor >= SavesCount)
                                {
                                    LoadGameCursor = 0;
                                }
                            }
                            if (KeysNewlyPressed.Contains(Keys.Enter))
                            {
                                GameState = "loadinggame";
                                SelectedDirectory = Directory.GetDirectories(DocumentsFolderPath + "/LightrealmSaves")[LoadGameCursor];
                            }
                            if (KeysNewlyPressed.Contains(Keys.Delete))
                            {
                                GameState = "deletinggame";
                                SelectedDirectory = Directory.GetDirectories(DocumentsFolderPath + "/LightrealmSaves")[LoadGameCursor];
                            }
                        }


                        if(KeysNewlyPressed.Contains(Keys.Escape))
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
                        GameWorld = new World(128, 128, 13);
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
                                GameWorld.Civilizations.Add(new Civilization(R, TryX, TryZ, GameWorld));
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
                        if(GameWorld.Cycle < 24192000000)
                        {
                            for (int i = 0; i < 12; i++)
                            {
                                GameWorld.ProgressOneMonth();
                            }
                        }
                        else if(GameWorld.Cycle < 24192000000 * 2)
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                GameWorld.ProgressOneMonth();
                            }
                        }
                        else
                        {
                            GameWorld.ProgressOneMonth();
                            // Update the check for 250 years to the new cycle count
                            if (GameWorld.Cycle >= 72576000000 || KeysNewlyPressed.Contains(Keys.Enter))
                            {
                                File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/lightrealmhistory.txt", GameWorld.HistoricalEvents.ToArray());
                                GameState = "choosepreferences";
                            }
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
                            GameWorld.ProgressOneMonth();

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
                            GameWorld.ProgressOneMonth();
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
                            StatOptions = new List<string>()
                            {
                                "[STR]: Strength (+Melee Power, +Bruteforce Task Efficiency)",
                                "[AGL]: Agility (+Reaction Chance, +Action Speed)",
                                "[DEX]: Dexterity (+Throwing Power, +Tool Effectiveness)",
                                "[END]: Endurance (+Max Energy, +Item Carrying Capacity)",
                                "[CRE]: Creativity (+Craft Quality/Rarity, +Writing Skill)",
                                "[CHA]: Charisma (Generally more likable/manipulative depending on intent, +Percep Magic, +Performance)",
                                "[FOC]: Focus (+Non-Percep Magic, Less susceptible to Magic, Feel Less Pain)"
                            };
                            GameState = "pickstatpreferences";
                            CurrentlyAssigningSkill = 7;
                        }
                    }
                    if (GameState == "pickstatpreferences")
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
                                        GameState = "findstartlocation"; // Update GameState
                                    }

                                    break; // Exit the loop once a stat is assigned
                                }
                            }
                        }
                    }


                    else if (GameState == "findstartlocation")
                    {
                        //try to find a place where someone might be

                        TheChosenOne = null;
                        TheChosenGroup = null;

                        foreach (Architect a in GameWorld.AllArchitects)
                        {
                            if (a.IsAlive && a.Location != null && GameWorld.SettlementTypes.Contains(a.Location.Type))
                            {
                                if (a.Sex == Sexes[CurrentlySelectingSex] &&
                                    a.Race == GameWorld.HumanoidRaces[CurrentlySelectingRace - 1] &&
                                    a.Profession == ArchitectProfessions[CurrentlySelectingArchitectProfession] &&
                                    a.Location.TradersAtThisLocation.Count > 0 &&
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
                            MapCursorX = TheChosenOne.Location.X;
                            MapCursorZ = TheChosenOne.Location.Z;
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
                            GamePlayerParty.Architects[0].Location.Load();

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

                            Exposition.Add(new TextStorage(GamePlayerParty.Architects[0].Name + " is a " + GamePlayerParty.Architects[0].Age + "-year-old " + GenderName + " from " + GamePlayerParty.Architects[0].Location.Name + ". ", Color.Blue));
                            Exposition.Add(new TextStorage(Capitalize(GamePlayerParty.Architects[0].Pronoun) + " works in " + GamePlayerParty.Architects[0].Location.Districts[Game1.r.Next(GamePlayerParty.Architects[0].Location.Districts.Count)].Name + " as a " + GamePlayerParty.Architects[0].Profession + ".", Color.Purple));

                            if (GamePlayerParty.Architects[0].Group != null)
                            {
                                if (GamePlayerParty.Architects[0].Location.Government == GamePlayerParty.Architects[0].Group)
                                {
                                    if (GamePlayerParty.Architects[0].Group.Leader == GamePlayerParty.Architects[0])
                                    {
                                        Exposition.Add(new TextStorage(Capitalize(GamePlayerParty.Architects[0].Pronoun) + " is the leader of " + GamePlayerParty.Architects[0].Group.Name + ", a " + GamePlayerParty.Architects[0].Group.Type + " group that rules " + GamePlayerParty.Architects[0].Group.Leader.Location.Name + ", with " + GamePlayerParty.Architects[0].Group.Architects.Count + " members.", Color.Red));
                                    }
                                    else
                                    {
                                        Exposition.Add(new TextStorage(Capitalize(GamePlayerParty.Architects[0].Pronoun) + " is a member of " + GamePlayerParty.Architects[0].Group.Name + ", a " + GamePlayerParty.Architects[0].Group.Type + " group that rules " + GamePlayerParty.Architects[0].Group.Leader.Location.Name + ", with " + GamePlayerParty.Architects[0].Group.Architects.Count + " members.", Color.Red));
                                    }
                                }
                                else
                                {
                                    if (GamePlayerParty.Architects[0].Group.Leader == GamePlayerParty.Architects[0])
                                    {
                                        Exposition.Add(new TextStorage(Capitalize(GamePlayerParty.Architects[0].Pronoun) + " is the leader of " + GamePlayerParty.Architects[0].Group.Name + ", a " + GamePlayerParty.Architects[0].Group.Type + " group based in " + GamePlayerParty.Architects[0].Group.Leader.Location.Name + ", with " + GamePlayerParty.Architects[0].Group.Architects.Count + " members.", Color.Red));
                                    }
                                    else
                                    {
                                        Exposition.Add(new TextStorage(Capitalize(GamePlayerParty.Architects[0].Pronoun) + " is a member of " + GamePlayerParty.Architects[0].Group.Name + ", a " + GamePlayerParty.Architects[0].Group.Type + " group based in " + GamePlayerParty.Architects[0].Group.Leader.Location.Name + ", with " + GamePlayerParty.Architects[0].Group.Architects.Count + " members.", Color.Red));
                                    }
                                }
                            }

                            if (GamePlayerParty.Architects[0].ScienceStudyPoints == 0 && GamePlayerParty.Architects[0].CultureStudyPoints == 0 && GamePlayerParty.Architects[0].MagicStudyPoints == 0)
                            {
                                Exposition.Add(new TextStorage(GamePlayerParty.Architects[0].Name + " finds " + GamePlayerParty.Architects[0].FavoriteScienceField + " science, " + GamePlayerParty.Architects[0].FavoriteCultureField + " culture, and " + GamePlayerParty.Architects[0].FavoriteMagicField + " magic very intriguing.", Color.Orange));
                            }
                            if (GamePlayerParty.Architects[0].ScienceStudyPoints > 0)
                            {
                                Exposition.Add(new TextStorage(GamePlayerParty.Architects[0].Name + " likes " + GamePlayerParty.Architects[0].FavoriteScienceField + ", and studies it avidly.", Color.Orange));
                            }
                            if (GamePlayerParty.Architects[0].CultureStudyPoints > 0)
                            {
                                Exposition.Add(new TextStorage(GamePlayerParty.Architects[0].Name + " likes " + GamePlayerParty.Architects[0].FavoriteCultureField + ", and explores it relentlessly.", Color.Orange));
                            }
                            if (GamePlayerParty.Architects[0].MagicStudyPoints > 0)
                            {
                                Exposition.Add(new TextStorage(GamePlayerParty.Architects[0].Name + " likes " + GamePlayerParty.Architects[0].FavoriteMagicField + ", and dedicates much research to it's pursuit.", Color.Orange));
                            }

                            Exposition.Add(new TextStorage(GamePlayerParty.Architects[0].Name + " has a deep appreciation for " + GamePlayerParty.Architects[0].FavoriteCloth.Name + ", " + GamePlayerParty.Architects[0].FavoriteStone.Name + ", " + GamePlayerParty.Architects[0].FavoriteWood.Name + ", " + GamePlayerParty.Architects[0].FavoriteGemstone.Name + ", and " + GamePlayerParty.Architects[0].FavoriteMetal.Name + ".", Color.Orange));
                            Exposition.Add(new TextStorage(
                            $"{GamePlayerParty.Architects[0].Pronoun} has " +
                            $"{GamePlayerParty.Architects[0].GetDescription(GamePlayerParty.Architects[0].Strength)} strength, " +
                            $"{GamePlayerParty.Architects[0].GetDescription(GamePlayerParty.Architects[0].Agility)} agility, " +
                            $"{GamePlayerParty.Architects[0].GetDescription(GamePlayerParty.Architects[0].Dexterity)} dexterity, " +
                            $"{GamePlayerParty.Architects[0].GetDescription(GamePlayerParty.Architects[0].Endurance)} endurance, " +
                            $"{GamePlayerParty.Architects[0].GetDescription(GamePlayerParty.Architects[0].Creativity)} creativity, " +
                            $"{GamePlayerParty.Architects[0].GetDescription(GamePlayerParty.Architects[0].Charisma)} charisma, and " +
                            $"{GamePlayerParty.Architects[0].GetDescription(GamePlayerParty.Architects[0].Focus)} focus.", Color.Purple));



                            if (GamePlayerParty.Architects[0].FavoriteBook == null)
                            {
                                Exposition.Add(new TextStorage(GamePlayerParty.Architects[0].Name + " does not particularly enjoy reading.", Color.Yellow));
                            }
                            else
                            {
                                Exposition.Add(new TextStorage(GamePlayerParty.Architects[0].Name + "'s favorite book is " + GamePlayerParty.Architects[0].FavoriteBook.Name + ", a book on " + GamePlayerParty.Architects[0].FavoriteBook.Subject + ".", Color.Yellow));
                            }

                            Exposition.Add(new TextStorage(GamePlayerParty.Architects[0].Name + " has no living family " + GamePlayerParty.Architects[0].Pronoun + " knows of. Perhaps it's time to start anew?", Color.Green));
                            Exposition.Add(new TextStorage("", Color.Green));
                            Exposition.Add(new TextStorage("", Color.Green));
                            Exposition.Add(new TextStorage("", Color.Green));
                            Exposition.Add(new TextStorage("", Color.Green));

                            Dictionary<string, string> CalamityIdeologicalObsessionMapping = new Dictionary<string, string>()
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


                            Exposition.Add(new TextStorage(GameWorld.Calamity[0].Name + " and their gang of " + CalamityIdeologicalObsessionMapping[GameWorld.CalamityIdeologicalObsession] + " have plagued ", Color.Aquamarine));
                            Exposition.Add(new TextStorage(GameWorld.Name + " for decades, but you cannot stand another second.", Color.Aquamarine));
                            Exposition.Add(new TextStorage("It has been a long time since " + GameWorld.Calamity[0].Name + " " + GrievanceReason + ", but", Color.Aquamarine));
                            Exposition.Add(new TextStorage("the memory continues to burden you. Your revenge will be difficult without proper experience and equipment, though.", Color.Aquamarine));
                            Exposition.Add(new TextStorage("You've saved up a bit of money " + ConvertProfessionToCareerDescription[GamePlayerParty.Architects[0].Profession] + ", but the merchants from " + GamePlayerParty.Architects[0].Location.TradersAtThisLocation[0].Base.Name + " won't be here forever.", Color.Aquamarine));
                            Exposition.Add(new TextStorage("Perhaps they can assist you in getting some supplies before you embark on your journey. Do not displease them or their debtshibas, and your quest will be glorious and fortunate.", Color.Aquamarine));

                            Exposition.Add(new TextStorage("", Color.White));
                            Exposition.Add(new TextStorage("Or maybe, " + GamePlayerParty.Architects[0].Name + " can move on, and find a different path.", Color.White));
                            Exposition.Add(new TextStorage("Press SPACE to continue...", Color.White));


                            GameState = "exposition";
                        }
                    }
                    else if (GameState == "partyturn")
                    {
                        if (Keyboard.GetState().IsKeyDown(Keys.F4))
                        {
                            GameWorld.Cycle += 1000;
                        }

                        if(GamePlayerParty.Architects.Count == 0)
                        {
                            GameState = "dead";
                        }
                        else if (!(GamePlayerParty.Architects.Contains(LoadedArchitects[ArchitectIndex])))
                        {
                            GameState = "otherturn";
                        }
                        else
                        {
                            MostRecentPartyTurnArchitect = LoadedArchitects[ArchitectIndex];

                            if (LoadedArchitects[ArchitectIndex].CooldownCycles == 0)
                            {
                                for (int DistrictX = 0; DistrictX < 7; DistrictX++)
                                {
                                    for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                                    {
                                        foreach (Architect a in LoadedArchitects[ArchitectIndex].District.DistrictMap[DistrictX + DistrictZ * 7].Architects)
                                        {
                                            a.UpdateNames();
                                        }
                                        foreach (Object o in LoadedArchitects[ArchitectIndex].District.DistrictMap[DistrictX + DistrictZ * 7].Objects)
                                        {
                                            o.UpdateNames();
                                        }
                                        foreach (Structure s in LoadedArchitects[ArchitectIndex].District.DistrictMap[DistrictX + DistrictZ * 7].Structures)
                                        {
                                            foreach (Room r in s.Rooms)
                                            {
                                                foreach (Architect a in r.Architects)
                                                {
                                                    a.UpdateNames();
                                                }
                                                foreach (Object o in r.Objects)
                                                {
                                                    o.UpdateNames();
                                                }
                                            }
                                        }
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
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.L))
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfLifeLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.D))
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfDeathLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.T))
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfTimeLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.A)) // Assuming 'A' for Path of Stars
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfStarsLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.H)) // Assuming 'H' for Path of Heat
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfHeatLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.I)) // Assuming 'I' for Path of Illusions
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfIllusionsLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.E)) // Assuming 'E' for Path of Ethereality
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfEtherealityLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.V)) // Assuming 'V' for Path of Void
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfVoidLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.S)) // Assuming 'S' for Path of Storms
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfStormsLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.F))
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfForgeLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.K)) // Assuming 'K' for Path of Lore
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfLoreLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.M)) // Assuming 'M' for Path of Mind
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfMindLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.U)) // Assuming 'U' for Path of Soul
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfSoulLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.B)) // Assuming 'B' for Path of Body
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfBodyLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.P)) // Assuming 'P' for Path of Space
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfSpaceLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.R)) // Assuming 'R' for Path of Reality
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfRealityLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;
                                                }

                                                else if (KeysNewlyPressed.Contains(Keys.G)) // Assuming 'G' for Path of Light
                                                {
                                                    LoadedArchitects[ArchitectIndex].PathOfLightLevel += 1;
                                                    LoadedArchitects[ArchitectIndex].SpendableLevels -= 1;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Exposition = new List<TextStorage>();

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
                                                if(!Keyboard.GetState().IsKeyDown(Keys.OemTilde))
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

                                        if (KeysNewlyPressed.Contains(Keys.LeftAlt) || KeysNewlyPressed.Contains(Keys.RightAlt))
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

                                        int CalculateProximityScore(Entity entity, Architect player)
                                        {
                                            bool IsInPlayersHands(Entity entity, Architect player)
                                            {
                                                return player.LeftHandObject == entity || player.RightHandObject == entity;
                                            }

                                            bool IsInPlayersInventory(Entity entity, Architect player)
                                            {
                                                return player.Inventory.Contains(entity);
                                            }

                                            bool IsInSameRoom(Entity entity, Room currentRoom)
                                            {
                                                if (entity is Architect architect)
                                                {
                                                    return architect.Room == currentRoom;
                                                }
                                                else if (entity is Object obj)
                                                {
                                                    return obj.Room == currentRoom;
                                                }
                                                return false;
                                            }

                                            bool IsInSameBlock(Entity entity, Block currentBlock)
                                            {
                                                if (currentBlock.Architects.Contains(entity) || currentBlock.Objects.Contains(entity))
                                                {
                                                    return true;
                                                }

                                                // Check within each structure in the block for the entity
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
                                                // Iterate through all blocks in the district to find the entity
                                                foreach (Block block in currentDistrict.DistrictMap)
                                                {
                                                    if (IsInSameBlock(entity, block))
                                                    {
                                                        return true;
                                                    }
                                                }

                                                return false;
                                            }

                                            // Entity is in the player's hands
                                            if (IsInPlayersHands(entity, player))
                                            {
                                                return 0;
                                            }
                                            // Entity is in the player's inventory, but not currently in their hands
                                            if (IsInPlayersInventory(entity, player))
                                            {
                                                return 1;
                                            }
                                            // Entity is in the same room as the player
                                            if (IsInSameRoom(entity, player.Room))
                                            {
                                                return 2;
                                            }
                                            // Entity is in the same block as the player, but not necessarily in the same room
                                            if (IsInSameBlock(entity, player.Block))
                                            {
                                                return 3;
                                            }
                                            // Entity is in the same district as the player, but not necessarily in the same block
                                            if (IsInSameDistrict(entity, player.District))
                                            {
                                                return 4;
                                            }
                                            // Entity is elsewhere in the world, not in the same district as the player
                                            return 5; // The highest proximity score, indicating the entity is the farthest away
                                        }


                                        List<Entity> FilterSubjectsForCommandPart(string commandPart, List<Entity> allSubjects, Architect MostRecentPartyTurnArchitect, out Dictionary<string, Entity> matchedSubjects)
                                        {
                                            List<(string referredName, Entity entity, int index)> matchedData = new List<(string, Entity, int)>();
                                            matchedSubjects = new Dictionary<string, Entity>();

                                            // Calculate proximity score for each entity and sort by score and referredName length
                                            List<(string referredName, Entity entity, int proximityScore)> allReferredNames = allSubjects
                                                .SelectMany(subject => subject.ReferredToNames.Concat(new[] { subject.Metadata, subject.Name })
                                                .Distinct()
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





                                        static List<Entity> CollectAllSubjects(Architect executor)
                                        {
                                            List<Entity> subjects = new List<Entity>();

                                            // Entities around

                                            foreach (Block b in executor.District.DistrictMap)
                                            {
                                                subjects.AddRange(b.Structures);
                                                subjects.AddRange(b.Objects);
                                                subjects.AddRange(b.Architects);

                                                foreach (Structure s in b.Structures)
                                                {
                                                    foreach (Room r in s.Rooms)
                                                    {
                                                        subjects.AddRange(r.Objects);
                                                        subjects.AddRange(r.Architects);
                                                    }
                                                }
                                            }


                                            // Add the items in the inventories of people who actually matter
                                            List<Entity> subjectsToAdd = new List<Entity>();

                                            foreach (Entity e in subjects)
                                            {
                                                if (e is Architect architect) // Using pattern matching to cast 'e' to 'Architect'
                                                {
                                                    //ok so this will ONLY trigger on the LOADED architects, as the ones arent arround we dont care about.
                                                    subjectsToAdd.AddRange(architect.Inventory);
                                                    subjectsToAdd.AddRange(architect.Clothing);
                                                    if (architect.LeftHandObject != null)
                                                    {
                                                        subjectsToAdd.Add(architect.LeftHandObject);
                                                    }
                                                    if (architect.RightHandObject != null)
                                                    {
                                                        subjectsToAdd.Add(architect.RightHandObject);
                                                    }

                                                    subjectsToAdd.AddRange(architect.CultureBank);
                                                }
                                            }

                                            // After the loop, add all collected entities to 'subjects'
                                            subjects.AddRange(subjectsToAdd);

                                            // Entities not around

                                            foreach (Architect a in GameWorld.AllArchitects)
                                            {
                                                if (!(subjects.Contains(a)))
                                                {
                                                    subjects.Add(a);
                                                }
                                            }

                                            foreach (Location l in GameWorld.AllLocations)
                                            {
                                                if (!(subjects.Contains(l)))
                                                {
                                                    subjects.Add(l);
                                                }
                                            }

                                            foreach (var skillValuePair in executor.XPValues)
                                            {
                                                subjects.Add(new Entity(skillValuePair.Item1.ToLower()));
                                            }

                                            // Spells known to the architect
                                            foreach (var spell in Game1.AllSpells)
                                            {
                                                subjects.Add(new Entity(spell.ToLower()));
                                            }
                                            foreach (var spell in Game1.AllLegendarySpells)
                                            {
                                                subjects.Add(new Entity(spell.ToLower()));
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
                                                "history", "sailing", "farming", "brewing", "jewel crafting", "divination",
                                                "rune crafting", "spellcasting", "negotiation", "investigation", "potions",
                                                "archery", "swordsmanship", "armor crafting", "thievery", "bardic arts",
                                                "mountaineering", "cartography", "astronomy", "necromancy", "elemental magic",
                                                "beast mastery", "divine magic", "illusion", "mechanics", "engineering",
                                                "book", "poem", "song"
                                            };

                                            entitiesToAdd = entitiesToAdd.Except(Domains).ToList();
                                            subjects.AddRange(entitiesToAdd.Select(entity => new Entity(entity)));
                                            subjects.AddRange(Domains.Select(domain => new Entity(domain))); // Ensure all domains are also added as entities if not already covered


                                            foreach (Entity e in subjects)
                                            {
                                                if (e is Object)
                                                {
                                                    ((Object)e).UpdateNames();
                                                }
                                                else if (e is Architect)
                                                {
                                                    ((Architect)e).UpdateNames();
                                                }
                                            }

                                            return subjects;
                                        }


                                        if (KeysNewlyPressed.Contains(Keys.Enter))
                                        {
                                            // Split the original command using the defined separators
                                            string originalCommand = LoadedArchitects[ArchitectIndex].Prompt;
                                            string[] commandSeparators = { " and then ", " and ", ", ", " then " };
                                            string[] commandParts = originalCommand.ToLower().Split(commandSeparators, StringSplitOptions.RemoveEmptyEntries);

                                            LoadedArchitects[ArchitectIndex].LastPrompt = LoadedArchitects[ArchitectIndex].Prompt;

                                            if (commandParts.Length > 0)
                                            {
                                                // Collect all subjects before processing the command
                                                List<Entity> allSubjects = CollectAllSubjects(LoadedArchitects[ArchitectIndex]);
                                                Dictionary<string, Entity> matchedSubjects;

                                                // Execute the first command part
                                                List<Entity> relevantSubjectsForFirstPart = FilterSubjectsForCommandPart(commandParts[0], allSubjects, MostRecentPartyTurnArchitect, out matchedSubjects);
                                                string processedCommandPart = commandParts[0];

                                                // Replace recognized subjects with placeholders
                                                foreach (var matchedSubject in matchedSubjects.Keys)
                                                {
                                                    // Use Regex to accurately replace the matched referredName with "~", considering word boundaries
                                                    processedCommandPart = Regex.Replace(processedCommandPart, $@"\b{Regex.Escape(matchedSubject)}\b", "~", RegexOptions.IgnoreCase);
                                                }

                                                // Execute the first part of the command
                                                bool segmentSuccess = RunCommand(LoadedArchitects[ArchitectIndex], processedCommandPart, relevantSubjectsForFirstPart);

                                                // If there are more parts of the command left, update the prompt with the remaining parts
                                                if (commandParts.Length > 1 && segmentSuccess)
                                                {
                                                    string remainingCommand = string.Join(" and ", commandParts.Skip(1)).Trim();
                                                    LoadedArchitects[ArchitectIndex].Prompt = remainingCommand;
                                                }
                                                else
                                                {
                                                    // If it was the last part or the command segment didn't run successfully, clear the prompt
                                                    LoadedArchitects[ArchitectIndex].Prompt = "";
                                                }
                                            }


                                            ArchitectIndex++; // Proceed to the next architect

                                            // If we have looped through all architects without finding a party member or triggering a reaction
                                            if (ArchitectIndex == LoadedArchitects.Count)
                                            {
                                                ArchitectIndex = 0; // Reset to start from the first architect in the next cycle
                                                TicksSinceLoad++; // Increment the game cycle count
                                                GameWorld.Cycle++;                  // No explicit break here, as we want to continue checking until we find a party member or trigger a reaction
                                                UpdateNonPlayerWorld();
                                            }
                                        }


                                        else if (LoadedArchitects[ArchitectIndex].Structure == null)
                                        {
                                            bool altPressed = Keyboard.GetState().IsKeyDown(Keys.OemTilde); // Check if Alt key is pressed

                                            foreach (var key in KeysNewlyPressed)
                                            {
                                                // Check if the key is one of the movement keys and if Alt is pressed for QWEADZXC keys
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
                                                                if (LoadedArchitects[ArchitectIndex].CombatCycles != 0)
                                                                {
                                                                    MakeObservation("You struggle to escape, and succeed!", Color.OrangeRed);
                                                                }

                                                                int newX = LoadedArchitects[ArchitectIndex].Block.X + offset.dx;
                                                                int newZ = LoadedArchitects[ArchitectIndex].Block.Z + offset.dz;

                                                                LoadedArchitects[ArchitectIndex].CurrentlyMovingPlace = "none";

                                                                if (newX == -1 || newX == 7 || newZ == -1 || newZ == 7)
                                                                {
                                                                    GameState = "travelmenu";
                                                                    GamePlayerParty.MapCursorDistrict = 0;
                                                                    MapCursorZ = LoadedArchitects[ArchitectIndex].Location.Z;
                                                                    MapCursorX = LoadedArchitects[ArchitectIndex].Location.X;

                                                                    //THEN unload the loaction 

                                                                    LoadedArchitects[ArchitectIndex].Location.Unload();
                                                                }
                                                                else
                                                                {
                                                                    LoadedArchitects[ArchitectIndex].Block.Architects.Remove(LoadedArchitects[ArchitectIndex]);
                                                                    LoadedArchitects[ArchitectIndex].Block = LoadedArchitects[ArchitectIndex].District.DistrictMap[newX + newZ * 7];

                                                                    foreach (Structure s in LoadedArchitects[ArchitectIndex].Block.Structures)
                                                                    {
                                                                        if (s.Type != "house" && s.Type != "bighouse")
                                                                        {
                                                                            MakeObservation(s.GetStructureDescription(), Color.Aqua);
                                                                        }
                                                                    }

                                                                    LoadedArchitects[ArchitectIndex].Block.Architects.Add(LoadedArchitects[ArchitectIndex]);
                                                                }

                                                                if (GameState != "travelmenu")
                                                                {
                                                                    ArchitectIndex++; // Proceed to the next architect

                                                                    // If we have looped through all architects without finding a party member or triggering a reaction
                                                                    if (ArchitectIndex == LoadedArchitects.Count)
                                                                    {
                                                                        ArchitectIndex = 0; // Reset to start from the first architect in the next cycle
                                                                        TicksSinceLoad++; // Increment the game cycle count
                                                                        GameWorld.Cycle++;                  // No explicit break here, as we want to continue checking until we find a party member or trigger a reaction
                                                                        UpdateNonPlayerWorld();
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (LoadedArchitects[ArchitectIndex].CombatCycles != 0)
                                                                {
                                                                    MakeObservation("You struggle to escape, and make progress...", Color.OrangeRed);
                                                                }

                                                                LoadedArchitects[ArchitectIndex].CurrentlyMovingPlace = KeyDirections[key];
                                                                LoadedArchitects[ArchitectIndex].CooldownCycles += (int)(Math.Round(35 * LoadedArchitects[ArchitectIndex].Speed()));
                                                                ArchitectIndex++; // Proceed to the next architect

                                                                // If we have looped through all architects without finding a party member or triggering a reaction
                                                                if (ArchitectIndex == LoadedArchitects.Count)
                                                                {
                                                                    ArchitectIndex = 0; // Reset to start from the first architect in the next cycle
                                                                    TicksSinceLoad++; // Increment the game cycle count
                                                                    GameWorld.Cycle++;                  // No explicit break here, as we want to continue checking until we find a party member or trigger a reaction
                                                                    UpdateNonPlayerWorld();
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            MakeObservation("You struggle to escape, and fail!", Color.OrangeRed);
                                                            //movement was a failure, still update game+

                                                            ArchitectIndex++; // Proceed to the next architect
                                                                              // If we have looped through all architects without finding a party member or triggering a reaction
                                                            if (ArchitectIndex == LoadedArchitects.Count)
                                                            {
                                                                ArchitectIndex = 0; // Reset to start from the first architect in the next cycle
                                                                TicksSinceLoad++; // Increment the game cycle count
                                                                GameWorld.Cycle++; // No explicit break here, as we want to continue checking until we find a party member or trigger a reaction
                                                                UpdateNonPlayerWorld();
                                                            }
                                                        }

                                                        //regradless of the outcome, you still loses peed

                                                        if (LoadedArchitects.Count >= 1)
                                                        {
                                                            //i.e. we didn't just leave lol
                                                            LoadedArchitects[ArchitectIndex].CooldownCycles += (int)(Math.Round(35 * LoadedArchitects[ArchitectIndex].Speed()));
                                                        }
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
                                ArchitectIndex++;
                                if (ArchitectIndex == LoadedArchitects.Count)
                                {
                                    ArchitectIndex = 0; // Reset to start from the first architect in the next cycle
                                    TicksSinceLoad++; // Increment the game cycle count
                                    GameWorld.Cycle++; // No explicit break here, as we want to continue checking until we find a party member or trigger a reaction
                                    UpdateNonPlayerWorld();
                                }
                            }

                            //set party turn for otherturn graphics

                            if (LoadedArchitects.Count != 0)
                            {
                                if (PlayerSpendableLevelsLastTick != LoadedArchitects[ArchitectIndex].SpendableLevels && LoadedArchitects[ArchitectIndex].SpendableLevels > 0)
                                {
                                    Announcements.Add(new TextStorage("You have leveled up to level " + LoadedArchitects[ArchitectIndex].Level + ". You have a new spendable level in Inventory.", Color.AliceBlue));
                                }
                                PlayerSpendableLevelsLastTick = LoadedArchitects[ArchitectIndex].SpendableLevels;

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
                        }
                    }
                    else if (GameState == "otherturn")
                    {
                        StoredAttacks = new List<Attack>();

                        List<Attack> playerAttacks = new List<Attack>();

                        UpdateNonPlayerWorld();

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
                                if (GamePlayerParty.Architects.Any(a => a.ReferredToNames.Contains(attack.Target)))
                                {
                                    GameState = "reaction";
                                    ArchitectIndex++; // Prepare the next architect for after the reaction
                                    if (ArchitectIndex == LoadedArchitects.Count)
                                    {
                                        ArchitectIndex = 0; // Reset to start from the first architect in the next cycle
                                        TicksSinceLoad++; // Increment the game cycle count
                                        GameWorld.Cycle++; // No explicit break here, as we want to continue checking until we find a party member or trigger a reaction
                                        UpdateNonPlayerWorld();
                                    }
                                    break; // Break from the while loop to handle the reaction
                                }
                            }

                            if (GameState == "reaction") break; // If reaction state is triggered, exit early

                            // If the architect is a party member, switch to party turn and break
                            if (GamePlayerParty.Architects.Contains(currentArchitect) && currentArchitect.CooldownCycles == 0)
                            {
                                if (currentArchitect.RuptureMode == true)
                                {
                                    GameState = "triggerrupture";
                                }
                                else
                                {
                                    GameState = "partyturn";
                                }

                                break; // Break from the while loop to switch to party turn
                            }

                            ArchitectIndex++; // Proceed to the next architect

                            // If we have looped through all architects without finding a party member or triggering a reaction
                            if (ArchitectIndex == LoadedArchitects.Count)
                            {
                                ArchitectIndex = 0; // Reset to start from the first architect in the next cycle
                                TicksSinceLoad++; // Increment the game cycle count
                                GameWorld.Cycle++; // No explicit break here, as we want to continue checking until we find a party member or trigger a reaction
                                UpdateNonPlayerWorld();
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

                        if(KeysNewlyPressed.Contains(Keys.Enter))
                        {
                            GameWorld.TriggerRupture(MapCursorX, MapCursorZ, LoadedArchitects[ArchitectIndex]);

                            LoadedArchitects[ArchitectIndex].RuptureMode = false;

                            if (LoadedArchitects[ArchitectIndex].Location.Region.Biome == "ethereal")
                            {
                                MakeObservation(LoadedArchitects[ArchitectIndex].Name + " successfully killed themselves in the fractal rift. How embarrassing...", Color.Magenta);
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
                                ArchitectIndex++; // Proceed to the next architect

                                // If we have looped through all architects without finding a party member or triggering a reaction
                                if (ArchitectIndex == LoadedArchitects.Count)
                                {
                                    ArchitectIndex = 0; // Reset to start from the first architect in the next cycle
                                    TicksSinceLoad++; // Increment the game cycle count
                                    GameWorld.Cycle++; // No explicit break here, as we want to continue checking until we find a party member or trigger a reaction
                                    UpdateNonPlayerWorld();
                                }

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
                    else if (GameState == "travelmenu")
                    {
                        Exposition = new List<TextStorage>();

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
                                bool isDestinationOcean = GameWorld.WorldMap[(MapCursorX + DeltaX) + ((MapCursorZ + DeltaZ) * GameWorld.Width)].Biome == "ocean";
                                bool isDestinationVoid = GameWorld.WorldMap[(MapCursorX + DeltaX) + ((MapCursorZ + DeltaZ) * GameWorld.Width)].Biome == "void";
                                bool isDestinationEthereal = GameWorld.WorldMap[(MapCursorX + DeltaX) + ((MapCursorZ + DeltaZ) * GameWorld.Width)].Biome == "ethereal";
                                bool isDestinationPort = !string.IsNullOrEmpty(GameWorld.WorldMap[(MapCursorX + DeltaX) + ((MapCursorZ + DeltaZ) * GameWorld.Width)].PortName);
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

                                    GameWorld.Cycle += Math.Round(18000 * GamePlayerParty.Architects.Min(architect => architect.Speed()));

                                    if (StoredEvent != null)
                                    {
                                        if (StoredEvent.Region.X != MapCursorX || StoredEvent.Region.Z != MapCursorZ)
                                        {
                                            StoredEvent = null;
                                        }
                                    }
                                    else
                                    {
                                        foreach (InteractableEvent e in GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Events)
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

                            if (GameWorld.WorldMap[MapCursorX + MapCursorZ * 128].Biome == "ocean")
                            {
                                if (GameWorld.WorldMap[MapCursorX + MapCursorZ * 128].PortName != "")
                                {
                                    Exposition.Add(new TextStorage("You can leave this port to start sailing.", Color.LightBlue));
                                }
                                else
                                {
                                    Exposition.Add(new TextStorage("You are currently sailing.", Color.LightBlue));
                                }
                            }
                            else
                            {
                                Exposition.Add(new TextStorage("The area is vacant and beautiful.", Color.Magenta));
                                Exposition.Add(new TextStorage("You could gather " + ConvertListToString(biomeDictionary[GameWorld.WorldMap[MapCursorX + MapCursorZ * 128].Biome]) + " here.", Color.Magenta));
                                Exposition.Add(new TextStorage("Press [S] to stop here.", Color.Magenta));
                            }
                        }

                        if (StoredEvent != null)
                        {
                            Exposition.Clear();
                            Exposition.Add(new TextStorage(StoredEvent.Intrigue, Color.LimeGreen));
                            Exposition.Add(new TextStorage("Press [E] to approach.", Color.Red));
                        }



                        if (KeysNewlyPressed.Contains(Keys.S) && GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Width].MyLocation == null)
                        {
                            GameState = "gatheringandcrafting";
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


                            Exposition.Add(new TextStorage(Game1.MostRecentPartyTurnArchitect.Name + " arrives at " + GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Width].MyLocation.Name + ", on the outskirts of " + Game1.MostRecentPartyTurnArchitect.District.Name + ".", Color.Blue));

                            // Determine structure counts and such
                            List<Structure> Taverns = new List<Structure>();
                            List<Structure> Markets = new List<Structure>();
                            List<Structure> Houses = new List<Structure>();

                            for (int x = 0; x < 7; x++)
                            {
                                for (int z = 0; z < 7; z++)
                                {
                                    foreach (Structure S in Game1.MostRecentPartyTurnArchitect.Location.Districts[GamePlayerParty.MapCursorDistrict].DistrictMap[x + z * 7].Structures)
                                    {
                                        if (S.Type == "tavern")
                                        {
                                            Taverns.Add(S);
                                        }
                                        else if (S.Type == "market")
                                        {
                                            Markets.Add(S);
                                        }
                                        else if (S.Type == "house")
                                        {
                                            Houses.Add(S);
                                        }
                                    }

                                }
                            }

                            if (Game1.MostRecentPartyTurnArchitect.Location.Type == "spire")
                            {
                                Exposition.Add(new TextStorage("The spire of " + Game1.MostRecentPartyTurnArchitect.Location.Name + " looms ahead of you. Whatever lies within is unknown.", new Color(100, 100, 100)));
                            }

                            if (MostRecentPartyTurnArchitect.Location.Districts[GamePlayerParty.MapCursorDistrict].Architects.Count == 0)
                            {
                                if (GameWorld.CalamityStructures.Contains(Game1.MostRecentPartyTurnArchitect.Location.Type))
                                {
                                    if (r.Next(0, 4) == 0)
                                    {
                                        Exposition.Add(new TextStorage("No one seems to be home, but you see a tall signaling brazier. Maybe it can be reactivated somehow?", Color.DarkGray));
                                    }
                                    else
                                    {
                                        Exposition.Add(new TextStorage("Noises emnate from the structure, someone must be home...", Color.DarkGray));
                                        ((Architect)(MostRecentPartyTurnArchitect.Location.Government)).Location = MostRecentPartyTurnArchitect.Location;
                                        ((Architect)(MostRecentPartyTurnArchitect.Location.Government)).Location.Districts[0].Architects.Add(((Architect)(MostRecentPartyTurnArchitect.Location.Government)));
                                    }
                                }
                                else
                                {
                                    Exposition.Add(new TextStorage("Strangely enough, no one seems to be here at all...", Color.DarkGray));
                                }
                            }
                            else if (MostRecentPartyTurnArchitect.Location.Districts[GamePlayerParty.MapCursorDistrict].Architects.Count == 1)
                            {
                                Exposition.Add(new TextStorage("You hear the noises of someone off in the distance. Might be worth investigating...", Color.Gray));
                            }
                            else if (MostRecentPartyTurnArchitect.Location.Districts[GamePlayerParty.MapCursorDistrict].Architects.Count < 5)
                            {
                                Exposition.Add(new TextStorage("You only hear and see a couple of people. It doesn't seem like many people are here.", Color.Gray));
                            }
                            else if (MostRecentPartyTurnArchitect.Location.Districts[GamePlayerParty.MapCursorDistrict].Architects.Count < 10)
                            {
                                Exposition.Add(new TextStorage("This doesn't seem like a very populated area.", Color.Gray));
                            }
                            else if (MostRecentPartyTurnArchitect.Location.Districts[GamePlayerParty.MapCursorDistrict].Architects.Count < 25)
                            {
                                Exposition.Add(new TextStorage("A variety of people are busy going about their days.", Color.LightGray));
                            }
                            else if (MostRecentPartyTurnArchitect.Location.Districts[GamePlayerParty.MapCursorDistrict].Architects.Count < 50)
                            {
                                Exposition.Add(new TextStorage("The area is bustling with activity", Color.White));
                            }
                            else if (MostRecentPartyTurnArchitect.Location.Districts[GamePlayerParty.MapCursorDistrict].Architects.Count < 125)
                            {
                                Exposition.Add(new TextStorage("You hear swarms of people caught up in all sorts of activities.", Color.White));
                            }
                            else
                            {
                                Exposition.Add(new TextStorage("There is a MASSIVE crowd of people here. A mixture of chaos and order permeates the area.", Color.Yellow));
                            }

                            if (Taverns.Count == 1)
                            {
                                Exposition.Add(new TextStorage("Seems like a tavern is nearby. " + Taverns[0].Name + " may be a good place to stop.", Color.Orange));
                            }
                            else if (Taverns.Count > 1)
                            {
                                Exposition.Add(new TextStorage("Multiple taverns are competing in this district. May be worth it to check them out.", Color.Orange));
                            }

                            if (Markets.Count == 1)
                            {
                                Exposition.Add(new TextStorage("You see a large market. " + Markets[0].Name + " must be the hub of economic activity here.", Color.Yellow));
                            }
                            else
                            {
                                Exposition.Add(new TextStorage("No commerce is present. The market for this location must be in a different district.", Color.Yellow));
                            }


                            GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Width].MyLocation.Load();

                            Game1.MostRecentPartyTurnArchitect.Location = GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Width].MyLocation;
                            Game1.MostRecentPartyTurnArchitect.District = Game1.MostRecentPartyTurnArchitect.Location.Districts[GamePlayerParty.MapCursorDistrict];

                        }

                    }
                    else if (GameState == "exposition")
                    {
                        if (GamePlayerParty.CurrentEvent != null)
                        {
                            Exposition = new List<TextStorage>
                            {
                                new TextStorage(GamePlayerParty.CurrentEvent.Info, Color.White),
                                new TextStorage("Do you approach? (Y/N)", Color.Yellow)
                            };

                            if (KeysNewlyPressed.Contains(Keys.Y))
                            {
                                MostRecentPartyTurnArchitect.Location = new Location("clearing", GameWorld.GetRace(""), 0, 0, 0, MapCursorX, MapCursorZ, GamePlayerParty.CurrentEvent.HomeCivilization, GamePlayerParty.CurrentEvent.Region);
                                MostRecentPartyTurnArchitect.District = MostRecentPartyTurnArchitect.Location.Districts[0];
                                MostRecentPartyTurnArchitect.Block = MostRecentPartyTurnArchitect.District.DistrictMap[new[] { 0, 1, 2, 3, 4, 5, 6, 7, 13, 14, 20, 21, 27, 28, 34, 35, 41, 42, 43, 44, 45, 46, 47, 48 }[new Random().Next(24)]];

                                MostRecentPartyTurnArchitect.Location.Load();

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
                            }
                        }
                        else
                        {
                            if (KeysNewlyPressed.Contains(Keys.Space))
                            {
                                if (SeenTips == true)
                                {
                                    GameState = "otherturn";
                                }
                                else
                                {
                                    SeenTips = true;
                                    Exposition.Clear(); ;
                                    Exposition.Add(new TextStorage("Use commands to explore. Anything from \"ask where a tavern is\" to \"forcefully throw Diamoi Voklizo at happy debtshiba\" is accepted.", Color.White));
                                    Exposition.Add(new TextStorage("Watch your Energy and Bleeding, dictated by the Heart and its droplets at the top of the screen. Do not let it go dark.", Color.White));
                                    Exposition.Add(new TextStorage("Access player information with commands or use \"open menu\" to see all of it.", Color.White));
                                    Exposition.Add(new TextStorage("Good luck...", Color.White));
                                    Exposition.Add(new TextStorage("", Color.White));
                                    Exposition.Add(new TextStorage("", Color.White));
                                    Exposition.Add(new TextStorage("", Color.White));
                                    Exposition.Add(new TextStorage("", Color.White));
                                    Exposition.Add(new TextStorage("", Color.White));
                                    Exposition.Add(new TextStorage("", Color.White));
                                    Exposition.Add(new TextStorage("", Color.White));
                                    Exposition.Add(new TextStorage("", Color.White));
                                    Exposition.Add(new TextStorage("", Color.White));
                                    Exposition.Add(new TextStorage("", Color.White));
                                    Exposition.Add(new TextStorage("", Color.White));
                                    Exposition.Add(new TextStorage("", Color.White));
                                    Exposition.Add(new TextStorage("", Color.White));
                                    Exposition.Add(new TextStorage("", Color.White));
                                    Exposition.Add(new TextStorage("", Color.White));
                                }
                            }
                        }
                    }
                    else if (GameState == "gatheringandcrafting")
                    {
                        if (KeysNewlyPressed.Contains(Keys.X))
                        {
                            // Transition to the travel menu
                            GameState = "travelmenu";
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
                            {1, "HarvestableWood"},
                            {2, "HarvestableStone"},
                            {3, "HarvestableMetal"},
                            {4, "HarvestableSand"},
                            {5, "HarvestableFiber"},
                            {6, "HarvestableIce"}
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
                                if (architect.Inventory.Any(item => item.Name.Equals(requiredTool, StringComparison.OrdinalIgnoreCase)))
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
                                Material resourceMaterial = harvestableMaterials.FirstOrDefault(hm => hm.Value.GetType().Name.Contains(resourceKey.Value)).Value;
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
                                            int count = r.Next(0, architect.Strength);

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
                                                Exposition.Add(new TextStorage($"{architect.Name} harvested {count} {resourceType}.", Color.LightBlue));
                                                for (int i = 0; i < count; i++)
                                                {
                                                    architect.Inventory.Add(new Object(null, resourceType, new List<Material>() { resourceMaterial }, null));
                                                }
                                            }
                                            else
                                            {
                                                // Select a random break activity
                                                string breakActivity = breakActivities[r.Next(breakActivities.Count)];
                                                Exposition.Add(new TextStorage($"{architect.Name} {breakActivity}.", Color.LightBlue));
                                            }

                                        }
                                    }

                                    if (toolNeeded)
                                    {
                                        Exposition.Add(new TextStorage($"You need a {toolRequired} to harvest {resourceType} here.", Color.Yellow));
                                    }
                                }
                                else
                                {
                                    Exposition.Add(new TextStorage($"There is no {resourceKey.Value.ToLower().Replace("harvestable", "")} to harvest in this biome.", Color.Red));
                                }
                            }
                        }
                    }

                }
            }


            previousState = Keyboard.GetState();

            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();

            void DrawWorld()
            {
                // Variables for elevation-based color adjustment
                float minBrightness = 0.0f; // Minimum brightness for the lowest elevation
                float maxBrightness = 1.55f; // Maximum brightness for the highest elevation

                // Start drawing with additive blend state for region tiles (elevation)
                _spriteBatch.End();
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {
                        int index = x + z * GameWorld.Width;
                        Rectangle tileRect = new Rectangle(
                            (10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0),
                            10 + z * TileZDistance,
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
                            //_spriteBatch.Draw(CursorT, tileRect, Color.White);


                            _spriteBatch.Draw(Engine.Render.SpriteSheet.Texture, tileRect, Engine.Render.SpriteSheet.Get("tile-cursor"), Color.White);
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

                            string tile = string.Concat("tile-", GameWorld.WorldMap[index].Biome);
                            _spriteBatch.Draw(Engine.Render.SpriteSheet.Texture, tileRect, Engine.Render.SpriteSheet.Get(tile), finalColor);
                            //_spriteBatch.Draw(TileAtlas[GameWorld.WorldMap[index].Biome], tileRect, finalColor);
                        }
                    }
                }

                // End the first phase of drawing
                _spriteBatch.End();

                // Begin second phase for drawing structures, ports without elevation or blight adjustments
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {
                        int index = x + z * GameWorld.Width;
                        Rectangle tileRect = new Rectangle(
                            (10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0),
                            10 + z * TileZDistance,
                            TileSize,
                            TileSize
                        );

                        // Check if the region and the location (if it exists) are both explored
                        if (GameWorld.WorldMap[index].Explored || GameState != "travelmenu")
                        {
                            // Draw the port if it exists and the region is explored
                            if (GameWorld.WorldMap[index].PortName != "" &&
                                (GameState != "travelmenu" || GameWorld.WorldMap[index].Explored))
                            {
                                _spriteBatch.Draw(Engine.Render.SpriteSheet.Texture, tileRect, Engine.Render.SpriteSheet.Get("tile-port"), Color.White);
                               // _spriteBatch.Draw(PortT, tileRect, Color.White);
                            }

                            // Draw locations based on exploration status
                            if (GameWorld.WorldMap[index].MyLocation != null &&
                                (GameState != "travelmenu" || GameWorld.WorldMap[index].MyLocation.Explored))
                            {
                                string tile = string.Concat("loc-", GameWorld.WorldMap[index].MyLocation.PrimaryRace.Name, GameWorld.WorldMap[index].MyLocation.Type);
                                _spriteBatch.Draw(Engine.Render.SpriteSheet.Texture, tileRect, Engine.Render.SpriteSheet.Get(tile), Color.White);
                                /*
                                _spriteBatch.Draw(
                                    TileAtlas[GameWorld.WorldMap[index].MyLocation.PrimaryRace.Name + GameWorld.WorldMap[index].MyLocation.Type],
                                    tileRect,
                                    Color.White
                                );
                                */
                            }
                        }
                    }
                }

                // End the second phase of drawing
                _spriteBatch.End();
                _spriteBatch.Begin();
            }

            if (KeysNewlyPressed.Contains(Keys.PageUp) && Keyboard.GetState().IsKeyDown(Keys.LeftAlt))
            {
                int X = Mouse.GetState().X;
                int Z = Mouse.GetState().Y;
            }

            string GenerateUniqueKeyForObject(Object obj)
            {
                // Base key is constructed from the material name, object name (if not null), and type
                var key = $"{obj.Materials[0].Name}-{obj.Name ?? "null"}-{obj.Type}";

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
            List<(string Description, int Count, int IndentationLevel)> CondenseAndStructureList(IEnumerable<Object> objects, int indentationLevel = 0, bool isShadowStorage = false)
            {
                var result = new List<(string Description, int Count, int IndentationLevel)>();

                foreach (var obj in objects)
                {
                    // Handle shadow storage as a special case
                    if (obj.Type == "shadow storage" && !isShadowStorage)
                    {
                        // Add the shadow storage itself to the list
                        result.Add(($"{obj.Materials[0].Name} {obj.Name ?? obj.Type}", 1, indentationLevel));

                        // Retrieve contents from the shadow storage, marking the recursive call with isShadowStorage = true
                        var shadowContents = LoadedArchitects[ArchitectIndex].ShadowStorage;
                        var structuredShadowContents = CondenseAndStructureList(shadowContents, indentationLevel + 1, true);
                        result.AddRange(structuredShadowContents);
                        continue; // Skip the regular processing for this object
                    }

                    string description = $"{obj.Materials[0].Name} {obj.Name ?? obj.Type}";
                    int count = objects.Count(o => GenerateUniqueKeyForObject(o) == GenerateUniqueKeyForObject(obj));

                    result.Add((description, count, indentationLevel));

                    if (obj.ContainedObjects.Any())
                    {
                        var sortedContainedObjects = obj.ContainedObjects
                            .OrderBy(co => co.Materials[0].Name)
                            .ThenBy(co => co.Name ?? co.Type)
                            .ToList();

                        var structuredContainedObjects = CondenseAndStructureList(sortedContainedObjects, indentationLevel + 1);
                        result.AddRange(structuredContainedObjects);
                    }
                }

                // Ensure results are unique at the current level before returning
                return result.GroupBy(r => r.Description).Select(g => g.First()).ToList();
            }

            // GetCurrentPageList as you've defined

            // DrawList as you've defined
            void DrawList(SpriteBatch spriteBatch, List<(string Description, int Count, int IndentationLevel)> list, Vector2 startCoords, SpriteFont font)
            {
                int line = 0;
                foreach (var (Description, Count, IndentationLevel) in list)
                {
                    string textToDraw = $"{new string(' ', 4 * IndentationLevel)}{Description} x{Count}";
                    spriteBatch.DrawString(font, textToDraw, new Vector2(startCoords.X, startCoords.Y + line * 20), Color.White);
                    line++;
                }
            }

            // Assuming you have a method to handle user input to navigate pages, you would update CurrentObjectPage accordingly
            // And call PaginateAndDrawList again to redraw the list for the new page


            void DrawCenteredText(SpriteBatch spriteBatch, string text, float yPosition, SpriteFont Font, Color color)
            {
                // Load the GameData.Shibafont SpriteFont
                Vector2 textSize = Font.MeasureString(text);

                // Calculate the center position based on the PreferredBackBufferWidth
                float xPosition = (GraphicsDevice.PresentationParameters.BackBufferWidth - textSize.X) / 2;

                // Adjust the y position if needed
                Vector2 position = new Vector2(xPosition, yPosition);

                // Draw the text
                spriteBatch.DrawString(Font, text, position, color);
            }
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


            //tips


            if (GameState == "generatehistory" || GameState == "choosepreferences")
            {
                // This would ideally be within your update or draw loop
                if (currentObject == "")
                {
                    currentObject = Tips[r.Next(Tips.Count)]; // Assuming 'Tips' is a List<string> and 'r' is a Random instance
                }

                DrawCenteredText(_spriteBatch, currentObject, 1250, Engine.Render.Shibafont, new Color(objectOpacity, 0, 0));

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
                int screenWidth = _graphics.PreferredBackBufferWidth;
                int imageWidth = 744;

                Rectangle destinationRectangle = new Rectangle((screenWidth - imageWidth) / 2, 100, imageWidth, 216);

                _spriteBatch.Draw(Engine.Render.TitleScreen, destinationRectangle, Color.White);

                if(FakeName == "")
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


                DrawCenteredText(_spriteBatch, "The tale of " + FakeName, 300, Engine.Render.Shibafont, new Color(NameOpacity, NameOpacity, NameOpacity));

                NameCycles++;

                if(NameCycles < 50)
                {
                    NameOpacity += 5;
                }
                else if (NameCycles > 150)
                {
                    NameOpacity -= 5;
                }

                if(NameCycles == 100)
                {
                    NameOpacity = 255;
                    //off sync but i cant tell why so ill do this
                }

                if(NameCycles > 199)
                {
                    FakeName = GenerateUniqueFakeName();
                    NameCycles = 0;
                }

                //DrawCenteredText(_spriteBatch, "Press F to start a new Founder game.", 500);
                DrawCenteredText(_spriteBatch, "Press C to start a new game.", 550, Engine.Render.Shibafont, Color.White);
                DrawCenteredText(_spriteBatch, "Hold CTRL+L to load an existing save.", 600, Engine.Render.Shibafont, Color.White);

                _spriteBatch.Draw(Engine.Render.Astrionalis, new Rectangle(100, 200, 640, 1280), Color.White);
                _spriteBatch.Draw(Engine.Render.Celestrioris, new Rectangle(1800, 200, 720, 1270), Color.White);
            }
            else if (GameState == "savinggame")
            {
                _spriteBatch.DrawString(Engine.Render.Shibafont, "Saving " + GameWorld.Name + " data. This may take half a minute...", new Vector2(200, 200), Color.White);
            }
            else if (GameState == "loadinggamemenu")
            {
                int SavesCount = Directory.GetDirectories(DocumentsFolderPath + "/LightrealmSaves").Count();

                if (SavesCount > 0)
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "Use arrow keys and ENTER to select a savegame:", new Vector2(200, 200), Color.White);

                    int Number = 0;

                    foreach (string d in Directory.GetDirectories(DocumentsFolderPath + "/LightrealmSaves"))
                    {
                        if (Number == LoadGameCursor)
                        {
                            _spriteBatch.DrawString(Engine.Render.Shibafont, "(>) " + d, new Vector2(200, 230 + Number * 30), Color.White);
                        }
                        else
                        {
                            _spriteBatch.DrawString(Engine.Render.Shibafont, "( ) " + d, new Vector2(200, 230 + Number * 30), Color.White);
                        }

                        Number++;
                    }

                    _spriteBatch.DrawString(Engine.Render.Shibafont, "Press DELETE to remove a savegame.", new Vector2(200, 1300), Color.White);
                }
                else
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "You have no savegames. You should probably go make one now.", new Vector2(200, 200), Color.White);
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "Press ESC to return to title.", new Vector2(200, 230), Color.White);
                }
            }
            else if (GameState == "deletinggame")
            {
                _spriteBatch.DrawString(Engine.Render.Shibafont, "This action cannot be undone. Are you sure?", new Vector2(30, 30), Color.White);
                _spriteBatch.DrawString(Engine.Render.Shibafont, "Confirm (CTRL Y)", new Vector2(30, 60), Color.White);
                _spriteBatch.DrawString(Engine.Render.Shibafont, "Cancel (CTRL N)", new Vector2(30, 90), Color.White);
            }
            else if (GameState == "loadinggame")
            {
                _spriteBatch.DrawString(Engine.Render.Shibafont, "Loading save data, this may take half a minute...", new Vector2(200, 200), Color.White);
            }
            else if (GameState == "pickstatpreferences")
            {
                int Line = 1;

                if(StatOptions.Count == 7)
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "Choose your most prefered stat of these:", new Vector2(100, 100), Color.White);
                }
                else
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "Lovely, now choose your next-highest stat:", new Vector2(100, 100), Color.White);
                }

                foreach(string s in StatOptions)
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, s, new Vector2(100, 100 + Line*50), Color.White);
                    Line++;
                }
            }
            else if (GameState == "generatingworld")
            {
                DrawCenteredText(_spriteBatch, "Generating Landmass...", 100, Engine.Render.Shibafont, Color.White);
            }
            else if (GameState == "generatehistory")
            {
                void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color)
                {
                    Vector2 direction = end - start;
                    float rotation = (float)Math.Atan2(direction.Y, direction.X);
                    float length = direction.Length();

                    spriteBatch.Draw(
                        texture: Engine.Render.PixelTexture,
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

                for (int i = 20; i != 0; i--)
                {
                    int CurrentItemListing = (GameWorld.HistoricalEvents.Count - i);
                    if (CurrentItemListing >= 0)
                    {
                        _spriteBatch.DrawString(Engine.Render.BabyShibafont, GameWorld.HistoricalEvents[CurrentItemListing], new Vector2(1800, 400 + ((-1) * (20 * i))), Color.White);
                    }
                }

                for (int i = 20; i != 0; i--)
                {
                    int CurrentItemListing = (GameWorld.AbridgedHistoricalEvents.Count - i);
                    if (CurrentItemListing >= 0)
                    {
                        _spriteBatch.DrawString(Engine.Render.BabyShibafont, GameWorld.AbridgedHistoricalEvents[CurrentItemListing], new Vector2(1800, 800 + ((-1) * (20 * i))), Color.White);
                    }
                }

                DrawWorld();

                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {
                        if (new Rectangle((10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0), 10 + z * TileZDistance, TileSize, TileSize).Contains(new Point(Mouse.GetState().X, Mouse.GetState().Y)))
                        {
                            if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation == null)
                            {
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "No Notable Location, " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome, new Vector2(DrawX + 500, DrawY + 30), Color.White);
                            }
                            else
                            {
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Name + ", " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome + " " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Type + ".", new Vector2(DrawX, DrawY), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Population: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.TruePopulation(), new Vector2(DrawX, DrawY + 30), Color.White);

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
                                    ArchitectPopulation += d.Architects.Count;

                                    for (int DistrictX = 0; DistrictX < 7; DistrictX++)
                                    {
                                        for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                                        {
                                            structures += d.DistrictMap[DistrictX + DistrictZ * 7].Structures.Count();
                                        }
                                    }
                                }

                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Total Architects: " + ArchitectPopulation, new Vector2(DrawX, DrawY + 60), Color.White);

                                if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government == null)
                                {
                                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, "No Notable Government", new Vector2(DrawX, DrawY + 90), Color.White);
                                }
                                else
                                {
                                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Government: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government.Name, new Vector2(DrawX, DrawY + 90), Color.White);
                                }

                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Colonization Desire: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.ColonizationDesire, new Vector2(DrawX, DrawY + 120), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Wealth: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Wealth, new Vector2(DrawX, DrawY + 150), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Is Saving Up To Settle: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.IsSavingUpToSettle.ToString(), new Vector2(DrawX, DrawY + 180), Color.White);


                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Structures: " + structures, new Vector2(DrawX, DrawY + 210), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Distinct Groups: " + groups, new Vector2(DrawX, DrawY + 240), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Districts: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Districts.Count, new Vector2(DrawX, DrawY + 270), Color.White);
                            }
                        }
                    }
                }// Updating cycle counts for drawing week, month, and year
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Week " + Math.Round((decimal)(GameWorld.Cycle / 6048000)), new Vector2(1900, 1280), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Month " + Math.Round((decimal)(GameWorld.Cycle / 24192000), 0, MidpointRounding.ToNegativeInfinity).ToString(), new Vector2(1900, 1240), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Year " + Math.Round((decimal)(Math.Round((decimal)(GameWorld.Cycle / 290304000), 0, MidpointRounding.ToNegativeInfinity)), 0, MidpointRounding.ToNegativeInfinity), new Vector2(1900, 1200), Color.White);

                // Keeping other text draws as they are since they're not dependent on cycle conversion
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Architects, Total: " + GameWorld.TotalArchitects + ", Living: " + GameWorld.LivingArchitects + ", Dead: " + GameWorld.DeadArchitects, new Vector2(1900, 1320), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Distinct Groups: " + GameWorld.Groups.Count, new Vector2(1900, 1360), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Written Objects: " + GameWorld.TotalWrittenObjects, new Vector2(1900, 1400), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Crafts Practiced: " + GameWorld.TotalCrafts, new Vector2(2100, 1400), Color.White);

                // Trade lines code remains the same as it's not directly dependent on the cycle count conversions

                // Updating the calculation for Month and Year using new cycles
                int Month = (int)Math.Round((decimal)(GameWorld.Cycle / 24192000)) % 12 + 1;
                int Year = (int)Math.Round((decimal)(GameWorld.Cycle / 290304000));

                // Updating the drawing of game world name, month/year display, and pause instruction
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, GameWorld.Name + " (hover over map for more info)", new Vector2(DrawX + 500, DrawY), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, Month + "/" + Year, new Vector2(1900, 1160), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Pause Generation with ENTER", new Vector2(800 + 1300, 1200), Color.White);



                //trade lines

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
            else if (GameState == "choosepreferences")
            {
                DrawWorld();

                if (AlreadyTriedASearch)
                {
                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Could not find a character with your preferences.", new Vector2(DrawX + 500, 50), Color.White);
                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Change preferences or press C to continue generation.", new Vector2(DrawX + 500, 100), Color.White);
                }
                else
                {
                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Historical events exported to Desktop. Rename to save.):", new Vector2(DrawX + 500, 50), Color.White);
                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Choose your character preferences ([] denotes a keybind!):", new Vector2(DrawX + 500, 100), Color.White);
                }

                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "[1] Race: " + GameWorld.Races[CurrentlySelectingRace].Name, new Vector2(DrawX + 500, 150), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "[2] Sex: " + Sexes[CurrentlySelectingSex], new Vector2(DrawX + 500, 200), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Press ENTER to continue...):", new Vector2(DrawX + 500, 300), Color.White);

                _spriteBatch.DrawString(Engine.Render.BabyShibafont, GameWorld.Name + " (hover over map for more info)", new Vector2(DrawX + 500, DrawY), Color.White);
                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {

                        if (new Rectangle((10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0), 10 + z * TileZDistance, TileSize, TileSize).Contains(new Point(Mouse.GetState().X, Mouse.GetState().Y)))
                        {
                            if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation == null)
                            {
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "No Notable Location, " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome, new Vector2(DrawX, DrawY), Color.White);
                            }
                            else
                            {
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Name + ", " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome + " " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Type + ".", new Vector2(DrawX, DrawY), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Population: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.TruePopulation(), new Vector2(DrawX, DrawY + 30), Color.White);

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

                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Total Architects: " + ArchitectPopulation, new Vector2(DrawX, DrawY + 60), Color.White);

                                if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government == null)
                                {
                                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, "No Notable Government", new Vector2(DrawX, DrawY + 90), Color.White);
                                }
                                else
                                {
                                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Government: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government.Name, new Vector2(DrawX, DrawY + 90), Color.White);
                                }

                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Colonization Desire: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.ColonizationDesire, new Vector2(DrawX, DrawY + 120), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Wealth: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Wealth, new Vector2(DrawX, DrawY + 150), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Is Saving Up To Settle: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.IsSavingUpToSettle.ToString(), new Vector2(DrawX, DrawY + 180), Color.White);


                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Structures: " + structures, new Vector2(DrawX, DrawY + 210), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Distinct Groups: " + groups, new Vector2(DrawX, DrawY + 240), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Districts: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Districts.Count, new Vector2(DrawX, DrawY + 270), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Items: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.GeneralItemsWeHave.Count, new Vector2(DrawX, DrawY + 300), Color.White);
                            }
                        }
                    }
                }
            }
            else if (GameState == "choosefounderoptions")
            {
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Choose your civilization preferences ([] denotes a keybind!):", new Vector2(DrawX + 500, 100), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "[1] Race: " + GameWorld.Races[CurrentlySelectingRace], new Vector2(DrawX + 500, 150), Color.White);

                DrawWorld();

                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Press ENTER to continue...):", new Vector2(DrawX + 500, 300), Color.White);

                _spriteBatch.DrawString(Engine.Render.BabyShibafont, GameWorld.Name + " (hover over map for more info)", new Vector2(DrawX + 500, DrawY), Color.White);
                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {
                        if (new Rectangle((10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0), 10 + z * TileZDistance, TileSize, TileSize).Contains(new Point(Mouse.GetState().X, Mouse.GetState().Y)))
                        {
                            if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation == null)
                            {
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "No Notable Location, " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome, new Vector2(DrawX, DrawY), Color.White);
                            }
                            else
                            {
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Name + ", " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome + " " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Type + ".", new Vector2(DrawX, DrawY), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Population: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.TruePopulation(), new Vector2(DrawX, DrawY + 30), Color.White);

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

                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Total Architects: " + ArchitectPopulation, new Vector2(DrawX, DrawY + 60), Color.White);

                                if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government == null)
                                {
                                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, "No Notable Government", new Vector2(DrawX, DrawY + 90), Color.White);
                                }
                                else
                                {
                                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Government: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government.Name, new Vector2(DrawX, DrawY + 90), Color.White);
                                }

                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Colonization Desire: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.ColonizationDesire, new Vector2(DrawX, DrawY + 120), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Wealth: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Wealth, new Vector2(DrawX, DrawY + 150), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Is Saving Up To Settle: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.IsSavingUpToSettle.ToString(), new Vector2(DrawX, DrawY + 180), Color.White);


                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Structures: " + structures, new Vector2(DrawX, DrawY + 210), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Distinct Groups: " + groups, new Vector2(DrawX, DrawY + 240), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Districts: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Districts.Count, new Vector2(DrawX, DrawY + 270), Color.White);
                            }
                        }
                    }
                }
            }
            else if (GameState == "founder")
            {
                _spriteBatch.Draw(Engine.Render.GuideT, new Rectangle(0, 0, 192, 192), Color.White);

                // Render tiles and update CivilizationParticles
                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {
                        Rectangle drawRect = new Rectangle((10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0), 10 + z * TileZDistance, TileSize, TileSize);

                        if (MapCursorX == x && MapCursorZ == z)
                        {
                            _spriteBatch.Draw(Engine.Render.SpriteSheet.Texture, drawRect, Engine.Render.SpriteSheet.Get("tile-cursor"), Color.White);                         
           
                            //_spriteBatch.Draw(CursorT, new Rectangle((10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0), 10 + z * TileZDistance, TileSize, TileSize), Color.White);
                        }
                        else
                        {
                            // Draw the tile

                            _spriteBatch.Draw(Engine.Render.SpriteSheet.Texture, drawRect, Engine.Render.SpriteSheet.Get(string.Concat("tile-", GameWorld.WorldMap[x + z * GameWorld.Width].Biome)), new Color(100, 100, 100));

                            /*
                            _spriteBatch.Draw(TileAtlas[GameWorld.WorldMap[x + z * GameWorld.Width].Biome],
                                new Rectangle((10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0), 10 + z * TileZDistance, TileSize, TileSize),
                                new Color(100, 100, 100));
                            */

                            if (GameWorld.WorldMap[x + z * GameWorld.Width].Owner != null)
                            {
                                _spriteBatch.Draw(Engine.Render.SpriteSheet.Texture, drawRect, Engine.Render.SpriteSheet.Get("tile-outline"), ColorConverter[GameWorld.WorldMap[x + z * GameWorld.Width].Owner.Color]);
                                // Draw outline for owned tile
                                /*
                                _spriteBatch.Draw(OutlineT,
                                    new Rectangle((10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0), 10 + z * TileZDistance, TileSize, TileSize),
                                    ColorConverter[GameWorld.WorldMap[x + z * GameWorld.Width].Owner.Color]);
                                */

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
                            _spriteBatch.Draw(Engine.Render.SpriteSheet.Texture, drawRect, 
                                Engine.Render.SpriteSheet.Get(string.Concat("loc-", GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.PrimaryRace.Name, GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Type)),
                                Color.White);
                            
                            /*
                            _spriteBatch.Draw(TileAtlas[GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.PrimaryRace.Name + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Type],
                                new Rectangle((10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0), 10 + z * TileZDistance, TileSize, TileSize),
                                Color.White);
                            */
                        }

                    }
                }

                // Update CivilizationParticles
                for (int index = 0; index < CivilizationParticles.Count; index++)
                {
                    var i = CivilizationParticles[index];
                    _spriteBatch.Draw(Engine.Render.PixelTexture, new Rectangle(i.Item1, i.Item2, i.Item4, i.Item4), i.Item3);

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
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, GameWorld.Name + " (hover over map for more info)", new Vector2(DrawX + 500, DrawY), Color.White);

                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Week " + Math.Round((decimal)(GameWorld.Cycle / 120960)), new Vector2(1900, 1280), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Month " + Math.Round((decimal)(GameWorld.Cycle / 483840), 0, MidpointRounding.ToNegativeInfinity).ToString(), new Vector2(1900, 1240), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Year " + Math.Round((decimal)(Math.Round((decimal)(GameWorld.Cycle / 5806080), 0, MidpointRounding.ToNegativeInfinity)), 0, MidpointRounding.ToNegativeInfinity), new Vector2(1900, 1200), Color.White);


                _spriteBatch.DrawString(Engine.Render.Shibafont, GamePlayerCivilization.Name, new Vector2(1900, 200), ColorConverter[GamePlayerCivilization.Color]);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Cursor: RTDGCV", new Vector2(1900, 220), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Select: F", new Vector2(1900, 240), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "[S]tructures", new Vector2(1900, 260), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "[M]illitary", new Vector2(1900, 280), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "D[I]stricts", new Vector2(1900, 300), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Res[O]urces", new Vector2(1900, 320), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Di[P]lomacy", new Vector2(1900, 340), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Sett[L]ement", new Vector2(1900, 360), Color.White);

                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Wait and Build up Resources", new Vector2(1900, 420), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "[1] Wait 1 Month", new Vector2(1900, 440), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "[2] Wait 1 Year", new Vector2(1900, 460), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "[3] Wait 10 Years", new Vector2(1900, 480), Color.White);



                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {
                        if (MapCursorX == x && MapCursorZ == z)
                        {
                            if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation == null)
                            {
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "No Notable Location, " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome, new Vector2(DrawX, DrawY), Color.White);
                            }
                            else
                            {
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Name + ", " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome + " " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Type + ".", new Vector2(DrawX, DrawY), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Population: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.TruePopulation(), new Vector2(DrawX, DrawY + 30), Color.White);

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

                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Total Architects: " + ArchitectPopulation, new Vector2(DrawX, DrawY + 60), Color.White);

                                if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government == null)
                                {
                                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, "No Notable Government", new Vector2(DrawX, DrawY + 90), Color.White);
                                }
                                else
                                {
                                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Government: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government.Name, new Vector2(DrawX, DrawY + 90), Color.White);
                                }

                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Colonization Desire: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.ColonizationDesire, new Vector2(DrawX, DrawY + 120), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Wealth: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Wealth, new Vector2(DrawX, DrawY + 150), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Is Saving Up To Settle: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.IsSavingUpToSettle.ToString(), new Vector2(DrawX, DrawY + 180), Color.White);


                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Structures: " + structures, new Vector2(DrawX, DrawY + 210), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Distinct Groups: " + groups, new Vector2(DrawX, DrawY + 240), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Districts: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Districts.Count, new Vector2(DrawX, DrawY + 270), Color.White);
                            }
                        }
                    }
                }
            }
            else if (GameState == "findstartlocation")
            {

                DrawWorld();

                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {
                        if (new Rectangle((10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0), 10 + z * TileZDistance, TileSize, TileSize).Contains(new Point(Mouse.GetState().X, Mouse.GetState().Y)))
                        {
                            if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation == null)
                            {
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "No Notable Location, " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome, new Vector2(DrawX, DrawY), Color.White);
                            }
                            else
                            {
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Name + ", " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome + " " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Type + ".", new Vector2(DrawX, DrawY), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Population: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.TruePopulation(), new Vector2(DrawX, DrawY + 30), Color.White);

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

                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Total Architects: " + ArchitectPopulation, new Vector2(DrawX, DrawY + 60), Color.White);

                                if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government == null)
                                {
                                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, "No Notable Government", new Vector2(DrawX, DrawY + 90), Color.White);
                                }
                                else
                                {
                                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Government: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government.Name, new Vector2(DrawX, DrawY + 90), Color.White);
                                }

                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Colonization Desire: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.ColonizationDesire, new Vector2(DrawX, DrawY + 120), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Wealth: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Wealth, new Vector2(DrawX, DrawY + 150), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Is Saving Up To Settle: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.IsSavingUpToSettle.ToString(), new Vector2(DrawX, DrawY + 180), Color.White);


                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Structures: " + structures, new Vector2(DrawX, DrawY + 210), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Distinct Groups: " + groups, new Vector2(DrawX, DrawY + 240), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Districts: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Districts.Count, new Vector2(DrawX, DrawY + 270), Color.White);
                            }

                            if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation != null)
                            {

                                string tile = string.Concat("loc-", GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.PrimaryRace.Name, GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Type);
                                Rectangle drawRect = new Rectangle((10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0), 10 + z * TileZDistance, TileSize, TileSize);
                                _spriteBatch.Draw(Engine.Render.SpriteSheet.Texture, drawRect, Engine.Render.SpriteSheet.Get(tile), Color.White);


                                // _spriteBatch.Draw(TileAtlas[GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.PrimaryRace.Name + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Type], , Color.White);
                            }
                        }
                    }
                }
            }
            else if (GameState == "architectfound")
            {
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Architect Found!", new Vector2(1500, 100), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Name: " + TheChosenOne.Name, new Vector2(1500, 130), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, GameWorld.Name + " (hover over map for more info)", new Vector2(DrawX + 500, DrawY), Color.White);

                DrawWorld();

                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {

                        if (new Rectangle((10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0), 10 + z * TileZDistance, TileSize, TileSize).Contains(new Point(Mouse.GetState().X, Mouse.GetState().Y)))
                        {
                            if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation == null)
                            {
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "No Notable Location, " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome, new Vector2(DrawX, DrawY), Color.White);
                            }
                            else
                            {
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Name + ", " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome + " " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Type + ".", new Vector2(DrawX, DrawY), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Population: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.TruePopulation(), new Vector2(DrawX, DrawY + 30), Color.White);

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

                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Total Architects: " + ArchitectPopulation, new Vector2(DrawX, DrawY + 60), Color.White);

                                if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government == null)
                                {
                                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, "No Notable Government", new Vector2(DrawX, DrawY + 90), Color.White);
                                }
                                else
                                {
                                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Government: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government.Name, new Vector2(DrawX, DrawY + 90), Color.White);
                                }

                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Colonization Desire: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.ColonizationDesire, new Vector2(DrawX, DrawY + 120), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Wealth: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Wealth, new Vector2(DrawX, DrawY + 150), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Is Saving Up To Settle: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.IsSavingUpToSettle.ToString(), new Vector2(DrawX, DrawY + 180), Color.White);


                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Structures: " + structures, new Vector2(DrawX, DrawY + 210), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Distinct Groups: " + groups, new Vector2(DrawX, DrawY + 240), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Districts: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Districts.Count, new Vector2(DrawX, DrawY + 270), Color.White);
                            }
                        }
                    }
                }
            }
            else if (GameState == "partyturn" || GameState == "reaction" || GameState == "otherturn")
            {
                if (!InInventory && !Keyboard.GetState().IsKeyDown(Keys.Tab))
                {
                    _spriteBatch.Draw(Engine.Render.GUI, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), Color.White);

                    // Calculate the color intensity based on the current energy relative to MaxEnergy
                    // Using a linear interpolation method to ensure the transition is smooth
                    double energyPercentage = (double)MostRecentPartyTurnArchitect.Energy / MostRecentPartyTurnArchitect.MaxEnergy();
                    int colorIntensity = (int)Math.Round(energyPercentage * 255);

                    // Ensure the color intensity stays within the range [0, 255]
                    // Although Math.Clamp inherently ensures this, it's good to reinforce the valid range in the comment
                    colorIntensity = Math.Clamp(colorIntensity, 0, 255);

                    // Define the rectangle where the energy GUI will be drawn
                    Rectangle energyGuiRect = new Rectangle(600, 30, 200, 200);

                    // Use a gradient from red (low energy) to green (high energy) to represent energy level visually
                    // Red component decreases with energy increase, green component increases with energy increase
                    Color energyColor = new Color(255 - colorIntensity, colorIntensity, 0, 255); // No blue component

                    // Draw the energy GUI with the calculated color
                    _spriteBatch.Draw(Engine.Render.HealthGuiT, energyGuiRect, energyColor);


                    // Drawing droplets for each bleeding stack
                    int dropletSize = 24; // Size of each droplet
                    int startX = 700; // Center X position of the health GUI (600 + half of its width)
                    int startY = 230; // Starting Y position (30 + height of the health GUI + some padding)

                    for (int i = 0; i < MostRecentPartyTurnArchitect.Bleeding; i++)
                    {
                        // Calculate the Y position for the current droplet
                        int posY = startY + (dropletSize * i);

                        // Draw the droplet at the calculated position
                        _spriteBatch.Draw(Engine.Render.BleedT, new Rectangle(startX - (dropletSize / 2), posY, dropletSize, dropletSize), Color.White);
                    }

                    _spriteBatch.DrawString(Engine.Render.Shibafont, MostRecentPartyTurnArchitect.Name, new Vector2(50, 70), Color.White);
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "L: " + MostRecentPartyTurnArchitect.Location.Name, new Vector2(50, 90), Color.White);
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "D: " + MostRecentPartyTurnArchitect.District.Name, new Vector2(50, 110), Color.White);
                    if (MostRecentPartyTurnArchitect.Structure != null)
                    {
                        _spriteBatch.DrawString(Engine.Render.Shibafont, "S: " + MostRecentPartyTurnArchitect.Structure.Name, new Vector2(50, 130), Color.White);
                    }

                    if (MostRecentPartyTurnArchitect.LeftHandObject != null)
                    {
                        _spriteBatch.DrawString(Engine.Render.Shibafont, "LH: " + MostRecentPartyTurnArchitect.LeftHandObject.ReferredToNames[0], new Vector2(50, 150), Color.White);
                    }
                    else
                    {
                        _spriteBatch.DrawString(Engine.Render.Shibafont, "LH: (empty)", new Vector2(50, 150), Color.White);
                    }

                    _spriteBatch.DrawString(Engine.Render.Shibafont, "Speed: " + MostRecentPartyTurnArchitect.Speed(), new Vector2(400, 90), Color.White);
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "CD: " + MostRecentPartyTurnArchitect.CooldownCycles, new Vector2(400, 100), Color.White);

                    if (MostRecentPartyTurnArchitect.CombatCycles > 0)
                    {
                        _spriteBatch.DrawString(Engine.Render.Shibafont, "Evade: " + MostRecentPartyTurnArchitect.EscapeChance(), new Vector2(400, 110), Color.White);
                    }

                    if (MostRecentPartyTurnArchitect.RightHandObject != null)
                    {
                        _spriteBatch.DrawString(Engine.Render.Shibafont, "RH: " + MostRecentPartyTurnArchitect.RightHandObject.ReferredToNames[0], new Vector2(50, 170), Color.White);
                    }
                    else
                    {
                        _spriteBatch.DrawString(Engine.Render.Shibafont, "RH: (empty)", new Vector2(50, 170), Color.White);
                    }


                    if (MostRecentPartyTurnArchitect.CurrentlyMovingPlace == "none")
                    {
                        _spriteBatch.DrawString(Engine.Render.Shibafont, "You are not moving right now.", new Vector2(50, 1175), Color.White);
                    }
                    else if (KeyDirections.ContainsValue(MostRecentPartyTurnArchitect.CurrentlyMovingPlace))
                    {
                        _spriteBatch.DrawString(Engine.Render.Shibafont, "You are currently headed to the " + MostRecentPartyTurnArchitect.CurrentlyMovingPlace, new Vector2(50, 1175), Color.White);
                    }
                    else
                    {
                        _spriteBatch.DrawString(Engine.Render.Shibafont, "w h a t", new Vector2(50, 1175), Color.White);
                    }


                    int Line = 0;

                    _spriteBatch.DrawString(Engine.Render.Shibafont, "What do you do? \"I " + MostRecentPartyTurnArchitect.Prompt + "_\"", new Vector2(50, 1225), Color.White);

                    //district map
                    for
                    (int DistrictX = 0; DistrictX < 7; DistrictX++)
                    {
                        for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                        {
                            if (MostRecentPartyTurnArchitect.Block.X == DistrictX && MostRecentPartyTurnArchitect.Block.Z == DistrictZ)
                            {
                                _spriteBatch.Draw(Engine.Render.PixelTexture, new Rectangle(450 + DistrictX * 16, 1290 + DistrictZ * 16, 16, 16), Color.Red);
                            }
                            else
                            {
                                //Texture2D DecidedTexture = null;
                                Rectangle decidedRect = Rectangle.Empty;

                                if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures.Count == 0)
                                {
                                    if (GameWorld.WorldMap[MostRecentPartyTurnArchitect.Location.X + MostRecentPartyTurnArchitect.Location.Z * GameWorld.Width].Biome == "desert")
                                    {
                                        //DecidedTexture = DistrictEmptyDesertT;
                                        decidedRect = Engine.Render.SpriteSheet.Get("map-emptydesert");
                                    }
                                    else if (GameWorld.WorldMap[MostRecentPartyTurnArchitect.Location.X + MostRecentPartyTurnArchitect.Location.Z * GameWorld.Width].Biome == "taiga" || GameWorld.WorldMap[MostRecentPartyTurnArchitect.Location.X + MostRecentPartyTurnArchitect.Location.Z * GameWorld.Width].Biome == "mountain" || GameWorld.WorldMap[MostRecentPartyTurnArchitect.Location.X + MostRecentPartyTurnArchitect.Location.Z * GameWorld.Width].Biome == "forest" || GameWorld.WorldMap[MostRecentPartyTurnArchitect.Location.X + MostRecentPartyTurnArchitect.Location.Z * GameWorld.Width].Biome == "lightforest")
                                    {
                                        //DecidedTexture = DistrictEmptyTreesT;
                                        decidedRect = Engine.Render.SpriteSheet.Get("map-emptytrees");
                                    }
                                    else if (GameWorld.WorldMap[MostRecentPartyTurnArchitect.Location.X + MostRecentPartyTurnArchitect.Location.Z * GameWorld.Width].Biome == "tundra" || GameWorld.WorldMap[MostRecentPartyTurnArchitect.Location.X + MostRecentPartyTurnArchitect.Location.Z * GameWorld.Width].Biome == "snowpeak")
                                    {
                                        //DecidedTexture = DistrictEmptySnowT;
                                        decidedRect = Engine.Render.SpriteSheet.Get("map-emptysnow");
                                    }
                                    else if (GameWorld.WorldMap[MostRecentPartyTurnArchitect.Location.X + MostRecentPartyTurnArchitect.Location.Z * GameWorld.Width].Biome == "ocean")
                                    {
                                        //DecidedTexture = DistrictEmptyOceanT;
                                        decidedRect = Engine.Render.SpriteSheet.Get("map-ocean");
                                    }
                                    else if (GameWorld.WorldMap[MostRecentPartyTurnArchitect.Location.X + MostRecentPartyTurnArchitect.Location.Z * GameWorld.Width].Biome == "plains")
                                    {
                                        //DecidedTexture = DistrictEmptyPlainsT;
                                        decidedRect = Engine.Render.SpriteSheet.Get("map-emptyplains");
                                    }
                                }
                                else if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures.Count == 1)
                                {
                                    if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures[0].Type == "bighouse" || MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures[0].Type == "house")
                                    {
                                        //DecidedTexture = DistrictBuildingT;
                                        decidedRect = Engine.Render.SpriteSheet.Get("map-buildings");
                                    }
                                    else if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures[0].Type == "prism")
                                    {
                                        //DecidedTexture = DistrictPrismT;
                                        decidedRect = Engine.Render.SpriteSheet.Get("map-prism");
                                    }
                                    else if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures[0].Type == "spire")
                                    {
                                        //DecidedTexture = DistrictSpireT;
                                        decidedRect = Engine.Render.SpriteSheet.Get("map-spire");
                                    }
                                    else if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures[0].Type == "market")
                                    {
                                        //DecidedTexture = DistrictMarketT;
                                        decidedRect = Engine.Render.SpriteSheet.Get("map-market");
                                    }
                                    else
                                    {
                                        //DecidedTexture = DistrictSpecialBuildingT;
                                        decidedRect = Engine.Render.SpriteSheet.Get("map-specialbuilding");
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
                                        //DecidedTexture = DistrictPrismT;
                                        decidedRect = Engine.Render.SpriteSheet.Get("map-prism");
                                    }
                                    else if (FoundMarket && !FoundSpecial && !FoundHouse)
                                    {
                                        //DecidedTexture = DistrictMarketT;
                                        decidedRect = Engine.Render.SpriteSheet.Get("map-market");
                                    }
                                    else if (FoundMarket)
                                    {
                                        //DecidedTexture = DistrictMarketSurroundedT;
                                        decidedRect = Engine.Render.SpriteSheet.Get("map-marketsurround");
                                    }
                                    else if (FoundHouse && !FoundSpecial)
                                    {
                                        //DecidedTexture = DistrictManyBuildingsT;
                                        decidedRect = Engine.Render.SpriteSheet.Get("map-manybuildings");
                                    }
                                    else
                                    {
                                        //DecidedTexture = DistrictSpecialAndBuildingsT;
                                        decidedRect = Engine.Render.SpriteSheet.Get("map-specialandbuildings");
                                    }

                                }


                                Color BuildingColor = Color.White; //luminarch or other

                                if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures.Count == 0)
                                {

                                }
                                else if (MostRecentPartyTurnArchitect.Location.PrimaryRace.Name == "nightfell")
                                {
                                    BuildingColor = new Color(100, 100, 100);
                                }
                                else if (MostRecentPartyTurnArchitect.Location.PrimaryRace.Name == "archaix")
                                {
                                    BuildingColor = new Color(150, 150, 150);
                                }
                                else if (MostRecentPartyTurnArchitect.Location.PrimaryRace.Name == "isofractal")
                                {
                                    BuildingColor = new Color(50, 150, 255);
                                }
                                else if (MostRecentPartyTurnArchitect.Location.PrimaryRace.Name == "photonexus")
                                {
                                    BuildingColor = ColorConverter[MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures[0].FakePhotonexusColor];
                                }
                                else if (MostRecentPartyTurnArchitect.Location.PrimaryRace.Name == "shade")
                                {
                                    BuildingColor = new Color(0, 0, 0);

                                    if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Structures.Contains(MostRecentPartyTurnArchitect.Location.Prism))
                                    {
                                        //DecidedTexture = ShadeCoreT;
                                        decidedRect = Engine.Render.SpriteSheet.Get("loc-shadecore");
                                    }
                                    else
                                    {
                                        //DecidedTexture = ShadeOutpostT;
                                        decidedRect = Engine.Render.SpriteSheet.Get("loc-shadeoutpost");
                                    }
                                }


                                if (decidedRect != Rectangle.Empty)
                                {
                                    _spriteBatch.Draw(Engine.Render.SpriteSheet.Texture, new Rectangle(450 + DistrictX * 16, 1290 + DistrictZ * 16, 16, 16), decidedRect, BuildingColor);
                                }

                                /*
                                if(DecidedTexture != null)
                                {
                                    _spriteBatch.Draw(DecidedTexture, new Rectangle(450 + DistrictX * 16, 1290 + DistrictZ * 16, 16, 16), BuildingColor);
                                }
                                */

                                foreach (Object o in MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Objects)
                                {
                                    if (o.Type == "well")
                                    {
                                        
                                        _spriteBatch.Draw(Engine.Render.SpriteSheet.Texture, new Rectangle(450 + DistrictX * 16, 1290 + DistrictZ * 16, 16, 16), Engine.Render.SpriteSheet.Get("map-well"), Color.White);
                                        //_spriteBatch.Draw(DistrictWellT, new Rectangle(450 + DistrictX * 16, 1290 + DistrictZ * 16, 16, 16), Color.White);
                                    }
                                    else if (o.Type == "shadow storage")
                                    {
                                        _spriteBatch.Draw(Engine.Render.SpriteSheet.Texture, new Rectangle(450 + DistrictX * 16, 1290 + DistrictZ * 16, 16, 16), Engine.Render.SpriteSheet.Get("map-shadowstorage"), Color.White);
                                         //_spriteBatch.Draw(DistrictShadowStorageT, new Rectangle(450 + DistrictX * 16, 1290 + DistrictZ * 16, 16, 16), Color.White);
                                    }
                                }

                                if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Architects.Count > 0 && FlashTick > 50)
                                {
                                    Color HeavinessColor = Color.White;
                                    if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Architects.Count > 10)
                                    {
                                        HeavinessColor = Color.Blue;
                                    }
                                    else if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Architects.Count > 5)
                                    {
                                        HeavinessColor = Color.CornflowerBlue;
                                    }
                                    else if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Architects.Count > 2)
                                    {
                                        HeavinessColor = Color.LightBlue;
                                    }
                                    else if (MostRecentPartyTurnArchitect.District.DistrictMap[DistrictX + DistrictZ * 7].Architects.Count > 1)
                                    {
                                        HeavinessColor = Color.LightCyan;
                                    }
                                    _spriteBatch.Draw(Engine.Render.SpriteSheet.Texture, new Rectangle(450 + DistrictX * 16, 1290 + DistrictZ * 16, 16, 16), Engine.Render.SpriteSheet.Get("map-architecthere"), HeavinessColor);

                                    //_spriteBatch.Draw(ArchitectHere, new Rectangle(450 + DistrictX * 16, 1290 + DistrictZ * 16, 16, 16), HeavinessColor);
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

                        _spriteBatch.DrawString(Engine.Render.Shibafont, "Owes you: " + MostRecentPartyTurnArchitect.Structure.MarketDebt, new Vector2(220, 1350), c);
                    }

                    Line = 0;

                    int MaxLines = 26;  // Set the maximum number of lines you want to display
                    int MaxLength = 800; // Adjusted MaxLength for smaller font size

                    int screenHeight = _graphics.PreferredBackBufferHeight;
                    int lineHeight = 20; // Adjusted line height for smaller font size
                    int yPos = screenHeight - 400; // Initial Y position at the bottom

                    int totalLinesDisplayed = 0;  // Track the total number of lines displayed

                    List<TextStorage> reversedAnnouncements = new List<TextStorage>(Announcements);
                    reversedAnnouncements.Reverse();

                    foreach (TextStorage announcement in reversedAnnouncements)
                    {
                        List<string> lines = new List<string>();

                        if (Engine.Render.BabyShibafont.MeasureString(announcement.Data).X > MaxLength)
                        {
                            // Split announcement into lines if it exceeds MaxLength
                            string[] words = announcement.Data.Split(' ');
                            string currentLine = "";

                            foreach (var word in words)
                            {
                                if (Engine.Render.BabyShibafont.MeasureString(currentLine + word).X > MaxLength)
                                {
                                    lines.Add(currentLine);
                                    currentLine = word + " ";
                                }
                                else
                                {
                                    currentLine += word + " ";
                                }
                            }

                            lines.Add(currentLine);
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
                                DrawAnnouncementLine(lines[i], ref yPos, announcement.Color);
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

                    // Helper function to draw each announcement line
                    void DrawAnnouncementLine(string text, ref int yPosition, Color color)
                    {
                        _spriteBatch.DrawString(Engine.Render.BabyShibafont, text, new Vector2(50, yPosition), color);
                        yPosition -= lineHeight;
                    }


                    // Determine the source collection of objects based on the condition
                    var sourceObjects = MostRecentPartyTurnArchitect.Structure != null ? MostRecentPartyTurnArchitect.Room.Objects : MostRecentPartyTurnArchitect.Block.Objects;

                    // Condense and structure the list from the chosen collection
                    var structuredList = CondenseAndStructureList(sourceObjects);

                    // Draw the structured list
                    DrawList(_spriteBatch, structuredList, new Vector2(2150, 100), Engine.Render.BabyShibafont);

                    int Houses = 0;

                    Dictionary<string, int> GetUniqueArchitects(IEnumerable<Architect> architects)
                    {
                        var uniqueArchitects = new Dictionary<string, int>();
                        foreach (var architect in architects)
                        {
                            if (architect == MostRecentPartyTurnArchitect)
                            {
                                continue;
                            }
                            // Create a description for the architect
                            string description = $"{architect.Race.RaceLetter} {ConvertArchitectToDescription(architect)} CD:{architect.CooldownCycles} {architect.Energy} {architect.Bleeding} DIS: {architect.GetDistance(MostRecentPartyTurnArchitect)}";
                            if (uniqueArchitects.ContainsKey(description))
                            {
                                uniqueArchitects[description]++;
                            }
                            else
                            {
                                uniqueArchitects.Add(description, 1);
                            }
                        }
                        return uniqueArchitects;
                    }
                    void DrawArchitects(SpriteBatch spriteBatch, Dictionary<string, int> uniqueArchitects, Vector2 startCoords, SpriteFont font, int indentSize)
                    {
                        int line = 0;
                        foreach (var architect in uniqueArchitects)
                        {
                            string textToDraw = $"{architect.Key} x{architect.Value}";
                            Vector2 indentCoords = new Vector2(startCoords.X, startCoords.Y + line * 20);
                            spriteBatch.DrawString(font, textToDraw, indentCoords, Color.White);
                            line++;
                        }
                    }

                    //draw architects and buildings

                    if (MostRecentPartyTurnArchitect.Structure != null)
                    {
                        _spriteBatch.Draw(Engine.Render.PixelTexture, new Rectangle(1650, 0, 444, 1920), Color.Black);

                        // Assuming you have a method to get a dictionary of unique architects
                        var uniqueArchitects = GetUniqueArchitects(MostRecentPartyTurnArchitect.Room.Architects);
                        DrawArchitects(_spriteBatch, uniqueArchitects, new Vector2(950, 100), Engine.Render.Shibafont, 20);
                    }
                    else
                    {

                        // Assuming you have a method to get a dictionary of unique architects
                        var uniqueArchitects = GetUniqueArchitects(MostRecentPartyTurnArchitect.Block.Architects);
                        DrawArchitects(_spriteBatch, uniqueArchitects, new Vector2(950, 100), Engine.Render.Shibafont, 20);

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
                                _spriteBatch.DrawString(Engine.Render.Shibafont, "(" + s.Type.Substring(0, 1).ToUpper() + ") " + s.Name, new Vector2(1700, Line * 30 + 200), Color.White);
                            }
                        }
                    }

                    if (Houses == 1)
                    {
                        _spriteBatch.DrawString(Engine.Render.Shibafont, "1 house (house 1)", new Vector2(1700, 180), Color.White);
                    }
                    else if (Houses > 1)
                    {
                        _spriteBatch.DrawString(Engine.Render.Shibafont, Houses + " houses (house 1-" + Houses + ")", new Vector2(1700, 180), Color.White);
                    }


                    if (IsInGui)
                    {
                        _spriteBatch.Draw(Engine.Render.ReactionGUIT, new Rectangle((_graphics.PreferredBackBufferWidth - 1919) / 2, (_graphics.PreferredBackBufferHeight - 1080) / 2, 1919, 1080), Color.White);
                        int Linee = 0;
                        foreach (string s in ItemPickupGuiLines)
                        {
                            DrawCenteredText(_spriteBatch, s, Linee * 20 + (_graphics.PreferredBackBufferHeight / 2) + 100, Engine.Render.Shibafont, Color.White);
                            Linee++;
                        }
                    }

                    if (GameState == "reaction")
                    {
                        _spriteBatch.Draw(Engine.Render.ReactionGUIT, new Rectangle((_graphics.PreferredBackBufferWidth - 1919) / 2, (_graphics.PreferredBackBufferHeight - 1080) / 2, 1919, 1080), Color.White);

                        // Calculate the success chances for the MostRecentPartyTurnArchitect
                        var successChances = MostRecentPartyTurnArchitect.CalculateSuccessChances(StoredAttacks[0], GameWorld.ReactionModifierInt, StoredAttacks[0].Attacker, StoredAttacks[0].Attacker.GetProficiency(StoredAttacks[0].Weapon.DamageType));

                        int y = 600;
                        int d = 30;
                        DrawCenteredText(_spriteBatch, StoredAttacks[0].Attacker.Name + " is aiming a " + StoredAttacks[0].Verb, y, Engine.Render.Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "at you with " + StoredAttacks[0].Weapon.ReferredToNames[0], y + d, Engine.Render.Shibafont, Color.White);

                        DrawCenteredText(_spriteBatch, "[S] Sustain (" + successChances.sustain + "% evs.)", y + d * 2, Engine.Render.Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "[D] Duck (" + successChances.duck + "% evs.)", y + d * 3, Engine.Render.Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "[J] Jump (" + successChances.jump + "% evs.)", y + d * 4, Engine.Render.Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "[R] Roll (" + successChances.roll + "% evs.)", y + d * 5, Engine.Render.Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "[N] Disarm (" + successChances.disarm + "% evs.)", y + d * 6, Engine.Render.Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "[C] Redirect (" + successChances.redirect + "% evs.)", y + d * 7, Engine.Render.Shibafont, Color.White);
                        if ((MostRecentPartyTurnArchitect.LeftHandObject != null && MostRecentPartyTurnArchitect.LeftHandObject.IsWeapon) || (MostRecentPartyTurnArchitect.RightHandObject != null && MostRecentPartyTurnArchitect.RightHandObject.IsWeapon))
                        {
                            DrawCenteredText(_spriteBatch, "[P] Parry the Attack (requires weapon) (" + successChances.parry + "%)", y + d * 8, Engine.Render.Shibafont, Color.White);
                        }
                        if ((MostRecentPartyTurnArchitect.RightHandObject != null && MostRecentPartyTurnArchitect.RightHandObject.Type == "shield") || (MostRecentPartyTurnArchitect.LeftHandObject != null && MostRecentPartyTurnArchitect.LeftHandObject.Type == "shield"))
                        {
                            DrawCenteredText(_spriteBatch, "[B] Block the Attack (requires shield) (" + successChances.block + "%)", y + d * 9, Engine.Render.Shibafont, Color.White);
                        }

                        DrawCenteredText(_spriteBatch, "Chances calculated with Agility, items carried/Endurance,", y + d * 10, Engine.Render.Shibafont, Color.White);
                        DrawCenteredText(_spriteBatch, "your/opponent skills, attack type, etc.", y + d * 11, Engine.Render.Shibafont, Color.White);
                    }


                    Vector2 CalculateSunMoonPosition(float angle, float radius, Vector2 centerPosition, bool isSun)
                    {
                        // Calculate the position on the circle for the sun or moon
                        float x = centerPosition.X + radius * (float)Math.Cos(angle);
                        float y = centerPosition.Y + radius * (float)Math.Sin(angle);

                        // If it's the sun, we use the angle directly. For the moon, we add 180 degrees (Math.PI radians) to place it on the opposite side
                        if (!isSun)
                        {
                            x = centerPosition.X - radius * (float)Math.Cos(angle);
                            y = centerPosition.Y - radius * (float)Math.Sin(angle);
                        }

                        return new Vector2(x, y);
                    }

                    // Calculate current time
                    double currentCycle = GameWorld.Cycle;
                    double cycleDurationInSeconds = 0.1;
                    int totalSeconds = (int)Math.Round(currentCycle * cycleDurationInSeconds);
                    int hours = (totalSeconds / 3600) % 24;
                    int minutes = (totalSeconds % 3600) / 60;

                    float CalculateSkyBrightness(int hours, int minutes)
                    {
                        float brightness;
                        int timeInMinutes = hours * 60 + minutes;

                        // Convert times to minutes for easier comparison
                        int sixAM = 6 * 60, nineAM = 9 * 60, sixPM = 18 * 60, eightPM = 20 * 60, fourAM = 4 * 60;

                        if (timeInMinutes >= nineAM && timeInMinutes <= sixPM)
                        {
                            // Full brightness from 9:00 AM to 6:00 PM
                            brightness = 1f;
                        }
                        else if (timeInMinutes > sixPM && timeInMinutes <= eightPM)
                        {
                            // Decrease brightness from 6:00 PM to 8:00 PM
                            // Calculate linear interpolation from 1 to 0.25 over the range
                            float factor = (float)(timeInMinutes - sixPM) / (eightPM - sixPM);
                            brightness = 1f - (0.75f * factor);
                        }
                        else if (timeInMinutes > eightPM || timeInMinutes < fourAM)
                        {
                            // Nighttime brightness should be at 25%
                            brightness = 0.25f;
                        }
                        else if (timeInMinutes >= fourAM && timeInMinutes < sixAM)
                        {
                            // Increase brightness from 4:00 AM to 6:00 AM
                            // Calculate linear interpolation from 0.25 to 1 over the range
                            float factor = (float)(timeInMinutes - fourAM) / (sixAM - fourAM);
                            brightness = 0.25f + (0.75f * factor);
                        }
                        else // For the time between 6:00 AM and 9:00 AM
                        {
                            // Morning time, full brightness
                            brightness = 1f;
                        }

                        return brightness;
                    }

                    float CalculateSunMoonRotation(int hours, int minutes)
                    {
                        // Convert hours and minutes to angle for rotation
                        // Noon = 0 degrees, Sunset = 90 degrees clockwise, Midnight = 180 degrees, etc.
                        float timeInDegrees = ((hours + minutes / 60f) / 24f) * 360f;
                        float rotation = MathHelper.ToRadians(timeInDegrees - 90); // Adjust so that noon is 0 degrees rotation
                        return rotation;
                    }

                    // SkyT Brightness Adjustment
                    float skyBrightness = CalculateSkyBrightness(hours, minutes);

                    // Calculate angle for sun/moon position
                    float sunMoonAngle = CalculateSunMoonRotation(hours, minutes);

                    // Center position for the circular path and radius
                    Vector2 centerPosition = new Vector2(20, _graphics.PreferredBackBufferHeight - 128); // Adjusted for bottom left
                    float radius = 100; // Arbitrary radius for the circular path of sun and moon

                    // Calculate positions
                    Vector2 sunPosition = CalculateSunMoonPosition(sunMoonAngle, radius, centerPosition, true);
                    Vector2 moonPosition = CalculateSunMoonPosition(sunMoonAngle, radius, centerPosition, false);

                    // Draw SkyT with adjusted brightness
                    _spriteBatch.Draw(Engine.Render.SkyT, centerPosition, null, new Color(skyBrightness, skyBrightness, skyBrightness), 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);

                    // Draw Sun and Moon without rotation, on calculated positions
                    _spriteBatch.Draw(Engine.Render.SunT, sunPosition, null, Color.White, 0f, new Vector2(Engine.Render.SunT.Width / 2, Engine.Render.SunT.Height / 2), 0.5f, SpriteEffects.None, 0f);
                    _spriteBatch.Draw(Engine.Render.MoonT, moonPosition, null, Color.White, 0f, new Vector2(Engine.Render.MoonT.Width / 2, Engine.Render.MoonT.Height / 2), 0.5f, SpriteEffects.None, 0f);

                    // Draw ClockT statically
                    _spriteBatch.Draw(Engine.Render.ClockT, centerPosition, null, Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);

                }
                else
                {
                    _spriteBatch.Draw(Engine.Render.InventoryGUI, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), Color.White);
                    if (MostRecentPartyTurnArchitect.CurrentlyMovingPlace == "none")
                    {
                        _spriteBatch.DrawString(Engine.Render.Shibafont, "You are not moving right now.", new Vector2(50, 1175), Color.White);
                    }
                    else if (KeyDirections.ContainsValue(MostRecentPartyTurnArchitect.CurrentlyMovingPlace))
                    {
                        _spriteBatch.DrawString(Engine.Render.Shibafont, "You are currently headed to the " + MostRecentPartyTurnArchitect.CurrentlyMovingPlace, new Vector2(50, 1175), Color.White);
                    }
                    else
                    {
                        _spriteBatch.DrawString(Engine.Render.Shibafont, "w h a t", new Vector2(50, 1175), Color.White);
                    }

                    _spriteBatch.DrawString(Engine.Render.Shibafont, "What do you do? \"I " + MostRecentPartyTurnArchitect.Prompt + "_\"", new Vector2(50, 1225), Color.White);

                    int line = 0;

                    void DrawTextAtPosition(SpriteBatch spriteBatch, string text, Vector2 position, SpriteFont font)
                    {
                        spriteBatch.DrawString(font, text, position, Color.White);
                    }
                    // Assuming MostRecentPartyTurnArchitect.Inventory is the collection of inventory objects
                    var inventoryObjects = MostRecentPartyTurnArchitect.Inventory;
                    var structuredInventoryList = CondenseAndStructureList(inventoryObjects);

                    DrawInventoryList(_spriteBatch, structuredInventoryList, new Vector2(2150, 100), Engine.Render.BabyShibafont);

                    // New function for drawing the inventory list with structured and indented display
                    void DrawInventoryList(SpriteBatch spriteBatch, List<(string Description, int Count, int IndentationLevel)> list, Vector2 startCoords, SpriteFont font)
                    {
                        int Line = 0;
                        foreach (var (Description, Count, IndentationLevel) in list)
                        {
                            string textToDraw = $"{new string(' ', 4 * IndentationLevel)}{Description} x{Count}";
                            DrawTextAtPosition(spriteBatch, textToDraw, new Vector2(startCoords.X, startCoords.Y + line * 20), font);
                            Line++;
                        }
                    }




                    line = 0;
                    foreach (Object o in MostRecentPartyTurnArchitect.Clothing)
                    {
                        DrawCenteredTextAtPosition(_spriteBatch, o.ReferredToNames[0], 1650, 600 + 10 * line, Engine.Render.BabyShibafont);

                        line++;
                    }

                    line = 0;

                    foreach (Imbuement i in MostRecentPartyTurnArchitect.CurrentlyActiveImbuements)
                    {
                        line++;
                        DrawCenteredTextAtPosition(_spriteBatch, i.GetDescription(), 1050, 600 + 10 * line, Engine.Render.BabyShibafont);
                    }

                    line = 0;

                    foreach ((string, int) p in MostRecentPartyTurnArchitect.XPValues)
                    {
                        if (p.Item2 != 0)
                        {
                            line++;

                            DrawCenteredTextAtPosition(_spriteBatch, p.Item1 + ": " + ConvertNumberToProficiency(MostRecentPartyTurnArchitect.GetProficiency(p.Item1)), 280, 120 + 10 * line, Engine.Render.BabyShibafont);
                        }
                    }
                    if (line == 0)
                    {
                        DrawCenteredTextAtPosition(_spriteBatch, "No proficiencies. Go learn something.", 280, 120 + 10 * line, Engine.Render.BabyShibafont);
                    }

                    //level up screen
                    void DrawPathLevel(SpriteBatch spriteBatch, SpriteFont font, string pathName, int pathLevel, int Y, Color color)
                    {
                        string text = pathLevel > 0 ? $"{pathName} Level {pathLevel}" : $"{pathName} (not activated)";
                        DrawCenteredText(_spriteBatch, text, Y, font, pathLevel > 0 ? color : new Color(40, 40, 40));
                    }

                    if (MostRecentPartyTurnArchitect.SpendableLevels > 0)
                    {
                        int newWidth = 1919 + 200; // Increase width by 200
                        int newHeight = 1080 + 200; // Increase height by 200

                        _spriteBatch.Draw(
                            Engine.Render.ReactionGUIT,
                            new Rectangle(
                                (_graphics.PreferredBackBufferWidth - newWidth) / 2, // Centered X
                                (_graphics.PreferredBackBufferHeight - newHeight) / 2, // Centered Y
                                newWidth,
                                newHeight),
                            Color.White);

                        int boxX = (_graphics.PreferredBackBufferWidth - newWidth) / 2;
                        int boxY = (_graphics.PreferredBackBufferHeight - newHeight) / 2;

                        void DrawPathLevels(Party GamePlayerParty, SpriteBatch spriteBatch, SpriteFont font)
                        {
                            int position = 500;
                            int spacing = 20;

                            // Each path's drawing code:
                            DrawPathLevel(spriteBatch, font, "[X] Path of Shadow", MostRecentPartyTurnArchitect.PathOfShadowLevel, position, Color.MidnightBlue);
                            position += spacing;

                            DrawPathLevel(spriteBatch, font, "[L] Path of Life", MostRecentPartyTurnArchitect.PathOfLifeLevel, position, Color.ForestGreen);
                            position += spacing;

                            DrawPathLevel(spriteBatch, font, "[D] Path of Death", MostRecentPartyTurnArchitect.PathOfDeathLevel, position, Color.DarkRed);
                            position += spacing;

                            DrawPathLevel(spriteBatch, font, "[T] Path of Time", MostRecentPartyTurnArchitect.PathOfTimeLevel, position, Color.SkyBlue);
                            position += spacing;

                            DrawPathLevel(spriteBatch, font, "[A] Path of Stars", MostRecentPartyTurnArchitect.PathOfStarsLevel, position, Color.Gold);
                            position += spacing;

                            DrawPathLevel(spriteBatch, font, "[H] Path of Heat", MostRecentPartyTurnArchitect.PathOfHeatLevel, position, Color.OrangeRed);
                            position += spacing;

                            DrawPathLevel(spriteBatch, font, "[I] Path of Illusions", MostRecentPartyTurnArchitect.PathOfIllusionsLevel, position, Color.Magenta);
                            position += spacing;

                            DrawPathLevel(spriteBatch, font, "[E] Path of Ethereality", MostRecentPartyTurnArchitect.PathOfEtherealityLevel, position, Color.LightBlue);
                            position += spacing;

                            DrawPathLevel(spriteBatch, font, "[V] Path of Void", MostRecentPartyTurnArchitect.PathOfVoidLevel, position, Color.DarkSlateBlue);
                            position += spacing;

                            DrawPathLevel(spriteBatch, font, "[S] Path of Storms", MostRecentPartyTurnArchitect.PathOfStormsLevel, position, Color.Cyan);
                            position += spacing;

                            DrawPathLevel(spriteBatch, font, "[F] Path of Forge", MostRecentPartyTurnArchitect.PathOfForgeLevel, position, Color.DarkOrange);
                            position += spacing;

                            DrawPathLevel(spriteBatch, font, "[K] Path of Lore", MostRecentPartyTurnArchitect.PathOfLoreLevel, position, Color.SeaGreen);
                            position += spacing;

                            DrawPathLevel(spriteBatch, font, "[M] Path of Mind", MostRecentPartyTurnArchitect.PathOfMindLevel, position, Color.LightSeaGreen);
                            position += spacing;

                            DrawPathLevel(spriteBatch, font, "[U] Path of Soul", MostRecentPartyTurnArchitect.PathOfSoulLevel, position, Color.MediumPurple);
                            position += spacing;

                            DrawPathLevel(spriteBatch, font, "[B] Path of Body", MostRecentPartyTurnArchitect.PathOfBodyLevel, position, Color.SandyBrown);
                            position += spacing;

                            DrawPathLevel(spriteBatch, font, "[P] Path of Space", MostRecentPartyTurnArchitect.PathOfSpaceLevel, position, Color.DarkOrchid);
                            position += spacing;

                            DrawPathLevel(spriteBatch, font, "[R] Path of Reality", MostRecentPartyTurnArchitect.PathOfRealityLevel, position, Color.IndianRed);
                            position += spacing;

                            DrawPathLevel(spriteBatch, font, "[G] Path of Light", MostRecentPartyTurnArchitect.PathOfLightLevel, position, Color.Yellow);
                        }

                        DrawPathLevels(GamePlayerParty, _spriteBatch, Engine.Render.BabyShibafont);

                        if (!Keyboard.GetState().IsKeyDown(Keys.LeftControl))
                        {
                            if (Keyboard.GetState().IsKeyDown(Keys.X))
                            {
                                _spriteBatch.Draw(
                                    Engine.Render.ReactionGUIT,
                                    new Rectangle(
                                        (_graphics.PreferredBackBufferWidth - newWidth) / 2, // Centered X
                                        (_graphics.PreferredBackBufferHeight - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                DrawCenteredText(_spriteBatch, "PATH OF SHADOW", 500, Engine.Render.BabyShibafont, Color.MidnightBlue);
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 AGL ", 520, Engine.Render.BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 1) ? Color.MidnightBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 2: Become harder to see and target.", 540, Engine.Render.BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 2) ? Color.MidnightBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 AGL ", 560, Engine.Render.BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 3) ? Color.MidnightBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 4: Become invisible at the cost of energy with \"become one with shadow\"", 580, Engine.Render.BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 4) ? Color.MidnightBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 AGL ", 600, Engine.Render.BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 5) ? Color.MidnightBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 6: Your possessions become invisible with you.", 620, Engine.Render.BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 6) ? Color.MidnightBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 AGL ", 640, Engine.Render.BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 7) ? Color.MidnightBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 8:  Invisibilty no longer causes energy loss.", 660, Engine.Render.BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 8) ? Color.MidnightBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 9: +1 AGL ", 680, Engine.Render.BabyShibafont, (MostRecentPartyTurnArchitect.PathOfShadowLevel >= 9) ? Color.MidnightBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "PRESS CTRL X TO LEVEL UP THIS PATH.", 700, Engine.Render.BabyShibafont, Color.White);
                            }

                            if (Keyboard.GetState().IsKeyDown(Keys.L)) // Assuming 'L' is the key for Path of Life
                            {
                                _spriteBatch.Draw(Engine.Render.ReactionGUIT, new Rectangle((_graphics.PreferredBackBufferWidth - newWidth) / 2, (_graphics.PreferredBackBufferHeight - newHeight) / 2, newWidth, newHeight), Color.White);

                                int position = 500;
                                int spacing = 20;

                                // Title
                                DrawCenteredText(_spriteBatch, "PATH OF LIFE", position, Engine.Render.BabyShibafont, Color.ForestGreen);
                                position += spacing;

                                // Display abilities with conditional coloring based on the level
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 CHA", position, Engine.Render.BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 1) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;

                                DrawCenteredText(_spriteBatch, "LVL 2: Unlock communication with extra races.", position, Engine.Render.BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 2) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;

                                DrawCenteredText(_spriteBatch, "LVL 3: +1 CHA", position, Engine.Render.BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 3) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;

                                DrawCenteredText(_spriteBatch, "LVL 4: Full communication with all animals.", position, Engine.Render.BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 4) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 CHA", position, Engine.Render.BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 5) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 6: Pacify/Tame animals, add to party. Max of Path LVL animals.", position, Engine.Render.BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 6) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 CHA", position, Engine.Render.BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 7) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;

                                DrawCenteredText(_spriteBatch, "LVL 8: Buff/augment your animals with \"augument\".", position, Engine.Render.BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 8) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;

                                DrawCenteredText(_spriteBatch, "LVL 9: +1 CHA", position, Engine.Render.BabyShibafont, (MostRecentPartyTurnArchitect.PathOfLifeLevel >= 9) ? Color.ForestGreen : new Color(40, 40, 40));
                                position += spacing;

                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "PRESS CTRL + L TO LEVEL UP THIS PATH.", position, Engine.Render.BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.D))
                            {
                                _spriteBatch.Draw(
                                    Engine.Render.ReactionGUIT,
                                    new Rectangle(
                                        (_graphics.PreferredBackBufferWidth - newWidth) / 2, // Centered X
                                        (_graphics.PreferredBackBufferHeight - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                // Title
                                DrawCenteredText(_spriteBatch, "PATH OF DEATH", 500, Engine.Render.BabyShibafont, Color.DarkRed);
                                // Abilities
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 FOC", 520, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 1 ? Color.DarkRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 2: Raise ((LVL/2) rounded down) weakened undead servants.", 540, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 2 ? Color.DarkRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 FOC", 560, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 3 ? Color.DarkRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 4: Fire spectral bolts.", 580, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 4 ? Color.DarkRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 FOC", 600, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 5 ? Color.DarkRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 6: Foes slain by bolts may become undead.", 620, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 6 ? Color.DarkRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 FOC", 640, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 7 ? Color.DarkRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 8: Return to life with 20 energy once a week.", 660, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfDeathLevel >= 8 ? Color.DarkRed : new Color(40, 40, 40));
                                // Leveling up instruction
                                DrawCenteredText(_spriteBatch, "PRESS CTRL + D TO LEVEL UP THIS PATH.", 680, Engine.Render.BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.T))
                            {
                                _spriteBatch.Draw(
                                    Engine.Render.ReactionGUIT,
                                    new Rectangle(
                                        (_graphics.PreferredBackBufferWidth - newWidth) / 2, // Centered X
                                        (_graphics.PreferredBackBufferHeight - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                // Title
                                DrawCenteredText(_spriteBatch, "PATH OF TIME", 500, Engine.Render.BabyShibafont, Color.SkyBlue);
                                // Abilities
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 FOC", 520, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfTimeLevel >= 1 ? Color.SkyBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 2: Gain some control over your timeline.", 540, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfTimeLevel >= 2 ? Color.SkyBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 FOC", 560, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfTimeLevel >= 3 ? Color.SkyBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 4: Reverse a cycle and its events once per day.", 580, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfTimeLevel >= 4 ? Color.SkyBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 FOC", 600, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfTimeLevel >= 5 ? Color.SkyBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 6: Accelerate your timeline briefly.", 620, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfTimeLevel >= 6 ? Color.SkyBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 FOC", 640, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfTimeLevel >= 7 ? Color.SkyBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 8: Freeze everyone’s timeline but your own.", 660, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfTimeLevel >= 8 ? Color.SkyBlue : new Color(40, 40, 40));
                                // Leveling up instruction
                                DrawCenteredText(_spriteBatch, "PRESS CTRL + T TO LEVEL UP THIS PATH.", 680, Engine.Render.BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.A)) // Assuming 'A' is the key for Path of Stars
                            {
                                _spriteBatch.Draw(
                                    Engine.Render.ReactionGUIT,
                                    new Rectangle(
                                        (_graphics.PreferredBackBufferWidth - newWidth) / 2, // Centered X
                                        (_graphics.PreferredBackBufferHeight - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                // Title
                                DrawCenteredText(_spriteBatch, "PATH OF STARS", 500, Engine.Render.BabyShibafont, Color.Gold);
                                // Abilities
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 CRE", 520, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 1 ? Color.Gold : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 2: Summon a falling star on strike.", 540, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 2 ? Color.Gold : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 CRE", 560, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 3 ? Color.Gold : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 4: Starmarked creatures ignite.", 580, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 4 ? Color.Gold : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 CRE", 600, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 5 ? Color.Gold : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 6: Fire stars from your hands with \"starstrike ~\".", 620, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 6 ? Color.Gold : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 CRE", 640, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 7 ? Color.Gold : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 8: Open a portal to a star.", 660, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfStarsLevel >= 8 ? Color.Gold : new Color(40, 40, 40));
                                // Leveling up instruction
                                DrawCenteredText(_spriteBatch, "PRESS CTRL + A TO LEVEL UP THIS PATH.", 680, Engine.Render.BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.H)) // Assuming 'H' is the key for Path of Heat
                            {
                                _spriteBatch.Draw(
                                    Engine.Render.ReactionGUIT,
                                    new Rectangle(
                                        (_graphics.PreferredBackBufferWidth - newWidth) / 2, // Centered X
                                        (_graphics.PreferredBackBufferHeight - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                // Title
                                DrawCenteredText(_spriteBatch, "PATH OF HEAT", 500, Engine.Render.BabyShibafont, Color.OrangeRed);
                                // Abilities
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 END", 520, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 1 ? Color.OrangeRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 2: Conjure and throw waves of heat.", 540, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 2 ? Color.OrangeRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 END", 560, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 3 ? Color.OrangeRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 4: Control heat of objects you touch.", 580, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 4 ? Color.OrangeRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 END", 600, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 5 ? Color.OrangeRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 6: Conjure larger waves of heat.", 620, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 6 ? Color.OrangeRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 END", 640, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 7 ? Color.OrangeRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 8: Set self on fire at will.", 660, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfHeatLevel >= 8 ? Color.OrangeRed : new Color(40, 40, 40));
                                // Leveling up instruction
                                DrawCenteredText(_spriteBatch, "PRESS CTRL + H TO LEVEL UP THIS PATH.", 680, Engine.Render.BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.I)) // For Path of Illusions
                            {
                                _spriteBatch.Draw(
                                    Engine.Render.ReactionGUIT,
                                    new Rectangle(
                                        (_graphics.PreferredBackBufferWidth - newWidth) / 2, // Centered X
                                        (_graphics.PreferredBackBufferHeight - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF ILLUSIONS", 500, Engine.Render.BabyShibafont, Color.Magenta);
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 CHA", 520, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfIllusionsLevel >= 1 ? Color.Magenta : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 2: Summon an incorporeal immobile duplicate of yourself or an object.", 540, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfIllusionsLevel >= 2 ? Color.Magenta : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 CHA", 560, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfIllusionsLevel >= 3 ? Color.Magenta : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 4: Summon a duplicate of an animate object. Your duplicates move on their own", 580, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfIllusionsLevel >= 4 ? Color.Magenta : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 CHA", 600, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfIllusionsLevel >= 5 ? Color.Magenta : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 6: Control all clones you create.", 620, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfIllusionsLevel >= 6 ? Color.Magenta : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 CHA", 640, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfIllusionsLevel >= 7 ? Color.Magenta : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 8: Switch places with a duplicate of yourself at will.", 660, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfIllusionsLevel >= 8 ? Color.Magenta : new Color(40, 40, 40));
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD I FOR PATH DETAILS. CTRL+I TO LEVEL.", 680, Engine.Render.BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.E)) // For Path of Ethereality
                            {
                                _spriteBatch.Draw(
                                    Engine.Render.ReactionGUIT,
                                    new Rectangle(
                                        (_graphics.PreferredBackBufferWidth - newWidth) / 2, // Centered X
                                        (_graphics.PreferredBackBufferHeight - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF ETHEREALITY", 500, Engine.Render.BabyShibafont, Color.LightBlue);
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 DEX", 520, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfEtherealityLevel >= 1 ? Color.LightBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 2: Take less damage, generally.", 540, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfEtherealityLevel >= 2 ? Color.LightBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 DEX", 560, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfEtherealityLevel >= 3 ? Color.LightBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 4: Enter the ethereal plane briefly.", 580, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfEtherealityLevel >= 4 ? Color.LightBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 DEX", 600, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfEtherealityLevel >= 5 ? Color.LightBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 6: Send objects to the ethereal plane.", 620, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfEtherealityLevel >= 6 ? Color.LightBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 DEX", 640, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfEtherealityLevel >= 7 ? Color.LightBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 8: Instantaneous travel anywhere.", 660, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfEtherealityLevel >= 8 ? Color.LightBlue : new Color(40, 40, 40));
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD E FOR PATH DETAILS. CTRL+E TO LEVEL.", 680, Engine.Render.BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.V)) // For Path of Void
                            {
                                _spriteBatch.Draw(
                                    Engine.Render.ReactionGUIT,
                                    new Rectangle(
                                        (_graphics.PreferredBackBufferWidth - newWidth) / 2, // Centered X
                                        (_graphics.PreferredBackBufferHeight - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF VOID", 500, Engine.Render.BabyShibafont, Color.DarkSlateBlue);
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 CRE", 520, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfVoidLevel >= 1 ? Color.DarkSlateBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 2: Create voids that you can store items for later usage.", 540, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfVoidLevel >= 2 ? Color.DarkSlateBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 CRE", 560, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfVoidLevel >= 3 ? Color.DarkSlateBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 4: Fire matter projectiles from voids.", 580, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfVoidLevel >= 4 ? Color.DarkSlateBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 CRE", 600, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfVoidLevel >= 5 ? Color.DarkSlateBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 6: Compel creatures into voids.", 620, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfVoidLevel >= 6 ? Color.DarkSlateBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 CRE", 640, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfVoidLevel >= 7 ? Color.DarkSlateBlue : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 8: Voids last forever and are interconnected.", 660, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfVoidLevel >= 8 ? Color.DarkSlateBlue : new Color(40, 40, 40));
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD V FOR PATH DETAILS. CTRL+V TO LEVEL.", 680, Engine.Render.BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.S)) // For Path of Storms
                            {
                                _spriteBatch.Draw(
                                    Engine.Render.ReactionGUIT,
                                    new Rectangle(
                                        (_graphics.PreferredBackBufferWidth - newWidth) / 2, // Centered X
                                        (_graphics.PreferredBackBufferHeight - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF STORMS", 500, Engine.Render.BabyShibafont, Color.Cyan);
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 STR", 520, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfStormsLevel >= 1 ? Color.Cyan : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 2: Energy strike on foes.", 540, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfStormsLevel >= 2 ? Color.Cyan : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 STR", 560, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfStormsLevel >= 3 ? Color.Cyan : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 4: Flow with uncontrollable energy.", 580, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfStormsLevel >= 4 ? Color.Cyan : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 STR", 600, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfStormsLevel >= 5 ? Color.Cyan : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 6: Direct energy into objects.", 620, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfStormsLevel >= 6 ? Color.Cyan : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 STR", 640, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfStormsLevel >= 7 ? Color.Cyan : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 8: Gain flight and powerful energy manipulation.", 660, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfStormsLevel >= 8 ? Color.Cyan : new Color(40, 40, 40));
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD S FOR PATH DETAILS. CTRL+S TO LEVEL.", 680, Engine.Render.BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.F)) // For Path of Forge
                            {
                                _spriteBatch.Draw(
                                    Engine.Render.ReactionGUIT,
                                    new Rectangle(
                                        (_graphics.PreferredBackBufferWidth - newWidth) / 2, // Centered X
                                        (_graphics.PreferredBackBufferHeight - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF FORGE", 500, Engine.Render.BabyShibafont, Color.DarkOrange);
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 DEX", 520, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfForgeLevel >= 1 ? Color.DarkOrange : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 2: Craft any weapon at a forge with the right materials.", 540, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfForgeLevel >= 2 ? Color.DarkOrange : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 DEX", 560, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfForgeLevel >= 3 ? Color.DarkOrange : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 4: Weapons you make have an extra imbuement.", 580, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfForgeLevel >= 4 ? Color.DarkOrange : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 DEX", 600, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfForgeLevel >= 5 ? Color.DarkOrange : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 6: Your imbuements have more effectiveness.", 620, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfForgeLevel >= 6 ? Color.DarkOrange : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 DEX", 640, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfForgeLevel >= 7 ? Color.DarkOrange : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 8: Touch objects to give them three extra imbuements.", 660, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfForgeLevel >= 8 ? Color.DarkOrange : new Color(40, 40, 40));
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD F FOR PATH DETAILS. CTRL+F TO LEVEL.", 680, Engine.Render.BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.K)) // For Path of Lore
                            {
                                _spriteBatch.Draw(
                                    Engine.Render.ReactionGUIT,
                                    new Rectangle(
                                        (_graphics.PreferredBackBufferWidth - newWidth) / 2, // Centered X
                                        (_graphics.PreferredBackBufferHeight - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF LORE", 500, Engine.Render.BabyShibafont, Color.SeaGreen);
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 CRE", 520, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfLoreLevel >= 1 ? Color.SeaGreen : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 2: Access lore from other lorepathers, containing secrets.", 540, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfLoreLevel >= 2 ? Color.SeaGreen : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 CRE", 560, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfLoreLevel >= 3 ? Color.SeaGreen : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 4: Trade knowledge with people mentally.", 580, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfLoreLevel >= 4 ? Color.SeaGreen : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 CRE", 600, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfLoreLevel >= 5 ? Color.SeaGreen : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 6: Increase max path level by 4.", 620, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfLoreLevel >= 6 ? Color.SeaGreen : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 CRE", 640, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfLoreLevel >= 7 ? Color.SeaGreen : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 8: Access a wellspring of history at will.", 660, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfLoreLevel >= 8 ? Color.SeaGreen : new Color(40, 40, 40));
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD K FOR PATH DETAILS. CTRL+K TO LEVEL.", 680, Engine.Render.BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.M)) // For Path of Mind
                            {
                                _spriteBatch.Draw(
                                    Engine.Render.ReactionGUIT,
                                    new Rectangle(
                                        (_graphics.PreferredBackBufferWidth - newWidth) / 2, // Centered X
                                        (_graphics.PreferredBackBufferHeight - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF MIND", 500, Engine.Render.BabyShibafont, Color.LightSeaGreen);
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 FOC", 520, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfMindLevel >= 1 ? Color.LightSeaGreen : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 2: Enhanced magical power.", 540, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfMindLevel >= 2 ? Color.LightSeaGreen : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 FOC", 560, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfMindLevel >= 3 ? Color.LightSeaGreen : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 4: Decreased magical energy usage.", 580, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfMindLevel >= 4 ? Color.LightSeaGreen : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 FOC", 600, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfMindLevel >= 5 ? Color.LightSeaGreen : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 6: Option to double spell effects.", 620, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfMindLevel >= 6 ? Color.LightSeaGreen : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 FOC", 640, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfMindLevel >= 7 ? Color.LightSeaGreen : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 8: Triple Spell effects and reduced energy usage.", 660, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfMindLevel >= 8 ? Color.LightSeaGreen : new Color(40, 40, 40));
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD M FOR PATH DETAILS. CTRL+M TO LEVEL.", 680, Engine.Render.BabyShibafont, Color.White);
                            }
                            else if (Keyboard.GetState().IsKeyDown(Keys.U)) // For Path of Soul
                            {
                                _spriteBatch.Draw(
                                    Engine.Render.ReactionGUIT,
                                    new Rectangle(
                                        (_graphics.PreferredBackBufferWidth - newWidth) / 2, // Centered X
                                        (_graphics.PreferredBackBufferHeight - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF SOUL", 500, Engine.Render.BabyShibafont, Color.MediumPurple);
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 END", 520, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfSoulLevel >= 1 ? Color.MediumPurple : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 2: Increases to all nonphysical stats.", 540, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfSoulLevel >= 2 ? Color.MediumPurple : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 END", 560, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfSoulLevel >= 3 ? Color.MediumPurple : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 4: Greatly increased energy generation.", 580, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfSoulLevel >= 4 ? Color.MediumPurple : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 END", 600, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfSoulLevel >= 5 ? Color.MediumPurple : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 6: Exit your body, moving through walls.", 620, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfSoulLevel >= 6 ? Color.MediumPurple : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 END", 640, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfSoulLevel >= 7 ? Color.MediumPurple : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 8: Possess a new vessel if you die.", 660, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfSoulLevel >= 8 ? Color.MediumPurple : new Color(40, 40, 40));
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD U FOR PATH DETAILS. CTRL+U TO LEVEL.", 680, Engine.Render.BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.B)) // For Path of Body
                            {
                                _spriteBatch.Draw(
                                    Engine.Render.ReactionGUIT,
                                    new Rectangle(
                                        (_graphics.PreferredBackBufferWidth - newWidth) / 2, // Centered X
                                        (_graphics.PreferredBackBufferHeight - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF BODY", 500, Engine.Render.BabyShibafont, Color.SandyBrown);
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 STR", 520, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 1 ? Color.SandyBrown : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 2: Increases to all physical stats.", 540, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 2 ? Color.SandyBrown : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 STR", 560, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 3 ? Color.SandyBrown : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 4: Greatly increased unarmed melee capabilities.", 580, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 4 ? Color.SandyBrown : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 STR", 600, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 5 ? Color.SandyBrown : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 6: Unarmed strikes channel radiant energy.", 620, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 6 ? Color.SandyBrown : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 STR", 640, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 7 ? Color.SandyBrown : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 8: Move your body in any physically imaginable way.", 660, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfBodyLevel >= 8 ? Color.SandyBrown : new Color(40, 40, 40));
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD B FOR PATH DETAILS. CTRL+B TO LEVEL.", 680, Engine.Render.BabyShibafont, Color.White);
                            }

                            else if (Keyboard.GetState().IsKeyDown(Keys.P)) // For Path of Space
                            {
                                _spriteBatch.Draw(
                                    Engine.Render.ReactionGUIT,
                                    new Rectangle(
                                        (_graphics.PreferredBackBufferWidth - newWidth) / 2, // Centered X
                                        (_graphics.PreferredBackBufferHeight - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF SPACE", 500, Engine.Render.BabyShibafont, Color.DarkOrchid);
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 STR", 520, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfSpaceLevel >= 1 ? Color.DarkOrchid : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 2: Open a portal for travel.", 540, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfSpaceLevel >= 2 ? Color.DarkOrchid : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 STR", 560, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfSpaceLevel >= 3 ? Color.DarkOrchid : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 4: Telekinesis for small objects.", 580, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfSpaceLevel >= 4 ? Color.DarkOrchid : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 STR", 600, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfSpaceLevel >= 5 ? Color.DarkOrchid : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 6: Telekinesis for heavier objects.", 620, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfSpaceLevel >= 6 ? Color.DarkOrchid : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 STR", 640, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfSpaceLevel >= 7 ? Color.DarkOrchid : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 8: Telekinesis without limits, including self for flight.", 660, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfSpaceLevel >= 8 ? Color.DarkOrchid : new Color(40, 40, 40));
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD P FOR PATH DETAILS. CTRL+P TO LEVEL.", 680, Engine.Render.BabyShibafont, Color.White);
                            }


                            else if (Keyboard.GetState().IsKeyDown(Keys.R)) // For Path of Reality
                            {
                                _spriteBatch.Draw(
                                    Engine.Render.ReactionGUIT,
                                    new Rectangle(
                                        (_graphics.PreferredBackBufferWidth - newWidth) / 2, // Centered X
                                        (_graphics.PreferredBackBufferHeight - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF REALITY", 500, Engine.Render.BabyShibafont, Color.IndianRed);
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 DEX", 520, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 1 ? Color.IndianRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 2: Alter object properties.", 540, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 2 ? Color.IndianRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 DEX", 560, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 3 ? Color.IndianRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 4: Change state of matter by touch.", 580, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 4 ? Color.IndianRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 DEX", 600, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 5 ? Color.IndianRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 6: Duplicate objects.", 620, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 6 ? Color.IndianRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 DEX", 640, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 7 ? Color.IndianRed : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 8: Remove objects from reality.", 660, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfRealityLevel >= 8 ? Color.IndianRed : new Color(40, 40, 40));
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD R FOR PATH DETAILS. CTRL+R TO LEVEL.", 680, Engine.Render.BabyShibafont, Color.White);
                            }
                            else if (Keyboard.GetState().IsKeyDown(Keys.G)) // For Path of Light
                            {
                                _spriteBatch.Draw(
                                    Engine.Render.ReactionGUIT,
                                    new Rectangle(
                                        (_graphics.PreferredBackBufferWidth - newWidth) / 2, // Centered X
                                        (_graphics.PreferredBackBufferHeight - newHeight) / 2, // Centered Y
                                        newWidth,
                                        newHeight),
                                    Color.White);

                                // Title and Abilities
                                DrawCenteredText(_spriteBatch, "PATH OF LIGHT", 500, Engine.Render.BabyShibafont, Color.Yellow);
                                DrawCenteredText(_spriteBatch, "LVL 1: +1 AGL", 520, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 1 ? Color.Yellow : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 2: Conjure photons to create a spark.", 540, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 2 ? Color.Yellow : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 3: +1 AGL", 560, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 3 ? Color.Yellow : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 4: Sparks fire radiant beams at enemies.", 580, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 4 ? Color.Yellow : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 5: +1 AGL", 600, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 5 ? Color.Yellow : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 6: Use sparks to heal nearby creatures.", 620, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 6 ? Color.Yellow : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 7: +1 AGL", 640, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 7 ? Color.Yellow : new Color(40, 40, 40));
                                DrawCenteredText(_spriteBatch, "LVL 8: Create a Photonexus, loyal to you.", 660, Engine.Render.BabyShibafont, MostRecentPartyTurnArchitect.PathOfLightLevel >= 8 ? Color.Yellow : new Color(40, 40, 40));
                                // Instruction for leveling up
                                DrawCenteredText(_spriteBatch, "HOLD L FOR PATH DETAILS. CTRL+L TO LEVEL.", 680, Engine.Render.BabyShibafont, Color.White);
                            }
                        }
                    }
                }
            }
            else if (GameState == "dead")
            {
                int MaxLines = 26;  // Set the maximum number of lines you want to display
                int MaxLength = 500;

                int screenHeight = _graphics.PreferredBackBufferHeight;
                int lineHeight = 25;
                int yPos = screenHeight - 400; // Initial Y position at the bottom

                int totalLinesDisplayed = 0;  // Track the total number of lines displayed

                DrawCenteredText(_spriteBatch, "All members of your party have perished. You have lost influence in the world.", 400, Engine.Render.Shibafont, Color.White);
                DrawCenteredText(_spriteBatch, "Press SPACE to return to the title screen.", 450, Engine.Render.Shibafont, Color.White);

            }
            else if (GameState == "travelmenu")
            {
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, MapCursorX.ToString(), new Vector2(2000, 10), Color.White);
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, MapCursorZ.ToString(), new Vector2(2000, 20), Color.White);


                _spriteBatch.Draw(Engine.Render.GuideT, new Rectangle(0, 0, 192, 192), Color.White);

                DrawWorld();

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

                            // Assuming ArchitectHereTexture is a Texture2D and its position needs calculation
                            Vector2 position = new Vector2(x, z); // Placeholder for actual position calculation

                            if (drawColor != Color.Gray)
                            {
                                _spriteBatch.Draw(Engine.Render.ArchitectHere, new Rectangle((10 + x * TileXDistance) + ((z % 2 == 1) ? TileXDistance / 2 : 0), 10 + z * TileZDistance, TileSize, TileSize), drawColor);
                            }
                        }
                    }
                }

                for (int x = 0; x < GameWorld.Width; x++)
                {
                    for (int z = 0; z < GameWorld.Length; z++)
                    {

                        if (x == MapCursorX && z == MapCursorZ)
                        {
                            if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation == null)
                            {
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "No Notable Location, " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome, new Vector2(DrawX + 500, DrawY), Color.White);
                            }
                            else
                            {
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Name + ", " + GameWorld.WorldMap[x + z * GameWorld.Width].Biome + " " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Type + ".", new Vector2(DrawX + 500, DrawY), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Population: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.TruePopulation(), new Vector2(DrawX + 500, DrawY + 30), Color.White);

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
                                            _spriteBatch.DrawString(Engine.Render.Shibafont, ">" + d.Name + " (" + d.Industry.Substring(0, 4) + ".)", new Vector2(DrawX + 900, 1100 + DistrictLine * 20), Color.White);
                                        }
                                        else
                                        {
                                            _spriteBatch.DrawString(Engine.Render.Shibafont, ">" + d.Name + " (" + d.Industry + ")", new Vector2(DrawX + 900, 1100 + DistrictLine * 20), Color.White);
                                        }
                                    }
                                    else
                                    {
                                        if(d.Industry.Length > 4)
                                        {
                                            _spriteBatch.DrawString(Engine.Render.Shibafont, " " + d.Name + " (" + d.Industry.Substring(0, 4) + ".)", new Vector2(DrawX + 900, 1100 + DistrictLine * 20), Color.White);
                                        }
                                        else
                                        {
                                            _spriteBatch.DrawString(Engine.Render.Shibafont, " " + d.Name + " (" + d.Industry + ")", new Vector2(DrawX + 900, 1100 + DistrictLine * 20), Color.White);
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

                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Total Architects: " + ArchitectPopulation, new Vector2(DrawX + 500, DrawY + 60), Color.White);

                                if (GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government == null)
                                {
                                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, "No Notable Government", new Vector2(DrawX + 500, DrawY + 90), Color.White);
                                }
                                else
                                {
                                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Government: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Government.Name, new Vector2(DrawX + 500, DrawY + 90), Color.White);
                                }

                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Colonization Desire: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.ColonizationDesire, new Vector2(DrawX + 500, DrawY + 120), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Wealth: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Wealth, new Vector2(DrawX + 500, DrawY + 150), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Is Saving Up To Settle: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.IsSavingUpToSettle.ToString(), new Vector2(DrawX + 500, DrawY + 180), Color.White);

                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Structures: " + structures, new Vector2(DrawX + 500, DrawY + 210), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Distinct Groups: " + groups, new Vector2(DrawX + 500, DrawY + 240), Color.White);
                                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Districts: " + GameWorld.WorldMap[x + z * GameWorld.Width].MyLocation.Districts.Count, new Vector2(DrawX + 500, DrawY + 270), Color.White);
                            }
                        }
                    }
                }

                int Line = 0;

                _spriteBatch.DrawString(Engine.Render.Shibafont, "Press SPACE to continue...", new Vector2(50, _graphics.PreferredBackBufferHeight - GameWorld.Width), Color.White);

                foreach (TextStorage t in Exposition)
                {
                    _spriteBatch.DrawString(Engine.Render.BabyShibafont, t.Data, new Vector2(DrawX + 500, (DrawY + Line*15)-400), t.Color);
                    Line++;
                }
            }
            else if (GameState == "triggerrupture")
            {
                DrawWorld();
                _spriteBatch.DrawString(Engine.Render.BabyShibafont, "Use RTDGCV and Enter to position your focus.", new Vector2(DrawX + 500, DrawY + 60), Color.White);
            }
            else if (GameState == "gatheringandcrafting")
            {
                if (GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "forest" || GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "lightforest" || GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "taiga")
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "[1] Harvest Wood", new Vector2(100, 100), Color.LimeGreen);
                }
                else
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "[1] Harvest Wood (invalid biome)", new Vector2(100, 100), Color.Red);
                }

                if (GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "mountain" || GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "snowpeak")
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "[2] Harvest Stone", new Vector2(100, 150), Color.LimeGreen);
                }
                else
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "[2] Harvest Stone (invalid biome)", new Vector2(100, 150), Color.Red);
                }

                if (GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "snowpeak")
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "[3] Harvest Metal", new Vector2(100, 200), Color.LimeGreen);
                }
                else
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "[3] Harvest Metal (invalid biome)", new Vector2(100, 200), Color.Red);
                }

                if (GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "desert")
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "[4] Harvest Sand", new Vector2(100, 250), Color.LimeGreen);
                }
                else
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "[4] Harvest Sand (invalid biome)", new Vector2(100, 250), Color.Red);
                }

                if (GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "plains")
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "[5] Harvest Fiber", new Vector2(100, 300), Color.LimeGreen);
                }
                else
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "[5] Harvest Fiber (invalid biome)", new Vector2(100, 300), Color.Red);
                }

                if (GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "tundra" || GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "taiga" || GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "mountain" || GameWorld.WorldMap[MapCursorX + MapCursorZ * GameWorld.Length].Biome == "snowpeak")
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "[6] Harvest Ice", new Vector2(100, 350), Color.LimeGreen);
                }
                else
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, "[6] Harvest Ice (invalid biome)", new Vector2(100, 350), Color.Red);
                }

                _spriteBatch.DrawString(Engine.Render.Shibafont, "[X] Leave", new Vector2(100, 700), Color.Orange);

                int MaxLinesExposition = 15;  // Set the maximum number of lines to display

                int LineExposition = 0;

                // Reverse the Exposition list in place and take the first 15 elements
                List<TextStorage> reversedExposition = new List<TextStorage>(Exposition);
                reversedExposition.Reverse();

                foreach (TextStorage t in reversedExposition.Take(MaxLinesExposition))
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, t.Data, new Vector2(1000, 50 + LineExposition * 25), t.Color);

                    LineExposition++;
                }
            }
            else if (GameState == "exposition")
            {
                int Line = 0;


                foreach (TextStorage t in Exposition)
                {
                    _spriteBatch.DrawString(Engine.Render.Shibafont, t.Data, new Vector2(50, 50 + Line * 25), t.Color);

                    Line++;
                }
            }
            if (Engine.Input.IsKeyDown(Keys.Escape))
            {
                _spriteBatch.DrawString(Engine.Render.Shibafont, "Quitting... (" + Math.Round((decimal)((100 - EscapeTicks) / 10)) + ")", new Vector2(10, 10), Color.White);
                _spriteBatch.DrawString(Engine.Render.Shibafont, "Press CTRL-S to save your game.", new Vector2(10, 60), Color.White);
            }

            Engine.Draw(_spriteBatch, gameTime);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}