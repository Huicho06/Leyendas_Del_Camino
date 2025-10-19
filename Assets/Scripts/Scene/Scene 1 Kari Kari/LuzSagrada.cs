using System.Linq;
using UnityEngine;

public class LuzSagrada : MonoBehaviour
{
    [Header("Configuración de la luz")]
    public Light luz;                     // Asigna la luz del foco
    public float detectionRange = 15f;    // Alcance de la luz
    public LayerMask enemyLayer;          // Capa Enemy
    public LayerMask obstacleMask = ~0;   // Paredes, etc.

    public bool encendida = false;
    public bool debug = false;

    void Start()
    {
        if (luz != null)
            luz.enabled = encendida;
    }

    void Update()
    {
        if (encendida)
            RevisarEnemigos();
    }

    public void Activar()
    {
        encendida = true;
        if (luz) luz.enabled = true;
    }

    public void Desactivar()
    {
        encendida = false;
        if (luz) luz.enabled = false;
        DescongelarTodos();
    }

    void RevisarEnemigos()
    {
        if (!luz) return;

        Vector3 origen = luz.transform.position;
        Vector3 direccion = luz.transform.forward;
        float rango = detectionRange;
        float mitadAngulo = luz.spotAngle * 0.5f;

        Collider[] enemigos = Physics.OverlapSphere(origen, rango, enemyLayer);
        foreach (var col in enemigos)
        {
            if (!col) continue;
            Vector3 haciaEnemigo = col.bounds.center - origen;
            float dist = haciaEnemigo.magnitude;
            float angulo = Vector3.Angle(direccion, haciaEnemigo);
            if (angulo > mitadAngulo) continue;

            // Raycast para verificar obstáculos
            var hits = Physics.RaycastAll(origen, haciaEnemigo.normalized, dist, obstacleMask | enemyLayer)
                             .OrderBy(h => h.distance);
            RaycastHit? primero = hits.FirstOrDefault(h => !h.collider.isTrigger);
            if (!primero.HasValue) continue;

            bool enLuz = primero.Value.collider == col;
            var eb = col.GetComponent<EnemyBehavior>();
            if (eb)
                eb.Freeze(enLuz); // ← Aquí se congela al enemigo
        }
    }

    void DescongelarTodos()
    {
        foreach (EnemyBehavior e in FindObjectsOfType<EnemyBehavior>())
            e.Freeze(false);
    }
}
