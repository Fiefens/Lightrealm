using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Lightrealm
{
    public static class WorldSubgroupManager
    {
        public static T EntityGet<T>(int entityId) where T : Entity
        {
            if (Game1.GameWorld == null || Game1.GameWorld.EntityLedger == null)
            {
                return (T)Convert.ChangeType(Game1.TemporaryEntityLedger[entityId], typeof(T));
            }

            return (T)Convert.ChangeType(Game1.GameWorld.EntityLedger[entityId], typeof(T));
        }

        public static Dictionary<string, int> ConvertObjectiveToRequiredEntityCount = new Dictionary<string, int>()
                    {
                        { "GI", 1 },
                        { "SVT", 4 },
                        { "RB", 2 },
                        { "CI", 2 },
                        { "GT", 1 },
                        { "ESP", 1 }
                    };


        public static List<string> LegendTypes = new List<string>() { "hunter", "adventurer", "assassin", "rogue", "artisan", "diplomat", "enchanter" };

        public static List<string> AdventuringLocations = new List<string> { "archway", "hallway", "keep", "monument", "outpost", "sanctum", "stronghold", "spire", "tower"};
        public static List<string> VisitationLocations = new List<string> { "commune", "cove", "hoard", "monastery", "preserve"};

        public static void ManageLegends(World World, decimal Days)
        {
            int Month = ((int)Math.Round((decimal)(World.Cycle / 24192000)) % 12) + 1;
            int Year = (int)Math.Round((decimal)(World.Cycle / 290304000), MidpointRounding.ToZero);

            string Date = "(" + Month + "/" + Year + ")";

            int MonthToDayConstant = (int)Math.Round(28 / Days);

            double CalculateDistance(int x1, int z1, int x2, int z2)
            {
                return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(z2 - z2, 2));
            }

            void LogEvent(string data, Region r, EntityList<Entity> e)
            {
                string eventText = $"{Date} {data}";
                World.HistoricalEvents.Add(new Event(eventText, r, e));
            }

            List<Architect> LegendsToRemove = new List<Architect>();
            foreach(Architect a in World.Legends)
            {
                if (!a.IsAlive)
                {
                    LegendsToRemove.Add(a);
                }
            }
            foreach(Architect a in LegendsToRemove)
            {
                World.Legends.Remove(a);
            }



            foreach (Architect a in World.Legends)
            {
               


                if (!LegendTypes.Contains(a.Profession))
                {
                    a.Profession = LegendTypes[Game1.GameWorld.rnd.Next(7)];
                    LogEvent(a.Name + " started their journey as a " + a.Profession + ".", a.Location.Region, new EntityList<Entity>(){a});
                }

                if (Game1.GameWorld.rnd.Next(500 * MonthToDayConstant) == 1 && a.Profession != "enchanter" && a.Profession != "artisan" && a.Profession != "adventurer")
                {
                    int currentX = a.Location.Region.X;
                    int currentZ = a.Location.Region.Z;

                    List<Location> nearbyLocations = World.AllLocations.Where(loc => CalculateDistance(currentX, currentZ, loc.Region.X, loc.Region.Z) < 10).ToList();

                    if (nearbyLocations.Any())
                    {
                        a.NextMigrationLocation = nearbyLocations[Game1.GameWorld.rnd.Next(nearbyLocations.Count())];
                        LogEvent(a.Name + " migrated to " + a.NextMigrationLocation.Name + ".", a.NextMigrationLocation.Region, new EntityList<Entity>(){a, a.NextMigrationLocation});
                        a.MigrationReason = "I'm headed to " + a.NextMigrationLocation.Name + ", I don't stay in one place for long.";
                    }
                    else
                    {
                        a.NextMigrationLocation = null;
                    }
                }

                bool usePreferredTargets = Game1.GameWorld.rnd.Next(2) == 0; // 50% chance

                // Handling "hunter" profession
                if (a.Profession == "hunter")
                {
                    if (a.LegendaryTarget == null)
                    {
                        List<Architect> shuffledArchitects = Game1.ShuffleNew(World.AllHistoricalArchitects.ToList());

                        Architect targetArchitect = shuffledArchitects.FirstOrDefault(a =>
                            (a.Profession == "beast" || a.Profession == "animal" || a.Profession == "end") && !World.Calamity.Contains(a));

                        if (targetArchitect != null)
                        {
                            a.LegendaryTarget = targetArchitect;
                            LogEvent(a.Name + " started tracking " + targetArchitect.Name + ".", a.Location.Region, new EntityList<Entity>(){a, targetArchitect});
                        }
                    }
                    else
                    {
                        a.HuntingProgress += Game1.GameWorld.rnd.Next(5, 8) == 5 ? 5 : -3;

                        if (a.HuntingProgress > 100)
                        {
                            WorldActionInitiator.InitiateAction(World, "hunterfight", a, new EntityList<Entity> { a.Location, a.District, a.LegendaryTarget }, false);
                            a.LegendaryTarget = null;
                            a.HuntingProgress = 0;
                        }
                    }
                }
                // Handling "assassin" profession
                else if (a.Profession == "assassin")
                {
                    if (a.LegendaryTarget == null && Game1.GameWorld.rnd.Next(1, 25 * MonthToDayConstant) == 1)
                    {
                        EntityList<Architect> shuffledArchitects = new EntityList<Architect>(World.AllHistoricalArchitects);
                        Game1.ShuffleNew(shuffledArchitects);

                        Architect targetArchitect = null;

                        if (usePreferredTargets && a.Enemies.Any())
                        {
                            targetArchitect = shuffledArchitects.FirstOrDefault(target =>
                                a.Enemies.Contains(target) && World.HumanoidRaces.Contains(target.Race) && target.IsAlive);
                        }

                        if (targetArchitect == null)
                        {
                            targetArchitect = shuffledArchitects.FirstOrDefault(target =>
                                World.HumanoidRaces.Contains(target.Race) && target.Reputation < -20 && !World.Calamity.Contains(target) && target.IsAlive);
                        }

                        if (targetArchitect != null)
                        {
                            a.LegendaryTarget = targetArchitect;
                            LogEvent(a.Name + " started searching for information on their next target, " + targetArchitect.Name + ".", a.Location.Region, new EntityList<Entity>(){a, targetArchitect});
                        }
                    }
                    else if (a.LegendaryTarget != null)
                    {
                        a.HuntingProgress += Game1.GameWorld.rnd.Next(5, 8) == 5 ? 5 : -3;

                        if (a.HuntingProgress > 100)
                        {
                            WorldActionInitiator.InitiateAction(World, "hunterfight", a, new EntityList<Entity> { a.Location, a.District, a.LegendaryTarget }, false);
                            a.LegendaryTarget = null;
                            a.HuntingProgress = 0;
                        }
                    }
                }
                // Handling "rogue" profession
                else if (a.Profession == "rogue")
                {
                    if (a.LegendaryTarget == null)
                    {
                        var allStructuresWithObjects = World.AllLocations
                            .SelectMany(location => location.AllStructures
                                .Where(structure => structure.HistoricalObjects.Count() > 0));

                        var preferredStructuresWithObjects = a.PreferredTargetLocations()
                            .SelectMany(location => location.AllStructures
                                .Where(structure => structure.HistoricalObjects.Count() > 0));

                        var targetStructure = preferredStructuresWithObjects.Any() && usePreferredTargets
                            ? preferredStructuresWithObjects.ElementAt(Game1.GameWorld.rnd.Next(preferredStructuresWithObjects.Count()))
                            : allStructuresWithObjects.ElementAtOrDefault(Game1.GameWorld.rnd.Next(allStructuresWithObjects.Count()));

                        if (targetStructure != null)
                        {
                            var targetObject = targetStructure.HistoricalObjects[Game1.GameWorld.rnd.Next(targetStructure.HistoricalObjects.Count())];
                            a.LegendaryTarget = targetObject;
                            a.LegendaryTargetStructure = targetStructure;
                            LogEvent(a.Name + " started searching for information on the great treasure, " + targetObject.Name + ".", a.Location.Region, new EntityList<Entity>(){a, targetObject});
                        }
                    }
                    else
                    {
                        a.HuntingProgress += Game1.GameWorld.rnd.Next(5, 8) == 5 ? 5 : -3;

                        if (a.HuntingProgress > 100)
                        {
                            WorldActionInitiator.InitiateAction(World, "artifacttheft", a, new EntityList<Entity> { a.Location, a.District, a.LegendaryTargetStructure, a.LegendaryTarget }, false);
                            a.LegendaryTarget = null;
                            a.LegendaryTargetStructure = null;
                            a.HuntingProgress = 0;
                        }
                    }
                }
                // Handling "artisan" profession
                else if (a.Profession == "artisan")
                {
                    if (Game1.GameWorld.rnd.Next(1, 1000 * MonthToDayConstant) == 1 && a.Location.AllStructures.Count() > 0)
                    {
                        Object o = World.MagicalSuperLoot(5);
                        Structure Storage = a.Location.AllStructures[Game1.GameWorld.rnd.Next(a.Location.AllStructures.Count())];
                        Storage.HistoricalObjects.Add(o);
                        o.Name = World.GenerateUniqueName("1S" + Game1.GameWorld.rnd.Next(6) + "s 1W2s", o, Game1.GameWorld.rnd);

                        LogEvent(a.Name + " created a " + o.Type + " in " + a.Location.Name + ", named it " + o.Name + ", and stored it inside " + Storage.Name + ".", a.Location.Region, new EntityList<Entity>(){a, o, a.Location, Storage});
                    }
                }
                // Handling "enchanter" profession
                else if (a.Profession == "enchanter")
                {
                    if (Game1.GameWorld.rnd.Next(1, 200 * MonthToDayConstant) == 1 && a.District.GeneralItemsWeHave.Count() > 0)
                    {
                        List<string> availableItems = a.District.GeneralItemsWeHave
                            .Where(item => !item.Contains("&cont(")).ToList();

                        if (availableItems.Count() > 0)
                        {
                            string selectedItemString = availableItems[Game1.GameWorld.rnd.Next(availableItems.Count())];

                            string[] itemParts = selectedItemString.Split(new[] { ',' }, 3);
                            string itemType = itemParts[0];
                            int itemCount = int.Parse(itemParts[1]);
                            string itemMaterialsAndContained = itemParts[2];

                            if (itemCount > 1)
                            {
                                itemCount--;
                                string updatedItemString = $"{itemType},{itemCount},{itemMaterialsAndContained}";
                                a.District.GeneralItemsWeHave[a.District.GeneralItemsWeHave.IndexOf(selectedItemString)] = updatedItemString;
                            }
                            else
                            {
                                a.District.GeneralItemsWeHave.Remove(selectedItemString);
                            }

                            EntityList<Object> objects = Game1.ConvertStringToObjects(selectedItemString);
                            Object o = objects.First();

                            o.Name = World.GenerateUniqueName("1S" + Game1.GameWorld.rnd.Next(3, 9) + "s", o, Game1.GameWorld.rnd);
                            o.Rarity = "legendary";

                            Structure Storage = a.Location.AllStructures[Game1.GameWorld.rnd.Next(a.Location.AllStructures.Count())];
                            Storage.HistoricalObjects.Add(o);

                            LogEvent($"{a.Name} enchanted a {o.Type} in {a.Location.Name}, named it {o.Name}, and stored it inside {Storage.Name}.", a.Location.Region, new EntityList<Entity>(){a, o, Storage});
                        }
                    }
                }
                // Handling "diplomat" profession
                else if (a.Profession == "diplomat")
                {
                    if (a.DiplomacyCooldown > 0)
                    {
                        a.DiplomacyCooldown--;
                    }
                    else
                    {
                        if (Game1.GameWorld.rnd.Next(1, 100 * MonthToDayConstant) == 1)
                        {
                            WorldActionInitiator.InitiateAction(World, "negotiate", a, new EntityList<Entity> { a.Location }, false);
                            a.DiplomacyCooldown = 100;
                        }
                    }
                }
                // Handling "adventurer" profession
                else if (a.Profession == "adventurer")
                {
                    if (a.AdventureCooldown > 0)
                    {
                        a.AdventureCooldown -= 1 / MonthToDayConstant;
                    }
                    else if (a.AdventureCooldown == 0)
                    {
                        // Initiate the adventure action
                        WorldActionInitiator.InitiateAction(World, "adventure", a, new EntityList<Entity> { a.Location }, false);

                        int currentX = a.Location.Region.X;
                        int currentZ = a.Location.Region.Z;

                        // Filter locations based on whether they have been explored and their type
                        List<Location> potentialLocations = World.AllLocations
                            .Where(loc => !a.ExploredLocations.Contains(loc)
                                          && (AdventuringLocations.Contains(loc.Type) || VisitationLocations.Contains(loc.Type))).ToList();

                        if (potentialLocations.Any())
                        {
                            var chosenLocation = potentialLocations[Game1.GameWorld.rnd.Next(potentialLocations.Count())];
                            a.NextMigrationLocation = chosenLocation;
                            a.MigrationReason = "I am traveling to " + chosenLocation.Name + " in search of adventure.";

                            // Add the chosen location to the explored locations list
                            a.ExploredLocations.Add(chosenLocation);

                            LogEvent(a.Name + " travelled to " + a.NextMigrationLocation.Name + ".", a.NextMigrationLocation.Region, new EntityList<Entity>(){a, a.NextMigrationLocation});
                            a.AdventureCooldown = Game1.GameWorld.rnd.Next(50, 100 + a.Age);
                        }
                    }
                }
            }
        }

        public static EntityList<LocationBuilderPacket> ManageFactions(World World, decimal Days)
        {
            int Month = ((int)Math.Round((decimal)(World.Cycle / 24192000)) % 12) + 1;
            int Year = (int)Math.Round((decimal)(World.Cycle / 290304000), MidpointRounding.ToZero);
            string Date = "(" + Month + "/" + Year + ")";

            void LogEvent(string data, Region r, EntityList<Entity> e)
            {
                string eventText = $"{Date} {data}";
                World.HistoricalEvents.Add(new Event(eventText, r, e));
            }

            int MonthToDayConstant = (int)Math.Round(28 / Days);
            EntityList<LocationBuilderPacket> LBPs = new EntityList<LocationBuilderPacket>();

            foreach (Faction f in World.AllFactions)
            {

                // Branch Off
                if (Game1.GameWorld.rnd.Next(100) == 1)
                {
                    var eligibleGroups = f.SatelliteGroups.Where(g => g.Architects.Count > 5).ToList();
                    if (eligibleGroups.Any())
                    {
                        Group selectedGroup = eligibleGroups[Game1.GameWorld.rnd.Next(eligibleGroups.Count)];
                        var nonLeaderMembers = selectedGroup.Architects.Where(a => a != selectedGroup.Leader).ToList();
                        if (nonLeaderMembers.Any())
                        {
                            Architect selectedArchitect = nonLeaderMembers[Game1.GameWorld.rnd.Next(nonLeaderMembers.Count)];
                            selectedGroup.Architects.Remove(selectedArchitect);
                            Group newGroup = new Group(new EntityList<Architect> { selectedArchitect }, f.CoreValue, selectedArchitect, null);
                            f.SatelliteGroups.Add(newGroup);
                            newGroup.HomeFaction = f;
                        }
                    }
                }

                // Recruitment

                if (Game1.GameWorld.rnd.Next(75*MonthToDayConstant) == 1)
                {
                    List<Architect> EligibleArchitects = Game1.GameWorld.AllHistoricalArchitects
                        .Where(a => a.IsAlive &&
                                    !a.IsCalamity &&
                                    a.Profession != "warlock" &&
                                    a.Profession != "sorcerer" &&
                                    Game1.GameWorld.HumanoidRaces.Contains(a.Race) &&
                                    !f.SatelliteGroups.Any(g => g.Architects.Contains(a))).ToList();

                    if (EligibleArchitects.Count > 0)
                    {
                        Architect a = EligibleArchitects[Game1.GameWorld.rnd.Next(EligibleArchitects.Count)];
                        Group GroupToAddTo = f.SatelliteGroups[Game1.GameWorld.rnd.Next(f.SatelliteGroups.Count)];

                        GroupToAddTo.Architects.Add(a);
                        a.NextMigrationLocation = GroupToAddTo.Base != null ? GroupToAddTo.Base : (GroupToAddTo.HomeFaction.Base != null ? GroupToAddTo.HomeFaction.Base : Game1.GameWorld.AllLocations[Game1.GameWorld.rnd.Next(Game1.GameWorld.AllLocations.Count)]);
                        a.MigrationReason = "I am headed to " + a.NextMigrationLocation.Name + " to join " + f.Name + ".";

                        // Rewrite a.Contacts
                        a.Contacts.Clear(); // Clear previous contacts

                        // Collect all architects from the satellite groups
                        List<Architect> potentialContacts = f.SatelliteGroups
                            .SelectMany(g => g.Architects)
                            .Where(contact => contact != a) // Exclude the recruited architect
                            .ToList();

                        if (potentialContacts.Count > 0)
                        {
                            // Add 1-5 random contacts to a.Contacts
                            int contactCount = Game1.GameWorld.rnd.Next(1, Math.Min(5, potentialContacts.Count) + 1);
                            List<Architect> selectedContacts = potentialContacts
                                .OrderBy(_ => Game1.GameWorld.rnd.Next()) // Shuffle the list randomly
                                .Take(contactCount)
                                .ToList();

                            foreach (Architect contact in selectedContacts)
                            {
                                a.Contacts.Add(contact);
                            }
                        }

                        var region = f.Base?.Region ?? GroupToAddTo.HomeFaction?.Base?.Region
             ?? Game1.GameWorld.AllLocations[Game1.GameWorld.rnd.Next(Game1.GameWorld.AllLocations.Count)].Region;

                        a.Level = 4;

                        a.UpdateProficienciesToCurrentLevel();
                        a.KitOutArchitect("generic4");

                        LogEvent($"{a.Name} was recruited to {GroupToAddTo.Name} of {f.Name}.",
                                 region,
                                 new EntityList<Entity>() { a, GroupToAddTo, f });

                    }
                }

                // Generate plans based on the faction's alignment and objectives
                if (Game1.GameWorld.rnd.Next(2 * MonthToDayConstant) == 1 && f.CurrentPlan == null) //for now only one plan per faction
                {
                    bool Positive = f.CoreValue == "order" || f.CoreValue == "resistance";
                    List<string> PossiblePlans = new List<string>();

                    // Determine possible plans based on the faction's core value
                    switch (f.CoreValue.ToLower())
                    {
                        case "order":
                            PossiblePlans.AddRange(new List<string> { "GI", "CI", "ESP" });
                            Positive = true;
                            break;

                        case "resistance":
                            PossiblePlans.AddRange(new List<string> { "GI", "SVT", "GT", "ESP", "RB" });
                            Positive = true;
                            break;

                        case "enlightenment":
                            PossiblePlans.AddRange(new List<string> { "GI", "SVT", "CI", "ESP" });
                            Positive = false;
                            break;

                        case "rogue":
                            PossiblePlans.AddRange(new List<string> { "GI", "SVT", "RB", "CI", "GT", "ESP" });
                            Positive = false;
                            break;

                        default:
                            break;
                    }

                    int numObjectives = Game1.GameWorld.rnd.Next(1, 3); // Pick 1 to 2 objectives

                    // Initialize the master list with all possible locations
                    EntityList<Location> MasterLocationList = new EntityList<Location>(Game1.GameWorld.AllLocations);

                    MasterLocationList.Shuffle();



                    
                    List<(Location, List<Entity>, string)> PossibleLocationEntityObjectiveTriplets = new List<(Location, List<Entity>, string)>();




                    // Filter the master list based on the current objective
                    foreach (Location l in MasterLocationList)
                    {
                        bool isValidLocation = false;
                        List<Entity> objectiveEntitiesForCurrentObjective = new List<Entity>();

                        if (Positive)
                        {
                            if (new List<string> { "hoard", "keep", "fortress", "monastery", "monument", "outpost", "sanctum", "shadecore", "shadeoutpost", "spire", "cove", "tower", "bastion" }.Contains(l.Type))
                            {
                                if (l.Type == "fort" && l.Government != null)
                                {
                                    bool hasNegativeFaction = Game1.GameWorld.AllFactions.Any(
                                        f => (f.CoreValue == "enlightenment" || f.CoreValue == "rogue") &&
                                             f.SatelliteGroups.Contains(l.Government)
                                    );
                                    if (!hasNegativeFaction) continue;
                                }
                                isValidLocation = true;
                            }
                        }
                        else
                        {
                            if (new List<string> { "archway", "bastion", "fort", "hallway", "hoard", "isofractalcore", "isofractaloutpost", "camp", "city", "town", "village", "preserve", "sanctum", "pyramid", "spire", "toroid", "towers" }.Contains(l.Type))
                            {
                                if (l.Type == "fort" && l.Government != null)
                                {
                                    bool hasPositiveFaction = Game1.GameWorld.AllFactions.Any(
                                        f => (f.CoreValue == "order" || f.CoreValue == "resistance") &&
                                             f.SatelliteGroups.Contains(l.Government)
                                    );
                                    if (!hasPositiveFaction) continue;
                                }
                                isValidLocation = true;
                            }
                        }


                        if (isValidLocation)
                        {
                            foreach(string s in PossiblePlans)
                            {
                                switch (s)
                                {
                                    // Specific logic for each objective
                                    case "GI":
                                        objectiveEntitiesForCurrentObjective.Add(l);
                                        break;

                                    case "SVT":
                                        var structuresWithNamedObjects = l.AllStructures
                                            .Select(s => new
                                            {
                                                Structure = s,
                                                ValidObjects = s.HistoricalObjects.Where(o => o.Name != null).ToList()
                                            })
                                            .Where(x => x.ValidObjects.Count > 0)
                                            .ToList();

                                        if (structuresWithNamedObjects.Count > 0)
                                        {
                                            var rnd = Game1.GameWorld.rnd;
                                            var selected = structuresWithNamedObjects[rnd.Next(structuresWithNamedObjects.Count)];
                                            var targetStructure = selected.Structure;
                                            var targetObject = selected.ValidObjects[rnd.Next(selected.ValidObjects.Count)];
                                            var targetDistrict = targetStructure.Block.District;

                                            objectiveEntitiesForCurrentObjective.Add(l); // Location
                                            objectiveEntitiesForCurrentObjective.Add(targetDistrict); // District
                                            objectiveEntitiesForCurrentObjective.Add(targetStructure); // Structure
                                            objectiveEntitiesForCurrentObjective.Add(targetObject); // Artifact
                                        }
                                        break;


                                    case "RB":
                                        if (l.AllStructures.Count > 0)
                                        {
                                            var targetStructure = l.AllStructures[Game1.GameWorld.rnd.Next(l.AllStructures.Count)];
                                            objectiveEntitiesForCurrentObjective.Add(l); // Location
                                            objectiveEntitiesForCurrentObjective.Add(targetStructure); // Building
                                        }
                                        break;

                                    case "CI":
                                        var validArchitects = l.Districts
                                            .SelectMany(d => d.DistrictArchitects)
                                            .ToList();

                                        if (validArchitects.Count > 0)
                                        {
                                            var targetArchitect = validArchitects[Game1.GameWorld.rnd.Next(validArchitects.Count)];
                                            objectiveEntitiesForCurrentObjective.Add(l); // Location
                                            objectiveEntitiesForCurrentObjective.Add(targetArchitect); // Architect
                                        }
                                        break;

                                    case "GT":
                                        objectiveEntitiesForCurrentObjective.Add(l); // Location
                                        break;

                                    case "ESP":
                                        bool isEnemyEspBase = Game1.GameWorld.AllFactions
                                            .Any(f => f.SatelliteGroups.Contains(l.Government) &&
                                                      (f.CoreValue == "enlightenment" || f.CoreValue == "rogue"));
                                        if (isEnemyEspBase)
                                        {
                                            objectiveEntitiesForCurrentObjective.Add(l); // Location
                                        }
                                        break;

                                    default:
                                        objectiveEntitiesForCurrentObjective.Add(null);
                                        break;
                                }

                                if (objectiveEntitiesForCurrentObjective.Count > 0)
                                {
                                    PossibleLocationEntityObjectiveTriplets.Add((l, new List<Entity>(objectiveEntitiesForCurrentObjective), s));
                                }
                                objectiveEntitiesForCurrentObjective.Clear();
                            }
                        }
                    }

                    // Phase 1.5: Filter out locations who don't have the right number of Entities in their Tuple for stuff yk

                    PossibleLocationEntityObjectiveTriplets = PossibleLocationEntityObjectiveTriplets
                        .Where(tuple => tuple.Item2.Count == ConvertObjectiveToRequiredEntityCount[tuple.Item3])
                        .ToList();



                    //so now we have a list of possible objectives, locations, and entities. So now we need to sort out locations that have a ton of objective tuples at them.


                    // Phase 2: Final Location and Plan Creation
                    if (PossibleLocationEntityObjectiveTriplets.Count > 0)
                    {
                        if (Game1.GameWorld.rnd.NextDouble() < 0.5)
                        {
                            var preferred = f.PreferredTargetLocations();
                            var filtered = PossibleLocationEntityObjectiveTriplets
                                .Where(t => preferred.Contains(t.Item1))
                                .ToList();
                            if (filtered.Count > 0)
                                PossibleLocationEntityObjectiveTriplets = filtered;
                        }

                        // Pick a required starting objective type from the list of available ones in the triplets
                        var availableObjectiveTypes = PossibleLocationEntityObjectiveTriplets.Select(t => t.Item3).Distinct().ToList();
                        string requiredObjective = availableObjectiveTypes[Game1.GameWorld.rnd.Next(availableObjectiveTypes.Count)];

                        // Only keep triplets matching the required first objective
                        var requiredObjectiveTriplets = PossibleLocationEntityObjectiveTriplets
                            .Where(t => t.Item3 == requiredObjective)
                            .ToList();

                        if (requiredObjectiveTriplets.Count > 0)
                        {
                            // Attempt to build a plan starting with that required objective
                            int maxObjectiveCount = Game1.GameWorld.rnd.Next(1, 3); // Try for 2, fallback to 1
                            List<(Location, List<Entity>, string)> selectedObjectives = null;
                            Location finalLocation = null;

                            while (maxObjectiveCount > 0)
                            {
                                var groupedByLocation = requiredObjectiveTriplets
                                    .GroupBy(t => t.Item1)
                                    .Where(g => g.Count() >= maxObjectiveCount)
                                    .ToList();

                                if (groupedByLocation.Count > 0)
                                {
                                    var selectedGroup = groupedByLocation[Game1.GameWorld.rnd.Next(groupedByLocation.Count)];
                                    finalLocation = selectedGroup.Key;
                                    selectedObjectives = selectedGroup.Take(maxObjectiveCount).ToList();
                                    break;
                                }

                                maxObjectiveCount--;
                            }

                            if (selectedObjectives != null)
                            {
                                var finalObjectiveEntities = selectedObjectives
                                    .Select(t => t.Item2)
                                    .ToList();

                                var objectiveTypes = selectedObjectives
                                    .Select(t => t.Item3)
                                    .ToList();

                                if (!finalObjectiveEntities.Any(o => o.Any(e => e == null)))
                                {
                                    double cycleForPlanInitiation = Game1.GameWorld.Cycle + Game1.GameWorld.rnd.Next(
                                        24192000 * 6, // 6 months
                                        290304000     // 1 year
                                    );

                                    Group g = f.SatelliteGroups[Game1.GameWorld.rnd.Next(f.SatelliteGroups.Count)];

                                    EntityList<Architect> planInitiators = new EntityList<Architect>(g.Architects);

                                    

                                    Plan newPlan = new Plan(
                                        finalLocation,
                                        (double)Math.Round(cycleForPlanInitiation),
                                        planInitiators,
                                        objectiveTypes,
                                        finalObjectiveEntities,
                                        g.Name
                                    );

                                    newPlan.StoredGroup = g;

                                    List<string> entityNames = finalObjectiveEntities.SelectMany(x => x).Where(x => x != null).Select(x => x.Name).Distinct().ToList();

                                    foreach (var participant in planInitiators)
                                    {
                                        if (participant != planInitiators.First())
                                        {
                                            double execCycle = cycleForPlanInitiation;
                                            int y = (int)(execCycle / 290304000); execCycle %= 290304000;
                                            int m = (int)(execCycle / 24192000); execCycle %= 24192000;
                                            int d = (int)(execCycle / 864000);

                                            string dateString = $"{m + 1}/{d + 1}/{y}";

                                            string[] letterVariants = new string[]
                                            {
                            $"Meet at {finalLocation.Name} on {dateString}. '{newPlan.Name}' is set then to take place. -{string.Concat(planInitiators.First().Name.Split().Select(word => word[0]))}",
                            $"Plan '{newPlan.Name}' set. Assemble at {finalLocation.Name}, {dateString}. Our targets include {Game1.FormatAndList(entityNames)}. -{string.Concat(planInitiators.First().Name.Split().Select(word => word[0]))}",
                            $"Attend {finalLocation.Name} on {dateString}. Plan '{newPlan.Name}' targets {Game1.FormatAndList(entityNames)}. -{string.Concat(planInitiators.First().Name.Split().Select(word => word[0]))}"
                                            };

                                            string letterContent = letterVariants[Game1.GameWorld.rnd.Next(letterVariants.Length)];
                                            Letter l = new Letter(planInitiators.First(), participant, new TextStorage(letterContent, Color.LightBlue, new EntityList<Entity>() { finalLocation, newPlan }), true);
                                        }
                                    }

                                    string planObjective = objectiveTypes.Count == 1
                                        ? objectiveTypes[0] switch
                                        {
                                            "GI" => "gathering intelligence",
                                            "ESP" => "espionage",
                                            "CI" => "kidnapping",
                                            "SVT" => "stealing",
                                            "RB" => "razing",
                                            "GT" => "stealing",
                                            _ => "Unknown Objective"
                                        }
                                        : "multiple operations";

                                    string eventDetails = $"{f.Name} has devised a plan named '{newPlan.Name}' with a primary objective of {planObjective}, targeted at {Game1.FormatAndList(entityNames)}. The plan is set to be executed in {Math.Round(cycleForPlanInitiation / 290304000)}.";

                                    EntityList<Entity> involvedEntities = new EntityList<Entity>
                {
                    finalLocation,
                    f,
                    finalObjectiveEntities[0].FirstOrDefault()
                };
                                    involvedEntities.AddRange(finalObjectiveEntities.SelectMany(x => x));

                                    LogEvent(eventDetails, finalLocation.Region, involvedEntities);
                                    f.CurrentPlan = newPlan;
                                }
                            }
                        }
                    }

                }

                // Base Assignment Logic
                foreach (Group g in f.SatelliteGroups)
                {
                    if (g.Base == null)
                    {
                        if (f.Organized)
                        {
                            string type = f.SatelliteGroups[0] == g ? "bastion" : "fort";
                            List<(int, int)> possibleLocations = new List<(int, int)>();

                            foreach (Region r in World.WorldMap)
                            {
                                if (r.Location == null && r.Biome != "ocean" && r.Biome != "void" &&
                                    (type == "bastion" || Vector2.Distance(new Vector2(f.Outposts[0].Region.X, f.Outposts[0].Region.Z), new Vector2(r.X, r.Z)) < 15))
                                {
                                    possibleLocations.Add((r.X, r.Z));
                                }
                            }

                            if (possibleLocations.Count > 0)
                            {
                                (int, int) coords = possibleLocations[Game1.GameWorld.rnd.Next(possibleLocations.Count)];
                                LocationBuilderPacket l = new LocationBuilderPacket(g, coords.Item1, coords.Item2, type, Game1.GameWorld.GetRace(""), 0, 0, g.Leader.HomeLocation.HomeCivilization, new EntityList<Object>(), f.SatelliteGroups[0].Leader.HomeLocation, "");
                                LBPs.Add(l);
                            }
                        }
                        else
                        {
                            f.Base = f.SatelliteGroups[0].Leader.HomeLocation;
                        }
                    }


                    // Expel the dead

                    var architectsToRemove = new List<Architect>();

                    foreach (Architect a in g.Architects)
                    {
                        if (!a.IsAlive)
                        {
                            architectsToRemove.Add(a);
                        }
                    }

                    foreach (Architect a in architectsToRemove)
                    {
                        g.Architects.Remove(a);
                    }

                }

                if (f.CurrentPlan != null && f.CurrentPlan.Foiled().Item1)
                {
                    LogEvent("The plan " + f.CurrentPlan.Name + " was foiled due to " + f.CurrentPlan.Foiled().Item2, f.CurrentPlan.PlanLocation.Region, new EntityList<Entity>() { f.CurrentPlan });
                    f.CurrentPlan = null;
                }

                if(f.CurrentPlan != null)
                {
                    if (f.CurrentPlan.CycleForPlanInitiation - 6048000d < Game1.GameWorld.Cycle)
                    {
                        //migrate

                        if (f.CurrentPlan.AnnouncedTraveling == false)
                        {
                            if (f.CurrentPlan.PlanInitiators.Count == 1)
                            {
                                LogEvent(f.CurrentPlan.PlanInitiators.First().Name + " left for " + f.CurrentPlan.PlanLocation.Name + ", for the execution of " + f.CurrentPlan.Name + " created by " + f.Name + ".", f.CurrentPlan.PlanLocation.Region, new EntityList<Entity>() { f.CurrentPlan.PlanInitiators.First(), f.CurrentPlan.PlanLocation, f.CurrentPlan });
                            }
                            else if (f.CurrentPlan.PlanInitiators[0].Group != null)
                            {
                                LogEvent(f.CurrentPlan.StoredGroup.Name + ", led by " + f.CurrentPlan.StoredGroup.Leader.Name + " left for " + f.CurrentPlan.PlanLocation.Name + ", for the execution of " + f.CurrentPlan.Name + " created by " + f.Name + ".", f.CurrentPlan.PlanLocation.Region, new EntityList<Entity>() { f.CurrentPlan.StoredGroup, f.CurrentPlan.StoredGroup.Leader, f.CurrentPlan.PlanLocation, f.CurrentPlan });
                            }

                            f.CurrentPlan.AnnouncedTraveling = true;
                        }

                        foreach (Architect AA in f.CurrentPlan.PlanInitiators)
                        {
                            if (AA.Unit != null && AA.Unit.TargetLocation != f.CurrentPlan.PlanLocation)
                            {
                                AA.Unit.TargetLocation = f.CurrentPlan.PlanLocation;
                            }
                            else if (AA.Unit == null && (AA.Location != f.CurrentPlan.PlanLocation || AA.District != f.CurrentPlan.PlanLocation.Districts[0]))
                            {
                                AA.NextMigrationLocation = f.CurrentPlan.PlanLocation;
                                AA.MigrationReason = "My goals are beyond your understanding, traveler.";
                            }
                        }
                    }

                    if (f.CurrentPlan.CycleForPlanInitiation < Game1.GameWorld.Cycle )
                    {
                        //execute with THOSE WHO HAVE ARRIVED.

                        //test lack of arrival, this runs if the time has run out for people to arrive.


                        //temporary condition to force everyone to be there

                        bool AreTheyAllArrived = f.CurrentPlan.PlanInitiators.All(a => a.Location == f.CurrentPlan.PlanLocation);

                        if (f.CurrentPlan.AllArrivedCycle <= 0 && AreTheyAllArrived)
                        {
                            f.CurrentPlan.AllArrivedCycle = Game1.GameWorld.Cycle;
                        }
                        else if (Game1.GameWorld.Cycle > f.CurrentPlan.AllArrivedCycle + 6048000) // execute plan one week after arrival
                        {
                            Architect leader = null;
                            LogEvent(f.CurrentPlan.Name + "'s execution began at " + f.CurrentPlan.PlanLocation.Name + ".", f.CurrentPlan.PlanLocation.Region, new EntityList<Entity>() { f.CurrentPlan, f.CurrentPlan.PlanLocation });

                            while (f.CurrentPlan.ObjectiveEntities.Count > 0)
                            {
                                string currentObjectiveType = f.CurrentPlan.ObjectiveTypes[0];
                                List<Entity> currentObjectiveEntities = f.CurrentPlan.ObjectiveEntities[0];

                                if (f.CurrentPlan.ObjectiveEntities.Count != f.CurrentPlan.ObjectiveTypes.Count)
                                {
                                    break; // Safety check to avoid mismatches
                                }

                                EntityList<Entity> planTargets = new EntityList<Entity>();


                                // Perform the action and get success rate
                                int successRate = 0;
                                switch (currentObjectiveType)
                                {
                                    case "GI":
                                        f.InsightedLocations.Add(f.CurrentPlan.PlanLocation);
                                        LogEvent(f.CurrentPlan.CreditedName + ", and thus " + f.Name + ", gathered an increased insight unto " + f.CurrentPlan.PlanLocation.Name + ", to assist the execution of " + f.CurrentPlan.Name + ".", f.CurrentPlan.PlanLocation.Region, new EntityList<Entity>() { f.CurrentPlan.StoredGroup, f, f.CurrentPlan.PlanLocation, f.CurrentPlan });
                                        successRate = Game1.GameWorld.rnd.Next(0, 100);
                                        break;

                                    case "ESP":
                                        f.InsightedLocations.Add(f.CurrentPlan.PlanLocation);
                                        LogEvent(f.CurrentPlan.CreditedName + ", and thus " + f.Name + ", spied around " + f.CurrentPlan.PlanLocation.Name + ", gaining valuable sources.", f.CurrentPlan.PlanLocation.Region, new EntityList<Entity>() { f.CurrentPlan.StoredGroup, f, f.CurrentPlan.PlanLocation });
                                        successRate = Game1.GameWorld.rnd.Next(0, 100);
                                        break;

                                    case "CI":
                                        if (currentObjectiveEntities.Count >= 2)
                                        {
                                            planTargets.Add(currentObjectiveEntities[0]);
                                            planTargets.Add(((Architect)currentObjectiveEntities[1]).District);
                                            planTargets.Add(currentObjectiveEntities[1]);
                                            successRate = WorldActionInitiator.InitiateAction(Game1.GameWorld, "kidnaptarget", f.CurrentPlan.StoredGroup, planTargets, true);
                                        }
                                        break;

                                    case "SVT":
                                        if (currentObjectiveEntities.Count >= 4)
                                        {
                                            planTargets.Add(currentObjectiveEntities[0]);
                                            planTargets.Add(currentObjectiveEntities[1]);
                                            planTargets.Add(currentObjectiveEntities[2]);
                                            planTargets.Add(currentObjectiveEntities[3]);
                                            successRate = WorldActionInitiator.InitiateAction(Game1.GameWorld, "artifacttheft", f.CurrentPlan.StoredGroup, planTargets, true);
                                        }
                                        break;

                                    case "RB":
                                        if (currentObjectiveEntities.Count >= 2)
                                        {
                                            planTargets.Add(currentObjectiveEntities[0]);
                                            planTargets.Add(currentObjectiveEntities[1]);
                                            successRate = WorldActionInitiator.InitiateAction(Game1.GameWorld, "razebuilding", f.CurrentPlan.StoredGroup, planTargets, true);
                                        }
                                        break;

                                    case "GT":
                                        if (currentObjectiveEntities.Count >= 1)
                                        {
                                            planTargets.Add(currentObjectiveEntities[0]);
                                            successRate = WorldActionInitiator.InitiateAction(Game1.GameWorld, "theft", f.CurrentPlan.StoredGroup, planTargets, true);
                                        }
                                        break;

                                    default:
                                        break;
                                }

                                // Compose the report letter
                                if (leader != null)
                                {
                                    // Select a random architect from the faction's satellite groups
                                    var allArchitects = f.SatelliteGroups.SelectMany(g => g.Architects).ToList();
                                    if (allArchitects.Any())
                                    {
                                        var recipient = allArchitects[Game1.GameWorld.rnd.Next(allArchitects.Count)];

                                        string objectiveDescription = "";
                                        string actionDetails = "";
                                        List<Entity> entities = f.CurrentPlan.ObjectiveEntities[0];

                                        switch (currentObjectiveType)
                                        {
                                            case "GI":
                                                objectiveDescription = Game1.GameWorld.rnd.Next(3) switch
                                                {
                                                    0 => "gathering critical intelligence",
                                                    1 => "acquiring important information",
                                                    2 => "collecting strategic data",
                                                    _ => ""
                                                };
                                                actionDetails = $"We traveled to {entities[0]?.Name} to conduct reconnaissance and gather intelligence.";
                                                break;

                                            case "ESP":
                                                objectiveDescription = Game1.GameWorld.rnd.Next(3) switch
                                                {
                                                    0 => "conducting covert espionage",
                                                    1 => "spying on operations",
                                                    2 => "performing secret reconnaissance",
                                                    _ => ""
                                                };
                                                actionDetails = $"We infiltrated {entities[0]?.Name} to carry out espionage activities.";
                                                break;

                                            case "CI":
                                                objectiveDescription = Game1.GameWorld.rnd.Next(3) switch
                                                {
                                                    0 => "kidnapping a high-value target",
                                                    1 => "abducting a crucial individual",
                                                    2 => "removing a key figure",
                                                    _ => ""
                                                };
                                                actionDetails = $"We went to {entities[0]?.Name} to kidnap {entities[1]?.Name}, who was located in {((Architect)entities[1])?.District?.Name}.";
                                                break;

                                            case "SVT":
                                                objectiveDescription = Game1.GameWorld.rnd.Next(3) switch
                                                {
                                                    0 => "stealing a priceless artifact",
                                                    1 => "acquiring a rare treasure",
                                                    2 => "seizing a valuable relic",
                                                    _ => ""
                                                };

                                                if (entities.Count < 2)
                                                {
                                                    actionDetails = $"Our plan to thieve was foiled by unknown circumstances.";
                                                }
                                                else if (entities.Count == 2 || (entities.Count > 2 && entities[2]?.Name == entities[0]?.Name))
                                                {
                                                    actionDetails = $"We aimed to steal {entities[1]?.Name} from {entities[0]?.Name}.";
                                                }
                                                else if (entities.Count == 1)
                                                {
                                                    actionDetails = $"We aimed to steal from {entities[0]?.Name}.";
                                                }
                                                else
                                                {
                                                    actionDetails = $"We aimed to steal {entities[3]?.Name} from {entities[2]?.Name} in {entities[0]?.Name}.";
                                                }
                                                break;

                                            case "RB":
                                                objectiveDescription = Game1.GameWorld.rnd.Next(3) switch
                                                {
                                                    0 => "razing an important structure",
                                                    1 => "destroying a key building",
                                                    2 => "leveling a strategic location",
                                                    _ => ""
                                                };
                                                if (entities[1]?.Name == entities[0]?.Name)
                                                {
                                                    actionDetails = $"Our objective was to raze {entities[1]?.Name}.";
                                                }
                                                else
                                                {
                                                    actionDetails = $"Our objective was to raze {entities[1]?.Name} located in {entities[0]?.Name}.";
                                                }
                                                break;

                                            case "GT":
                                                objectiveDescription = Game1.GameWorld.rnd.Next(3) switch
                                                {
                                                    0 => "conducting general theft",
                                                    1 => "stealing vital resources",
                                                    2 => "plundering supplies",
                                                    _ => ""
                                                };
                                                actionDetails = $"We executed a theft operation in {entities[0]?.Name} to acquire essential resources.";
                                                break;

                                            default:
                                                objectiveDescription = "an unknown action";
                                                actionDetails = "Details of the operation are unavailable.";
                                                break;
                                        }

                                        string successMessage = successRate >= 50 ? Game1.GameWorld.rnd.Next(3) switch
                                        {
                                            0 => "considered a success",
                                            1 => "executed successfully",
                                            2 => "a positive move overall",
                                            _ => ""
                                        } : Game1.GameWorld.rnd.Next(3) switch
                                        {
                                            0 => "deemed a failure",
                                            1 => "unsuccessful in its goals",
                                            2 => "a disappointing result",
                                            _ => ""
                                        };

                                        string opinion = successRate >= 75
                                            ? Game1.GameWorld.rnd.Next(3) switch
                                            {
                                                0 => "The plan exceeded expectations, delivering outstanding results.",
                                                1 => "The operation was carried out flawlessly, achieving remarkable success.",
                                                2 => "The mission's execution was exemplary, setting a new standard for excellence.",
                                                _ => ""
                                            }
                                            : successRate >= 50
                                            ? Game1.GameWorld.rnd.Next(3) switch
                                            {
                                                0 => "While the results were satisfactory, there remains room for improvement.",
                                                1 => "The operation achieved its goals but could benefit from refinement.",
                                                2 => "The mission met expectations, though future plans should aim higher.",
                                                _ => ""
                                            }
                                            : Game1.GameWorld.rnd.Next(3) switch
                                            {
                                                0 => "The plan encountered setbacks and requires thorough analysis.",
                                                1 => "The operation's shortcomings highlight areas needing significant improvement.",
                                                2 => "The mission failed to meet its objectives, necessitating a reevaluation of our strategy.",
                                                _ => ""
                                            };

                                        string report = $"Dear {recipient.Name}, " +
                                                        Game1.GameWorld.rnd.Next(3) switch
                                                        {
                                                            0 => $"I write to inform you of the outcome of our recent operation, '{f.CurrentPlan.Name}'. ",
                                                            1 => $"Regarding our recent endeavor, '{f.CurrentPlan.Name}', I am writing to provide a detailed account. ",
                                                            2 => $"Allow me to update you on the results of our latest operation, '{f.CurrentPlan.Name}'. ",
                                                            _ => ""
                                                        } +
                                                        $"The plan's primary objective was {objectiveDescription}. " +
                                                        $"{actionDetails} " +
                                                        $"The operation was {successMessage}. {opinion} " +
                                                        Game1.GameWorld.rnd.Next(3) switch
                                                        {
                                                            0 => $"I hope to see you soon, Yours in strategy, {leader.Name}",
                                                            1 => $"We shall meet again soon, with respect, {leader.Name}",
                                                            2 => $"Soon we must meet to discuss further goals. Faithfully yours, {leader.Name}",
                                                            _ => ""
                                                        };


                                        Letter reportLetter = new Letter(
                                            leader,
                                            recipient,
                                            new TextStorage(report, Color.White, new EntityList<Entity>() { f.CurrentPlan.PlanLocation, f.CurrentPlan }),
                                            true
                                        );
                                    }
                                    else
                                    {
                                        return new EntityList<LocationBuilderPacket>() { }; // Handle case where there are no architects
                                    }
                                }



                                f.CurrentPlan.ObjectiveEntities.RemoveAt(0);
                                f.CurrentPlan.ObjectiveTypes.RemoveAt(0);
                            }

                            f.PlansExecutedSuccessfully++;
                            f.CurrentPlan.Complete = true;

                            foreach (Architect AA in f.CurrentPlan.PlanInitiators)
                            {
                                AA.NextMigrationLocation = AA.HomeLocation;
                                AA.MigrationReason = "I'd prefer not discuss it.";

                                if (AA.Unit != null)
                                {
                                    AA.Unit.TargetLocation = AA.HomeLocation;
                                    AA.MigrationReason = "I'd prefer not discuss it.";
                                }
                            }

                            f.CurrentPlan = null;
                        }
                    }
                }

            }


            return LBPs;
        }
    }
}
