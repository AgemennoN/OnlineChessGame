using System.Collections.Generic;
using UnityEngine;

public class Rook : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        List<Vector2Int> directionList = new List<Vector2Int>()
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

        foreach (Vector2Int direction in directionList)
        {
            int distance = 1;
            int checkTileX = currentX + (direction.x * distance);
            int checkTileY = currentY + (direction.y * distance);

            // If the tile in the direction is in the boundaries increase check distance in the direction
            while (checkTileX < tileCountX && checkTileX >= 0 && checkTileY < tileCountY && checkTileY >= 0)
            {
                // If the tile is empty, it is available to move
                if (board[checkTileX, checkTileY] == null)
                {
                    r.Add(new Vector2Int(checkTileX, checkTileY));
                }
                // If the tile is NOT empty
                else
                {
                    // If the chess piece on the tile is an enemy piece the tile is available
                    if (board[checkTileX, checkTileY].team != team)
                        r.Add(new Vector2Int(checkTileX, checkTileY));

                    break;
                }
                distance++;
                checkTileX = currentX + (direction.x * distance);
                checkTileY = currentY + (direction.y * distance);
            }
        }
        return r;
    }

}
