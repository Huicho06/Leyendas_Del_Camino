using TMPro;
using UnityEngine;

public class PromptManager : MonoBehaviour
{
    public static PromptManager instance;

    [Header("UI")]
    public TMP_Text promptText;   // Único TMP_Text de la escena

    private void Awake()
    {
        // Singleton
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Muestra un prompt en pantalla
    /// </summary>
    public void ShowPrompt(string message)
    {
        if (promptText == null || NoteInteraction.isReadingNote) return;

        promptText.text = message;
        promptText.gameObject.SetActive(true);
    }

    /// <summary>
    /// Oculta el prompt
    /// </summary>
    public void HidePrompt()
    {
        if (promptText == null) return;

        promptText.gameObject.SetActive(false);
        promptText.text = "";
    }
}
