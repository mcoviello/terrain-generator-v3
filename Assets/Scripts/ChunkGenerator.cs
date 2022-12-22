using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkGenerator : MonoBehaviour
{
    public static void GenerateChunkMeshForLOD(int VerticesAlongEdge, int ChunkSize, int LOD,
        out List<Vector3> outVertices, out int[] outIndices, out List<Vector2> outUvs)
    {
        var vertexPositions = new List<Vector3>();
        var vertexUVs = new List<Vector2>();
        float lodMul = 1/Mathf.Pow(2, LOD-1);
        var VerticesAlongEdgeForLOD = Mathf.RoundToInt(VerticesAlongEdge * lodMul);

        float DistToMove = (float)ChunkSize / (float)(VerticesAlongEdgeForLOD - 1);
        for (int z = 0; z < VerticesAlongEdgeForLOD; z++)
        {
            for (int x = 0; x < VerticesAlongEdgeForLOD; x++)
            {
                vertexPositions.Add(new Vector3(x * DistToMove, 0, z * DistToMove));
                vertexUVs.Add(new Vector2(x, z));
            }
        }

        var indices = new int[VerticesAlongEdgeForLOD * VerticesAlongEdgeForLOD * 6];

        for (int z = 0; z < VerticesAlongEdgeForLOD - 1; z++)
        {
            for (int x = 0; x < VerticesAlongEdgeForLOD - 1; x++)
            {
                int startingVert = (z * VerticesAlongEdgeForLOD) + x;
                int startingInd = startingVert * 6;
                int[] verts = { startingVert, startingVert + VerticesAlongEdgeForLOD, startingVert + 1, startingVert + VerticesAlongEdgeForLOD + 1 };

                indices[startingInd]       = verts[0];
                indices[startingInd + 1]   = verts[1];
                indices[startingInd + 2]   = verts[2];
                indices[startingInd + 3]   = verts[1];
                indices[startingInd + 4]   = verts[3];
                indices[startingInd + 5]   = verts[2];
            }
        }

        Mesh genMesh = new Mesh();
        outVertices = vertexPositions;
        outUvs = vertexUVs;
        outIndices = indices;
    }
}
