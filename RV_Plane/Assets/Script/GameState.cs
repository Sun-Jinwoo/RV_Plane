using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estado global de la partida.
/// Centraliza el tablero lógico, el turno, piezas capturadas,
/// variables especiales (en passant) y el resultado final.
/// </summary>
public class GameState : MonoBehaviour
{
    public static GameState Instance;

    // ─────────────────────────────────────────────
    //  ESTADO DEL JUEGO
    // ─────────────────────────────────────────────

    public bool isWhiteTurn = true;
    public ChessPiece selectedPiece;
    public ChessPiece[,] board = new ChessPiece[8, 8];

    /// <summary>
    /// Celda de en passant activa (puede ser null).
    /// Se establece cuando un peón avanza dos casillas.
    /// </summary>
    public Vector2Int? enPassantTarget = null;

    public enum GameResult { InProgress, WhiteWins, BlackWins, Stalemate }
    public GameResult result = GameResult.InProgress;

    // ─────────────────────────────────────────────
    //  EVENTOS (suscríbete desde la UI)
    // ─────────────────────────────────────────────

    public delegate void OnTurnChanged(bool isWhiteTurn);
    public event OnTurnChanged TurnChanged;

    public delegate void OnCheckDetected(bool isWhiteKingInCheck);
    public event OnCheckDetected CheckDetected;

    public delegate void OnGameOver(GameResult result);
    public event OnGameOver GameOver;

    public delegate void OnPawnPromotion(ChessPiece pawn);
    public event OnPawnPromotion PawnPromotion;

    // ─────────────────────────────────────────────
    //  UNITY
    // ─────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        InitBoard();
    }

    // ─────────────────────────────────────────────
    //  INICIALIZACIÓN
    // ─────────────────────────────────────────────

    /// <summary>
    /// Registra las piezas en el tablero lógico leyendo las posiciones
    /// iniciales de los ChessPiece hijos del objeto Pieces.
    /// Llama esto después de que las piezas estén instanciadas en la escena.
    /// </summary>
    public void InitBoard()
    {
        board = new ChessPiece[8, 8];
        foreach (ChessPiece piece in AllPiecesInScene())
        {
            if (IsInBoard(piece.col, piece.row))
                board[piece.col, piece.row] = piece;
        }
    }

    // ─────────────────────────────────────────────
    //  LÓGICA DE TURNO Y MOVIMIENTO
    // ─────────────────────────────────────────────

    /// <summary>
    /// Ejecuta el movimiento de una pieza hacia (destCol, destRow).
    /// Aplica reglas especiales: captura, enroque, en passant, promoción.
    /// Comprueba jaque mate y tablas al final.
    /// </summary>
    public void ExecuteMove(ChessPiece piece, int destCol, int destRow)
    {
        enPassantTarget = null; // resetear antes de cada movimiento

        // ── Captura normal ──────────────────────────
        ChessPiece captured = board[destCol, destRow];
        if (captured != null)
            RemovePiece(captured);

        // ── En passant ──────────────────────────────
        if (piece.type == PieceType.Pawn)
        {
            int dir = piece.isWhite ? 1 : -1;

            // Captura en passant
            if (destCol != piece.col && captured == null)
            {
                ChessPiece epPawn = board[destCol, destRow - dir];
                if (epPawn != null) RemovePiece(epPawn);
            }

            // Establecer target de en passant si avanzó 2
            if (Mathf.Abs(destRow - piece.row) == 2)
                enPassantTarget = new Vector2Int(destCol, piece.row + dir);
        }

        // ── Enroque ─────────────────────────────────
        if (piece.type == PieceType.King)
        {
            int colDiff = destCol - piece.col;
            if (Mathf.Abs(colDiff) == 2)
                ExecuteCastling(piece, destCol);
        }

        // ── Mover la pieza en el tablero lógico ─────
        board[piece.col, piece.row] = null;
        board[destCol, destRow] = piece;
        piece.col = destCol;
        piece.row = destRow;
        piece.hasMoved = true;

        // ── Promoción de peón ────────────────────────
        if (piece.type == PieceType.Pawn)
        {
            int lastRow = piece.isWhite ? 7 : 0;
            if (piece.row == lastRow)
                PawnPromotion?.Invoke(piece);
        }

        // ── Limpiar selección y UI ───────────────────
        TileHighlighter.Instance?.ClearHighlights();
        selectedPiece = null;

        // ── Cambiar turno ────────────────────────────
        isWhiteTurn = !isWhiteTurn;
        TurnChanged?.Invoke(isWhiteTurn);

        // ── Jaque, jaque mate y tablas ───────────────
        if (MoveValidator.Instance.IsInCheck(isWhiteTurn))
        {
            CheckDetected?.Invoke(isWhiteTurn);
            TileHighlighter.Instance?.ShowCheckHighlight();

            if (MoveValidator.Instance.IsCheckmate(isWhiteTurn))
            {
                result = isWhiteTurn ? GameResult.BlackWins : GameResult.WhiteWins;
                GameOver?.Invoke(result);
                return;
            }
        }
        else if (MoveValidator.Instance.IsStalemate(isWhiteTurn))
        {
            result = GameResult.Stalemate;
            GameOver?.Invoke(result);
        }
    }

    /// <summary>Promociona el peón al tipo elegido (llamado desde la UI).</summary>
    public void PromotePawn(ChessPiece pawn, PieceType newType)
    {
        pawn.type = newType;
        // Aquí puedes también intercambiar el modelo 3D si tienes prefabs por tipo
    }

    // ─────────────────────────────────────────────
    //  ENROQUE INTERNO
    // ─────────────────────────────────────────────

    private void ExecuteCastling(ChessPiece king, int destCol)
    {
        int row = king.row;
        bool kingSide = destCol == 6;

        int rookFromCol = kingSide ? 7 : 0;
        int rookToCol   = kingSide ? 5 : 3;

        ChessPiece rook = board[rookFromCol, row];
        if (rook == null) return;

        board[rookFromCol, row] = null;
        board[rookToCol, row] = rook;
        rook.col = rookToCol;
        rook.hasMoved = true;

        // Mueve el modelo 3D de la torre
        Vector3 rookWorldPos = BoardManager.Instance.GetWorldPosition(rookToCol, row);
        rookWorldPos.y = rook.transform.position.y;
        rook.MoveTo(rookToCol, row, rookWorldPos);
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────

    private void RemovePiece(ChessPiece piece)
    {
        board[piece.col, piece.row] = null;
        piece.gameObject.SetActive(false);
    }

    public void SwitchTurn()
    {
        isWhiteTurn = !isWhiteTurn;
        TurnChanged?.Invoke(isWhiteTurn);
    }

    /// <summary>Itera sobre todas las piezas vivas del tablero lógico.</summary>
    public IEnumerable<ChessPiece> AllPieces()
    {
        for (int c = 0; c < 8; c++)
            for (int r = 0; r < 8; r++)
                if (board[c, r] != null)
                    yield return board[c, r];
    }

    /// <summary>Lee las piezas directamente de la escena (para inicializar).</summary>
    private IEnumerable<ChessPiece> AllPiecesInScene()
        => FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);

    private bool IsInBoard(int c, int r) => c >= 0 && c < 8 && r >= 0 && r < 8;
}
