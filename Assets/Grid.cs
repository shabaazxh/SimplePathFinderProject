using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum TerrainType {
    Water,
    Grass,
    Sand,
    Mountain
}

public class Node {

    public Vector3 Position {get; set;}
    public TerrainType TerrainType {get; set;}
    public int Cost {get; set;}

    public Node(Vector3 position, TerrainType terrainType ,int cost)
    {
        Position = position;
        Cost = cost;
        TerrainType = terrainType;
    }
}

public class Grid : MonoBehaviour
{

    public int Width = 20;
    public int Height = 20;

    [Range(0,1)]
    public float noise;

    [Header("Pathfinding")]
    public float nodeRadius;
    float nodeDiameter;
    int nodeGridX, nodeGridY;
    Node[,] grid;

    //[HideInInspector]
    public Transform Ground;

    public HashSet<Vector3> WalkableCells;

    TerrainGenerator terrainGenerator;

    void Awake()
    {
        nodeDiameter = nodeRadius * 2;
        nodeGridX = Mathf.RoundToInt(Width / nodeDiameter);
        nodeGridY = Mathf.RoundToInt(Height / nodeDiameter);
        terrainGenerator = GetComponent<TerrainGenerator>();
    }

    public void Start()
    {
        WalkableCells = new HashSet<Vector3>();
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        grid = new Node[nodeGridX, nodeGridY];

        Ground.position = new Vector3(Width / 2f, 0, Height / 2f);
        Ground.localScale = new Vector3(Width / 10f, 1, Height / 10f);
        Camera.main.transform.position = new Vector3(Ground.position.x, 5f * (Width / 10f), Ground.position.z - Height / 2f - Height / 4f - (Height / 10f));

        LocateWalkableCells();
    }

    private void LocateWalkableCells()
    {
        for(int z = 0; z < Height; z++)
        {
            for(int x = 0; x < Width; x++)
            {
                noise = GetNoiseValue(x, z);
                Debug.Log(noise);
                if(noise > 0.5) {

                    WalkableCells.Add(new Vector3(x, 0, z));
                    grid[x,z] = new Node(new Vector3(x, 0, z), TerrainType.Mountain, 1);
                } 
                else if(noise >= 0.9) {
                    grid[x,z] = new Node(new Vector3(x, 0, z), TerrainType.Water, 1);
                }
                else if(noise >= 0.8 && noise < 0.9) {
                    grid[x,z] = new Node(new Vector3(x, 0, z), TerrainType.Sand, 1);
                }
                else {
                    grid[x,z] = new Node(new Vector3(x, 0, z), TerrainType.Grass, 1);
                }
            }
        }
    }

    private float GetNoiseValue(int x, int z)
    {
        int pos = (x * Width) + z;
        //Debug.Log(terrainGenerator.noiseArray.Length);

        //Debug.Log(pos + " " + Mathf.Round(terrainGenerator.noiseArray[10000] * 10) / 10);
        //Debug.Log(pos);
        return Mathf.Round(terrainGenerator.noiseArray[pos] * 10) / 10;
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(Width, 1, Height));
        if(grid != null)
        {
            foreach(Node n in grid)
            {
                if(n.TerrainType == TerrainType.Grass)
                {
                    Gizmos.color = Color.green;
                } 
                else if(n.TerrainType == TerrainType.Mountain)
                {
                    Gizmos.color = Color.black;
                } 
                else if(n.TerrainType == TerrainType.Water)
                {
                    Gizmos.color = Color.blue;
                }
                else if(n.TerrainType == TerrainType.Sand)
                {
                    Gizmos.color = Color.yellow;
                }
                else {
                    Gizmos.color = Color.white;
                }

                Gizmos.DrawCube(n.Position, Vector3.one * (nodeDiameter - .1f));
            }
        }
    }

}
