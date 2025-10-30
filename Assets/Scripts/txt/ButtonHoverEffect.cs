using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image background;
    public RectTransform rectTransform;
    public Color normalColor = new Color(0.77f, 0.66f, 0.45f); // beige
    public Color hoverColor = new Color(0.90f, 0.75f, 0.50f);  // más claro
    public float scaleFactor = 1.05f;
    public float speed = 8f;

    private bool isHovered;

    void Start()
    {
        if (background == null)
            background = GetComponent<Image>();
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        float targetScale = isHovered ? scaleFactor : 1f;
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, Vector3.one * targetScale, Time.deltaTime * speed);
        background.color = Color.Lerp(background.color, isHovered ? hoverColor : normalColor, Time.deltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovered = true;
    public void OnPointerExit(PointerEventData eventData) => isHovered = false;
}
