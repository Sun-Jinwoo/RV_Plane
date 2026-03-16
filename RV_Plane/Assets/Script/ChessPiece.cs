using UnityEngine;

public enum PieceType { King, Queen, Rook, Bishop, Knight, Pawn }

public class ChessPiece : MonoBehaviour
{
    public PieceType type;
    public bool isWhite;
    public int col, row;   // posición lógica en el tablero (0-7)

    public void MoveTo(int newCol, int newRow, Vector3 worldPos)
    {
        // Actualiza lógica
        GameState.Instance.board[col, row] = null;
        col = newCol;
        row = newRow;
        GameState.Instance.board[col, row] = this;

        // Mueve en el mundo AR con una animación suave
        StartCoroutine(SmoothMove(worldPos));
    }

    System.Collections.IEnumerator SmoothMove(Vector3 target)
    {
        float t = 0f;
        Vector3 start = transform.position;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
        transform.position = target;
    }
}