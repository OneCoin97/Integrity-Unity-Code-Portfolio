using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DDObjectPool
{
    public interface IObjectPoolOwner<TId>
    {
        void returnToPool(ObjectPoolEntry<TId> entry);
    }

    /// <summary>
    /// 풀에서 재사용되는 오브젝트의 공통 생명주기를 정의한다.
    /// </summary>
    public abstract class ObjectPoolEntry<TId> : MonoBehaviour
    {
        [SerializeField] private TId id = default;

        private IObjectPoolOwner<TId> owner;

        public TId Id => id;
        public bool IsActive { get; private set; }

        public abstract void initializeObject();

        public virtual void onRent()
        {
            IsActive = true;
            gameObject.SetActive(true);
        }

        public virtual void onReturn()
        {
            IsActive = false;
            gameObject.SetActive(false);
        }

        internal void setOwner(IObjectPoolOwner<TId> poolOwner)
        {
            owner = poolOwner;
        }

        public void returnToPool()
        {
            owner?.returnToPool(this);
        }
    }

    /// <summary>
    /// UIView가 반복 항목을 생성할 때 사용하는 최소 범위의 오브젝트 풀.
    /// </summary>
    [Serializable]
    public sealed class ObjectPoolManager<TEntry, TId> : IObjectPoolOwner<TId>
        where TEntry : ObjectPoolEntry<TId>
    {
        private readonly Dictionary<TId, Queue<TEntry>> pool = new Dictionary<TId, Queue<TEntry>>();

        private Transform activeParent;
        private Transform inactiveParent;
        private bool worldPositionStays;

        public void Initialize(
            Transform activeParent,
            bool worldPositionStays = true,
            Transform inactiveParent = null)
        {
            this.activeParent = activeParent;
            this.inactiveParent = inactiveParent != null ? inactiveParent : activeParent;
            this.worldPositionStays = worldPositionStays;
        }

        public TEntry Instantiate(TEntry prefab)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            TEntry instance = takeAvailable(prefab.Id);
            if (instance == null)
            {
                instance = Object.Instantiate(prefab);
                instance.setOwner(this);
            }

            instance.transform.SetParent(activeParent, worldPositionStays);
            instance.onRent();
            return instance;
        }

        public void Destroy(TEntry instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!pool.TryGetValue(instance.Id, out Queue<TEntry> entries))
            {
                entries = new Queue<TEntry>();
                pool.Add(instance.Id, entries);
            }

            instance.transform.SetParent(inactiveParent, false);
            instance.initializeObject();
            instance.onReturn();
            entries.Enqueue(instance);
        }

        public void returnToPool(ObjectPoolEntry<TId> entry)
        {
            if (entry is TEntry typedEntry)
            {
                Destroy(typedEntry);
            }
        }

        private TEntry takeAvailable(TId id)
        {
            if (!pool.TryGetValue(id, out Queue<TEntry> entries))
            {
                return null;
            }

            while (entries.Count > 0)
            {
                TEntry entry = entries.Dequeue();
                if (entry != null)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
