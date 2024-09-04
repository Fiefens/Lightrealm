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

        public EntityList<Architect> Architects { get; set; } = new EntityList<Architect>();

        public EntityList<Architect> ArchitectsToRemove { get; set; } = new EntityList<Architect>();

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

        public EntityList<Architect> ArchitectsWhoDeclined { get; set; } = new EntityList<Architect>();

        public List<string> CaravanItems { get; set; } = new List<string>();

        public EntityList<Group> GroupsIKnowAbout { get; set; } = new EntityList<Group>();

        public EntityList<Location> TradeRoute { get; set; } = new EntityList<Location>();

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

        private int _factionId;
        public Faction HomeFaction
        {
            get => EntityGet<Faction>(_factionId);
            set => _factionId = value?.ID ?? 0;
        }


        public void Recruit(World world, string Date)
        {
            // Ensure the leader has a valid location and region
            if (this.Leader?.Location?.Region?.Realm == null)
                return;

            // Select an architect from the World based on the criteria
            var validArchitects = world.AllArchitects.Where(arch =>
                arch.HomeLocation != null &&  // The architect has a home location
                world.SettlementTypes.Contains(arch.HomeLocation.Type) &&  // The home location is a settlement
                arch.HomeLocation.Region?.Realm == this.Leader.Location.Region.Realm &&  // The architect is in the same Realm as the leader
                !(
                    (arch.HomeLocation.Government is Architect && arch.HomeLocation.Government == arch) ||  // The architect is not the government
                    (arch.HomeLocation.Government is Group && ((Group)(arch.HomeLocation.Government)).Architects.Contains(arch))  // The architect is not a member of a group that is the government
                ) &&
                world.HumanoidRaces.Contains(arch.Race)  // The architect is a humanoid
            ).ToList();

            if (validArchitects.Any())
            {
                // Select a random architect from the valid architects
                Architect selectedArchitect = validArchitects[new Random().Next(validArchitects.Count)];

                // Add the selected architect to the group
                Architects.Add(selectedArchitect);

                world.HistoricalEvents.Add(new Event($"{Date} {this.Name} sought out and recruited {selectedArchitect.Name} to their group.", selectedArchitect.Location.Region, new EntityList<Entity>()));
            }
        }

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
