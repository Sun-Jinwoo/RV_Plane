using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections.Generic;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Reemplaza el Ground Plane Stage de Vuforia usando AR Foundation.
/// 
/// FUNCIONAMIENTO:
///   1. Detecta planos horizontales con ARPlaneManager
///   2. Al tocar la pantalla hace un Raycast AR contra esos planos
///   3. Coloca el tablero (chessRoot) en ese punto
///   4. Una vez colocado, oculta los planos y activa el juego
///
/// SETUP en Inspector:
///   - arRaycastManager  → componente ARRaycastManager del XR Origin
///   - arPlaneManager    → componente ARPlaneManager del XR Origin
///   - chessRootPrefab   → prefab con el tablero + piezas
///   - placementIndicator→ prefab de un quad/círculo que sigue al plano detectado
/// </summary>
public class ARPlacementController : MonoBehaviour
{
    [Header("AR Components (del XR Origin)")]
    public ARRaycastManager arRaycastManager;
    public ARPlaneManager arPlaneManager;

    [Header("Prefabs")]
    [Tooltip("Prefab raíz con tablero + piezas")]
    public GameObject chessRootPrefab;

    [Tooltip("Quad semitransparente que indica dónde se colocará el tablero")]
    public GameObject placementIndicatorPrefab;

    [Header("Estado")]
    public bool boardPlaced = false;

    // Internos
    private GameObject chessRootInstance;
    private GameObject indicator;
    private Pose currentPlacementPose;
    private bool placementPoseIsValid = false;
    private static readonly List<ARRaycastHit> hits = new();

    // ─────────────────────────────────────────────
    //  UNITY
    // ─────────────────────────────────────────────

    void Start()
    {
        EnhancedTouchSupport.Enable();

        // Crea el indicador visual
        if (placementIndicatorPrefab != null)
            indicator = Instantiate(placementIndicatorPrefab);

        // Oculta los planos detectados (opcional, para limpieza visual)
        arPlaneManager.planePrefab = null;
    }

    void OnDestroy()
    {
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        if (boardPlaced) return;

        UpdatePlacementPose();
        UpdateIndicator();
        HandlePlacementInput();
    }

    // ─────────────────────────────────────────────
    //  POSE DEL PLANO DETECTADO
    // ─────────────────────────────────────────────

    /// <summary>
    /// Lanza un raycast desde el centro de la pantalla contra los planos AR detectados.
    /// Actualiza currentPlacementPose con la posición/rotación del hit.
    /// </summary>
    private void UpdatePlacementPose()
    {
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        placementPoseIsValid = arRaycastManager.Raycast(
            screenCenter,
            hits,
            TrackableType.PlaneWithinPolygon);

        if (placementPoseIsValid)
            currentPlacementPose = hits[0].pose;
    }

    // ─────────────────────────────────────────────
    //  INDICADOR VISUAL
    // ─────────────────────────────────────────────

    private void UpdateIndicator()
    {
        if (indicator == null) return;

        indicator.SetActive(placementPoseIsValid);

        if (placementPoseIsValid)
        {
            indicator.transform.SetPositionAndRotation(
                currentPlacementPose.position,
                currentPlacementPose.rotation);
        }
    }

    // ─────────────────────────────────────────────
    //  INPUT PARA COLOCAR EL TABLERO
    // ─────────────────────────────────────────────

    private void HandlePlacementInput()
    {
        if (!placementPoseIsValid) return;

        bool tapped = false;

#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            tapped = true;
#else
        if (Touch.activeTouches.Count > 0 &&
            Touch.activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Began)
            tapped = true;
#endif

        if (tapped)
            PlaceBoard();
    }

    // ─────────────────────────────────────────────
    //  COLOCAR EL TABLERO
    // ─────────────────────────────────────────────

    private void PlaceBoard()
    {
        // Instancia o mueve el ChessRoot
        if (chessRootInstance == null)
        {
            chessRootInstance = Instantiate(
                chessRootPrefab,
                currentPlacementPose.position,
                currentPlacementPose.rotation);

            // Inicializa el juego una vez colocado
            GameState.Instance.InitBoard();
        }
        else
        {
            // Permite reposicionar tocando de nuevo antes de confirmar
            chessRootInstance.transform.SetPositionAndRotation(
                currentPlacementPose.position,
                currentPlacementPose.rotation);
        }

        // Oculta indicador y congela la posición
        boardPlaced = true;
        if (indicator != null) indicator.SetActive(false);

        // Desactiva los planos AR visibles
        SetPlanesVisible(false);

        // Notifica al BoardManager la nueva posición del origen
        if (BoardManager.Instance != null)
        {
            // El boardOrigin debe ser hijo del ChessRoot en el prefab
            // No necesitas reasignarlo si es hijo del prefab
        }

        Debug.Log($"[ARPlacement] Tablero colocado en {currentPlacementPose.position}");
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────

    private void SetPlanesVisible(bool visible)
    {
        foreach (var plane in arPlaneManager.trackables)
            plane.gameObject.SetActive(visible);

        arPlaneManager.enabled = visible;
    }

    /// <summary>
    /// Permite reposicionar el tablero (llamar desde un botón UI "Mover tablero").
    /// </summary>
    public void ResetPlacement()
    {
        boardPlaced = false;
        SetPlanesVisible(true);
        arPlaneManager.enabled = true;
        if (indicator != null) indicator.SetActive(true);
    }
}
