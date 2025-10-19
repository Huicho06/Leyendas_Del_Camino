using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PalancaInteractable : MonoBehaviour
{
    [Header("Referencias")]
    public PalancaLuzAnimada palanca;    // arrastra aquí el script PalancaLuzAnimada
    public TMP_Text promptText;          // texto "[E] Encender luces" o "[E] Apagar luces"

    private bool playerNearby = false;

    void Start()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        if (promptText) promptText.gameObject.SetActive(false);
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

        // Si la palanca tiene luces encendidas → mostrar "Apagar"
        if (palanca.EstadoEncendido)
            promptText.text = "[E] Apagar luces";
        else
            promptText.text = "[E] Encender luces";
    }
}
