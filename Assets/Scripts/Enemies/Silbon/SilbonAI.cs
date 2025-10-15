using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NavMeshAgent))]
[DisallowMultipleComponent]
public class SilbonAI : MonoBehaviour
{
    [Header("Patrulla")]
    public Transform[] patrolPoints;
    public float patrolWait = 1.5f;

    [Header("Detección por oído")]
    public float hearingRange = 20f;      // distancia máxima para oír
    [Range(0f, 5f)] public float hearingThreshold = 0.5f; // intensidad mínima (no atenuada)
    public float investigateTime = 4f;    // cuánto investiga

    [Header("Proximidad")]
    public float proximityKillRange = 1.5f;
    public float killDelay = 0.2f;

    [Header("Silbido (paradoja inversa)")]
    public AudioClip whistleClip;         // <-- arrastra aquí tu clip
    public float whistleInterval = 10f;   // cada cuántos segundos silba
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f); // leve variación
    [Tooltip("Distancia a la que lo consideramos 'cerca' para invertir el volumen")]
    public float nearDistance = 5f;
    [Tooltip("Distancia a la que lo consideramos 'lejos' para invertir el volumen")]
    public float farDistance = 35f;
    [Range(0f, 1f)] public float minVolumeWhenNear = 0.1f; // volumen cuando está MUY cerca (debe sonar lejano)
    [Range(0f, 1f)] public float maxVolumeWhenFar = 1.0f;  // volumen cuando está MUY lejos (debe sonar cercano)
    public bool useLowPass = true;        // opcional: engaña el oído cortando agudos
    public int lowPassCutoffNear = 900;   // cuando está cerca, suena filtrado (lejano)
    public int lowPassCutoffFar = 5000;   // cuando está lejos, suena claro (cercano)

    private NavMeshAgent agent;
    private int idx = 0;
    private Transform player;

    private enum State { Patrolling, Investigating }
    private State state = State.Patrolling;

    private Vector3 investigatePosition;
    private Coroutine investigateCoroutine;

    // Ruido objetivo (último)
    private int currentTargetNoiseId = -1;

    // Audio
    private AudioSource whistleSource;
    private AudioLowPassFilter lpFilter;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Configurar AudioSource para el silbido si no existe
        whistleSource = GetComponent<AudioSource>();
        if (!whistleSource)
            whistleSource = gameObject.AddComponent<AudioSource>();

        whistleSource.playOnAwake = false;
        whistleSource.loop = false;
        whistleSource.spatialBlend = 1f;               // 3D para paneo direccional
        whistleSource.rolloffMode = AudioRolloffMode.Custom;
        // Curva plana para que NO atenúe por distancia y nosotros mandemos con volume inverso
        AnimationCurve flat = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        whistleSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, flat);
        whistleSource.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, AnimationCurve.Constant(0f, 1f, 1f));
        whistleSource.dopplerLevel = 0f;

        if (useLowPass)
        {
            lpFilter = GetComponent<AudioLowPassFilter>();
            if (!lpFilter) lpFilter = gameObject.AddComponent<AudioLowPassFilter>();
            lpFilter.enabled = true;
            lpFilter.cutoffFrequency = lowPassCutoffFar;
        }
    }

    void Start()
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[0].position);

        if (whistleClip != null)
            StartCoroutine(WhistleLoop());
    }

    void Update()
    {
        // Matar por proximidad
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
            NoiseManager.Instance.GetMostRecentNoise(transform.position, hearingRange, hearingThreshold,
                                                     out Vector3 pos, out int noiseId))
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

    IEnumerator WhistleLoop()
    {
        // Pequeño offset inicial aleatorio para que no sea tan predecible
        yield return new WaitForSeconds(Random.Range(0.25f, 1.5f));

        while (true)
        {
            PlayInvertedWhistle();
            yield return new WaitForSeconds(whistleInterval);
        }
    }

    void PlayInvertedWhistle()
    {
        if (whistleClip == null) return;

        float dist = (player != null)
            ? Vector3.Distance(transform.position, player.position)
            : farDistance;

        // Normalizar distancia entre near y far [0..1] donde 0 = cerca, 1 = lejos
        float t = Mathf.InverseLerp(nearDistance, farDistance, dist);

        // Volumen invertido (cerca => volumen bajo; lejos => volumen alto)
        float volume = Mathf.Lerp(minVolumeWhenNear, maxVolumeWhenFar, t);

        // Opcional: también invertimos claridad con un LowPass (cerca => más filtrado)
        if (useLowPass && lpFilter != null)
        {
            float cutoff = Mathf.Lerp(lowPassCutoffNear, lowPassCutoffFar, t);
            lpFilter.cutoffFrequency = cutoff;
        }

        whistleSource.clip = whistleClip;
        whistleSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        whistleSource.volume = volume;

        // Disparo one-shot para permitir overlap si justo cambia la distancia
        whistleSource.PlayOneShot(whistleClip, volume);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, proximityKillRange);

        // Visualización de near/far para el silbido
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, nearDistance);
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, farDistance);
    }
}
