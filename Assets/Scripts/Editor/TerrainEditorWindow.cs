using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class TerrainEditorWindow : EditorWindow
{
    public static readonly string CHUNK_EXPORT_PATH = "Assets/Resources/ChunkData.asset";
    public static int ChunkSize;
    public static int VerticesAlongEdge;
    public static int NoOfLODS;

    private static Color bgColor = new Color(0.1f, 0.1f, 0.1f, 1);
    private static Color mainGrid = Color.green;
    private static Color subGrid = new Color(0, 1, 0, 0.2f);

    private int currentPreviewLOD;
    private List<Vector3> previewVerts;

    private Material material;

    [MenuItem("Terrain Generator/Chunk Generation Settings")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(TerrainEditorWindow), false, "Chunk Generation Settings");
    }

    private void OnEnable()
    {
        currentPreviewLOD = 1;
    }

    private void OnGUI()
    {
        //Limit to only even
        VerticesAlongEdge = Mathf.CeilToInt(VerticesAlongEdge / 2) * 2;
        //Stop generation of mesh without enough detail for the LODS
        VerticesAlongEdge = Mathf.Max(VerticesAlongEdge, (int)Mathf.Pow(2, NoOfLODS));
        GUILayout.Label("Chunk Generation Settings", EditorStyles.boldLabel);
        ChunkSize = EditorGUILayout.IntSlider("Chunk Size", ChunkSize, 2, 1024);
        VerticesAlongEdge = EditorGUILayout.IntSlider("Vertices along Edge" ,VerticesAlongEdge, 2, 64);
        NoOfLODS = EditorGUILayout.IntSlider("Number of LODs" ,NoOfLODS, 1, 5);
        if(GUILayout.Button("Generate Chunk Meshes"))
        {
            GenerateAllChunkLODs();
        }
        GUILayout.Space(20);

        //Debug Chunk LOD Drawing
        material = new Material(Shader.Find("Hidden/Internal-Colored"));
        GUILayout.Label("LOD Mesh Preview", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();

        string[] lodOptions = new string[NoOfLODS];

        for(int i = 1; i <= NoOfLODS; i++)
        {
            lodOptions[i-1] = i.ToString();
        }

        var previewLODindex = Mathf.Min(NoOfLODS, currentPreviewLOD) - 1;
        previewLODindex = GUILayout.SelectionGrid(previewLODindex, lodOptions, NoOfLODS);
        currentPreviewLOD = previewLODindex + 1;
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        Rect layoutRectangle = GUILayoutUtility.GetRect(10, 1, 100, 100);

        if (Event.current.type == EventType.Repaint)
        {
            GUI.BeginClip(layoutRectangle);
            GL.PushMatrix();
            GL.Clear(true, false, Color.black);
            material.SetPass(0);

            // Draw Background
            GL.Begin(GL.QUADS);
            GL.Color(bgColor);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(layoutRectangle.width, 0, 0);
            GL.Vertex3(layoutRectangle.width, layoutRectangle.width, 0);
            GL.Vertex3(0, layoutRectangle.width, 0);
            GL.End();

            //Draw Chunk Preview
            GL.Begin(GL.LINES);
            DrawMeshLODPreview(layoutRectangle.width, layoutRectangle.width/10f);
            GL.End();

            GL.PopMatrix();
            GUI.EndClip();
        }
    }

    private void DrawMeshLODPreview(float previewSize, float padding)
    {
        if(previewVerts == null)
        {
            previewVerts = new List<Vector3>();
        }

        previewVerts.Clear();

        float lodMul = 1 / Mathf.Pow(2, currentPreviewLOD-1);
        int VerticesAlongEdgeForLOD = Mathf.RoundToInt(VerticesAlongEdge * lodMul);

        for (int z = 0; z < VerticesAlongEdgeForLOD; z++)
        {
            for (int x = 0; x < VerticesAlongEdgeForLOD; x++)
            {
                previewVerts.Add(new Vector3(
                    (previewSize - padding)/(VerticesAlongEdgeForLOD-1) * x + padding/2, 
                    (previewSize - padding)/ (VerticesAlongEdgeForLOD-1) * z + padding/2, 
                    0));
            }
        }

        for (int z = 0; z < VerticesAlongEdgeForLOD - 1; z++)
        {
            for (int x = 0; x < VerticesAlongEdgeForLOD - 1; x++)
            {
                int startingVert = (z * VerticesAlongEdgeForLOD) + x;
                GL.Color(mainGrid);
                EditorDrawLine(previewVerts[startingVert], previewVerts[startingVert + 1]);
                EditorDrawLine(previewVerts[startingVert + 1], previewVerts[startingVert + VerticesAlongEdgeForLOD + 1]);
                EditorDrawLine(previewVerts[startingVert + VerticesAlongEdgeForLOD + 1], previewVerts[startingVert + VerticesAlongEdgeForLOD]);
                EditorDrawLine(previewVerts[startingVert + VerticesAlongEdgeForLOD], previewVerts[startingVert]);
                GL.Color(subGrid);
                EditorDrawLine(previewVerts[startingVert], previewVerts[startingVert + VerticesAlongEdgeForLOD + 1]);
            }
        }
    }

    private void EditorDrawLine(Vector3 point1, Vector3 point2)
    {
        //Assumes we are already in GL Lines primative mode!
        GL.Vertex3(point1.x, point1.y,0);
        GL.Vertex3(point2.x, point2.y,0);
    }

    static void GenerateAllChunkLODs()
    {
        ChunkMeshes generatedChunkMeshes = ScriptableObject.CreateInstance<ChunkMeshes>();
        generatedChunkMeshes.Init(ChunkSize, VerticesAlongEdge, NoOfLODS);
        for (int i = 0; i < NoOfLODS; i++)
        {
            ChunkGenerator.GenerateChunkMeshForLOD(VerticesAlongEdge, ChunkSize, i, 
                out List<Vector3> vertices, out int[] indices, out List<Vector2> uvs);

            generatedChunkMeshes.LODs[i].Indices = indices;
            generatedChunkMeshes.LODs[i].UVs = uvs;
            generatedChunkMeshes.LODs[i].Vertices = vertices;
        }
        AssetDatabase.CreateAsset(generatedChunkMeshes, CHUNK_EXPORT_PATH);

        //Don't use singleton, because probably in editor when this is run
        TerrainGenerationManager manager = GameObject.Find("Terrain Manager").GetComponent<TerrainGenerationManager>();
        manager.ChunkLODData = generatedChunkMeshes;
        manager.ChunkSize = ChunkSize;
        manager.VerticesAlongEdge = VerticesAlongEdge;
        manager.NoOfLODs = NoOfLODS;
    }
}
