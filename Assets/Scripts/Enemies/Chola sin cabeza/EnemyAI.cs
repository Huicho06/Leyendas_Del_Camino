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
    public float hearingThreshold = 0.5f; // intensidad mínima percibida
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
            // Comprobar proximidad letal
            float distToPlayer = Vector3.Distance(transform.position, player.position);
            if (distToPlayer <= proximityKillRange)
            {
                StartCoroutine(KillPlayer());
            }
        }

        if (NoiseManager.Instance != null)
        {
            // escuchar ruidos
            if (NoiseManager.Instance.GetBestNoise(transform.position, hearingRange, hearingThreshold, out Vector3 pos, out float intensity))
            {
                if (state != State.Investigating)
                    StartInvestigate(pos);
                else
                    investigatePosition = pos; // actualizar objetivo si hay ruido más fuerte
            }
        }

        // patrulla
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
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
                break;
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

        // Matar al jugador: desactivar controlador y reiniciar escena
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
