using System.Collections;
using UnityEngine;

// PieceType está definido en PieceType.cs

/// <summary>
/// Componente adjunto a cada pieza del ajedrez.
/// Almacena su posición lógica, tipo, color y si ya se movió (enroque).
/// </summary>
public class ChessPiece : MonoBehaviour
{
    [Header("Identidad")]
    public PieceType type;
    public bool isWhite;

    [Header("Posición lógica en el tablero (0–7)")]
    public int col;
    public int row;

    /// <summary>
    /// True si la pieza ya se movió al menos una vez.
    /// Usado para enroque y avance doble del peón.
    /// </summary>
    [HideInInspector]
    public bool hasMoved = false;

    [Header("Animación")]
    [Tooltip("Velocidad del movimiento suave (unidades por segundo)")]
    public float moveSpeed = 4f;

    [Tooltip("Altura del arco durante el movimiento (0 = deslizamiento plano)")]
    public float arcHeight = 0.05f;

    private Coroutine moveCoroutine;

    // ─────────────────────────────────────────────
    //  MOVIMIENTO
    // ─────────────────────────────────────────────

    /// <summary>
    /// Actualiza la posición lógica y anima el modelo hacia worldPos.
    /// </summary>
    public void MoveTo(int newCol, int newRow, Vector3 worldPos)
    {
        col = newCol;
        row = newRow;
        hasMoved = true;

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(SmoothMoveArc(worldPos));
    }

    private IEnumerator SmoothMoveArc(Vector3 target)
    {
        float t = 0f;
        Vector3 start = transform.position;
        float distance = Vector3.Distance(start, target);

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            float clamped = Mathf.Clamp01(t);

            // Arco parabólico
            float arc = arcHeight * Mathf.Sin(clamped * Mathf.PI) * distance;
            Vector3 pos = Vector3.Lerp(start, target, clamped);
            pos.y += arc;

            transform.position = pos;
            yield return null;
        }

        transform.position = target;
        moveCoroutine = null;
    }

    // ─────────────────────────────────────────────
    //  HIGHLIGHT VISUAL (escala al seleccionar)
    // ─────────────────────────────────────────────

    private Vector3 originalScale;
    private bool isSelected = false;

    void Awake() => originalScale = transform.localScale;

    public void SetSelected(bool selected)
    {
        if (isSelected == selected) return;
        isSelected = selected;
        transform.localScale = selected ? originalScale * 1.15f : originalScale;
    }
}