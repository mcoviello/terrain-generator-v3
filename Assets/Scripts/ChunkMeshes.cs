using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChunkMeshes : ScriptableObject
{
    public int ChunkSize;
    public int VerticesAlongEdge;
    public int NoOfLODs;
    public MeshInfo[] LODs;

    public void Init(int chunkSize, int verticesAlongEdge, int noOfLODs)
    {
        ChunkSize = chunkSize;
        VerticesAlongEdge = verticesAlongEdge;
        NoOfLODs = noOfLODs;

        LODs = new MeshInfo[NoOfLODs];
        for(int i = 0; i < NoOfLODs; i++)
        {
            LODs[i] = new MeshInfo();
        }
    }

    public ChunkMeshData[] GetLODMeshes()
    {
        ChunkMeshData[] data = new ChunkMeshData[NoOfLODs];
        for (int i = 0; i < NoOfLODs; i++)
        {
            ChunkMeshData genMesh = new ChunkMeshData();
            genMesh.vertices = LODs[i].Vertices.ToArray(); ;
            genMesh.uvs = LODs[i].UVs.ToArray();
            genMesh.indices = LODs[i].Indices;
            //Makes heightmap vertex updates faster in the chunk info class
            data[i] = genMesh;
        }

        return data;
    }
}

[System.Serializable]
public class MeshInfo
{
    public int[] Indices;
    public List<Vector2> UVs;
    public List<Vector3> Vertices;
}
