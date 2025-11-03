using UnityEngine;
using System.Collections;

public class PathFollower : MonoBehaviour
{
    [Header("Ruta del alma (Ajayu)")]
    public Transform[] waypoints;        // puntos del camino
    public float moveSpeed = 2f;         // velocidad de movimiento
    public float turnSpeed = 5f;         // suavizado al girar
    public bool loop = false;            // repetir camino
    public bool autoStart = false;       // si inicia sola
    public Animator animator;            // opcional (para animación de flotar o caminar)

    private int currentIndex = 0;
    private bool isMoving = false;

    void Start()
    {
        if (autoStart) BeginRoute();
    }

    public void BeginRoute()
    {
        if (waypoints.Length == 0) return;
        currentIndex = 0;
        isMoving = true;

        if (animator)
            animator.SetBool("isMoving", true);
    }

    void Update()
    {
        if (!isMoving || waypoints.Length == 0)
            return;

        Transform target = waypoints[currentIndex];
        Vector3 dir = (target.position - transform.position).normalized;

        // movimiento
        transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

        // rotación suave
        if (dir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, turnSpeed * Time.deltaTime);
        }

        // siguiente punto
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist < 0.3f)
        {
            currentIndex++;

            if (currentIndex >= waypoints.Length)
            {
                if (loop)
                    currentIndex = 0;
                else
                    StopRoute();
            }
        }
    }

    public void StopRoute()
    {
        isMoving = false;
        if (animator)
            animator.SetBool("isMoving", false);
    }

    // útil para activar recuerdos o eventos al llegar a ciertos puntos
    public void TriggerAtPoint()
    {
        SoulAI soul = GetComponent<SoulAI>();
        if (soul)
            soul.TriggerMemory();
    }

    // para depuración en editor
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2)
            return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] && waypoints[i + 1])
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
    }
}
