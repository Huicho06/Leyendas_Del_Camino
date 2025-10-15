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
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHitGround) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            hasHitGround = true;
            NoiseManager.Instance.ReportNoise(transform.position, noiseIntensity);
            Destroy(gameObject);
        }
        else
        {
            NoiseManager.Instance.ReportNoise(transform.position, noiseIntensity);
        }
    }
}
