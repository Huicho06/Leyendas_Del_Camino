

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

    [Header("Teletransporte sigiloso")]
    public float teleportCooldown = 8f;       // segundos entre teletransportes
    public float teleportDistance = 10f;      // distancia máxima detrás del jugador
    public float minTeleportRange = 5f;       // distancia mínima de aparición
    public float teleportHeightOffset = 0.5f; // para evitar clip con el suelo

    private float lastTeleportTime = -Mathf.Infinity;

    private NavMeshAgent agent;
    private Animator anim;
    private int idx = 0;
    private Transform player;

    private enum State { Patrolling, Investigating }
    private State state = State.Patrolling;

    private Vector3 investigatePosition;
    private Coroutine investigateCoroutine;

    // recordamos el último ruido objetivo
    private int currentTargetNoiseId = -1;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        agent.speed = 3.5f;
        agent.stoppingDistance = proximityKillRange;
        if (patrolPoints != null && patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[0].position);
    }

    void Update()
    {
        UpdateAnimationState();

        if (player != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, player.position);
            if (distToPlayer <= proximityKillRange)
            {
                StartCoroutine(KillPlayer());
            }
        }

        // detección de ruido
        if (NoiseManager.Instance != null &&
            NoiseManager.Instance.GetMostRecentNoise(transform.position, hearingRange, hearingThreshold, out Vector3 pos, out int noiseId))
        {
            if (noiseId != currentTargetNoiseId)
            {
                currentTargetNoiseId = noiseId;
                if (state != State.Investigating)
                {
                    StartInvestigate(pos);
                }
                else
                {
                    investigatePosition = pos;
                    agent.SetDestination(investigatePosition);
                }
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

        // teletransporte sigiloso (nuevo)
        TryTeleportNearPlayer();
    }

    void UpdateAnimationState()
    {
        if (anim == null || agent == null) return;

        bool walking = agent.velocity.magnitude > 0.1f;
        anim.SetBool("isWalking", walking);
        anim.SetBool("isIdle", !walking);
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

        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // --- NUEVAS FUNCIONES ---

    bool PlayerIsLookingAtEnemy()
    {
        if (player == null) return false;

        Vector3 dirToEnemy = (transform.position - player.position).normalized;
        float angle = Vector3.Angle(player.forward, dirToEnemy);

        // si está dentro de 50°, consideramos que el jugador la ve
        return angle < 50f;
    }

    void TryTeleportNearPlayer()
    {
        if (player == null) return;
        if (Time.time - lastTeleportTime < teleportCooldown) return;
        if (PlayerIsLookingAtEnemy()) return; // no se teletransporta si la ves

        Vector3 randomDir = -player.forward + new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
        randomDir.Normalize();

        Vector3 targetPos = player.position + randomDir * Random.Range(minTeleportRange, teleportDistance);
        targetPos.y += teleportHeightOffset;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            lastTeleportTime = Time.time;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, proximityKillRange);
    }
}
