using UnityEngine;
using System.Collections;
using UnityEngine.Playables; // 👈 Importante para usar Timeline

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
    public PlayableDirector playableDirector; // 🎬 Timeline de Unity

    private int notasRecogidas = 0;
    private bool cinematicaActiva = false;

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
    }

    public void AgregarNotaPuzzle()
    {
        notasRecogidas++;
        Debug.Log($"🧩 Nota número {notasRecogidas} recogida.");

        if (imagenCompleta != null && !imagenCompleta.activeSelf)
            imagenCompleta.SetActive(true);

        switch (notasRecogidas)
        {
            case 1:
                if (parte1 != null) parte1.SetActive(true);
                break;
            case 2:
                if (parte2 != null) parte2.SetActive(true);
                break;
            case 3:
                if (parte3 != null) parte3.SetActive(true);
                break;
        }
    }

    public bool EsUltimaNota()
    {
        // Cambia este número si tienes más o menos de 3 notas
        return notasRecogidas == 3 && !cinematicaActiva;
    }

    public IEnumerator CinematicaFinal()
    {
        cinematicaActiva = true;
        Debug.Log("🎬 Iniciando Cinemática Final (Timeline)...");

        // Bloquear control del jugador (si existe un script de movimiento)
        var playerController = playerCamera.GetComponentInParent<MonoBehaviour>();
        if (playerController != null && playerController.enabled)
        {
            playerController.enabled = false;
        }

        // Asegurarnos de que la cinemática está asignada
        if (playableDirector != null)
        {
            Debug.Log("▶️ Ejecutando Timeline...");
            playableDirector.gameObject.SetActive(true);
            playableDirector.Play();
        }
        else
        {
            Debug.LogError("❌ No hay PlayableDirector asignado en NotePuzzleManager.");
        }

        yield return null;
    }
}
