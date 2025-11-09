using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class SceneTrigger : MonoBehaviour
{
    public PlayableDirector director;
    public GameObject cinematicCamera;
    public GameObject playerCamera;

    [Header("Opcional: canvas que se cerrará al terminar la animación")]
    public GameObject canvasToClose;

    [Header("Opcional: silenciar audio durante la animación")]
    public bool muteAudioDuringCinematic = true;

    [Header("Opcional: cambiar de escena al finalizar la animación")]
    public bool changeSceneOnEnd = false;
    [SerializeField] private string sceneToLoad = ConstantGames.BENI; // Puedes cambiar el valor por defecto

    bool triggered = false;

    // Para restaurar estados
    private List<AudioSource> audioSources = new List<AudioSource>();
    private List<bool> audioSourcesStates = new List<bool>();

    private List<AudioListener> audioListeners = new List<AudioListener>();
    private List<bool> audioListenersStates = new List<bool>();

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (other.CompareTag("Player"))
        {
            triggered = true;

            // Cambia cámaras
            playerCamera.SetActive(false);
            cinematicCamera.SetActive(true);

            // Silencia audio si está activado
            if (muteAudioDuringCinematic)
                MuteAllAudio();

            // Reproduce Timeline
            director.Play();
            director.stopped += OnTimelineEnd;
        }
    }

    void OnTimelineEnd(PlayableDirector d)
    {
        // Vuelve a la cámara del jugador
        cinematicCamera.SetActive(false);
        playerCamera.SetActive(true);

        // Cierra el canvas si está asignado
        if (canvasToClose != null)
            canvasToClose.SetActive(false);

        // Restaura audio
        if (muteAudioDuringCinematic)
            RestoreAudio();

        // Cambia de escena si está activado
        if (changeSceneOnEnd && !string.IsNullOrEmpty(sceneToLoad))
        {
            // Usa tu sistema de carga con pantalla de carga
            if (LoaderScene.Instance != null)
            {
                LoaderScene.Instance.LoadSceneString(sceneToLoad);
            }
            else
            {
                Debug.LogError("No existe una instancia activa de LoaderScene en la escena.");
            }
        }

        director.stopped -= OnTimelineEnd;
    }

    void MuteAllAudio()
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

    void RestoreAudio()
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
