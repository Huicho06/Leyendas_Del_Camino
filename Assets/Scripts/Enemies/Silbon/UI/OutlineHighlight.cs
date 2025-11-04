using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class OutlineHighlight : MonoBehaviour
{
    private Renderer rend;
    private Material originalMat;
    public Material highlightMat;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
            originalMat = rend.material;
    }

    public void SetHighlight(bool estado)
    {
        if (rend == null || highlightMat == null) return;

        if (estado)
            rend.material = highlightMat;
        else
            rend.material = originalMat;
    }
}
