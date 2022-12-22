using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//Manages the pool of chunks to be requested by the Terrain Generation Manager
//Not an object pool, because it needs to be able to sort the chunks by distance to the player!

public class ChunkPoolManager : Singleton<ChunkPoolManager>
{
    [SerializeField] private int chunkPoolSize;
    [SerializeField] private GameObject chunkPrefab;
    private List<GameObject> chunks;

    CompareChunksDistToPlayer ChunkComparer;

    private void Awake()
    {
        PlayerController.onPlayerCrossedChunkBoundary += SortChunkList;
        ChunkComparer = new();
        //Spawn Chunk Pool
        chunks = new List<GameObject>();

        for(int i = 0; i < chunkPoolSize; i++)
        {
            GameObject obj = Instantiate(chunkPrefab);
            obj.SetActive(false);
            obj.transform.SetParent(transform);

            if(i == 0) { chunks.Add(obj); }

            SortedAdd(obj);
        }
    }

    public GameObject RequestChunk(Vector3 newPosition, Vector2Int newGridPosition)
    {
        GameObject objToReturn = chunks.Last();
        chunks.RemoveAt(chunks.Count - 1);
        ChunkInfo poolScript = objToReturn.GetComponent<ChunkInfo>();

        if (poolScript != null)
        {
            poolScript.ReuseChunk(newPosition, newGridPosition);
        }

        SortedAdd(objToReturn);

        return objToReturn;
    }

    public ChunkInfo GetChunkIfExists(Vector2Int gridPos)
    {
        //TODO: Could this be more efficient?
        //Could add map of chunks to positions... could fill up memory though.
        ChunkInfo temp;
        foreach(GameObject g in chunks)
        {
            temp = g.GetComponent<ChunkInfo>();
            if (temp.gridCoordinates == gridPos)
            {
                return temp;
            }
        }

        return null;
    }

    private void SortedAdd(GameObject objToAdd)
    {
        int binarySearchInd = chunks.BinarySearch(objToAdd, ChunkComparer);
        if(binarySearchInd < 0)
        {
            binarySearchInd = ~binarySearchInd;
        }
        chunks.Insert(binarySearchInd, objToAdd);
    }

    public void SortChunkList(Vector2Int newChunkGridCoords)
    {
        //Called when the player crosses a chunk boundary - NOT EVERY FRAME
        chunks.Sort(ChunkComparer);
    }
}

class CompareChunksDistToPlayer : IComparer<GameObject>
{
    public int Compare(GameObject x, GameObject y)
    {
        //Sort chunks by distance to the player
        Vector3 playerPos = PlayerController.Instance.transform.position;
        var distToChunk1 = Vector3.Distance(playerPos, x.transform.position);
        var distToChunk2 = Vector3.Distance(playerPos, y.transform.position);

        return distToChunk1.CompareTo(distToChunk2);
    }
}
