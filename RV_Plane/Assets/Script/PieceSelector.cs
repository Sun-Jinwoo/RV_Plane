using UnityEngine;

public class PieceSelector : MonoBehaviour
{
    public BoardManager boardManager;
    private Camera arCamera;

    void Start() => arCamera = Camera.main;

    void Update()
    {
        if (Input.touchCount == 0) return;
        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        Ray ray = arCamera.ScreenPointToRay(touch.position);

        // ¿Tocó una pieza?
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            ChessPiece piece = hit.collider.GetComponentInParent<ChessPiece>();
            if (piece != null && piece.isWhite == GameState.Instance.isWhiteTurn)
            {
                GameState.Instance.selectedPiece = piece;
                HighlightPiece(piece);
                return;
            }
        }

        // ¿Tocó una celda destino?
        if (GameState.Instance.selectedPiece != null)
        {
            Plane boardPlane = new Plane(boardManager.boardOrigin.up,
                                         boardManager.boardOrigin.position);
            if (boardPlane.Raycast(ray, out float dist))
            {
                Vector3 hitPoint = ray.GetPoint(dist);
                if (boardManager.GetBoardPosition(hitPoint, out int col, out int row))
                {
                    TryMovePiece(GameState.Instance.selectedPiece, col, row, hitPoint);
                }
            }
        }
    }

    void TryMovePiece(ChessPiece piece, int col, int row, Vector3 worldPos)
    {
        // Aquí conectas con MoveValidator
        Vector3 snappedPos = boardManager.GetWorldPosition(col, row);
        snappedPos.y = piece.transform.position.y;  // mantiene la altura
        piece.MoveTo(col, row, snappedPos);
        GameState.Instance.selectedPiece = null;
        GameState.Instance.SwitchTurn();
    }

    void HighlightPiece(ChessPiece piece)
    {
        // Opcional: escala la pieza para indicar selección
        piece.transform.localScale *= 1.2f;
    }
}