using UnityEngine;
using UnityEngine.Playables;

public class SceneTrigger : MonoBehaviour
{
    public PlayableDirector director;
    public GameObject cinematicCamera;
    public GameObject playerCamera;

    bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (other.CompareTag("Player"))
        {
            triggered = true;

            // Cambia cámaras
            playerCamera.SetActive(false);
            cinematicCamera.SetActive(true);

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

        director.stopped -= OnTimelineEnd;
    }
}
