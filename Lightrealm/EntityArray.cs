using Lightrealm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lightrealm
{
    [Serializable]
    public class EntityArray<T> : IList<T> where T : Entity
    {
        public int[] _entityIds;

        [NonSerialized]
        private Type _entityType = typeof(T);

        // Property to store the EntityType as a string for serialization
        public string EntityTypeString
        {
            get => _entityType.AssemblyQualifiedName;
            set => _entityType = Type.GetType(value);
        }

        public Type EntityType
        {
            get => _entityType;
            set => _entityType = value;
        }

        public int Count => _entityIds.Length;

        public bool IsReadOnly => false;

        public EntityArray(int length)
        {
            _entityIds = new int[length];
        }

        public EntityArray(IEnumerable<T> items)
        {
            _entityIds = items.Select(item => item.ID).ToArray();
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _entityIds.Length)
                {
                    throw new IndexOutOfRangeException($"Index {index} is out of range for entity IDs array.");
                }

                int entityId = _entityIds[index];
                return EntityGet<T>(entityId);
            }
            set
            {
                if (index < 0 || index >= _entityIds.Length)
                {
                    throw new IndexOutOfRangeException($"Index {index} is out of range for entity IDs array.");
                }

                _entityIds[index] = value.ID;
            }
        }

        public void Add(T item)
        {
            throw new NotSupportedException("Cannot add items to a fixed-length array.");
        }

        public void Clear()
        {
            _entityIds = new int[0];
        }

        public bool Contains(T item)
        {
            return _entityIds.Contains(item.ID);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < _entityIds.Length; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var id in _entityIds)
            {
                yield return EntityGet<T>(id);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf(_entityIds, item.ID);
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException("Cannot insert items into a fixed-length array.");
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException("Cannot remove items from a fixed-length array.");
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException("Cannot remove items from a fixed-length array.");
        }

        private TE EntityGet<TE>(int entityId) where TE : Entity
        {
            if (Game1.GameWorld != null && Game1.GameWorld.EntityLedger != null && Game1.GameWorld.EntityLedger.ContainsKey(entityId))
            {
                return (TE)Game1.GameWorld.EntityLedger[entityId];
            }
            if (Game1.TemporaryEntityLedger.ContainsKey(entityId))
            {
                return (TE)Game1.TemporaryEntityLedger[entityId];
            }
            throw new KeyNotFoundException("Entity ID not found in either AllEntities or TemporaryEntities.");
        }
    }
}
