using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Force
    {
        public static T Entity<T>(int entityId) where T : Entity
        {
            if (Game1.GameWorld == null || Game1.GameWorld.AllEntities == null)
            {
                return (T)Convert.ChangeType(Game1.TemporaryEntities[entityId], typeof(T));
            }

            return (T)Convert.ChangeType(Game1.GameWorld.AllEntities[entityId], typeof(T));
        }

        public string Name { get; set; }
        public string Description { get; set; }

        private int _baseId;
        public Entity Base
        {
            get => Entity<Entity>(_baseId);
            set => _baseId = value?.ID ?? 0;
        }

        public int Power { get; set; } = 0;

        public int MoralCompass { get; set; } = 0;
        public int StabilityCompass { get; set; } = 0;
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

        public Force(string name, string description, int power, int moralCompass, int stabilityCompass, int propertyValue, int familyValue, int powerValue, int moneyValue, int knowledgeValue, int spiritualityValue, int prowessValue, int patriotismValue, int courageValue, int creativityValue, Entity basee)
        {
            Name = name;
            Description = description;
            Power = power;
            MoralCompass = moralCompass;
            StabilityCompass = stabilityCompass;
            PropertyValue = propertyValue;
            FamilyValue = familyValue;
            PowerValue = powerValue;
            MoneyValue = moneyValue;
            KnowledgeValue = knowledgeValue;
            SpiritualityValue = spiritualityValue;
            ProwessValue = prowessValue;
            PatriotismValue = patriotismValue;
            CourageValue = courageValue;
            CreativityValue = creativityValue;
            Base = basee;
        }
    }
}
