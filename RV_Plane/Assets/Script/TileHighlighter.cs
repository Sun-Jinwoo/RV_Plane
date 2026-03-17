using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Instancia quads semitransparentes sobre las celdas válidas al seleccionar una pieza.
/// 
/// SETUP en Inspector:
///   - validMoveTile   → Prefab quad verde semitransparente
///   - captureTile     → Prefab quad rojo semitransparente
///   - selectedTile    → Prefab quad amarillo semitransparente
///   - checkTile       → Prefab quad naranja semitransparente (rey en jaque)
///   - boardManager    → Referencia al BoardManager
///
/// CÓMO CREAR LOS PREFABS DE TILE:
///   1. GameObject → 3D Object → Quad
///   2. Rotar X: 90 para que quede plano sobre el tablero
///   3. Crear un Material con Shader "Universal Render Pipeline/Lit"
///      o "Standard", modo Transparent
///   4. Ajustar color con alpha ~100-140
///   5. Escalar el Quad al tamaño de una celda (ej. 0.095, 1, 0.095)
///   6. Guardar como Prefab en Assets/Prefabs/Tiles/
/// </summary>
public class TileHighlighter : MonoBehaviour
{
    public static TileHighlighter Instance;

    [Header("Prefabs de tiles (quads semitransparentes)")]
    public GameObject validMoveTile;   // verde
    public GameObject captureTile;     // rojo
    public GameObject selectedTile;    // amarillo
    public GameObject checkTile;       // naranja

    [Header("Referencias")]
    public BoardManager boardManager;

    [Header("Altura sobre el tablero")]
    [Tooltip("Offset Y para que el quad flote ligeramente sobre la superficie")]
    public float tileHeightOffset = 0.001f;

    // Pool de tiles activos
    private readonly List<GameObject> activeTiles = new();

    void Awake() => Instance = this;

    // ─────────────────────────────────────────────
    //  API PÚBLICA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Muestra resaltados para la pieza seleccionada:
    ///   - amarillo en su celda actual
    ///   - verde en movimientos válidos
    ///   - rojo en capturas posibles
    /// </summary>
    public void ShowHighlights(ChessPiece piece, List<Vector2Int> validMoves)
    {
        ClearHighlights();

        // Celda de la pieza seleccionada
        SpawnTile(selectedTile, piece.col, piece.row);

        // Movimientos válidos y capturas
        foreach (var move in validMoves)
        {
            ChessPiece target = MoveValidator.Instance.GetPieceAt(move);
            bool isCapture = target != null && target.isWhite != piece.isWhite;
            SpawnTile(isCapture ? captureTile : validMoveTile, move.x, move.y);
        }

        // Jaque: resalta el rey del bando en turno si está en jaque
        ShowCheckHighlight();
    }

    /// <summary>Elimina todos los tiles activos.</summary>
    public void ClearHighlights()
    {
        foreach (var tile in activeTiles)
            if (tile != null) Destroy(tile);
        activeTiles.Clear();
    }

    /// <summary>Resalta el rey si está en jaque (llamado al final del turno).</summary>
    public void ShowCheckHighlight()
    {
        bool currentTurn = GameState.Instance.isWhiteTurn;
        if (!MoveValidator.Instance.IsInCheck(currentTurn)) return;

        // Buscar el rey
        for (int c = 0; c < 8; c++)
            for (int r = 0; r < 8; r++)
            {
                ChessPiece p = GameState.Instance.board[c, r];
                if (p != null && p.isWhite == currentTurn && p.type == PieceType.King)
                {
                    SpawnTile(checkTile, c, r);
                    return;
                }
            }
    }

    // ─────────────────────────────────────────────
    //  INTERNAL
    // ─────────────────────────────────────────────

    private void SpawnTile(GameObject prefab, int col, int row)
    {
        if (prefab == null) return;

        Vector3 worldPos = boardManager.GetWorldPosition(col, row);
        worldPos.y += tileHeightOffset;

        GameObject tile = Instantiate(prefab, worldPos, Quaternion.identity,
                                       boardManager.boardOrigin);
        activeTiles.Add(tile);
    }
}
