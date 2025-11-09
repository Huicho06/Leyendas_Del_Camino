using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerInteract : MonoBehaviour
{
    [Header("Configuración de interacción")]
    public Camera cam;
    public float distancia = 3f;
    public KeyCode teclaInteractuar = KeyCode.E;

    private PlayerInventory inventario;
    private GameObject objetoActual;

    void Start()
    {
        inventario = GetComponent<PlayerInventory>();
        if (!cam && Camera.main)
            cam = Camera.main;
    }

    void Update()
    {
        RevisarInteraccionVisual();

        if (Input.GetKeyDown(teclaInteractuar) && objetoActual != null)
        {
            // --- Recoger tubo ---
            var pickup = objetoActual.GetComponent<PickupItem>();
            if (pickup != null && pickup.itemType == PickupItem.ItemType.Tubo)
            {
                inventario.AddTubo(pickup.itemID);
                pickup.PickUp();

                InteractionUI1.Instance?.OcultarMensaje();
                return;
            }

            // --- Colocar tubo ---
            var pared = objetoActual.GetComponent<ParedDeTubos>();
            if (pared != null && inventario.tuboEquipado != null)
            {
                pared.IntentarColocarTubo(inventario.tuboEquipado);
                InteractionUI1.Instance?.OcultarMensaje();
                return;
            }
        }
    }

    void RevisarInteraccionVisual()
    {
        // Quitar brillo previo
        if (objetoActual != null)
        {
            var hl = objetoActual.GetComponent<OutlineHighlight>();
            if (hl != null)
                hl.SetHighlight(false);
        }

        objetoActual = null;

        // Raycast al centro de la cámara
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, distancia))
        {
            var pickup = hit.collider.GetComponent<PickupItem>();
            var pared = hit.collider.GetComponent<ParedDeTubos>();

            // --- Pickup ---
            if (pickup != null)
            {
                objetoActual = hit.collider.gameObject;
                InteractionUI1.Instance?.MostrarMensaje("Presiona E para recoger");

                var hl = objetoActual.GetComponent<OutlineHighlight>();
                if (hl != null) hl.SetHighlight(true);
                return;
            }

            // --- Pared de tubos ---
            if (pared != null)
            {
                objetoActual = hit.collider.gameObject;
                InteractionUI1.Instance?.MostrarMensaje("Presiona E para colocar");

                var hl = objetoActual.GetComponent<OutlineHighlight>();
                if (hl != null) hl.SetHighlight(true);
                return;
            }
        }

        // --- Si no hay nada en mira ---
        InteractionUI1.Instance?.OcultarMensaje();
    }
}
