using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Plan : Entity
    {
        private int _planLocationID;
        public Location PlanLocation
        {
            get => EntityGet<Location>(_planLocationID);
            set => _planLocationID = value?.ID ?? 0;
        }

        public int Effectiveness = 0;

        public bool AnnouncedTraveling = false;

        public double CycleForPlanInitiation;
        public EntityList<Architect> PlanInitiators;

        public bool Complete = false;

        public double AllArrivedCycle = -6048000; //setting it to this ensures plans cant be executed until cycle 0 (lmao)

        public Group StoredGroup = null;

        public List<string> ObjectiveTypes = new List<string>();
        public List<List<Entity>> ObjectiveEntities = new List<List<Entity>>(); // Changed to List<List<Entity>>

        public static List<string> PlanNames = new List<string>() { "Operation", "Initiative", "Project", "Plan", "Procedure" };

        public string CreditedName = "";

        // Updated constructor to accept List<List<Entity>> for objectiveEntities
        public Plan(Location planLocation, double cycleForPlanInitiation, EntityList<Architect> planInitiators, List<string> objectiveTypes, List<List<Entity>> objectiveEntities, string creditedName)
        {
            PlanLocation = planLocation;
            CycleForPlanInitiation = cycleForPlanInitiation;
            PlanInitiators = planInitiators;
            ObjectiveTypes = objectiveTypes;
            ObjectiveEntities = objectiveEntities;

            CreditedName = creditedName;

            Name = PlanNames[Game1.GameWorld.rnd.Next(PlanNames.Count)] + " " + Game1.GameWorld.GenerateUniqueName("1W1w", this, Game1.GameWorld.rnd);

            Effectiveness = Game1.GameWorld.rnd.Next(30, 60);
        }

        public (bool, string) Foiled()
        {
            if (ObjectiveTypes.Count != ObjectiveEntities.Count)
            {
                return (true, "incompetent planning procedures.");
            }


            if(PlanInitiators.Any(a => a.IsAlive == false))
            {
                return (true, "a plan executor death.");
            }
            else if (PlanInitiators.Any(a => a.Bound == true))
            {
                return (true, "a plan executor's imprisonment.");
            }
            else if (PlanInitiators.Count == 0)
            {
                return (true, "the plan executor(s)' mysterious disappearance.");
            }

            for (int i = 0; i < ObjectiveTypes.Count; i++)
            {
                string objectiveType = ObjectiveTypes[i];
                List<Entity> entities = ObjectiveEntities[i];

                if (PlanLocation == null)
                {
                    return (true, "incompetent navigational procedures.");
                }

                switch (objectiveType)
                {
                    case "GI":
                        // If PlanLocation is unloaded and population is 0, it's foiled
                        if (!PlanLocation.Districts.Any(d => d.IsLoaded) && PlanLocation.TruePopulation() == 0)
                        {
                            return (true, "the target location being deserted.");
                        }

                        // If PlanLocation is loaded and all present architects are in the player's active party, it's foiled
                        if (PlanLocation.Districts.Any(d => d.IsLoaded) && Game1.LoadedArchitects.All(a => Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a)))
                        {
                            return (true, "the target location being deserted.");
                        }
                        break;

                    case "CI":
                        // The second entity should be an Architect, and their location should be the first entity (a Location)
                        if (entities.Count >= 2 && entities[0] is Location location && entities[1] is Architect architect)
                        {
                            if (architect.Location != location)
                            {
                                return (true, "the target moving to a new location.");
                            }
                        }
                        break;

                    case "SVT":
                        // Check if the location is loaded
                        if (entities.Count >= 3 && entities[0] is Location targetLocation && entities[2] is Structure targetStructure)
                        {
                            if (!targetLocation.Districts.Any(d => d.IsLoaded))
                            {
                                // If not loaded, check if the historical objects list no longer contains the target object
                                var historicalObjects = targetStructure.HistoricalObjects.Where(o => o.Name != null).ToList();
                                if (!historicalObjects.Any())
                                {
                                    return (true, "the object not being in its expected place.");
                                }
                            }
                            else
                            {
                                // If loaded, check if the object is in any room, including contained objects
                                bool objectExists = targetStructure.Rooms.Any(room =>
                                    room.Objects.Any(o => o.ContainedObjects.Any(oo => oo.Name != null)));

                                if (!objectExists)
                                {
                                    return (true, "the object not being in its expected place.");
                                }
                            }
                        }
                        break;

                    case "RB":
                        // **Corrected**: The structure *being* reinforced means the plan is foiled.
                        if (entities.Count >= 2 && entities[1] is Structure reinforcedStructure)
                        {
                            if (reinforcedStructure.Reinforced)
                            {
                                return (true, "the structure's reinforcement.");
                            }
                        }
                        break;

                    case "GT":
                    case "ESP":
                        // **Corrected**: If the structure is reinforced, they can't access it, so the plan fails.
                        if (entities.Count >= 1 && entities[0] is Location targetMarketLocation)
                        {
                            if (targetMarketLocation.Market != null)
                            {
                                if (targetMarketLocation.Market.Reinforced)
                                {
                                    return (true, "the market being reinforced.");
                                }
                            }
                            else
                            {
                                if (targetMarketLocation.AllStructures.Count > 0)
                                {
                                    if (targetMarketLocation.AllStructures[0].Reinforced)
                                    {
                                        return (true, "the core structure being reinforced.");
                                    }
                                }
                                else
                                {
                                    return (true, "no structure existing.");
                                }
                            }
                        }
                        break;

                    default:
                        return (true, $"the objective type being unknown, '{objectiveType}'.");
                }
            }

            return (false, "Plan has no obstacles.");
        }

    }
}
