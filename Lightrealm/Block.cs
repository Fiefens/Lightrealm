using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Block : Entity
    {
        public EntityList<Structure> Structures { get; set; } = new EntityList<Structure>();
        public EntityList<Structure> StructuresToRemove { get; set; } = new EntityList<Structure>();
        public EntityList<Architect> Architects { get; set; } = new EntityList<Architect>();
        public EntityList<Architect> ArchitectsToRemove { get; set; } = new EntityList<Architect>();
        public EntityList<Object> Objects { get; set; } = new EntityList<Object>();
        public EntityList<Object> ObjectsToRemove { get; set; } = new EntityList<Object>();
        public EntityList<Object> ObjectsToAdd { get; set; } = new EntityList<Object>();


        public EntityList<Architect> BuriedArchitects { get; set; } = new EntityList<Architect>();
        public EntityList<Object> BuriedObjects { get; set; } = new EntityList<Object>();

        public Entity SocializationTopic;
        public Entity DiscussionTopic;

        public string Biome { get; set; } = "";

        public int X { get; set; }
        public int Z { get; set; }
        public District District;

        public static readonly Dictionary<(int, int), string> BlockDescriptions = new()
        {
            // Corners
            [(0, 0)] = "the northwesternmost block",
            [(6, 0)] = "the northeasternmost block",
            [(0, 6)] = "the southwesternmost block",
            [(6, 6)] = "the southeasternmost block",

            // Center
            [(3, 3)] = "the central block",

            // Edges
            [(0, 1)] = "just south of the northwesternmost block",
            [(0, 5)] = "just north of the southwesternmost block",
            [(6, 1)] = "just south of the northeasternmost block",
            [(6, 5)] = "just north of the southeasternmost block",

            [(1, 0)] = "one block east of the northwesternmost block",
            [(5, 0)] = "one block west of the northeasternmost block",
            [(1, 6)] = "one block east of the southwesternmost block",
            [(5, 6)] = "one block west of the southeasternmost block",

            [(0, 2)] = "two blocks south of the northwesternmost block",
            [(0, 4)] = "two blocks north of the southwesternmost block",
            [(6, 2)] = "two blocks south of the northeasternmost block",
            [(6, 4)] = "two blocks north of the southeasternmost block",

            [(2, 0)] = "two blocks east of the northwesternmost block",
            [(4, 0)] = "two blocks west of the northeasternmost block",
            [(2, 6)] = "two blocks east of the southwesternmost block",
            [(4, 6)] = "two blocks west of the southeasternmost block",

            [(3, 0)] = "along the northern edge, near the center",
            [(3, 6)] = "along the southern edge, near the center",
            [(0, 3)] = "along the western edge, near the center",
            [(6, 3)] = "along the eastern edge, near the center",

            // Near Center
            [(3, 2)] = "one block north of the central block",
            [(3, 4)] = "one block south of the central block",
            [(2, 3)] = "one block west of the central block",
            [(4, 3)] = "one block east of the central block",

            [(3, 1)] = "two blocks north of the central block",
            [(3, 5)] = "two blocks south of the central block",
            [(1, 3)] = "two blocks west of the central block",
            [(5, 3)] = "two blocks east of the central block",

            [(2, 2)] = "one block northwest of the central block",
            [(4, 2)] = "one block northeast of the central block",
            [(2, 4)] = "one block southwest of the central block",
            [(4, 4)] = "one block southeast of the central block",

            [(1, 1)] = "two blocks northwest of the central block",
            [(5, 1)] = "two blocks northeast of the central block",
            [(1, 5)] = "two blocks southwest of the central block",
            [(5, 5)] = "two blocks southeast of the central block",

            [(1, 2)] = "one block north and two blocks west of the central block",
            [(5, 2)] = "one block north and two blocks east of the central block",
            [(1, 4)] = "one block south and two blocks west of the central block",
            [(5, 4)] = "one block south and two blocks east of the central block",

            [(2, 1)] = "two blocks north and one block west of the central block",
            [(4, 1)] = "two blocks north and one block east of the central block",
            [(2, 5)] = "two blocks south and one block west of the central block",
            [(4, 5)] = "two blocks south and one block east of the central block"
        };


        // Function to retrieve the description
        public string GetTextDescription()
        {
            return BlockDescriptions.TryGetValue((X, Z), out var description)
                ? description
                : $"exactly at ({X}, {Z}), in an undefined zone";
        }

        public Block(int x, int z, District d)
        {
            X = x;
            Z = z;
            District = d;

            Biome = d.Location.Region.Biome;
        }

        public Block()
        {

        }

        public bool HasWell()
        {
            foreach (Object o in Objects)
            {
                if (o.Type == "well")
                {
                    return true;
                }
            }
            return false;
        }

        public (bool, string) HasStructure(string Type)
        {
            foreach (Structure s in Structures)
            {
                if (s.Type == Type)
                {
                    return (true, s.Name);
                }
            }
            return (false, "");
        }


        // Function to calculate the distance between blocks
        int CalculateDistance(Block block1, Block block2)
        {
            int Distance = 0;

            if (block2.District.Location != block1.District.Location)
            {
                Distance += 10 * (int)Math.Round(Vector2.Distance(
                    new Vector2(block2.District.Location.X, block2.District.Location.Z),
                    new Vector2(block1.District.Location.X, block1.District.Location.Z)));
            }
            if (block2.District != block1.District)
            {
                Distance += 7;
            }
            if (block2 != block1)
            {
                Distance += Math.Abs(block1.X - block2.X) + Math.Abs(block1.Z - block2.Z);
            }

            return Distance;
        }

        // Shared variables for distance and result
        (Location, District, Block, Room) DeterminedLocation = (null, null, null, null);
        int minDistance = int.MaxValue;

        // Helper function to search within a specific district
        void SearchDistrict(District district, string thing, Block origin)
        {
            foreach (Block block in district.DistrictMap)
            {
                if (thing == "well" && block.HasWell())
                {
                    int distance = CalculateDistance(origin, block);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        DeterminedLocation = (district.Location, district, block, null);
                    }
                }
                else if (thing == "structure")
                {
                    foreach (Structure s in block.Structures)
                    {
                        int distance = CalculateDistance(origin, block);
                        if (distance < minDistance)
                        {
                            if (s.Block.District.IsLoaded)
                            {
                                Room randomRoom = s.Rooms[Game1.GameWorld.rnd.Next(s.Rooms.Count)];
                                minDistance = distance;
                                DeterminedLocation = (district.Location, district, block, randomRoom);
                            }
                            else
                            {
                                minDistance = distance;
                                DeterminedLocation = (district.Location, district, block, null);
                            }
                        }
                    }
                }
                else if (Game1.GameWorld.StructureTypes.Any(e => e.Metadata == thing))
                {
                    foreach (Structure s in block.Structures)
                    {
                        if (s.Type == thing)
                        {
                            int distance = CalculateDistance(origin, block);
                            if (distance < minDistance)
                            {
                                if (s.Block.District.IsLoaded)
                                {
                                    Room randomRoom = s.Rooms[Game1.GameWorld.rnd.Next(s.Rooms.Count)];
                                    minDistance = distance;
                                    DeterminedLocation = (district.Location, district, block, randomRoom);
                                }
                                else
                                {
                                    minDistance = distance;
                                    DeterminedLocation = (district.Location, district, block, null);
                                }
                            }
                        }
                    }
                }
            }
        }

        // Main function
        public (Location, District, Block, Room) FindNearestThing(string thing)
        {
            DeterminedLocation = (null, null, null, null);
            minDistance = int.MaxValue;

            if (this.District != null)
            {
                SearchDistrict(this.District, thing, this);
            }

            if (this.District?.Location != null)
            {
                foreach (District otherDistrict in this.District.Location.Districts)
                {
                    if (otherDistrict != this.District)
                    {
                        SearchDistrict(otherDistrict, thing, this);
                    }
                }
            }

            foreach (Region region in Game1.GameWorld.WorldMap)
            {
                if (region.Location != null && region.Location != this.District?.Location)
                {
                    foreach (District district in region.Location.Districts)
                    {
                        SearchDistrict(district, thing, this);
                    }
                }
            }

            return DeterminedLocation;
        }

        public (Location, District, Block, Room) FindRandomThingInCurrentDistrict(string thing)
        {
            (Location, District, Block, Room) RandomLocation = (null, null, null, null);

            List<(Location, District, Block, Room)> potentialLocations = new List<(Location, District, Block, Room)>();

            // Get the current district
            District currentDistrict = this.District;

            foreach (Block block in currentDistrict.DistrictMap)
            {
                if (thing == "well" && block.HasWell())
                {
                    potentialLocations.Add((this.District.Location, currentDistrict, block, null));
                }
                else if (thing == "structure")
                {
                    foreach (Structure s in block.Structures)
                    {
                        if (s.Block.District.IsLoaded)
                        {
                            Room randomRoom = s.Rooms[Game1.GameWorld.rnd.Next(s.Rooms.Count())];
                            potentialLocations.Add((this.District.Location, currentDistrict, block, randomRoom));
                        }
                        else
                        {
                            potentialLocations.Add((this.District.Location, currentDistrict, block, null));
                        }
                    }
                }
                else if (Game1.GameWorld.StructureTypes.Any(e => e.Metadata == thing))
                {
                    foreach (Structure s in block.Structures)
                    {
                        if (s.Type == thing)
                        {
                            if (s.Block.District.IsLoaded)
                            {
                                Room randomRoom = s.Rooms[Game1.GameWorld.rnd.Next(s.Rooms.Count())];
                                potentialLocations.Add((this.District.Location, currentDistrict, block, randomRoom));
                            }
                            else
                            {
                                potentialLocations.Add((this.District.Location, currentDistrict, block, null));
                            }
                        }
                    }
                }
                else if (Game1.GameWorld.UniqueNameCatalogue.ContainsKey(thing))
                {
                    // Handle finding Object, Architect, or Group from SubjectCatalogue if necessary
                    var subject = Game1.GameWorld.UniqueNameCatalogue[thing];
                    if (subject is Object obj)
                    {
                        // Handle object-specific logic
                    }
                    else if (subject is Architect architect)
                    {
                        // Handle architect-specific logic
                    }
                    else if (subject is Group group)
                    {
                        // Handle group-specific logic
                    }
                }
            }

            if (potentialLocations.Count > 0)
            {
                RandomLocation = potentialLocations[Game1.GameWorld.rnd.Next(potentialLocations.Count)];
            }

            return RandomLocation;
        }
    }
}
