using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Material
    {
        public string Name { get; set; }
        public string Type { get; set; } // wood, stone, metal, or cloth
        public int Toughness { get; set; } // 0-5
        public int Rarity { get; set; }

        public Material(string name, string type, int toughness, int rarity)
        {
            Name = name;
            Type = type;
            Toughness = toughness;
            Rarity = rarity;
        }
        public Material()
        {
            //default constructor for serialization
        }
    }
}
