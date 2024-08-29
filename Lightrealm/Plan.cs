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

        public double CycleForPlanInitiation;
        public Entity PlanInitiator;

        public List<string> ObjectiveTypes = new List<string>();
        public List<List<Entity>> ObjectiveEntities = new List<List<Entity>>(); // Changed to List<List<Entity>>

        public static List<string> PlanNames = new List<string>() { "Operation", "Initiative", "Project", "Plan", "Procedure" };

        // Updated constructor to accept List<List<Entity>> for objectiveEntities
        public Plan(Location planLocation, double cycleForPlanInitiation, Entity planInitiators, List<string> objectiveTypes, List<List<Entity>> objectiveEntities)
        {
            PlanLocation = planLocation;
            CycleForPlanInitiation = cycleForPlanInitiation;
            PlanInitiator = planInitiators;
            ObjectiveTypes = objectiveTypes;
            ObjectiveEntities = objectiveEntities;

            Name = PlanNames[Game1.r.Next(PlanNames.Count)] + " " + Game1.GameWorld.GenerateUniqueName("1W1w", this);

            Effectiveness = Game1.r.Next(30, 60);
        }
    }
}
