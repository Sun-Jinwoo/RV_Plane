using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maneja la selección y movimiento de piezas mediante toque en pantalla.
/// Integra MoveValidator, TileHighlighter y GameState.ExecuteMove.
/// 
/// SETUP:
///   - Adjunta a un GameObject vacío "Managers"
///   - Asigna boardManager en el Inspector
///   - Las piezas deben tener Collider (MeshCollider o BoxCollider)
///   - El Layer "Board" (opcional) permite filtrar el raycast al tablero
/// </summary>
public class PieceSelector : MonoBehaviour
{
    public static PieceSelector Instance;

    [Header("Referencias")]
    public BoardManager boardManager;

    private Camera arCamera;
    private List<Vector2Int> currentValidMoves = new();

    void Awake() => Instance = this;

    void Start() => arCamera = Camera.main;

    // ─────────────────────────────────────────────
    //  INPUT
    // ─────────────────────────────────────────────

    void Update()
    {
        if (GameState.Instance.result != GameState.GameResult.InProgress) return;

        // Editor / PC: clic de ratón
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
            HandleInput(Input.mousePosition);
#else
        // Dispositivo móvil: primer toque
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            HandleInput(Input.GetTouch(0).position);
#endif
    }

    // ─────────────────────────────────────────────
    //  LÓGICA DE SELECCIÓN Y MOVIMIENTO
    // ─────────────────────────────────────────────

    private void HandleInput(Vector2 screenPos)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);

        // ── 1. ¿Tocó una pieza? ──────────────────────
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            ChessPiece tappedPiece = hit.collider.GetComponentInParent<ChessPiece>();

            if (tappedPiece != null)
            {
                bool isFriendly = tappedPiece.isWhite == GameState.Instance.isWhiteTurn;

                // Tocó una pieza propia → seleccionar
                if (isFriendly)
                {
                    SelectPiece(tappedPiece);
                    return;
                }

                // Tocó una pieza enemiga con pieza seleccionada → intentar captura
                if (GameState.Instance.selectedPiece != null)
                {
                    var dest = new Vector2Int(tappedPiece.col, tappedPiece.row);
                    if (currentValidMoves.Contains(dest))
                    {
                        ConfirmMove(dest);
                        return;
                    }
                }

                // Deseleccionar si toca una pieza enemiga sin movimiento posible
                Deselect();
                return;
            }
        }

        // ── 2. ¿Tocó el plano del tablero? ───────────
        if (GameState.Instance.selectedPiece != null)
        {
            Plane boardPlane = new Plane(
                boardManager.boardOrigin.up,
                boardManager.boardOrigin.position);

            if (boardPlane.Raycast(ray, out float dist))
            {
                Vector3 hitPoint = ray.GetPoint(dist);

                if (boardManager.GetBoardPosition(hitPoint, out int col, out int row))
                {
                    var dest = new Vector2Int(col, row);
                    if (currentValidMoves.Contains(dest))
                        ConfirmMove(dest);
                    else
                        Deselect();
                }
            }
        }
    }

    // ─────────────────────────────────────────────
    //  SELECT / DESELECT
    // ─────────────────────────────────────────────

    private void SelectPiece(ChessPiece piece)
    {
        // Deselecciona la anterior
        if (GameState.Instance.selectedPiece != null)
            GameState.Instance.selectedPiece.SetSelected(false);

        GameState.Instance.selectedPiece = piece;
        piece.SetSelected(true);

        // Calcula movimientos válidos y los muestra
        currentValidMoves = MoveValidator.Instance.GetValidMoves(piece);
        TileHighlighter.Instance.ShowHighlights(piece, currentValidMoves);

        Debug.Log($"[PieceSelector] Seleccionada: {piece.type} en ({piece.col},{piece.row}) " +
                  $"| Movimientos válidos: {currentValidMoves.Count}");
    }

    private void Deselect()
    {
        if (GameState.Instance.selectedPiece != null)
        {
            GameState.Instance.selectedPiece.SetSelected(false);
            GameState.Instance.selectedPiece = null;
        }
        currentValidMoves.Clear();
        TileHighlighter.Instance.ClearHighlights();
    }

    // ─────────────────────────────────────────────
    //  EJECUTAR MOVIMIENTO
    // ─────────────────────────────────────────────

    private void ConfirmMove(Vector2Int dest)
    {
        ChessPiece piece = GameState.Instance.selectedPiece;
        piece.SetSelected(false);

        // Mueve el modelo 3D al centro de la celda destino
        Vector3 worldPos = boardManager.GetWorldPosition(dest.x, dest.y);
        worldPos.y = piece.transform.position.y;
        piece.MoveTo(dest.x, dest.y, worldPos);

        // Actualiza el estado lógico, verifica jaque/mate, cambia turno
        GameState.Instance.ExecuteMove(piece, dest.x, dest.y);

        currentValidMoves.Clear();

        Debug.Log($"[PieceSelector] Movimiento: {piece.type} → ({dest.x},{dest.y}) " +
                  $"| Turno: {(GameState.Instance.isWhiteTurn ? "Blancas" : "Negras")}");
    }
}
