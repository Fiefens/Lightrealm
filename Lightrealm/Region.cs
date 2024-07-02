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
        public static T Entity<T>(int entityId) where T : Entity
        {
            if (Game1.GameWorld == null || Game1.GameWorld.AllEntities == null)
            {
                return (T)Convert.ChangeType(Game1.TemporaryEntities[entityId], typeof(T));
            }

            return (T)Convert.ChangeType(Game1.GameWorld.AllEntities[entityId], typeof(T));
        }

        public string Biome { get; set; }
        public int Elevation { get; set; }
        public int Heat { get; set; }
        public int X { get; set; }
        public int Z { get; set; }

        private int _myLocationId;
        public Location MyLocation
        {
            get => Entity<Location>(_myLocationId);
            set => _myLocationId = value?.ID ?? 0;
        }

        private int _worldId;
        public World World
        {
            get => Entity<World>(_worldId);
            set => _worldId = value?.ID ?? 0;
        }

        public List<(int, int)> TragedyPoints { get; set; } = new List<(int, int)>();

        public bool Explored { get; set; } = false;
        public bool RegionallyExplored { get; set; } = false;

        private int _blightId;
        public Blight Blight
        {
            get => Entity<Blight>(_blightId);
            set => _blightId = value?.ID ?? 0;
        }

        public string PortName { get; set; } = "";

        private int _harvestableWoodId;
        public Material HarvestableWood
        {
            get => Entity<Material>(_harvestableWoodId);
            set => _harvestableWoodId = value?.ID ?? 0;
        }

        private int _harvestableStoneId;
        public Material HarvestableStone
        {
            get => Entity<Material>(_harvestableStoneId);
            set => _harvestableStoneId = value?.ID ?? 0;
        }

        private int _harvestableMetalId;
        public Material HarvestableMetal
        {
            get => Entity<Material>(_harvestableMetalId);
            set => _harvestableMetalId = value?.ID ?? 0;
        }

        private int _harvestableSandId;
        public Material HarvestableSand
        {
            get => Entity<Material>(_harvestableSandId);
            set => _harvestableSandId = value?.ID ?? 0;
        }

        private int _harvestableIceId;
        public Material HarvestableIce
        {
            get => Entity<Material>(_harvestableIceId);
            set => _harvestableIceId = value?.ID ?? 0;
        }

        private int _harvestableFiberId;
        public Material HarvestableFiber
        {
            get => Entity<Material>(_harvestableFiberId);
            set => _harvestableFiberId = value?.ID ?? 0;
        }

        private List<int> _events = new List<int>();
        public EntityList<InteractableEvent> Events
        {
            get => _events.Select(id => Entity<InteractableEvent>(id)).ToList();
            set => _events = value.Select(e => e.ID).ToList();
        }

        private int _ownerId;
        public Civilization Owner
        {
            get => Entity<Civilization>(_ownerId);
            set => _ownerId = value?.ID ?? 0;
        }


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
            return new Rectangle(
                            (Game1.RegionXMod + X * Game1.TileXDistance) + ((Z % 2 == 1) ? Game1.TileXDistance / 2 : 0),
                            Game1.RegionYMod + Z * Game1.TileZDistance,
                            Game1.TileSize,
                            Game1.TileSize
                        );
        }





    }
}
