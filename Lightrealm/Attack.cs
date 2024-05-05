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
        public string Verb;
        public Architect Attacker;
        public string Target;
        public Object Weapon;

        public Attack(string verb, Architect attacker, string target, Object weapon)
        {
            Verb = verb;
            Attacker = attacker;
            Target = target;
            Weapon = weapon;
        }
    }
}
