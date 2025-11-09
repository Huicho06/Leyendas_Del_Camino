using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NotePickup : MonoBehaviour
{
    [Header("Contenido de la nota")]
    [TextArea(5, 15)]
    public string noteContent;
    public Sprite noteSprite;

    [Header("UI para lectura")]
    public GameObject noteUI;
    public TMP_Text noteText;
    public Image noteImage;

    private bool isPlayerNearby = false;
    private bool isReading = false;
    private bool isCollected = false;

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
            if (!isCollected)
            {
                CollectNote();
            }
            else if (!isReading)
            {
                OpenNote();
            }
            else
            {
                CloseNote();
            }
        }
    }

    void CollectNote()
    {
        isCollected = true;
        PromptManager.instance.HidePrompt();

        // Agregar la nota al inventario
        NoteInventory.instance.AddNote(noteContent, noteSprite);

        // ⚡ Actualizar puzzle
        if (NotePuzzleManager.instance != null)
        {
            NotePuzzleManager.instance.AgregarNotaPuzzle();

            // Si ya completó todas las notas, no mostrar la UI
            if (NotePuzzleManager.instance.EsUltimaNota())
            {
                Debug.Log("🎯 Última nota recogida. Iniciando cinemática final...");
                StartCoroutine(NotePuzzleManager.instance.CinematicaFinal());
                Destroy(gameObject, 1f);
                return;
            }

        }

        // Mostrar nota normalmente
        OpenNote();

        // Si quieres que el objeto desaparezca al recoger:
        // Destroy(gameObject);
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
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerNearby = true;

        if (!isCollected)
            PromptManager.instance.ShowPrompt("[E] Recoger nota");
        else
            PromptManager.instance.ShowPrompt("[E] Leer nota");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerNearby = false;
        PromptManager.instance.HidePrompt();
    }
}
