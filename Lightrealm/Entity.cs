using Microsoft.Xna.Framework;
using NAudio.Midi;
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
        public int ID;
        private List<string> _referredToNames = new List<string>();
        public Rectangle Hitbox = new Rectangle();

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
            if(Game1.GameWorld != null)
            {
                ID = Game1.GameWorld.NextUniqueID;
                Game1.GameWorld.NextUniqueID++;
            }
            else
            {
                ID = Game1.TemporaryNextUniqueID;
                Game1.TemporaryNextUniqueID++;
            }
        }

        public Entity(string metadata)
        {
            Metadata = metadata;
            Name = metadata;
            ReferredToNames.Add(Name);
        }
    }
}
