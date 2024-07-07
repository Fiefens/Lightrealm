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
        public bool IsPrimary { get; set; } = false;

        private int _locationId;

        
        public Location Location
        {
            get => EntityGet<Location>(_locationId);
            set => _locationId = value?.ID ?? 0;
        }

        public List<Architect> Architects { get; set; } = new List<Architect>();

        public List<Architect> ArchitectsToRemove { get; set; } = new List<Architect>();

        private int _leaderId;

        
        public Architect Leader
        {
            get => EntityGet<Architect>(_leaderId);
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
            get => EntityGet<Location>(_baseId);
            set => _baseId = value?.ID ?? 0;
        }

        public List<Architect> Enemies { get; set; } = new List<Architect>();

        public List<Architect> ArchitectsWhoDeclined { get; set; } = new List<Architect>();

        public List<string> CaravanItems { get; set; } = new List<string>();

        public List<Group> GroupsIKnowAbout { get; set; } = new List<Group>();

        public List<Location> TradeRoute { get; set; } = new List<Location>();

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

        public Group(List<Architect> architects, string type, Architect leader, Location Basee)
        {
            Name = Game1.GameWorld.GenerateUniqueName("1S" + (Game1.r.Next(3, 6)) + "s", this);
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
