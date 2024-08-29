using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Room : Entity
    {
        private int _structureId;

        
        public Structure Structure
        {
            get => EntityGet<Structure>(_structureId);
            set => _structureId = value?.ID ?? 0;
        }

        public EntityList<Object> Objects { get; set; } = new EntityList<Object>();

        public EntityList<Object> ObjectsToRemove { get; set; } = new EntityList<Object>();

        public EntityList<Architect> Architects { get; set; } = new EntityList<Architect>();

        public EntityList<Architect> ArchitectsToRemove { get; set; } = new EntityList<Architect>();

        public Room(Structure structure, EntityList<Object> objects, EntityList<Architect> architects, EntityList<Architect> architectsToRemove)
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

        public Door FindQuickestExitDoor()
        {
            if (Structure == null || Structure.Rooms == null || Structure.Rooms.Count() == 0)
            {
                return null;
            }

            Room exitRoom = Structure.Rooms[0];
            if (this == exitRoom)
            {
                return null; // Already in the exit room
            }

            // Use BFS to find the shortest path to the exit room
            Queue<Room> queue = new Queue<Room>();
            Dictionary<Room, Door> cameFrom = new Dictionary<Room, Door>();
            EntityList<Room> visited = new EntityList<Room>();

            queue.Enqueue(this);
            visited.Add(this);

            while (queue.Count() > 0)
            {
                Room currentRoom = queue.Dequeue();

                foreach (Object obj in currentRoom.Objects)
                {
                    if (obj is Door door)
                    {
                        Room nextRoom = door.DestinationRoom;
                        if (!visited.Contains(nextRoom))
                        {
                            visited.Add(nextRoom);
                            queue.Enqueue(nextRoom);
                            cameFrom[nextRoom] = door;

                            if (nextRoom == exitRoom)
                            {
                                // Backtrack to find the first door in the path
                                Door quickestDoor = door;
                                while (cameFrom.ContainsKey(currentRoom) && cameFrom[currentRoom] != null)
                                {
                                    quickestDoor = cameFrom[currentRoom];
                                    currentRoom = quickestDoor.SourceRoom;
                                }
                                return quickestDoor;
                            }
                        }
                    }
                }
            }

            return null; // No path to exit room found
        }

        public Door FindQuickestDoorToRoom(Room targetRoom)
        {
            if (Structure == null || Structure.Rooms == null || Structure.Rooms.Count() == 0 || targetRoom == null)
            {
                return null;
            }

            if (this == targetRoom)
            {
                return null; // Already in the target room
            }

            // Use BFS to find the shortest path to the target room
            Queue<Room> queue = new Queue<Room>();
            Dictionary<Room, Door> cameFrom = new Dictionary<Room, Door>();
            EntityList<Room> visited = new EntityList<Room>();

            queue.Enqueue(this);
            visited.Add(this);

            while (queue.Count() > 0)
            {
                Room currentRoom = queue.Dequeue();

                foreach (Object obj in currentRoom.Objects)
                {
                    if (obj is Door door)
                    {
                        Room nextRoom = door.DestinationRoom;
                        if (!visited.Contains(nextRoom))
                        {
                            visited.Add(nextRoom);
                            queue.Enqueue(nextRoom);
                            cameFrom[nextRoom] = door;

                            if (nextRoom == targetRoom)
                            {
                                // Backtrack to find the first door in the path
                                Door quickestDoor = door;
                                while (cameFrom.ContainsKey(currentRoom) && cameFrom[currentRoom] != null)
                                {
                                    quickestDoor = cameFrom[currentRoom];
                                    currentRoom = quickestDoor.SourceRoom;
                                }
                                return quickestDoor;
                            }
                        }
                    }
                }
            }

            return null; // No path to target room found
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
                Object o = new Object(null, type, new EntityList<Material> { material }, null);
                o.Room = this; o.Block = this.Structure.Block;
                Objects.Add(o);
            }

            if (Structure.Type == "spire")
            {
                Material m = Structure.Block.District.Location.PrimaryRace == Game1.GameWorld.GetRace("nightfell") ? Game1.GameWorld.Darkstone : Game1.GameWorld.Illuminite;
                int Position = Structure.Rooms.IndexOf(this);

                // Add exit door if it's the first room
                if (Position == 0)
                    AddObject("exit door", m);

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
                    Door downStaircase = new Door(this, Structure.Rooms[Position - 1], "down", null, "spiral staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(downStaircase);

                    Door upStaircase = new Door(Structure.Rooms[Position - 1], this, "up", null, "spiral staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[Position - 1].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[Position - 1].Objects.Add(upStaircase);
                }

                if (Position == Structure.Rooms.Count() - 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure78"));
                }

                if (Game1.r.Next(1, 4) == 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("general"));
                }
            }
            else if (Structure.Type == "tower")
            {
                Material m = Game1.GameWorld.Stones[Game1.r.Next(Game1.GameWorld.Stones.Count())];
                int Position = Structure.Rooms.IndexOf(this);

                // Add exit door if it's the first room
                if (Position == 0)
                    AddObject("exit door", m);

                // Add random number of bookcases
                for (int i = Game1.r.Next(0, 2); i > 0; i--)
                    AddObject("bookcase", m);

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
                    Door downStaircase = new Door(this, Structure.Rooms[Position - 1], "down", null, "spiral staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(downStaircase);

                    Door upStaircase = new Door(Structure.Rooms[Position - 1], this, "up", null, "spiral staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[Position - 1].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[Position - 1].Objects.Add(upStaircase);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("general"));
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
                    AddObject("exit door", Game1.GameWorld.Glass);  // Entrance door for the first room

                    // Door leading to the next room
                    string ForwardDirection = Door.AllDoorDirections[Game1.r.Next(Door.AllDoorDirections.Count())];
                    Door forwardDoor = new Door(this, Structure.Rooms[Position + 1], ForwardDirection, null, "archway", new EntityList<Material> { Game1.GameWorld.Archaeon }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(forwardDoor);
                }
                else if (Position == Structure.Rooms.Count() - 1)
                {
                    // Last room specific setup
                    // Door leading back to the previous room
                    string BackwardDirection = Door.AllDoorDirections[Game1.r.Next(Door.AllDoorDirections.Count())];
                    Door backwardDoor = new Door(this, Structure.Rooms[Position - 1], BackwardDirection, null, "archway", new EntityList<Material> { Game1.GameWorld.Archaeon }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(backwardDoor);

                    // Logic to find and place an artifact in the final room of the sanctum
                    Object Artifact = null;
                    foreach (Object o in Structure.Block.District.Location.UnplacedArtifacts)
                    {
                        if (Game1.GameWorld.AllLegendarySpells.Contains(o.SpecialKnowledge))
                        {
                            Structure.Block.District.Location.UnplacedArtifacts.Remove(o);
                            Artifact = o;
                            break;
                        }
                    }

                    if (Artifact != null)
                    {
                        Object pedestal = new Object(null, "pedestal", new EntityList<Material>() { Game1.GameWorld.Archaeon }, null);
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
                    string BackwardDirection = Door.AllDoorDirections[Game1.r.Next(Door.AllDoorDirections.Count())];
                    Door backwardDoor = new Door(this, Structure.Rooms[Position - 1], BackwardDirection, null, "archway", new EntityList<Material> { Game1.GameWorld.Archaeon }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(backwardDoor);

                    // Door leading to the next room
                    string ForwardDirection = Door.AllDoorDirections[Game1.r.Next(Door.AllDoorDirections.Count())];
                    Door forwardDoor = new Door(this, Structure.Rooms[Position + 1], ForwardDirection, null, "archway", new EntityList<Material> { Game1.GameWorld.Archaeon }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(forwardDoor);
                }
            }

            else if (Structure.Block.District.Location.Layout == "archway")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);
                int TotalRooms = Structure.Rooms.Count();
                int ThirdRooms = TotalRooms / 3;

                string GetOppositeDirection(string direction)
                {
                    switch (direction.ToLower())
                    {
                        case "north": return "south";
                        case "south": return "north";
                        case "east": return "west";
                        case "west": return "east";
                        case "up": return "down";
                        case "down": return "up";
                        default: throw new ArgumentException("Invalid direction", nameof(direction));
                    }
                }

                if (Position == 0)
                {
                    // Room X: Entrance door and door up to the next room
                    AddObject("exit door", m);
                    Door upStaircase = new Door(this, Structure.Rooms[Position + 1], "up", null, "staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, this.NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(upStaircase);
                }
                else if (Position < ThirdRooms - 1)
                {
                    // Left side O's: Up door to the next room and down door to the previous
                    Door upStaircase = new Door(this, Structure.Rooms[Position + 1], "up", null, "staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, this.NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(upStaircase);
                    Door downStaircase = new Door(this, Structure.Rooms[Position - 1], "down", null, "staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, this.NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(downStaircase);
                }
                else if (Position == ThirdRooms - 1)
                {
                    // Room F: Door in Direction A to the next room and down door to the previous
                    string directionA = new List<string> { "north", "east", "south", "west" }[Game1.r.Next(4)];
                    Door directionADoor = new Door(this, Structure.Rooms[Position + 1], directionA, null, "door", new EntityList<Material> { m }, false, false, null, null, 255, false, this.NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(directionADoor);
                    Door downStaircase = new Door(this, Structure.Rooms[Position - 1], "down", null, "staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, this.NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(downStaircase);
                }
                else if (Position < 2 * ThirdRooms - 1)
                {
                    // Top O's: Direction A door to the next room and Direction B door to the previous
                    Door previousDirectionADoor = Structure.Rooms[Position - 1].Objects.OfType<Door>().FirstOrDefault(d => d.Type == "door" && (d.Direction == "north" || d.Direction == "east" || d.Direction == "south" || d.Direction == "west"));
                    if (previousDirectionADoor == null)
                    {
                        throw new Exception("Previous direction door not found.");
                    }
                    string directionA = previousDirectionADoor.Direction;
                    string directionB = GetOppositeDirection(directionA);

                    Door directionADoor = new Door(this, Structure.Rooms[Position + 1], directionA, null, "door", new EntityList<Material> { m }, false, false, null, null, 255, false, this.NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(directionADoor);
                    Door directionBDoor = new Door(this, Structure.Rooms[Position - 1], directionB, null, "door", new EntityList<Material> { m }, false, false, null, null, 255, false, this.NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(directionBDoor);
                }
                else if (Position == 2 * ThirdRooms - 1)
                {
                    // Room C: Direction B door to the previous and down door to the next set of O's
                    Door previousDirectionADoor = Structure.Rooms[Position - 1].Objects.OfType<Door>().FirstOrDefault(d => d.Type == "door" && (d.Direction == "north" || d.Direction == "east" || d.Direction == "south" || d.Direction == "west"));
                    if (previousDirectionADoor == null)
                    {
                        throw new Exception("Previous direction door not found.");
                    }
                    string directionA = previousDirectionADoor.Direction;
                    string directionB = GetOppositeDirection(directionA);

                    Door directionBDoor = new Door(this, Structure.Rooms[Position - 1], directionB, null, "door", new EntityList<Material> { m }, false, false, null, null, 255, false, this.NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(directionBDoor);
                    Door downStaircase = new Door(this, Structure.Rooms[Position + 1], "down", null, "staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, this.NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(downStaircase);
                }
                else if (Position < TotalRooms - 1)
                {
                    // Right side O's: Down door to the next room and up door to the previous
                    Door downStaircase = new Door(this, Structure.Rooms[Position + 1], "down", null, "staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, this.NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(downStaircase);
                    Door upStaircase = new Door(this, Structure.Rooms[Position - 1], "up", null, "staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, this.NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(upStaircase);
                }
                else
                {
                    // Room D: Exit door and up door to the previous
                    AddObject("exit door", m);
                    Door upStaircase = new Door(this, Structure.Rooms[Position - 1], "up", null, "staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, this.NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(upStaircase);
                }
            }

            else if (Structure.Type == "core" || Structure.Type == "heart")
            {
                Material m = Structure.Type == "core" ? Structure.Block.District.Location.HomeCivilization.CulturalGemstone : Game1.GameWorld.ShadeSludge;
                int Position = Structure.Rooms.IndexOf(this);

                // Core room does not create doors
                if (Position == 0)
                {
                    // Add entrance door
                    AddObject("exit door", m);
                }
                else
                {
                    // Randomly attach this room to any of the existing rooms
                    int randomExistingRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create door from current room to the random existing room
                    Door newDoor = new Door(this, Structure.Rooms[randomExistingRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(newDoor);

                    // Create door from the random existing room to the current room in the opposite direction
                    string oppositeDirection = OppositeDirection(direction);
                    Door returnRoomDoor = new Door(Structure.Rooms[randomExistingRoomIndex], this, oppositeDirection, null, oppositeDirection == "up" || oppositeDirection == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomExistingRoomIndex].NumberOfDoors(), Structure.Block, Structure, Structure.Rooms[randomExistingRoomIndex]);
                    Structure.Rooms[randomExistingRoomIndex].Objects.Add(returnRoomDoor);
                }

                string OppositeDirection(string direction)
                {
                    switch (direction)
                    {
                        case "north": return "south";
                        case "east": return "west";
                        case "south": return "north";
                        case "west": return "east";
                        case "up": return "down";
                        case "down": return "up";
                        default: return "";
                    }
                }
            }

            else if (Structure.Type == "fortress" || Structure.Type == "monument" || Structure.Type == "stronghold")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                string GetOppositeDirection(string direction)
                {
                    switch (direction.ToLower())
                    {
                        case "north":
                            return "south";
                        case "south":
                            return "north";
                        case "east":
                            return "west";
                        case "west":
                            return "east";
                        case "up":
                            return "down";
                        case "down":
                            return "up";
                        default:
                            throw new ArgumentException("Invalid direction", nameof(direction));
                    }
                }

                // First room (core room) setup
                if (Position == 0)
                {
                    // Add exit door
                    AddObject("exit door", m);
                }
                else // Attach to an existing room
                {
                    // Determine the range of rooms to bias towards
                    int lowerBound = Math.Max(0, Position - 3);
                    int randomIndex = Game1.r.Next(lowerBound, Position); // Ensure we only select rooms that have been processed

                    // Pick a random direction to connect to the selected room
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create a door in the current room, connecting to the selected room
                    Door newDoor = new Door(this, Structure.Rooms[randomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, this.NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(newDoor);

                    // Add opposite door in the connected room
                    string oppositeDirection = GetOppositeDirection(direction);
                    Door oppositeDoor = new Door(Structure.Rooms[randomIndex], this, oppositeDirection, null, oppositeDirection == "up" || oppositeDirection == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomIndex].NumberOfDoors(), Structure.Block, Structure, Structure.Rooms[randomIndex]);
                    Structure.Rooms[randomIndex].Objects.Add(oppositeDoor);
                }
            }


            else if (Structure.Block.District.Location.Layout == "hallway")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                string GetOppositeDirection(string direction)
                {
                    switch (direction.ToLower())
                    {
                        case "north":
                            return "south";
                        case "south":
                            return "north";
                        case "east":
                            return "west";
                        case "west":
                            return "east";
                        case "up":
                            return "down";
                        case "down":
                            return "up";
                        default:
                            throw new ArgumentException("Invalid direction", nameof(direction));
                    }
                }

                // Generate the initial direction randomly for the first room
                string[] horizontalDirections = new string[] { "north", "east", "south", "west" };
                string direction = "";
                string oppositeDirection = "";

                if (Position == 0)
                {
                    // First room setup: Add exit door
                    AddObject("exit door", m);
                }
                else if (Position == 1)
                {
                    // For the second room, generate a random primary direction
                    direction = horizontalDirections[Game1.r.Next(horizontalDirections.Length)];
                    oppositeDirection = GetOppositeDirection(direction);

                    // Create door to the previous room
                    Door newDoor = new Door(this, Structure.Rooms[Position - 1], oppositeDirection, null, "door", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(newDoor);

                    // Create door in the previous room connecting back to the current room
                    Door previousRoomDoor = new Door(Structure.Rooms[Position - 1], this, direction, null, "door", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[Position - 1].NumberOfDoors(), Structure.Block, Structure, Structure.Rooms[Position - 1]);
                    Structure.Rooms[Position - 1].Objects.Add(previousRoomDoor);
                }
                else
                {
                    // For all subsequent rooms, scan the previous room for the primary direction
                    Room previousRoom = Structure.Rooms[Position - 1];
                    Door primaryDoor = previousRoom.Objects.OfType<Door>().FirstOrDefault();
                    if (primaryDoor != null)
                    {
                        direction = GetOppositeDirection(primaryDoor.Direction);
                        oppositeDirection = primaryDoor.Direction;

                        // Create door to the previous room
                        Door newDoor = new Door(this, previousRoom, oppositeDirection, null, "door", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                        Objects.Add(newDoor);

                        // Create door in the previous room connecting back to the current room
                        Door previousRoomDoor = new Door(previousRoom, this, direction, null, "door", new EntityList<Material> { m }, false, false, null, null, 255, false, previousRoom.NumberOfDoors(), Structure.Block, Structure, previousRoom);
                        previousRoom.Objects.Add(previousRoomDoor);
                    }
                }
            }
            else if (Structure.Type == "keep")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                if (Position == 0)
                {
                    // Add entrance door for the first room
                    AddObject("exit door", m);
                }
                else
                {
                    // Randomly attach this room to any of the previous rooms
                    int randomPreviousRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create door from current room to previous room
                    Door newDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
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
                    Door previousRoomDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, oppositeDirection, null, direction == "up" || direction == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
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
                    AddObject("exit door", m);
                }
                else
                {
                    // Randomly attach this room to any of the previous rooms
                    int randomPreviousRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create door from current room to previous room
                    Door newDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
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
                    Door previousRoomDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, oppositeDirection, null, direction == "up" || direction == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
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
                    AddObject("exit door", m);
                }
                else
                {
                    // Randomly attach this room to any of the previous rooms
                    int randomPreviousRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create door from current room to previous room
                    Door newDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
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
                    Door previousRoomDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, oppositeDirection, null, direction == "up" || direction == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
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
                    AddObject("exit door", m);
                }
                else
                {
                    // Randomly attach this room to any of the previous rooms
                    int randomPreviousRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create door from current room to previous room
                    Door newDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
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
                    Door previousRoomDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, oppositeDirection, null, direction == "up" || direction == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
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
                    AddObject("exit door", m);
                }
                else
                {
                    // Randomly attach this room to any of the previous rooms
                    int randomPreviousRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create door from current room to previous room
                    Door newDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
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
                    Door previousRoomDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, oppositeDirection, null, direction == "up" || direction == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[randomPreviousRoomIndex].Objects.Add(previousRoomDoor);
                }
            }
            else if (Structure.Block.District.Location.Layout == "pyramid" || Structure.Type == "commune")
            {
                string GetOppositeDirection(string direction)
                {
                    switch (direction.ToLower())
                    {
                        case "north":
                            return "south";
                        case "south":
                            return "north";
                        case "east":
                            return "west";
                        case "west":
                            return "east";
                        case "up":
                            return "down";
                        case "down":
                            return "up";
                        default:
                            throw new ArgumentException("Invalid direction", nameof(direction));
                    }
                }
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);
                int TotalRooms = Structure.Rooms.Count();

                // Determine the floor and position on the floor
                int firstFloorCount = TotalRooms / 2;           // 1/2 of the rooms
                int secondFloorCount = (TotalRooms - firstFloorCount) * 2 / 3; // 2/3 of the remaining rooms
                int thirdFloorCount = TotalRooms - firstFloorCount - secondFloorCount; // The rest of the rooms

                if (Position == 0)
                {
                    // First room (core room) setup
                    AddObject("exit door", m);
                }
                else
                {
                    // Connect to a room on the same floor or to a previous room
                    int startIndex, endIndex;

                    if (Position < firstFloorCount)
                    {
                        startIndex = 0;
                        endIndex = firstFloorCount;
                    }
                    else if (Position < firstFloorCount + secondFloorCount)
                    {
                        startIndex = firstFloorCount;
                        endIndex = firstFloorCount + secondFloorCount;
                    }
                    else
                    {
                        startIndex = firstFloorCount + secondFloorCount;
                        endIndex = TotalRooms;
                    }

                    int connectIndex = Game1.r.Next(startIndex, Position); // Ensure we only select rooms that have been processed

                    // Pick a random direction to connect to the selected room
                    string direction = new List<string> { "north", "east", "south", "west" }[Game1.r.Next(4)];

                    // Create a door in both rooms, connecting them
                    Door newDoor = new Door(this, Structure.Rooms[connectIndex], direction, null, "door", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(newDoor);

                    // Add opposite door in the connected room
                    string oppositeDirection = GetOppositeDirection(direction);
                    Door oppositeDoor = new Door(Structure.Rooms[connectIndex], this, oppositeDirection, null, "door", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[connectIndex].NumberOfDoors(), Structure.Block, Structure, Structure.Rooms[connectIndex]);
                    Structure.Rooms[connectIndex].Objects.Add(oppositeDoor);
                }

                // Vertical connections between floors
                if (Position == firstFloorCount - 1)
                {
                    // Last room of the first floor connects to the first room of the second floor
                    Door upDoor = new Door(this, Structure.Rooms[Position + 1], "up", null, "staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(upDoor);
                    Door downDoor = new Door(Structure.Rooms[Position + 1], this, "down", null, "staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[Position + 1].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[Position + 1].Objects.Add(downDoor);
                }
                else if (Position == firstFloorCount + secondFloorCount - 1)
                {
                    // Last room of the second floor connects to the first room of the third floor
                    Door upDoor = new Door(this, Structure.Rooms[Position + 1], "up", null, "staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(upDoor);
                    Door downDoor = new Door(Structure.Rooms[Position + 1], this, "down", null, "staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[Position + 1].NumberOfDoors(), Structure.Block, Structure, this);
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
                    AddObject("exit door", m);
                }
                else
                {
                    // Randomly attach this room to any of the previous rooms
                    int randomPreviousRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create door from current room to previous room
                    Door newDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
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
                    Door previousRoomDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, oppositeDirection, null, direction == "up" || direction == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
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
                    AddObject("exit door", m);
                }
                else
                {
                    // Randomly attach this room to any of the previous rooms
                    int randomPreviousRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create door from current room to previous room
                    Door newDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
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
                    Door previousRoomDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, oppositeDirection, null, direction == "up" || direction == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
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
                    AddObject("exit door", m);
                }
                else
                {
                    // Randomly attach this room to any of the previous rooms
                    int randomPreviousRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west", "up", "down" }[Game1.r.Next(6)];

                    // Create door from current room to previous room
                    Door newDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], direction, null, direction == "up" || direction == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
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
                    Door previousRoomDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, oppositeDirection, null, direction == "up" || direction == "down" ? "staircase" : "door", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[randomPreviousRoomIndex].Objects.Add(previousRoomDoor);
                }
            }

            else if (Structure.Block.District.Location.Layout == "toroid")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);
                int TotalRooms = Structure.Rooms.Count();

                // Horizontal directions
                string[] horizontalDirections = new string[] { "north", "east", "south", "west" };

                if(Position == 0)
                {
                    AddObject("exit door", m);
                }

                // Connect rooms in a linear path
                if (Position < TotalRooms - 1)
                {
                    // Determine the direction randomly for the first room
                    string direction = horizontalDirections[Game1.r.Next(horizontalDirections.Length)];

                    // Create door to the next room
                    Door nextRoomDoor = new Door(this, Structure.Rooms[Position + 1], direction, null, "door", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
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
                    Door previousRoomDoor = new Door(Structure.Rooms[Position + 1], this, oppositeDirection, null, "door", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[Position + 1].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[Position + 1].Objects.Add(previousRoomDoor);
                }
                else
                {
                    // Connect the last room back to the first room
                    string direction = horizontalDirections[Game1.r.Next(horizontalDirections.Length)];

                    // Create door to the first room
                    Door firstRoomDoor = new Door(this, Structure.Rooms[0], direction, null, "door", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
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
                    Door previousRoomDoor = new Door(Structure.Rooms[0], this, oppositeDirection, null, "door", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[0].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[0].Objects.Add(previousRoomDoor);
                }
            }
            else if (Structure.Type == "tower")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                if(Position == 0)
                {
                    AddObject("exit door", m);
                }

                // Connect rooms in a linear upward progression
                if (Position < Structure.Rooms.Count() - 1)
                {
                    // Create an upward staircase to the next room
                    Door upDoor = new Door(this, Structure.Rooms[Position + 1], "up", null, "staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(upDoor);

                    // Create a downward staircase from the next room back to this room
                    Door downDoor = new Door(Structure.Rooms[Position + 1], this, "down", null, "staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[Position + 1].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[Position + 1].Objects.Add(downDoor);
                }
            }
            else if (Structure.Block.District.Location.Layout == "towers")
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                // Core room
                if (Position == 0)
                {
                    // Core room setup, no doors needed for now
                    AddObject("exit door", m);
                }
                else
                {
                    // Attach the room linearly and vertically to a random room
                    List<int> availablePositions = new List<int>();
                    for (int i = 0; i < Position; i++)
                    {
                        availablePositions.Add(i);
                    }

                    int randomPreviousRoomIndex = availablePositions[Game1.r.Next(availablePositions.Count())];

                    // Create an upward staircase to the current room
                    Door upDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, "up", null, "staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[randomPreviousRoomIndex].Objects.Add(upDoor);

                    // Create a downward staircase from the current room back to the previous room
                    Door downDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], "down", null, "staircase", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
                    Objects.Add(downDoor);
                }
            }
            else
            {
                Material m = this.Structure.Block.District.Location.HomeCivilization.CulturalStone;
                int Position = Structure.Rooms.IndexOf(this);

                if (Position == 0)
                {
                    // Add entrance door for the first room
                    AddObject("exit door", m);
                }
                else
                {
                    // Randomly attach this room to any of the previous rooms orthogonally
                    int randomPreviousRoomIndex = Game1.r.Next(Position);
                    string direction = new List<string> { "north", "east", "south", "west" }[Game1.r.Next(4)];

                    // Create door from current room to previous room
                    Door newDoor = new Door(this, Structure.Rooms[randomPreviousRoomIndex], direction, null, "door", new EntityList<Material> { m }, false, false, null, null, 255, false, NumberOfDoors(), Structure.Block, Structure, this);
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
                        default:
                            oppositeDirection = "north"; // default case, should not be hit
                            break;
                    }
                    Door previousRoomDoor = new Door(Structure.Rooms[randomPreviousRoomIndex], this, oppositeDirection, null, "door", new EntityList<Material> { m }, false, false, null, null, 255, false, Structure.Rooms[randomPreviousRoomIndex].NumberOfDoors(), Structure.Block, Structure, this);
                    Structure.Rooms[randomPreviousRoomIndex].Objects.Add(previousRoomDoor);
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
            else if (Structure.Type == "outpost" || Structure.Type == "fort" || Structure.Type == "bastion")
            {

                // Add common loot to each room
                Objects.AddRange(Game1.GameWorld.LootTableMachine("general"));

                // Add a small chance for magical loot in some rooms

                if (Game1.r.Next(1, 5) == 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure34"));
                }

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

                string Type = Roomtypes[Game1.r.Next(Roomtypes.Count())];
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
                    Object chest = new Object(null, "chest", new EntityList<Material> { m }, null);

                    // Add some contained items to the chest
                    if (Game1.r.Next(1, 3) == 1)
                    {
                        chest.ContainedObjects.Add(new Object(null, "book", new EntityList<Material> { m }, null));
                    }

                    if (Game1.r.Next(1, 3) == 1)
                    {
                        chest.ContainedObjects.Add(new Object(null, "bottle", new EntityList<Material> { m }, null));
                    }

                    if (Game1.r.Next(1, 3) == 1)
                    {
                        chest.ContainedObjects.Add(new Object(null, "scroll", new EntityList<Material> { m }, null));
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
                    Object chest = new Object(null, "chest", new EntityList<Material> { m }, null);

                    // Add some contained items to the chest
                    if (Game1.r.Next(1, 3) == 1)
                    {
                        chest.ContainedObjects.Add(new Object(null, "book", new EntityList<Material> { m }, null));
                    }

                    if (Game1.r.Next(1, 3) == 1)
                    {
                        chest.ContainedObjects.Add(new Object(null, "bottle", new EntityList<Material> { m }, null));
                    }

                    if (Game1.r.Next(1, 3) == 1)
                    {
                        chest.ContainedObjects.Add(new Object(null, "scroll", new EntityList<Material> { m }, null));
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

                if(Position == 0)
                {
                    int Count = Game1.r.Next(4, 9);
                    for(int I = 0; I < Count; I++)
                    {
                        Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure78"));
                    }
                }
            }
            else if (Structure.Type == "heart")
            {
                Material m = Game1.GameWorld.ShadeSludge;
                int Position = Structure.Rooms.IndexOf(this);

                // Add high-level magic items to each room
                if (Game1.r.Next(1, 4) == 1)
                {
                    Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure78"));
                }

                if (Position == 0)
                {
                    int Count = Game1.r.Next(4, 9);
                    for (int I = 0; I < Count; I++)
                    {
                        Objects.AddRange(Game1.GameWorld.LootTableMachine("magictreasure78"));
                    }
                }

                foreach(Object o in Objects)
                {
                    o.Materials.Clear();
                    o.Materials.Add(Game1.GameWorld.ShadeSludge);
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
                    Object smallPot = new Object(null, "small pot", new EntityList<Material> { m }, null);
                    smallPot.ContainedObjects.Add(new Object(null, "plant", new EntityList<Material> { plantMaterial }, null));
                    Objects.Add(smallPot);
                }

                if (Game1.r.Next(1, 2) == 1)
                {
                    Object bigPot = new Object(null, "big pot", new EntityList<Material> { m }, null);
                    bigPot.ContainedObjects.Add(new Object(null, "plant", new EntityList<Material> { plantMaterial }, null));
                    Objects.Add(bigPot);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    Object smallUrn = new Object(null, "small urn", new EntityList<Material> { m }, null);
                    smallUrn.ContainedObjects.Add(new Object(null, "plant", new EntityList<Material> { plantMaterial }, null));
                    Objects.Add(smallUrn);
                }

                if (Game1.r.Next(1, 3) == 1)
                {
                    Object bigUrn = new Object(null, "big urn", new EntityList<Material> { m }, null);
                    bigUrn.ContainedObjects.Add(new Object(null, "plant", new EntityList<Material> { plantMaterial }, null));
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
                if (Game1.r.Next(1, 5) == 1)
                {
                    AddObject("shortsword", m);
                }

                if (Game1.r.Next(1, 5) == 1)
                {
                    AddObject("shield", m);
                }

                if (Game1.r.Next(1, 5) == 1)
                {
                    AddObject("helmet", m);
                }

                if (Game1.r.Next(1, 5) == 1)
                {
                    AddObject("chestplate", m);
                }

                if (Game1.r.Next(1, 5) == 1)
                {
                    AddObject("leggings", m);
                }

                // Store some equipment on armor stands
                if (Game1.r.Next(1, 3) == 1)
                {
                    Object armorStand = new Object(null, "armor stand", new EntityList<Material> { m }, null);
                    if (Game1.r.Next(1, 2) == 1) armorStand.ContainedObjects.Add(new Object(null, "helmet", new EntityList<Material> { m }, null));
                    if (Game1.r.Next(1, 2) == 1) armorStand.ContainedObjects.Add(new Object(null, "chestplate", new EntityList<Material> { m }, null));
                    if (Game1.r.Next(1, 2) == 1) armorStand.ContainedObjects.Add(new Object(null, "leggings", new EntityList<Material> { m }, null));
                    Objects.Add(armorStand);
                }

                // Store some equipment in weapon racks
                if (Game1.r.Next(1, 3) == 1)
                {
                    Object weaponRack = new Object(null, "weapon rack", new EntityList<Material> { m }, null);
                    if (Game1.r.Next(1, 2) == 1) weaponRack.ContainedObjects.Add(new Object(null, "shortsword", new EntityList<Material> { m }, null));
                    if (Game1.r.Next(1, 2) == 1) weaponRack.ContainedObjects.Add(new Object(null, "shield", new EntityList<Material> { m }, null));
                    Objects.Add(weaponRack);
                }
            }
        }
    }
}
