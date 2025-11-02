using System.Linq;
using UnityEngine;

public class LuzSagrada : MonoBehaviour
{
    [Header("Configuración de la luz")]
    public Light luz;
    public float detectionRange = 15f;
    public LayerMask enemyLayer;
    public LayerMask obstacleMask = ~0;
    public bool encendida = false; // se controla desde la palanca
    public bool debug = false;

    [HideInInspector] public bool estaTocada = false;

    public event System.Action<LuzSagrada> OnEnemyTouchLight;

    void Start()
    {
        encendida = false;
        estaTocada = false;
        if (luz != null)
            luz.enabled = false;

        // Asegurar que quede apagada aunque otro script la active antes del primer frame
        Invoke(nameof(Desactivar), 0.05f);
    }


    void Update()
    {
        if (encendida && !estaTocada)
            RevisarEnemigos();
    }

    public void Activar()
    {
        encendida = true;
        estaTocada = false;
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
        float mitadAngulo = luz.spotAngle * 0.5f;

        Collider[] enemigos = Physics.OverlapSphere(origen, detectionRange, enemyLayer);
        foreach (var col in enemigos)
        {
            if (!col) continue;
            Vector3 haciaEnemigo = col.bounds.center - origen;
            float dist = haciaEnemigo.magnitude;
            float angulo = Vector3.Angle(direccion, haciaEnemigo);
            if (angulo > mitadAngulo) continue;

            var hits = Physics.RaycastAll(origen, haciaEnemigo.normalized, dist, obstacleMask | enemyLayer)
                             .OrderBy(h => h.distance);
            RaycastHit? primero = hits.FirstOrDefault(h => !h.collider.isTrigger);
            if (!primero.HasValue) continue;

            bool enLuz = primero.Value.collider == col;
            var eb = col.GetComponent<EnemyBehavior>();
            if (eb)
                eb.Freeze(enLuz);

            if (enLuz && !estaTocada)
            {
                estaTocada = true;
                OnEnemyTouchLight?.Invoke(this);
            }
        }
    }

    void DescongelarTodos()
    {
        foreach (EnemyBehavior e in FindObjectsOfType<EnemyBehavior>())
            e.Freeze(false);
    }

    void OnDrawGizmosSelected()
    {
        if (!debug || luz == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(luz.transform.position, detectionRange);
    }
}
