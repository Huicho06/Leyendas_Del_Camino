using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseOverlay;
    [SerializeField] private RawImage blurImage;
    [SerializeField] private Slider volumeSlider;

    [Header("Cámaras / Render")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera captureCamera;
    [SerializeField] private RenderTexture lowResRT;

    [Header("Audio")]
    [SerializeField] private AudioSource pauseMusic;

    [Header("Gameplay Scripts que deben bloquearse")]
    [SerializeField] private MonoBehaviour[] playerScripts;
    // ← aquí arrastras todos los scripts del player (Movimiento, Cámara, Disparo, etc.)

    private bool isPaused = false;
    private float globalVolume = 0.5f;
    private List<AudioSource> allAudioSources = new List<AudioSource>();

    void Awake()
    {
        if (pauseOverlay) pauseOverlay.SetActive(false);

        if (pauseMusic)
        {
            pauseMusic.ignoreListenerPause = true;
            pauseMusic.Stop();
        }

        // Volumen inicial = mitad
        if (volumeSlider)
        {
            volumeSlider.onValueChanged.AddListener(SetGlobalVolume);
            volumeSlider.value = 0.5f;
        }

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

        // Capturar el fondo
        if (captureCamera && lowResRT && blurImage && mainCamera)
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

        if (pauseOverlay) pauseOverlay.SetActive(true);
        Time.timeScale = 0f;

        RefreshAudioSources();
        foreach (var src in allAudioSources)
        {
            if (src && src.isPlaying && src != pauseMusic)
                src.Pause();
        }

        if (pauseMusic) pauseMusic.Play();

        // Bloquear TODOS los scripts del jugador
        foreach (var script in playerScripts)
            if (script) script.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;

        if (pauseOverlay) pauseOverlay.SetActive(false);
        Time.timeScale = 1f;

        foreach (var src in allAudioSources)
        {
            if (src && src != pauseMusic)
                src.UnPause();
        }

        if (pauseMusic) pauseMusic.Stop();

        // Reactivar scripts del jugador
        foreach (var script in playerScripts)
            if (script) script.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnResumeButton()
    {
        ResumeGame();
    }

    public void OnQuitButton()
    {
        Time.timeScale = 1f;
        if (pauseMusic) pauseMusic.Stop();

        foreach (var src in allAudioSources)
            if (src) src.UnPause();
    }

    public void SetGlobalVolume(float v)
    {
        globalVolume = Mathf.Clamp01(v);
        RefreshAudioSources();

        foreach (var src in allAudioSources)
            if (src) src.volume = globalVolume;
    }
}
