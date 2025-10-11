using UnityEngine;

public class NoteGlow : MonoBehaviour
{
    public Light glowLight;
    public float intensityMin = 2f;
    public float intensityMax = 7f;
    public float speed = 2f;

    void Update()
    {
        if (glowLight != null)
        {
            float intensity = Mathf.Lerp(intensityMin, intensityMax, Mathf.PingPong(Time.time * speed, 1));
            glowLight.intensity = intensity;
        }
    }
}
