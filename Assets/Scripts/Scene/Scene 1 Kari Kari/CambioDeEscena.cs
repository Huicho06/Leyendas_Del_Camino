
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
        col.isTrigger = true; // asegurarse de que sea trigger
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagJugador))
        {
            if (!string.IsNullOrEmpty(nombreEscenaDestino))
            {
                // Verifica que LoaderScene esté disponible en la escena
                if (LoaderScene.Instance != null)
                {
                    LoaderScene.Instance.LoadSceneString(nombreEscenaDestino);
                }
                else
                {
                    Debug.LogError("No existe un objeto con el script LoaderScene en la escena.");
                }
            }
            else
            {
                Debug.LogWarning("No se asignó una escena destino en el inspector.");
            }
        }
    }
}