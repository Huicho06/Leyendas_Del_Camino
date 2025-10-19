using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HideSpot : MonoBehaviour
{
    [Header("Punto de ocultarse")]
    public Transform hidePoint;  // posición donde se coloca el jugador/cámara

    [Header("Mensajes")]
    public string enterText = "Presiona E para esconderte";
    public string exitText = "Presiona E para salir";

    [Header("Opcional")]
    public bool faceForward = true;   // orientar al jugador según el HidePoint
    public bool requireCrouch = false; // exigir estar agachado para activarlo

    private bool playerInside;
    private PlayerHideSystem currentHider;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        var hider = other.GetComponentInParent<PlayerHideSystem>();
        if (!hider) return;
        playerInside = true;
        currentHider = hider;
        UpdatePrompt();
    }

    void OnTriggerExit(Collider other)
    {
        var hider = other.GetComponentInParent<PlayerHideSystem>();
        if (!hider || hider != currentHider) return;
        playerInside = false;
        currentHider = null;
        HideUI.Instance?.Show(false);
    }

    void Update()
    {
        if (!playerInside || currentHider == null) return;

        // Mostrar texto adecuado
        UpdatePrompt();

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentHider.IsHidden)
            {
                // Si estás escondido en ESTE spot, salimos
                if (currentHider.CurrentSpot == this)
                    currentHider.ExitHide();
            }
            else
            {
                if (requireCrouch && !currentHider.IsPlayerCrouching())
                    return;

                currentHider.EnterHide(this);
            }
        }
    }

    private void UpdatePrompt()
    {
        if (!HideUI.Instance) return;

        if (currentHider.IsHidden)
            HideUI.Instance.ShowExit();
        else
            HideUI.Instance.ShowEnter();

        HideUI.Instance.Show(true);
    }

}
