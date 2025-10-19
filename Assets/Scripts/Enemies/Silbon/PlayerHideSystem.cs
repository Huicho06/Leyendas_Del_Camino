using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHideSystem : MonoBehaviour
{
    [Header("Referencias")]
    public Transform cameraTransform;
    public GameObject flashlight;          // opcional: se apaga al esconderse
    public NoiseEmitter noiseEmitter;      // opcional: silenciar ruido al esconderse
    public PlayerMovement playerMovement;  // si no lo asignas, se busca en Start

    [Header("Ajustes")]
    public KeyCode interactKey = KeyCode.E;
    public float moveDuration = 0.35f;     // tiempo de transición a la posición de ocultarse
    public float exitDuration = 0.25f;     // tiempo de salida
    public bool freezeInputWhileHidden = true;
    public bool zeroNoiseWhileHidden = true;

    // Estado
    public bool IsHidden { get; private set; }
    public HideSpot CurrentSpot { get; private set; }

    // Internos
    private CharacterController controller;
    private Vector3 savedPlayerPos;
    private Quaternion savedPlayerRot;
    private Vector3 savedCamLocalPos;
    private Quaternion savedCamLocalRot;
    private bool inputWasEnabled = true;
    private bool flashlightWasOn = false;

    void Start()
    {
        if (!playerMovement) playerMovement = GetComponent<PlayerMovement>();
        controller = GetComponent<CharacterController>();
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
    }

    public bool IsPlayerCrouching()
    {
        return playerMovement ? playerMovement.IsCrouching : false;
    }

    public void EnterHide(HideSpot spot)
    {
        if (!spot || !spot.hidePoint || IsHidden) return;

        StopAllCoroutines();
        StartCoroutine(DoEnterHide(spot));
    }

    public void ExitHide()
    {
        if (!IsHidden) return;

        StopAllCoroutines();
        StartCoroutine(DoExitHide());
    }

    IEnumerator DoEnterHide(HideSpot spot)
    {
        CurrentSpot = spot;

        // Guardar estado
        savedPlayerPos = transform.position;
        savedPlayerRot = transform.rotation;

        if (cameraTransform)
        {
            savedCamLocalPos = cameraTransform.localPosition;
            savedCamLocalRot = cameraTransform.localRotation;
        }

        // Apagar linterna si procede
        if (flashlight)
        {
            flashlightWasOn = flashlight.activeSelf;
            flashlight.SetActive(false);
        }

        // Silencio de ruido
        bool prevUseNoise = false;
        if (noiseEmitter && zeroNoiseWhileHidden)
        {
            prevUseNoise = playerMovement ? playerMovement.useNoise : false;
            if (playerMovement) playerMovement.useNoise = false;
        }

        // Deshabilitar entrada de movimiento
        if (playerMovement && freezeInputWhileHidden)
        {
            inputWasEnabled = true; // asumimos que estaba activo
            //playerMovement.IsHidden = true; // expuesto para IA
        }

        // Mover al jugador/cámara suavemente al hidePoint
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 targetPos = spot.hidePoint.position;
        Quaternion targetRot = spot.faceForward ? spot.hidePoint.rotation : transform.rotation;

        // Mejor: desactivar momentáneamente el CharacterController para evitar empujes
        bool ccState = controller ? controller.enabled : false;
        if (controller) controller.enabled = false;

        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / moveDuration);
            transform.position = Vector3.Lerp(startPos, targetPos, k);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, k);
            yield return null;
        }
        transform.position = targetPos;
        transform.rotation = targetRot;

        if (controller) controller.enabled = ccState;

        IsHidden = true;

        // Opcional: ajustar cámara local si tu HidePoint está pensado para POV exacto
        // (por defecto la cámara se queda igual; si quieres, crea un hijo "CamLocal"
        // dentro de HidePoint para posiciones exactas de la cámara y copia su localPos/Rot)

        yield break;
    }

    IEnumerator DoExitHide()
    {
        var spot = CurrentSpot;

        // Restaurar entrada/ruido/linterna luego de la transición
        Vector3 fromPos = transform.position;
        Quaternion fromRot = transform.rotation;

        Vector3 toPos = savedPlayerPos;
        Quaternion toRot = savedPlayerRot;

        bool ccState = controller ? controller.enabled : false;
        if (controller) controller.enabled = false;

        float t = 0f;
        while (t < exitDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / exitDuration);
            transform.position = Vector3.Lerp(fromPos, toPos, k);
            transform.rotation = Quaternion.Slerp(fromRot, toRot, k);
            yield return null;
        }
        transform.position = toPos;
        transform.rotation = toRot;

        if (controller) controller.enabled = ccState;

        // Restaurar cámara local
        if (cameraTransform)
        {
            cameraTransform.localPosition = savedCamLocalPos;
            cameraTransform.localRotation = savedCamLocalRot;
        }

        // Restaurar linterna
        if (flashlight && flashlightWasOn) flashlight.SetActive(true);

        // Restaurar ruido / entrada
        if (playerMovement)
        {
            if (zeroNoiseWhileHidden) playerMovement.useNoise = true;
        }

        IsHidden = false;
        CurrentSpot = null;

        // Ocultar prompt si seguía visible
        HideUI.Instance?.Show(false);
    }
}
