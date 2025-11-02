
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
            UnfreezeAllEnemies(); // solo afecta a EnemyBehavior y SoulAI
    }

    void CheckEnemiesInLight()
    {
        if (!flashlight) return;

        Vector3 origin = flashlight.transform.position + flashlight.transform.forward * 0.1f;
        Vector3 forward = flashlight.transform.forward;
        float halfAngle = flashlight.spotAngle * 0.5f;

        Collider[] enemies = Physics.OverlapSphere(origin, detectionRange, enemyLayer, QueryTriggerInteraction.Ignore);
        if (debugLogs) Debug.Log($"[Flashlight] candidatos en enemyLayer: {enemies.Length}");

        if (enemies == null || enemies.Length == 0)
        {
            var any = Physics.OverlapSphere(origin, detectionRange, ~0, QueryTriggerInteraction.Ignore)
                             .Where(c => c.GetComponentInParent<SilbonAI>() != null || c.GetComponentInParent<SoulAI>() != null)
                             .ToArray();
            if (any.Length > 0)
            {
                if (debugLogs) Debug.Log($"[Flashlight] fallback encontró {any.Length} candidatos con SilbonAI o SoulAI");
                enemies = any;
            }
        }

        foreach (var col in enemies)
        {
            if (!col) continue;

            Vector3 targetPoint = useClosestPoint
                ? col.ClosestPoint(origin + forward * detectionRange)
                : col.bounds.center;

            Vector3 toEnemy = targetPoint - origin;
            float dist = toEnemy.magnitude;
            if (dist <= 0.0001f || dist > detectionRange) continue;

            float ang = Vector3.Angle(forward, toEnemy);
            if (ang > halfAngle)
            {
                var ebOff = col.GetComponent<EnemyBehavior>();
                if (ebOff) ebOff.Freeze(false);

                var soulOff = col.GetComponentInParent<SoulAI>();
                if (soulOff) soulOff.OnIlluminated(false, flashlight.transform);
                continue;
            }

            Vector3 dir = toEnemy.normalized;

            int maskAll = obstacleMask | enemyLayer;
            var hits = Physics.RaycastAll(origin, dir, dist, maskAll, QueryTriggerInteraction.Ignore)
                               .OrderBy(h => h.distance);

            RaycastHit? firstValid = null;
            foreach (var h in hits)
            {
                if (playerRoot && h.collider.transform.IsChildOf(playerRoot)) continue;
                firstValid = h; break;
            }

            bool inLight = false;
            if (firstValid.HasValue)
            {
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
                inLight = true;
                if (drawRays) Debug.DrawLine(origin, targetPoint, Color.green, 0.05f);
            }

            var eb = col.GetComponent<EnemyBehavior>();
            if (eb)
            {
                eb.ReactToLight(inLight);
            }

            var silbon = col.GetComponentInParent<SilbonAI>();
            if (silbon && inLight)
            {
                if (debugLogs) Debug.Log("[Flashlight] Silbón iluminado → OnLitByFlashlight()");
                silbon.OnLitByFlashlight(flashlight.transform, 1f);
            }

            // NUEVO: Souls
            var soul = col.GetComponentInParent<SoulAI>();
            if (soul != null)
            {
                soul.OnIlluminated(inLight, flashlight.transform);
                if (debugLogs) Debug.Log($"[Flashlight] Soul '{soul.name}' iluminada: {inLight}");
            }
        }
    }

    void UnfreezeAllEnemies()
    {
        foreach (EnemyBehavior enemy in FindObjectsOfType<EnemyBehavior>())
        {
            enemy.ReactToLight(false);
        }

        foreach (SoulAI soul in FindObjectsOfType<SoulAI>())
        {
            soul.OnIlluminated(false, flashlight ? flashlight.transform : null);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!flashlight) return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        Gizmos.DrawWireSphere(flashlight.transform.position, detectionRange);
    }
}
