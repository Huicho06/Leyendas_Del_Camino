using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class PlayerVisionEffect : MonoBehaviour
{
    public static PlayerVisionEffect instance;

    [Header("Volume del blur")]
    public Volume visionBlurVolume;
    public float fadeInSpeed = 2f;
    public float fadeOutSpeed = 1f;

    private Coroutine activeEffect;

    private void Awake()
    {
        instance = this;
    }

    public void TriggerBlur(float duration)
    {
        if (visionBlurVolume == null) return;
        if (activeEffect != null) StopCoroutine(activeEffect);
        activeEffect = StartCoroutine(DoBlur(duration));
    }

    private IEnumerator DoBlur(float duration)
    {
        float w = 0;
        // Fade in
        while (w < 1)
        {
            w += Time.deltaTime * fadeInSpeed;
            visionBlurVolume.weight = w;
            yield return null;
        }

        yield return new WaitForSeconds(duration);

        // Fade out
        while (w > 0)
        {
            w -= Time.deltaTime * fadeOutSpeed;
            visionBlurVolume.weight = w;
            yield return null;
        }

        visionBlurVolume.weight = 0;
        activeEffect = null;
    }
}
