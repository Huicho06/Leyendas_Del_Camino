using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

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
    public PlayableDirector playableDirector;

    [Header("Opcional: canvas que se cerrará al terminar la cinemática")]
    [SerializeField]
    private GameObject canvasToClose;

    [Header("Opcional: silenciar audio durante la cinemática")]
    public bool muteAudioDuringCinematic = true;

    // 🔹 NUEVO
    [Header("Objeto a activar al terminar el evento final")]
    public GameObject objetoFinalActivar; // ← aquí arrastras la pared, trigger o puerta que quieres activar

    private int notasRecogidas = 0;
    private bool cinematicaActiva = false;

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

        if (canvasToClose == null)
            canvasToClose = GameObject.Find("Canvas");

        // 🔹 NUEVO: aseguramos que el objeto esté apagado al inicio
        if (objetoFinalActivar != null)
            objetoFinalActivar.SetActive(false);
    }

    public void AgregarNotaPuzzle()
    {
        notasRecogidas++;
        Debug.Log($"🧩 Nota número {notasRecogidas} recogida.");

        if (imagenCompleta != null && !imagenCompleta.activeSelf)
            imagenCompleta.SetActive(true);

        switch (notasRecogidas)
        {
            case 1: if (parte1 != null) parte1.SetActive(true); break;
            case 2: if (parte2 != null) parte2.SetActive(true); break;
            case 3: if (parte3 != null) parte3.SetActive(true); break;
        }

        // 🔹 NUEVO: si es la última nota, inicia la cinemática
        if (EsUltimaNota())
        {
            StartCoroutine(CinematicaFinal());
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

        var playerController = playerCamera.GetComponentInParent<MonoBehaviour>();
        if (playerController != null && playerController.enabled)
            playerController.enabled = false;

        if (canvasToClose != null)
            canvasWasActive = canvasToClose.activeSelf;

        if (muteAudioDuringCinematic)
            MuteAllAudio();

        if (playableDirector != null)
        {
            playableDirector.gameObject.SetActive(true);
            playableDirector.stopped += OnCinematicEnd;
            yield return null;
            playableDirector.Play();
        }
        else
        {
            Debug.LogError("❌ No hay PlayableDirector asignado en NotePuzzleManager.");
        }
    }

    private void OnCinematicEnd(PlayableDirector director)
    {
        Debug.Log("⏹ Cinemática finalizada.");

        if (muteAudioDuringCinematic)
            RestoreAudio();

        if (canvasToClose != null && canvasWasActive)
            canvasToClose.SetActive(false);

        var playerController = playerCamera.GetComponentInParent<MonoBehaviour>();
        if (playerController != null)
            playerController.enabled = true;

        director.stopped -= OnCinematicEnd;

        // 🔹 NUEVO: Activar el objeto final al terminar la cinemática
        if (objetoFinalActivar != null)
        {
            objetoFinalActivar.SetActive(true);
            Debug.Log("✨ Objeto final activado tras la cinemática.");
        }
    }

    private void MuteAllAudio()
    {
        audioSources.Clear();
        audioSourcesStates.Clear();
        foreach (var source in FindObjectsOfType<AudioSource>())
        {
            audioSources.Add(source);
            audioSourcesStates.Add(source.enabled);
            source.enabled = false;
        }

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
