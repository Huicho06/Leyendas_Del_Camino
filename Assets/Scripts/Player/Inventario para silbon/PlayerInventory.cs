using UnityEngine;

[System.Serializable]
public class TuboData
{
    public string id;
    public GameObject prefab;
    public bool collected;
    public bool placed; // ← NUEVO: se marca true cuando se coloca
}

public class PlayerInventory : MonoBehaviour
{
    public TuboData[] tubos;
    public string tuboEquipado;

    public void AddTubo(string id)
    {
        foreach (var t in tubos)
        {
            if (t.id == id)
            {
                t.collected = true;
                Debug.Log($"✅ Tubo {id} agregado al inventario");
                return;
            }
        }
        FindObjectOfType<PlayerMovement>().ActualizarItemsDisponibles();

    }

    public void MarcarColocado(string id)
    {
        foreach (var t in tubos)
        {
            if (t.id == id)
            {
                t.placed = true;
                Debug.Log($"📦 Tubo {id} marcado como colocado");
                return;
            }
        }
    }

    public bool EstaColocado(string id)
    {
        foreach (var t in tubos)
        {
            if (t.id == id)
                return t.placed;
        }
        return false;
    }
}
