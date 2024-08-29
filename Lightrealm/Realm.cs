using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Realm : Entity
    {
        public EntityList<Region> ContainedRegions = new EntityList<Region>();
        public string Color;
        public int X = 0;
        public int Z = 0;

        public EntityList<Event> Events = new EntityList<Event>();
        public EntityList<Event> SignificantEvents = new EntityList<Event>();

    }
}
