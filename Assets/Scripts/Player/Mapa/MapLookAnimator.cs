using UnityEngine;

public class MapLookAnimator : MonoBehaviour
{
    [Header("Referencias")]
    public Transform playerCamera;
    public Animator animator;
    public string boolName = "Raised";

    [Header("Detección de mirada")]
    [Range(0, 89)] public float raiseStartAngle = 15f;   // desde este ángulo hacia abajo empieza a subir
    [Range(0, 89)] public float fullyRaisedAngle = 50f;  // a partir de este ángulo ya está totalmente arriba

    [Header("Suavizado")]
    public float smoothTime = 0.07f;

    private float factor;
    private float vel;
    private bool isRaised;

    void Start()
    {
        if (!playerCamera && Camera.main)
            playerCamera = Camera.main.transform;
        if (!animator)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!playerCamera || !animator) return;

        // --- Detección de mirada hacia abajo robusta ---
        // Tomamos el ángulo vertical (X) pero ajustamos para valores >180
        float pitch = playerCamera.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f; // convierte de [0,360] a [-180,180]

        // En muchos controladores, mirar hacia abajo da un valor NEGATIVO
        float lookDownAngle = Mathf.Abs(Mathf.Clamp(pitch, -89f, 89f));

        // Calculamos factor de inclinación hacia abajo (0 = recto, 1 = totalmente abajo)
        float target = Mathf.InverseLerp(raiseStartAngle, fullyRaisedAngle, lookDownAngle);
        factor = Mathf.SmoothDamp(factor, target, ref vel, smoothTime);

        // --- Lógica de activación con histéresis ---
        if (!isRaised && factor > 0.6f)
            isRaised = true;
        else if (isRaised && factor < 0.3f)
            isRaised = false;

        animator.SetBool(boolName, isRaised);
    }
}
