using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PalancaInteractable : MonoBehaviour
{
    [Header("Referencias")]
    public PalancaLuzAnimada palanca;    // arrastra aquí el script PalancaLuzAnimada
    public TMP_Text promptText;          // texto de interacción

    private bool playerNearby = false;

    void Start()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        if (promptText)
        {
            promptText.gameObject.SetActive(false);
            ActualizarTexto(); // Asegura texto coherente desde el inicio
        }

        if (palanca != null)
        {
            palanca.encendida = false; // Forzar apagado al inicio
        }
    }


    void Update()
    {
        if (!playerNearby) return;
        if (NoteInteraction.isReadingNote) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (palanca != null)
            {
                palanca.Activar();
                ActualizarTexto(); // actualiza el texto después del cambio
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerNearby = true;
        if (promptText && !NoteInteraction.isReadingNote)
        {
            promptText.gameObject.SetActive(true);
            ActualizarTexto();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerNearby = false;
        if (promptText) promptText.gameObject.SetActive(false);
    }

    void ActualizarTexto()
    {
        if (promptText == null || palanca == null) return;

        // Si la palanca está bloqueada (todas las luces fueron tocadas) → mensaje especial
        bool todasBloqueadas = true;
        foreach (var luz in palanca.lucesObjetivo)
        {
            if (luz != null && !luz.estaTocada)
            {
                todasBloqueadas = false;
                break;
            }
        }

        if (todasBloqueadas)
        {
            promptText.text = "Las luces dejaron de funcionar";
        }
        else if (palanca.EstadoEncendido)
        {
            promptText.text = "[E] Apagar luces";
        }
        else
        {
            promptText.text = "[E] Encender luces";
        }
    }
}
