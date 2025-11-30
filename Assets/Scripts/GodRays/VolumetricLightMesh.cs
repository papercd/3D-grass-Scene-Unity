using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VolumetricLightMesh : MonoBehaviour
{
    [Header("References")]
    public Light mainLight;

    [Header("Generation Settings")]
    public int sliceCount = 15;
    public float width = 20f;
    public float height = 10f;
    public float sliceSpacing = 1f;

    [Header("Runtime")]
    public bool updateEveryFrame = true;

    [Header("Gizmos")]
    public bool showGizmos = true;

    private MeshFilter meshFilter;
    private Mesh mesh;
    private Camera mainCamera;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        mainCamera = Camera.main;

        if (mainLight == null)
        {
            mainLight = RenderSettings.sun;
        }

        GenerateMesh();
    }

    void Update()
    {
        if (updateEveryFrame && mainLight != null)
        {
            GenerateMesh();
        }
    }

    void GenerateMesh()
    {
        if (mainLight == null || sliceCount < 1)
            return;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Get light direction
        Vector3 lightDir = -mainLight.transform.forward;

        // We want planes PARALLEL to light direction
        // One edge along light direction, width perpendicular to it
        Vector3 widthDir = Vector3.Cross(lightDir, Vector3.up).normalized;
        if (widthDir.magnitude < 0.001f) // Handle vertical light
        {
            widthDir = Vector3.Cross(lightDir, Vector3.forward).normalized;
        }

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        for (int i = 0; i < sliceCount; i++)
        {
            // Stack the planes vertically in Y direction
            float yOffset = i * sliceSpacing;
            Vector3 sliceCenter = transform.position + Vector3.up * yOffset;

            // Create vertices for a plane PARALLEL to light
            // The plane extends along the light direction (height) and across (width)
            Vector3 v1 = sliceCenter - widthDir * halfWidth - lightDir * halfHeight; // Bottom-left
            Vector3 v2 = sliceCenter + widthDir * halfWidth - lightDir * halfHeight; // Bottom-right  
            Vector3 v3 = sliceCenter - widthDir * halfWidth + lightDir * halfHeight; // Top-left
            Vector3 v4 = sliceCenter + widthDir * halfWidth + lightDir * halfHeight; // Top-right

            // Transform to local space
            v1 = transform.InverseTransformPoint(v1);
            v2 = transform.InverseTransformPoint(v2);
            v3 = transform.InverseTransformPoint(v3);
            v4 = transform.InverseTransformPoint(v4);

            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            vertices.Add(v4);

            // Add UVs
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));

            // Add triangles
            int baseIndex = i * 4;

            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 1);

            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 3);
        }

        // Create or update mesh
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "VolumetricSlices";
        }
        else
        {
            mesh.Clear();
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        if (mainLight == null)
            mainLight = RenderSettings.sun;

        if (mainLight == null)
            return;

        // Get light direction
        Vector3 lightDir = -mainLight.transform.forward;

        Vector3 widthDir = Vector3.Cross(lightDir, Vector3.up).normalized;
        if (widthDir.magnitude < 0.001f)
        {
            widthDir = Vector3.Cross(lightDir, Vector3.forward).normalized;
        }

        // Draw light direction
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, lightDir * 10f);

        // Draw Y-axis to show stacking direction
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, Vector3.up * (sliceCount * sliceSpacing));

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        for (int i = 0; i < sliceCount; i++)
        {
            // Stack vertically in Y
            float yOffset = i * sliceSpacing;
            Vector3 sliceCenter = transform.position + Vector3.up * yOffset;

            // Draw plane outline - planes are PARALLEL to light
            Vector3 v1 = sliceCenter - widthDir * halfWidth - lightDir * halfHeight;
            Vector3 v2 = sliceCenter + widthDir * halfWidth - lightDir * halfHeight;
            Vector3 v3 = sliceCenter - widthDir * halfWidth + lightDir * halfHeight;
            Vector3 v4 = sliceCenter + widthDir * halfWidth + lightDir * halfHeight;

            // Color code the slices
            if (i == 0)
                Gizmos.color = new Color(1, 0, 0, 0.5f); // Red for bottom
            else if (i == sliceCount - 1)
                Gizmos.color = new Color(0, 0, 1, 0.5f); // Blue for top
            else
                Gizmos.color = new Color(0, 1, 1, 0.3f); // Cyan for middle

            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v4);
            Gizmos.DrawLine(v4, v3);
            Gizmos.DrawLine(v3, v1);

            // Draw light rays along the plane to show they're parallel
            if (i % 3 == 0) // Every third plane
            {
                Gizmos.color = new Color(1, 1, 0, 0.3f);
                Gizmos.DrawLine(v1, v3); // Light travels along this edge
                Gizmos.DrawLine(v2, v4); // And this edge
            }

            // Draw center point
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(sliceCenter, 0.25f);
        }
    }
}