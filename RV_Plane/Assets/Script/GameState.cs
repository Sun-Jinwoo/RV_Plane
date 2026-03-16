using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState Instance;
    public bool isWhiteTurn = true;
    public ChessPiece selectedPiece;
    public ChessPiece[,] board = new ChessPiece[8, 8];

    void Awake() => Instance = this;

    public void SwitchTurn() => isWhiteTurn = !isWhiteTurn;
}