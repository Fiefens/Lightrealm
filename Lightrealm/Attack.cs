using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Attack : Entity
    {
        public static T Entity<T>(int entityId) where T : Entity
        {
            if (Game1.GameWorld == null || Game1.GameWorld.AllEntities == null)
            {
                return (T)Convert.ChangeType(Game1.TemporaryEntities[entityId], typeof(T));
            }

            return (T)Convert.ChangeType(Game1.GameWorld.AllEntities[entityId], typeof(T));
        }

        public string Verb { get; set; }

        private int _attackerId;
        public Architect Attacker
        {
            get => Entity<Architect>(_attackerId);
            set => _attackerId = value?.ID ?? 0;
        }

        private int _targetId;
        public Object Target
        {
            get => Entity<Object>(_targetId);
            set => _targetId = value?.ID ?? 0;
        }

        private int _weaponId;
        public Object Weapon
        {
            get => Entity<Object>(_weaponId);
            set => _weaponId = value?.ID ?? 0;
        }

        public Attack(string verb, Architect attacker, Object target, Object weapon)
        {
            Verb = verb;
            Attacker = attacker;
            Target = target;
            Weapon = weapon;
        }
    }
}
