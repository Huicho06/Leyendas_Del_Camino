using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class JeepRigidbodyController : MonoBehaviour
{
    [Header("Velocidades")]
    public float forwardSpeed = 12f;
    public float acceleration = 8f;
    public float brakeForce = 30f;

    [Header("Steering")]
    public float steerTorque = 8f;
    public float steerSpeedFactor = 6f;

    [Header("Grip / Lateral")]
    public float lateralGrip = 8f;

    [Header("Autocentrado (PD Controller)")]
    public float returnKp = 400f;
    public float returnKd = 40f;
    public float maxReturnTorque = 4000f;

    [Header("Limitadores y protecciones")]
    public float maxYawRate = 4f;          // rad/s para limitar giros rápidos
    public float maxFlipAngle = 120f;      // si el yaw dif > esto, reducimos steering (grados)

    [Header("Tilt visual")]
    public float tiltAmount = 6f;
    public float tiltSmooth = 6f;

    [Header("Input")]
    public bool useTouch = false;
    public float touchSensitivity = 0.01f;

    Rigidbody rb;
    float steerInput = 0f;
    float currentTilt = 0f;
    float targetTilt = 0f;

    // Guardamos yaw "recto" que define la dirección del motor (hacia adelante)
    private float straightYaw;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.maxAngularVelocity = 10f;

        // Freeze roll/pitch si prefieres hacerlo por Constraints también:
        // rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // guardamos la orientación inicial como "adelante"
        straightYaw = transform.eulerAngles.y;
    }

    void Update()
    {
        HandleInput();

        float forwardVel = Vector3.Dot(rb.velocity, transform.forward);
        targetTilt = -steerInput * Mathf.Clamp01(Mathf.Abs(forwardVel) / 8f) * tiltAmount;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSmooth);

        // visual tilt opcional en child visual:
        // if (transform.childCount > 0) transform.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, currentTilt);
    }

    void FixedUpdate()
    {
        // Protecciones: asegurarnos que no haya rotación X/Z por inercia (evita comportamientos raros)
        Vector3 av = rb.angularVelocity;
        av.x = 0f;
        av.z = 0f;
        rb.angularVelocity = av;

        ApplyForwardForce();    // usa la direction basada en straightYaw
        ApplyLateralGrip();
        ApplySteeringAndReturn();
    }

    void HandleInput()
    {
        steerInput = 0f;
        if (!useTouch)
        {
            steerInput = Input.GetAxis("Horizontal");
            if (Input.GetKey(KeyCode.Space))
                rb.AddForce(-rb.velocity.normalized * brakeForce * rb.mass * Time.deltaTime, ForceMode.Force);
        }
        else
        {
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                    steerInput = Mathf.Clamp(t.deltaPosition.x * touchSensitivity, -1f, 1f);
            }
        }
    }

    // --- Aquí está el cambio clave: empujar en la dirección de straightYaw (no en transform.forward)
    void ApplyForwardForce()
    {
        Vector3 driveDir = Quaternion.Euler(0f, straightYaw, 0f) * Vector3.forward; // dirección de empuje
        float forwardVel = Vector3.Dot(rb.velocity, driveDir);                     // componente de la velocidad en driveDir
        float velError = forwardSpeed - forwardVel;
        float force = Mathf.Clamp(velError * acceleration, -brakeForce, acceleration);
        Vector3 forwardForce = driveDir * force * rb.mass;
        rb.AddForce(forwardForce, ForceMode.Force);
    }

    void ApplyLateralGrip()
    {
        // Calculamos lateral según el eje derecho del transform (visual)
        Vector3 lateralVelocity = Vector3.Dot(rb.velocity, transform.right) * transform.right;
        Vector3 correctiveForce = -lateralVelocity * lateralGrip * rb.mass;
        if (correctiveForce.magnitude > lateralGrip * 50f * rb.mass)
            correctiveForce = correctiveForce.normalized * lateralGrip * 50f * rb.mass;

        rb.AddForce(correctiveForce * Time.fixedDeltaTime, ForceMode.Force);
    }

    void ApplySteeringAndReturn()
    {
        // 1) Steering: aplicamos torque yaw para girar visualmente
        float forwardVel = Vector3.Dot(rb.velocity, transform.forward);
        float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardVel) / steerSpeedFactor);

        // limitador si el coche está muy desalineado (reduce prob. de dar la vuelta)
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, straightYaw));
        float steerReduction = (angleDiff > maxFlipAngle) ? 0.3f : 1f; // reduce steering si angleDiff grande

        float steeringTorque = steerInput * steerTorque * speedFactor * steerReduction;
        rb.AddRelativeTorque(Vector3.up * steeringTorque, ForceMode.Acceleration);

        // 2) Limitar yaw rate para evitar giros completos
        Vector3 av = rb.angularVelocity;
        av.y = Mathf.Clamp(av.y, -maxYawRate, maxYawRate);
        rb.angularVelocity = av;

        // 3) Autocentrado PD (cuando no hay input)
        if (Mathf.Abs(steerInput) < 0.01f)
        {
            float currentYaw = transform.eulerAngles.y;
            float angleErrorDeg = Mathf.DeltaAngle(currentYaw, straightYaw);
            float angleErrorRad = angleErrorDeg * Mathf.Deg2Rad;

            float angVelY = rb.angularVelocity.y;
            float torque = -returnKp * angleErrorRad - returnKd * angVelY;
            torque = Mathf.Clamp(torque, -maxReturnTorque, maxReturnTorque);

            rb.AddRelativeTorque(Vector3.up * torque, ForceMode.Acceleration);

            // amortiguación extra en yaw
            rb.angularVelocity = new Vector3(0f, rb.angularVelocity.y * 0.98f, 0f);
        }
        else
        {
            // Si quieres que la dirección de empuje (straightYaw) cambie lentamente
            // para seguir curvas suaves del track, puedes descomentar y ajustar la velocidad:
            // straightYaw = Mathf.LerpAngle(straightYaw, transform.eulerAngles.y, Time.fixedDeltaTime * 0.2f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Obstacle"))
        {
            Vector3 impulse = -collision.contacts[0].normal * 3f;
            rb.AddForce(impulse * rb.mass, ForceMode.Impulse);
            GameManager.Instance?.OnPlayerHitObstacle();
        }
    }

    // utilidad pública
    public void ResetStraightYawToCurrent()
    {
        straightYaw = transform.eulerAngles.y;
    }
}
