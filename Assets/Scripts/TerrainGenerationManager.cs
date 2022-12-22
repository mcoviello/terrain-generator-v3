using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

    private Mesh[] ChunkLODMeshes;

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
                    GameObject newChunk = ChunkPoolManager.Instance.RequestChunk(new Vector3((iteratingChunkCoords.x * ChunkSize) - ChunkSize / 2, 0, (iteratingChunkCoords.y * ChunkSize) - ChunkSize / 2), iteratingChunkCoords);
                    newChunk.GetComponent<ChunkInfo>().UpdateMeshLOD(ChunkLODMeshes[LODToUse], LODToUse);
                    newChunk.GetComponent<MeshRenderer>().material = mat;
                    newChunk.SetActive(true);
                } else
                {
                    if(chunkToCheck.CurrentLOD != LODToUse)
                    {
                        //Needs an LOD Update.
                        chunkToCheck.UpdateMeshLOD(ChunkLODMeshes[LODToUse], LODToUse);
                    }
                }
            }
        }
    }
    private int CalculateLODLevelForChunk(float distanceToPlayer)
    {
        float distPercent = Mathf.InverseLerp(0.0f, viewDist * ChunkSize, distanceToPlayer * ChunkSize);
        int LODToUse = Mathf.RoundToInt(LODBias.Evaluate(distPercent) * (NoOfLODs - 1));
        return LODToUse;
    }

}
