using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables; // 👈 Importante para usar Timeline

public class NotePuzzleManager : MonoBehaviour
{
    public static NotePuzzleManager instance;

    [Header("Referencias del puzzle")]
    public GameObject imagenCompleta;
    public GameObject parte1;
    public GameObject parte2;
    public GameObject parte3;

    [Header("Evento final")]
    public FinalLightEvent finalLightEvent;

    [Header("Jugador / Cámara")]
    public Transform playerCamera;
    public Transform luzObjetivo;

    [Header("Cinemática final")]
    public PlayableDirector playableDirector; // 🎬 Timeline de Unity

    [Header("Opcional: canvas que se cerrará al terminar la cinemática")]
    [SerializeField]
    private GameObject canvasToClose;

    [Header("Opcional: silenciar audio durante la cinemática")]
    public bool muteAudioDuringCinematic = true;

    private int notasRecogidas = 0;
    private bool cinematicaActiva = false;

    // Para restaurar estados de audio
    private List<AudioSource> audioSources = new List<AudioSource>();
    private List<bool> audioSourcesStates = new List<bool>();

    private List<AudioListener> audioListeners = new List<AudioListener>();
    private List<bool> audioListenersStates = new List<bool>();

    private bool canvasWasActive = false;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (imagenCompleta != null)
            imagenCompleta.SetActive(false);

        if (parte1 != null) parte1.SetActive(false);
        if (parte2 != null) parte2.SetActive(false);
        if (parte3 != null) parte3.SetActive(false);

        // Buscar automáticamente el canvas si no se asignó
        if (canvasToClose == null)
        {
            canvasToClose = GameObject.Find("Canvas"); // Cambia "Canvas" por el nombre real de tu canvas
        }
    }

    public void AgregarNotaPuzzle()
    {
        notasRecogidas++;
        Debug.Log($"🧩 Nota número {notasRecogidas} recogida.");

        if (imagenCompleta != null && !imagenCompleta.activeSelf)
            imagenCompleta.SetActive(true);

        switch (notasRecogidas)
        {
            case 1:
                if (parte1 != null) parte1.SetActive(true);
                break;
            case 2:
                if (parte2 != null) parte2.SetActive(true);
                break;
            case 3:
                if (parte3 != null) parte3.SetActive(true);
                break;
        }
    }

    public bool EsUltimaNota()
    {
        return notasRecogidas == 3 && !cinematicaActiva;
    }

    public IEnumerator CinematicaFinal()
    {
        cinematicaActiva = true;
        Debug.Log("🎬 Iniciando Cinemática Final (Timeline)...");

        // Bloquear control del jugador
        var playerController = playerCamera.GetComponentInParent<MonoBehaviour>();
        if (playerController != null && playerController.enabled)
        {
            playerController.enabled = false;
        }

        // Guardar estado del canvas
        if (canvasToClose != null)
        {
            canvasWasActive = canvasToClose.activeSelf;
        }

        // Silenciar audio si está activado
        if (muteAudioDuringCinematic)
        {
            MuteAllAudio();
        }

        // Ejecutar cinemática
        if (playableDirector != null)
        {
            Debug.Log("▶️ Ejecutando Timeline...");

            // Asegurarnos de que el director está activo
            playableDirector.gameObject.SetActive(true);

            // Suscribirse al evento antes de Play()
            playableDirector.stopped += OnCinematicEnd;

            // Esperamos un frame para que Unity registre la activación correctamente
            yield return null;

            playableDirector.Play();
        }
        else
        {
            Debug.LogError("❌ No hay PlayableDirector asignado en NotePuzzleManager.");
        }

        yield return null;
    }

    private void OnCinematicEnd(PlayableDirector director)
    {
        Debug.Log("⏹ Cinemática finalizada.");

        // Restaurar audio
        if (muteAudioDuringCinematic)
        {
            RestoreAudio();
        }

        // Cerrar canvas solo si estaba activo
        if (canvasToClose != null && canvasWasActive)
        {
            canvasToClose.SetActive(false);
        }

        // Reactivar control del jugador
        var playerController = playerCamera.GetComponentInParent<MonoBehaviour>();
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        // Desuscribirse del evento
        director.stopped -= OnCinematicEnd;
    }

    private void MuteAllAudio()
    {
        // AudioSources
        audioSources.Clear();
        audioSourcesStates.Clear();
        foreach (var source in FindObjectsOfType<AudioSource>())
        {
            audioSources.Add(source);
            audioSourcesStates.Add(source.enabled);
            source.enabled = false;
        }

        // AudioListeners
        audioListeners.Clear();
        audioListenersStates.Clear();
        foreach (var listener in FindObjectsOfType<AudioListener>())
        {
            audioListeners.Add(listener);
            audioListenersStates.Add(listener.enabled);
            listener.enabled = false;
        }
    }

    private void RestoreAudio()
    {
        for (int i = 0; i < audioSources.Count; i++)
        {
            if (audioSources[i] != null)
                audioSources[i].enabled = audioSourcesStates[i];
        }

        for (int i = 0; i < audioListeners.Count; i++)
        {
            if (audioListeners[i] != null)
                audioListeners[i].enabled = audioListenersStates[i];
        }
    }
}
