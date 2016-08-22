using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;

public class CloudGenerator : MonoBehaviour {

    public enum DrawMode
    {
        NoiseMap, Mesh, ColorMap
    }

    // Which style preview we want, default is noiseMap.
    public DrawMode drawMode = DrawMode.NoiseMap;

    public Noise.NormalizeMode normalizeMode;

    // Should the Unity preview be directly updated?
    public bool autoUpdate = true;

    // Max square map size is 255 -> max vertices per mesh 65000 (a litte more)
    // for formula -> width -1 -> 241 - 1 = 240, whcihc in turn gives us the most LODs. 
    public const int mapChunkSize = 239;

    [Range(0, 6)]
    public int editorPreviewLOD;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    public float noiseScale;
    public int octaves = 4;

    public int cloudStartHeight;
    public Color cloudColor;

    [Range(0, 1)] // Wil turn this value in a slider in the Unity editor
    public float persistance = 0.5f;

    public float lacunarity = 2;
    public int seed = 1;

    public Vector2 offset = Vector2.one;

    //Queue<MapThreadInfo<CloudData>> cloudDataThreadInfoQueue = new Queue<MapThreadInfo<CloudData>>();
    //Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void DrawMapInEditor()
    {
        CloudDisplay mapDisplay = FindObjectOfType<CloudDisplay>();
        CloudData cloudData = generateCloudData(Vector2.zero);

        if (drawMode == DrawMode.NoiseMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(cloudData.heightMap));
        }
        else if (drawMode == DrawMode.ColorMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromColorMap(cloudData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            mapDisplay.DrawMesh(
                TerrainMeshGenerator.GenerateTerrainMesh(cloudData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD),
                TextureGenerator.TextureFromColorMap(cloudData.colorMap, mapChunkSize, mapChunkSize)
                );
        }
    }

    CloudData generateCloudData(Vector2 centre)
    {
        float[,] noiseMap = Noise.generateNoiseMap(mapChunkSize, mapChunkSize, noiseScale, seed, octaves, persistance, lacunarity, centre + offset, normalizeMode);
        Color[] colorMap = CreateColorMap(noiseMap, mapChunkSize, mapChunkSize);

        return new CloudData(noiseMap, colorMap);
    }

    Color[] CreateColorMap(float[,] noiseMap, int mapWidth, int mapHeight)
    {
        Color[] colorMap = new Color[mapWidth * mapHeight];

        // Add terrain types to the generated noise
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float currentHeight = noiseMap[x, y];

                colorMap[y * mapWidth + x] = cloudColor;
            }
        }

        return colorMap;
    }

    /// <summary>
    /// Called when the values change, and is used for validating if the given values are within accepted bounds.
    /// </summary>
    void OnValidate()
    {

        if (lacunarity < 1)
        {
            lacunarity = 1;
        }

        if (octaves < 0)
        {
            octaves = 0;
        }
    }
}

public struct CloudData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public CloudData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}
