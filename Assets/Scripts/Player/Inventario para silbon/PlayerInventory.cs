using UnityEngine;

[System.Serializable]
public class TuboData
{
    public string id;
    public GameObject prefab;
    public bool collected;
}

public class PlayerInventory : MonoBehaviour
{
    [Header("Tubos disponibles")]
    public TuboData[] tubos;

    [HideInInspector] public string tuboEquipado;

    public void AddTubo(string tuboID)
    {
        foreach (var t in tubos)
        {
            if (t.id == tuboID)
            {
                t.collected = true;
                Debug.Log($"🧩 Tubo añadido al inventario: {tuboID}");
                return;
            }
        }
        Debug.LogWarning($"❌ Intentaste agregar un tubo desconocido: {tuboID}");
    }

    public TuboData GetTuboByID(string id)
    {
        foreach (var t in tubos)
            if (t.id == id) return t;
        return null;
    }
}
