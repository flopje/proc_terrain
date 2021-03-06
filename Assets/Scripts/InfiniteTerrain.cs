﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets;
using UnityStandardAssets.Water;

public class InfiniteTerrain : MonoBehaviour
{

    public LODInfo[] detailLevels;
    public Transform viewer;
    public Material mapMaterial;

    public Material waterMaterial;

    const float scale = 2f;

    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public static float maxViewDistance;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    int chunkSize;
    int chunksVisibleInViewDistance;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    static MapGenerator mapGenerator;

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);

        // Draw first time 
        UpdateVisibleChunks();
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / scale;

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        // Set last visible terrain chunks invisible
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();


        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                }
                else
                {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial, mapGenerator.generateWaterMesh, waterMaterial));
                }

            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        GameObject waterMeshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;


        MeshFilter waterMeshFilter;
        MeshRenderer waterMeshRenderer;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        LODMesh collisionLODMesh;

        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;

        bool generateWaterPlane;

        public TerrainChunk(Vector2 coord, int chunkSize, LODInfo[] detailLevels, Transform parent, Material material, bool generateWaterPlane, Material waterMaterial)
        {
            this.detailLevels = detailLevels;
            this.generateWaterPlane = generateWaterPlane;

            position = coord * chunkSize;
            bounds = new Bounds(position, Vector2.one * chunkSize);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();

            meshRenderer.sharedMaterial = material;

            meshObject.transform.position = positionV3 * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;


            if (generateWaterPlane)
            {
                waterMeshObject = new GameObject("Tile");
                waterMeshObject.layer = 4; // Built-in layer 4 == water
                waterMeshFilter = waterMeshObject.AddComponent<MeshFilter>();
                waterMeshRenderer = waterMeshObject.AddComponent<MeshRenderer>();

                waterMeshRenderer.material = waterMaterial;

                waterMeshObject.transform.position = positionV3 * scale;
                waterMeshObject.transform.localScale = Vector3.one * scale;
                waterMeshObject.transform.parent = GameObject.Find("Water4Advanced").transform;

                waterMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                waterMeshObject.AddComponent<WaterTile>();
                
            }

            // Initially hide chunk, and let the update method decide if the chunk must be shown.
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk, generateWaterPlane);
                if(detailLevels[i].useForCollider)
                {
                    collisionLODMesh = lodMeshes[i];
                }
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        /// <summary>
        /// Set the visibilty of the chunk. If the chunk is further away from the player than maxViewDistance, the chunk will be set invisible (Disabled).
        /// </summary>
        public void UpdateTerrainChunk()
        {
            if (!mapDataReceived)
            {
                return;
            }

            float viewDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewDistanceFromNearestEdge <= maxViewDistance;

            if (visible)
            {
                int lodIndex = 0;
                for (int i = 0; i < detailLevels.Length - 1; i++)
                {
                    if (viewDistanceFromNearestEdge > detailLevels[i].visibleDistThreshold)
                    {
                        lodIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if (lodIndex != previousLODIndex)
                {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if (lodMesh.hasMesh)
                    {
                        previousLODIndex = lodIndex;
                        meshFilter.mesh = lodMesh.mesh;

                        if (generateWaterPlane)
                        {
                            waterMeshFilter.mesh = lodMesh.waterMesh;
                        }
                        meshFilter.sharedMesh = lodMesh.mesh;

                        if (generateWaterPlane)
                        {
                            waterMeshFilter.sharedMesh = lodMesh.waterMesh;
                        }
                    }
                    else if (!lodMesh.hasRequestMesh)
                    {
                        lodMesh.RequestMesh(mapData);
                    }
                }

                if(lodIndex == 0)
                {
                    if(collisionLODMesh.hasMesh)
                    {
                        meshCollider.sharedMesh = collisionLODMesh.mesh;
                    }
                    else if(!collisionLODMesh.hasRequestMesh)
                    {
                        collisionLODMesh.RequestMesh(mapData);
                    }
                }

                terrainChunksVisibleLastUpdate.Add(this);
            }

            SetVisible(visible);
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize + 2, MapGenerator.mapChunkSize + 2);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
            if (waterMeshObject != null)
            {
                waterMeshObject.SetActive(visible);
            }
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }

        public bool IsWaterVisible()
        {
            return waterMeshObject.activeSelf;
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public Mesh waterMesh;
        public bool hasRequestMesh;
        public bool hasMesh;

        MapData mapData;
        bool generateWaterPlane;

        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback, bool generateWaterPlane)
        {   
            this.lod = lod;
            this.updateCallback = updateCallback;
            this.generateWaterPlane = generateWaterPlane;
        }

        public void RequestMesh(MapData mapData)
        {
            this.mapData = mapData;
            hasRequestMesh = true;

            mapGenerator.RequestMeshData(this.mapData, lod, OnMeshDataReceived);
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();

            if (generateWaterPlane)
            {
                mapGenerator.RequestWaterMeshData(mapData, lod, OnMeshDataReceived);
            }
            else
            {
                hasMesh = true;
                updateCallback();
            }
        }

        void OnMeshDataReceived(WaterMeshData meshData)
        {
            waterMesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDistThreshold;
        public bool useForCollider;
    }

}
