using UnityEngine;

[ExecuteInEditMode]
public class AlignTreeBase : MonoBehaviour
{
    [ContextMenu("Align Mesh Base To Y=0")]
    void Align()
    {
        var mf = GetComponentInChildren<MeshFilter>();
        if (!mf || !mf.sharedMesh) return;

        var b = mf.sharedMesh.bounds;           // bounds en espacio local de la malla
        float minY = b.min.y * mf.transform.localScale.y;
        var lp = mf.transform.localPosition;
        mf.transform.localPosition = new Vector3(lp.x, lp.y - minY, lp.z);
    }
}
