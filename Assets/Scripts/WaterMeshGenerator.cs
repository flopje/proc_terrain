using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class WaterMeshGenerator {

    public static WaterMeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier,  int levelOfDetail)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        int meshSimplificationIncrement = levelOfDetail == 0 ? 1 : levelOfDetail * 2;
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

        WaterMeshData meshData = new WaterMeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;


        for (int y = 0; y < height; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < width; x += meshSimplificationIncrement)
            {
                //if (heightMap[x, y] < 0.5 )//&&  (x + 1 < width && heightMap[x + 1, y] < 0.5) || (y + 1 < height && heightMap[x, y + 1] < 0.5))//)
                //{
                    meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, 6.34f, topLeftZ - y);

                    // UV Index is a percentage based index, value between 0 - 1
                    meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                    // Right and bottom vertices can be ignored, as previously indexed vertices have drawn the triangles to these points, and none originate from them
                    // i  , i+1  , i+2
                    // i+w, i+w+1, i+w+2
                    // 
                    // i+2 and i+w+2  don't draw any triangles to their right or bottom, these are allready create when i+1 drawn both his triangles to right bottom 
                    // 
                    // Draws the needed triangles, 2 per vertex (creating a square)
                    // Drawing of a triangle allways happens clockwise.
                    if (x < width - 1 && y < height - 1)
                    {
                        meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                        meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                    }

                //} else
                //{
                //    break;
                //}
                vertexIndex++;
            }
        }

        return meshData;
    }
}

public class WaterMeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    int triangleIndex;

    public WaterMeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];

        // Total squares in new mesh * 6 ( 2 (traingles per square) * 3 (3 vertices per triangle))
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        // RecalculateNormals needed for correct lighting
        mesh.RecalculateNormals();

        return mesh;
    }
}