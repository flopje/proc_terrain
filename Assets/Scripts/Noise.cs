using UnityEngine;
using System.Collections;

///<summary>
///Based upon: https://www.youtube.com/watch?v=wbpMiKiSKm8&list=PLFt_AvWsXl0eBW2EiBtl_sxmDtSgZBxB3
///
/// Main Class for generating a noiseMap for usage in procedural terrain.
/// </summary> 
public static class Noise {

    /// <summary>
    /// Used to decide which normalization mode we want to use.
    /// Local means that the normalization of noise values will happen per chunk, used for static terrain.
    /// Global means we estimate the min and max values such that, for infinite terrain, we normalize all the chunks in the same range, 
    ///     eliminating any seems between chunks.
    /// </summary>
    public enum NormalizeMode
    {
        Local, Global
    };

    /// <summary>
    /// Generates a 2 dimensional float array holding generated 'noise' values. This resulting arra can be used for generating heightMaps.
    /// </summary>
    /// <param name="mapWidth">Width of the map.</param>
    /// <param name="mapHeight">Height of the map</param>
    /// <param name="scale">The scale used for geneating noise. Lower value means a more detailed (noisy) map. Thus lower values can be used for more detail.</param>
    /// <param name="seed">Int value used for random generation of offset values which in turn are used to offset the sample points (every octave values from a diff. location), giving us unique maps every time, but also giving us the possibility to maintain the same map, by keeping this value the same.</param>
    /// <param name="octaves">Number of passes/ noise maps to generate and combine</param>
    /// <param name="persistance">Controls the decrease in amplitude in an octave</param>
    /// <param name="lacunarity">Controls the increase in frequency used in an octave</param>
    /// <param name="offSet">Vector2 containing offset values for the random number generator. Giving us the posibilty to keep the seed the same, and still 'scroll' through values</param>
    /// <returns>
    /// 2 dimensial array containing float values.
    /// </returns>
    /// <remarks>
    /// Octaves, persistance and lacunarity should be used in conjuction with each other.
    /// Such that when we want a more detailed noise pass, which shouldn't affect the overall shape to much, we want to increase the lacunarity value, bu decrease the persistance value.
    /// </remarks>
    public static float[,] generateNoiseMap(int mapWidth, int mapHeight, float scale, int seed, int octaves, float persistance, float lacunarity, Vector2 offSet, NormalizeMode normalizeMode)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prNumberGenerator = new System.Random(seed);
        Vector2[] octaveOffset = new Vector2[octaves];

        // Y-axis -> (Height of he curves)
        float amplitude = 1;
        // X-Axis -> how ragged a curve is (times up/down occurs from a curve in a given timespan)
        float frequency = 1;

        float maxPossibleHeight = 0;

        for(int i = 0; i < octaves; i++)
        {
            float offsetX = prNumberGenerator.Next(-100000, 100000) + offSet.x;
            float offsetY = prNumberGenerator.Next(-100000, 100000) - offSet.y;
            octaveOffset[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        if ( scale <= 0 )
        {
            scale = 0.0001f; // divide by zero prevention
        }

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        // When scaling, these values will make sure to scale from the center of the plane, and not from the top, right.
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {

                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                // Octaves, a noiseMap result curve can be reffered to as octave.
                // More noisemapt, more octaves will lead to a more detailed final noisemap.
                for(int i = 0; i < octaves; i++)
                {
                    // SamplePoints
                    float sampleX = (x - halfWidth + octaveOffset[i].x) / scale * frequency ;
                    float sampleY = (y - halfHeight + octaveOffset[i].y) / scale * frequency ;

                    // *2 -1 will allow for negative values as result from the PerlinNoise method. 
                    // This will, in turn, allow for noiseHeight value to decrease
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; 
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                } else if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }
                noiseMap[x, y] = noiseHeight;
            }
        }

        // Normalize the vallues in noiseMap to be between 0 - 1. (which are the values the map can be generated off.
        // Because of * 2 -1 in the previous for loop, there could otherwise be values lowe than 0, and higher than 1.
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (normalizeMode == NormalizeMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else if (normalizeMode == NormalizeMode.Global)
                {
                    // Last division (1.75f) is an estimate based upon various results. This makes sure we lower the maxPossibleHeight value to accompagny our noisemap better.
                    // But this will also result in mountain peaks with a value higher than 1. So we adjust our animationCurve to accompagny that change.
                    float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight / 1.457f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
                
            }
        }

        return noiseMap;
    }
}
