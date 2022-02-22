using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [Header("Terrain Gen")]
    public int Width;
    public int Depth;
    public Gradient TerrainGradient;
    // Octaves implement
    public int Seed;
    [Range(1, 100)]
    public int Octaves;
    [Range(1, 100)]
    public float NoiseScale;
    [Range(0, 1)]
    public float Persistance;
    [Range(1, 100)]
    public float Lacunarity;
    [Range(1, 100)]
    public float HeightMultiplier;
    [Range(0, 1)]
    public float HeightThreshold;
    public Vector2 Offset;

    [Header("Terrain Visualize")]
    public GameObject VertexObject;
    public bool VisualizeVertices;


    private Vector3[] vertices;
    public int[] trianglePoints;
    Vector2[] uvs;
    Color[] colors;
    private Mesh mesh;
    private MeshFilter meshFilter;
    private float minHeight;
    private float maxHeight;

    [HideInInspector]
    public float[] noiseArray;

    [Header("Animate Settings")]
    public float perlineScale = 4.56f;
    public float waveSpeed = 1f;
    public float waveHeight = 2f;

    // Start is called before the first frame update
    void Awake()
    {
        mesh = new Mesh();
        mesh.name = "Procedural Terrain";
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        CreateMesh();
        UpdateMesh();

        if(VisualizeVertices)
        {
            DrawVertices();
        }

    }

    private void DrawVertices()
    {
        for(int i = 0; i <vertices.Length; i++)
        {
            Instantiate(VertexObject, vertices[i], Quaternion.identity, transform);
        }
    }

    public void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = trianglePoints;
        mesh.uv = uvs;
        mesh.colors = colors;

        mesh.RecalculateNormals();
    }

    public void CreateMesh()
    {
        // Depth = z? +1 because we count from 0 like we do in arrays
        vertices = new Vector3[(Width + 1) * (Depth + 1)];
        noiseArray = PerlinNoise();

        int i = 0;
        for(int z = 0; z <= Depth; z++)
        {
            for(int x = 0; x <= Width; x++)
            {
                var currentHeight = noiseArray[i];

                if(currentHeight > HeightThreshold)
                {
                    currentHeight *= HeightMultiplier;
                }

                vertices[i] = new Vector3(x, currentHeight, z);
                i++;
            }
        }

        // multiply by 6 because for each cell we need 6 vertex positions to make 2 triangles
        trianglePoints = new int[Width * Depth * 6];
        int currentTrianglePoint = 0;
        int currentVertexPoint = 0;
        

        for(int z = 0; z < Depth; z++)
        {
            for(int x = 0; x < Width; x++)
            {
                trianglePoints[currentTrianglePoint + 0] = currentVertexPoint + 0;
                trianglePoints[currentTrianglePoint + 1] = currentVertexPoint + Width + 1;
                trianglePoints[currentTrianglePoint + 2] = currentVertexPoint + 1;
                trianglePoints[currentTrianglePoint + 3] = currentVertexPoint + 1;
                trianglePoints[currentTrianglePoint + 4] = currentVertexPoint + Width + 1;
                trianglePoints[currentTrianglePoint + 5] = currentVertexPoint + Width + 2;

                currentVertexPoint++;
                currentTrianglePoint += 6;

            }
            currentVertexPoint++;
        }

        //UVs
        uvs = new Vector2[vertices.Length];

        i = 0;
        for(int z = 0; z <= Depth; z++)
        {
            for(int x = 0; x <= Width; x++)
            {
                uvs[i] = new Vector2((float)x / Width, (float)z / Depth); //get in range 0,1
                i++;
            }
        }


        // Colours
        colors = new Color[vertices.Length];
        i = 0;
        for (int z = 0; z <= Depth; z++)
        {
            for (int x = 0; x <= Width; x++)
            {
                float height = Mathf.InverseLerp(minHeight * HeightMultiplier, maxHeight * HeightMultiplier, vertices[i].y);
                colors[i] = TerrainGradient.Evaluate(height);
                i++;
            }
        }

    }

    void AnimateMesh()
    {
        for(int i = 0; i < vertices.Length; i++)
        {
            float px = (vertices[i].x * perlineScale) + (Time.timeSinceLevelLoad * waveSpeed);
            float pz = (vertices[i].z * perlineScale) + (Time.timeSinceLevelLoad * waveSpeed);

            vertices[i].y = ( Mathf.PerlinNoise( px, pz ) - 0.5f ) * waveHeight;
        }

        mesh.vertices = vertices;
    }

    float[] PerlinNoise()
    {
        float[] noiseArray = new float[(Width + 1) * (Depth + 1)];
        System.Random randomValue = new System.Random(Seed);

        // Since we want to sample from different locations we can create different vector2 position
        Vector2[] octaveOffset = new Vector2[Octaves];
        for(int i = 0; i < Octaves; i++)
        {
            float offsetX = randomValue.Next(-100000, 100000) + Offset.x;
            float offsetY = randomValue.Next(-100000, 100000) + Offset.y;
            octaveOffset[i] = new Vector2(offsetX, offsetY);
        }

        // Change noise scale to be towards the centre
        float halfWidth = Width / 2f;
        float halfDepth = Depth / 2f;

        // Applying lacuraity and persistence here:
        int n = 0;
        for(int z = 0; z < Depth; z++)
        {
            for(int x = 0; x < Width; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for(int i = 0; i < Octaves; i++)
                {
                    // Use octaveOffsets to apply the seed value
                    float sampleX = (x - halfWidth) / NoiseScale * frequency + octaveOffset[i].x;
                    float sampleY = (z - halfDepth) / NoiseScale * frequency + octaveOffset[i].y;


                    float perlineValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlineValue * amplitude;

                    amplitude *= Persistance;
                    frequency *= Lacunarity;

                }

                if(noiseHeight > maxHeight)
                {
                    maxHeight = noiseHeight;
                }
                else if(noiseHeight < minHeight)
                {
                    minHeight = noiseHeight;
                }

                noiseArray[n] = noiseHeight;
                n++;
            }
        }

        // Now we need to normalize the height back to the range 0-1 which is done with InverseLerp

        int k = 0;
        for(int z = 0; z <= Depth; z++)
        {
            for(int x = 0; x < Width; x++)
            {
                noiseArray[k] = Mathf.InverseLerp(minHeight, maxHeight, noiseArray[k]);
                k++;
            }
        }

        return noiseArray;
    }


    // Update is called once per frame
    void Update()
    {
        //AnimateMesh();
    }
}
