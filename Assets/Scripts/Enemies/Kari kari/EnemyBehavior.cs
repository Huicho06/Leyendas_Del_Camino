using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class EnemyBehavior : MonoBehaviour
{
    [Header("Configuración del enemigo")]
    public float detectionRange = 15f; // Distancia máxima para detectar al jugador
    public Transform player;           // Asigna el Player en el Inspector

    [Header("Velocidad")]
    public float minSpeed = 3f;        // Velocidad mínima
    public float maxSpeed = 9f;        // Velocidad máxima

    [Header("Audio de pasos")]
    public AudioSource footstepAudio;
    public AudioClip footstepClip;
    [Range(0f, 1f)]
    public float footstepVolume = 1f;

    [Header("Patrullaje")]
    public Transform[] patrolPoints;
    private int currentPatrolIndex = 0;

    private NavMeshAgent agent;
    private bool isFrozen = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (footstepAudio == null)
            footstepAudio = GetComponent<AudioSource>();

        if (footstepAudio != null)
        {
            footstepAudio.loop = true;
            footstepAudio.playOnAwake = false;
            footstepAudio.volume = footstepVolume;

            if (footstepClip != null)
                footstepAudio.clip = footstepClip;
        }
    }

    void Update()
    {
        if (player == null || agent == null) return;

        if (isFrozen)
        {
            agent.isStopped = true;
            StopFootsteps();
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        // Si el jugador está dentro del rango de detección → perseguir
        if (distance <= detectionRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            // Ajustar velocidad según proximidad
            float t = Mathf.Clamp01(1f - (distance / detectionRange));
            agent.speed = Mathf.Lerp(minSpeed, maxSpeed, t);

            PlayFootsteps();
        }
        else
        {
            // Patrullaje
            Patrol();
        }
    }

    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            agent.speed = maxSpeed;
            PlayFootsteps();
        }
    }

    public void Freeze(bool state)
    {
        isFrozen = state;
        if (state)
        {
            agent.ResetPath();
            StopFootsteps();
        }
    }

    private void PlayFootsteps()
    {
        if (footstepAudio == null) return;

        if (!footstepAudio.isPlaying && agent.velocity.magnitude > 0.1f)
            footstepAudio.Play();
    }

    private void StopFootsteps()
    {
        if (footstepAudio == null) return;
        if (footstepAudio.isPlaying)
            footstepAudio.Stop();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == player)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
