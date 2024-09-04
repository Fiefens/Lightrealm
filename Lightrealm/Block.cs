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


        public EntityList<Architect> BuriedArchitects { get; set; } = new EntityList<Architect>();
        public EntityList<Object> BuriedObjects { get; set; } = new EntityList<Object>();

        public Entity SocializationTopic;
        public Entity DiscussionTopic;

        public string Biome { get; set; } = "";

        public int X { get; set; }
        public int Z { get; set; }

        private int _districtId;

        
        public District District
        {
            get => EntityGet<District>(_districtId);
            set => _districtId = value?.ID ?? 0;
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

        public (Region, Location, District, Block, Room) FindNearestThing(string thing)
        {
            (Region, Location, District, Block, Room) DeterminedLocation = (null, null, null, null, null);

            int CalculateDistance(Block block1, Block block2)
            {
                int Distance = 0;

                if (block2.District.Location != block1.District.Location)
                {
                    Distance += 10 * (int)Math.Round(Vector2.Distance(new Vector2(block2.District.Location.X, block2.District.Location.Z), new Vector2(block1.District.Location.X, block1.District.Location.Z)));
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

            int minDistance = int.MaxValue;

            foreach (Region r in Game1.GameWorld.WorldMap)
            {
                if (r.Location != null)
                {
                    foreach (District district in r.Location.Districts)
                    {
                        foreach (Block block in district.DistrictMap)
                        {
                            if (thing == "well" && block.HasWell())
                            {
                                int distance = CalculateDistance(this, block);
                                if (distance < minDistance)
                                {
                                    minDistance = distance;
                                    DeterminedLocation = (r, r.Location, district, block, null);
                                }
                            }
                            else if (thing == "structure")
                            {
                                foreach (Structure s in block.Structures)
                                {
                                    int distance = CalculateDistance(this, block);
                                    if (distance < minDistance)
                                    {
                                        if (s.Block.District.IsLoaded)
                                        {
                                            minDistance = distance;
                                            Room randomRoom = s.Rooms[Game1.r.Next(s.Rooms.Count())];
                                            DeterminedLocation = (r, r.Location, district, block, randomRoom);
                                        }
                                        else
                                        {
                                            minDistance = distance;
                                            DeterminedLocation = (r, r.Location, district, block, null);
                                        }
                                    }
                                }
                            }
                            else if (Game1.StructureTypes.Contains(thing))
                            {
                                foreach (Structure s in block.Structures)
                                {
                                    if (s.Type == thing)
                                    {
                                        int distance = CalculateDistance(this, block);
                                        if (distance < minDistance)
                                        {
                                            if (s.Block.District.IsLoaded)
                                            {
                                                minDistance = distance;
                                                Room randomRoom = s.Rooms[Game1.r.Next(s.Rooms.Count())];
                                                DeterminedLocation = (r, r.Location, district, block, randomRoom);
                                            }
                                            else
                                            {
                                                minDistance = distance;
                                                DeterminedLocation = (r, r.Location, district, block, null);
                                            }
                                        }
                                    }
                                }
                            }
                            else if (Game1.GameWorld.SubjectCatalogue.ContainsKey(thing))
                            {
                                // Handle finding Object, Architect, or Group from SubjectCatalogue if necessary
                                var subject = Game1.GameWorld.SubjectCatalogue[thing];
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
                    }
                }
            }

            return DeterminedLocation;
        }

        public (Region, Location, District, Block, Room) FindRandomThingInCurrentDistrict(string thing)
        {
            (Region, Location, District, Block, Room) RandomLocation = (null, null, null, null, null);

            List<(Region, Location, District, Block, Room)> potentialLocations = new List<(Region, Location, District, Block, Room)>();

            // Get the current district
            District currentDistrict = this.District;

            foreach (Block block in currentDistrict.DistrictMap)
            {
                if (thing == "well" && block.HasWell())
                {
                    potentialLocations.Add((this.District.Location.Region, this.District.Location, currentDistrict, block, null));
                }
                else if (thing == "structure")
                {
                    foreach (Structure s in block.Structures)
                    {
                        if (s.Block.District.IsLoaded)
                        {
                            Room randomRoom = s.Rooms[Game1.r.Next(s.Rooms.Count())];
                            potentialLocations.Add((this.District.Location.Region, this.District.Location, currentDistrict, block, randomRoom));
                        }
                        else
                        {
                            potentialLocations.Add((this.District.Location.Region, this.District.Location, currentDistrict, block, null));
                        }
                    }
                }
                else if (Game1.StructureTypes.Contains(thing))
                {
                    foreach (Structure s in block.Structures)
                    {
                        if (s.Type == thing)
                        {
                            if (s.Block.District.IsLoaded)
                            {
                                Room randomRoom = s.Rooms[Game1.r.Next(s.Rooms.Count())];
                                potentialLocations.Add((this.District.Location.Region, this.District.Location, currentDistrict, block, randomRoom));
                            }
                            else
                            {
                                potentialLocations.Add((this.District.Location.Region, this.District.Location, currentDistrict, block, null));
                            }
                        }
                    }
                }
                else if (Game1.GameWorld.SubjectCatalogue.ContainsKey(thing))
                {
                    // Handle finding Object, Architect, or Group from SubjectCatalogue if necessary
                    var subject = Game1.GameWorld.SubjectCatalogue[thing];
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
                RandomLocation = potentialLocations[Game1.r.Next(potentialLocations.Count)];
            }

            return RandomLocation;
        }
    }
}
