using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class NoiseEmitter : MonoBehaviour
{
    public float walkNoise = 1f;
    public float runNoise = 3f;
    public float crouchNoise = 0.3f;
    public float throwNoise = 5f;

    public void EmitStep(bool running, bool crouching)
    {
        float intensity = crouching ? crouchNoise : (running ? runNoise : walkNoise);
        NoiseManager.Instance.ReportNoise(transform.position, intensity);
    }

    public void EmitAt(Vector3 position, float intensity)
    {
        NoiseManager.Instance.ReportNoise(position, intensity);
    }

    public void EmitThrow(Vector3 position)
    {
        EmitAt(position, throwNoise);
    }
}
