using UnityEngine;

/// <summary>
/// Convierte entre coordenadas lógicas del tablero (col, row)
/// y posiciones en el mundo AR (Vector3).
///
/// SETUP:
///   - Crea un GameObject vacío "BoardOrigin" y colócalo en la esquina a1 del tablero
///     (esquina inferior izquierda desde el punto de vista de las blancas)
///   - boardOrigin.forward debe apuntar hacia la fila 8 (hacia las negras)
///   - boardOrigin.right debe apuntar hacia la columna h
///   - Ajusta cellSize según el tamaño real de tu modelo 3D
/// </summary>
public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;

    [Header("Geometría del tablero")]
    [Tooltip("Tamaño de cada celda en unidades de Unity (antes de escalar ChessRoot)")]
    public float cellSize = 0.1f;

    [Tooltip("Esquina a1 del tablero (col=0, row=0). Forward=filas, Right=columnas")]
    public Transform boardOrigin;

    void Awake() => Instance = this;

    // ─────────────────────────────────────────────
    //  CONVERSIÓN DE COORDENADAS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Convierte coordenada lógica → posición en el mundo AR.
    /// Devuelve el centro de la celda a nivel del tablero.
    /// </summary>
    public Vector3 GetWorldPosition(int col, int row)
    {
        return boardOrigin.position
             + boardOrigin.right   * (col * cellSize + cellSize * 0.5f)
             + boardOrigin.forward * (row * cellSize + cellSize * 0.5f);
    }

    /// <summary>
    /// Convierte una posición en el mundo AR → coordenada lógica.
    /// Devuelve false si cae fuera del tablero.
    /// </summary>
    public bool GetBoardPosition(Vector3 worldPos, out int col, out int row)
    {
        Vector3 local = boardOrigin.InverseTransformPoint(worldPos);

        col = Mathf.FloorToInt(local.x / cellSize);
        row = Mathf.FloorToInt(local.z / cellSize);

        return col >= 0 && col < 8 && row >= 0 && row < 8;
    }

    // ─────────────────────────────────────────────
    //  DEBUG (dibuja las celdas en el editor)
    // ─────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (boardOrigin == null) return;

        for (int c = 0; c < 8; c++)
        {
            for (int r = 0; r < 8; r++)
            {
                bool isLight = (c + r) % 2 == 0;
                Gizmos.color = isLight
                    ? new Color(1f, 0.9f, 0.7f, 0.5f)
                    : new Color(0.4f, 0.25f, 0.1f, 0.5f);

                Vector3 center = GetWorldPosition(c, r);
                Gizmos.DrawCube(center,
                    new Vector3(cellSize * 0.95f, 0.002f, cellSize * 0.95f));
            }
        }
    }
#endif
}
