using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ChunkInfo : MonoBehaviour
{
    public int CurrentLOD { get; private set; }

    public RenderTexture maxQualityNoiseMap;

    public Vector2Int gridCoordinates;

    private MeshFilter mesh;

    private Texture2D reader;

    void Awake()
    {
        maxQualityNoiseMap = new RenderTexture(TerrainGenerationManager.Instance.VerticesAlongEdge, 
            TerrainGenerationManager.Instance.VerticesAlongEdge, 24, 
            RenderTextureFormat.R16);
        maxQualityNoiseMap.filterMode = FilterMode.Bilinear;
        maxQualityNoiseMap.useMipMap = false;
        maxQualityNoiseMap.enableRandomWrite = true;
        maxQualityNoiseMap.Create();

        reader = new Texture2D(maxQualityNoiseMap.width, maxQualityNoiseMap.height, TextureFormat.R16, false);
        reader.alphaIsTransparency = false;
        reader.wrapMode = TextureWrapMode.Clamp;
        reader.filterMode = FilterMode.Bilinear;

        mesh = this.GetComponent<MeshFilter>();
        gridCoordinates = new Vector2Int(int.MaxValue, int.MaxValue);
    }

    public void ReuseChunk(Vector3 newPos, Vector2Int newGridPos)
    {
        this.transform.position = newPos;
        gridCoordinates = newGridPos;
    }

    public void UpdateMeshLOD(Mesh newMesh, int newLOD)
    {
        CurrentLOD = newLOD;
        mesh.mesh = newMesh;
        AdjustMeshToMatchHeightmap();
    }

    public void RegenerateHeightMap()
    {
        //Request GPU to generate new heightmap
        NoiseGenerator.Instance.GenerateNoiseForChunk(gridCoordinates, maxQualityNoiseMap);
        //Request the GPU to retrieve the heightmap data from the GPU, and set callback
        GetHeightMapDataFromRenderTex(ReadBackHeightMapDataToTexture2D);
    }

    private void ReadBackHeightMapDataToTexture2D(AsyncGPUReadbackRequest readBackHeightMap)
    {
        //When the heightmap data has been retrieved, write it into a texture.
        reader.LoadRawTextureData(readBackHeightMap.GetData<ushort>());
        reader.Apply();

        //NOW update vertex heights
        AdjustMeshToMatchHeightmap();
    }

    private void AdjustMeshToMatchHeightmap()
    {
        var verts = mesh.mesh.vertices;
        //Calculate chunk dimensions at current LOD
        float lodMul = 1 / (float)Mathf.Pow(2, CurrentLOD - 1);
        int VerticesAlongEdgeForLOD = Mathf.RoundToInt(TerrainGenerationManager.Instance.VerticesAlongEdge * (lodMul));

        //Debug draw noise texture
        this.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", reader);

        //Update all vertex heights to match heightmap
        for (int i = 0; i < verts.Length; i++)
        {
            //Get the pixel at the place the vertex would be at
            Get2DArrIndex(i, VerticesAlongEdgeForLOD, out int x, out int y);
            //Calculate corresponding pixel on heightmap
            float xPerc = x / (float)VerticesAlongEdgeForLOD;
            float yPerc = y / (float)VerticesAlongEdgeForLOD;
            float height = reader.GetPixel(Mathf.RoundToInt(xPerc * reader.width),
                Mathf.RoundToInt(yPerc * reader.height)).r;
            //Set the height
            verts[i].y = height * height * 100;
        }

        mesh.mesh.vertices = verts;
        mesh.mesh.RecalculateNormals();
        mesh.mesh.RecalculateBounds();
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

    private void Get2DArrIndex(int vert, int dimensions, out int x, out int y)
    {
        x = vert % dimensions;
        y = vert / dimensions;
    }
}
