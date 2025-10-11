using UnityEngine;

public class ThrowableObject : MonoBehaviour
{
    public float throwForce = 10f;       // fuerza al lanzar
    public float noiseIntensity = 5f;    // ruido que genera
    public float lifetime = 5f;          // tiempo máximo antes de destruir

    private Rigidbody rb;
    private bool hasHitGround = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Throw(Vector3 direction)
    {
        rb.isKinematic = false;
        rb.AddForce(direction * throwForce, ForceMode.Impulse);

        // Destruir automáticamente después de lifetime segundos si no colisiona antes
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHitGround) return;

        // Si chocó con el suelo
        if (collision.gameObject.CompareTag("Ground"))
        {
            hasHitGround = true;

            // Emitir ruido al caer
            NoiseManager.Instance.ReportNoise(transform.position, noiseIntensity);

            // Destruir la piedra inmediatamente (solo queremos ruido)
            Destroy(gameObject);
        }
        else
        {
            // Opcional: si choca con otro objeto (pared, enemigo, etc.)
            NoiseManager.Instance.ReportNoise(transform.position, noiseIntensity);
        }
    }
}
