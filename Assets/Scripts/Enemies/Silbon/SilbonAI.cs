using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NavMeshAgent))]
[DisallowMultipleComponent]
public class SilbonAI : MonoBehaviour
{
    [Header("Jugador (opcional)")]
    public Transform player; // si está vacío, se buscará por Tag=Player

    [Header("Patrulla")]
    public Transform[] patrolPoints;
    public float patrolWait = 1.5f;
    public float patrolSpeed = 3.5f;
    public float investigateSpeed = 4.5f;

    [Header("Detección por oído")]
    public float hearingRange = 20f;
    [Range(0f, 5f)] public float hearingThreshold = 0.5f;
    public float investigateTime = 4f;

    [Header("Visión (FOV)")]
    public float viewRange = 18f;
    [Range(1f, 180f)] public float viewFOV = 70f;
    [Tooltip("Capas que bloquean la vista (paredes/suelo). NO incluyas la capa del Player.")]
    public LayerMask obstacleMask = ~0;

    [Header("Proximidad")]
    public float proximityKillRange = 1.5f;
    public float killDelay = 0.2f;

    [Header("Silbido (paradoja inversa)")]
    public AudioClip whistleClip;
    public float whistleInterval = 10f;
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    public float nearDistance = 5f;
    public float farDistance = 35f;
    [Range(0f, 1f)] public float minVolumeWhenNear = 0.1f;
    [Range(0f, 1f)] public float maxVolumeWhenFar = 1f;
    public bool useLowPass = true;
    public int lowPassCutoffNear = 900;
    public int lowPassCutoffFar = 5000;

    [Header("Persecución por linterna/visión")]
    public float chaseSpeed = 7f;
    public float chaseDuration = 5f;
    public float chaseCooldown = 0.3f;
    public float sightMemoryTime = 0.6f;

    [Tooltip("Si el agro fue por linterna y pierdo visión mientras el jugador está AGACHADO, corto el agro rápido.")]
    public float crouchAgroDropDelay = 0.35f;

    private NavMeshAgent agent;
    private int patrolIndex = 0;

    private enum State { Patrolling, Investigating, Chasing }
    private State state = State.Patrolling;

    private enum Agro { None, Sound, Light, Sight }
    private Agro agro = Agro.None;

    private Vector3 investigatePosition;
    private Coroutine investigateCoroutine;
    private int currentTargetNoiseId = -1;

    private AudioSource whistleSource;
    private AudioLowPassFilter lpFilter;

    private float chaseEndTime = -1f;
    private float lastChaseTriggerTime = -999f;
    private float lastSeenLightTime = -999f;
    private float lastSeenPlayerTime = -999f;

    private PlayerMovement playerMove;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }

        if (player) playerMove = player.GetComponent<PlayerMovement>();

        whistleSource = GetComponent<AudioSource>();
        if (!whistleSource) whistleSource = gameObject.AddComponent<AudioSource>();
        whistleSource.playOnAwake = false;
        whistleSource.loop = false;
        whistleSource.spatialBlend = 1f;
        whistleSource.rolloffMode = AudioRolloffMode.Custom;
        AnimationCurve flat = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        whistleSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, flat);
        whistleSource.dopplerLevel = 0f;

        if (useLowPass)
        {
            lpFilter = GetComponent<AudioLowPassFilter>();
            if (!lpFilter) lpFilter = gameObject.AddComponent<AudioLowPassFilter>();
            lpFilter.enabled = true;
        }
    }

    void Start()
    {
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                agent.Warp(hit.position);
            else
            {
                Debug.LogError("[SilbonAI] No hay NavMesh bajo el enemigo.");
                enabled = false; return;
            }
        }

        agent.autoBraking = false;
        agent.speed = patrolSpeed;

        if (patrolPoints != null && patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[0].position);

        if (whistleClip != null)
            StartCoroutine(WhistleLoop());
    }

    void Update()
    {
        if (!agent) return;

        // Kill por proximidad
        if (player)
        {
            float distToPlayer = Vector3.Distance(transform.position, player.position);
            if (distToPlayer <= proximityKillRange)
                StartCoroutine(KillPlayer());
        }

        // VISIÓN: ¿lo veo ahora?
        bool los = CanSeePlayer();
        if (los) lastSeenPlayerTime = Time.time;

        // Prioridad: CHASING
        if (state == State.Chasing)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            if (player) agent.SetDestination(player.position);

            bool sawRecently = (Time.time - lastSeenPlayerTime) <= sightMemoryTime;

            bool stillAgro = los || sawRecently || (Time.time < chaseEndTime);

            // Si el agro fue por LINTERNa y el jugador está en sigilo (agachado) y ya NO hay visión,
            // soltamos el agro más rápido.
            if (agro == Agro.Light && !los && IsPlayerCrouching() && (Time.time - lastSeenLightTime) > crouchAgroDropDelay)
                stillAgro = false;

            if (!stillAgro)
            {
                agro = Agro.None;
                state = State.Patrolling;
                agent.speed = patrolSpeed;
                if (patrolPoints != null && patrolPoints.Length > 0)
                    agent.SetDestination(patrolPoints[patrolIndex].position);
            }

            return;
        }

        // Si lo veo y no estaba persiguiendo → comienzo persecución (por visión)
        if (los && state != State.Chasing)
        {
            BeginChase(Agro.Sight);
            return;
        }

        // Oír SOLO el último ruido (si existe NoiseManager)
        if (NoiseManager.Instance != null &&
            NoiseManager.Instance.GetMostRecentNoise(transform.position, hearingRange, hearingThreshold,
                                                     out Vector3 pos, out int noiseId))
        {
            if (noiseId != currentTargetNoiseId)
            {
                currentTargetNoiseId = noiseId;
                if (state != State.Investigating) StartInvestigate(pos);
                else { investigatePosition = pos; agent.SetDestination(investigatePosition); }
            }
        }

        // Patrulla
        if (state == State.Patrolling && !agent.pathPending && patrolPoints != null && patrolPoints.Length > 0)
        {
            if (agent.remainingDistance <= agent.stoppingDistance + 0.05f)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                StartCoroutine(WaitAndGoTo(patrolPoints[patrolIndex].position));
            }
        }
    }

    bool CanSeePlayer()
    {
        if (!player) return false;

        Vector3 toPlayer = player.position - transform.position;
        float dist = toPlayer.magnitude;
        if (dist > viewRange) return false;

        Vector3 dir = toPlayer.normalized;
        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > viewFOV * 0.5f) return false; // fuera del cono

        // Raycast de línea de visión (bloqueos)
        if (Physics.Raycast(transform.position + Vector3.up * 1.6f, dir, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
        {
            // bloqueado por obstáculo
            if (((1 << hit.collider.gameObject.layer) & obstacleMask) != 0)
                return false;

            // Si lo primero que golpeo no es el jugador, tampoco lo veo.
            if (!hit.collider.transform.IsChildOf(player))
                return false;
        }

        return true;
    }

    bool IsPlayerCrouching()
    {
        if (!playerMove && player) playerMove = player.GetComponent<PlayerMovement>();
        return playerMove ? playerMove.IsCrouching : false;
    }

    void BeginChase(Agro cause)
    {
        agro = cause;
        state = State.Chasing;
        chaseEndTime = Time.time + Mathf.Max(0.5f, chaseDuration);

        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }

        if (player) playerMove = player.GetComponent<PlayerMovement>();

        if (agent && player)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
        }
    }

    IEnumerator WaitAndGoTo(Vector3 dest)
    {
        yield return new WaitForSeconds(patrolWait);
        agent.speed = patrolSpeed;
        agent.isStopped = false;
        agent.SetDestination(dest);
    }

    void StartInvestigate(Vector3 pos)
    {
        investigatePosition = pos;
        state = State.Investigating;

        if (investigateCoroutine != null) StopCoroutine(investigateCoroutine);
        investigateCoroutine = StartCoroutine(DoInvestigate());
    }

    IEnumerator DoInvestigate()
    {
        agent.speed = investigateSpeed;
        agent.isStopped = false;
        agent.SetDestination(investigatePosition);
        float start = Time.time;

        while (Time.time - start < investigateTime)
        {
            // Si durante la investigación lo veo → persecución
            if (CanSeePlayer())
            {
                BeginChase(Agro.Sight);
                yield break;
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f) break;
            yield return null;
        }

        state = State.Patrolling;
        agent.speed = patrolSpeed;
        if (patrolPoints != null && patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    IEnumerator KillPlayer()
    {
        if (!player) yield break;
        yield return new WaitForSeconds(killDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator WhistleLoop()
    {
        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
        while (true)
        {
            PlayInvertedWhistle();
            yield return new WaitForSeconds(whistleInterval);
        }
    }

    void PlayInvertedWhistle()
    {
        if (!whistleClip) return;

        float dist = player ? Vector3.Distance(transform.position, player.position) : farDistance;
        float t = Mathf.InverseLerp(nearDistance, farDistance, dist);
        float volume = Mathf.Lerp(minVolumeWhenNear, maxVolumeWhenFar, t);

        if (useLowPass && lpFilter != null)
            lpFilter.cutoffFrequency = Mathf.Lerp(lowPassCutoffNear, lowPassCutoffFar, t);

        whistleSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        whistleSource.PlayOneShot(whistleClip, volume);
    }

    public void OnLitByFlashlight(Transform lightSource, float intensity = 1f)
    {
        lastSeenLightTime = Time.time;
        if (Time.time < lastChaseTriggerTime + chaseCooldown) return;

        lastChaseTriggerTime = Time.time;
        // inicia persecución por linterna
        BeginChase(Agro.Light);
    }

    void OnDrawGizmosSelected()
    {
        // Oído y kill
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, proximityKillRange);

        // Visión
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Vector3 left = Quaternion.Euler(0, -viewFOV * 0.5f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewFOV * 0.5f, 0) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + left * viewRange);
        Gizmos.DrawLine(transform.position, transform.position + right * viewRange);
    }
}
