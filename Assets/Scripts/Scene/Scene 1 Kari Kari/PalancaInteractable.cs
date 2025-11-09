using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PalancaInteractable : MonoBehaviour
{
    [Header("Referencias")]
    public PalancaLuzAnimada palanca;

    private bool playerNearby = false;

    void Start()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        if (palanca != null)
            palanca.encendida = false;
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
                ActualizarTexto();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerNearby = true;
        ActualizarTexto();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerNearby = false;
        PromptManager.instance.HidePrompt();
    }

    void ActualizarTexto()
    {
        if (palanca == null || NoteInteraction.isReadingNote) return;

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
            PromptManager.instance.ShowPrompt("Las luces dejaron de funcionar");
        }
        else if (palanca.EstadoEncendido)
        {
            PromptManager.instance.ShowPrompt("[E] Apagar luces");
        }
        else
        {
            PromptManager.instance.ShowPrompt("[E] Encender luces");
        }
    }
}
