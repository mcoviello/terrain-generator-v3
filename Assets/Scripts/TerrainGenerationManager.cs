using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public struct ChunkMeshData
{
    public Vector3[] vertices;
    public Vector3[] normals;
    public Vector2[] uvs;
    public int[] indices;
}

public class TerrainGenerationManager : Singleton<TerrainGenerationManager>
{

    [SerializeField] private int viewDist;
    [SerializeField] private GameObject player;
    [SerializeField] private Material mat;
    [SerializeField] private AnimationCurve LODBias;
    [SerializeField] public ChunkMeshes ChunkLODData;
    [SerializeField] public float MaxHeight = 100;

    [HideInInspector] public int ChunkSize;
    [HideInInspector] public int VerticesAlongEdge;
    [HideInInspector] public int NoOfLODs;

    private ChunkMeshData[] ChunkLODMeshes;

    private List<ChunkInfo> PreviousFrameVisibleChunks;

    public void Awake()
    {
        ChunkLODMeshes = ChunkLODData.GetLODMeshes();
        PlayerController.onPlayerCrossedChunkBoundary += UpdateChunks;
        PreviousFrameVisibleChunks = new List<ChunkInfo>();
    }

    private void UpdateChunks(Vector2Int newPos)
    {
        // --- The General Algorithm ---//
        //Update all chunks shown previous frame
        //Find all positions where chunks should be shown
        //Hide all other chunks
        //Find all the shown chunks' LOD levels
        //Check if the chunks already exist
            //If they exist, check if the LOD needs updating
                //If it does, set new mesh blah blah
            //Else, generate the new chunk and noise map
        // --- End General Algorithm --- //

        foreach(ChunkInfo c in PreviousFrameVisibleChunks)
        {
            c.DisableChunk();
        }

        PreviousFrameVisibleChunks.Clear();

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
                    ChunkInfo c = ChunkPoolManager.Instance.GetChunkIfExists(iteratingChunkCoords);
                    c.EnableChunk();
                    PreviousFrameVisibleChunks.Add(c);
                } else
                {
                    chunkToCheck.EnableChunk();
                    PreviousFrameVisibleChunks.Add(chunkToCheck);

                    if (chunkToCheck.CurrentLOD != LODToUse)
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
        ChunkPoolManager.Instance.RequestChunk(new Vector3(Mathf.Round((chunkCoords.x * ChunkSize) - ChunkSize / 2.0f), 0, Mathf.Round((chunkCoords.y * ChunkSize) - ChunkSize / 2.0f)), chunkCoords, LODToUse);
        ChunkPoolManager.Instance.GetChunkIfExists(chunkCoords).SetMaterial(mat);
    }

    private int CalculateLODLevelForChunk(float distanceToPlayer)
    {
        float distPercent = Mathf.InverseLerp(0.0f, viewDist * ChunkSize, distanceToPlayer * ChunkSize);
        int LODToUse = Mathf.FloorToInt(LODBias.Evaluate(distPercent) * (NoOfLODs - 1));
        return LODToUse;
    }

}
