using System.Linq;
using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Configuración de la linterna")]
    public Light flashlight;                 // Spot light
    public float detectionRange = 20f;       // Alcance de la linterna
    public LayerMask enemyLayer;             // Capa de enemigos (incluye Silbón)
    [Tooltip("Capas que bloquean la luz (NO incluyas la capa Enemy aquí)")]
    public LayerMask obstacleMask = ~0;      // Suelo/paredes/escenario
    [Tooltip("Raíz del player para ignorar sus colliders (ej: objeto con CharacterController)")]
    public Transform playerRoot;             // Arrastra el root del jugador

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.Mouse0;

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
            UnfreezeAllEnemies(); // Solo afecta a EnemyBehavior
    }

    void CheckEnemiesInLight()
    {
        if (!flashlight) return;

        // Salgo un poco del collider del jugador para evitar autocolisión
        Vector3 origin = flashlight.transform.position + flashlight.transform.forward * 0.1f;
        Vector3 forward = flashlight.transform.forward;
        float halfAngle = flashlight.spotAngle * 0.5f;

        // 1) Candidatos por radio (solo enemigos)
        Collider[] enemies = Physics.OverlapSphere(origin, detectionRange, enemyLayer, QueryTriggerInteraction.Ignore);
        if (debugLogs) Debug.Log($"[Flashlight] candidatos en radio: {enemies.Length}");

        foreach (var col in enemies)
        {
            if (!col) continue;

            Vector3 toEnemy = col.bounds.center - origin;
            float dist = toEnemy.magnitude;
            if (dist > detectionRange) continue;

            // 2) Dentro del cono
            float ang = Vector3.Angle(forward, toEnemy);
            if (ang > halfAngle)
            {
                // Enemigos con EnemyBehavior se "descongelan" al salir del cono
                var ebOff = col.GetComponent<EnemyBehavior>();
                if (ebOff) ebOff.Freeze(false);
                continue;
            }

            Vector3 dir = toEnemy.normalized;

            // 3) RaycastAll: tomamos el primer impacto real (ignorando colliders del player)
            int maskAll = obstacleMask | enemyLayer;
            var hits = Physics.RaycastAll(origin, dir, dist, maskAll, QueryTriggerInteraction.Ignore)
                               .OrderBy(h => h.distance);

            RaycastHit? firstValid = null;
            foreach (var h in hits)
            {
                if (playerRoot && h.collider.transform.IsChildOf(playerRoot)) continue; // ignora player
                firstValid = h; break;
            }

            bool inLight = false;
            if (firstValid.HasValue)
            {
                inLight = (firstValid.Value.collider == col);
                if (drawRays)
                    Debug.DrawLine(origin, firstValid.Value.point, inLight ? Color.green : Color.red, 0.05f);
                if (debugLogs && !inLight)
                    Debug.Log($"[Flashlight] Bloqueado por: {firstValid.Value.collider.name} (layer {LayerMask.LayerToName(firstValid.Value.collider.gameObject.layer)})");
            }
            else
            {
                // No golpeó nada antes: vía libre
                inLight = true;
                if (drawRays) Debug.DrawLine(origin, col.bounds.center, Color.green, 0.05f);
            }

            // 4) Aplica efecto según tipo de enemigo
            var eb = col.GetComponent<EnemyBehavior>();
            if (eb) eb.Freeze(inLight); // tu lógica original para otros enemigos

            var silbon = col.GetComponent<SilbonAI>();
            if (silbon && inLight)
            {
                if (debugLogs) Debug.Log("[Flashlight] Silbón iluminado → OnLitByFlashlight()");
                silbon.OnLitByFlashlight(flashlight.transform, 1f);
            }
        }
    }

    void UnfreezeAllEnemies()
    {
        foreach (EnemyBehavior enemy in FindObjectsOfType<EnemyBehavior>())
            enemy.Freeze(false);
    }

    void OnDrawGizmosSelected()
    {
        if (!flashlight) return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        Gizmos.DrawWireSphere(flashlight.transform.position, detectionRange);
    }
}
