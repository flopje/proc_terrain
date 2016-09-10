using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour {

    public enum DrawMode
    {
        NoiseMap, ColorMap, Mesh, FallOfMap
    }

    // Which style preview we want, default is noiseMap.
    public DrawMode drawMode = DrawMode.NoiseMap;

    public Noise.NormalizeMode normalizeMode;

    // Should the Unity preview be directly updated?
    public bool autoUpdate = true;

    public bool useFallOfMap;
    public bool generateWaterMesh = false;
    public bool usetFlatShading;

    [Range(0,6)]
    public int editorPreviewLOD;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    public float noiseScale;
    public int octaves = 4;

    [Range(0,1)] // Wil turn this value in a slider in the Unity editor
    public float persistance = 0.5f;

    public float lacunarity = 2;
    public int seed = 1;

    public Vector2 offset = Vector2.one;

    public TerrainType[] regions;
    static MapGenerator instance;

    float[,] fallOfMap;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();
    Queue<MapThreadInfo<WaterMeshData>> waterMeshDataThreadInfoQueue = new Queue<MapThreadInfo<WaterMeshData>>();

    void Awake()
    {
        fallOfMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);
    }

    // Max square map size is 255 -> max vertices per mesh 65000 (a litte more)
    // for formula -> width -1 -> 241 - 1 = 240, whcihc in turn gives us the most LODs. 
    // Subtracting 2 for borderTriangles -> 241 - 2 = 239
    //
    // For flatshading we lower the size to 95, Because it generates double the amount of vertices.
    public static int mapChunkSize {
        get {
            if(instance == null) {
                instance = FindObjectOfType<MapGenerator>();
            }

            if(instance.usetFlatShading) {
                return 95;
            } else {
                return 239;
            }
        }
    }

    public void DrawMapInEditor()
    {
        MeshDisplay mapDisplay = FindObjectOfType<MeshDisplay>();
        MapData mapData = generateMapData(Vector2.zero);

        if (drawMode == DrawMode.NoiseMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColorMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            if (generateWaterMesh)
            {
                mapDisplay.DrawMesh(
                    TerrainMeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD, usetFlatShading),
                    WaterMeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, editorPreviewLOD),
                    TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize + 2, mapChunkSize + 2)
                );
            }
            else
            {
                mapDisplay.DrawMesh(
                    TerrainMeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD, usetFlatShading),
                    TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize)
                );
            }
            
        }
        else if (drawMode == DrawMode.FallOfMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(fallOfMap));
        }
    }

    public void RequestMapData(Vector2 centre, Action<MapData> callBack)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(centre, callBack);
        };
        new Thread(threadStart).Start();
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callBack)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callBack);
        };
        new Thread(threadStart).Start();
    }

    public void RequestWaterMeshData(MapData mapData, int lod, Action<WaterMeshData> callBack)
    {
        ThreadStart threadStart = delegate
        {
            WaterMeshDataThread(mapData, lod, callBack);
        };
        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 centre, Action<MapData> callBack)
    {
        MapData mapData = generateMapData(centre);
        lock(mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callBack, mapData));
        }
        
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callBack)
    {
        MeshData meshData = TerrainMeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod, usetFlatShading);
        
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callBack, meshData));
        }
    }

    void WaterMeshDataThread(MapData mapData, int lod, Action<WaterMeshData> callBack)
    {
        WaterMeshData waterMeshData = WaterMeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, lod);

        lock (meshDataThreadInfoQueue)
        {
            waterMeshDataThreadInfoQueue.Enqueue(new MapThreadInfo<WaterMeshData>(callBack, waterMeshData));
        }
    }

    void Update()
    {
        if (mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callBack(threadInfo.parameter);
            }
        }

        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callBack(threadInfo.parameter);
            }
        }

        if (waterMeshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < waterMeshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<WaterMeshData> threadInfo = waterMeshDataThreadInfoQueue.Dequeue();
                threadInfo.callBack(threadInfo.parameter);
            }
        }
    }

   MapData generateMapData(Vector2 centre)
    {
        // Adding broderTriangles to correctly calculate normals: -> mapChunkSize + 2 (+2 == border, 1 on each side)
        float[,] noiseMap = Noise.generateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, noiseScale, seed, octaves, persistance, lacunarity, centre + offset, normalizeMode);
        Color[] colorMap = CreateColorMap(noiseMap, mapChunkSize + 2, mapChunkSize + 2);

        return new MapData(noiseMap, colorMap);         
    }

    Color[] CreateColorMap(float[,] noiseMap, int mapWidth, int mapHeight)
    {
        Color[] colorMap = new Color[mapWidth * mapHeight];

        // Add terrain types to the generated noise
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (useFallOfMap)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - fallOfMap[x, y]);
                }

                float currentHeight = noiseMap[x, y];

                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight >= regions[i].height)
                    {
                        colorMap[y * mapWidth + x] = regions[i].color;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        return colorMap;
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callBack;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callBack, T parameter)
        {
            this.callBack = callBack;
            this.parameter = parameter;
        }
    }


    /// <summary>
    /// Called when the values change, and is used for validating if the given values are within accepted bounds.
    /// </summary>
    void OnValidate()
    {

        if(lacunarity < 1)
        {
            lacunarity = 1;
        }

        if(octaves < 0)
        {
            octaves = 0;
        }

        fallOfMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
    public Material material;
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}
