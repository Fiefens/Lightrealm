using Microsoft.Xna.Framework;
using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lightrealm
{
    [Serializable]
    public class Entity
    {
        private string _name;

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


        public int ID;
        private List<string> _referredToNames = new List<string>();

        public string Metadata;
        public string EntityType { get; set; }

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

        public void ClearReferredToNames()
        {
            _referredToNames.Clear();
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
    }
}
