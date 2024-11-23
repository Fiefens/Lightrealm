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
    public class Region : Entity
    {
        public string Biome { get; set; }
        public int Elevation { get; set; }
        public int Heat { get; set; }
        public int X { get; set; }
        public int Z { get; set; }

        public Realm Realm;

        private int _myLocationId;
        
        public Location Location
        {
            get => EntityGet<Location>(_myLocationId);
            set => _myLocationId = value?.ID ?? 0;
        }

        public List<(int, int)> TragedyPoints { get; set; } = new List<(int, int)>();

        public bool Explored { get; set; } = false;
        public bool DeepExplored { get; set; } = false;

        private int _blightId;
        
        public Blight Blight
        {
            get => EntityGet<Blight>(_blightId);
            set => _blightId = value?.ID ?? 0;
        }

        public string PortName { get; set; } = "";

        private int _harvestableWoodId;
        
        public Material HarvestableWood
        {
            get => EntityGet<Material>(_harvestableWoodId);
            set => _harvestableWoodId = value?.ID ?? 0;
        }

        private int _harvestableStoneId;
        
        public Material HarvestableStone
        {
            get => EntityGet<Material>(_harvestableStoneId);
            set => _harvestableStoneId = value?.ID ?? 0;
        }

        private int _harvestableMetalId;
        
        public Material HarvestableMetal
        {
            get => EntityGet<Material>(_harvestableMetalId);
            set => _harvestableMetalId = value?.ID ?? 0;
        }

        private int _harvestableSandId;
        
        public Material HarvestableSand
        {
            get => EntityGet<Material>(_harvestableSandId);
            set => _harvestableSandId = value?.ID ?? 0;
        }

        private int _harvestableIceId;
        
        public Material HarvestableIce
        {
            get => EntityGet<Material>(_harvestableIceId);
            set => _harvestableIceId = value?.ID ?? 0;
        }

        private int _harvestableFiberId;
        
        public Material HarvestableFiber
        {
            get => EntityGet<Material>(_harvestableFiberId);
            set => _harvestableFiberId = value?.ID ?? 0;
        }

        public EntityList<Unit> Units { get; set; } = new EntityList<Unit>();

        private int _ownerId;
        
        public Entity Owner
        {
            get => EntityGet<Civilization>(_ownerId);
            set => _ownerId = value?.ID ?? 0;
        }

        public Region(string biome, int elevation, int heat, int x, int z, World w)
        {
            Biome = biome;
            Elevation = elevation;
            Heat = heat;
            X = x;
            Z = z;

            HarvestableWood = w.Woods[Game1.r.Next(w.Woods.Count())];
            HarvestableFiber = w.Fibers[Game1.r.Next(w.Fibers.Count())];
            HarvestableStone = w.Stones[Game1.r.Next(w.Stones.Count())];
            HarvestableMetal = w.Metals[Game1.r.Next(w.Metals.Count())];
            HarvestableSand = w.Sands[Game1.r.Next(w.Sands.Count())];
            HarvestableIce = w.Ices[Game1.r.Next(w.Ices.Count())];
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
