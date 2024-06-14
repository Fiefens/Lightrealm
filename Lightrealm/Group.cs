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
        public List<Architect> Architects { get; set; } = new List<Architect>();
        public Architect Leader { get; set; }
        public List<Architect> ArchitectsToRemove { get; set; } = new List<Architect>();
        public string Type { get; set; }

        public int Stability { get; set; } = 50; //100 is a very solid group. 0 is cause for disbandment.
        public int Reputation { get; set; } = 0; //The better a group is, the more likely it will attack and destroy bad groups, the less likely it will be attacked regardless of its evil reputation, and the more stable it is.

        private double _daysOld; // Store age in days

        public int MonthsOld
        {
            get
            {
                return (int)(_daysOld / 28.0); // Return the age in months
            }
            set
            {
                _daysOld = value * 28.0; // Allow setting age in months, if needed
            }
        }

        public double DaysOld
        {
            get
            {
                return _daysOld; // Accessor for the age in days
            }
            set
            {
                _daysOld = value; // Mutator for the age in days
            }
        }

        public Location Base { get; set; } = null;

        public List<Architect> Enemies { get; set; } = new List<Architect>();
        public List<Architect> ArchitectsWhoDeclined { get; set; } = new List<Architect>();

        public List<Object> CaravanItems { get; set; } = new List<Object>();
        public int StoredVitalium { get; set; } = 0;

        public List<Location> TradeRoute = new List<Location>();
        public int MaxTradeRouteLength = Game1.r.Next(3, 10);
        public bool TradeCooldown = false;

        public int MoralCompass = 0;
        public int StabilityCompass = 0;

        public int Wealth = 0;

        public int PropertyValue = 0;
        public int FamilyValue = 0;
        public int PowerValue = 0;
        public int MoneyValue = 0;
        public int KnowledgeValue = 0;
        public int SpiritualityValue = 0;
        public int ProwessValue = 0;
        public int PatriotismValue = 0;
        public int CourageValue = 0;
        public int CreativityValue = 0;

        public string CaravanCatalogue()
        {
            Dictionary<string, int> itemCounts = new Dictionary<string, int>();

            // Count the occurrences of each type
            foreach (Object item in CaravanItems)
            {
                string itemTypeString = item.Type;

                if (itemCounts.ContainsKey(itemTypeString))
                {
                    itemCounts[itemTypeString]++;
                }
                else
                {
                    itemCounts[itemTypeString] = 1;
                }
            }

            // Create a formatted string for the catalogue
            StringBuilder catalogueBuilder = new StringBuilder();
            foreach (var pair in itemCounts)
            {
                catalogueBuilder.Append($"{pair.Key} {pair.Value} ");
            }

            return catalogueBuilder.ToString();
        }

        // groups with both a good and bad reputation can fall more easily...

        public List<Group> GroupsIKnowAbout { get; set; } = new List<Group>();

        public Group(List<Architect> architects, string type, Architect leader, Location Basee)
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
