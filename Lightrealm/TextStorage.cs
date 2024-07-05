using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class TextStorage : Entity
    {
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
