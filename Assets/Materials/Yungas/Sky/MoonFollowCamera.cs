using UnityEngine;

public class MoonFollowCamera : MonoBehaviour
{
    public Transform cameraTransform;
    public float distance = 1000f; // distancia constante desde la cámara

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // La luna siempre se mantiene al frente de la cámara a cierta distancia
        transform.position = cameraTransform.position + cameraTransform.forward * distance;

        // Siempre orientada hacia la cámara (para verse completa)
        transform.LookAt(cameraTransform.position);
        transform.Rotate(0, 180, 0); // gira para que la cara visible mire hacia el jugador
    }
}
