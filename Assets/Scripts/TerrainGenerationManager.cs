using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public struct ChunkMeshData
{
    public Vector3[] vertices;
    public Vector2[] uvs;
    public int[] indices;
}

public class TerrainGenerationManager : Singleton<TerrainGenerationManager>
{

    [SerializeField] int viewDist;
    [SerializeField] public ChunkMeshes ChunkLODData;
    [SerializeField] GameObject player;
    [SerializeField] Material mat;
    [SerializeField] AnimationCurve LODBias;

    [HideInInspector] public int ChunkSize;
    [HideInInspector] public int VerticesAlongEdge;
    [HideInInspector] public int NoOfLODs;

    private ChunkMeshData[] ChunkLODMeshes;

    public void Awake()
    {
        ChunkLODMeshes = ChunkLODData.GetLODMeshes();
        PlayerController.onPlayerCrossedChunkBoundary += UpdateChunks;
    }

    public void Start()
    {
        
    }

    public void Update()
    {
        
    }

    private void UpdateChunks(Vector2Int newPos)
    {
        // --- The General Algorithm ---//
        //Find all positions where chunks should be shown
        //Hide all other chunks
        //Find all the shown chunks' LOD levels
        //Check if the chunks already exist
            //If they exist, check if the LOD needs updating
                //If it does, set new mesh blah blah
            //Else, generate the new chunk and noise map
        // --- End General Algorithm --- //

        Vector2Int iteratingChunkCoords = newPos;

        //Iterate over every chunk in the view distance
        for(int x = -viewDist; x <= viewDist; x++)
        {
            for (int z = -viewDist; z <= viewDist; z++)
            {
                //Current chunk in render distance coords for checks
                iteratingChunkCoords.x = newPos.x + x;
                iteratingChunkCoords.y = newPos.y + z;

                var distToPlayer = Vector2Int.Distance(iteratingChunkCoords, newPos);
                int LODToUse = CalculateLODLevelForChunk(distToPlayer);

                ChunkInfo chunkToCheck = ChunkPoolManager.Instance.GetChunkIfExists(iteratingChunkCoords);

                if (chunkToCheck == null)
                {
                    //If chunk doesn't exist yet, make it.
                    InitializeNewChunk(iteratingChunkCoords, LODToUse);
                } else
                {
                    if(chunkToCheck.CurrentLOD != LODToUse)
                    {
                        //Needs an LOD Update.
                        chunkToCheck.UpdateMeshLOD(LODToUse);
                    }
                }
            }
        }
    }

    public ChunkMeshData GetMeshDataForLOD(int LOD)
    {
        if(LOD > -1 && LOD < ChunkLODMeshes.Length)
        {
            return ChunkLODMeshes[LOD];
        } else
        {
            Debug.LogError("Requested LOD is Invalid");
            return new ChunkMeshData();
        }
    }

    private void InitializeNewChunk(Vector2Int chunkCoords, int LODToUse)
    {
        GameObject newChunk = ChunkPoolManager.Instance.RequestChunk(new Vector3(Mathf.Round((chunkCoords.x * ChunkSize) - ChunkSize / 2.0f), 0, Mathf.Round((chunkCoords.y * ChunkSize) - ChunkSize / 2.0f)), chunkCoords);
        var newChunkInfo = newChunk.GetComponent<ChunkInfo>();
        newChunk.GetComponent<MeshRenderer>().material = mat;
        newChunk.SetActive(true);
        newChunkInfo.RegenerateHeightMap();
        newChunkInfo.UpdateMeshLOD(LODToUse);
    }

    private int CalculateLODLevelForChunk(float distanceToPlayer)
    {
        float distPercent = Mathf.InverseLerp(0.0f, viewDist * ChunkSize, distanceToPlayer * ChunkSize);
        int LODToUse = Mathf.RoundToInt(LODBias.Evaluate(distPercent) * (NoOfLODs - 1));
        return LODToUse;
    }

    public static float CalculateLODMultiplier(int CurrentLOD)
    {
        return 1 / (float)Mathf.Pow(2, CurrentLOD);
    }

}
