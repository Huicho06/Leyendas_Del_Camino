using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Light))]
public class FinalLightEvent : MonoBehaviour
{
    private Light lightSource;
    private bool eventTriggered = false;

    [Header("Configuración de la luz")]
    public float maxIntensity = 500f;     // Qué tan brillante será el destello
    public float maxRange = 250f;         // Qué tan lejos llega la luz
    public float duration = 5f;           // Cuánto dura la transición

    [Header("Cámara")]
    public Camera playerCamera;
    public float shakeIntensity = 0.5f;   // Qué tanto tiembla
    public float shakeDuration = 5f;      // Cuánto dura el temblor

    private Vector3 originalCamPos;

    void Start()
    {
        lightSource = GetComponent<Light>();
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    public void TriggerLightExplosion()
    {
        if (!eventTriggered)
        {
            lightSource.enabled = true; // Asegurar que la luz está activa
            StartCoroutine(LightExplosion());
        }
    }

    private IEnumerator LightExplosion()
    {
        eventTriggered = true;
        Debug.Log("💡 Iniciando LightExplosion...");

        originalCamPos = playerCamera.transform.localPosition;

        float startIntensity = lightSource.intensity;
        float startRange = lightSource.range;
        float elapsed = 0f;

        // Transición de brillo
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            lightSource.intensity = Mathf.Lerp(startIntensity, maxIntensity, t);
            lightSource.range = Mathf.Lerp(startRange, maxRange, t);

            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = originalCamPos + Random.insideUnitSphere * shakeIntensity * (1f - (elapsed / duration) * 0.5f);
            }

            yield return null;
        }

        Debug.Log("✨ Luz alcanzó su máximo. Manteniendo temblor...");

        float shakeEnd = 0f;
        while (shakeEnd < shakeDuration)
        {
            shakeEnd += Time.deltaTime;
            if (playerCamera != null)
                playerCamera.transform.localPosition = originalCamPos + Random.insideUnitSphere * shakeIntensity;
            yield return null;
        }

        if (playerCamera != null)
            playerCamera.transform.localPosition = originalCamPos;

        Debug.Log("💥 Evento final completado.");
    }
}
