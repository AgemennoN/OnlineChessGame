using System;
using System.Collections.Generic;
using UnityEngine;

public class ChessBoard : MonoBehaviour
{
    //  ART
    [Header("Art")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 0.75f;
    [SerializeField] private float yOffset = 0.1f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.5f;
    [SerializeField] private float deathSpacing = 0.35f;


    // PREFABS
    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] piecesPrefabs;
    [SerializeField] private Material[] teamMaterial;


    //  LOGIC
    [Header("Logic")]
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private ChessPiece[,] chessPieces;
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private ChessPiece currentlyDragging;

    private void Awake()
    {
        GenerateChessBoard(tileSize, TILE_COUNT_X, TILE_COUNT_Y);

        SpawnAllPieces();
        PositionAllPieces();
    }

    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover")))
        {
            Vector2Int hitPosition = ReturnTileIndex(info.transform.gameObject);

            // From not hovering to hovering
            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            
            // From hovering one tile to hovering an other tile
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            if (Input.GetMouseButtonDown(0))
            {
                // If is it your turn
                if (true)
                {
                    if (chessPieces[hitPosition.x, hitPosition.y] != null)
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                    }
                }
            }

            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPosition = new Vector2Int (currentlyDragging.currentX, currentlyDragging.currentY);

                bool isMoveValid = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                if (!isMoveValid)
                {
                    currentlyDragging.SetPosition(GetCenterOfTile(previousPosition.x, previousPosition.y));
                    currentlyDragging = null;
                }
                else
                {
                    currentlyDragging = null;
                }
            }


        }
        else
        {   // From hovering to not hovering on tiles 
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPosition(GetCenterOfTile(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
            }

        }
    }



    // Generate the Board 
    private void GenerateChessBoard(float tileSize, int tileCountX, int tileCountY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountY / 2) * tileSize) + boardCenter;

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
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

        int[] triangles = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        tile.AddComponent<BoxCollider>();
        tile.layer = LayerMask.NameToLayer("Tile");

        return tile;
    }

    // Spawn Chess Pieces
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

        int whiteTeam = 0;
        int blackTeam = 1;

        // White Pieces
        chessPieces[0, 0] = SpawnSinglePiece(TChessPiece.Rook, whiteTeam);
        chessPieces[1, 0] = SpawnSinglePiece(TChessPiece.Knight, whiteTeam);
        chessPieces[2, 0] = SpawnSinglePiece(TChessPiece.Bishop, whiteTeam);
        chessPieces[3, 0] = SpawnSinglePiece(TChessPiece.Queen, whiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(TChessPiece.King, whiteTeam);
        chessPieces[5, 0] = SpawnSinglePiece(TChessPiece.Bishop, whiteTeam);
        chessPieces[6, 0] = SpawnSinglePiece(TChessPiece.Knight, whiteTeam);
        chessPieces[7, 0] = SpawnSinglePiece(TChessPiece.Rook, whiteTeam);
        for (int x = 0; x < TILE_COUNT_X; x++)
            chessPieces[x, 1] = SpawnSinglePiece(TChessPiece.Pawn, whiteTeam);

        // Black Pieces
        chessPieces[0, 7] = SpawnSinglePiece(TChessPiece.Rook, blackTeam);
        chessPieces[1, 7] = SpawnSinglePiece(TChessPiece.Knight, blackTeam);
        chessPieces[2, 7] = SpawnSinglePiece(TChessPiece.Bishop, blackTeam);
        chessPieces[3, 7] = SpawnSinglePiece(TChessPiece.Queen, blackTeam);
        chessPieces[4, 7] = SpawnSinglePiece(TChessPiece.King, blackTeam);
        chessPieces[5, 7] = SpawnSinglePiece(TChessPiece.Bishop, blackTeam);
        chessPieces[6, 7] = SpawnSinglePiece(TChessPiece.Knight, blackTeam);
        chessPieces[7, 7] = SpawnSinglePiece(TChessPiece.Rook, blackTeam);
        for (int x = 0; x < TILE_COUNT_X; x++)
            chessPieces[x, 6] = SpawnSinglePiece(TChessPiece.Pawn, blackTeam);
    }
    private ChessPiece SpawnSinglePiece(TChessPiece type, int team)
    {
        ChessPiece cp = Instantiate(piecesPrefabs[(int)type],transform).GetComponent<ChessPiece>();

        cp.team = team;
        cp.type = type;

        Material[] mat = cp.GetComponent<MeshRenderer>().materials;
        mat[1] = teamMaterial[team];
        cp.GetComponent<MeshRenderer>().materials = mat;

        return cp;
    }

    // Position Chess Pieces
    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                    PositionSinglePiece(x, y, true);
    }
    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(GetCenterOfTile(x, y), force);

    }

    private Vector3 GetCenterOfTile(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    // Operations

    private bool MoveTo(ChessPiece cp, int x, int y)
    {
        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];

            if (cp.team == ocp.team)
            {
                return false;
            }
            else if(ocp.team == 0)
            {
                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(GetCenterOfTile(8, -1) +
                    new Vector3(0, 0, deadWhites.Count * deathSpacing));
            }
            else
            {
                deadBlacks.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(GetCenterOfTile(-1, 8) -
                    new Vector3(0, 0, deadBlacks.Count * deathSpacing));
            }
        }

        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y);
        
        return true;
    }
    private Vector2Int ReturnTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x,y] == hitInfo)
                    return new Vector2Int(x, y);

        return -Vector2Int.one;
    }
}
