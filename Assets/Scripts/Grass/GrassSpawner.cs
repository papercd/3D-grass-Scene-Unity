using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class GrassSpawner : MonoBehaviour
{
    [Header("Rendering")]
    public Mesh grassMesh;
    public Material grassMaterial;
    [Tooltip("Total number of grass blades to spawn")]
    public int grassCount = 2000;

    [Header("Appearance")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    public bool randomRotation = true;

    [Header("Shadows")]
    [Tooltip("Should grass cast shadows?")]
    public bool castShadows = false;  // NEW: Toggle for shadow casting

    // GPU buffers (persistent)
    private ComputeBuffer argsBuffer;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer normalBuffer;
    private ComputeBuffer tileIndexBuffer;

    private Bounds renderBounds;

    void Awake()
    {
        grassMesh = GrassMeshGenerator.CreateGrassQuad();
    }

    void Start()
    {
        if (grassMesh == null || grassMaterial == null)
        {
            Debug.LogError("GrassSpawner: assign grassMesh and grassMaterial in inspector.");
            enabled = false;
            return;
        }

        Terrain terrain = GetComponent<Terrain>();
        TerrainData tData = terrain.terrainData;
        Vector3 terrainOrigin = terrain.GetPosition();
        Vector3 terrainSize = tData.size;

        int n = Mathf.Max(1, grassCount);

        // Prepare data arrays
        Vector4[] positions = new Vector4[n];
        Vector4[] normals = new Vector4[n];
        Vector4[] tileIndices = new Vector4[n];

        Vector3 minGrass = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 maxGrass = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        for (int i = 0; i < n; i++)
        {
            float worldX = Random.Range(terrainOrigin.x, terrainOrigin.x + terrainSize.x);
            float worldZ = Random.Range(terrainOrigin.z, terrainOrigin.z + terrainSize.z);
            float worldY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + terrainOrigin.y;

            float normX = (worldX - terrainOrigin.x) / terrainSize.x;
            float normZ = (worldZ - terrainOrigin.z) / terrainSize.z;
            Vector3 normal = tData.GetInterpolatedNormal(normX, normZ);

            Vector3 pos = new Vector3(worldX, worldY, worldZ);

            positions[i] = new Vector4(pos.x, pos.y, pos.z, 0f);
            normals[i] = new Vector4(normal.x, normal.y, normal.z, 0f);
            tileIndices[i] = new Vector4(Random.Range(0, 9), 0f, 0f, 0f);

            minGrass = Vector3.Min(minGrass, pos);
            maxGrass = Vector3.Max(maxGrass, pos);
        }

        // Create ComputeBuffers and upload data ONCE
        positionBuffer = new ComputeBuffer(n, sizeof(float) * 4);
        normalBuffer = new ComputeBuffer(n, sizeof(float) * 4);
        tileIndexBuffer = new ComputeBuffer(n, sizeof(float) * 4);

        positionBuffer.SetData(positions);
        normalBuffer.SetData(normals);
        tileIndexBuffer.SetData(tileIndices);

        // Bind buffers to material (stays bound)
        grassMaterial.SetBuffer("_BasePos", positionBuffer);
        grassMaterial.SetBuffer("_BaseNormal", normalBuffer);
        grassMaterial.SetBuffer("_TileIndex", tileIndexBuffer);

        // Setup indirect args
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = (uint)grassMesh.GetIndexCount(0);
        args[1] = (uint)n; // instance count
        args[2] = (uint)grassMesh.GetIndexStart(0);
        args[3] = (uint)grassMesh.GetBaseVertex(0);

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        // Calculate bounds (for culling)
        Vector3 center = (minGrass + maxGrass) * 0.5f;
        Vector3 size = maxGrass - minGrass;
        renderBounds = new Bounds(center, size);

        Debug.Log($"Grass system initialized with {n} instances. Bounds: {renderBounds}");
    }

    void Update()
    {
        if (argsBuffer == null) return;

        // Determine shadow casting mode based on toggle
        var shadowMode = castShadows
            ? UnityEngine.Rendering.ShadowCastingMode.On
            : UnityEngine.Rendering.ShadowCastingMode.Off;

        // Just issue the draw call - no data upload!
        Graphics.DrawMeshInstancedIndirect(
            grassMesh,
            0,
            grassMaterial,
            renderBounds,
            argsBuffer,
            0,
            null,
            shadowMode,  // Now controlled by the castShadows toggle
            true,
            gameObject.layer
        );
    }

    void OnDestroy()
    {
        // Clean up GPU memory
        positionBuffer?.Release();
        normalBuffer?.Release();
        tileIndexBuffer?.Release();
        argsBuffer?.Release();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(renderBounds.center, renderBounds.size);
    }
}