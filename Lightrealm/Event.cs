using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Event : Entity
    {
        public string EventData { get; set; }
        public Region Region { get; set; }
        public bool Significant { get; set; }
        public EntityList<Entity> Entities { get; set; }

        // Constructor

        public Event(string @event, Region region, EntityList<Entity> entities, bool overrideSignificance = false)
        {
            EventData = @event;
            Region = region;
            Entities = entities;

            foreach (Entity e in entities)
            {
                if (e.GetType() != typeof(Location))
                {
                    e.Significance++;
                }
            }

            // Override significance if specified, otherwise determine based on entity significance
            if (overrideSignificance)
            {
                Significant = true;
            }
            else if (entities.Any(e => e.Significance >= 8))
            {
                Significant = true;
            }
            else
            {
                Significant = false;
            }

            // Add to significant events list if the event is significant
            if (Significant && Region != null && Region.Realm != null)
            {
                Region.Realm.SignificantEvents.Add(this);
                Game1.GameWorld.SignificantEvents.Add(this);
            }
        }

    }
}
