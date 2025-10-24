using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Patrulla")]
    public Transform[] patrolPoints;
    public float patrolWait = 1.5f;

    [Header("Detección por oído")]
    public float hearingRange = 20f;      // distancia máxima para oír
    public float hearingThreshold = 0.5f; // intensidad mínima (no atenuada)
    public float investigateTime = 4f;    // cuánto tiempo investiga en la posición del ruido

    [Header("Proximidad")]
    public float proximityKillRange = 1.5f; // distancia a la que el jugador muere instantáneamente
    public float killDelay = 0.2f;

    private NavMeshAgent agent;
    private int idx = 0;
    private Transform player;

    private enum State { Patrolling, Investigating }
    private State state = State.Patrolling;

    private Vector3 investigatePosition;
    private Coroutine investigateCoroutine;

    // NUEVO: recordamos el último ruido "objetivo" por su ID
    private int currentTargetNoiseId = -1;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (patrolPoints != null && patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[0].position);
    }

    void Update()
    {
        if (player != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, player.position);
            if (distToPlayer <= proximityKillRange)
            {
                StartCoroutine(KillPlayer());
            }
        }

        // Oír SOLO el ruido más reciente dentro de rango/umbral
        if (NoiseManager.Instance != null &&
            NoiseManager.Instance.GetMostRecentNoise(transform.position, hearingRange, hearingThreshold, out Vector3 pos, out int noiseId))
        {
            // Si este ruido es nuevo (ID distinto), saltamos directo a él
            if (noiseId != currentTargetNoiseId)
            {
                currentTargetNoiseId = noiseId;
                if (state != State.Investigating)
                {
                    StartInvestigate(pos);
                }
                else
                {
                    // Ya estamos investigando: redirigimos inmediatamente al nuevo último ruido
                    investigatePosition = pos;
                    agent.SetDestination(investigatePosition);
                }
            }
        }

        // Patrulla
        if (state == State.Patrolling && !agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                idx = (idx + 1) % patrolPoints.Length;
                StartCoroutine(WaitAndGoTo(patrolPoints[idx].position));
            }
        }
    }

    IEnumerator WaitAndGoTo(Vector3 dest)
    {
        yield return new WaitForSeconds(patrolWait);
        agent.SetDestination(dest);
    }

    void StartInvestigate(Vector3 pos)
    {
        investigatePosition = pos;
        state = State.Investigating;

        if (investigateCoroutine != null)
            StopCoroutine(investigateCoroutine);

        investigateCoroutine = StartCoroutine(DoInvestigate());
    }

    IEnumerator DoInvestigate()
    {
        agent.SetDestination(investigatePosition);
        float start = Time.time;

        while (Time.time - start < investigateTime)
        {
            // Si ya llegamos cerca del punto actual, paramos la investigación
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
                break;

            // Nota: si aparece un ruido más reciente, Update() cambiará investigatePosition
            // y hará agent.SetDestination(investigatePosition). No reiniciamos la corrutina,
            // solo dejamos que siga hasta que llegue o se agote el tiempo.
            yield return null;
        }

        state = State.Patrolling;
        if (patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[idx].position);
    }

    IEnumerator KillPlayer()
    {
        if (player == null) yield break;

        yield return new WaitForSeconds(killDelay);

        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, proximityKillRange);
    }
}
