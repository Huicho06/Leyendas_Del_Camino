
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AudioSource))]
public class SoulAI : MonoBehaviour
{
    [Header("Identidad")]
    public bool isTrueSoul = false;
    public string soulName = "Ajayu";

    [Header("Apariencia")]
    public Renderer soulRenderer;
    public Color trueColor = new Color(1f, 0.92f, 0.6f);
    public Color falseColor = new Color(1f, 0.2f, 0.2f);
    public float highlightIntensity = 2f;
    [Range(0f, 1f)] public float emissionBlend = 1f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip voiceClip;       // canto o voz de la esposa / clip de aproximación
    public AudioClip shoutClip;       // clip que gritan las almas enemigas al detectar
    public bool playOnApproach = true;

    [Header("Detección / Activación (alma buena)")]
    public float hearingDistance = 20f;     // empieza a oír el canto
    public float detectionDistance = 10f;   // alma te reconoce y anima

    [Header("Enemigos (NavMesh)")]
    public NavMeshAgent agent;              // para almas enemigas
    public float wanderRadius = 12f;
    public float wanderInterval = 4f;
    public float chaseDistance = 15f;       // si el jugador entra en este rango se agresivan
    public float attackRange = 2.2f;        // distancia de ataque (puedes aplicar daño)

    [Header("Desaparición por luz")]
    public float lightExposureTime = 2f;    // tiempo bajo la luz para disolver
    public float dissolveDuration = 1f;     // duracion anim disolve
    public string dissolveProperty = "_Dissolve"; // nombre del float en shader

    [Header("Memoria y Movimiento (alma buena)")]
    public Animator memoryAnimator;
    public Animator soulAnimator;           // anim por detectar (giro, brillar)
    public PathFollower pathFollower;
    public Transform player;
    public float detectionAnimationDelay = 2.5f; // espera antes de iniciar ruta

    // estado interno
    Renderer[] rends;
    Material[] instancedMats;
    bool hasStartedSinging = false;
    bool hasDetectedPlayer = false;

    // enemigo estado
    bool isAggressive = false;
    bool isDissolving = false;
    float lightTimer = 0f;
    Coroutine wanderRoutine;

    void Awake()
    {
        if (!soulRenderer) soulRenderer = GetComponentInChildren<Renderer>();
        audioSource = GetComponent<AudioSource>();

        // audio 3D básico
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = 0.9f;

        if (soulRenderer)
        {
            rends = soulRenderer.GetComponentsInChildren<Renderer>();
            // forzar instanciar materiales para poder modificar sin tocar sharedMaterials
            instancedMats = rends.SelectMany(r =>
            {
                var mats = r.materials; // this clones materials for this renderer
                // return array of clones
                return mats;
            }).ToArray();
        }

        if (!agent)
            agent = GetComponent<NavMeshAgent>();

        // si es enemigo y tiene agente, arrancar wandering
        if (!isTrueSoul && agent)
        {
            agent.stoppingDistance = 0.5f;
            wanderRoutine = StartCoroutine(WanderLoop());
        }
    }

    void Update()
    {
        if (isDissolving) return;
        if (!player) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (isTrueSoul)
        {
            // fase 1: se oye el canto
            if (!hasStartedSinging && dist <= hearingDistance)
            {
                hasStartedSinging = true;
                if (playOnApproach && voiceClip) StartSinging();
            }

            // fase 2: detecta y anima
            if (!hasDetectedPlayer && dist <= detectionDistance)
            {
                hasDetectedPlayer = true;
                StopSinging();
                if (soulAnimator) soulAnimator.SetTrigger("OnDetectPlayer");
                if (memoryAnimator) memoryAnimator.SetTrigger("OnDetectPlayer");
                StartCoroutine(DelayedStartGuide(detectionAnimationDelay));
            }
        }
        else
        {
            // comportamiento enemigo: si detecta al jugador, agresivar y perseguir
            if (!isAggressive && dist <= chaseDistance)
            {
                isAggressive = true;
                if (wanderRoutine != null) StopCoroutine(wanderRoutine);
                if (shoutClip && audioSource) audioSource.PlayOneShot(shoutClip);
            }

            if (isAggressive && agent)
            {
                agent.SetDestination(player.position);

                // si está muy cerca, atacar (aquí pones tu propia lógica de daño)
                if (dist <= attackRange)
                {
                    // ejemplo simple: mirar y detenerse
                    agent.isStopped = true;
                    transform.LookAt(player);
                    // TODO: llamar al método de daño del jugador
                }
                else
                {
                    agent.isStopped = false;
                }
            }
        }
    }

    IEnumerator DelayedStartGuide(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isTrueSoul)
        {
            if (playOnApproach && voiceClip) StartSinging();
            if (pathFollower) pathFollower.BeginRoute();
        }
    }

    // Wander loop para almas enemigas
    IEnumerator WanderLoop()
    {
        while (true)
        {
            if (agent && !isAggressive)
            {
                Vector3 newPos = RandomNavSphere(transform.position, wanderRadius);
                agent.SetDestination(newPos);
            }
            yield return new WaitForSeconds(wanderInterval);
        }
    }

    // helper para obtener punto aleatorio en NavMesh
    public static Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        Vector3 randDir = Random.insideUnitSphere * dist;
        randDir += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDir, out navHit, dist, NavMesh.AllAreas);
        return navHit.position;
    }

    // llamado desde FlashlightController
    public void OnIlluminated(bool illuminated, Transform flashlightTransform)
    {
        if (isDissolving) return;

        // actualización visual de emisión
        Color target = illuminated ? (isTrueSoul ? trueColor : falseColor) : Color.black;
        foreach (var r in rends)
        {
            foreach (var m in r.materials)
            {
                if (m.HasProperty("_EmissionColor"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", Color.Lerp(Color.black, target * highlightIntensity, illuminated ? emissionBlend : 0f));
                }
            }
        }

        // sonido breve al iluminar
        if (illuminated && isTrueSoul && audioSource && voiceClip && !audioSource.isPlaying)
        {
            audioSource.clip = voiceClip;
            audioSource.Play();
        }

        // enemigos: acumulador de tiempo bajo la luz
        if (!isTrueSoul)
        {
            if (illuminated)
            {
                lightTimer += Time.deltaTime;
                // opcional: cuando empieza a recibir la luz, frena y mira a la linterna
                if (lightTimer > 0f && agent) agent.isStopped = true;

                if (lightTimer >= lightExposureTime)
                {
                    // iniciar disolver
                    if (!isDissolving) StartCoroutine(DissolveAndDestroy());
                }
            }
            else
            {
                if (lightTimer > 0f)
                {
                    // salir de la luz: reiniciar contador y reanudar patrulla o persecución
                    lightTimer = 0f;
                    if (agent) agent.isStopped = false;
                    if (!isAggressive && wanderRoutine == null) wanderRoutine = StartCoroutine(WanderLoop());
                }
            }
        }
    }

    IEnumerator DissolveAndDestroy()
    {
        isDissolving = true;
        // parar agente
        if (agent) agent.isStopped = true;
        // reproducir sonido de muerte opcional
        // gradual set float on materials
        float t = 0f;
        while (t < dissolveDuration)
        {
            t += Time.deltaTime;
            float v = Mathf.Clamp01(t / dissolveDuration);
            foreach (var m in instancedMats)
            {
                if (m.HasProperty(dissolveProperty))
                    m.SetFloat(dissolveProperty, v);
            }
            yield return null;
        }
        Destroy(gameObject);
    }
    // dentro de la clase SoulAI
    public void TriggerMemory()
    {
        if (memoryAnimator != null)
            memoryAnimator.SetTrigger("ShowMemory");
    }

    // utilitarios audio/animacion
    public void StartSinging()
    {
        if (audioSource && voiceClip)
        {
            audioSource.clip = voiceClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    public void StopSinging()
    {
        if (audioSource) audioSource.Stop();
    }
}
