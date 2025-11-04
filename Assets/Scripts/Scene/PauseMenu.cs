using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;
    public Slider masterSlider;
    public Slider playerSlider;
    public Slider musicSlider;
    public Button resumeButton;
    public Button quitButton;

    [Header("Audio")]
    public AudioMixer mainMixer; // debes tener un AudioMixer

    [Header("Referencias")]
    public PlayerMovement playerMovement; // asigna tu script del jugador
    public GameObject blurEffect; // imagen o cámara con desenfoque URP

    private bool isPaused = false;

    void Start()
    {
        pausePanel.SetActive(false);
        if (blurEffect) blurEffect.SetActive(false);

        resumeButton.onClick.AddListener(Resume);
        quitButton.onClick.AddListener(QuitGame);

        masterSlider.onValueChanged.AddListener(v => mainMixer.SetFloat("MasterVolume", Mathf.Log10(v) * 20));
        playerSlider.onValueChanged.AddListener(v => mainMixer.SetFloat("PlayerVolume", Mathf.Log10(v) * 20));
        musicSlider.onValueChanged.AddListener(v => mainMixer.SetFloat("MusicVolume", Mathf.Log10(v) * 20));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f; // congela el juego
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerMovement) playerMovement.enabled = false;
        if (blurEffect) blurEffect.SetActive(true);
        pausePanel.SetActive(true);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerMovement) playerMovement.enabled = true;
        if (blurEffect) blurEffect.SetActive(false);
        pausePanel.SetActive(false);
    }

    void QuitGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
