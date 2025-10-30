using UnityEngine;

public class HandheldMapController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Cámara del jugador (la misma que usas para mirar)")]
    public Transform playerCamera;

    [Tooltip("Transform de la POSE de REPOSO (apenas visible). Colócalo como hijo de la cámara.")]
    public Transform restPose;

    [Tooltip("Transform de la POSE de LECTURA (mapa arriba, ocupa la vista). Hijo de la cámara.")]
    public Transform viewPose;

    [Header("Detección de mirada hacia abajo")]
    [Tooltip("Grados desde el horizonte a partir de los cuales empieza a subir el mapa (p.ej. 15°)")]
    [Range(0f, 89f)] public float raiseStartAngle = 15f;

    [Tooltip("Grados desde el horizonte donde el mapa ya está totalmente levantado (p.ej. 50°)")]
    [Range(0f, 89f)] public float fullyRaisedAngle = 50f;

    [Header("Suavizado")]
    [Tooltip("Velocidad de interpolación de la pose")]
    public float poseLerpSpeed = 10f;

    [Tooltip("Opcional: suavizado del factor 0-1")]
    public float factorSmoothTime = 0.08f;

    [Header("Opcionales")]
    [Tooltip("Opcional: deshabilita el mapa al correr")]
    public bool hideWhileRunning = true;

    [Tooltip("Referencia opcional a tu PlayerMovement para leer isRunning/IsHidden")]
    public PlayerMovement playerMovement;

    [Tooltip("Si hay CanvasGroup (UI del mapa), se regulará su alpha")]
    public CanvasGroup mapCanvasGroup;

    float visibleFactor;        // 0 reposo, 1 lectura
    float visibleFactorVel;     // para SmoothDamp

    void Reset()
    {
        playerCamera = Camera.main ? Camera.main.transform : null;
    }

    void Update()
    {
        if (!playerCamera || !restPose || !viewPose) return;

        // Ocultar si estás "escondido" (tu estado personalizado) o corriendo si así lo deseas
        bool forceHidden = false;
        bool isRunning = false;
        if (playerMovement)
        {
            forceHidden = playerMovement.IsHidden;
            // 'isRunning' interno del script no es público, así que lo inferimos:
            // una forma simple es aproximar con LeftShift y movimiento en Z, o
            // exponer un getter en tu PlayerMovement. Aquí, aproximamos:
            isRunning = Input.GetKey(KeyCode.LeftShift);
        }

        // 1) Calcular cuánto mira hacia abajo (0 = horizonte, 90 = suelo)
        // Dot con Vector3.down es 0 en horizonte, 1 mirando totalmente abajo.
        float downDot = Vector3.Dot(playerCamera.forward, Vector3.down);
        downDot = Mathf.Clamp01(downDot);

        // Convertimos tus ángulos a "dot" usando sin(ángulo)
        float startDot = Mathf.Sin(raiseStartAngle * Mathf.Deg2Rad);
        float endDot = Mathf.Sin(fullyRaisedAngle * Mathf.Deg2Rad);

        // 2) Factor “raw” de visibilidad (0-1) según la mirada
        float targetFactor = Mathf.InverseLerp(startDot, endDot, downDot);

        // Reglas adicionales
        if (forceHidden) targetFactor = 0f;
        if (hideWhileRunning && isRunning) targetFactor = Mathf.Min(targetFactor, 0.25f); // apenas asoma si corres

        // 3) Suavizado del factor para evitar pops
        visibleFactor = Mathf.SmoothDamp(visibleFactor, targetFactor, ref visibleFactorVel, factorSmoothTime);

        // 4) Interpolar pose entre reposo y lectura
        // Posición y rotación locales (los dos poses deben ser hijos de la cámara)
        transform.localPosition = Vector3.Lerp(restPose.localPosition, viewPose.localPosition, visibleFactor);
        transform.localRotation = Quaternion.Slerp(restPose.localRotation, viewPose.localRotation, visibleFactor);

        // 5) (Opcional) Alpha si tienes UI encima del mesh (CanvasGroup)
        if (mapCanvasGroup)
        {
            // Un pequeño “ease” para que aparezca un poco más tarde que la pose
            float eased = Mathf.SmoothStep(0f, 1f, visibleFactor);
            mapCanvasGroup.alpha = eased;
            mapCanvasGroup.blocksRaycasts = eased > 0.95f;
            mapCanvasGroup.interactable = mapCanvasGroup.blocksRaycasts;
        }
    }
}
