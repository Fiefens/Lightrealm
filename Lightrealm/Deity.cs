using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Deity : Entity
    {
        public string Allignment { get; set; }

        public Deity(string name, string allignment)
        {
            Name = name;
            Allignment = allignment;
            AddReferredToName(name);
        }
        public Deity()
        {
            //default constructor for serialization
        }
    }
}
