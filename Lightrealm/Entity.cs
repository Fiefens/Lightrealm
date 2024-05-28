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

        private List<string> _referredToNames = new List<string>();

        public List<string> ReferredToNames
        {
            get
            {
                if (_referredToNames.Count == 0)
                {
                    return new List<string> { Name };
                }
                return _referredToNames;
            }
            set
            {
                _referredToNames = value;
            }
        }

        public void AddReferredToName(string name)
        {
            _referredToNames.Add(name);
        }

        public void ClearReferredToNames()
        {
            _referredToNames.Clear();
        }



        public string Metadata;

        public Entity()
        {

        }

        public Entity(string metadata)
        {
            Metadata = metadata;
            Name = metadata;
            ReferredToNames.Add(Name);
        }
    }
}
