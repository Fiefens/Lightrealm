using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Region : Entity
    {
        public string Biome { get; set; }
        public int Elevation { get; set; }
        public int Heat { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
        public Location MyLocation { get; set; }
        public World World;

        public List<(int, int)> TragedyPoints = new List<(int, int)>();

        public bool Explored = false;

        public Blight Blight;

        public string PortName = "";

        public Material HarvestableWood;
        public Material HarvestableStone;
        public Material HarvestableMetal;
        public Material HarvestableSand;
        public Material HarvestableIce;
        public Material HarvestableFiber;

        public List<InteractableEvent> Events = new List<InteractableEvent>(); 

        public Civilization Owner;

        public Region(string biome, int elevation, int heat, int x, int z, World w)
        {
            Biome = biome;
            Elevation = elevation;
            Heat = heat;
            X = x;
            Z = z;
            World = w;

            HarvestableWood = w.Woods[Game1.r.Next(w.Woods.Count)];
            HarvestableFiber = w.Fibers[Game1.r.Next(w.Fibers.Count)]; 
            HarvestableStone = w.Stones[Game1.r.Next(w.Stones.Count)];
            HarvestableMetal = w.Metals[Game1.r.Next(w.Metals.Count)];
            HarvestableSand = w.Sands[Game1.r.Next(w.Sands.Count)];
            HarvestableIce = w.Ices[Game1.r.Next(w.Ices.Count)];
        }
        public Region()
        {
            //default constructor for serialization
        }
        public Rectangle BoundingBox()
        {
            return new Rectangle((10 + X * Game1.TileXDistance) + ((Z % 2 == 1) ? Game1.TileZDistance / 2 : 0), 10 + Z * Game1.TileZDistance, Game1.TileSize, Game1.TileSize);
        }

    }
}
