using UnityEngine;
using TMPro;

public class InteractionUI1 : MonoBehaviour
{
    public static InteractionUI1 Instance;

    [Header("Referencias UI")]
    [SerializeField] private GameObject root;           // Panel o Canvas del texto
    [SerializeField] private TextMeshProUGUI texto;     // Texto principal

    void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (!root) root = gameObject;
        if (texto != null) texto.text = "";

        // Empieza apagado
        root.SetActive(false);
    }

    public void MostrarMensaje(string mensaje)
    {
        if (!root || !texto) return;

        root.SetActive(true);
        texto.gameObject.SetActive(true);
        texto.text = mensaje;
    }

    public void OcultarMensaje()
    {
        if (!root || !texto) return;

        texto.text = "";
        root.SetActive(false);
    }
}
