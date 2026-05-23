using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Lightrealm
{
    [Serializable]
    public class Entity
    {
        private string _name;

        public bool AnnouncedSelfThisLoad = false;

        public bool Expunged = false;

        public int LettersReceived = 0;

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                if (!ReferredToNames.Contains(value))
                {
                    ReferredToNames.Add(value);
                }
            }
        }

        public string EntityColor = "";

        public Quest AssociatedQuest;
        public Objective HookedObjective;
        public Objective IAmAFinalPointerThatFollowsAfterThisObjective;

        public Civilization HahaCivHiredMe;

        public bool Expertise = false;

        public int ID;
        public int Significance = 0;
        public int Reputation = 0;
        public List<string> _referredToNames = new List<string>();

        public virtual bool Incapacitated
        {
            get
            {
                return this switch
                {
                    Architect architect => !architect.IsAlive || architect.Bound,
                    Faction faction => !faction.SatelliteGroups.Any(g => g.Architects.Count > 0),
                    Location location => location.TruePopulation() == 0,
                    Group group => group.Architects.Count == 0,
                    Association association => Game1.GameState != "ascendant",
                    _ => false
                };
            }
        }



        public void Delete()
        {
            Game1.GameWorld.EntityLedger.Remove(ID);
            Game1.AnnouncementEntitiesToDeleteThisCycle.Add(ID);
        }


        public bool Reinforced = false;

        public string Metadata;
        public string EntityType { get; set; }

        public EntityList<Entity> Enemies { get; set; } = new EntityList<Entity>();
        public EntityList<Event> AssociatedEvents { get; set; } = new EntityList<Event>();

        public Entity PairedObject()
        {
            // If this object is a door, find the paired door in the destination room
            if (this is Door currentDoor)
            {
                // Find the opposite door in the destination room
                Room destinationRoom = currentDoor.DestinationRoom;
                if (destinationRoom != null)
                {
                    // Look for the door in the destination room that leads back to the current room
                    Door pairedDoor = (Door)(destinationRoom.Objects.FirstOrDefault(obj => obj is Door door && door.DestinationRoom == currentDoor.Room));
                    return pairedDoor;
                }
            }
            // If this object is a structure, find the exit door for the structure
            else if (this is Structure structure)
            {
                // Find the exit door within the first room of the structure
                Object exitDoor = structure.Rooms[0].Objects.FirstOrDefault(obj => obj.Type == "exit door");
                return exitDoor;
            }
            // If this object is an exit door, find the structure it belongs to and return the structure
            else if (this is Object obj && obj.Type == "exit door")
            {
                if (obj.Structure != null && obj.Structure.Rooms[0].Objects.Contains(obj))
                {
                    // If the exit door belongs to a structure, return the structure
                    return obj.Structure;
                }
            }

            // Return null if no paired object is found
            return null;
        }


        public void PissOffEntityOrPlace(Entity Victim, bool StealthAttempt)
        {
            List<string> victimFriends = new List<string>();
            List<Entity> entitiesToPissOff = new List<Entity>();

            // Early return if the Victim has already declared you an enemy
            if (Victim.Enemies.Contains(this))
            {
                return;
            }

            // Gather entities to piss off based on the Victim type
            if (Victim is Location location)
            {
                if (location.Government != null)
                {
                    entitiesToPissOff.Add(location.Government);
                }
                foreach (var district in location.Districts)
                {
                    entitiesToPissOff.AddRange(district.DistrictArchitects.Where(a => Game1.GameWorld.rnd.Next(0, 100) < 1)); // 1 percent chance
                }
            }
            else if (Victim is Architect architect)
            {
                entitiesToPissOff.Add(architect);
                if (architect.Group != null)
                {
                    entitiesToPissOff.Add(architect.Group);
                    entitiesToPissOff.AddRange(architect.Group.Architects);
                }
            }
            else if (Victim is Group group)
            {
                entitiesToPissOff.Add(group);
                entitiesToPissOff.AddRange(group.Architects);
                if (group.HomeFaction != null)
                {
                    entitiesToPissOff.Add(group.HomeFaction);
                }
            }
            else if (Victim is Faction faction)
            {
                entitiesToPissOff.Add(faction);
                foreach (var satelliteGroup in faction.SatelliteGroups)
                {
                    entitiesToPissOff.Add(satelliteGroup);
                    entitiesToPissOff.AddRange(satelliteGroup.Architects);
                }
            }

            // Add enemies and gather names of successfully added enemies
            foreach (var entity in entitiesToPissOff)
            {
                if (AddEnemy(entity))
                {
                    victimFriends.Add(entity.Name);
                }
            }

            // Announce if any new enemies were made
            if (victimFriends.Count > 0)
            {
                MakeAnnouncement(
                    Victim,
                    StealthAttempt,
                    victimFriends,
                    (Victim is Architect a
                        ? (a.Location != null
                            ? a.Location.Region
                            : Game1.GameWorld.WorldMap[Game1.GameWorld.rnd.Next(Game1.GameWorld.WorldMap.Count())])
                    : Victim is Group g
                        ? (g.Leader.Location != null
                            ? g.Leader.Location.Region
                            : Game1.GameWorld.WorldMap[Game1.GameWorld.rnd.Next(Game1.GameWorld.WorldMap.Count())])
                    : Victim is Location l
                        ? l.Region
                    : ((Faction)Victim).Base.Region),
                    new EntityList<Entity>() { Victim }.Union(entitiesToPissOff)
                );
            }

            // Specific logic for player association
            if (Game1.GameWorld != null && Game1.GameWorld.GamePlayerAssociation != null && Game1.GameWorld.GamePlayerAssociation.Parties.Contains(this))
            {
                Victim.Enemies.Add(Game1.GameWorld.GamePlayerAssociation);
            }
        }

        private bool AddEnemy(Entity entity)
        {
            // Early return if the entity has already declared you an enemy
            if (entity.Enemies.Contains(this))
            {
                return false;
            }

            entity.Enemies.Add(this);
            return true;
        }

        public static string FormatEnemyList(List<string> enemies)
        {
            int count = enemies.Count;

            if (count == 0)
                return "";

            if (count <= 3)
                return Game1.FormatAndList(enemies);

            var topThree = enemies.Take(3).ToList();
            int others = count - 3;

            return $"{Game1.FormatAndList(topThree)}, and {others} other{(others == 1 ? "" : "s")}";
        }

        private void MakeAnnouncement(Entity Victim, bool StealthAttempt, List<string> victimFriends, Region r, EntityList<Entity> Entities)
        {
            int Month = ((int)Math.Round((decimal)(Game1.GameWorld.Cycle / 24192000)) % 12) + 1;
            int Year = (int)Math.Round((decimal)(Game1.GameWorld.Cycle / 290304000), MidpointRounding.ToZero);
            string formattedEnemies = FormatEnemyList(victimFriends); // <-- updated to use the new function
            string message;

            if (StealthAttempt)
            {
                message = $"({Month}/{Year}) {Victim.Name} discovered the actions of {this.Name} and declared them an enemy. {formattedEnemies} followed suit.";
            }
            else
            {
                message = $"({Month}/{Year}) {Victim.Name} declared {this.Name} as an enemy. {formattedEnemies} followed suit.";
            }

            Game1.GameWorld.HistoricalEvents.Add(new Event(message, r, Entities));
        }




        public EntityHashSet<Location> GetMySpecialLocations()
        {
            EntityHashSet<Location> specialLocations = new EntityHashSet<Location>();

            // Check if this entity is a Group and has a base
            if (this is Group g)
            {
                if (g.Base != null)
                {
                    specialLocations.Add(g.Base);
                }

                if (g.HomeFaction != null)
                {
                    foreach (Location l in g.HomeFaction.Outposts)
                    {
                        specialLocations.Add(l);
                    }
                }
            }
            // Check if this entity is an Architect and has relevant locations
            else if (this is Architect a)
            {
                if (a.HomeLocation != null)
                {
                    specialLocations.Add(a.HomeLocation);
                }
                if (a.Location != null)
                {
                    specialLocations.Add(a.Location);
                }
            }
            // Check if this entity is a Faction and add its outposts
            else if (this is Faction f)
            {
                foreach (Location l in f.Outposts)
                {
                    specialLocations.Add(l);
                }
            }

            // Optionally, remove locations with no population
            var toRemove = new List<Location>();
            foreach (var l in specialLocations)
            {
                if (l.TruePopulation() == 0)
                {
                    toRemove.Add(l);
                }
            }
            foreach (var l in toRemove)
            {
                specialLocations.Remove(l);
            }

            return specialLocations;
        }


        public EntityHashSet<Location> PreferredTargetLocations()
        {
            EntityHashSet<Location> BadPlaces = new EntityHashSet<Location>();

            foreach(Entity e in Enemies)
            {
                if(e is Group g && g.Base != null)
                {
                    BadPlaces.Add(g.Base);

                    if(g.HomeFaction != null)
                    {
                        foreach(Location l in g.HomeFaction.Outposts)
                        {
                            BadPlaces.Add(l);
                        }
                    }
                }
                else if (e is Architect a)
                {
                    if(a.HomeLocation != null)
                        BadPlaces.Add(a.HomeLocation);
                    if(a.Location != null)
                        BadPlaces.Add(a.Location);
                }
                else if (e is Faction f)
                {
                    foreach (Location l in f.Outposts)
                    {
                        BadPlaces.Add(l);
                    }
                }
            }


            var toRemove = new List<Location>();
            foreach (var l in BadPlaces)
            {
                if (l.TruePopulation() == 0)
                {
                    toRemove.Add(l);
                }
            }
            foreach (var l in toRemove)
            {
                BadPlaces.Remove(l);
            }


            return BadPlaces;
        }

        public static T EntityGet<T>(int entityId) where T : Entity
        {
            if (entityId == 0)
            {
                return null;
            }

            Entity entity = null;

            if (Game1.GameWorld == null)
            {
                if (Game1.TemporaryEntityLedger.ContainsKey(entityId))
                {
                    entity = Game1.TemporaryEntityLedger[entityId];
                }
            }
            else
            {
                if (Game1.GameWorld.EntityLedger.ContainsKey(entityId))
                {
                    entity = Game1.GameWorld.EntityLedger[entityId];
                }
            }

            if (entity == null)
            {
                throw new KeyNotFoundException($"Entity ID {entityId} not found in either AllEntities or TemporaryEntities.");
            }

            if (entity is T typedEntity)
            {
                return typedEntity;
            }
            else
            {
                return null;
            }
        }

        [NonSerialized]
        public Rectangle Hitbox = new Rectangle();

        public List<string> ReferredToNames
        {
            get
            {
                if (_referredToNames.Count() == 0)
                {
                    if (this is Object && Name == null)
                    {
                        return new List<string> { Game1.FormatMaterialList(((Object)this).Materials) + " " + ((Object)this).EntityType };
                    }
                    else if (Name != null)
                    {
                        return new List<string> { Name };
                    }
                    else if (Metadata != null)
                    {
                        return new List<string> { Metadata };
                    }
                    else
                    {
                        return new List<string>();
                    }
                }
                return _referredToNames;
            }
            set
            {
                _referredToNames = value;
            }
        }

        public void AddReferredToName(string name)
        {
            _referredToNames.Add(name);
        }

        public Entity()
        {
            EntityType = GetType().Name;

            if (Game1.GameWorld != null)
            {
                ID = Game1.GameWorld.NextUniqueID;
                Game1.GameWorld.NextUniqueID++;
                Game1.GameWorld.EntityLedger.Add(ID, this);
            }
            else
            {
                ID = Game1.TemporaryNextUniqueID;
                Game1.TemporaryNextUniqueID++;
                Game1.TemporaryEntityLedger.Add(ID, this);
            }

            AddReferredToName(ID.ToString());
        }

        public Entity(string metadata)
        {
            EntityType = GetType().Name;
            Metadata = metadata;
            Name = metadata;
            AddReferredToName(Name);

            EntityType = GetType().Name;

            if (Game1.GameWorld != null)
            {
                ID = Game1.GameWorld.NextUniqueID;
                Game1.GameWorld.NextUniqueID++;
                Game1.GameWorld.EntityLedger.Add(ID, this);
            }
            else
            {
                ID = Game1.TemporaryNextUniqueID;
                Game1.TemporaryNextUniqueID++;
                Game1.TemporaryEntityLedger.Add(ID, this);
            }

            AddReferredToName(ID.ToString());
        }

        public static List<string> InvalidQuestRaces = new List<string>() { "photonexus", "hypernexus", "icosidodecahedron", "isofractal", "shadeheart", "shade" };

        public bool IsValidQuestTarget()
        {
            if (this is Architect a)
            {
                if (!a.IsAlive || a.Location == null || a.District == null || a.IsCalamity)
                    return false;

                if (InvalidQuestRaces.Contains(a.Race.Name))
                    return false;
            }

            if (string.IsNullOrEmpty(Name))
                return false;


            if (ReferredToNames.Any(string.IsNullOrEmpty))
                return false;

            if (this.HookedObjective != null || this.IAmAFinalPointerThatFollowsAfterThisObjective != null || this.AssociatedQuest != null)
                return false;


            return true;
        }

    }
}
