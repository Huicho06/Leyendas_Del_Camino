using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickupItem : MonoBehaviour
{
    public enum ItemType { Tubo }
    public ItemType itemType = ItemType.Tubo;

    [Header("Identificador único del tubo")]
    public string itemID = "Tubo_01";

    [Header("Sonido al recoger")]
    public AudioClip pickupClip;

    [Range(0f, 1f)]
    public float volumen = 0.8f;

    public void PickUp()
    {
        Debug.Log($"🎵 Tubo recogido: {itemID}");

        if (pickupClip != null)
        {
            // Reproduce el sonido en el punto donde se recogió
            AudioSource.PlayClipAtPoint(pickupClip, transform.position, volumen);
        }

        // Desactiva el objeto del mundo
        gameObject.SetActive(false);
    }
}
