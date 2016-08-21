using UnityEngine;
using System.Collections;
using System;

public class MeshDisplay : MonoBehaviour {

    public Renderer textureRenderer;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public MeshFilter waterMeshFilter;

    /// <summary>
    /// Create a Color array containing the correct colors for given noiseMap values.
    /// </summary>
    /// <param name="noiseMap">2-dimensianal float array containing noisemap values.</param>
    public void DrawTexture(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;   
    }

    public void DrawMesh(MeshData meshData, WaterMeshData waterMeshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
        waterMeshFilter.sharedMesh = waterMeshData.CreateMesh();
    }
}
