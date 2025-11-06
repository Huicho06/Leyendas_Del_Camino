using UnityEngine;

public class SkyFollowCamera : MonoBehaviour
{
    public Transform cam;
    void LateUpdate()
    {
        if (cam != null)
            transform.position = cam.position;
    }
}
