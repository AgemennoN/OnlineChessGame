using System.Collections.Generic;
using UnityEngine;

public class Knight : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        List<Vector2Int> directionList = new List<Vector2Int>()
        {
            new Vector2Int(1, 2),
            new Vector2Int(2, 1),
            new Vector2Int(2, -1),
            new Vector2Int(1, -2),
            new Vector2Int(-1, -2),
            new Vector2Int(-2, -1),
            new Vector2Int(-2, 1),
            new Vector2Int(-1, 2)
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
}
