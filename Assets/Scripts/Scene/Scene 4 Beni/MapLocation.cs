using UnityEngine;
using UnityEngine.UI;

public class MapLocation : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;        // Jugador en el mundo
    public Transform playerIcon;    // Icono que se moverá sobre el mapa
    public SpriteRenderer mapSprite; // Sprite del mapa

    [Header("Rango del mundo (en coordenadas del terreno real)")]
    public Vector2 worldMin; // esquina inferior izquierda (en el mundo real)
    public Vector2 worldMax; // esquina superior derecha (en el mundo real)

    private Vector2 mapSize;   // tamaño del mapa en unidades locales

    void Start()
    {
        if (!mapSprite)
        {
            Debug.LogError("⚠️ Asigna el SpriteRenderer del mapa.");
            enabled = false;
            return;
        }

        // tamaño visible del sprite (en unidades del mundo)
        mapSize = mapSprite.bounds.size;
    }

    void Update()
    {
        if (!player || !playerIcon) return;

        // 1️⃣ Obtener posición del jugador (X,Z del mundo)
        Vector2 playerPos = new Vector2(player.position.x, player.position.z);

        // 2️⃣ Normalizar entre 0 y 1 según límites del mundo
        float normX = Mathf.InverseLerp(worldMin.x, worldMax.x, playerPos.x);
        float normY = Mathf.InverseLerp(worldMin.y, worldMax.y, playerPos.y);

        // 3️⃣ Calcular la posición dentro del mapa (en coordenadas locales)
        float localX = Mathf.Lerp(-mapSize.x / 2, mapSize.x / 2, normX);
        float localY = Mathf.Lerp(-mapSize.y / 2, mapSize.y / 2, normY);

        // 4️⃣ Aplicar posición al icono del jugador
        playerIcon.localPosition = new Vector3(localX, localY, playerIcon.localPosition.z);
    }
}
