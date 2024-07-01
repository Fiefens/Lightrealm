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

        public string Name;
        public string Description;

        public Entity Base;

        public int Power = 0;

        public int MoralCompass = 0;
        public int StabilityCompass = 0;
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
