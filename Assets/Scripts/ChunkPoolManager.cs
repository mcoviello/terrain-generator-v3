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
    private Dictionary<Vector2Int, GameObject> chunkCoords;

    CompareChunksDistToPlayer ChunkComparer;

    private void Awake()
    {
        PlayerController.onPlayerCrossedChunkBoundary += SortChunkList;
        ChunkComparer = new();
        //Spawn Chunk Pool
        chunks = new List<GameObject>();
        chunkCoords = new Dictionary<Vector2Int, GameObject>();

        for(int i = 0; i < chunkPoolSize; i++)
        {
            GameObject obj = Instantiate(chunkPrefab);
            obj.transform.SetParent(transform);
            //Terrain Layer
            obj.layer = 3;

            if (i == 0)
            {
                chunks.Add(obj);
            }
            else
            {
                SortedAdd(obj);
            }
        }
    }

    public GameObject RequestChunk(Vector3 newPosition, Vector2Int newGridPosition, int LOD)
    {
        GameObject objToReturn = chunks.Last();
        chunks.RemoveAt(chunks.Count - 1);

        ChunkInfo poolScript = objToReturn.GetComponent<ChunkInfo>();

        Vector2Int oldCoords = poolScript.gridCoordinates;
        if (chunkCoords.ContainsKey(oldCoords))
        {
            chunkCoords.Remove(oldCoords);
        }

        poolScript.ReuseChunk(newPosition, newGridPosition, LOD);

        SortedAdd(objToReturn);
        chunkCoords.Add(newGridPosition, objToReturn);

        return objToReturn;
    }

    public ChunkInfo GetChunkIfExists(Vector2Int gridPos)
    {
        if (chunkCoords.ContainsKey(gridPos)){
            return chunkCoords[gridPos].GetComponent<ChunkInfo>();
        }

        return null;
    }

    private void SortedAdd(GameObject objToAdd)
    {
        int binarySearchInd = chunks.BinarySearch(objToAdd, ChunkComparer);
        if(binarySearchInd < 0)
        {
            binarySearchInd = (chunks.Count) - ~binarySearchInd;
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
        var distToChunk1 = (playerPos - x.transform.position).sqrMagnitude;
        var distToChunk2 = (playerPos - y.transform.position).sqrMagnitude;

        return distToChunk1.CompareTo(distToChunk2);
    }
}
