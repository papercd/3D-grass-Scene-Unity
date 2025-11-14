using UnityEngine;

public static class GrassMeshGenerator
{
    public static Mesh CreateGrassQuad()
    {
        Mesh mesh = new Mesh();
        mesh.name = "GrassQuad_BottomPivot";

        // Vertices — bottom at y=0, width=1, height=1
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0f, 0f),  // bottom left
            new Vector3( 0.5f, 0f, 0f),  // bottom right
            new Vector3(-0.5f, 1f, 0f),  // top left
            new Vector3( 0.5f, 1f, 0f)   // top right
        };

        // Triangles (two facing +Z)
        mesh.triangles = new int[]
        {
            0, 2, 1,
            2, 3, 1
        };

        // UVs (standard 0–1)
        mesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };

        // Normals (face forward along +Z)
        mesh.normals = new Vector3[]
        {
            Vector3.forward,
            Vector3.forward,
            Vector3.forward,
            Vector3.forward
        };

        mesh.RecalculateBounds();
        return mesh;
    }
}
