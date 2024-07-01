using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    public static class LegendsManager
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

        static List<string> AdventuringLocations = new List<string> { "archway", "hallway", "keep", "monument", "outpost", "sanctum", "stronghold", "spire", "tower"};
        static List<string> VisitationLocations = new List<string> { "commune", "cove", "hoard", "monastery", "preserve"};

        public static void ManageLegends(World World, int Days)
        {
            int Month = ((int)Math.Round((decimal)(World.Cycle / 24192000)) % 12) + 1;
            int Year = (int)Math.Round((decimal)(World.Cycle / 290304000), MidpointRounding.ToZero);

            string Date = "(" + Month + "/" + Year + ")";

            int MonthToDayConstant = (28 / Days);

            foreach (Architect a in World.Legends)
            {
                if (!a.IsAlive)
                {
                    continue;
                }

                void LogEvent(string Event)
                {
                    a.Location.LocationHistoricalEvents.Add(Date + " " + Event);
                    World.HistoricalEvents.Add(Date + " " + Event);
                }


                if (!LegendTypes.Contains(a.Profession))
                {
                    a.Profession = LegendTypes[Game1.r.Next(7)];

                    LogEvent(a.Name + " started their journey as a " + a.Profession + ".");
                }


                //migrate around a bit maybe

                if (Game1.r.Next(500*MonthToDayConstant) == 1 && a.Profession != "enchanter" && a.Profession != "artisan" && a.Profession != "adventurer")
                {
                    double CalculateDistance(int x1, int z1, int x2, int z2)
                    {
                        return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(z2 - z1, 2));
                    }

                    // Current location coordinates
                    int currentX = a.Location.Region.X;
                    int currentZ = a.Location.Region.Z;

                    // Filter locations based on distance
                    var nearbyLocations = World.AllLocations
                        .Where(loc => CalculateDistance(currentX, currentZ, loc.Region.X, loc.Region.Z) < 10)
                        .ToList();

                    if (nearbyLocations.Any())
                    {
                        a.NextMigrationLocation = nearbyLocations[Game1.r.Next(nearbyLocations.Count)];

                        LogEvent(a.Name + " migrated to " + a.NextMigrationLocation.Name + ".");
                    }
                    else
                    {
                        // Handle the case where no nearby locations are found within the specified distance
                        a.NextMigrationLocation = null; // or set to a default location
                    }
                }

                if (a.Profession == "hunter")
                {
                    int Decider = Game1.r.Next(1, 100 * MonthToDayConstant);

                    if (a.LegendaryTarget == null)
                    {
                        List<Architect> shuffledArchitects = World.AllArchitects.OrderBy(a => Guid.NewGuid()).ToList();

                        // Get the first item where Profession is "beast", "animal", or "end"
                        Architect targetArchitect =
                            shuffledArchitects.FirstOrDefault(a =>
                            (a.Profession == "beast" || a.Profession == "animal" || a.Profession == "end") && !World.Calamity.Contains(a));

                        if (targetArchitect != null)
                        {
                            a.LegendaryTarget = targetArchitect;
                            LogEvent(a.Name + " started tracking " + targetArchitect.Name + ".");
                        }
                    }

                    if (Decider < 5)
                    {
                        a.HuntingProgress += 5;
                    }
                    else if (Decider < 8)
                    {
                        a.HuntingProgress -= 3;
                    }

                    if (a.HuntingProgress > 100)
                    {
                        string[] approaches = new string[]
                        {
                            "cautiously stalked the target from the shadows",
                            "moved in, hoping for a swift kill",
                            "set up an ambush using the nearby terrain",
                            "patiently waited for the perfect moment to strike"
                        };
                        string[] fightDetails = new string[]
                        {
                            "The battle was fierce and relentless",
                            "The target put up a strong resistance",
                            "The fight was over quickly",
                            "The fight was a struggle that lasted for hours"
                        };
                        string[] victories = new string[]
                        {
                            "In the end, the hunter emerged victorious",
                            "The hunter triumphed with ease",
                            "The hunter claimed victory after a hard-fought battle",
                            "Ultimately, the hunter stood victorious"
                        };
                        string[] failures = new string[]
                        {
                            "Unfortunately, the hunter was defeated, though still alive",
                            "The target managed to escape",
                            "The hunter was forced to retreat",
                            "Despite their efforts, the hunter failed"
                        };
                        string[] deaths = new string[]
                        {
                            "Tragically, the hunter was slain",
                            "The target overpowered and killed the hunter",
                            "The hunter met their demise in the encounter",
                            "Sadly, the hunter did not survive the battle"
                        };

                        string approach = approaches[Game1.r.Next(approaches.Length)];
                        string fightDetail = fightDetails[Game1.r.Next(fightDetails.Length)];
                        string outcome;

                        int outcomeRoll = Game1.r.Next(100); // Get a number between 0 and 99

                        if (outcomeRoll < 60)
                        {
                            // 60% chance of success
                            outcome = victories[Game1.r.Next(victories.Length)];

                            ((Architect)(a.LegendaryTarget)).IsAlive = false;
                        }
                        else if (outcomeRoll < 95)
                        {
                            // 35% chance of failure and escape
                            outcome = failures[Game1.r.Next(failures.Length)];
                        }
                        else
                        {
                            // 5% chance of death
                            outcome = deaths[Game1.r.Next(deaths.Length)];
                            a.IsAlive = false;
                        }

                        string logMessage = $"{a.Name} located {a.LegendaryTarget.Name}. {a.Name} {approach}. {fightDetail}. {outcome}.";
                        LogEvent(logMessage);

                        a.LegendaryTarget = null;
                        a.HuntingProgress = 0;
                    }
                }
                else if (a.Profession == "assassin")
                {
                    int Decider = Game1.r.Next(1, 25 * MonthToDayConstant);

                    if (a.LegendaryTarget == null && Decider == 1)
                    {
                        List<Architect> shuffledArchitects = World.AllArchitects.OrderBy(a => Guid.NewGuid()).ToList();

                        // Get the first item where Race is in HumanoidRaces, Reputation < -49, and not in Calamity
                        Architect targetArchitect = shuffledArchitects.FirstOrDefault(target =>
                            World.HumanoidRaces.Contains(target.Race) && target.Reputation < -20 && !World.Calamity.Contains(target) && target.IsAlive);

                        if (targetArchitect != null)
                        {
                            a.LegendaryTarget = targetArchitect;
                            LogEvent(a.Name + " started searching for information on their next target, " + targetArchitect.Name + ".");
                        }
                    }
                    else if (a.LegendaryTarget != null)
                    {

                        if (Decider < 5)
                        {
                            a.HuntingProgress += 5;
                        }
                        else if (Decider < 8)
                        {
                            a.HuntingProgress -= 3;
                        }

                        if (a.HuntingProgress > 100)
                        {
                            string[] approaches = new string[]
                            {
                    "cautiously stalked the target from a distance",
                    "moved in silently for the kill",
                    "set up a trap using the surrounding environment",
                    "patiently observed the target, waiting for the perfect moment to strike"
                            };
                            string[] fightDetails = new string[]
                            {
                    "The confrontation was intense and brutal",
                    "The target fought back fiercely",
                    "The kill was swift and efficient",
                    "The struggle lasted for a long time"
                            };
                            string[] victories = new string[]
                            {
                    "In the end, the assassin completed their mission successfully",
                    "The assassin triumphed without much difficulty",
                    "The assassin claimed victory after a tough fight",
                    "Ultimately, the assassin accomplished their deadly task"
                            };
                            string[] failures = new string[]
                            {
                    "The assassin failed, but managed to escape alive",
                    "The target evaded the assassin's attempts",
                    "The assassin was forced to retreat",
                    "Despite their efforts, the assassin did not succeed"
                            };
                            string[] deaths = new string[]
                            {
                    "Tragically, the assassin was killed",
                    "The target overpowered and killed the assassin",
                    "The assassin met their end in the confrontation",
                    "Sadly, the assassin did not survive the encounter"
                            };

                            string approach = approaches[Game1.r.Next(approaches.Length)];
                            string fightDetail = fightDetails[Game1.r.Next(fightDetails.Length)];
                            string outcome;

                            int outcomeRoll = Game1.r.Next(100); // Get a number between 0 and 99

                            if (outcomeRoll < 60)
                            {
                                // 60% chance of success
                                outcome = victories[Game1.r.Next(victories.Length)];
                                ((Architect)(a.LegendaryTarget)).IsAlive = false;
                            }
                            else if (outcomeRoll < 95)
                            {
                                // 35% chance of failure and escape
                                outcome = failures[Game1.r.Next(failures.Length)];
                            }
                            else
                            {
                                // 5% chance of death
                                outcome = deaths[Game1.r.Next(deaths.Length)];
                                a.IsAlive = false;
                            }

                            string logMessage = $"{a.Name} located {a.LegendaryTarget.Name}. {a.Name} {approach}. {fightDetail}. {outcome}.";
                            a.HuntingProgress = 0;
                            LogEvent(logMessage);
                        }
                    }

                }
                else if (a.Profession == "rogue")
                {
                    int Decider = Game1.r.Next(1, 100 * MonthToDayConstant);

                    if (a.LegendaryTarget == null)
                    {
                        var allStructuresWithObjects = World.AllLocations
                            .SelectMany(location => location.AllStructures
                            .Where(structure => structure.HistoricalObjects.Count > 0))
                            .ToList();

                        if(allStructuresWithObjects.Count > 0)
                        {

                            var selectedStructure = allStructuresWithObjects[Game1.r.Next(allStructuresWithObjects.Count)];
                            var targetObject = selectedStructure.HistoricalObjects[Game1.r.Next(selectedStructure.HistoricalObjects.Count)];
                            
                            if (targetObject != null)
                            {
                                a.LegendaryTarget = targetObject;
                                a.LegendaryTargetStructure = selectedStructure;
                                LogEvent(a.Name + " started searching for information on the great treasure, " + targetObject.Name + ".");
                            }
                        }
                    }
                    else
                    {
                        if (Decider < 5)
                        {
                            a.HuntingProgress += 5;
                        }
                        else if (Decider < 8)
                        {
                            a.HuntingProgress -= 3;
                        }

                        if (a.HuntingProgress > 100)
                        {
                            string[] approaches = new string[]
                            {
                    "carefully planned the heist",
                    "moved in quietly, avoiding detection",
                    "used the cover of night to approach the target",
                    "waited patiently for the right moment to strike"
                            };
                            string[] theftDetails = new string[]
                            {
                    "The attempt was tense and nerve-wracking",
                    "The target location was well-guarded",
                    "The theft was executed swiftly",
                    "It was a long and risky operation"
                            };
                            string[] successes = new string[]
                            {
                    "In the end, the rogue successfully stole the item",
                    "The rogue made off with the treasure without a hitch",
                    "The rogue completed the heist with great skill",
                    "Ultimately, the rogue managed to secure the prize"
                            };
                            string[] failures = new string[]
                            {
                    "The rogue was caught but managed to escape",
                    "The target was too heavily guarded, and the rogue fled",
                    "The rogue was forced to abandon the mission",
                    "Despite their efforts, the rogue failed to steal the item"
                            };

                            string[] deaths;

                            if (a.LegendaryTargetStructure.Block.District.Location.Government != null)
                            {
                                deaths = new string[]
                                {
                                    "The rogue was caught and executed by " + a.LegendaryTargetStructure.Block.District.Location.Government.Name,
                                    "The guards overpowered and killed the rogue",
                                    "The rogue met their demise during the attempt",
                                    "Tragically, the rogue did not survive the heist"
                                };
                            }
                            else
                            {
                                deaths = new string[]
                               {
                                    "The rogue was caught and executed by the general population",
                                    "The guards overpowered and killed the rogue",
                                    "The rogue met their demise during the attempt",
                                    "Tragically, the rogue did not survive the heist"
                               };
                            }

                            string approach = approaches[Game1.r.Next(approaches.Length)];
                            string theftDetail = theftDetails[Game1.r.Next(theftDetails.Length)];
                            string outcome;

                            int outcomeRoll = Game1.r.Next(10000); // Get a number between 0 and 9999

                            if (outcomeRoll < 195)
                            {
                                // 19.5% chance of success
                                outcome = successes[Game1.r.Next(successes.Length)];
                                a.Inventory.Add((Object)a.LegendaryTarget);
                                a.LegendaryTargetStructure.HistoricalObjects.Remove((Object)a.LegendaryTarget);
                            }
                            else if (outcomeRoll < 9950)
                            {
                                // 80% chance of failure and escape
                                outcome = failures[Game1.r.Next(failures.Length)];
                            }
                            else
                            {
                                // 0.5% chance of death
                                outcome = deaths[Game1.r.Next(deaths.Length)];
                                a.IsAlive = false;
                            }

                            string logMessage = $"{a.Name} found the treasure he had been searching for, {a.LegendaryTarget.ReferredToNames[0]}. {a.Name} {approach}. {theftDetail}. {outcome}.";
                            a.HuntingProgress = 0;
                            LogEvent(logMessage);

                            a.LegendaryTarget = null;
                            a.LegendaryTargetStructure = null;
                        }
                    }
                }
                else if (a.Profession == "artisan")
                {
                    int Decider = Game1.r.Next(1, 1000 * MonthToDayConstant);

                    if (Decider == 1 && a.Location.AllStructures.Count > 0)
                    {
                        Object o = World.MagicalSuperLoot(5);
                        Structure Storage = a.Location.AllStructures[Game1.r.Next(a.Location.AllStructures.Count)];
                        Storage.HistoricalObjects.Add(o);
                        o.Name = World.GenerateUniqueName("1S" + Game1.r.Next(6) + "s 1W2s", o);

                        LogEvent(a.Name + " created a " + o.Type + " in " + a.Location.Name + ", named it " + o.Name + ", and stored it inside " + Storage.Name + ".");
                    }
                }
                else if (a.Profession == "enchanter")
                {
                    int Decider = Game1.r.Next(1, 200 * MonthToDayConstant);

                    if (Decider == 1 && a.District.GeneralItemsWeHave.Count > 0)
                    {
                        // Filter out items that contain other items
                        var availableItems = a.District.GeneralItemsWeHave
                            .Where(item => !item.Contains("&cont("))
                            .ToList();

                        if (availableItems.Count == 0) return; // No valid items to enchant

                        // Select a random item from the district's general items
                        string selectedItemString = availableItems[Game1.r.Next(availableItems.Count)];

                        // Decrement the count if it's a stack
                        string[] itemParts = selectedItemString.Split(new[] { ',' }, 3);
                        string itemType = itemParts[0];
                        int itemCount = int.Parse(itemParts[1]);
                        string itemMaterialsAndContained = itemParts[2];

                        if (itemCount > 1)
                        {
                            // If the item is part of a stack, decrement the count
                            itemCount--;
                            string updatedItemString = $"{itemType},{itemCount},{itemMaterialsAndContained}";
                            a.District.GeneralItemsWeHave[a.District.GeneralItemsWeHave.IndexOf(selectedItemString)] = updatedItemString;
                        }
                        else
                        {
                            // If it's not a stack, remove the item from the list
                            a.District.GeneralItemsWeHave.Remove(selectedItemString);
                        }

                        // Convert the item string to an Object and take one item from the stack
                        List<Object> objects = Game1.ConvertStringToObjects(selectedItemString);
                        Object o = objects.First(); // Taking only one object from the list

                        // Perform the enchanting
                        o.Name = World.GenerateUniqueName("1S" + Game1.r.Next(3, 9) + "s", o);
                        o.Rarity = "legendary";

                        // Store the enchanted object in a random structure
                        Structure Storage = a.Location.AllStructures[Game1.r.Next(a.Location.AllStructures.Count)];
                        Storage.HistoricalObjects.Add(o);

                        // Log the event
                        LogEvent($"{a.Name} enchanted a {o.Type} in {a.Location.Name}, named it {o.Name}, and stored it inside {Storage.Name}.");
                    }
                }


                else if (a.Profession == "diplomat")
                {
                    if(a.DiplomacyCooldown > 0)
                    {
                        a.DiplomacyCooldown--;
                    }
                    else
                    {
                        int FirstDecider = Game1.r.Next(1, 100 * MonthToDayConstant);

                        if (FirstDecider == 1)
                        {
                            int Decider = Game1.r.Next(1, 9); // Randomly decide between 1 and 8 for different negotiation topics

                            // Define the base success rate and charisma effect
                            double baseSuccessRate = 0.5;
                            double charismaEffect = 0.1; // Influence of charisma on success rate

                            bool negotiationSuccess = Game1.r.NextDouble() < (baseSuccessRate + (a.Charisma * charismaEffect / (1 + (a.Charisma * charismaEffect))));

                            if (Decider == 1)
                            {
                                if (negotiationSuccess)
                                {
                                    LogEvent(a.Name + " negotiated with " + a.Location.Name + " to try to promote peace. The negotiations were very productive.");
                                }
                                else
                                {
                                    LogEvent(a.Name + " negotiated with " + a.Location.Name + " to try to promote peace. The negotiations were not very successful.");
                                }
                            }
                            else if (Decider == 2)
                            {
                                if (negotiationSuccess)
                                {
                                    LogEvent(a.Name + " negotiated with " + a.Location.Name + " to try to promote more trade. The negotiations were very productive.");
                                }
                                else
                                {
                                    LogEvent(a.Name + " negotiated with " + a.Location.Name + " to try to promote more trade. The negotiations were not very successful.");
                                }
                            }
                            else if (Decider == 3)
                            {
                                string threat;
                                string additionalInfo = "";

                                int threatType = Game1.r.Next(1, 4);
                                if (threatType == 1)
                                {
                                    threat = World.Colossals[Game1.r.Next(World.Colossals.Count)].Name;
                                }
                                else if (threatType == 2)
                                {
                                    threat = World.Blights[Game1.r.Next(World.Blights.Count)].Name;
                                }
                                else
                                {
                                    threat = World.Calamity[0].Name;
                                    additionalInfo = " and their gang of " + Game1.CalamityIdeologicalObsessionMapping[World.CalamityIdeologicalObsession];
                                }

                                if (negotiationSuccess)
                                {
                                    LogEvent(a.Name + " negotiated with " + a.Location.Name + " to deal with the threat of " + threat + additionalInfo + ". The negotiations were very productive.");
                                }
                                else
                                {
                                    LogEvent(a.Name + " negotiated with " + a.Location.Name + " to deal with the threat of " + threat + additionalInfo + ". The negotiations were not very successful.");
                                }
                            }

                            else if (Decider == 4)
                            {
                                if (negotiationSuccess)
                                {
                                    LogEvent(a.Name + " negotiated with " + a.Location.Name + " to establish better relations. The negotiations were very productive.");
                                }
                                else
                                {
                                    LogEvent(a.Name + " negotiated with " + a.Location.Name + " to establish better relations. The negotiations were not very successful.");
                                }
                            }

                            a.DiplomacyCooldown += 100; //randomness introduced by above decider not this
                        }
                    }
                }
                else if (a.Profession == "adventurer")
                {
                    if (a.AdventureCooldown > 0)
                    {
                        //wait at your current location to start your adventure...?
                        a.AdventureCooldown -= 1 / MonthToDayConstant;
                    }
                    else if (a.AdventureCooldown == 0)
                    {
                        bool pillagingStill = true;
                        int diminishingReturnCounter = 0;

                        if (AdventuringLocations.Contains(a.Location.Type))
                        {
                            while (pillagingStill)
                            {
                                int decider = Game1.r.Next(100);

                                if (decider < 5) // Decreased likelihood of continuing pillaging
                                {
                                    pillagingStill = false;
                                }
                                else if (decider < 7)
                                {
                                    List<Structure> structures = a.Location.AllStructures;
                                    Random rand = new Random();
                                    Structure selectedStructure = null;
                                    Object selectedArtifact = null;

                                    // Shuffle the list of structures to ensure randomness
                                    structures = structures.OrderBy(x => rand.Next()).ToList();

                                    foreach (var structure in structures)
                                    {
                                        if (structure.HistoricalObjects != null && structure.HistoricalObjects.Count > 0)
                                        {
                                            // Get a random artifact from the structure
                                            selectedArtifact = structure.HistoricalObjects[rand.Next(structure.HistoricalObjects.Count)];
                                            selectedStructure = structure;
                                            break; // Stop searching once we find an artifact
                                        }
                                    }

                                    if (selectedStructure != null && selectedArtifact != null)
                                    {
                                        LogEvent(a.Name + " took " + selectedArtifact + " from " + selectedStructure.Name + ".");
                                        a.Inventory.Add(selectedArtifact);
                                        selectedStructure.HistoricalObjects.Remove(selectedArtifact);
                                    }
                                }
                                else if (decider < 20) // Decreased chance of general item looting
                                {
                                    List<Object> o = World.LootTableMachine("general");
                                    a.Location.Wealth -= Game1.r.Next(50, 100);

                                    a.Inventory.AddRange(o);

                                    LogEvent(a.Name + " stole some general items from " + a.Location.Name + ".");
                                    diminishingReturnCounter++;
                                    pillagingStill = false;
                                }

                                // Implement diminishing returns
                                if (diminishingReturnCounter > 1)
                                {
                                    pillagingStill = false;
                                }
                            }

                            a.AdventureCooldown = Game1.r.Next(50, 100 + a.Age);
                        }
                        else if (VisitationLocations.Contains(a.Location.Type))
                        {
                            while (pillagingStill)
                            {
                                int decider = Game1.r.Next(100 * MonthToDayConstant);

                                List<string> shobeSubjects = new List<string>(){ "relationships", "mining",
                                "combat", "crafting", "trading", "stealth", "alchemy", "cooking", "fishing",
                                "hunting", "quests", "gathering", "imbuement", "healing", "navigation",
                                "tactics", "survival", "diplomacy", "lockpicking", "animal taming", "herbalism",
                                "herbs", "blacksmithing", "tailoring", "carpentry", "architecture",
                                "history", "sailing", "farming", "brewing", "divination",
                                "spellcasting", "negotiation", "investigation", "potions",
                                "archery", "swordsmanship", "armor crafting", "thievery",
                                "mountaineering", "cartography", "astronomy", "necromancy", "spatiomancy", "conjuromancy", "fractalmancy", "perceptomancy",
                                "beasts", "divination", "divinity", "illusion", "mechanics", "engineering" };

                                if (decider < 5) // Decreased likelihood of continuing activities
                                {
                                    District selectedDistrict = a.Location.Districts[Game1.r.Next(a.Location.Districts.Count)];

                                    if (selectedDistrict.Architects.Count > 0)
                                    {
                                        Architect selectedArch = selectedDistrict.Architects[Game1.r.Next(selectedDistrict.Architects.Count)];

                                        LogEvent(a.Name + " had a lovely chat about " + shobeSubjects[Game1.r.Next(shobeSubjects.Count)] + " in " + a.Location.Name + " with " + selectedArch.Name + ".");
                                    }
                                }
                                else if (decider < 7)
                                {
                                    List<Object> o = World.LootTableMachine("general");
                                    a.Location.Wealth += Game1.r.Next(20, 50);
                                    a.Inventory.AddRange(o);
                                    LogEvent(a.Name + " purchased some general items from " + a.Location.Name + ".");
                                }
                                else if (decider < 20)
                                {
                                    pillagingStill = false;
                                }
                            }

                            a.AdventureCooldown = Game1.r.Next(50, 100 + a.Age);
                        }






                        // Current location coordinates
                        int currentX = a.Location.Region.X;
                        int currentZ = a.Location.Region.Z;

                        // Filter locations based on whether they have been explored and their type
                        List<Location> potentialLocations = World.AllLocations
                            .Where(loc => !a.ExploredLocations.Contains(loc)
                                          && (AdventuringLocations.Contains(loc.Type) || VisitationLocations.Contains(loc.Type)))
                            .ToList();

                        if (potentialLocations.Any())
                        {
                            var chosenLocation = potentialLocations[Game1.r.Next(potentialLocations.Count)];
                            a.NextMigrationLocation = chosenLocation;

                            // Add the chosen location to the explored locations list
                            a.ExploredLocations.Add(chosenLocation);

                            LogEvent(a.Name + " travelled to " + a.NextMigrationLocation.Name + ".");
                            a.AdventureCooldown = Game1.r.Next(50, 100 + a.Age);
                        }
                    }
                }
            }
        }
    }
}
