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
using System.Runtime.CompilerServices;
using UnityEditor.UIElements;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class ChunkInfo : MonoBehaviour
{
    public int CurrentLOD { get; private set; }

    public Vector2Int gridCoordinates;

    private MeshFilter chunkMesh;

    private MeshRenderer chunkRenderer;

    private MeshCollider chunkCollider;

    public ComputeBuffer buffer;

    private float[] heightMap;

    private bool ColliderGenerated;

    void Awake()
    {
        buffer = new ComputeBuffer(TerrainGenerationManager.Instance.VerticesAlongEdge * TerrainGenerationManager.Instance.VerticesAlongEdge, 
            sizeof(float), ComputeBufferType.Structured);

        heightMap = new float[TerrainGenerationManager.Instance.VerticesAlongEdge * TerrainGenerationManager.Instance.VerticesAlongEdge];

        chunkMesh = GetComponent<MeshFilter>();
        chunkMesh.mesh = new Mesh();
        chunkMesh.mesh.indexFormat = IndexFormat.UInt32;
        chunkMesh.mesh.MarkDynamic();

        chunkRenderer = GetComponent<MeshRenderer>();

        chunkCollider = GetComponent<MeshCollider>();
        chunkCollider.sharedMesh = new Mesh();
        chunkCollider.sharedMesh.indexFormat = IndexFormat.UInt32;
        chunkCollider.sharedMesh.MarkDynamic();

        gridCoordinates = Vector2Int.one * int.MaxValue;
    }

    public void ReuseChunk(Vector3 newPos, Vector2Int newGridPos, int LOD)
    {
        DisableChunk();
        ColliderGenerated = false;
        transform.position = newPos;
        gridCoordinates = newGridPos;
        CurrentLOD = LOD;
        RegenerateHeightMap();
    }

    /// <summary>
    /// Alert the chunk of an LOD change.
    /// </summary>
    /// <param name="newLOD"></param>
    public void UpdateMeshLOD(int newLOD)
    {
        CurrentLOD = newLOD;
        ScheduleMeshHeightAdjustment(chunkMesh.mesh, newLOD, true);
        UpdateColliderBasedOnLOD();
    }
    /// <summary>
    /// Disable all chunk interactivity but still allow for changes.
    /// </summary>
    public void DisableChunk()
    {
        chunkRenderer.enabled = false;
        chunkCollider.enabled = false;
    }

    /// <summary>
    /// Re-enable the chunk interactivity.
    /// </summary>
    public void EnableChunk()
    {
        chunkRenderer.enabled = true;

        if(ColliderGenerated && CurrentLOD <= TerrainGenerationManager.Instance.HighestLODToHaveCollission)
            chunkCollider.enabled = true;
    }

    public void SetMaterial(Material material)
    {
        chunkRenderer.material = material;
    }

    public void RegenerateHeightMap()
    {
        //Request GPU to generate new heightmap
        NoiseGenerator.Instance.GenerateNoiseForChunk(gridCoordinates, buffer);
        //Schedule a readback from the GPU to the Heightmap Array
        ScheduleAsyncGPURequest(HandleAsyncGPURequest);
    }

    private void ScheduleAsyncGPURequest(Action<AsyncGPUReadbackRequest> onRequestCompleteCallback)
    {
        IEnumerator RequestAsync(AsyncGPUReadbackRequest request, Action<AsyncGPUReadbackRequest> callbackAction)
        {
            while (!request.done)
            {
                yield return null;
            }
            callbackAction(request);
        }

        AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(buffer);
        StartCoroutine(RequestAsync(request, onRequestCompleteCallback));
    }

    private void HandleAsyncGPURequest(AsyncGPUReadbackRequest readBackHeightMap)
    {
        //When the heightmap data has been retrieved, write it into the array.
        heightMap = readBackHeightMap.GetData<float>().ToArray();

        ScheduleMeshHeightAdjustment(chunkMesh.mesh, CurrentLOD, true);

        UpdateColliderBasedOnLOD();
    }

    void UpdateColliderBasedOnLOD()
    {
        if (CurrentLOD <= TerrainGenerationManager.Instance.HighestLODToHaveCollission)
        {
            if (!ColliderGenerated)
            {
                chunkCollider.enabled = true;
                ScheduleMeshHeightAdjustment(chunkCollider.sharedMesh, 4, false, RefreshChunkCollider);
                ColliderGenerated = true;
            }
        } else
        {
            chunkCollider.enabled = false;
        }
    }

    #region Mesh Adjustment
    private void ScheduleMeshHeightAdjustment(Mesh mesh, int newLOD, bool checkForLODChanges, Action callbackAction = null)
    {
        ChunkMeshData desiredLODData = TerrainGenerationManager.Instance.GetMeshDataForLOD(newLOD);

        AdjustMeshHeightJob job = new AdjustMeshHeightJob();
        job.verts = new NativeArray<Vector3>(desiredLODData.vertices, Allocator.TempJob);
        job.indices = new NativeArray<int>(desiredLODData.indices, Allocator.TempJob);
        job.heightMap = new NativeArray<float>(heightMap, Allocator.TempJob); ;
        job.verticesAlongEdge = TerrainGenerationManager.Instance.VerticesAlongEdge;
        job.CurrentLOD = newLOD;
        job.maxHeight = TerrainGenerationManager.Instance.MaxHeight;
        job.result = new NativeArray<Vector3>(desiredLODData.vertices.Length, Allocator.TempJob);
        job.normals = new NativeArray<Vector3>(desiredLODData.vertices.Length, Allocator.TempJob);

        JobHandle handle = job.Schedule();
        JobManager.Instance.ScheduleJobForCompletion(handle);
        StartCoroutine(HandleMeshAdjustmentJob(mesh, job, handle, newLOD, desiredLODData, checkForLODChanges, callbackAction));
    }

    private IEnumerator HandleMeshAdjustmentJob(Mesh meshToUpdate, AdjustMeshHeightJob job, JobHandle handle, int jobLOD, 
        ChunkMeshData desiredLODData, bool CheckForLODChange, Action callbackAction)
    {
        while(!handle.IsCompleted)
        {
          yield return null;
        }

        //Seems to sometimes still not be complete?
        handle.Complete();

        //In case a different level of LOD has been requested since this began
        //Only if you want to check for changes, collider doesn't need to change for LODS.
        if(!CheckForLODChange || (CurrentLOD == jobLOD))
        {
            SetMeshFromChunkMeshData(meshToUpdate, desiredLODData, job);
            EnableChunk();
        }

        job.verts.Dispose();
        job.indices.Dispose();
        job.heightMap.Dispose();
        job.result.Dispose();
        job.normals.Dispose();

        callbackAction?.Invoke();
    }

    private void RefreshChunkCollider()
    {
        chunkCollider.convex = false;
    }

    private void SetMeshFromChunkMeshData(Mesh mesh, ChunkMeshData initialData ,AdjustMeshHeightJob job) {
        mesh.Clear(false);
        mesh.SetVertices(job.result);
        mesh.SetIndices(initialData.indices, MeshTopology.Triangles, 0);
        mesh.SetNormals(job.normals);
        mesh.uv = initialData.uvs;
    }

    //Job to export array of Vector3s with modified heights.
    [BurstCompile]
    private struct AdjustMeshHeightJob : IJob
    {
        public NativeArray<Vector3> verts;
        public NativeArray<int> indices;
        public NativeArray<float> heightMap;
        public NativeArray<Vector3> result;
        public NativeArray<Vector3> normals;

        public int verticesAlongEdge;
        public float maxHeight;
        public int CurrentLOD;

        public void Execute()
        {
            float lodMul = Util.CalculateLODMultiplier(CurrentLOD);
            int verticesAlongEdgeForLOD = Mathf.RoundToInt(verticesAlongEdge * (lodMul));
            AdjustMeshToMatchHeightmap(verticesAlongEdgeForLOD);
            CalculateAdjustedMeshNormals(verticesAlongEdgeForLOD);
        }
        private void AdjustMeshToMatchHeightmap(int verticesAlongEdgeForLOD)
        {
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
                result[i] = new Vector3( verts[i].x, height * maxHeight, verts[i].z);
            }
        }

        private void CalculateAdjustedMeshNormals(int verticesAlongEdgeForLOD)
        {
            for (int z = 0; z < verticesAlongEdgeForLOD-1; z++)
            {
                for (int x = 0; x < verticesAlongEdgeForLOD-1; x++)
                {
                    int vert = z * (verticesAlongEdgeForLOD) + x;
                    int vert2 = vert + verticesAlongEdgeForLOD;
                    int vert3 = vert + 1;
                    int vert4 = vert + verticesAlongEdgeForLOD + 1;

                    Vector3 triNorm = CalculateTriangleNormal(result[vert], result[vert2], result[vert3]);
                    normals[vert] += triNorm;
                    normals[vert2] += triNorm;
                    normals[vert3] += triNorm;

                    triNorm = CalculateTriangleNormal(result[vert2], result[vert4], result[vert3]);
                    normals[vert2] += triNorm;
                    normals[vert4] += triNorm;
                    normals[vert3] += triNorm;
                }
            }
        }

        private Vector3 CalculateTriangleNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            return Vector3.Cross(b - a, c - a).normalized;
        }

        private void Get2DArrIndex(int vert, int dimensions, out int x, out int y)
        {
            x = vert % dimensions;
            y = vert / dimensions;
        }
    }
    #endregion

    private void OnApplicationQuit()
    {
        buffer.Dispose();
    }
}
