using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GeneradorInteractable : MonoBehaviour
{
    [Header("UI")]
    public GameObject uiJuego;
    public RectTransform barra;
    public RectTransform zonaObjetivo;
    public RectTransform cursor;
    public TMP_Text feedbackText;

    [Header("Sonido")]
    public AudioSource audioSource;
    public AudioClip clicClip;
    public AudioClip exitoClip;

    [Header("Configuración")]
    public float velocidadCursor = 200f;
    public int golpesNecesarios = 3;
    public float zonaTamaño = 50f;

    [Header("Palancas")]
    public List<PalancaLuzAnimada> palancas;

    private int golpesCorrectos = 0;
    [HideInInspector] public bool jugando = false;
    private bool subiendo = true;
    private Vector2 barraMinMax;
    [HideInInspector] public bool estaCompletado = false;

    void Start()
    {
        if (uiJuego) uiJuego.SetActive(false);
        barraMinMax = new Vector2(barra.rect.yMin, barra.rect.yMax);
        ResetJuego();

        foreach (var palanca in palancas)
            if (palanca != null)
                palanca.ForzarEncendido();
    }

    void Update()
    {
        if (!jugando) return;

        float delta = velocidadCursor * Time.deltaTime;
        cursor.anchoredPosition += new Vector2(0, subiendo ? delta : -delta);

        if (cursor.anchoredPosition.y >= barra.rect.yMax) subiendo = false;
        if (cursor.anchoredPosition.y <= barra.rect.yMin) subiendo = true;

        if (Input.GetKeyDown(KeyCode.E))
            IntentarGolpe();
    }

    private void IntentarGolpe()
    {
        float cursorY = cursor.anchoredPosition.y;
        float zonaY = zonaObjetivo.anchoredPosition.y;

        if (cursorY >= zonaY - zonaTamaño / 2 && cursorY <= zonaY + zonaTamaño / 2)
        {
            golpesCorrectos++;
            feedbackText.text = $"Golpe correcto! {golpesCorrectos}/{golpesNecesarios}";
            if (audioSource && clicClip) audioSource.PlayOneShot(clicClip);

            if (golpesCorrectos >= golpesNecesarios)
            {
                GeneradorEncendido();
                return;
            }
        }
        else
        {
            feedbackText.text = "Fallaste! Reinicia.";
            golpesCorrectos = 0;
            if (audioSource && clicClip) audioSource.PlayOneShot(clicClip);
        }

        float nuevaY = Random.Range(barraMinMax.x + zonaTamaño / 2, barraMinMax.y - zonaTamaño / 2);
        zonaObjetivo.anchoredPosition = new Vector2(zonaObjetivo.anchoredPosition.x, nuevaY);
    }

    private void GeneradorEncendido()
    {
        jugando = false;
        feedbackText.text = "Generador encendido!";
        if (audioSource && exitoClip) audioSource.PlayOneShot(exitoClip);

        foreach (var palanca in palancas)
            if (palanca != null)
                palanca.ForzarEncendido();


        estaCompletado = true;

        StartCoroutine(CerrarUI());

    }

    private System.Collections.IEnumerator CerrarUI()
    {
        yield return new WaitForSeconds(1.5f);
        if (uiJuego) uiJuego.SetActive(false);
    }

    public void IniciarMinijuego()
    {
        ResetJuego();
        jugando = true;

        if (uiJuego) uiJuego.SetActive(true);
        if (feedbackText) feedbackText.text = "¡Inicia el generador!";
    }

    private void ResetJuego()
    {
        golpesCorrectos = 0;
        float nuevaY = Random.Range(barraMinMax.x + zonaTamaño / 2, barraMinMax.y - zonaTamaño / 2);
        zonaObjetivo.anchoredPosition = new Vector2(zonaObjetivo.anchoredPosition.x, nuevaY);
        cursor.anchoredPosition = new Vector2(cursor.anchoredPosition.x, barraMinMax.x);
        subiendo = true;
    }
}
