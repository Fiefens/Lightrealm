using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Lightrealm
{
    public class CommandProcessor
    {
        public static T EntityGet<T>(int entityId) where T : Entity
        {
            if (Game1.GameWorld == null || Game1.GameWorld.EntityLedger == null)
            {
                return (T)Convert.ChangeType(Game1.TemporaryEntityLedger[entityId], typeof(T));
            }

            return (T)Convert.ChangeType(Game1.GameWorld.EntityLedger[entityId], typeof(T));
        }

        public static void MakeObservation(string data, Color color, EntityList<Entity> entities)
        {
            string capitalizedData = Game1.Capitalize(data);
            Game1.Observations.Add(new TextStorage(capitalizedData, color, entities));
            Game1.Announcements.Add(new TextStorage(capitalizedData, color, entities));
        }

        public static void AddMessage(string data, Color color, EntityList<Entity> entities)
        {
            string capitalizedData = Game1.Capitalize(data);
            Game1.Messages.Add(new TextStorage(capitalizedData, color, entities));
            Game1.Announcements.Add(new TextStorage(capitalizedData, color, entities));
        }

        public static bool CanUnderstandEachOther(Entity sender, Entity receiver)
        {
            // Ensure the receiver is an Architect and assign it to recArch
            if (!(receiver is Architect recArch) || !(sender is Architect senderArch))
            {
                return false; // They cannot understand each other
            }

            // If either party has a Path of Life Level of 4 or greater
            if (senderArch.PathOfLifeLevel >= 4 || recArch.PathOfLifeLevel >= 4)
            {
                return true; // They can automatically understand each other
            }

            // Check if both are either ExtraRaces or HumanoidRaces
            bool senderIsValidRace = Game1.GameWorld.HumanoidRaces.Contains(senderArch.Race) || Game1.GameWorld.ExtraRaces.Contains(senderArch.Race);
            bool receiverIsValidRace = Game1.GameWorld.HumanoidRaces.Contains(recArch.Race) || Game1.GameWorld.ExtraRaces.Contains(recArch.Race);

            if (senderIsValidRace && receiverIsValidRace)
            {
                return true; // They can understand each other
            }

            // If none of the above conditions are met
            return false;
        }


        public static bool RunCommand(Architect Executor, string CommandID, List<Entity> Subjects, List<Architect> LoadedArchitects, World GameWorld, Random r, string OriginalCommand)
        {
            //replace inside command pronouns
            int Month = ((int)Math.Round((decimal)(GameWorld.Cycle / 24192000)) % 12) + 1;
            int Year = (int)Math.Round((decimal)(GameWorld.Cycle / 290304000), MidpointRounding.ToZero);


            


            string Date = "(" + Month + "/" + Year + ")";

            bool CanMessageSubject = Subjects.Count() > 0 && (Subjects[0] is Architect) && (GameWorld.HumanoidRaces.Contains(((Architect)Subjects[0]).Race) || (GameWorld.ExtraRaces.Contains(((Architect)Subjects[0]).Race) && Executor.PathOfLifeLevel >= 2) || Executor.PathOfLifeLevel >= 4);

            if (Subjects == null)
            {
                Subjects = new List<Entity>();
            }

            EntityList<Architect> ArchitectsToUse;

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
                { "wait", 1 },
                { "wait a second", 1 },
                { "wait one second", 1 },
                { "wait two seconds", 2 },
                { "wait three seconds", 3 },
                { "wait four seconds", 4 },
                { "wait five seconds", 5 },
                { "wait six seconds", 6 },
                { "wait seven seconds", 7 },
                { "wait eight seconds", 8 },
                { "wait nine seconds", 9 },
                { "wait ten seconds", 10 },
                { "wait twenty seconds", 20 },
                { "wait thirty seconds", 30 },
                { "wait forty seconds", 40 },
                { "wait fifty seconds", 50 },
                { "wait a minute", 60 },
                { "wait one minute", 60 },
                { "wait two minutes", 120 },
                { "wait three minutes", 180 },
                { "wait four minutes", 240 },
                { "wait five minutes", 300 }
            };

            if (CommandID != null && Game1.RecognizedMessages.ContainsKey(CommandID))
            {
                Architect Reciever = (Architect)Subjects[0];

                Subjects.RemoveAt(0);

                SendMessage(CommandID, Executor, Reciever, new EntityList<Entity>(Subjects), GameWorld);

            }
            else if (CommandID == "leave_structure")
            {
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed()));

                Executor.HideValue = 0;

                if (Executor.Structure == null)
                {
                    MakeObservation("You are not in a structure.", Color.Yellow, new EntityList<Entity>());
                }
                else if (Executor.Room != Executor.Structure.Rooms[0] && !(Executor.Location.Layout == "archway" && Executor.Room == Executor.Structure.Rooms.Last()))
                {
                    MakeObservation("There is not a way to exit through (Try entering doors your character remembers, marked by [<]).", Color.Yellow, new EntityList<Entity>());
                }
                else
                {
                    if (r.Next(100) <= Executor.EscapeChance() || Executor.CombatCycles == 0)
                    {
                        Executor.Room.Architects.Remove(Executor);
                        Executor.Structure = null;
                        Executor.Room = null;
                        Executor.Block.Architects.Add(Executor);

                        foreach (Architect a in Executor.Block.Architects)
                        {
                            a.Historical = true;
                            Game1.GameWorld.AllHistoricalArchitects.Add(a);
                        }

                        Game1.SwitchState("otherturn", false);
                    }
                    else
                    {
                        MakeObservation("You struggle to escape, and fail!", Color.OrangeRed, new EntityList<Entity>());
                        Executor.CooldownCycles += (int)Math.Round(25 / Executor.Speed());
                    }
                }
            }
            else if (CommandID == "enter")
            {
                Executor.HideValue = 0;
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed()));

                if (Executor.OnTopOfStructure != null)
                {
                    MakeObservation("You need to get down from " + Executor.OnTopOfStructure + ", first.", Color.Yellow, new EntityList<Entity>());
                }
                else if (Subjects[0].Reinforced)
                {
                    MakeObservation("The door won't budge. You might need to bash it down.", Color.Yellow, new EntityList<Entity>());
                }
                else if (LoadedArchitects[Game1.ArchitectIndex].Structure != null && Subjects[0] is Door && (Executor.Room != null ? Executor.Room.Objects : Executor.Block.Objects).Contains(Subjects[0]))
                {
                    if (r.Next(100) <= Executor.EscapeChance() || Executor.CombatCycles == 0)
                    {
                        Executor.Room.Architects.Remove(Executor);
                        Executor.Room = ((Door)Subjects[0]).DestinationRoom;
                        Executor.Room.Architects.Add(Executor);

                        foreach (Architect a in Executor.Room.Architects)
                        {
                            a.Historical = true;
                            Game1.GameWorld.AllHistoricalArchitects.Add(a);
                        }

                        Executor.CooldownCycles += (int)(Math.Round(25 / Executor.Speed()));
                    }
                    else
                    {
                        MakeObservation("You struggle to escape, and fail!", Color.OrangeRed, new EntityList<Entity>());
                        Executor.CooldownCycles += (int)Math.Round(25 / Executor.Speed());
                    }
                }
                else if (LoadedArchitects[Game1.ArchitectIndex].Structure == null && Subjects[0] is Structure)
                {
                    if (r.Next(100) <= Executor.EscapeChance() || Executor.CombatCycles == 0)
                    {
                        Executor.Structure = (Structure)Subjects[0];
                        Executor.Room = ((Structure)Subjects[0]).Rooms[0];
                        Executor.Block.Architects.Remove(Executor);
                        Executor.Structure.Rooms[0].Architects.Add(Executor);
                        Executor.CooldownCycles += (int)(Math.Round(25 / Executor.Speed()));


                        foreach (Architect a in Executor.Room.Architects)
                        {
                            a.Historical = true;
                            Game1.GameWorld.AllHistoricalArchitects.Add(a);
                        }


                        Game1.Exposition.Add(new TextStorage(Executor.Name + " enters " + ((Structure)Subjects[0]).Name + ", a " + ((Structure)Subjects[0]).Type + ".", Color.LightBlue, new EntityList<Entity>() { }));

                        if (((Structure)Subjects[0]).PrimarySmells.Count() > 0)
                        {
                            Game1.Exposition.Add(new TextStorage("The fresh scent of " + ((Structure)Subjects[0]).PrimarySmells[0] + " fills the area.", Color.Yellow, new EntityList<Entity>() { }));
                        }
                        if (((Structure)Subjects[0]).Type == "shrine" && ((Structure)Subjects[0]).Rooms.Any(room => room.Objects.Any(obj => obj.Type == "altar")))
                        {
                            Game1.Exposition.Add(new TextStorage("An altar lies in the grand hall of this shrine. Perhaps you could offer it something?", Color.Yellow, new EntityList<Entity>() { }));
                        }

                        Game1.SwitchState("exposition", false);
                    }
                    else
                    {
                        MakeObservation("You struggle to escape, and fail!", Color.OrangeRed, new EntityList<Entity>());
                        Executor.CooldownCycles += (int)Math.Round(25 / Executor.Speed());
                    }
                }
                else if (Subjects[0] is Object obj && obj.Type == "exit door" && Executor.Room != null)
                {
                    if (r.Next(100) <= Executor.EscapeChance() || Executor.CombatCycles == 0)
                    {
                        Executor.CooldownCycles += (int)(Math.Round(25 / Executor.Speed()));
                        Executor.Room.Architects.Remove(Executor);
                        Executor.Structure = null;
                        Executor.Room = null;
                        Executor.Block.Architects.Add(Executor);

                        foreach (Architect a in Executor.Block.Architects)
                        {
                            a.Historical = true;
                            Game1.GameWorld.AllHistoricalArchitects.Add(a);
                        }

                        Game1.Exposition.Add(new TextStorage(Executor.Name + " exits through the " + obj.Type + ".", Color.Blue, new EntityList<Entity>() { }));
                        Game1.GameState = "exposition";
                    }
                    else
                    {
                        MakeObservation("You struggle to escape, and fail!", Color.OrangeRed, new EntityList<Entity>());
                        Executor.CooldownCycles += (int)Math.Round(25 / Executor.Speed());
                    }
                }
                else
                {
                    MakeObservation("The specified subject does not lead you anywhere.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "go_prone" && (Subjects.Count() == 0 || Subjects[0].Metadata == "down"))
            {
                MakeObservation("You get on the ground.", Color.Orange, new EntityList<Entity>());
                Executor.OnGround = true;
            }
            else if (CommandID == "stand_up" && (Subjects.Count() == 0 || Subjects[0].Metadata == "up"))
            {
                MakeObservation("You stand up.", Color.Green, new EntityList<Entity>());
                Executor.CooldownCycles += (int)Math.Round((20 - Executor.Agility) * Executor.Speed());
                Executor.OnGround = false;
            }
            else if (CommandID == "move_direction")
            {
                if (!Game1.TriedFakeMove)
                {
                    MakeObservation("Some commands have shortcuts. For instance, directional movement can be initiated with the NUMPAD, the Click GUI by the district map, or by pressing Ctrl + QWEADZXC.", Color.Lime, new EntityList<Entity>());
                    Game1.TriedFakeMove = true;
                }

                if(Executor.YLevelInFeet > 0)
                {
                    MakeObservation("You need to be on the ground to move between blocks.", Color.Yellow, new EntityList<Entity>());
                }
                else
                {
                    Executor.Move(Subjects[0].Metadata);
                }
            }
            
            else if (CommandID == "attack_specific_body_part")
            {
                Object Weapon;

                // Check the player's main hand based on their handedness
                Object mainHand = LoadedArchitects[Game1.ArchitectIndex].MainHeldObject;
                Object offHand = LoadedArchitects[Game1.ArchitectIndex].OffHeldObject;

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
                    Weapon = LoadedArchitects[Game1.ArchitectIndex].BodyParts[r.Next(LoadedArchitects[Game1.ArchitectIndex].BodyParts.Count())];
                }

                if (Subjects[0] is Architect a && (Executor.Room != null && Executor.Room.Architects.Contains(a) || (Executor.Block.Architects.Contains(a) && Executor.Room == null)))
                {
                    Object targetBodyPart = a.FindBodyPart(Subjects[1].Metadata);

                    if (targetBodyPart != null && Weapon.WeaponMaximumRange >= Executor.GetDistance(a) && Math.Abs(a.YLevelInFeet - Executor.YLevelInFeet) <= 5)
                    {
                        Game1.CalculateAttack(Game1.DetermineAttackVerb(Weapon.DamageType), Executor, targetBodyPart, "decideforme", Weapon);
                        if (Executor.DoubleStrikeReady)
                        {
                            MakeObservation("You double strike!", Color.Pink, new EntityList<Entity>());
                            Game1.CalculateAttack(Game1.DetermineAttackVerb(Weapon.DamageType), Executor, targetBodyPart, "decideforme", Weapon);
                            Executor.DoubleStrikeReady = false;
                        }
                    }
                    else if (targetBodyPart == null)
                    {
                        MakeObservation("The targeted creature doesn't have one of those, or you are not being specific enough (try left X, right X...?)", Color.Yellow, new EntityList<Entity>());
                    }
                    else
                    {
                        Game1.Announcements.Add(new TextStorage("You wave your hands around, but you aren't close enough.", Color.Yellow, new EntityList<Entity>() { }));
                    }
                }
                else
                {
                    MakeObservation("You can't target body parts of an object, at least not yet.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "basic_attack")
            {
                // Find weapon and then calculate the attack

                if (!Game1.GameWorld.GamePlayerAssociation.HasAttacked)
                {
                    Game1.GameWorld.GamePlayerAssociation.HasAttacked = true;
                    MakeObservation("For more control, try attacking a specific part, with a specific weapon, or both.", Color.Green, new EntityList<Entity>());
                }

                Object Weapon;

                // Check the player's main hand based on their handedness
                Object MainHandObject = LoadedArchitects[Game1.ArchitectIndex].MainHeldObject;
                Object OffHandObject = LoadedArchitects[Game1.ArchitectIndex].OffHeldObject;

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
                    Weapon = LoadedArchitects[Game1.ArchitectIndex].BodyParts[r.Next(LoadedArchitects[Game1.ArchitectIndex].BodyParts.Count())];
                }

                Object Target;

                if (Subjects[0] is Architect a && (Executor.Room != null && Executor.Room.Architects.Contains(a) || (Executor.Block.Architects.Contains(a) && Executor.Room == null)))
                {
                    Target = ((Architect)(Subjects[0])).BodyParts[r.Next(((Architect)(Subjects[0])).BodyParts.Count())];
                }
                else if (Subjects[0] is Object o && (Executor.Room != null && Executor.Room.Objects.Contains(o) || (Executor.Block.Objects.Contains(o) && Executor.Room == null)))
                {
                    Target = (Object)(Subjects[0]);
                }
                else
                {
                    MakeObservation("You can't attack that.", Color.Yellow, new EntityList<Entity>());
                    return false;
                }

                // Check if the target is reinforced
                if (Target.Reinforced)
                {
                    Target.Integrity -= 20; // Drop integrity by 20

                    if (Target.Integrity < 0)
                    {
                        // Reset integrity and unreinforce
                        Target.Integrity = 50;
                        Target.Reinforced = false;

                        MakeObservation(Target.ReferredToNames[0] + " is no longer reinforced!", Color.Orange, new EntityList<Entity>());

                        // Search for the paired door/object/structure and unreinforce it
                        Object pairedObject = (Object)(Target.PairedObject());
                        if (pairedObject != null && pairedObject.Reinforced)
                        {
                            pairedObject.Reinforced = false;
                            pairedObject.Integrity = 50;
                        }
                    }
                    return true;
                }

                if (Weapon.WeaponMaximumRange >= Executor.GetDistance((Architect)(Target.Owner)) && Math.Abs(((Architect)(Target.Owner)).YLevelInFeet - Executor.YLevelInFeet) <= 5)
                {
                    Game1.CalculateAttack(Game1.DetermineAttackVerb(Weapon.DamageType), Executor, Target, "decideforme", Weapon);
                    if (Executor.DoubleStrikeReady)
                    {
                        MakeObservation("You double strike!", Color.Pink, new EntityList<Entity>());
                        Game1.CalculateAttack(Game1.DetermineAttackVerb(Weapon.DamageType), Executor, Target, "decideforme", Weapon);
                        Executor.DoubleStrikeReady = false;
                    }
                }
                else
                {
                    Game1.Announcements.Add(new TextStorage("You wave your hands around, but you aren't close enough.", Color.Yellow, new EntityList<Entity>() { }));
                }
            }
            else if (CommandID == "attack_with_weapon")
            {
                Object Weapon = LoadedArchitects[Game1.ArchitectIndex].MainHeldObject == Subjects[1] ? LoadedArchitects[Game1.ArchitectIndex].MainHeldObject :
                                LoadedArchitects[Game1.ArchitectIndex].OffHeldObject == Subjects[1] ? LoadedArchitects[Game1.ArchitectIndex].OffHeldObject :
                                Executor.BodyParts.FirstOrDefault(bp => bp == Subjects[1]) ??
                                Executor.FindBodyPart(Subjects[1].Metadata);

                Object Target;

                if (Subjects[0] is Architect a && (Executor.Room != null && Executor.Room.Architects.Contains(a) || (Executor.Block.Architects.Contains(a) && Executor.Room == null)))
                {
                    Target = a.BodyParts[r.Next(a.BodyParts.Count())];
                }
                else if (Subjects[0] is Object o && (Executor.Room != null && Executor.Room.Objects.Contains(o) || (Executor.Block.Objects.Contains(o) && Executor.Room == null)))
                {
                    Target = (Object)(Subjects[0]);
                }
                else
                {
                    MakeObservation("You can't attack that.", Color.Yellow, new EntityList<Entity>());
                    return false;
                }
                if (Target.Reinforced)
                {
                    Target.Integrity -= 20;

                    if (Target.Integrity < 0)
                    {
                        Target.Integrity = 50;
                        Target.Reinforced = false;

                        MakeObservation(Target.ReferredToNames[0] + " is no longer reinforced!", Color.Orange, new EntityList<Entity>());

                        Object pairedObject = (Object)(Target.PairedObject());
                        if (pairedObject != null && pairedObject.Reinforced)
                        {
                            pairedObject.Reinforced = false;
                            pairedObject.Integrity = 50;
                        }
                    }
                    return true;
                }

                if (Weapon != null && Weapon.WeaponMaximumRange >= Executor.GetDistance((Architect)Target.Owner) && Math.Abs(((Architect)(Target.Owner)).YLevelInFeet - Executor.YLevelInFeet) <= 5)
                {
                    Game1.CalculateAttack(Game1.DetermineAttackVerb(Weapon.DamageType), Executor, Target, "decideforme", Weapon);
                    if (Executor.DoubleStrikeReady)
                    {
                        MakeObservation("You double strike!", Color.Pink, new EntityList<Entity>());
                        Game1.CalculateAttack(Game1.DetermineAttackVerb(Weapon.DamageType), Executor, Target, "decideforme", Weapon);
                        Executor.DoubleStrikeReady = false;
                    }
                }
                else if (Weapon == null)
                {
                    MakeObservation("You need to have that object in your hands or as an accessible part of your body.", Color.Yellow, new EntityList<Entity>());
                }
                else
                {
                    Game1.Announcements.Add(new TextStorage("You wave your hands around, but you aren't close enough.", Color.Yellow, new EntityList<Entity>() { }));
                }
            }

            else if (CommandID == "attack_body_part_with_item")
            {
                if (Subjects[0] is Architect a)
                {
                    if ((Executor.Room != null && Executor.Room.Architects.Contains(a) || (Executor.Block.Architects.Contains(a) && Executor.Room == null)))
                    {
                        if (a.BodyParts.Any(bodyPart => bodyPart.Type == Subjects[1].Metadata))
                        {
                            Object item = null;

                            if (Subjects[2] is Object)
                            {
                                item = LoadedArchitects[Game1.ArchitectIndex].MainHeldObject == Subjects[2] ? LoadedArchitects[Game1.ArchitectIndex].MainHeldObject :
                                       LoadedArchitects[Game1.ArchitectIndex].OffHeldObject == Subjects[2] ? LoadedArchitects[Game1.ArchitectIndex].OffHeldObject :
                                       Executor.BodyParts.FirstOrDefault(bp => bp == Subjects[2]) ??
                                       Executor.FindBodyPart(Subjects[2].Metadata);
                            }
                            else if (Subjects[2] is Entity)
                            {
                                item = Executor.FindBodyPart(Subjects[2].Metadata);
                            }

                            if (item == null)
                            {
                                // Item not accessible
                                MakeObservation("You need to have that object in your hands or as an accessible part of your body.", Color.Yellow, new EntityList<Entity>());
                            }
                            else if (item.WeaponMaximumRange < Executor.GetDistance(a) || Math.Abs(a.YLevelInFeet - Executor.YLevelInFeet) > 5)
                            {
                                // Target too far away (distance or height)
                                Game1.Announcements.Add(new TextStorage("You wave your hands around, but you aren't close enough.", Color.Yellow, new EntityList<Entity>() { }));
                            }
                            else
                            {
                                // Valid attack
                                Game1.CalculateAttack(Game1.DetermineAttackVerb(item.DamageType), Executor, a.FindBodyPart(Subjects[1].Metadata), "decideforme", item);
                                if (Executor.DoubleStrikeReady)
                                {
                                    MakeObservation("You double strike!", Color.Pink, new EntityList<Entity>());
                                    Game1.CalculateAttack(Game1.DetermineAttackVerb(item.DamageType), Executor, a.FindBodyPart(Subjects[1].Metadata), "decideforme", item);
                                    Executor.DoubleStrikeReady = false;
                                }
                            }
                        }
                        else
                        {
                            // Target doesn't have the specified body part
                            MakeObservation("The targeted creature doesn't have one of those, or you are not being specific enough (try left X, right X...?)", Color.Yellow, new EntityList<Entity>());
                        }
                    }
                    else
                    {
                        // Can't attack the specified target
                        MakeObservation("You can't attack that.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else if (Subjects[0] is Object)
                {
                    // Body parts of objects cannot be attacked
                    MakeObservation("You can't target body parts of an object.", Color.Yellow, new EntityList<Entity>());
                }
                else
                {
                    // Default fallback for invalid targets
                    MakeObservation("You can't attack that.", Color.Yellow, new EntityList<Entity>());
                }
            }



            else if (CommandID == "fix_hair")
            {
                MakeObservation("You change your flamestyle.", Color.MediumPurple, new EntityList<Entity>());

                Executor.HairID += 1;

                if (Executor.HairID == 4)
                {
                    Executor.HairID = 0;
                }
            }

            else if (CommandID == "game_menu")
            {
                Game1.InInventory = true;
                Game1.Inventory.Text = "Return";
                Game1.Inventory.Hitbox.X = 1174;
            }


            else if (CommandID == "become_invisible")
            {
                if (Executor.Invisible)
                {
                    MakeObservation("You are already in the shadows.", Color.Yellow, new EntityList<Entity>());
                }
                else if (Executor.PathOfShadowLevel >= 4)
                {
                    MakeObservation("You enter the darkness.", Color.Gray, new EntityList<Entity>());
                    Executor.Invisible = true;
                    Executor.ExtraStealth += 1000;
                }
                else
                {
                    MakeObservation("You are not experienced enough in the shadows to partake in such a maneuver.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "exit_invisibility")
            {
                Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed()));
                if (Executor.Invisible)
                {
                    MakeObservation("You exit the shadows.", Color.Gray, new EntityList<Entity>());
                    Executor.Invisible = false;
                }
                else
                {
                    MakeObservation("You are not in the shadows.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "level_up" && Subjects[0].Metadata == "up")
            {
                Executor.Level++;
                Executor.SpendableLevels++;
                MakeObservation("You divine an imbuement of great power.", Color.Yellow, new EntityList<Entity>());

            }

            else if (CommandID == "engage_target")
            {
                if (Subjects[0] is Architect targetArchitect)
                {
                    if (ArchitectsToUse.Contains(targetArchitect))
                    {
                        foreach (var architect in ArchitectsToUse)
                        {
                            if (architect == targetArchitect) // The target architect
                            {
                                Executor.ModifyDistance(architect, -1); // Decrease distance by 1
                            }
                            else
                            {
                                Executor.ModifyDistance(architect, 2); // Increase distance with all others by 2
                            }
                        }
                        MakeObservation("You focus your target, shifting distances.", Color.Green, new EntityList<Entity>());
                        Executor.CooldownCycles += (int)Math.Round((10 / Executor.Speed()));
                    }
                    else
                    {
                        MakeObservation("The target architect is not in the same area.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("The target is not an architect.", Color.Red, new EntityList<Entity>());
                }
            }

            else if (CommandID == "approach_target")
            {
                if (Subjects[0] is Architect targetArchitect)
                {
                    if (ArchitectsToUse.Contains(targetArchitect))
                    {
                        Executor.ModifyDistance(targetArchitect, -2); // Decrease distance by 2
                        MakeObservation("You move closer to the target.", Color.Green, new EntityList<Entity>());
                        Executor.CooldownCycles += (int)Math.Round((15 / Executor.Speed()));
                    }
                    else
                    {
                        MakeObservation("The target architect is not in the same area.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("The target is not an architect.", Color.Red, new EntityList<Entity>());
                }
            }
            else if (CommandID == "distance_from_target")
            {
                if (Subjects[0] is Architect targetArchitect)
                {
                    if (ArchitectsToUse.Contains(targetArchitect))
                    {
                        Executor.ModifyDistance(targetArchitect, 2); // Increase distance by 2
                        MakeObservation("You increase your distance from the target.", Color.Green, new EntityList<Entity>());
                        Executor.CooldownCycles += (int)Math.Round((15 / Executor.Speed()));
                    }
                    else
                    {
                        MakeObservation("The target architect is not in the same area.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("The target is not an architect.", Color.Red, new EntityList<Entity>());
                }
            }
            else if (CommandID == "pet")
            {
                if (Subjects[0] is Architect targetArchitect)
                {
                    if (ArchitectsToUse.Contains(targetArchitect))
                    {
                        Executor.CooldownCycles += (int)Math.Round((15 / Executor.Speed()));
                        MakeObservation("You deliver headpats to " + targetArchitect.ReferredToNames[0] + ".", Color.Green, new EntityList<Entity>());

                        if (!Game1.GameWorld.HumanoidRaces.Contains(targetArchitect.Race) && !Game1.GameWorld.ExtraRaces.Contains(targetArchitect.Race))
                        {
                            if (targetArchitect.CombatCycles == 0)
                            {
                                MakeObservation(targetArchitect.ReferredToNames[0] + ": *happy shibesque noises*", Color.Lime, new EntityList<Entity>());
                            }
                            else
                            {
                                MakeObservation(targetArchitect.ReferredToNames[0] + " doesn't appear to be in the mood...", Color.Yellow, new EntityList<Entity>());
                            }
                        }
                        else
                        {
                            MakeObservation(targetArchitect.ReferredToNames[0] + " looks very uncomfortable.", Color.Orange, new EntityList<Entity>());
                        }
                    }
                    else
                    {
                        MakeObservation("The target is not in the same area.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You weren't supposed to pet this.", Color.Yellow, new EntityList<Entity>());
                }
            }

            else if (CommandID == "wield_item")
            {
                Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed()));
                if (Subjects[0] is Object && LoadedArchitects[Game1.ArchitectIndex].Inventory.Contains((Object)Subjects[0]))
                {
                    if (LoadedArchitects[Game1.ArchitectIndex].MainHeldObject == null)
                    {
                        LoadedArchitects[Game1.ArchitectIndex].MainHeldObject = ((Object)Subjects[0]);
                        LoadedArchitects[Game1.ArchitectIndex].Inventory.Remove(((Object)Subjects[0]));
                    }
                    else if (LoadedArchitects[Game1.ArchitectIndex].OffHeldObject == null)
                    {
                        LoadedArchitects[Game1.ArchitectIndex].OffHeldObject = ((Object)Subjects[0]);
                        LoadedArchitects[Game1.ArchitectIndex].Inventory.Remove(((Object)Subjects[0]));
                    }
                    else
                    {
                        MakeObservation("Your hands are full.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("That is not an object in your inventory.", Color.Yellow, new EntityList<Entity>());
                }
            }





            else if (CommandID == "ditch_inventory")
            {
                Executor.CooldownCycles += (int)(Math.Round(50 / Executor.Speed()));

                EntityList<Object> Clothings = new EntityList<Object>();
                foreach (Object o in Executor.Clothing)
                {
                    if (o.Type != "undergarment" && o.Type != "brassiere")
                    {
                        Clothings.Add(o);
                    }
                }

                foreach (Object o in Clothings)
                {
                    Executor.Clothing.Remove(o);
                    (Executor.Room != null ? Executor.Room.Objects : Executor.Block.Objects).Add(o);
                    o.Room = Executor.Room;
                    o.Block = Executor.Block;

                    // Incur market debt if in a market
                    if (Executor.Structure != null && Executor.Structure.Type == "market")
                    {
                        Executor.Structure.MarketDebt += o.Value();
                    }
                }

                var itemsToMove = new EntityList<Object>(Executor.Inventory);

                foreach (Object o in itemsToMove)
                {
                    Executor.Inventory.Remove(o);
                    (Executor.Room != null ? Executor.Room.Objects : Executor.Block.Objects).Add(o);
                    o.Room = Executor.Room;
                    o.Block = Executor.Block;

                    // Incur market debt if in a market
                    if (Executor.Structure != null && Executor.Structure.Type == "market")
                    {
                        Executor.Structure.MarketDebt += o.Value();
                    }
                }

                MakeObservation("You drop your inventory.", Color.Orange, new EntityList<Entity>());
            }




            else if (CommandID == "place_item_in")
            {
                Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed()));
                if (Subjects[0] == Executor.MainHeldObject || Subjects[0] == Executor.OffHeldObject || Executor.Inventory.Contains(Subjects[0]))
                {
                    // Check if the subject is "shadow storage" directly, avoiding the need for it to be an Object
                    if (Subjects[1].Metadata == "shadow storage" || (Subjects[1] is Object && ((Object)(Subjects[1])).Type == "shadow storage"))
                    {
                        // Shadow storage logic
                        if (Executor.Room == null)
                        {
                            bool StorageFound = false;
                            foreach (Object o in Executor.Block.Objects)
                            {
                                if (o.Type == "shadow storage")
                                {
                                    if (!Executor.ShadowStorage.Contains((Object)Subjects[0]))
                                    {
                                        Executor.ShadowStorage.Add((Object)Subjects[0]);

                                        // Optionally clear the object from hands or inventory, but keep it linked to shadow storage
                                        if (Subjects[0] == Executor.MainHeldObject)
                                        {
                                            Executor.MainHeldObject = null;
                                        }
                                        else if (Subjects[0] == Executor.OffHeldObject)
                                        {
                                            Executor.OffHeldObject = null;
                                        }
                                        else if (Executor.Inventory.Contains(Subjects[0]))
                                        {
                                            Executor.Inventory.Remove((Object)(Subjects[0]));
                                        }

                                        MakeObservation("You place the " + Subjects[0].ReferredToNames[0] + " into the shadow storage.", Color.Green, new EntityList<Entity>() { Subjects[0] });
                                        StorageFound = true;

                                        // Add historical event for placing the item
                                        Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Executor.Name + " placed " + Subjects[0].Name + " into shadow storage in " + Executor.Location.Name + ".", Executor.Location.Region, new EntityList<Entity>() { Executor, Subjects[0], Executor.Location }));
                                    }
                                    else
                                    {
                                        MakeObservation("The item is already in the shadow storage.", Color.Yellow, new EntityList<Entity>() { });
                                    }
                                    break; // Exit the loop once the shadow storage is processed
                                }
                            }

                            if (!StorageFound)
                            {
                                MakeObservation("There is not a shadow storage nearby.", Color.Yellow, new EntityList<Entity>());
                            }
                        }
                        else
                        {
                            MakeObservation("There is not a shadow storage nearby.", Color.Yellow, new EntityList<Entity>());
                        }
                    }
                    else if (Subjects[1] is Object subjectObject && subjectObject.IsContainer)
                    {
                        // Handling normal container logic
                        if (Subjects[0] == Executor.MainHeldObject)
                        {
                            Executor.MainHeldObject = null;
                        }
                        else if (Subjects[0] == Executor.OffHeldObject)
                        {
                            Executor.OffHeldObject = null;
                        }
                        else if (Executor.Inventory.Contains(Subjects[0]))
                        {
                            Executor.Inventory.Remove((Object)(Subjects[0]));
                        }

                        subjectObject.ContainedObjects.Add((Object)Subjects[0]);

                        MakeObservation("You place the " + Subjects[0].ReferredToNames[0] + " into the " + Subjects[1].ReferredToNames[0] + ".", Color.Green, new EntityList<Entity>() { Subjects[0], Subjects[1] });

                        // Add historical event for placing the item
                        if (Executor.Structure == null)
                        {
                            Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Executor.Name + " placed " + Subjects[0].Name + " into " + Subjects[1].ReferredToNames[0] + " in " + Executor.Location.Name + ".", Executor.Location.Region, new EntityList<Entity>() { Executor, Subjects[0], Executor.Location }));
                        }
                        else
                        {
                            Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Executor.Name + " placed " + Subjects[0].Name + " into " + Subjects[1].ReferredToNames[0] + " in " + Executor.Location.Name + ", at the " + Executor.Structure.Type + " " + Executor.Structure.Name + ".", Executor.Location.Region, new EntityList<Entity>() { Executor, Subjects[0], Subjects[1], Executor.Structure }));
                        }
                    }
                    else
                    {
                        MakeObservation(Subjects[1].ReferredToNames[0] + " can't hold anything.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    if (Executor.Clothing.Contains(Subjects[1]))
                    {
                        if (Executor.Sex == "male")
                        {
                            MakeObservation("You are going to have to take that off first, sir.", Color.Yellow, new EntityList<Entity>());
                        }
                        else
                        {
                            MakeObservation("You are going to have to take that off first, madame.", Color.Yellow, new EntityList<Entity>());
                        }
                    }
                    else
                    {
                        MakeObservation("You don't have that.", Color.Yellow, new EntityList<Entity>());
                    }
                }
            }
            else if (CommandID == "claim_structure")
            {
                if (Executor.Structure == null)
                {
                    MakeObservation("You need to be in a structure.", Color.Yellow, new EntityList<Entity>());
                }
                else
                {
                    bool canClaimStructure = true;

                    foreach (Architect a in Game1.LoadedArchitects)
                    {
                        // Check if the architect is alive, not in the player's association, and calls this location home
                        if (a.IsAlive && !Game1.GameWorld.GamePlayerAssociation.Associates.Contains(a) && a.HomeLocation == Executor.Location)
                        {
                            canClaimStructure = false;
                            break; // No need to check further if one condition fails
                        }
                    }

                    // Check surrounding districts for any architects
                    if (canClaimStructure && Executor.Location.Districts.Count > 1)
                    {
                        foreach (District district in Executor.Location.Districts)
                        {
                            // Skip the Executor's current district
                            if (district != Executor.District && district.Architects.Count > 0)
                            {
                                canClaimStructure = false;
                                break; // No need to check further if one district fails
                            }
                        }
                    }

                    if (canClaimStructure)
                    {
                        // Logic for allowing structure claim
                        Game1.GameWorld.GamePlayerAssociation.Residences.Add(Executor.Structure);
                        MakeObservation(Executor.Structure.Name + " is now a residence of " + Game1.GameWorld.GamePlayerAssociation.Name + ". You can ascend a party with \"ascend\".", Color.Green, new EntityList<Entity>());
                    }
                    else
                    {
                        // Logic for not allowing structure claim
                        MakeObservation("You cannot control this structure yet. Try to secure the entire location first.", Color.Red, new EntityList<Entity>());
                    }
                }
            }

            else if (CommandID == "ascend")
            {
                if (Executor.Structure != null && Game1.GameWorld.GamePlayerAssociation.Residences.Contains(Executor.Structure))
                {
                    Game1.SwitchState("ascendant", false);
                    Game1.AscendantState = "main";

                    foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                    {
                        Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + a.Name + " sought greater control in " + a.Location.Name + ".", Executor.Location.Region, new EntityList<Entity>() { a, a.Location }));
                    }

                    Executor.Location.Government = Game1.GameWorld.GamePlayerAssociation.ActiveParty;

                    Game1.GameWorld.RevealNearbyTiles(Game1.GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX, Game1.GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ, 24, false);

                    Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].District.Unload();

                    Game1.GameWorld.GamePlayerAssociation.ActiveParty = null;
                }
                else
                {
                    MakeObservation("You do not control the structure you are in. If you have no competition, you can claim a structure with \"claim this structure\"", Color.Red, new EntityList<Entity>());
                }
            }
            else if (CommandID == "take_item_from")
            {
                Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed()));

                if (Subjects[1].Metadata == "shadow storage" || (Subjects[1] is Object && ((Object)Subjects[1]).Type == "shadow storage"))
                {
                    // Shadow storage logic
                    if (Executor.Room == null)
                    {
                        bool StorageFound = false;

                        foreach (Object o in Executor.Block.Objects)
                        {
                            if (o.Type == "shadow storage")
                            {
                                if (Executor.ShadowStorage.Contains((Object)Subjects[0]))
                                {
                                    if (Executor.MainHeldObject == null)
                                    {
                                        Executor.MainHeldObject = (Object)Subjects[0];
                                    }
                                    else if (Executor.OffHeldObject == null)
                                    {
                                        Executor.OffHeldObject = (Object)Subjects[0];
                                    }
                                    else
                                    {
                                        Executor.Inventory.Add((Object)Subjects[0]);
                                    }

                                    Executor.ShadowStorage.Remove((Object)Subjects[0]);

                                    MakeObservation("You retrieve the " + Subjects[0].ReferredToNames[0] + " from the shadow storage.", Color.Green, new EntityList<Entity>() { Subjects[0] });
                                    StorageFound = true;

                                    // Add historical event for taking the item
                                    Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Executor.Name + " took " + Subjects[0].Name + " from shadow storage in " + Executor.Location.Name + ".", Executor.Location.Region, new EntityList<Entity>() { Executor, Subjects[0], Executor.Location }));
                                }
                                else
                                {
                                    MakeObservation("The shadow storage does not contain that.", Color.Green, new EntityList<Entity>());
                                }
                                break; // Exit the loop once the shadow storage is processed
                            }
                        }

                        if (!StorageFound)
                        {
                            MakeObservation("There is not a shadow storage nearby.", Color.Yellow, new EntityList<Entity>());
                        }
                    }
                    else
                    {
                        MakeObservation("There is not a shadow storage nearby.", Color.Green, new EntityList<Entity>());
                    }
                }
                else
                {
                    EntityList<Object> searchScope = Executor.Room != null ? Executor.Room.Objects : Executor.Block.Objects;

                    Object container = null;
                    Object itemToTake = null;

                    foreach (Object obj in searchScope)
                    {
                        if (obj.Type == ((Object)Subjects[1]).Type && obj.Materials.SequenceEqual(((Object)Subjects[1]).Materials))
                        {
                            if (Subjects[1].Name == null || obj.Name == Subjects[1].Name)
                            {
                                foreach (Object containedObj in obj.ContainedObjects)
                                {
                                    if (containedObj.Type == ((Object)Subjects[0]).Type && containedObj.Materials.SequenceEqual(((Object)Subjects[0]).Materials))
                                    {
                                        if (Subjects[0].Name == null || containedObj.Name == Subjects[0].Name)
                                        {
                                            container = obj;
                                            itemToTake = containedObj;
                                            break;
                                        }
                                    }
                                }
                                if (container != null && itemToTake != null)
                                {
                                    break;
                                }
                            }
                        }
                    }

                    if (itemToTake != null && container != null)
                    {
                        if (Executor.MainHeldObject == null)
                        {
                            Executor.MainHeldObject = itemToTake;
                        }
                        else if (Executor.OffHeldObject == null)
                        {
                            Executor.OffHeldObject = itemToTake;
                        }
                        else
                        {
                            Executor.Inventory.Add(itemToTake);
                        }

                        container.ContainedObjects.Remove(itemToTake);

                        MakeObservation("You remove the " + itemToTake.ReferredToNames[0] + " from the " + container.ReferredToNames[0] + ".", Color.Green, new EntityList<Entity>() { itemToTake, container });

                        // Add historical event for taking the item

                        if (itemToTake.Name != null)
                        {
                            if (Executor.Structure == null)
                            {
                                Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Executor.Name + " took " + itemToTake.Name + " from " + container.ReferredToNames[0] + " in " + Executor.Location.Name + ".", Executor.Location.Region, new EntityList<Entity>() { Executor, itemToTake, Executor.Location }));
                            }
                            else
                            {
                                Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Executor.Name + " took " + itemToTake.Name + " from " + container.ReferredToNames[0] + " in " + Executor.Location.Name + ", at the " + Executor.Structure.Type + " " + Executor.Structure.Name + ".", Executor.Location.Region, new EntityList<Entity>() { Executor, itemToTake, Executor.Location, Executor.Structure }));
                            }
                        }

                        if (Game1.PossibleMagicalItems.Contains(itemToTake.Type) && (itemToTake.SpecialKnowledge != null && Game1.GameWorld.AllLegendarySpells.Contains(itemToTake.SpecialKnowledge)))
                        {
                            MakeObservation("This legendary artifact contains an unprecedented magic of unknown orgin.", Color.PaleVioletRed, new EntityList<Entity>());
                            MakeObservation(itemToTake.SpecialKnowledge.Name, Color.PaleVioletRed, new EntityList<Entity>());
                            MakeObservation(Game1.SkillSpellDescriptions[itemToTake.SpecialKnowledge.Name], Color.PaleVioletRed, new EntityList<Entity>());
                        }

                    }
                    else
                    {
                        MakeObservation("You cannot take that for some reason.", Color.Green, new EntityList<Entity>());
                    }
                }
            }


            else if (CommandID != null && waitCommands.ContainsKey(CommandID.ToLower()))
            {
                int secondsToWait = waitCommands[CommandID.ToLower()];
                Executor.CooldownCycles += secondsToWait * 10;
                string observationMessage = secondsToWait == 1 ? "You wait for one second." : $"You wait for {secondsToWait} seconds.";
                MakeObservation(observationMessage, Color.Green, new EntityList<Entity>());
            }
            else if (CommandID != null && CommandID.ToLower().StartsWith("wait"))
            {
                MakeObservation("Try using common wait times, in plain words, like \"wait six seconds\".", Color.ForestGreen, new EntityList<Entity>());
            }
            else if (CommandID == "wear_item" && (Subjects.Count() == 1 || Subjects[0].Metadata == "all"))
            {
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed()));

                if (Subjects[0].Metadata == "all")
                {
                    var wearableItems = Executor.Inventory.Where(item => item.IsWearable);

                    if (Executor.MainHeldObject != null && Executor.MainHeldObject.IsWearable)
                    {
                        wearableItems.Add(Executor.MainHeldObject);
                        Executor.MainHeldObject = null;
                    }

                    if (Executor.OffHeldObject != null && Executor.OffHeldObject.IsWearable)
                    {
                        wearableItems.Add(Executor.OffHeldObject);
                        Executor.OffHeldObject = null;
                    }

                    foreach (var item in wearableItems)
                    {
                        if (item.Type == "undergarment" || item.Type == "brassiere")
                        {
                            // Drop the undergarment item
                            Executor.Inventory.Remove(item);

                            if (Executor.Room != null)
                            {
                                Executor.Room.Objects.Add(item);
                                item.Room = Executor.Room;
                                item.Block = Executor.Room.Structure.Block;
                                MakeObservation("You drop the " + item.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { item });
                            }
                            else
                            {
                                Executor.Block.Objects.Add(item);
                                item.Block = Executor.Block;
                                MakeObservation("You drop the " + item.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { item });
                            }

                            if (Executor.Structure != null && Executor.Structure.Type == "market")
                            {
                                Executor.Structure.MarketDebt += item.Value();
                            }

                            // Add historical event for dropping the item
                            if (item.Name != null)
                            {
                                if (Executor.Structure == null)
                                {
                                    Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Executor.Name + " dropped " + item.Name + " in " + Executor.Location.Name + ".", Executor.Location.Region, new EntityList<Entity>() { Executor, item, Executor.Location }));
                                }
                                else
                                {
                                    Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Executor.Name + " dropped " + item.Name + " in " + Executor.Location.Name + ", at the " + Executor.Structure.Type + " " + Executor.Structure.Name + ".", Executor.Location.Region, new EntityList<Entity>() { Executor, item, Executor.Location, Executor.Structure }));

                                    Executor.Structure.HistoricalObjects.Add(item);
                                }
                            }
                        }
                        else if (Executor.Clothing.Any(c => c.Type == item.Type) && item.Type != "amulet")
                        {
                            MakeObservation($"You can't wear more than one {item.Type}, fascist.", Color.Yellow, new EntityList<Entity>());
                        }
                        else
                        {
                            MakeObservation("You put on the " + item.ReferredToNames[0] + ".", Color.Green, new EntityList<Entity>() { item });
                            Executor.Clothing.Add(item);
                            Executor.Inventory.Remove(item);
                            Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed()));
                        }
                    }
                }
                else if (Subjects[0] is Object && (Executor.Inventory.Contains(((Object)Subjects[0])) || Executor.MainHeldObject == ((Object)Subjects[0]) || Executor.OffHeldObject == ((Object)Subjects[0])))
                {
                    if (((Object)Subjects[0]).IsWearable)
                    {
                        if (Executor.Clothing.Any(c => c.Type == ((Object)Subjects[0]).Type) && ((Object)Subjects[0]).Type != "amulet")
                        {
                            MakeObservation($"You can't wear more than one {((Object)Subjects[0]).Type}, fascist.", Color.Yellow, new EntityList<Entity>());
                        }
                        else
                        {
                            MakeObservation("You put on the " + Subjects[0].ReferredToNames[0] + ".", Color.Green, new EntityList<Entity>() { Subjects[0] });

                            if (Executor.Inventory.Contains(((Object)Subjects[0])))
                            {
                                Executor.Inventory.Remove((Object)Subjects[0]);
                            }
                            else if (Executor.OffHeldObject == ((Object)Subjects[0]))
                            {
                                Executor.OffHeldObject = null;
                            }
                            else
                            {
                                Executor.MainHeldObject = null;
                            }

                            Executor.Clothing.Add(((Object)Subjects[0]));
                        }
                    }
                    else
                    {
                        if (Executor.Clothing.Count() > 0)
                        {
                            MakeObservation("You hang the " + Subjects[0].ReferredToNames[0] + " off of your " + Executor.Clothing[Game1.r.Next(Executor.Clothing.Count())].ReferredToNames[0] + ". You feel disadvantaged, but stylish.", Color.Green, new EntityList<Entity>() { Subjects[0] });
                            Executor.Clothing.Add(((Object)Subjects[0]));
                        }
                        else
                        {
                            MakeObservation("You aren't wearing anything to hang it off of. On a semi-related note, please put something on.", Color.Yellow, new EntityList<Entity>());
                        }
                    }
                }
                else if (Subjects[0] is Architect a)
                {
                    if (a.Race == GameWorld.GetRace("shiba") && a.Block == Executor.Block && a.Room == Executor.Room)
                    {
                        MakeObservation("You deploy the shiba inu. It climbs up to your face and merges with your soul.", Color.Green, new EntityList<Entity>());
                        Executor.MeldedShibas.Add(a);

                        if (a.Room != null)
                        {
                            a.Room.Architects.Remove(a);
                        }
                        else
                        {
                            a.Block.Architects.Remove(a);
                        }

                        a.Room = null;
                        a.Block = null;
                        Game1.LoadedArchitectsToRemove.Add(a);
                    }
                    else
                    {
                        MakeObservation("You can't wear that, it's not a shiba inu.", Color.Green, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You don't have an object like that.", Color.Green, new EntityList<Entity>());
                }
            }

            else if (CommandID == "pick_up_item" && (Subjects.Count() == 1 || (Subjects.Count() == 2 && Subjects[0].Metadata == "up") || Subjects[0].Metadata == "all"))
            {
                if (Subjects.Count() == 2 && Subjects[0].Metadata == "up")
                {
                    Subjects.RemoveAt(0);
                }

                Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed()));


                if (Subjects[0] is Object || Subjects[0].Metadata == "all")
                {
                    EntityList<Object> objectList = null;

                    if (Executor.Room != null)
                    {
                        objectList = LoadedArchitects[Game1.ArchitectIndex].Room.Objects;
                    }
                    else
                    {
                        objectList = LoadedArchitects[Game1.ArchitectIndex].Block.Objects;
                    }

                    if (Subjects[0].Metadata == "all")
                    {
                        // Create a copy of objectList to iterate over
                        var objectsToPickUp = objectList.ToList();

                        foreach (var obj in objectsToPickUp)
                        {
                            if (obj.Weight > 6000)
                            {
                                MakeObservation("The " + obj.ReferredToNames[0] + " is too heavy to pick up.", Color.Yellow, new EntityList<Entity>() { obj });
                                continue;
                            }

                            MakeObservation("You pick up the " + obj.ReferredToNames[0] + " and put it in your inventory.", Color.Yellow, new EntityList<Entity>() { obj });

                            // Modify the original objectList outside of this loop
                            objectList.Remove(obj);
                            LoadedArchitects[Game1.ArchitectIndex].Inventory.Add(obj);

                            // Update historical events, remove from room/block, update GUI, etc.
                            if (obj.Room != null && obj.Room.Structure.HistoricalObjects.Contains(obj))
                            {
                                obj.Room.Structure.HistoricalObjects.Remove(obj);
                            }

                            obj.Block = null;
                            obj.Room = null;

                            if (obj.Name != null)
                            {
                                Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Executor.Name + " acquired " + obj.Name + " in " + Executor.Location.Name + ".", Executor.Location.Region, new EntityList<Entity>() { Executor, obj, Executor.Location }));
                            }

                            if (obj.Imbuements.Count() > 0 || obj.IsWeapon || obj.Name != null)
                            {
                                Game1.IsInGui = true;

                                if (obj.Name != null)
                                {
                                    Game1.ItemPickupGuiLines.Add(Game1.Capitalize(obj.Name) + ", " + Game1.Capitalize(obj.Materials[0].Name) + " " + Game1.Capitalize(obj.Type));
                                }
                                else
                                {
                                    Game1.ItemPickupGuiLines.Add(Game1.Capitalize(obj.Materials[0].Name) + " " + Game1.Capitalize(obj.Type));
                                }

                                if (obj.Imbuements.Count() == 0)
                                {
                                    Game1.ItemPickupGuiLines.Add("This object has no imbuements.");
                                }
                                else
                                {
                                    Game1.ItemPickupGuiLines.Add("This object has some intriguing properties.");
                                    foreach (Imbuement i in obj.Imbuements)
                                    {
                                        Game1.ItemPickupGuiLines.Add(i.GetDescription());
                                    }
                                }

                                Game1.PickupConfirm.InvisibleLock = false;
                            }


                            if (Executor.Structure != null && Executor.Structure.Type == "market")
                            {
                                Executor.Structure.MarketDebt -= obj.Value();
                            }


                            Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed()));
                        }
                    }
                    else if (objectList != null)
                    {
                        // Search for other objects with the same ReferredToNames[0]
                        bool otherObjectsExist = objectList.Any(obj => obj != Subjects[0] && obj.ReferredToNames[0] == Subjects[0].ReferredToNames[0]);

                        if (otherObjectsExist)
                        {
                            if (((Object)Subjects[0]).Weight > 6000)
                            {
                                MakeObservation("The " + Subjects[0].ReferredToNames[0] + " is too heavy to pick up.", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                            }
                            else
                            {
                                Executor.TryPickUpItemType = ((Object)Subjects[0]).Type;
                                Executor.TryPickUpMaterials.Clear();

                                foreach (var material in ((Object)Subjects[0]).Materials)
                                {
                                    Executor.TryPickUpMaterials.Add(material);
                                }
                            }

                        }
                        else
                        {
                            if (((Object)Subjects[0]).Weight > 6000)
                            {
                                MakeObservation("The " + Subjects[0].ReferredToNames[0] + " is too heavy to pick up.", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                            }
                            else
                            {
                                // Proceed as normal
                                MakeObservation("You pick up the " + Subjects[0].ReferredToNames[0] + " and put it in your inventory.", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                                objectList.Remove((Object)Subjects[0]);
                                LoadedArchitects[Game1.ArchitectIndex].Inventory.Add((Object)Subjects[0]);

                                Game1.ItemPickupGuiLines.Clear();

                                if (((Object)Subjects[0]).Room != null && ((Object)Subjects[0]).Room.Structure.HistoricalObjects.Contains(((Object)Subjects[0])))
                                {
                                    ((Object)Subjects[0]).Room.Structure.HistoricalObjects.Remove(((Object)Subjects[0]));
                                }

                                ((Object)Subjects[0]).Block = null;
                                ((Object)Subjects[0]).Room = null;

                                if (Game1.PossibleMagicalItems.Contains(((Object)Subjects[0]).Type) && ((Object)Subjects[0]).SpecialKnowledge != null && Game1.GameWorld.AllLegendarySpells.Contains(((Object)Subjects[0]).SpecialKnowledge))
                                {
                                    MakeObservation("This legendary artifact contains an unprecedented magic of unknown orgin.", Color.PaleVioletRed, new EntityList<Entity>());
                                    MakeObservation(((Object)Subjects[0]).SpecialKnowledge.Name, Color.PaleVioletRed, new EntityList<Entity>());
                                    MakeObservation(Game1.SkillSpellDescriptions[((Object)Subjects[0]).SpecialKnowledge.Name], Color.PaleVioletRed, new EntityList<Entity>());
                                }

                                if (((Object)Subjects[0]).Name != null)
                                {
                                    Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Executor.Name + " acquired " + ((Object)Subjects[0]).Name + " in " + Executor.Location.Name + ".", Executor.Location.Region, new EntityList<Entity>() { Executor, Subjects[0], Executor.Location }));
                                }

                                if (((Object)Subjects[0]).Imbuements.Count() > 0 || ((Object)Subjects[0]).IsWeapon || ((Object)Subjects[0]).Name != null)
                                {
                                    Game1.IsInGui = true;

                                    if (((Object)Subjects[0]).Name != null)
                                    {
                                        Game1.ItemPickupGuiLines.Add(Game1.Capitalize(((Object)Subjects[0]).Name) + ", " + Game1.Capitalize(((Object)Subjects[0]).Materials[0].Name) + " " + Game1.Capitalize(((Object)Subjects[0]).Type));
                                    }
                                    else
                                    {
                                        Game1.ItemPickupGuiLines.Add(Game1.Capitalize(((Object)Subjects[0]).Materials[0].Name) + " " + Game1.Capitalize(((Object)Subjects[0]).Type));
                                    }

                                    if (((Object)Subjects[0]).Imbuements.Count() == 0)
                                    {
                                        Game1.ItemPickupGuiLines.Add("This object has no imbuements.");
                                    }
                                    else
                                    {
                                        List<string> ImbuementDescriptions = new List<string>();

                                        Game1.ItemPickupGuiLines.Add("This object has some intriguing properties.");

                                        foreach (Imbuement i in ((Object)Subjects[0]).Imbuements)
                                        {
                                            Game1.ItemPickupGuiLines.Add(i.GetDescription());
                                        }
                                    }

                                    Game1.PickupConfirm.InvisibleLock = false;
                                }

                                if (Executor.Structure != null && Executor.Structure.Type == "market")
                                {
                                    Executor.Structure.MarketDebt -= ((Object)(Subjects[0])).Value();
                                }

                            }
                        }
                    }
                    else if (LoadedArchitects[Game1.ArchitectIndex].OffHeldObject == Subjects[0])
                    {
                        MakeObservation("You stash the " + Subjects[0].ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                        LoadedArchitects[Game1.ArchitectIndex].OffHeldObject = null;
                        LoadedArchitects[Game1.ArchitectIndex].Inventory.Add((Object)Subjects[0]);
                    }
                    else if (LoadedArchitects[Game1.ArchitectIndex].MainHeldObject == Subjects[0])
                    {
                        MakeObservation("You stash the " + Subjects[0].ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                        LoadedArchitects[Game1.ArchitectIndex].MainHeldObject = null;
                        LoadedArchitects[Game1.ArchitectIndex].Inventory.Add((Object)Subjects[0]);
                    }
                    else
                    {
                        MakeObservation("You couldn't find anything like that in the area.", Color.Yellow, new EntityList<Entity>() { });
                    }
                }
                else if (Subjects[0] is Architect a && a.Race.Name == "shiba")
                {
                    MakeObservation("Perhaps you can wear the creature directly.", Color.Yellow, new EntityList<Entity>() { });
                }
                else
                {
                    MakeObservation("You cannot pick up that.", Color.Yellow, new EntityList<Entity>() { });
                }
            }


            else if (CommandID == "drop_item" && (Subjects.Count() == 1 || Subjects[0].Metadata == "all"))
            {
                Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed()));
                bool Found = true;

                // Test if the subject is 'all' to drop all items
                if (Subjects[0].Metadata == "all")
                {
                    var itemsToDrop = Executor.Inventory.ToList(); // Create a list of items to drop

                    foreach (var itemToDrop in itemsToDrop)
                    {
                        Executor.Inventory.Remove(itemToDrop);

                        EntityList<Object> objectList = Executor.Room != null ? Executor.Room.Objects : Executor.Block.Objects;

                        if (Executor.Room != null)
                        {
                            Executor.Room.Objects.Add(itemToDrop);
                            itemToDrop.Room = Executor.Room;
                            itemToDrop.Block = Executor.Room.Structure.Block;
                            MakeObservation("You drop the " + itemToDrop.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { itemToDrop });
                        }
                        else
                        {
                            Executor.Block.Objects.Add(itemToDrop);
                            itemToDrop.Block = Executor.Block;
                            MakeObservation("You drop the " + itemToDrop.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { itemToDrop });
                        }

                        if (Executor.Structure != null && Executor.Structure.Type == "market")
                        {
                            Executor.Structure.MarketDebt += itemToDrop.Value();
                        }

                        // Add historical event for dropping the item
                        if (itemToDrop.Name != null)
                        {
                            if (Executor.Structure == null)
                            {
                                Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Executor.Name + " dropped " + itemToDrop.Name + " in " + Executor.Location.Name + ".", Executor.Location.Region, new EntityList<Entity>() { Executor, itemToDrop, Executor.Location }));
                            }
                            else
                            {
                                Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Executor.Name + " dropped " + itemToDrop.Name + " in " + Executor.Location.Name + ", at the " + Executor.Structure.Type + " " + Executor.Structure.Name + ".", Executor.Location.Region, new EntityList<Entity>() { Executor, itemToDrop, Executor.Location, Executor.Structure }));

                                Executor.Structure.HistoricalObjects.Add(itemToDrop);
                            }
                        }

                        Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed()));
                    }
                }
                else
                {
                    // Test if the carried entity is the subject
                    if (Executor.CarryingEntity == Subjects[0])
                    {
                        // Drop the carried object or architect
                        if (Executor.CarryingEntity is Object carriedObject)
                        {
                            if (Executor.Room != null)
                            {
                                Executor.Room.Objects.Add(carriedObject);
                                carriedObject.Room = Executor.Room;
                                carriedObject.Block = Executor.Room.Structure.Block;
                                MakeObservation("You drop the " + carriedObject.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { carriedObject });
                            }
                            else
                            {
                                Executor.Block.Objects.Add(carriedObject);
                                carriedObject.Block = Executor.Block;
                                MakeObservation("You drop the " + carriedObject.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { carriedObject });
                            }
                        }
                        else if (Executor.CarryingEntity is Architect carriedArchitect)
                        {
                            if (Executor.Room != null)
                            {
                                Executor.Room.Architects.Add(carriedArchitect);
                                carriedArchitect.Room = Executor.Room;
                            }
                            else
                            {
                                Executor.Block.Architects.Add(carriedArchitect);
                            }
                            MakeObservation("You drop " + carriedArchitect.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { carriedArchitect });
                        }

                        Executor.CarryingEntity = null;
                    }
                    else
                    {
                        Object itemToDrop = ((Object)Subjects[0]);

                        if (itemToDrop == Executor.MainHeldObject)
                        {
                            Executor.MainHeldObject = null;
                        }
                        else if (itemToDrop == Executor.OffHeldObject)
                        {
                            Executor.OffHeldObject = null;
                        }
                        else if (Executor.Inventory.Contains(itemToDrop))
                        {
                            // Search for other objects with the same ReferredToNames[0]
                            bool otherObjectsExist = Executor.Inventory.Any(obj => obj != itemToDrop && obj.ReferredToNames[0] == itemToDrop.ReferredToNames[0]);

                            if (otherObjectsExist)
                            {
                                Executor.TryDropItemType = itemToDrop.Type;
                                Executor.TryDropMaterials.Clear();

                                foreach (var material in itemToDrop.Materials)
                                {
                                    Executor.TryDropMaterials.Add(material);
                                }
                                // Do not proceed with dropping the item here
                                return true;
                            }
                            else
                            {
                                Executor.Inventory.Remove(itemToDrop);
                            }
                        }
                        else
                        {
                            Found = false;
                        }

                        if (Found)
                        {
                            EntityList<Object> objectList = Executor.Room != null ? Executor.Room.Objects : Executor.Block.Objects;

                            if (Executor.Room != null)
                            {
                                Executor.Room.Objects.Add(itemToDrop);
                                itemToDrop.Room = Executor.Room;
                                itemToDrop.Block = Executor.Room.Structure.Block;
                                MakeObservation("You drop the " + itemToDrop.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { itemToDrop });
                            }
                            else
                            {
                                Executor.Block.Objects.Add(itemToDrop);
                                itemToDrop.Block = Executor.Block;
                                MakeObservation("You drop the " + itemToDrop.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { itemToDrop });
                            }

                            if (Executor.Structure != null && Executor.Structure.Type == "market")
                            {
                                Executor.Structure.MarketDebt += itemToDrop.Value();
                            }

                            // Add historical event for dropping the item
                            if (itemToDrop.Name != null)
                            {
                                if (Executor.Structure == null)
                                {
                                    Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Executor.Name + " dropped " + itemToDrop.Name + " in " + Executor.Location.Name + ".", Executor.Location.Region, new EntityList<Entity>() { Executor, itemToDrop, Executor.Location }));
                                }
                                else
                                {
                                    Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Executor.Name + " dropped " + itemToDrop.Name + " in " + Executor.Location.Name + ", at the " + Executor.Structure.Type + " " + Executor.Structure.Name + ".", Executor.Location.Region, new EntityList<Entity>() { Executor, itemToDrop, Executor.Location, Executor.Structure }));

                                    Executor.Structure.HistoricalObjects.Add(itemToDrop);
                                }
                            }
                        }
                        else
                        {
                            MakeObservation("You don't have that.", Color.Yellow, new EntityList<Entity>());
                        }
                    }
                }
            }


            else if (CommandID == "remove_worn_item")
            {
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed()));
                if (Subjects[0] is Object && Executor.Clothing.Contains(((Object)Subjects[0])))
                {
                    MakeObservation("You take off the " + Subjects[0].ReferredToNames[0] + ".", Color.Green, new EntityList<Entity>() { Subjects[0] });

                    // Remove the item from the Clothing list
                    Executor.Clothing.Remove((Object)Subjects[0]);

                    // Add the item back to the Executor's inventory
                    Executor.Inventory.Add((Object)Subjects[0]);
                }
                else if (Subjects[0] is Architect)
                {
                    if (((Architect)Subjects[0]).Race == GameWorld.GetRace("shiba") && Executor.MeldedShibas.Contains(Subjects[0]))
                    {
                        MakeObservation("You remove the shiba inu from your face, feeling a sense of loss.", Color.Green, new EntityList<Entity>());
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
                        MakeObservation("You can't take that off, its not a shiba inu. On a semi-related note, how the hell did you get that on?", Color.Green, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You aren't wearing an object like that.", Color.Green, new EntityList<Entity>());
                }
            }
            else if (CommandID == "examine")
            {
                if (Subjects[0] is Architect)
                {
                    MakeObservation(Subjects[0].ReferredToNames[0] + " (Race: " + ((Architect)Subjects[0]).Race.Name + ")", Color.White, new EntityList<Entity>() { Subjects[0] });
                    MakeObservation(((Architect)Subjects[0]).Race.Description, Color.LimeGreen, new EntityList<Entity>());
                    MakeObservation(((Architect)Subjects[0]).CheckEnergyLevel(), Color.Magenta, new EntityList<Entity>());
                    MakeObservation(((Architect)Subjects[0]).DescribeArchitectInventory(), Color.Orange, new EntityList<Entity>());

                    var specialRaces = new HashSet<string>
                    {
                        "shade",
                        "shadeheart",
                        "isofractal",
                        "icosidodecahedron",
                        "photonexus",
                        "hypernexus"
                    };

                    // Check if the subject's race is either in HumanoidRaces or matches one of the special races
                    if (GameWorld.HumanoidRaces.Contains(((Architect)Subjects[0]).Race) ||
                        specialRaces.Contains(((Architect)Subjects[0]).Race.Name) 
                        || Game1.GameWorld.ColossalTypes.Contains((((Architect)Subjects[0])).Race))
                    {
                        MakeObservation("Press F2 (or fn+F2) for a portrait.", Color.Cyan, new EntityList<Entity>());
                        Game1.StoredPortrait = ((Architect)Subjects[0]);
                    }
                    else
                    {
                        Game1.StoredPortrait = null;
                    }
                }
                else if (Subjects[0] is Object)
                {
                    if ((LoadedArchitects[Game1.ArchitectIndex].Room != null && LoadedArchitects[Game1.ArchitectIndex].Room.Objects.Contains(Subjects[0])) || LoadedArchitects[Game1.ArchitectIndex].Block.Objects.Contains(Subjects[0]) || (LoadedArchitects[Game1.ArchitectIndex].MainHeldObject == Subjects[0] || LoadedArchitects[Game1.ArchitectIndex].OffHeldObject == Subjects[0] || LoadedArchitects[Game1.ArchitectIndex].Inventory.Contains(Subjects[0])) || LoadedArchitects[Game1.ArchitectIndex].Clothing.Contains(Subjects[0]))
                    {
                        MakeObservation(Subjects[0].ReferredToNames[0], Color.White, new EntityList<Entity>() { Subjects[0] });
                        MakeObservation(((Object)Subjects[0]).Description, Color.White, new EntityList<Entity>());

                        string Materials = "Materials: ";
                        List<string> materialNames = ((Object)Subjects[0]).Materials.Select(m => m.Name).ToList();

                        if (materialNames.Count() > 1)
                        {
                            // Insert "and" before the last element
                            materialNames[^1] = "and " + materialNames[^1];

                            // Join all elements with a comma, except the last one which already has "and"
                            Materials += String.Join(", ", materialNames);
                        }
                        else if (materialNames.Count() == 1)
                        {
                            // If there's only one material, just add it
                            Materials += materialNames[0];
                        }

                        // Replace "/m" with the formatted material list
                        Materials = Materials.Replace("/m", Game1.FormatMaterialList(((Object)Subjects[0]).Materials));

                        MakeObservation(Materials, Color.White, (((Object)Subjects[0]).Materials.Cast<Entity>()));

                        foreach (Imbuement i in ((Object)Subjects[0]).Imbuements)
                        {
                            MakeObservation(i.GetDescription(), Color.Magenta, new EntityList<Entity>());
                        }
                    }
                    else
                    {
                        MakeObservation("You couldn't find anything like that nearby.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else if (Subjects[0] is Structure && LoadedArchitects[Game1.ArchitectIndex].Room == null && LoadedArchitects[Game1.ArchitectIndex].Block.Structures.Contains(Subjects[0]))
                {
                    MakeObservation(((Structure)Subjects[0]).GetStructureDescription(), Color.White, new EntityList<Entity>() { ((Structure)Subjects[0]) });
                }
                else
                {
                    MakeObservation("You couldn't find anything like that nearby.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "give_item")
            {
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed()));
                if (!(Subjects[0] is Object))
                {
                    MakeObservation("You can't give something that isn't an object.", Color.Yellow, new EntityList<Entity>());
                    return (false);
                }
                else if ((!(Subjects[1] is Object)) && (!(Subjects[1] is Architect)))
                {
                    MakeObservation("You can't give to something that isn't a person or object.", Color.Yellow, new EntityList<Entity>());
                    return (false);
                }
                else if (!(Executor.Inventory.Contains(Subjects[0])) && !(Executor.OffHeldObject == Subjects[0]) && !(Executor.MainHeldObject == Subjects[0]))
                {
                    MakeObservation("You don't have that in your inventory or hands.", Color.Yellow, new EntityList<Entity>());
                    return (false);
                }
                else if (Executor.MainHeldObject == Subjects[0])
                {
                    Executor.MainHeldObject = null;
                }
                else if (Executor.OffHeldObject == Subjects[0])
                {
                    Executor.OffHeldObject = null;
                }
                else if (Executor.Inventory.Contains(Subjects[0]))
                {
                    Executor.Inventory.Remove((Object)(Subjects[0]));
                }

                Object GivenObject = ((Object)(Subjects[0]));

                //if we didnt return it means we found a givingobject and something to give it to. we also took it out of their hands.

                if (Subjects[1] is Architect)
                {
                    AddMessage(Executor.Name + ": Here, take this.", Color.White, new EntityList<Entity>() { Executor });
                    MakeObservation("You give the " + Subjects[0].ReferredToNames[0] + " to " + Subjects[1].ReferredToNames[0] + ".", Color.LightBlue, new EntityList<Entity>() { Subjects[1], GivenObject });


                    if (CanUnderstandEachOther(Subjects[1], Executor))
                    {
                        AddMessage(Subjects[1].ReferredToNames[0] + ": Thank you. I appreciate this.", Color.Pink, new EntityList<Entity>() { Subjects[1] });
                    }
                    else
                    {
                        AddMessage(Subjects[1].ReferredToNames[0] + ": *happy shibesque noises*", Color.Pink, new EntityList<Entity>() { Subjects[1] });
                    }

                    ((Architect)Subjects[1]).Inventory.Add(GivenObject);
                }
                else
                {
                    //subject 1 is an object

                    if (((Object)Subjects[1]).Type == "altar")
                    {
                        MakeObservation("You place your " + Subjects[0].ReferredToNames[0] + " on the " + Subjects[1].ReferredToNames[0] + ". It fizzles...", Color.Yellow, new EntityList<Entity>() { Subjects[0], Subjects[1] });

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
                        string OutcomeString = (new List<string>() { "reject", "reject", "reject", "reject", "reject", "coffee", "tea", "divineprotection", "double", "lightninggrenade", "icedtea", "icedcoffee", "spatialgrenade", "double", "heal", "double", "divinemight", "learnspell", "double", "convertmaterialtodivine", "divineartifact", "divineartifact", "divineweapon" })[Outcome];

                        Deity PrayingDeity;
                        if (LoadedArchitects[Game1.ArchitectIndex].Structure == null && LoadedArchitects[Game1.ArchitectIndex].Structure.Type == "shrine")
                        {
                            PrayingDeity = LoadedArchitects[Game1.ArchitectIndex].Structure.PrayingDeity;
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
                                    MakeObservation("...and absolutely nothing happens.", Color.Red, new EntityList<Entity>());
                                    break;
                                }
                            case "coffee":
                                {
                                    MakeObservation(PrayingDeity.Name + " has conjured for you a cup of coffee!", Color.Goldenrod, new EntityList<Entity>() { PrayingDeity });

                                    Object o = new Object(null, "small cup", new EntityList<Material>() { LoadedArchitects[Game1.ArchitectIndex].Location.HomeCivilization.CulturalStone }, PrayingDeity);
                                    o.ContainedObjects.Add(new Object(null, "drink", new EntityList<Material> { GameWorld.Coffee }, PrayingDeity));
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
                                    MakeObservation(PrayingDeity.Name + " has conjured for you a cup of tea!", Color.Goldenrod, new EntityList<Entity>() { PrayingDeity });

                                    Object o = new Object(null, "small cup", new EntityList<Material>() { LoadedArchitects[Game1.ArchitectIndex].Location.HomeCivilization.CulturalStone }, PrayingDeity);
                                    o.ContainedObjects.Add(new Object(null, "drink", new EntityList<Material> { GameWorld.Tea }, PrayingDeity));
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
                                    MakeObservation(PrayingDeity.Name + " offers you a barrier between the blades of your enemies!", Color.Goldenrod, new EntityList<Entity>() { PrayingDeity });
                                    Executor.DivineProtection += 5;
                                    break;
                                }
                            case "double":
                                {
                                    // Code for the 'double' case
                                    MakeObservation(PrayingDeity.Name + " has blessed your offering and doubled it!", Color.Goldenrod, new EntityList<Entity>() { PrayingDeity });
                                    Executor.Room.Objects.Add(new Object(GivenObject.Name, GivenObject.Type, GivenObject.Materials, GivenObject.IfTrueUseInIfFalseUseOn, GivenObject.IsContainer, GivenObject.CompositionContent, GivenObject.Creator, GivenObject.Weight, GivenObject.IsGeneralGood, GivenObject.Block, GivenObject.Structure, GivenObject.Room, GivenObject.IsWearable));
                                    Executor.Room.Objects.Add(new Object(GivenObject.Name, GivenObject.Type, GivenObject.Materials, GivenObject.IfTrueUseInIfFalseUseOn, GivenObject.IsContainer, GivenObject.CompositionContent, GivenObject.Creator, GivenObject.Weight, GivenObject.IsGeneralGood, GivenObject.Block, GivenObject.Structure, GivenObject.Room, GivenObject.IsWearable));
                                    break;
                                }
                            case "lightninggrenade":
                                {
                                    MakeObservation(PrayingDeity.Name + " has gifted you a strange sphere filled with lightning...", Color.Goldenrod, new EntityList<Entity>() { PrayingDeity });
                                    Executor.Room.Objects.Add(new Object(null, "lightning grenade", new EntityList<Material>() { GameWorld.Glass }, PrayingDeity));
                                    break;
                                }
                            case "spatialgrenade":
                                {
                                    MakeObservation(PrayingDeity.Name + " has gifted you a strange sphere filled with violet energy...", Color.Goldenrod, new EntityList<Entity>() { PrayingDeity });
                                    Executor.Room.Objects.Add(new Object(null, "spatial grenade", new EntityList<Material>() { GameWorld.Glass }, PrayingDeity));
                                    break;
                                }
                            case "icedcoffee":
                                {
                                    MakeObservation(PrayingDeity.Name + " has conjured for you a cup of iced coffee!", Color.Goldenrod, new EntityList<Entity>() { PrayingDeity });

                                    Object o = new Object(null, "small cup", new EntityList<Material>() { LoadedArchitects[Game1.ArchitectIndex].Location.HomeCivilization.CulturalStone }, PrayingDeity);
                                    o.ContainedObjects.Add(new Object(null, "drink", new EntityList<Material> { GameWorld.Coffee }, PrayingDeity));
                                    o.ContainedObjects.Add(new Object(null, "cube", new EntityList<Material> { GameWorld.Ices[r.Next(GameWorld.Ices.Count())] }, PrayingDeity));
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
                                    MakeObservation(PrayingDeity.Name + " has conjured for you a cup of iced tea!", Color.Goldenrod, new EntityList<Entity>() { PrayingDeity });

                                    Object o = new Object(null, "small cup", new EntityList<Material>() { LoadedArchitects[Game1.ArchitectIndex].Location.HomeCivilization.CulturalStone }, PrayingDeity);
                                    o.ContainedObjects.Add(new Object(null, "drink", new EntityList<Material> { GameWorld.Tea }, PrayingDeity));
                                    o.ContainedObjects.Add(new Object(null, "cube", new EntityList<Material> { GameWorld.Ices[r.Next(GameWorld.Ices.Count())] }, PrayingDeity));
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
                                    MakeObservation(PrayingDeity.Name + " envelops you in a beautiful energy wave, fully repairing your body!", Color.Goldenrod, new EntityList<Entity>() { PrayingDeity });
                                    foreach (Object o in Executor.BodyParts)
                                    {
                                        o.Integrity = 100;
                                    }
                                    Executor.Energy = Executor.MaxEnergy;
                                    break;
                                }
                            case "divinemight":
                                {
                                    // Code for the 'divinemight' case
                                    MakeObservation(PrayingDeity.Name + " offers you a burst of power against your mightiest foes!", Color.Goldenrod, new EntityList<Entity>() { PrayingDeity });
                                    Executor.DivineMight += 12;
                                    break;
                                }
                            case "learnspell":
                                {
                                    // Code for the 'learnspell' case
                                    MakeObservation(PrayingDeity.Name + " attempts to infuse magic into your being...", Color.Goldenrod, new EntityList<Entity>() { PrayingDeity });
                                    if (r.Next(1, 3) == 1 || GameWorld.DiscoveredSpells.Count() == 0)
                                    {
                                        var randomSpell = GameWorld.DiscoveredSpells[r.Next(GameWorld.DiscoveredSpells.Count())];
                                        if (!LoadedArchitects[Game1.ArchitectIndex].SpellsKnown.Contains(randomSpell))
                                        {
                                            MakeObservation("You feel a tremendous pain, followed by a strange, uplifting peace.", Color.Goldenrod, new EntityList<Entity>());
                                            LoadedArchitects[Game1.ArchitectIndex].SpellsKnown.Add(randomSpell);
                                        }
                                        else
                                        {
                                            MakeObservation("You feel a tremendous pain, followed by an intense feeling of dissatisfaction.", Color.Goldenrod, new EntityList<Entity>());
                                        }
                                    }
                                    else
                                    {
                                        MakeObservation("You feel a tremendous pain, followed by an intense feeling of dissatisfaction.", Color.Goldenrod, new EntityList<Entity>());
                                    }
                                    break;
                                }
                            case "convertmaterialtodivine":
                                {
                                    GivenObject.Materials.Clear();
                                    MakeObservation(PrayingDeity.Name + " alters your object into a brilliant form!", Color.Goldenrod, new EntityList<Entity>() { PrayingDeity });

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

                                    MakeObservation(PrayingDeity.Name + " reshapes your object into an incredible form!", Color.Goldenrod, new EntityList<Entity>() { PrayingDeity });

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
                                        Executor.Room.Objects.Add(Game1.GenerateRandomWeapon(WeaponMaterial, "rare"));
                                    }
                                    else
                                    {
                                        Executor.Block.Objects.Add(Game1.GenerateRandomWeapon(WeaponMaterial, "rare"));
                                    }

                                    break;
                                }
                            case "divineartifact":
                                {
                                    // Code for the 'divineweapon' case

                                    Material artifactMaterial;

                                    MakeObservation(PrayingDeity.Name + " reshapes your object into an indescribable form!", Color.Goldenrod, new EntityList<Entity>() { PrayingDeity });

                                    if (PrayingDeity == GameWorld.LightDeity)
                                    {
                                        artifactMaterial = GameWorld.Prismite;
                                    }
                                    else
                                    {
                                        artifactMaterial = GameWorld.Shadesteel;
                                    }

                                    Object o = Game1.GameWorld.MagicalSuperLoot(6);

                                    o.Materials.Clear();

                                    o.Materials.Add(artifactMaterial);

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

                            default:
                                {
                                    // Code for any other case that is not specifically handled
                                    break;
                                }
                        }

                    }
                    else
                    {
                        MakeObservation("You place your " + Subjects[0].ReferredToNames[0] + " on the " + Subjects[1].ReferredToNames[0] + " and wait, patiently. Nothing happens. You pick it back up.", Color.Yellow, new EntityList<Entity>() { Subjects[0], Subjects[1] });
                        Executor.Inventory.Add((Object)(Subjects[0]));
                    }
                }
            }
            else if (CommandID == "throw_item")
            {
                Object ThrowingObject = null;

                // Check if the item is in the inventory
                if (Executor.Inventory.Contains((Object)Subjects[0]))
                {
                    // Wield the item first
                    if (Executor.MainHeldObject == null)
                    {
                        Executor.MainHeldObject = (Object)Subjects[0];
                        MakeObservation("You take out the item with your dominant hand.", Color.Yellow, new EntityList<Entity>());
                    }
                    else if (Executor.OffHeldObject == null)
                    {
                        Executor.OffHeldObject = (Object)Subjects[0];
                        MakeObservation("You take out the item with your non-dominant hand.", Color.Yellow, new EntityList<Entity>());
                    }
                    else
                    {
                        MakeObservation("Your hands are full.", Color.Yellow, new EntityList<Entity>());
                        return false;
                    }

                    Executor.Inventory.Remove((Object)Subjects[0]);
                    Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed()));
                }

                if (Executor.MainHeldObject == Subjects[0])
                {
                    ThrowingObject = Executor.MainHeldObject;
                    Executor.MainHeldObject = null;
                }
                else if (Executor.OffHeldObject == Subjects[0])
                {
                    ThrowingObject = Executor.OffHeldObject;
                    Executor.OffHeldObject = null;
                }

                if (ThrowingObject == null)
                {
                    if (Executor.OffHeldObject == null && Executor.MainHeldObject == null)
                    {
                        Game1.Observations.Add(new TextStorage("Your hands are empty. You must have an object in your hands to throw it.", Color.Yellow, new EntityList<Entity>() { }));
                        Game1.Announcements.Add(new TextStorage("Your hands are empty. You must have an object in your hands to throw it.", Color.Yellow, new EntityList<Entity>() { }));
                    }
                    else
                    {
                        Game1.Observations.Add(new TextStorage("You do not have an object like that in your hands.", Color.Yellow, new EntityList<Entity>() { }));
                        Game1.Announcements.Add(new TextStorage("You do not have an object like that in your hands.", Color.Yellow, new EntityList<Entity>() { }));
                    }
                }
                else
                {
                    Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed()));
                    MakeObservation("You fling your " + Subjects[0].ReferredToNames[0] + " at nothing. Expectedly, it falls to the ground.", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                    Executor.Inventory.Remove(ThrowingObject);

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

            else if (CommandID == "craft")
            {
                var objectsToSearch = Executor.Room?.Objects ?? Executor.Block?.Objects;
                var forgeNearby = objectsToSearch?.FirstOrDefault(obj => obj.Type == "forge") != null;
                if (forgeNearby)
                {
                    Executor.Crafting = true;
                }
                else
                {
                    MakeObservation("You need to be near a forge to do that.", Color.Orange, new EntityList<Entity>());
                }

            }
            else if (CommandID == "throw_item_at")
            {
                Object ThrowingObject = null;

                // Check if the target is valid
                if (!(Subjects[1] is Architect || Subjects[1] is Object))
                {
                    MakeObservation("You can't throw at that.", Color.Yellow, new EntityList<Entity>());
                    return false;
                }

                if (Executor.OffHeldObject == Subjects[0])
                {
                    ThrowingObject = Executor.OffHeldObject;
                    Executor.OffHeldObject = null;
                }
                else if (Executor.MainHeldObject == Subjects[0])
                {
                    ThrowingObject = Executor.MainHeldObject;
                    Executor.MainHeldObject = null;
                }
                else
                {
                    // Wield the item if it is not already in hand
                    if (Executor.Inventory.Contains(Subjects[0]))
                    {
                        if (Executor.OffHeldObject == null)
                        {
                            Executor.OffHeldObject = (Object)Subjects[0];
                            ThrowingObject = Executor.OffHeldObject;
                            Executor.Inventory.Remove((Object)Subjects[0]);
                            MakeObservation("You wield the " + Subjects[0].ReferredToNames[0] + " in your left hand.", Color.Yellow, new EntityList<Entity>());
                        }
                        else if (Executor.MainHeldObject == null)
                        {
                            Executor.MainHeldObject = (Object)Subjects[0];
                            ThrowingObject = Executor.MainHeldObject;
                            Executor.Inventory.Remove((Object)Subjects[0]);
                            MakeObservation("You wield the " + Subjects[0].ReferredToNames[0] + " in your right hand.", Color.Yellow, new EntityList<Entity>());
                        }
                        else
                        {
                            MakeObservation("You need to have an open hand to pull out the " + Subjects[0].ReferredToNames[0] + " and throw it.", Color.Yellow, new EntityList<Entity>());
                            return false;
                        }
                        // Apply cooldown for wielding
                        Executor.CooldownCycles += (int)(Math.Round((15 - Executor.Dexterity) / Executor.Speed()));
                    }
                    else
                    {
                        MakeObservation("The specified object is not in your inventory.", Color.Yellow, new EntityList<Entity>());
                        return false;
                    }
                }

                // Apply cooldown for throwing
                Executor.CooldownCycles += (int)(Math.Round((15 - Executor.Dexterity) / Executor.Speed()));

                MakeObservation("You throw the " + Subjects[0].ReferredToNames[0] + "...", Color.Yellow, new EntityList<Entity>());

                // Handle the logic for throwing at an architect or object
                if (Subjects[1] is Architect targetArchitect)
                {
                    Object targetBodyPart = targetArchitect.BodyParts[r.Next(targetArchitect.BodyParts.Count())];
                    ((Object)Subjects[0]).AirborneTarget = targetBodyPart;
                    MakeObservation("You aim at the " + targetArchitect.Name + "'s " + targetBodyPart.Type + ".", Color.Yellow, new EntityList<Entity>());
                }
                else if (Subjects[1] is Object targetObject)
                {
                    ((Object)Subjects[0]).AirborneTarget = targetObject;
                    MakeObservation("You aim at the " + targetObject.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>());
                }

                ((Object)Subjects[0]).AirborneCyclesToHitTarget = Math.Max(1, r.Next(12, 20) - Executor.Dexterity);
                ((Object)Subjects[0]).Thrower = Executor;
                ((Object)Subjects[0]).AirbornePower = Executor.Dexterity + Executor.GetDistance(Subjects[0]) + 3;

                Executor.ChangeXP("throwing", r.Next(1, 4));

                // Remove the throwing object from the hand it was thrown
                if (ThrowingObject != null)
                {
                    if (Executor.OffHeldObject == ThrowingObject)
                    {
                        Executor.OffHeldObject = null;
                    }
                    else if (Executor.MainHeldObject == ThrowingObject)
                    {
                        Executor.MainHeldObject = null;
                    }
                }

                if (Executor.Structure != null)
                {
                    Executor.Room.Objects.Add((Object)Subjects[0]);
                }
                else
                {
                    Executor.Block.Objects.Add((Object)Subjects[0]);
                }
            }

            else if (CommandID.StartsWith("cast_spell_at"))
            {
                int numSubjects = int.Parse(CommandID.Last().ToString());  // Get the final number
                if (Executor.SpellsKnown.Contains(Subjects[0]) ||
                    (Executor.OffHeldObject != null && Executor.OffHeldObject.SpecialKnowledge == Subjects[0]) ||
                    (Executor.MainHeldObject != null && Executor.MainHeldObject.SpecialKnowledge == Subjects[0]))
                {
                    Entity Spell = Subjects[0];
                    Subjects.RemoveAt(0);

                    EntityList<Entity> Targets = new EntityList<Entity>();

                    for (int i = 0; i < numSubjects && i < Subjects.Count(); i++)
                    {
                        Entity e = Subjects[i];

                        // Add spells that can be casted at literally anything to the list below
                        if ((e is Object || e is Architect) || Spell.Metadata == "expunge" || ((e is Object || e is Structure || e is Architect) && Spell.Metadata == "liquify"))
                        {
                            Targets.Add(e);
                        }
                        else
                        {
                            MakeObservation(Spell.Metadata + " cannot be casted at " + e.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>());
                        }
                    }

                    if (Targets.Count() != 0)
                    {
                        Game1.Announcements.AddRange(Executor.CastSpell(Spell.Metadata, Targets));
                    }
                    else
                    {
                        Game1.Observations.Add(new TextStorage("You couldn't find a sufficient target. Most spells can only target architects and objects.", Color.Yellow, new EntityList<Entity>() { }));
                        Game1.Announcements.Add(new TextStorage("You couldn't find a sufficient target. Most spells can only target architects and objects.", Color.Yellow, new EntityList<Entity>() { }));
                    }
                }
                else
                {
                    MakeObservation("You don't know or wield a spell like that.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "cast_spell")
            {
                MakeObservation("You fail to concentrate. You will need a point of interest to cast the spell at, even if unused.", Color.Yellow, new EntityList<Entity>());
            }
            else if (CommandID == "consume")
            {
                bool FoundObject = false;

                if (Executor.OffHeldObject == (Subjects[0]))
                {
                    FoundObject = true;
                    Executor.OffHeldObject = null;
                }
                else if (Executor.MainHeldObject == (Subjects[0]))
                {
                    FoundObject = true;
                    Executor.MainHeldObject = null;
                }
                else if (Executor.Inventory.Contains(Subjects[0]))
                {
                    FoundObject = true;
                    Executor.Inventory.Remove((Object)Subjects[0]);
                }

                if (FoundObject)
                {
                    Object EatingObject = ((Object)Subjects[0]);

                    // Check if the object is a container
                    if (EatingObject.IsContainer)
                    {
                        if (EatingObject.ContainedObjects.Count() > 0)
                        {
                            // Consume each contained object
                            foreach (var containedObject in EatingObject.ContainedObjects)
                            {
                                ConsumeObject(containedObject, Executor);
                            }
                        }
                        else
                        {
                            // Consume the container itself if empty
                            ConsumeObject(EatingObject, Executor);
                        }
                    }
                    else
                    {
                        // Normal consume logic for non-container objects
                        ConsumeObject(EatingObject, Executor);
                    }
                }
                else
                {
                    MakeObservation("You don't have anything like that.", Color.Yellow, new EntityList<Entity>());
                }


                // Helper method to handle the consumption logic
                void ConsumeObject(Object EatingObject, Architect Executor)
                {
                    Executor.CooldownCycles += (int)Math.Round(10 * Executor.Speed());

                    if (EatingObject.Type == "salve")
                    {
                        MakeObservation("You apply the salve. The pain begins to vanish.", Color.Yellow, new EntityList<Entity>());
                        Executor.Pain = Math.Max(0, Executor.Pain - 35);
                        Executor.Bleeding = Math.Max(0, Executor.Bleeding - 2);
                        Executor.Energy += 5;
                    }
                    else if (EatingObject.Type == "bandage")
                    {
                        MakeObservation("You apply the bandage. Your bleeding slows.", Color.Yellow, new EntityList<Entity>());
                        Executor.Pain = Math.Max(0, Executor.Pain - 5);
                        Executor.Bleeding = (int)Math.Round(Math.Max(0, Executor.Bleeding * 0.3m));
                    }
                    else if (EatingObject.Type == "vial")
                    {
                        MakeObservation("You drink the vial. You feel energized.", Color.Yellow, new EntityList<Entity>());
                        Executor.Pain = Math.Max(0, Executor.Pain - 5);
                        Executor.Energy += Math.Max(0, 50 + r.Next(-10, 11));
                        Executor.DaysSinceLiquid = 0;
                    }
                    else if (EatingObject.Type == "portion" || EatingObject.Type == "drink" || EatingObject.Type == "cube")
                    {
                        MakeObservation("You consume the " + EatingObject.Materials[0].Name + " " + EatingObject.Type + ", and recover some energy.", Color.Yellow, new EntityList<Entity>());

                        if (EatingObject.Materials[0].Name == "coffee" || EatingObject.Materials[0].Name == "tea")
                        {
                            Executor.DaysSinceCoffeeOrTea = 0;
                            Executor.DaysSinceLiquid = 0;
                        }

                        Executor.Energy += 5;
                    }
                    else if (EatingObject.Type == "fragment")
                    {
                        MakeObservation("You eat the fragment. You feel ready for the day.", Color.Yellow, new EntityList<Entity>());
                        Executor.Energy += 5;
                        Executor.DaysSinceFood = 0;
                    }
                    else
                    {
                        MakeObservation("You consume the " + EatingObject.ReferredToNames[0] + ". You don't feel so great...", Color.Yellow, new EntityList<Entity>());
                        Executor.Energy /= 2;
                        Executor.DaysSinceFood = 0;
                    }
                }
            }

            else if (CommandID == "recall_information")
            {
                if (Subjects[0].Metadata == "spells")
                {
                    MakeObservation("Spells Known:", Color.LightBlue, new EntityList<Entity>());

                    if (Executor.SpellsKnown.Count() == 0)
                    {
                        MakeObservation("You know no spells.", Color.Yellow, new EntityList<Entity>());
                    }
                    else
                    {
                        foreach (Entity s in Executor.SpellsKnown)
                        {
                            MakeObservation(s.Metadata, Color.Aqua, new EntityList<Entity>() { new Entity(s.Metadata) });
                            MakeObservation(Game1.SkillSpellDescriptions[s.Metadata], Color.LightCyan, new EntityList<Entity>() { new Entity(s.Metadata) });
                        }
                    }
                }
                else if (Subjects[0].Metadata == "skills")
                {
                    MakeObservation("Skills Known:", Color.LightBlue, new EntityList<Entity>());

                    if (Executor.SkillsKnown.Count() == 0)
                    {
                        MakeObservation("You know no skills.", Color.Yellow, new EntityList<Entity>());
                    }
                    else
                    {
                        foreach (Entity s in Executor.SkillsKnown)
                        {
                            MakeObservation(s.Metadata, Color.Aqua, new EntityList<Entity>() { new Entity(s.Metadata) });
                            MakeObservation(Game1.SkillSpellDescriptions[s.Metadata], Color.LightCyan, new EntityList<Entity>() { new Entity(s.Metadata) });
                        }
                    }
                }
                else
                {
                    MakeObservation("Use this command to list either your spells or skills.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "reposition")
            {
                MakeObservation("You reposition all of your limbs.", Color.MediumPurple, new EntityList<Entity>());

                Executor.CooldownCycles += (int)Math.Round(3 * Executor.Speed());

                foreach (Object o in Executor.BodyParts)
                {
                    var textStorage = o.UpdateExposure(-(15 + Executor.Dexterity));
                    if (textStorage != null)
                    {
                        MakeObservation(textStorage.Data, textStorage.Color, textStorage.Entities);
                    }
                }
            }
            else if (CommandID == "retract")
            {
                Executor.CooldownCycles += (int)Math.Round(3 * Executor.Speed());

                Object bodyPart = null;

                if (Executor.BodyParts.Contains(Subjects[0]))
                {
                    bodyPart = (Object)Subjects[0];
                }
                else
                {
                    bodyPart = Executor.FindBodyPart(Subjects[0].Metadata);
                }

                if (bodyPart != null)
                {
                    var textStorage = bodyPart.UpdateExposure(-100);
                    if (textStorage != null)
                    {
                        MakeObservation(textStorage.Data, textStorage.Color, textStorage.Entities);
                    }

                    MakeObservation("You reposition your " + Subjects[0].Metadata + ".", Color.MediumPurple, new EntityList<Entity>() { Subjects[0] });
                }
                else
                {
                    MakeObservation("You don't have one of those.", Color.MediumPurple, new EntityList<Entity>());
                }
            }
            else if (CommandID == "free")
            {
                if (Subjects[0] is Architect a && ArchitectsToUse.Contains(a))
                {
                    if (a.Bound)
                    {
                        MakeObservation("You free " + a.ReferredToNames[0] + " from their bondage.", Color.Green, new EntityList<Entity>());

                        // Create a list of possible responses
                        List<string> possibleResponses = new List<string>
                        {
                            a.ReferredToNames[0] + ": Thank you! I'm " + a.Name + ". I'm not sure how I got here.",
                            a.ReferredToNames[0] + ": I can't believe I'm free! My name is " + a.Name + ". How can I ever repay you?",
                            a.ReferredToNames[0] + ": Finally, freedom! I am called " + a.Name + ". You have my eternal gratitude."
                        };

                        // Choose a random response
                        
                        int index = Game1.r.Next(possibleResponses.Count());

                        a.Bound = false;
                        a.CyclesLeftInTask = 0;
                        a.Task = "";

                        MakeObservation(possibleResponses[index], Color.Green, new EntityList<Entity>());
                    }
                    else
                    {
                        MakeObservation("There is nothing to free " + a.ReferredToNames[0] + " from.", Color.Green, new EntityList<Entity>());
                    }
                }
            }

            else if (CommandID == "read_object")
            {
                // Check for the specified object in both hands and inventory
                Object objectToRead = null;
                if (Executor.MainHeldObject != null && Executor.MainHeldObject == Subjects[0])
                {
                    objectToRead = Executor.MainHeldObject;
                }
                else if (Executor.OffHeldObject != null && Executor.OffHeldObject == Subjects[0])
                {
                    objectToRead = Executor.OffHeldObject;
                }
                else if (Executor.Inventory.Any(item => item == Subjects[0]))
                {
                    objectToRead = Executor.Inventory.First(item => item == Subjects[0]);
                }

                if (objectToRead != null)
                {
                    if (objectToRead.CompositionContent != null)
                    {
                        // Object has composition content
                        MakeObservation("You read " + objectToRead.ReferredToNames[0] + ". " + objectToRead.CompositionContent.GetCompleteWorkDescription(), Color.Honeydew, new EntityList<Entity>() { objectToRead });

                        int contentLength = objectToRead.CompositionContent.Sections.Count();
                        Executor.CooldownCycles += (int)(Math.Round((125 * contentLength) / Executor.Speed()));

                        if (objectToRead.SpecialKnowledge != null)
                        {
                            if (GameWorld.AllSpells.Contains(objectToRead.SpecialKnowledge))
                            {
                                MakeObservation("You learned the spell \"" + objectToRead.SpecialKnowledge.Metadata + "\"!", Color.LightBlue, new EntityList<Entity>() { objectToRead.SpecialKnowledge });
                                MakeObservation(Game1.SkillSpellDescriptions[objectToRead.SpecialKnowledge.Metadata], Color.LightBlue, new EntityList<Entity>());
                                if (!Executor.SpellsKnown.Contains(objectToRead.SpecialKnowledge))
                                {
                                    Executor.SpellsKnown.Add(objectToRead.SpecialKnowledge);
                                }
                            }
                            else if (GameWorld.AllSkills.Contains(objectToRead.SpecialKnowledge))
                            {
                                MakeObservation("You learned the skill \"" + objectToRead.SpecialKnowledge.Metadata + "\"!", Color.LightBlue, new EntityList<Entity>() { objectToRead.SpecialKnowledge });
                                MakeObservation(Game1.SkillSpellDescriptions[objectToRead.SpecialKnowledge.Metadata], Color.LightBlue, new EntityList<Entity>());

                                if (!Executor.SkillsKnown.Contains(objectToRead.SpecialKnowledge))
                                {
                                    Executor.SkillsKnown.Add(objectToRead.SpecialKnowledge);
                                }

                                // Check and warn if learning skills past 3
                                if (Executor.SkillsKnown.Count() >= 3)
                                {
                                    MakeObservation("Learning additional skills past 3 will replace older skills.", Color.OrangeRed, new EntityList<Entity>());
                                }

                                // Discard old skills if more than 3
                                while (Executor.SkillsKnown.Count() > 3)
                                {
                                    MakeObservation("Your mind is too unfocused for " + Executor.SkillsKnown[0] + ".", Color.OrangeRed, new EntityList<Entity>() { Executor.SkillsKnown[0] });
                                    Executor.SkillsKnown.RemoveAt(0);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (objectToRead.SpecialKnowledge != null)
                        {
                            MakeObservation("You read the " + objectToRead.ReferredToNames[0] + ". It contains detailed instructions on how to perform the skill " + objectToRead.SpecialKnowledge.Metadata + ".", Color.SkyBlue, new EntityList<Entity>() { objectToRead });
                            MakeObservation(Game1.SkillSpellDescriptions[objectToRead.SpecialKnowledge.Metadata], Color.SkyBlue, new EntityList<Entity>() { objectToRead });
                            if (!Executor.SkillsKnown.Contains(objectToRead.SpecialKnowledge))
                            {
                                Executor.SkillsKnown.Add(objectToRead.SpecialKnowledge);
                            }

                            // Check and warn if learning skills past 3
                            if (Executor.SkillsKnown.Count() >= 3)
                            {
                                MakeObservation("Learning additional skills past 3 will replace older skills.", Color.OrangeRed, new EntityList<Entity>());
                            }

                            // Discard old skills if more than 3
                            while (Executor.SkillsKnown.Count() > 3)
                            {
                                MakeObservation("Your mind is too unfocused for " + Executor.SkillsKnown[0] + ".", Color.OrangeRed, new EntityList<Entity>() { Executor.SkillsKnown[0] });
                                Executor.SkillsKnown.RemoveAt(0);
                            }
                        }
                        else
                        {
                            if (objectToRead.LetterContent != null)
                            {
                                MakeObservation("You read the letter: " + objectToRead.LetterContent.Text.Data, Color.Cyan, objectToRead.LetterContent.Text.Entities);
                            }
                            else
                            {
                                MakeObservation("You look over the " + objectToRead.ReferredToNames[0] + ", but it has nothing written on it.", Color.LightBlue, new EntityList<Entity>() { objectToRead });
                            }
                        }

                    }
                }
                else
                {
                    MakeObservation("You don't have " + Subjects[0] + " in your hands or inventory.", Color.Red, new EntityList<Entity>() { Subjects[0] });
                }
            }

            else if (CommandID == "perform_composition")
            {
                if (Subjects[0] is Composition compositionToPerform)
                {
                    string action = compositionToPerform.Type == "song" ? "sing" : "recite";
                    MakeObservation($"You {action} " + compositionToPerform.Name + ". " + compositionToPerform.GetCompleteWorkDescription(), Color.LightBlue, new EntityList<Entity>() { Subjects[0] });

                    // Determine the list of architects based on the location of the Executor
                    var architects = Executor.Room == null ? Executor.Block.Architects : Executor.Room.Architects;

                    // Randomly select a subset of architects to react, between 1 and 6
                    int numReactions = Math.Min(Game1.r.Next(1, 7), architects.Count());
                    EntityList<Architect> reactingArchitects = architects.ShuffleNew().Take(numReactions);


                    // React to performance in the vicinity
                    foreach (var architect in reactingArchitects)
                    {
                        if (architect != Executor && Game1.GameWorld.HumanoidRaces.Contains(architect.Race))
                        {
                            int randomModifier = Game1.r.Next(-2, 3);  // Random number from -2 to 2
                            int score = Executor.Charisma + randomModifier;
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
                            AddMessage(architect.ReferredToNames[0] + ": " + reaction, Color.Magenta, new EntityList<Entity>() { architect });
                        }
                    }
                }
                else
                {
                    MakeObservation("You do not remember a composition named " + Subjects[0].Name + ".", Color.Red, new EntityList<Entity>() { Subjects[0] });
                }
            }
            else if (CommandID == "write_composition")
            {
                // Decide on type based on the subject provided
                string type = Subjects[0].Metadata.ToLower();
                if (type == "book")
                {
                    // Find a writable object for books
                    Object writableObject = Executor.MainHeldObject != null && Executor.MainHeldObject.IsWritable && Executor.MainHeldObject.CompositionContent == null
                                                ? Executor.MainHeldObject
                                                : Executor.Inventory.FirstOrDefault(item => item.IsWritable && item.CompositionContent == null);

                    if (writableObject == null)
                    {
                        MakeObservation("You have nothing suitable for writing in your hands or inventory.", Color.Red, new EntityList<Entity>());
                    }
                    else
                    {
                        // Create a new Composition without a specific domain
                        Composition newComposition = new Composition(type, Executor, Executor);
                        writableObject.CompositionContent = newComposition;
                        writableObject.Name = newComposition.Name; // Assign the generated book name to the object

                        // Log historical event
                        Game1.GameWorld.HistoricalEvents.Add(new Event($"{Date} {Executor.Name} authored a book titled '{newComposition.Name}' in {Executor.Location.Name}.", Executor.Location.Region, new EntityList<Entity>() { Executor, newComposition, Executor.Location }));

                        // Provide detailed feedback to the user
                        MakeObservation($"You write a book titled '{newComposition.Name}' on yourself, because nothing else comes to mind apparently. {newComposition.GetCompleteWorkDescription()} It is now stored in your {writableObject.Name}.", Color.Blue, new EntityList<Entity>() { newComposition, writableObject });
                    }
                }
                else if (type == "poem" || type == "song")
                {
                    // Create a new Composition in memory for poems or songs
                    Composition newComposition = new Composition(type, Executor, Executor);
                    Executor.CultureBank.Add(newComposition); // Assuming Executor has a CultureBank list to store compositions

                    // Log historical event
                    Game1.GameWorld.HistoricalEvents.Add(new Event($"{Date} {Executor.Name} composed a {type} titled '{newComposition.Name}' in {Executor.Location.Name}.", Executor.Location.Region, new EntityList<Entity>() { Executor, newComposition }));

                    // Provide detailed feedback to the user
                    MakeObservation($"You compose a {type} titled '{newComposition.Name}. {newComposition.GetCompleteWorkDescription()}. It is now stored in your memory.", Color.LightBlue, new EntityList<Entity>() { newComposition });
                }
                else
                {
                    MakeObservation("You can only write poems, books, or songs.", Color.Blue, new EntityList<Entity>());
                }
            }

            else if (CommandID == "write_about_topic")
            {
                string type = Subjects[0].Metadata; // This should be either "book", "poem", or "song"

                if (type == "book")
                {
                    Object writableObject = Executor.MainHeldObject != null && Executor.MainHeldObject.IsWritable && Executor.MainHeldObject.CompositionContent == null
                                ? Executor.MainHeldObject
                                : Executor.OffHeldObject != null && Executor.OffHeldObject.IsWritable && Executor.OffHeldObject.CompositionContent == null
                                    ? Executor.OffHeldObject
                                    : Executor.Inventory.FirstOrDefault(item => item.IsWritable && item.CompositionContent == null);

                    if (writableObject == null)
                    {
                        MakeObservation("You have nothing suitable for writing in your hands or inventory.", Color.Red, new EntityList<Entity>());
                    }
                    else
                    {
                        // Create a new Composition with a specific domain
                        Composition newComposition = new Composition(type, Executor, Subjects[1]);
                        writableObject.CompositionContent = newComposition;
                        writableObject.Name = newComposition.Name; // Assign the generated book name to the object

                        // Log historical event
                        Game1.GameWorld.HistoricalEvents.Add(new Event($"{Date} {Executor.Name} authored a book titled '{newComposition.Name}' about {Subjects[1].ReferredToNames[0]} in {Executor.Location.Name}.", Executor.Location.Region, new EntityList<Entity>() { Executor, newComposition, Subjects[1], Executor.Location }));

                        // Provide detailed feedback to the user
                        MakeObservation($"You write a book titled '{newComposition.Name}' about {Subjects[1].ReferredToNames[0]}. {newComposition.GetCompleteWorkDescription()}. It is now stored in your {writableObject.Name}.", Color.LightBlue, new EntityList<Entity>() { newComposition, Subjects[1], writableObject });
                    }
                }
                else if (type == "poem" || type == "song")
                {
                    // Create a new Composition in memory for poems or songs
                    Composition newComposition = new Composition(type, Executor, Subjects[1]);
                    Executor.CultureBank.Add(newComposition); // Assuming Executor has a CultureBank list to store compositions

                    // Log historical event
                    Game1.GameWorld.HistoricalEvents.Add(new Event($"{Date} {Executor.Name} composed a {type} titled '{newComposition.Name}' about {Subjects[1].ReferredToNames[0]} in {Executor.Location.Name}.", Executor.Location.Region, new EntityList<Entity>() { Executor, newComposition, Subjects[1], Executor.Location }));

                    // Provide detailed feedback to the user
                    MakeObservation($"You compose a {type} titled '{newComposition.Name}' about {Subjects[1].ReferredToNames[0]}. {newComposition.GetCompleteWorkDescription()}. It is now stored in your memory.", Color.LightBlue, new EntityList<Entity>() { newComposition, Subjects[1] });
                }
                else
                {
                    MakeObservation("You can only write poems, books, or songs.", Color.Blue, new EntityList<Entity>());
                }
            }
            else if (CommandID.StartsWith("write ~ about "))
            {
                MakeObservation("You can't write about that because either it or the art form doesn't exist, or you don't care enough about one or the other.", Color.Red, new EntityList<Entity>());
            }
            else if (CommandID == "sneak")
            {
                MakeObservation("You start moving slower and less noticeably.", Color.Orange, new EntityList<Entity>());
                Executor.MovementMode = "sneaking";
            }
            else if (CommandID == "walk")
            {
                MakeObservation("You start moving regularly.", Color.Orange, new EntityList<Entity>());
                Executor.MovementMode = "walking";
            }
            else if (CommandID == "sprint")
            {
                MakeObservation("You start moving quickly, consuming more energy.", Color.Orange, new EntityList<Entity>());
                Executor.MovementMode = "sprinting";
            }
            else if (CommandID == "assemble_trap")
            {
                Executor.CooldownCycles += (int)(Math.Round(100 / Executor.Speed()));
                if (Subjects[0] is Object o)
                {
                    if (o.Materials[0].Type == "fiber")
                    {
                        bool Found = false;

                        if(Executor.MainHeldObject == o)
                        {
                            Executor.MainHeldObject = null;
                            Found = true;
                        }
                        else if (Executor.OffHeldObject == o)
                        {
                            Executor.OffHeldObject = null;
                            Found = true;
                        }
                        else if (Executor.Inventory.Contains(o))
                        {
                            Executor.Inventory.Remove(o);
                            Found = true;
                        }

                        if(Found)
                        {
                            EntityList<Object> ObjList = Executor.Room != null ? Executor.Room.ObjectsToAdd : Executor.Block.ObjectsToAdd;
                            EntityList<Architect> ArchList = Executor.Room != null ? Executor.Room.Architects : Executor.Block.Architects;

                            Object Trap = new Object(null, "rope trap", new EntityList<Material>() { o.Materials[0] }, Executor);
                            Trap.Block = Executor.Block;
                            if (Executor.Room != null)
                            {
                                Trap.Room = Executor.Room;
                            }

                            Trap.AwareArchitects = new EntityList<Architect>(ArchList);

                            ObjList.Add(Trap);
                            MakeObservation("You rig up a trap. It will trigger when someone unaware of its existence enters the room.", Color.Orange, new EntityList<Entity>());

                        }
                        else
                        {
                            MakeObservation("You need to have fiber to set a trap.", Color.Orange, new EntityList<Entity>());
                        }
                    }
                    else
                    {
                        MakeObservation("You need to use fiber to set a trap.", Color.Orange, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You need fiber to set a trap.", Color.Orange, new EntityList<Entity>());
                }
            }
            else if (CommandID == "repair")
            {
                Executor.CooldownCycles += (int)(Math.Round(100 / Executor.Speed()));

                if (Subjects[0] is Object o && Subjects[1] is Object O && !o.IsBodyPart && !O.IsBodyPart)
                {
                    if (Executor.Inventory.Contains(o) && (Executor.MainHeldObject == O || Executor.OffHeldObject == O || Executor.Inventory.Contains(O)))
                    {
                        if (o.Integrity >= 100)
                        {
                            MakeObservation(o.ReferredToNames[0] + " is already fully repaired.", Color.Orange, new EntityList<Entity>());
                        }
                        else if (O.Materials[0] == o.Materials[0])
                        {
                            bool Found = false;

                            if (Executor.MainHeldObject == O)
                            {
                                Executor.MainHeldObject = null;
                                Found = true;
                            }
                            else if (Executor.OffHeldObject == O)
                            {
                                Executor.OffHeldObject = null;
                                Found = true;
                            }
                            else if (Executor.Inventory.Contains(O))
                            {
                                Executor.Inventory.Remove(O);
                                Found = true;
                            }

                            if (Found)
                            {
                                o.Integrity = 100;
                                MakeObservation(o.ReferredToNames[0] + " has been repaired.", Color.Green, new EntityList<Entity>());
                            }
                        }
                        else
                        {
                            MakeObservation("The objects must share core materials.", Color.Orange, new EntityList<Entity>());
                        }
                    }
                    else
                    {
                        MakeObservation("You must have both objects in your inventory or hands to repair.", Color.Orange, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You can only repair non-body-part objects.", Color.Orange, new EntityList<Entity>());
                }
            }
            else if (CommandID == "hide")
            {
                Entity HidingEntity = Subjects[0];
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed()));

                Executor.HideValue = 0;
                string announcement;

                if (HidingEntity is Structure s)
                {
                    Executor.HideValue += 40;
                    announcement = Executor.HideValue >= 40
                        ? "You effectively use the structure to conceal yourself."
                        : "The structure provides limited cover, leaving you partially exposed.";
                }
                else if (HidingEntity is Object o)
                {
                    Executor.HideValue += (int)Math.Round((decimal)(o.Weight / 100));
                    announcement = Executor.HideValue > 20
                        ? "You find decent concealment using the object."
                        : "The object offers minimal hiding potential.";
                }
                else if (HidingEntity is Architect a)
                {
                    int sizeDifference = Game1.SizeDifference(a.Race.Size, Executor.Race.Size);

                    if (sizeDifference > 0)
                    {
                        Executor.HideValue += 20 * sizeDifference;
                        MakeObservation("You hide behind " + a.ReferredToNames[0] + ".", Color.Orange, new EntityList<Entity>());
                        announcement = Executor.HideValue >= 40
                            ? "The architect's presence provides strong concealment."
                            : "You find moderate concealment behind this individual.";
                    }
                    else
                    {
                        MakeObservation("You try to hide behind " + a.ReferredToNames[0] + ", but it doesn't have much of an effect.", Color.Orange, new EntityList<Entity>());
                        announcement = "Your attempt to hide is ineffective due to size differences.";
                    }
                }
                else
                {
                    announcement = "You cannot hide here.";
                }

                // Announcement based on the HideValue
                if (Executor.HideValue >= 50)
                {
                    announcement += " You feel completely hidden.";
                }
                else if (Executor.HideValue >= 30)
                {
                    announcement += " You are somewhat hidden but remain visible to careful observers.";
                }
                else
                {
                    announcement += " You remain highly visible.";
                }

                MakeObservation(announcement, Color.LimeGreen, new EntityList<Entity>());
            }


            else if (CommandID == "bury_something")
            {
                if (Executor.Room == null)
                {
                    if (Subjects[0] is Architect a && a.Block == Executor.Block)
                    {
                        if((a.IsAlive == false || a.UnconsciousCycles > 0) && a.Block == Executor.Block)
                        {
                            a.IsAlive = false;
                            Executor.CooldownCycles += (int)(Math.Round(100 / Executor.Speed()));

                            MakeObservation("You bury " + a.ReferredToNames[0] + ".", Color.Orange, new EntityList<Entity>());

                            a.Block.Architects.Remove(a);
                            a.Block.BuriedArchitects.Add(a);
                        }
                        else
                        {
                            MakeObservation("They would probably notice...", Color.Orange, new EntityList<Entity>());
                        }
                    }
                    else if (Subjects[0] is Object o && o.Block == Executor.Block)
                    {
                        MakeObservation("You bury " + o.ReferredToNames[0] + ".", Color.Orange, new EntityList<Entity>());
                        Executor.CooldownCycles += (int)(Math.Round(100 / Executor.Speed()));
                        o.Block.BuriedObjects.Add(o);
                        o.Block.Objects.Remove(o);
                    }
                    else
                    {
                        MakeObservation("You can only bury an object or person. Set it/them on the ground first.", Color.Orange, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You need to be outside.", Color.Orange, new EntityList<Entity>());
                }
            }
            else if (CommandID == "dig")
            {
                if (Executor.Room == null)
                {
                    if (Executor.Block.BuriedArchitects.Count > 0 || Executor.Block.BuriedObjects.Count > 0)
                    {
                        // Return all buried architects to the surface
                        foreach (Architect buriedArchitect in Executor.Block.BuriedArchitects)
                        {
                            Executor.Block.Architects.Add(buriedArchitect);
                            Executor.CooldownCycles += (int)(Math.Round(100 / Executor.Speed()));
                            MakeObservation("You dig up " + buriedArchitect.ReferredToNames[0] + ".", Color.Orange, new EntityList<Entity>());
                        }
                        Executor.Block.BuriedArchitects.Clear();

                        // Return all buried objects to the surface
                        foreach (Object buriedObject in Executor.Block.BuriedObjects)
                        {
                            Executor.Block.Objects.Add(buriedObject);
                            Executor.CooldownCycles += (int)(Math.Round(100 / Executor.Speed()));
                            MakeObservation("You dig up " + buriedObject.ReferredToNames[0] + ".", Color.Orange, new EntityList<Entity>());
                        }
                        Executor.Block.BuriedObjects.Clear();
                    }
                    else
                    {
                        MakeObservation("There is nothing buried here to dig up.", Color.Orange, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You need to be outside to dig.", Color.Orange, new EntityList<Entity>());
                }
            }
            else if (CommandID == "climb")
            {
                if (Subjects[0] is Structure s)
                {
                    if (Executor.Room != null)
                    {
                        MakeObservation("You need to be outside.", Color.Orange, new EntityList<Entity>());
                    }
                    else if (s.Block == Executor.Block)
                    {
                        Executor.CooldownCycles += (int)(Math.Round(40 / Executor.Speed()));

                        MakeObservation("You climb atop " + s.Name + ".", Color.Green, new EntityList<Entity>());
                        Executor.OnTopOfStructure = s;
                        Executor.YLevelInFeet += 30;
                    }
                    else
                    {
                        MakeObservation("There is not a structure like that nearby.", Color.Orange, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You can only climb structures.", Color.Orange, new EntityList<Entity>());
                }
            }
            else if (CommandID == "get_down")
            {
                if(Executor.OnTopOfStructure != null)
                {
                    Executor.CooldownCycles += (int)(Math.Round(40 / Executor.Speed()));
                    MakeObservation("You climb down from " + Executor.OnTopOfStructure.Name + ".", Color.Green, new EntityList<Entity>());
                    Executor.OnTopOfStructure = null;
                    Executor.YLevelInFeet = 0;
                }
                else
                {
                    MakeObservation("You are not on top of a structure.", Color.Orange, new EntityList<Entity>());
                }
            }
            else if (CommandID == "carve")
            {
                Executor.CooldownCycles += (int)(Math.Round(100 / Executor.Speed()));

                Entity Image = Subjects[0];

                if (Subjects[1] is Object Obj)
                {
                    bool isInSameBlockOrRoomOrInventory =
                        Obj.Block == Executor.Block ||
                        Obj.Room == Executor.Room ||
                        Executor.Inventory.Contains(Obj);

                    if (isInSameBlockOrRoomOrInventory)
                    {
                        MakeObservation("You carve a symbol of " + Image.ReferredToNames[0] + " into " + Obj.ReferredToNames[0] + ".", Color.Green, new EntityList<Entity>());
                        Obj.CarvedSymbols.Add(Image);
                    }
                    else
                    {
                        MakeObservation("The object must be in the same block, room, or your inventory to carve it.", Color.Red, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You can only carve symbols into objects.", Color.Red, new EntityList<Entity>());
                }
            }

            else if (CommandID == "sculpt")
            {
                Executor.CooldownCycles += (int)(Math.Round(100 / Executor.Speed()));

                Entity Image = Subjects[0];

                if (Subjects[1] is Object Obj)
                {
                    bool isInSameBlockOrRoomOrInventory =
                        Obj.Block == Executor.Block ||
                        Obj.Room == Executor.Room;

                    if (isInSameBlockOrRoomOrInventory)
                    {
                        MakeObservation("You carve " + Obj.ReferredToNames[0] + " into a statue of " + Image.ReferredToNames[0] + ".", Color.Green, new EntityList<Entity>());

                        Obj.Type = "statue";
                        Obj.CarvedSymbols.Add(Image);
                    }
                    else
                    {
                        MakeObservation("The object must be in the same block or room to sculpt it.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You can only carve an object into a statue. I suppose you could, but probably shouldn't.", Color.Yellow, new EntityList<Entity>());
                }
            }


            else if (CommandID == "reinforce")
            {
                Executor.CooldownCycles += (int)(Math.Round(100 / Executor.Speed()));

                if (Subjects[0] is Entity entityToReinforce && Subjects[1] is Object reinforcementMaterial)
                {
                    if(reinforcementMaterial.Weight < 3000)
                    {
                        bool canReinforce = false;

                        // Check if Executor is reinforcing a structure from the outside
                        if (entityToReinforce is Structure structure)
                        {
                            // You can reinforce the structure either from the outside (Executor.Block must contain the structure) or from inside Structure.Rooms[0]
                            if ((Executor.Structure == null && Executor.Block.Structures.Contains(structure)) ||
                                (Executor.Structure == structure && Executor.Room == structure.Rooms[0]))
                            {
                                canReinforce = true;

                                // Reinforce the exit door of the structure
                                Object exitDoor = structure.Rooms[0].Objects.FirstOrDefault(obj => obj.Type == "exit door");
                                if (exitDoor != null)
                                {
                                    exitDoor.Reinforced = true;
                                }

                                // Reinforce the structure itself
                                structure.Reinforced = true;
                            }
                            else
                            {
                                MakeObservation("You can only reinforce the structure if you're nearby it or in the exit room.", Color.Orange, new EntityList<Entity>());
                            }
                        }
                        // Check if Executor is reinforcing a door
                        else if (entityToReinforce is Door door)
                        {
                            if (Executor.Room != null && Executor.Room.Objects.Contains(door))
                            {
                                canReinforce = true;

                                // Reinforce the opposite door in the destination room
                                Room destinationRoom = door.DestinationRoom;
                                Door oppositeDoor = (Door)(destinationRoom.Objects.FirstOrDefault(d => d is Door D && D.DestinationRoom == Executor.Room));
                                if (oppositeDoor != null)
                                {
                                    oppositeDoor.Reinforced = true;
                                }

                                // Reinforce the door itself
                                door.Reinforced = true;
                            }
                            else
                            {
                                MakeObservation("You need to be in the same room as the door to reinforce it.", Color.Orange, new EntityList<Entity>());
                            }
                        }
                        // Check if Executor is reinforcing an exit door
                        else if (entityToReinforce is Object obj && obj.Type == "exit door")
                        {
                            if (Executor.Room != null && Executor.Room.Objects.Contains(obj))
                            {
                                canReinforce = true;

                                // Reinforce the structure the Executor is in
                                if (Executor.Structure != null)
                                {
                                    Executor.Structure.Reinforced = true;
                                }

                                // Reinforce the exit door itself
                                obj.Reinforced = true;
                            }
                            else
                            {
                                MakeObservation("You need to be in the same room as the exit door to reinforce it.", Color.Orange, new EntityList<Entity>());
                            }
                        }

                        if (canReinforce)
                        {
                            // Remove the reinforcement material from the Executor's inventory or hands
                            bool materialFound = false;

                            if (Executor.MainHeldObject == reinforcementMaterial)
                            {
                                Executor.MainHeldObject = null;
                                materialFound = true;
                            }
                            else if (Executor.OffHeldObject == reinforcementMaterial)
                            {
                                Executor.OffHeldObject = null;
                                materialFound = true;
                            }
                            else if (Executor.Inventory.Contains(reinforcementMaterial))
                            {
                                Executor.Inventory.Remove(reinforcementMaterial);
                                materialFound = true;
                            }

                            if (materialFound)
                            {
                                // Add the reinforcement material to the room or block objects
                                EntityList<Object> objList = Executor.Room != null ? Executor.Room.Objects : Executor.Block.Objects;
                                objList.Add(reinforcementMaterial);

                                // Set the entity as reinforced
                                entityToReinforce.Reinforced = true;

                                MakeObservation("You successfully reinforce " + entityToReinforce.ReferredToNames[0] + ".", Color.Green, new EntityList<Entity>());
                            }
                            else
                            {
                                MakeObservation("You don't have the required material to reinforce " + entityToReinforce.ReferredToNames[0] + ".", Color.Orange, new EntityList<Entity>());
                            }
                        }
                    }
                    else
                    {
                        MakeObservation("That object is not heavy enough to reinforce anything.", Color.Orange, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You can only reinforce doors and structure entrances.", Color.Orange, new EntityList<Entity>());
                }
            }
            else if (CommandID == "ignite")
            {
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed()));

                if (Subjects[0] is Object o)
                {
                    bool isInSameBlockOrRoomOrInventory =
                        o.Block == Executor.Block ||
                        o.Room == Executor.Room ||
                        Executor.Inventory.Contains(o);

                    if (isInSameBlockOrRoomOrInventory)
                    {
                        MakeObservation("You manifest some energy to strike a small flame.", Color.Green, new EntityList<Entity>());
                        o.FireCycles += 3;
                    }
                    else
                    {
                        MakeObservation("The object must be nearby to ignite it.", Color.Orange, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("It would be unfeasible to ignite that.", Color.Orange, new EntityList<Entity>());
                }
            }

            else if (CommandID == "brew")
            {
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed()));

                // Check if the first subject is either coffee or tea
                if (Subjects[0].Metadata != "coffee" && Subjects[0].Metadata != "tea")
                {
                    MakeObservation("You can only brew coffee or tea.", Color.Yellow, new EntityList<Entity>());
                }
                else
                {
                    // Check if the second subject is an object the player has
                    Object ingredient = null;
                    if (Executor.MainHeldObject == Subjects[1])
                    {
                        ingredient = Executor.MainHeldObject;
                    }
                    else if (Executor.OffHeldObject == Subjects[1])
                    {
                        ingredient = Executor.OffHeldObject;
                    }
                    else if (Executor.Inventory.Contains(Subjects[1]))
                    {
                        ingredient = (Object)Subjects[1];
                    }

                    // Check if the ingredient is either coffee or tea based on Materials
                    if (ingredient != null && ingredient.Materials[0].Name == Subjects[0].Metadata)
                    {
                        // Check if the third subject is a container
                        if (Subjects[2] is Object container && container.IsContainer)
                        {
                            // Remove the ingredient from wherever it was held
                            if (Executor.MainHeldObject == ingredient)
                            {
                                Executor.MainHeldObject = null;  // Remove from main hand
                            }
                            else if (Executor.OffHeldObject == ingredient)
                            {
                                Executor.OffHeldObject = null;  // Remove from off hand
                            }
                            else if (Executor.Inventory.Contains(ingredient))
                            {
                                Executor.Inventory.Remove(ingredient);  // Remove from inventory
                            }

                            // Create the brewed drink object inside the container
                            Object brewedDrink = new Object(null, "drink", new EntityList<Material> { GameWorld.Coffee }, Executor);
                            if (Subjects[0].Metadata == "tea")
                            {
                                brewedDrink.Materials[0] = GameWorld.Tea;  // Change to tea if that's what we're brewing
                            }

                            container.ContainedObjects.Add(brewedDrink);

                            MakeObservation($"You brew {Subjects[0].Metadata} and pour it into the {container.Type}.", Color.Green, new EntityList<Entity>() { brewedDrink, container });
                        }
                        else
                        {
                            MakeObservation("You need a container to brew into.", Color.Yellow, new EntityList<Entity>());
                        }
                    }
                    else
                    {
                        MakeObservation("You need the right ingredients to brew " + Subjects[0].Metadata + ".", Color.Yellow, new EntityList<Entity>());
                    }
                }
            }
            else if (CommandID == "hug")
            {
                Executor.CooldownCycles += (int)(Math.Round(30 / Executor.Speed()));

                if (Subjects[0] is Architect Interactee)
                {
                    // Check if they are in the same room or block
                    if (Executor.Room == Interactee.Room || Executor.Block == Interactee.Block)
                    {
                        if (Interactee.GetOpinion(Executor) > 20)
                        {
                            Interactee.ChangeOpinion(Executor, 3);
                            MakeObservation("You hug " + Interactee.Name + ". They seem to appreciate it.", Color.Green, new EntityList<Entity>() { Interactee });
                        }
                        else
                        {
                            Interactee.ChangeOpinion(Executor, -1);
                            MakeObservation("You hug " + Interactee.Name + ". They look confused.", Color.Orange, new EntityList<Entity>() { Interactee });
                        }
                    }
                    else
                    {
                        MakeObservation("You are too far away to hug " + Interactee.Name + ".", Color.Yellow, new EntityList<Entity>() { Interactee });
                    }
                }
                else
                {
                    MakeObservation("You hug the " + Subjects[0].ReferredToNames[0] + ". It does not hug back.", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                }
            }

            else if (CommandID == "tickle")
            {
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed()));

                if (Subjects[0] is Architect Interactee)
                {
                    // Check if they are in the same room or block
                    if (Executor.Room == Interactee.Room || Executor.Block == Interactee.Block)
                    {
                        if (Executor.Sex != Interactee.Sex)
                        {
                            if (Interactee.GetOpinion(Executor) > 15)
                            {
                                Interactee.ChangeOpinion(Executor, 2);
                                MakeObservation("You tickle " + Interactee.Name + ". They giggle and seem amused.", Color.Green, new EntityList<Entity>() { Interactee });
                            }
                            else
                            {
                                Interactee.ChangeOpinion(Executor, -3);
                                MakeObservation("You tickle " + Interactee.Name + ". They seem uncomfortable and annoyed.", Color.Orange, new EntityList<Entity>() { Interactee });
                            }
                        }
                        else
                        {
                            MakeObservation("You tickle " + Interactee.Name + ". They look at you questioningly.", Color.Yellow, new EntityList<Entity>() { Interactee });
                        }
                    }
                    else
                    {
                        MakeObservation("You are too far away to tickle " + Interactee.Name + ".", Color.Yellow, new EntityList<Entity>() { Interactee });
                    }
                }
                else
                {
                    MakeObservation("You tickle the " + Subjects[0].ReferredToNames[0] + ". It does not react.", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                }
            }

            else if (CommandID == "hold_hand")
            {
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed()));

                if (Subjects[0] is Architect Interactee)
                {
                    // Check if they are in the same room or block
                    if (Executor.Room == Interactee.Room || Executor.Block == Interactee.Block)
                    {
                        if (Executor.Sex != Interactee.Sex)
                        {
                            if (Interactee.GetOpinion(Executor) > 30)
                            {
                                Interactee.ChangeOpinion(Executor, 5);
                                MakeObservation("You hold hands with " + Interactee.Name + ". They warmly reciprocate.", Color.Green, new EntityList<Entity>() { Interactee });
                            }
                            else
                            {
                                Interactee.ChangeOpinion(Executor, -5);
                                MakeObservation("You hold hands with " + Interactee.Name + ". They look at you strangely.", Color.Orange, new EntityList<Entity>() { Interactee });
                            }
                        }
                        else
                        {
                            MakeObservation("You hold hands with " + Interactee.Name + ". They don't seem to notice.", Color.Yellow, new EntityList<Entity>() { Interactee });
                        }
                    }
                    else
                    {
                        MakeObservation("You are too far away to hold hands with " + Interactee.Name + ".", Color.Yellow, new EntityList<Entity>() { Interactee });
                    }
                }
                else
                {
                    MakeObservation("You try to hold the hand of the " + Subjects[0].ReferredToNames[0] + "... oh wait... what hand...", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                }
            }
            else if (CommandID == "shove")
            {
                Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed()));

                // Check if the target is an Architect
                if (Subjects[0] is Architect Interactee)
                {
                    // Check if they are in the same room or block
                    if (Executor.Room == Interactee.Room || Executor.Block == Interactee.Block)
                    {
                        // Shove the architect and apply the distance logic
                        MakeObservation("You shove " + Interactee.Name + " forcefully.", Color.Orange, new EntityList<Entity>() { Interactee });

                        // Run the distance modification
                        Executor.ModifyDistance(Interactee, 2);  // This pushes the architect 2 units away (you can adjust this as needed)
                    }
                    else
                    {
                        MakeObservation("You are too far away to shove " + Interactee.Name + ".", Color.Yellow, new EntityList<Entity>() { Interactee });
                    }
                }
                else
                {
                    MakeObservation("You can't shove the " + Subjects[0].ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                }
            }
            else if (CommandID == "carry")
            {
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed()));

                // Check if the Executor has free hands
                if (Executor.MainHeldObject != null || Executor.OffHeldObject != null)
                {
                    MakeObservation("Your hands are full. You need to free up some space to carry anything.", Color.Yellow, new EntityList<Entity>());
                }
                else
                {
                    // Check if the subject is close enough to the Executor
                    Entity subject = Subjects[0];
                    if (Executor.GetDistance(subject) > 2)
                    {
                        MakeObservation("You are too far away to carry " + subject.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>());
                    }
                    else
                    {
                        // If the subject is an object, check weight capacity
                        if (subject is Object obj)
                        {
                            double maxCarryWeight = 7000 + (Executor.Strength * 1000);
                            if (obj.Weight > maxCarryWeight)
                            {
                                MakeObservation("The " + obj.ReferredToNames[0] + " is too heavy for you to carry.", Color.Orange, new EntityList<Entity>());
                            }
                            else
                            {
                                // Remove the object from its current location (room or block)
                                if (Executor.Room != null)
                                {
                                    Executor.Room.ObjectsToRemove.Add(obj);
                                }
                                else
                                {
                                    Executor.Block.ObjectsToRemove.Add(obj);
                                }

                                // Carry the object
                                Executor.MainHeldObject = obj;
                                MakeObservation("You pick up and carry the " + obj.ReferredToNames[0] + ".", Color.Green, new EntityList<Entity>() { obj });
                            }
                        }
                        // If the subject is an Architect, check size difference
                        else if (subject is Architect targetArchitect)
                        {
                            int sizeDifference = Game1.SizeDifference(targetArchitect.Race.Size, Executor.Race.Size);
                            if (sizeDifference > 1)
                            {
                                MakeObservation(targetArchitect.ReferredToNames[0] + " is too large for you to carry.", Color.Orange, new EntityList<Entity>());
                            }
                            else if (targetArchitect.IsAlive && targetArchitect.UnconsciousCycles == 0)
                            {
                                MakeObservation(targetArchitect.ReferredToNames[0] + " resists whatever the hell they think you're doing.", Color.Orange, new EntityList<Entity>());
                            }
                            else
                            {
                                // Remove the architect from its current location
                                if (targetArchitect.Room != null)
                                {
                                    targetArchitect.Room.ArchitectsToRemove.Add(targetArchitect);
                                }
                                else
                                {
                                    targetArchitect.Block.ArchitectsToRemove.Add(targetArchitect);
                                }

                                // Carry the architect
                                Executor.CarryingEntity = targetArchitect;
                                MakeObservation("You pick up and carry " + targetArchitect.ReferredToNames[0] + ".", Color.Green, new EntityList<Entity>() { targetArchitect });
                            }
                        }
                    }
                }
            }
            else if (CommandID == "disarm_traps")
            {
                Executor.CooldownCycles += (int)(Math.Round(50 / Executor.Speed())); // Adjust the X to a reasonable value like 50

                // Get the relevant object list and architect list
                EntityList<Object> ObjList = Executor.Room != null ? Executor.Room.Objects : Executor.Block.Objects;
                EntityList<Object> ObjectsToRemove = Executor.Room != null ? Executor.Room.ObjectsToRemove : Executor.Block.ObjectsToRemove;
                EntityList<Object> ObjectsToAdd = Executor.Room != null ? Executor.Room.ObjectsToAdd : Executor.Block.ObjectsToAdd;

                Object trapToDisarm = ObjList.FirstOrDefault(o => o.Type == "rope trap");

                if (trapToDisarm != null)
                {
                    // Queue the trap for removal
                    ObjectsToRemove.Add(trapToDisarm);

                    // Create the new object (rope) to be added later
                    Object newRope = new Object(null, "bunch", new EntityList<Material>() { trapToDisarm.Materials[0] }, Executor);

                    if (Executor.Room != null)
                    {
                        newRope.Room = Executor.Room;
                        newRope.Block = Executor.Room.Structure.Block;
                    }
                    else
                    {
                        newRope.Block = Executor.Block;
                    }

                    // Queue the new rope for addition
                    ObjectsToAdd.Add(newRope);

                    // Observation about successful disarming
                    MakeObservation("You carefully disarm a trap and recover some rope.", Color.Green, new EntityList<Entity>() { newRope });
                }
                else
                {
                    // Observation about no traps present
                    MakeObservation("There are no traps to disarm here.", Color.Yellow, new EntityList<Entity>());
                }
            }

            else if (CommandID == "knock")
            {
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed()));

                EntityList<Architect> responseArchitects = new EntityList<Architect>();
                bool validKnock = false;

                // Determine if the player is knocking on a structure, door, or exit door
                if (Subjects[0] is Structure structure && Executor.Block.Structures.Contains(structure) && Executor.Room == null)
                {
                    // Knocking on a structure
                    responseArchitects = structure.Rooms[0].Architects;
                    validKnock = true;
                }
                else if (Subjects[0] is Door door && Executor.Room != null && Executor.Room.Objects.Contains(door))
                {
                    // Knocking on a door (not an exit door)
                    responseArchitects = door.DestinationRoom.Architects;
                    validKnock = true;
                }
                else if (Subjects[0] is Object obj && obj.Type == "exit door" && Executor.Room != null && Executor.Room == Executor.Structure.Rooms[0])
                {
                    // Knocking on an exit door
                    responseArchitects = Executor.Room.Structure.Block.Architects;
                    validKnock = true;
                }

                // Ensure the knock is valid
                if (!validKnock)
                {
                    MakeObservation("You knock. Expectedly, there is no reply.", Color.Yellow, new EntityList<Entity>());
                }
                else
                {
                    Structure targetStructure = (Subjects[0] is Structure) ? (Structure)Subjects[0] : Executor.Structure;
                    bool isNonResponsiveStructure = false;

                    if (targetStructure != null)
                    {
                        string structureType = targetStructure.Type.ToLower();
                        isNonResponsiveStructure = structureType == "bastion" || structureType == "commune" || structureType == "core" ||
                                                    structureType == "fort" || structureType == "fortress" || structureType == "heart" ||
                                                    structureType == "keep" || structureType == "monastery" || structureType == "monument" ||
                                                    structureType == "mound" || structureType == "outpost" || structureType == "sanctum" ||
                                                    structureType == "scaffold" || structureType == "scum" || structureType == "spire" ||
                                                    structureType == "stronghold";
                    }

                    if (isNonResponsiveStructure)
                    {
                        MakeObservation("Your knock echoes with no response.", Color.Gray, new EntityList<Entity>());
                    }
                    else if (responseArchitects.Count() > 0)
                    {
                        int responseIndex = Game1.r.Next(9); // Adjusted range to include all cases

                        switch (responseIndex)
                        {
                            case 0:
                                MakeObservation("You hear a voice, \"Come in.\"", Color.Green, new EntityList<Entity>());
                                break;
                            case 1:
                                MakeObservation("You hear a voice, \"No, we don't want to purchase shiba statues.\"", Color.Green, new EntityList<Entity>());
                                break;
                            case 2:
                                MakeObservation("There is some rustling behind the door, but no one responds.", Color.Yellow, new EntityList<Entity>());
                                break;
                            case 3:
                                MakeObservation("The door creaks open slightly, but no one emerges.", Color.Gray, new EntityList<Entity>());
                                break;
                            case 4:
                                MakeObservation("A muffled voice responds, \"Who's there?\"", Color.Green, new EntityList<Entity>());
                                break;
                            case 5:
                                MakeObservation("You hear an irritated voice say, \"Go away, we're busy!\"", Color.Orange, new EntityList<Entity>());
                                break;
                            case 6:
                                MakeObservation("A voice from inside calls, \"Just a moment!\"", Color.Green, new EntityList<Entity>());
                                break;
                            case 7:
                                MakeObservation("You hear footsteps approaching, then silence.", Color.Yellow, new EntityList<Entity>());
                                break;
                            case 8:
                                MakeObservation("A voice yells, \"Not now! Come back later!\"", Color.OrangeRed, new EntityList<Entity>());
                                break;
                        }
                    }
                    else
                    {
                        MakeObservation("Your knock goes unanswered.", Color.Gray, new EntityList<Entity>());
                    }
                }
            }
            else if (CommandID == "stretch")
            {
                Executor.CooldownCycles += (int)(Math.Round(50 / Executor.Speed()));
                Executor.DestabilizedCycles = 0;

                MakeObservation(Executor.Name + " stretches, stabilizing themselves.", Color.Green, new EntityList<Entity>());
            }
            else if (CommandID == "polish")
            {
                Executor.CooldownCycles += (int)(Math.Round(50 / Executor.Speed()));

                if (Subjects[0] is Object objToPolish)
                {
                    objToPolish.Polished = true;
                    MakeObservation("You carefully polish the " + objToPolish.ReferredToNames[0] + ", making it shine.", Color.Green, new EntityList<Entity>() { objToPolish });
                }
                else
                {
                    MakeObservation("You can only polish objects.", Color.Yellow, new EntityList<Entity>());
                }
            }

            else if (CommandID == "clean")
            {
                Executor.CooldownCycles += (int)(Math.Round(50 / Executor.Speed()));

                if (Subjects[0] is Object objToClean)
                {
                    objToClean.Cleaned = true;
                    MakeObservation("You clean the " + objToClean.ReferredToNames[0] + ", making it spotless.", Color.Green, new EntityList<Entity>() { objToClean });
                }
                else
                {
                    MakeObservation("You can only clean objects.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "bind")
            {
                Executor.CooldownCycles += (int)(Math.Round(50 / Executor.Speed()));

                // Ensure the player has two objects to bind
                Object firstObject = Executor.MainHeldObject ?? Executor.OffHeldObject ?? Executor.Inventory.FirstOrDefault();
                Object secondObject = null;

                // Find the second object, ensuring it's different from the first one
                if (firstObject != Executor.MainHeldObject)
                    secondObject = Executor.MainHeldObject;
                else if (firstObject != Executor.OffHeldObject)
                    secondObject = Executor.OffHeldObject;

                // If not found in hands, look in the inventory
                if (secondObject == null || secondObject == firstObject)
                {
                    secondObject = Executor.Inventory.FirstOrDefault(obj => obj != firstObject);
                }

                if (firstObject != null && secondObject != null)
                {
                    // Remove the objects from the player's main hand, offhand, or inventory
                    if (Executor.MainHeldObject == firstObject)
                        Executor.MainHeldObject = null;
                    else if (Executor.OffHeldObject == firstObject)
                        Executor.OffHeldObject = null;
                    else
                        Executor.Inventory.Remove(firstObject);

                    if (Executor.MainHeldObject == secondObject)
                        Executor.MainHeldObject = null;
                    else if (Executor.OffHeldObject == secondObject)
                        Executor.OffHeldObject = null;
                    else
                        Executor.Inventory.Remove(secondObject);

                    // Create the MultiObject from the two objects
                    EntityList<Object> boundObjects = new EntityList<Object> { firstObject, secondObject };
                    MultiObject combinedObject = new MultiObject(Executor, boundObjects);

                    // Add the new MultiObject to the player's inventory
                    Executor.Inventory.Add(combinedObject);

                    // Provide feedback to the player
                    MakeObservation($"You bind {firstObject.ReferredToNames[0]} and {secondObject.ReferredToNames[0]} together into a new object.", Color.Green, new EntityList<Entity>() { combinedObject });
                }
                else
                {
                    // Inform the player that they need two objects to bind
                    MakeObservation("You need two different objects to bind together.", Color.Red, new EntityList<Entity>());
                }
            }

            else if (CommandID == "tame_creature")
            {
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed()));
                if (Subjects[0] is Architect && ((((Architect)(Subjects[0])).Room == Executor.Room) && (((Architect)(Subjects[0])).Block == Executor.Block)))
                {
                    AddMessage(Executor.Name + ": Wild one, join the ranks of my great conquest.", Color.Green, new EntityList<Entity>() { Executor });
                    if (!GameWorld.HumanoidRaces.Contains(((Architect)Subjects[0]).Race) && !GameWorld.ExtraRaces.Contains(((Architect)Subjects[0]).Race) && !GameWorld.ConstructRaces.Contains(((Architect)Subjects[0]).Race) && !GameWorld.ColossalTypes.Contains(((Architect)Subjects[0]).Race))
                    {
                        int ExistingAnimals = 0;
                        foreach (Architect a in GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                        {
                            if (!GameWorld.HumanoidRaces.Contains(a.Race) && !GameWorld.ExtraRaces.Contains(a.Race))
                            {
                                ExistingAnimals++;
                            }
                        }

                        if (Executor.PathOfLifeLevel >= 6 && ExistingAnimals < Executor.PathOfLifeLevel)
                        {
                            AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + ": *happy shibesque noises*", Color.Green, new EntityList<Entity>() { Subjects[0] });
                            GameWorld.GamePlayerAssociation.ActiveParty.Architects.Add(((Architect)Subjects[0]));
                        }
                        else
                        {
                            AddMessage(((Architect)Subjects[0]).Name + ": *sad shibesque noises*", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                        }
                    }
                    else
                    {
                        AddMessage(((Architect)Subjects[0]).ReferredToNames[0] + " *thinks to self* Something is wrong with this one.", Color.Orange, new EntityList<Entity>() { Subjects[0] });
                        ((Architect)Subjects[0]).ChangeOpinion(Executor, -20);
                    }
                }
                else
                {
                    MakeObservation("You couldn't find anything like that nearby.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "starstrike")
            {
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed()));
                Executor.Energy -= Math.Max(0, 12 - Executor.PathOfStarsLevel);

                if (Subjects[0] is Architect targetArchitect && (targetArchitect.Room == Executor.Room && targetArchitect.Block == Executor.Block))
                {
                    MakeObservation("You flick your wrist...", Color.Green, new EntityList<Entity>());
                    int StarCount = 0;

                    if (Executor.PathOfStarsLevel >= 6)
                    {
                        StarCount = r.Next(2, 4);
                        MakeObservation($"Stars fly from your hands!", Color.Goldenrod, new EntityList<Entity>());
                    }
                    else
                    {
                        MakeObservation($"...but nothing happens.", Color.Yellow, new EntityList<Entity>());
                    }

                    for (int i = 0; i < StarCount; i++)
                    {
                        Object o = new Object(null, "star", new EntityList<Material>() { GameWorld.Energy }, Executor);
                        o.Thrower = Executor;
                        o.AirborneTarget = Subjects[0];

                        if (targetArchitect.Room != null)
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
                    MakeObservation("You couldn't find an architect like that nearby.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "flamestrike")
            {
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed()));
                Executor.Energy -= Math.Max(0, 15 - Executor.PathOfHeatLevel);
                if (Subjects[0] is Architect targetArchitect && ArchitectsToUse.Contains(Subjects[0]))
                {
                    MakeObservation("You wave...", Color.Green, new EntityList<Entity>());

                    if (Executor.PathOfHeatLevel >= 2)
                    {
                        MakeObservation($"A large flame emnates from your hand!", Color.Goldenrod, new EntityList<Entity>());
                        Object o = new Object(null, "wave", new EntityList<Material>() { GameWorld.Flame }, Executor);
                        o.AirborneTarget = Subjects[0];
                        o.AirborneCyclesToHitTarget = 30;
                        o.Thrower = Executor;
                        (targetArchitect.Room != null ? targetArchitect.Room.Objects : targetArchitect.Block.Objects).Add(o);
                    }
                    else
                    {
                        MakeObservation($"...but nothing happens.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You couldn't find an architect like that nearby.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "heat_object")
            {
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed()));
                Executor.Energy -= 10 - Executor.PathOfHeatLevel;
                if (Executor.PathOfHeatLevel >= 4)
                {
                    var targetObject = Subjects[0];

                    if (Executor.OffHeldObject == targetObject || Executor.MainHeldObject == targetObject)
                    {
                        MakeObservation("You focus...", Color.Green, new EntityList<Entity>());
                        ((Object)targetObject).HeatInCelsius += 50;
                        MakeObservation($"The {targetObject.Name} in your hand heats up intensely!", Color.Goldenrod, new EntityList<Entity>() { targetObject });
                    }
                    else
                    {
                        MakeObservation($"...but you're not holding the intended target.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation($"...but your control over heat is not strong enough.", Color.Yellow, new EntityList<Entity>());
                }
            }

            else if (CommandID == "starsmite")
            {
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed()));
                Executor.Energy -= Math.Max(0, 25 - Executor.PathOfStarsLevel);

                if (Subjects[0] is Architect targetArchitect && (targetArchitect.Room == Executor.Room && targetArchitect.Block == Executor.Block))
                {
                    MakeObservation("You point and wave...", Color.Green, new EntityList<Entity>());

                    if (Executor.PathOfStarsLevel >= 8)
                    {
                        MakeObservation($"A swirling vortex appears, and a cosmic energy beam strikes " + Subjects[0].ReferredToNames[0] + "!", Color.Goldenrod, new EntityList<Entity>() { Subjects[0] });

                        foreach (Object o in targetArchitect.BodyParts)
                        {
                            o.Integrity -= r.Next(10, 20);
                        }
                        targetArchitect.Bleeding += r.Next(2, 6);

                        targetArchitect.ChangeOpinion(Executor, -100);
                    }
                    else
                    {
                        MakeObservation($"...but nothing happens.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You couldn't find an architect like that nearby.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "conjure_spark")
            {
                Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed()));
                Executor.Energy -= Math.Max(0, 5 - Executor.PathOfLightLevel);
                if (Executor.PathOfLightLevel >= 1)
                {
                    MakeObservation("You hold your hand out, collecting light...", Color.Green, new EntityList<Entity>());
                    MakeObservation("A radiant spark appears!", Color.Green, new EntityList<Entity>());

                    Object Spark = new Object(null, "spark", new EntityList<Material>() { GameWorld.Energy }, Executor);
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

                    if (Executor.Sparks.Count() > Executor.PathOfLightLevel)
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
                        MakeObservation("You feel a loss of connection to your earliest spark.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "evoke_strike")
            {
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed()));
                Executor.Energy -= 10 - Executor.PathOfStarsLevel;
                Object FoundSpark = null;

                if (Executor.PathOfLightLevel >= 4)
                {
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
                        EntityList<Object> objectsInArea = FoundSpark.Room != null ? FoundSpark.Room.Objects : FoundSpark.Block.Objects;
                        EntityList<Architect> architectsInArea = FoundSpark.Room != null ? FoundSpark.Room.Architects : FoundSpark.Block.Architects;

                        MakeObservation("You evoke your spark...", Color.White, new EntityList<Entity>());

                        if (Subjects[0] is Architect architectTarget && architectsInArea.Contains(architectTarget))
                        {
                            Object BP = architectTarget.BodyParts[r.Next(architectTarget.BodyParts.Count())];
                            BP.Integrity -= r.Next(10, Executor.PathOfStarsLevel * 5);
                            architectTarget.Bleeding += Game1.r.Next(5);
                            architectTarget.Pain += Game1.r.Next(5);
                            architectTarget.ChangeOpinion(Executor, -100);
                            MakeObservation("A heavenly beam pierces through " + BP.ReferredToNames[0] + ", leaving a burning hole!", Color.Magenta, new EntityList<Entity>() { BP });
                        }
                        else if (Subjects[0] is Object objectTarget && objectsInArea.Contains(objectTarget))
                        {
                            objectTarget.Integrity -= r.Next(10, Executor.PathOfStarsLevel * 5);
                            MakeObservation("A heavenly beam pierces through " + objectTarget.ReferredToNames[0] + ", leaving a burning hole!", Color.Magenta, new EntityList<Entity>() { objectTarget });
                        }
                        else
                        {
                            MakeObservation("The beam fails to target properly.", Color.Yellow, new EntityList<Entity>());
                        }

                        Executor.Sparks.Remove(FoundSpark);
                        objectsInArea.Remove(FoundSpark);
                    }
                    else
                    {
                        MakeObservation("You couldn't find one of your sparks in the vicinity.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red, new EntityList<Entity>());
                }
            }
            else if (CommandID == "evoke_blindness")
            {
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed()));
                Executor.Energy -= 10 - Executor.PathOfStarsLevel;
                Object FoundSpark = null;

                if (Executor.PathOfLightLevel >= 2)
                {
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
                        EntityList<Architect> architectsInArea = FoundSpark.Room != null ? FoundSpark.Room.Architects : FoundSpark.Block.Architects;
                        bool foundArchitects = false;

                        foreach (Architect architect in architectsInArea)
                        {
                            if (architect.TargetArchitect != null && GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(architect.TargetArchitect) && (architect.Task == "killtarget" || architect.Task == "disabletarget"))
                            {
                                architect.BlindCycles += 50;
                                architect.ChangeOpinion(Executor, -60);
                                MakeObservation(architect.ReferredToNames[0] + " is blinded by the radiance!", Color.Magenta, new EntityList<Entity>() { architect });
                                foundArchitects = true;
                            }
                        }

                        if (!foundArchitects)
                        {
                            MakeObservation("No one is blinded...", Color.Yellow, new EntityList<Entity>());
                        }

                        Executor.Sparks.Remove(FoundSpark);
                        FoundSpark.Room?.Objects.Remove(FoundSpark);
                        FoundSpark.Block?.Objects.Remove(FoundSpark);
                    }
                    else
                    {
                        MakeObservation("You couldn't find one of your sparks in the vicinity.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red, new EntityList<Entity>());
                }
            }
            else if (CommandID == "evoke_healing")
            {
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed()));
                Object FoundSpark = null;

                if (Executor.PathOfLightLevel >= 6)
                {
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
                        EntityList<Architect> architectsInArea = FoundSpark.Room != null ? FoundSpark.Room.Architects : FoundSpark.Block.Architects;
                        bool foundArchitects = false;

                        foreach (Architect architect in architectsInArea)
                        {
                            if (GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(architect))
                            {
                                if (architect.CombatCycles == 0)
                                {
                                    architect.Energy = architect.MaxEnergy;
                                    MakeObservation(architect.Name + " is enveloped in brilliance and fully healed!", Color.Magenta, new EntityList<Entity>() { architect });
                                }
                                else
                                {
                                    MakeObservation(architect.Name + " is too distracted for brilliance.", Color.Yellow, new EntityList<Entity>() { architect });
                                }
                                foundArchitects = true;
                            }
                        }

                        if (!foundArchitects)
                        {
                            MakeObservation("There is no one to heal...", Color.Yellow, new EntityList<Entity>());
                        }

                        Executor.Sparks.Remove(FoundSpark);
                        FoundSpark.Room?.Objects.Remove(FoundSpark);
                        FoundSpark.Block?.Objects.Remove(FoundSpark);
                    }
                    else
                    {
                        MakeObservation("You couldn't find one of your sparks in the vicinity.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red, new EntityList<Entity>());
                }
            }

            else if (CommandID == "evoke_nexus")
            {
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed()));
                Executor.Energy -= 30 - Executor.PathOfLightLevel;
                Object FoundSpark = null;

                if (Executor.PathOfLightLevel >= 8) // Assuming the ability requires a certain level to use
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
                        Architect a = new Architect("", Game1.Sexes[r.Next(Game1.Sexes.Count())], Game1.GameWorld.GetRace("photonexus"), 0, "prismancer", new EntityList<Object>(), Executor.Location, Executor.District, Executor.Block, "", 1, true);
                        a.Name = Game1.GameWorld.GenerateUniqueArchitectName(a);
                        GameWorld.GamePlayerAssociation.ActiveParty.Architects.Add(a);
                        Game1.LoadedArchitects.Add(a);
                        MakeObservation("A photonexus appears!", Color.Cyan, new EntityList<Entity>());

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
                        MakeObservation("You couldn't find one of your sparks in the vicinity.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red, new EntityList<Entity>());
                }
            }
            else if (CommandID == "inflame")
            {
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed()));
                Executor.Energy -= 15 - Executor.PathOfHeatLevel;

                if (Executor.PathOfHeatLevel >= 8)
                {
                    Executor.FireSeconds += 500;
                    MakeObservation("Your flame burns brighter!", Color.Red, new EntityList<Entity>());
                }
                else
                {
                    MakeObservation("You don't have control over that.", Color.Red, new EntityList<Entity>());
                }
            }
            else if (CommandID == "unflame")
            {
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed()));
                Executor.Energy -= 15 - Executor.PathOfHeatLevel;
                if (Executor.PathOfHeatLevel >= 8)
                {
                    Executor.FireSeconds = 0;
                    MakeObservation("You stop blazing!", Color.Red, new EntityList<Entity>());
                }
                else
                {
                    MakeObservation("You don't have control over that.", Color.Red, new EntityList<Entity>());
                }
            }

            else if (CommandID == "augment_creature")
            {
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed()));
                Executor.Energy -= Math.Max(0, 25 - Executor.PathOfLifeLevel);

                if (Subjects[0] is Architect && ((((Architect)(Subjects[0])).Room == Executor.Room) && (((Architect)(Subjects[0])).Block == Executor.Block)))
                {
                    if (!GameWorld.HumanoidRaces.Contains(((Architect)Subjects[0]).Race) && !GameWorld.ExtraRaces.Contains(((Architect)Subjects[0]).Race) && GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Subjects[0]))
                    {
                        if (Executor.PathOfLifeLevel >= 8)
                        {
                            MakeObservation("You gesture.", Color.Magenta, new EntityList<Entity>());

                            if (((Architect)Subjects[0]).Augment == true)
                            {
                                MakeObservation(Subjects[0].ReferredToNames[0] + " already has an augmentation.", Color.Magenta, new EntityList<Entity>() { Subjects[0] });
                            }
                            else
                            {
                                int Shibe = r.Next(3);

                                if (Shibe == 0)
                                {
                                    MakeObservation(Subjects[0].ReferredToNames[0] + " is enveloped in a golden light, becoming stronger!", Color.Magenta, new EntityList<Entity>() { Subjects[0] });
                                    ((Architect)Subjects[0]).Strength += 2;
                                }
                                else if (Shibe == 1)
                                {
                                    MakeObservation(Subjects[0].ReferredToNames[0] + " is enveloped in a white light, becoming more agile!", Color.Magenta, new EntityList<Entity>() { Subjects[0] });
                                    ((Architect)Subjects[0]).Agility += 2;
                                }
                                else if (Shibe == 2)
                                {
                                    MakeObservation(Subjects[0].ReferredToNames[0] + " is enveloped in a red light, becoming more durable!", Color.Magenta, new EntityList<Entity>() { Subjects[0] });
                                    ((Architect)Subjects[0]).MaxEnergyMod += 30;
                                    ((Architect)Subjects[0]).Energy = ((Architect)Subjects[0]).MaxEnergy;
                                }

                                ((Architect)Subjects[0]).Augment = true;
                            }
                        }
                        else
                        {
                            MakeObservation("You aren't powerful enough to do that.", Color.Yellow, new EntityList<Entity>());
                        }
                    }
                    else
                    {
                        MakeObservation("We know you love slavery, but you can't augment humanoids or creatures you don't control.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You couldn't find anything augmentable like that nearby.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "raise_dead")
            {
                if (Executor.PathOfDeathLevel < 2)
                {
                    MakeObservation("You don't know how to do that.", Color.Yellow, new EntityList<Entity>());
                }
                else
                {
                    Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed()));
                    Executor.Energy -= Math.Max(0, 30 - Executor.PathOfDeathLevel);
                    MakeObservation("You conjure a spark of dark energy, and speak the name of " + Subjects[0].ReferredToNames[0] + "...", Color.Purple, new EntityList<Entity>() { Subjects[0] });

                    if (Subjects[0] is Architect architect)
                    {
                        architect.RaiseFromTheDead(Executor, Subjects[0].ReferredToNames[0], Executor.PathOfDeathLevel, 2);

                        if (architect.IsAlive)
                        {
                            if (!GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(architect))
                            {
                                GameWorld.GamePlayerAssociation.ActiveParty.Architects.Add(architect);
                            }
                        }
                    }
                    else
                    {
                        MakeObservation("...but nothing happens.", Color.Purple, new EntityList<Entity>());
                    }
                }
            }


            else if (CommandID == "fire_spectral_bolt")
            {
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed()));
                Executor.Energy -= Math.Max(0, 15 - Executor.PathOfDeathLevel);
                if (Executor.PathOfDeathLevel >= 4 || (Executor.UndeadCreator != null && Executor.UndeadCreator.PathOfDeathLevel >= 8))
                {
                    if (Subjects[0] is Architect)
                    {
                        Object o = new Object(null, "energy bolt", new EntityList<Material>() { GameWorld.Spectre }, false, false, null, Executor, 0, false, Executor.Block, Executor.Structure, Executor.Room, false);
                        o.AirborneTarget = Subjects[0];
                        o.Owner = Executor;

                        if (Executor.Room != null)
                        {
                            Executor.Room.Objects.Add(o);
                        }
                        else
                        {
                            Executor.Block.Objects.Add(o);
                        }

                        MakeObservation("You fire a spectral bolt at " + Subjects[0].ReferredToNames[0] + ".", Color.Cyan, new EntityList<Entity>() { Subjects[0] });
                    }
                    else
                    {
                        MakeObservation("The spirit you pulled from " + GameWorld.DarkDeity.Name + "-knows-where only seeks the living and dead.", Color.Yellow, new EntityList<Entity>() { GameWorld.DarkDeity });
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Yellow, new EntityList<Entity>());
                }
            }



            else if (CommandID == "increase_weight")
            {
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed()));
                Executor.Energy -= Math.Max(0, 10 - Executor.PathOfRealityLevel);

                if (Executor.PathOfRealityLevel >= 2)
                {
                    if (Subjects[0] is Object)
                    {
                        if (!((Object)Subjects[0]).RealityAugmented)
                        {
                            MakeObservation(Subjects[0].ReferredToNames[0] + " increases in weight!", Color.Green, new EntityList<Entity>() { Subjects[0] });
                            ((Object)Subjects[0]).Weight *= 2; // Adjust the weight increase as necessary
                            ((Object)Subjects[0]).RealityAugmented = true;
                        }
                        else
                        {
                            MakeObservation(Subjects[0].ReferredToNames[0] + " has already been reality augmented.", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                        }
                    }
                    else
                    {
                        MakeObservation(Subjects[0].ReferredToNames[0] + " isn't an object.", Color.Green, new EntityList<Entity>() { Subjects[0] });
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red, new EntityList<Entity>());
                }
            }


            else if (CommandID == "increase_temperature")
            {
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed()));
                Executor.Energy -= Math.Max(0, 10 - Executor.PathOfRealityLevel);

                if (Executor.PathOfRealityLevel >= 2)
                {
                    if (Subjects[0] is Object)
                    {
                        if (!((Object)Subjects[0]).RealityAugmented)
                        {
                            MakeObservation(Subjects[0].ReferredToNames[0] + " heats up!", Color.Green, new EntityList<Entity>() { Subjects[0] });
                            ((Object)Subjects[0]).HeatInCelsius += 50; // Adjust the temperature increase as needed
                            ((Object)Subjects[0]).RealityAugmented = true;
                        }
                        else
                        {
                            MakeObservation(Subjects[0].ReferredToNames[0] + " has already been reality augmented.", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                        }
                    }
                    else
                    {
                        MakeObservation(Subjects[0].ReferredToNames[0] + " isn't an object.", Color.Green, new EntityList<Entity>() { Subjects[0] });
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red, new EntityList<Entity>());
                }
            }


            // Increase aerodynamics of an object
            // Increase aerodynamics of an object
            else if (CommandID == "increase_aerodynamics")
            {
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed()));
                Executor.Energy -= Math.Max(0, 10 - Executor.PathOfRealityLevel);

                if (Executor.PathOfRealityLevel >= 2)
                {
                    if (Subjects[0] is Object)
                    {
                        if (!((Object)Subjects[0]).RealityAugmented)
                        {
                            MakeObservation(Subjects[0].ReferredToNames[0] + " becomes more aerodynamic!", Color.Green, new EntityList<Entity>() { Subjects[0] });
                            ((Object)Subjects[0]).ProjectileAerodynamic = true;
                            ((Object)Subjects[0]).RealityAugmented = true;
                        }
                        else
                        {
                            MakeObservation(Subjects[0].ReferredToNames[0] + " has already been reality augmented.", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                        }
                    }
                    else
                    {
                        MakeObservation(Subjects[0].ReferredToNames[0] + " isn't an object.", Color.Green, new EntityList<Entity>() { Subjects[0] });
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red, new EntityList<Entity>());
                }
            }


            // Increase integrity of an object
            // Increase integrity of an object
            else if (CommandID == "increase_integrity")
            {
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed()));
                Executor.Energy -= Math.Max(0, 10 - Executor.PathOfRealityLevel);

                if (Executor.PathOfRealityLevel >= 2)
                {
                    if (Subjects[0] is Object)
                    {
                        if (!((Object)Subjects[0]).RealityAugmented)
                        {
                            // Increase integrity but ensure it does not exceed 100
                            ((Object)Subjects[0]).Integrity = ((Object)Subjects[0]).Integrity + 20; // Assuming each use increases integrity
                            ((Object)Subjects[0]).RealityAugmented = true;

                            MakeObservation(Subjects[0].ReferredToNames[0] + " becomes more structurally sound!", Color.Green, new EntityList<Entity>() { Subjects[0] });
                        }
                        else
                        {
                            MakeObservation(Subjects[0].ReferredToNames[0] + " has already been reality augumented.", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                        }
                    }
                    else
                    {
                        MakeObservation(Subjects[0].ReferredToNames[0] + " isn't an object.", Color.Green, new EntityList<Entity>() { Subjects[0] });
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red, new EntityList<Entity>());
                }
            }


            // Decrease weight of an object
            else if (CommandID == "decrease_weight")
            {
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed()));
                Executor.Energy -= Math.Max(0, 10 - Executor.PathOfRealityLevel);

                if (Executor.PathOfRealityLevel >= 2)
                {
                    if (Subjects[0] is Object)
                    {
                        if (!((Object)Subjects[0]).RealityAugmented)
                        {
                            ((Object)Subjects[0]).Weight /= 2; // Halve the weight
                            ((Object)Subjects[0]).RealityAugmented = true;
                            MakeObservation(Subjects[0].ReferredToNames[0] + " decreases in weight!", Color.Green, new EntityList<Entity>() { Subjects[0] });
                        }
                        else
                        {
                            MakeObservation(Subjects[0].ReferredToNames[0] + " has already been reality augmented.", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                        }
                    }
                    else
                    {
                        MakeObservation(Subjects[0].ReferredToNames[0] + " isn't an object.", Color.Green, new EntityList<Entity>() { Subjects[0] });
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red, new EntityList<Entity>());
                }
            }

            // Decrease temperature of an object
            else if (CommandID == "decrease_temperature")
            {
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed()));
                Executor.Energy -= Math.Max(0, 10 - Executor.PathOfRealityLevel);

                if (Executor.PathOfRealityLevel >= 2)
                {
                    if (Subjects[0] is Object)
                    {
                        if (!((Object)Subjects[0]).RealityAugmented)
                        {
                            ((Object)Subjects[0]).HeatInCelsius -= 50; // Decrease the temperature by a balanced amount
                            ((Object)Subjects[0]).RealityAugmented = true;
                            MakeObservation(Subjects[0].ReferredToNames[0] + " cools down!", Color.Green, new EntityList<Entity>() { Subjects[0] });
                        }
                        else
                        {
                            MakeObservation(Subjects[0].ReferredToNames[0] + " has already been reality augmented.", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                        }
                    }
                    else
                    {
                        MakeObservation(Subjects[0].ReferredToNames[0] + " isn't an object.", Color.Green, new EntityList<Entity>() { Subjects[0] });
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red, new EntityList<Entity>());
                }
            }

            // Decrease aerodynamics of an object
            else if (CommandID == "decrease_aerodynamics")
            {
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed()));
                Executor.Energy -= Math.Max(0, 10 - Executor.PathOfRealityLevel);

                if (Executor.PathOfRealityLevel >= 2)
                {
                    if (Subjects[0] is Object)
                    {
                        if (!((Object)Subjects[0]).RealityAugmented)
                        {
                            ((Object)Subjects[0]).ProjectileAerodynamic = false; // Reverse the aerodynamic property
                            ((Object)Subjects[0]).RealityAugmented = true;
                            MakeObservation(Subjects[0].ReferredToNames[0] + " becomes less aerodynamic!", Color.Green, new EntityList<Entity>() { Subjects[0] });
                        }
                        else
                        {
                            MakeObservation(Subjects[0].ReferredToNames[0] + " has already been reality augmented.", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                        }
                    }
                    else
                    {
                        MakeObservation(Subjects[0].ReferredToNames[0] + " isn't an object.", Color.Green, new EntityList<Entity>() { Subjects[0] });
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red, new EntityList<Entity>());
                }
            }

            // Decrease integrity of an object
            else if (CommandID == "decrease_integrity")
            {
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed()));
                Executor.Energy -= Math.Max(0, 10 - Executor.PathOfRealityLevel);

                if (Executor.PathOfRealityLevel >= 2)
                {
                    if (Subjects[0] is Object)
                    {
                        if (!((Object)Subjects[0]).RealityAugmented)
                        {
                            ((Object)Subjects[0]).Integrity = Math.Max(0, ((Object)Subjects[0]).Integrity - 20); // Decrease integrity, ensuring it doesn't go below 0
                            ((Object)Subjects[0]).RealityAugmented = true;
                            MakeObservation(Subjects[0].ReferredToNames[0] + " becomes less structurally sound!", Color.Green, new EntityList<Entity>() { Subjects[0] });
                        }
                        else
                        {
                            MakeObservation(Subjects[0].ReferredToNames[0] + " has already been reality augmented.", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                        }
                    }
                    else
                    {
                        MakeObservation(Subjects[0].ReferredToNames[0] + " isn't an object.", Color.Green, new EntityList<Entity>() { Subjects[0] });
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red, new EntityList<Entity>());
                }
            }


            // Liquify an object, building, or structure
            else if (CommandID == "liquify")
            {
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed()));
                Executor.Energy -= 15 - Executor.PathOfRealityLevel;

                if (Executor.PathOfRealityLevel >= 4)
                {
                    if (Subjects[0] is Object || Subjects[0] is Structure)
                    {
                        MakeObservation(Subjects[0].ReferredToNames[0] + " liquifies, and slowly seeps into the ground...", Color.Green, new EntityList<Entity>() { Subjects[0] });

                        if (Subjects[0] is Structure)
                        {
                            foreach (Room R in ((Structure)Subjects[0]).Rooms)
                            {
                                foreach (Object o in R.Objects)
                                {
                                    o.Block = ((Structure)Subjects[0]).Block;
                                    o.Room = null;
                                    ((Structure)Subjects[0]).Block.Objects.Add(o);
                                }
                                foreach (Architect a in R.Architects)
                                {
                                    a.Block = ((Structure)Subjects[0]).Block;
                                    a.Room = null;
                                    ((Structure)Subjects[0]).Block.Architects.Add(a);
                                }
                            }
                            ((Structure)Subjects[0]).Block.Structures.Remove((Structure)Subjects[0]);
                        }
                        else
                        {
                            bool Success = false;
                            foreach (Architect a in LoadedArchitects)
                            {
                                if (a.Inventory.Contains((Object)Subjects[0]))
                                {
                                    Success = true;

                                    a.Inventory.Remove((Object)Subjects[0]);
                                }
                                else if (a.Clothing.Contains((Object)Subjects[0]))
                                {
                                    Success = true;
                                    a.Clothing.Remove((Object)Subjects[0]);
                                }
                                else if (a.OffHeldObject == (Object)Subjects[0])
                                {
                                    Success = true;
                                    a.OffHeldObject = null;
                                }
                                else if (a.MainHeldObject == (Object)Subjects[0])
                                {
                                    Success = true;
                                    a.OffHeldObject = null;
                                }
                            }

                            if (!Success)
                            {
                                EntityList<Object> NecessaryList = ((Object)Subjects[0]).Room != null ? ((Object)Subjects[0]).Room.Objects : ((Object)Subjects[0]).Block.Objects;

                                NecessaryList.Remove((Object)Subjects[0]);
                            }
                        }
                    }
                    else
                    {
                        MakeObservation(Subjects[0].ReferredToNames[0] + " is not a suitable target for liquification.", Color.Red, new EntityList<Entity>() { Subjects[0] });
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red, new EntityList<Entity>());
                }
            }

            // Split an object into two copies of itself
            else if (CommandID == "split")
            {
                Executor.CooldownCycles += (int)(Math.Round(30 / Executor.Speed()));
                Executor.Energy -= 25 - Executor.PathOfRealityLevel;

                int currentYear = (int)(Game1.GameWorld.Cycle / 290304000);
                int currentMonth = ((int)(Game1.GameWorld.Cycle / 24192000) % 12) + 1;
                int currentDay = ((int)(Game1.GameWorld.Cycle / 864000) % 30) + 1;
                string currentDate = $"{currentYear:D4}-{currentMonth:D2}-{currentDay:D2}";

                if (Executor.PathOfRealityLevel >= 6)
                {
                    if (Executor.SplitDates == null)
                    {
                        Executor.SplitDates = new List<string>();
                    }

                    if (!Executor.SplitDates.Contains(currentDate))
                    {
                        if (Subjects[0] is Object currentObject)
                        {
                            MakeObservation("You manifest spatial particles...", Color.Purple, new EntityList<Entity>());

                            // Create clone of the current object
                            Object Clone = new Object
                            {
                                Type = currentObject.Type,
                                Materials = new EntityList<Material>(currentObject.Materials),
                                Description = currentObject.Description,
                                IsContainer = currentObject.IsContainer,
                                ContainedObjects = new EntityList<Object>(currentObject.ContainedObjects),
                                IfTrueUseInIfFalseUseOn = currentObject.IfTrueUseInIfFalseUseOn,
                                YLevelInFeet = currentObject.YLevelInFeet,
                                YVelocity = currentObject.YVelocity,
                                Weight = currentObject.Weight,
                                WeaponMaximumRange = currentObject.WeaponMaximumRange,
                                Block = currentObject.Block,
                                Room = currentObject.Room,
                                HeatInCelsius = currentObject.HeatInCelsius,
                                IsConsumable = currentObject.IsConsumable,
                                IsWearable = currentObject.IsWearable,
                                Rarity = currentObject.Rarity,
                                IsBodyPart = currentObject.IsBodyPart,
                                MajorArteryIsSevered = currentObject.MajorArteryIsSevered,
                                AirborneTarget = currentObject.AirborneTarget,
                                AirbornePower = currentObject.AirbornePower,
                                AirborneCyclesToHitTarget = currentObject.AirborneCyclesToHitTarget,
                                Creator = currentObject.Creator,
                                FireCycles = currentObject.FireCycles,
                                WetCycles = currentObject.WetCycles,
                                DestabilizedCycles = currentObject.DestabilizedCycles,
                                FractalCycles = currentObject.FractalCycles,
                                RematerializeLocation = currentObject.RematerializeLocation,
                                IsCoveredInPlants = currentObject.IsCoveredInPlants,
                                CoverageValues = new List<(string, int)>(currentObject.CoverageValues),
                                Coverage = currentObject.Coverage,
                                CoverageName = currentObject.CoverageName,
                                IsWeapon = currentObject.IsWeapon,
                                DamageType = currentObject.DamageType,
                                ProjectileAerodynamic = currentObject.ProjectileAerodynamic,
                                Strength = currentObject.Strength,
                                Dissipating = currentObject.Dissipating,
                                Integrity = currentObject.Integrity,
                                IsWritable = currentObject.IsWritable,
                                SpecialKnowledge = currentObject.SpecialKnowledge,
                                IsGeneralGood = currentObject.IsGeneralGood,
                                // Assume deep cloning specifics here as necessary
                            };

                            // Place the clone in the same environment as the original
                            if (currentObject.Room != null)
                            {
                                currentObject.Room.Objects.Add(Clone);
                            }
                            else if (currentObject.Block != null)
                            {
                                currentObject.Block.Objects.Add(Clone);
                            }
                            else
                            {
                                Executor.Block.Objects.Add(Clone);
                            }

                            MakeObservation(Subjects[0].ReferredToNames[0] + " splits into two!", Color.Green, new EntityList<Entity>() { Subjects[0] });

                            // Add the current date to the split dates list
                            Executor.SplitDates.Add(currentDate);
                        }
                        else
                        {
                            MakeObservation(Subjects[0].ReferredToNames[0] + " is not an object and cannot be split.", Color.Red, new EntityList<Entity>() { Subjects[0] });
                        }
                    }
                    else
                    {
                        MakeObservation("You can only split an object once per day.", Color.Red, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red, new EntityList<Entity>());
                }
            }



            else if (CommandID == "blip")
            {
                Executor.CooldownCycles += (int)(Math.Round(40 / Executor.Speed()));
                Executor.Energy -= 20 - Executor.PathOfRealityLevel;

                if (Executor.PathOfRealityLevel >= 8)
                {
                    MakeObservation("You tear a rift in reality...", Color.Purple, new EntityList<Entity>());

                    if (Subjects[0] == Executor.RealityBlipFocus)
                    {
                        Executor.RealityFocusTries += 1; // Increment focus count
                    }
                    else
                    {
                        Executor.RealityBlipFocus = Subjects[0];
                        Executor.RealityFocusTries = 1; // Reset focus count
                    }

                    switch (Executor.RealityFocusTries)
                    {
                        case 1:
                            MakeObservation($"...and focus your reality-bending energy on {Subjects[0].Name}...", Color.Purple, new EntityList<Entity>() { Subjects[0] });
                            break;
                        case 2:
                            MakeObservation($"...you feel a stronger connection to {Subjects[0].Name}'s essence...", Color.Purple, new EntityList<Entity>() { Subjects[0] });
                            break;
                        case 3:
                            if (Subjects[0] is Architect a)
                            {
                                Game1.GameWorld.AllHistoricalArchitects.Remove(a);

                                foreach (Architect A in Game1.GameWorld.AllHistoricalArchitects)
                                {
                                    var indicesToRemove = new List<int>();

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
                                    Game1.GameWorld.Colossals.Remove(a);
                                if (Game1.LoadedArchitects.Contains(a))
                                    Game1.LoadedArchitectsToRemove.Add(a);
                                foreach (Location l in Game1.GameWorld.AllLocations)
                                {
                                    if (l.Government == a)
                                        l.Government = null;
                                }
                                if (a.Room != null)
                                {
                                    a.Room.Architects.Remove(a);
                                    a.DropInventory(true);
                                }
                                else if (a.Block != null)
                                {
                                    a.Block.Architects.Remove(a);
                                    a.DropInventory(true);
                                }
                                a.IsAlive = false;
                                a.Location = null;
                                a.District = null;
                            }

                            Executor.Focus = 0; // Reset after successful expunge
                            Executor.RealityBlipFocus = null;
                            MakeObservation($"... and blip {Subjects[0].Name} from reality!", Color.Purple, new EntityList<Entity>() { Subjects[0] });
                            break;
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red, new EntityList<Entity>());
                }
            }
            else if (CommandID == "use_skill")
            {
                if (Executor.SkillsKnown.Contains(Subjects[0]))
                {
                    if (!Executor.UsedSkills.Contains(Subjects[0]))
                    {
                        Executor.CooldownCycles += (int)Math.Round(2 / Executor.Speed());

                        if (Subjects[0].Metadata == "deflect")
                        {
                            EntityList<Object> Objects = Executor.Room != null ? Executor.Room.Objects : Executor.Block.Objects;

                            bool Success = false;

                            MakeObservation("You enter a trance...", Color.LightCyan, new EntityList<Entity>());

                            foreach (Object o in Objects)
                            {
                                if (o.AirborneTarget == Executor)
                                {
                                    Architect InitialThrower = o.Thrower;
                                    Architect InitialTarget = Executor;

                                    MakeObservation("You deflect the " + o.ReferredToNames[0] + " back to " + o.Thrower.Name + "!", Color.LightCyan, new EntityList<Entity>() { o, o.Thrower });

                                    o.Thrower = InitialTarget;
                                    o.AirborneTarget = InitialThrower;
                                    o.AirborneCyclesToHitTarget += 10;
                                }
                                break;
                            }

                            if (!Success)
                            {
                                MakeObservation("There is nothing to deflect!", Color.Yellow, new EntityList<Entity>());
                            }
                            else
                            {
                                Executor.UsedSkills.Add(Subjects[0]);
                            }
                        }
                        else if (Subjects[0].Metadata == "dropkick")
                        {
                            if (Executor.CyclesSinceJump <= 30)
                            {
                                MakeObservation("You prepare to dropkick. Quickly make an attack with a foot or leg.", Color.Yellow, new EntityList<Entity>());
                                Executor.DropKickReady = true;
                                Executor.UsedSkills.Add(Subjects[0]);
                            }
                            else
                            {
                                MakeObservation("You need to have jumped in the last 3 seconds.", Color.Yellow, new EntityList<Entity>());
                            }
                        }
                        else if (Subjects[0].Metadata == "double strike")
                        {
                            MakeObservation("You prepare to double strike...", Color.LightCyan, new EntityList<Entity>());
                            Executor.UsedSkills.Add(Subjects[0]);
                            Executor.DoubleStrikeReady = true;
                        }
                        else if (Subjects[0].Metadata == "quick strike")
                        {
                            MakeObservation("You prepare to quick strike...", Color.LightCyan, new EntityList<Entity>());
                            Executor.UsedSkills.Add(Subjects[0]);
                            Executor.QuickStrikeReady = true;
                        }
                        else if (Subjects[0].Metadata == "severing strike")
                        {
                            MakeObservation("You prepare to sever your foe...", Color.LightCyan, new EntityList<Entity>());
                            Executor.UsedSkills.Add(Subjects[0]);
                            Executor.SeveringStrikeReady = true;
                        }
                        else if (Subjects[0].Metadata == "backflip")
                        {
                            MakeObservation("You backflip through the air!", Color.LightCyan, new EntityList<Entity>());
                            Executor.UsedSkills.Add(Subjects[0]);
                            Executor.ReactionBoostCycles += 60;
                        }
                        else if (Subjects[0].Metadata == "escape")
                        {
                            if (Executor.Room == null)
                            {
                                // Outside, teleport to an adjacent block.
                                var directionOffsets = new List<(int dx, int dz)>
                                {
                                    (0, -1),   // north
                                    (1, -1),   // northeast
                                    (1, 0),    // east
                                    (1, 1),    // southeast
                                    (0, 1),    // south
                                    (-1, 1),   // southwest
                                    (-1, 0),   // west
                                    (-1, -1)   // northwest
                                };

                                
                                bool Success = false;
                                int attempts = 0;

                                while (!Success && attempts < directionOffsets.Count())
                                {
                                    var randomOffset = directionOffsets[Game1.r.Next(directionOffsets.Count())];
                                    int newX = Executor.Block.X + randomOffset.dx;
                                    int newZ = Executor.Block.Z + randomOffset.dz;

                                    // Ensure the new position is within the bounds of the 7x7 grid
                                    if (newX >= 0 && newX < 7 && newZ >= 0 && newZ < 7)
                                    {
                                        Executor.Block.Architects.Remove(Executor);
                                        Executor.Block = Executor.District.DistrictMap[newX + newZ * 7];
                                        Executor.Block.Architects.Add(Executor);
                                        Success = true;
                                        Executor.UsedSkills.Add(Subjects[0]);
                                        MakeObservation("You travel to an adjacent block instantaneously.", Color.Green, new EntityList<Entity>());
                                    }

                                    attempts++;
                                }

                                if (!Success)
                                {
                                    MakeObservation("You couldn't find a valid block to escape to.", Color.Yellow, new EntityList<Entity>());
                                }
                            }
                            else
                            {
                                // Inside, teleport through an adjacent door.
                                bool Success = false;
                                EntityList<Object> doors = Executor.Room.Objects.Where(o => o.Type == "door");

                                if (doors.Count() > 0)
                                {
                                    
                                    Object randomDoor = doors[Game1.r.Next(doors.Count())];
                                    if (randomDoor is Door door)
                                    {
                                        Executor.Room.Architects.Remove(Executor);
                                        Executor.Room = door.DestinationRoom;
                                        Executor.Room.Architects.Add(Executor);
                                        Success = true;
                                        Executor.UsedSkills.Add(Subjects[0]);
                                        MakeObservation("You dash through a door instantaneously.", Color.Green, new EntityList<Entity>());
                                    }
                                }

                                if (!Success)
                                {
                                    // Use an exit door
                                    if (Executor.Structure != null && (Executor.Room == Executor.Structure.Rooms[0] || (Executor.Location.Layout == "archway" && Executor.Room == Executor.Structure.Rooms.Last())))
                                    {
                                        Executor.Room.Architects.Remove(Executor);
                                        Executor.Structure = null;
                                        Executor.Room = null;
                                        Executor.Block.Architects.Add(Executor);
                                        Executor.UsedSkills.Add(Subjects[0]);
                                        MakeObservation("You dash out the exit door instantaneously.", Color.Green, new EntityList<Entity>());
                                    }
                                    else
                                    {
                                        MakeObservation("There is not a door to escape through.", Color.Yellow, new EntityList<Entity>());
                                    }
                                }
                            }
                        }
                        else if (Subjects[0].Metadata == "finale")
                        {
                            MakeObservation("You prepare a final blow...", Color.Green, new EntityList<Entity>());
                            Executor.UsedSkills.Add(Subjects[0]);
                            Executor.FinaleReady = true;
                        }
                        else if (Subjects[0].Metadata == "concentration")
                        {
                            MakeObservation("You concentrate, gaining more focus.", Color.Green, new EntityList<Entity>());
                            Executor.UsedSkills.Add(Subjects[0]);
                            Executor.ExtraFocusTicks = 300;
                        }
                        else if (Subjects[0].Metadata == "body slam")
                        {
                            MakeObservation("You prepare to body slam...", Color.Green, new EntityList<Entity>());
                            Executor.UsedSkills.Add(Subjects[0]);
                            Executor.BodySlamReady = true;
                        }
                        else if (Subjects[0].Metadata == "leg sweep")
                        {
                            MakeObservation("You prepare to leg sweep...", Color.Green, new EntityList<Entity>());
                            Executor.UsedSkills.Add(Subjects[0]);
                            Executor.LegSweepReady = true;
                        }
                    }
                    else
                    {
                        MakeObservation("You've already used that skill this visit, and " + Executor.Name + " is bored of it. Leave the district to refresh your skills.", Color.Yellow, new EntityList<Entity>() { Executor });
                    }
                }
                else
                {
                    MakeObservation("That either isn't a skill, or you don't know the skill.", Color.OrangeRed, new EntityList<Entity>());
                }
            }
            else
            {
                string observationMessage = Executor.Name + " is not sure what you mean.";
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                // Ensure the directory exists
                string saveDirectory = Path.Combine(documentsPath, "LightrealmSaves");
                if (!Directory.Exists(saveDirectory))
                {
                    Directory.CreateDirectory(saveDirectory);
                }

                string contentPath = Path.Combine(saveDirectory, "failedCommands.txt");
                string failedCommand = CommandID + Environment.NewLine;

                // Write the failed command to the file using FileStream
                using (FileStream fileStream = new FileStream(contentPath, FileMode.Append, FileAccess.Write))
                {
                    byte[] failedCommandBytes = System.Text.Encoding.UTF8.GetBytes(failedCommand);
                    fileStream.Write(failedCommandBytes, 0, failedCommandBytes.Length);
                }

                MakeObservation(observationMessage, Color.Goldenrod, new EntityList<Entity>());

                return false;
            }




            //if we got to this point that means we exited the if statement by running a command successfully, therefore we can return "true"
            return (true);
        }

        
        public static void SendMessage(string MessageID, Architect Sender, Architect Receiver, EntityList<Entity> Subjects, World GameWorld)
        {
            Message DecidedMessage = null;

            string GetDirectionFromAngle(double angle)
            {
                if (angle >= 337.5 || angle < 22.5) return "east";
                else if (angle >= 22.5 && angle < 67.5) return "northeast";
                else if (angle >= 67.5 && angle < 112.5) return "north";
                else if (angle >= 112.5 && angle < 157.5) return "northwest";
                else if (angle >= 157.5 && angle < 202.5) return "west";
                else if (angle >= 202.5 && angle < 247.5) return "southwest";
                else if (angle >= 247.5 && angle < 292.5) return "south";
                else if (angle >= 292.5 && angle < 337.5) return "southeast";
                else return "unknown";
            }

            string GetDirectionFromDelta(double deltaX, double deltaZ)
            {
                double angle = Math.Atan2(deltaZ, deltaX) * (180 / Math.PI);
                if (angle < 0) angle += 360;

                if (angle >= 337.5 || angle < 22.5)
                    return "east";
                else if (angle >= 22.5 && angle < 67.5)
                    return "southeast";
                else if (angle >= 67.5 && angle < 112.5)
                    return "south";
                else if (angle >= 112.5 && angle < 157.5)
                    return "southwest";
                else if (angle >= 157.5 && angle < 202.5)
                    return "west";
                else if (angle >= 202.5 && angle < 247.5)
                    return "northwest";
                else if (angle >= 247.5 && angle < 292.5)
                    return "north";
                else if (angle >= 292.5 && angle < 337.5)
                    return "northeast";
                else
                    return "unknown";
            }


            string GetRandomDirection()
            {
                string[] directions = { "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest" };
                Random rnd = new Random();
                return directions[rnd.Next(directions.Length)];
            }

            string GetRandomLocation()
            {
                Random rnd = new Random();
                var randomLocation = GameWorld.AllLocations[rnd.Next(GameWorld.AllLocations.Count())];
                return randomLocation.Name;
            }

            string GetRandomDistrict(Location location)
            {
                Random rnd = new Random();
                var randomDistrict = location.Districts[rnd.Next(location.Districts.Count())];
                return randomDistrict.Name;
            }

            string GetRandomStructure(Location location)
            {
                Random rnd = new Random();
                var randomStructure = location.AllStructures[rnd.Next(location.AllStructures.Count())];
                return randomStructure.Name;
            }


            if (!CanUnderstandEachOther(Receiver, Sender))
            {
                Sender.AnnounceToParty(Receiver.ReferredToNames[0] + " cannot understand you.", Color.Yellow, new EntityList<Entity>() { Receiver });
            }
            else
            {
                if (MessageID == "ask_name")
                {
                    DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "question", MessageID,
                            "What is your name?", //content
                            "My name is " + Receiver.Name + ".", //truthful response
                            "There are some who call me \"" + Receiver.FalsifiedName + "\".", //made up response
                            "I'm not sure I remember.", //unknowing response
                            "Isn't the content of my character more important?", //derailing response
                            "It's not as pretty as your name." //flattering response
                        );
                }
                else if (MessageID == "ask_directions")
                {
                    if (Subjects[0] is Architect architect)
                    {
                        string mapUpdated = "";

                        if (Game1.LoadedArchitects.Contains(architect))
                        {
                            // Calculate direction based on the architect's block
                            double deltaX = architect.Block.X - Sender.Block.X;
                            double deltaZ = architect.Block.Z - Sender.Block.Z;
                            string direction = GetDirectionFromDelta(deltaX, deltaZ);

                            if (Sender.Location != architect.Block.District.Location)
                            {
                                mapUpdated = " [Map Updated]";
                            }

                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "question", MessageID,
                                "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?", // content
                                $"Yes, you need to travel {direction}.{mapUpdated}", // truthful/compliant response
                                $"Yes, you need to travel {GetRandomDirection()}.", // made up/denial response
                                "No, I don't know, unfortunately.", // unknowing/confused response
                                "They aren't very nice, you don't want to see them.", // derailing response
                                "Why see them when you could instead talk with me?" // flattering response
                            );

                            if (Sender.Location != architect.Block.District.Location)
                            {
                                // Store the location instead of revealing it directly
                                DecidedMessage.StoredRevealLocations.Add(architect.Block.District.Location);
                            }
                        }
                        else
                        {
                            if (architect.Location != null)
                            {
                                string locationName = architect.Location.Name;

                                if (Sender.Location != architect.Location)
                                {
                                    mapUpdated = " [Map Updated]";
                                }

                                DecidedMessage = new Message
                                (
                                    Sender, Receiver, Subjects, "question", MessageID,
                                    "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?", // content
                                    $"I saw them at {locationName} once.{mapUpdated}", // truthful/compliant response
                                    $"I saw them at {GetRandomLocation()} once.", // made up/denial response
                                    "No, I don't know, unfortunately.", // unknowing/confused response
                                    "They aren't very nice, you don't want to see them.", // derailing response
                                    "Why see them when you could instead talk with me?" // flattering response
                                );

                                if (Sender.Location != architect.Location)
                                {
                                    // Store the location instead of revealing it directly
                                    DecidedMessage.StoredRevealLocations.Add(architect.Location);
                                }
                            }
                            else
                            {
                                DecidedMessage = new Message
                                (
                                    Sender, Receiver, Subjects, "question", MessageID,
                                    "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?", // content
                                    "Wherever they are, it is lost to time.", // truthful/compliant response
                                    $"I saw them at {GetRandomLocation()} once.", // made up/denial response
                                    "No, I don't know, unfortunately.", // unknowing/confused response
                                    "They aren't very nice, you don't want to see them.", // derailing response
                                    "Why see them when you could instead talk with me?" // flattering response
                                );
                            }
                        }
                    }
                    else if (Subjects[0] is Object obj)
                    {
                        bool foundInLocation = false;
                        foreach (var district in Sender.Location.Districts)
                        {
                            foreach (var block in district.DistrictMap)
                            {
                                foreach (var structure in block.Structures)
                                {
                                    if (structure.Rooms.Any(room => room.Objects.Contains(obj)))
                                    {
                                        foundInLocation = true;

                                        DecidedMessage = new Message
                                        (
                                            Sender, Receiver, Subjects, "question", MessageID,
                                            "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?", // content
                                            $"I saw it in the structure {structure.Name}.", // truthful/compliant response
                                            $"I saw it in the structure {GetRandomStructure(Sender.Location)}.", // made up/denial response
                                            "No, I don't know, unfortunately.", // unknowing/confused response
                                            "I personally don't care for that object.", // derailing response
                                            "You are already beautiful without it." // flattering response
                                        );
                                        break;
                                    }
                                }
                                if (foundInLocation) break;
                            }
                            if (foundInLocation) break;
                        }

                        if (!foundInLocation)
                        {
                            foreach (var location in GameWorld.AllLocations)
                            {
                                foreach (var district in location.Districts)
                                {
                                    if (location.AllStructures.Any(s => s.HistoricalObjects.Contains(obj)))
                                    {
                                        Random rnd = new Random();
                                        var randomLocation = GameWorld.AllLocations[rnd.Next(GameWorld.AllLocations.Count())];
                                        var randomDistrict = randomLocation.Districts[rnd.Next(randomLocation.Districts.Count())];
                                        string mapUpdated = "";

                                        if (Sender.Location != location)
                                        {
                                            mapUpdated = " [Map Updated]";
                                        }

                                        DecidedMessage = new Message
                                        (
                                            Sender, Receiver, Subjects, "question", MessageID,
                                            "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?", // content
                                            $"I believe it is at {location.Name}, in the {district.Name} district.{mapUpdated}", // truthful/compliant response
                                            $"I belive it is at {randomLocation.Name}, in the {randomDistrict.Name} district.", // made up/denial response
                                            "No, I don't know, unfortunately.", // unknowing/confused response
                                            "I personally don't care for that object.", // derailing response
                                            "You are already beautiful without it." // flattering response
                                        );

                                        if (Sender.Location != location)
                                        {
                                            // Store the location instead of revealing it directly
                                            DecidedMessage.StoredRevealLocations.Add(location);
                                        }

                                        break;
                                    }
                                }
                                if (foundInLocation) break;
                            }
                        }
                    }
                    else if (Subjects[0] is Structure structure)
                    {
                        if (structure.Rooms.Contains(Sender.Room))
                        {
                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "question", MessageID,
                                "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?", // content
                                "We are already here.", // truthful/compliant response
                                $"You are currently inside {GetRandomStructure(Sender.Location)}.", // made up/denial response
                                "No, I don't know, unfortunately.", // unknowing/confused response
                                "I personally hate that place.", // derailing response
                                "It would be such a nice place to hang out..." // flattering response
                            );
                        }
                        else if (structure.Block == Sender.Block)
                        {
                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "question", MessageID,
                                "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?", // content
                                "It is right nearby.", // truthful/compliant response
                                $"It is to the {GetRandomDirection()}.", // made up/denial response
                                "No, I don't know, unfortunately.", // unknowing/confused response
                                "I personally hate that place.", // derailing response
                                "It would be such a nice place to hang out..." // flattering response
                            );
                        }
                        else if (structure.Block.District == Sender.Block.District)
                        {
                            // Calculate direction
                            double deltaX = structure.Block.X - Sender.Block.X;
                            double deltaZ = structure.Block.Z - Sender.Block.Z;
                            string direction = GetDirectionFromDelta(deltaX, deltaZ);

                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "question", MessageID,
                                "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?", // content
                                $"It is {direction} from here.", // truthful/compliant response
                                $"It is {GetRandomDirection()} from here.", // made up/denial response
                                "No, I don't know, unfortunately.", // unknowing/confused response
                                "I personally hate that place.", // derailing response
                                "It would be such a nice place to hang out..." // flattering response
                            );
                        }
                        else
                        {
                            string mapUpdated = "";

                            if (Sender.Location != structure.Block.District.Location)
                            {
                                mapUpdated = " [Map Updated]";
                            }

                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "question", MessageID,
                                "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?", // content
                                $"It is in {structure.Block.District.Location.Name}, in the {structure.Block.District.Name} district.{mapUpdated}", // truthful/compliant response
                                $"It is in {GetRandomLocation()}, in the {GetRandomDistrict(structure.Block.District.Location)} district.", // made up/denial response
                                "No, I don't know, unfortunately.", // unknowing/confused response
                                "I personally hate that place.", // derailing response
                                "It would be such a nice place to hang out..." // flattering response
                            );

                            if (Sender.Location != structure.Block.District.Location)
                            {
                                // Store the location instead of revealing it directly
                                DecidedMessage.StoredRevealLocations.Add(structure.Block.District.Location);
                            }
                        }
                    }
                    else if (Subjects[0] is Location location)
                    {
                        // Calculate direction
                        double deltaX = location.Region.X - Sender.Location.Region.X;
                        double deltaZ = location.Region.Z - Sender.Location.Region.Z;
                        string direction = GetDirectionFromDelta(deltaX, deltaZ);
                        string mapUpdated = "";

                        if (Sender.Location != location)
                        {
                            mapUpdated = " [Map Updated]";
                        }

                        DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "question", MessageID,
                            "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?", // content
                            $"It is {direction} from here.{mapUpdated}", // truthful/compliant response
                            $"It is {GetRandomDirection()} from here.", // made up/denial response
                            "No, I don't know, unfortunately.", // unknowing/confused response
                            "Do you not like " + Receiver.Location.Name + "?", // derailing response
                            "You're going to leave me?" // flattering response
                        );

                        if (Sender.Location != location)
                        {
                            // Store the location instead of revealing it directly
                            DecidedMessage.StoredRevealLocations.Add(location);
                        }
                    }
                    else if (Subjects[0] is Civilization civilization)
                    {
                        var capitol = civilization.Capitol;
                        // Calculate direction
                        double deltaX = capitol.Region.X - Sender.Location.Region.X;
                        double deltaZ = capitol.Region.Z - Sender.Location.Region.Z;
                        string direction = GetDirectionFromDelta(deltaX, deltaZ);
                        string mapUpdated = "";

                        DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "question", MessageID,
                            "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?", // content
                            $"Their capitol is {direction} from here.{mapUpdated}", // truthful/compliant response
                            $"Their capitol is {GetRandomDirection()} from here.", // made up/denial response
                            "No, I don't know, unfortunately.", // unknowing/confused response
                            "Do you not like " + Receiver.Location.HomeCivilization.Name + "?", // derailing response
                            "You're going to leave me?" // flattering response
                        );
                    }
                    else if (Subjects[0] is Group group)
                    {
                        var leader = group.Leader;
                        if (leader.Structure != null && leader.Block != null)
                        {
                            // Calculate direction
                            double deltaX = leader.Block.X - Sender.Block.X;
                            double deltaZ = leader.Block.Z - Sender.Block.Z;
                            string direction = GetDirectionFromDelta(deltaX, deltaZ);
                            string mapUpdated = "";

                            if (Sender.Location != leader.Structure.Block.District.Location)
                            {
                                mapUpdated = " [Map Updated]";
                            }

                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "question", MessageID,
                                "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?", // content
                                $"Yes, you need to travel {direction}.{mapUpdated}", // truthful/compliant response
                                $"Yes, you need to travel {GetRandomDirection()}.", // made up/denial response
                                "No, I don't know, unfortunately.", // unknowing/confused response
                                "They aren't that important.", // derailing response
                                "We could be a group, you know..." // flattering response
                            );

                            if (Sender.Location != leader.Structure.Block.District.Location)
                            {
                                // Store the location instead of revealing it directly
                                DecidedMessage.StoredRevealLocations.Add(leader.Structure.Block.District.Location);
                            }
                        }
                        else if (leader.Location != null)
                        {
                            string locationName = leader.Location.Name;
                            string mapUpdated = "";

                            if (Sender.Location != leader.Location)
                            {
                                mapUpdated = " [Map Updated]";
                            }

                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "question", MessageID,
                                "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?", // content
                                $"They are at {locationName}.{mapUpdated}", // truthful/compliant response
                                $"They are at {GetRandomLocation()}.", // made up/denial response
                                "No, I don't know, unfortunately.", // unknowing/confused response
                                "They aren't that important.", // derailing response
                                "We could be a group, you know..." // flattering response
                            );

                            if (Sender.Location != leader.Location)
                            {
                                // Store the location instead of revealing it directly
                                DecidedMessage.StoredRevealLocations.Add(leader.Location);
                            }
                        }
                        else
                        {
                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "question", MessageID,
                                "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?", // content
                                "Wherever they are, it is lost to time.", // truthful/compliant response
                                $"They are at {GetRandomLocation()}.", // made up/denial response
                                "No, I don't know, unfortunately.", // unknowing/confused response
                                "They aren't that important.", // derailing response
                                "We could be a group, you know..." // flattering response
                            );
                        }
                    }

                    if(DecidedMessage == null)
                    {
                        DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "question", MessageID,
                            "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?", // content
                            $"Sorry, I don't know.", // truthful/compliant response
                            $"Sorry, I don't know.", // made up/denial response
                            "Sorry, I don't know.", // unknowing/confused response
                            "Sorry, I don't know.", // derailing response
                            "Sorry, I don't know." // flattering response
                        );
                    }
                }


                // Method to calculate direction from delta


                else if (MessageID == "ask_generic_directions")
                {
                    string thing = Subjects[0].ReferredToNames[0];
                    (Region nearestRegion, Location nearestLocation, District nearestDistrict, Block nearestBlock, Room nearestRoom) = Sender.Block.FindNearestThing(thing);

                    if (nearestBlock != null)
                    {
                        if (nearestDistrict == Sender.District)
                        {
                            // Calculate direction
                            double deltaX = nearestBlock.X - Sender.Block.X;
                            double deltaZ = -(nearestBlock.Z - Sender.Block.Z); // Invert Z-axis
                            double angle = Math.Atan2(deltaZ, deltaX) * (180 / Math.PI);
                            if (angle < 0) angle += 360;
                            string direction = GetDirectionFromAngle(angle);

                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "question", MessageID,
                                $"Would you tell me where I can find a {thing}?", // content
                                $"Yes, you need to travel {direction} to find the nearest {thing}.", // truthful/compliant response
                                $"Yes, you need to travel {GetRandomDirection()} to find the nearest {thing}.", // made up/denial response
                                "No, I don't know, unfortunately.", // unknowing/confused response
                                $"You don't look like you need a {thing}", // derailing response
                                $"You don't need a {thing} as much as I need you." // flattering response
                            );
                        }
                        else if (nearestLocation == Sender.Location)
                        {
                            // It's in a different district but at this location
                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "question", MessageID,
                                $"Would you tell me where I can find a {thing}?", // content
                                $"Yes, you need to go to the {nearestDistrict.Name} district nearby to find the nearest {thing}.", // truthful/compliant response
                                $"Yes, you need to travel {GetRandomDirection()} to find the nearest {thing}.", // made up/denial response
                                "No, I don't know, unfortunately.", // unknowing/confused response
                                $"You don't look like you need a {thing}", // derailing response
                                $"You don't need a {thing} as much as I need you." // flattering response
                            );
                        }
                        else
                        {
                            // It's at a different location
                            // Calculate direction
                            double deltaX = nearestLocation.Region.X - Sender.Location.Region.X;
                            double deltaZ = -(nearestLocation.Region.Z - Sender.Location.Region.Z); // Invert Z-axis
                            string direction = GetDirectionFromDelta(deltaX, deltaZ);
                            string mapUpdated = " [Map Updated]";

                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "question", MessageID,
                                $"Would you tell me where I can find a {thing}?", // content
                                $"Yes, you need to travel {direction} to {nearestLocation.Name} to find the nearest {thing}.{mapUpdated}", // truthful/compliant response
                                $"Yes, you need to travel {GetRandomDirection()} to find the nearest {thing}.", // made up/denial response
                                "No, I don't know, unfortunately.", // unknowing/confused response
                                $"You don't look like you need a {thing}", // derailing response
                                $"You don't need a {thing} as much as I need you." // flattering response
                            );

                            if (Sender.Location != nearestLocation)
                            {
                                // Store the location instead of revealing it directly
                                DecidedMessage.StoredRevealLocations.Add(nearestLocation);
                            }
                        }
                    }
                    else
                    {
                        DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "question", MessageID,
                            $"Would you tell me where I can find a {thing}?", // content
                            "Wherever it is, it is lost to time.", // truthful/compliant response
                            $"I saw a {thing} at {GetRandomLocation()} once.", // made up/denial response
                            "No, I don't know, unfortunately.", // unknowing/confused response
                            $"You don't look like you need a {thing}", // derailing response
                            $"You don't need a {thing} as much as I need you." // flattering response
                        );
                    }
                }
                else if (MessageID == "ask_about_something")
                {
                    string entityType = Subjects[0].GetType().Name;
                    string truthfulResponse = "";
                    string madeUpResponse = "";

                    if (Subjects[0] is Architect Arch)
                    {
                        string genderName;
                        if (Arch.Age > 16)
                        {
                            genderName = Arch.Sex == "male" ? "man" : "woman";
                        }
                        else
                        {
                            genderName = Arch.Sex == "male" ? "boy" : "girl";
                        }

                        string locationName = Arch.HomeLocation != null ? Arch.HomeLocation.Name : "unknown location";

                        truthfulResponse = $"{Arch.Name} is a {Arch.Age}-year-old {genderName} from {locationName}. They are a {Arch.Profession}.";

                        // Generate a made-up response
                        Random rnd = new Random();
                        var randomArchitect = GameWorld.AllHistoricalArchitects.GetRandomItem();
                        string randomGenderName = randomArchitect.Age > 16 ? (randomArchitect.Sex == "male" ? "man" : "woman") : (randomArchitect.Sex == "male" ? "boy" : "girl");
                        string randomLocationName = randomArchitect.Location != null ? randomArchitect.Location.Name : "unknown location";
                        madeUpResponse = $"{randomArchitect.Name} is a {randomArchitect.Age}-year-old {randomGenderName} from {randomLocationName}, working as a {randomArchitect.Profession}.";
                    }
                    else
                    {
                        truthfulResponse = $"{Subjects[0].ReferredToNames[0]} is a {entityType}.";

                        // Generate a made-up response
                        Random rnd = new Random();
                        var randomArchitect = GameWorld.AllHistoricalArchitects.GetRandomItem();
                        string randomGenderName = randomArchitect.Age > 16 ? (randomArchitect.Sex == "male" ? "man" : "woman") : (randomArchitect.Sex == "male" ? "boy" : "girl");
                        string randomLocationName = randomArchitect.Location != null ? randomArchitect.Location.Name : "unknown location";
                        madeUpResponse = $"{randomArchitect.Name} is a {randomArchitect.Age}-year-old {randomGenderName} from {randomLocationName}, working as a {randomArchitect.Profession}.";
                    }


                    // Search for the last historical event that contains the subject's name
                    var lastEvent = GameWorld.HistoricalEvents.LastOrDefault(e => e.EventData.Contains(string.IsNullOrEmpty(Subjects[0].Name) ? Subjects[0].ReferredToNames[0] : Subjects[0].Name));
                    string historicalEvent = lastEvent != null ? lastEvent.EventData : "";

                    truthfulResponse += " "+historicalEvent;
                    madeUpResponse += " "+historicalEvent;
                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "question", MessageID,
                        $"Can you tell me about {Subjects[0].ReferredToNames[0]}?", //content
                        truthfulResponse, //truthful/compliant response
                        madeUpResponse, //made up/denial response
                        "I'm not sure, sorry.", //unknowing/confused response
                        "Why do you want to know about that?", //derailing response
                        $"Why talk about {Subjects[0].ReferredToNames[0]} when we could talk about you?" //flattering response
                    );
                }
                else if (MessageID == "ask_ruler")
                {
                    if(Sender.Location.Government != null)
                    {
                        string truthfulGovernment = Sender.Location.Government.Name;
                        Random rnd = new Random();
                        var randomLocation = GameWorld.AllLocations[rnd.Next(GameWorld.AllLocations.Count())];
                        string madeUpGovernment = randomLocation.Government.Name;

                        DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "question", MessageID,
                            "Who rules this place?", //content
                            $"{truthfulGovernment} is our government.", //truthful/compliant response
                            $"{madeUpGovernment} is our government.", //made up/denial response
                            "I'm not sure, sorry.", //unknowing/confused response
                            "Anarchy is truly superior.", //derailing response
                            $"I'm not sure... do you govern me?" //flattering response
                        );
                    }
                    else
                    {
                        Random rnd = new Random();
                        var randomLocation = GameWorld.AllLocations[rnd.Next(GameWorld.AllLocations.Count())];
                        string madeUpGovernment = randomLocation.Government.Name;

                        DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "question", MessageID,
                            "Who rules this place?", //content
                            $"We are an anarcho-syndicalist commune at the moment.", //truthful/compliant response
                            $"{madeUpGovernment} is our government.", //made up/denial response
                            "I'm not sure, sorry.", //unknowing/confused response
                            "Anarchy is truly superior.", //derailing response
                            $"I'm not sure... do you govern me?" //flattering response
                        );
                    }
                }
                else if (MessageID == "ask_trade")
                {
                    (Region nearestRegion, Location nearestLocation, District nearestDistrict, Block nearestBlock, Room nearestRoom) = Sender.Block.FindNearestThing("market");

                    if (nearestBlock != null && nearestLocation == Sender.Location)
                    {
                        // Calculate direction if in the same district
                        if (nearestDistrict == Sender.Block.District)
                        {
                            double deltaX = nearestBlock.X - Sender.Block.X;
                            double deltaZ = nearestBlock.Z - Sender.Block.Z;
                            string direction = GetDirectionFromDelta(deltaX, deltaZ);


                            string TruthfulResponsse = "";
                            if (Sender.Structure != null && Sender.Structure.Type == "market")
                            {
                                TruthfulResponsse = $"Yes, but we are just here to ledger your purchases. Please take whatever you wish, but leave better value. Do not cross me, my debtshibas are always watching.";
                            }
                            else
                            {
                                TruthfulResponsse = $"We have a market dedicated to trade {direction} in this district.";
                            }

                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "question", MessageID,
                                "I'd like to trade.", //content
                                TruthfulResponsse, //truthful/compliant response
                                "We don't do trade here.", //made up/denial response
                                "What is trade?", //unknowing/confused response
                                "I prefer communism.", //derailing response
                                "You look valuable..." //flattering response
                            );
                        }
                        else
                        {
                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "question", MessageID,
                                "I'd like to trade.", //content
                                $"We have a market dedicated to trade in {nearestDistrict.Name}, a nearby district.", //truthful/compliant response
                                "We don't do trade here.", //made up/denial response
                                "What is trade?", //unknowing/confused response
                                "I prefer communism.", //derailing response
                                "You look valuable..." //flattering response
                            );
                        }
                    }
                    else
                    {
                        DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "question", MessageID,
                            "I'd like to trade.", //content
                            "We don't have a dedicated market set up yet.", //truthful/compliant response
                            "We don't do trade here.", //made up/denial response
                            "What is trade?", //unknowing/confused response
                            "I prefer communism.", //derailing response
                            "You look valuable..." //flattering response
                        );
                    }
                }
                else if (MessageID == "ask_them_join")
                {
                    DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "request", MessageID,
                            "Join me on my quest!", //content
                            "I would be honored. I accept.", //truthful/compliant response
                            "Sorry, I belong here.", //made up/denial response
                            "What? Where?", //unknowing/confused response
                            "If one could even call it a group...", //derailing response
                            "If it means I spend more time with you." //flattering response

                        );
                }
                else if (MessageID == "ask_to_join")
                {
                    string response;

                    if (Receiver.Group != null)
                    {
                        response = "Yes, welcome to " + Receiver.Group.Name + ".";
                    }
                    else
                    {
                        response = "Yes, welcome to my new group. Not sure what I'll call it.";
                    }

                    DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "question", MessageID,
                            "May I join your group?", // content
                            response, // truthful/compliant response
                            "Sorry, we aren't looking for members at this time.", // made up/denial response
                            "What? What group?", // unknowing/confused response
                            "Can I join YOUR group?", // derailing response
                            "If it means you spend more time with me." // flattering response
                        );
                }

                else if (MessageID == "ask_current_structure")
                {
                    Structure currentStructure = Receiver.Structure;
                    string truthfulResponse = "";
                    string madeUpResponse = "";
                    string historicalEvent = "";

                    if (currentStructure != null)
                    {
                        // Get the last historical event related to the current structure
                        var lastEvent = GameWorld.HistoricalEvents.LastOrDefault(e => e.EventData.Contains(currentStructure.Name));
                        if (lastEvent != null)
                        {
                            historicalEvent = $" {lastEvent.EventData}";
                        }

                        truthfulResponse = $"You are in {currentStructure.Name}.{historicalEvent}";

                        // Generate a made-up response
                        Random rnd = new Random();
                        var randomLocation = GameWorld.AllLocations[rnd.Next(GameWorld.AllLocations.Count())];
                        var randomStructure = randomLocation.AllStructures[rnd.Next(randomLocation.AllStructures.Count())];
                        var randomEvent = GameWorld.HistoricalEvents[rnd.Next(GameWorld.HistoricalEvents.Count())].EventData;
                        madeUpResponse = $"You are in {currentStructure.Name}. {randomEvent}";
                    }
                    else
                    {
                        truthfulResponse = "We are outside...";
                        madeUpResponse = "The structure of reality, which binds to your every move.";
                    }

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "question", MessageID,
                        "Can you tell me about this structure?", //content
                        truthfulResponse, //truthful/compliant response
                        madeUpResponse, //made up/denial response
                        "I don't know what this place is.", //unknowing/confused response
                        "Why do you want to know about this place?", //derailing response
                        "This place suits you perfectly!" //flattering response
                    );
                }
                else if (MessageID == "ask_location")
                {
                    Location location = Receiver.Location;
                    string truthfulResponse = "";
                    string madeUpResponse = "";
                    string historicalEvent = "";

                    if (location != null)
                    {
                        // Get the last historical event related to the current structure
                        var lastEvent = GameWorld.HistoricalEvents.LastOrDefault(e => e.EventData.Contains(location.Name));
                        if (lastEvent != null)
                        {
                            historicalEvent = $" {lastEvent}";
                        }

                        truthfulResponse = $"You are at {location.Name}. {historicalEvent}";

                        // Generate a made-up response
                        Random rnd = new Random();
                        var randomLocation = GameWorld.AllLocations[rnd.Next(GameWorld.AllLocations.Count())];
                        var randomEvent = GameWorld.HistoricalEvents[rnd.Next(GameWorld.HistoricalEvents.Count())];
                        madeUpResponse = $"You are in {randomLocation.Name}. {randomEvent}";
                    }
                    else
                    {
                        truthfulResponse = "Are we even in reality?";
                        madeUpResponse = "Our location is irrelevant to reality.";
                    }

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "question", MessageID,
                        "Can you tell me about this location?", //content
                        truthfulResponse, //truthful/compliant response
                        madeUpResponse, //made up/denial response
                        "I don't know what this area is.", //unknowing/confused response
                        "Why do you care?", //derailing response
                        "I love this place, and you here!" //flattering response
                    );
                }
                else if (MessageID == "ask_profession")
                {
                    string truthfulResponse = "";
                    string madeUpResponse = "";

                    truthfulResponse = $"I am a {Receiver.Profession}.";

                    // Generate a made-up response
                    Random rnd = new Random();
                    var randomArchitect = GameWorld.AllHistoricalArchitects.GetRandomItem();
                    madeUpResponse = $"I am a {randomArchitect.Profession}.";

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "question", MessageID,
                        "What is your profession?", //content
                        truthfulResponse, //truthful/compliant response
                        madeUpResponse, //made up/denial response
                        "What is a profession?", //unknowing/confused response
                        "I believe that to be irrelevant.", //derailing response
                        "It's not as interesting as what you do." //flattering response
                    );
                }
                else if (MessageID == "ask_traveling")
                {
                    string truthfulResponse = "";
                    string madeUpResponse = "I'm going nowhere in particular.";

                    truthfulResponse = Receiver.MigrationReason;

                    if (String.IsNullOrEmpty(truthfulResponse))
                        truthfulResponse = "I'm not headed anywhere far from here.";

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "question", MessageID,
                        "Where are you traveling to?", //content
                        truthfulResponse, //truthful/compliant response
                        madeUpResponse, //made up/denial response
                        "What do you mean?", //unknowing/confused response
                        "Life is like a road, we are always traveling down it.", //derailing response
                        "Nowhere, can I travel with you?" //flattering response
                    );
                }
                else if (MessageID == "greet")
                {
                    string[] greetings = { "Hello", "Hail", "Greetings", "Salutations", "Hey", "Good day" };
                    Random rnd = new Random();
                    string randomGreeting = greetings[rnd.Next(greetings.Length)];

                    string greetingContent = Sender.KnownArchitects.Contains(Receiver) ?
                                             $"{randomGreeting}, {Receiver.Name}." :
                                             $"{randomGreeting}, {Receiver.Profession}.";

                    string truthfulResponse = greetings[rnd.Next(greetings.Length)] + ". How can I assist you today?";
                    string denialResponse = "Do not speak to me, " +
                                            Sender.Profession + ".";
                    string confusedResponse = greetings[rnd.Next(greetings.Length)] + ", um... do I know you?";
                    string derailingResponse = "Spare me the formalities.";
                    string flatteringResponse = "Ah, " +
                                                (Receiver.KnownArchitects.Contains(Sender) ? Sender.Name : Sender.Profession) +
                                                "! Your presence brightens my day! How can I help?";

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "request", MessageID,
                        greetingContent, // content
                        truthfulResponse, // truthful/compliant response
                        denialResponse, // made up/denial response
                        confusedResponse, // unknowing/confused response
                        derailingResponse, // derailing response
                        flatteringResponse // flattering response
                    );
                }

                else if (MessageID == "farewell")
                {
                    string[] farewells = { "Goodbye", "Farewell", "See you", "Take care", "Adieu", "Until next time" };
                    Random rnd = new Random();
                    string randomFarewell = farewells[rnd.Next(farewells.Length)];

                    string farewellContent = Sender.KnownArchitects.Contains(Receiver) ?
                                             $"{randomFarewell}, {Receiver.Name}." :
                                             $"{randomFarewell}, {Receiver.Profession}.";

                    string truthfulResponse = randomFarewell + ", " +
                                              (Receiver.KnownArchitects.Contains(Sender) ? Sender.Name : Sender.Profession) + ".";
                    string denialResponse = "Oh. " + farewells[rnd.Next(farewells.Length)] + ", " +
                                            (Receiver.KnownArchitects.Contains(Sender) ? Sender.Name : Sender.Profession) + ".";
                    string confusedResponse = "I guess this is goodbye?";
                    string derailingResponse = "Finally, some peace and quiet.";
                    string flatteringResponse = "I miss you already, " +
                                                (Receiver.KnownArchitects.Contains(Sender) ? Sender.Name : Sender.Profession) + "!";

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "question", MessageID,
                        farewellContent, // content
                        truthfulResponse, // truthful/compliant response
                        denialResponse, // made up/denial response
                        confusedResponse, // unknowing/confused response
                        derailingResponse, // derailing response
                        flatteringResponse // flattering response
                    );
                }

                else if (MessageID == "thank")
                {
                    string[] thanks = { "Thank you", "I am grateful", "Many thanks", "I appreciate it", "You have my gratitude" };
                    Random rnd = new Random();
                    string randomThank = thanks[rnd.Next(thanks.Length)];

                    string thankContent = Sender.KnownArchitects.Contains(Receiver) ?
                                          $"{randomThank}, {Receiver.Name}." :
                                          $"{randomThank}, {Receiver.Profession}.";

                    string truthfulResponse = "You are most welcome.";
                    string denialResponse = "It was nothing, really.";
                    string confusedResponse = "Why are you thanking me?";
                    string derailingResponse = "Enough of the formalities.";
                    string flatteringResponse = "Your gratitude is greatly appreciated.";

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "question", MessageID,
                        thankContent, // content
                        truthfulResponse, // truthful/compliant response
                        denialResponse, // made up/denial response
                        confusedResponse, // unknowing/confused response
                        derailingResponse, // derailing response
                        flatteringResponse // flattering response
                    );
                }
                else if (MessageID == "apologize")
                {
                    string[] apologies = { "I am sorry", "I sincerely apologize", "Sincerest apologies", "My apologies", "I apologize" };
                    Random rnd = new Random();
                    string randomApology = apologies[rnd.Next(apologies.Length)];

                    string apologyContent = Sender.KnownArchitects.Contains(Receiver) ?
                                            $"{randomApology}, {Receiver.Name}." :
                                            $"{randomApology}, {Receiver.Profession}.";

                    string truthfulResponse = "It is alright, I forgive you.";
                    string denialResponse = "There is nothing to forgive.";
                    string confusedResponse = "Why are you apologizing?";
                    string derailingResponse = "About time.";
                    string flatteringResponse = "Your apology is accepted with grace.";

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "request", MessageID,
                        apologyContent, // content
                        truthfulResponse, // truthful/compliant response
                        denialResponse, // made up/denial response
                        confusedResponse, // unknowing/confused response
                        derailingResponse, // derailing response
                        flatteringResponse // flattering response
                    );
                }

                else if (MessageID == "ask_health")
                {
                    string GenerateHealthReport(Architect architect)
                    {
                        var healthReport = new StringBuilder();
                        Random rnd = new Random();

                        // Select a random body part from the architect
                        var randomBodyPart = architect.BodyParts[rnd.Next(architect.BodyParts.Count())];

                        // Determine the condition of the selected body part
                        string condition;
                        if (randomBodyPart.Integrity > 80) condition = "excellent";
                        else if (randomBodyPart.Integrity > 60) condition = "good";
                        else if (randomBodyPart.Integrity > 40) condition = "fair";
                        else if (randomBodyPart.Integrity > 20) condition = "poor";
                        else condition = "critical";

                        healthReport.Append($" My {randomBodyPart.Type} is in {condition} condition.");

                        // Calculate energy percentage
                        int energyPercentage = (int)Math.Round(((double)architect.Energy / architect.MaxEnergy) * 100);

                        // Determine the energy condition
                        string energyCondition;
                        if (energyPercentage > 80) energyCondition = "full of energy";
                        else if (energyPercentage > 60) energyCondition = "energetic";
                        else if (energyPercentage > 40) energyCondition = "okay";
                        else if (energyPercentage > 20) energyCondition = "tired";
                        else energyCondition = "exhausted";

                        healthReport.Append($" I am {energyCondition}.");

                        return healthReport.ToString();
                    }





                    string GenerateMadeUpHealthReport(Architect architect)
                    {
                        Random rnd = new Random();
                        var healthReport = new StringBuilder();

                        // Randomly select a body part from the architect
                        var randomBodyPart = architect.BodyParts[rnd.Next(architect.BodyParts.Count())];

                        // Generate a random integrity value with a bias towards higher numbers
                        int biasedRandomInt(int min, int max)
                        {
                            return rnd.Next(min, max) * rnd.Next(min, max) / max;
                        }
                        int randomIntegrity = biasedRandomInt(40, 100);

                        string condition;
                        if (randomIntegrity > 80) condition = "excellent";
                        else if (randomIntegrity > 60) condition = "good";
                        else if (randomIntegrity > 40) condition = "fair";
                        else if (randomIntegrity > 20) condition = "poor";
                        else condition = "critical";

                        healthReport.Append($" My {randomBodyPart.Type} is in {condition} condition.");

                        // Generate a random energy value with a bias towards higher numbers
                        int randomEnergy = biasedRandomInt(40, 100);
                        int energyPercentage = (int)Math.Round(((double)randomEnergy / 100) * 100);

                        string energyCondition;
                        if (energyPercentage > 80) energyCondition = "full of energy";
                        else if (energyPercentage > 60) energyCondition = "energetic";
                        else if (energyPercentage > 40) energyCondition = "okay";
                        else if (energyPercentage > 20) energyCondition = "tired";
                        else energyCondition = "exhausted";

                        healthReport.Append($" I am {energyCondition}.");

                        return healthReport.ToString();
                    }


                    string truthfulResponse = "";
                    string madeUpResponse = "";

                    if (Receiver is Architect architect)
                    {
                        truthfulResponse = GenerateHealthReport(architect);
                        madeUpResponse = GenerateMadeUpHealthReport(architect);
                    }
                    else
                    {
                        truthfulResponse = "I cannot assess your health.";
                        madeUpResponse = "I cannot assess your health.";
                    }

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "question", MessageID,
                        "How are you feeling?", //content
                        truthfulResponse, //truthful/compliant response
                        madeUpResponse, //made up/denial response
                        "I'm not sure how to describe it.", //unknowing/confused response
                        "Health is a state of mind.", //derailing response
                        "Not as good as you!" //flattering response
                    );
                }
                else if (MessageID == "ask_news")
                {
                    string truthfulResponse = "";
                    string madeUpResponse = "";
                    string flatteringResponse = "You arrived here.";

                    // Get the latest 5 (or fewer) events where Event.Region.Location is not null and Location == Sender.Location
                    var latestEvents = GameWorld.HistoricalEvents
                        .Where(e => e.Region?.Location != null && e.Region.Location == Sender.Location && e.Significant)
                        .TakeLast(5)
                        .ToList();

                    if (latestEvents.Count() > 0)
                    {
                        Random rnd = new Random();
                        var randomEvent = latestEvents[rnd.Next(latestEvents.Count)];
                        truthfulResponse = $"Recently, {randomEvent.EventData}";

                        // First, search for significant events at a random location
                        var randomLocation = GameWorld.AllLocations[rnd.Next(GameWorld.AllLocations.Count)];
                        var randomLocationEvents = GameWorld.HistoricalEvents
                            .Where(e => e.Region?.Location != null && e.Region.Location == randomLocation && e.Significant)
                            .TakeLast(5)
                            .ToList();

                        // If no significant events found, search again without significance filter
                        if (randomLocationEvents.Count == 0)
                        {
                            randomLocationEvents = GameWorld.HistoricalEvents
                                .Where(e => e.Region?.Location != null && e.Region.Location == randomLocation)
                                .TakeLast(5)
                                .ToList();
                        }

                        // Generate the made-up response based on found events or fallback
                        if (randomLocationEvents.Count > 0)
                        {
                            var randomMadeUpEvent = randomLocationEvents[rnd.Next(randomLocationEvents.Count)];
                            madeUpResponse = $"Recently, {randomMadeUpEvent.EventData}";
                        }
                        else
                        {
                            madeUpResponse = "Nothing too interesting has happened here.";
                        }
                    }

                    else
                    {
                        truthfulResponse = "Nothing too interesting has happened here.";
                        madeUpResponse = "Nothing too interesting has happened here.";
                    }

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "question", MessageID,
                        "What's the latest news here?", //content
                        truthfulResponse, //truthful/compliant response
                        madeUpResponse, //made up/denial response
                        "I'm not sure, sorry.", //unknowing/confused response
                        "The future is more important than the past.", //derailing response
                        flatteringResponse //flattering response
                    );
                }

                else if (MessageID == "challenge")
                {
                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "request", MessageID,
                        "I challenge you to a duel!", //content
                        "I accept your challenge!", //truthful/compliant response
                        "I am not interested at this time.", //made up/denial response
                        "Why would we need to fight?", //unknowing/confused response
                        "Fighting solves nothing.", //derailing response
                        "I don't think I can defeat you... but sure." //flattering response
                    );
                }

                else if (MessageID == "ask_story")
                {
                    string truthfulResponse = "";
                    string madeUpResponse = "";
                    string receiverName = Receiver.Name;

                    // Find all historical events containing the receiver's name
                    var events = GameWorld.HistoricalEvents
                        .Where(e => e.EventData.Contains(receiverName))
                        .TakeLast(5)
                        .Select(e =>
                        {
                            int endIndex = e.EventData.IndexOf(") ") + 2;
                            string processedEvent = e.EventData.Substring(endIndex);
                            processedEvent = processedEvent.Replace(receiverName, "I");
                            int yearStart = e.EventData.IndexOf("/") + 1;
                            int yearEnd = e.EventData.IndexOf(")", yearStart);
                            string year = e.EventData.Substring(yearStart, yearEnd - yearStart);
                            return $"In {year}, {processedEvent}";
                        })
                        ;

                    if (events.Count() > 0)
                    {
                        truthfulResponse = string.Join(" ", events);
                    }
                    else
                    {
                        truthfulResponse = "Nothing too important happened to me.";
                    }

                    // Generate a made-up response
                    Random rnd = new Random();
                    var randomArchitect = GameWorld.AllHistoricalArchitects.GetRandomItem();
                    var randomEvents = GameWorld.HistoricalEvents
                        .Where(e => e.EventData.Contains(randomArchitect.Name))
                        .TakeLast(5)
                        .Select(e =>
                        {
                            int endIndex = e.EventData.IndexOf(") ") + 2;
                            string processedEvent = e.EventData.Substring(endIndex);
                            processedEvent = processedEvent.Replace(randomArchitect.Name, "I");
                            int yearStart = e.EventData.IndexOf("/") + 1;
                            int yearEnd = e.EventData.IndexOf(")", yearStart);
                            string year = e.EventData.Substring(yearStart, yearEnd - yearStart);
                            return $"In {year}, {processedEvent}";
                        })
                        ;

                    if (randomEvents.Count() > 0)
                    {
                        madeUpResponse = string.Join(" ", randomEvents);
                    }
                    else
                    {
                        madeUpResponse = "Nothing too important happened to me.";
                    }

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "question", MessageID,
                        "Tell me your story.", //content
                        truthfulResponse, //truthful/compliant response
                        madeUpResponse, //made up/denial response
                        "I'm not sure what to say.", //unknowing/confused response
                        "Stories are for another time.", //derailing response
                        "I'd bet its not as interesting as yours!" //flattering response
                    );
                }
                else if (MessageID == "ask_history")
                {
                    string truthfulResponse = "";
                    string madeUpResponse = "";
                    string subjectName = Subjects[0].Name;

                    // Find all historical events containing the subject's name
                    var events = GameWorld.HistoricalEvents
                        .Where(e => e.EventData.Contains(subjectName))
                        .TakeLast(5)
                        .Select(e =>
                        {
                            int endIndex = e.EventData.IndexOf(") ") + 2;
                            string processedEvent = e.EventData.Substring(endIndex);
                            int yearStart = e.EventData.IndexOf("/") + 1;
                            int yearEnd = e.EventData.IndexOf(")", yearStart);
                            string year = e.EventData.Substring(yearStart, yearEnd - yearStart);
                            return $"In {year}, {processedEvent}";
                        })
                        ;

                    if (events.Count() > 0)
                    {
                        truthfulResponse = string.Join(" ", events);
                    }
                    else
                    {
                        truthfulResponse = "Nothing too important happened here.";
                    }

                    // Generate a made-up response
                    Random rnd = new Random();
                    var randomLocation = GameWorld.AllLocations[rnd.Next(GameWorld.AllLocations.Count())];
                    var randomEvents = GameWorld.HistoricalEvents
                        .Where(e => e.EventData.Contains(randomLocation.Name))
                        .TakeLast(5)
                        .Select(e =>
                        {
                            int endIndex = e.EventData.IndexOf(") ") + 2;
                            string processedEvent = e.EventData.Substring(endIndex);
                            int yearStart = e.EventData.IndexOf("/") + 1;
                            int yearEnd = e.EventData.IndexOf(")", yearStart);
                            string year = e.EventData.Substring(yearStart, yearEnd - yearStart);
                            return $"In {year}, {processedEvent}";
                        })
                        ;

                    if (randomEvents.Count() > 0)
                    {
                        madeUpResponse = string.Join(" ", randomEvents);
                    }
                    else
                    {
                        madeUpResponse = "Nothing too important happened here.";
                    }

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "question", MessageID,
                        "Can you tell me about the history here?", //content
                        truthfulResponse, //truthful/compliant response
                        madeUpResponse, //made up/denial response
                        "I'm not sure what to say.", //unknowing/confused response
                        "History is best left in the past.", //derailing response
                        "You arrived here, thats been pretty nice!" //flattering response
                    );
                }
                else if (MessageID == "ask_opinion")
                {
                    string GetOpinionDescription(int opinion)
                    {
                        if (opinion > 75) return "think very fondly of";
                        else if (opinion > 50) return "have a positive view of";
                        else if (opinion > 25) return "like";
                        else if (opinion > 0) return "have a slightly positive opinion of";
                        else if (opinion == 0) return "feel neutral about";
                        else if (opinion > -25) return "dont really understand";
                        else if (opinion > -50) return "dislike";
                        else if (opinion > -75) return "have a negative view of";
                        else return "absolutely detest";
                    }

                    int opinion;
                    if (Subjects[0] is Architect subjectArchitect)
                    {
                        opinion = Receiver.GetOpinion(subjectArchitect);
                    }
                    else
                    {
                        Random rnd = new Random();
                        opinion = rnd.Next(-100, 101);
                    }

                    string opinionDescription = GetOpinionDescription(opinion);
                    string truthfulResponse = $"I {opinionDescription} {Subjects[0].ReferredToNames[0]}.";
                    string madeUpResponse = $"I {GetOpinionDescription(new Random().Next(-100, 101))} {Subjects[0].ReferredToNames[0]}.";

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "question", MessageID,
                        $"What do you think of {Subjects[0].ReferredToNames[0]}?", //content
                        truthfulResponse, //truthful/compliant response
                        madeUpResponse, //made up/denial response
                        "I'm not sure what to think.", //unknowing/confused response
                        "Why do you care about my opinion?", //derailing response
                        $"I don't know, but you are amazing!" //flattering response
                    );
                }
                else if (MessageID == "ask_interests")
                {
                    string truthfulResponse = "";
                    string madeUpResponse = "";
                    string derailingResponse = "";

                    // Determine the receiver's highest proficiency
                    if (Receiver.XPValues.Count() > 0)
                    {
                        var highestProficiency = Receiver.XPValues.OrderByDescending(xp => xp.Item2).First();
                        truthfulResponse = $"I am very interested in {highestProficiency.Item1}.";
                    }
                    else
                    {
                        truthfulResponse = "I am thinking about taking up shiba taming.";
                    }

                    // Generate a made-up response
                    Random rnd = new Random();
                    var randomProficiency = Receiver.XPValues.Count() > 0 ? Receiver.XPValues[rnd.Next(Receiver.XPValues.Count())] : ("alchemy", rnd.Next(1, 101));
                    madeUpResponse = $"I am very interested in {randomProficiency.Item1}.";

                    // Determine the sender's lowest proficiency
                    if (Sender.XPValues.Count() > 0)
                    {
                        var lowestProficiency = Sender.XPValues.OrderBy(xp => xp.Item2).First();
                        derailingResponse = $"I can't help but notice how abysmal your {lowestProficiency.Item1} is.";
                    }
                    else
                    {
                        derailingResponse = "I can't help but notice how abysmal your skills are.";
                    }

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "question", MessageID,
                        "What are you interested in?", //content
                        truthfulResponse, //truthful/compliant response
                        madeUpResponse, //made up/denial response
                        "I'm not really sure yet.", //unknowing/confused response
                        derailingResponse, //derailing response
                        "You." //flattering response
                    );
                }
                else if (MessageID == "ask_family")
                {
                    DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "question", MessageID,
                            "Do you have family alive?", //content
                            "I don't know.", //truthful/compliant response
                            "Oh, they're around... somewhere...", //made up/denial response
                            "What is a family?", //unknowing/confused response
                            "Your family is a family.", //derailing response
                            "You are my family." //flattering response

                        );
                }
                else if (MessageID == "provide_assistance")
                {
                    List<string> DeathLocationTypes = new List<string>
    {
        "keep",
        "fortress",
        "monument",
        "sanctum",
        "cove",
        "commune",
        "stronghold",
        "outpost",
        "core",
        "tower",
        "spire"
    };

                    string truthfulResponse = "";
                    string madeUpResponse = "Nothing is wrong.";

                    // Find the nearest location with a TruePopulation > 0 and a type in DeathLocationTypes
                    var nearestLocation = GameWorld.AllLocations
                        .Where(loc => loc.TruePopulation() > 0 && DeathLocationTypes.Contains(loc.Type))
                        .OrderBy(loc =>
                        {
                            double deltaX = loc.Region.X - Sender.Location.Region.X;
                            double deltaZ = loc.Region.Z - Sender.Location.Region.Z;
                            return Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
                        })
                        .FirstOrDefault();

                    EntityList<Location> StoredReveal = new EntityList<Location>();

                    if (nearestLocation != null)
                    {
                        truthfulResponse = $"I am worried about {nearestLocation.Name}, a {nearestLocation.Type}. You can find it at... [Map Updated]";

                        // Store the location instead of revealing it directly
                        StoredReveal.Add(nearestLocation);
                    }
                    else
                    {
                        truthfulResponse = "Nothing is wrong.";
                    }

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "question", MessageID,
                        "Do you need help with something?", // content
                        truthfulResponse, // truthful/compliant response
                        madeUpResponse, // made up/denial response
                        "I'm not really sure.", // unknowing/confused response
                        "No. Do you?", // derailing response
                        "Your concern is appreciated." // flattering response
                    );
                    DecidedMessage.StoredRevealLocations.AddRange(StoredReveal);
                }

                else if (MessageID == "ask_advice")
                {
                    List<string> adviceList = new List<string>
                    {
                        "Follow your heart.",
                        "There are upsides and downsides to X.",
                        "Only time will tell.",
                        "If you really think X is what you want, then go for it.",
                        "It's a journey, not a destination.",
                        "The X lies within you.",
                        "What is to be, will be.",
                        "The best path is the one you make for yourself.",
                        "All things will be balanced in the end.",
                        "No matter what, we will find a way.",
                        "X is part of a grander scheme in life.",
                        "X wants to change, but it stays the same.",
                        "The unknown holds a great potential.",
                        "Whatever you do, do not follow your heart.",
                        "Every step forward is a step toward understanding.",
                        "Maybe the real X were the friends we made along the way.",
                        "The answers will come when you least expect them.",
                        "Life's greatest secrets are revealed in due time.",
                        "Trust in the flow of reality.",
                        "Each moment is a piece of the larger puzzle.",
                        "The path may reveal itself if you only follow.",
                        "If you wish to know the future, you must know the past.",
                        "All things happen for a reason.",
                        "Search your heart or search your mind, but not both.",
                        "Let the tides of fate carry you.",
                        "In stillness, the truth becomes clear.",
                        "The soul's journey is never straightforward.",
                        "X is at the end of the horizon.",
                        "X may be found in the seams.",
                        "The shadows make more noticeable the light."
                    };

                    string subjectName = Subjects[0].Name;

                    string PickRandomAdvice()
                    {
                        int index = Game1.r.Next(adviceList.Count());
                        string advice = adviceList[index].Replace("X", subjectName);
                        adviceList.RemoveAt(index);
                        return advice;
                    }

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "question", MessageID,
                        "I'm not sure about " + Subjects[0].ReferredToNames[0] + ".", //content
                        PickRandomAdvice(), //truthful/compliant response
                        PickRandomAdvice(), //made up/denial response
                        PickRandomAdvice(), //unknowing/confused response
                        PickRandomAdvice(), //derailing response
                        PickRandomAdvice()  //flattering response
                    );
                }

                else if (MessageID == "inform_quest")
                {
                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "request", MessageID,
                        "I am on a quest to slay the great " + (GameWorld.Calamity[0].Name).Split(' ')[0] + ".", //content
                        "I wish you the best of luck.", //truthful/compliant response
                        "Hah, we will see.", //made up/denial response
                        "Who is that?", //unknowing/confused response
                        GameWorld.Calamity[0].Pronoun + " is already dead.", //derailing response
                        "You look like a strong adventurer. You've got this!" //flattering response

                    );
                }
                else if (MessageID == "tell_story_about")
                {
                    string subjectName = Subjects[0].Name != null ? Subjects[0].Name : Subjects[0].ReferredToNames[0];
                    string introduction = "";

                    if (Subjects[0] is Architect architect)
                    {
                        if (architect.HomeLocation != null && architect.Profession != null)
                        {
                            introduction = $"{architect.Name} was a {architect.Profession} from {architect.HomeLocation.Name}.";
                        }
                        else if (architect.Profession != null)
                        {
                            introduction = $"{architect.Name} was a {architect.Profession}.";
                        }
                        else if (architect.HomeLocation != null)
                        {
                            introduction = $"{architect.Name} was from {architect.HomeLocation.Name}.";
                        }
                        else
                        {
                            introduction = $"{architect.Name} was a {architect.Race.Name}.";
                        }
                    }
                    else if (Subjects[0] is Object gameObject)
                    {
                        string materials = Game1.FormatMaterialList(gameObject.Materials);
                        introduction = $"{gameObject.ReferredToNames[0]} was a {materials} {gameObject.Type}.";
                    }
                    else
                    {
                        introduction = $"{Subjects[0].Name} was a {Subjects[0].GetType().Name}.";
                    }

                    var events = GameWorld.HistoricalEvents
                        .Where(e => e.EventData.Contains(subjectName))
                        .Select(e =>
                        {
                            int endIndex = e.EventData.IndexOf(") ") + 2;
                            string processedEvent = e.EventData.Substring(endIndex);
                            int yearStart = e.EventData.IndexOf("/") + 1;
                            int yearEnd = e.EventData.IndexOf(")", yearStart);
                            string year = e.EventData.Substring(yearStart, yearEnd - yearStart);
                            return $"In {year}, {processedEvent}";
                        })
                        ;

                    string content;
                    if (events.Count() > 0)
                    {
                        content = introduction + " " + string.Join(" ", events);
                    }
                    else
                    {
                        content = introduction + " There isn't much else to say.";
                    }

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "question", MessageID,
                        content, //content
                        "That was a nice story.", //truthful/compliant response
                        "I didn't really enjoy that.", //made up/denial response
                        "I'm not sure I understand.", //unknowing/confused response
                        "Enough about that, lets talk about me.", //derailing response
                        "Wow! Now tell me your story." //flattering response
                    );
                }


                else if (MessageID == "compliment")
                {
                    List<string> compliments = new List<string>
                    {
                        "Your skillset is rather impressive.",
                        "You have quite the dedication.",
                        "You are rather brilliant.",
                        "Your kindness is inspiring to us all.",
                        "Your enthusiasm is contagious.",
                        "Your creativity is a beautiful thing."
                    };

                    if(Receiver.Clothing.Count() > 0)
                    {
                        compliments.Add("Your " + Receiver.Clothing[Game1.r.Next(Receiver.Clothing.Count())].Type + " looks very nice.");
                    }

                    Random rnd = new Random();
                    string randomCompliment = compliments[rnd.Next(compliments.Count())];

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "request", MessageID,
                        randomCompliment, //content
                        "I appreciate that.", //truthful/compliant response
                        "Spare me the flattery.", //made up/denial response
                        "I'm not sure how to respond to that.", //unknowing/confused response
                        "Flattery is like the wind.", //derailing response
                        "Your words mean everything to me." //flattering response
                    );
                }
                else if (MessageID == "insult")
                {
                    List<string> insults = new List<string>
                    {
                        "Your incompetence is astounding",
                        "You are a disgrace",
                        "You have zero redeeming qualities",
                        "Your skills are laughable",
                        "You are an embarrassment",
                        "Your existence is a burden",
                        "You are utterly useless",
                        "You lack any talent",
                        "You are a complete failure"
                    };

                    Random rnd = new Random();
                    string randomInsult = insults[rnd.Next(insults.Count())];

                    DecidedMessage = new Message
                    (
                        Sender, Receiver, Subjects, "request", MessageID,
                        randomInsult + ".", //content
                        "How could you say such a thing?", //truthful/compliant response
                        "Your words mean nothing to me.", //made up/denial response
                        "Why would you say that?", //unknowing/confused response
                        "No, " + randomInsult + "!", //derailing response
                        "I wish you were as right as you were beautiful." //flattering response
                    );
                }

                else if (MessageID == "surrender")
                {
                    DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "request", MessageID,
                            "Wait! I surrender!", //content
                            "Stay put and do what I say.", //truthful/compliant response
                            "I do not accept.", //made up/denial response
                            "What? Why?", //unknowing/confused response
                            "I surrender too!", //derailing response
                            "Surrender to my looks? Accepted." //flattering response

                        );
                }
                else if (MessageID == "demand_surrender")
                {
                    DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "request", MessageID,
                            "Surrender. Now.", //content
                            "I yield!", //truthful/compliant response
                            "Over my dead body!", //made up/denial response
                            "Surround what?", //unknowing/confused response
                            "You first!", //derailing response
                            "Okay! But only to you." //flattering response

                        );
                }
                else if (MessageID == "demand_item")
                {
                    DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "request", MessageID,
                            "Drop your " + Subjects[0].ReferredToNames[0] + " and I may consider letting you live.", //content
                            "Okay! I'll do it!", //truthful/compliant response
                            "Over my dead body!", //made up/denial response
                            "What? I don't have that!", //unknowing/confused response
                            "You first!", //derailing response
                            "I'll drop it, but marry me." //flattering response

                        );
                }
                else
                {
                    throw new Exception("You should not be here.");
                }


                if (DecidedMessage != null)
                {
                    Receiver.MessagesNotRespondedTo.Add(DecidedMessage);

                    Color c;

                    if(Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Sender) || Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Receiver))
                    {
                        c = new Color(0, 255, 255);
                    }
                    else
                    {
                        c = new Color(0, 75, 75);
                    }

                    Sender.AnnounceToParty(Sender.ReferredToNames[0] + ": " + DecidedMessage.MessageContent, c, new EntityList<Entity> { Sender }.Union(DecidedMessage.Subjects));
                    Sender.CooldownCycles += (int)Math.Round(30 / Sender.Speed());
                }
            }
        }
    }
}
