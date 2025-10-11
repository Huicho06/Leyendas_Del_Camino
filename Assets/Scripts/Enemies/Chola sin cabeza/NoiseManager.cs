using System.Collections.Generic;
using UnityEngine;

public class NoiseManager : MonoBehaviour
{
    public static NoiseManager Instance;

    class Noise
    {
        public Vector3 position;
        public float intensity;
        public float time;
        public Noise(Vector3 p, float i, float t) { position = p; intensity = i; time = t; }
    }

    private List<Noise> noises = new List<Noise>();
    public float noiseLifetime = 5f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        float t = Time.time;
        noises.RemoveAll(n => t - n.time > noiseLifetime);
    }

    public void ReportNoise(Vector3 pos, float intensity)
    {
        noises.Add(new Noise(pos, intensity, Time.time));
    }

    // Devuelve el ruido más fuerte dentro de maxRange y encima del umbral
    public bool GetBestNoise(Vector3 listenerPos, float maxRange, float minIntensity, out Vector3 outPos, out float outIntensity)
    {
        outPos = Vector3.zero; outIntensity = 0f;
        float bestScore = 0f;
        foreach (var n in noises)
        {
            float dist = Vector3.Distance(listenerPos, n.position);
            if (dist > maxRange) continue;
            float score = n.intensity / (1f + dist); // intensidad atenuada por distancia
            if (score > bestScore && n.intensity >= minIntensity)
            {
                bestScore = score;
                outPos = n.position;
                outIntensity = n.intensity;
            }
        }
        return bestScore > 0f;
    }
}
