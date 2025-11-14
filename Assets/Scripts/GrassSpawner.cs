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

    // internal
    private Matrix4x4[] matrices;
    private Vector3[] basePositions;
    private Vector3[] baseNormals;

    private Vector4[] tileIndex4;


    private Vector3 debugTerrainOrigin;
    private Vector3 debugTerrainSize;
    private Vector3 debugGrassMin;
    private Vector3 debugGrassMax;


    // Instancing property IDs
    static readonly int k_BasePosId = Shader.PropertyToID("_BasePos");
    static readonly int k_BaseNormalId = Shader.PropertyToID("_BaseNormal");

    static readonly int k_TileIndexId = Shader.PropertyToID("_TileIndex");


    // Unity limit: DrawMeshInstanced max instances per call is 1023

    const int MAX_INSTANCES_PER_BATCH = 1023;


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


        /*
        Terrain terrain = GetComponent<Terrain>();
        TerrainData tData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = tData.size;

        Vector3 minGrass = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 maxGrass = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        for (int i = 0; i < 500; i++)
        {
            float normX = Random.value;
            float normZ = Random.value;

            float height = tData.GetInterpolatedHeight(normX, normZ);

            float worldX = terrainPos.x + normX * terrainSize.x;
            float worldZ = terrainPos.z + normZ * terrainSize.z;
            float worldY = terrainPos.y + height;

            Vector3 pos = new Vector3(worldX, worldY, worldZ);

            // Track min/max
            minGrass = Vector3.Min(minGrass, pos);
            maxGrass = Vector3.Max(maxGrass, pos);
        }

        // Terrain world bounds
        Vector3 terrainMin = terrainPos;
        Vector3 terrainMax = terrainPos + terrainSize;

        // Log everything
        Debug.Log($"Terrain origin: {terrainPos} size: {terrainSize}");
        Debug.Log($"Terrain world bounds: min {terrainMin}  max {terrainMax}");
        Debug.Log($"Grass generated bounds: min {minGrass}  max {maxGrass}");
        Debug.Log($"Grass span X size: {maxGrass.x - minGrass.x}, Z size: {maxGrass.z - minGrass.z}");
        */


        Terrain terrain = GetComponent<Terrain>();
        TerrainData tData = terrain.terrainData;
        Vector3 terrainOrigin = terrain.GetPosition();
        Vector3 terrainSize = tData.size;


        Vector3 minGrass = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 maxGrass = new Vector3(float.MinValue, float.MinValue, float.MinValue);


        float terrainWidth = tData.size.x;
        float terrainLength = tData.size.z;
        float terrainHeight = tData.size.y;

        int n = Mathf.Max(1, grassCount);
        matrices = new Matrix4x4[n];
        basePositions = new Vector3[n];
        baseNormals = new Vector3[n];
        tileIndex4 = new Vector4[n];

        for (int i = 0; i < n; i++)
        {


            float worldX = Random.Range(terrainOrigin.x, terrainOrigin.x + terrainWidth);
            float worldZ = Random.Range(terrainOrigin.z, terrainOrigin.z + terrainLength);
            float worldY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + terrainOrigin.y;

            // Sample normal (normalized [0,1] coords)
            float normX = (worldX - terrainOrigin.x) / terrainWidth;
            float normZ = (worldZ - terrainOrigin.z) / terrainLength;
            Vector3 normal = tData.GetInterpolatedNormal(normX, normZ);

            Vector3 pos = new Vector3(worldX, worldY, worldZ);

            basePositions[i] = pos;
            baseNormals[i] = normal;

            float randomTile = Random.Range(0, 9); // 0–8 for 3x3 atlas
            tileIndex4[i] = new Vector4(randomTile, 0f, 0f, 0f);

            // blade transform (position is used to place the quad; keep pivot at bottom in mesh)
            Quaternion rot = Quaternion.identity;
            //float s = Random.Range(scaleRange.x, scaleRange.y);
            matrices[i] = Matrix4x4.TRS(pos, rot, Vector3.one);
        }
    }

    void Update()
    {
        if (matrices == null || matrices.Length == 0) return;

        // we will chunk into batches of <= 1023
        int total = matrices.Length;
        int offset = 0;

        // Convert Vector3 arrays to Vector4 arrays (w unused)
        Vector4[] pos4 = new Vector4[total];
        Vector4[] norm4 = new Vector4[total];


        for (int i = 0; i < total; i++)
        {
            Vector3 p = basePositions[i];
            Vector3 nrm = baseNormals[i];
            pos4[i] = new Vector4(p.x, p.y, p.z, 0f);
            norm4[i] = new Vector4(nrm.x, nrm.y, nrm.z, 0f);

        }

        // MaterialPropertyBlock reused per batch
        MaterialPropertyBlock props = new MaterialPropertyBlock();

        while (offset < total)
        {
            int batchSize = Mathf.Min(MAX_INSTANCES_PER_BATCH, total - offset);

            // Build slice arrays for this batch
            Matrix4x4[] matrixSlice = new Matrix4x4[batchSize];
            Vector4[] posSlice = new Vector4[batchSize];
            Vector4[] normSlice = new Vector4[batchSize];
            Vector4[] tileSlice = new Vector4[batchSize];


            System.Array.Copy(matrices, offset, matrixSlice, 0, batchSize);
            System.Array.Copy(pos4, offset, posSlice, 0, batchSize);
            System.Array.Copy(norm4, offset, normSlice, 0, batchSize);
            System.Array.Copy(tileIndex4, offset, tileSlice, 0, batchSize);

            props.Clear();
            props.SetVectorArray(k_BasePosId, posSlice);
            props.SetVectorArray(k_BaseNormalId, normSlice);
            props.SetVectorArray(k_TileIndexId, tileSlice);


            Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, matrixSlice, batchSize, props, UnityEngine.Rendering.ShadowCastingMode.On, true, gameObject.layer);

            offset += batchSize;
        }
    }

    // For debugging: visualize first few positions
    void OnDrawGizmosSelected()
    {
        if (basePositions == null) return;
        Gizmos.color = Color.green;
        int count = Mathf.Min(64, basePositions.Length);
        for (int i = 0; i < count; i++)
        {
            Gizmos.DrawSphere(basePositions[i], 0.05f);
            Gizmos.DrawRay(basePositions[i], baseNormals[i] * 0.2f);
        }

        Terrain t = GetComponent<Terrain>();
        Vector3 p = t.GetPosition();
        Vector3 s = t.terrainData.size;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(p + s * 0.5f, s);

        DebugTerrainAndGrassBounds();

        // existing code kept...
        if (debugTerrainSize != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(debugTerrainOrigin + debugTerrainSize * 0.5f, debugTerrainSize);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube((debugGrassMin + debugGrassMax) * 0.5f, debugGrassMax - debugGrassMin);
        }

    }

    void DebugTerrainAndGrassBounds()
    {
        Terrain terrain = GetComponent<Terrain>();
        TerrainData tData = terrain.terrainData;
        Vector3 terrainOrigin = terrain.GetPosition();
        Vector3 tSize = tData.size;

        // Compute min/max of generated basePositions
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        for (int i = 0; i < basePositions.Length; i++)
        {
            Vector3 p = basePositions[i];
            if (p.x < min.x) min.x = p.x;
            if (p.y < min.y) min.y = p.y;
            if (p.z < min.z) min.z = p.z;
            if (p.x > max.x) max.x = p.x;
            if (p.y > max.y) max.y = p.y;
            if (p.z > max.z) max.z = p.z;
        }

        Debug.Log($"Terrain origin: {terrainOrigin}  size: {tSize}");
        Debug.Log($"Terrain world bounds: min {terrainOrigin}  max {terrainOrigin + tSize}");
        Debug.Log($"Grass generated bounds: min {min}  max {max}");
        Debug.Log($"Grass span X size: {max.x - min.x}, Z size: {max.z - min.z}");

        debugTerrainOrigin = terrainOrigin;
        debugTerrainSize = tSize;
        debugGrassMin = min;
        debugGrassMax = max;

    }

}
