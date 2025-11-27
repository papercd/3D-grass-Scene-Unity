using System.Collections.Generic;
using UnityEngine;

public class LeafInstanceRenderer : MonoBehaviour
{
    [Header("Leaf Settings")]
    public Mesh leafQuad;
    public Material leafMaterial;
    public int leafCount = 5000;
    public float leafSize = 0.3f;

    private ComputeBuffer posBuffer;
    private ComputeBuffer normalBuffer;
    private ComputeBuffer randomBuffer;

    private Bounds renderBounds;
    private int actualLeafCount;
    private Material materialInstance; // NEW: Store the instance

    void Start()
    {
        Mesh canopyMesh = GetComponent<MeshFilter>().sharedMesh;

        if (!ValidateComponents(canopyMesh)) return;

        // Create material instance
        materialInstance = new Material(leafMaterial);
        Debug.Log("Material instance created: " + materialInstance.name);

        GenerateLeaves(canopyMesh);
    }

    bool ValidateComponents(Mesh canopyMesh)
    {
        if (canopyMesh == null)
        {
            Debug.LogError("No canopy mesh found!");
            return false;
        }
        if (leafQuad == null)
        {
            Debug.LogError("No leaf quad assigned!");
            return false;
        }
        if (leafMaterial == null)
        {
            Debug.LogError("No leaf material assigned!");
            return false;
        }
        return true;
    }

    void GenerateLeaves(Mesh canopyMesh)
    {
        List<Vector3> positions = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();

        SampleMeshSurface(canopyMesh, transform, leafCount, positions, normals);

        actualLeafCount = positions.Count;
        Debug.Log($"Generated {actualLeafCount} leaves");

        if (actualLeafCount == 0)
        {
            Debug.LogError("No leaves generated!");
            return;
        }

        // Create buffers
        posBuffer = new ComputeBuffer(actualLeafCount, sizeof(float) * 4);
        normalBuffer = new ComputeBuffer(actualLeafCount, sizeof(float) * 4);
        randomBuffer = new ComputeBuffer(actualLeafCount, sizeof(float) * 4);

        Vector4[] posData = new Vector4[actualLeafCount];
        Vector4[] normData = new Vector4[actualLeafCount];
        Vector4[] randomData = new Vector4[actualLeafCount];

        for (int i = 0; i < actualLeafCount; i++)
        {
            posData[i] = new Vector4(positions[i].x, positions[i].y, positions[i].z, 0);
            normData[i] = new Vector4(normals[i].x, normals[i].y, normals[i].z, 0);

            randomData[i] = new Vector4(
                Random.value,
                Random.value,
                Random.value,
                Random.value
            );
        }

        posBuffer.SetData(posData);
        normalBuffer.SetData(normData);
        randomBuffer.SetData(randomData);

        // Set buffers on material INSTANCE
        materialInstance.SetBuffer("_BasePos", posBuffer);
        materialInstance.SetBuffer("_BaseNormal", normalBuffer);
        materialInstance.SetBuffer("_RandomValues", randomBuffer);

        Debug.Log("Buffers set on material instance");

        CalculateBounds(positions);
    }

    void CalculateBounds(List<Vector3> positions)
    {
        Vector3 min = positions[0];
        Vector3 max = positions[0];

        foreach (var p in positions)
        {
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        renderBounds = new Bounds((min + max) * 0.5f, max - min);
        renderBounds.Expand(leafSize * 3f);

        Debug.Log($"Bounds: center={renderBounds.center}, size={renderBounds.size}");
    }

    void Update()
    {
        if (posBuffer != null && leafQuad != null && materialInstance != null)
        {
            materialInstance.SetFloat("_LeafSize", leafSize);

            Graphics.DrawMeshInstancedProcedural(
                leafQuad, 0, materialInstance, renderBounds, actualLeafCount);
        }
    }

    void OnDestroy()
    {
        posBuffer?.Release();
        normalBuffer?.Release();
        randomBuffer?.Release();

        // Destroy material instance
        if (materialInstance != null)
        {
            if (Application.isPlaying)
                Destroy(materialInstance);
            else
                DestroyImmediate(materialInstance);
        }
    }

    static void SampleMeshSurface(Mesh mesh, Transform transform, int count,
        List<Vector3> outPositions, List<Vector3> outNormals)
    {
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;

        int triCount = triangles.Length / 3;
        float[] cumulativeAreas = new float[triCount];

        float totalArea = 0f;
        for (int i = 0; i < triCount; i++)
        {
            int i0 = triangles[i * 3];
            int i1 = triangles[i * 3 + 1];
            int i2 = triangles[i * 3 + 2];

            float area = Vector3.Cross(
                vertices[i1] - vertices[i0],
                vertices[i2] - vertices[i0]).magnitude * 0.5f;

            totalArea += area;
            cumulativeAreas[i] = totalArea;
        }

        if (totalArea <= 0f)
        {
            Debug.LogError("Mesh has zero area!");
            return;
        }

        for (int k = 0; k < count; k++)
        {
            float r = Random.value * totalArea;
            int triIndex = System.Array.BinarySearch(cumulativeAreas, r);
            if (triIndex < 0) triIndex = ~triIndex;

            int t0 = triangles[triIndex * 3];
            int t1 = triangles[triIndex * 3 + 1];
            int t2 = triangles[triIndex * 3 + 2];

            Vector3 a = vertices[t0];
            Vector3 b = vertices[t1];
            Vector3 c = vertices[t2];

            float u = Random.value;
            float v = Random.value;
            if (u + v > 1f)
            {
                u = 1f - u;
                v = 1f - v;
            }

            Vector3 localPos = a + (b - a) * u + (c - a) * v;
            Vector3 localNormal = normals[t0] * (1 - u - v) + normals[t1] * u + normals[t2] * v;

            outPositions.Add(transform.TransformPoint(localPos));
            outNormals.Add(transform.TransformDirection(localNormal).normalized);
        }
    }
}