using UnityEngine;

public class ParedDeTubosManager : MonoBehaviour
{
    public static ParedDeTubosManager Instance;

    [Header("Paredes del puzzle")]
    public ParedDeTubos[] paredes;  // ← arrastra aquí las 4 paredes en el inspector

    [Header("Puerta final")]
    public GameObject puerta;       // ← arrastra aquí tu puerta o muro
    public AudioClip sonidoAbrir;   // sonido cuando se abre
    [Range(0f, 1f)] public float volumen = 0.8f;

    private bool puzzleCompletado = false;

    void Awake()
    {
        Instance = this;
    }

    public void VerificarPuzzle()
    {
        if (puzzleCompletado) return;

        foreach (var pared in paredes)
        {
            if (pared == null) continue;
            var campo = typeof(ParedDeTubos).GetField("tieneTubo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool tieneTubo = (bool)campo.GetValue(pared);
            if (!tieneTubo)
            {
                Debug.Log("⏳ Aún faltan tubos por colocar...");
                return;
            }
        }

        // Si llegamos aquí, todos los tubos están colocados
        puzzleCompletado = true;
        AbrirPuerta();
    }

    private void AbrirPuerta()
    {
        Debug.Log("🚪 ¡Todos los tubos colocados! La puerta se abre.");

        if (puerta != null)
        {
            puerta.SetActive(false); // la desactiva
        }

        if (sonidoAbrir != null)
        {
            AudioSource.PlayClipAtPoint(sonidoAbrir, transform.position, volumen);
        }
    }
}
