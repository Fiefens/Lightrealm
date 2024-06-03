using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class TextStorage
    {
        public string Data { get; set; }
        public Color Color { get; set; }
        public List<Entity> Entities { get; set; }

        public TextStorage(string data, Color color, List<Entity> Entities)
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
