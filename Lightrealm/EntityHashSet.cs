using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Lightrealm
{
    [Serializable]
    public class EntityHashSet<T> : ICollection<T> where T : Entity
    {
        public HashSet<int> _entityIds = new HashSet<int>();

        [NonSerialized]
        private Dictionary<int, T> _cachedEntities = new();

        [NonSerialized]
        private Type _entityType = typeof(T);

        // This will be serialized and used to restore the Type
        private string _entityTypeString;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _cachedEntities = new Dictionary<int, T>();
            if (!string.IsNullOrEmpty(_entityTypeString))
            {
                var resolved = Type.GetType(_entityTypeString);
                if (resolved != null)
                    _entityType = resolved;
            }
        }

        // Expose the type string via a property (optional, for external use)
        public string EntityTypeString
        {
            get => _entityTypeString;
            set
            {
                _entityTypeString = value;
                _entityType = Type.GetType(value);
            }
        }

        public Type EntityType
        {
            get => _entityType;
            set
            {
                _entityType = value;
                _entityTypeString = value?.AssemblyQualifiedName;
            }
        }

        // Ensure the constructor initializes the type string correctly
        public EntityHashSet()
        {
            _cachedEntities = new Dictionary<int, T>();
            _entityType = typeof(T);
            _entityTypeString = _entityType.AssemblyQualifiedName;
        }

        public EntityHashSet(IEnumerable<T> items) : this()
        {
            _entityIds = new HashSet<int>(items.Select(item => item.ID));
        }

        public int Count => _entityIds.Count;

        public bool IsReadOnly => false;

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
                array[arrayIndex++] = EntityGet(id);
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
                yield return EntityGet(id);
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
                var entity = EntityGet(id);
                selectedSet.Add(selector(entity));
            }
            return selectedSet;
        }

        public EntityHashSet<TResult> SelectMany<TResult>(Func<T, IEnumerable<TResult>> selector) where TResult : Entity
        {
            var selectedSet = new EntityHashSet<TResult>();
            foreach (var id in _entityIds)
            {
                var entity = EntityGet(id);
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
                var entity = EntityGet(id);
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

            int randomIndex = Game1.GameWorld.rnd.Next(_entityIds.Count);
            int currentIndex = 0;

            foreach (int id in _entityIds)
            {
                if (currentIndex == randomIndex)
                {
                    return EntityGet(id);
                }
                currentIndex++;
            }

            throw new InvalidOperationException("Random index out of bounds.");
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item); // Reuse the existing Add method
            }
        }

        public void RemoveAll(Func<T, bool> predicate)
        {
            var itemsToRemove = _entityIds.Where(id => predicate(EntityGet(id))).ToList();
            foreach (var id in itemsToRemove)
            {
                _entityIds.Remove(id);
            }
        }

        private T EntityGet(int entityId)
        {
            if (_cachedEntities.TryGetValue(entityId, out var cached))
                return (T)cached;

            if (Game1.GameWorld != null && Game1.GameWorld.EntityLedger.TryGetValue(entityId, out var entity))
            {
                _cachedEntities[entityId] = (T)entity;
                return (T)entity;
            }
            if (Game1.TemporaryEntityLedger.TryGetValue(entityId, out entity))
            {
                _cachedEntities[entityId] = (T)entity;
                return (T)entity;
            }

            throw new KeyNotFoundException("Entity ID not found");
        }

        public void UnionWith(IEnumerable<T> other)
        {
            foreach (var item in other)
            {
                _entityIds.Add(item.ID);
            }
        }

        public void RemoveWhere(Predicate<T> match)
        {
            var itemsToRemove = _entityIds.Where(id => match(EntityGet(id))).ToList();
            foreach (var id in itemsToRemove)
            {
                _entityIds.Remove(id);
            }
        }

        // ToEntityList method to convert the EntityHashSet to EntityList
        public EntityList<T> ToEntityList()
        {
            var entityList = new EntityList<T>();
            foreach (var id in _entityIds)
            {
                entityList.Add(EntityGet(id));
            }
            return entityList;
        }
    }
}
