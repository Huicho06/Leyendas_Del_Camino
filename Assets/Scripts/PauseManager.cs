using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseOverlay;     // Contiene Blur + Tint + Menú
    [SerializeField] private RawImage blurImage;          // Asigna RawImage_Blur aquí

    [Header("Cámaras / Render")]
    [SerializeField] private Camera mainCamera;           // Tu cámara principal
    [SerializeField] private Camera captureCamera;        // Cámara para capturar baja res (desactivada)
    [SerializeField] private RenderTexture lowResRT;      // 320x180 o 512x288

    [Header("Audio")]
    [SerializeField] private AudioSource pauseMusic;      // Música del menú de pausa

    [Tooltip("Opcional: Si quieres pausar selectivamente audios que no respeten AudioListener.pause")]
    [SerializeField] private List<AudioSource> gameplayAudioToPause = new List<AudioSource>();

    private bool isPaused = false;

    void Awake()
    {
        if (pauseOverlay) pauseOverlay.SetActive(false);
        if (pauseMusic)
        {
            // Queremos que suene aunque pausemos el AudioListener
            pauseMusic.ignoreListenerPause = true;
            pauseMusic.Stop();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;

        // 1) Capturar un frame a baja resolución para el "blur"
        if (captureCamera && lowResRT && blurImage)
        {
            // Alinear parámetros importantes por si cambiaste FOV/clipping en runtime
            if (mainCamera)
            {
                captureCamera.transform.position = mainCamera.transform.position;
                captureCamera.transform.rotation = mainCamera.transform.rotation;
                captureCamera.fieldOfView = mainCamera.fieldOfView;
                captureCamera.nearClipPlane = mainCamera.nearClipPlane;
                captureCamera.farClipPlane = mainCamera.farClipPlane;
            }

            captureCamera.targetTexture = lowResRT;
            captureCamera.enabled = true;
            captureCamera.Render();        // Render: toma “foto” a baja resolución
            captureCamera.enabled = false; // Volvemos a apagarla
            blurImage.texture = lowResRT;  // Mostramos la imagen “borrosa”
        }

        // 2) Mostrar overlay y menú
        if (pauseOverlay) pauseOverlay.SetActive(true);

        // 3) Pausar tiempo (físicas, animaciones basadas en Time.deltaTime)
        Time.timeScale = 0f;

        // 4) Pausar todo el audio del juego rápido:
        //    AudioListener.pause detiene todos los AudioSource,
        //    excepto aquellos con ignoreListenerPause = true (como pauseMusic).
        AudioListener.pause = true;

        // 5) Por si tienes audios fuera del listener (muy raro), pausarlos manualmente
        foreach (var a in gameplayAudioToPause)
        {
            if (a && a.isPlaying) a.Pause();
        }

        // 6) Reproducir música de pausa
        if (pauseMusic) pauseMusic.Play();

        // 7) Mostrar cursor para navegar menú
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;

        // 1) Ocultar overlay
        if (pauseOverlay) pauseOverlay.SetActive(false);

        // 2) Reanudar tiempo
        Time.timeScale = 1f;

        // 3) Reanudar audio global
        AudioListener.pause = false;

        // 4) Parar música de pausa
        if (pauseMusic) pauseMusic.Stop();

        // 5) Reanudar audios pausados manualmente
        foreach (var a in gameplayAudioToPause)
        {
            if (a) a.UnPause();
        }

        // 6) Restaurar cursor como te guste (si usas FPS)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Llamar desde botón UI "Resume"
    public void OnResumeButton() => ResumeGame();

    // Ejemplo: botón "Quit to Main Menu" (tú implementas la carga de escena)
    public void OnQuitButton()
    {
        // Antes de cambiar de escena, asegúrate de restaurar estado
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (pauseMusic) pauseMusic.Stop();
        // SceneManager.LoadScene("MainMenu");
    }
}
