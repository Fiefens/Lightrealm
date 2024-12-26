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

            foreach (Architect a in World.Legends)
            {
                if (!a.IsAlive)
                {
                    continue;
                }


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
                        EntityList<Architect> shuffledArchitects = Game1.ShuffleNewEL(World.AllHistoricalArchitects.ToEntityList());

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
                            WorldActionInitiator.InitiateAction(World, "hunterfight", a, new EntityList<Entity> { a.Location, a.District, a.LegendaryTarget });
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
                        List<Location> potentialLocations = World.AllLocations
                            .Where(loc => !a.ExploredLocations.Contains(loc)
                                          && (AdventuringLocations.Contains(loc.Type) || VisitationLocations.Contains(loc.Type))).ToList();

                        if (potentialLocations.Any())
                        {
                            var chosenLocation = potentialLocations[Game1.GameWorld.rnd.Next(potentialLocations.Count())];
                            a.NextMigrationLocation = chosenLocation;

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

                if (Game1.GameWorld.rnd.Next(75) == 1)
                {
                    EntityList<Architect> EligibleArchitects = Game1.GameWorld.AllHistoricalArchitects
                        .ToEntityList()
                        .Where(a => Game1.GameWorld.HumanoidRaces.Contains(a.Race) &&
                                    !Game1.GameWorld.Calamity.Contains(a) &&
                                    a.Profession != "warlock" &&
                                    a.Profession != "sorcerer" &&
                                    a.IsAlive &&
                                    !f.SatelliteGroups.Any(g => g.Architects.Contains(a)));

                    if (EligibleArchitects.Count > 0)
                    {
                        Architect a = EligibleArchitects[Game1.GameWorld.rnd.Next(EligibleArchitects.Count)];
                        Group GroupToAddTo = f.SatelliteGroups[Game1.GameWorld.rnd.Next(f.SatelliteGroups.Count)];

                        GroupToAddTo.Architects.Add(a);
                        a.NextMigrationLocation = GroupToAddTo.Base != null ? GroupToAddTo.Base : GroupToAddTo.HomeFaction.Base;

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

                        LogEvent($"{a.Name} was recruited to {GroupToAddTo.Name} of {f.Name}.",
                                 region,
                                 new EntityList<Entity>() { a, GroupToAddTo, f });

                    }
                }


                // Generate plans based on the faction's alignment and objectives
                if (Game1.GameWorld.rnd.Next(5 + (f.Plans.Count * 5)) == 1)
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
                    int numObjectives = Game1.GameWorld.rnd.Next(1, 4); // Pick 1 to 3 objectives
                    double probability = 1.0; // 100% chance for the first iteration

                    // Initialize the master list with all possible locations
                    EntityList<Location> MasterLocationList = new EntityList<Location>(Game1.GameWorld.AllLocations);

                    // Phase 1: Location Validation
                    for (int i = 0; i < numObjectives; i++)
                    {
                        if (Game1.GameWorld.rnd.NextDouble() < probability)
                        {
                            // Pick a random objective type from the possible plans
                            string selectedObjective = PossiblePlans[Game1.GameWorld.rnd.Next(PossiblePlans.Count)];
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
                                        // Specific logic for each objective
                                        case "GI":
                                            if (!f.Plans.Any(p => p.PlanLocation == l))
                                            {
                                                objectiveEntitiesForCurrentObjective.Add(null); // No specific entities needed
                                            }
                                            break;

                                        case "SVT":
                                            var validStructuresWithObjects = l.AllStructures
                                                .Where(s => s.HistoricalObjects.Any(o => o.Name != null))
                                                .Select(s => new
                                                {
                                                    Structure = s,
                                                    ValidObjects = s.HistoricalObjects.Where(o => o.Name != null).ToList()
                                                })
                                                .ToList();

                                            if (validStructuresWithObjects.Count > 0)
                                            {
                                                var selectedStructureWithObjects = validStructuresWithObjects[Game1.GameWorld.rnd.Next(validStructuresWithObjects.Count)];
                                                var targetStructure = selectedStructureWithObjects.Structure;
                                                var targetObject = selectedStructureWithObjects.ValidObjects[Game1.GameWorld.rnd.Next(selectedStructureWithObjects.ValidObjects.Count)];
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
                                                .SelectMany(d => d.Architects)
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
                        if (Game1.GameWorld.rnd.NextDouble() < 0.8)
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
                        var selectedTuple = LocationEntityTuples[Game1.GameWorld.rnd.Next(LocationEntityTuples.Count)];

                        List<List<Entity>> finalObjectiveEntities = new List<List<Entity>>();

                        for (int i = 0; i < ObjectiveTypes.Count; i++)
                        {
                            finalObjectiveEntities.Add(selectedTuple.Item2);
                        }

                        double cycleForPlanInitiation = Game1.GameWorld.Cycle + Game1.GameWorld.rnd.Next(
                            24192000 * 36, // 3 months
                            290304000 * 3  // 3 years
                        );

                        Group planInitiator = f.SatelliteGroups[Game1.GameWorld.rnd.Next(f.SatelliteGroups.Count)];


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


                        foreach (var participant in planInitiator.Architects) // Assuming 'Members' contains participants
                        {
                            if (participant != planInitiator.Leader)
                            {
                                // Convert cycle to readable date
                                double executionCycle = cycleForPlanInitiation;
                                int years = (int)(executionCycle / 290304000);
                                executionCycle %= 290304000;
                                int months = (int)(executionCycle / 24192000);
                                executionCycle %= 24192000;
                                int weeks = (int)(executionCycle / 6048000);
                                executionCycle %= 6048000;
                                int days = (int)(executionCycle / 864000);

                                string dateString = $"{months + 1}/{days + 1}/{years}";

                                // Generate short letter variations
                                string[] letterVariants = new string[]
                                {
                                    $"Meet at {selectedTuple.Item1.Name} on {dateString}. '{newPlan.Name}' is set then to take place. -{string.Concat(planInitiator.Leader.Name.Split().Select(word => word[0]))}",
                                    $"Plan '{newPlan.Name}' set. Assemble at {selectedTuple.Item1.Name}, {dateString}. Our targets include {Game1.FormatAndList(entityNames)}. -{string.Concat(planInitiator.Leader.Name.Split().Select(word => word[0]))}",
                                    $"Attend {selectedTuple.Item1.Name} on {dateString}. Plan '{newPlan.Name}' targets {Game1.FormatAndList(entityNames)}. -{string.Concat(planInitiator.Leader.Name.Split().Select(word => word[0]))}"
                                };

                                // Randomly select one of the letter variants
                                string letterContent = letterVariants[new Random().Next(letterVariants.Length)];

                                // Send the letter
                                Letter l = new Letter(planInitiator.Leader, participant, new TextStorage(letterContent, Color.LightBlue, new EntityList<Entity>() { selectedTuple.Item1, newPlan }), true);

                            }
                        }

                        string eventDetails = $"{f.Name} has devised a plan named '{newPlan.Name}' with a primary objective of {planObjective}, targeted at {Game1.FormatAndList(entityNames)}. The plan is set to be executed in {Math.Round(cycleForPlanInitiation / 290304000)}.";

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
                }

                // Execute Plans
                foreach (Plan p in f.Plans.ToList())
                {
                    if (p.CycleForPlanInitiation < Game1.GameWorld.Cycle)
                    {
                        Architect leader = null;
                        if (p.PlanInitiator is Architect a)
                        {
                            leader = a;
                            LogEvent(a.Name + " left for " + p.PlanLocation.Name + ", for the execution of " + p.Name + " created by " + f.Name + ".", p.PlanLocation.Region, new EntityList<Entity>() { a, p.PlanLocation, p });
                        }
                        else if (p.PlanInitiator is Group g)
                        {
                            leader = g.Leader;
                            LogEvent(g.Name + ", led by " + g.Leader.Name + " left for " + p.PlanLocation.Name + ", for the execution of " + p.Name + " created by " + f.Name + ".", p.PlanLocation.Region, new EntityList<Entity>() { g, g.Leader, p.PlanLocation, p });
                        }

                        while (p.ObjectiveEntities.Count > 0)
                        {
                            string currentObjectiveType = p.ObjectiveTypes[0];
                            List<Entity> currentObjectiveEntities = p.ObjectiveEntities[0];

                            if (p.ObjectiveEntities.Count != p.ObjectiveTypes.Count)
                            {
                                break; // Safety check to avoid mismatches
                            }

                            EntityList<Entity> planTargets = new EntityList<Entity>();


                            // Perform the action and get success rate
                            int successRate = 0;
                            switch (currentObjectiveType)
                            {
                                case "GI":
                                    f.InsightedLocations.Add(p.PlanLocation);
                                    LogEvent(p.PlanInitiator.Name + ", and thus " + f.Name + ", gathered an increased insight unto " + p.PlanLocation.Name + ", to assist the execution of " + p.Name + ".", p.PlanLocation.Region, new EntityList<Entity>() { p.PlanInitiator, f, p.PlanLocation, p });
                                    successRate = Game1.GameWorld.rnd.Next(0, 100);
                                    break;

                                case "ESP":
                                    f.InsightedLocations.Add(p.PlanLocation);
                                    LogEvent(p.PlanInitiator.Name + ", and thus " + f.Name + ", spied around " + p.PlanLocation.Name + ", gaining valuable sources.", p.PlanLocation.Region, new EntityList<Entity>() { p.PlanInitiator, f, p.PlanLocation });
                                    successRate = Game1.GameWorld.rnd.Next(0, 100);
                                    break;

                                case "CI":
                                    if (currentObjectiveEntities.Count >= 2)
                                    {
                                        planTargets.Add(currentObjectiveEntities[0]);
                                        planTargets.Add(((Architect)currentObjectiveEntities[1]).District);
                                        planTargets.Add(currentObjectiveEntities[1]);
                                        successRate = WorldActionInitiator.InitiateAction(Game1.GameWorld, "kidnaptarget", p.PlanInitiator, planTargets);
                                    }
                                    break;

                                case "SVT":
                                    if (currentObjectiveEntities.Count >= 4)
                                    {
                                        planTargets.Add(currentObjectiveEntities[0]);
                                        planTargets.Add(currentObjectiveEntities[1]);
                                        planTargets.Add(currentObjectiveEntities[2]);
                                        planTargets.Add(currentObjectiveEntities[3]);
                                        successRate = WorldActionInitiator.InitiateAction(Game1.GameWorld, "artifacttheft", p.PlanInitiator, planTargets);
                                    }
                                    break;

                                case "RB":
                                    if (currentObjectiveEntities.Count >= 2)
                                    {
                                        planTargets.Add(currentObjectiveEntities[0]);
                                        planTargets.Add(currentObjectiveEntities[1]);
                                        successRate = WorldActionInitiator.InitiateAction(Game1.GameWorld, "razebuilding", p.PlanInitiator, planTargets);
                                    }
                                    break;

                                case "GT":
                                    if (currentObjectiveEntities.Count >= 1)
                                    {
                                        planTargets.Add(currentObjectiveEntities[0]);
                                        successRate = WorldActionInitiator.InitiateAction(Game1.GameWorld, "theft", p.PlanInitiator, planTargets);
                                    }
                                    break;

                                default:
                                    break;
                            }

                            // Compose the report letter
                            if (leader != null && (leader is Architect || leader is Group))
                            {
                                // Select a random architect from the faction's satellite groups
                                var allArchitects = f.SatelliteGroups.SelectMany(g => g.Architects).ToList();
                                if (allArchitects.Any())
                                {
                                    var recipient = allArchitects[Game1.GameWorld.rnd.Next(allArchitects.Count)];

                                    string objectiveDescription = "";
                                    string actionDetails = "";
                                    List<Entity> entities = p.ObjectiveEntities[0];

                                    switch (currentObjectiveType)
                                    {
                                        case "GI":
                                            objectiveDescription = Game1.GameWorld.rnd.Next(3) switch
                                            {
                                                0 => "gathering critical intelligence",
                                                1 => "acquiring important information",
                                                2 => "collecting strategic data"
                                            };
                                            actionDetails = $"We traveled to {entities[0]?.Name} to conduct reconnaissance and gather intelligence.";
                                            break;

                                        case "ESP":
                                            objectiveDescription = Game1.GameWorld.rnd.Next(3) switch
                                            {
                                                0 => "conducting covert espionage",
                                                1 => "spying on operations",
                                                2 => "performing secret reconnaissance"
                                            };
                                            actionDetails = $"We infiltrated {entities[0]?.Name} to carry out espionage activities.";
                                            break;

                                        case "CI":
                                            objectiveDescription = Game1.GameWorld.rnd.Next(3) switch
                                            {
                                                0 => "kidnapping a high-value target",
                                                1 => "abducting a crucial individual",
                                                2 => "removing a key figure"
                                            };
                                            actionDetails = $"We went to {entities[0]?.Name} to kidnap {entities[1]?.Name}, who was located in {((Architect)entities[1])?.District?.Name}.";
                                            break;

                                        case "SVT":
                                            objectiveDescription = Game1.GameWorld.rnd.Next(3) switch
                                            {
                                                0 => "stealing a priceless artifact",
                                                1 => "acquiring a rare treasure",
                                                2 => "seizing a valuable relic"
                                            };
                                            if (entities.Count <= 2 || entities[2]?.Name == entities[0]?.Name)
                                            {
                                                // Structure and Location have the same name
                                                actionDetails = $"We aimed to steal {entities[3]?.Name} from {entities[2]?.Name}.";
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
                                                2 => "leveling a strategic location"
                                            };
                                            if (entities[1]?.Name == entities[0]?.Name)
                                            {
                                                // Structure and Location have the same name
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
                                                2 => "plundering supplies"
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
                                        2 => "a positive move overall"
                                    } : Game1.GameWorld.rnd.Next(3) switch
                                    {
                                        0 => "deemed a failure",
                                        1 => "unsuccessful in its goals",
                                        2 => "a disappointing result"
                                    };

                                    string opinion = successRate >= 75
                                        ? Game1.GameWorld.rnd.Next(3) switch
                                        {
                                            0 => "The plan exceeded expectations, delivering outstanding results.",
                                            1 => "The operation was carried out flawlessly, achieving remarkable success.",
                                            2 => "The mission's execution was exemplary, setting a new standard for excellence."
                                        }
                                        : successRate >= 50
                                        ? Game1.GameWorld.rnd.Next(3) switch
                                        {
                                            0 => "While the results were satisfactory, there remains room for improvement.",
                                            1 => "The operation achieved its goals but could benefit from refinement.",
                                            2 => "The mission met expectations, though future plans should aim higher."
                                        }
                                        : Game1.GameWorld.rnd.Next(3) switch
                                        {
                                            0 => "The plan encountered setbacks and requires thorough analysis.",
                                            1 => "The operation's shortcomings highlight areas needing significant improvement.",
                                            2 => "The mission failed to meet its objectives, necessitating a reevaluation of our strategy."
                                        };

                                    string report = $"Dear {recipient.Name}, " +
                                                    Game1.GameWorld.rnd.Next(3) switch
                                                    {
                                                        0 => $"I write to inform you of the outcome of our recent operation, '{p.Name}'. ",
                                                        1 => $"Regarding our recent endeavor, '{p.Name}', I am writing to provide a detailed account. ",
                                                        2 => $"Allow me to update you on the results of our latest operation, '{p.Name}'. "
                                                    } +
                                                    $"The plan's primary objective was {objectiveDescription}. " +
                                                    $"{actionDetails} " +
                                                    $"The operation was {successMessage}. {opinion} " +
                                                    Game1.GameWorld.rnd.Next(3) switch
                                                    {
                                                        0 => $"I hope to see you soon, Yours in strategy, {leader.Name}",
                                                        1 => $"We shall meet again soon, with respect, {leader.Name}",
                                                        2 => $"Soon we must meet to discuss further goals. Faithfully yours, {leader.Name}"
                                                    };

                                    Letter reportLetter = new Letter(
                                        leader,
                                        recipient,
                                        new TextStorage(report, Color.White, new EntityList<Entity>() { p.PlanLocation, p }),
                                        true
                                    );
                                }
                                else
                                {
                                    return new EntityList<LocationBuilderPacket>() { }; // Handle case where there are no architects
                                }
                            }



                            p.ObjectiveEntities.RemoveAt(0);
                            p.ObjectiveTypes.RemoveAt(0);
                        }

                        f.PlansExecutedSuccessfully++;

                        f.Plans.Remove(p);
                    }
                }

            }


            return LBPs;
        }
    }
}
