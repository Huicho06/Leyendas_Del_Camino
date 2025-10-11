using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class NoteInteraction : MonoBehaviour
{
    [Header("Configuración UI")]
    public GameObject noteUI;        // Panel completo de la nota
    public TMP_Text noteText;        // Texto dentro del panel
    public Image noteImage;          // Imagen dentro del panel
    public TMP_Text interactionText; // Texto "[E] Leer nota"

    [Header("Contenido de la nota")]
    [TextArea(5, 15)]
    public string noteContent;       // Texto de la nota
    public Sprite noteSprite;        // Imagen opcional

    [Header("Control del jugador")]
    public MonoBehaviour playerControl; // Asigna aquí el script que controla al jugador (movimiento/linterna)
    public static bool isReadingNote = false;
    private bool isPlayerNearby = false;
    private bool isReading = false;

    void Start()
    {
        noteUI.SetActive(false);
        if (interactionText != null)
            interactionText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isPlayerNearby && !isReading && Input.GetKeyDown(KeyCode.E))
        {
            OpenNote();
        }
        else if (isReading && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseNote();
        }
    }

    void OpenNote()
    {
        isReading = true;
        isReadingNote = true; // <-- bloquea input globalmente
        noteUI.SetActive(true);
        if (noteText != null)
            noteText.text = noteContent;
        if (noteImage != null && noteSprite != null)
            noteImage.sprite = noteSprite;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (interactionText != null)
            interactionText.gameObject.SetActive(false);
    }

    void CloseNote()
    {
        isReading = false;
        isReadingNote = false; // <-- desbloquea input
        noteUI.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }



    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (interactionText != null)
                interactionText.gameObject.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (interactionText != null)
                interactionText.gameObject.SetActive(false);
        }
    }
}
