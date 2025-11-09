
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
    public AudioClip voiceClip;
    public AudioClip shoutClip;
    public bool playOnApproach = true;

    [Header("Detección / Alma buena")]
    public float hearingDistance = 20f;
    public float detectionDistance = 10f;
    public float detectionAnimationDelay = 2.5f;

    [Header("Enemigo (NavMesh)")]
    public NavMeshAgent agent;
    public float wanderRadius = 12f;
    public float wanderInterval = 4f;
    public float chaseDistance = 15f;

    [Header("Desaparición por luz")]
    public float lightExposureTime = 2f;
    public float dissolveDuration = 1f;
    public string dissolveProperty = "_Dissolve";

    [Header("Referencias externas")]
    public Animator memoryAnimator;
    public Animator soulAnimator;
    public PathFollower pathFollower;
    public Transform player;

    Renderer[] rends;
    Material[] instancedMats;
    bool hasStartedSinging = false;
    bool hasDetectedPlayer = false;
    bool isAggressive = false;
    bool isDissolving = false;
    float lightTimer = 0f;
    Coroutine wanderRoutine;

    [Header("Efecto de visión borrosa")]
    public UnityEngine.Rendering.Volume visionBlurVolume;
    public float fadeInSpeed = 2f;
    public float fadeOutSpeed = 1f;
    public float blurDuration = 2f;

    void Awake()
    {
        if (!soulRenderer) soulRenderer = GetComponentInChildren<Renderer>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = 0.9f;

        if (soulRenderer)
        {
            rends = soulRenderer.GetComponentsInChildren<Renderer>();
            instancedMats = rends.SelectMany(r => r.materials).ToArray();
        }

        if (!agent)
            agent = GetComponent<NavMeshAgent>();

        if (!isTrueSoul && agent)
        {
            agent.stoppingDistance = 0.5f;
            wanderRoutine = StartCoroutine(WanderLoop());
        }
    }

    void Update()
    {
        if (isDissolving || !player) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (isTrueSoul)
            HandleTrueSoul(dist);
        else
            HandleEnemySoul(dist);
    }

    void HandleTrueSoul(float dist)
    {
        // Empieza a cantar si el jugador está cerca
        if (!hasStartedSinging && dist <= hearingDistance)
        {
            hasStartedSinging = true;
            Debug.Log("💫 Alma buena empieza a cantar");
            if (playOnApproach && voiceClip) StartSinging();
        }

        // Detecta al jugador a distancia más corta
        if (!hasDetectedPlayer && dist <= detectionDistance)
        {
            hasDetectedPlayer = true;
            Debug.Log("👁️ Alma detectó al jugador");
            StopSinging();
            if (soulAnimator) soulAnimator.SetTrigger("OnDetectPlayer");
            if (memoryAnimator) memoryAnimator.SetTrigger("OnDetectPlayer");
            StartCoroutine(DelayedStartGuide(detectionAnimationDelay));
        }
    }

    void HandleEnemySoul(float dist)
    {
        if (!isAggressive && dist <= chaseDistance)
        {
            isAggressive = true;
            if (wanderRoutine != null) StopCoroutine(wanderRoutine);
            if (shoutClip) audioSource.PlayOneShot(shoutClip);
        }

        if (isAggressive && agent)
        {
            agent.SetDestination(player.position);

            if (dist <= agent.stoppingDistance + 0.5f)
            {
                StartCoroutine(SlowPlayerThenDisappear());
            }
        }
    }

    IEnumerator DelayedStartGuide(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isTrueSoul)
        {
            // Reanuda el canto después de la animación
            if (playOnApproach && voiceClip) StartSinging();
            if (pathFollower) pathFollower.BeginRoute();
        }
    }

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

    public static Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        Vector3 randDir = Random.insideUnitSphere * dist + origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDir, out navHit, dist, NavMesh.AllAreas);
        return navHit.position;
    }

    public void OnIlluminated(bool illuminated, Transform flashlightTransform)
    {
        if (isDissolving) return;

        Color target = illuminated ? (isTrueSoul ? trueColor : falseColor) : Color.black;
        foreach (var r in rends)
        {
            foreach (var m in r.materials)
            {
                if (m.HasProperty("_EmissionColor"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor",
                        Color.Lerp(Color.black, target * highlightIntensity, illuminated ? emissionBlend : 0f));
                }
            }
        }

        if (illuminated && isTrueSoul && voiceClip && !audioSource.isPlaying)
        {
            audioSource.clip = voiceClip;
            audioSource.loop = true;
            audioSource.Play();
        }

        if (!isTrueSoul)
        {
            if (illuminated)
            {
                lightTimer += Time.deltaTime;
                if (lightTimer > 0f && agent) agent.isStopped = true;

                if (lightTimer >= lightExposureTime && !isDissolving)
                    StartCoroutine(DissolveAndDestroy());
            }
            else
            {
                lightTimer = 0f;
                if (agent) agent.isStopped = false;
                if (!isAggressive && wanderRoutine == null)
                    wanderRoutine = StartCoroutine(WanderLoop());
            }
        }
    }

    IEnumerator SlowPlayerThenDisappear()
    {
        if (isDissolving) yield break;
        isDissolving = true;

        if (PlayerVisionEffect.instance != null)
            PlayerVisionEffect.instance.TriggerBlur(blurDuration);

        if (player != null)
        {
            var move = player.GetComponent<PlayerMovement>();
            if (move != null)
            {
                float originalWalk = move.walkSpeed;
                float originalRun = move.runSpeed;

                move.walkSpeed *= 0.5f;
                move.runSpeed *= 0.5f;

                yield return new WaitForSeconds(5f);

                move.walkSpeed = originalWalk;
                move.runSpeed = originalRun;
            }
        }

        yield return StartCoroutine(DissolveAndDestroy());
    }

    private IEnumerator ApplyBlurEffect()
    {
        if (visionBlurVolume == null)
            yield break;

        float weight = 0f;

        while (weight < 1f)
        {
            weight += Time.deltaTime * fadeInSpeed;
            visionBlurVolume.weight = weight;
            yield return null;
        }

        yield return new WaitForSeconds(blurDuration);

        while (weight > 0f)
        {
            weight -= Time.deltaTime * fadeOutSpeed;
            visionBlurVolume.weight = weight;
            yield return null;
        }

        visionBlurVolume.weight = 0f;
    }

    IEnumerator DissolveAndDestroy()
    {
        isDissolving = true;
        if (agent) agent.isStopped = true;

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

    public void TriggerMemory()
    {
        if (memoryAnimator) memoryAnimator.SetTrigger("ShowMemory");
    }

    public void StartSinging()
    {
        if (audioSource && voiceClip)
        {
            audioSource.clip = voiceClip;
            audioSource.loop = true;
            audioSource.spatialBlend = 1f; // 3D
            audioSource.Play();
        }
    }

    public void StopSinging()
    {
        if (audioSource) audioSource.Stop();
    }

    // --- Dibujar los rangos en la escena ---
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isTrueSoul ? new Color(0.3f, 1f, 0.6f, 0.3f) : new Color(1f, 0.3f, 0.3f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, hearingDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionDistance);
    }
}
