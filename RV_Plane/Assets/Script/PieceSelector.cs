using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Selección y movimiento de piezas con AR Foundation + New Input System.
/// Usa ARRaycastManager para detectar el plano del tablero con precisión.
/// </summary>
public class PieceSelector : MonoBehaviour
{
    public static PieceSelector Instance;

    [Header("Referencias")]
    public BoardManager boardManager;
    public ARRaycastManager arRaycastManager;
    public ARPlacementController placementController;

    private Camera arCamera;
    private List<Vector2Int> currentValidMoves = new();
    private static readonly List<ARRaycastHit> arHits = new();

    void Awake() => Instance = this;

    void Start()
    {
        arCamera = Camera.main;
        EnhancedTouchSupport.Enable();
    }

    void OnDestroy() => EnhancedTouchSupport.Disable();

    // ─────────────────────────────────────────────
    //  INPUT
    // ─────────────────────────────────────────────

    void Update()
    {
        // Solo activo cuando el tablero ya está colocado y el juego en curso
        if (!placementController.boardPlaced) return;
        if (GameState.Instance.result != GameState.GameResult.InProgress) return;

#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            HandleInput(Mouse.current.position.ReadValue());
#else
        if (Touch.activeTouches.Count > 0 &&
            Touch.activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Began)
            HandleInput(Touch.activeTouches[0].screenPosition);
#endif
    }

    // ─────────────────────────────────────────────
    //  LÓGICA DE SELECCIÓN
    // ─────────────────────────────────────────────

    private void HandleInput(Vector2 screenPos)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);

        // ── 1. ¿Tocó una pieza? (Raycast físico contra colliders) ───
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            ChessPiece tappedPiece = hit.collider.GetComponentInParent<ChessPiece>();

            if (tappedPiece != null)
            {
                bool isFriendly = tappedPiece.isWhite == GameState.Instance.isWhiteTurn;

                if (isFriendly)
                {
                    SelectPiece(tappedPiece);
                    return;
                }

                if (GameState.Instance.selectedPiece != null)
                {
                    var dest = new Vector2Int(tappedPiece.col, tappedPiece.row);
                    if (currentValidMoves.Contains(dest))
                    {
                        ConfirmMove(dest);
                        return;
                    }
                }

                Deselect();
                return;
            }
        }

        // ── 2. ¿Tocó el tablero? (Raycast AR contra el plano) ───────
        if (GameState.Instance.selectedPiece != null)
        {
            // Primero intentamos con ARRaycastManager (más preciso en AR)
            if (arRaycastManager.Raycast(screenPos, arHits, TrackableType.PlaneWithinPolygon))
            {
                Vector3 hitPoint = arHits[0].pose.position;
                TryMoveToPoint(hitPoint);
                return;
            }

            // Fallback: Raycast contra el plano matemático del tablero
            Plane boardPlane = new Plane(
                boardManager.boardOrigin.up,
                boardManager.boardOrigin.position);

            if (boardPlane.Raycast(ray, out float dist))
                TryMoveToPoint(ray.GetPoint(dist));
        }
    }

    private void TryMoveToPoint(Vector3 worldPoint)
    {
        if (boardManager.GetBoardPosition(worldPoint, out int col, out int row))
        {
            var dest = new Vector2Int(col, row);
            if (currentValidMoves.Contains(dest))
                ConfirmMove(dest);
            else
                Deselect();
        }
    }

    // ─────────────────────────────────────────────
    //  SELECT / DESELECT
    // ─────────────────────────────────────────────

    private void SelectPiece(ChessPiece piece)
    {
        GameState.Instance.selectedPiece?.SetSelected(false);
        GameState.Instance.selectedPiece = piece;
        piece.SetSelected(true);

        currentValidMoves = MoveValidator.Instance.GetValidMoves(piece);
        TileHighlighter.Instance.ShowHighlights(piece, currentValidMoves);

        Debug.Log($"[PieceSelector] {piece.type} ({piece.col},{piece.row}) | Movimientos: {currentValidMoves.Count}");
    }

    private void Deselect()
    {
        GameState.Instance.selectedPiece?.SetSelected(false);
        GameState.Instance.selectedPiece = null;
        currentValidMoves.Clear();
        TileHighlighter.Instance?.ClearHighlights();
    }

    // ─────────────────────────────────────────────
    //  CONFIRMAR MOVIMIENTO
    // ─────────────────────────────────────────────

    private void ConfirmMove(Vector2Int dest)
    {
        ChessPiece piece = GameState.Instance.selectedPiece;
        piece.SetSelected(false);

        Vector3 worldPos = boardManager.GetWorldPosition(dest.x, dest.y);
        worldPos.y = piece.transform.position.y;
        piece.MoveTo(dest.x, dest.y, worldPos);

        GameState.Instance.ExecuteMove(piece, dest.x, dest.y);
        currentValidMoves.Clear();

        Debug.Log($"[PieceSelector] {piece.type} → ({dest.x},{dest.y})");
    }
}