using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class JeepController : MonoBehaviour
{
    [Header("Movimiento")]
    public float forwardSpeed = 8f;         // Velocidad hacia adelante
    public float lateralSpeed = 6f;         // Velocidad lateral
    public float lateralLimit = 4f;         // Límite lateral
    public float smoothLateral = 8f;        // Suavizado lateral

    [Header("Input")]
    public bool useTouch = false;
    public float touchSensitivity = 0.01f;

    [Header("Visual")]
    public Transform jeepModel;      // Modelo hijo
    public Animator jeepAnimator;    // Animator hijo

    private NavMeshAgent agent;
    private float targetX;
    private Vector3 velocity = Vector3.zero;
    private float horizontalInput;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = true;
        agent.updatePosition = true;
        agent.isStopped = false;
        agent.speed = 0f;
    }

    void Start()
    {
        targetX = transform.position.x;
    }

    void Update()
    {
        HandleInput();
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        MoveJeep();
    }

    void HandleInput()
    {
        horizontalInput = 0f;

        if (!useTouch)
            horizontalInput = Input.GetAxis("Horizontal");
        else if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                horizontalInput = t.deltaPosition.x * touchSensitivity;
        }

        targetX += horizontalInput * lateralSpeed * Time.deltaTime;
        targetX = Mathf.Clamp(targetX, -lateralLimit, lateralLimit);
    }

    void MoveJeep()
    {
        // Posición lateral suavizada
        float newX = Mathf.SmoothDamp(transform.position.x, targetX, ref velocity.x, 1f / smoothLateral, lateralSpeed, Time.fixedDeltaTime);

        // Movimiento hacia adelante
        Vector3 forwardMove = transform.forward * forwardSpeed * Time.fixedDeltaTime;
        Vector3 newPosition = transform.position + forwardMove;
        newPosition.x = newX;

        agent.Move(newPosition - transform.position);

        // Rotación física del jeep (suave, evita que se voltee)
        float tilt = Mathf.Clamp((targetX - transform.position.x) * 4f, -10f, 10f);
        Quaternion targetRot = Quaternion.Euler(0f, transform.eulerAngles.y, -tilt);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.fixedDeltaTime * 6f);

        // Rotación visual del modelo
        if (jeepModel != null)
        {
            float modelTilt = Mathf.Clamp(horizontalInput * 15f, -15f, 15f);
            Quaternion modelRot = Quaternion.Euler(0f, 0f, -modelTilt);
            jeepModel.localRotation = Quaternion.Lerp(jeepModel.localRotation, modelRot, Time.fixedDeltaTime * 6f);
        }
    }

    void UpdateAnimator()
    {
        if (jeepAnimator == null) return;

        float threshold = 0.1f;
        bool left = horizontalInput < -threshold;
        bool right = horizontalInput > threshold;

        jeepAnimator.SetBool("IsTurningLeft", left);
        jeepAnimator.SetBool("IsTurningRight", right);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
            GameManager.Instance?.OnPlayerHitObstacle();
    }
}
