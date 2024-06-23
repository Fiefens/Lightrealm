using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Material : Entity
    {
        public string Type { get; set; } // wood, stone, metal, or cloth
        public int Toughness { get; set; } // 0-20
        public int Rarity { get; set; }
        public string Color = "white";

        public Material(string name, string type, int toughness, int rarity, string color)
        {
            Name = name;
            Type = type;
            Toughness = toughness;
            Rarity = rarity;
            AddReferredToName(Name);
            Color = color;

            Game1.MaterialsToAddToWorld.Add(this);
        }
        public Material()
        {
            //default constructor for serialization
        }
    }
}
