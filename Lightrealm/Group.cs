using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Group : Entity
    {
        public static T Entity<T>(int entityId) where T : Entity
        {
            if (Game1.GameWorld == null || Game1.GameWorld.AllEntities == null)
            {
                return (T)Convert.ChangeType(Game1.TemporaryEntities[entityId], typeof(T));
            }

            return (T)Convert.ChangeType(Game1.GameWorld.AllEntities[entityId], typeof(T));
        }

        private List<int> _architects = new List<int>();
        public EntityList<Architect> Architects
        {
            get => new EntityList<Architect>(_architects.Select(id => Entity<Architect>(id)).ToList());
            set => _architects = value.Select(e => e.ID).ToList();
        }

        private List<int> _architectsToRemove = new List<int>();
        public EntityList<Architect> ArchitectsToRemove
        {
            get => new EntityList<Architect>(_architectsToRemove.Select(id => Entity<Architect>(id)).ToList());
            set => _architectsToRemove = value.Select(e => e.ID).ToList();
        }

        private int _leaderId;
        public Architect Leader
        {
            get => Entity<Architect>(_leaderId);
            set => _leaderId = value?.ID ?? 0;
        }

        public string Type { get; set; }

        public double CycleLastTraded { get; set; } = 0;
        public double CycleLastMoved { get; set; } = 0;

        public int Stability { get; set; } = 50; // 100 is a very solid group. 0 is cause for disbandment.
        public int Reputation { get; set; } = 0; // The better a group is, the more likely it will attack and destroy bad groups, the less likely it will be attacked regardless of its evil reputation, and the more stable it is.

        private double _daysOld; // Store age in days

        public int MonthsOld
        {
            get => (int)(_daysOld / 28.0); // Return the age in months
            set => _daysOld = value * 28.0; // Allow setting age in months, if needed
        }

        public double DaysOld
        {
            get => _daysOld; // Accessor for the age in days
            set => _daysOld = value; // Mutator for the age in days
        }

        private int _baseId;
        public Location Base
        {
            get => Entity<Location>(_baseId);
            set => _baseId = value?.ID ?? 0;
        }

        private List<int> _enemies = new List<int>();
        public EntityList<Architect> Enemies
        {
            get => new EntityList<Architect>(_enemies.Select(id => Entity<Architect>(id)).ToList());
            set => _enemies = value.Select(e => e.ID).ToList();
        }

        private List<int> _architectsWhoDeclined = new List<int>();
        public EntityList<Architect> ArchitectsWhoDeclined
        {
            get => new EntityList<Architect>(_architectsWhoDeclined.Select(id => Entity<Architect>(id)).ToList());
            set => _architectsWhoDeclined = value.Select(e => e.ID).ToList();
        }

        public List<string> CaravanItems { get; set; } = new List<string>();

        private List<int> _groupsIKnowAbout = new List<int>();
        public EntityList<Group> GroupsIKnowAbout
        {
            get => new EntityList<Group>(_groupsIKnowAbout.Select(id => Entity<Group>(id)).ToList());
            set => _groupsIKnowAbout = value.Select(e => e.ID).ToList();
        }

        private List<int> _tradeRoute = new List<int>();
        public EntityList<Location> TradeRoute
        {
            get => new EntityList<Location>(_tradeRoute.Select(id => Entity<Location>(id)).ToList());
            set => _tradeRoute = value.Select(e => e.ID).ToList();
        }

        public int MaxTradeRouteLength { get; set; } = Game1.r.Next(3, 10);
        public bool WaitingForCooldownToTrade { get; set; } = false;

        public int MoralCompass { get; set; } = 0;
        public int StabilityCompass { get; set; } = 0;

        public int Wealth { get; set; } = 0;

        public int PropertyValue { get; set; } = 0;
        public int FamilyValue { get; set; } = 0;
        public int PowerValue { get; set; } = 0;
        public int MoneyValue { get; set; } = 0;
        public int KnowledgeValue { get; set; } = 0;
        public int SpiritualityValue { get; set; } = 0;
        public int ProwessValue { get; set; } = 0;
        public int PatriotismValue { get; set; } = 0;
        public int CourageValue { get; set; } = 0;
        public int CreativityValue { get; set; } = 0;


        public string CaravanCatalogue()
        {
            Dictionary<string, int> itemCounts = new Dictionary<string, int>();

            // Count the occurrences of each type
            foreach (string item in CaravanItems)
            {
                string[] itemParts = item.Split(',');
                string itemTypeString = itemParts[0];
                int itemCount = int.Parse(itemParts[1]);

                if (itemCounts.ContainsKey(itemTypeString))
                {
                    itemCounts[itemTypeString] += itemCount;
                }
                else
                {
                    itemCounts[itemTypeString] = itemCount;
                }
            }

            // Create a formatted string for the catalogue
            StringBuilder catalogueBuilder = new StringBuilder();
            foreach (var pair in itemCounts)
            {
                catalogueBuilder.Append($"{pair.Key} {pair.Value} ");
            }

            return catalogueBuilder.ToString().Trim();
        }

        public Group(EntityList<Architect> architects, string type, Architect leader, Location Basee)
        {
            Name = leader.Location.Region.World.GenerateUniqueName("1S" + (Game1.r.Next(3, 6)) + "s", this);
            Architects = architects;
            Type = type;
            Leader = leader;
            Base = Basee;

            MoralCompass = leader.MoralCompass;
            StabilityCompass = leader.StabilityCompass;
            PropertyValue = leader.PropertyValue;
            FamilyValue = leader.FamilyValue;
            PowerValue = leader.PowerValue;
            MoneyValue = leader.MoneyValue;
            KnowledgeValue = leader.KnowledgeValue;
            SpiritualityValue = leader.SpiritualityValue;
            ProwessValue = leader.ProwessValue;
            PatriotismValue = leader.PatriotismValue;
            CourageValue = leader.CourageValue;
            CreativityValue = leader.CreativityValue;

            AddReferredToName(Name);
        }
        public Group()
        {
            //default constructor for serialization
        }
    }
}
