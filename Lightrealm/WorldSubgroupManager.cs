using Microsoft.Xna.Framework;
using nFMOD;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static List<string> LegendTypes = new List<string>() { "hunter", "adventurer", "assassin", "rogue", "artisan", "diplomat", "enchanter" };

        public static List<string> AdventuringLocations = new List<string> { "archway", "hallway", "keep", "monument", "outpost", "sanctum", "stronghold", "spire", "tower"};
        public static List<string> VisitationLocations = new List<string> { "commune", "cove", "hoard", "monastery", "preserve"};

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

                void LogEvent(string data, Region r, EntityList<Entity> e)
                {
                    string eventText = $"{Date} {data}";
                    World.HistoricalEvents.Add(new Event(eventText, r, e));
                }


                if (!LegendTypes.Contains(a.Profession))
                {
                    a.Profession = LegendTypes[Game1.r.Next(7)];
                    LogEvent(a.Name + " started their journey as a " + a.Profession + ".", a.Location.Region, new EntityList<Entity>(){a});
                }

                if (Game1.r.Next(500 * MonthToDayConstant) == 1 && a.Profession != "enchanter" && a.Profession != "artisan" && a.Profession != "adventurer")
                {
                    double CalculateDistance(int x1, int z1, int x2, int z2)
                    {
                        return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(z2 - z2, 2));
                    }

                    int currentX = a.Location.Region.X;
                    int currentZ = a.Location.Region.Z;

                    var nearbyLocations = World.AllLocations
                        .Where(loc => CalculateDistance(currentX, currentZ, loc.Region.X, loc.Region.Z) < 10);

                    if (nearbyLocations.Any())
                    {
                        a.NextMigrationLocation = nearbyLocations[Game1.r.Next(nearbyLocations.Count())];
                        LogEvent(a.Name + " migrated to " + a.NextMigrationLocation.Name + ".", a.NextMigrationLocation.Region, new EntityList<Entity>(){a, a.NextMigrationLocation});
                    }
                    else
                    {
                        a.NextMigrationLocation = null;
                    }
                }

                bool usePreferredTargets = Game1.r.Next(2) == 0; // 50% chance

                // Handling "hunter" profession
                if (a.Profession == "hunter")
                {
                    if (a.LegendaryTarget == null)
                    {
                        EntityList<Architect> shuffledArchitects = Game1.ShuffleNewEL(World.AllArchitects);

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
                        a.HuntingProgress += Game1.r.Next(5, 8) == 5 ? 5 : -3;

                        if (a.HuntingProgress > 100)
                        {
                            WorldActionInitiator.InitiateAction(World, "hunterfight", a, new EntityList<Entity> { a.Location, a.District, a.LegendaryTarget });
                            a.LegendaryTarget = null;
                            a.HuntingProgress = 0;
                        }
                    }
                }
                // Handling "assassin" profession
                else if (a.Profession == "assassin")
                {
                    if (a.LegendaryTarget == null && Game1.r.Next(1, 25 * MonthToDayConstant) == 1)
                    {
                        EntityList<Architect> shuffledArchitects = new EntityList<Architect>(World.AllArchitects);
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
                        a.HuntingProgress += Game1.r.Next(5, 8) == 5 ? 5 : -3;

                        if (a.HuntingProgress > 100)
                        {
                            WorldActionInitiator.InitiateAction(World, "hunterfight", a, new EntityList<Entity> { a.Location, a.District, a.LegendaryTarget });
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
                            ? preferredStructuresWithObjects.ElementAt(Game1.r.Next(preferredStructuresWithObjects.Count()))
                            : allStructuresWithObjects.ElementAtOrDefault(Game1.r.Next(allStructuresWithObjects.Count()));

                        if (targetStructure != null)
                        {
                            var targetObject = targetStructure.HistoricalObjects[Game1.r.Next(targetStructure.HistoricalObjects.Count())];
                            a.LegendaryTarget = targetObject;
                            a.LegendaryTargetStructure = targetStructure;
                            LogEvent(a.Name + " started searching for information on the great treasure, " + targetObject.Name + ".", a.Location.Region, new EntityList<Entity>(){a, targetObject});
                        }
                    }
                    else
                    {
                        a.HuntingProgress += Game1.r.Next(5, 8) == 5 ? 5 : -3;

                        if (a.HuntingProgress > 100)
                        {
                            WorldActionInitiator.InitiateAction(World, "artifacttheft", a, new EntityList<Entity> { a.Location, a.District, a.LegendaryTargetStructure, a.LegendaryTarget });
                            a.LegendaryTarget = null;
                            a.LegendaryTargetStructure = null;
                            a.HuntingProgress = 0;
                        }
                    }
                }
                // Handling "artisan" profession
                else if (a.Profession == "artisan")
                {
                    if (Game1.r.Next(1, 1000 * MonthToDayConstant) == 1 && a.Location.AllStructures.Count() > 0)
                    {
                        Object o = World.MagicalSuperLoot(5);
                        Structure Storage = a.Location.AllStructures[Game1.r.Next(a.Location.AllStructures.Count())];
                        Storage.HistoricalObjects.Add(o);
                        o.Name = World.GenerateUniqueName("1S" + Game1.r.Next(6) + "s 1W2s", o);

                        LogEvent(a.Name + " created a " + o.Type + " in " + a.Location.Name + ", named it " + o.Name + ", and stored it inside " + Storage.Name + ".", a.Location.Region, new EntityList<Entity>(){a, o, a.Location, Storage});
                    }
                }
                // Handling "enchanter" profession
                else if (a.Profession == "enchanter")
                {
                    if (Game1.r.Next(1, 200 * MonthToDayConstant) == 1 && a.District.GeneralItemsWeHave.Count() > 0)
                    {
                        List<string> availableItems = a.District.GeneralItemsWeHave
                            .Where(item => !item.Contains("&cont(")).ToList();

                        if (availableItems.Count() > 0)
                        {
                            string selectedItemString = availableItems[Game1.r.Next(availableItems.Count())];

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

                            o.Name = World.GenerateUniqueName("1S" + Game1.r.Next(3, 9) + "s", o);
                            o.Rarity = "legendary";

                            Structure Storage = a.Location.AllStructures[Game1.r.Next(a.Location.AllStructures.Count())];
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
                        if (Game1.r.Next(1, 100 * MonthToDayConstant) == 1)
                        {
                            WorldActionInitiator.InitiateAction(World, "negotiate", a, new EntityList<Entity> { a.Location });
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
                        WorldActionInitiator.InitiateAction(World, "adventure", a, new EntityList<Entity> { a.Location });

                        int currentX = a.Location.Region.X;
                        int currentZ = a.Location.Region.Z;

                        // Filter locations based on whether they have been explored and their type
                        EntityList<Location> potentialLocations = World.AllLocations
                            .Where(loc => !a.ExploredLocations.Contains(loc)
                                          && (AdventuringLocations.Contains(loc.Type) || VisitationLocations.Contains(loc.Type)));

                        if (potentialLocations.Any())
                        {
                            var chosenLocation = potentialLocations[Game1.r.Next(potentialLocations.Count())];
                            a.NextMigrationLocation = chosenLocation;

                            // Add the chosen location to the explored locations list
                            a.ExploredLocations.Add(chosenLocation);

                            LogEvent(a.Name + " travelled to " + a.NextMigrationLocation.Name + ".", a.NextMigrationLocation.Region, new EntityList<Entity>(){a, a.NextMigrationLocation});
                            a.AdventureCooldown = Game1.r.Next(50, 100 + a.Age);
                        }
                    }
                }
            }
        }

        public static EntityList<LocationBuilderPacket> ManageFactions(World World, int Days)
        {
            int Month = ((int)Math.Round((decimal)(World.Cycle / 24192000)) % 12) + 1;
            int Year = (int)Math.Round((decimal)(World.Cycle / 290304000), MidpointRounding.ToZero);
            string Date = "(" + Month + "/" + Year + ")";

            void LogEvent(string data, Region r, EntityList<Entity> e)
            {
                string eventText = $"{Date} {data}";
                World.HistoricalEvents.Add(new Event(eventText, r, e));
            }

            int MonthToDayConstant = (28 / Days);
            EntityList<LocationBuilderPacket> LBPs = new EntityList<LocationBuilderPacket>();

            foreach (Faction f in World.AllFactions)
            {
                // Branch Off
                if (Game1.r.Next(100) == 1)
                {
                    var eligibleGroups = f.SatelliteGroups.Where(g => g.Architects.Count > 5).ToList();
                    if (eligibleGroups.Any())
                    {
                        Group selectedGroup = eligibleGroups[Game1.r.Next(eligibleGroups.Count)];
                        var nonLeaderMembers = selectedGroup.Architects.Where(a => a != selectedGroup.Leader).ToList();
                        if (nonLeaderMembers.Any())
                        {
                            Architect selectedArchitect = nonLeaderMembers[Game1.r.Next(nonLeaderMembers.Count)];
                            selectedGroup.Architects.Remove(selectedArchitect);
                            Group newGroup = new Group(new EntityList<Architect> { selectedArchitect }, f.CoreValue, selectedArchitect, null);
                            f.SatelliteGroups.Add(newGroup);
                            newGroup.HomeFaction = f;
                        }
                    }
                }

                // Recruitment

                if(Game1.r.Next(15) == 1)
                {
                    EntityList<Architect> EligibleArchitects = Game1.GameWorld.AllArchitects.Where(a => Game1.GameWorld.HumanoidRaces.Contains(a.Race) && !Game1.GameWorld.Calamity.Contains(a) && a.Profession != "warlock" && a.Profession != "sorcerer" && a.IsAlive && !f.SatelliteGroups.Any(g => g.Architects.Contains(a)));

                    if(EligibleArchitects.Count > 0)
                    {
                        Architect a = EligibleArchitects[Game1.r.Next(EligibleArchitects.Count)];
                        Group GroupToAddTo = f.SatelliteGroups[Game1.r.Next(f.SatelliteGroups.Count)];

                        GroupToAddTo.Architects.Add(a);
                        a.NextMigrationLocation = GroupToAddTo.Base != null ? GroupToAddTo.Base : GroupToAddTo.HomeFaction.Base;

                        LogEvent(a.Name + " was recruited to " + GroupToAddTo.Name + " of " + f.Name + ".", f.Base != null ? (f.Base.Region != null ? f.Base.Region : GroupToAddTo.HomeFaction.Base.Region) : null, new EntityList<Entity>(){a, GroupToAddTo, f});
                    }
                }

                // Generate plans based on the faction's alignment and objectives
                if (Game1.r.Next(30 + (f.Plans.Count * 10)) == 1)
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

                    List<string> ObjectiveTypes = new List<string>();
                    List<Tuple<Location, List<Entity>>> LocationEntityTuples = new List<Tuple<Location, List<Entity>>>();
                    Random rand = new Random();
                    int numObjectives = rand.Next(1, 4); // Pick 1 to 3 objectives
                    double probability = 1.0; // 100% chance for the first iteration

                    // Initialize the master list with all possible locations
                    EntityList<Location> MasterLocationList = new EntityList<Location>(Game1.GameWorld.AllLocations);

                    // Phase 1: Location Validation
                    for (int i = 0; i < numObjectives; i++)
                    {
                        if (rand.NextDouble() < probability)
                        {
                            // Pick a random objective type from the possible plans
                            string selectedObjective = PossiblePlans[rand.Next(PossiblePlans.Count)];
                            ObjectiveTypes.Add(selectedObjective);

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
                                    switch (selectedObjective)
                                    {
                                        // Add specific logic for each objective as needed
                                        case "GI":
                                            if (!f.Plans.Any(p => p.PlanLocation == l))
                                            {
                                                objectiveEntitiesForCurrentObjective.Add(null); // No specific entities needed
                                            }
                                            break;

                                        // Other cases remain the same
                                        // ...

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
                                        LocationEntityTuples.Add(new Tuple<Location, List<Entity>>(l, objectiveEntitiesForCurrentObjective));
                                    }
                                }
                            }

                            if (LocationEntityTuples.Count == 0) break;

                            probability *= 0.4;
                        }
                    }

                    // Phase 2: Final Location and Plan Creation
                    if (LocationEntityTuples.Count > 0 && ObjectiveTypes.Count == LocationEntityTuples.Count)
                    {
                        // 80% chance to prefer target locations
                        if (rand.NextDouble() < 0.8)
                        {
                            EntityHashSet<Location> preferredLocations = f.PreferredTargetLocations();
                            var filteredTuples = LocationEntityTuples
                                .Where(t => preferredLocations.Contains(t.Item1))
                                .ToList();

                            // If the filtered list is empty, default back to the original LocationEntityTuples
                            if (filteredTuples.Count > 0)
                            {
                                LocationEntityTuples = filteredTuples;
                            }
                        }

                        // Pick a random location
                        var selectedTuple = LocationEntityTuples[rand.Next(LocationEntityTuples.Count)];

                        List<List<Entity>> finalObjectiveEntities = new List<List<Entity>>();

                        for (int i = 0; i < ObjectiveTypes.Count; i++)
                        {
                            finalObjectiveEntities.Add(selectedTuple.Item2);
                        }

                        double cycleForPlanInitiation = Game1.GameWorld.Cycle + rand.Next(
                            24192000 * 3, // 3 months
                            290304000 * 3  // 3 years
                        );

                        Entity planInitiator = f.SatelliteGroups[Game1.r.Next(f.SatelliteGroups.Count)];


                        // Generate the historical event for the created plan
                        string planObjective = ObjectiveTypes.First() switch
                        {
                            "GI" => "gathering intelligence",
                            "ESP" => "espionage",
                            "CI" => "kidnapping",
                            "SVT" => "stealing",
                            "RB" => "razing",
                            "GT" => "stealing",
                            _ => "Unknown Objective"
                        };

                        List<string> entityNames = finalObjectiveEntities[0].Select(entity => entity.Name).ToList();




                        Plan newPlan = new Plan(
                            selectedTuple.Item1,
                            (double)Math.Round(cycleForPlanInitiation),
                            planInitiator,
                            ObjectiveTypes,
                            finalObjectiveEntities
                        );

                        string eventDetails = $"{f.Name} has devised a plan named '{newPlan.Name}' with a primary objective of {planObjective}, targetted at {Game1.FormatAndList(entityNames)}. The plan is set to be executed in {Math.Round(cycleForPlanInitiation / 290304000)}.";

                        // Log the historical event
                        EntityList<Entity> involvedEntities = new EntityList<Entity>
                        {
                            selectedTuple.Item1, // The location
                            f,                   // The faction
                            selectedTuple.Item2.First() // The primary target entity
                        };

                        // Add all entities involved in the objective
                        involvedEntities.AddRange(finalObjectiveEntities[0]);

                        // Log the historical event with the correct entities
                        LogEvent(eventDetails, selectedTuple.Item1.Region, involvedEntities);

                        f.Plans.Add(newPlan);
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
                                (int, int) coords = possibleLocations[Game1.r.Next(possibleLocations.Count)];
                                LocationBuilderPacket l = new LocationBuilderPacket(g, coords.Item1, coords.Item2, type, Game1.GameWorld.GetRace(""), 0, 0, g.Leader.HomeLocation.HomeCivilization, new EntityList<Object>(), f.SatelliteGroups[0].Leader.HomeLocation, "");
                                LBPs.Add(l);
                            }
                        }
                        else
                        {
                            f.Base = f.SatelliteGroups[0].Leader.HomeLocation;
                        }
                    }
                }

                // Execute Plans
                foreach (Plan p in f.Plans.ToList())
                {
                    if (p.CycleForPlanInitiation < Game1.GameWorld.Cycle)
                    {
                        while (p.ObjectiveEntities.Count > 0)
                        {
                            string currentObjectiveType = p.ObjectiveTypes[0];
                            List<Entity> currentObjectiveEntities = p.ObjectiveEntities[0];

                            if (p.ObjectiveEntities.Count != p.ObjectiveTypes.Count)
                            {
                                break; // Safety check to avoid mismatches
                            }

                            EntityList<Entity> planTargets = new EntityList<Entity>();
                            Entity leader = null;

                            if (p.PlanInitiator is Architect a)
                            {
                                leader = a;
                                LogEvent(a.Name + " left for " + p.PlanLocation.Name + ", for the execution of " + p.Name + " created by " + f.Name + ".", p.PlanLocation.Region, new EntityList<Entity>(){a, p.PlanLocation, p});
                            }
                            else if (p.PlanInitiator is Group g)
                            {
                                leader = g.Leader;
                                LogEvent(g.Name + ", led by " + g.Leader.Name + " left for " + p.PlanLocation.Name + ", for the execution of " + p.Name + " created by " + f.Name + ".", p.PlanLocation.Region, new EntityList<Entity>(){g, g.Leader, p.PlanLocation, p});
                            }

                            switch (currentObjectiveType)
                            {
                                case "GI":
                                    f.InsightedLocations.Add(p.PlanLocation);
                                    LogEvent(p.PlanInitiator.Name + ", and thus " + f.Name + ", gathered an increased insight unto " + p.PlanLocation.Name + ", to assist the execution of " + p.Name + ".", p.PlanLocation.Region, new EntityList<Entity>(){p.PlanInitiator, f, p.PlanLocation, p});
                                    break;

                                case "ESP":
                                    f.InsightedLocations.Add(p.PlanLocation);
                                    LogEvent(p.PlanInitiator.Name + ", and thus " + f.Name + ", spied around " + p.PlanLocation.Name + ", gaining valuable sources.", p.PlanLocation.Region, new EntityList<Entity>(){p.PlanInitiator, f, p.PlanLocation});
                                    break;

                                case "CI":
                                    if (currentObjectiveEntities.Count >= 2)
                                    {
                                        planTargets.Add(currentObjectiveEntities[0]);
                                        planTargets.Add(((Architect)currentObjectiveEntities[1]).District);
                                        planTargets.Add(currentObjectiveEntities[1]);
                                        WorldActionInitiator.InitiateAction(Game1.GameWorld, "kidnaptarget", p.PlanInitiator, planTargets);
                                    }
                                    break;

                                case "SVT":
                                    if (currentObjectiveEntities.Count >= 4)
                                    {
                                        planTargets.Add(currentObjectiveEntities[0]);
                                        planTargets.Add(currentObjectiveEntities[1]);
                                        planTargets.Add(currentObjectiveEntities[2]);
                                        planTargets.Add(currentObjectiveEntities[3]);
                                        WorldActionInitiator.InitiateAction(Game1.GameWorld, "artifacttheft", p.PlanInitiator, planTargets);
                                    }
                                    break;

                                case "RB":
                                    if (currentObjectiveEntities.Count >= 2)
                                    {
                                        planTargets.Add(currentObjectiveEntities[0]);
                                        planTargets.Add(currentObjectiveEntities[1]);
                                        WorldActionInitiator.InitiateAction(Game1.GameWorld, "razebuilding", p.PlanInitiator, planTargets);
                                    }
                                    break;

                                case "GT":
                                    if (currentObjectiveEntities.Count >= 1)
                                    {
                                        planTargets.Add(currentObjectiveEntities[0]);
                                        WorldActionInitiator.InitiateAction(Game1.GameWorld, "theft", p.PlanInitiator, planTargets);
                                    }
                                    break;

                                default:
                                    break;
                            }

                            p.ObjectiveEntities.RemoveAt(0);
                            p.ObjectiveTypes.RemoveAt(0);
                        }

                        f.Plans.Remove(p);
                    }
                }
            }


            return LBPs;
        }
    }
}
