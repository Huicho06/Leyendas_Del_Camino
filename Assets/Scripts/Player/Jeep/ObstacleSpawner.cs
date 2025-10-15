using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public Transform spawnOrigin;           // punto de referencia (normalmente un transform delante del jeep)
    public GameObject obstaclePrefab;
    public int poolSize = 20;
    public float spawnInterval = 1.0f;      // cada cuánto tiempo (s)
    public float spawnDistanceMin = 30f;    // distancia mínima desde spawnOrigin
    public float spawnDistanceMax = 60f;    // distancia máxima
    public float laneOffset = 3.0f;         // separación entre carriles
    public Vector3 spawnAreaNoise = new Vector3(2f, 0f, 4f); // variación aleatoria

    private List<GameObject> pool;
    private float timer = 0f;
    private float[] laneX;

    void Start()
    {
        pool = new List<GameObject>(poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            var go = Instantiate(obstaclePrefab, Vector3.one * 1000f, Quaternion.identity);
            go.SetActive(false);
            pool.Add(go);
        }

        laneX = new float[] { -laneOffset, 0f, laneOffset };
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnObstacle();
            timer = 0f;
        }
    }

    void SpawnObstacle()
    {
        GameObject obj = GetFromPool();
        if (obj == null) return;

        int lane = Random.Range(0, laneX.Length);
        float z = Random.Range(spawnDistanceMin, spawnDistanceMax);
        Vector3 noise = new Vector3(Random.Range(-spawnAreaNoise.x, spawnAreaNoise.x), 0f, Random.Range(-spawnAreaNoise.z, spawnAreaNoise.z));
        Vector3 spawnPos = spawnOrigin.position + spawnOrigin.forward * z + spawnOrigin.right * laneX[lane] + noise;
        obj.transform.position = spawnPos;
        obj.transform.rotation = Quaternion.identity;
        obj.SetActive(true);
    }

    GameObject GetFromPool()
    {
        foreach (var go in pool)
        {
            if (!go.activeInHierarchy) return go;
        }
        // si no hay disponible opcionalmente expandir pool
        return null;
    }
}
