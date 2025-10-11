using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalTrigger : MonoBehaviour
{
    [Tooltip("Número de la siguiente escena a cargar")]
    public int nextSceneIndex;

    private void OnTriggerEnter(Collider other)
    {
        // Verificamos que el jugador sea quien toque el goal
        if (other.CompareTag("Player"))
        {
            // Carga la siguiente escena
            SceneManager.LoadScene(nextSceneIndex);
        }
    }
}
