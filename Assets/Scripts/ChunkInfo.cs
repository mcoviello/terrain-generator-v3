using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using UnityEditor;

public class ChunkInfo : MonoBehaviour
{
    public int CurrentLOD { get; private set; }

    public RenderTexture maxQualityNoiseMap;

    public Vector2Int gridCoordinates;

    private MeshFilter mesh;

    public Texture2D reader;

    private float[] heightMap;

    private bool currentlyAdjustingMesh;

    private Vector4 FloatUnpackingVector = new Vector4(1.0f, 1 / 255.0f, 1 / 65025.0f, 1 / 160581375.0f);


    void Awake()
    {
        maxQualityNoiseMap = new RenderTexture(TerrainGenerationManager.Instance.VerticesAlongEdge, 
            TerrainGenerationManager.Instance.VerticesAlongEdge, 24, 
            RenderTextureFormat.ARGB32);
        maxQualityNoiseMap.filterMode = FilterMode.Point;
        maxQualityNoiseMap.useMipMap = false;
        maxQualityNoiseMap.enableRandomWrite = true;
        maxQualityNoiseMap.wrapMode = TextureWrapMode.Clamp;
        maxQualityNoiseMap.Create();

        reader = new Texture2D(maxQualityNoiseMap.width, maxQualityNoiseMap.height, TextureFormat.ARGB32, false);
        reader.alphaIsTransparency = true;
        reader.wrapMode = TextureWrapMode.Clamp;
        reader.filterMode = FilterMode.Point;

        heightMap = new float[maxQualityNoiseMap.width * maxQualityNoiseMap.height];

        mesh = this.GetComponent<MeshFilter>();
        mesh.mesh = new Mesh();
        mesh.mesh.MarkDynamic();
        gridCoordinates = new Vector2Int(int.MaxValue, int.MaxValue);
    }

    public void ReuseChunk(Vector3 newPos, Vector2Int newGridPos)
    {
        this.transform.position = newPos;
        gridCoordinates = newGridPos;
    }

    public void UpdateMeshLOD(int newLOD)
    {
        CurrentLOD = newLOD;
        ScheduleMeshHeightAdjustment(newLOD);
    }

    #region Height Map Generation
    public void RegenerateHeightMap()
    {
        //Request GPU to generate new heightmap
        NoiseGenerator.Instance.GenerateNoiseForChunk(gridCoordinates, maxQualityNoiseMap);
        //Request the GPU to retrieve the heightmap data from the GPU, and set callback
        GetHeightMapDataFromRenderTex(ReadBackHeightMapDataToTexture2D);
    }
    public void GetHeightMapDataFromRenderTex(Action<AsyncGPUReadbackRequest> onRequestCompleteCallback)
    {
        IEnumerator RequestAsync(AsyncGPUReadbackRequest request, Action<AsyncGPUReadbackRequest> callbackAction)
        {
            while (!request.done)
            {
                yield return null;
            }
            callbackAction(request);
        }

        AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(maxQualityNoiseMap);
        StartCoroutine(RequestAsync(request, onRequestCompleteCallback));
    }

    float DecodeFloatRGBA(Color enc)
    {
        return Vector4.Dot(enc, FloatUnpackingVector);
    }

    private void ReadBackHeightMapDataToTexture2D(AsyncGPUReadbackRequest readBackHeightMap)
    {
        //When the heightmap data has been retrieved, write it into a texture.
        reader.LoadRawTextureData(readBackHeightMap.GetData<uint>());
        reader.Apply();

        //Debug draw noise texture
        this.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", reader);

        UnpackColorsToFloats();

        //NOW update vertex heights
        //AdjustMeshToMatchHeightmap();
        ScheduleMeshHeightAdjustment(CurrentLOD);
    }
    #endregion

    #region Mesh Adjustment
    public void ScheduleMeshHeightAdjustment(int newLOD)
    {
        ChunkMeshData desiredLODData = TerrainGenerationManager.Instance.GetMeshDataForLOD(newLOD);

        AdjustMeshHeightJob job = new AdjustMeshHeightJob();
        job.verts = new NativeArray<Vector3>(desiredLODData.vertices, Allocator.TempJob); ;
        job.heightMap = new NativeArray<float>(heightMap, Allocator.TempJob); ;
        job.verticesAlongEdge = TerrainGenerationManager.Instance.VerticesAlongEdge;
        job.CurrentLOD = newLOD;
        job.result = new NativeArray<Vector3>(desiredLODData.vertices.Length, Allocator.TempJob); ;

        JobHandle handle = job.Schedule();
        JobManager.Instance.ScheduleJobForCompletion(handle);
        StartCoroutine(HandleMeshAdjustmentJob(job, handle, newLOD, desiredLODData));
    }

    private void UnpackColorsToFloats()
    {
        var colors = reader.GetPixels();

        for (int i = 0; i < colors.Length; i++)
        {
            heightMap[i] = DecodeFloatRGBA(colors[i]);
        }
    }

    private IEnumerator HandleMeshAdjustmentJob(AdjustMeshHeightJob job, JobHandle handle, int jobLOD, ChunkMeshData desiredLODData)
    {
        while(!handle.IsCompleted)
        {
          yield return null;
        }

        handle.Complete();

        //In case a different level of LOD has been requested since this began
        if(CurrentLOD == jobLOD)
        {
            desiredLODData.vertices = job.result.ToArray();
            SetMeshFromChunkMeshData(desiredLODData);
        }

        job.verts.Dispose();
        job.heightMap.Dispose();
        job.result.Dispose();
    }

    void SetMeshFromChunkMeshData(ChunkMeshData data) {
        mesh.mesh.Clear(true);
        mesh.mesh.SetVertices(data.vertices);
        mesh.mesh.SetIndices(data.indices, MeshTopology.Triangles, 0);
        mesh.mesh.SetUVs(0,data.uvs);
        mesh.mesh.RecalculateBounds();
        mesh.mesh.RecalculateNormals();
        mesh.mesh.MarkModified();
    }

    //Job to export array of Vector3s with modified heights.
    [BurstCompile]
    public struct AdjustMeshHeightJob : IJob
    {
        public NativeArray<Vector3> verts;
        public NativeArray<float> heightMap;
        public NativeArray<Vector3> result;
        public int verticesAlongEdge;
        public int CurrentLOD;
        public void Execute()
        {
            AdjustMeshToMatchHeightmap();
        }
        private void AdjustMeshToMatchHeightmap()
        {
            //Calculate chunk dimensions at current LOD
            float lodMul = TerrainGenerationManager.CalculateLODMultiplier(CurrentLOD);
            int verticesAlongEdgeForLOD = Mathf.RoundToInt(verticesAlongEdge * (lodMul));


            //Update all vertex heights to match heightmap
            for (int i = 0; i < verts.Length; i++)
            {
                //Get the pixel at the place the vertex would be at
                Get2DArrIndex(i, verticesAlongEdgeForLOD, out int x, out int y);
                float xPercentage = x / (float)(verticesAlongEdgeForLOD - 1);
                float yPercentage = y / (float)(verticesAlongEdgeForLOD - 1);

                int index = (Mathf.RoundToInt(yPercentage * (verticesAlongEdge - 1)) * verticesAlongEdge)
                    + Mathf.RoundToInt(xPercentage * (verticesAlongEdge - 1));
                float height = heightMap[index];
                result[i] = new Vector3( verts[i].x, height * 100.0f, verts[i].z);
            }
        }

        private void Get2DArrIndex(int vert, int dimensions, out int x, out int y)
        {
            x = vert % dimensions;
            y = vert / dimensions;
        }
    }
#endregion
}
