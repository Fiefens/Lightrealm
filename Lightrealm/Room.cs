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

                // Generate loot if applicable
                if (Game1.r.Next(1, 4) == 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("general"));
                }

                if (Position == 0)
                {
                    // First room specific setup
                    AddObject("door", Game1.GameWorld.Glass);  // Entrance door for the first room

                    // Door leading to the next room
                    string ForwardDirection = Door.AllDoorDirections[Game1.r.Next(Door.AllDoorDirections.Count)];
                    Door forwardDoor = new Door(this, Structure.Rooms[Position + 1], ForwardDirection, null, "archway", new List<Material> { Game1.GameWorld.Archaeon }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(forwardDoor);
                }
                else if (Position == Structure.Rooms.Count - 1)
                {
                    // Last room specific setup
                    // Door leading back to the previous room
                    string BackwardDirection = Door.AllDoorDirections[Game1.r.Next(Door.AllDoorDirections.Count)];
                    Door backwardDoor = new Door(this, Structure.Rooms[Position - 1], BackwardDirection, null, "archway", new List<Material> { Game1.GameWorld.Archaeon }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(backwardDoor);

                    // Logic to find and place an artifact in the final room of the sanctum
                    Object Artifact = null;
                    foreach (Object o in Structure.Block.District.Location.UnplacedArtifacts)
                    {
                        if (Game1.AllLegendarySpells.Contains(o.SpecialKnowledge))
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
                    // Middle rooms setup
                    // Door leading back to the previous room
                    string BackwardDirection = Door.AllDoorDirections[Game1.r.Next(Door.AllDoorDirections.Count)];
                    Door backwardDoor = new Door(this, Structure.Rooms[Position - 1], BackwardDirection, null, "archway", new List<Material> { Game1.GameWorld.Archaeon }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(backwardDoor);

                    // Door leading to the next room
                    string ForwardDirection = Door.AllDoorDirections[Game1.r.Next(Door.AllDoorDirections.Count)];
                    Door forwardDoor = new Door(this, Structure.Rooms[Position + 1], ForwardDirection, null, "archway", new List<Material> { Game1.GameWorld.Archaeon }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(forwardDoor);
                }
            }

            else if (Structure.Type == "archway")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);
                int TotalRooms = Structure.Rooms.Count;
                int QuarterRooms = TotalRooms / 4;

                if (Position == 0)
                {
                    // First room: add entrance door
                    AddObject("door", m);
                }

                if (Position < QuarterRooms)
                {
                    // First 1/4 rooms: going upwards
                    if (Position > 0)
                    {
                        Door upStaircase = new Door(Structure.Rooms[Position - 1], this, "up", null, "upward staircase", new List<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[Position - 1].NumberOfDoors(), Structure.Block, Structure, this);
                        Structure.Rooms[Position - 1].Objects.Add(upStaircase);

                        Door downStaircase = new Door(this, Structure.Rooms[Position - 1], "down", null, "downward staircase", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                        Objects.Add(downStaircase);
                    }
                }
                else if (Position < (3 * QuarterRooms))
                {
                    // Middle 1/2 rooms: going horizontally
                    if (Position > 0)
                    {
                        string BackwardDirection = Door.AllDoorDirections[Game1.r.Next(Door.AllDoorDirections.Count)];
                        Door backwardDoor = new Door(this, Structure.Rooms[Position - 1], BackwardDirection, null, "doorway", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                        Objects.Add(backwardDoor);

                        string ForwardDirection = Door.AllDoorDirections[Game1.r.Next(Door.AllDoorDirections.Count)];
                        Door forwardDoor = new Door(this, Structure.Rooms[Position + 1], ForwardDirection, null, "doorway", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                        Objects.Add(forwardDoor);
                    }
                }
                else
                {
                    // Last 1/4 rooms: going downward
                    if (Position > 0)
                    {
                        Door downStaircase = new Door(Structure.Rooms[Position - 1], this, "down", null, "downward staircase", new List<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[Position - 1].NumberOfDoors(), Structure.Block, Structure, this);
                        Structure.Rooms[Position - 1].Objects.Add(downStaircase);

                        Door upStaircase = new Door(this, Structure.Rooms[Position - 1], "up", null, "upward staircase", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                        Objects.Add(upStaircase);
                    }

                    if (Position == TotalRooms - 1)
                    {
                        // Last room: add exit door
                        AddObject("door", m);
                    }
                }
            }
            else if (Structure.Type == "commune")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                // Entrance room
                if (Position == 0)
                {
                    // Add entrance door
                    AddObject("door", m);

                    // Add staircase up to the central room
                    Door staircaseUp = new Door(this, Structure.Rooms[Position + 1], "up", null, "staircase", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(staircaseUp);
                }
                // Central room
                else if (Position == 1)
                {
                    // Doors to north, east, south, and west rooms
                    Door northDoor = new Door(this, Structure.Rooms[Position + 1], "north", null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Door eastDoor = new Door(this, Structure.Rooms[Position + 2], "east", null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                     Door southDoor = new Door(this, Structure.Rooms[Position + 3], "south", null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Door westDoor = new Door(this, Structure.Rooms[Position + 4], "west", null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(northDoor);
                    Objects.Add(eastDoor);
                    Objects.Add(southDoor);
                    Objects.Add(westDoor);
                }
                // North, East, South, and West rooms
                else if (Position >= 2 && Position <= 5)
                {
                    // Add staircase up to the room directly above
                    Door staircaseUp = new Door(this, Structure.Rooms[Position + 4], "up", null, "staircase", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(staircaseUp);
                }
                // Remaining rooms stacked on top of the upper center room
                else
                {
                    // Add staircase up to the next room
                    Door staircaseUp = new Door(this, Structure.Rooms[Position + 1], "up", null, "staircase", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(staircaseUp);
                }

                // Add staircase down from previous room, except for the entrance room
                if (Position > 0)
                {
                    Door staircaseDown = new Door(this, Structure.Rooms[Position - 1], "down", null, "staircase", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[Position - 1].Objects.Add(staircaseDown);
                }
            }
            else if (Structure.Type == "core" || Structure.Type == "heart")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                // Central room
                if (Position == 0)
                {
                    // Add doors to north, east, south, west, up, and down rooms
                    Door northDoor = new Door(this, Structure.Rooms[Position + 1], "north", null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Door eastDoor = new Door(this, Structure.Rooms[Position + 2], "east", null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Door southDoor = new Door(this, Structure.Rooms[Position + 3], "south", null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Door westDoor = new Door(this, Structure.Rooms[Position + 4], "west", null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Door upDoor = new Door(this, Structure.Rooms[Position + 5], "up", null, "staircase", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Door downDoor = new Door(this, Structure.Rooms[Position + 6], "down", null, "staircase", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(northDoor);
                    Objects.Add(eastDoor);
                    Objects.Add(southDoor);
                    Objects.Add(westDoor);
                    Objects.Add(upDoor);
                    Objects.Add(downDoor);
                }
                // North, East, South, West, Up, and Down rooms
                else if (Position >= 1 && Position <= 6)
                {
                    // Add door to the next room in the same direction
                    string direction = "";
                    switch (Position)
                    {
                        case 1:
                            direction = "north";
                            break;
                        case 2:
                            direction = "east";
                            break;
                        case 3:
                            direction = "south";
                            break;
                        case 4:
                            direction = "west";
                            break;
                        case 5:
                            direction = "up";
                            break;
                        case 6:
                            direction = "down";
                            break;
                    }
                    Door nextRoomDoor = new Door(this, Structure.Rooms[Position + 6], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(nextRoomDoor);
                }
                // Remaining rooms
                else
                {
                    // Randomly attach remaining rooms to the core rooms
                    List<int> coreRoomIndices = new List<int> { 1, 2, 3, 4, 5, 6 };
                    int randomCoreRoomIndex = coreRoomIndices[Game1.r.Next(coreRoomIndices.Count)];
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];
                    Door randomRoomDoor = new Door(this, Structure.Rooms[randomCoreRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(randomRoomDoor);
                }

                // Add door to connect to the previous room, except for the central room
                if (Position > 0)
                {
                    Door previousRoomDoor = new Door(this, Structure.Rooms[Position - 1], "back", null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[Position - 1].Objects.Add(previousRoomDoor);
                }
            }

            else if (Structure.Type == "fortress")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                // Core room
                if (Position == 0)
                {
                    // Add random doors to connect to other rooms
                    List<string> directions = new List<string> { "north", "east", "south", "west", "up", "down" };
                    int numDoors = Game1.r.Next(2, directions.Count);  // Random number of doors between 2 and the number of directions
                    for (int i = 0; i < numDoors; i++)
                    {
                        string direction = directions[i];
                        Door newDoor = new Door(this, Structure.Rooms[Position + i + 1], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                        Objects.Add(newDoor);
                    }
                }
                // Remaining rooms
                else
                {
                    // Randomly attach remaining rooms to the previous room
                    List<int> availablePositions = new List<int>();
                    for (int i = 0; i < Structure.Rooms.Count; i++)
                    {
                        if (i != Position)
                        {
                            availablePositions.Add(i);
                        }
                    }
                    int randomIndex = availablePositions[Game1.r.Next(availablePositions.Count)];
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];
                    Door newDoor = new Door(this, Structure.Rooms[randomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(newDoor);
                }

                // Add door to connect to the previous room
                if (Position > 0)
                {
                    string previousDirection = ""; // determine the direction to connect to the previous room based on layout
                    switch (Position % 6)
                    {
                        case 1:
                            previousDirection = "north";
                            break;
                        case 2:
                            previousDirection = "east";
                            break;
                        case 3:
                            previousDirection = "south";
                            break;
                        case 4:
                            previousDirection = "west";
                            break;
                        case 5:
                            previousDirection = "up";
                            break;
                        case 0:
                            previousDirection = "down";
                            break;
                    }
                    Door previousRoomDoor = new Door(this, Structure.Rooms[Position - 1], previousDirection, null, previousDirection == "up" || previousDirection == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[Position - 1].Objects.Add(previousRoomDoor);
                }
            }
            else if (Structure.Type == "hallway")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                // Generate the first connection direction randomly
                string[] horizontalDirections = new string[] { "north", "east", "south", "west" };
                string direction;

                // If it's the first room, determine the direction randomly
                if (Position == 0)
                {
                    direction = horizontalDirections[Game1.r.Next(horizontalDirections.Length)];
                    Door initialDoor = new Door(this, Structure.Rooms[Position + 1], direction, null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(initialDoor);
                }
                else
                {
                    // For subsequent rooms, use the direction of the previous room's door
                    direction = ((Door)(Objects[0])).Direction; // Assuming the first object is always the door leading to the next room
                    Door nextDoor = new Door(this, Structure.Rooms[Position + 1], direction, null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(nextDoor);
                }

                // Add door to connect to the previous room
                if (Position > 0)
                {
                    string oppositeDirection;
                    switch (direction)
                    {
                        case "north":
                            oppositeDirection = "south";
                            break;
                        case "east":
                            oppositeDirection = "west";
                            break;
                        case "south":
                            oppositeDirection = "north";
                            break;
                        case "west":
                            oppositeDirection = "east";
                            break;
                        default:
                            oppositeDirection = "north"; // default case, should not be hit
                            break;
                    }
                    Door previousRoomDoor = new Door(this, Structure.Rooms[Position - 1], oppositeDirection, null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[Position - 1].Objects.Add(previousRoomDoor);
                }
            }
            else if (Structure.Type == "keep")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                if (Position == 0)
                {
                    // Add entrance door for the first room
                    AddObject("door", m);
                }
                else
                {
                    // Randomly attach this room to any of the previous rooms
                    int randomPreviousRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create door from current room to previous room
                    Door newDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(newDoor);

                    // Create door from previous room to current room in the opposite direction
                    string oppositeDirection;
                    switch (direction)
                    {
                        case "north":
                            oppositeDirection = "south";
                            break;
                        case "east":
                            oppositeDirection = "west";
                            break;
                        case "south":
                            oppositeDirection = "north";
                            break;
                        case "west":
                            oppositeDirection = "east";
                            break;
                        case "up":
                            oppositeDirection = "down";
                            break;
                        case "down":
                            oppositeDirection = "up";
                            break;
                        default:
                            oppositeDirection = "north"; // default case, should not be hit
                            break;
                    }
                    Door previousRoomDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, oppositeDirection, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[randomPreviousRoomIndex].Objects.Add(previousRoomDoor);
                }
            }
            else if (Structure.Type == "monastery")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                if (Position == 0)
                {
                    // Add entrance door for the first room
                    AddObject("door", m);
                }
                else
                {
                    // Randomly attach this room to any of the previous rooms
                    int randomPreviousRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create door from current room to previous room
                    Door newDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(newDoor);

                    // Create door from previous room to current room in the opposite direction
                    string oppositeDirection;
                    switch (direction)
                    {
                        case "north":
                            oppositeDirection = "south";
                            break;
                        case "east":
                            oppositeDirection = "west";
                            break;
                        case "south":
                            oppositeDirection = "north";
                            break;
                        case "west":
                            oppositeDirection = "east";
                            break;
                        case "up":
                            oppositeDirection = "down";
                            break;
                        case "down":
                            oppositeDirection = "up";
                            break;
                        default:
                            oppositeDirection = "north"; // default case, should not be hit
                            break;
                    }
                    Door previousRoomDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, oppositeDirection, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[randomPreviousRoomIndex].Objects.Add(previousRoomDoor);
                }
            }

            else if (Structure.Type == "monument")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                if (Position == 0)
                {
                    // Add entrance door for the first room
                    AddObject("door", m);
                }
                else
                {
                    // Randomly attach this room to any of the previous rooms
                    int randomPreviousRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create door from current room to previous room
                    Door newDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(newDoor);

                    // Create door from previous room to current room in the opposite direction
                    string oppositeDirection;
                    switch (direction)
                    {
                        case "north":
                            oppositeDirection = "south";
                            break;
                        case "east":
                            oppositeDirection = "west";
                            break;
                        case "south":
                            oppositeDirection = "north";
                            break;
                        case "west":
                            oppositeDirection = "east";
                            break;
                        case "up":
                            oppositeDirection = "down";
                            break;
                        case "down":
                            oppositeDirection = "up";
                            break;
                        default:
                            oppositeDirection = "north"; // default case, should not be hit
                            break;
                    }
                    Door previousRoomDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, oppositeDirection, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[randomPreviousRoomIndex].Objects.Add(previousRoomDoor);
                }
            }

            else if (Structure.Type == "dock")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                if (Position == 0)
                {
                    // Add entrance door for the first room
                    AddObject("door", m);
                }
                else
                {
                    // Randomly attach this room to any of the previous rooms
                    int randomPreviousRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create door from current room to previous room
                    Door newDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(newDoor);

                    // Create door from previous room to current room in the opposite direction
                    string oppositeDirection;
                    switch (direction)
                    {
                        case "north":
                            oppositeDirection = "south";
                            break;
                        case "east":
                            oppositeDirection = "west";
                            break;
                        case "south":
                            oppositeDirection = "north";
                            break;
                        case "west":
                            oppositeDirection = "east";
                            break;
                        case "up":
                            oppositeDirection = "down";
                            break;
                        case "down":
                            oppositeDirection = "up";
                            break;
                        default:
                            oppositeDirection = "north"; // default case, should not be hit
                            break;
                    }
                    Door previousRoomDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, oppositeDirection, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[randomPreviousRoomIndex].Objects.Add(previousRoomDoor);
                }
            }
            else if (Structure.Type == "hoard")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                if (Position == 0)
                {
                    // Add entrance door for the first room
                    AddObject("door", m);
                }
                else
                {
                    // Randomly attach this room to any of the previous rooms
                    int randomPreviousRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create door from current room to previous room
                    Door newDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(newDoor);

                    // Create door from previous room to current room in the opposite direction
                    string oppositeDirection;
                    switch (direction)
                    {
                        case "north":
                            oppositeDirection = "south";
                            break;
                        case "east":
                            oppositeDirection = "west";
                            break;
                        case "south":
                            oppositeDirection = "north";
                            break;
                        case "west":
                            oppositeDirection = "east";
                            break;
                        case "up":
                            oppositeDirection = "down";
                            break;
                        case "down":
                            oppositeDirection = "up";
                            break;
                        default:
                            oppositeDirection = "north"; // default case, should not be hit
                            break;
                    }
                    Door previousRoomDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, oppositeDirection, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[randomPreviousRoomIndex].Objects.Add(previousRoomDoor);
                }
            }

            else if (Structure.Type == "mound")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                if (Position == 0)
                {
                    // Add entrance door for the first room
                    AddObject("door", m);
                }
                else
                {
                    // Randomly attach this room to any of the previous rooms
                    int randomPreviousRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create door from current room to previous room
                    Door newDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(newDoor);

                    // Create door from previous room to current room in the opposite direction
                    string oppositeDirection;
                    switch (direction)
                    {
                        case "north":
                            oppositeDirection = "south";
                            break;
                        case "east":
                            oppositeDirection = "west";
                            break;
                        case "south":
                            oppositeDirection = "north";
                            break;
                        case "west":
                            oppositeDirection = "east";
                            break;
                        case "up":
                            oppositeDirection = "down";
                            break;
                        case "down":
                            oppositeDirection = "up";
                            break;
                        default:
                            oppositeDirection = "north"; // default case, should not be hit
                            break;
                    }
                    Door previousRoomDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, oppositeDirection, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[randomPreviousRoomIndex].Objects.Add(previousRoomDoor);
                }
            }
            else if (Structure.Type == "pyramid")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);
                int TotalRooms = Structure.Rooms.Count;

                // Determine the floor and position on the floor
                int firstFloorCount = TotalRooms / 2;           // 3/6 of the rooms
                int secondFloorCount = TotalRooms / 3;          // 2/6 of the rooms
                int thirdFloorCount = TotalRooms - firstFloorCount - secondFloorCount; // 1/6 of the rooms

                if (Position < firstFloorCount)
                {
                    // First floor: connect rooms horizontally
                    if (Position > 0)
                    {
                        Door previousRoomDoor = new Door(this, Structure.Rooms[Position - 1], "west", null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                        Structure.Rooms[Position - 1].Objects.Add(previousRoomDoor);
                        Door currentRoomDoor = new Door(this, Structure.Rooms[Position - 1], "east", null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                        Objects.Add(currentRoomDoor);
                    }
                }
                else if (Position < firstFloorCount + secondFloorCount)
                {
                    // Second floor: connect rooms horizontally
                    int secondFloorPosition = Position - firstFloorCount;
                    if (secondFloorPosition > 0)
                    {
                        Door previousRoomDoor = new Door(this, Structure.Rooms[Position - 1], "west", null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                        Structure.Rooms[Position - 1].Objects.Add(previousRoomDoor);
                        Door currentRoomDoor = new Door(this, Structure.Rooms[Position - 1], "east", null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                        Objects.Add(currentRoomDoor);
                    }
                }
                else
                {
                    // Third floor: connect rooms horizontally
                    int thirdFloorPosition = Position - firstFloorCount - secondFloorCount;
                    if (thirdFloorPosition > 0)
                    {
                        Door previousRoomDoor = new Door(this, Structure.Rooms[Position - 1], "west", null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                        Structure.Rooms[Position - 1].Objects.Add(previousRoomDoor);
                        Door currentRoomDoor = new Door(this, Structure.Rooms[Position - 1], "east", null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                        Objects.Add(currentRoomDoor);
                    }
                }

                // Connect floors vertically
                if (Position == firstFloorCount - 1)
                {
                    // Last room of the first floor connects to the first room of the second floor
                    Door upDoor = new Door(this, Structure.Rooms[Position + 1], "up", null, "staircase", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(upDoor);
                    Door downDoor = new Door(Structure.Rooms[Position + 1], this, "down", null, "staircase", new List<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[Position + 1].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[Position + 1].Objects.Add(downDoor);
                }
                else if (Position == firstFloorCount + secondFloorCount - 1)
                {
                    // Last room of the second floor connects to the first room of the third floor
                    Door upDoor = new Door(this, Structure.Rooms[Position + 1], "up", null, "staircase", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(upDoor);
                    Door downDoor = new Door(Structure.Rooms[Position + 1], this, "down", null, "staircase", new List<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[Position + 1].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[Position + 1].Objects.Add(downDoor);
                }
            }
            else if (Structure.Type == "scaffold")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                if (Position == 0)
                {
                    // Add entrance door for the first room
                    AddObject("door", m);
                }
                else
                {
                    // Randomly attach this room to any of the previous rooms
                    int randomPreviousRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create door from current room to previous room
                    Door newDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(newDoor);

                    // Create door from previous room to current room in the opposite direction
                    string oppositeDirection;
                    switch (direction)
                    {
                        case "north":
                            oppositeDirection = "south";
                            break;
                        case "east":
                            oppositeDirection = "west";
                            break;
                        case "south":
                            oppositeDirection = "north";
                            break;
                        case "west":
                            oppositeDirection = "east";
                            break;
                        case "up":
                            oppositeDirection = "down";
                            break;
                        case "down":
                            oppositeDirection = "up";
                            break;
                        default:
                            oppositeDirection = "north"; // default case, should not be hit
                            break;
                    }
                    Door previousRoomDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, oppositeDirection, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[randomPreviousRoomIndex].Objects.Add(previousRoomDoor);
                }
            }
            else if (Structure.Type == "scum")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                if (Position == 0)
                {
                    // Add entrance door for the first room
                    AddObject("door", m);
                }
                else
                {
                    // Randomly attach this room to any of the previous rooms
                    int randomPreviousRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create door from current room to previous room
                    Door newDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(newDoor);

                    // Create door from previous room to current room in the opposite direction
                    string oppositeDirection;
                    switch (direction)
                    {
                        case "north":
                            oppositeDirection = "south";
                            break;
                        case "east":
                            oppositeDirection = "west";
                            break;
                        case "south":
                            oppositeDirection = "north";
                            break;
                        case "west":
                            oppositeDirection = "east";
                            break;
                        case "up":
                            oppositeDirection = "down";
                            break;
                        case "down":
                            oppositeDirection = "up";
                            break;
                        default:
                            oppositeDirection = "north"; // default case, should not be hit
                            break;
                    }
                    Door previousRoomDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, oppositeDirection, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[randomPreviousRoomIndex].Objects.Add(previousRoomDoor);
                }
            }
            else if (Structure.Type == "ship")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                if (Position == 0)
                {
                    // Add entrance door for the first room
                    AddObject("door", m);
                }
                else
                {
                    // Randomly attach this room to any of the previous rooms
                    int randomPreviousRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create door from current room to previous room
                    Door newDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(newDoor);

                    // Create door from previous room to current room in the opposite direction
                    string oppositeDirection;
                    switch (direction)
                    {
                        case "north":
                            oppositeDirection = "south";
                            break;
                        case "east":
                            oppositeDirection = "west";
                            break;
                        case "south":
                            oppositeDirection = "north";
                            break;
                        case "west":
                            oppositeDirection = "east";
                            break;
                        case "up":
                            oppositeDirection = "down";
                            break;
                        case "down":
                            oppositeDirection = "up";
                            break;
                        default:
                            oppositeDirection = "north"; // default case, should not be hit
                            break;
                    }
                    Door previousRoomDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, oppositeDirection, null, direction == "up" || direction == "down" ? "staircase" : "door", new List<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[randomPreviousRoomIndex].Objects.Add(previousRoomDoor);
                }
            }

            else if (Structure.Type == "toroid")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);
                int TotalRooms = Structure.Rooms.Count;

                // Horizontal directions
                string[] horizontalDirections = new string[] { "north", "east", "south", "west" };

                // Connect rooms in a linear path
                if (Position < TotalRooms - 1)
                {
                    // Determine the direction randomly for the first room
                    string direction = horizontalDirections[Game1.r.Next(horizontalDirections.Length)];

                    // Create door to the next room
                    Door nextRoomDoor = new Door(this, Structure.Rooms[Position + 1], direction, null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(nextRoomDoor);

                    // Create door from the next room back to this room in the opposite direction
                    string oppositeDirection;
                    switch (direction)
                    {
                        case "north":
                            oppositeDirection = "south";
                            break;
                        case "east":
                            oppositeDirection = "west";
                            break;
                        case "south":
                            oppositeDirection = "north";
                            break;
                        case "west":
                            oppositeDirection = "east";
                            break;
                        default:
                            oppositeDirection = "north"; // default case, should not be hit
                            break;
                    }
                    Door previousRoomDoor = new Door(Structure.Rooms[Position + 1], this, oppositeDirection, null, "door", new List<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[Position + 1].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[Position + 1].Objects.Add(previousRoomDoor);
                }
                else
                {
                    // Connect the last room back to the first room
                    string direction = horizontalDirections[Game1.r.Next(horizontalDirections.Length)];

                    // Create door to the first room
                    Door firstRoomDoor = new Door(this, Structure.Rooms[0], direction, null, "door", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(firstRoomDoor);

                    // Create door from the first room back to the last room in the opposite direction
                    string oppositeDirection;
                    switch (direction)
                    {
                        case "north":
                            oppositeDirection = "south";
                            break;
                        case "east":
                            oppositeDirection = "west";
                            break;
                        case "south":
                            oppositeDirection = "north";
                            break;
                        case "west":
                            oppositeDirection = "east";
                            break;
                        default:
                            oppositeDirection = "north"; // default case, should not be hit
                            break;
                    }
                    Door previousRoomDoor = new Door(Structure.Rooms[0], this, oppositeDirection, null, "door", new List<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[0].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[0].Objects.Add(previousRoomDoor);
                }
            }
            else if (Structure.Type == "tower")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                // Connect rooms in a linear upward progression
                if (Position < Structure.Rooms.Count - 1)
                {
                    // Create an upward staircase to the next room
                    Door upDoor = new Door(this, Structure.Rooms[Position + 1], "up", null, "staircase", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(upDoor);

                    // Create a downward staircase from the next room back to this room
                    Door downDoor = new Door(Structure.Rooms[Position + 1], this, "down", null, "staircase", new List<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[Position + 1].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[Position + 1].Objects.Add(downDoor);
                }
            }
            else if (Structure.Type == "towers")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                // Core room
                if (Position == 0)
                {
                    // Core room setup, no doors needed for now
                }
                else
                {
                    // Attach the room linearly and vertically to a random room
                    List<int> availablePositions = new List<int>();
                    for (int i = 0; i < Position; i++)
                    {
                        availablePositions.Add(i);
                    }

                    int randomPreviousRoomIndex = availablePositions[Game1.r.Next(availablePositions.Count)];

                    // Create an upward staircase to the current room
                    Door upDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, "up", null, "staircase", new List<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[randomPreviousRoomIndex].Objects.Add(upDoor);

                    // Create a downward staircase from the current room back to the previous room
                    Door downDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], "down", null, "staircase", new List<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(downDoor);
                }
            }





            //start populating items





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
            else if (Structure.Type == "shrine")
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
            else if (Structure.Type == "library")
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
            else if (Structure.Type == "tavern")
            {
                for (int i = Game1.r.Next(3, 7); i > 0; i--)
                    AddObject("keg", RoomCiv.CulturalWood, true);

                for (int i = Game1.r.Next(4, 6); i > 0; i--)
                    AddObject("table", RoomCiv.CulturalWood, true);

                for (int i = Game1.r.Next(4, 8); i > 0; i--)
                    AddObject("chair", RoomCiv.CulturalWood, true);
            }
            else if (Structure.Type == "forge")
            {
                for (int i = Game1.r.Next(1, 3); i > 0; i--)
                    AddObject("forge", RoomCiv.CulturalMetal, false);

                for (int i = Game1.r.Next(4, 8); i > 0; i--)
                    AddObject("weapon rack", RoomCiv.CulturalWood, true);

                // Continue with other objects specific to a forge, like tables, chairs, tool racks, etc.
            }
            else if (Structure.Type == "watchtower")
            {
                for (int i = Game1.r.Next(1, 3); i > 0; i--)
                    AddObject("weapon rack", RoomCiv.CulturalWood, true);

                for (int i = Game1.r.Next(0, 3); i > 0; i--)
                    AddObject("chest", RoomCiv.CulturalWood, true);

                // Continue with other objects specific to a watchtower, like tables, etc.
            }
            else if (Structure.Type == "market")
            {
                for (int i = Game1.r.Next(7, 12); i > 0; i--)
                    AddObject("chest", RoomCiv.CulturalWood, true);

                for (int i = Game1.r.Next(4, 6); i > 0; i--)
                    AddObject("table", RoomCiv.CulturalWood, true);

                // Continue with other objects specific to a market, like chairs, etc.
            }
            else if (Structure.Type == "bighouse")
            {
                for (int i = Game1.r.Next(1, 5); i > 0; i--)
                    AddObject("bed", RoomCiv.CulturalWood, true);

                // Continue with other objects specific to a big house, like tables, chairs, etc.
            }
            else if (Structure.Type == "outpost")
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
            else if (Structure.Type == "commune" || Structure.Type == "keep")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                // Add common loot to each room
                Objects.AddRange(Game1.GameWorld.LootTableMachine("general"));

                // Add a small chance for magical loot in some rooms
                if (Game1.r.Next(1, 6) == 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure34"));
                }

                // Add some specific useful items from the object constructor
                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("bookcase", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("table", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("chair", m);
                }

                if (Game1.r.Next(1, 4) == 1)
                {
                    AddObject("altar", m);
                }
            }
            else if (Structure.Type == "monastery")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                // Add common loot to each room
                Objects.AddRange(Game1.GameWorld.LootTableMachine("general"));

                // Add a small chance for magical loot in some rooms
                if (Game1.r.Next(1, 6) == 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure34"));
                }

                // Add some specific useful items from the object constructor
                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("bookcase", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("table", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("chair", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("altar", m);
                }
            }
            else if (Structure.Type == "dock" || Structure.Type == "ship")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalWood;
                int Position = Structure.Rooms.IndexOf(this);

                // Add common loot to each room
                Objects.AddRange(Game1.GameWorld.LootTableMachine("general"));

                // Add a small chance for magical loot in some rooms
                if (Game1.r.Next(1, 6) == 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure34"));
                }

                // Add some specific useful items from the object constructor
                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("table", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("chair", m);
                }

                // Add chests with contained items
                if (Game1.r.Next(1, 3) == 1)
                {
                    Object chest = new Object(null, "chest", new List<Material> { m }, null);

                    // Add some contained items to the chest
                    if (Game1.r.Next(1, 3) == 1)
                    {
                        chest.ContainedObjects.Add(new Object(null, "book", new List<Material> { m }, null));
                    }

                    if (Game1.r.Next(1, 3) == 1)
                    {
                        chest.ContainedObjects.Add(new Object(null, "bottle", new List<Material> { m }, null));
                    }

                    if (Game1.r.Next(1, 3) == 1)
                    {
                        chest.ContainedObjects.Add(new Object(null, "scroll", new List<Material> { m }, null));
                    }

                    Objects.Add(chest);
                }
            }
            else if (Structure.Type == "fortress")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                // Add common loot to each room
                Objects.AddRange(Game1.GameWorld.LootTableMachine("general"));

                // Add a small chance for magical loot in some rooms
                if (Game1.r.Next(1, 8) == 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure34"));
                }

                // Add some specific useful items from the object constructor
                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("table", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("chair", m);
                }

                if (Game1.r.Next(1, 4) == 1)
                {
                    AddObject("weapon rack", m);
                }

                if (Game1.r.Next(1, 4) == 1)
                {
                    AddObject("armor stand", m);
                }

                if (Game1.r.Next(1, 4) == 1)
                {
                    AddObject("bed", m);
                }

                // Add chests with contained items
                if (Game1.r.Next(1, 3) == 1)
                {
                    Object chest = new Object(null, "chest", new List<Material> { m }, null);

                    // Add some contained items to the chest
                    if (Game1.r.Next(1, 3) == 1)
                    {
                        chest.ContainedObjects.Add(new Object(null, "book", new List<Material> { m }, null));
                    }

                    if (Game1.r.Next(1, 3) == 1)
                    {
                        chest.ContainedObjects.Add(new Object(null, "bottle", new List<Material> { m }, null));
                    }

                    if (Game1.r.Next(1, 3) == 1)
                    {
                        chest.ContainedObjects.Add(new Object(null, "scroll", new List<Material> { m }, null));
                    }

                    Objects.Add(chest);
                }
            }
            else if (Structure.Type == "mound")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                // Add a lot of general loot to each room
                for (int i = 0; i < 5; i++) // Increase the number of general loot items
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("general"));
                }

                // Add a very rare chance for something good
                if (Game1.r.Next(1, 20) == 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure34"));
                }
            }
            else if (Structure.Type == "core")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                // Add high-level magic items to each room
                if(Game1.r.Next(1,4) == 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure78"));
                }
            }
            else if (Structure.Type == "monument")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                // Add general loot to each room
                Objects.AddRange(Game1.GameWorld.LootTableMachine("general"));

                // Add a small chance for magical loot in some rooms
                if (Game1.r.Next(1, 8) == 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure34"));
                }

                // Add specific useful items for a monument
                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("pedestal", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("pillar", m);
                }

                if (Game1.r.Next(1, 4) == 1)
                {
                    AddObject("altar", m);
                }

                // Add some decorative items
                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("statue", m); // Assuming "statue" is a valid object type
                }

                if (Game1.r.Next(1, 4) == 1)
                {
                    AddObject("urn", m);
                }
            }

            else if (Structure.Block.District.Location.Type == "observatory")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                // Add general loot to each room
                Objects.AddRange(Game1.GameWorld.LootTableMachine("general"));

                // Add a small chance for magical loot in some rooms
                if (Game1.r.Next(1, 8) == 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure34"));
                }

                // Add some specific useful items from the object constructor
                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("bookcase", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("table", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("chair", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("scroll", m);
                }

                // Add scientific instruments if available in the object constructor
                if (Game1.r.Next(1, 4) == 1)
                {
                    AddObject("jar", m); // Using "jar" as a proxy for scientific instruments
                }
            }
            else if (Structure.Block.District.Location.Type == "library")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                // Add general loot to each room
                Objects.AddRange(Game1.GameWorld.LootTableMachine("general"));

                // Add a small chance for magical loot in some rooms
                if (Game1.r.Next(1, 8) == 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure34"));
                }

                // Add specific useful items for a library
                if (Game1.r.Next(1, 2) == 1)
                {
                    AddObject("bookcase", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("table", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("chair", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("scroll", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("book", m);
                }

                if (Game1.r.Next(1, 4) == 1)
                {
                    AddObject("wax tablet", m);
                }
            }
            else if (Structure.Block.District.Location.Type == "conservatory")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                Material plantMaterial = Game1.GameWorld.Membrane;
                int Position = Structure.Rooms.IndexOf(this);

                // Add pottery items containing plant objects
                if (Game1.r.Next(1, 2) == 1)
                {
                    Object smallPot = new Object(null, "small pot", new List<Material> { m }, null);
                    smallPot.ContainedObjects.Add(new Object(null, "plant", new List<Material> { plantMaterial }, null));
                    Objects.Add(smallPot);
                }

                if (Game1.r.Next(1, 2) == 1)
                {
                    Object bigPot = new Object(null, "big pot", new List<Material> { m }, null);
                    bigPot.ContainedObjects.Add(new Object(null, "plant", new List<Material> { plantMaterial }, null));
                    Objects.Add(bigPot);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    Object smallUrn = new Object(null, "small urn", new List<Material> { m }, null);
                    smallUrn.ContainedObjects.Add(new Object(null, "plant", new List<Material> { plantMaterial }, null));
                    Objects.Add(smallUrn);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    Object bigUrn = new Object(null, "big urn", new List<Material> { m }, null);
                    bigUrn.ContainedObjects.Add(new Object(null, "plant", new List<Material> { plantMaterial }, null));
                    Objects.Add(bigUrn);
                }
            }
            else if (Structure.Block.District.Location.Type == "prison")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                // Add general loot to each room
                Objects.AddRange(Game1.GameWorld.LootTableMachine("general"));

                // Add specific useful items for a prison
                if (Game1.r.Next(1, 2) == 1)
                {
                    AddObject("bed", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("table", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("chair", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("chain", m); // Assuming "chain" represents chains or shackles
                }

                if (Game1.r.Next(1, 4) == 1)
                {
                    AddObject("barrel", m); // Assuming barrels are used for storage in a prison
                }
            }
            else if (Structure.Block.District.Location.Type == "tomb")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                // Add general loot to each room
                Objects.AddRange(Game1.GameWorld.LootTableMachine("general"));

                // Add a small chance for magical loot in some rooms
                if (Game1.r.Next(1, 8) == 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure34"));
                }

                // Add specific useful items for a tomb
                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("altar", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("pedestal", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("urn", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("book", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("scroll", m);
                }

                // Add pottery items
                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("small pot", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("big pot", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("small urn", m);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    AddObject("big urn", m);
                }
            }
            else if (Structure.Block.District.Location.Type == "gallery")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                // Add general loot to each room
                Objects.AddRange(Game1.GameWorld.LootTableMachine("general"));

                // Add a small chance for magical loot in some rooms
                if (Game1.r.Next(1, 8) == 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure34"));
                }

                // Add pedestals for display
                if (Game1.r.Next(1, 2) == 1)
                {
                    AddObject("pedestal", m);
                }

                if (Game1.r.Next(1, 2) == 1)
                {
                    AddObject("pedestal", m);
                }

                if (Game1.r.Next(1, 2) == 1)
                {
                    AddObject("pedestal", m);
                }

                if (Game1.r.Next(1, 2) == 1)
                {
                    AddObject("pedestal", m);
                }

                if (Game1.r.Next(1, 2) == 1)
                {
                    AddObject("pedestal", m);
                }
            }
            else if (Structure.Block.District.Location.Type == "armory")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalMetal;
                int Position = Structure.Rooms.IndexOf(this);

                // Add general loot to each room
                Objects.AddRange(Game1.GameWorld.LootTableMachine("general"));

                // Add a small chance for magical loot in some rooms
                if (Game1.r.Next(1, 8) == 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure34"));
                }

                // Add military equipment
                if (Game1.r.Next(1, 2) == 1)
                {
                    AddObject("sword", m);
                }

                if (Game1.r.Next(1, 2) == 1)
                {
                    AddObject("shield", m);
                }

                if (Game1.r.Next(1, 2) == 1)
                {
                    AddObject("helmet", m);
                }

                if (Game1.r.Next(1, 2) == 1)
                {
                    AddObject("chestplate", m);
                }

                if (Game1.r.Next(1, 2) == 1)
                {
                    AddObject("leggings", m);
                }

                // Store some equipment on armor stands
                if (Game1.r.Next(1, 3) == 1)
                {
                    Object armorStand = new Object(null, "armor stand", new List<Material> { m }, null);
                    if (Game1.r.Next(1, 2) == 1) armorStand.ContainedObjects.Add(new Object(null, "helmet", new List<Material> { m }, null));
                    if (Game1.r.Next(1, 2) == 1) armorStand.ContainedObjects.Add(new Object(null, "chestplate", new List<Material> { m }, null));
                    if (Game1.r.Next(1, 2) == 1) armorStand.ContainedObjects.Add(new Object(null, "leggings", new List<Material> { m }, null));
                    Objects.Add(armorStand);
                }

                // Store some equipment in weapon racks
                if (Game1.r.Next(1, 3) == 1)
                {
                    Object weaponRack = new Object(null, "weapon rack", new List<Material> { m }, null);
                    if (Game1.r.Next(1, 2) == 1) weaponRack.ContainedObjects.Add(new Object(null, "sword", new List<Material> { m }, null));
                    if (Game1.r.Next(1, 2) == 1) weaponRack.ContainedObjects.Add(new Object(null, "shield", new List<Material> { m }, null));
                    Objects.Add(weaponRack);
                }
            }
        }
    }
}
