using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerGenerador : MonoBehaviour
{
    public GeneradorInteractable generador;
    public TMP_Text promptText;

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
        if (generador == null) return;

        // Si el generador ya fue completado, no permite reiniciar
        if (generador.estaCompletado)
        {
            if (promptText && !promptText.text.Contains("activo"))
                promptText.text = "El generador ya está activo";
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) && !generador.jugando)
        {
            generador.IniciarMinijuego();
            OcultarPrompt();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerNearby = true;

        if (promptText)
        {
            if (generador != null && generador.estaCompletado)
                promptText.text = "El generador ya está activo";
            else
                promptText.text = "[E] Activar generador";

            promptText.gameObject.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerNearby = false;
        OcultarPrompt();
    }

    private void OcultarPrompt()
    {
        if (promptText)
            promptText.gameObject.SetActive(false);
    }
}
