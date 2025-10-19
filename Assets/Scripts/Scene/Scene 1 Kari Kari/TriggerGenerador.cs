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

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (generador != null && !generador.jugando)
            {
                generador.IniciarMinijuego();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerNearby = true;
        if (promptText)
        {
            promptText.text = "[E] Activar generador";
            promptText.gameObject.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerNearby = false;
        if (promptText)
            promptText.gameObject.SetActive(false);
    }
}
