using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1;

        // One Tile Front Move
        if (board[currentX, currentY + direction] == null)
        {
            r.Add(new Vector2Int(currentX, currentY + direction));
            
            // White Two Tile Front Move
            if (team == 0 && currentY == 1 && board[currentX, currentY + (direction * 2)] == null)
                r.Add(new Vector2Int(currentX, currentY + (direction * 2)));
            // Black Two Tile Front Move
            if (team == 1 && currentY == 6 && board[currentX, currentY + (direction * 2)] == null)
                r.Add(new Vector2Int(currentX, currentY + (direction * 2)));
        }

        // Diagonal Kill Move
        if (currentX != 7) // If NOT on the right most tile 
        {
            // if There is a chess piece on the diagonal and it is an enemy piece
            if (board[currentX + 1, currentY + direction] != null && board[currentX + 1, currentY + direction].team != team)
                r.Add(new Vector2Int(currentX + 1, currentY + direction));
        }
        if (currentX != 0) // If NOT on the left most tile 
        {
            // if There is a chess piece on the diagonal and it is an enemy piece
            if (board[currentX - 1, currentY + direction] != null && board[currentX - 1, currentY + direction].team != team)
                r.Add(new Vector2Int(currentX - 1, currentY + direction));
        }

        return r;
    }

    public override TSpecialMove GetSpacialMove(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        int direction = (team == 0) ? 1 : -1;
        int readyToPromoteTile = (team == 0) ? 6 : 1;

        // Promoting
        if (currentY == readyToPromoteTile)
        {
            return TSpecialMove.Promotion;
        }

        // EnPassant
        if (moveList.Count > 0)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            
            if (board[lastMove[1].x, lastMove[1].y].type == TChessPiece.Pawn) // If the last move belongs to a pawn
            {
                if (Mathf.Abs(lastMove[1].y - lastMove[0].y) == 2) // If the pawn move was a double front
                {
                    if (lastMove[1].y == currentY) // If this pawn and the enemy pawn are on the same y tiles
                    {
                        if (lastMove[1].x - 1 == currentX || lastMove[1].x + 1 == currentX) // If the pawns are near each others 
                        {
                            availableMoves.Add(new Vector2Int(lastMove[1].x, lastMove[1].y + direction));
                            return TSpecialMove.EnPassant;
                        }
                    }
                }
            }
        }

        // Promotion

        return TSpecialMove.None;
    }

}
