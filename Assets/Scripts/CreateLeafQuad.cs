using UnityEngine;

public class CreateLeafQuad : MonoBehaviour
{
    [ContextMenu("Create Leaf Quad")]
    void CreateQuad()
    {
        Mesh mesh = new Mesh();
        mesh.name = "LeafQuad";

        // Vertices - centered at origin, 1x1 size
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0), // Bottom-left
            new Vector3(0.5f, -0.5f, 0),  // Bottom-right
            new Vector3(-0.5f, 0.5f, 0),  // Top-left
            new Vector3(0.5f, 0.5f, 0)    // Top-right
        };

        // UVs
        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0, 0), // Bottom-left
            new Vector2(1, 0), // Bottom-right
            new Vector2(0, 1), // Top-left
            new Vector2(1, 1)  // Top-right
        };

        // Triangles
        int[] triangles = new int[]
        {
            0, 2, 1, // First triangle
            2, 3, 1  // Second triangle
        };

        // Normals (all facing forward)
        Vector3[] normals = new Vector3[]
        {
            Vector3.forward,
            Vector3.forward,
            Vector3.forward,
            Vector3.forward
        };

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.normals = normals;

        // Save as asset
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(mesh, "Assets/LeafQuad.asset");
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log("Leaf quad created at Assets/LeafQuad.asset");
#endif
    }
}