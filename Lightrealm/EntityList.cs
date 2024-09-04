using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lightrealm
{
    [Serializable]
    public class EntityList<T> : IList<T> where T : Entity
    {
        public List<int> _entityIds = new List<int>();

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

        public EntityList() { }

        public EntityList(IEnumerable<T> items)
        {
            _entityIds = items.Select(item => item.ID).ToList();
        }

        public int Count => _entityIds.Count;

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            _entityIds.Add(item.ID);
        }

        public EntityList<T> Distinct()
        {
            var distinctEntityList = new EntityList<T>();
            var distinctIds = _entityIds.Distinct();
            distinctEntityList.AddRange(distinctIds.Select(id => EntityGet<T>(id)));
            return distinctEntityList;
        }

        public EntityList<T> Take(int count)
        {
            var takenEntityList = new EntityList<T>();
            takenEntityList.AddRange(_entityIds.Take(count).Select(id => EntityGet<T>(id)));
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
                int k = new Random().Next(n + 1);
                int value = _entityIds[k];
                _entityIds[k] = _entityIds[n];
                _entityIds[n] = value;
            }
        }

        public EntityList<T> ShuffleNew()
        {
            var shuffledEntityList = new EntityList<T>(this);
            shuffledEntityList.Shuffle();
            return shuffledEntityList;
        }

        public bool Contains(T item)
        {
            if(item == null)
            {
                return false;
            }
            return _entityIds.Contains(item.ID);
        }

        public EntityList<TResult> Select<TResult>(Func<T, TResult> selector) where TResult : Entity
        {
            var selectedEntityList = new EntityList<TResult>();
            selectedEntityList.AddRange(_entityIds.Select(id => selector(EntityGet<T>(id))));
            return selectedEntityList;
        }

        public EntityList<TResult> SelectMany<TResult>(Func<T, IEnumerable<TResult>> selector) where TResult : Entity
        {
            var selectedEntityList = new EntityList<TResult>();
            selectedEntityList.AddRange(_entityIds.SelectMany(id => selector(EntityGet<T>(id)).Select(e => e.ID)).Select(EntityGet<TResult>));
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
            castedEntityList.AddRange(_entityIds.Select(id => (TResult)(EntityGet<Entity>(id))));
            return castedEntityList;
        }

        public EntityList<T> GetRange(int index, int count)
        {
            var rangeEntityList = new EntityList<T>();
            var rangeIds = _entityIds.GetRange(index, count);
            rangeEntityList.AddRange(rangeIds.Select(id => EntityGet<T>(id)));
            return rangeEntityList;
        }

        public EntityList<U> ConvertAll<U>(Func<T, U> converter) where U : Entity
        {
            var convertedEntityList = new EntityList<U>();
            foreach (var id in _entityIds)
            {
                var entity = EntityGet<T>(id);
                var convertedEntity = converter(entity);
                convertedEntityList.Add(convertedEntity);
            }
            return convertedEntityList;
        }

        public EntityList<T> OrderBy<TKey>(Func<T, TKey> keySelector) where TKey : IComparable<TKey>
        {
            var orderedEntities = _entityIds
                .Select(id => EntityGet<T>(id))
                .OrderBy(keySelector);

            var orderedEntityList = new EntityList<T>();
            orderedEntityList.AddRange(orderedEntities);

            return orderedEntityList;
        }

        public EntityList<T> OrderByDescending<TKey>(Func<T, TKey> keySelector) where TKey : IComparable<TKey>
        {
            var orderedEntities = _entityIds
                .Select(id => EntityGet<T>(id))
                .OrderByDescending(keySelector);

            var orderedEntityList = new EntityList<T>();
            orderedEntityList.AddRange(orderedEntities);

            return orderedEntityList;
        }

        public EntityList<T> Union(IEnumerable<T> other)
        {
            var unionEntityList = new EntityList<T>(this);
            unionEntityList.AddRange(other);
            return unionEntityList;
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
            var idsToRemove = _entityIds.Where(id => match(EntityGet<T>(id)));
            _entityIds.RemoveAll(id => idsToRemove.Contains(id));
        }

        public void Sort(Comparison<T> comparison)
        {
            var sorted = this.OrderBy(item => item, Comparer<T>.Create(comparison));
            _entityIds = sorted.Select(item => item.ID).ToList();
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
            return _entityIds.Select(id => EntityGet<T>(id)).ToList();
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

        public EntityList<T> Except(EntityList<T> other)
        {
            var resultEntityList = new EntityList<T>();

            var otherIds = new HashSet<int>(other._entityIds);

            foreach (var id in _entityIds)
            {
                if (!otherIds.Contains(id))
                {
                    resultEntityList.Add(EntityGet<T>(id));
                }
            }

            return resultEntityList;
        }

        public EntityList<T> Reverse()
        {
            var reversedEntityList = new EntityList<T>();
            for (int i = _entityIds.Count - 1; i >= 0; i--)
            {
                reversedEntityList.Add(EntityGet<T>(_entityIds[i]));
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
                T entity = EntityGet<T>(id);
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
                var entity = EntityGet<T>(id);
                if (predicate(entity))
                {
                    filteredEntityList.Add(entity);
                }
            }
            return filteredEntityList;
        }
    }
}
