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

        public Architect Attacker;
        public Object Target;
        public Object Weapon;


        public Attack(string verb, Architect attacker, Object target, Object weapon)
        {
            Verb = verb;
            Attacker = attacker;
            Target = target;
            Weapon = weapon;

            // Only proceed if target is a body part
            if (target.IsBodyPart)
            {
                if (attacker.ImportantThisLoad ||
                    (target.Creator is Architect creatorArchitect && creatorArchitect.ImportantThisLoad))
                {
                    attacker.ImportantThisLoad = true;

                    if (target.Creator is Architect creator)
                    {
                        creator.ImportantThisLoad = true;
                    }
                }
            }
        }


        public Attack()
        {

        }
    }
}
