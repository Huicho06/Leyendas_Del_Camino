using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool IsPaused { get; private set; } = false;

    public Transform playerSpawn;
    public GameObject playerPrefab;

    public float respawnDelay = 1.2f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void OnPlayerHitObstacle()
    {
        if (IsPaused) return;
        IsPaused = true;
        // aquí podrías reproducir sonido, anim, pantalla roja, etc.
        Debug.Log("Jugador golpeado - reiniciando (o mostrar menu)");
        Invoke(nameof(RespawnPlayer), respawnDelay);
    }

    void RespawnPlayer()
    {
        // simple: recenter player to spawn
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && playerSpawn != null)
        {
            player.transform.position = playerSpawn.position;
            player.transform.rotation = playerSpawn.rotation;
        }
        // desactivar pausa
        IsPaused = false;
    }

    public void EndMinigameAndLoadLevel(string sceneName)
    {
        // ejemplo: cargar siguiente escena / nivel
        SceneManager.LoadScene(sceneName);
    }
}
