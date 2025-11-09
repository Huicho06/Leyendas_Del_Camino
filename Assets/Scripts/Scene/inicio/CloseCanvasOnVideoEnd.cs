using UnityEngine;
using UnityEngine.Video;

public class CloseCanvasOnVideoEnd : MonoBehaviour
{
    [Header("Referencia al VideoPlayer")]
    public VideoPlayer videoPlayer;

    [Header("Canvas o GameObject que se cerrará al finalizar el video")]
    public GameObject canvasToClose;

    void Start()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (videoPlayer != null)
        {
            // Se ejecuta cuando termina el video
            videoPlayer.loopPointReached += OnVideoEnd;
        }
        else
        {
            Debug.LogError("No se encontró un VideoPlayer asignado en CloseCanvasOnVideoEnd.");
        }
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        if (canvasToClose != null)
        {
            canvasToClose.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false); // si no asignas nada, se desactiva el propio objeto
        }
    }
}
