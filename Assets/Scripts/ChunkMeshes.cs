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

    public Mesh[] GetLODMeshes()
    {
        Mesh[] meshes = new Mesh[NoOfLODs];
        for (int i = 0; i < NoOfLODs; i++)
        {
            Mesh genMesh = new Mesh();
            genMesh.SetVertices(LODs[i].Vertices);
            genMesh.SetUVs(0, LODs[i].UVs);
            genMesh.SetIndices(LODs[i].Indices, MeshTopology.Triangles, 0);
            meshes[i] = genMesh;
        }

        return meshes;
    }
}

[System.Serializable]
public class MeshInfo
{
    public int[] Indices;
    public List<Vector2> UVs;
    public List<Vector3> Vertices;
}
