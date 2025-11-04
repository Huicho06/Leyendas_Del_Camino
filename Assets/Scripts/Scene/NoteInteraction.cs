using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class NoteInteraction : MonoBehaviour
{
    [Header("Contenido de la nota")]
    [TextArea(5, 15)]
    public string noteContent;
    public Sprite noteSprite;

    [Header("UI")]
    public GameObject noteUI;
    public TMP_Text noteText;
    public Image noteImage;

    private bool isPlayerNearby = false;
    private bool isReading = false;

    public static bool isReadingNote = false;

    void Start()
    {
        if (noteUI != null)
            noteUI.SetActive(false);
    }

    void Update()
    {
        if (!isPlayerNearby) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isReading)
            {
                OpenNote();
            }
            else
            {
                CloseNote();
            }
        }
    }

    void OpenNote()
    {
        isReading = true;
        isReadingNote = true;
        if (noteUI != null)
            noteUI.SetActive(true);

        if (noteText != null)
            noteText.text = noteContent;

        if (noteImage != null && noteSprite != null)
            noteImage.sprite = noteSprite;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PromptManager.instance.HidePrompt();
    }

    void CloseNote()
    {
        isReading = false;
        isReadingNote = false;
        if (noteUI != null)
            noteUI.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerNearby = true;
        if (!isReading)
            PromptManager.instance.ShowPrompt("[E] Leer nota");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerNearby = false;
        PromptManager.instance.HidePrompt();
    }
}
