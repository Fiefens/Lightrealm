using Humanizer;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Reflection;
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

static Dictionary<string, string[]> ChapterReadText = new Dictionary<string, string[]>
{
    { "Chapter 1: Basic Combat", new[]
        {
            " ",
            "[ Basic Combat ]",
            "To defeat an architect, you must deplete their bodily energy supply without them depleting yours. To do this, you must land attacks, cast spells, and/or use other abilities to bring their energy down as fast as possible. This is most commonly done with the \"approach\" and \"directed attack\" actions.",
            "Creatures in Lightrealm have the ability to quickly heal themselves, so focus on ending a combat quickly rather than keeping your reserves up. Ending a fight near death is no less of a victory than ending one in peak condition."
        }
    },
    { "Chapter 2: Stats", new[]
        {
            " ",
            "[ Stats ]",
            "Certain stats improve your characters' combat ability. ",
            "Strength generally improves melee power and speed.",
            "Agility increases action speed, your chance to dodge attacks, and your chance to escape when moving away from a combat encounter.",
            "Dexterity improves exposure, throwing power, and ability to dodge projectiles.",
            "Endurance improves your maximum energy and decreases the burden carried items have on your action speed.",
            "Creativity increases the maximum imbuements you can sustain from magical items.",
            "Focus improves your magic effectiveness and decreases pain felt and accrued."
        }
    },
    { "Chapter 3: Weapons", new[]
        {
            " ",
            "[ Weapons ]",
            "Weapons have a damage type that dictates their effects. Bashing weapons apply significant energy loss, functional damage, and considerable pain, but inflict minimal bleeding. Piercing weapons excel at damaging functions of body parts and destroying energy, but cause minor pain and minimal bleeding. Slashing weapons offer a balanced array of bleeding, energy loss, and pain, with slightly less functional damage than other types. Thrashing weapons cause great pain and bleeding, but minimal functional damage or direct energy loss."
        }
    },

    { "Chapter 4: Imbuements", new[]
        {
            " ",
            "[ Imbuements ]",
            "Certain objects you find in a world may possess one or more magical imbuements. If you find one, it will provide either a conditional passive bonus, or a triggerable effect. Your character can sustain imbuements equal to 3 + Creativity, and you can use the game menu and the manage imbuements button to change which imbuements you wish to channel at any time."
        }
    },
    { "Chapter 5: Strike Targeting", new[]
        {
            " ",
            "[ Strike Targeting ]",
            "Depending on your target, you might want to strike a different body part. You might want to strike unarmored parts to maximize general bleeding, pain, and energy loss. Targeting core parts like head, neck, and torso can be harder but cause uncontrollable bleeding later in combat. Targeting hands or arms can cause the item in said hand or arm to be dropped. Targeting legs, wings, or other movement parts can greatly reduce action speed. If a part has recently been attacked, it is likely less exposed, and if a part has been used in an attack recently, it might be easier to strike."
        }
    },
    { "Chapter 6: Reactions", new[]
        {
            " ",
            "[ Reactions ]",
            "When you are being attacked by a general blow, you have an opportunity to react. Reactability is generally easier when you are skilled at the reaction, and is generally harder when you are destabilized, have atypical apparel, or are exposed. Most reactions will cancel a hit entirely in exchange for exposing you, but some reactions have special other effects.",
            "Sustaining a hit is default, you take the hit normally. Parrying an attack requires a weapon or shield, and is more effective with specific weapons. Parrying exposes the utilized arm and hand. Blocking an attack is very effective, but requires a shield and exposes all unblocked body parts. Ducking is more effective against high body attacks and exposes upper body parts. Jumping is more effective against low body attacks and exposes lower body parts. Rolling away from an attack is less effective in general, but reduces your exposure all around. Disarming an attacker attempts to remove the attacker's weapon. A success will not protect you, but will remove the attacker's weapon from their hand. Redirecting an attack is slightly less likely to succeed, but a success will expose the limb used in the attack."
        }
    },
    { "Chapter 7: Armor", new[]
        {
            " ",
            "[ Armor ]",
            "Armor grants certain body parts coverage. For instance, a hood grants half head coverage and half neck coverage, which multiplied with material strength can sometimes reduce significant damage.",
            "For example, a copper chestplate that grants high coverage, but with low material toughness, would stifle a torso or shoulder attack about 20% of the time, reducing it or uncommonly preventing it."
        }
    },
    { "Chapter 8: Exposure", new[]
        {
            " ",
            "[ Exposure ]",
            "When you make an attack with a limb or use a limb to make a protective maneuver, that limb gets exposed. Use the \"reposition\" action to decrease exposure of all limbs and the \"retract\" action to heavily decrease the exposure of a commonly-used limb. Exposure decreases your ability to react to attacks against a limb, sometimes making protecting the limb impossible.",
            "While not directly visible, your opponent gets exposed just as you do, so watch for combat techniques you can exploit."
        }
    },
    { "Chapter 9: Throwing and Projectiles", new[]
        {
            " ",
            "[ Throwing and Projectiles ]",
            "Objects that are airborne have a specific target and a time-to-hit. Projectiles are created by throwing, imbuements, or other abilities. A projectile might miss if the target is particularly dextrous. Leaving the vicinity of the projectile will also cause it to miss."
        }
    },
    { "Chapter 10: Conditions", new[]
        {
            " ",
            "[ Conditions ]",
            "A variety of effects can cause temporary conditions to fall upon you or your opponent.",
            "If you are in combat, or use a combative maneuver, you become Engaged (E). It will be more difficult to escape until you are able to neutralize the threat.",
            "If you are ignited, you will become On Fire (F). Every second, you will take damage depending on how much fire you have accrued, limited at five, then your fire will reduce.",
            "If you are wet (W), you will be immunized to fire.",
            "If you are blind, (B), you will be unable to see what is happening around you, but will still be able to act.",
            "If you are destabilized (D), your character's reaction success chances will be cut in half.",
            "If you are knocked out or experience too much pain, your character will go Unconscious (U), preventing action until you return to consciousness.",
            "If you are radiant (R), you will be slightly slowed.",
            "If a construct initiates a Cloak ability (C), they will gain extra stealth, helping them escape and/or avoid attacks.",
            "When something is Fractal (T) it disappears into the fractal dimension until it is no longer fractal.",
            "If someone is covered in plants (P), their attacks are easier to react against.",
            "When something is Held (H), it cannot physically move.",
            "If something is Dismissed (S), it can escape combat much easier, and gets a slight bonus to dodging attacks."
        }
    },
    { "Chapter 11: Spells", new[]
        {
            " ",
            "[ Spells ]",
            "If you know a spell and are close enough, you can spend your energy to cast it and apply various negative effects. Most spells are not efficient at doing direct damage. You would typically use a spell to gain an edge for physical weapons rather than relying fully on spells for the kill. Focus improves most spell effects and reduces the energy needed to cast them."
        }
    },
    { "Chapter 12: Skills", new[]
        {
            " ",
            "[ Skills ]",
            "Skills are technical combat maneuvers that provide an instant advantage or augment the following move. Skills can be used once per location, you must leave to refresh your skill list. You can learn up to 3 skills. Skills are instantaneous; they have no cooldown after use."
        }
    },
    { "Chapter 13: Health and Healing Items", new[]
        {
            " ",
            "[ Health and Healing Items ]",
            "Several items can be used in or out of combat to mitigate critical conditions, for a price.",
            "Energy is required to live. If you lose all your energy, your character's nuclear fusion will cease and they will subsequently die. You can restore energy slowly out of combat, or with vitalium vials.",
            "Pain will cause you to \"falter\", accruing for you extra cooldown cycles, which force you to wait longer until your turn. If your pain meter (near your energy orb) fills up, you will go unconscious until it can naturally stabilize (or until you get beaten to death). You can remedy pain with salves, or pain will slowly disappear over time.",
            "Bleeding will cause energy loss every second equal to bleed stacks accrued, then decrease by one. You can remedy bleeding with bandages, or wait for the stacks to run out.",
            "Body parts have an Integrity that determines how functional they are. For example, losing leg or foot integrity will force your character to the ground and/or lose speed, losing arm integrity can cause you to drop items, and losing head/torso integrity can cause heavy bleeding. Body part integrity cannot be healed mid-combat, and it will slowly heal out of combat.",
            "Using any healing items in combat will destabilize you, so you may consider if you truly need one.",
            "You can also only use 3 healing items before you develop healing sickness, you must move to a new block or room before using more."
        }
    },
    { "Chapter 14: Nerd Stuff", new[]
        {
            " ",
            "[ Nerd Stuff ]",
            "Grenades are thrown items that create a massive area of effect upon contact with something. They mystically travel rather slowly through the air. You might not want to be in the room when one explodes...",
            "You can shove an enemy to push them 3-4 units away, dependent on strength.",
            "You can use a fiber bunch to rig a trap. If an architect who did not see the trap get set enters the area, the trap will spring and they might be bound or destabilized."
        }
    },
    { "Chapter 15: Path Magic", new[]
        {
            " ",
            "[ Path Magic ]",
            "If you defeat an architect of a higher level than you, you will absorb their soul. You can channel the soul into a particular element, which will boost stats and unlock special abilities."
        }
    }
};


        public static void MakeObservation(string data, Color color, EntityList<Entity> entities)
        {
            string capitalizedData = Game1.Capitalize(data);
            Game1.Announcements.Add(new TextStorage(capitalizedData, color, entities));
        }

        public static bool CanUnderstandEachOther(Entity sender, Entity receiver)
        {
            // Ensure both sender and receiver are Architects
            if (!(receiver is Architect recArch) || !(sender is Architect senderArch))
            {
                return false; // They cannot understand each other
            }

            // Path of Life Level >= 4 overrides everything
            if (senderArch.PathOfLifeLevel >= 4 || recArch.PathOfLifeLevel >= 4)
            {
                return true;
            }

            bool senderIsHumanoid = Game1.GameWorld.HumanoidRaces.Contains(senderArch.Race);
            bool receiverIsHumanoid = Game1.GameWorld.HumanoidRaces.Contains(recArch.Race);

            if (senderIsHumanoid && receiverIsHumanoid)
            {
                return true; // All humanoids understand each other
            }

            bool senderIsExtra = Game1.GameWorld.ExtraRaces.Contains(senderArch.Race);
            bool receiverIsExtra = Game1.GameWorld.ExtraRaces.Contains(recArch.Race);

            if (senderIsExtra && receiverIsExtra && senderArch.Race == recArch.Race)
            {
                return true; // ExtraRaces only understand others of the same race
            }

            if (((senderIsExtra && receiverIsHumanoid) || (receiverIsExtra && senderIsHumanoid)) && (senderArch.PathOfLifeLevel >= 2 || recArch.PathOfLifeLevel >= 2))
            {
                return true; // ExtraRaces can understatns dpathoflifers 2.
            }

            return false;
        }



        public static bool RunCommand(Architect Executor, string CommandID, List<Entity> Subjects, List<Architect> LoadedArchitects, World GameWorld, Random r, string OriginalCommand)
        {
            //replace inside command pronouns
            int Month = ((int)Math.Round((decimal)(GameWorld.Cycle / 24192000)) % 12) + 1;
            int Year = (int)Math.Round((decimal)(GameWorld.Cycle / 290304000), MidpointRounding.ToZero);


            //importance
            if (Game1.GameWorld.GamePlayerAssociation?.ActiveParty?.Architects != null &&
        Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Executor))
            {
                Executor.ImportantThisLoad = true;

                foreach (var subject in Subjects)
                {
                    if (subject is Architect arch)
                    {
                        arch.ImportantThisLoad = true;
                    }
                }
            }


            Game1.LastRanCommand = OriginalCommand;
            Game1.LastRanCommandBlockX = Executor.Block.X;
            Game1.LastRanCommandBlockZ = Executor.Block.Z;
            Game1.ReferredToNamesOfLastCommandEntities = Subjects
    .Select((s, index) => $"Subject {index + 1}: {s.ReferredToNames[0]}")
    .ToList();

            Game1.StructureIsNullIfNull = Executor.Structure;
            Game1.RoomIndexIsNegativeOneIfNull = Executor.Room == null ? -1 : Executor.Room.Structure.Rooms.IndexOf(Executor.Room);

            string Date = "(" + Month + "/" + Year + ")";

            bool CanMessageSubject = Subjects.Count() > 0 && (Subjects[0] is Architect) && (GameWorld.HumanoidRaces.Contains(((Architect)Subjects[0]).Race) || (GameWorld.ExtraRaces.Contains(((Architect)Subjects[0]).Race) && Executor.PathOfLifeLevel >= 2) || Executor.PathOfLifeLevel >= 4);

            if (Subjects == null)
            {
                Subjects = new List<Entity>();
            }

            EntityList<Architect> ArchitectsToUse; 
            EntityList<Object> ObjectsToUse;


            if (Executor.Room != null)
            {
                ArchitectsToUse = Executor.Room.Architects; // Use architects from the room if it's not null
                ObjectsToUse = Executor.Room.Objects;
            }
            else
            {
                ArchitectsToUse = Executor.Block.Architects; // Otherwise, use architects from the block
                ObjectsToUse = Executor.Block.Objects;
            }


            if (CommandID != null && Game1.RecognizedMessages.ContainsKey(CommandID) && Subjects[0] is Architect Reciever)
            {
                var trimmedSubjects = new List<Entity>(Subjects.Skip(1));

                if (ArchitectsToUse.Contains(Reciever) || Executor.Invocations.Contains("telepathy"))
                    SendMessage(CommandID, Executor, Reciever, new EntityList<Entity>(trimmedSubjects), GameWorld);
                else
                    MakeObservation("That person is not nearby.", Color.Yellow, new EntityList<Entity>());
            }

            else if (CommandID == "leave_structure")
            {
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed));
                Executor.HideValue = 0;

                Object exitDoor = null;
                if (Executor.Structure != null)
                    exitDoor = Executor.Room.Objects.FirstOrDefault(o => o.Type == "exit door");

                if (Executor.Structure == null)
                {
                    Executor.AnnounceToParty(Executor.ReferredToNames[0] + " is not in a structure.", Color.Yellow, new EntityList<Entity>() { Executor });
                }
                else if (exitDoor == null)
                {
                    Executor.CooldownCycles += (int)Math.Round(25 / Executor.Speed);

                    if (!Executor.Room.TriedBreak)
                    {
                        Executor.AnnounceToParty($"There is not a way for {Executor.ReferredToNames[0]} to exit through. Use this command again to try to destroy the walls, or path to the exit normally.", Color.Yellow, new EntityList<Entity>() { Executor });
                        Executor.Room.TriedBreak = true;
                    }
                    else
                    {
                        Executor.Bash();

                    }
                }
                else
                {
                    if (exitDoor.Reinforced)
                    {
                        Executor.AnnounceToParty($"The door won't budge. {Executor.ReferredToNames[0]} might need to bash it down.", Color.Yellow, new EntityList<Entity>() { Executor });
                    }
                    else
                    {
                        // Reuse enter logic for exiting through the door
                        Executor.Enter(exitDoor, false);

                        if (Executor.Room == null)
                        {
                            Game1.SwitchState("otherturn", false);
                            Game1.ProgressTutorial(21);
                        }
                    }
                }
            }

            else if (CommandID == "enter")
            {
                Executor.Enter(Subjects[0], false);

            }
            else if (CommandID == "go_prone" && (Subjects.Count() == 0 || Subjects[0].Metadata == "down"))
            {
                MakeObservation("You get on the ground.", Color.Orange, new EntityList<Entity>());
                Executor.OnGround = true;
                Game1.SFX.Add(Game1.Cloth);
            }
            else if (CommandID == "stand_up" && (Subjects.Count() == 0 || Subjects[0].Metadata == "up"))
            {
                MakeObservation("You stand up.", Color.Green, new EntityList<Entity>());
                Executor.CooldownCycles += (int)Math.Round((20 - Executor.Agility) * Executor.Speed);
                Executor.OnGround = false;
                Game1.SFX.Add(Game1.Cloth);
            }
            else if (CommandID == "move_direction")
            {
                if (!Game1.TriedFakeMove)
                {
                    MakeObservation("Some commands have shortcuts. For instance, directional movement can be initiated with the NUMPAD, the Click GUI by the district map, or by pressing Ctrl + QWEADZXC.", Color.Lime, new EntityList<Entity>());
                    Game1.TriedFakeMove = true;
                }

                if (Executor.YLevelInFeet > 0)
                {
                    MakeObservation("You need to be on the ground to move between blocks.", Color.Yellow, new EntityList<Entity>());
                }
                else if (Executor.Structure != null)
                {
                    MakeObservation("You must be outside to move. To move between doors, use the \"enter\" command.", Color.Yellow, new EntityList<Entity>());
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
                    Weapon = LoadedArchitects[Game1.ArchitectIndex].BodyParts[Game1.GameWorld.rnd.Next(LoadedArchitects[Game1.ArchitectIndex].BodyParts.Count())];
                }

                if (Subjects[0] is Architect a && ((Executor.Room != null && Executor.Room.Architects.Contains(a)) || (Executor.Block.Architects.Contains(a) && Executor.Room == null)))
                {
                    Object targetBodyPart = a.FindBodyPart(Subjects[1].Metadata);

                    if (targetBodyPart != null && ((Weapon.WeaponMaximumRange + (Executor.Invocations.Contains("slashing") ? 2 : 0)) >= Executor.GetDistance(a)) && Math.Abs(a.YLevelInFeet - Executor.YLevelInFeet) <= 5 && Game1.TutorialActive == false)
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
                        Game1.Announcements.Add(new TextStorage("You wave your hands around, but you aren't close enough. Use the \"approach target\" command to get closer.", Color.Yellow, new EntityList<Entity>() { }));
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
                    Weapon = LoadedArchitects[Game1.ArchitectIndex].BodyParts[Game1.GameWorld.rnd.Next(LoadedArchitects[Game1.ArchitectIndex].BodyParts.Count())];
                }

                Object Target;

                if (Subjects[0] is Architect a && (Executor.Room != null && Executor.Room.Architects.Contains(a) || (Executor.Block.Architects.Contains(a) && Executor.Room == null)))
                {
                    Target = ((Architect)(Subjects[0])).BodyParts[Game1.GameWorld.rnd.Next(((Architect)(Subjects[0])).BodyParts.Count())];
                }
                else if (Subjects[0] is Object o && (((Executor.Room != null && Executor.Room.Objects.Contains(o)) || (Executor.Block.Objects.Contains(o) && Executor.Room == null))))
                {
                    Target = (Object)(Subjects[0]);
                }
                else if (Subjects[0] is Structure s)
                {
                    if (Executor.Room == null && Executor.Block == s.Block)
                    {
                        if (s.Reinforced)
                        {
                            MakeObservation("You attack the structure, weakening its defenses.", Color.Yellow, new EntityList<Entity>());
                            s.DoorIntegrity -= 20;
                            Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed));

                            if (s.DoorIntegrity < 0)
                            {
                                // Reset structure integrity and break reinforcement
                                s.DoorIntegrity = 50;
                                s.Reinforced = false;
                                MakeObservation(s.ReferredToNames[0] + " is no longer reinforced!", Color.Orange, new EntityList<Entity>());

                                // Find the exit door in the structure's primary room
                                var exitDoor = s.Rooms[0].Objects.FirstOrDefault(obj => obj.Type == "exit door");

                                if (exitDoor != null)
                                {
                                    exitDoor.Reinforced = false;
                                    exitDoor.Integrity = 50;
                                }
                            }
                        }
                        else
                        {
                            MakeObservation("You attack the structure, but it seems to be fruitless.", Color.Yellow, new EntityList<Entity>());
                        }

                        return true;
                    }
                    else
                    {
                        MakeObservation("That structure is too far away, or you are not outside it.", Color.Yellow, new EntityList<Entity>());
                        return false;
                    }
                }
                else
                {
                    MakeObservation("You can't attack that.", Color.Yellow, new EntityList<Entity>());
                    return false;
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

                // Check if the target is reinforced
                if (Target.Reinforced)
                {
                    Target.Integrity -= 20; // Drop integrity by 20
                    Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed));


                    if (Target.Integrity < 0)
                    {
                        // Reset integrity and unreinforce
                        Target.Integrity = 50;
                        Target.Reinforced = false;

                        MakeObservation(Target.ReferredToNames[0] + " is no longer reinforced!", Color.Orange, new EntityList<Entity>());

                        // If the target is an exit door, also unreinforce its structure
                        if (Target is Object exitDoor && exitDoor.Type == "exit door" && exitDoor.Structure != null)
                        {
                            exitDoor.Structure.Reinforced = false;
                            exitDoor.Structure.DoorIntegrity = 50;
                        }
                        // If the target is a door, find and unreinforce its paired door
                        else if (Target is Door door && door.DestinationRoom != null)
                        {
                            string opposingDirection = GetOpposingDirection(door.Direction);
                            var pairedDoor = door.DestinationRoom.Objects
                                .OfType<Door>()
                                .FirstOrDefault(d => d.Direction == opposingDirection && d.DestinationRoom == door.SourceRoom);

                            if (pairedDoor != null)
                            {
                                pairedDoor.Reinforced = false;
                                pairedDoor.Integrity = 50;
                            }
                        }
                    }
                    return true;
                }


                if (((Target.Owner == null) || ((Weapon.WeaponMaximumRange + (Executor.Invocations.Contains("slashing") ? 2 : 0)) >= Executor.GetDistance((Architect)(Target.Owner)) && Math.Abs(((Architect)(Target.Owner)).YLevelInFeet - Executor.YLevelInFeet) <= 5)) && Game1.TutorialActive == false)
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
                    Game1.Announcements.Add(new TextStorage("You wave your hands around, but you aren't close enough. Use the \"approach target\" command to get closer.", Color.Yellow, new EntityList<Entity>() { }));
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
                    Target = a.BodyParts[Game1.GameWorld.rnd.Next(a.BodyParts.Count())];
                }
                else if (Subjects[0] is Object o && (Executor.Room != null && Executor.Room.Objects.Contains(o) || (Executor.Block.Objects.Contains(o) && Executor.Room == null)))
                {
                    Target = (Object)(Subjects[0]);
                }
                else if (Subjects[0] is Structure s)
                {
                    if (Executor.Room == null && Executor.Block == s.Block)
                    {
                        if (s.Reinforced)
                        {
                            MakeObservation("You attack the structure, weakening its defenses.", Color.Yellow, new EntityList<Entity>());
                            s.DoorIntegrity -= 20;
                            Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed));


                            if (s.DoorIntegrity < 0)
                            {
                                // Reset structure integrity and break reinforcement
                                s.DoorIntegrity = 50;
                                s.Reinforced = false;
                                MakeObservation(s.ReferredToNames[0] + " is no longer reinforced!", Color.Orange, new EntityList<Entity>());

                                // Find the exit door in the structure's primary room
                                var exitDoor = s.Rooms[0].Objects.FirstOrDefault(obj => obj.Type == "exit door");

                                if (exitDoor != null)
                                {
                                    exitDoor.Reinforced = false;
                                    exitDoor.Integrity = 50;
                                }
                            }
                        }
                        else
                        {
                            MakeObservation("You attack the structure, but it seems to be fruitless.", Color.Yellow, new EntityList<Entity>());
                        }

                        return true;
                    }
                    else
                    {
                        MakeObservation("That structure is too far away, or you are not outside it.", Color.Yellow, new EntityList<Entity>());
                        return false;
                    }
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

                if (
                    Weapon != null &&
                    (
                        !Target.IsBodyPart ||
                        (
                            Target.Creator == null ||
                            (Weapon.WeaponMaximumRange + (Executor.Invocations.Contains("slashing") ? 2 : 0)) >= Executor.GetDistance((Architect)Target.Creator)
                        )
                    ) &&
                    Math.Abs(((Architect)(Target.Owner)).YLevelInFeet - Executor.YLevelInFeet) <= 5 &&
                    Game1.TutorialActive == false
                )
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
                    Game1.Announcements.Add(new TextStorage("You wave your hands around, but you aren't close enough. Use the \"approach target\" command to get closer.", Color.Yellow, new EntityList<Entity>() { }));
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
                            else if (((item.WeaponMaximumRange + (Executor.Invocations.Contains("slashing") ? 2 : 0)) < Executor.GetDistance(a) || Math.Abs(a.YLevelInFeet - Executor.YLevelInFeet) > 5) && Game1.TutorialActive == false)
                            {
                                // Target too far away (distance or height)
                                Game1.Announcements.Add(new TextStorage("You wave your hands around, but you aren't close enough. Use the \"approach target\" command to get closer.", Color.Yellow, new EntityList<Entity>() { }));
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
                Game1.SFX.Add(Game1.MenuSelect);
            }


            else if (CommandID == "become_shadow")
            {
                if (Executor.Invisible)
                {
                    MakeObservation("You are already in the shadows.", Color.Yellow, new EntityList<Entity>());
                }
                else if (Executor.PathOfShadowLevel >= 4)
                {
                    MakeObservation("You enter the darkness.", Color.Gray, new EntityList<Entity>());

                    Executor.TryComment("useshadow", 50);

                    Executor.Invisible = true;
                    Game1.SFX.Add(Game1.Invisibility);
                    Executor.ExtraStealth += 1000;
                }
                else
                {
                    MakeObservation("You are not experienced enough in the shadows to partake in such a maneuver.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "exit_invisibility")
            {
                Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed));
                if (Executor.Invisible)
                {
                    MakeObservation("You exit the shadows.", Color.Gray, new EntityList<Entity>());
                    Game1.SFX.Add(Game1.Invisibility);
                    Executor.TryComment("useshadow", 50);
                    Executor.Invisible = false;
                }
                else
                {
                    MakeObservation("You are not in the shadows.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "level_up")
            {
                if (Subjects[0].Metadata == "up")
                {
                    Executor.Level++;
                    Executor.SpendableLevels++;
                    MakeObservation("You divine a soul from above.", Color.Yellow, new EntityList<Entity>());
                }
                else if (Subjects[0].Metadata == "down")
                {
                    Executor.Level = 1;
                    Executor.SpendableLevels = 0;

                    Executor.PathOfShadowLevel = 0;
                    Executor.PathOfLifeLevel = 0;
                    Executor.PathOfRealityLevel = 0;
                    Executor.PathOfLightLevel = 0;
                    Executor.PathOfDeathLevel = 0;
                    Executor.PathOfStarsLevel = 0;
                    Executor.PathOfHeatLevel = 0;
                    Executor.PathOfBodyLevel = 0;
                    MakeObservation("You lose all souls.", Color.Yellow, new EntityList<Entity>());
                }
                else
                {
                    MakeObservation("What in the name of doge are you trying to do...", Color.Yellow, new EntityList<Entity>());
                }
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
                                Executor.ModifyDistance(architect, 2); // Increase Up all others by 2
                            }
                        }
                        MakeObservation("You focus your target, shifting distances.", Color.Green, new EntityList<Entity>());
                        Executor.CooldownCycles += (int)Math.Round((10 / Executor.Speed));

                        Executor.StepSound(2);
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
                        Executor.CooldownCycles += (int)Math.Round((15 / Executor.Speed));

                        Executor.StepSound(2);
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
                        Executor.CooldownCycles += (int)Math.Round((15 / Executor.Speed));

                        Executor.StepSound(2);
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
                        Executor.CooldownCycles += (int)Math.Round((15 / Executor.Speed));
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
                Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed));

                if (Executor.CarryingEntity != null)
                {
                    MakeObservation("You need to drop your carried item before wielding anything.", Color.Yellow, new EntityList<Entity>());
                }
                else
                {
                    if (Subjects[0] is Object item && Executor.Inventory.Contains(item))
                    {
                        bool mainArmFunctional = Executor.MainInteractionAppendage.Integrity >= 60;
                        bool offArmFunctional = Executor.OffInteractionAppendage.Integrity >= 50;

                        if (Executor.Race.MainInteractionAppendage.EndsWith("hand"))
                        {
                            string mainArmName = Executor.Race.MainInteractionAppendage.Replace("hand", "arm").ToLower();
                            Object mainArm = Executor.BodyParts.FirstOrDefault(x => x.Type.ToLower() == mainArmName);
                            if (mainArm != null && mainArm.Integrity < 60)
                            {
                                mainArmFunctional = false;
                            }
                        }

                        if (Executor.Race.OffInteractionAppendage.EndsWith("hand"))
                        {
                            string offArmName = Executor.Race.OffInteractionAppendage.Replace("hand", "arm").ToLower();
                            Object offArm = Executor.BodyParts.FirstOrDefault(x => x.Type.ToLower() == offArmName);
                            if (offArm != null && offArm.Integrity < 50)
                            {
                                offArmFunctional = false;
                            }
                        }

                        bool determineFailureMessage = false;

                        // Check if the item is two-handed and both hands are empty
                        if (item.IsTwoHanded)
                        {
                            if (mainArmFunctional && offArmFunctional && Executor.MainHeldObject == null && Executor.OffHeldObject == null)
                            {
                                Executor.MainHeldObject = item;
                                Executor.Inventory.Remove(item);
                                MakeObservation("You wield the " + item.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { item });
                                Game1.SFX.Add(Game1.Wield);
                                determineFailureMessage = true;
                            }
                            else
                            {
                                MakeObservation("You need both empty hands to wield a two-handed weapon.", Color.Yellow, new EntityList<Entity>());
                                determineFailureMessage = true;
                                
                            }
                        }
                        else // Single-handed item logic remains the same
                        {
                            // Prevent wielding a single-handed item in the off hand if the main hand holds a two-handed weapon
                            if (Executor.MainHeldObject != null && Executor.MainHeldObject.IsTwoHanded && Executor.OffHeldObject == null)
                            {
                                MakeObservation("You cannot wield another item in your empty hand while holding a two-handed weapon.", Color.Yellow, new EntityList<Entity>());
                            }
                            else
                            {
                                if (mainArmFunctional && Executor.MainHeldObject == null)
                                {
                                    Executor.MainHeldObject = item;
                                    Executor.Inventory.Remove(item);
                                    MakeObservation("You wield the " + item.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { item });
                                    Game1.SFX.Add(Game1.Wield);
                                    determineFailureMessage = true;
                                }
                                else if (offArmFunctional && Executor.OffHeldObject == null)
                                {
                                    Executor.OffHeldObject = item;
                                    Executor.Inventory.Remove(item);
                                    MakeObservation("You wield the " + item.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { item });
                                    Game1.SFX.Add(Game1.Wield);
                                    determineFailureMessage = true;
                                }
                            }
                        }

                        if (!determineFailureMessage)
                        {
                            if (Executor.MainHeldObject != null && Executor.OffHeldObject != null)
                            {
                                MakeObservation("Your hands are full.", Color.Yellow, new EntityList<Entity>());
                            }
                            else
                            {
                                MakeObservation("You have no functional arm/hand pairs that can hold the item. Wait for them to heal (click energy orb) or leave the location.", Color.Yellow, new EntityList<Entity>());
                            }
                        }
                    }
                    else
                    {
                        MakeObservation("That is not an object in your inventory.", Color.Yellow, new EntityList<Entity>());
                    }
                }
            }



            else if (CommandID == "holster_item")
            {
                Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed));

                if (Subjects[0] is Object itemToHolster)
                {
                    // Check if the item is in one of the Executor's hands
                    if (Executor.MainHeldObject == itemToHolster)
                    {
                        Executor.Inventory.Add(itemToHolster);
                        Executor.MainHeldObject = null;
                        MakeObservation("You holster the " + itemToHolster.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { itemToHolster });
                        Game1.SFX.Add(Game1.Holster);
                    }
                    else if (Executor.OffHeldObject == itemToHolster)
                    {
                        Executor.Inventory.Add(itemToHolster);
                        Executor.OffHeldObject = null;
                        MakeObservation("You holster the " + itemToHolster.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { itemToHolster });
                        Game1.SFX.Add(Game1.Holster);
                    }
                    else
                    {
                        MakeObservation("You are not holding that item.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("That is not a valid item to holster.", Color.Yellow, new EntityList<Entity>());
                }
            }





            else if (CommandID == "ditch_inventory")
            {
                Executor.CooldownCycles += (int)(Math.Round(50 / Executor.Speed));

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
                        Executor.Structure.MarketDebtToUs += o.Value();
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
                        Executor.Structure.MarketDebtToUs += o.Value();
                    }
                }

                MakeObservation("You drop your inventory.", Color.Orange, new EntityList<Entity>());
            }


            else if (CommandID == "take_item_from")
            {
                Executor.CooldownCycles += (int)Math.Round(5 / Executor.Speed);

                if (Subjects.Count > 1 && (Subjects[1].Metadata == "shadow fountain" || (Subjects[1] is Object && ((Object)Subjects[1]).Type == "shadow fountain")))
                {
                    // Shadow storage logic
                    if (Executor.Room == null)
                    {
                        bool StorageFound = false;

                        foreach (Object o in Executor.Block.Objects)
                        {
                            if (o.Type == "shadow fountain")
                            {
                                if (Executor.ShadowStorage.Contains((Object)Subjects[0]))
                                {
                                    // Multi-item check
                                    bool otherItemsExist = Executor.ShadowStorage.Count(obj => obj.Type == ((Object)Subjects[0]).Type && obj.Materials.SequenceEqual(((Object)Subjects[0]).Materials)) > 1;

                                    if (otherItemsExist)
                                    {
                                        Executor.SelectedContainer = o;
                                        Executor.TryTakeItemType = ((Object)Subjects[0]).Type;
                                        Executor.TryTakeMaterials.Clear();
                                        Executor.TryTakeMaterials.UnionWith(((Object)Subjects[0]).Materials);

                                        Game1.GameState = "trytake"; // Switch to selection state
                                    }
                                    else
                                    {
                                        // Proceed with normal item retrieval
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

                                        ((Object)Subjects[0]).PlaySound();

                                        // Add historical event for taking the item
                                        Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Executor.Name + " took " + Subjects[0].Name + " from shadow storage in " + Executor.Location.Name + ".", Executor.Location.Region, new EntityList<Entity>() { Executor, Subjects[0], Executor.Location }));

                                    }
                                }
                                else
                                {
                                    MakeObservation("The shadow storage does not contain that.", Color.Green, new EntityList<Entity>());
                                }
                                break;
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

                    // Standard container item retrieval
                    foreach (Object obj in searchScope)
                    {
                        foreach (Object containedObj in obj.ContainedObjects)
                        {
                            if (containedObj.Type == ((Object)Subjects[0]).Type && containedObj.Materials.SequenceEqual(((Object)Subjects[0]).Materials))
                            {
                                container = obj;
                                itemToTake = containedObj;
                                break;
                            }
                        }
                        if (container != null && itemToTake != null) break;
                    }

                    if (container == null || itemToTake == null)
                    {
                        MakeObservation("You cannot take that for some reason.", Color.Green, new EntityList<Entity>());
                    }
                    else
                    {
                        // Multi-item check
                        bool otherItemsExist = container.ContainedObjects.Count(obj => obj.Type == itemToTake.Type && obj.Materials.SequenceEqual(itemToTake.Materials)) > 1;

                        if (otherItemsExist)
                        {
                            Executor.SelectedContainer = container;
                            Executor.TryTakeItemType = itemToTake.Type;
                            Executor.TryTakeMaterials.Clear();
                            Executor.TryTakeMaterials.UnionWith(itemToTake.Materials);

                            Game1.GameState = "trytake"; // Switch to selection state
                        }
                        else
                        {
                            // Proceed with taking the item
                            if (Executor.MainHeldObject == null) Executor.MainHeldObject = itemToTake;
                            else if (Executor.OffHeldObject == null) Executor.OffHeldObject = itemToTake;
                            else Executor.Inventory.Add(itemToTake);

                            Executor.IDidSomethingBadSoScanForShockMines();

                            container.ContainedObjects.Remove(itemToTake);

                            MakeObservation("You remove the " + itemToTake.ReferredToNames[0] + " from the " + container.ReferredToNames[0] + ".", Color.Green, new EntityList<Entity>() { itemToTake, container });

                            if (Game1.PossibleMagicalItems.Contains(((Object)Subjects[0]).Type)
                                        && ((Object)Subjects[0]).SpecialKnowledge != null
                                        && Game1.GameWorld.AllLegendarySpells.Contains(((Object)Subjects[0]).SpecialKnowledge))
                            {
                                MakeObservation(
                                    "This legendary artifact contains an unprecedented magic of unknown origin.",
                                    Color.PaleVioletRed,
                                    new EntityList<Entity>()
                                );
                                MakeObservation(
                                    ((Object)Subjects[0]).SpecialKnowledge.Name,
                                    Color.PaleVioletRed,
                                    new EntityList<Entity>()
                                );
                                MakeObservation(
                                    Game1.SkillSpellDescriptions[((Object)Subjects[0]).SpecialKnowledge.Name],
                                    Color.PaleVioletRed,
                                    new EntityList<Entity>()
                                );

                            }

                            itemToTake.PlaySound();

                            if (Executor.Structure != null && Executor.Structure.Type == "market")
                            {
                                Executor.Structure.MarketDebtToUs -= itemToTake.Value();
                            }
                        }
                    }
                }
            }

            else if (CommandID == "place_item_in")
            {
                Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed));

                Object container = (Subjects[1] is Object subjectObject && subjectObject.IsContainer) ? subjectObject : null;
                bool isShadowStorage = Subjects[1].Metadata == "shadow fountain" || (Subjects[1] is Object && ((Object)(Subjects[1])).Type == "shadow fountain");

                if (isShadowStorage)
                {
                    foreach (Object o in Executor.Block.Objects)
                    {
                        if (o.Type == "shadow fountain")
                        {
                            container = o;
                            break;
                        }
                    }

                    if (container == null)
                    {
                        MakeObservation("There is no shadow storage nearby.", Color.Yellow, new EntityList<Entity>());
                    }
                }

                if (container == null)
                {
                    MakeObservation(Subjects[1].ReferredToNames[0] + " can't hold anything.", Color.Yellow, new EntityList<Entity>());
                }
                else
                {
                    Object itemToPlace = (Object)Subjects[0];

                    // Multi-item check
                    bool otherItemsExist = Executor.Inventory.Count(obj => obj.Type == itemToPlace.Type && obj.Materials.SequenceEqual(itemToPlace.Materials)) > 1;

                    if (otherItemsExist)
                    {
                        Executor.SelectedContainerForPlacing = container;
                        Executor.TryPlaceItemType = itemToPlace.Type;
                        Executor.TryPlaceMaterials.Clear();
                        Executor.TryPlaceMaterials.UnionWith(itemToPlace.Materials);

                        Game1.GameState = "tryplace"; // Switch to selection state
                    }
                    else
                    {
                        // Remove from inventory or hands
                        if (Executor.MainHeldObject == itemToPlace) Executor.MainHeldObject = null;
                        else if (Executor.OffHeldObject == itemToPlace) Executor.OffHeldObject = null;
                        else Executor.Inventory.Remove(itemToPlace);

                        // Place into the container (shadow storage or normal)
                        if (isShadowStorage)
                        {
                            Executor.ShadowStorage.Add(itemToPlace);
                            MakeObservation("You place the " + itemToPlace.ReferredToNames[0] + " into the shadow storage.", Color.Green, new EntityList<Entity>() { itemToPlace });
                        }
                        else
                        {
                            container.ContainedObjects.Add(itemToPlace);
                            MakeObservation("You place the " + itemToPlace.ReferredToNames[0] + " into the " + container.ReferredToNames[0] + ".", Color.Green, new EntityList<Entity>() { itemToPlace, container });
                        }

                        itemToPlace.PlaySound();

                        if (Executor.Structure != null && Executor.Structure.Type == "market")
                        {
                            Executor.Structure.MarketDebtToUs += itemToPlace.Value();
                        }
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
                            if (district != Executor.District && district.DistrictArchitects.Count > 0)
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
                if (Executor.TriedAscend == false)
                {
                    MakeObservation("The \"ascend\" command lets you access Ascendant Mode from your current player. This process cannot be reversed. Redo this action to ascend to power.", Color.Red, new EntityList<Entity>());
                    Executor.TriedAscend = true;
                }
                else
                {
                    if (Executor.Structure != null && Game1.GameWorld.GamePlayerAssociation.Residences.Contains(Executor.Structure))
                    {
                        Game1.GameState = "ascendant";
                        Game1.AscendantState = "main";
                        Game1.GameWorld.GameMode = "ascendant";

                        foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                        {
                            Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + a.Name + " sought greater control in " + a.Location.Name + ".", Executor.Location.Region, new EntityList<Entity>() { a, a.Location }));
                        }

                        Executor.Location.Government = Game1.GameWorld.GamePlayerAssociation.ActiveParty;

                        Game1.GameWorld.GamePlayerAssociation.Residences.Add(Executor.Structure);

                        Game1.GameWorld.RevealNearbyTiles(Game1.GameWorld.GamePlayerAssociation.ActiveParty.MapCursorX, Game1.GameWorld.GamePlayerAssociation.ActiveParty.MapCursorZ, 24, false);

                        Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects[0].District.Unload();

                        foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                        {
                            // Break all prisms into fragments
                            var prismsToConvert = a.Inventory
                                .Where(item => item.Type == "prism" && item.Name == null && item.Materials[0] == Game1.GameWorld.Vitalium)
                                .ToList();

                            foreach (var prism in prismsToConvert)
                            {
                                a.Inventory.Remove(prism);
                                prism.Delete();

                                for (int i = 0; i < 50; i++)
                                {
                                    a.Inventory.Add(
                                        new Object(null, "fragment", new EntityList<Material> { Game1.GameWorld.Vitalium }, null)
                                    );
                                }
                            }
                        }


                        Game1.GameWorld.GamePlayerAssociation.Associates.AddRange(Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects);
                        Game1.GameWorld.GamePlayerAssociation.Associates = Game1.GameWorld.GamePlayerAssociation.Associates.Distinct();

                        MakeObservation(Game1.GameWorld.GamePlayerAssociation.Name + " has elevated to a much greater operation. However, instability is inevitable with such a misplaced group. Act quickly to ensure loyalty, and your new association will live long and prosperously.", Color.OrangeRed, new EntityList<Entity>());

                    }
                    else
                    {
                        MakeObservation("You do not control the structure you are in. If you have no competition, you can claim a structure with \"claim this structure\"", Color.Red, new EntityList<Entity>());
                    }
                }

            }
            else if (CommandID == "wait")
            {
                string timeKey = Subjects[0].Metadata?.ToLowerInvariant();
                Dictionary<string, decimal> waitTimeLookup = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
{
    { "a cycle", 0.1m },
    { "one cycle", 0.1m },
    { "two cycles", 0.2m },
    { "three cycles", 0.3m },
    { "four cycles", 0.4m },
    { "five cycles", 0.5m },
    { "six cycles", 0.6m },
    { "seven cycles", 0.7m },
    { "eight cycles", 0.8m },
    { "nine cycles", 0.9m },
    { "a second", 1 },
    { "one second", 1 },
    { "two seconds", 2 },
    { "three seconds", 3 },
    { "four seconds", 4 },
    { "five seconds", 5 },
    { "six seconds", 6 },
    { "seven seconds", 7 },
    { "eight seconds", 8 },
    { "nine seconds", 9 },
    { "ten seconds", 10 },
    { "eleven seconds", 11 },
    { "twelve seconds", 12 },
    { "thirteen seconds", 13 },
    { "fourteen seconds", 14 },
    { "fifteen seconds", 15 },
    { "sixteen seconds", 16 },
    { "seventeen seconds", 17 },
    { "eighteen seconds", 18 },
    { "nineteen seconds", 19 },
    { "twenty seconds", 20 },
    { "twenty-one seconds", 21 },
    { "twenty-two seconds", 22 },
    { "twenty-three seconds", 23 },
    { "twenty-four seconds", 24 },
    { "twenty-five seconds", 25 },
    { "thirty seconds", 30 },
    { "thirty-five seconds", 35 },
    { "forty seconds", 40 },
    { "forty-five seconds", 45 },
    { "fifty seconds", 50 },
    { "fifty-five seconds", 55 },
    { "sixty seconds", 60 },
    { "a minute", 60 },
    { "one minute", 60 },
    { "two minutes", 120 },
    { "three minutes", 180 },
    { "four minutes", 240 },
    { "five minutes", 300 },
    { "six minutes", 360 },
    { "seven minutes", 420 },
    { "eight minutes", 480 },
    { "nine minutes", 540 },
    { "ten minutes", 600 },
    { "fifteen minutes", 900 },
    { "twenty minutes", 1200 },
    { "thirty minutes", 1800 },
    { "forty-five minutes", 2700 },
    { "one hour", 3600 },
    { "an hour", 3600 }
};

                if (timeKey != null && waitTimeLookup.TryGetValue(timeKey, out decimal value))
                {
                    bool isCycle = timeKey.Contains("cycle");
                    int cyclesToWait = (int)Math.Round(value * 10); // 10 cycles per second

                    Executor.CooldownCycles += cyclesToWait;

                    string observationMessage = isCycle
                        ? (cyclesToWait == 1
                            ? "You wait for one cycle."
                            : $"You wait for {cyclesToWait} cycles.")
                        : (value == 1
                            ? "You wait for one second."
                            : $"You wait for {value:0} seconds.");

                    MakeObservation(observationMessage, Color.Green, new EntityList<Entity>());
                }
                else
                {
                    MakeObservation("That is not a recognized time duration.", Color.ForestGreen, new EntityList<Entity>());
                }
            }
            else if (CommandID == "wear_item" && (Subjects.Count() == 1 || Subjects[0].Metadata == "all"))
            {
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed));

                if (Subjects[0].Metadata == "all")
                {
                    var wearableItems = Executor.Inventory.Where(item => item.IsWearable);

                    if (wearableItems.Count > 0)
                    {
                        (wearableItems[0]).PlaySound();
                    }

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
                                Executor.Structure.MarketDebtToUs += item.Value();
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
                            Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed));
                        }
                    }
                }
                else if (Subjects[0] is Object && (Executor.Inventory.Contains(((Object)Subjects[0])) || Executor.MainHeldObject == ((Object)Subjects[0]) || Executor.OffHeldObject == ((Object)Subjects[0])))
                {

                    Object item = ((Object)Subjects[0]);

                    if (item.IsWearable)
                    {
                        if (Executor.Clothing.Any(c => c.Type == item.Type) && item.Type != "amulet")
                        {
                            var existing = Executor.Clothing.First(c => c.Type == item.Type);

                            // Swap out existing item
                            Executor.Clothing.Remove(existing);
                            Executor.Inventory.Add(existing);

                            Executor.Clothing.Add(item);
                            Executor.Inventory.Remove(item);

                            MakeObservation($"You swap out the {existing.ReferredToNames[0]} for the {item.ReferredToNames[0]}.", Color.Green, new EntityList<Entity>() { existing, item });
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

                            ((Object)Subjects[0]).PlaySound();

                            Executor.Clothing.Add(((Object)Subjects[0]));
                        }
                    }
                    else
                    {
                        if (Executor.Clothing.Count() > 0)
                        {
                            MakeObservation("You hang the " + Subjects[0].ReferredToNames[0] + " off of your " + Executor.Clothing[Game1.GameWorld.rnd.Next(Executor.Clothing.Count())].ReferredToNames[0] + ". You feel awkwardly disadvantaged, but stylish.", Color.Green, new EntityList<Entity>() { Subjects[0] });
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

                        Game1.SFX.Add(Game1.Shatter);

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
                List<Entity> effectiveSubjects = (Subjects.Count == 2 && Subjects[0].Metadata == "up")
    ? Subjects.Skip(1).ToList()
    : Subjects;

                Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed));

                bool KeepAddingSounds = true;

                if (effectiveSubjects[0] is Object || effectiveSubjects[0].Metadata == "all")
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

                    if (effectiveSubjects[0].Metadata == "all")
                    {
                        // Summarized 'pick up all' logic
                        var objectsToPickUp = objectList.ToList();
                        int pickedUpCount = 0;
                        int tooHeavyCount = 0;

                        // We'll still record historical events, but skip the repeated pick-up messages
                        foreach (var obj in objectsToPickUp)
                        {
                            if (obj.Weight > 6000)
                            {
                                tooHeavyCount++;
                                continue;
                            }

                            // Remove from room/block and add to inventory
                            objectList.Remove(obj);
                            LoadedArchitects[Game1.ArchitectIndex].Inventory.Add(obj);

                            // Remove references to the old location
                            if (obj.Room != null && obj.Room.Structure.HistoricalObjects.Contains(obj))
                            {
                                obj.Room.Structure.HistoricalObjects.Remove(obj);
                            }
                            obj.Block = null;
                            obj.Room = null;

                            // Historical event for each object
                            if (obj.Name != null)
                            {
                                Game1.GameWorld.HistoricalEvents.Add(new Event(
                                    Date + " " + Executor.Name + " acquired " + obj.Name + " in " + Executor.Location.Name + ".",
                                    Executor.Location.Region,
                                    new EntityList<Entity>() { Executor, obj, Executor.Location }
                                ));
                            }

                            // GUI updates if it has imbuements or a name
                            if (obj.Imbuements.Count() > 0 || obj.IsWeapon || obj.AmuletGift != "")
                            {
                                Game1.IsInGui = true;
                                if (obj.Name != null)
                                {
                                    Game1.ItemPickupGuiLines.Add(
                                        Game1.Capitalize(obj.Name) + ", " +
                                        Game1.Capitalize(obj.Materials[0].Name) + " " +
                                        Game1.Capitalize(obj.Type)
                                    );
                                }
                                else
                                {
                                    Game1.ItemPickupGuiLines.Add(
                                        Game1.Capitalize(obj.Materials[0].Name) + " " +
                                        Game1.Capitalize(obj.Type)
                                    );
                                }

                                if (obj.Materials[0].Type == "metal")
                                {
                                    Executor.CompanionMessage("materials", "");
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

                                if(obj.AmuletGift != "")
                                {
                                    Game1.ItemPickupGuiLines.Add("This amulet provides a divine power.");
                                    Game1.ItemPickupGuiLines.Add(obj.Description);
                                }

                                Game1.PickupConfirm.InvisibleLock = false;
                            }

                            // Market debt adjustments
                            if (Executor.Structure != null && Executor.Structure.Type == "market")
                            {
                                Executor.Structure.MarketDebtToUs -= obj.Value();
                            }

                            // Cooldown
                            Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed));
                            pickedUpCount++;
                        }

                        // Single summarized messages
                        if (pickedUpCount > 0)
                        {
                            MakeObservation(
                                $"You gather up {pickedUpCount} item{(pickedUpCount > 1 ? "s" : "")} and add {(pickedUpCount > 1 ? "them" : "it")} to your inventory.",
                                Color.Yellow,
                                new EntityList<Entity>()
                            );
                        }
                        else
                        {
                            MakeObservation("There are no items you can pick up here.", Color.Yellow, new EntityList<Entity>());
                        }

                        if (tooHeavyCount > 0)
                        {
                            MakeObservation(
                                $"{tooHeavyCount} item{(tooHeavyCount > 1 ? "s" : "")} {(tooHeavyCount > 1 ? "were" : "was")} too heavy to pick up.",
                                Color.Yellow,
                                new EntityList<Entity>()
                            );
                        }
                    }
                    else if (objectList != null)
                    {
                        // Search for other objects with the same ReferredToNames[0]
                        bool otherObjectsExist = objectList.Any(
                            obj => obj != effectiveSubjects[0] && obj.ReferredToNames[0] == effectiveSubjects[0].ReferredToNames[0]
                        );

                        if (otherObjectsExist)
                        {
                            if (((Object)effectiveSubjects[0]).Weight > 6000)
                            {
                                MakeObservation(
                                    "The " + effectiveSubjects[0].ReferredToNames[0] + " is too heavy to pick up.",
                                    Color.Yellow,
                                    new EntityList<Entity>() { effectiveSubjects[0] }
                                );


                                if (((Object)effectiveSubjects[0]).Weight < (7000 + (Executor.Strength * 1000)))
                                    Executor.CompanionMessage("haul", "");
                            }
                            else
                            {
                                Executor.TryPickUpItemType = ((Object)effectiveSubjects[0]).Type;
                                Executor.TryPickUpMaterials.Clear();

                                foreach (var material in ((Object)effectiveSubjects[0]).Materials)
                                {
                                    Executor.TryPickUpMaterials.Add(material);
                                }
                            }
                        }
                        else
                        {
                            if (objectList.Contains(effectiveSubjects[0]))
                            {

                                if (((Object)effectiveSubjects[0]).Weight > 6000)
                                {
                                    MakeObservation(
                                        "The " + effectiveSubjects[0].ReferredToNames[0] + " is too heavy to pick up.",
                                        Color.Yellow,
                                        new EntityList<Entity>() { effectiveSubjects[0] }
                                    );

                                    if (((Object)effectiveSubjects[0]).Weight < (7000 + (Executor.Strength * 1000)))
                                        Executor.CompanionMessage("haul", "");
                                }
                                else
                                {
                                    // Proceed as normal
                                    MakeObservation(
                                        "You pick up the " + effectiveSubjects[0].ReferredToNames[0] + " and put it in your inventory.",
                                        Color.Yellow,
                                        new EntityList<Entity>() { effectiveSubjects[0] }
                                    );

                                    Executor.IDidSomethingBadSoScanForShockMines();



                                    objectList.Remove((Object)effectiveSubjects[0]);

                                    LoadedArchitects[Game1.ArchitectIndex].Inventory.Add((Object)effectiveSubjects[0]);

                                    if (KeepAddingSounds)
                                    {
                                        ((Object)effectiveSubjects[0]).PlaySound();
                                    }
                                    KeepAddingSounds = false;

                                    Game1.ItemPickupGuiLines.Clear();

                                    if (((Object)effectiveSubjects[0]).Room != null &&
                                        ((Object)effectiveSubjects[0]).Room.Structure.HistoricalObjects.Contains(((Object)effectiveSubjects[0])))
                                    {
                                        ((Object)effectiveSubjects[0]).Room.Structure.HistoricalObjects.Remove(((Object)effectiveSubjects[0]));
                                    }

                                    ((Object)effectiveSubjects[0]).Block = null;
                                    ((Object)effectiveSubjects[0]).Room = null;

                                    if (Game1.PossibleMagicalItems.Contains(((Object)effectiveSubjects[0]).Type)
                                        && ((Object)effectiveSubjects[0]).SpecialKnowledge != null
                                        && Game1.GameWorld.AllLegendarySpells.Contains(((Object)effectiveSubjects[0]).SpecialKnowledge))
                                    {
                                        MakeObservation(
                                            "This legendary artifact contains an unprecedented magic of unknown origin.",
                                            Color.PaleVioletRed,
                                            new EntityList<Entity>()
                                        );
                                        MakeObservation(
                                            ((Object)effectiveSubjects[0]).SpecialKnowledge.Name,
                                            Color.PaleVioletRed,
                                            new EntityList<Entity>()
                                        );
                                        MakeObservation(
                                            Game1.SkillSpellDescriptions[((Object)effectiveSubjects[0]).SpecialKnowledge.Name],
                                            Color.PaleVioletRed,
                                            new EntityList<Entity>()
                                        );

                                    }

                                    if (((Object)effectiveSubjects[0]).Name != null)
                                    {
                                        Game1.GameWorld.HistoricalEvents.Add(new Event(
                                            Date + " " + Executor.Name + " acquired " + ((Object)effectiveSubjects[0]).Name + " in " + Executor.Location.Name + ".",
                                            Executor.Location.Region,
                                            new EntityList<Entity>() { Executor, effectiveSubjects[0], Executor.Location }
                                        ));
                                    }

                                    if (((Object)effectiveSubjects[0]).Imbuements.Count() > 0 ||
                                        ((Object)effectiveSubjects[0]).IsWeapon || ((Object)effectiveSubjects[0]).AmuletGift != "")
                                    {
                                        Game1.IsInGui = true;

                                        if (((Object)effectiveSubjects[0]).Name != null)
                                        {
                                            Game1.ItemPickupGuiLines.Add(
                                                Game1.Capitalize(((Object)effectiveSubjects[0]).Name) + ", " +
                                                Game1.Capitalize(((Object)effectiveSubjects[0]).Materials[0].Name) + " " +
                                                Game1.Capitalize(((Object)effectiveSubjects[0]).Type)
                                            );
                                        }
                                        else
                                        {
                                            Game1.ItemPickupGuiLines.Add(
                                                Game1.Capitalize(((Object)effectiveSubjects[0]).Materials[0].Name) + " " +
                                                Game1.Capitalize(((Object)effectiveSubjects[0]).Type)
                                            );
                                        }

                                        if (((Object)effectiveSubjects[0]).Materials[0].Type == "metal")
                                        {
                                            Executor.CompanionMessage("materials", "");
                                        }

                                        if (((Object)effectiveSubjects[0]).Imbuements.Count() == 0)
                                        {
                                            Game1.ItemPickupGuiLines.Add("This object has no imbuements.");
                                        }
                                        else
                                        {
                                            List<string> ImbuementDescriptions = new List<string>();
                                            Game1.ItemPickupGuiLines.Add("This object has some intriguing properties.");
                                            foreach (Imbuement i in ((Object)effectiveSubjects[0]).Imbuements)
                                            {
                                                Game1.ItemPickupGuiLines.Add(i.GetDescription());
                                            }
                                        }

                                        if (((Object)effectiveSubjects[0]).AmuletGift != "")
                                        {
                                            Game1.ItemPickupGuiLines.Add("This amulet provides a divine power.");
                                            Game1.ItemPickupGuiLines.Add(((Object)effectiveSubjects[0]).Description);
                                        }


                                        Game1.PickupConfirm.InvisibleLock = false;
                                        Executor.TryComment("ontreasure", 25);
                                    }

                                    if (Executor.Structure != null && Executor.Structure.Type == "market")
                                    {
                                        Executor.Structure.MarketDebtToUs -= ((Object)(effectiveSubjects[0])).Value();
                                    }

                                    if (((Object)(effectiveSubjects[0])).Imbuements.Count > 0)
                                    {
                                        Executor.CompanionMessage("imbuements", "");
                                    }
                                    else if (((Object)(effectiveSubjects[0])).CompositionContent != null ||
         (Game1.LoadedHooks.Contains(effectiveSubjects[0]) && effectiveSubjects[0].HookedObjective.RequiredInteraction == "read") ||
            ((Object)(effectiveSubjects[0])).Type == "skill scroll" ||
         ((Object)(effectiveSubjects[0])).LetterContent != null ||
         (((Object)(effectiveSubjects[0])).Type == "book" && effectiveSubjects[0].Name != null && effectiveSubjects[0].Name.StartsWith("Chapter")))
                                    {
                                        Executor.CompanionMessage("reading", "");
                                    }

                                    else if (((Object)(effectiveSubjects[0])).IsWearable)
                                    {
                                        Executor.CompanionMessage("wear", "");
                                    }
                                    else if (((Object)(effectiveSubjects[0])).Type == "invocation crystal")
                                    {
                                        Executor.CompanionMessage("invocationcrystal", "");
                                    }
                                    else if (((Object)(effectiveSubjects[0])).IsConsumable && ((Object)(effectiveSubjects[0])).Type != "fragment")
                                    {
                                        Executor.CompanionMessage("consume", "");
                                    }
                                }
                            }
                            else
                            {
                                // Proceed as normal
                                MakeObservation(
                                    "That object is too far away.",
                                    Color.Yellow,
                                    new EntityList<Entity>() { effectiveSubjects[0] }
                                );
                            }
                        }

                        if (effectiveSubjects[0] is Object o && o.Type == "fragment" && o.Materials[0].Name == "vitalium")
                        {
                            int fragmentCount = Executor.Inventory.Count(item => item is Object obj && obj.Type == "fragment" &&
                                                                                 obj.Materials.Any(m => m.Name == "vitalium"));

                            if (fragmentCount >= 100 && !(Executor.Structure != null && Executor.Structure.Type == "market"))
                            {
                                Executor.CompanionMessage("lotoffrags", "");
                            }
                        }
                        else if (effectiveSubjects[0] is Object O && Game1.Tools.Contains(O.Type))
                        {
                            Executor.CompanionMessage("harvest", "");
                        }
                    }
                    else if (LoadedArchitects[Game1.ArchitectIndex].OffHeldObject == effectiveSubjects[0])
                    {
                        MakeObservation(
                            "You stash the " + effectiveSubjects[0].ReferredToNames[0] + ".",
                            Color.Yellow,
                            new EntityList<Entity>() { effectiveSubjects[0] }
                        );
                        LoadedArchitects[Game1.ArchitectIndex].OffHeldObject = null;
                        LoadedArchitects[Game1.ArchitectIndex].Inventory.Add((Object)effectiveSubjects[0]);
                        ((Object)effectiveSubjects[0]).PlaySound();
                    }
                    else if (LoadedArchitects[Game1.ArchitectIndex].MainHeldObject == effectiveSubjects[0])
                    {
                        MakeObservation(
                            "You stash the " + effectiveSubjects[0].ReferredToNames[0] + ".",
                            Color.Yellow,
                            new EntityList<Entity>() { effectiveSubjects[0] }
                        );
                        LoadedArchitects[Game1.ArchitectIndex].MainHeldObject = null;
                        LoadedArchitects[Game1.ArchitectIndex].Inventory.Add((Object)effectiveSubjects[0]);
                        ((Object)effectiveSubjects[0]).PlaySound();
                    }
                    else
                    {
                        MakeObservation("You couldn't find anything like that in the area.", Color.Yellow, new EntityList<Entity>() { });
                    }
                }
                else if (effectiveSubjects[0] is Architect a && a.Race.Name == "shiba")
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
                Executor.CooldownCycles += 1;
                bool Found = true;

                // Test if the subject is 'all' to drop all items
                if (Subjects[0].Metadata == "all")
                {
                    var itemsToDrop = Executor.Inventory.ToList(); // Create a list of items to drop
                    int countToDrop = itemsToDrop.Count;
                    bool Market = false;

                    if (countToDrop > 0)
                    {
                        // We'll play sound only once for the entire batch
                        Executor.Inventory[0].PlaySound();

                        // Remove them all from inventory and place in current room/block
                        foreach (var itemToDrop in itemsToDrop)
                        {
                            Executor.Inventory.Remove(itemToDrop);

                            EntityList<Object> objectList = Executor.Room != null
                                ? Executor.Room.Objects
                                : Executor.Block.Objects;

                            if (Executor.Room != null)
                            {
                                Executor.Room.Objects.Add(itemToDrop);
                                itemToDrop.Room = Executor.Room;
                                itemToDrop.Block = Executor.Room.Structure.Block;
                            }
                            else
                            {
                                Executor.Block.Objects.Add(itemToDrop);
                                itemToDrop.Block = Executor.Block;
                            }

                            // Market debt for each item
                            if (Executor.Structure != null && Executor.Structure.Type == "market")
                            {
                                Executor.Structure.MarketDebtToUs += itemToDrop.Value();
                                Market = true;
                            }

                            // Historical event for each item
                            if (itemToDrop.Name != null)
                            {
                                if (Executor.Structure == null)
                                {
                                    Game1.GameWorld.HistoricalEvents.Add(new Event(
                                        Date + " " + Executor.Name + " dropped " + itemToDrop.Name + " in " + Executor.Location.Name + ".",
                                        Executor.Location.Region,
                                        new EntityList<Entity>() { Executor, itemToDrop, Executor.Location }
                                    ));
                                }
                                else
                                {
                                    Game1.GameWorld.HistoricalEvents.Add(new Event(
                                        Date + " " + Executor.Name + " dropped " + itemToDrop.Name + " in " + Executor.Location.Name +
                                        ", at the " + Executor.Structure.Type + " " + Executor.Structure.Name + ".",
                                        Executor.Location.Region,
                                        new EntityList<Entity>() { Executor, itemToDrop, Executor.Location, Executor.Structure }
                                    ));

                                    Executor.Structure.HistoricalObjects.Add(itemToDrop);
                                }
                            }

                            // Cooldown for each drop
                            Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed));
                        }

                        // Single summarized message
                        MakeObservation(
                            $"You drop all {Humanizer.NumberToWordsExtension.ToWords(countToDrop)} item{(countToDrop > 1 ? "s" : "")} from your inventory.",
                            Color.Yellow,
                            new EntityList<Entity>()
                        );
                    }
                    else
                    {
                        MakeObservation("Your inventory is empty.", Color.Yellow, new EntityList<Entity>() { });
                    }

                    if (Market)
                        Executor.TryComment("exchange", 20);
                }
                else if (Executor.CarryingEntity is Object carriedItem && carriedItem == Subjects[0])
                {
                    if (Executor.Room != null)
                    {
                        Executor.Room.Objects.Add(carriedItem);
                        carriedItem.Room = Executor.Room;
                        carriedItem.Block = Executor.Room.Structure.Block;
                        MakeObservation(
                            "You drop the " + carriedItem.ReferredToNames[0] + ".",
                            Color.Yellow,
                            new EntityList<Entity>() { carriedItem }
                        );
                        carriedItem.PlaySound();
                    }
                    else
                    {
                        Executor.Block.Objects.Add(carriedItem);
                        carriedItem.Block = Executor.Block;
                        MakeObservation(
                            "You drop the " + carriedItem.ReferredToNames[0] + ".",
                            Color.Yellow,
                            new EntityList<Entity>() { carriedItem }
                        );
                        carriedItem.PlaySound();
                    }

                    if (Executor.Structure != null && Executor.Structure.Type == "market")
                    {
                        Executor.Structure.MarketDebtToUs += carriedItem.Value();


                        Executor.TryComment("exchange", 20);
                    }

                    // Add historical event for dropping the hauled item
                    if (carriedItem.Name != null)
                    {
                        if (Executor.Structure == null)
                        {
                            Game1.GameWorld.HistoricalEvents.Add(new Event(
                                Date + " " + Executor.Name + " dropped " + carriedItem.Name + " in " + Executor.Location.Name + ".",
                                Executor.Location.Region,
                                new EntityList<Entity>() { Executor, carriedItem, Executor.Location }
                            ));
                        }
                        else
                        {
                            Game1.GameWorld.HistoricalEvents.Add(new Event(
                                Date + " " + Executor.Name + " dropped " + carriedItem.Name + " in " + Executor.Location.Name +
                                ", at the " + Executor.Structure.Type + " " + Executor.Structure.Name + ".",
                                Executor.Location.Region,
                                new EntityList<Entity>() { Executor, carriedItem, Executor.Location, Executor.Structure }
                            ));

                            Executor.Structure.HistoricalObjects.Add(carriedItem);
                        }
                    }

                    // Clear the carried entity
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
                        bool otherObjectsExist = Executor.Inventory.Any(
                            obj => obj != itemToDrop && obj.ReferredToNames[0] == itemToDrop.ReferredToNames[0]
                        );

                        if (otherObjectsExist)
                        {
                            // Set up the TryDrop logic for duplicates
                            Executor.TryDropItemType = itemToDrop.Type;
                            Executor.TryDropMaterials.Clear();

                            foreach (var material in itemToDrop.Materials)
                            {
                                Executor.TryDropMaterials.Add(material);
                            }

                            // Optionally switch game state for duplicate handling
                            Game1.GameState = "trydrop";

                            return true; // Exit early to avoid completing the drop action
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
                        EntityList<Object> objectList = Executor.Room != null
                            ? Executor.Room.Objects
                            : Executor.Block.Objects;

                        if (Executor.Room != null)
                        {
                            Executor.Room.Objects.Add(itemToDrop);
                            itemToDrop.Room = Executor.Room;
                            itemToDrop.Block = Executor.Room.Structure.Block;
                            MakeObservation(
                                "You drop the " + itemToDrop.ReferredToNames[0] + ".",
                                Color.Yellow,
                                new EntityList<Entity>() { itemToDrop }
                            );
                            itemToDrop.PlaySound();
                        }
                        else
                        {
                            Executor.Block.Objects.Add(itemToDrop);
                            itemToDrop.Block = Executor.Block;
                            MakeObservation(
                                "You drop the " + itemToDrop.ReferredToNames[0] + ".",
                                Color.Yellow,
                                new EntityList<Entity>() { itemToDrop }
                            );
                            itemToDrop.PlaySound();
                        }

                        if (Executor.Structure != null && Executor.Structure.Type == "market")
                        {
                            Executor.Structure.MarketDebtToUs += itemToDrop.Value();
                            Executor.TryComment("exchange", 20);
                        }

                        // Add historical event for dropping the item
                        if (itemToDrop.Name != null)
                        {
                            if (Executor.Structure == null)
                            {
                                Game1.GameWorld.HistoricalEvents.Add(new Event(
                                    Date + " " + Executor.Name + " dropped " + itemToDrop.Name + " in " + Executor.Location.Name + ".",
                                    Executor.Location.Region,
                                    new EntityList<Entity>() { Executor, itemToDrop, Executor.Location }
                                ));
                            }
                            else
                            {
                                Game1.GameWorld.HistoricalEvents.Add(new Event(
                                    Date + " " + Executor.Name + " dropped " + itemToDrop.Name + " in " + Executor.Location.Name +
                                    ", at the " + Executor.Structure.Type + " " + Executor.Structure.Name + ".",
                                    Executor.Location.Region,
                                    new EntityList<Entity>() { Executor, itemToDrop, Executor.Location, Executor.Structure }
                                ));

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


            else if (CommandID == "drop_haul")
            {
                Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed));

                // Check if Executor is carrying anything
                if (Executor.CarryingEntity == null)
                {
                    MakeObservation("You are not carrying anything right now.", Color.Yellow, new EntityList<Entity>());
                }
                else
                {
                    // Handle the carried entity
                    if (Executor.CarryingEntity is Object carriedObject)
                    {
                        // Drop the carried object in the appropriate location
                        if (Executor.Room != null)
                        {
                            Executor.Room.Objects.Add(carriedObject);
                            carriedObject.Room = Executor.Room;
                            carriedObject.Block = Executor.Room.Structure.Block;
                            MakeObservation("You drop the " + carriedObject.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { carriedObject });
                            carriedObject.PlaySound();
                        }
                        else
                        {
                            Executor.Block.Objects.Add(carriedObject);
                            carriedObject.Block = Executor.Block;
                            MakeObservation("You drop the " + carriedObject.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { carriedObject });
                            carriedObject.PlaySound();
                        }

                        if (Executor.Structure != null && Executor.Structure.Type == "market")
                        {
                            Executor.Structure.MarketDebtToUs += carriedObject.Value();
                        }

                        // Add historical event for dropping the object
                        if (carriedObject.Name != null)
                        {
                            if (Executor.Structure == null)
                            {
                                Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Executor.Name + " dropped " + carriedObject.Name + " in " + Executor.Location.Name + ".", Executor.Location.Region, new EntityList<Entity>() { Executor, carriedObject, Executor.Location }));
                            }
                            else
                            {
                                Game1.GameWorld.HistoricalEvents.Add(new Event(Date + " " + Executor.Name + " dropped " + carriedObject.Name + " in " + Executor.Location.Name + ", at the " + Executor.Structure.Type + " " + Executor.Structure.Name + ".", Executor.Location.Region, new EntityList<Entity>() { Executor, carriedObject, Executor.Location, Executor.Structure }));

                                Executor.Structure.HistoricalObjects.Add(carriedObject);
                            }
                        }
                    }
                    else if (Executor.CarryingEntity is Architect carriedArchitect)
                    {
                        // Drop the carried architect
                        if (Executor.Room != null)
                        {
                            Executor.Room.Architects.Add(carriedArchitect);
                            carriedArchitect.Room = Executor.Room;
                            MakeObservation("You carefully place " + carriedArchitect.ReferredToNames[0] + " down.", Color.Yellow, new EntityList<Entity>() { carriedArchitect });
                        }
                        else
                        {
                            Executor.Block.Architects.Add(carriedArchitect);
                            carriedArchitect.Block = Executor.Block;
                            MakeObservation("You carefully place " + carriedArchitect.ReferredToNames[0] + " down.", Color.Yellow, new EntityList<Entity>() { carriedArchitect });
                        }
                    }

                    // Clear the carried entity
                    Executor.CarryingEntity = null;
                }
            }


            else if (CommandID == "remove_worn_item")
            {
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed));
                if (Subjects[0] is Object && Executor.Clothing.Contains(((Object)Subjects[0])))
                {
                    MakeObservation("You take off the " + Subjects[0].ReferredToNames[0] + ".", Color.Green, new EntityList<Entity>() { Subjects[0] });

                    // Remove the item from the Clothing list
                    Executor.Clothing.Remove((Object)Subjects[0]);

                    ((Object)Subjects[0]).PlaySound();

                    // Add the item back to the Executor's inventory
                    Executor.Inventory.Add((Object)Subjects[0]);
                }
                else if (Subjects[0] is Architect)
                {
                    if (((Architect)Subjects[0]).Race == GameWorld.GetRace("shiba") && Executor.MeldedShibas.Contains(Subjects[0]))
                    {
                        MakeObservation("You remove the shiba inu from your face, feeling a sense of loss.", Color.Green, new EntityList<Entity>());
                        Executor.MeldedShibas.Remove(((Architect)Subjects[0]));

                        Game1.SFX.Add(Game1.Revive);

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
                        MakeObservation("You can't take that off, its not a shiba inu. On a semi-related note, how the hell did you get that on? Did you have the AUDACITY to wear a shiba inu and then turn it into something else JUST to break my game? Did you ban the race of shiba inus? I don't even know what to say. What the hell probably works?", Color.Green, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You aren't wearing an object like that.", Color.Green, new EntityList<Entity>());
                }
            }
            else if (CommandID == "examine")
            {
                if (Executor.BlindCycles > 0)
                {
                    MakeObservation("You are blind.", Color.Magenta, new EntityList<Entity>());
                }
                else
                {

                    if (Subjects[0] is Architect a)
                    {
                        a = a.ArchitectILookLike;

                        MakeObservation($"{a.ReferredToNames[0]} (Race: {a.Race.Name})", Color.White, new EntityList<Entity> { a });
                        MakeObservation(a.Race.Description, Color.LimeGreen, new EntityList<Entity>());
                        MakeObservation(a.CheckEnergyLevel(), Color.Magenta, new EntityList<Entity>());
                        MakeObservation(a.DescribeArchitectInventory(), Color.Orange, new EntityList<Entity>());

                        var specialRaces = new HashSet<string>
                        {
                            "shade", "shadeheart", "isofractal", "icosidodecahedron", "photonexus", "hypernexus"
                        };

                        if (Game1.GameWorld.HumanoidRaces.Contains(a.Race) ||
                            specialRaces.Contains(a.Race.Name) ||
                            Game1.GameWorld.ColossalTypes.Contains(a.Race))
                        {
                            MakeObservation("Press F2 (or fn+F2) for a portrait.", Color.Cyan, new EntityList<Entity>());
                            Game1.StoredPortrait = a;
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

                            if (((Object)Subjects[0]).IsWeapon)
                            {
                                MakeObservation("Inflicts " + ((Object)Subjects[0]).DamageType + " damage.", Color.White, new EntityList<Entity>());
                            }

                            if (((Object)Subjects[0]).Type == "skill scroll" && ((Object)Subjects[0]).SpecialKnowledge != null)
                            {
                                MakeObservation("Contains knowledge of " + ((Object)Subjects[0]).SpecialKnowledge.Name + ".", Color.Lime, new EntityList<Entity>() { ((Object)Subjects[0]).SpecialKnowledge });
                            }


                            if (((Object)Subjects[0]).IsWeapon || ((Object)Subjects[0]).IsWearable || ((Object)Subjects[0]).Type == "bar")
                            {
                                MakeObservation("Primary Material Toughness: " + ((Object)Subjects[0]).Materials[0].Toughness, Color.White, new EntityList<Entity>());
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

                    if (Game1.LoadedFinalPointers.Contains(Subjects[0]))
                    {
                        MakeObservation(Subjects[0].IAmAFinalPointerThatFollowsAfterThisObjective.FinalMessage.Data, Subjects[0].IAmAFinalPointerThatFollowsAfterThisObjective.FinalMessage.Color, Subjects[0].IAmAFinalPointerThatFollowsAfterThisObjective.FinalMessage.Entities);
                        GameWorld.GamePlayerAssociation.ActiveParty.Intrigue.Add(new TextStorage(Subjects[0].IAmAFinalPointerThatFollowsAfterThisObjective.FinalIntrigue, Color.Magenta, new EntityList<Entity>()));


                        foreach (Party p in GameWorld.GamePlayerAssociation.Parties)
                            p.ActiveObjectives.Remove(Subjects[0].IAmAFinalPointerThatFollowsAfterThisObjective);

                        Game1.LoadedFinalPointers.Remove(Subjects[0]);

                    }

                    if (Game1.LoadedHooks.Contains(Subjects[0]) && (Subjects[0]).HookedObjective != null && (Subjects[0]).HookedObjective.RequiredInteraction == "examine")
                    {
                        MakeObservation("More interestingly however, " + (Subjects[0]).HookedObjective.PointerMessage.Data, (Subjects[0]).HookedObjective.PointerMessage.Color, (Subjects[0]).HookedObjective.PointerMessage.Entities);

                        TextStorage t = new TextStorage((Subjects[0]).HookedObjective.PointerIntrigue, Color.White, new EntityList<Entity>());

                        t.AttachedQuest = (Subjects[0]).HookedObjective.ParentQuest;

                        Executor.TrySendCompMessageForObjective((Subjects[0]).HookedObjective.ActualTask);


                        if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Executor))
                        {
                            Game1.GameWorld.GamePlayerAssociation.ActiveParty.ActiveObjectives.Add((Subjects[0]).HookedObjective);
                        }

                        Game1.GameWorld.GamePlayerAssociation.ActiveParty.Intrigue.Add(t);
                        MakeObservation("[Intrigue Updated]", Color.Magenta, new EntityList<Entity>());
                    }
                }
            }
            else if (CommandID == "give_item")
            {
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed));
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
                    MakeObservation(Executor.Name + ": Here, take this.", Color.White, new EntityList<Entity>() { Executor });
                    MakeObservation("You give the " + Subjects[0].ReferredToNames[0] + " to " + Subjects[1].ReferredToNames[0] + ".", Color.LightBlue, new EntityList<Entity>() { Subjects[1], GivenObject });

                    ((Object)Subjects[0]).PlaySound();

                    if (CanUnderstandEachOther(Subjects[1], Executor))
                    {
                        MakeObservation(Subjects[1].ReferredToNames[0] + ": Thank you. I appreciate this.", Color.Pink, new EntityList<Entity>() { Subjects[1] });
                    }
                    else
                    {
                        MakeObservation(Subjects[1].ReferredToNames[0] + ": *happy shibesque noises*", Color.Pink, new EntityList<Entity>() { Subjects[1] });
                    }

                    ((Architect)Subjects[1]).Inventory.Add(GivenObject);
                }
                else
                {
                    //subject 1 is an object

                    if (((Object)Subjects[1]).Type == "altar")
                    {
                        if (Game1.GameWorld.Cycle > (Executor.LastDivinationCycle+(864000)))
                        {
                            Executor.LastDivinationCycle = Game1.GameWorld.Cycle;
                            Executor.ShrineUsesLeft = 3;
                        }

                        if (Executor.ShrineUsesLeft > 0)
                        {
                            Executor.ShrineUsesLeft--;

                            ((Object)Subjects[0]).PlaySound();
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

                            int Outcome = (Quality * 2) + Game1.GameWorld.rnd.Next(-4, 5);

                            Outcome = Math.Max(0, Math.Min(Outcome, 22));



                            //outcome will be from 0-22, depending on quality
                            string OutcomeString = (new List<string>() { "reject", "reject", "reject", "reject", "reject", "coffee", "tea", "divineprotection", "double", "lightninggrenade", "icedtea", "icedcoffee", "spatialgrenade", "double", "divinemight", "double", "divinemight", "learnspell", "double", "convertmaterialtodivine", "divineartifact", "divineartifact", "divineweapon" })[Outcome];

                            Deity PrayingDeity;
                            if (LoadedArchitects[Game1.ArchitectIndex].Structure == null && LoadedArchitects[Game1.ArchitectIndex].Structure.Type == "shrine")
                            {
                                PrayingDeity = LoadedArchitects[Game1.ArchitectIndex].Structure.PrayingDeity;
                            }
                            else if (Game1.GameWorld.rnd.Next(1, 3) == 1)
                            {
                                PrayingDeity = Game1.GameWorld.LightDeity;
                            }
                            else
                            {
                                PrayingDeity = Game1.GameWorld.DarkDeity;
                            }

                            Game1.SFX.Add(Game1.Expel);

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
                                        MakeObservation(
                                            PrayingDeity.Name + " has blessed your offering and doubled it!",
                                            Color.Goldenrod,
                                            new EntityList<Entity>() { PrayingDeity }
                                        );

                                        Object firstClone = Game1.Clone(GivenObject);
                                        Object secondClone = Game1.Clone(GivenObject);

                                        Executor.Room.Objects.Add(firstClone);
                                        Executor.Room.Objects.Add(secondClone);

                                        break;
                                    }

                                case "lightninggrenade":
                                    {
                                        MakeObservation(PrayingDeity.Name + " has gifted you a strange sphere filled with lightning...", Color.Goldenrod, new EntityList<Entity>() { PrayingDeity });
                                        Executor.Room.Objects.Add(new Object(null, "lightning grenade", new EntityList<Material>() { GameWorld.Glass }, PrayingDeity));
                                        Executor.CompanionMessage("grenade", "");
                                        break;
                                    }
                                case "spatialgrenade":
                                    {
                                        MakeObservation(PrayingDeity.Name + " has gifted you a strange sphere filled with violet energy...", Color.Goldenrod, new EntityList<Entity>() { PrayingDeity });
                                        Executor.Room.Objects.Add(new Object(null, "spatial grenade", new EntityList<Material>() { GameWorld.Glass }, PrayingDeity));
                                        Executor.CompanionMessage("grenade", "");
                                        break;
                                    }
                                case "icedcoffee":
                                    {
                                        MakeObservation(PrayingDeity.Name + " has conjured for you a cup of iced coffee!", Color.Goldenrod, new EntityList<Entity>() { PrayingDeity });

                                        Object o = new Object(null, "small cup", new EntityList<Material>() { LoadedArchitects[Game1.ArchitectIndex].Location.HomeCivilization.CulturalStone }, PrayingDeity);
                                        o.ContainedObjects.Add(new Object(null, "drink", new EntityList<Material> { GameWorld.Coffee }, PrayingDeity));
                                        o.ContainedObjects.Add(new Object(null, "cube", new EntityList<Material> { GameWorld.Ices[Game1.GameWorld.rnd.Next(GameWorld.Ices.Count())] }, PrayingDeity));
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
                                        o.ContainedObjects.Add(new Object(null, "cube", new EntityList<Material> { GameWorld.Ices[Game1.GameWorld.rnd.Next(GameWorld.Ices.Count())] }, PrayingDeity));
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
                                        if (Game1.GameWorld.rnd.Next(1, 3) == 1 || GameWorld.DiscoveredSpells.Count() == 0)
                                        {
                                            var randomSpell = GameWorld.DiscoveredSpells[Game1.GameWorld.rnd.Next(GameWorld.DiscoveredSpells.Count())];
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
                                            Executor.Room.Objects.Add(Game1.GameWorld.GenerateRandomWeapon(WeaponMaterial, "rare"));
                                        }
                                        else
                                        {
                                            Executor.Block.Objects.Add(Game1.GameWorld.GenerateRandomWeapon(WeaponMaterial, "rare"));
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
                            MakeObservation("You fail to divinate. Perhaps you've been overambitious...", Color.Yellow, new EntityList<Entity>() { });
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
                if (Subjects[0] is Object o)
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
                        Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed));
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
                            Game1.Announcements.Add(new TextStorage("Your hands are empty. You must have an object in your hands to throw it.", Color.Yellow, new EntityList<Entity>() { }));
                        }
                        else
                        {
                            Game1.Announcements.Add(new TextStorage("You do not have an object like that in your hands.", Color.Yellow, new EntityList<Entity>() { }));
                        }
                    }
                    else
                    {
                        Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed));
                        MakeObservation("You fling your " + Subjects[0].ReferredToNames[0] + " at nothing. Expectedly, it falls to the ground.", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                        Game1.SFX.Add(Game1.Duck);
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
                else
                {
                    MakeObservation("You can only throw objects.", Color.Yellow, new EntityList<Entity>());
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
                            Game1.SFX.Add(Game1.Wield);
                        }
                        else if (Executor.MainHeldObject == null)
                        {
                            Executor.MainHeldObject = (Object)Subjects[0];
                            ThrowingObject = Executor.MainHeldObject;
                            Executor.Inventory.Remove((Object)Subjects[0]);
                            MakeObservation("You wield the " + Subjects[0].ReferredToNames[0] + " in your right hand.", Color.Yellow, new EntityList<Entity>());
                            Game1.SFX.Add(Game1.Wield);
                        }
                        else
                        {
                            MakeObservation("You need to have an open hand to pull out the " + Subjects[0].ReferredToNames[0] + " and throw it.", Color.Yellow, new EntityList<Entity>());
                            return false;
                        }
                        // Apply cooldown for wielding
                        Executor.CooldownCycles += (int)(Math.Round((15 - Executor.Dexterity) / Executor.Speed));
                    }
                    else
                    {
                        MakeObservation("The specified object is not in your inventory.", Color.Yellow, new EntityList<Entity>());
                        return false;
                    }
                }

                // Apply cooldown for throwing
                Executor.CooldownCycles += (int)(Math.Round((12 - Executor.Dexterity) / Executor.Speed));

                MakeObservation("You throw the " + Subjects[0].ReferredToNames[0] + "...", Color.Yellow, new EntityList<Entity>());
                Game1.SFX.Add(Game1.Duck);

                // Handle the logic for throwing at an architect or object
                if (Subjects[1] is Architect targetArchitect)
                {
                    Object targetBodyPart = targetArchitect.BodyParts[Game1.GameWorld.rnd.Next(targetArchitect.BodyParts.Count())];
                    ((Object)Subjects[0]).AirborneTarget = targetBodyPart;
                    MakeObservation("You aim at the " + targetArchitect.Name + "'s " + targetBodyPart.Type + ".", Color.Yellow, new EntityList<Entity>());
                }
                else if (Subjects[1] is Object targetObject)
                {
                    ((Object)Subjects[0]).AirborneTarget = targetObject;
                    MakeObservation("You aim at the " + targetObject.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>());
                }

                ((Object)Subjects[0]).AirborneCyclesToHitTarget = Math.Max(1, Game1.GameWorld.rnd.Next(12, 20) - Executor.Dexterity) * (((Object)Subjects[0]).Type.Contains("grenade") ? 10 : 1);
                ((Object)Subjects[0]).Thrower = Executor;
                ((Object)Subjects[0]).AirbornePower = Executor.Dexterity + Executor.GetDistance(Subjects[0]) + 3;

                Executor.ChangeXP("throwing", Game1.GameWorld.rnd.Next(1, 4));

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
                Entity Spell = Subjects[0];

                if (Executor.SpellsKnown.Contains(Spell) ||
                    (Executor.OffHeldObject != null && Executor.OffHeldObject.SpecialKnowledge == Spell) ||
                    (Executor.MainHeldObject != null && Executor.MainHeldObject.SpecialKnowledge == Spell))
                {
                    EntityList<Entity> Targets = new EntityList<Entity>();

                    var targetSubjects = Subjects.Skip(1).Take(numSubjects);  // Skip the spell itself, take the next N as targets

                    foreach (Entity e in targetSubjects)
                    {
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
                        for (int i = 0; i < Executor.SpellcastingPower; i++)
                        {
                            List<TextStorage> text = Executor.CastSpell(Spell.Metadata, Targets);

                            foreach (TextStorage t in text)
                            {
                                Executor.AnnounceToParty(t.Data, t.Color, t.Entities);
                            }
                        }
                    }
                    else
                    {
                        Executor.AnnounceToParty("Most spells can only target architects and objects.", Color.Yellow, new EntityList<Entity>());
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



                if (Executor.HealStrikes == 3)
                {
                    Executor.AnnounceToParty("You cannot use another healing item until you move.", Color.Red, new EntityList<Entity>());
                }
                else
                {
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

                        Executor.HealStrikes++;

                        if (Executor.HealStrikes == 3)
                        {
                            Executor.AnnounceToParty("Your body is sick of healing items. Move around a bit to use more.", Color.Red, new EntityList<Entity>());
                        }
                    }
                    else
                    {
                        MakeObservation("You don't have anything like that.", Color.Yellow, new EntityList<Entity>());
                    }

                }


                // Helper method to handle the consumption logic
                void ConsumeObject(Object EatingObject, Architect Executor)
                {
                    Executor.CooldownCycles += (int)Math.Round(10 * Executor.Speed);

                    Executor.TryComment("consume", 75);

                    Game1.SFX.Add(Game1.Apply);

                    

                    if (EatingObject.Type == "salve")
                    {
                        if (Executor.CombatCycles > 0)
                        {
                            MakeObservation("You swiftly apply the salve. The pain begins to vanish, but you are destabilized.", Color.Yellow, new EntityList<Entity>());
                            Executor.DestabilizedCycles += 120;
                        }
                        else
                        {
                            MakeObservation("You carefully apply the salve. The pain begins to vanish.", Color.Yellow, new EntityList<Entity>());
                        }

                        Executor.Pain = Math.Max(0, Executor.Pain - 35);
                        Executor.Bleeding = Math.Max(0, Executor.Bleeding - 2);
                        Executor.Energy += 5;

                    }
                    else if (EatingObject.Type == "bandage")
                    {
                        if (Executor.CombatCycles > 0)
                        {
                            MakeObservation("You swiftly apply the bandage. Your bleeding slows, but you are destabilized.", Color.Yellow, new EntityList<Entity>());
                            Executor.DestabilizedCycles += 120;
                        }
                        else
                        {
                            MakeObservation("You carefully apply the bandage. Your bleeding slows.", Color.Yellow, new EntityList<Entity>());
                        }

                        Executor.Pain = Math.Max(0, Executor.Pain - 5);
                        Executor.Bleeding = (int)Math.Round(Math.Max(0, Executor.Bleeding * 0.3m));
                    }
                    else if (EatingObject.Type == "vial")
                    {
                        int EnergyConstant = 50;

                        if (Executor.CombatCycles > 0)
                        {
                            MakeObservation("You swiftly drink the vial. You feel somewhat energized, but destabilized.", Color.Yellow, new EntityList<Entity>());
                            Executor.DestabilizedCycles += 120;
                            EnergyConstant = 30;
                        }
                        else
                        {
                            MakeObservation("You drink the vial. You feel energized.", Color.Yellow, new EntityList<Entity>());
                        }


                        Executor.Pain = Math.Max(0, Executor.Pain - 5);
                        Executor.Energy += Math.Max(0, EnergyConstant + Game1.GameWorld.rnd.Next(-10, 11));
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
                else if (Subjects[0].Metadata == "invocations")
                {
                    MakeObservation("Invocations Invoked:", Color.LightBlue, new EntityList<Entity>());

                    if (Executor.SkillsKnown.Count() == 0)
                    {
                        MakeObservation("You have not encountered any invocations.", Color.Yellow, new EntityList<Entity>());
                    }
                    else
                    {
                        foreach (string s in Executor.Invocations)
                        {
                            MakeObservation(s, Color.LightPink, new EntityList<Entity>() { });
                            MakeObservation(Game1.GiftDescriptions[s], Color.LightPink, new EntityList<Entity>() { });
                        }
                    }
                }
                else
                {
                    MakeObservation("Use this command to list either your spells, skills, or invocations.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "reposition")
            {
                MakeObservation("You reposition all of your limbs.", Color.MediumPurple, new EntityList<Entity>());

                Executor.CooldownCycles += (int)Math.Round(3 * Executor.Speed);

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
                Executor.CooldownCycles += (int)Math.Round(3 * Executor.Speed);

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

                        int index = Game1.GameWorld.rnd.Next(possibleResponses.Count());



                        a.KnownArchitects.Add(Executor);
                        Executor.KnownArchitects.Add(a);

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
                    if (Game1.LoadedHooks.Contains(objectToRead) && objectToRead.HookedObjective.RequiredInteraction == "read")
                    {
                        MakeObservation(objectToRead.HookedObjective.PointerMessage.Data, objectToRead.HookedObjective.PointerMessage.Color, objectToRead.HookedObjective.PointerMessage.Entities);

                        TextStorage t = new TextStorage(objectToRead.HookedObjective.PointerIntrigue, Color.White, new EntityList<Entity>());
                        t.AttachedQuest = objectToRead.HookedObjective.ParentQuest;
                        Game1.GameWorld.GamePlayerAssociation.ActiveParty.Intrigue.Add(t);
                        MakeObservation("[Intrigue Updated]", Color.Magenta, new EntityList<Entity>());

                        Executor.TrySendCompMessageForObjective(objectToRead.HookedObjective.ActualTask);

                        if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Executor))
                        {
                            Game1.GameWorld.GamePlayerAssociation.ActiveParty.ActiveObjectives.Add(objectToRead.HookedObjective);
                        }

                    }
                    else if (Game1.LoadedFinalPointers.Contains(objectToRead) && objectToRead.HookedObjective.RequiredInteractionForLast == "read")
                    {
                        MakeObservation(objectToRead.IAmAFinalPointerThatFollowsAfterThisObjective.FinalMessage.Data, objectToRead.IAmAFinalPointerThatFollowsAfterThisObjective.FinalMessage.Color, objectToRead.IAmAFinalPointerThatFollowsAfterThisObjective.FinalMessage.Entities);
                        GameWorld.GamePlayerAssociation.ActiveParty.Intrigue.Add(new TextStorage(objectToRead.IAmAFinalPointerThatFollowsAfterThisObjective.FinalIntrigue, Color.Magenta, new EntityList<Entity>()));
                        
                        foreach (Party p in Game1.GameWorld.GamePlayerAssociation.Parties)
                            p.ActiveObjectives.Remove(objectToRead.IAmAFinalPointerThatFollowsAfterThisObjective);

                        Game1.LoadedFinalPointers.Remove(objectToRead);
                    }
                    else if (objectToRead.Name != null && ChapterReadText.ContainsKey(objectToRead.Name))
                    {
                        bool toggleColor = true; // Flag to alternate colors
                        foreach (string paragraph in ChapterReadText[objectToRead.Name])
                        {
                            Color currentColor = toggleColor ? new Color(0, 255, 0) : new Color(0, 200, 125);
                            MakeObservation(paragraph.Trim(), currentColor, new EntityList<Entity>() { objectToRead });

                            // Toggle the color for the next iteration
                            toggleColor = !toggleColor;
                        }
                    }

                    else if (objectToRead.CompositionContent != null)
                    {
                        // Object has composition content
                        MakeObservation("You read " + objectToRead.ReferredToNames[0] + ". " + objectToRead.CompositionContent.GetCompleteWorkDescription(), Color.Honeydew, new EntityList<Entity>() { objectToRead });

                        int contentLength = objectToRead.CompositionContent.Sections.Count();
                        Executor.CooldownCycles += (int)(Math.Round((125 * contentLength) / Executor.Speed));

                        if (objectToRead.SpecialKnowledge != null)
                        {
                            if (GameWorld.AllSpells.Contains(objectToRead.SpecialKnowledge))
                            {
                                MakeObservation("You learned the spell \"" + objectToRead.SpecialKnowledge.Metadata + "\". You can cast it from the \"abilities\" tab.", Color.LightBlue, new EntityList<Entity>() { objectToRead.SpecialKnowledge });
                                MakeObservation(Game1.SkillSpellDescriptions[objectToRead.SpecialKnowledge.Metadata], Color.LightBlue, new EntityList<Entity>());
                                if (!Executor.SpellsKnown.Contains(objectToRead.SpecialKnowledge))
                                {
                                    Executor.SpellsKnown.Add(objectToRead.SpecialKnowledge);
                                }
                            }
                            else if (GameWorld.AllSkills.Contains(objectToRead.SpecialKnowledge))
                            {
                                MakeObservation("You learned the skill \"" + objectToRead.SpecialKnowledge.Metadata + "\". You can initiate it from the \"abilities\" tab.", Color.LightBlue, new EntityList<Entity>() { objectToRead.SpecialKnowledge });
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
                                MakeObservation("Your mind is too unfocused for " + Executor.SkillsKnown[0].Name + ".", Color.OrangeRed, new EntityList<Entity>() { Executor.SkillsKnown[0] });
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
                    MakeObservation("You don't have " + Subjects[0].ReferredToNames[0] + " in your hands or inventory.", Color.Red, new EntityList<Entity>() { Subjects[0] });
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
                    int numReactions = Math.Min(Game1.GameWorld.rnd.Next(1, 7), architects.Count());
                    EntityList<Architect> reactingArchitects = architects.ShuffleNew().Take(numReactions);


                    // React to performance in the vicinity
                    foreach (var architect in reactingArchitects)
                    {
                        if (architect != Executor && Game1.GameWorld.HumanoidRaces.Contains(architect.Race))
                        {
                            int randomModifier = Game1.GameWorld.rnd.Next(-2, 3);  // Random number from -2 to 2
                            int score = Executor.Charisma + randomModifier;
                            score = Math.Clamp(score, 0, 9); // Ensure score is within 0-9

                            // Determine the
                            // based on the score
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
                            MakeObservation(architect.ReferredToNames[0] + ": " + reaction, Color.Magenta, new EntityList<Entity>() { architect });
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
                Executor.CooldownCycles += (int)(Math.Round(100 / Executor.Speed));
                if (Subjects[0] is Object o)
                {
                    if (o.Materials[0].Type == "fiber")
                    {
                        bool Found = false;

                        if (Executor.MainHeldObject == o)
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

                        if (Found)
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
                Executor.CooldownCycles += (int)(Math.Round(100 / Executor.Speed));

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
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed));

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
                        if ((a.IsAlive == false || a.UnconsciousCycles > 0) && a.Block == Executor.Block)
                        {
                            a.IsAlive = false;
                            Executor.CooldownCycles += (int)(Math.Round(100 / Executor.Speed));

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
                        Executor.CooldownCycles += (int)(Math.Round(100 / Executor.Speed));
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
                            Executor.CooldownCycles += (int)(Math.Round(100 / Executor.Speed));
                            MakeObservation("You dig up " + buriedArchitect.ReferredToNames[0] + ".", Color.Orange, new EntityList<Entity>());
                        }
                        Executor.Block.BuriedArchitects.Clear();

                        // Return all buried objects to the surface
                        foreach (Object buriedObject in Executor.Block.BuriedObjects)
                        {
                            Executor.Block.Objects.Add(buriedObject);
                            Executor.CooldownCycles += (int)(Math.Round(100 / Executor.Speed));
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
            else if (CommandID == "get_down" || (CommandID == "climb" && Subjects[0].Metadata == "down"))
            {
                if (Executor.OnTopOfStructure != null)
                {
                    Executor.CooldownCycles += (int)(Math.Round(40 / Executor.Speed));
                    MakeObservation("You climb down from " + Executor.OnTopOfStructure.Name + ".", Color.Green, new EntityList<Entity>());
                    Executor.OnTopOfStructure = null;
                    Executor.YLevelInFeet = 0;
                }
                else
                {
                    MakeObservation("You are not on top of a structure.", Color.Orange, new EntityList<Entity>());
                }
            }
            else if (CommandID == "climb")
            {
                if (Subjects[0] is Structure s)
                {
                    if (Executor.Room != null)
                    {
                        if(Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Executor))
                            Executor.AnnounceToParty("You need to be outside.", Color.Orange, new EntityList<Entity>());
                    }
                    else if (s.Block == Executor.Block)
                    {
                        Executor.CooldownCycles += (int)(Math.Round(40 / Executor.Speed));
                        
                        if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Executor))
                            Executor.AnnounceToParty("You climb atop " + s.Name + ".", Color.Green, new EntityList<Entity>());

                        Executor.OnTopOfStructure = s;
                        Executor.YLevelInFeet += 30;
                    }
                    else
                    {
                        Executor.AnnounceToParty("There is not a structure like that nearby.", Color.Orange, new EntityList<Entity>());
                    }
                }
                else
                {
                    Executor.AnnounceToParty("You can only climb structures.", Color.Orange, new EntityList<Entity>());
                }
            }
            else if (CommandID == "carve")
            {
                Executor.CooldownCycles += (int)(Math.Round(100 / Executor.Speed));

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
                        Obj.UpdateCarvedSymbols();

                        Game1.SFX.Add(Game1.Parry);
                        Game1.SFX.Add(Game1.Parry);
                        Game1.SFX.Add(Game1.Parry);
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
                Executor.CooldownCycles += (int)(Math.Round(100 / Executor.Speed));

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
                        Obj.UpdateCarvedSymbols();
                        Game1.SFX.Add(Game1.Parry);
                        Game1.SFX.Add(Game1.Parry);
                        Game1.SFX.Add(Game1.Parry);
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
                // Helper function to get the opposing direction of a door
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

                Executor.CooldownCycles += (int)(Math.Round(100 / Executor.Speed));

                if (Subjects.Count >= 1 && Subjects[0] is Entity entityToReinforce)
                {
                    // Identify the reinforcement material
                    Object reinforcementMaterial = null;

                    // Check if the Executor is carrying a material
                    if (Executor.CarryingEntity is Object carriedObject && carriedObject.Weight > 3000)
                    {
                        reinforcementMaterial = carriedObject;
                    }
                    // Check if the material is in the Executor's hands
                    else if (Executor.MainHeldObject is Object mainHeld && mainHeld.Weight > 3000)
                    {
                        reinforcementMaterial = mainHeld;
                    }
                    else if (Executor.OffHeldObject is Object offHeld && offHeld.Weight > 3000)
                    {
                        reinforcementMaterial = offHeld;
                    }
                    // Check if the material is in the inventory
                    else
                    {
                        reinforcementMaterial = Executor.Inventory.FirstOrDefault(obj => obj.Weight > 3000);
                    }

                    // Check if the material is in the current room or block
                    if (reinforcementMaterial == null && Executor.Room != null)
                    {
                        reinforcementMaterial = Executor.Room.Objects.FirstOrDefault(obj => obj.Weight > 3000);
                    }
                    else if (reinforcementMaterial == null && Executor.Block != null)
                    {
                        reinforcementMaterial = Executor.Block.Objects.FirstOrDefault(obj => obj.Weight > 3000);
                    }

                    if (reinforcementMaterial == null)
                    {
                        MakeObservation("You don't have or see a suitable material to reinforce " + entityToReinforce.ReferredToNames[0] + ".", Color.Orange, new EntityList<Entity>());
                    }
                    else
                    {
                        // Check if the entity to reinforce is valid
                        bool canReinforce = false;

                        // Reinforce a structure
                        if (entityToReinforce is Structure structure)
                        {
                            if ((Executor.Structure == null && Executor.Block.Structures.Contains(structure)) ||
                                (Executor.Structure == structure && Executor.Room == structure.Rooms[0]))
                            {
                                canReinforce = true;
                                structure.Reinforced = true;
                                MakeObservation("You reinforce the structure door.", Color.Green, new EntityList<Entity>() { structure });

                                // Reinforce the first exit door in the structure's primary room
                                var exitDoor = structure.Rooms[0].Objects.FirstOrDefault(obj => obj.Type == "exit door");
                                if (exitDoor != null)
                                {
                                    exitDoor.Reinforced = true;
                                }
                            }
                            else
                            {
                                MakeObservation("You need to be near or inside the structure to reinforce it.", Color.Orange, new EntityList<Entity>());
                            }
                        }

                        // Reinforce a door
                        else if (entityToReinforce is Door door)
                        {
                            if (Executor.Room != null && Executor.Room.Objects.Contains(door))
                            {
                                canReinforce = true;
                                door.Reinforced = true;
                                MakeObservation("You reinforce the door, making it sturdier.", Color.Green, new EntityList<Entity>() { door });

                                // Find and reinforce the opposing door in the destination room
                                string opposingDirection = GetOpposingDirection(door.Direction);
                                var opposingDoor = door.DestinationRoom?.Objects
                                    .OfType<Door>()
                                    .FirstOrDefault(d => d.Direction == opposingDirection && d.DestinationRoom == Executor.Room);

                                if (opposingDoor != null)
                                {
                                    opposingDoor.Reinforced = true;
                                }
                            }
                            else
                            {
                                MakeObservation("You need to be in the same room as the door to reinforce it.", Color.Orange, new EntityList<Entity>());
                            }
                        }

                        // Reinforce an exit door
                        else if (entityToReinforce is Object obj && obj.Type == "exit door")
                        {
                            if (Executor.Room != null && Executor.Room.Objects.Contains(obj))
                            {
                                canReinforce = true;
                                obj.Reinforced = true;
                                MakeObservation("You reinforce the exit door, increasing its durability.", Color.Green, new EntityList<Entity>() { obj });

                                // Reinforce the structure containing the exit door
                                if (Executor.Structure != null)
                                {
                                    Executor.Structure.Reinforced = true;
                                }
                            }
                            else
                            {
                                MakeObservation("You need to be in the same room as the exit door to reinforce it.", Color.Orange, new EntityList<Entity>());
                            }
                        }
                        else
                        {
                            MakeObservation("You can only reinforce structures, doors, and exit doors.", Color.Orange, new EntityList<Entity>());
                        }

                        if (canReinforce)
                        {
                            // If reinforcement material is from carrying or inventory, remove it
                            if (reinforcementMaterial == Executor.CarryingEntity)
                            {
                                if (Executor.Room != null)
                                    Executor.Room.ObjectsToAdd.Add(reinforcementMaterial);
                                else
                                    Executor.Block.ObjectsToAdd.Add(reinforcementMaterial);

                                Executor.CarryingEntity = null;
                            }
                            else if (reinforcementMaterial == Executor.MainHeldObject)
                            {
                                Executor.MainHeldObject = null;
                            }
                            else if (reinforcementMaterial == Executor.OffHeldObject)
                            {
                                Executor.OffHeldObject = null;
                            }
                            else if (Executor.Inventory.Contains(reinforcementMaterial))
                            {
                                Executor.Inventory.Remove(reinforcementMaterial);
                            }

                            // If material is already in the room/block, no need to move it
                            if (!(Executor.Room != null && Executor.Room.Objects.Contains(reinforcementMaterial)) &&
                                !(Executor.Block != null && Executor.Block.Objects.Contains(reinforcementMaterial)))
                            {
                                if (Executor.Room != null)
                                {
                                    Executor.Room.Objects.Add(reinforcementMaterial);
                                }
                                else
                                {
                                    Executor.Block.Objects.Add(reinforcementMaterial);
                                }
                            }
                        }
                    }
                }
                else
                {
                    MakeObservation("You need a valid entity and suitable material to reinforce.", Color.Orange, new EntityList<Entity>());
                }
            }

            else if (CommandID == "ignite")
            {
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed));

                if (Subjects[0] is Object o)
                {
                    bool isInSameBlockOrRoomOrInventory =
                        o.Block == Executor.Block ||
                        o.Room == Executor.Room ||
                        Executor.Inventory.Contains(o);

                    if (isInSameBlockOrRoomOrInventory)
                    {
                        MakeObservation("You manifest some energy to strike a small flame.", Color.Green, new EntityList<Entity>());
                        o.FireSeconds += 3;
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
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed));

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


                            Game1.SFX.Add(Game1.Brewing);
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
                Executor.CooldownCycles += (int)(Math.Round(30 / Executor.Speed));

                if (Subjects[0] is Architect Interactee)
                {
                    // Check if they are in the same room or block
                    if (Executor.Room == Interactee.Room || Executor.Block == Interactee.Block)
                    {
                        if (Interactee.GetOpinion(Executor.ArchitectILookLike) > 20)
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

            else if (CommandID == "shapeshift")
            {
                if(Executor.Invocations.Contains("transformation"))
                {
                    if (ArchitectsToUse.Contains(Subjects[0]))
                    {
                        MakeObservation("You quietly transmorph into " + Subjects[0].ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>());
                        Executor.ArchitectILookLike = (Architect)(Subjects[0]);

                        Executor.ImportantThisLoad = true;
                        ((Architect)(Subjects[0])).ImportantThisLoad = true;
                    }
                    else
                    {
                        MakeObservation("Thats either not an architect, or not near you.", Color.Yellow, new EntityList<Entity>());

                    }
                }
                else
                {
                    MakeObservation("You do not have that power.", Color.Yellow, new EntityList<Entity>());
                }
            }

            else if (CommandID == "tickle")
            {
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed));

                if (Subjects[0] is Architect Interactee)
                {
                    // Check if they are in the same room or block
                    if (Executor.Room == Interactee.Room || Executor.Block == Interactee.Block)
                    {
                        if (Executor.Sex != Interactee.Sex)
                        {
                            if (Interactee.GetOpinion(Executor.ArchitectILookLike) > 15)
                            {
                                Interactee.ChangeOpinion(Executor, 2);
                                MakeObservation("You tickle " + Interactee.Name + ". They giggle and seem amused.", Color.Green, new EntityList<Entity>() { Interactee });
                            }
                            else
                            {
                                Interactee.ChangeOpinion(Executor.ArchitectILookLike, -3);
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
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed));

                if (Subjects[0] is Architect Interactee)
                {
                    // Check if they are in the same room or block
                    if (Executor.Room == Interactee.Room || Executor.Block == Interactee.Block)
                    {
                        if (Executor.Sex != Interactee.Sex)
                        {
                            if (Interactee.GetOpinion(Executor.ArchitectILookLike) > 30)
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
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed));

                // Check if the target is an Architect
                if (Subjects[0] is Architect Interactee)
                {
                    if(Interactee == Executor)
                    {
                        MakeObservation("You cannot do this until you find the spell that voids Newton's third law.", Color.Yellow, new EntityList<Entity>() { });
                    }
                    else
                    {
                        // Check if they are in the same room or block
                        if ((Executor.Room == Interactee.Room || Executor.Block == Interactee.Block) && Executor.GetDistance(Interactee) <= 1)
                        {
                            // Shove the architect and apply the distance logic
                            MakeObservation("You shove " + Interactee.Name + " forcefully.", Color.Orange, new EntityList<Entity>() { Interactee });

                            // Run the distance modification
                            Executor.ModifyDistance(Interactee, Executor.Strength >= 4 ? 4 : 3);
                            
                        }
                        else
                        {
                            MakeObservation("You are too far away to shove " + Interactee.Name + ".", Color.Yellow, new EntityList<Entity>() { Interactee });
                        }
                    }
                }
                else
                {
                    MakeObservation("You can't shove the " + Subjects[0].ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                }
            }
            else if (CommandID == "carry")
            {
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed));

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
                        // Check if the object is already in the Executor's inventory or hands
                        if (Executor.Inventory.Contains(subject) || subject == Executor.MainHeldObject || subject == Executor.OffHeldObject)
                        {
                            MakeObservation("That is already in your inventory. Use this command to move heavy objects or people.", Color.Yellow, new EntityList<Entity>());
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
                                    Executor.IDidSomethingBadSoScanForShockMines();
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
                                    Executor.CarryingEntity = obj;

                                    // Incur market debt if in a market
                                    if (Executor.Structure != null && Executor.Structure.Type == "market")
                                    {
                                        Executor.Structure.MarketDebtToUs -= obj.Value();
                                    }

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
            }
            else if (CommandID == "disarm_traps")
            {
                Executor.CooldownCycles += (int)(Math.Round(50 / Executor.Speed)); // Adjust the X to a reasonable value like 50

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
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed));

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
                                                    structureType == "hoard" || structureType == "outpost" || structureType == "sanctum" ||
                                                    structureType == "scaffold" || structureType == "scum" || structureType == "spire" ||
                                                    structureType == "stronghold";
                    }

                    if (isNonResponsiveStructure)
                    {
                        MakeObservation("Your knock echoes with no response.", Color.Gray, new EntityList<Entity>());
                    }
                    else if (responseArchitects.Count() > 0)
                    {
                        int responseIndex = Game1.GameWorld.rnd.Next(9); // Adjusted range to include all cases

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
                Executor.CooldownCycles += (int)(Math.Round(50 / Executor.Speed));
                Executor.DestabilizedCycles = 0;

                MakeObservation(Executor.Name + " stretches, stabilizing themselves.", Color.Green, new EntityList<Entity>());
            }
            else if (CommandID == "polish")
            {
                Executor.CooldownCycles += (int)(Math.Round(50 / Executor.Speed));

                if (Subjects[0] is Object objToPolish)
                {
                    objToPolish.Polished = true;
                    MakeObservation("You carefully polish the " + objToPolish.ReferredToNames[0] + ", making it shine.", Color.Green, new EntityList<Entity>() { objToPolish });
                    Game1.SFX.Add(Game1.Clean);
                }
                else
                {
                    MakeObservation("You can only polish objects.", Color.Yellow, new EntityList<Entity>());
                }
            }

            else if (CommandID == "clean")
            {
                Executor.CooldownCycles += (int)(Math.Round(50 / Executor.Speed));

                if (Subjects[0] is Object objToClean)
                {
                    objToClean.Cleaned = true;
                    Game1.SFX.Add(Game1.Clean);
                    MakeObservation("You clean the " + objToClean.ReferredToNames[0] + ", making it spotless.", Color.Green, new EntityList<Entity>() { objToClean });
                }
                else
                {
                    MakeObservation("You can only clean objects.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "bind")
            {
                Executor.CooldownCycles += (int)(Math.Round(50 / Executor.Speed));

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
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed));
                if (Subjects[0] is Architect && ((((Architect)(Subjects[0])).Room == Executor.Room) && (((Architect)(Subjects[0])).Block == Executor.Block)))
                {
                    MakeObservation(Executor.Name + ": Wild one, join the ranks of my great conquest.", Color.Green, new EntityList<Entity>() { Executor });
                    Game1.SFX.Add(Game1.TalkSounds[Executor.VoiceType]);

                    if (!GameWorld.HumanoidRaces.Contains(((Architect)Subjects[0]).Race) && !GameWorld.ExtraRaces.Contains(((Architect)Subjects[0]).Race) && !GameWorld.ConstructRaces.Contains(((Architect)Subjects[0]).Race) && !GameWorld.ColossalTypes.Contains(((Architect)Subjects[0]).Race))
                    {
                        int ExistingAnimals = 0;
                        foreach (Architect A in GameWorld.GamePlayerAssociation.ActiveParty.Architects)
                        {
                            if (!GameWorld.HumanoidRaces.Contains(A.Race) && !GameWorld.ExtraRaces.Contains(A.Race))
                            {
                                ExistingAnimals++;
                            }
                        }


                        if (Subjects[0] is Architect a && a.Race.Name == "debtshiba")
                        {
                            Executor.AnnounceToParty("One does not simply \"tame\" a debtshiba.", Color.Red, new EntityList<Entity>());
                            Executor.AnnounceToParty("You know not the power you ask.", Color.Red, new EntityList<Entity>());
                        }
                        else if (Executor.PathOfLifeLevel >= 6 && ExistingAnimals < Executor.PathOfLifeLevel)
                        {
                            MakeObservation(((Architect)Subjects[0]).ReferredToNames[0] + ": *happy shibesque noises*", Color.Green, new EntityList<Entity>() { Subjects[0] });
                            Game1.SFX.Add(Game1.Augment);

                            Executor.TryComment("uselife", 95);
                            GameWorld.GamePlayerAssociation.ActiveParty.Architects.Add(((Architect)Subjects[0]));
                        }
                        else
                        {
                            MakeObservation(((Architect)Subjects[0]).Name + ": *sad shibesque noises*", Color.Yellow, new EntityList<Entity>() { Subjects[0] });
                            Game1.SFX.Add(Game1.Augment);
                        }
                    }
                    else
                    {
                        MakeObservation(((Architect)Subjects[0]).ReferredToNames[0] + " *thinks to self* [Something is wrong with this one...]", Color.Orange, new EntityList<Entity>() { Subjects[0] });
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
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed));
                Executor.Energy -= Math.Max(0, 12 - Executor.PathOfStarsLevel);

                Executor.RuinInvisibility();

                if (Subjects[0] is Architect targetArchitect && (targetArchitect.Room == Executor.Room && targetArchitect.Block == Executor.Block))
                {
                    MakeObservation("You flick your wrist...", Color.Green, new EntityList<Entity>());
                    int StarCount = 0;

                    if (Executor.PathOfStarsLevel >= 6)
                    {
                        StarCount = Game1.GameWorld.rnd.Next(2, 4);
                        MakeObservation($"Stars fly from your hands!", Color.Goldenrod, new EntityList<Entity>());
                        Game1.SFX.Add(Game1.StarStrike);
                    }
                    else
                    {
                        MakeObservation($"...but nothing happens.", Color.Yellow, new EntityList<Entity>());
                    }

                    Executor.TryComment("usestars", 10);

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
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed));
                Executor.Energy -= Math.Max(0, 15 - Executor.PathOfHeatLevel);
                Executor.RuinInvisibility();

                if (Subjects[0] is Architect targetArchitect && ArchitectsToUse.Contains(Subjects[0]))
                {
                    MakeObservation("You wave...", Color.Green, new EntityList<Entity>());

                    if (Executor.PathOfHeatLevel >= 2)
                    {
                        MakeObservation($"A large flame emnates from your hand!", Color.Goldenrod, new EntityList<Entity>());
                        Game1.SFX.Add(Game1.FlameStrike);
                        Object o = new Object(null, "wave", new EntityList<Material>() { GameWorld.Flame }, Executor);
                        o.AirborneTarget = Subjects[0];
                        o.AirborneCyclesToHitTarget = 15;
                        Executor.TryComment("useheat", 20);
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
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed));
                Executor.Energy -= 10 - Executor.PathOfHeatLevel;

                if (Executor.PathOfHeatLevel >= 4)
                {
                    var targetObject = Subjects[0];

                    if (Executor.OffHeldObject == targetObject || Executor.MainHeldObject == targetObject)
                    {
                        MakeObservation("You focus...", Color.Green, new EntityList<Entity>());
                        ((Object)targetObject).HeatInCelsius += 50;
                        Game1.SFX.Add(Game1.FlameStrike);
                        MakeObservation($"The {targetObject.ReferredToNames[0]} in your hand heats up intensely!", Color.Goldenrod, new EntityList<Entity>() { targetObject });

                        Executor.TryComment("useheat", 50);
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
                Executor.RuinInvisibility();

                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed));
                Executor.Energy -= Math.Max(0, 25 - Executor.PathOfStarsLevel);

                Executor.TryComment("usestars", 10);

                if (Subjects[0] is Architect targetArchitect && (targetArchitect.Room == Executor.Room && targetArchitect.Block == Executor.Block))
                {
                    MakeObservation("You point and wave...", Color.Green, new EntityList<Entity>());

                    if (Executor.PathOfStarsLevel >= 8)
                    {
                        MakeObservation($"A swirling vortex appears, and a cosmic energy beam strikes " + Subjects[0].ReferredToNames[0] + "!", Color.Goldenrod, new EntityList<Entity>() { Subjects[0] });
                        Game1.SFX.Add(Game1.StarSmite);

                        foreach (Object o in targetArchitect.BodyParts)
                        {
                            o.Integrity -= Game1.GameWorld.rnd.Next(10, 15 + Executor.PathOfStarsLevel);
                        }
                        targetArchitect.Bleeding += Game1.GameWorld.rnd.Next(2, 2 + Executor.PathOfStarsLevel);
                        targetArchitect.Energy -= Game1.GameWorld.rnd.Next(4, 8 + Executor.PathOfStarsLevel);

                        targetArchitect.ChangeOpinion(Executor, -1900);
                    }
                    else
                    {
                        MakeObservation($"...but nothing happens.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else if (Subjects[0] is Object targetObject && (targetObject.Room == Executor.Room && targetObject.Block == Executor.Block))
                {
                    MakeObservation("You point and focus your cosmic energy...", Color.Green, new EntityList<Entity>());

                    if (Executor.PathOfStarsLevel >= 8)
                    {
                        MakeObservation($"A swirling vortex appears, and a cosmic energy beam strikes the object!", Color.Goldenrod, new EntityList<Entity>() { Subjects[0] });
                        Game1.SFX.Add(Game1.StarSmite);

                        targetObject.Integrity -= 3 * Game1.GameWorld.rnd.Next(10, 15 + Executor.PathOfStarsLevel);
                    }
                    else
                    {
                        MakeObservation($"...but nothing happens.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You couldn't find a valid target nearby.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "conjure_spark")
            {
                Executor.CooldownCycles += (int)(Math.Round(5 / Executor.Speed));
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
                        Game1.SFX.Add(Game1.ConjureSpark);
                        Spark.Room = Executor.Room;
                    }
                    else
                    {
                        Executor.Block.Objects.Add(Spark);
                        Game1.SFX.Add(Game1.ConjureSpark);
                        Spark.Block = Executor.Block;
                    }

                    if (Executor.Sparks.Count() > Executor.PathOfLightLevel)
                    {
                        if (Executor.Sparks[0].Room != null)
                        {
                            Executor.Sparks[0].Room.Objects.Remove(Executor.Sparks[0]);
                            Executor.Sparks.RemoveAt(0);
                        }
                        else if (Executor.Sparks[0].Block != null)
                        {
                            Executor.Sparks[0].Block.Objects.Remove(Executor.Sparks[0]);
                            Executor.Sparks.RemoveAt(0);
                        }
                        else
                        {
                            foreach(Architect a in Game1.LoadedArchitects)
                            {
                                if(a.MainHeldObject == Executor.Sparks[0])
                                {
                                    a.MainHeldObject = null;
                                }
                                else if (a.OffHeldObject == Executor.Sparks[0])
                                {
                                    a.OffHeldObject = null;
                                }
                                else if (a.Inventory.Contains(Executor.Sparks[0]))
                                {
                                    a.Inventory.Remove(Executor.Sparks[0]);

                                }
                                else if (a.ShadowStorage.Contains(Executor.Sparks[0]))
                                {
                                    a.ShadowStorage.Remove(Executor.Sparks[0]);
                                }
                            }
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
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed));
                Executor.Energy -= 10 - Executor.PathOfStarsLevel;
                Object FoundSpark = null;
                Executor.RuinInvisibility();

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
                        Game1.SFX.Add(Game1.EvokeSpark);

                        if (Subjects[0] is Architect architectTarget && architectsInArea.Contains(architectTarget))
                        {
                            Object BP = architectTarget.BodyParts[Game1.GameWorld.rnd.Next(architectTarget.BodyParts.Count())];
                            BP.Integrity -= Game1.GameWorld.rnd.Next(10, Executor.PathOfStarsLevel * 5);
                            architectTarget.Bleeding += Game1.GameWorld.rnd.Next(5 + Executor.PathOfStarsLevel);
                            architectTarget.Pain += Game1.GameWorld.rnd.Next(5 + Executor.PathOfStarsLevel);
                            architectTarget.ChangeOpinion(Executor, -100);
                            MakeObservation("A heavenly beam pierces through " + BP.ReferredToNames[0] + ", leaving a burning space!", Color.Magenta, new EntityList<Entity>() { BP });
                            Executor.TryComment("uselight", 30);
                        }
                        else if (Subjects[0] is Object objectTarget && objectsInArea.Contains(objectTarget))
                        {
                            objectTarget.Integrity -= Game1.GameWorld.rnd.Next(10, Executor.PathOfStarsLevel * 5);
                            MakeObservation("A heavenly beam pierces through " + objectTarget.ReferredToNames[0] + ", leaving a burning space!", Color.Magenta, new EntityList<Entity>() { objectTarget });
                            Executor.TryComment("uselight", 30);
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
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed));
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

                        MakeObservation("You evoke your spark...", Color.White, new EntityList<Entity>());
                        Game1.SFX.Add(Game1.EvokeSpark);

                        foreach (Architect architect in architectsInArea)
                        {
                            if (architect.TargetArchitect != null && GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(architect.TargetArchitect) && (architect.Task == "killtarget" || architect.Task == "disabletarget"))
                            {
                                architect.BlindCycles += 100;
                                architect.ChangeOpinion(Executor, -60);
                                MakeObservation(architect.ReferredToNames[0] + " is blinded by the radiance!", Color.Magenta, new EntityList<Entity>() { architect });
                                foundArchitects = true;
                            }
                        }

                        if (!foundArchitects)
                        {
                            MakeObservation("No one is blinded...", Color.Yellow, new EntityList<Entity>());
                        }
                        else
                        {
                            Executor.TryComment("uselight", 30);
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
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed));
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

                        Executor.TryComment("uselight", 30);

                        MakeObservation("You evoke your spark...", Color.White, new EntityList<Entity>());
                        Game1.SFX.Add(Game1.EvokeSpark);
                        foreach (Architect architect in architectsInArea)
                        {
                            if (GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(architect))
                            {
                                if (architect.CombatCycles == 0)
                                {
                                    architect.Energy += 10 + Executor.PathOfLightLevel*2;
                                    MakeObservation(architect.Name + " is enveloped in brilliance and healed some energy!", Color.Magenta, new EntityList<Entity>() { architect });
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
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed));
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
                        Architect a = new Architect("", Game1.Sexes[Game1.GameWorld.rnd.Next(Game1.Sexes.Count())], Game1.GameWorld.GetRace("photonexus"), 0, "prismancer", new EntityList<Object>(), Executor.Location, Executor.District, Executor.Block, "", 1, true);
                        a.Name = Game1.GameWorld.GenerateUniqueArchitectName(a);
                        GameWorld.GamePlayerAssociation.ActiveParty.Architects.Add(a);
                        Game1.LoadedArchitects.Add(a);

                        a.Transient = true;
                        a.PopulateSelf(false);

                        MakeObservation("You evoke your spark...", Color.White, new EntityList<Entity>());
                        Game1.SFX.Add(Game1.EvokeSpark);
                        MakeObservation("A photonexus appears!", Color.Cyan, new EntityList<Entity>());

                        Executor.TryComment("uselight", 30);

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
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed));
                Executor.Energy -= 15 - Executor.PathOfHeatLevel;

                if (Executor.PathOfHeatLevel >= 8)
                {
                    Executor.FireSeconds += 500;
                    MakeObservation("Your flame burns brighter!", Color.Red, new EntityList<Entity>());
                    Executor.TryComment("useheat", 30);
                }
                else
                {
                    MakeObservation("You don't have control over that.", Color.Red, new EntityList<Entity>());
                }
            }
            else if (CommandID == "unflame")
            {
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed));
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
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed));
                Executor.Energy -= Math.Max(0, 25 - Executor.PathOfLifeLevel);

                if (Subjects[0] is Architect && ArchitectsToUse.Contains(Subjects[0]))
                {
                    if (!GameWorld.HumanoidRaces.Contains(((Architect)Subjects[0]).Race) && !GameWorld.ExtraRaces.Contains(((Architect)Subjects[0]).Race) && GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Subjects[0]))
                    {
                        if (Executor.PathOfLifeLevel >= 8)
                        {
                            MakeObservation("You gesture.", Color.Magenta, new EntityList<Entity>());

                            if (((Architect)Subjects[0]).HasBeenAugmented == true)
                            {
                                MakeObservation(Subjects[0].ReferredToNames[0] + " already has an augmentation.", Color.Magenta, new EntityList<Entity>() { Subjects[0] });
                            }
                            else
                            {
                                int Shibe = Game1.GameWorld.rnd.Next(3);

                                if (Shibe == 0)
                                {
                                    MakeObservation(Subjects[0].ReferredToNames[0] + " is enveloped in a golden light, becoming stronger!", Color.Magenta, new EntityList<Entity>() { Subjects[0] });
                                    ((Architect)Subjects[0]).Strength += 2;
                                    Game1.SFX.Add(Game1.Augment);
                                }
                                else if (Shibe == 1)
                                {
                                    MakeObservation(Subjects[0].ReferredToNames[0] + " is enveloped in a white light, becoming more agile!", Color.Magenta, new EntityList<Entity>() { Subjects[0] });
                                    ((Architect)Subjects[0]).Agility += 2;
                                    Game1.SFX.Add(Game1.Augment);
                                }
                                else if (Shibe == 2)
                                {
                                    MakeObservation(Subjects[0].ReferredToNames[0] + " is enveloped in a red light, becoming more durable!", Color.Magenta, new EntityList<Entity>() { Subjects[0] });
                                    ((Architect)Subjects[0]).MaxEnergyMod += 30;
                                    ((Architect)Subjects[0]).Energy = ((Architect)Subjects[0]).MaxEnergy;
                                    Game1.SFX.Add(Game1.Augment);
                                }

                                Executor.TryComment("uselife", 50);

                                ((Architect)Subjects[0]).HasBeenAugmented = true;
                            }
                        }
                        else
                        {
                            MakeObservation("You aren't powerful enough to do that.", Color.Yellow, new EntityList<Entity>());
                        }
                    }
                    else
                    {
                        MakeObservation("How generous, but you can't augment humanoids or creatures you don't control.", Color.Yellow, new EntityList<Entity>());
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
                    Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed));
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
                        Executor.TryComment("usedeath", 40);
                    }
                    else
                    {
                        MakeObservation("...but nothing happens.", Color.Purple, new EntityList<Entity>());
                    }
                }
            }
            else if (CommandID == "dismember_corpse")
            {
                if (Subjects[0] is Architect architect && architect.IsAlive == false && ArchitectsToUse.Contains(Subjects[0]))
                {
                    MakeObservation("You dismember the corpse.", Color.DarkMagenta, new EntityList<Entity>());

                    var addToList = Executor.Room != null ? (ICollection<Object>)Executor.Room.Objects : Executor.Block.Objects;

                    // Drop body parts
                    foreach (Object o in architect.BodyParts)
                        addToList.Add(o);
                    architect.BodyParts.Clear();

                    // Drop clothing
                    foreach (Object o in architect.Clothing)
                        addToList.Add(o);
                    architect.Clothing.Clear();

                    // Drop inventory
                    foreach (Object o in architect.Inventory)
                        addToList.Add(o);
                    architect.Inventory.Clear();

                    // Drop held items
                    if (architect.MainHeldObject != null)
                    {
                        addToList.Add(architect.MainHeldObject);
                        architect.MainHeldObject = null;
                    }
                    if (architect.OffHeldObject != null)
                    {
                        addToList.Add(architect.OffHeldObject);
                        architect.OffHeldObject = null;
                    }

                    // Remove architect
                    if (Executor.Room != null)
                        Executor.Room.Architects.Remove(architect);
                    else
                        Executor.Block.Architects.Remove(architect);

                    Game1.LoadedArchitectsToRemove.Add(architect);
                }
                else
                {
                    MakeObservation("You must target a nearby dead architect.", Color.DarkMagenta, new EntityList<Entity>());
                }
            }

            else if (CommandID == "fire_spectral_bolt")
            {
                Executor.CooldownCycles += (int)(Math.Round(15 / Executor.Speed));
                Executor.Energy -= Math.Max(0, 15 - Executor.PathOfDeathLevel);
                Executor.RuinInvisibility();


                if (Executor.PathOfDeathLevel >= 4 || (Executor.UndeadCreator != null && Executor.UndeadCreator.PathOfDeathLevel >= 8))
                {
                    if (Subjects[0] is Architect)
                    {
                        Object o = new Object(null, "bolt", new EntityList<Material>() { GameWorld.Spectre }, false, false, null, Executor, 0, false, Executor.Block, Executor.Structure, Executor.Room, false);
                        o.AirborneTarget = Subjects[0];
                        o.AirborneCyclesToHitTarget = 8;
                        o.Owner = Executor;
                        o.Thrower = Executor;

                        if (Executor.Room != null)
                        {
                            Executor.Room.Objects.Add(o);
                        }
                        else
                        {
                            Executor.Block.Objects.Add(o);
                        }

                        Game1.SFX.Add(Game1.SpectralBolt);
                        MakeObservation("You fire a spectral bolt at " + Subjects[0].ReferredToNames[0] + ".", Color.Cyan, new EntityList<Entity>() { Subjects[0] });

                        if (Executor.PathOfDeathLevel >= 4)
                        {
                            Executor.TryComment("usedeath", 10);
                        }
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
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed));
                Executor.Energy -= Math.Max(0, 10 - Executor.PathOfRealityLevel);

                if (Executor.PathOfRealityLevel >= 2)
                {
                    if (Subjects[0] is Object)
                    {
                        if (!((Object)Subjects[0]).RealityAugmented)
                        {
                            Executor.TryComment("usereality", 75);
                            MakeObservation(Subjects[0].ReferredToNames[0] + " increases in weight!", Color.Green, new EntityList<Entity>() { Subjects[0] });
                            ((Object)Subjects[0]).Weight *= 2; // Adjust the weight increase as necessary
                            ((Object)Subjects[0]).RealityAugmented = true;
                            Game1.SFX.Add(Game1.Immortalize);
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
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed));
                Executor.Energy -= Math.Max(0, 10 - Executor.PathOfRealityLevel);

                if (Executor.PathOfRealityLevel >= 2)
                {
                    if (Subjects[0] is Object)
                    {
                        if (!((Object)Subjects[0]).RealityAugmented)
                        {
                            Executor.TryComment("usereality", 75);
                            MakeObservation(Subjects[0].ReferredToNames[0] + " heats up!", Color.Green, new EntityList<Entity>() { Subjects[0] });
                            ((Object)Subjects[0]).HeatInCelsius += 50; // Adjust the temperature increase as needed
                            ((Object)Subjects[0]).RealityAugmented = true;
                            Game1.SFX.Add(Game1.Immortalize);
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
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed));
                Executor.Energy -= Math.Max(0, 10 - Executor.PathOfRealityLevel);

                if (Executor.PathOfRealityLevel >= 2)
                {
                    if (Subjects[0] is Object)
                    {
                        if (!((Object)Subjects[0]).RealityAugmented)
                        {
                            Executor.TryComment("usereality", 75);
                            MakeObservation(Subjects[0].ReferredToNames[0] + " becomes more aerodynamic!", Color.Green, new EntityList<Entity>() { Subjects[0] });
                            ((Object)Subjects[0]).ProjectileAerodynamic = true;
                            ((Object)Subjects[0]).RealityAugmented = true;
                            Game1.SFX.Add(Game1.Immortalize);
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
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed));
                Executor.Energy -= Math.Max(0, 10 - Executor.PathOfRealityLevel);

                if (Executor.PathOfRealityLevel >= 2)
                {
                    if (Subjects[0] is Object)
                    {
                        if (!((Object)Subjects[0]).RealityAugmented)
                        {
                            Executor.TryComment("usereality", 75);
                            // Increase integrity but ensure it does not exceed 100
                            ((Object)Subjects[0]).Integrity = ((Object)Subjects[0]).Integrity + 20; // Assuming each use increases integrity
                            ((Object)Subjects[0]).RealityAugmented = true;
                            Game1.SFX.Add(Game1.Immortalize);

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
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed));
                Executor.Energy -= Math.Max(0, 10 - Executor.PathOfRealityLevel);

                if (Executor.PathOfRealityLevel >= 2)
                {
                    if (Subjects[0] is Object)
                    {
                        if (!((Object)Subjects[0]).RealityAugmented)
                        {
                            Executor.TryComment("usereality", 75);
                            ((Object)Subjects[0]).Weight /= 2; // Halve the weight
                            ((Object)Subjects[0]).RealityAugmented = true;
                            MakeObservation(Subjects[0].ReferredToNames[0] + " decreases in weight!", Color.Green, new EntityList<Entity>() { Subjects[0] });
                            Game1.SFX.Add(Game1.Immortalize);
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
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed));
                Executor.Energy -= Math.Max(0, 10 - Executor.PathOfRealityLevel);

                if (Executor.PathOfRealityLevel >= 2)
                {
                    if (Subjects[0] is Object)
                    {
                        if (!((Object)Subjects[0]).RealityAugmented)
                        {
                            Executor.TryComment("usereality", 75);
                            ((Object)Subjects[0]).HeatInCelsius -= 50; // Decrease the temperature by a balanced amount
                            ((Object)Subjects[0]).RealityAugmented = true;
                            MakeObservation(Subjects[0].ReferredToNames[0] + " cools down!", Color.Green, new EntityList<Entity>() { Subjects[0] });
                            Game1.SFX.Add(Game1.Immortalize);
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
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed));
                Executor.Energy -= Math.Max(0, 10 - Executor.PathOfRealityLevel);

                if (Executor.PathOfRealityLevel >= 2)
                {
                    if (Subjects[0] is Object)
                    {
                        if (!((Object)Subjects[0]).RealityAugmented)
                        {
                            Executor.TryComment("usereality", 75);
                            ((Object)Subjects[0]).ProjectileAerodynamic = false; // Reverse the aerodynamic property
                            ((Object)Subjects[0]).RealityAugmented = true;
                            MakeObservation(Subjects[0].ReferredToNames[0] + " becomes less aerodynamic!", Color.Green, new EntityList<Entity>() { Subjects[0] });
                            Game1.SFX.Add(Game1.Immortalize);
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
                Executor.CooldownCycles += (int)(Math.Round(10 / Executor.Speed));
                Executor.Energy -= Math.Max(0, 10 - Executor.PathOfRealityLevel);

                if (Executor.PathOfRealityLevel >= 2)
                {
                    if (Subjects[0] is Object)
                    {
                        if (!((Object)Subjects[0]).RealityAugmented)
                        {
                            Executor.TryComment("usereality", 75);
                            ((Object)Subjects[0]).Integrity = Math.Max(0, ((Object)Subjects[0]).Integrity - 20); // Decrease integrity, ensuring it doesn't go below 0
                            ((Object)Subjects[0]).RealityAugmented = true;
                            MakeObservation(Subjects[0].ReferredToNames[0] + " becomes less structurally sound!", Color.Green, new EntityList<Entity>() { Subjects[0] });
                            Game1.SFX.Add(Game1.Immortalize);
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
                Executor.CooldownCycles += (int)(Math.Round(20 / Executor.Speed));
                Executor.Energy -= 15 - Executor.PathOfRealityLevel;

                if (Executor.PathOfRealityLevel >= 4)
                {
                    if (Subjects[0] is Object || Subjects[0] is Structure)
                    {
                        Executor.TryComment("usereality", 80);

                        MakeObservation(Subjects[0].ReferredToNames[0] + " liquifies, and slowly seeps into the ground...", Color.Green, new EntityList<Entity>() { Subjects[0] });
                        Game1.SFX.Add(Game1.WaterBolt);

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

                            ((Structure)Subjects[0]).MarketDebtToUs -= 100000;
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
                                    a.MainHeldObject = null;
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
                Executor.CooldownCycles += (int)(Math.Round(30 / Executor.Speed));
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
                                FireSeconds = currentObject.FireSeconds,
                                WetCycles = currentObject.WetCycles,
                                DestabilizedCycles = currentObject.DestabilizedCycles,
                                FractalCycles = currentObject.FractalCycles,
                                RematerializeLocation = currentObject.RematerializeLocation,
                                PlantCycles = currentObject.PlantCycles,
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

                            Game1.SFX.Add(Game1.Shatter);

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
                Executor.CooldownCycles += (int)(Math.Round(40 / Executor.Speed));
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

                    if (Subjects[0] is Architect AA && AA.Race.Name == "debtshiba")
                    {
                        MakeObservation("...but one does not simply \"blip\" a debtshiba.", Color.Red, new EntityList<Entity>());
                    }
                    else
                    {

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
                                Game1.SFX.Add(Game1.Expel);
                                break;
                        }
                    }
                }
                else
                {
                    MakeObservation("You don't know how to do that.", Color.Red, new EntityList<Entity>());
                }
            }
            else if (CommandID == "toggle_console")
            {
                if ((Executor.Room != null ? Executor.Room.Objects : Executor.Block.Objects).Contains(Subjects[0]))
                {
                    if (Subjects[0] is Object o && o.Type == "console")
                    {
                        if (o.ConsoleOn) // It is currently ON, so TURN OFF
                        {
                            if (o.StoredInvocation != null)
                            {
                                MakeObservation("You lose the gift of " + o.StoredInvocation + ".", Color.LightBlue, new EntityList<Entity>());
                                Executor.Invocations.Remove(o.StoredInvocation);
                            }
                            else
                            {
                                MakeObservation("You power off the device.", Color.Gray, new EntityList<Entity>());
                            }

                            o.LastToggler = Executor;
                            o.ConsoleOn = false;
                        }
                        else // It is currently OFF, so TURN ON
                        {
                            if (o.StoredInvocation != null)
                            {
                                MakeObservation("You gain the gift of " + o.StoredInvocation + ".", Color.LightBlue, new EntityList<Entity>());
                                Executor.Invocations.Add(o.StoredInvocation);
                            }
                            else
                            {
                                MakeObservation("You power on the device.", Color.Green, new EntityList<Entity>());
                            }
                            o.LastToggler = Executor;
                            o.ConsoleOn = true;
                        }
                    }
                    else
                    {
                        MakeObservation("That is not a console.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("That object is not nearby.", Color.Yellow, new EntityList<Entity>());
                }
            }

            else if (CommandID == "invoke_crystal")
            {
                if (Subjects[0] is Object o && o.Type == "invocation crystal")
                {
                    bool Has = false;
                    // If the object is in the inventory, remove it
                    if (Executor.Inventory.Contains(o))
                    {
                        Executor.Inventory.Remove(o);
                        Has = true;
                    }

                    // If it's in either hand, set that hand's object to null
                    if (Executor.MainHeldObject == o)
                    {
                        Executor.MainHeldObject = null;
                        Has = true;
                    }
                    else if (Executor.OffHeldObject == o)
                    {
                        Executor.OffHeldObject = null;
                        Has = true;
                    }


                    if (Has)
                    {
                        MakeObservation("You invoke the crystal...", Color.OrangeRed, new EntityList<Entity>());

                        var gifts = new List<string>
                        {
                            "wind", "alacrity", "shadows", "lightning", "mindreaving",
                            "telepathy", "siphoning", "detection", "slashing", "transformation",
                            "blight", "abjuration", "swiftness"
                        };

                        // Filter out invocations the player already has
                        var availableGifts = gifts.Except(Executor.Invocations).ToList();

                        if (availableGifts.Count > 0)
                        {
                            string Gift = availableGifts[Game1.GameWorld.rnd.Next(availableGifts.Count)];

                            MakeObservation($"...and acquire the gift of {Game1.Capitalize(Gift)}.", Color.OrangeRed, new EntityList<Entity>());
                            MakeObservation(Game1.GiftDescriptions[Gift], Color.OrangeRed, new EntityList<Entity>());

                            Executor.Invocations.Add(Gift);
                        }
                        else
                        {
                            MakeObservation("...but you already have every invocation.", Color.OrangeRed, new EntityList<Entity>());
                        }
                    }
                    else
                    {
                        MakeObservation("You need to have that object on you.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("You cannot invoke that.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "toggle_flight")
            {
                if (Executor.Invocations.Contains("wind"))
                {
                    Executor.InFlight = !Executor.InFlight; // Toggle the state

                    if (Executor.InFlight)
                    {
                        Executor.CooldownCycles += (int)Math.Round(10 / Executor.Speed);

                        MakeObservation("You take flight. Your Y level is " + Executor.YLevelInFeet + " feet.", Color.Pink, new EntityList<Entity>());
                    }
                    else
                    {
                        Executor.CooldownCycles += (int)Math.Round(10 / Executor.Speed);

                        MakeObservation("You are no longer flying. Your Y level is " + Executor.YLevelInFeet + " feet.", Color.Yellow, new EntityList<Entity>());
                    }
                }

                else
                {
                    MakeObservation("You do not have that power.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "curse")
            {
                if (Executor.Invocations.Contains("blight"))
                {
                    MakeObservation("You shout a word...", Color.Gray, new EntityList<Entity>() { });

                    if (Subjects[0] is Architect a && ArchitectsToUse.Contains(a) && a.RecievedCurse == false)
                    {
                        int Shibe = Game1.GameWorld.rnd.Next(3);

                        if (Shibe == 0)
                        {
                            MakeObservation(a.ReferredToNames[0] + " feels horribly weak!", Color.Gray, new EntityList<Entity>() { a });
                            a.Strength -= 1;
                        }
                        else if (Shibe == 1)
                        {
                            MakeObservation(a.ReferredToNames[0] + " feels horribly frail!", Color.Gray, new EntityList<Entity>() { a });
                            a.Endurance -= 1;
                        }
                        else if (Shibe == 2)
                        {
                            MakeObservation(a.ReferredToNames[0] + " feels horribly slow!", Color.Gray, new EntityList<Entity>() { a });
                            a.Agility -= 1;
                        }

                        a.RecievedCurse = true;
                    }
                    else
                    {
                        MakeObservation("You need to curse an architect who has not been cursed prior.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {

                    MakeObservation("You do not have that power.", Color.Yellow, new EntityList<Entity>());

                }
            }
            else if (CommandID == "degravitate")
            {
                if(Executor.Invocations.Contains("swiftness"))
                {
                    if (Subjects[0] is Architect a && ArchitectsToUse.Contains(a) && a.RecievedDexBonus == false)
                    {
                        a.RecievedDexBonus = true;
                        a.Dexterity += 1;
                        MakeObservation(a.ReferredToNames[0] + " feels peculiarly lighter...", Color.Green, new EntityList<Entity>() { a });
                    }
                    else
                    {
                        MakeObservation("You need to target a nearby architect who has not been degravitated.", Color.Yellow, new EntityList<Entity>());

                    }
                }
                else
                {

                    MakeObservation("You do not have that power.", Color.Yellow, new EntityList<Entity>());

                }
            }
            else if (CommandID == "peer")
            {
                if (Executor.BlindCycles > 0)
                {
                    MakeObservation("You are blind.", Color.Magenta, new EntityList<Entity>());
                }
                else
                {

                    if (Executor.Invocations.Contains("detection"))
                    {
                        EntityList<Architect> ArchitectsSeen = new EntityList<Architect>();
                        EntityList<Object> ObjectsSeen = new EntityList<Object>();


                        bool Works = true;

                        if (Subjects[0] is Door d && ObjectsToUse.Contains(d))
                        {
                            ArchitectsSeen.AddRange(d.DestinationRoom.Architects);
                            ObjectsSeen.AddRange(d.DestinationRoom.Objects);
                        }
                        else if (Subjects[0] is Object o && ObjectsToUse.Contains(o) && o.Type == "exit door" && Executor.Structure != null)
                        {
                            ArchitectsSeen.AddRange(Executor.Structure.Block.Architects);
                            ObjectsSeen.AddRange(Executor.Structure.Block.Objects);
                        }
                        else if (Subjects[0] is Structure s && Executor.Block.Structures.Contains(s) && Executor.Structure == null)
                        {
                            ArchitectsSeen.AddRange(s.Rooms[0].Architects);
                            ObjectsSeen.AddRange(s.Rooms[0].Objects);
                        }
                        else
                        {
                            Works = false;
                        }


                        if (Works)
                        {
                            List<string> Arch = new List<string>();
                            List<string> Obj = new List<string>();

                            MakeObservation("Architects Seen: " + (ArchitectsSeen.Count > 0 ? "" : "None"), Color.Orange, new EntityList<Entity>());
                            foreach (Architect a in ArchitectsSeen)
                            {
                                Arch.Add(a.ReferredToNames[0]);
                            }
                            MakeObservation(Game1.FormatAndList(Arch), Color.Yellow, new EntityList<Entity>());


                            MakeObservation("Unique Objects Seen: " + (ObjectsSeen.Count > 0 ? "" : "None"), Color.Orange, new EntityList<Entity>());
                            foreach (Object o in ObjectsSeen)
                            {
                                if (!Obj.Contains(o.ReferredToNames[0]))
                                {
                                    Obj.Add(o.ReferredToNames[0]);
                                }
                            }
                            MakeObservation(Game1.FormatAndList(Obj), Color.Yellow, new EntityList<Entity>());
                        }
                        else
                        {
                            MakeObservation("You cannot peer through that.", Color.Yellow, new EntityList<Entity>());
                        }
                    }
                    else
                    {
                        MakeObservation("You do not have that power.", Color.Yellow, new EntityList<Entity>());
                    }
                }
            }
            else if (CommandID == "flight_boost")
            {
                if (Executor.Invocations.Contains("wind"))
                {
                    if (Executor.InFlight)
                    {
                        MakeObservation("You boost airborne.", Color.Pink, new EntityList<Entity>());
                        Executor.YLevelInFeet += 50;
                        Executor.CooldownCycles += (int)Math.Round(25 / Executor.Speed);
                    }
                    else
                    {
                        MakeObservation("You need to be flying to do that.", Color.Yellow, new EntityList<Entity>());
                    }
                }

                else
                {
                    MakeObservation("You do not have that power.", Color.Yellow, new EntityList<Entity>());
                }
            }
            else if (CommandID == "mindreave")
            {
                if (Subjects[0] is Architect a && ArchitectsToUse.Contains(a))
                {
                    if (Executor.Invocations.Contains("mindreaving"))
                    {
                        if (Executor.NextMindreavingCycle <= Game1.GameWorld.Cycle)
                        {
                            MakeObservation("You tear a rift in the mind of " + a.ReferredToNames[0] + ".", Color.Yellow, new EntityList<Entity>());

                            a.Task = "";
                            a.TargetArchitect = null;
                            a.CyclesLeftInTask = 0;
                            a.DestabilizedCycles = 45;
                            a.UnconsciousCycles = 30;
              

                            Executor.NextMindreavingCycle = Game1.GameWorld.Cycle + 864000;
                        }
                        else
                        {
                            double remainingCycles = Executor.NextMindreavingCycle - Game1.GameWorld.Cycle;
                            double remainingSeconds = remainingCycles / 10; // Assuming 10 cycles per second

                            string timeMessage;
                            if (remainingSeconds >= 3600)
                            {
                                timeMessage = $"{Math.Round(remainingSeconds / 3600, 1)} hours";
                            }
                            else
                            {
                                timeMessage = $"{Math.Round(remainingSeconds / 60)} minutes";
                            }

                            MakeObservation($"You cannot mindreave for another {timeMessage}.", Color.Yellow, new EntityList<Entity>());
                        }
                    }
                    else
                    {
                        MakeObservation("You do not have that power.", Color.Yellow, new EntityList<Entity>());
                    }
                }
                else
                {
                    MakeObservation("That architect is not nearby.", Color.Yellow, new EntityList<Entity>());
                }
            }

            else if (CommandID == "use_skill")
            {
                if (Executor.SkillsKnown.Contains(Subjects[0]))
                {
                    if (!Executor.UsedSkills.Contains(Subjects[0]))
                    {
                        Executor.CooldownCycles += (int)Math.Round(2 / Executor.Speed);

                        Executor.TryComment("skilluse", 80);

                        if (Subjects[0].Metadata == "deflect")
                        {
                            EntityList<Object> Objects = Executor.Room != null ? Executor.Room.Objects : Executor.Block.Objects;

                            bool Success = false;

                            MakeObservation("You enter a trance...", Color.LightCyan, new EntityList<Entity>());
                            Game1.SFX.Add(Game1.TurnPage);


                            foreach (Object o in Objects)
                            {
                                if (o.AirborneTarget == Executor)
                                {
                                    Architect InitialThrower = o.Thrower;
                                    Architect InitialTarget = Executor;

                                    MakeObservation("You deflect the " + o.ReferredToNames[0] + " back to " + o.Thrower.Name + "!", Color.LightCyan, new EntityList<Entity>() { o, o.Thrower });
                                    Game1.SFX.Add(Game1.Parry);

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
                                Game1.SFX.Add(Game1.TurnPage);
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
                            Game1.SFX.Add(Game1.TurnPage);
                            Executor.UsedSkills.Add(Subjects[0]);
                            Executor.DoubleStrikeReady = true;
                        }
                        else if (Subjects[0].Metadata == "quick strike")
                        {
                            MakeObservation("You prepare to quick strike...", Color.LightCyan, new EntityList<Entity>());
                            Game1.SFX.Add(Game1.TurnPage);
                            Executor.UsedSkills.Add(Subjects[0]);
                            Executor.QuickStrikeReady = true;
                        }
                        else if (Subjects[0].Metadata == "severing strike")
                        {
                            MakeObservation("You prepare to sever your foe...", Color.LightCyan, new EntityList<Entity>());
                            Game1.SFX.Add(Game1.TurnPage);
                            Executor.UsedSkills.Add(Subjects[0]);
                            Executor.SeveringStrikeReady = true;
                        }
                        else if (Subjects[0].Metadata == "backflip")
                        {
                            MakeObservation("You backflip through the air!", Color.LightCyan, new EntityList<Entity>());
                            Game1.SFX.Add(Game1.Duck);
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
                                    var randomOffset = directionOffsets[Game1.GameWorld.rnd.Next(directionOffsets.Count())];
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
                                        Game1.SFX.Add(Game1.FS0);
                                        Game1.SFX.Add(Game1.FS1);
                                        Game1.SFX.Add(Game1.FS2);
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

                                    Object randomDoor = doors[Game1.GameWorld.rnd.Next(doors.Count())];
                                    if (randomDoor is Door door)
                                    {
                                        Executor.Room.Architects.Remove(Executor);
                                        Executor.Room = door.DestinationRoom;
                                        Executor.Room.Architects.Add(Executor);
                                        Success = true;
                                        Executor.UsedSkills.Add(Subjects[0]);
                                        MakeObservation("You dash through a door instantaneously.", Color.Green, new EntityList<Entity>());
                                        Game1.SFX.Add(Game1.FS0);
                                        Game1.SFX.Add(Game1.FS1);
                                        Game1.SFX.Add(Game1.FS2);
                                    }
                                }

                                if (!Success)
                                {
                                    // Use an exit door
                                    if (Executor.Structure != null && (Executor.Room == Executor.Structure.Rooms[0] || (Executor.Location.Layout == "archway" && Executor.Room == Executor.Structure.Rooms.Last())))
                                    {
                                        Executor.Room.Architects.Remove(Executor);
                                        Executor.Room = null;
                                        Executor.Block.Architects.Add(Executor);
                                        Executor.UsedSkills.Add(Subjects[0]);
                                        MakeObservation("You dash out the exit door instantaneously.", Color.Green, new EntityList<Entity>());
                                        Game1.SFX.Add(Game1.FS0);
                                        Game1.SFX.Add(Game1.FS1);
                                        Game1.SFX.Add(Game1.FS2);
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
                            Game1.SFX.Add(Game1.TurnPage);
                            Executor.UsedSkills.Add(Subjects[0]);
                            Executor.FinaleReady = true;
                        }
                        else if (Subjects[0].Metadata == "concentration")
                        {
                            MakeObservation("You concentrate, gaining more focus.", Color.Green, new EntityList<Entity>());
                            Game1.SFX.Add(Game1.TurnPage);
                            Executor.UsedSkills.Add(Subjects[0]);
                            Executor.ExtraFocusTicks = 300;
                        }
                        else if (Subjects[0].Metadata == "body slam")
                        {
                            MakeObservation("You prepare to body slam...", Color.Green, new EntityList<Entity>());
                            Game1.SFX.Add(Game1.TurnPage);
                            Executor.UsedSkills.Add(Subjects[0]);
                            Executor.BodySlamReady = true;
                        }
                        else if (Subjects[0].Metadata == "leg sweep")
                        {
                            MakeObservation("You prepare to leg sweep...", Color.Green, new EntityList<Entity>());
                            Game1.SFX.Add(Game1.TurnPage);
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

                MakeObservation(observationMessage, Color.Goldenrod, new EntityList<Entity>());

                return false;
            }


            List<Entity> recentlyMentionedEntities = new List<Entity>();
            int textStoragesChecked = 0;

            foreach (var announcement in Game1.Announcements.AsEnumerable().Reverse())
            {
                if (textStoragesChecked >= 15 || recentlyMentionedEntities.Count >= 15)
                {
                    break;
                }

                if (announcement.Entities != null)
                {
                    foreach (var mentionedEntity in announcement.Entities)
                    {
                        if (recentlyMentionedEntities.Count < 15)
                        {
                            recentlyMentionedEntities.Add(mentionedEntity);
                        }
                    }
                }

                textStoragesChecked++;
            }
            Game1.RecentlyMentionedEntities = recentlyMentionedEntities;


            //if we got to this point that means we exited the if statement by running a command successfully, therefore we can return "true"
            return (true);
        }

        public static Dictionary<string, string[]> personalityDenials = new Dictionary<string, string[]>
{
    { "generic", new[] { "^p and I wouldn't mesh well.", "^p and I just wouldn't get along.", "I don't see it working with ^p around." } },
    { "tactician", new[] { "^p and I would both try to lead.", "^p and I would end up second-guessing each other.", "^p and I would clash over strategy." } },
    { "arrogant", new[] { "^p already brings enough ego.", "^p and I are both too much to share space.", "^p and I wouldn't stop trying to outshine each other." } },
    { "sovereign", new[] { "^p and I would clash for control.", "^p and I both want the throne.", "^p and I can't both be in charge." } },
    { "soldier", new[] { "^p and I would argue about tactics.", "^p and I both try to take command.", "^p and I wouldn't follow the same orders." } },
    { "caretaker", new[] { "^p and I would both try to take care of everyone.", "^p and I would step on each other's feet.", "^p and I would get in each other's way trying to help." } },
    { "immature", new[] { "^p and I would probably get a little too carried away together.", "^p and I might push things too far just for fun.", "^p and I would keep things light, but maybe too light." } },
    { "delusional", new[] { "^p and I would both be lost in our own worlds.", "^p and I would blur the line between dream and reality.", "^p and I would completely lose track of what's real." } },
    { "laid-back", new[] { "^p and I would never get anything done.", "^p and I would relax ourselves into uselessness.", "^p and I would keep putting everything off." } },
    { "optimist", new[] { "^p and I would hype each other up endlessly.", "^p and I would both be the bright side, with no balance.", "^p and I might ignore the real problems." } },
    { "cynic", new[] { "^p and I would just drag each other down.", "^p and I would make everything sound worse.", "^p and I wouldn't help anyone's mood." } },
    { "cunning", new[] { "^p and I would constantly try to outplay each other.", "^p and I would be too busy scheming to cooperate.", "^p and I would never trust each other." } },
    { "hothead", new[] { "^p and I would argue nonstop.", "^p and I would start a fire before the enemy does.", "^p and I would explode over everything." } },
    { "survivalist", new[] { "^p and I would both try to go it alone.", "^p and I would hoard supplies and avoid each other.", "^p and I wouldn't rely on anyone-not even each other." } },
    { "analytic", new[] { "^p and I would get stuck overanalyzing everything.", "^p and I would debate every decision to death.", "^p and I would stall out thinking too hard." } },
    { "idealist", new[] { "^p and I would argue over how to fix the world.", "^p and I would chase different visions.", "^p and I would never agree on the goal." } },
    { "dreamer", new[] { "^p and I would get lost in fantasy.", "^p and I would drift away from what matters.", "^p and I would dream instead of doing." } },
    { "hedonist", new[] { "^p and I would party instead of adventuring.", "^p and I would chase thrills over progress.", "^p and I would forget the mission." } },
    { "competitor", new[] { "^p and I would try to outdo each other constantly.", "^p and I would compete instead of cooperate.", "^p and I would both demand the spotlight." } },
    { "melancholic", new[] { "^p and I would spiral into gloom.", "^p and I would bring each other down.", "^p and I would never lift each other up." } },
    { "collector", new[] { "^p and I would fight over loot.", "^p and I would both hoard too much.", "^p and I wouldn't want to share." } }
};


        public static string GetDirectionFromAngle(double angle)
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
        public static string GenerateHealthReport(Architect architect)
        {
            var healthReport = new StringBuilder();

            // Select a random body part from the architect
            var randomBodyPart = architect.BodyParts[Game1.GameWorld.rnd.Next(architect.BodyParts.Count())];

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





        public static string GenerateMadeUpHealthReport(Architect architect)
        {
            var healthReport = new StringBuilder();

            // Randomly select a body part from the architect
            var randomBodyPart = architect.BodyParts[Game1.GameWorld.rnd.Next(architect.BodyParts.Count())];

            // Generate a random integrity value with a bias towards higher numbers
            int biasedRandomInt(int min, int max)
            {
                return Game1.GameWorld.rnd.Next(min, max) * Game1.GameWorld.rnd.Next(min, max) / max;
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


        public static string GetDirectionFromDelta(double deltaX, double deltaZ)
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

        public static string GetRandomDirection()
        {
            string[] directions = { "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest" };
            return directions[Game1.GameWorld.rnd.Next(directions.Length)];
        }

        public static Location GetRandomLocation()
        {
            ;
            var randomLocation = Game1.GameWorld.AllLocations[Game1.GameWorld.rnd.Next(Game1.GameWorld.AllLocations.Count())];
            return randomLocation;
        }

        public static District GetRandomDistrict(Location location)
        {
            var randomDistrict = location.Districts[Game1.GameWorld.rnd.Next(location.Districts.Count())];
            return randomDistrict;
        }

        public static Structure GetRandomStructure(Location location)
        {
            var randomStructure = location.AllStructures[Game1.GameWorld.rnd.Next(location.AllStructures.Count())];
            return randomStructure;
        }

        public static void SendMessage(string MessageID, Architect Sender, Architect Receiver, EntityList<Entity> Subjects, World GameWorld)
        {
            Message DecidedMessage = null;

            if (!CanUnderstandEachOther(Receiver, Sender))
            {
                if(Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Sender))
                {
                    Sender.AnnounceToParty(Receiver.ReferredToNames[0] + " cannot understand you.", Color.Yellow, new EntityList<Entity>() { Receiver });
                }
            }
            else
            {
                if (Sender == Receiver)
                {
                    switch (Sender.SelfMessageTracker)
                    {
                        case 0:
                            Sender.AnnounceToParty("You're talking to yourself again.", Color.Red, new EntityList<Entity>() { Receiver });
                            break;
                        case 1:
                            Sender.AnnounceToParty("How is this helpful in any way?", Color.Red, new EntityList<Entity>() { Receiver });
                            break;
                        case 2:
                            Sender.AnnounceToParty("This isn't therapy.", Color.Red, new EntityList<Entity>() { Receiver });
                            break;
                        case 3:
                            Sender.AnnounceToParty("This is an echo chamber.", Color.Red, new EntityList<Entity>() { Receiver });
                            break;
                        case 4:
                            Sender.AnnounceToParty("Things that only YOU want to hear.", Color.Red, new EntityList<Entity>() { Receiver });
                            break;
                        case 5:
                            Sender.AnnounceToParty("Sights that only YOU want to see.", Color.Red, new EntityList<Entity>() { Receiver });
                            break;
                        case 6:
                            Sender.AnnounceToParty("Never even looking to the TRUTH:", Color.Red, new EntityList<Entity>() { Receiver });
                            break;
                        case 7:
                            Sender.AnnounceToParty("YOU NEED HELP.", Color.Red, new EntityList<Entity>() { Receiver });
                            break;
                        default:
                            return;
                    }

                    Sender.SelfMessageTracker = (Sender.SelfMessageTracker + 1) % 8;
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

                        DecidedMessage.ResponseEntitiesForOne.Add(Receiver);
                    }
                    else if (MessageID == "ask_directions")
                    {
                        if (Subjects[0] is Architect architect)
                        {
                            string mapUpdated = "";

                            if (Game1.LoadedArchitects.Contains(architect))
                            {
                                if (architect.Room != null && architect.Structure != null && architect.Structure.Block != null)
                                {
                                    // Architect is in a known room/structure
                                    double deltaX = architect.Structure.Block.X - Sender.Block.X;
                                    double deltaZ = architect.Structure.Block.Z - Sender.Block.Z;
                                    string direction = GetDirectionFromDelta(deltaX, deltaZ);

                                    DecidedMessage = new Message
                                    (
                                        Sender, Receiver, Subjects, "question", MessageID,
                                        $"Would you tell me where I can find {Subjects[0].ReferredToNames[0]}?", // content
                                        $"Yes, they're in {(architect.Structure.Type.Contains("house") ? "a house" : $"the structure {architect.Structure.Name}")} to the {direction}. {mapUpdated}{Game1.Capitalize(architect.Pronoun)} looks like... [Knowledge Updated]", // truthful/compliant
                                        $"Yes, they're in the structure {GetRandomStructure(Sender.Location).Name}.", // denial
                                        "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.", // unknowing
                                        "They aren't very nice, you don't want to see them.", // derailing
                                        "Why see them when you could instead talk with me?" // flattering
                                    );

                                    DecidedMessage.StoredKnownArchs.Add(architect);
                                    DecidedMessage.ResponseEntitiesForOne.Add(architect.Structure);
                                }
                                else if (architect.Block != null)
                                {
                                    // Architect is in a known block (fallback)
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
                                        $"Would you tell me where I can find {Subjects[0].ReferredToNames[0]}?", // content
                                        $"Yes, you need to travel {direction}.{mapUpdated} {architect.Pronoun} looks like... [Knowledge Updated]", // truthful/compliant
                                        $"Yes, you need to travel {GetRandomDirection()}.", // denial
                                        "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.", // unknowing
                                        "They aren't very nice, you don't want to see them.", // derailing
                                        "Why see them when you could instead talk with me?" // flattering
                                    );

                                    if (Sender.Location != architect.Block.District.Location)
                                    {
                                        DecidedMessage.StoredRevealLocations.Add(architect.Block.District.Location);
                                    }

                                    DecidedMessage.StoredKnownArchs.Add(architect);
                                }
                            }

                            else if (architect.District != null &&
                                     Sender.Location.Districts.Contains(architect.District))
                            {
                                // They're in a nearby district
                                string nearbyDistrictName = architect.District.Name;

                                DecidedMessage = new Message
                                (
                                    Sender, Receiver, Subjects, "question", MessageID,
                                    $"Would you tell me where I can find {Subjects[0].ReferredToNames[0]}?",
                                    $"They are in the nearby {nearbyDistrictName} district.",
                                    $"Yes, you need to travel {GetRandomDirection()}.",
                                    "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.",
                                    "They aren't very nice, you don't want to see them.",
                                    "Why see them when you could instead talk with me?"
                                );

                                DecidedMessage.ResponseEntitiesForOne.Add(architect.District);
                            }
                            else if (architect.Location != null)
                            {

                                Location l = GetRandomLocation();
                                if (Sender.Location != architect.Location)
                                {
                                    mapUpdated = " [Map Updated]";
                                }
                                DecidedMessage = new Message
                                (
                                    Sender, Receiver, Subjects, "question", MessageID,
                                    $"Would you tell me where I can find {Subjects[0].ReferredToNames[0]}?",
                                    $"Last I heard they were at {architect.Location.Name}.{mapUpdated}",
                                    $"I believe they're at {l.Name}.",
                                    "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.",
                                    "They aren't very nice, you don't want to see them.",
                                    "Why see them when you could instead talk with me?"
                                );
                                // Known but remote
                                if (Sender.Location != architect.Location)
                                {
                                    DecidedMessage.StoredRevealLocations.Add(architect.Location);
                                }

                                DecidedMessage.ResponseEntitiesForTwo.Add(l);
                            }
                            else
                            {
                                // Unknown
                                Location l = GetRandomLocation();

                                DecidedMessage = new Message
                                (
                                    Sender, Receiver, Subjects, "question", MessageID,
                                    $"Would you tell me where I can find {Subjects[0].ReferredToNames[0]}?",
                                    "Wherever they are, it is lost to time.",
                                    $"I believe they're at {l.Name}.",
                                    "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.",
                                    "They aren't very nice, you don't want to see them.",
                                    "Why see them when you could instead talk with me?"
                                );

                                DecidedMessage.ResponseEntitiesForTwo.Add(l);
                            }
                        }

                        else if (Subjects[0] is Object obj)
                        {
                            bool foundInLocation = false;

                            foreach (var district in Sender.Location.Districts)
                            {
                                foreach (var block in district.DistrictMap)
                                {
                                    // Check block-level objects
                                    if (block.Objects.Contains(obj) || block.Objects.Any(o => o.ContainedObjects.Contains(obj)))
                                    {
                                        foundInLocation = true;

                                        Structure s = GetRandomStructure(Sender.Location);

                                        DecidedMessage = new Message
                                        (
                                            Sender, Receiver, Subjects, "question", MessageID,
                                            "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?",
                                            $"I saw it outside in the {district.Name} district.",
                                            $"I saw it in {s.Name}.",
                                            "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.",
                                            "I don't care.",
                                            "You are already beautiful without it."
                                        );

                                        DecidedMessage.ResponseEntitiesForTwo.Add(s);
                                        DecidedMessage.ResponseEntitiesForOne.Add(district);

                                        break;
                                    }

                                    // Check inside structures and rooms
                                    foreach (var structure in block.Structures)
                                    {
                                        if (structure.Rooms.Any(room =>
                                            room.Objects.Contains(obj) ||
                                            room.Objects.Any(o => o.ContainedObjects.Contains(obj))))
                                        {
                                            foundInLocation = true;

                                            Structure s = GetRandomStructure(Sender.Location);

                                            DecidedMessage = new Message
                                            (
                                                Sender, Receiver, Subjects, "question", MessageID,
                                                "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?",
                                                $"I saw it in the structure {structure.Name}.",
                                                $"I saw it in the structure {s.Name}.",
                                                "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.",
                                                "I don't care.",
                                                "You are already beautiful without it."
                                            );
                                            DecidedMessage.ResponseEntitiesForOne.Add(structure);
                                            DecidedMessage.ResponseEntitiesForTwo.Add(s);

                                            break;
                                        }
                                    }



                                    if (foundInLocation) break;
                                }
                                if (foundInLocation) break;
                            }


                            //Check consoles

                            if (obj.Type == "console" && obj.ConsoleDropLocation != null)
                            {
                                foundInLocation = true;


                                DecidedMessage = new Message
                                (
                                    Sender, Receiver, Subjects, "question", MessageID,
                                    "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?",
                                    $"It fell from the sky at {obj.ConsoleDropLocation.Name}, in the {obj.ConsoleDropLocation.Districts[0].Name} district. You cannot miss it. [Map Updated]",
                                    $"It has not yet fallen from the sky.",
                                    "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.",
                                    "I don't care.",
                                    "You are already beautiful without it."
                                );


                                if (Sender.Location != obj.ConsoleDropLocation)
                                {
                                    // Store the location instead of revealing it directly
                                    DecidedMessage.StoredRevealLocations.Add(obj.ConsoleDropLocation);
                                }

                                DecidedMessage.ResponseEntitiesForOne.Add(obj.ConsoleDropLocation);
                                DecidedMessage.ResponseEntitiesForOne.Add(obj.ConsoleDropLocation.Districts[0]);
                            }



                            if (!foundInLocation)
                            {
                                foreach (var location in GameWorld.AllLocations)
                                {
                                    foreach (var district in location.Districts)
                                    {
                                        if (location.AllStructures.Any(s => s.HistoricalObjects.Contains(obj)))
                                        {
                                            var randomLocation = GameWorld.AllLocations[Game1.GameWorld.rnd.Next(GameWorld.AllLocations.Count())];
                                            var randomDistrict = randomLocation.Districts[Game1.GameWorld.rnd.Next(randomLocation.Districts.Count())];
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
                                                "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.", // unknowing/confused response
                                                "I don't care.", // derailing response
                                                "You are already beautiful without it." // flattering response
                                            );

                                            DecidedMessage.ResponseEntitiesForOne.Add(location);
                                            DecidedMessage.ResponseEntitiesForOne.Add(district);
                                            DecidedMessage.ResponseEntitiesForTwo.Add(randomLocation);
                                            DecidedMessage.ResponseEntitiesForTwo.Add(randomDistrict);

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


                            if (!foundInLocation && DecidedMessage == null)
                            {
                                foreach (var a in Game1.GameWorld.AllHistoricalArchitects)
                                {
                                    if (a.MainHeldObject == obj || a.OffHeldObject == obj ||
                                        a.Clothing.Contains(obj) || a.Inventory.Contains(obj))
                                    {
                                        string archLoc = a.Location != null ? a.Location.Name : "an unknown location";
                                        string archName = string.IsNullOrEmpty(a.Name) ? a.ReferredToNames[0] : a.Name;

                                        DecidedMessage = new Message
                                        (
                                            Sender, Receiver, Subjects, "question", MessageID,
                                            "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?",
                                            $"Yes, it is with {archName} in {archLoc}.",
                                            $"I believe someone in {archLoc} might have it.",
                                            "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.",
                                            "I don't care.",
                                            "You are already beautiful without it."
                                        );


                                        DecidedMessage.ResponseEntitiesForOne.Add(a);

                                        if (a.Location != null)
                                        {
                                            DecidedMessage.ResponseEntitiesForOne.Add(a.Location);
                                            DecidedMessage.ResponseEntitiesForTwo.Add(a.Location);
                                        }

                                        break;
                                    }
                                }
                            }


                        }
                        else if (Subjects[0] is Structure structure)
                        {
                            if (structure.Rooms.Contains(Sender.Room))
                            {
                                Structure s = GetRandomStructure(Sender.Location);

                                DecidedMessage = new Message
                                (
                                    Sender, Receiver, Subjects, "question", MessageID,
                                    "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?", // content
                                    "We are already here.", // truthful/compliant response
                                    $"You are currently inside {s.Name}.", // made up/denial response
                                    "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.", // unknowing/confused response
                                    "I personally can't stand that place.", // derailing response
                                    "It would be such a nice place to hang out..." // flattering response
                                );

                                DecidedMessage.ResponseEntitiesForTwo.Add(s);
                            }
                            else if (structure.Block == Sender.Block)
                            {
                                DecidedMessage = new Message
                                (
                                    Sender, Receiver, Subjects, "question", MessageID,
                                    "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?", // content
                                    "It is right nearby.", // truthful/compliant response
                                    $"It is to the {GetRandomDirection()}.", // made up/denial response
                                    "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.", // unknowing/confused response
                                    "I personally can't stand that place.", // derailing response
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
                                    "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.", // unknowing/confused response
                                    "I personally can't stand that place.", // derailing response
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

                                Location l = GetRandomLocation();
                                District d = l.Districts[0];

                                DecidedMessage = new Message
                                (
                                    Sender, Receiver, Subjects, "question", MessageID,
                                    "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?", // content
                                    $"It is in {structure.Block.District.Location.Name}, in the {structure.Block.District.Name} district.{mapUpdated}", // truthful/compliant response
                                    $"It is in {l.Name}, in the {d.Name} district.", // made up/denial response
                                    "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.", // unknowing/confused response
                                    "I personally can't stand that place.", // derailing response
                                    "It would be such a nice place to hang out..." // flattering response
                                );

                                DecidedMessage.ResponseEntitiesForOne.Add(structure.Block.District.Location);
                                DecidedMessage.ResponseEntitiesForOne.Add(structure.Block.District);

                                DecidedMessage.ResponseEntitiesForTwo.Add(l);
                                DecidedMessage.ResponseEntitiesForTwo.Add(d);

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
                                "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.", // unknowing/confused response
                                "Do you not like " + Receiver.Location.Name + "?", // derailing response
                                "You're going to leave me?" // flattering response
                            );

                            DecidedMessage.ResponseEntitiesForFour.Add(Receiver.Location);

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
                                "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.", // unknowing/confused response
                                "Do you not like " + Receiver.Location.HomeCivilization.Name + "?", // derailing response
                                "You're going to leave me?" // flattering response
                            );

                            DecidedMessage.ResponseEntitiesForFour.Add(Receiver.Location);

                        }
                        else if (Subjects[0] is Group group)
                        {
                            Architect leader = group.Leader;
                            string mapUpdated = "";

                            if (Game1.LoadedArchitects.Contains(leader) && leader.Block != null)
                            {
                                // Calculate direction based on the leader's block
                                double deltaX = leader.Block.X - Sender.Block.X;
                                double deltaZ = leader.Block.Z - Sender.Block.Z;
                                string direction = GetDirectionFromDelta(deltaX, deltaZ);

                                if (Sender.Location != leader.Block.District.Location)
                                {
                                    mapUpdated = " [Map Updated]";
                                }

                                DecidedMessage = new Message
                                (
                                    Sender, Receiver, Subjects, "question", MessageID,
                                    "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?",
                                    $"Yes, you need to travel {direction}.{mapUpdated}",
                                    $"Yes, you need to travel {GetRandomDirection()}.",
                                    "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.",
                                    "They aren't that important.",
                                    "We could be a group, you know..."
                                );

                                if (Sender.Location != leader.Block.District.Location)
                                {
                                    DecidedMessage.StoredRevealLocations.Add(leader.Block.District.Location);
                                }
                            }
                            else if (leader.Location != null)
                            {
                                Location locationName = leader.Location;

                                if (Sender.Location != leader.Location)
                                {
                                    mapUpdated = " [Map Updated]";
                                }

                                Location l = GetRandomLocation();

                                DecidedMessage = new Message
                                (
                                    Sender, Receiver, Subjects, "question", MessageID,
                                    "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?",
                                    $"Last I heard they were at {locationName.Name}. {mapUpdated}",
                                    $"They are at {l.Name}.",
                                    "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.",
                                    "They aren't that important.",
                                    "We could be a group, you know..."
                                );

                                if (Sender.Location != leader.Location)
                                {
                                    DecidedMessage.StoredRevealLocations.Add(leader.Location);
                                }

                                DecidedMessage.ResponseEntitiesForOne.Add(locationName);
                                DecidedMessage.ResponseEntitiesForTwo.Add(l);
                            }
                            else
                            {
                                Location l = GetRandomLocation();
                                DecidedMessage = new Message
                                (
                                    Sender, Receiver, Subjects, "question", MessageID,
                                    "Would you tell me where I can find " + Subjects[0].ReferredToNames[0] + "?",
                                    "Wherever they are, it is lost to time.",
                                    $"They are at {l.Name}.",
                                    "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.",
                                    "They aren't that important.",
                                    "We could be a group, you know..."
                                );

                                DecidedMessage.ResponseEntitiesForTwo.Add(l);
                            }
                        }
                        if (DecidedMessage == null)
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


                        if (DecidedMessage.IgnorantResponse == "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.")
                        {
                            string unknowingResponse;

                            var truthProfessions = new HashSet<string>
{
    "scholar", "mage", "engineer", "entertainer", "artificer", "bard", "sage", "luminary",
    "warlock", "sorcerer", "necromancer", "spatiomancer", "perceptomancer", "conjumancer",
    "fractalmancer", "archmage", "magician", "archbard", "archsage", "archluminary", "archartificer", "scribe"
};

                            var populatedLibraries = Game1.GameWorld.AllLocations
                                .SelectMany(l => l.AllStructures)
                                .Where(s => s.Type == "library")
                                .Where(s =>
                                {
                                    var district = s.Block?.District;
                                    if (district == null) return false;

                                    // Only check rooms if in same district as sender
                                    if (district == Sender.Block.District)
                                    {
                                        return s.Rooms.Any(r =>
                                            r.Architects.Any(a => a.IsAlive && truthProfessions.Contains(a.Profession)));
                                    }

                                    // Otherwise, assume it's potentially populated
                                    return true;
                                })
                                .OrderBy(s =>
                                {
                                    if (s.Block.District == Sender.Block.District)
                                        return 0;
                                    if (s.Block.District.Location == Sender.Block.District.Location)
                                        return 1;
                                    return 2;
                                })
                                .ToList();



                            if (populatedLibraries.Any())
                            {
                                var closestLibrary = populatedLibraries.First();

                                if (closestLibrary.Block.District == Sender.Block.District)
                                {
                                    // Calculate direction to the library
                                    double deltaX = closestLibrary.Block.X - Sender.Block.X;
                                    double deltaZ = -(closestLibrary.Block.Z - Sender.Block.Z); // Invert Z-axis like elsewhere
                                    double angle = Math.Atan2(deltaZ, deltaX) * (180 / Math.PI);
                                    if (angle < 0) angle += 360;
                                    string direction = GetDirectionFromAngle(angle);

                                    var helpfulArchitect = closestLibrary.Rooms
                                        .SelectMany(r => r.Architects)
                                        .FirstOrDefault(a => a.IsAlive && truthProfessions.Contains(a.Profession));

                                    if (helpfulArchitect != null)
                                    {
                                        unknowingResponse = $"Try finding {helpfulArchitect.Name} at the library {closestLibrary.Name} {direction} of here, {helpfulArchitect.Pronoun} might be able to help you. {Game1.Capitalize(helpfulArchitect.Pronoun)} looks like... [Knowledge Updated]";
                                        DecidedMessage.StoredKnownArchs.Add(helpfulArchitect);
                                        DecidedMessage.ResponseEntitiesForOne.Add(helpfulArchitect);
                                        DecidedMessage.ResponseEntitiesForOne.Add(closestLibrary);
                                    }
                                    else
                                    {
                                        // Fallback message, unlikely to be reached
                                        unknowingResponse = $"Try visiting the library {closestLibrary.Name} {direction} of here, someone there might help.";
                                        DecidedMessage.ResponseEntitiesForOne.Add(closestLibrary);
                                    }
                                }

                                else if (closestLibrary.Block.District.Location == Sender.Block.District.Location)
                                {
                                    unknowingResponse = $"Try finding the library {closestLibrary.Name} in the nearby district {closestLibrary.Block.District.Name}, there may be a scholar there who can help you.";
                                    DecidedMessage.ResponseEntitiesForOne.Add(closestLibrary.Block.District);
                                    DecidedMessage.ResponseEntitiesForOne.Add(closestLibrary);
                                }
                                else
                                {
                                    unknowingResponse = $"Try finding the library {closestLibrary.Name} at the {closestLibrary.Block.District.Name} district in {closestLibrary.Block.District.Location.Name}.";
                                    DecidedMessage.ResponseEntitiesForOne.Add(closestLibrary.Block.District.Location);
                                    DecidedMessage.ResponseEntitiesForOne.Add(closestLibrary.Block.District);
                                    DecidedMessage.ResponseEntitiesForOne.Add(closestLibrary);
                                }
                            }
                            else
                            {
                                unknowingResponse = "Try finding a scholar at a library. They dedicate their lives to cataloguing odd information.";
                            }


                            DecidedMessage.IgnorantResponse = unknowingResponse;
                        }

                    }


                    // Method to calculate direction from delta


                    else if (MessageID == "ask_generic_directions")
                    {
                        string thing = Subjects[0].ReferredToNames[0];
                        (Location nearestLocation, District nearestDistrict, Block nearestBlock, Room nearestRoom) = Sender.Block.FindNearestThing(thing);

                        if (nearestBlock != null)
                        {
                            //search up omega shiba inu
                            
                            if(thing == "stronghold" || thing == "monument" )
                            {
                                DecidedMessage = new Message
                                (
                                    Sender, Receiver, Subjects, "question", MessageID,
                                    $"Would you tell me where I can find a {thing}?", // content
                                    $"I'm not sure I know.", // truthful/compliant response
                                    $"I'm not sure I know.", // made up/denial response
                                    $"I'm not sure I know.", // unknowing/confused response
                                    $"I'm not sure I know.", // derailing response
                                    $"I'm not sure I know." // flattering response
                                );
                            }
                            else if (nearestDistrict == Sender.District)
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
                                    $"You don't look like you need a {thing}.", // derailing response
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
                                    $"You don't look like you need a {thing}.", // derailing response
                                    $"You don't need a {thing} as much as I need you." // flattering response
                                );

                                DecidedMessage.ResponseEntitiesForOne.Add(nearestDistrict);
                            }
                            else
                            {
                                // It's at a different location
                                // Calculate direction
                                double deltaX = nearestLocation.Region.X - Sender.Location.Region.X;
                                double deltaZ = (nearestLocation.Region.Z - Sender.Location.Region.Z); // Invert Z-axis
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

                                DecidedMessage.ResponseEntitiesForOne.Add(nearestLocation);
                                if (Sender.Location != nearestLocation)
                                {
                                    // Store the location instead of revealing it directly
                                    DecidedMessage.StoredRevealLocations.Add(nearestLocation);
                                }
                            }
                        }
                        else
                        {
                            Location l = GetRandomLocation();

                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "question", MessageID,
                                $"Would you tell me where I can find a {thing}?", // content
                                "That knowledge is lost to time.", // truthful/compliant response
                                $"I saw a {thing} at {l.Name} once.", // made up/denial response
                                "No, I don't know, unfortunately.", // unknowing/confused response
                                $"You don't look like you need a {thing}", // derailing response
                                $"You don't need a {thing} as much as I need you." // flattering response
                            );

                            DecidedMessage.ResponseEntitiesForTwo.Add(l);
                        }
                    }
                    else if (MessageID == "ask_about_something")
                    {
                        string entityType = Subjects[0].GetType().Name;
                        string truthfulResponse = "";
                        string madeUpResponse = "";

                        var randomArchitect = GameWorld.AllHistoricalArchitects.GetRandomItem();
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
                            string randomGenderName = randomArchitect.Age > 16 ? (randomArchitect.Sex == "male" ? "man" : "woman") : (randomArchitect.Sex == "male" ? "boy" : "girl");
                            string randomLocationName = randomArchitect.Location != null ? randomArchitect.Location.Name : "unknown location";
                            madeUpResponse = $"{randomArchitect.Name} is a {randomArchitect.Age}-year-old {randomGenderName} from {randomLocationName}, working as a {randomArchitect.Profession}.";
                        }
                        else
                        {
                            truthfulResponse = $"{Subjects[0].ReferredToNames[0]} is a {entityType}.";

                            // Generate a made-up response
                            string randomGenderName = randomArchitect.Age > 16 ? (randomArchitect.Sex == "male" ? "man" : "woman") : (randomArchitect.Sex == "male" ? "boy" : "girl");
                            string randomLocationName = randomArchitect.Location != null ? randomArchitect.Location.Name : "unknown location";
                            madeUpResponse = $"{randomArchitect.Name} is a {randomArchitect.Age}-year-old {randomGenderName} from {randomLocationName}, working as a {randomArchitect.Profession}.";
                        }


                        var lastEvent = GameWorld.HistoricalEvents.LastOrDefault(e =>
                               e.EventData.Contains(string.IsNullOrEmpty(Subjects[0].Name) ? Subjects[0].ReferredToNames[0] : Subjects[0].Name) && !e.EventData.Contains("arrived in"));

                        string historicalEvent = "";

                        if (lastEvent != null)
                        {
                            string eventData = lastEvent.EventData;

                            // Find the year inside the parentheses (Month/Year)
                            int startParen = eventData.IndexOf('(');
                            int slash = eventData.IndexOf('/');
                            int endParen = eventData.IndexOf(')');

                            if (startParen != -1 && slash != -1 && endParen != -1 && slash < endParen)
                            {
                                string year = eventData.Substring(slash + 1, endParen - slash - 1).Trim();
                                string mainEvent = eventData.Substring(endParen + 1).Trim();
                                historicalEvent = $"In {year}, {mainEvent}";
                            }
                        }

                        truthfulResponse += " " + historicalEvent;
                        madeUpResponse += " " + historicalEvent;
                        DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "question", MessageID,
                            $"Can you tell me about {Subjects[0].ReferredToNames[0]}?", //content
                            truthfulResponse, //truthful/compliant response
                            madeUpResponse, //made up/denial response
                            "I'm not sure, sorry.", //unknowing/confused response
                            "Why do you care so much?", //derailing response
                            $"Why talk about {Subjects[0].ReferredToNames[0]} when we could talk about you?" //flattering response
                        );

                        DecidedMessage.ResponseEntitiesForOne.Add(Subjects[0]);
                        DecidedMessage.ResponseEntitiesForTwo.Add(randomArchitect);
                    }
                    else if (MessageID == "ask_ruler")
                    {
                        if (Sender.Location.Government != null)
                        {
                            Entity truthfulGovernment = Sender.Location.Government;
                            var filteredLocations = GameWorld.AllLocations
                                .Where(location => location.Government != null)
                                .ToList();

                            var randomLocation = filteredLocations[Game1.GameWorld.rnd.Next(filteredLocations.Count)];

                            Entity madeUpGovernment = randomLocation.Government;

                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "question", MessageID,
                                "Who rules this place?", //content
                                $"{truthfulGovernment.Name} is our government.", //truthful/compliant response
                                $"{madeUpGovernment.Name} is our government.", //made up/denial response
                                "I'm not sure, sorry.", //unknowing/confused response
                                "Anarchy is truly superior.", //derailing response
                                $"I'm not sure... do you govern me?" //flattering response
                            );

                            DecidedMessage.ResponseEntitiesForOne.Add(truthfulGovernment);
                            DecidedMessage.ResponseEntitiesForTwo.Add(madeUpGovernment);
                        }
                        else
                        {
                            var randomLocation = GameWorld.AllLocations[Game1.GameWorld.rnd.Next(GameWorld.AllLocations.Count())];
                            Entity madeUpGovernment = randomLocation.Government;

                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "question", MessageID,
                                "Who rules this place?", //content
                                $"We are an anarcho-syndicalist commune at the moment.", //truthful/compliant response
                                $"{madeUpGovernment.Name} is our government.", //made up/denial response
                                "I'm not sure, sorry.", //unknowing/confused response
                                "Anarchy is truly superior.", //derailing response
                                $"I'm not sure... do you govern me?" //flattering response
                            );

                            DecidedMessage.ResponseEntitiesForTwo.Add(madeUpGovernment);
                        }
                    }
                    else if (MessageID == "ask_trade")
                    {
                        (Location nearestLocation, District nearestDistrict, Block nearestBlock, Room nearestRoom) = Sender.Block.FindNearestThing("market");

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
                                    "We don't \"do\" trade here.", //made up/denial response
                                    "What is trade?", //unknowing/confused response
                                    "I prefer communism.", //derailing response
                                    "You look valuable." //flattering response
                                );
                            }
                            else
                            {
                                DecidedMessage = new Message
                                (
                                    Sender, Receiver, Subjects, "question", MessageID,
                                    "I'd like to trade.", //content
                                    $"We have a market dedicated to trade in {nearestDistrict.Name}, a nearby district.", //truthful/compliant response
                                    "We don't \"do\" trade here.", //made up/denial response
                                    "What is trade?", //unknowing/confused response
                                    "I prefer communism.", //derailing response
                                    "You look valuable." //flattering response
                                );

                                DecidedMessage.ResponseEntitiesForTwo.Add(nearestDistrict);
                            }
                        }
                        else
                        {
                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "question", MessageID,
                                "I'd like to trade.", //content
                                "We don't have a dedicated market set up yet.", //truthful/compliant response
                                "We don't \"do\" trade here.", //made up/denial response
                                "What is trade?", //unknowing/confused response
                                "I prefer communism.", //derailing response
                                "You look valuable." //flattering response
                            );
                        }
                    }
                    else if (MessageID == "ask_them_join")
                    {
                        // Collect all personalities from the Active Party (flattening all lists)
                        List<string> existingPersonalities = Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects
                            .SelectMany(a => a.Personalities)
                            .ToList();

                        // Get the requested person's personalities (Receiver in this case)
                        List<string> newPersonalities = Receiver.Personalities;

                        // Find conflicting personalities
                        var conflictingPersonalities = existingPersonalities.Intersect(newPersonalities).ToList();

                        string denialResponse;

                        if (conflictingPersonalities.Any()) // Only use personality denial if it's the main issue
                        {
                            string chosenPersonality = conflictingPersonalities[Game1.GameWorld.rnd.Next(conflictingPersonalities.Count)];
                            string template = personalityDenials[chosenPersonality][Game1.GameWorld.rnd.Next(3)];

                            // Find who is the conflicting party member
                            var conflictingMember = Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects
                                .FirstOrDefault(a => a.Personalities.Contains(chosenPersonality));

                            string replacementName = (conflictingMember != null && conflictingMember != Sender)
                                ? conflictingMember.Name // Refer to the actual conflicting member
                                : "you"; // Default to "you" if it's the asker

                            denialResponse = template.Replace("^p", replacementName);
                        }
                        else
                        {
                            denialResponse = "Sorry, I belong here.";
                        }

                        DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "request", MessageID,
                            "Join me on my quest!", // Content
                            "I would be honored. I accept.", // Truthful/Compliant Response
                            denialResponse, // Updated Denial Response
                            "What? Where?", // Unknowing/Confused Response
                            "If one could even call it a group...", // Derailing Response
                            "If it means I spend more time with you." // Flattering Response
                        );
                    }
                    else if (MessageID == "ask_to_join")
                    {
                        // Collect all personalities from the Active Party (flattening all lists)
                        List<string> existingPersonalities = Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects
                            .SelectMany(a => a.Personalities)
                            .ToList();

                        // Get the requested person's personalities (Sender in this case)
                        List<string> newPersonalities = Sender.Personalities;

                        // Find conflicting personalities
                        var conflictingPersonalities = existingPersonalities.Intersect(newPersonalities).ToList();

                        string denialResponse;

                        if (conflictingPersonalities.Any()) // Only use personality denial if it's the main issue
                        {
                            string chosenPersonality = conflictingPersonalities[Game1.GameWorld.rnd.Next(conflictingPersonalities.Count)];
                            string template = personalityDenials[chosenPersonality][Game1.GameWorld.rnd.Next(3)];

                            // Find who is the conflicting party member
                            var conflictingMember = Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects
                                .FirstOrDefault(a => a.Personalities.Contains(chosenPersonality));

                            string replacementName = (conflictingMember != null && conflictingMember != Sender)
                                ? conflictingMember.Name // Refer to the actual conflicting member
                                : "you"; // Default to "you" if it's the asker

                            denialResponse = template.Replace("^p", replacementName);
                        }
                        else
                        {
                            denialResponse = "Sorry, we aren't looking for members at this time.";
                        }

                        string response = (Receiver.Group != null)
                            ? "Yes, welcome to " + Receiver.Group.Name + "."
                            : "Yes, welcome to my new group. Not sure what I'll call it.";

                        DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "question", MessageID,
                            "May I join your group?", // Content
                            response, // Truthful/Compliant Response
                            denialResponse, // Updated Denial Response
                            "What? What group?", // Unknowing/Confused Response
                            "Can I join YOUR group?", // Derailing Response
                            "If it means you spend more time with me." // Flattering Response
                        );

                        if (Receiver.Group != null)
                        {
                            DecidedMessage.ResponseEntitiesForOne.Add(Receiver.Group);
                        }
                    }
                    else if (MessageID == "ask_current_structure")
                    {
                        Structure currentStructure = Receiver.Structure;
                        string truthfulResponse = "";
                        string madeUpResponse = "";
                        string historicalEvent = "";

                        // Generate a made-up response
                        var randomLocation = GameWorld.AllLocations[Game1.GameWorld.rnd.Next(GameWorld.AllLocations.Count())];
                        var randomStructure = randomLocation.AllStructures[Game1.GameWorld.rnd.Next(randomLocation.AllStructures.Count())];
                        var randomEvent = GameWorld.HistoricalEvents[Game1.GameWorld.rnd.Next(GameWorld.HistoricalEvents.Count())].EventData;

                        if (currentStructure != null)
                        {
                            // Get the last historical event related to the current structure
                            var lastEvent = GameWorld.HistoricalEvents.LastOrDefault(e => e.EventData.Contains(currentStructure.Name));
                            if (lastEvent != null)
                            {
                                historicalEvent = $" {lastEvent.EventData}";
                            }

                            truthfulResponse = $"You are in {currentStructure.Name}.{historicalEvent}";

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
                            "Wherever we are, it suits you perfectly." //flattering response
                        );

                        DecidedMessage.ResponseEntitiesForOne.Add(currentStructure);
                        DecidedMessage.ResponseEntitiesForTwo.Add(currentStructure);
                    }
                    else if (MessageID == "ask_location")
                    {
                        Location location = Receiver.Location;
                        string truthfulResponse = "";
                        string madeUpResponse = "";
                        string historicalEvent = "";

                        var randomLocation = GameWorld.AllLocations[Game1.GameWorld.rnd.Next(GameWorld.AllLocations.Count())];
                        var randomEvent = GameWorld.HistoricalEvents[Game1.GameWorld.rnd.Next(GameWorld.HistoricalEvents.Count())];

                        if (location != null)
                        {
                            // Get the last historical event related to the current structure
                            var lastEvent = GameWorld.HistoricalEvents.LastOrDefault(e => e.EventData.Contains(location.Name) && !e.EventData.Contains("arrived in"));
                            if (lastEvent != null)
                            {
                                historicalEvent = $" {lastEvent.EventData}";
                            }

                            truthfulResponse = $"You are at {location.Name}. {historicalEvent}";

                            madeUpResponse = $"You are in {randomLocation.Name}. {randomEvent.EventData}";
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
                            "Wherever we are, it suits you perfectly." //flattering response
                        );

                        if (location != null)
                        {
                            DecidedMessage.ResponseEntitiesForOne.Add(location);
                            DecidedMessage.ResponseEntitiesForTwo.Add(randomLocation);
                        }
                    }
                    else if (MessageID == "ask_profession")
                    {
                        string truthfulResponse = "";
                        string madeUpResponse = "";

                        truthfulResponse = $"I am a {Receiver.Profession}.";

                        // Generate a made-up response
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
                            "It's not as interesting as yours." //flattering response
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


                        foreach (Location l in Game1.GameWorld.AllLocations)
                        {
                            if (truthfulResponse.Contains(l.Name))
                            {
                                DecidedMessage.ResponseEntitiesForOne.Add(l);
                            }
                        }
                    }
                    else if (MessageID == "greet")
                    {
                        string[] greetings = { "Hello", "Hail", "Greetings", "Salutations", "Hey", "Good day" };
                        string randomGreeting = greetings[Game1.GameWorld.rnd.Next(greetings.Length)];

                        string greetingContent = Sender.KnownArchitects.Contains(Receiver) ?
                                                 $"{randomGreeting}, {Receiver.Name}." :
                                                 $"{randomGreeting}, {Receiver.Profession}.";

                        string truthfulResponse = greetings[Game1.GameWorld.rnd.Next(greetings.Length)] + ". How can I assist you today?";
                        string denialResponse = "Do not speak to me, " +
                                                Sender.Profession + ".";
                        string confusedResponse = greetings[Game1.GameWorld.rnd.Next(greetings.Length)] + ", um... do I know you?";
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
                        string randomFarewell = farewells[Game1.GameWorld.rnd.Next(farewells.Length)];

                        string farewellContent = Sender.KnownArchitects.Contains(Receiver) ?
                                                 $"{randomFarewell}, {Receiver.Name}." :
                                                 $"{randomFarewell}, {Receiver.Profession}.";

                        string truthfulResponse = randomFarewell + ", " +
                                                  (Receiver.KnownArchitects.Contains(Sender) ? Sender.Name : Sender.Profession) + ".";
                        string denialResponse = "Oh. " + farewells[Game1.GameWorld.rnd.Next(farewells.Length)] + ", " +
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
                        string randomThank = thanks[Game1.GameWorld.rnd.Next(thanks.Length)];

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
                        string randomApology = apologies[Game1.GameWorld.rnd.Next(apologies.Length)];

                        string apologyContent = Sender.KnownArchitects.Contains(Receiver) ?
                                                $"{randomApology}, {Receiver.Name}." :
                                                $"{randomApology}, {Receiver.Profession}.";

                        string truthfulResponse = "It is alright, I forgive you.";
                        string denialResponse = "There is nothing to forgive.";
                        string confusedResponse = "Why are you apologizing?";
                        string derailingResponse = "About time.";
                        string flatteringResponse = "Oh, don't worry about it.";

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
                            "Fine. I'm fine." //flattering response
                        );
                    }
                    else if (MessageID == "ask_news")
                    {
                        string truthfulResponse = "";
                        string madeUpResponse = "";
                        string flatteringResponse = "You arrived here...";

                        // Get the latest 5 (or fewer) events where Event.Region.Location is not null and Location == Sender.Location
                        List<Event> latestEvents = GameWorld.HistoricalEvents
                            .Where(e => e.Region?.Location != null && e.Region.Location == Sender.Location && e.Significant)
                            .TakeLast(5)
                            .ToList();

                        if (latestEvents.Count() > 0)
                        {
                            Event randomEvent = latestEvents[Game1.GameWorld.rnd.Next(latestEvents.Count)];
                            Event randomMadeUpEvent = null;

                            truthfulResponse = $"Recently, {randomEvent.EventData}";

                            // First, search for significant events at a random location
                            var randomLocation = GameWorld.AllLocations[Game1.GameWorld.rnd.Next(GameWorld.AllLocations.Count)];
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
                                randomMadeUpEvent = randomLocationEvents[Game1.GameWorld.rnd.Next(randomLocationEvents.Count)];
                                madeUpResponse = $"Recently, {randomMadeUpEvent.EventData}";
                            }
                            else
                            {
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
                            if (randomEvent != null)
                            {
                                DecidedMessage.ResponseEntitiesForOne.AddRange(randomEvent.Entities);
                            }
                            if (randomMadeUpEvent != null)
                            {
                                DecidedMessage.ResponseEntitiesForTwo.AddRange(randomMadeUpEvent.Entities);
                            }
                        }

                        else
                        {
                            truthfulResponse = "Nothing too interesting has happened here.";
                            madeUpResponse = "Nothing too interesting has happened here.";

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

                        var truthfulEvents = GameWorld.HistoricalEvents
                            .Where(e => e.EventData.Contains(receiverName))
                            .TakeLast(5)
                            .ToList();

                        var truthfulLines = new List<string>();
                        var truthfulEntities = new EntityList<Entity>();

                        foreach (var ev in truthfulEvents)
                        {
                            int endIndex = ev.EventData.IndexOf(") ") + 2;
                            string processedEvent = ev.EventData.Substring(endIndex).Replace(receiverName, "I");
                            int yearStart = ev.EventData.IndexOf("/") + 1;
                            int yearEnd = ev.EventData.IndexOf(")", yearStart);
                            string year = ev.EventData.Substring(yearStart, yearEnd - yearStart);

                            truthfulLines.Add($"In {year}, {processedEvent}");
                            truthfulEntities.AddRange(ev.Entities);
                        }

                        if (truthfulLines.Count > 0)
                        {
                            truthfulResponse = string.Join(" ", truthfulLines);
                        }
                        else
                        {
                            truthfulResponse = "Nothing too important happened to me.";
                        }

                        var randomArchitect = GameWorld.AllHistoricalArchitects.GetRandomItem();

                        var madeUpEvents = GameWorld.HistoricalEvents
                            .Where(e => e.EventData.Contains(randomArchitect.Name))
                            .TakeLast(5)
                            .ToList();

                        var madeUpLines = new List<string>();
                        var madeUpEntities = new EntityList<Entity>();

                        foreach (var ev in madeUpEvents)
                        {
                            int endIndex = ev.EventData.IndexOf(") ") + 2;
                            string processedEvent = ev.EventData.Substring(endIndex).Replace(randomArchitect.Name, "I");
                            int yearStart = ev.EventData.IndexOf("/") + 1;
                            int yearEnd = ev.EventData.IndexOf(")", yearStart);
                            string year = ev.EventData.Substring(yearStart, yearEnd - yearStart);

                            madeUpLines.Add($"In {year}, {processedEvent}");
                            madeUpEntities.AddRange(ev.Entities);
                        }

                        if (madeUpLines.Count > 0)
                        {
                            madeUpResponse = string.Join(" ", madeUpLines);
                        }
                        else
                        {
                            madeUpResponse = "Nothing too important happened to me.";
                        }

                        DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "question", MessageID,
                            "Tell me your story.", // content
                            truthfulResponse,      // truthful/compliant response
                            madeUpResponse,        // made up/denial response
                            "I'm not sure what to say.", // unknowing/confused response
                            "Stories are for another time.", // derailing response
                            "I'd bet it's not as interesting as yours!" // flattering response
                        );

                        DecidedMessage.ResponseEntitiesForOne = truthfulEntities; // truthful entities
                        DecidedMessage.ResponseEntitiesForTwo = madeUpEntities;   // lie entities
                    }

                    else if (MessageID == "ask_history")
                    {
                        string truthfulResponse = "";
                        string madeUpResponse = "";
                        string subjectName = Subjects[0].Name;

                        var truthfulEvents = GameWorld.HistoricalEvents
                            .Where(e => e.EventData.Contains(subjectName))
                            .TakeLast(5)
                            .ToList();

                        var truthfulLines = new List<string>();
                        var truthfulEntities = new EntityList<Entity>();

                        foreach (var ev in truthfulEvents)
                        {
                            int endIndex = ev.EventData.IndexOf(") ") + 2;
                            string processedEvent = ev.EventData.Substring(endIndex);
                            int yearStart = ev.EventData.IndexOf("/") + 1;
                            int yearEnd = ev.EventData.IndexOf(")", yearStart);
                            string year = ev.EventData.Substring(yearStart, yearEnd - yearStart);

                            truthfulLines.Add($"In {year}, {processedEvent}");
                            truthfulEntities.AddRange(ev.Entities);
                        }

                        if (truthfulLines.Count > 0)
                        {
                            truthfulResponse = string.Join(" ", truthfulLines);
                        }
                        else
                        {
                            truthfulResponse = "Nothing too important happened here.";
                        }

                        var randomLocation = GameWorld.AllLocations[Game1.GameWorld.rnd.Next(GameWorld.AllLocations.Count())];

                        var madeUpEvents = GameWorld.HistoricalEvents
                            .Where(e => e.EventData.Contains(randomLocation.Name))
                            .TakeLast(5)
                            .ToList();

                        var madeUpLines = new List<string>();
                        var madeUpEntities = new EntityList<Entity>();

                        foreach (var ev in madeUpEvents)
                        {
                            int endIndex = ev.EventData.IndexOf(") ") + 2;
                            string processedEvent = ev.EventData.Substring(endIndex);
                            int yearStart = ev.EventData.IndexOf("/") + 1;
                            int yearEnd = ev.EventData.IndexOf(")", yearStart);
                            string year = ev.EventData.Substring(yearStart, yearEnd - yearStart);

                            madeUpLines.Add($"In {year}, {processedEvent}");
                            madeUpEntities.AddRange(ev.Entities);
                        }

                        if (madeUpLines.Count > 0)
                        {
                            madeUpResponse = string.Join(" ", madeUpLines);
                        }
                        else
                        {
                            madeUpResponse = "Nothing too important happened here.";
                        }

                        DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "question", MessageID,
                            "Can you tell me about the history here?", // content
                            truthfulResponse,     // truthful/compliant response
                            madeUpResponse,       // made up/denial response
                            "I'm not sure what to say.", // unknowing/confused response
                            "History is best left in the past.", // derailing response
                            "You arrived..." // flattering response
                        );

                        DecidedMessage.ResponseEntitiesForOne = truthfulEntities; // truthful entities
                        DecidedMessage.ResponseEntitiesForTwo = madeUpEntities;   // lie entities
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
                            opinion = Receiver.GetOpinion(subjectArchitect.ArchitectILookLike);
                        }
                        else
                        {
                            opinion = Game1.GameWorld.rnd.Next(-100, 101);
                        }

                        string opinionDescription = GetOpinionDescription(opinion);
                        string truthfulResponse = $"I {opinionDescription} {Subjects[0].ReferredToNames[0]}.";
                        string madeUpResponse = $"I {GetOpinionDescription(Game1.GameWorld.rnd.Next(-100, 101))} {Subjects[0].ReferredToNames[0]}.";

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

                        DecidedMessage.ResponseEntitiesForOne.Add(Subjects[0]);
                        DecidedMessage.ResponseEntitiesForTwo.Add(Subjects[0]);
                    }
                    else if (MessageID == "ask_interests")
                    {
                        string truthfulResponse = "";
                        string madeUpResponse = "";
                        string derailingResponse = "";

                        // Determine the receiver's highest proficiency
                        if (Receiver.Proficiencies.Count() > 0)
                        {
                            var highestProficiency = Receiver.Proficiencies.OrderByDescending(xp => xp.Item2).First();
                            truthfulResponse = $"I am very interested in {highestProficiency.Item1}.";
                        }
                        else
                        {
                            truthfulResponse = "I am thinking about taking up shiba taming.";
                        }

                        // Generate a made-up response
                        var randomProficiency = Receiver.Proficiencies.Count() > 0 ? Receiver.Proficiencies[Game1.GameWorld.rnd.Next(Receiver.Proficiencies.Count())] : ("alchemy", Game1.GameWorld.rnd.Next(1, 101));
                        madeUpResponse = $"I am very interested in {randomProficiency.Item1}.";

                        // Determine the sender's lowest proficiency
                        if (Sender.Proficiencies.Count() > 0)
                        {
                            var lowestProficiency = Sender.Proficiencies.OrderBy(xp => xp.Item2).First();
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
                        List<(string, Architect)> Family = new();

                        if (Receiver.Mother != null)
                            Family.Add(("mother", Receiver.Mother));
                        if (Receiver.Father != null)
                            Family.Add(("father", Receiver.Father));
                        if (Receiver.PaternalGrandMother != null)
                            Family.Add(("paternal grandmother", Receiver.PaternalGrandMother));
                        if (Receiver.PaternalGrandFather != null)
                            Family.Add(("paternal grandfather", Receiver.PaternalGrandFather));
                        if (Receiver.MaternalGrandMother != null)
                            Family.Add(("maternal grandmother", Receiver.MaternalGrandMother));
                        if (Receiver.MaternalGrandFather != null)
                            Family.Add(("maternal grandfather", Receiver.MaternalGrandFather));

                        foreach (var sibling in Receiver.Siblings)
                        {
                            if (sibling != null)
                            {
                                string relation = sibling.Sex == "male" ? "brother" : sibling.Sex == "female" ? "sister" : "sibling";
                                Family.Add((relation, sibling));
                            }
                        }

                        string truthful = "I'm honestly not sure.";
                        if (Family.Any())
                        {
                            var randomFamily = Family[GameWorld.rnd.Next(Family.Count)];
                            bool alive = randomFamily.Item2.IsAlive;
                            string status = alive ? "have" : "had";
                            truthful = $"I {status} a {randomFamily.Item1} named {randomFamily.Item2.Name}.";
                        }

                        DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "question", MessageID,
                                "Do you have any family?", // updated question
                                truthful, // dynamic truthful response
                                "Oh, they're around... somewhere...", // made up/denial response
                                "What is a family?", // unknowing/confused response
                                "We are all family, if you only stop to think about it.", // derailing response
                                "You are my family." // flattering response
                            );
                    }

                    else if (MessageID == "offer_assistance")
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


                        if (Receiver.HookedObjective != null && Receiver.HookedObjective.RequiredInteraction == "offerassistance")
                        {
                            truthfulResponse = Receiver.HookedObjective.PointerMessage.Data;

                            DecidedMessage = new Message
                               (
                                   Sender, Receiver, Subjects, "question", MessageID,
                                   "How may I be of assistance?", // content
                                   truthfulResponse, // truthful/compliant response
                                   truthfulResponse, // made up/denial response
                                   truthfulResponse, // unknowing/confused response
                                   truthfulResponse, // derailing response
                                   truthfulResponse // flattering response
                               );

                            DecidedMessage.ResponseEntitiesForOne.AddRange(Receiver.HookedObjective.PointerMessage.Entities);
                            DecidedMessage.ResponseEntitiesForTwo.AddRange(Receiver.HookedObjective.PointerMessage.Entities);
                            DecidedMessage.ResponseEntitiesForThree.AddRange(Receiver.HookedObjective.PointerMessage.Entities);
                            DecidedMessage.ResponseEntitiesForFour.AddRange(Receiver.HookedObjective.PointerMessage.Entities);
                            DecidedMessage.ResponseEntitiesForFive.AddRange(Receiver.HookedObjective.PointerMessage.Entities);

                            DecidedMessage.IgnoreHeader = true;

                            if (Receiver.TerminalTalk && Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Sender))
                            {
                                Receiver.DieOnResponse = true; // kill the person next tick
                            }
                        }
                        else
                        {

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
                                "How may I be of assistance?", // content
                                truthfulResponse, // truthful/compliant response
                                madeUpResponse, // made up/denial response
                                "I'm not really sure.", // unknowing/confused response
                                "By leaving my presence.", // derailing response
                                "Your concern is appreciated." // flattering response
                            );

                            DecidedMessage.StoredRevealLocations.AddRange(StoredReveal);



                            if (nearestLocation != null)
                            {
                                DecidedMessage.ResponseEntitiesForOne.Add(nearestLocation);
                            }
                        }

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

                        string subjectName = Subjects[0].Name != null ? Subjects[0].Name : Subjects[0].ReferredToNames[0];

                        string PickRandomAdvice()
                        {
                            int index = Game1.GameWorld.rnd.Next(adviceList.Count());
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
                        if (GameWorld.Calamity.Count > 0)
                        {
                            Subjects.Add(GameWorld.Calamity[0]);

                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "request", MessageID,
                                "I am on a quest to slay the great " + (GameWorld.Calamity[0].Name).Split(' ')[0] + ".", //content
                                "I wish you the best of luck.", //truthful/compliant response
                                "Hah, we will see.", //made up/denial response
                                "Who is that?", //unknowing/confused response
                                GameWorld.Calamity[0].Pronoun + " is already dead.", //derailing response
                                "I used to be an adventurer like you." //flattering response

                            );
                        }
                        else
                        {
                            DecidedMessage = new Message
                            (
                                Sender, Receiver, Subjects, "request", MessageID,
                                "I am not sure where to go.", //content
                                "I wish you the best of luck.", //truthful/compliant response
                                "We shall see.", //made up/denial response
                                "We are the answer.", //unknowing/confused response
                                "How do you want me to help?", //derailing response
                                "I used to be an adventurer like you." //flattering response

                            );
                        }
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

                        var matchingEvents = GameWorld.HistoricalEvents
                            .Where(e => e.EventData.Contains(subjectName))
                            .ToList();

                        // Sort events by date or leave as-is if already ordered chronologically

                        // Set a random cap between 20 and 30
                        int maxEventCap = Game1.GameWorld.rnd.Next(20, 31);

                        var eventLines = new List<string>();
                        var associatedEntities = new EntityList<Entity>();

                        int addedCount = 0;

                        foreach (var ev in matchingEvents)
                        {
                            // Randomly skip events with a ~50% chance, unless we've hit the cap
                            if (Game1.GameWorld.rnd.NextDouble() < 0.5)
                                continue;

                            int endIndex = ev.EventData.IndexOf(") ") + 2;
                            string processedEvent = ev.EventData.Substring(endIndex);
                            int yearStart = ev.EventData.IndexOf("/") + 1;
                            int yearEnd = ev.EventData.IndexOf(")", yearStart);
                            string year = ev.EventData.Substring(yearStart, yearEnd - yearStart);

                            eventLines.Add($"In {year}, {processedEvent}");
                            associatedEntities.AddRange(ev.Entities);
                            addedCount++;

                            if (addedCount >= maxEventCap)
                                break;
                        }


                        string content;
                        if (eventLines.Count > 0)
                        {
                            content = introduction + " " + string.Join(" ", eventLines);
                        }
                        else
                        {
                            content = introduction + " There isn't much else to say.";
                        }

                        DecidedMessage = new Message
                        (
                            Sender, Receiver, Subjects, "question", MessageID,
                            content, // content
                            "That was a nice story.",  // truthful/compliant response
                            "I didn't really enjoy that.", // made up/denial response
                            "I'm not sure I understand.", // unknowing/confused response
                            "Enough about that, let's talk about me.", // derailing response
                            "Woah, now tell me your story." // flattering response
                        );

                        DecidedMessage.ResponseEntitiesForOne = associatedEntities; // set truthful associated entities
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

                        var complimentableClothing = Receiver.Clothing
                        .Where(o => o.Type != "brassiere" && o.Type != "undergarment")
                        .ToList();

                        if (complimentableClothing.Count > 0)
                        {
                            var item = complimentableClothing[Game1.GameWorld.rnd.Next(complimentableClothing.Count)];
                            compliments.Add("Your " + item.Type + " looks very nice.");
                        }

                        string randomCompliment = compliments[Game1.GameWorld.rnd.Next(compliments.Count())];

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

                        string randomInsult = insults[Game1.GameWorld.rnd.Next(insults.Count())];

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
                                "You'd better surrender to this." //flattering response

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
                                "Ok! I didn't want to fight to begin with..." //flattering response

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
                                "Fine, only for you." //flattering response

                            );
                    }
                    else
                    {
                        throw new Exception("You should not be here.");
                    }


                    if (DecidedMessage != null)
                    {
                        bool hasTelepathy = Sender.Invocations.Contains("telepathy");

                        static int Clamp(int value, int min, int max)
                        {
                            if (value < min) return min;
                            if (value > max) return max;
                            return value;
                        }

                        if (hasTelepathy && (Sender.Block != Receiver.Block || Sender.Room != Receiver.Room))
                        {
                            Color c = (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Sender) ||
                                       Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Receiver)) ?
                                       new Color(0, 255, 255) : new Color(0, 75, 75);

                            Sender.AnnounceToParty(Sender.ReferredToNames[0] + ": " + DecidedMessage.MessageContent, c, new EntityList<Entity> { Sender }.Union(DecidedMessage.Subjects));
                            Sender.CooldownCycles += (int)Math.Round(30 / Sender.Speed);

                            bool PlaySound = Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Sender) ||
                                             (Sender.Room == null && Sender.Block.Architects.Any(t => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(t))) ||
                                             (Sender.Room != null && Sender.Room.Architects.Any(t => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(t)));

                            bool Darken = !Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Sender) &&
                                          !Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Receiver);

                            bool messageSeenBefore = Receiver.ResponseDatabase.Any(KvP => KvP.Key == DecidedMessage.MessageContent);

                            if (messageSeenBefore && !(Receiver.ArchitectsIWillTellTruthTo.Contains(Sender) ||
                                                       Sender.ArchitectsWhoSurrenderedToMe.Contains(Receiver) ||
                                                       DecidedMessage.MessageContent.StartsWith("Would you tell me where I can find")))
                            {
                                Receiver.AnnounceToParty(Receiver.ReferredToNames[0] + ": " + Receiver.ResponseDatabase[DecidedMessage.MessageContent].Item1,
                                    new Color(0, 255, 0) * (Darken ? 0.3f : 1.0f),
                                    new EntityList<Entity> { Receiver }.Union(Receiver.ResponseDatabase[DecidedMessage.MessageContent].Item2));
                            }
                            else
                            {
                                bool bothAreSapient = (Game1.GameWorld.HumanoidRaces.Contains(Sender.Race) || Game1.GameWorld.ExtraRaces.Contains(Sender.Race)) &&
                                                      (Game1.GameWorld.HumanoidRaces.Contains(Receiver.Race) || Game1.GameWorld.ExtraRaces.Contains(Receiver.Race));

                                bool canReceiveMessage = bothAreSapient || Receiver.PathOfLifeLevel >= 4 || Sender.PathOfLifeLevel >= 4;

                                if ((Receiver.CombatCycles > 0 && !(DecidedMessage.MessageID == "surrender" || DecidedMessage.MessageID == "demand_surrender")) ||
                                    !canReceiveMessage || Receiver.Bound)
                                {
                                    Sender.AnnounceToParty(Receiver.ReferredToNames[0] + " does not reply.",  //SENDER SPECIFICALLY replies so you hear it.
                                        Color.Yellow * (Darken ? 0.3f : 1.0f),
                                        new EntityList<Entity> { Receiver });
                                }
                                else
                                {
                                    int baseChanceToTruth = 60 + Sender.Charisma * 3;
                                    int baseChanceToMakeUp = 20;
                                    int baseChanceToClaimIgnorance = 10;
                                    int baseChanceToDerail = 5;
                                    int baseChanceToFlatter = 5;

                                    if (Game1.ProbablyWillTellTruth.Contains(Receiver.Profession))
                                    {
                                        baseChanceToTruth += 15;
                                        baseChanceToMakeUp -= 5;
                                        baseChanceToClaimIgnorance -= 5;
                                    }

                                    if (Receiver.ArchitectsIWillTellTruthTo.Contains(Sender) ||
                                        Sender.ArchitectsWhoSurrenderedToMe.Contains(Receiver) ||
                                        Game1.TutorialActive)
                                    {
                                        baseChanceToTruth = 100;
                                        baseChanceToMakeUp = 0;
                                        baseChanceToClaimIgnorance = 0;
                                        baseChanceToDerail = 0;
                                        baseChanceToFlatter = 0;
                                    }
                                    else if (DecidedMessage.MessageContent.StartsWith("Would you tell me where I can find") &&
                                             DecidedMessage.MessageID != "ask_generic_directions")
                                    {
                                        var truthProfessions = new HashSet<string>
                {
                    "scholar", "mage", "engineer", "entertainer", "artificer", "bard", "sage", "luminary",
                    "warlock", "sorcerer", "necromancer", "spatiomancer", "perceptomancer", "conjumancer",
                    "fractalmancer", "archmage", "magician", "archbard", "archsage", "archluminary", "archartificer", "scribe"
                };

                                        if (truthProfessions.Contains(Receiver.Profession))
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
                                    else if (DecidedMessage.MessageID == "challenge" && Receiver.Level == 1)
                                    {
                                        baseChanceToTruth = 0;
                                        baseChanceToMakeUp = 100;
                                        baseChanceToClaimIgnorance = 0;
                                        baseChanceToDerail = 0;
                                        baseChanceToFlatter = 0;
                                    }
                                    else
                                    {
                                        int senderOpinion = Receiver.GetOpinion(Sender.ArchitectILookLike);

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

                                        int focus = Receiver.Focus;
                                        int charisma = Receiver.Charisma;

                                        baseChanceToDerail -= (7 - focus) * 2;
                                        baseChanceToFlatter += charisma;

                                        baseChanceToTruth = Clamp(baseChanceToTruth, 0, 100);
                                        baseChanceToMakeUp = Clamp(baseChanceToMakeUp, 0, 100);
                                        baseChanceToClaimIgnorance = Clamp(baseChanceToClaimIgnorance, 0, 100);
                                        baseChanceToDerail = Clamp(baseChanceToDerail, 0, 100);
                                        baseChanceToFlatter = Clamp(baseChanceToFlatter, 0, 100);

                                        // Normalize
                                        int total = baseChanceToTruth + baseChanceToMakeUp + baseChanceToClaimIgnorance + baseChanceToDerail + baseChanceToFlatter;
                                        if (total > 0)
                                        {
                                            float scale = 100f / total;
                                            baseChanceToTruth = (int)(baseChanceToTruth * scale);
                                            baseChanceToMakeUp = (int)(baseChanceToMakeUp * scale);
                                            baseChanceToClaimIgnorance = (int)(baseChanceToClaimIgnorance * scale);
                                            baseChanceToDerail = (int)(baseChanceToDerail * scale);
                                            baseChanceToFlatter = 100 - (baseChanceToTruth + baseChanceToMakeUp + baseChanceToClaimIgnorance + baseChanceToDerail);
                                        }
                                    }

                                    int randomNumber = Game1.GameWorld.rnd.Next(1, 101);
                                    string response;
                                    string responseType = "";
                                    EntityList<Entity> responseEntities = new EntityList<Entity>();

                                    if (randomNumber <= baseChanceToTruth)
                                    {
                                        response = DecidedMessage.PositiveResponse;
                                        responseType = "truth";
                                        responseEntities = DecidedMessage.ResponseEntitiesForOne ?? new EntityList<Entity>();
                                    }
                                    else if (randomNumber <= baseChanceToTruth + baseChanceToMakeUp)
                                    {
                                        response = DecidedMessage.DirectRefusalResponse;
                                        responseType = "lie";
                                        responseEntities = DecidedMessage.ResponseEntitiesForTwo ?? new EntityList<Entity>();
                                    }
                                    else if (randomNumber <= baseChanceToTruth + baseChanceToMakeUp + baseChanceToClaimIgnorance)
                                    {
                                        response = DecidedMessage.IgnorantResponse;
                                        responseType = "ignore";
                                        responseEntities = DecidedMessage.ResponseEntitiesForThree ?? new EntityList<Entity>();
                                    }
                                    else if (randomNumber <= baseChanceToTruth + baseChanceToMakeUp + baseChanceToClaimIgnorance + baseChanceToDerail)
                                    {
                                        response = DecidedMessage.DerailingResponse;
                                        responseType = "derail";
                                        responseEntities = DecidedMessage.ResponseEntitiesForFour ?? new EntityList<Entity>();
                                    }
                                    else
                                    {
                                        response = DecidedMessage.FlatteringResponse;
                                        responseType = "flirt";
                                        responseEntities = DecidedMessage.ResponseEntitiesForFive ?? new EntityList<Entity>();
                                    }

                                    // Now use the correct entities in the announcement
                                    Sender.AnnounceToParty(
                                        Receiver.ReferredToNames[0] + ": " + response,
                                        new Color(0, 255, 0) * (Darken ? 0.3f : 1.0f),
                                        new EntityList<Entity> { Receiver }.Union(responseEntities)
                                    );

                                }
                            }
                        }
                        else
                        {
                            Receiver.MessagesNotRespondedTo.Add(DecidedMessage);

                            Color c = (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Sender) ||
                                       Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Receiver)) ?
                                       new Color(0, 255, 255) : new Color(0, 75, 75);

                            Sender.AnnounceToParty(Sender.ReferredToNames[0] + ": " + DecidedMessage.MessageContent, c, new EntityList<Entity> { Sender }.Union(DecidedMessage.Subjects));
                            Sender.CooldownCycles += (int)Math.Round(30 / Sender.Speed);

                            bool PlaySound = Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(Sender) ||
                                             (Sender.Room == null && Sender.Block.Architects.Any(t => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(t))) ||
                                             (Sender.Room != null && Sender.Room.Architects.Any(t => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(t)));

                            if (PlaySound)
                                Game1.SFX.Add(Game1.TalkSounds[Sender.VoiceType]);
                        }
                    }
                }
            }
        }
    }
}
