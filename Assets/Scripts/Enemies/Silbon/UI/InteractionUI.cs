using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI I;

    [SerializeField] GameObject root;      // Panel contenedor (ej: NoteUI)
    [SerializeField] TMP_Text label;       // Un solo TextMeshProUGUI

    void Awake()
    {
        I = this;
        if (!root) root = gameObject;
        Hide(); // al iniciar, oculto
    }

    public void Show(string text)
    {
        if (label) label.text = text;
        if (root && !root.activeSelf) root.SetActive(true);
    }

    public void Hide()
    {
        if (root && root.activeSelf) root.SetActive(false);
    }
}
