using UnityEngine;

public class ParedDeTubos : MonoBehaviour
{
    [Header("Configuración del puzzle")]
    public string idCorrecto = "Tubo_01";
    public Transform puntoColocacion;   // donde aparece el tubo
    public GameObject tuboPrefab;       // modelo que se muestra al colocar

    [Header("Audio")]
    public AudioClip sonidoColocar;     // sonido del tubo encajando
    [Range(0f, 1f)] public float volumen = 0.9f;

    private bool tieneTubo = false;

    public bool IntentarColocarTubo(string id)
    {
        if (tieneTubo)
        {
            Debug.Log("Ya hay un tubo colocado aquí.");
            return false;
        }

        if (id != idCorrecto)
        {
            Debug.Log("❌ Este tubo no corresponde a esta pared.");
            // puedes poner un sonido de error si quieres
            return false;
        }

        tieneTubo = true;
        Debug.Log($"✅ Tubo {id} colocado correctamente.");

        // Instanciar el tubo visual
        if (tuboPrefab != null && puntoColocacion != null)
        {
            GameObject tuboVisual = Instantiate(tuboPrefab, puntoColocacion.position, puntoColocacion.rotation);
            tuboVisual.transform.SetParent(puntoColocacion);
        }

        // Reproducir sonido al colocar
        if (sonidoColocar != null)
        {
            AudioSource.PlayClipAtPoint(sonidoColocar, transform.position, volumen);
        }

        return true;
    }
}
