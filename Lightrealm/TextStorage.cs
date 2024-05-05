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

        public TextStorage(string data, Color color)
        {
            this.Data = data;
            this.Color = color;
        }
        public TextStorage()
        {

        }
    }
}
