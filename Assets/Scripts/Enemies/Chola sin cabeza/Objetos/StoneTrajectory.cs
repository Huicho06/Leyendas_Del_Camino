using UnityEngine;

public class StoneTrajectory : MonoBehaviour
{
    public Transform startPoint;       // punto donde sostienes la piedra (holdPoint)
    public int resolution = 30;        // cuántos puntos dibujar
    public float throwForce = 10f;     // fuerza que se usará
    public LineRenderer lineRenderer;  // asigna un LineRenderer en el inspector

    void Update()
    {
        // No va a dibujar nada por sí mismo, lo llamaremos desde PlayerMovement
        // Mantener vació o puedes poner un Debug opcional
    }

    public void RenderTrajectory()
    {
        Vector3[] points = new Vector3[resolution];
        Vector3 startPos = startPoint.position;
        Vector3 startVel = startPoint.forward * throwForce;

        for (int i = 0; i < resolution; i++)
        {
            float t = i * 0.1f;
            points[i] = startPos + startVel * t + 0.5f * Physics.gravity * t * t;
        }

        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
    }

    public void ClearTrajectory()
    {
        lineRenderer.positionCount = 0;
    }
}
