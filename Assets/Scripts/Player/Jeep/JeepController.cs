using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class JeepController : MonoBehaviour
{
    [Header("Movimiento")]
    public float forwardSpeed = 8f;         // velocidad constante hacia adelante (m/s)
    public float lateralSpeed = 6f;         // velocidad lateral (m/s)
    public float lateralLimit = 4.0f;       // límite en X desde el centro (evita salirse del camino)
    public float smoothLateral = 8f;        // suavizado para movimiento lateral

    [Header("Input")]
    public bool useTouch = false;           // true para controles táctiles
    public float touchSensitivity = 0.01f;

    private NavMeshAgent agent;
    private float targetX = 0f;
    private Vector3 velocity = Vector3.zero;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        // vamos a controlar el movimiento manualmente:
        agent.updateRotation = false;
        agent.updateUpAxis = true;
        agent.updatePosition = true; // mantenemos la posición en NavMesh
        agent.isStopped = false;
        // Opcional: dejar agent.speed = 0 para que no interfiera
        agent.speed = 0f;
    }

    void Start()
    {
        targetX = transform.position.x;
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        MoveJeep();
    }

    void HandleInput()
    {
        float horizontal = 0f;

        if (!useTouch)
        {
            // teclado/joystick estándar: A/D, flechas, gamepad axis "Horizontal"
            horizontal = Input.GetAxis("Horizontal");
        }
        else
        {
            // control táctil simple: arrastrar horizontalmente en pantalla para moverse.
            // se convierte delta de dedo en movimiento horizontal
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                {
                    horizontal = t.deltaPosition.x * touchSensitivity;
                }
            }
        }

        // actualizar objetivo lateral en base al input
        targetX += horizontal * lateralSpeed * Time.deltaTime;
        // restringir dentro de lateralLimit (centro en 0)
        targetX = Mathf.Clamp(targetX, -lateralLimit, lateralLimit);
    }

    void MoveJeep()
    {
        // interpolación suave de la posición X
        float newX = Mathf.SmoothDamp(transform.position.x, targetX, ref velocity.x, 1f / smoothLateral, lateralSpeed, Time.fixedDeltaTime);

        // movimiento hacia adelante constante en la dirección local forward
        Vector3 forwardMove = transform.forward * forwardSpeed * Time.fixedDeltaTime;

        // nuevo position combinada: mantenemos la Y y Z calculada por forward
        Vector3 newPosition = transform.position;
        newPosition += forwardMove;
        newPosition.x = newX;

        // mover con NavMeshAgent (usa agent.Move para respetar colisiones con NavMesh?)
        // agent.Move aplica movimiento relativo; calculamos deltas:
        Vector3 delta = newPosition - transform.position;
        agent.Move(delta);

        // fallback: si no quieres NavMeshAgent usa:
        // transform.position = newPosition;

        // orientamos el jeep ligeramente según velocidad lateral (opcional, para feedback)
        float tilt = Mathf.Clamp((targetX - transform.position.x) * 4f, -15f, 15f);
        Quaternion targetRot = Quaternion.Euler(0f, transform.eulerAngles.y, -tilt);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.fixedDeltaTime * 6f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            // notificar al GameManager
            GameManager.Instance?.OnPlayerHitObstacle();
        }
    }
}
