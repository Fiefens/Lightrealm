using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Region
    {
        public string Biome { get; set; }
        public int Elevation { get; set; }
        public int Heat { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
        public Hyperregion Hyperregion;
        public Location Location;
        public List<(int, int)> TragedyPoints { get; set; } = new List<(int, int)>();
        public bool Explored { get; set; } = false;
        public bool DeepExplored { get; set; } = false;
        public Blight Blight;
        public string PortName { get; set; } = "";
        public Material HarvestableWood;
        public Material HarvestableStone;
        public Material HarvestableMetal;
        public Material HarvestableSand;
        public Material HarvestableIce;
        public Material HarvestableFiber;
        public EntityList<Unit> Units { get; set; } = new EntityList<Unit>();
        public Entity Owner;


        public Region(string biome, int elevation, int heat, int x, int z, World w)
        {
            Biome = biome;
            Elevation = elevation;
            Heat = heat;
            X = x;
            Z = z;

            SerializableRandom rr = Game1.GameWorld != null ? Game1.GameWorld.rnd : Game1.TemporaryRand;

            HarvestableWood = w.Woods[rr.Next(w.Woods.Count())];
            HarvestableFiber = w.Fibers[rr.Next(w.Fibers.Count())];
            HarvestableStone = w.Stones[rr.Next(w.Stones.Count())];

            HarvestableMetal = w.Metals.Take(8)[rr.Next(w.Metals.Count() / 2)];
            HarvestableSand = w.Sands[rr.Next(w.Sands.Count())];
            HarvestableIce = w.Ices[rr.Next(w.Ices.Count())];
        }

        public Region()
        {
            //default constructor for serialization
        }

        public Rectangle BoundingBox()
        {
            return new Rectangle(
                (Game1.RegionXMod + X * Game1.TileXDistance) + ((Z % 2 == 1) ? Game1.TileXDistance / 2 : 0),
                Game1.RegionYMod + Z * Game1.TileZDistance,
                Game1.TileSize,
                Game1.TileSize
            );
        }
    }
}
