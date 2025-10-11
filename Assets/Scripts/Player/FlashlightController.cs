using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Configuración de la linterna")]
    public Light flashlight;                // Luz tipo Spot
    public float detectionRange = 20f;      // Alcance de la linterna
    public LayerMask enemyLayer;            // Capa de los enemigos

    private void Start()
    {
        if (flashlight != null)
            flashlight.enabled = false;
    }

    private void Update()
    {
        // Click izquierdo para encender/apagar
        if (Input.GetMouseButtonDown(0) && flashlight != null)
            flashlight.enabled = !flashlight.enabled;

        // Si está encendida, verificar enemigos
        if (flashlight != null && flashlight.enabled)
            CheckEnemiesInLight();
        else
            UnfreezeAllEnemies();
    }

    private void CheckEnemiesInLight()
    {
        Vector3 origin = flashlight.transform.position;
        Vector3 direction = flashlight.transform.forward;

        Collider[] enemies = Physics.OverlapSphere(origin, detectionRange, enemyLayer);

        foreach (Collider enemy in enemies)
        {
            if (enemy == null) continue;

            Vector3 dirToEnemy = (enemy.transform.position - origin).normalized;
            float angle = Vector3.Angle(direction, dirToEnemy);

            bool inLight = false;

            if (angle < flashlight.spotAngle / 2f)
            {
                if (Physics.Raycast(origin, dirToEnemy, out RaycastHit hit, detectionRange))
                {
                    if (hit.collider == enemy)
                        inLight = true;
                }
            }

            enemy.GetComponent<EnemyBehavior>()?.Freeze(inLight);
        }
    }

    private void UnfreezeAllEnemies()
    {
        foreach (EnemyBehavior enemy in FindObjectsOfType<EnemyBehavior>())
            enemy.Freeze(false);
    }
}
