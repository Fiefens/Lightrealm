using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class TextStorage : Entity
    {
        public static T Entity<T>(int entityId) where T : Entity
        {
            if (Game1.GameWorld == null || Game1.GameWorld.AllEntities == null)
            {
                return (T)Convert.ChangeType(Game1.TemporaryEntities[entityId], typeof(T));
            }

            return (T)Convert.ChangeType(Game1.GameWorld.AllEntities[entityId], typeof(T));
        }

        public string Data { get; set; }
        public Color Color { get; set; }

        public EntityList<Entity> Entities { get; set; } = new EntityList<Entity>();

        public TextStorage(string data, Color color, EntityList<Entity> Entities)
        {
            this.Data = data;
            this.Color = color;
            this.Entities = Entities;
        }


        public TextStorage()
        {

        }
    }
}
