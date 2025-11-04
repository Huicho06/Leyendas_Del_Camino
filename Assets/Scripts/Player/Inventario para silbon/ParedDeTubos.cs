using UnityEngine;

public class ParedDeTubos : MonoBehaviour
{
    [Header("Configuración del puzzle")]
    public string idCorrecto = "Tubo_01";
    public Transform puntoColocacion;
    public GameObject tuboPrefab;

    [Header("Audio")]
    public AudioClip sonidoColocar;
    [Range(0f, 1f)] public float volumen = 0.9f;

    [HideInInspector] public bool tieneTubo = false;

    public bool IntentarColocarTubo(string id)
    {
        if (tieneTubo)
        {
            Debug.Log("Ya hay un tubo colocado aquí.");
            return false;
        }

        if (id != idCorrecto)
        {
            Debug.Log($"❌ Este tubo ({id}) no corresponde aquí ({idCorrecto}).");
            return false;
        }

        tieneTubo = true;
        Debug.Log($"✅ Tubo {id} colocado correctamente.");

        // Instanciar el tubo visual
        if (tuboPrefab && puntoColocacion)
        {
            GameObject tuboVisual = Instantiate(tuboPrefab, puntoColocacion.position, puntoColocacion.rotation);
            tuboVisual.transform.SetParent(puntoColocacion);
        }

        // Reproducir sonido
        if (sonidoColocar)
            AudioSource.PlayClipAtPoint(sonidoColocar, transform.position, volumen);

        // Buscar al jugador y actualizar su inventario
        var player = GameObject.FindWithTag("Player");
        if (player)
        {
            var inv = player.GetComponent<PlayerInventory>();
            var move = player.GetComponent<PlayerMovement>();

            if (inv) inv.MarcarColocado(id);
            if (move) move.QuitarTuboDeLaMano();
        }

        // Revisar puzzle
        ParedDeTubosManager.Instance?.VerificarPuzzle();

        return true;
    }
    public bool TieneTubo() => tieneTubo;

}
