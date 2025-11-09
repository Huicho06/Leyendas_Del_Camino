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
    public float patrolSpeed = 4.5f;        // más rápido
    public float investigateSpeed = 6.5f;   // más rápido

    [Header("Detección por oído")]
    public float hearingRange = 20f;
    [Range(0f, 5f)] public float hearingThreshold = 0.5f;
    public float investigateTime = 4f;

    [Header("Sensibilidad según movimiento del jugador")]
    [Tooltip("Multiplicadores sobre el rango base según cómo se mueve el jugador")]
    public float runHearingMultiplier = 2.0f;
    public float walkHearingMultiplier = 1.0f;
    public float crouchHearingMultiplier = 0.4f;
    [Tooltip("Velocidad estimada a partir de la cual consideramos que está caminando")]
    public float walkSpeedThreshold = 1.3f;
    [Tooltip("Velocidad estimada a partir de la cual consideramos que está corriendo")]
    public float runSpeedThreshold = 3.0f;

    [Header("Visión (FOV)")]
    public float viewRange = 25f; // mayor rango de visión
    [Range(1f, 180f)] public float viewFOV = 85f; // más amplio
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
    public float chaseSpeed = 10f; // mucho más rápido
    public float chaseDuration = 6f;
    public float chaseCooldown = 0.3f;
    public float sightMemoryTime = 0.6f;
    [Tooltip("Si el agro fue por linterna y pierdo visión mientras el jugador está AGACHADO, corto el agro rápido.")]
    public float crouchAgroDropDelay = 0.35f;

    [Header("Retorno a patrulla")]
    public float returnToPatrolDelay = 3f;

    private NavMeshAgent agent;
    private Animator animator;
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
    private float lastStimulusTime = 0f;

    // referencia al movimiento del jugador (si lo tiene)
    private PlayerMovement playerMove;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }

        if (player) playerMove = player.GetComponent<PlayerMovement>();

        // audio
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

        lastStimulusTime = Time.time;
    }

    void Update()
    {
        if (!agent) return;

        // animación
        bool isWalking = agent.velocity.magnitude > 0.1f && agent.remainingDistance > agent.stoppingDistance;
        if (animator) animator.SetBool("isWalking", isWalking);

        // matar por proximidad
        if (player && Vector3.Distance(transform.position, player.position) <= proximityKillRange)
            StartCoroutine(KillPlayer());

        // VISIÓN
        bool los = CanSeePlayer();
        if (los)
        {
            lastSeenPlayerTime = Time.time;
            lastStimulusTime = Time.time;

            if (state != State.Chasing)
            {
                BeginChase(Agro.Sight);
                return;
            }
        }

        // AUDICIÓN (solo si NO lo ve)
        if (!los && NoiseManager.Instance != null)
        {
            // calculamos la distancia de oído según cómo se mueve el jugador
            float currentHearingRange = hearingRange;

            // velocidad real del jugador
            float playerSpeed = GetPlayerSpeed();
            bool isCrouching = IsPlayerCrouching();

            if (isCrouching)
            {
                currentHearingRange *= crouchHearingMultiplier;
            }
            else if (playerSpeed >= runSpeedThreshold)
            {
                currentHearingRange *= runHearingMultiplier;
            }
            else if (playerSpeed >= walkSpeedThreshold)
            {
                currentHearingRange *= walkHearingMultiplier;
            }
            else
            {
                // casi quieto → se oye muy poco
                currentHearingRange *= 0.3f;
            }

            if (NoiseManager.Instance.GetMostRecentNoise(
                transform.position, currentHearingRange, hearingThreshold,
                out Vector3 pos, out int noiseId))
            {
                if (noiseId != currentTargetNoiseId)
                {
                    currentTargetNoiseId = noiseId;
                    lastStimulusTime = Time.time;

                    if (state != State.Investigating && state != State.Chasing)
                    {
                        StartInvestigate(pos);
                        return;
                    }
                    else if (state == State.Investigating)
                    {
                        investigatePosition = pos;
                        agent.SetDestination(investigatePosition);
                    }
                }
            }
        }

        // volver a patrullar
        if (state != State.Patrolling && (Time.time - lastStimulusTime) > returnToPatrolDelay)
        {
            agro = Agro.None;
            state = State.Patrolling;
            agent.speed = patrolSpeed;
            if (patrolPoints != null && patrolPoints.Length > 0)
                agent.SetDestination(patrolPoints[patrolIndex].position);
        }

        // patrulla
        if (state == State.Patrolling && !agent.pathPending && patrolPoints != null && patrolPoints.Length > 0)
        {
            if (agent.remainingDistance <= agent.stoppingDistance + 0.05f)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                StartCoroutine(WaitAndGoTo(patrolPoints[patrolIndex].position));
            }
        }

        // persecución: seguir al jugador
        if (state == State.Chasing && player)
        {
            agent.SetDestination(player.position);
            if (Time.time > chaseEndTime)
            {
                state = State.Patrolling;
                agent.speed = patrolSpeed;
            }
        }
    }

    // ------------------------------------------------------------
    // UTILIDADES DEL JUGADOR
    // ------------------------------------------------------------
    float GetPlayerSpeed()
    {
        if (!player) return 0f;

        // primero probar rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb) return rb.velocity.magnitude;

        // luego charactercontroller
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc) return cc.velocity.magnitude;

        // si no hay nada, 0
        return 0f;
    }

    bool IsPlayerCrouching()
    {
        if (playerMove != null)
        {
            // tu PlayerMovement sí parece tener IsCrouching
            return playerMove.IsCrouching;
        }
        return false;
    }

    // ------------------------------------------------------------
    // VISIÓN
    // ------------------------------------------------------------
    bool CanSeePlayer()
    {
        if (!player) return false;

        var hideSys = player.GetComponent<PlayerHideSystem>();
        if (hideSys && hideSys.IsHidden) return false;

        Vector3 origin = transform.position + Vector3.up * 1.6f;
        Vector3 toPlayer = (player.position + Vector3.up * 1.2f) - origin;
        float dist = toPlayer.magnitude;
        if (dist > viewRange) return false;

        Vector3 dir = toPlayer.normalized;
        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > viewFOV * 0.5f) return false;

        // raycast para saber si hay pared en medio
        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
        {
            // si lo que golpeo NO es el player → no lo veo
            if (!hit.collider.transform.IsChildOf(player)) return false;
            // si la capa del hit está marcada como obstáculo → no lo veo
            if (((1 << hit.collider.gameObject.layer) & obstacleMask) != 0) return false;
        }

        return true;
    }

    // ------------------------------------------------------------

    void BeginChase(Agro cause)
    {
        agro = cause;
        state = State.Chasing;
        chaseEndTime = Time.time + Mathf.Max(0.5f, chaseDuration);
        lastStimulusTime = Time.time;

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
            if (CanSeePlayer())
            {
                BeginChase(Agro.Sight);
                yield break;
            }

            if ((Time.time - lastStimulusTime) > returnToPatrolDelay) break;

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

        // ✅ Detener por completo al Silbón
        if (agent)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            yield return new WaitForSeconds(0.05f); // pequeño delay para asegurar que se frene
            agent.enabled = false;
        }

        // ✅ Asegurarse de que el Silbón no empuje físicamente
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // ✅ Girar el Silbón hacia el jugador (mirada ligeramente elevada)
        Vector3 lookDir = (player.position - transform.position);
        lookDir.y += 1.2f; // 👁️ ahora mira más arriba (a la cara del jugador)
        lookDir.Normalize();

        Quaternion targetRot = Quaternion.LookRotation(lookDir);
        float rotateSpeed = 5f;
        float rotTime = 0f;
        while (rotTime < 0.6f)
        {
            rotTime += Time.deltaTime * rotateSpeed;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotTime);
            yield return null;
        }

        // ✅ Hacer que la cámara del jugador mire hacia el rostro del Silbón
        Camera playerCam = Camera.main;
        if (playerCam != null)
        {
            float t = 0f;
            Quaternion startRot = playerCam.transform.rotation;

            // Subimos la altura del punto de mira del Silbón para apuntar más arriba
            Vector3 targetPos = transform.position + Vector3.up * 2.4f; // 👈 más alto que antes
            Quaternion endRot = Quaternion.LookRotation(targetPos - playerCam.transform.position);

            while (t < 1f)
            {
                t += Time.deltaTime * 1.2f;
                playerCam.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
        }

        // ✅ Reproducir animación (solo si existe el parámetro)
        if (animator)
        {
            // Evitamos el error si no existe el parámetro
            if (HasAnimatorParameter(animator, "Attack"))
                animator.SetTrigger("Attack");
        }

        // ✅ Espera unos segundos para simular la "cinemática"
        yield return new WaitForSeconds(2.8f);

        // ✅ Reinicia la escena
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    bool HasAnimatorParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter p in anim.parameters)
        {
            if (p.name == paramName)
                return true;
        }
        return false;
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
        lastStimulusTime = Time.time;
        if (Time.time < lastChaseTriggerTime + chaseCooldown) return;

        lastChaseTriggerTime = Time.time;
        BeginChase(Agro.Light);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, proximityKillRange);

        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Vector3 left = Quaternion.Euler(0, -viewFOV * 0.5f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewFOV * 0.5f, 0) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + left * viewRange);
        Gizmos.DrawLine(transform.position, transform.position + right * viewRange);
    }
}
