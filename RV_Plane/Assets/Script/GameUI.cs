using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI mínima para el juego de ajedrez AR.
///
/// SETUP en Canvas (World Space recomendado para AR):
///   - turnLabel       → TextMeshProUGUI  "Turno: Blancas"
///   - statusLabel     → TextMeshProUGUI  "Jaque!" / "Jaque Mate!" / "Tablas"
///   - promotionPanel  → Panel con 4 botones (Reina, Torre, Alfil, Caballo)
///   - Asigna los botones de promoción en el Inspector
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("Etiquetas")]
    public TextMeshProUGUI turnLabel;
    public TextMeshProUGUI statusLabel;

    [Header("Panel de promoción")]
    public GameObject promotionPanel;
    public Button promoteQueenBtn;
    public Button promoteRookBtn;
    public Button promoteBishopBtn;
    public Button promoteKnightBtn;

    private ChessPiece pendingPromotion;

    void Start()
    {
        // Suscribir eventos del GameState
        GameState.Instance.TurnChanged    += OnTurnChanged;
        GameState.Instance.CheckDetected  += OnCheckDetected;
        GameState.Instance.GameOver       += OnGameOver;
        GameState.Instance.PawnPromotion  += OnPawnPromotion;

        // Botones de promoción
        promoteQueenBtn .onClick.AddListener(() => Promote(PieceType.Queen));
        promoteRookBtn  .onClick.AddListener(() => Promote(PieceType.Rook));
        promoteBishopBtn.onClick.AddListener(() => Promote(PieceType.Bishop));
        promoteKnightBtn.onClick.AddListener(() => Promote(PieceType.Knight));

        promotionPanel.SetActive(false);
        UpdateTurnLabel(true);
        statusLabel.text = "";
    }

    void OnDestroy()
    {
        if (GameState.Instance == null) return;
        GameState.Instance.TurnChanged   -= OnTurnChanged;
        GameState.Instance.CheckDetected -= OnCheckDetected;
        GameState.Instance.GameOver      -= OnGameOver;
        GameState.Instance.PawnPromotion -= OnPawnPromotion;
    }

    // ─────────────────────────────────────────────
    //  CALLBACKS
    // ─────────────────────────────────────────────

    private void OnTurnChanged(bool isWhiteTurn)
    {
        UpdateTurnLabel(isWhiteTurn);
        statusLabel.text = "";
    }

    private void OnCheckDetected(bool isWhiteInCheck)
    {
        statusLabel.text = isWhiteInCheck ? "¡Jaque a las Blancas!" : "¡Jaque a las Negras!";
        statusLabel.color = Color.red;
    }

    private void OnGameOver(GameState.GameResult result)
    {
        string msg = result switch
        {
            GameState.GameResult.WhiteWins => "¡Jaque Mate! Ganan las Blancas",
            GameState.GameResult.BlackWins => "¡Jaque Mate! Ganan las Negras",
            GameState.GameResult.Stalemate => "Tablas por ahogado",
            _ => ""
        };
        statusLabel.text = msg;
        statusLabel.color = Color.yellow;
        turnLabel.text = "";
    }

    private void OnPawnPromotion(ChessPiece pawn)
    {
        pendingPromotion = pawn;
        promotionPanel.SetActive(true);
        // Pausa el juego mientras el jugador elige
        Time.timeScale = 0f;
    }

    // ─────────────────────────────────────────────
    //  PROMOCIÓN
    // ─────────────────────────────────────────────

    private void Promote(PieceType type)
    {
        if (pendingPromotion == null) return;
        GameState.Instance.PromotePawn(pendingPromotion, type);
        pendingPromotion = null;
        promotionPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────

    private void UpdateTurnLabel(bool isWhiteTurn)
    {
        if (turnLabel == null) return;
        turnLabel.text = isWhiteTurn ? "Turno: Blancas" : "Turno: Negras";
        turnLabel.color = isWhiteTurn ? Color.white : new Color(0.2f, 0.2f, 0.2f);
    }
}
