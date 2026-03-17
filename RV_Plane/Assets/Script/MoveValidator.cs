using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Valida movimientos de ajedrez para cada tipo de pieza.
/// Incluye: movimiento básico, capturas, enroque, en passant y promoción de peón.
/// </summary>
public class MoveValidator : MonoBehaviour
{
    public static MoveValidator Instance;

    void Awake() => Instance = this;

    // ─────────────────────────────────────────────
    //  PUNTO DE ENTRADA PRINCIPAL
    // ─────────────────────────────────────────────

    /// <summary>
    /// Devuelve todas las celdas a las que puede moverse la pieza dada.
    /// Ya filtra movimientos que dejarían al rey propio en jaque.
    /// </summary>
    public List<Vector2Int> GetValidMoves(ChessPiece piece)
    {
        List<Vector2Int> candidates = GetCandidateMoves(piece);
        List<Vector2Int> valid = new();

        foreach (Vector2Int move in candidates)
        {
            if (!MoveLeavesKingInCheck(piece, move))
                valid.Add(move);
        }

        return valid;
    }

    // ─────────────────────────────────────────────
    //  MOVIMIENTOS CANDIDATOS POR TIPO
    // ─────────────────────────────────────────────

    private List<Vector2Int> GetCandidateMoves(ChessPiece piece)
    {
        return piece.type switch
        {
            PieceType.Pawn   => GetPawnMoves(piece),
            PieceType.Knight => GetKnightMoves(piece),
            PieceType.Bishop => GetSlidingMoves(piece, bishopDirs),
            PieceType.Rook   => GetSlidingMoves(piece, rookDirs),
            PieceType.Queen  => GetSlidingMoves(piece, queenDirs),
            PieceType.King   => GetKingMoves(piece),
            _                => new List<Vector2Int>()
        };
    }

    // ── Direcciones de movimiento ──────────────────
    private static readonly Vector2Int[] bishopDirs =
        { new(1,1), new(1,-1), new(-1,1), new(-1,-1) };

    private static readonly Vector2Int[] rookDirs =
        { new(1,0), new(-1,0), new(0,1), new(0,-1) };

    private static readonly Vector2Int[] queenDirs =
        { new(1,1), new(1,-1), new(-1,1), new(-1,-1),
          new(1,0), new(-1,0), new(0,1),  new(0,-1)  };

    // ─────────────────────────────────────────────
    //  PEÓN
    // ─────────────────────────────────────────────

    private List<Vector2Int> GetPawnMoves(ChessPiece piece)
    {
        var moves = new List<Vector2Int>();
        int dir      = piece.isWhite ? 1 : -1;
        int startRow = piece.isWhite ? 1 : 6;
        int col = piece.col, row = piece.row;

        // Avance simple
        var oneStep = new Vector2Int(col, row + dir);
        if (IsInBoard(oneStep) && GetPieceAt(oneStep) == null)
        {
            moves.Add(oneStep);

            // Avance doble desde fila inicial
            var twoStep = new Vector2Int(col, row + dir * 2);
            if (row == startRow && GetPieceAt(twoStep) == null)
                moves.Add(twoStep);
        }

        // Capturas en diagonal
        foreach (int dc in new[] { -1, 1 })
        {
            var capture = new Vector2Int(col + dc, row + dir);
            if (!IsInBoard(capture)) continue;

            ChessPiece target = GetPieceAt(capture);
            if (target != null && target.isWhite != piece.isWhite)
                moves.Add(capture);
        }

        // En passant
        var ep = GameState.Instance.enPassantTarget;
        if (ep.HasValue)
        {
            var epPos = ep.Value;
            if (epPos.y == row + dir &&
                (epPos.x == col - 1 || epPos.x == col + 1))
                moves.Add(epPos);
        }

        return moves;
    }

    // ─────────────────────────────────────────────
    //  CABALLO
    // ─────────────────────────────────────────────

    private static readonly Vector2Int[] knightJumps =
    {
        new(2,1), new(2,-1), new(-2,1), new(-2,-1),
        new(1,2), new(1,-2), new(-1,2), new(-1,-2)
    };

    private List<Vector2Int> GetKnightMoves(ChessPiece piece)
    {
        var moves = new List<Vector2Int>();
        foreach (var jump in knightJumps)
        {
            var dest = new Vector2Int(piece.col + jump.x, piece.row + jump.y);
            if (!IsInBoard(dest)) continue;
            ChessPiece target = GetPieceAt(dest);
            if (target == null || target.isWhite != piece.isWhite)
                moves.Add(dest);
        }
        return moves;
    }

    // ─────────────────────────────────────────────
    //  PIEZAS DESLIZANTES (alfil, torre, reina)
    // ─────────────────────────────────────────────

    private List<Vector2Int> GetSlidingMoves(ChessPiece piece, Vector2Int[] directions)
    {
        var moves = new List<Vector2Int>();
        foreach (var dir in directions)
        {
            int c = piece.col + dir.x;
            int r = piece.row + dir.y;

            while (IsInBoard(c, r))
            {
                ChessPiece target = GetPieceAt(c, r);
                if (target == null)
                {
                    moves.Add(new Vector2Int(c, r));
                }
                else
                {
                    if (target.isWhite != piece.isWhite)
                        moves.Add(new Vector2Int(c, r)); // captura
                    break; // bloqueado
                }
                c += dir.x;
                r += dir.y;
            }
        }
        return moves;
    }

    // ─────────────────────────────────────────────
    //  REY
    // ─────────────────────────────────────────────

    private List<Vector2Int> GetKingMoves(ChessPiece piece)
    {
        var moves = new List<Vector2Int>();

        foreach (var dir in queenDirs)
        {
            var dest = new Vector2Int(piece.col + dir.x, piece.row + dir.y);
            if (!IsInBoard(dest)) continue;
            ChessPiece target = GetPieceAt(dest);
            if (target == null || target.isWhite != piece.isWhite)
                moves.Add(dest);
        }

        // Enroque
        AddCastlingMoves(piece, moves);

        return moves;
    }

    // ─────────────────────────────────────────────
    //  ENROQUE
    // ─────────────────────────────────────────────

    private void AddCastlingMoves(ChessPiece king, List<Vector2Int> moves)
    {
        if (king.hasMoved) return;
        if (IsInCheck(king.isWhite)) return;

        int row = king.row;

        // Enroque corto (lado del rey)
        ChessPiece rookK = GetPieceAt(7, row);
        if (rookK != null && !rookK.hasMoved &&
            GetPieceAt(5, row) == null && GetPieceAt(6, row) == null &&
            !IsCellAttacked(new Vector2Int(5, row), !king.isWhite) &&
            !IsCellAttacked(new Vector2Int(6, row), !king.isWhite))
        {
            moves.Add(new Vector2Int(6, row));
        }

        // Enroque largo (lado de la reina)
        ChessPiece rookQ = GetPieceAt(0, row);
        if (rookQ != null && !rookQ.hasMoved &&
            GetPieceAt(1, row) == null && GetPieceAt(2, row) == null &&
            GetPieceAt(3, row) == null &&
            !IsCellAttacked(new Vector2Int(3, row), !king.isWhite) &&
            !IsCellAttacked(new Vector2Int(2, row), !king.isWhite))
        {
            moves.Add(new Vector2Int(2, row));
        }
    }

    // ─────────────────────────────────────────────
    //  JAQUE Y SEGURIDAD DEL REY
    // ─────────────────────────────────────────────

    /// <summary>¿Está en jaque el rey del color indicado?</summary>
    public bool IsInCheck(bool white)
    {
        Vector2Int kingPos = FindKing(white);
        return IsCellAttacked(kingPos, !white);
    }

    /// <summary>¿Está en jaque mate el color indicado?</summary>
    public bool IsCheckmate(bool white)
    {
        if (!IsInCheck(white)) return false;
        return !HasAnyValidMove(white);
    }

    /// <summary>¿Ahogado? (sin movimientos pero sin jaque)</summary>
    public bool IsStalemate(bool white)
    {
        if (IsInCheck(white)) return false;
        return !HasAnyValidMove(white);
    }

    private bool HasAnyValidMove(bool white)
    {
        foreach (ChessPiece p in GameState.Instance.AllPieces())
        {
            if (p == null || p.isWhite != white) continue;
            if (GetValidMoves(p).Count > 0) return true;
        }
        return false;
    }

    /// <summary>Simula el movimiento y comprueba si el propio rey queda en jaque.</summary>
    private bool MoveLeavesKingInCheck(ChessPiece piece, Vector2Int dest)
    {
        ChessPiece[,] backup   = (ChessPiece[,])GameState.Instance.board.Clone();
        ChessPiece captured    = GetPieceAt(dest);
        int origCol = piece.col, origRow = piece.row;

        // Simula
        GameState.Instance.board[origCol, origRow] = null;
        GameState.Instance.board[dest.x, dest.y]   = piece;
        piece.col = dest.x; piece.row = dest.y;

        bool inCheck = IsInCheck(piece.isWhite);

        // Restaura
        piece.col = origCol; piece.row = origRow;
        GameState.Instance.board = backup;

        return inCheck;
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────

    private Vector2Int FindKing(bool white)
    {
        for (int c = 0; c < 8; c++)
            for (int r = 0; r < 8; r++)
            {
                var p = GameState.Instance.board[c, r];
                if (p != null && p.isWhite == white && p.type == PieceType.King)
                    return new Vector2Int(c, r);
            }
        return Vector2Int.zero;
    }

    /// <summary>¿Alguna pieza del bando 'byWhite' ataca la celda?</summary>
    public bool IsCellAttacked(Vector2Int cell, bool byWhite)
    {
        foreach (ChessPiece p in GameState.Instance.AllPieces())
        {
            if (p == null || p.isWhite != byWhite) continue;
            foreach (var m in GetCandidateMoves(p))
                if (m == cell) return true;
        }
        return false;
    }

    public ChessPiece GetPieceAt(int col, int row)
        => IsInBoard(col, row) ? GameState.Instance.board[col, row] : null;

    public ChessPiece GetPieceAt(Vector2Int pos)
        => GetPieceAt(pos.x, pos.y);

    private bool IsInBoard(int c, int r)
        => c >= 0 && c < 8 && r >= 0 && r < 8;

    private bool IsInBoard(Vector2Int v)
        => IsInBoard(v.x, v.y);
}
