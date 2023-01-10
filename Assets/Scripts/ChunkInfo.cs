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

    public Texture2D reader;

    private float[] heightMap;

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

    float DecodeFloatRGBA(Color enc)
    {
        Vector4 kDecodeDot = new Vector4(1.0f, 1 / 255.0f, 1 / 65025.0f, 1 / 160581375.0f);
        return Vector4.Dot(enc, kDecodeDot);
    }

    private void ReadBackHeightMapDataToTexture2D(AsyncGPUReadbackRequest readBackHeightMap)
    {
        //When the heightmap data has been retrieved, write it into a texture.
        reader.LoadRawTextureData(readBackHeightMap.GetData<uint>());
        reader.Apply();

        UnpackColorsToFloats();

        //NOW update vertex heights
        AdjustMeshToMatchHeightmap();
    }

    private void UnpackColorsToFloats()
    {
        var colors = reader.GetPixels();

        for (int i = 0; i < colors.Length; i++) {
            heightMap[i] = DecodeFloatRGBA(colors[i]);
        }
    }

    private void AdjustMeshToMatchHeightmap()
    {
        var verts = mesh.mesh.vertices;
        //Calculate chunk dimensions at current LOD
        float lodMul = 1 / (float)Mathf.Pow(2, CurrentLOD - 1);
        int verticesAlongEdge = TerrainGenerationManager.Instance.VerticesAlongEdge;
        int verticesAlongEdgeForLOD = Mathf.RoundToInt(verticesAlongEdge * (lodMul));

        //Debug draw noise texture
        this.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", reader);

        //Update all vertex heights to match heightmap
        for (int i = 0; i < verts.Length; i++)
        {
            //Get the pixel at the place the vertex would be at
            Get2DArrIndex(i, verticesAlongEdgeForLOD, out int x, out int y);
            float xPercentage = x / (float)(verticesAlongEdgeForLOD - 1);
            float yPercentage = y / (float)(verticesAlongEdgeForLOD - 1);

            int index = (Mathf.RoundToInt(yPercentage * (verticesAlongEdge-1)) * verticesAlongEdge)
                + Mathf.RoundToInt(xPercentage * (verticesAlongEdge-1));
            float height = heightMap[index];
            verts[i].y = height * 100.0f;
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
