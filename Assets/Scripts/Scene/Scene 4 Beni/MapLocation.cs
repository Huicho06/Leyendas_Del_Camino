using UnityEngine;
using UnityEngine.UI;

public class MapLocation : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;          // El jugador
    public Transform playerIcon;      // El ícono del jugador sobre el mapa
    public SpriteRenderer mapSprite;  // Sprite del mapa

    [Header("Marcadores de esquina")]
    public Transform worldMinMarker;  // Esquina inferior izquierda del mundo
    public Transform worldMaxMarker;  // Esquina superior derecha del mundo

    private Vector2 worldMin;
    private Vector2 worldMax;
    private Vector2 mapSize;

    void Start()
    {
        if (!mapSprite)
        {
            Debug.LogError("⚠️ Falta asignar el SpriteRenderer del mapa.");
            enabled = false;
            return;
        }

        // Si hay marcadores, usa sus coordenadas automáticamente
        if (worldMinMarker && worldMaxMarker)
        {
            worldMin = new Vector2(worldMinMarker.position.x, worldMinMarker.position.z);
            worldMax = new Vector2(worldMaxMarker.position.x, worldMaxMarker.position.z);
        }
        else
        {
            // En caso de que no existan, usa los bounds del sprite
            worldMin = new Vector2(mapSprite.bounds.min.x, mapSprite.bounds.min.z);
            worldMax = new Vector2(mapSprite.bounds.max.x, mapSprite.bounds.max.z);
        }

        mapSize = mapSprite.bounds.size;
    }

    void Update()
    {
        if (!player || !playerIcon) return;

        // 1️⃣ Obtener posición del jugador (X,Z del mundo)
        Vector2 playerPos = new Vector2(player.position.x, player.position.z);

        // 2️⃣ Normalizar entre 0 y 1 dentro de los límites del mundo
        float normX = Mathf.InverseLerp(worldMin.x, worldMax.x, playerPos.x);
        float normY = Mathf.InverseLerp(worldMin.y, worldMax.y, playerPos.y);

        // 3️⃣ Calcular posición local dentro del mapa
        float localX = Mathf.Lerp(-mapSize.x / 2, mapSize.x / 2, normX);
        float localY = Mathf.Lerp(-mapSize.y / 2, mapSize.y / 2, normY);

        // 4️⃣ Aplicar posición
        playerIcon.localPosition = new Vector3(localX, localY, playerIcon.localPosition.z);
    }
}
