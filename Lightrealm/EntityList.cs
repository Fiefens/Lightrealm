using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Lightrealm
{
    [Serializable]
    public class EntityList<T> : IList<T> where T : Entity
    {
        public List<int> _entityIds = new List<int>();

        [NonSerialized]
        public Dictionary<int, T> _cachedEntities = new();

        [NonSerialized]
        private Type _entityType = typeof(T);

        private string _entityTypeString;

        public EntityList()
        {
            _cachedEntities = new Dictionary<int, T>();
            _entityType = typeof(T);
            _entityTypeString = _entityType.AssemblyQualifiedName;
        }

        public EntityList(IEnumerable<T> items) : this()
        {
            _entityIds = items.Select(item => item.ID).ToList();
        }

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

        public Type EntityType
        {
            get => _entityType;
            set
            {
                _entityType = value;
                _entityTypeString = value?.AssemblyQualifiedName;
            }
        }

        public string EntityTypeString
        {
            get => _entityTypeString;
            set
            {
                _entityTypeString = value;
                _entityType = Type.GetType(value);
            }
        }

        public int Count => _entityIds.Count;
        public bool IsReadOnly => false;

        private T GetCachedEntity(int id)
        {
            if (_cachedEntities.TryGetValue(id, out T value))
                return value;

            T entity = EntityGet<T>(id);
            _cachedEntities[id] = entity;
            return entity;
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _entityIds.Count)
                {
                    throw new IndexOutOfRangeException($"Index {index} is out of range for entity IDs list.");
                }

                int entityId = _entityIds[index];
                return EntityGet<T>(entityId);
            }
            set
            {
                if (index < 0 || index >= _entityIds.Count)
                {
                    throw new IndexOutOfRangeException($"Index {index} is out of range for entity IDs list.");
                }

                _entityIds[index] = value.ID;
            }
        }

        public void Add(T item)
        {
            _entityIds.Add(item.ID);
        }

        public EntityList<T> Distinct()
        {
            var distinctEntityList = new EntityList<T>();
            var seen = new HashSet<int>();

            foreach (int id in _entityIds)
            {
                if (seen.Add(id))
                {
                    distinctEntityList.Add(GetCachedEntity(id));
                }
            }

            return distinctEntityList;
        }

        public EntityList<T> Take(int count)
        {
            var takenEntityList = new EntityList<T>();

            int limit = Math.Min(count, _entityIds.Count); // Avoid out-of-range
            for (int i = 0; i < limit; i++)
            {
                takenEntityList.Add(GetCachedEntity(_entityIds[i]));
            }

            return takenEntityList;
        }



        public bool SequenceEqual(EntityList<T> other)
        {
            return _entityIds.SequenceEqual(other._entityIds);
        }

        public void Clear()
        {
            _entityIds.Clear();
        }

        public void Shuffle()
        {
            int n = _entityIds.Count;
            while (n > 1)
            {
                n--;
                int k = Game1.GameWorld.rnd.Next(n + 1);
                int value = _entityIds[k];
                _entityIds[k] = _entityIds[n];
                _entityIds[n] = value;
            }
        }

        public EntityList<T> ShuffleNew()
        {
            var shuffledIds = new List<int>(_entityIds);

            int n = shuffledIds.Count;
            while (n > 1)
            {
                n--;
                int k = Game1.GameWorld.rnd.Next(n + 1);
                (shuffledIds[k], shuffledIds[n]) = (shuffledIds[n], shuffledIds[k]);
            }

            var list = new EntityList<T> { _entityIds = shuffledIds }; // Reuse IDs directly
            return list;
        }


        public bool Contains(T item)
        {
            if (item == null)
            {
                return false;
            }
            return _entityIds.Contains(item.ID);
        }

        public EntityList<TResult> Select<TResult>(Func<T, TResult> selector) where TResult : Entity
        {
            var selectedEntityList = new EntityList<TResult>();

            foreach (int id in _entityIds)
            {
                T entity = GetCachedEntity(id);
                TResult result = selector(entity);
                if (result != null)
                    selectedEntityList.Add(result);
            }

            return selectedEntityList;
        }



        public EntityList<TResult> SelectMany<TResult>(Func<T, IEnumerable<TResult>> selector) where TResult : Entity
        {
            var selectedEntityList = new EntityList<TResult>();

            foreach (int id in _entityIds)
            {
                T entity = GetCachedEntity(id);
                IEnumerable<TResult> results = selector(entity);
                if (results == null) continue;

                foreach (TResult result in results)
                {
                    if (result != null)
                        selectedEntityList.Add(result);
                }
            }

            return selectedEntityList;
        }



        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < _entityIds.Count; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        public EntityList<TResult> Cast<TResult>() where TResult : Entity
        {
            var castedEntityList = new EntityList<TResult>();

            foreach (int id in _entityIds)
            {
                Entity entity = EntityGet<Entity>(id);
                if (entity is TResult result)
                {
                    castedEntityList.Add(result);
                }
            }

            return castedEntityList;
        }


        public EntityList<T> GetRange(int index, int count)
        {
            var rangeEntityList = new EntityList<T>();

            int limit = Math.Min(index + count, _entityIds.Count);
            for (int i = index; i < limit; i++)
            {
                rangeEntityList.Add(GetCachedEntity(_entityIds[i]));
            }

            return rangeEntityList;
        }


        public EntityList<U> ConvertAll<U>(Func<T, U> converter) where U : Entity
        {
            var convertedEntityList = new EntityList<U>();
            foreach (var id in _entityIds)
            {
                var entity = GetCachedEntity(id);
                var convertedEntity = converter(entity);
                convertedEntityList.Add(convertedEntity);
            }
            return convertedEntityList;
        }

        public EntityList<T> OrderBy<TKey>(Func<T, TKey> keySelector) where TKey : IComparable<TKey>
        {
            var entities = new List<T>(_entityIds.Count);
            foreach (int id in _entityIds)
            {
                entities.Add(GetCachedEntity(id));
            }

            entities.Sort((a, b) => keySelector(a).CompareTo(keySelector(b)));

            var orderedEntityList = new EntityList<T>();
            orderedEntityList.AddRange(entities);
            return orderedEntityList;
        }
        public EntityList<T> OrderByDescending<TKey>(Func<T, TKey> keySelector) where TKey : IComparable<TKey>
        {
            var entities = new List<T>(_entityIds.Count);
            foreach (int id in _entityIds)
            {
                entities.Add(GetCachedEntity(id));
            }

            entities.Sort((a, b) => keySelector(b).CompareTo(keySelector(a)));

            var orderedEntityList = new EntityList<T>();
            orderedEntityList.AddRange(entities);
            return orderedEntityList;
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
            return _entityIds.IndexOf(item.ID);
        }

        public void Insert(int index, T item)
        {
            _entityIds.Insert(index, item.ID);
        }

        public bool Remove(T item)
        {
            return _entityIds.Remove(item.ID);
        }

        public void RemoveAt(int index)
        {
            _entityIds.RemoveAt(index);
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items), "Items collection cannot be null.");
            }

            foreach (var item in items)
            {
                if (item == null)
                {
                    throw new ArgumentException("Cannot add null item to the entity list.");
                }
                _entityIds.Add(item.ID);
            }
        }

        public void RemoveAll(Predicate<T> match)
        {
            for (int i = _entityIds.Count - 1; i >= 0; i--)
            {
                int id = _entityIds[i];
                T entity = EntityGet<T>(id);
                if (match(entity))
                {
                    _entityIds.RemoveAt(i);
                }
            }
        }

        public void Sort(Comparison<T> comparison)
        {
            var entities = new List<T>(_entityIds.Count);
            foreach (int id in _entityIds)
            {
                entities.Add(GetCachedEntity(id));
            }

            entities.Sort(comparison);

            _entityIds.Clear();
            foreach (T entity in entities)
            {
                _entityIds.Add(entity.ID);
            }
        }



        public void ForEach(Action<T> action)
        {
            foreach (var item in this)
            {
                action(item);
            }
        }

        public static EntityList<T> FromDictionary<TKey>(Dictionary<TKey, IEnumerable<T>> dictionary)
        {
            var entityList = new EntityList<T>();
            foreach (var kvp in dictionary)
            {
                entityList.AddRange(kvp.Value);
            }
            return entityList;
        }

        public Dictionary<string, EntityList<T>> ToDictionary<TKey>(Func<T, TKey> keySelector)
        {
            return this.GroupBy(keySelector)
                       .ToDictionary(g => g.Key.ToString(), g => new EntityList<T>(g));
        }

        public List<T> ToList()
        {
            var list = new List<T>(_entityIds.Count);
            foreach (int id in _entityIds)
            {
                list.Add(GetCachedEntity(id));
            }
            return list;
        }



        public void RemoveRange(int index, int count)
        {
            if (index < 0 || count < 0 || index + count > _entityIds.Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            _entityIds.RemoveRange(index, count);
        }

        private TE EntityGet<TE>(int entityId) where TE : Entity
        {
            if (Game1.GameWorld != null && Game1.GameWorld.EntityLedger.TryGetValue(entityId, out var entity))
                return (TE)entity;

            if (Game1.TemporaryEntityLedger.TryGetValue(entityId, out entity))
                return (TE)entity;

            throw new KeyNotFoundException("Entity ID not found in either EntityLedger or TemporaryEntityLedger.");
        }


        public void UnionWith(EntityList<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other), "The other EntityList cannot be null.");
            }

            foreach (var id in other._entityIds)
            {
                if (!_entityIds.Contains(id))
                {
                    _entityIds.Add(id);
                }
            }
        }


        public EntityList<T> ThenBy<TKey>(Func<T, TKey> keySelector) where TKey : IComparable<TKey>
        {
            var entities = new List<T>(_entityIds.Count);
            foreach (int id in _entityIds)
            {
                entities.Add(GetCachedEntity(id));
            }

            entities.Sort((a, b) => keySelector(a).CompareTo(keySelector(b)));

            var orderedEntityList = new EntityList<T>();
            orderedEntityList.AddRange(entities);
            return orderedEntityList;
        }


        public EntityList<T> Union(IEnumerable<T> other)
        {
            var unionEntityList = new EntityList<T>(this);

            var existingIds = new HashSet<int>(_entityIds);

            foreach (var item in other)
            {
                if (item != null && existingIds.Add(item.ID))
                {
                    unionEntityList._entityIds.Add(item.ID);
                    unionEntityList._cachedEntities[item.ID] = item;
                }
            }

            return unionEntityList;
        }

        public EntityList<T> Except(EntityList<T> other)
        {
            var resultEntityList = new EntityList<T>(); // Preallocate capacity

            var otherIds = new HashSet<int>(other._entityIds);

            foreach (var id in _entityIds)
            {
                if (!otherIds.Contains(id))
                {
                    resultEntityList.Add(GetCachedEntity(id));
                }
            }

            return resultEntityList;
        }


        public EntityList<T> Reverse()
        {
            var reversedEntityList = new EntityList<T>();
            for (int i = _entityIds.Count - 1; i >= 0; i--)
            {
                reversedEntityList.Add(GetCachedEntity(_entityIds[i]));
            }
            return reversedEntityList;
        }


        public void RemoveWhere(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match), "Predicate cannot be null.");
            }

            var idsToRemove = new List<int>();

            foreach (var id in _entityIds)
            {
                T entity = GetCachedEntity(id);
                if (match(entity))
                {
                    idsToRemove.Add(id);
                }
            }

            foreach (var id in idsToRemove)
            {
                _entityIds.Remove(id);
            }
        }

        public EntityList<T> Where(Func<T, bool> predicate)
        {
            var filteredEntityList = new EntityList<T>();
            foreach (var id in _entityIds)
            {
                T entity = GetCachedEntity(id);
                if (predicate(entity))
                {
                    filteredEntityList.Add(entity);
                }
            }
            return filteredEntityList;
        }

        public bool Exists(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match), "Predicate cannot be null.");
            }

            foreach (var id in _entityIds)
            {
                if (match(GetCachedEntity(id)))
                    return true;
            }

            return false;
        }


    }
}
