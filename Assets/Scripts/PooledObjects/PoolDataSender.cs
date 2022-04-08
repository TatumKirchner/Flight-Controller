using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PoolManager
{
    public class PoolDataSender : MonoBehaviour
    {
        [SerializeField]
        private List<PooledObjectManager.PoolStruct> m_PoolData = new List<PooledObjectManager.PoolStruct>();

        private void Start()
        {
            PooledObjectManager.instance.InitalizePools(m_PoolData);
        }
    }
}
