using System.Collections.Generic;
using UnityEngine;

public class King : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        List<Vector2Int> directionList = new List<Vector2Int>()
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(1, 0),
            new Vector2Int(1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(-1, 1)
        };

        foreach (Vector2Int direction in directionList)
        {
            int checkTileX = currentX + direction.x;
            int checkTileY = currentY + direction.y;

            // Checks if the tile is in the boundaries
            if (checkTileX < tileCountX && checkTileX >= 0 && checkTileY < tileCountY && checkTileY >= 0)
            {
                // If the tile is empty, it is available to move
                if (board[checkTileX, checkTileY] == null)
                {
                    r.Add(new Vector2Int(checkTileX, checkTileY));
                }
                // If the tile is NOT empty
                else
                {
                    // If the chess piece on the tile is an enemy piece, it is available to move
                    if (board[checkTileX, checkTileY].team != team)
                        r.Add(new Vector2Int(checkTileX, checkTileY));
                }
            }
        }
        return r;
    }

    public override TSpecialMove GetSpacialMove(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        TSpecialMove r = TSpecialMove.None;

        Vector2Int[] kingMove = moveList.Find(m => m[0].x == 4 && m[0].y == ((team == 0) ? 0 : 7));
        Vector2Int[] leftRookMove = moveList.Find(m => m[0].x == 0 && m[0].y == ((team == 0) ? 0 : 7));
        Vector2Int[] rightRookMove = moveList.Find(m => m[0].x == 7 && m[0].y == ((team == 0) ? 0 : 7));

        if (kingMove == null)
        {
            // Left Castle Move
            if (leftRookMove == null)
                if (board[0, currentY].type == TChessPiece.Rook && board[0, currentY].team == team)
                    if (board[3, currentY] == null)
                        if (board[2, currentY] == null)
                            if (board[1, currentY] == null)
                            {
                                availableMoves.Add(new Vector2Int(2, currentY));
                                r = TSpecialMove.Castling;
                            }
            // Right Castle Move
            if (rightRookMove == null)
                if (board[7, currentY].type == TChessPiece.Rook && board[7, currentY].team == team)
                    if (board[5, currentY] == null)
                        if (board[6, currentY] == null)
                        {
                            availableMoves.Add(new Vector2Int(6, currentY));
                            r = TSpecialMove.Castling;
                        }
        }
        return r;
    }
}
