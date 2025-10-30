using UnityEngine;
using TMPro;

public class TMP_GlowPulse : MonoBehaviour
{
    public TMP_Text text;
    public Color glowColor = Color.cyan;
    public float speed = 2f;

    private Material mat;

    void Start()
    {
        mat = text.fontMaterial;
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f;
        mat.SetColor("_GlowColor", glowColor * (0.5f + t));
    }
}
