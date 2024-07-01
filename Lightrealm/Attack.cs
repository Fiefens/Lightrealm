using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Attack
    {
        public static T Entity<T>(int entityId) where T : Entity
        {
            if (Game1.GameWorld == null || Game1.GameWorld.AllEntities == null)
            {
                return (T)Convert.ChangeType(Game1.TemporaryEntities[entityId], typeof(T));
            }

            return (T)Convert.ChangeType(Game1.GameWorld.AllEntities[entityId], typeof(T));
        }

        public string Verb;
        public Architect Attacker;
        public Object Target;
        public Object Weapon;

        public Attack(string verb, Architect attacker, Object target, Object weapon)
        {
            Verb = verb;
            Attacker = attacker;
            Target = target;
            Weapon = weapon;
        }
    }
}
