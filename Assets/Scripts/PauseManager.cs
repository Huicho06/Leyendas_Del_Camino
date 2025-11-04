using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseOverlay;
    [SerializeField] private RawImage blurImage;
    [SerializeField] private Slider volumeSlider;          // ← arrastra tu slider

    [Header("Cámaras / Render")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera captureCamera;
    [SerializeField] private RenderTexture lowResRT;

    [Header("Audio")]
    [SerializeField] private AudioSource pauseMusic;       // ← música del menú pausa

    [Header("Gameplay")]
    [SerializeField] private FlashlightController flashlightController;

    private bool isPaused = false;
    private float globalVolume = 1f;
    private List<AudioSource> allAudioSources = new List<AudioSource>();

    void Awake()
    {
        if (pauseOverlay) pauseOverlay.SetActive(false);

        if (pauseMusic)
        {
            pauseMusic.ignoreListenerPause = true;
            pauseMusic.Stop();
        }

        if (volumeSlider)
        {
            volumeSlider.onValueChanged.AddListener(SetGlobalVolume);
            volumeSlider.value = 1f;
        }

        // Guardar referencia a todos los AudioSource iniciales
        RefreshAudioSources();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    void RefreshAudioSources()
    {
        allAudioSources.Clear();
        allAudioSources.AddRange(FindObjectsOfType<AudioSource>(true));
    }

    public void TogglePause()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;

        // Capturar imagen de fondo si las cámaras y el blur están disponibles
        if (captureCamera != null && lowResRT != null && blurImage != null && mainCamera != null)
        {
            captureCamera.transform.SetPositionAndRotation(mainCamera.transform.position, mainCamera.transform.rotation);
            captureCamera.fieldOfView = mainCamera.fieldOfView;
            captureCamera.nearClipPlane = mainCamera.nearClipPlane;
            captureCamera.farClipPlane = mainCamera.farClipPlane;

            captureCamera.targetTexture = lowResRT;
            captureCamera.enabled = true;
            captureCamera.Render();
            captureCamera.enabled = false;

            blurImage.texture = lowResRT;
        }

        if (pauseOverlay != null) pauseOverlay.SetActive(true);

        Time.timeScale = 0f;

        // Pausar todos los audios menos el de pausa
        RefreshAudioSources();
        foreach (var src in allAudioSources)
        {
            if (src != null && src.isPlaying && src != pauseMusic)
                src.Pause();
        }

        if (pauseMusic != null)
            pauseMusic.Play();

        // Desactivar FlashlightController si existe
        if (flashlightController != null)
            flashlightController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;

        if (pauseOverlay != null) pauseOverlay.SetActive(false);
        Time.timeScale = 1f;

        // Reanudar audios
        foreach (var src in allAudioSources)
        {
            if (src != null && src != pauseMusic)
                src.UnPause();
        }

        if (pauseMusic != null)
            pauseMusic.Stop();

        // Activar FlashlightController si existe
        if (flashlightController != null)
            flashlightController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    public void OnResumeButton() => ResumeGame();

    public void OnQuitButton()
    {
        Time.timeScale = 1f;
        if (pauseMusic) pauseMusic.Stop();
        foreach (var src in allAudioSources)
        {
            if (src) src.UnPause();
        }
        // SceneManager.LoadScene("MainMenu");
    }

    public void SetGlobalVolume(float v)
    {
        globalVolume = Mathf.Clamp01(v);
        RefreshAudioSources();
        foreach (var src in allAudioSources)
        {
            if (src) src.volume = globalVolume;
        }
    }
}
