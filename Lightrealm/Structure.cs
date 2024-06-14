using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Structure : Entity
    {
        public string Type { get; set; }
        public string GUID { get; set; }
        public Entity Owner { get; set; } = null;

        public string FakeIsofractalColor;

        public List<Room> Rooms { get; set; } = new List<Room>();

        public List<Object> HistoricalObjects { get; set; } = new List<Object>();

        public List<Material> Materials { get; set; } = new List<Material>();
        public List<string> PrimarySmells { get; set; } = new List<string>();
        public List<string> LightingMethods { get; set; } = new List<string>();
        public int LightLevelOf5 { get; set; } = 0;
        public int Windows { get; set; }
        public double AgeInYears { get; set; } = 0;

        public int MarketDebt = 0;

        public Deity PrayingDeity;

        public Block Block { get; set; }

        public int XInDistrict { get; set; }
        public int ZInDistrict { get; set; }

        public Structure(string type, List<Object> Objects, List<Room> rooms, Block block, List<Material> materials, List<string> primarySmells, List<string> lightingMethods, int lightLevelOf5, int windows)
        {
            Type = type;

            if(type != "core")
            {
                FakeIsofractalColor = Game1.Colors[Game1.r.Next(Game1.Colors.Count)];
            }
            else
            {
                FakeIsofractalColor = "white";
            }

            GUID = Guid.NewGuid().ToString();
            Rooms = rooms;
            Block = block;

            Block.District.Location.AllStructures.Add(this);

            if(type == "sanctum" || type == "outpost" || type == "spire")
            {
                Name = Block.District.Location.Name;
            }
            else
            {
                Name = Block.District.Location.Region.World.GenerateUniqueName("1S" + (Game1.r.Next(2, 4)) + "s 1S" + (Game1.r.Next(2, 4)) + "s", this);
            }

            if(type == "shrine")
            {
                if (block.District.Location.PrimaryRace == Game1.GameWorld.GetRace("nightfell"))
                {
                    PrayingDeity = Game1.GameWorld.LightDeity;
                }
                else if (block.District.Location.PrimaryRace == Game1.GameWorld.GetRace("luminarch"))
                {
                    PrayingDeity = Game1.GameWorld.DarkDeity;
                }
                else
                {
                    if(Game1.r.Next(1,3) == 1)
                    {
                        PrayingDeity = Game1.GameWorld.LightDeity;
                    }
                    else
                    {
                        PrayingDeity = Game1.GameWorld.DarkDeity;
                    }
                }
            }

            Materials = materials;
            LightLevelOf5 = lightLevelOf5;
            LightingMethods = lightingMethods;
            Windows = windows;

            ReferredToNames = new List<string>() { Name, Name + ", " + type };

            if (Type == "house" || Type == "bighouse")
            {
                int count = Block.Structures.Count(s => s.Type == "house" || s.Type == "bighouse");

                AddReferredToName("house " + (count + 1).ToString());
            }

            //determine smells
        }

        public string GetRoomStructure()
        {
            var graph = new Dictionary<Room, List<Room>>();
            foreach (Room room in Rooms)
            {
                // Initialize the adjacency list for each room
                graph[room] = new List<Room>();
            }

            // Map door connections to room-to-room connections
            foreach (Room room in Rooms)
            {
                foreach (Object obj in room.Objects)
                {
                    if (obj is Door door)
                    {
                        // Assuming Door has a reference to both SourceRoom and DestinationRoom
                        if (!graph[door.SourceRoom].Contains(door.DestinationRoom))
                        {
                            graph[door.SourceRoom].Add(door.DestinationRoom);
                        }
                        if (!graph[door.DestinationRoom].Contains(door.SourceRoom))
                        {
                            graph[door.DestinationRoom].Add(door.SourceRoom);
                        }
                    }
                }
            }

            // Analyze the structure based on the graph
            var visited = new HashSet<Room>();
            var roomLevels = new Dictionary<Room, int>(); // Tracks the vertical level of rooms
            int maxLevel = 0, minLevel = 0; // Track highest and lowest levels for tower and basement detection

            void DFS(Room room, int level)
            {
                visited.Add(room);
                roomLevels[room] = level;
                maxLevel = Math.Max(maxLevel, level);
                minLevel = Math.Min(minLevel, level);

                foreach (var adjacentRoom in graph[room])
                {
                    if (!visited.Contains(adjacentRoom))
                    {
                        // Determine if the connection is vertical or horizontal by door direction
                        var door = room.Objects.OfType<Door>().FirstOrDefault(d => d.DestinationRoom == adjacentRoom);
                        int nextLevel = level;
                        if (door != null && Door.VerticalDoorDirections.Contains(door.Direction))
                        {
                            nextLevel = door.Direction == "up" ? level + 1 : level - 1;
                        }

                        DFS(adjacentRoom, nextLevel);
                    }
                }
            }

            // Start DFS from the first room, assuming it's at ground level (level 0)
            DFS(Rooms[0], 0);

            string description = "The structure features ";

            // Determine if the structure has towers or basements
            if (maxLevel > 1) description += "one or more tall towers, ";
            if (minLevel < 0) description += "underground areas, ";


            int hallwayCount = 0;
            foreach (var room in Rooms)
            {
                var horizontalDoors = room.Objects.OfType<Door>().Count(door => Door.OrthogonalDoorDirections.Contains(door.Direction));
                if (horizontalDoors == 1)
                {
                    // Assuming a hallway if a room has exactly one horizontal door (leading to another room in a linear fashion)
                    hallwayCount++;
                }
            }

            // For simplicity, assuming every two rooms with one horizontal door each count as one hallway
            hallwayCount /= 2;

            if (hallwayCount > 1) description += "a series of long hallways, ";

            // Detect balconies or overhanging rooms
            // Simplified check: Any room with a single 'down' door could be considered as a balcony or overhang
            int balconyCount = Rooms.Count(room => room.Objects.OfType<Door>().Count(door => door.Direction == "down") == 1);
            if (balconyCount > 0) description += "a balcony or overhanging areas, ";

            description = description.TrimEnd(',', ' ') + ".";

            return description;
        }

        public string GetStructureDescription()
        {
            // Random introductory sentences
            List<string> introductions = new List<string>
    {
        $"You see a {Type} called {Name}.",
        $"The {Type} named {Name} stands before you.",
        $"In front of you is a {Type} known as {Name}.",
        $"You come across a {Type} known as {Name}."
    };

            // Select a random introduction
            Random rnd = new Random();
            string introduction = introductions[rnd.Next(introductions.Count)];

            string description = introduction + " ";

            int directionalRoomsCount = 0;
            int verticalRoomsCount = 0;

            // Determine the count of directional and vertical rooms
            foreach (Room room in Rooms)
            {
                foreach (Object obj in room.Objects)
                {
                    if (obj is Door door)
                    {
                        if (Door.OrthogonalDoorDirections.Contains(door.Direction))
                            directionalRoomsCount++;
                        else if (Door.VerticalDoorDirections.Contains(door.Direction))
                            verticalRoomsCount++;
                    }
                }
            }

            // Describe general size
            string sizeDescription = "";
            if (verticalRoomsCount > directionalRoomsCount)
            {
                if (Rooms.Count < 5)
                    sizeDescription = "short";
                else if (Rooms.Count < 10)
                    sizeDescription = "tall";
                else if (Rooms.Count < 20)
                    sizeDescription = "towering";
                else
                    sizeDescription = "skyscraping";
            }
            else
            {
                if (Rooms.Count < 3)
                    sizeDescription = "quite small";
                else if (Rooms.Count < 5)
                    sizeDescription = "averagely sized";
                else if (Rooms.Count < 10)
                    sizeDescription = "fairly large";
                else if (Rooms.Count < 20)
                    sizeDescription = "very expansive";
                else
                    sizeDescription = "absolutely monumental";
            }

            // Describe lighting
            string lightingDescription = LightingMethods.Count > 0 ? $"lit by {String.Join(", ", LightingMethods)}" : "";

            // Describe age
            string ageDescription = "";
            if (AgeInYears < 10)
            {
                ageDescription = "It appears recently made";
            }
            else if (AgeInYears < 50)
            {
                ageDescription = "It has a few signs of wear";
            }
            else if (AgeInYears < 100)
            {
                ageDescription = "It appears rather worn";
            }
            else
            {
                ageDescription = "It is ancient, bearing the marks of ages";
            }

            // Combine descriptions with a 50% chance to alter their order
            bool isLightingFirst = rnd.Next(2) == 0; // 50% chance
            if (isLightingFirst)
            {
                description += $"It is {lightingDescription} and it is {sizeDescription}. {ageDescription}.";
            }
            else
            {
                description += $"{Game1.Capitalize(ageDescription)}, {sizeDescription}, and {lightingDescription}.";
            }

            // Clean up the description
            return description.Replace("..", ".").Trim();
        }



        public Structure()
        {

        }
    }
}
