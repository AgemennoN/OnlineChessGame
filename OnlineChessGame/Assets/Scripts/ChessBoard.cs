using System;
using System.Collections.Generic;
using UnityEngine;

public enum TSpecialMove{
    None = 0,
    EnPassant,
    Castling,
    Promotion
}

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
    [SerializeField] private float dragLevitation = 0.5f;

    // UI Elements
    [SerializeField] private GameObject victoryScreen;

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
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();
    private TSpecialMove specialMove;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private ChessPiece currentlyDragging;
    private bool isWhiteTurn;

    private void Awake()
    {
        GenerateChessBoard(tileSize, TILE_COUNT_X, TILE_COUNT_Y);

        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;
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

        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
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
                tiles[currentHover.x, currentHover.y].layer = ContainsValidMove(ref availableMoves, currentHover) ?
                    LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            if (Input.GetMouseButtonDown(0) && !victoryScreen.gameObject.activeSelf)
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    // If is it your turn
                    int teamTurn = isWhiteTurn ? 0 : 1;
                    if (chessPieces[hitPosition.x, hitPosition.y].team == teamTurn)
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];

                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);

                        specialMove = currentlyDragging.GetSpacialMove(ref chessPieces, ref moveList, ref availableMoves);

                        HighlightTiles();
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
                }
                currentlyDragging = null;
                RemoveHighlightTiles();
            }


        }
        else
        {   // From hovering to not hovering on tiles 
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = ContainsValidMove(ref availableMoves, currentHover) ?
                    LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPosition(GetCenterOfTile(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }

        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragLevitation);
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

    // CheckMate
    private void CheckMate(int team)
    {
        Display(team);
    }
    private void Display(int team)
    {
        victoryScreen.gameObject.SetActive(true);
        victoryScreen.transform.GetChild(team).gameObject.SetActive(true);
    }
    public void OnRestartButton()
    {
        // Reset UI 
        victoryScreen.gameObject.SetActive(false);
        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);

        // Reset Board
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    Destroy(chessPieces[x, y].gameObject);
                    chessPieces[x, y] = null;
                }
            }
        }

        for (int i = 0; i < deadWhites.Count; i++)
            Destroy(deadWhites[i].gameObject);
        for (int i = 0; i < deadBlacks.Count; i++)
            Destroy(deadBlacks[i].gameObject);

        deadWhites.Clear();
        deadBlacks.Clear();

        // Clear Variables
        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();

        // Restart the Game
        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;
    }
    public void OnExitButton()
    {
        Application.Quit();
    }

    // Special Move
    private void SpecialMoveProcesser()
    {
        if (specialMove == TSpecialMove.EnPassant)
        {
            Vector2Int[] allyPawnMove = moveList[moveList.Count - 1];
            Vector2Int[] enemyPawnMove = moveList[moveList.Count - 2];

            if (allyPawnMove[1].x == enemyPawnMove[1].x)
            {
                Debug.Log("Same X");
                if (Mathf.Abs(allyPawnMove[1].y - enemyPawnMove[1].y) == 1)
                {
                    Debug.Log("Mathf.Abs(allyPawnMove[1].y - enemyPawnMove[1].y) == 1");

                    KillPiece(chessPieces[enemyPawnMove[1].x, enemyPawnMove[1].y]);
                }
            }
        }

        // elseif castle or promotion

    }


    // Operations
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2Int pos)
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;

        return false;
    } 
    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
    }
    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        
        availableMoves.Clear();
    }
    private bool MoveTo(ChessPiece cp, int x, int y)
    {
        if (ContainsValidMove(ref availableMoves, new Vector2Int(x, y)) == false)
            return false;
        
        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        // If there is a piece on the tile
        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];

            if (cp.team == ocp.team)    // If not enemy can't it is an invalid Move
                return false;
            else                        // If an enemy piece kill
                KillPiece(ocp);
        }

        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) });
        PositionSinglePiece(x, y);
        SpecialMoveProcesser();

        isWhiteTurn = !isWhiteTurn;

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
    private void KillPiece(ChessPiece cp)
    {
        if (cp.team == 0)      // Defeated piece is White
        {
            if (cp.type == TChessPiece.King)
                CheckMate(1);

            deadWhites.Add(cp);
            cp.SetScale(Vector3.one * deathSize);
            cp.SetPosition(GetCenterOfTile(8, -1) +
                new Vector3(0, 0, deadWhites.Count * deathSpacing));
        }
        else                   // Defeated piece is Black
        {
            if (cp.type == TChessPiece.King)
                CheckMate(0);

            deadBlacks.Add(cp);
            cp.SetScale(Vector3.one * deathSize);
            cp.SetPosition(GetCenterOfTile(-1, 8) -
                new Vector3(0, 0, deadBlacks.Count * deathSpacing));
        }
    }
}
