using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkInfo : MonoBehaviour
{
    public int CurrentLOD { get; private set; }

    public RenderTexture maxQualityNoiseMap;

    public Vector2Int gridCoordinates;

    private MeshFilter mesh;

    void Awake()
    {
        maxQualityNoiseMap = new RenderTexture(TerrainGenerationManager.Instance.VerticesAlongEdge, TerrainGenerationManager.Instance.VerticesAlongEdge, 24);
        maxQualityNoiseMap.enableRandomWrite = true;
        maxQualityNoiseMap.Create();
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
        NoiseGenerator.Instance.GenerateNoiseForChunk(gridCoordinates, maxQualityNoiseMap);
        this.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", maxQualityNoiseMap);
        AdjustMeshToMatchHeightmap();
    }

    private void AdjustMeshToMatchHeightmap()
    {
        var verts = mesh.mesh.vertices;
        Rect rectReadPicture = new Rect(0, 0, maxQualityNoiseMap.width, maxQualityNoiseMap.height);
        Texture2D tempTex = new Texture2D(maxQualityNoiseMap.width, maxQualityNoiseMap.height);
        tempTex.ReadPixels(rectReadPicture, 0, 0);
        float lodMul = 1 / Mathf.Pow(2, CurrentLOD - 1);
        int VerticesAlongEdgeForLOD = Mathf.RoundToInt(TerrainGenerationManager.Instance.VerticesAlongEdge * lodMul);
        int distToMove = verts.Length / VerticesAlongEdgeForLOD;
        for (int i = 0; i < verts.Length; i += distToMove)
        {
            float height = Random.Range(0, 10);
            //Get the pixel at the place the vertex would be at
            //tempTex.GetPixel();
            //Set the height

            verts[i].y = height;
        }

        mesh.mesh.vertices = verts;
    }
}
