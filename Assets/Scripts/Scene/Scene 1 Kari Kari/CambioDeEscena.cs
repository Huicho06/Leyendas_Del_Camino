
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class CambioDeEscenaPorTrigger : MonoBehaviour
{
    [Header("Configuración")]
    public string nombreEscenaDestino; // nombre exacto de la escena
    public string tagJugador = "Player"; // tag del jugador

    private void Start()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // asegúrate de que el collider sea trigger
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagJugador))
        {
            if (!string.IsNullOrEmpty(nombreEscenaDestino))
                SceneManager.LoadScene(nombreEscenaDestino);
            else
                Debug.LogWarning("No se asignó una escena destino en el inspector.");
        }
    }
}
