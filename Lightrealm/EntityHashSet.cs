using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lightrealm
{
    [Serializable]
    public class EntityHashSet<T> : ICollection<T> where T : Entity
    {
        public HashSet<int> _entityIds = new HashSet<int>();

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

        public int Count => _entityIds.Count;

        public bool IsReadOnly => false;

        private static readonly Random _random = new Random();

        public EntityHashSet() { }

        public EntityHashSet(IEnumerable<T> items)
        {
            _entityIds = new HashSet<int>(items.Select(item => item.ID));
        }

        public void Add(T item)
        {
            _entityIds.Add(item.ID);
        }

        public void Clear()
        {
            _entityIds.Clear();
        }

        public bool Contains(T item)
        {
            return _entityIds.Contains(item.ID);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var id in _entityIds)
            {
                array[arrayIndex++] = EntityGet<T>(id);
            }
        }

        public bool Remove(T item)
        {
            return _entityIds.Remove(item.ID);
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

        public EntityHashSet<T> Distinct()
        {
            return new EntityHashSet<T>(this);
        }

        public EntityHashSet<T> Union(IEnumerable<T> other)
        {
            var unionSet = new EntityHashSet<T>(this);
            foreach (var item in other)
            {
                unionSet.Add(item);
            }
            return unionSet;
        }

        public EntityHashSet<TResult> Select<TResult>(Func<T, TResult> selector) where TResult : Entity
        {
            var selectedSet = new EntityHashSet<TResult>();
            foreach (var id in _entityIds)
            {
                var entity = EntityGet<T>(id);
                selectedSet.Add(selector(entity));
            }
            return selectedSet;
        }

        public EntityHashSet<TResult> SelectMany<TResult>(Func<T, IEnumerable<TResult>> selector) where TResult : Entity
        {
            var selectedSet = new EntityHashSet<TResult>();
            foreach (var id in _entityIds)
            {
                var entity = EntityGet<T>(id);
                foreach (var result in selector(entity))
                {
                    selectedSet.Add(result);
                }
            }
            return selectedSet;
        }

        public void ForEach(Action<T> action)
        {
            foreach (var item in this)
            {
                action(item);
            }
        }

        public EntityHashSet<T> Where(Func<T, bool> predicate)
        {
            var filteredSet = new EntityHashSet<T>();
            foreach (var id in _entityIds)
            {
                var entity = EntityGet<T>(id);
                if (predicate(entity))
                {
                    filteredSet.Add(entity);
                }
            }
            return filteredSet;
        }

        public T GetRandomItem()
        {
            if (_entityIds.Count == 0)
            {
                throw new InvalidOperationException("Cannot select a random item from an empty EntityHashSet.");
            }

            int randomIndex = _random.Next(_entityIds.Count);
            int currentIndex = 0;

            foreach (int id in _entityIds)
            {
                if (currentIndex == randomIndex)
                {
                    return EntityGet<T>(id);
                }
                currentIndex++;
            }

            throw new InvalidOperationException("Random index out of bounds.");
        }

        private TE EntityGet<TE>(int entityId) where TE : Entity
        {
            if (Game1.GameWorld != null && Game1.EntityLedger != null && Game1.EntityLedger.ContainsKey(entityId))
            {
                return (TE)Game1.EntityLedger[entityId];
            }
            if (Game1.TemporaryEntities.ContainsKey(entityId))
            {
                return (TE)Game1.TemporaryEntities[entityId];
            }
            throw new KeyNotFoundException("Entity ID not found in either AllEntities or TemporaryEntities.");
        }

        // ToEntityList method to convert the EntityHashSet to EntityList
        public EntityList<T> ToEntityList()
        {
            var entityList = new EntityList<T>();
            foreach (var id in _entityIds)
            {
                entityList.Add(EntityGet<T>(id));
            }
            return entityList;
        }
    }
}
