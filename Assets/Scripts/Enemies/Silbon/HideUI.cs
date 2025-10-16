using UnityEngine;
using TMPro;

public class HideUI : MonoBehaviour
{
    public static HideUI Instance;

    [SerializeField] private GameObject root;               // Panel
    [SerializeField] private TextMeshProUGUI entrarText;    // "Presiona E para esconderte"
    [SerializeField] private TextMeshProUGUI salirText;     // "Presiona E para salir"

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Show(false);
    }

    public void ShowEnter()
    {
        if (!root) return;
        root.SetActive(true);
        if (entrarText) entrarText.gameObject.SetActive(true);
        if (salirText) salirText.gameObject.SetActive(false);
    }

    public void ShowExit()
    {
        if (!root) return;
        root.SetActive(true);
        if (entrarText) entrarText.gameObject.SetActive(false);
        if (salirText) salirText.gameObject.SetActive(true);
    }

    public void Show(bool visible)
    {
        if (!root) return;
        root.SetActive(visible);
    }
}
