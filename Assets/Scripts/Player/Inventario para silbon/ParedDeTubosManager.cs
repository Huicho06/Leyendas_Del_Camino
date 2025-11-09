using UnityEngine;

public class ParedDeTubosManager : MonoBehaviour
{
    public static ParedDeTubosManager Instance;

    [Header("Paredes del puzzle")]
    public ParedDeTubos[] paredes;

    [Header("Puerta final")]
    public GameObject puerta;
    public AudioClip sonidoAbrir;
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
            if (pared == null)
            {
                Debug.LogWarning(" Hay una pared sin asignar en el manager.");
                return;
            }

            if (!pared.TieneTubo())
            {
                Debug.Log(" Aún faltan tubos por colocar...");
                return;
            }
        }

        puzzleCompletado = true;
        AbrirPuerta();
    }

    private void AbrirPuerta()
    {
        Debug.Log(" ¡Todos los tubos colocados! La puerta se abre.");

        if (puerta != null)
            puerta.SetActive(false);

        if (sonidoAbrir != null)
            AudioSource.PlayClipAtPoint(sonidoAbrir, transform.position, volumen);
    }
}
