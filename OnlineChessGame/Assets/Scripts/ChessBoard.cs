using System;
using UnityEngine;

public class ChessBoard : MonoBehaviour
{
    //  ART
    [Header("Art")]
    [SerializeField] private Material tileMaterial;

    //  LOGIC
    [Header("Logic")]

    private float tileSize = 1;
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private const float zOffset = 0.1f;
    private GameObject[,] tiles;

    private void Awake()
    {
        GenerateChessBoard(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
    }

    private void GenerateChessBoard(float tileSize, int tileCountX, int tileCountY)
    {
        tiles = new GameObject[tileCountX,tileCountY];
        for (int x = 0; x < tileCountX; x++)
            for (int y = 0; y < tileCountY; y++)
                tiles[x, y] = GenerateSingleTile(tileSize, x, y); 
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tile = new GameObject(string.Format("X: {0}, Y: {1}", x, y));
        
        tile.transform.parent = transform;

        Mesh mesh = new Mesh();
        tile.AddComponent<MeshFilter>().mesh = mesh;
        tile.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, zOffset, y * tileSize);
        vertices[1] = new Vector3(x * tileSize, zOffset, (y + 1) * tileSize);
        vertices[2] = new Vector3((x + 1) * tileSize, zOffset, y * tileSize);
        vertices[3] = new Vector3((x + 1) * tileSize, zOffset, (y + 1) * tileSize);

        int[] triangles = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        tile.AddComponent<BoxCollider>();

        return tile;
    }
}
