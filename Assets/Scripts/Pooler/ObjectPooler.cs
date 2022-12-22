using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : Singleton<ObjectPooler>
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefabToPool;
        public int poolSize;
    }

    [SerializeField]public List<Pool> pools;

    public Dictionary<string, Queue<GameObject>> poolDict;

    void Awake()
    {
        poolDict = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            GameObject poolObj = new GameObject(pool.tag + " Pool");
            poolObj.transform.SetParent(this.gameObject.transform);
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for(int i = 0; i < pool.poolSize; i++)
            {
                GameObject obj = Instantiate(pool.prefabToPool);
                obj.SetActive(false);
                obj.transform.SetParent(poolObj.transform);
                objectPool.Enqueue(obj);
            }

            poolDict.Add(pool.tag, objectPool);
        }
    }

    public GameObject RequestFromPool(string poolTag)
    {
        if (!poolDict.ContainsKey(poolTag))
        {
            Debug.LogError("Pool Dictionary does not contain a pool called " + poolTag);
            return null;
        }
        GameObject objToReturn =  poolDict[poolTag].Dequeue();
        poolDict[poolTag].Enqueue(objToReturn);
        IPoolable poolScript = objToReturn.GetComponent<IPoolable>();

        if(poolScript != null)
        {
            poolScript.OnObjectSpawned();
        }

        return objToReturn;
    }

    public GameObject RequestFromPool(string poolTag, Vector3 positionToSet)
    {
        GameObject obj = RequestFromPool(poolTag);
        obj.transform.position = positionToSet;
        return obj;
    }
}
