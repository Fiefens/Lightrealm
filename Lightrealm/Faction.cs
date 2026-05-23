using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Faction : Entity
    {
        public static List<string> ValueList = new List<string>() { "order", "resistance", "enlightenment", "rogue" };
        public static List<string> ResourceList = new List<string>() { "impoverished", "mercenary", "soldier", "magical" };
        // order factions tend to clash with resistance and outlaw
        // outlaw factions tend to clash with enlightenment

        public string TargetedIndustry = "";

        public string Description = "";

        public int PlansExecutedSuccessfully = 0;

        public EntityList<Location> InsightedLocations = new EntityList<Location>();

        public Plan CurrentPlan = null;

        public bool Organized = false;
        public string CoreValue = ValueList[Game1.GameWorld.rnd.Next(ValueList.Count)];
        public string ResourceValue = "";

        public EntityList<Group> SatelliteGroups = new EntityList<Group>();
        public Location Base;

        public EntityList<Location> Outposts = new EntityList<Location>();

        public Faction(Architect Leader, bool Organized)
        {
            this.Organized = Organized;
            this.ResourceValue = ResourceList[Game1.GameWorld.rnd.Next(4)];
            this.CoreValue = ValueList[Game1.GameWorld.rnd.Next(4)];

            Group g = new Group(new EntityList<Architect>() { Leader }, CoreValue, Leader, null);
            g.HomeFaction = this;
            SatelliteGroups.Add(g);

            this.Name = Game1.GameWorld.GenerateUniqueName("1S" + Game1.GameWorld.rnd.Next(2, 5) + "s1w", this, Game1.GameWorld.rnd);
        }
    }
}
