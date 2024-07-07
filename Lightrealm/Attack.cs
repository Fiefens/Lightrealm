using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Lightrealm
{
    [Serializable]
    public class Attack : Entity
    {
        public string Verb { get; set; }

        private int _attackerId;

        
        public Architect Attacker
        {
            get => EntityGet<Architect>(_attackerId);
            set => _attackerId = value?.ID ?? 0;
        }

        private int _targetId;

        
        public Object Target
        {
            get => EntityGet<Object>(_targetId);
            set => _targetId = value?.ID ?? 0;
        }

        private int _weaponId;

        
        public Object Weapon
        {
            get => EntityGet<Object>(_weaponId);
            set => _weaponId = value?.ID ?? 0;
        }

        public Attack(string verb, Architect attacker, Object target, Object weapon)
        {
            Verb = verb;
            Attacker = attacker;
            Target = target;
            Weapon = weapon;
        }

        public Attack()
        {

        }
    }
}
