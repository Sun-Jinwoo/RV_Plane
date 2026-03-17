// ARDebugger.cs — versión con logs (más simple y segura)
using UnityEngine;
using Vuforia;

public class ARDebugger : MonoBehaviour
{
    private float timer = 0f;

    void Start()
    {
        Debug.Log("=== AR DEBUGGER INICIADO ===");

        // Verifica que Vuforia arrancó
        var vuforia = VuforiaBehaviour.Instance;
        if (vuforia != null)
        {
            // No existe RegisterVuforiaStartedCallback, usar evento Started si está disponible
            VuforiaApplication.Instance.OnVuforiaStarted += OnVuforiaStarted;
            VuforiaApplication.Instance.OnVuforiaPaused += OnPause;
        }
    }

    void OnVuforiaStarted()
    {
        Debug.Log("? Vuforia INICIADO correctamente");
    }

    void OnPause(bool paused)
    {
        Debug.Log($"Vuforia pause: {paused}");
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < 3f) return; // log cada 3 segundos
        timer = 0f;

        bool vuforiaActivo = VuforiaBehaviour.Instance != null;
        bool camaraActiva = false;
#if VUFORIA_CAMERA
        if (VuforiaBehaviour.Instance != null)
        {
            // CameraDevice es una clase estática, se accede a través de sus miembros estáticos o por VuforiaBehaviour
            // Usamos la propiedad IsActive (según los signatures)
            camaraActiva = CameraDevice.Instance != null && CameraDevice.Instance.IsActive;
        }
#endif

        Debug.Log($"[AR] Vuforia:{vuforiaActivo} | Camara:{camaraActiva}");
    }

    void OnDestroy()
    {
        var vuforia = VuforiaBehaviour.Instance;
        if (vuforia != null)
        {
            VuforiaApplication.Instance.OnVuforiaStarted -= OnVuforiaStarted;
            VuforiaApplication.Instance.OnVuforiaPaused -= OnPause;
        }
    }
}