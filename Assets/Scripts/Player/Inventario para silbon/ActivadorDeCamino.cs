using UnityEngine;

public class ActivadorDeCamino : MonoBehaviour
{
    [Header("Objetos a activar")]
    public GameObject paredDestino; // pared o puerta que se activa
    public Light luzIndicadora;      // luz que se enciende
    public GameObject triggerCambio; // trigger para cambiar de escena

    [Header("Luz - Efecto visual")]
    public float nuevaIntensidad = 8f;
    public float nuevoRango = 15f;
    public float duracionTransicion = 2f;

    [Header("Audio opcional")]
    public AudioClip sonidoActivacion;
    [Range(0f, 1f)] public float volumenSonido = 0.8f;

    private bool activado = false;

    private void OnDestroy()
    {
        if (activado) return;
        activado = true;

        ActivarCamino();
    }

    private void ActivarCamino()
    {
        // 1️⃣ Activa la pared o puerta
        if (paredDestino != null)
        {
            paredDestino.SetActive(true);
            Debug.Log(" Pared activada");
        }

        // 2️⃣ Enciende la luz
        if (luzIndicadora != null)
        {
            StartCoroutine(TransicionLuz());
            Debug.Log(" Luz encendida");
        }

        // 3️⃣ Activa el trigger de cambio de escena
        if (triggerCambio != null)
        {
            triggerCambio.SetActive(true);
            Debug.Log(" Trigger de cambio de escena activado");
        }

        // 4️⃣ Sonido
        if (sonidoActivacion != null)
        {
            AudioSource.PlayClipAtPoint(sonidoActivacion, transform.position, volumenSonido);
        }
    }

    private System.Collections.IEnumerator TransicionLuz()
    {
        float tiempo = 0f;
        float intensidadInicial = luzIndicadora.intensity;
        float rangoInicial = luzIndicadora.range;

        while (tiempo < duracionTransicion)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracionTransicion;

            luzIndicadora.intensity = Mathf.Lerp(intensidadInicial, nuevaIntensidad, t);
            luzIndicadora.range = Mathf.Lerp(rangoInicial, nuevoRango, t);

            yield return null;
        }

        luzIndicadora.intensity = nuevaIntensidad;
        luzIndicadora.range = nuevoRango;
    }
}
