using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public float sensibilidad = 200f;
    public Transform playerRoot; // apunta a PlayerRoot
    public bool blockInput = false;

    private float rotacionX = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (blockInput) return; // bloquea el input

        float mouseX = Input.GetAxis("Mouse X") * sensibilidad * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidad * Time.deltaTime;

        rotacionX -= mouseY;
        rotacionX = Mathf.Clamp(rotacionX, -90f, 90f);
        transform.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);

        playerRoot.Rotate(Vector3.up * mouseX);
    }

}
