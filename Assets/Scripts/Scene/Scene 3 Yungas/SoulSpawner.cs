using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class SoulSpawner : MonoBehaviour
{
    [Header("Configuración general")]
    public GameObject soulPrefab;           // Prefab del alma
    public int totalSouls = 50;             // Cuántas almas generar
    public float spawnRadius = 200f;        // Radio alrededor del centro
    public float minDistanceBetweenSouls = 5f;
    public LayerMask groundMask;

    [Header("Opcional: referencias")]
    public Transform centerPoint;           // Centro del mapa (por defecto el spawner)
    public bool spawnContinuously = false;
    public float respawnDelay = 10f;

    private List<GameObject> souls = new List<GameObject>();

    void Start()
    {
        if (centerPoint == null)
            centerPoint = transform;

        SpawnAllSouls();

        if (spawnContinuously)
            InvokeRepeating(nameof(RespawnSouls), respawnDelay, respawnDelay);
    }

    void SpawnAllSouls()
    {
        int spawned = 0;
        int tries = 0;

        while (spawned < totalSouls && tries < totalSouls * 10)
        {
            tries++;
            Vector3 randomPos = GetRandomNavMeshPoint(centerPoint.position, spawnRadius);
            if (IsFarEnough(randomPos))
            {
                GameObject s = Instantiate(soulPrefab, randomPos, Quaternion.identity);
                souls.Add(s);
                spawned++;
            }
        }

        Debug.Log($"👻 {spawned} almas generadas en el mapa.");
    }

    bool IsFarEnough(Vector3 pos)
    {
        foreach (var s in souls)
        {
            if (s == null) continue;
            if (Vector3.Distance(pos, s.transform.position) < minDistanceBetweenSouls)
                return false;
        }
        return true;
    }

    Vector3 GetRandomNavMeshPoint(Vector3 origin, float dist)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector3 rand = origin + Random.insideUnitSphere * dist;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(rand, out hit, 10f, NavMesh.AllAreas))
                return hit.position;
        }
        return origin;
    }

    void RespawnSouls()
    {
        souls.RemoveAll(s => s == null);
        if (souls.Count < totalSouls)
        {
            int toSpawn = totalSouls - souls.Count;
            Debug.Log($"♻️ Respawneando {toSpawn} almas...");
            for (int i = 0; i < toSpawn; i++)
            {
                Vector3 randomPos = GetRandomNavMeshPoint(centerPoint.position, spawnRadius);
                GameObject s = Instantiate(soulPrefab, randomPos, Quaternion.identity);
                souls.Add(s);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.3f);
        if (centerPoint)
            Gizmos.DrawWireSphere(centerPoint.position, spawnRadius);
    }
}
