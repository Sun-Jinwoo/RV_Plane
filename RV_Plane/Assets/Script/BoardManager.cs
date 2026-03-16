using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public float cellSize = 0.1f;      // tamaño de cada celda (ajusta a tu modelo)
    public Transform boardOrigin;      // esquina a1 del tablero

    // Convierte coordenada lógica ? posición en el mundo AR
    public Vector3 GetWorldPosition(int col, int row)
    {
        return boardOrigin.position
             + boardOrigin.right * (col * cellSize)
             + boardOrigin.forward * (row * cellSize);
    }

    // Convierte posición del mundo ? coordenada lógica
    public bool GetBoardPosition(Vector3 worldPos, out int col, out int row)
    {
        Vector3 local = boardOrigin.InverseTransformPoint(worldPos);
        col = Mathf.RoundToInt(local.x / cellSize);
        row = Mathf.RoundToInt(local.z / cellSize);
        return col >= 0 && col < 8 && row >= 0 && row < 8;
    }
}