using System.Collections.Generic;
using UnityEngine;

public class NoiseManager : MonoBehaviour
{
    public static NoiseManager Instance;

    private class Noise
    {
        public Vector3 position;
        public float intensity;
        public float time;
        public int id;
        public Noise(Vector3 p, float i, float t, int id)
        {
            position = p; intensity = i; time = t; this.id = id;
        }
    }

    private List<Noise> noises = new List<Noise>();
    public float noiseLifetime = 5f;

    private int nextId = 1; // ID incremental para distinguir ruidos en el tiempo

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
        noises.Add(new Noise(pos, intensity, Time.time, nextId++));
    }

    /// <summary>
    /// Devuelve el ruido MÁS RECIENTE (no el "mejor") dentro de rango y sobre umbral.
    /// Esto evita que el enemigo siga "el rastro" y vaya directo al último ruido.
    /// </summary>
    public bool GetMostRecentNoise(Vector3 listenerPos, float maxRange, float minIntensity, out Vector3 outPos, out int outId)
    {
        outPos = Vector3.zero;
        outId = -1;

        // Buscamos desde el más nuevo al más viejo
        for (int i = noises.Count - 1; i >= 0; i--)
        {
            var n = noises[i];
            if (n.intensity < minIntensity) continue;
            if (Vector3.Distance(listenerPos, n.position) > maxRange) continue;

            outPos = n.position;
            outId = n.id;
            return true; // El primero que cumpla (por ir de atrás hacia adelante) es el más reciente
        }
        return false;
    }
}
