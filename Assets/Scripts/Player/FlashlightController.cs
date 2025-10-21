using System.Linq;
using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Configuración de la linterna")]
    public Light flashlight;                 // Spot Light
    public float detectionRange = 20f;       // Alcance
    public LayerMask enemyLayer;             // Capa de enemigos (incluye Silbón)
    [Tooltip("Capas que bloquean la luz (NO incluyas la capa Enemy aquí)")]
    public LayerMask obstacleMask = ~0;      // Suelo/paredes/escenario
    [Tooltip("Raíz del player para ignorar sus colliders (ej. objeto con CharacterController)")]
    public Transform playerRoot;             // Arrastra el root del jugador

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.Mouse0;

    [Header("Robustez de rayos")]
    [Tooltip("Usar Collider.ClosestPoint para apuntar mejor a colliders grandes/irregulares.")]
    public bool useClosestPoint = true;

    [Header("Debug")]
    public bool debugLogs = false;
    public bool drawRays = false;

    void Start()
    {
        if (flashlight) flashlight.enabled = false;
        if (!playerRoot) playerRoot = transform; // fallback
    }

    void Update()
    {
        if (flashlight && Input.GetKeyDown(toggleKey))
            flashlight.enabled = !flashlight.enabled;

        if (flashlight && flashlight.enabled)
            CheckEnemiesInLight();
        else
            UnfreezeAllEnemies(); // solo afecta a EnemyBehavior
    }

    void CheckEnemiesInLight()
    {
        if (!flashlight) return;

        // Salgo un poco del collider del player para evitar autocolisión
        Vector3 origin = flashlight.transform.position + flashlight.transform.forward * 0.1f;
        Vector3 forward = flashlight.transform.forward;
        float halfAngle = flashlight.spotAngle * 0.5f;

        // 1) Candidatos por radio (solo enemigos en enemyLayer)
        Collider[] enemies = Physics.OverlapSphere(origin, detectionRange, enemyLayer, QueryTriggerInteraction.Ignore);
        if (debugLogs) Debug.Log($"[Flashlight] candidatos en enemyLayer: {enemies.Length}");

        // Fallback: si enemyLayer no devolvió nada, intentar con cualquier capa, pero solo objetos que tengan SilbonAI
        if (enemies == null || enemies.Length == 0)
        {
            var any = Physics.OverlapSphere(origin, detectionRange, ~0, QueryTriggerInteraction.Ignore)
                             .Where(c => c.GetComponentInParent<SilbonAI>() != null)
                             .ToArray();
            if (any.Length > 0)
            {
                if (debugLogs) Debug.Log($"[Flashlight] fallback encontró {any.Length} candidatos con SilbonAI");
                enemies = any;
            }
        }

        foreach (var col in enemies)
        {
            if (!col) continue;

            // Punto objetivo para el rayo (ClosestPoint ayuda con colliders grandes)
            Vector3 targetPoint = useClosestPoint
                ? col.ClosestPoint(origin + forward * detectionRange)
                : col.bounds.center;

            Vector3 toEnemy = targetPoint - origin;
            float dist = toEnemy.magnitude;
            if (dist <= 0.0001f || dist > detectionRange) continue;

            // 2) Dentro del cono del Spot
            float ang = Vector3.Angle(forward, toEnemy);
            if (ang > halfAngle)
            {
                // Enemigos con EnemyBehavior: descongelar si salen del cono
                var ebOff = col.GetComponent<EnemyBehavior>();
                if (ebOff) ebOff.Freeze(false);
                continue;
            }

            Vector3 dir = toEnemy.normalized;

            // 3) RaycastAll: primero válido (ignorando colliders del player). Necesitamos ver si algo bloquea
            int maskAll = obstacleMask | enemyLayer;
            var hits = Physics.RaycastAll(origin, dir, dist, maskAll, QueryTriggerInteraction.Ignore)
                               .OrderBy(h => h.distance);

            RaycastHit? firstValid = null;
            foreach (var h in hits)
            {
                if (playerRoot && h.collider.transform.IsChildOf(playerRoot)) continue; // ignora player/arma/cámara
                firstValid = h; break;
            }

            bool inLight = false;
            if (firstValid.HasValue)
            {
                // ACEPTAR HIJOS DEL MISMO ENEMIGO (no solo el mismo collider)
                Transform hitT = firstValid.Value.collider.transform;
                Transform enemyT = col.transform;

                bool sameEnemy =
                    hitT == enemyT ||
                    hitT.IsChildOf(enemyT) ||
                    enemyT.IsChildOf(hitT);

                inLight = sameEnemy;

                if (drawRays)
                    Debug.DrawLine(origin, firstValid.Value.point, inLight ? Color.green : Color.red, 0.05f);

                if (debugLogs && !inLight)
                    Debug.Log($"[Flashlight] Bloqueado por: {firstValid.Value.collider.name} (layer {LayerMask.LayerToName(firstValid.Value.collider.gameObject.layer)})");
            }
            else
            {
                // No golpeó nada antes → vía libre
                inLight = true;
                if (drawRays) Debug.DrawLine(origin, targetPoint, Color.green, 0.05f);
            }

            // 4) Aplicar efecto
            var eb = col.GetComponent<EnemyBehavior>();
            if (eb)
            {
                // eb.Freeze(inLight); // ← Congelación clásica (otros enemigos)
                eb.ReactToLight(inLight); // Retroceso del Kari Kari
            }       // ← sistema clásico de congelar (no tocar)

            var silbon = col.GetComponentInParent<SilbonAI>(); // ← importante: en padre por si el collider es hijo
            if (silbon && inLight)
            {
                if (debugLogs) Debug.Log("[Flashlight] Silbón iluminado → OnLitByFlashlight()");
                silbon.OnLitByFlashlight(flashlight.transform, 1f); // activa persecución
            }
        }
    }

    void UnfreezeAllEnemies()
    {
        foreach (EnemyBehavior enemy in FindObjectsOfType<EnemyBehavior>())
        {
            // enemy.Freeze(false); // ← Descomenta si usas el sistema clásico
            enemy.ReactToLight(false); // Deja de retroceder si estaba retrocediendo
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!flashlight) return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        Gizmos.DrawWireSphere(flashlight.transform.position, detectionRange);
    }
}
