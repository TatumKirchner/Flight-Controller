using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace PoolManager 
{
    public class PooledObjectManager : MonoBehaviour
    {
        [System.Serializable]
        public struct PoolStruct
        {
            public string m_PoolName;
            public GameObject m_Prefab;
            public int m_Size;
            [Tooltip("If true, once the pool has used all of its GameObjects, re-use the first active one. Otherwise grow the pool.")]
            public bool m_LimitReachUseActiveObject;
            [Tooltip("If true, parent all of the pooled objects under an empty GameObject.")]
            public bool m_AutoParentObjects;
        }

        private List<PoolStruct> m_Pools = new List<PoolStruct>();

        private Dictionary<GameObject, List<GameObject>> m_PoolsObjects = new Dictionary<GameObject, List<GameObject>>();
        private Dictionary<GameObject, List<GameObject>> m_PoolsActiveObjects = new Dictionary<GameObject, List<GameObject>>();

        public static PooledObjectManager instance;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(this);
            }
        }

        public void InitalizePools(List<PoolStruct> pools)
        {
            m_Pools = pools;
            ClearOlderPools();
            CreatePools();
        }

        private void ClearOlderPools()
        {
            m_PoolsObjects.Clear();
            m_PoolsActiveObjects.Clear();
        }

        private void CreatePools()
        {
            for (int i = 0; i < m_Pools.Count; i++)
            {
                PoolStruct pool = m_Pools[i];
                if (IsValidPoolStruct(pool))
                {
                    m_PoolsObjects.Add(pool.m_Prefab, new List<GameObject>());
                    m_PoolsActiveObjects.Add(pool.m_Prefab, new List<GameObject>());
                    GrowPool(m_Pools[i]);
                }
            }
        }

        private bool IsValidPoolStruct(PoolStruct pool)
        {
            return pool.m_Prefab != null;
        }

        private void GrowPool(PoolStruct pool)
        {
            if (pool.m_AutoParentObjects)
            {
                GameObject poolParent = new GameObject(pool.m_PoolName + " Pool");
                for (int i = 0; i < pool.m_Size; i++)
                {
                    CreatePooledObject(pool.m_Prefab, poolParent.transform);
                }
            }
            else
            {
                for (int i = 0; i < pool.m_Size; i++)
                {
                    CreatePooledObject(pool.m_Prefab);
                }
            }
        }

        /// <summary>
        /// Creates a pooled object that is parented to the poolParent.
        /// </summary>
        /// <param name="poolPrefab">The prefab to create.</param>
        /// <param name="poolParent">The parent which the prefabs will be parented to.</param>
        private void CreatePooledObject(GameObject poolPrefab, Transform poolParent)
        {
            GameObject pooledGameObject = Instantiate(poolPrefab, poolParent.transform);
            
            if (!pooledGameObject.TryGetComponent(out PooledObject pooledScript))
            {
                Debug.LogErrorFormat("Invalid Prefab for PoolManager: The prefab {0} should be derived from PooledObject.", poolPrefab.name);
                Destroy(pooledGameObject);
            }
            else
            {
                pooledScript.InitPooledObject(poolPrefab);
                pooledGameObject.SetActive(false);
                m_PoolsObjects[poolPrefab].Add(pooledGameObject); ;
            }
        }

        private void CreatePooledObject(GameObject poolPrefab)
        {
            GameObject pooledGameObject = Instantiate(poolPrefab);
            
            if (!pooledGameObject.TryGetComponent(out PooledObject pooledScript))
            {
                Debug.LogErrorFormat("Invalid Prefab for PoolManager: The prefab {0} should be derived from PooledObject.", poolPrefab.name);
                Destroy(pooledGameObject);
            }
            else
            {
                pooledScript.InitPooledObject(poolPrefab);
                pooledGameObject.SetActive(false);
                m_PoolsObjects[poolPrefab].Add(pooledGameObject); ;
            }
        }

        /// <summary>
        /// Gets a component from a pooled object.
        /// </summary>
        /// <typeparam name="T">The component to get.</typeparam>
        /// <param name="prefab">The pooled prefab to get the component from.</param>
        /// <returns></returns>
        public T UseObjectFromPool<T>(GameObject prefab)
        {
            GameObject pooledObject = UseObjectFromPool(prefab);
            return pooledObject.GetComponent<T>();
        }

        /// <summary>
        /// Get a GameObject from the pool and set the position and rotation.
        /// </summary>
        /// <param name="prefab">The prefab to get from the pool.</param>
        /// <param name="position">The position to set the GameObject to.</param>
        /// <param name="rotation">The rotation to set the GameObject to.</param>
        /// <param name="useLocalPosition">If true will set the local position of the GameObject.</param>
        /// <returns></returns>
        public GameObject UseObjectFromPool(GameObject prefab, Vector3 position, Quaternion rotation, bool useLocalPosition = false)
        {
            GameObject pooledObject = GetObjectFromPool(prefab);

            if (pooledObject != null)
            {
                if (useLocalPosition)
                {
                    pooledObject.transform.localPosition = position;
                    pooledObject.transform.localRotation = rotation;
                    pooledObject.SetActive(true);
                }
                else
                {
                    pooledObject.transform.SetPositionAndRotation(position, rotation);
                    pooledObject.SetActive(true);
                }

                m_PoolsActiveObjects[prefab].Add(pooledObject);
            }

            return pooledObject;
        }

        private GameObject UseObjectFromPool(GameObject prefab)
        {
            GameObject pooledObject = GetObjectFromPool(prefab);

            if (pooledObject != null)
            {
                pooledObject.SetActive(true);

                m_PoolsActiveObjects[prefab].Add(pooledObject);
            }

            return pooledObject;
        }

        private GameObject GetObjectFromPool(GameObject prefab)
        {
            List<GameObject> pooledObject = new List<GameObject>();
            m_PoolsObjects.TryGetValue(prefab, out pooledObject);

            for (int i = 0; i < pooledObject.Count; i++)
            {
                if (!pooledObject[i].activeInHierarchy)
                {
                    return pooledObject[i];
                }
            }
            return NotEnoughPooledObject(prefab);
        }

        private GameObject NotEnoughPooledObject(GameObject prefab)
        {
            PoolStruct pool = GetPoolStruct(prefab);

            if (IsValidPoolStruct(pool))
            {
                if (pool.m_LimitReachUseActiveObject)
                {
                    return UseFirstActivePooledObject(pool);
                }
                else
                {
                    return DynamicGrowPool(pool);
                }
            }

            return null;
        }

        private PoolStruct GetPoolStruct(GameObject poolPrefab)
        {
            for (int i = 0; i < m_Pools.Count; i++)
            {
                if (m_Pools[i].m_Prefab.Equals(poolPrefab))
                {
                    return m_Pools[i];
                }
            }

            return new PoolStruct() { m_Prefab = null };
        }

        private GameObject UseFirstActivePooledObject(PoolStruct pool)
        {
            List<GameObject> activeObjects = new List<GameObject>();
            m_PoolsActiveObjects.TryGetValue(pool.m_Prefab, out activeObjects);

            if (activeObjects.Count != 0)
            {
                GameObject firstElement = activeObjects[0];
                activeObjects.RemoveAt(0);
                return firstElement;
            }

            return null;
        }

        private GameObject DynamicGrowPool(PoolStruct pool)
        {
            Debug.LogErrorFormat("POOL TO SMALL: The pool {0} is not big enough, creating {1} more PooledObject. Increments the pool size.",
                pool.m_Prefab.name, pool.m_Size);

            GrowPool(pool);
            return GetObjectFromPool(pool.m_Prefab);
        }

        public void ReturnPooledObject(PooledObject pooledObject, GameObject poolOwner)
        {
            PoolStruct pool = GetPoolStruct(pooledObject.gameObject);

            if (m_PoolsActiveObjects.ContainsKey(pooledObject.PoolOwner))
            {
                List<GameObject> activeObjects = m_PoolsActiveObjects[poolOwner];

                if (activeObjects.Count != 0)
                {
                    activeObjects.Remove(pooledObject.gameObject);
                }
            }
        }
    }
}
