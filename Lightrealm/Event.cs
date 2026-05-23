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

        public Event(string @event, Region region, EntityList<Entity> entities, string Override = "")
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
                e.AssociatedEvents.Add(this);
            }

            if(Game1.MostRecentPartyTurnArchitect != null)
            {
                foreach (Entity e in Entities)
                {
                    if (e is Architect a && Game1.LoadedArchitects.Contains(a))
                    {
                        a.ImportantThisLoad = true;
                    }
                }
            }

            // Determine significance based on override or entity significance
            if (string.IsNullOrEmpty(Override))
            {
                Significant = entities.Any(e => e.Significance >= 8);
            }
            else if (Override.ToLower() == "significant")
            {
                Significant = true;
            }
            else if (Override.ToLower() == "insignificant")
            {
                Significant = false;
            }

            // Add to significant events list if the event is significant
            if (Significant && Region != null && Region.Hyperregion != null)
            {
                Region.Hyperregion.SignificantEvents.Add(this);
                Game1.GameWorld.SignificantEvents.Add(this);
            }
        }

    }
}
