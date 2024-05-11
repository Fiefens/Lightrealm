using Lightrealm.GameEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Room
    {
        public Structure Structure { get; set; }
        public List<Object> Objects { get; set; } = new List<Object>();
        public List<Object> ObjectsToRemove { get; set; } = new List<Object>();
        public List<Architect> Architects { get; set; } = new List<Architect>();
        public List<Architect> ArchitectsToRemove { get; set; } = new List<Architect>();

        public Room(Structure structure, List<Object> objects, List<Architect> architects, List<Architect> architectsToRemove)
        {
            Structure = structure;
            Objects = objects;
            Architects = architects;
            ArchitectsToRemove = architectsToRemove;
        }

        public int NumberOfDoors()
        {
            int Doors = 0;
            foreach(Object o in Objects)
            {
                if(o is Door)
                {
                    Doors++;
                }
            }
            return Doors;
        }

        public Room()
        {

        }
        public void PopulateRoom()
        {
            Civilization RoomCiv = Structure.Block.District.Location.HomeCivilization;
            int VerticalDecider = Game1.r.Next(1, 5);

            // Helper function to create and add objects
            void AddObject(string type, Material material, bool isContainer = false)
            {
                Object o = new Object(null, type, new List<Material> { material }, null);
                o.Room = this; o.Structure = this.Structure; o.Block = this.Structure.Block;
                Objects.Add(o);
            }
            
            if (Structure.Type == "spire")
            {
                Material m = Structure.Block.District.Location.PrimaryRace == Game1.GameWorld.GetRace("nightfell") ? Game1.GameWorld.Darkstone : Game1.GameWorld.Illuminite;
                int Position = Structure.Rooms.IndexOf(this);

                // Add exit door if it's the first room
                if (Position == 0)
                    AddObject("door", m);

                // Add random number of bookcases
                for (int i = Game1.r.Next(0, 2); i > 0; i--)
                    AddObject("bookcase", m);

                // Add random number of altars
                for (int i = Game1.r.Next(0, 2); i > 0; i--)
                    AddObject("altar", m);

                // Add random number of tables
                for (int i = Game1.r.Next(0, 2); i > 0; i--)
                    AddObject("table", m);

                // Add random number of chairs
                for (int i = Game1.r.Next(0, 2); i > 0; i--)
                    AddObject("chair", m);

                // Add doors to connect with other rooms, if applicable
                if (Position != 0)
                {
                    // Logic to create and add doors to connect this room with the previous room
                    // For example, downward and upward spiral staircases as doors
                    Door downStaircase = new Door(this, Structure.Rooms[Position - 1], "down", null, "downward spiral staircase", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(downStaircase);

                    Door upStaircase = new Door(Structure.Rooms[Position - 1], this, "up", null, "upward spiral staircase", new List<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[Position - 1].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[Position - 1].Objects.Add(upStaircase);
                }

                if(Position == Structure.Rooms.Count - 1)
                {
                    Objects.AddRange(Structure.Block.District.Location.Region.World.LootTableMachine("magictreasure78"));
                }

                if(Game1.r.Next(1,4) == 1)
                {
                    Objects.AddRange(Structure.Block.District.Location.Region.World.LootTableMachine("general"));
                }
            }

            else if (Structure.Type == "sanctum")
            {
                int Position = Structure.Rooms.IndexOf(this);

                // Add exit door if it's the first room
                if (Position == 0)
                    AddObject("door", Game1.GameWorld.Glass);

                if(Game1.r.Next(1,4) == 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("general"));
                }

                // Add specific objects for a sanctum
                if (Position + 1 == Structure.Rooms.Count)
                {
                    // Logic to find and place an artifact in the final room of the sanctum
                    Object Artifact = null;

                    foreach (Object o in Structure.Block.District.Location.UnplacedArtifacts)
                    {
                        if (Engine.Data.AllLegendarySpells.Contains(o.SpellContained))
                        {
                            Structure.Block.District.Location.UnplacedArtifacts.Remove(o);
                            Artifact = o;
                            break;
                        }
                    }

                    if (Artifact != null)
                    {
                        Object pedestal = new Object(null, "pedestal", new List<Material>() { Game1.GameWorld.Archaeon }, null);
                        pedestal.ContainedObjects.Add(Artifact);
                        Objects.Add(pedestal);
                        Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure34"));
                        Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure56"));
                        Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure910"));
                    }
                }
                else
                {
                    for (int i = Game1.r.Next(0, 5); i > 0; i--)
                        AddObject("pillar", Game1.GameWorld.Archaeon);

                    // Add doors to connect with other rooms
                    if (Position != 0)
                    {
                        string MainDirection = Door.AllDoorDirections[Game1.r.Next(Door.AllDoorDirections.Count)];

                        Door d = new Door(this, Structure.Rooms[Position - 1], MainDirection, null, "archway", new List<Material> { Game1.GameWorld.Archaeon }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                        Objects.Add(d);

                        Door D = new Door(Structure.Rooms[Position - 1], this, Game1.InvertDoorDirection[MainDirection], null, "archway", new List<Material> { Game1.GameWorld.Archaeon }, false, false, null, null, 255, false, Structure.Rooms[Position - 1].NumberOfDoors(), Structure.Block, Structure, this);
                        Structure.Rooms[Position - 1].Objects.Add(D);
                    }
                }
            }


            if (Structure.Type == "house")
            {
                // Adding beds
                for (int i = Game1.r.Next(1, 2); i > 0; i--)
                    AddObject("bed", RoomCiv.CulturalWood, true);

                // Adding tables
                for (int i = Game1.r.Next(0, 3); i > 0; i--)
                    AddObject("table", RoomCiv.CulturalWood, true);

                // Adding chairs
                for (int i = Game1.r.Next(0, 3); i > 0; i--)
                    AddObject("chair", RoomCiv.CulturalWood, true);
            }

            if (Structure.Type == "shrine")
            {
                // Adding altars, each shrine might have one or more altars
                for (int i = Game1.r.Next(1, 4); i > 0; i--)
                    AddObject("altar", RoomCiv.CulturalMetal);

                // Shrines might also have tables for offerings or scriptures
                for (int i = Game1.r.Next(0, 3); i > 0; i--)
                    AddObject("table", RoomCiv.CulturalWood);

                // Adding chairs for devotees or visitors
                for (int i = Game1.r.Next(4, 8); i > 0; i--)
                    AddObject("chair", RoomCiv.CulturalWood);

                // Additional shrine-specific objects can be added here
            }

            if (Structure.Type == "library")
            {
                // Adding bookcases filled with books
                for (int i = Game1.r.Next(10, 20); i > 0; i--)
                    AddObject("bookcase", RoomCiv.CulturalWood);

                // Libraries also need tables for reading and study
                for (int i = Game1.r.Next(4, 6); i > 0; i--)
                    AddObject("table", RoomCiv.CulturalWood);

                // Chairs for seating in the library
                for (int i = Game1.r.Next(4, 8); i > 0; i--)
                    AddObject("chair", RoomCiv.CulturalWood);

                // Additional library-specific objects like lamps, globes, etc., can be added here
            }

            if (Structure.Type == "tavern")
            {
                for (int i = Game1.r.Next(3, 7); i > 0; i--)
                    AddObject("keg", RoomCiv.CulturalWood, true);

                for (int i = Game1.r.Next(4, 6); i > 0; i--)
                    AddObject("table", RoomCiv.CulturalWood, true);

                for (int i = Game1.r.Next(4, 8); i > 0; i--)
                    AddObject("chair", RoomCiv.CulturalWood, true);
            }

            if (Structure.Type == "forge")
            {
                for (int i = Game1.r.Next(1, 3); i > 0; i--)
                    AddObject("forge", RoomCiv.CulturalMetal, false);

                for (int i = Game1.r.Next(4, 8); i > 0; i--)
                    AddObject("weapon rack", RoomCiv.CulturalWood, true);

                // Continue with other objects specific to a forge, like tables, chairs, tool racks, etc.
            }

            if (Structure.Type == "watchtower")
            {
                for (int i = Game1.r.Next(1, 3); i > 0; i--)
                    AddObject("weapon rack", RoomCiv.CulturalWood, true);

                for (int i = Game1.r.Next(0, 3); i > 0; i--)
                    AddObject("chest", RoomCiv.CulturalWood, true);

                // Continue with other objects specific to a watchtower, like tables, etc.
            }

            if (Structure.Type == "market")
            {
                for (int i = Game1.r.Next(7, 12); i > 0; i--)
                    AddObject("chest", RoomCiv.CulturalWood, true);

                for (int i = Game1.r.Next(4, 6); i > 0; i--)
                    AddObject("table", RoomCiv.CulturalWood, true);

                // Continue with other objects specific to a market, like chairs, etc.
            }

            if (Structure.Type == "bighouse")
            {
                for (int i = Game1.r.Next(1, 5); i > 0; i--)
                    AddObject("bed", RoomCiv.CulturalWood, true);

                // Continue with other objects specific to a big house, like tables, chairs, etc.
            }

            if (Structure.Type == "outpost")
            {
                List<string> Roomtypes = new List<string>()
{
    "armory",
    "foodstorage",
    "goodsstorage",
    "archery",
    "planning",
    "diningroom",
    "brewery",
    "quarters",
    "passageway",
    "forge",
    "garden",
    "vault"
};

                string Type = Roomtypes[Game1.r.Next(Roomtypes.Count)];
                switch (Type)
                {
                    case "armory":
                        for (int i = Game1.r.Next(4, 8); i > 0; i--)
                            AddObject("armor stand", RoomCiv.CulturalWood, true);
                        break;

                    case "foodstorage":
                        for (int i = Game1.r.Next(7, 12); i > 0; i--)
                            AddObject("barrel", RoomCiv.CulturalWood, true);
                        break;

                    case "goodsstorage":
                        for (int i = Game1.r.Next(7, 12); i > 0; i--)
                            AddObject("bin", RoomCiv.CulturalWood, true);
                        break;

                    case "archery":
                        for (int i = Game1.r.Next(1, 5); i > 0; i--)
                            AddObject("target board", RoomCiv.CulturalWood, false);
                        break;

                    case "planning":
                        AddObject("table", RoomCiv.CulturalWood, true);
                        // Map object can be added here
                        break;

                    case "diningroom":
                        for (int i = Game1.r.Next(5, 6); i > 0; i--)
                        {
                            AddObject("table", RoomCiv.CulturalWood, true);
                            AddObject("chair", RoomCiv.CulturalWood, true);
                        }
                        break;

                    case "brewery":
                        for (int i = Game1.r.Next(5, 25); i > 0; i--)
                            AddObject("keg", RoomCiv.CulturalWood, true);
                        break;

                    case "quarters":
                        for (int i = Game1.r.Next(1, 2); i > 0; i--)
                            AddObject("bed", RoomCiv.CulturalWood, true);
                        break;

                    case "passageway":
                        // Add objects suitable for a passageway
                        break;

                    case "forge":
                        for (int i = Game1.r.Next(1, 3); i > 0; i--)
                            AddObject("forge", RoomCiv.CulturalMetal, false);
                        break;

                    case "garden":
                        for (int i = Game1.r.Next(6, 10); i > 0; i--)
                            AddObject("plant", RoomCiv.CulturalWood, false);
                        break;

                    case "vault":
                        AddObject("pedestal", RoomCiv.CulturalStone, true);
                        break;
                }
            }

        }
    }
}
