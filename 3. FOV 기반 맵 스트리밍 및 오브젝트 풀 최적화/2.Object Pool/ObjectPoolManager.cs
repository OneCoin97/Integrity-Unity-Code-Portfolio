using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DDObjectPool
{
    public interface IObjectPoolOwner<A>
    {
        void iDestroy(ObjectPoolEntry<A> entry);
    }
    /// <summary>
    /// 오브젝트 풀에 저장될 오브젝트의 기본 클래스입니다.
    /// </summary>
    /// <typeparam name="A">오브젝트를 구분할 ID의 타입</typeparam>
    public abstract class ObjectPoolEntry<A> : MonoBehaviour
    {
        public A id;
        private IObjectPoolOwner<A> owner;
        public bool isActive;

        public abstract void initializeObject();

        public virtual void destroy()
        {
            gameObject.SetActive(false);
        }

        public virtual IEnumerator destroyIE()
        {
            yield return null;
            gameObject.SetActive(false);
        }

        public void setOwner(IObjectPoolOwner<A> owner)
        {
            this.owner = owner;
        }
        public virtual void instantiate()
        {
            gameObject.SetActive(true);
        }

        public virtual void realDestroy()
        {
            Destroy(gameObject);
        }

        public virtual void destroySelf()
        {
            owner?.iDestroy(this);
        }
    }



    /// <summary>
    /// 오브젝트 풀 매니저 클래스입니다.
    /// </summary>
    /// <typeparam name="T">풀링할 오브젝트의 타입</typeparam>
    /// <typeparam name="A">오브젝트를 구분할 ID의 타입</typeparam>
    [Serializable]
    public class ObjectPoolManager<T, A> : IObjectPoolOwner<A> where T : ObjectPoolEntry<A>
    {
        private Dictionary<A, Queue<T>> objectPool;
        private Transform creativeArea;
        private Transform disableArea;

        private LinkedList<A> usageOrder;
        private Dictionary<A, LinkedListNode<A>> usageNodes;
        private HashSet<A> permanentList;
        private MonoBehaviour poolOwner;
        [NonSerialized] private Dictionary<T, Coroutine> pendingDestroyCoroutines;
        [NonSerialized] private int poolGeneration;
        

        private bool parent;

        [SerializeField]
        private bool useDestroyIE;
        [SerializeField]
        private int typeAmountLimit = 20;
        [SerializeField]
        private int objectAmountLimit = 20;
        
        private int additionalTypeAmountLimit = 0;
        private int totalTypeAmountLimit
        {
            get { return additionalTypeAmountLimit + typeAmountLimit; }
        }
        
        [ReadOnly,SerializeField] private int countTypeOverflow;
        [ReadOnly,SerializeField] private int countObjectOverflow;
        
       

        public void Initialize(Transform creativeArea, MonoBehaviour poolOwner,bool parent = true,Transform disableArea = null)
        {
            countObjectOverflow = 0;
            countTypeOverflow = 0;
            this.creativeArea = creativeArea;
            objectPool = new Dictionary<A, Queue<T>>();
            this.poolOwner = poolOwner;
            pendingDestroyCoroutines = new Dictionary<T, Coroutine>();
            poolGeneration++;
            permanentList = new HashSet<A>();
            this.parent = parent;
            if (disableArea == null)
            {
                this.disableArea = creativeArea;
            }
            else
            {
                this.disableArea = disableArea;
            }

            usageOrder = new LinkedList<A>();
            usageNodes = new Dictionary<A, LinkedListNode<A>>();
        }
        
        /// <summary>
        /// 지정한 수량만큼의 프리팹 인스턴스를 오브젝트 풀에 미리 저장합니다.
        /// </summary>
        /// <param name="prefab">풀에 저장할 프리팹 오브젝트입니다.</param>
        /// <param name="amount">미리 생성하여 풀에 저장할 인스턴스의 수량입니다.objectAmountLimit을 넘길 수 있습니다.</param>
        /// <param name="isPermanent">
        /// <c>true</c>로 설정하면 해당 프리팹 타입이 영구적으로 표시되어 타입 한도를 초과해도 LRU(Least Recently Used) 메커니즘에 의해 제거되지 않습니다.
        /// </param>
        public void StoreObjectInPool(T prefab, int amount,bool isPermanent)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            if (isPermanent)
            {
                if (permanentList.Add(prefab.id))
                    additionalTypeAmountLimit++;
            }

            if (!objectPool.TryGetValue(prefab.id, out Queue<T> objects))
            {
                if (objectPool.Count >= totalTypeAmountLimit)
                {
                    if (!RemoveLeastRecentlyUsedType())
                        return;
                }

                objects = new Queue<T>();
                objectPool.Add(prefab.id, objects);
            }

            UpdateUsageOrder(prefab.id);

            for (int i = 0; i < amount; i++)
            {
                T instance = Object.Instantiate(prefab);
                instance.isActive = false;
                instance.setOwner(this);
                instance.transform.SetParent(disableArea);
                instance.initializeObject();
                objects.Enqueue(instance);
                instance.gameObject.SetActive(false);
            }
        }


     
        public IEnumerator objectWaitIE()
        {
            foreach (var entry in objectPool)
            {
                foreach (var data in entry.Value)
                {
                    if (data != null)
                    {
                        data.gameObject.SetActive(true);
                    }
                }
            }

            yield return new WaitForSeconds(0.5f);
            
            foreach (var entry in objectPool)
            {
                foreach (var data in entry.Value)
                {
                    if (data != null)
                    {
                        data.gameObject.SetActive(false);
                    }
                }
            }
        }
        
        
       
        /// <summary>
        /// 오브젝트를 가져오거나 새로 생성합니다.
        /// </summary>
        /// <param name="prefab">프리팹 오브젝트</param>
        /// <returns>사용 가능한 오브젝트 인스턴스</returns>
        public T Instantiate(T prefab)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }
            
            UpdateUsageOrder(prefab.id);

            if (objectPool.TryGetValue(prefab.id, out Queue<T> objects) && objects.Count > 0)
            {
                T newObject = objects.Dequeue();
                if (newObject != null)
                {
                    newObject.isActive = true;
                    newObject.transform.SetParent(creativeArea,parent);
                    newObject.instantiate();
                    return newObject;
                }
            }

            T createdObject = Object.Instantiate(prefab);
            createdObject.isActive = true;
            createdObject.setOwner(this);
            createdObject.transform.SetParent(creativeArea,parent);
            createdObject.instantiate();
            return createdObject;
        }
        
        /// <summary>
        /// 오브젝트를 풀에 반환하거나 파괴합니다.
        /// </summary>
        /// <param name="instance">반환할 오브젝트 인스턴스</param>
        public void Destroy(T instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!instance.isActive)
            {
                return;
            }

            if (!usageNodes.ContainsKey(instance.id))
            {
                UpdateUsageOrder(instance.id);
            }

            instance.isActive = false;
            
            if (!objectPool.TryGetValue(instance.id, out Queue<T> objects))
            {
                // 타입 수가 한도를 넘으면 LRU 타입 제거
                if (objectPool.Count >= totalTypeAmountLimit)
                {
                    if (!RemoveLeastRecentlyUsedType())
                    {
                        instance.transform.SetParent(disableArea);
                        instance.realDestroy();
                        return;
                    }
                }

                objects = new Queue<T>();
                objectPool.Add(instance.id, objects);
            }

            if (useDestroyIE)
            {
                if (pendingDestroyCoroutines.ContainsKey(instance))
                    return;

                int generation = poolGeneration;
                Coroutine coroutine = poolOwner.StartCoroutine(DestroyIE(instance, objects, generation));
                pendingDestroyCoroutines[instance] = coroutine;
            }
            else
            {
                if (objects.Count < objectAmountLimit)
                {
                    instance.transform.SetParent(disableArea);
                    instance.initializeObject();
                    objects.Enqueue(instance);
                    instance.destroy();
                }
                else
                {
                    countObjectOverflow++;
                    instance.transform.SetParent(disableArea);
                    instance.realDestroy(); // 풀에 여유 공간이 없을 때 파괴
                }
            }
        }

        private IEnumerator DestroyIE(T instance, Queue<T> objects, int generation)
        {
            try
            {
                instance.isActive = false;
                instance.transform.SetParent(disableArea);
                yield return instance.destroyIE();

                if (instance == null)
                    yield break;

                if (generation != poolGeneration ||
                    !objectPool.TryGetValue(instance.id, out Queue<T> currentObjects) ||
                    !ReferenceEquals(currentObjects, objects))
                {
                    Object.Destroy(instance.gameObject);
                    yield break;
                }

                instance.initializeObject();
                if (objects.Count < objectAmountLimit)
                {
                    objects.Enqueue(instance);
                }
                else
                {
                    instance.realDestroy();
                }
            }
            finally
            {
                if (pendingDestroyCoroutines != null)
                    pendingDestroyCoroutines.Remove(instance);
            }
        }
        
        
        /// <summary>
        /// 풀의 모든 데이터를 삭제하고 메모리를 정리합니다.
        /// </summary>
        public void ClearAllData()
        {
            poolGeneration++;

            if (pendingDestroyCoroutines != null && pendingDestroyCoroutines.Count > 0)
            {
                List<KeyValuePair<T, Coroutine>> pendingDestructions =
                    new List<KeyValuePair<T, Coroutine>>(pendingDestroyCoroutines);
                pendingDestroyCoroutines.Clear();

                foreach (var pendingDestruction in pendingDestructions)
                {
                    if (pendingDestruction.Value != null && poolOwner != null)
                        poolOwner.StopCoroutine(pendingDestruction.Value);

                    if (pendingDestruction.Key != null)
                        Object.Destroy(pendingDestruction.Key.gameObject);
                }
            }

            foreach (var entry in objectPool)
            {
                foreach (var data in entry.Value)
                {
                    if (data != null)
                    {
                        Object.Destroy(data.gameObject);
                    }
                }
            }

            objectPool.Clear();
            usageOrder.Clear();
            usageNodes.Clear();
            permanentList.Clear();
            additionalTypeAmountLimit = 0;
            
        }

        /// <summary>
        /// 사용 순서를 업데이트합니다.
        /// </summary>
        /// <param name="id">사용된 타입의 ID</param>
        private void UpdateUsageOrder(A id)
        {
            if (usageNodes.TryGetValue(id, out LinkedListNode<A> node))
            {
                // 기존 노드를 리스트의 끝으로 이동
                usageOrder.Remove(node);
                usageOrder.AddLast(node);
            }
            else
            {
                // 새로운 노드를 리스트의 끝에 추가
                node = new LinkedListNode<A>(id);
                usageOrder.AddLast(node);
                usageNodes.Add(id, node);
            }
        }

        /// <summary>
        /// 가장 오래된 타입을 제거합니다.
        /// </summary>
        private bool RemoveLeastRecentlyUsedType()
        {
            // 리스트의 첫 번째 노드가 가장 오래된 타입
            LinkedListNode<A> oldestNode = usageOrder.First;
            while (oldestNode != null &&
                   (permanentList.Contains(oldestNode.Value) ||
                    !objectPool.ContainsKey(oldestNode.Value)))
            {
                oldestNode = oldestNode.Next;
            }
            if (oldestNode != null)
            {
                countTypeOverflow++;
                A oldestId = oldestNode.Value;

                // 해당 타입의 오브젝트들을 파괴
                if (objectPool.TryGetValue(oldestId, out Queue<T> objects))
                {
                    Queue<T> objectsCopy = new Queue<T>(objects);
                    poolOwner?.StartCoroutine(destroyObjects(objectsCopy));
                    objectPool.Remove(oldestId);
                }

                // 사용 순서에서 제거
                usageOrder.Remove(oldestNode);
                usageNodes.Remove(oldestId);
                return true;
            }

            return false;
        }

        private IEnumerator destroyObjects(Queue<T> objects)
        {
            while (objects.Count > 0)
            {
                T obj = objects.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj.gameObject);
                    yield return null;
                }
            }
        }


        public void iDestroy(ObjectPoolEntry<A> entry)
        {
            if (entry is T obj)
            {
                Destroy(obj);
            }
        }
    }
}
