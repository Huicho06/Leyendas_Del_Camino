using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(AudioSource))]
public class PriestStoryTrigger : MonoBehaviour
{
    [Header("Configuración")]
    public string playerTag = "Player";
    public float activationDistance = 4f;

    [Header("Animación")]
    public string talkingBoolName = "IsTalking";
    private Animator animator;

    [Header("Audios de historia")]
    public AudioClip[] storyClips;
    private AudioSource audioSource;

    private Transform player;
    private bool hasTalked = false;
    private bool isTalking = false;

    // 🔒 Referencias al control del jugador
    private PlayerMovement playerMovement;
    private PlayerCameraController playerCamera;
    private AudioSource playerAudio;

    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerMovement = playerObj.GetComponent<PlayerMovement>();
            playerCamera = playerObj.GetComponentInChildren<PlayerCameraController>();
            playerAudio = playerObj.GetComponent<AudioSource>();
        }
    }

    void Update()
    {
        if (hasTalked || player == null || isTalking) return;

        float distance = Vector3.Distance(player.position, transform.position);
        if (distance <= activationDistance)
            StartCoroutine(StartConversation());
    }

    IEnumerator StartConversation()
    {
        isTalking = true;

        // 🔇 Silenciar al jugador
        if (playerAudio != null) playerAudio.mute = true;

        // 🚫 Bloquear movimiento y cámara
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerCamera != null) playerCamera.enabled = false;

        // Mirar al jugador
        Vector3 lookPos = player.position - transform.position;
        lookPos.y = 0;
        transform.rotation = Quaternion.LookRotation(lookPos);

        // Activar animación
        animator.SetBool(talkingBoolName, true);

        // Reproducir audios en orden
        foreach (AudioClip clip in storyClips)
        {
            audioSource.clip = clip;
            audioSource.Play();
            yield return new WaitForSeconds(clip.length + 0.2f);
        }

        // Terminar animación
        animator.SetBool(talkingBoolName, false);

        // 🔓 Reactivar control del jugador
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerCamera != null) playerCamera.enabled = true;
        if (playerAudio != null) playerAudio.mute = false;

        isTalking = false;
        hasTalked = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
    }
}
