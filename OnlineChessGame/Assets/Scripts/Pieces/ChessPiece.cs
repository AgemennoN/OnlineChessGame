using UnityEngine;

public enum TChessPiece
{
    Pawn = 0,
    Rook = 1,
    Knight = 2,
    Bishop = 3,
    Queen = 4,
    King = 5
}

public class ChessPiece : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public TChessPiece type;

    private Vector3 desiredPosition;
    private Vector3 desiredScale;
}
