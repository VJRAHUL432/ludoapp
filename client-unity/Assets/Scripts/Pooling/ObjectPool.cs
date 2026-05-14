using System.Collections.Generic;
using UnityEngine;

namespace Ludo.Pooling
{
    /// <summary>
    /// Generic GameObject pool. Avoids GC spikes for frequently spawned items
    /// (kill effects, dice particles, toast notifications, etc.).
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialSize = 8;
        [SerializeField] private int maxSize = 64;

        private readonly Stack<GameObject> _pool = new Stack<GameObject>();

        private void Awake()
        {
            if (prefab == null) return;
            for (int i = 0; i < initialSize; i++) _pool.Push(CreateNew());
        }

        public GameObject Spawn(Vector3 position, Quaternion rotation)
        {
            GameObject go = _pool.Count > 0 ? _pool.Pop() : CreateNew();
            if (go == null) return null;
            go.transform.SetPositionAndRotation(position, rotation);
            go.SetActive(true);
            return go;
        }

        public void Despawn(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            if (_pool.Count >= maxSize) { Destroy(go); return; }
            _pool.Push(go);
        }

        private GameObject CreateNew()
        {
            if (prefab == null) { Debug.LogError("[ObjectPool] No prefab assigned"); return null; }
            var go = Instantiate(prefab, transform);
            go.SetActive(false);
            return go;
        }
    }
}
