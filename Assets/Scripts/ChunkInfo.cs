using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkInfo : MonoBehaviour
{
    public int CurrentLOD { get; private set; }

    public Texture2D maxQualityNoiseMap;

    public Vector2Int gridCoordinates;

    private MeshFilter mesh;

    void Awake()
    {
        maxQualityNoiseMap = new Texture2D(TerrainGenerationManager.Instance.VerticesAlongEdge, TerrainGenerationManager.Instance.VerticesAlongEdge);
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
    }
}
