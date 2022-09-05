using System;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private Transform rematchIndicator;

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

    // Multiplayer Lpgic
    private int playerCount = -1;
    private int currentTeam = -1;
    private bool localGame;
    private bool[] playerRematch = new bool[2];
    [SerializeField] private Button btnRematch;

    private void Start()
    {
        GenerateChessBoard(tileSize, TILE_COUNT_X, TILE_COUNT_Y);

        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;

        RegisterEvents();
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
                    if (chessPieces[hitPosition.x, hitPosition.y].team == teamTurn && currentTeam == teamTurn)
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];

                        // Gets a list for available moves to highlight 
                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                        // Adds special moves to the availableMoves list
                        specialMove = currentlyDragging.GetSpacialMove(ref chessPieces, ref moveList, ref availableMoves);

                        // Remove the moves that make you checked by enemy team from the availableMoves List
                        PreventCheck();

                        HighlightTiles();
                    }
                }
            }

            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPosition = new Vector2Int (currentlyDragging.currentX, currentlyDragging.currentY);

                if (ContainsValidMove(ref availableMoves, new Vector2Int(hitPosition.x, hitPosition.y)))
                {
                    MoveTo(previousPosition.x, previousPosition.y, hitPosition.x, hitPosition.y);

                    // Send move to the server
                    NetMakeMove mm = new NetMakeMove();
                    mm.originalX = previousPosition.x;
                    mm.originalY = previousPosition.y;
                    mm.destinationX = hitPosition.x;
                    mm.destinationY = hitPosition.y;
                    mm.teamId = currentTeam;
                    Client.Instance.SendToServer(mm);
                }
                else
                {
                    RemoveHighlightTiles();
                    currentlyDragging.SetPosition(GetCenterOfTile(previousPosition.x, previousPosition.y));
                }

                currentlyDragging = null;
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
    
    public void OnRematchButton()
    {
        if (localGame)
        {
            NetRematch wrm = new NetRematch();
            wrm.teamId = 0;
            wrm.wantRematch = 1;
            Client.Instance.SendToServer(wrm);

            NetRematch brm = new NetRematch();
            brm.teamId = 1;
            brm.wantRematch = 1;
            Client.Instance.SendToServer(brm);

            MenuUI.Instance.ChangeCamera(CameraAngle.whiteTeam);
            currentTeam = 0;
        }
        else
        {
            NetRematch rm = new NetRematch();
            rm.teamId = currentTeam;
            rm.wantRematch = 1;
            Client.Instance.SendToServer(rm);
        }
    }
    public void RestartGame()
    {
        // Reset UI 
        btnRematch.interactable = true;

        rematchIndicator.GetChild(0).gameObject.SetActive(false);
        rematchIndicator.GetChild(1).gameObject.SetActive(false);

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

        // Clear Variables (Field Reset)
        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();
        playerRematch[0] = playerRematch[1] = false;

        // Restart the Game
        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;
    }
    public void OnMenuButton()
    {
        NetRematch rm = new NetRematch();
        rm.teamId = currentTeam;
        rm.wantRematch = 0;
        Client.Instance.SendToServer(rm);

        RestartGame();
        MenuUI.Instance.OnLeaveFromGame();

        Invoke("ShutDownRelay", 0.1f);

        // Reset some values
        playerCount = -1;
        currentTeam = -1;

    }

    private void ShutDownRelay()
    {
        Client.Instance.Shutdown();
        Server.Instance.Shutdown();
    }

    // Preventing Self Check moves and checking Checkmate
    public void PreventCheck()
    {
        ChessPiece king = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    // Find the King of the team which is currently playing
                    if (chessPieces[x, y].type == TChessPiece.King && chessPieces[x, y].team == currentlyDragging.team)
                    {
                        king = chessPieces[x, y];
                    }
                }
            }
        }
        SimulateMoveForSinglePiece(currentlyDragging, ref availableMoves, king);
    }
    private void SimulateMoveForSinglePiece(ChessPiece cp, ref List<Vector2Int> moves, ChessPiece targetKing)
    {
        // Save current Positions and initialize variables
        int realX = cp.currentX;
        int realY = cp.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();
        Vector2Int kingPosition = new Vector2Int(targetKing.currentX, targetKing.currentY);
        
        // Simulate all the moves and check if the targetKing is checked

        foreach (Vector2Int move in moves)
        {
            // Create Simulation Board
            List<ChessPiece> enemyPieces = new List<ChessPiece>();
            ChessPiece[,] simBoard = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
            for (int x = 0; x < TILE_COUNT_X; x++)
            {
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    if (chessPieces[x, y] != null)
                    {
                        simBoard[x, y] = chessPieces[x, y];
                        if (simBoard[x, y].team != cp.team)
                        {
                            enemyPieces.Add(simBoard[x, y]);
                        }
                    }
                }
            }
            // Make the move in Simulation
            simBoard[cp.currentX, cp.currentY] = null;
            int simX = move.x;
            int simY = move.y;
            if (cp.type == TChessPiece.King)    // If the piece moved is the king change the King's position
                kingPosition = new Vector2Int(simX, simY);
            cp.currentX = simX;
            cp.currentY = simY;
            simBoard[cp.currentX, cp.currentY] = cp;
            // If cp defeat an enemy piece remove it from the list
            ChessPiece deadEnemyPiece = enemyPieces.Find(d => d.currentX == simX && d.currentY == simY);
            if (deadEnemyPiece != null)
                enemyPieces.Remove(deadEnemyPiece);

            // Simulate over all the enemy pieces' moves and check if these moves contain the King's Position
            foreach (ChessPiece enemy in enemyPieces)
            {
                List<Vector2Int> enemyMoves = enemy.GetAvailableMoves(ref simBoard, TILE_COUNT_X, TILE_COUNT_Y);

                if (enemyMoves.Contains(kingPosition))
                {
                    movesToRemove.Add(move);
                    break;
                }
            }

            cp.currentX = realX;
            cp.currentY = realY;
        }

        // Remove unvalid moves from moves list
        for (int i = 0; i < movesToRemove.Count; i++)
        {
            moves.Remove(movesToRemove[i]);
        }

    }
    private bool CheckForCheckMate()
    {
        ChessPiece targetKing = null;

        List<ChessPiece> defendingPieces = new List<ChessPiece>();
        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    if (chessPieces[x, y].team != ((isWhiteTurn) ? 0 : 1))
                    {
                        defendingPieces.Add(chessPieces[x, y]);
                        // Find the Defending King
                        if (chessPieces[x, y].type == TChessPiece.King)
                        {
                            targetKing = chessPieces[x, y];
                        }
                    }
                    else
                    {
                        attackingPieces.Add(chessPieces[x, y]);
                    }
                }
            }
        }

        bool isKingAtRisk = false;
        // Is the king attacked right now
        foreach (ChessPiece attPiece in attackingPieces)
        {
            List<Vector2Int> attackingAvailableMoves = attPiece.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            if (attackingAvailableMoves.Contains(new Vector2Int(targetKing.currentX, targetKing.currentY)))
            {
                isKingAtRisk = true;
                break;
            }
        }

        if (isKingAtRisk == true) // If king is at risk check for available moves
        {
            foreach (ChessPiece defPiece in defendingPieces)
            {
                List<Vector2Int> defendingAvailableMoves = defPiece.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);

                SimulateMoveForSinglePiece(defPiece, ref defendingAvailableMoves, targetKing);

                if (defendingAvailableMoves.Count > 0)  // If there is available Moves for defending team Not CheckMate
                {
                    return false;
                }
            }
        }
        else if (isKingAtRisk == false)   // Not CheckMate
        {
            return false;
        }

        return true;
    }
    
    // Special Move
    private void SpecialMoveProcesser()
    {
        if (specialMove == TSpecialMove.Promotion)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            if (lastMove[1].y == 0 || lastMove[1].y == 7)
            {
                ChessPiece thePawn = chessPieces[lastMove[1].x, lastMove[1].y];
                ChessPiece newQueen = SpawnSinglePiece(TChessPiece.Queen, thePawn.team);

                newQueen.transform.position = thePawn.transform.position;
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);
            }
        }
        
        if (specialMove == TSpecialMove.EnPassant)
        {
            Vector2Int[] allyPawnMove = moveList[moveList.Count - 1];
            Vector2Int[] enemyPawnMove = moveList[moveList.Count - 2];

            if (allyPawnMove[1].x == enemyPawnMove[1].x)
            {
                if (Mathf.Abs(allyPawnMove[1].y - enemyPawnMove[1].y) == 1)
                {
                    KillPiece(chessPieces[enemyPawnMove[1].x, enemyPawnMove[1].y]);
                }
            }
        }

        if (specialMove == TSpecialMove.Castling)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];

            // Left Castling
            if (lastMove[1].x == 2)
            {
                ChessPiece leftRook = chessPieces[0, lastMove[1].y];
                chessPieces[0, lastMove[1].y] = null;
                chessPieces[3, lastMove[1].y] = leftRook;
                PositionSinglePiece(3, lastMove[1].y);
            }
            // Right Castling
            if (lastMove[1].x == 6)
            {
                ChessPiece rightRook = chessPieces[7, lastMove[1].y];
                chessPieces[7, lastMove[1].y] = null;
                chessPieces[5, lastMove[1].y] = rightRook;
                PositionSinglePiece(5, lastMove[1].y);
            }
        }

    }


    // Operations
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2Int pos)
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;

        return false;
    } 
    private Vector3 GetCenterOfTile(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
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
    private void MoveTo(int originalX, int originalY, int x, int y)
    {

        Vector2Int previousPosition = new Vector2Int(originalX, originalY);
        ChessPiece cp = chessPieces[originalX, originalY];

        // If there is a piece on the tile
        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];

            if (cp.team == ocp.team)    // If not enemy can't it is an invalid Move
                return;
            else                        // If an enemy piece kill
                KillPiece(ocp);
        }

        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) });
        PositionSinglePiece(x, y);
        SpecialMoveProcesser();

        if (CheckForCheckMate())
            CheckMate(cp.team);

        isWhiteTurn = !isWhiteTurn;
        if (localGame)
        {
            currentTeam = (currentTeam == 0) ? 1 : 0;
            MenuUI.Instance.ChangeCamera((currentTeam == 0) ? CameraAngle.whiteTeam : CameraAngle.blackTeam);
        }

        RemoveHighlightTiles();
        return;
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
            deadWhites.Add(cp);
            cp.SetScale(Vector3.one * deathSize);
            cp.SetPosition(GetCenterOfTile(8, -1) +
                new Vector3(0, 0, deadWhites.Count * deathSpacing));
        }
        else                   // Defeated piece is Black
        {
            deadBlacks.Add(cp);
            cp.SetScale(Vector3.one * deathSize);
            cp.SetPosition(GetCenterOfTile(-1, 8) -
                new Vector3(0, 0, deadBlacks.Count * deathSpacing));
        }
    }

    #region
    private void RegisterEvents()
    {
        NetUtility.S_WELCOME += OnWelcomeServer;
        NetUtility.S_MAKE_MOVE += OnMakeMoveServer;
        NetUtility.S_REMATCH += OnRematchServer;
        

        NetUtility.C_WELCOME += OnWelcomeClient;
        NetUtility.C_START_GAME += OnStartGameClient;
        NetUtility.C_MAKE_MOVE += OnMakeMoveClient;
        NetUtility.C_REMATCH += OnRematchClient;

        MenuUI.Instance.SetLocalGame += OnSetLocalGame;
    }


    private void UnRegisterEvents()
    {
        NetUtility.S_WELCOME -= OnWelcomeServer;
        NetUtility.S_MAKE_MOVE -= OnMakeMoveServer;
        NetUtility.S_REMATCH -= OnRematchServer;


        NetUtility.C_WELCOME -= OnWelcomeClient;
        NetUtility.C_START_GAME -= OnStartGameClient;
        NetUtility.C_MAKE_MOVE -= OnMakeMoveClient;
        NetUtility.C_REMATCH -= OnRematchClient;

        MenuUI.Instance.SetLocalGame -= OnSetLocalGame;
    }

    // Server
    private void OnWelcomeServer(NetMessage msg, NetworkConnection cnn)
    {

        // Client has connected, assign a team and return the message back to him
        NetWelcome nw = msg as NetWelcome;

        // Assigne a team
        nw.AssignedTeam = ++playerCount; 

        // Return back to the client
        Server.Instance.SendToClient(cnn, nw);


        // If full, start the game
        if (playerCount == 1)
            Server.Instance.Broadcast(new NetStartGame());

    }
    private void OnMakeMoveServer(NetMessage msg, NetworkConnection cnn)
    {
        Server.Instance.Broadcast(msg);
    }
    private void OnRematchServer(NetMessage msg, NetworkConnection cnn)
    {
        Server.Instance.Broadcast(msg);
    }

    // Client
    private void OnWelcomeClient(NetMessage msg)
    {
        // Receive the connection message
        NetWelcome nw = msg as NetWelcome;

        // Assign the team
        currentTeam = nw.AssignedTeam;
        Debug.Log("Current Team is: " + currentTeam);

        if (localGame == true && currentTeam == 0)
            Server.Instance.Broadcast(new NetStartGame());
    }
    private void OnStartGameClient(NetMessage msg)
    {
        // We just need to change the camera bec game is already working in bg
        MenuUI.Instance.ChangeCamera((currentTeam == 0) ? CameraAngle.whiteTeam : CameraAngle.blackTeam);
    }
    private void OnMakeMoveClient(NetMessage msg)
    {
        NetMakeMove mm = msg as NetMakeMove;

        if (mm.teamId != currentTeam)
        {
            ChessPiece cp = chessPieces[mm.originalX, mm.originalY];
            availableMoves = cp.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            specialMove = cp.GetSpacialMove(ref chessPieces, ref moveList, ref availableMoves);

            MoveTo(mm.originalX, mm.originalY, mm.destinationX, mm.destinationY);
        }

    }
    private void OnRematchClient(NetMessage msg)
    {
        NetRematch rm = msg as NetRematch;

        // Set boolean for rematch
        playerRematch[rm.teamId] = rm.wantRematch == 1;

        // Activate the piece of UI on the opponent player
        if (rm.teamId != currentTeam)
        {
            rematchIndicator.GetChild((rm.wantRematch == 1) ? 0 : 1).gameObject.SetActive(true);
            if (rm.wantRematch != 1)
            {
                btnRematch.interactable = false;
            }
        }

        // If both players wants rematch restart game
        if (playerRematch[0] && playerRematch[1])
            RestartGame();


    }



    //
    private void OnSetLocalGame(bool isLocalGame)
    {
        playerCount = -1;
        currentTeam = -1;
        localGame = isLocalGame;
    }

    #endregion

}
