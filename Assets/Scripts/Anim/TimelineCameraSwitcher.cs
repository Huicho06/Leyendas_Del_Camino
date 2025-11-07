using UnityEngine;
using UnityEngine.Playables;

public class TimelineCameraSwitcher : MonoBehaviour
{
    public PlayableDirector director;
    public GameObject playerCamera;
    public GameObject cinematicCamera;

    void Start()
    {
        // Asegura que al iniciar el juego esté activa la cámara del jugador
        playerCamera.SetActive(true);
        cinematicCamera.SetActive(false);

        // Cuando la timeline termine, ejecutará el método
        director.stopped += OnTimelineEnd;
    }

    public void PlayCinematic()
    {
        // Activa la cámara de la cinemática
        playerCamera.SetActive(false);
        cinematicCamera.SetActive(true);

        // Reproduce la timeline
        director.Play();
    }

    void OnTimelineEnd(PlayableDirector d)
    {
        // Al finalizar, vuelve a la cámara del jugador
        cinematicCamera.SetActive(false);
        playerCamera.SetActive(true);
    }
}
