using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Blight : Entity
    {
        public static T Entity<T>(int entityId) where T : Entity
        {
            if (Game1.GameWorld == null || Game1.GameWorld.AllEntities == null)
            {
                return (T)Convert.ChangeType(Game1.TemporaryEntities[entityId], typeof(T));
            }

            return (T)Convert.ChangeType(Game1.GameWorld.AllEntities[entityId], typeof(T));
        }

        public bool Spawned { get; set; } = false;
        public int SpreadChance { get; set; } = Game1.r.Next(500, 1000);
        public int FoundingYear { get; set; }

        public static List<string> adjectives = new List<string>
        {
            "Tainted",
            "Withered",
            "Bleak",
            "Ravaged",
            "Barren",
            "Desecrated",
            "Infested",
            "Corroded",
            "Deformed",
            "Defiled",
            "Desolate",
            "Necrotic"
        };
        public static List<string> nouns = new List<string>
        {
            "Void",
            "Barrens",
            "Abyss",
            "Hellscape",
            "Blight",
            "Expanse",
            "Morass",
            "Crater"
        };

        public Blight(World w)
        {
            FoundingYear = Game1.r.Next(1, 245);
            Name = w.Name; //placeholder that is guaranteed to exist in the world so the loop will run

            while (w.SubjectCatalogue.ContainsKey(Name))
            {
                Name = "The " + adjectives[Game1.r.Next(adjectives.Count)] + " " + nouns[Game1.r.Next(nouns.Count)];
            }
            w.SubjectCatalogue.Add(Name, this);
        }
    }
}
