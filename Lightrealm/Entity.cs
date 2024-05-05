using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Entity
    {
        public string Name { get; set; }
        public List<string> ReferredToNames { get; set; } = new List<string>();
        public string Metadata;

        public Entity()
        {

        }

        public Entity(string metadata)
        {
            Metadata = metadata;
            Name = metadata;
        }
    }
}
