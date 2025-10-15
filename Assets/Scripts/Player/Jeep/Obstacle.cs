using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Obstacle : MonoBehaviour
{
    void OnEnable()
    {
        // opcional: despu�s de X segundos desactivar (para reciclar)
        Invoke(nameof(DisableSelf), 25f);
    }

    void OnDisable()
    {
        CancelInvoke();
    }

    void DisableSelf()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // notificar golpe
            GameManager.Instance?.OnPlayerHitObstacle();
            // opcional: desactivar obst�culo
            gameObject.SetActive(false);
        }
    }
}
