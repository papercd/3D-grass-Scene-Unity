using UnityEngine;

/// <summary>
/// Procedurally generates terrain height using Perlin noise.
/// Creates subtle hills and valleys while keeping terrain mostly flat.
/// </summary>
[RequireComponent(typeof(Terrain))]
public class TerrainHeightGenerator : MonoBehaviour
{
    [Header("Height Settings")]
    [Tooltip("Maximum height variation (in Unity units)")]
    [Range(0f, 20f)]
    public float heightScale = 3f;

    [Tooltip("Base height offset (raises entire terrain)")]
    [Range(0f, 50f)]
    public float baseHeight = 0f;

    [Header("Noise Settings")]
    [Tooltip("Noise frequency - higher = more hills, lower = smoother/larger hills")]
    [Range(0.001f, 0.1f)]
    public float noiseScale = 0.02f;

    [Tooltip("Number of noise layers (more = more detail)")]
    [Range(1, 6)]
    public int octaves = 3;

    [Tooltip("How much each octave contributes")]
    [Range(0f, 1f)]
    public float persistence = 0.5f;

    [Tooltip("Frequency multiplier per octave")]
    [Range(1f, 4f)]
    public float lacunarity = 2f;

    [Header("Random Seed")]
    [Tooltip("Seed for noise generation (change for different terrain)")]
    public int seed = 0;

    [Header("Smoothing")]
    [Tooltip("Apply smoothing pass to reduce sharp edges")]
    public bool applySmoothing = true;

    [Range(0, 5)]
    public int smoothingPasses = 1;

    [Header("Optional: Flatten Center")]
    [Tooltip("Create a flat area in the center (good for arena)")]
    public bool flattenCenter = false;

    [Range(0f, 1f)]
    public float flattenRadius = 0.3f;

    [Range(0f, 1f)]
    public float flattenFalloff = 0.2f;

    private Terrain terrain;
    private TerrainData terrainData;

    void Awake()
    {
        terrain = GetComponent<Terrain>();
        terrainData = terrain.terrainData;
    }

    /// <summary>
    /// Generate terrain heights
    /// </summary>
    [ContextMenu("Generate Terrain")]
    public void GenerateTerrain()
    {
        // Try to get components if not already cached
        if (terrain == null)
            terrain = GetComponent<Terrain>();

        if (terrain == null)
        {
            Debug.LogError("❌ No Terrain component found on this GameObject!");
            return;
        }

        if (terrain.terrainData == null)
        {
            Debug.LogError("❌ No TerrainData assigned to the Terrain component!");
            return;
        }

        terrainData = terrain.terrainData;

        Debug.Log("🏔️ Generating procedural terrain...");

        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;
        float[,] heights = new float[width, height];

        // Generate base noise
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                heights[x, y] = CalculateHeight(x, y, width, height);
            }
        }

        // Optional: Flatten center
        if (flattenCenter)
        {
            heights = ApplyCenterFlattening(heights, width, height);
        }

        // Optional: Smoothing
        if (applySmoothing)
        {
            for (int i = 0; i < smoothingPasses; i++)
            {
                heights = SmoothHeights(heights, width, height);
            }
        }

        // Apply to terrain
        terrainData.SetHeights(0, 0, heights);

        Debug.Log($"✅ Terrain generated! Resolution: {width}x{height}, Height Scale: {heightScale}");
    }

    /// <summary>
    /// Calculate height at a specific point using multi-octave Perlin noise
    /// </summary>
    float CalculateHeight(int x, int y, int width, int height)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float noiseHeight = 0f;
        float maxValue = 0f; // For normalization

        // Multi-octave noise (fractal Brownian motion)
        for (int i = 0; i < octaves; i++)
        {
            // Calculate sample coordinates
            float sampleX = (x / (float)width) * noiseScale * frequency + seed;
            float sampleY = (y / (float)height) * noiseScale * frequency + seed;

            // Sample Perlin noise
            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);

            // Perlin returns 0-1, shift to -1 to 1 for more natural terrain
            perlinValue = perlinValue * 2f - 1f;

            noiseHeight += perlinValue * amplitude;
            maxValue += amplitude;

            // Adjust amplitude and frequency for next octave
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        // Normalize to 0-1 range
        noiseHeight = (noiseHeight / maxValue + 1f) * 0.5f;

        // Apply height scale and base height
        float finalHeight = (noiseHeight * heightScale + baseHeight) / terrainData.size.y;

        return Mathf.Clamp01(finalHeight);
    }

    /// <summary>
    /// Apply smoothing to reduce sharp peaks/valleys
    /// </summary>
    float[,] SmoothHeights(float[,] heights, int width, int height)
    {
        float[,] smoothed = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Average with neighbors
                float sum = heights[x, y];
                int count = 1;

                // Check all 8 neighbors
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        {
                            sum += heights[nx, ny];
                            count++;
                        }
                    }
                }

                smoothed[x, y] = sum / count;
            }
        }

        return smoothed;
    }

    /// <summary>
    /// Flatten the center area for an arena
    /// </summary>
    float[,] ApplyCenterFlattening(float[,] heights, int width, int height)
    {
        int centerX = width / 2;
        int centerY = height / 2;
        float maxDistance = Mathf.Sqrt(centerX * centerX + centerY * centerY);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Calculate normalized distance from center
                float dx = (x - centerX) / (float)centerX;
                float dy = (y - centerY) / (float)centerY;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                // Apply flattening based on distance
                if (distance < flattenRadius)
                {
                    // Fully flat in center
                    heights[x, y] = baseHeight / terrainData.size.y;
                }
                else if (distance < flattenRadius + flattenFalloff)
                {
                    // Smooth transition from flat to natural
                    float t = (distance - flattenRadius) / flattenFalloff;
                    t = Mathf.SmoothStep(0f, 1f, t); // Smooth interpolation

                    float flatHeight = baseHeight / terrainData.size.y;
                    heights[x, y] = Mathf.Lerp(flatHeight, heights[x, y], t);
                }
                // else: keep original height
            }
        }

        return heights;
    }

    /// <summary>
    /// Flatten entire terrain
    /// </summary>
    [ContextMenu("Flatten Terrain")]
    public void FlattenTerrain()
    {
        if (terrain == null)
            terrain = GetComponent<Terrain>();

        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogError("❌ No Terrain or TerrainData found!");
            return;
        }

        terrainData = terrain.terrainData;

        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;
        float[,] heights = new float[width, height];

        float flatHeight = baseHeight / terrainData.size.y;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                heights[x, y] = flatHeight;
            }
        }

        terrainData.SetHeights(0, 0, heights);
        Debug.Log("✅ Terrain flattened");
    }

    /// <summary>
    /// Randomize seed for new terrain
    /// </summary>
    [ContextMenu("Randomize Seed")]
    public void RandomizeSeed()
    {
        seed = Random.Range(0, 10000);
        Debug.Log($"🎲 New seed: {seed}");
    }
}